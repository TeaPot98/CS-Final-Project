using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Powertrain : MonoBehaviour
{
	public EngineSO engine;
	public TransmissionSO transmission;
	public List<Wheel> wheels;

	private float _throttle = 0f;
	private float _brake = 0f;
	private float _steering = 0f;
	private Rigidbody _rb;

	[SerializeField] private int _gear = 1;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	private void Start()
	{
	}

	private void FixedUpdate()
	{
		float rawTorque = Utils.ComputeTorque(Utils.RemapToRange(_throttle, 0f, 1f, 1000f, engine.maxRpm),
			engine.GetTorqueCurveParams());

		float transmissionTorque = Utils.ComputeTransmissionTorque(rawTorque, _gear, transmission);

		// TODO: Implement wheel radius
		float wheelForce = transmissionTorque / 0.3f;

		wheels.ForEach(wheel =>
		{
			if (!wheel.IsDriving || !wheel.IsGrounded) return;

			Debug.Log("Applying force");

			_rb.AddForceAtPosition(wheel.transform.forward * wheelForce, wheel.transform.position, ForceMode.Force);
		});

		Debug.Log("Raw Torque: " + rawTorque);
		Debug.Log("Transmission Torque: " + transmissionTorque);
		Debug.Log("Wheel Force: " + wheelForce);
	}

	private void OnSteering(InputValue value)
	{
		_steering = value.Get<float>();
	}

	private void OnThrottle(InputValue value)
	{
		_throttle = value.Get<float>();
	}

	private void OnBrake(InputValue value)
	{
		_brake = value.Get<float>();
	}

	private void OnGearShift(InputValue value)
	{
		_gear += (int)value.Get<float>();

		Debug.Log("Gear Shift: " + _gear);
	}
}