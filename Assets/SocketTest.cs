using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System.Net.Sockets;


// some code taken from https://answers.unity.com/questions/15422/unity-project-and-3rd-party-apps.html?childToView=15477#answer-15477
public class SocketTest : MonoBehaviour
{
	internal bool socketReady = false;
	Socket mySocket;
	TcpListener listener;
	UdpClient udpc;
	NetworkStream theStream;
	StreamWriter theWriter;
	StreamReader theReader;
	[SerializeField] string Host = "127.0.0.1";
	[SerializeField] int Port = 4444;
	void Start()
	{
		setupSocket();
	}
	void Update()
	{
	}
	// **********************************************
	public void setupSocket()
	{
		try
		{
			IPAddress address = IPAddress.Parse(Host);
			//listener = new TcpListener(address,Port);
			//mySocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

			udpc = new UdpClient(Host, Port);
			Debug.Log("set up server on port " + Port);



			Debug.Log("Socket Set up");
		}
		catch (Exception e)
		{
			Debug.Log("Socket error: "+e);
		}
	}
	public void writeSocket(string theLine)
	{
		if (!socketReady)
			return;
		string foo = theLine + "\r\n";
		theWriter.Write(foo);
		theWriter.Flush();
	}
	public string readSocket()
	{
		if (!socketReady)
			return "";
		if (theStream.DataAvailable)
			return theReader.ReadLine();
		return "";
	}
	public void closeSocket()
	{
		if (!socketReady)
			return;
		theWriter.Close();
		theReader.Close();
		mySocket.Close();
		socketReady = false;
	}
}
