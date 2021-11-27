using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	[SerializeField] InputField ipAddress;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void Host()
	{
		GameObject Networker = new GameObject("Networker");
		Networker.AddComponent<ServerManager>().Host = ipAddress.text;
		DontDestroyOnLoad(Networker);
	}
	public void Join()
	{
		GameObject Networker = new GameObject("Networker");
		Networker.AddComponent<ClientManager>().Host = ipAddress.text;
		DontDestroyOnLoad(Networker);
	}
}
