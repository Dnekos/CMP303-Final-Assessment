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


	[System.Serializable]
	public struct Inputs
	{
		public int PlayerIndex;
		public Vector2 inputDir;
		public Vector2 LookDir;
		public bool Shooting;
		public bool Jumping;
	}
	[System.Serializable]
	public struct PositionalPackage
	{
		public int PlayerIndex;
		public float TimeStamp;
		public Vector3 Position;
		public Quaternion Rotation;
		public Quaternion HeadRotation;
		public bool FiredGun;
	}

	[Header("Inputs"), SerializeField]
	public Inputs ActiveInputs;

	[Header("Networking Stuff"), Tooltip("max length of list to track player positions for purposes of serverside hit registration"), SerializeField]
	int MaxTrackedPositions = 50;
	public List<KeyValuePair<float, Vector3>> RecordedPositions; // key is timestamp, value is position
	List<PositionalPackage> packages, predictions;

	// smaller local variables
	float xRotation = 0f;
	bool FiredSinceLastPack = false;
	
	// components
	Rigidbody rb;
	Gun heldGun;
	[HideInInspector] public HealthManager health; // public so that basenetworker can access it though PC
	BaseNetworker networker;

	// Start is called before the first frame update
	protected virtual void Start()
	{
		health = GetComponent<HealthManager>();
		rb = GetComponent<Rigidbody>();
		heldGun = GetComponentInChildren<Gun>();
		networker = FindObjectOfType<BaseNetworker>();

		packages = new List<PositionalPackage>();
		predictions = new List<PositionalPackage>();
		RecordedPositions = new List<KeyValuePair<float, Vector3>>();
	}

	private void Update()
	{
		// shooting always runs
		if (ActiveInputs.Shooting)
		{
			FiredSinceLastPack = heldGun.ShootProjectile();
			ActiveInputs.Shooting = false;
		}

		// if there are transform packages, that means its an online player and we dont want to do the other update stuff
		if (packages.Count > 0)
		{
			RunInterpolation();
			return;
		}


		UpdateLook();

		isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);

		if (ActiveInputs.Jumping && isGrounded)
		{
			rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
			ActiveInputs.Jumping = false;
		}
	}

	void RunInterpolation()
	{
		if (packages.Count < 2) // if there aren't enough to predict, just use what we have
		{
			ApplyPrediction(packages[packages.Count - 1]);
			return;
		}
		
		PositionalPackage prediction = packages[packages.Count - 1], // using prediction as most recent to set default values for stuff like index
			secondRecent = packages[packages.Count - 2];

		float currTime = networker.GetBufferedTime();
		float TimeDifference = (currTime - prediction.TimeStamp) / networker.GetSendRate(); //(networker.GetBufferedTime() - secondRecent.TimeStamp) / (prediction.TimeStamp - secondRecent.TimeStamp);//(networker.GetBufferedTime() - secondRecent.TimeStamp);
		prediction.TimeStamp = currTime;

		prediction.Position =  Vector3.LerpUnclamped(secondRecent.Position, prediction.Position, TimeDifference);
		prediction.Rotation = Quaternion.LerpUnclamped(secondRecent.Rotation, prediction.Rotation, TimeDifference); ;
		prediction.HeadRotation = Quaternion.LerpUnclamped(secondRecent.HeadRotation, prediction.HeadRotation, TimeDifference); ;

		predictions.Add(prediction);
		if (packages.Count > 4) // if we have more than we need
			packages.RemoveAt(0); // pop the oldest one


		if (predictions.Count < 3) // if there arent enough to interpolate the predictions
		{
			ApplyPrediction(prediction); // just apply the prediction
			return;
		}

		PositionalPackage lastPrediction = predictions[predictions.Count - 2], secondLastPrediction = predictions[predictions.Count - 3];

		
		// double prediction
		PositionalPackage doublePrediction = lastPrediction;
		TimeDifference = (currTime - lastPrediction.TimeStamp) / networker.GetSendRate();//(networker.GetBufferedTime() - secondLastPrediction.TimeStamp) / (lastPrediction.TimeStamp - secondLastPrediction.TimeStamp); //networker.GetBufferedTime() - secondLastPrediction.TimeStamp;
		doublePrediction.Position = Vector3.LerpUnclamped(secondLastPrediction.Position, lastPrediction.Position, TimeDifference);
		doublePrediction.Rotation = Quaternion.LerpUnclamped(secondLastPrediction.Rotation, lastPrediction.Rotation, TimeDifference); ;
		doublePrediction.HeadRotation = Quaternion.LerpUnclamped(secondLastPrediction.HeadRotation, lastPrediction.HeadRotation, TimeDifference);

		// average them out
		transform.position = Vector3.Lerp(doublePrediction.Position,prediction.Position,0.5f);
		transform.rotation = Quaternion.Slerp(doublePrediction.Rotation, prediction.Rotation, 0.5f);
		head.rotation = Quaternion.Slerp(doublePrediction.HeadRotation, prediction.HeadRotation, 0.5f);

		RecordedPositions.Add(new KeyValuePair<float, Vector3>(currTime, transform.position));
		if (packages.Count > MaxTrackedPositions) // if we have more than we need
			packages.RemoveAt(0); // pop the oldest one

	}

	private void UpdateLook()
	{
		//Debug.LogError("deltatime = " + Time.deltaTime);
		 
		xRotation -= ActiveInputs.LookDir.y * Sensitivity;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);
		head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

		transform.Rotate(Vector3.up * ActiveInputs.LookDir.x * Sensitivity);
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

	public PositionalPackage PackUp()
	{
		PositionalPackage pack = new PositionalPackage();
		pack.PlayerIndex = ActiveInputs.PlayerIndex;
		pack.TimeStamp = networker.GetBufferedTime();

		pack.Position = transform.position;
		pack.Rotation = transform.rotation;
		pack.HeadRotation = head.rotation;
		pack.FiredGun = FiredSinceLastPack;
		FiredSinceLastPack = false;
		return pack;
	}
	public void Unpack(PositionalPackage pack)
	{
		ActiveInputs.Shooting = pack.FiredGun; // shooting we want to happen immediately.

		packages.Add(pack);
		if (packages.Count > 4) // if we have more than we need
			packages.RemoveAt(0); // pop the oldest one
	}

	void ApplyPrediction(PositionalPackage pack)
	{
		transform.position = pack.Position;
		transform.rotation = pack.Rotation;
		head.localRotation = pack.HeadRotation;
	}

}
