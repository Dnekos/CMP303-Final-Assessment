using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("Move Speeds"), SerializeField] float acceleration = 1;
	[SerializeField] float maxSpeed, minSpeed;

	[Header("Looking"), SerializeField] float Sensitivity = 100;
	[SerializeField] Transform head;

	[Header("Ground Checking")]
	public bool isGrounded;
	[SerializeField] Transform groundCheck;
	[SerializeField] float groundDistance = 0.4f;
	[SerializeField] LayerMask groundMask;

	[Header("Jumping"), SerializeField]
	float jumpForce; //lmao goku game

	Gun heldGun;

	[System.Serializable]
	public struct Inputs
	{
		public Vector2 inputDir;
		public Vector2 LookDir;
		public bool Shooting;
		public bool Jumping;
	}

	[SerializeField]
	public Inputs ActiveInputs;

	Rigidbody rb;
	float xRotation = 0f;

	// Start is called before the first frame update
	protected virtual void Start()
	{
		rb = GetComponent<Rigidbody>();
		heldGun = GetComponentInChildren<Gun>();
	}

	private void Update()
	{
		UpdateLook();
		//Debug.Log("update "+ActiveInputs.LookDir);

		isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);


		if (ActiveInputs.Jumping && isGrounded)
		{
			rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
			ActiveInputs.Jumping = false;
		}
		if (ActiveInputs.Shooting)
		{
			heldGun.ShootProjectile(true);
			ActiveInputs.Shooting = false;
		}
	}

	private void UpdateLook()
	{
		Vector2 LD = ActiveInputs.LookDir * Sensitivity * Time.deltaTime;

		xRotation -= LD.y;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);
		head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

		transform.Rotate(Vector3.up * ActiveInputs.LookDir.x);
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		Vector3 move = transform.right * ActiveInputs.inputDir.x + transform.forward * ActiveInputs.inputDir.y;

		if (ActiveInputs.inputDir != Vector2.zero && rb.velocity.magnitude < maxSpeed)
		{
			rb.AddForce(move.normalized * acceleration * rb.mass);
		}

		//else if (inputDir != Vector2.zero && rb.velocity.magnitude > maxSpeed)
		//	rb.velocity = inputDir * maxSpeed;

		if (rb.velocity.magnitude < minSpeed)
		{
			rb.velocity = Vector2.zero;
		}
	}
}
