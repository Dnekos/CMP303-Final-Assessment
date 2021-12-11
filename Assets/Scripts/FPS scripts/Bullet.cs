using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
	public float Damage;
	[SerializeField] float Lifetime = 2;
	float countdown;
	[SerializeField] GameObject particle;

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
		HealthManager target = collision.gameObject.GetComponent<HealthManager>();
		if (target != null)
		{
			if (target.Health > 0)
			{
				target.GetHit(Damage, transform.position);
				Instantiate(particle, transform.position, transform.rotation);
			}
		}
		Debug.Log("hit " + collision.gameObject);
		Destroy(gameObject);
	}
}
