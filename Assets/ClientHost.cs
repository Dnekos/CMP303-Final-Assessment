using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

public class ClientHost : MonoBehaviour
{
	[SerializeField] bool isServer = false;
	[SerializeField] string Host = "127.0.0.1";
	[SerializeField] int ServerPort = 4444, ClientPort = 4445;

	[SerializeField] PlayerController player;

	Socket soc;
	TcpListener server;
	TcpClient client;
	ArrayList SocList;
	NetworkStream clientStream;

	// Start is called before the first frame update
	void Start()
    {
		IPAddress address = IPAddress.Parse(Host);

		if (isServer)
		{
			server = new TcpListener(address,ServerPort);
			server.Start();
		}
		else
		{
			client = new TcpClient();
			client.Connect(address, ServerPort);
		}
    }

    // Update is called once per frame
    void Update()
    {
		if (isServer)
		{
			if (server.Pending())
			{
				client = server.AcceptTcpClient();
				Debug.Log("I found " + client.Client);

			}
			else
				Debug.Log("Looking for client");
			if (client != null)
			{
				NetworkStream stream = client.GetStream();
				if (stream.CanRead)
				{
					byte[] bytes = new byte[client.ReceiveBufferSize];
					stream.Read(bytes, 0, (int)client.ReceiveBufferSize);

					Debug.Log(System.Text.Encoding.Default.GetString(bytes));
					player.ActiveInputs = JsonUtility.FromJson<PlayerController.Inputs>(System.Text.Encoding.Default.GetString(bytes));
				}
			}
				
		}
		else
		{
			if (client.Connected)
			{
				clientStream =  client.GetStream();

				//string str = "sending data";
				//byte[] sending = System.Text.Encoding.Default.GetBytes(str);
				//clientStream.Write(sending,0,sending.Length);

				string jsoninputs =  JsonUtility.ToJson(player.ActiveInputs);
				byte[] buffer = System.Text.Encoding.Default.GetBytes(jsoninputs);
				clientStream.Write(buffer, 0, buffer.Length);
				//writer.Write();
			}
		}
    }
}