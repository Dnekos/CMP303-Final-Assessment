using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
	public bool isPlayer = false;
	public float Health = 1;

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.tag == "Melee")
			Debug.Log("hit");
	}
	
	public void GetHit(float damage)
	{
		Health -= damage;
		if (Health <= 0)
		{
			if (!isPlayer)
			{
				Destroy(gameObject);
			}
		}
		Debug.Log(gameObject + "got hit for " + damage + " damage");
	}
}
