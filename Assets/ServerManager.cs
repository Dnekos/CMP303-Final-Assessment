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
	
	public void AllGoToScene(int index)
	{
		Debug.Log("wut");
		foreach (NetworkStream stream in streams)
			if (stream.CanWrite)
			{
				byte[] buffer = System.Text.Encoding.Default.GetBytes("SceneChange:" + index + "," + (clients.Count + 1));
				stream.Write(buffer, 0, buffer.Length);
			}
		TotalPlayers = clients.Count + 1;
		SceneManager.LoadScene(index);
	}

	// Update is called once per frame
	void Update()
	{
		if (server.Pending())
		{ 
			clients.Add(server.AcceptTcpClient());
			Debug.Log("I found " + clients[clients.Count-1].Client);
			streams.Add(clients[clients.Count - 1].GetStream());

			byte[] buffer = System.Text.Encoding.Default.GetBytes("Index:"+ streams.Count);
			streams[streams.Count-1].Write(buffer, 0, buffer.Length);
		}
		else if (clients.Count < 1)
			Debug.Log("Looking for client");

		if (clients.Count > 0) // if we have clients
		{
			for (int i = 0; i < clients.Count;i++) // iterate through them
			{
				if (streams[i].CanRead && streams[i].DataAvailable) // check if there is stuff available to read
				{
					byte[] bytes = new byte[clients[i].ReceiveBufferSize];
					streams[i].Read(bytes, 0, (int)clients[i].ReceiveBufferSize); // read it

					Debug.Log(System.Text.Encoding.Default.GetString(bytes));

					ParseRead(bytes); // interpret the data (usually going through JSON)
				}
				if (AllPlayers.Length > 0 && streams[i].CanWrite) // if we can write and have players in game
				{
					string jsoninputs = "TransformPacket:" + JsonUtility.ToJson(AllPlayers[PlayerIndex].PackUp()) + "\n";
					byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
					streams[i].Write(buffer, 0, buffer.Length);
				}
			}
		}
	}
	protected override void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		base.OnLevelFinishedLoading(scene, mode);
		if (scene.buildIndex == 1)
		{
			string clientIP = (clients[0].Client.RemoteEndPoint as IPEndPoint).Address.ToString();
			Debug.LogError("Client is at " + clientIP);
			Ping uping = new Ping(clientIP);
			
			System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
			System.Net.NetworkInformation.PingReply reply = ping.Send(clientIP);
			Debug.LogError("Client Ping is " + reply.RoundtripTime);
		}
	}
}
