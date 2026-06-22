using System;
using UnityEngine;

public class Wheel : MonoBehaviour
{
	public bool IsDriving = false;

	public GameObject wheelModel;
	public GameObject tireMesh;
	public GameObject carObject;

	public bool IsGrounded = false;

	private float _suspensionRestDist = 1f;
	private float _suspensionTravel = 0.5f;
	private float _springStrength = 7000f;

	private float _springDamper = 250f;
	// private float _tireGripFactor = 1f;
	// private float _tireMass = 10f;
	// private float _carTopSpeed = 1000f;
	// private float _acceleration = 100f;
	// private float _maxRotationAngle = 30.0f;
	// public bool torque;

	// public bool steering;

	// private AnimationCurve _powerCurve;
	// private AnimationCurve _maxSteeringAngleCurve;
	private Rigidbody _carRigidBody;

	private Transform _carTransform;

	public float TireRadius;
	private Transform _tireTransform;
	private Vector3 _tireTransformPosition;
	private Renderer _tireMeshRenderer;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	private void Start()
	{
		// _carManager = GetComponentInParent<CarManager>();
		_carRigidBody = carObject.GetComponent<Rigidbody>();
		_carTransform = carObject.GetComponent<Transform>();

		_tireMeshRenderer = tireMesh.GetComponent<Renderer>();

		_tireTransform = GetComponent<Transform>();
		_tireTransformPosition = _tireTransform.position;

		TireRadius = 0.5f * _tireMeshRenderer.bounds.size.y;
		// _tireMass = _carManager.tireMass;
		// _tireGripFactor = _carManager.tireGripFactor;
		// _maxRotationAngle = _carManager.maxRotationAngle;
		//   
		// _suspensionRestDist = _carManager.suspensionRestDist;
		// _suspensionTravel = _carManager.suspensionTravel;
		// _springStrength = _carManager.springStrength;
		// _springDamper = _carManager.springDamper;
		//   
		// _carTopSpeed = _carManager.carTopSpeed;
		// _acceleration = _carManager.acceleration;
		// _powerCurve = _carManager.powerCurve;
		// _maxSteeringAngleCurve = _carManager.maxSteeringAngleCurve;
	}

	// Update is called once per frame
	private void Update()
	{
	}

	private void FixedUpdate()
	{
		// ----- Get car's parameters from CarManager -----
		// _tireMass = _carManager.tireMass;
		// _tireGripFactor = _carManager.tireGripFactor;
		// _maxRotationAngle = _carManager.maxRotationAngle;
		//   
		// _suspensionRestDist = _carManager.suspensionRestDist;
		// _springStrength = _carManager.springStrength;
		// _springDamper = _carManager.springDamper;

		// ------------------------------------

		// forward speed of the car (in the direction of driving)
		// float carSpeed = Vector3.Dot(_carTransform.forward, _carRigidBody.linearVelocity);

		// normalized car speed
		// float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / _carTopSpeed);

		Vector3 tireWorldVel = _carRigidBody.GetPointVelocity(_tireTransformPosition);
		_tireTransformPosition = _tireTransform.position;

		Ray ray = new(_tireTransformPosition, -_tireTransform.up);
		RaycastHit tireRay;
		bool rayDidHit = Physics.Raycast(ray, out tireRay, _suspensionRestDist + _suspensionTravel);

		// Debug.Log("Ray did hit: " + rayDidHit);

		IsGrounded = rayDidHit;

		// ------ Spring force-------
		if (rayDidHit)
		{
			// world-space direction of the spring force
			Vector3 springDir = _tireTransform.up;

			// calculate offset from the raycast
			float offset = _suspensionRestDist - tireRay.distance;

			// calculate velocity along the spring direction
			// note that springDir is a unit vector, so this returns the magnitude of tireWorldVel
			// as projected onto springDir
			float vel = Vector3.Dot(springDir, tireWorldVel);

			// calculate the magnitude of the dampened spring force
			float force = offset * _springStrength - vel * _springDamper;

			// apply the force at the location of this tire
			// in the direction of the suspension
			_carRigidBody.AddForceAtPosition(springDir * force, _tireTransformPosition);
			Debug.DrawRay(_tireTransformPosition, springDir * force, Color.red);
		}

		if (rayDidHit)
			wheelModel.transform.localPosition =
				new Vector3(0f, -tireRay.distance + TireRadius, 0f);
		else
			// Reset the wheel mesh position to the suspension rest distance
			wheelModel.transform.localPosition =
				new Vector3(0f, -_suspensionRestDist + TireRadius, 0f);
	}
}