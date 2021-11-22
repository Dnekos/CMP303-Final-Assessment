using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{

	public float Damage;
	[SerializeField] float Lifetime = 2;
	float countdown;

	private void Update()
	{
		countdown += Time.deltaTime;
		if (countdown >= Lifetime)
			Destroy(gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.collider.isTrigger)
			return;
		Debug.Log("hit " + collision.gameObject);
		Destroy(gameObject);
	}
}
