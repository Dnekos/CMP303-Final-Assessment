using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using System.Net.NetworkInformation;

public abstract class BaseNetworker : MonoBehaviour
{
	static float CurrentTime;
	static float DeltaTime;

	public int PlayerIndex;
	public int TotalPlayers;

	public string ServerIP = "127.0.0.1";
	[SerializeField] protected int ServerPort = 4444;

	[SerializeField] protected PlayerController[] AllPlayers;

	[SerializeField] GameObject PlayerPrefab, OnlinePrefab;
	[SerializeField] GameObject DisconnnectUI;

	protected float PingBuffer = 0; // helps get the time with respect to the time it takes for a message to be sent to the server
	[SerializeField, Tooltip("How many frames in between sending data to the server or vice versa")] protected float MilisecondsBetweenSends = 30;
	protected float TimeAtNextSend;

	protected void ParseRead(byte[] bytes)
	{
		// '\n' is used to seperate complete packets
		foreach (string packet in System.Text.Encoding.Default.GetString(bytes).Split('\n'))
		{
			string[] splitPacket = packet.Split(new char[] { ':' }, 2); // packets are split by the format "MessageType":"MessageData"
			switch (splitPacket[0]) // go to the correct messagetype
			{
				case "TransformPacket": // contains various data regarding positioning from another player
					ApplyTransformPacketToPlayer(splitPacket[1]);
					break;
				case "SceneChange": // tells client to change the scene
					string[] commaindexed = splitPacket[1].Split(',');
					TotalPlayers = int.Parse(commaindexed[1]);
					SceneManager.LoadScene(int.Parse(commaindexed[0]));
					break;
				case "Index": // message tells players which order they joined, so that we can differentiate TransformPackages
					PlayerIndex = int.Parse(splitPacket[1]);
					break;
				case "HalfPing": // message contains the buffer required to synch times, also serves as official start of the round
					PingBuffer = float.Parse(splitPacket[1]) - GetBufferedTime(); // we subtract the current time to make sure we are zeroed out with the server
					TimeAtNextSend = GetBufferedTime();
					break;
				case "DetectedHit":
					HealthManager.HitMarker hit = JsonUtility.FromJson<HealthManager.HitMarker>(splitPacket[1]); // deserialize the struct
					SendHitRegistration(hit);
					break;
				case "ConfirmedHit":
					HealthManager.HitMarker confirmedhit = JsonUtility.FromJson<HealthManager.HitMarker>(splitPacket[1]); // deserialize the struct
					AllPlayers[confirmedhit.PlayerIndex].health.TakeDamage(confirmedhit.damage);
					break;
			}
		}
	}

	protected virtual void ApplyTransformPacketToPlayer(string packet)
	{
		PlayerController.PositionalPackage ReadInputs = JsonUtility.FromJson<PlayerController.PositionalPackage>(packet); // deserialize the struct
		AllPlayers[ReadInputs.PlayerIndex].Unpack(ReadInputs); // set transform data to the proper player
	}

	/// <summary>
	/// Grabs client connection information and checks the state
	/// adapted from https://stackoverflow.com/questions/1387459/how-to-check-if-tcpclient-connection-is-closed
	/// </summary>
	/// <returns>returns state of client connection</returns>
	public bool isClientConnected(TcpClient client)
	{
		IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

		TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
		foreach (TcpConnectionInformation c in tcpConnections)
		{
			// figure out which connection is the specified one
			if (c.LocalEndPoint.Equals(client.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
			{
				if (c.State == TcpState.Established) // check if the connection is still intact
					return true;
				else
					return false;
			}
		}
		return false;
	}

	/// <summary>
	/// sets things up client side so the user can safely exit the game
	/// </summary>
	public void SetUpDisconnect(string DisconnectText = "A player has disconnected from the game")
	{
		Instantiate(DisconnnectUI).GetComponentInChildren<Text>().text = DisconnectText; // open disconnect menu and set text
		AllPlayers[PlayerIndex].enabled = false; // turn off player controls
		Cursor.lockState = CursorLockMode.None; // enable mouse
		Destroy(gameObject); // delete networking script so that it can't accidently write to the closed connection
	}

	public int GetIndex(PlayerController pc)
	{
		for (int i = 0; i < AllPlayers.Length; i++)
		{
			if (AllPlayers[i] == pc)
				return i;
		}
		return -1;
	}

	/// <summary>
	/// this class needs to be overrided since the clientserver won't need to go back and verify, while clients do need the server's hit verification
	/// </summary>
	public abstract void SendHitRegistration(HealthManager.HitMarker hit);

	#region Scene Changing
	/// <summary>
	/// Runs as start of game. Tells our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
	/// </summary>
	void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	/// <summary>
	/// Tells our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled
	/// </summary>
	void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	/// <summary>
	/// Spawns players at premade spawnpoints if the scene is the right one.
	/// </summary>
	protected virtual void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		Debug.Log(scene.name);
		if (scene.buildIndex == 1)
		{
			Transform spawnpoints = GameObject.Find("SpawnPoints").transform;
			AllPlayers = new PlayerController[TotalPlayers];
			
			for (int i = 0; i < TotalPlayers; i++)
			{
				// theres different prefab objects for local and online players, each sharing a common script we want to keep track of
				if (i == PlayerIndex)
					AllPlayers[i] = Instantiate(PlayerPrefab, spawnpoints.GetChild(i).position, spawnpoints.GetChild(i).rotation).GetComponent<PlayerController>();
				else
					AllPlayers[i] = Instantiate(OnlinePrefab, spawnpoints.GetChild(i).position, spawnpoints.GetChild(i).rotation).GetComponent<PlayerController>();
				AllPlayers[i].ActiveInputs.PlayerIndex = i; // marking the local player's index for position packages
			}
		}
	}
	#endregion

	#region Timing Utility Functions
	public float GetSendRate()
	{
		return MilisecondsBetweenSends * 0.001f;
	}

	public float GetBufferedTime()
	{
		 return Time.realtimeSinceStartup + PingBuffer;
	}
	#endregion
}
