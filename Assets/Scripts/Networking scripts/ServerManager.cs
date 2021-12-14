using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class ServerManager : BaseNetworker
{
	[SerializeField, Header("Server Variables")]
	int MaxClients = 3;
	TcpListener server;
	List<NetworkStream> streams;
	public List<TcpClient> clients;

	// Start is called before the first frame update
	void Start()
	{
		IPAddress address = IPAddress.Parse(ServerIP);

		server = new TcpListener(address, ServerPort);
		server.Start();

		clients = new List<TcpClient>();
		streams = new List<NetworkStream>();

		PlayerIndex = 0;
	}
	
	/// <summary>
	/// send a message to each client to change scenes together
	/// </summary>
	public void AllGoToScene(int index)
	{
		foreach (NetworkStream stream in streams)
			if (stream.CanWrite)
			{
				byte[] buffer = System.Text.Encoding.Default.GetBytes("SceneChange:" + index + "," + (clients.Count + 1));
				stream.Write(buffer, 0, buffer.Length);
			}
		TotalPlayers = clients.Count + 1; // determine the final playercount
		SceneManager.LoadScene(index); // the server loads the scene as well now
	}

	// Update is called once per frame
	void Update()
	{
		if (server.Pending() && clients.Count < MaxClients && AllPlayers.Length == 0) // if there is a client trying to connect, and the game hasnt started
		{ 
			// add client to lists
			clients.Add(server.AcceptTcpClient());
			streams.Add(clients[clients.Count - 1].GetStream());
			Debug.Log("I found " + clients[clients.Count-1].Client);

			// send client packet to tell them which player ID to have
			byte[] buffer = System.Text.Encoding.Default.GetBytes("Index:"+ streams.Count);
			streams[streams.Count-1].Write(buffer, 0, buffer.Length);
		}

		for (int i = 0; i < clients.Count; i++) // iterate through connected clients
		{
			// check if client is still connected. if not, safely disconnect
			if (!isClientConnected(clients[i]))
			{
				foreach (TcpClient client in clients)
					client.Close();
				SetUpDisconnect();
				return;
			}

			// check if there are packets available to read
			if (streams[i].CanRead && streams[i].DataAvailable) 
			{
				byte[] bytes = new byte[clients[i].ReceiveBufferSize];
				streams[i].Read(bytes, 0, (int)clients[i].ReceiveBufferSize); // read it
				ParseRead(bytes); // interpret the data
			}
		}

		// if we have players in game and the timer for sending out transform data is done, send out transform data
		if (AllPlayers.Length > 0 && GetBufferedTime() > TimeAtNextSend)
		{
			// set next intravel to send transform data
			TimeAtNextSend += MilisecondsBetweenSends * 0.001f;

			// send data
			SendTransformPacketToAllPlayers(AllPlayers[PlayerIndex].PackUp());
		}

	}

	/// <summary>
	/// override that makes sure to send data to each other player as well
	/// </summary>
	protected override void ApplyTransformPacketToPlayer(string packet)
	{
		base.ApplyTransformPacketToPlayer(packet);
		SendTransformPacketToAllPlayers(JsonUtility.FromJson<PlayerController.PositionalPackage>(packet));
	}

	void SendTransformPacketToAllPlayers(PlayerController.PositionalPackage packet)
	{
		for (int i = 0; i < streams.Count; i++)
		{
			if (i+1 != packet.PlayerIndex && streams[i].CanWrite) // make sure we dont send transform packet to the player it came from
			{
				string jsoninputs = "TransformPacket:" + JsonUtility.ToJson(packet) + "\n";
				byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
				streams[i].Write(buffer, 0, buffer.Length);
			}
		}
	}

	/// <summary>
	/// this game uses a recording of recent server-side positions to determine if it thinks a hit indeed landed
	/// </summary>
	/// <param name="hit">struct containing information about the hit</param>
	public override void SendHitRegistration(HealthManager.HitMarker hit)
	{
		// if this hit occured server side, we know that it hit
		if (hit.PlayerIndex == PlayerIndex)
		{
			SendConfirmedHit(hit);
			return;
		}

		// go through the list of recorded positions for that player, most recent first
		for (int p = AllPlayers[hit.PlayerIndex].RecordedPositions.Count - 1; p > -1; p--)
		{
			KeyValuePair<float, Vector3> pos = AllPlayers[hit.PlayerIndex].RecordedPositions[p];
			if (pos.Key <= hit.TimeStamp) // find the first recording position that the hit could have been
			{
				// check if the bullet is inside
				if (AllPlayers[hit.PlayerIndex].GetComponent<Collider>().bounds.Contains(hit.BulletPosition))
				{
					Debug.Log("HIT");
					SendConfirmedHit(hit);
				}
				return; // stop looking
			}
		}
				
	}
	/// <summary>
	/// short function for the server to tell all players about the confirmed hit
	/// </summary>
	/// <param name="hit"></param>
	void SendConfirmedHit(HealthManager.HitMarker hit)
	{
		string jsoninputs = "ConfirmedHit:" + JsonUtility.ToJson(hit) + "\n";
		byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
		foreach (NetworkStream stream in streams)
			stream.Write(buffer, 0, buffer.Length);
		AllPlayers[hit.PlayerIndex].health.TakeDamage(hit.damage);
	}

	/// <summary>
	/// overrides the base OnLevelFinishedLoading to do server specific stuff, like determining Ping
	/// </summary>
	protected override void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		base.OnLevelFinishedLoading(scene, mode);
		if (scene.buildIndex == 1)
		{
			for (int i = 0; i < clients.Count; i++)
			{
				string clientIP = (clients[i].Client.RemoteEndPoint as IPEndPoint).Address.ToString();
				Ping uping = new Ping(clientIP);

				// send ping
				System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
				System.Net.NetworkInformation.PingReply reply = ping.Send(clientIP, 1000);
				Debug.Log("Client Ping is " + reply.RoundtripTime);

				// tell clients what the ping is
				string message = "HalfPing:" + (reply.RoundtripTime * 0.5f * 0.001) + "\n";
				byte[] buffer = System.Text.Encoding.Default.GetBytes(message);
				streams[i].Write(buffer, 0, buffer.Length);


				AllPlayers[i + 1].Ping = Mathf.Max(1, reply.RoundtripTime * 0.5f); // we want 1 to be the min for this as this Ping is used for interpolation smoothing
			}
			// ping buffer is the ping with respect to the server - time ar level load. So for the server itself the ping is 0
			PingBuffer = -GetBufferedTime();
		}
	}
	private void OnApplicationQuit()
	{
		// make sure we close the client cleanly when closing the game
		foreach (TcpClient client in clients)
			client.Close();
	}
	private void OnDestroy()
	{
		OnApplicationQuit();
	}

}
