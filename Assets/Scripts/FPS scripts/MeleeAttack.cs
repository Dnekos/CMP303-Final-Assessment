using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{

	public bool TargetEnemy = true;
	public float Lifetime = 1;
	public float Damage = 1;



	IEnumerator LifeTimer()
	{
		Debug.Log(Lifetime);
		yield return new WaitForSeconds(Lifetime);
		Destroy(gameObject);
	}
	
	// Start is called before the first frame update
	void Start()
    {
		StartCoroutine(LifeTimer());
	}

	private void OnTriggerEnter(Collider other)
	{
		HealthManager health = other.GetComponent<HealthManager>();
		if (other.isTrigger || !health)
			return;

		if ((TargetEnemy && !health.isPlayer) || (!TargetEnemy && health.isPlayer))
			other.GetComponent<HealthManager>().GetHit(Damage);


	}
}
