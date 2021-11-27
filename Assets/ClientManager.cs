using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

public class ClientManager : MonoBehaviour
{
	public string Host = "127.0.0.1";
	[SerializeField] int ServerPort = 4444, ClientPort = 4445;

	[SerializeField] PlayerController player;

	Socket soc;
	TcpListener server;
	TcpClient client;
	ArrayList SocList;


	// Start is called before the first frame update
	void Start()
	{
		IPAddress address = IPAddress.Parse(Host);

		client = new TcpClient();
		client.Connect(address, ServerPort);
	}


	// Update is called once per frame
	void Update()
	{

		if (client.Connected)
		{
			NetworkStream stream = client.GetStream();

			if (player != null)
			{
				string jsoninputs = player.ActiveInputs.GetType() + ":" + JsonUtility.ToJson(player.ActiveInputs) + "\n";
				byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
				stream.Write(buffer, 0, buffer.Length);
			}
			if (stream.CanRead)
			{
				byte[] bytes = new byte[client.ReceiveBufferSize];
				stream.Read(bytes, 0, (int)client.ReceiveBufferSize);

				Debug.Log(System.Text.Encoding.Default.GetString(bytes));
				
				
				//player.ActiveInputs = JsonUtility.FromJson<PlayerController.Inputs>(System.Text.Encoding.Default.GetString(bytes));

			}
		}

	}
}
