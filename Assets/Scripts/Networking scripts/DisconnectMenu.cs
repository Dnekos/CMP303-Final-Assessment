using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DisconnectMenu : MonoBehaviour
{
	public void QuitGame()
	{
		Application.Quit();
	}

	public void Return(int index)
	{
		SceneManager.LoadScene(index);
	}
}
