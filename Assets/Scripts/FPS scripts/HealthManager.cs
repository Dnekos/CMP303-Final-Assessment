using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
	public float Health = 1;
	BaseNetworker networker;
	Text healthtext;
	bool isLocalPlayer;

	[System.Serializable]
	public struct HitMarker
	{
		public int PlayerIndex;
		public float TimeStamp;
		public Vector3 BulletPosition;
		public float damage;
	}


	private void Start()
	{
		networker = FindObjectOfType<BaseNetworker>();
		healthtext = GameObject.Find("HealthText").GetComponent<Text>();
		isLocalPlayer = (networker.PlayerIndex == networker.GetIndex(GetComponent<PlayerController>()));
	}
	private void Update()
	{
		if (isLocalPlayer)
			healthtext.text = "Health: " + Health;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.tag == "Melee")
			Debug.Log("hit");
	}

	// build hitmarker and send it to the networker so that the server can attempt to register the hit
	public void GetHit(float damage,Vector3 BulletPos)
	{
		HitMarker hit;
		hit.damage = damage;
		hit.BulletPosition = BulletPos;
		hit.TimeStamp = networker.GetBufferedTime();
		hit.PlayerIndex = networker.PlayerIndex;
		networker.SendHitRegistration(hit);
	}
	public void TakeDamage(float damage)
	{ 
		Health -= damage;
		if (Health <= 0)
		{
			networker.SetUpDisconnect(isLocalPlayer ? "You Lose" : "You Win");
			Destroy(gameObject);
		}
		Debug.Log(gameObject + "got hit for " + damage + " damage");
	}
}
