using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class BaseNetworker : MonoBehaviour
{
	public int PlayerIndex;
	public int TotalPlayers;

	public string ServerIP = "127.0.0.1";
	[SerializeField] protected int ServerPort = 4444;

	[SerializeField] protected PlayerController[] AllPlayers;

	[SerializeField] GameObject PlayerPrefab, OnlinePrefab;


	protected void ParseRead(byte[] bytes)
	{
		Debug.Log("reading " + System.Text.Encoding.Default.GetString(bytes));
		foreach (string packet in System.Text.Encoding.Default.GetString(bytes).Split('\n'))
		{
			string[] splitPacket = packet.Split(new char[] { ':' }, 2);
			switch (splitPacket[0])
			{
				case "PlayerController+Inputs":
					PlayerController.Inputs ReadInputs = JsonUtility.FromJson<PlayerController.Inputs>(splitPacket[1]); // deserialize the struct
					AllPlayers[ReadInputs.PlayerIndex].ActiveInputs = ReadInputs; // set inputs to the proper player
					break;
				case "SceneChange":
					string[] commaindexed = splitPacket[1].Split(',');
					TotalPlayers = int.Parse(commaindexed[1]);
					SceneManager.LoadScene(int.Parse(commaindexed[0]));
					break;
				case "Index":
					PlayerIndex = int.Parse(splitPacket[1]);
					break;

			}
		}

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

	void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		Debug.Log(scene.name);
		if (scene.buildIndex == 1)
		{
			Debug.Log("Lego impression 2: HEY");

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

}
