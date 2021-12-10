	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.InputSystem;

public class LocalPlayer : PlayerController
{
	// Start is called before the first frame update
	protected override void Start()
	{
		base.Start();
		Cursor.lockState = CursorLockMode.Locked;
	}

	public void OnLook(InputAction.CallbackContext context)
	{
		ActiveInputs.LookDir = context.ReadValue<Vector2>() * Time.deltaTime;
	}
	public void OnMove(InputAction.CallbackContext context)
	{
		ActiveInputs.inputDir = context.ReadValue<Vector2>();
	}

	public void OnShoot(InputAction.CallbackContext context)
	{
		if (context.performed)
			ActiveInputs.Shooting = true;
	}

	public void OnJump(InputAction.CallbackContext context)
	{
		Debug.Log("ground " + isGrounded);
		if (context.performed)
			ActiveInputs.Jumping = true;
	}
}