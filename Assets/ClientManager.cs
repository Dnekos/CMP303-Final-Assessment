using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;


public class ClientManager : BaseNetworker
{
	TcpClient client;
	NetworkStream stream;

	// Start is called before the first frame update
	void Start()
	{
		IPAddress address = IPAddress.Parse(ServerIP);

		client = new TcpClient();
		if (!client.ConnectAsync(address, ServerPort).Wait(1000))
		{
			Debug.LogError("failed to connect");
		}
		else
		{			
			Debug.LogError("Connected");
			stream = client.GetStream();
		}
	}

	
	// Update is called once per frame
	void Update()
	{
		if (client.Connected)	
		{
			if (AllPlayers.Length > 0 && stream.CanWrite)
			{
				string jsoninputs = "TransformPacket:" + JsonUtility.ToJson(AllPlayers[PlayerIndex].PackUp()) + "\n";
				byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
				stream.Write(buffer, 0, buffer.Length);
			}
			
			if (stream.CanRead && stream.DataAvailable)
			{
				byte[] bytes = new byte[client.ReceiveBufferSize];
				stream.Read(bytes, 0, (int)client.ReceiveBufferSize);

				ParseRead(bytes);
			}
			
		}
		
	}
	
}
