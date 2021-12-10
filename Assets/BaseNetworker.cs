using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using UnityEngine.SceneManagement;

public class BaseNetworker : MonoBehaviour
{
	static float CurrentTime;
	static float DeltaTime;

	public int PlayerIndex;
	public int TotalPlayers;

	public string ServerIP = "127.0.0.1";
	[SerializeField] protected int ServerPort = 4444;

	[SerializeField] protected PlayerController[] AllPlayers;

	[SerializeField] GameObject PlayerPrefab, OnlinePrefab;

	protected bool RoundStarted = false;

	protected void ParseRead(byte[] bytes)
	{
		//Debug.Log("reading " + System.Text.Encoding.Default.GetString(bytes));
		foreach (string packet in System.Text.Encoding.Default.GetString(bytes).Split('\n'))
		{
			string[] splitPacket = packet.Split(new char[] { ':' }, 2); // packages are split by the format "MessageType":"MessageData"
			switch (splitPacket[0])
			{
				case "TransformPacket": // contains various data regarding positioning from another player
					PlayerController.PositionalPackage ReadInputs = JsonUtility.FromJson<PlayerController.PositionalPackage>(splitPacket[1]); // deserialize the struct
					AllPlayers[ReadInputs.PlayerIndex].Unpack(ReadInputs); // set transform data to the proper player
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
					HalfPingBuffer = float.Parse(splitPacket[1]) - GetBufferedTime(); // we subtract the current time to make sure we are zeroed out with the server
					TimeAtNextSend = GetBufferedTime();
					RoundStarted = true;
					break;

			}
		}

	}

	// adapted from https://stackoverflow.com/questions/1387459/how-to-check-if-tcpclient-connection-is-closed
	public bool isClientConnected(TcpClient client)
	{
		IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

		TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();

		foreach (TcpConnectionInformation c in tcpConnections)
		{
			TcpState stateOfConnection = c.State;

			if (c.LocalEndPoint.Equals(client.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
			{
				if (stateOfConnection == TcpState.Established)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		return false;
	}

	void OnEnable()
	{
		Debug.Log("Lego impression: HEY");
		//Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	void OnDisable()
	{
		//Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	protected virtual void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		Debug.Log(scene.name);
		if (scene.buildIndex == 1)
		{

			Transform spawnpoints = GameObject.Find("SpawnPoints").transform;
			AllPlayers = new PlayerController[TotalPlayers];
			
			for (int i = 0; i < TotalPlayers; i++)
			{
				if (i == PlayerIndex)
					AllPlayers[i] = Instantiate(PlayerPrefab, spawnpoints.GetChild(i).position, spawnpoints.GetChild(i).rotation).GetComponent<PlayerController>();
				else
					AllPlayers[i] = Instantiate(OnlinePrefab, spawnpoints.GetChild(i).position, spawnpoints.GetChild(i).rotation).GetComponent<PlayerController>();
				AllPlayers[i].ActiveInputs.PlayerIndex = i;
			}
		}
	}

	protected float HalfPingBuffer = 0; // helps get the time with respect to the time it takes for a message to be sent to the server
	[SerializeField, Tooltip("How many frames in between sending data to the server or vice versa")] protected float MilisecondsBetweenSends = 30;
	protected float TimeAtNextSend;

	public float GetSendRate()
	{
		return MilisecondsBetweenSends * 0.001f;
	}

	public float GetBufferedTime()
	{
		 return Time.realtimeSinceStartup + HalfPingBuffer;
	}
}
