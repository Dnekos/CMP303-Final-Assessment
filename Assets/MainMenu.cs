using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	[SerializeField] InputField ipAddress;
	[SerializeField] Button StartButton, HostBtn, JoinBtn;
	[SerializeField] Text numplayers;
	ServerManager host;

	[SerializeField] GameObject ServerPrefab,ClientPrefab;

	private void Update()
	{
		if (host?.clients != null )
			numplayers.text = "Players Joined: " + host.clients.Count;
	}
	public void Host()
	{
		HostBtn.interactable = false;
		JoinBtn.interactable = false;

		host = Instantiate(ServerPrefab).GetComponent<ServerManager>();
		host.ServerIP = ipAddress.text;
		DontDestroyOnLoad(host.gameObject);

		Debug.Log(host.ServerIP);
		StartButton.interactable = true;
	}
	public void Join()
	{
		HostBtn.interactable = false;
		JoinBtn.interactable = false;

		GameObject Networker = Instantiate(ClientPrefab);
		Networker.GetComponent<ClientManager>().ServerIP = ipAddress.text;
		DontDestroyOnLoad(Networker);

		Debug.LogError("made client");

	}

	public void BeginGame()
	{
		Debug.Log(host);
		host.AllGoToScene(1);
	}
}
