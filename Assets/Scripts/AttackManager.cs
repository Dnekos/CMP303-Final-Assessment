using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackManager : MonoBehaviour
{
	Animator anim;
	[SerializeField] GameObject col;
	[SerializeField] Transform HitboxSpawn;
	// Start is called before the first frame update
	void Start()
    {
		anim = GetComponent<Animator>();
    }

	public void OnAttack(InputAction.CallbackContext context)
	{
		if (!context.performed)
			return;

		anim.SetTrigger("attackbtn");
	}
	public void TurnOnCol()
	{
		MeleeAttack Hitbox = Instantiate(col, HitboxSpawn.position, HitboxSpawn.rotation).GetComponent<MeleeAttack>();
		Hitbox.Lifetime = anim.GetCurrentAnimatorStateInfo(0).length;
		Debug.Log(anim.GetCurrentAnimatorStateInfo(0));
	}
}
