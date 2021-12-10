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
		if (server.Pending() && AllPlayers.Length == 0) // if there is a client trying to connect, and the game hasnt started
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
			if (streams[i].CanRead && streams[i].DataAvailable) // check if there are packets available to read
			{
				byte[] bytes = new byte[clients[i].ReceiveBufferSize];
				streams[i].Read(bytes, 0, (int)clients[i].ReceiveBufferSize); // read it
				ParseRead(bytes); // interpret the data
			}

			if (AllPlayers.Length > 0 && streams[i].CanWrite && GetBufferedTime() > TimeAtNextSend) // if we can write and have players in game
			{
				// set next intravel to send transformdata
				TimeAtNextSend += MilisecondsBetweenSends * 0.001f;

				string jsoninputs = "TransformPacket:" + JsonUtility.ToJson(AllPlayers[PlayerIndex].PackUp()) + "\n";
				byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
				streams[i].Write(buffer, 0, buffer.Length);
			}
		}
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
				string message = "HalfPing:" + (reply.RoundtripTime * 0.5f) + "\n";
				byte[] buffer = System.Text.Encoding.Default.GetBytes(message);
				streams[i].Write(buffer, 0, buffer.Length);
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

}
