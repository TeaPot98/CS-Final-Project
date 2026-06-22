using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car : MonoBehaviour
{
	public List<Wheel> wheels;
	public EngineSO engine;
	public TransmissionSO transmission;

	public string GearLabel = "N";
	public string RpmLabel = "1000 RPM";
	public string SpeedLabel = "0 km/h";

	private Rigidbody _rb;

	private int _gear = 0;
	private float _speed = 0f;
	private float _throttle = 0f;
	private float _brake = 0f;
	private float _steering = 0f;

	private float prevRpm = 1000f;
	private float prevWheelAngularVelocity = 0f;


	private void Start()
	{
		_rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		float engineTorque = engine.GetTorque(_throttle);
		float transmissionTorque = transmission.GetTorque(engineTorque, _gear);

		float deltaRpm = Utils.ComputeEngineDeltaRpm(1f, engineTorque, _throttle, 1f, 1f, engine.GetRpm(), prevRpm);

		float expectedWheelAngularVelocity = transmission.GetRpm(engine.GetRpm(), _gear) * 2f * (float)Math.PI / 60f;
		float deltaWheelAngularVelocity = Utils.ComputeWheelDeltaAngularVelocity(1f, engineTorque, 1f, 1f, _brake,
			expectedWheelAngularVelocity, prevWheelAngularVelocity);

		Debug.Log("D RPM: " + deltaRpm);
		Debug.Log("D AV: " + deltaWheelAngularVelocity);

		RpmLabel = (int)engine.GetRpm() + " RPM";

		float wheelForce = transmissionTorque / GetTireRadius();

		Debug.Log(wheelForce);

		wheels.ForEach(wheel =>
		{
			if (!wheel.IsDriving || !wheel.IsGrounded) return;

			// Debug.Log("Applying force");

			_rb.AddForceAtPosition(wheel.transform.forward * wheelForce, wheel.transform.position, ForceMode.Force);

			Debug.DrawRay(wheel.transform.position, wheel.transform.forward * wheelForce / 1000f, Color.green);
		});

		_speed = _rb.linearVelocity.magnitude;
		SpeedLabel = (int)(_speed * 3600f / 1000f) + " km/h";

		// Debug.Log("Raw Torque: " + engineTorque);
		// Debug.Log("Transmission Torque: " + transmissionTorque);
		// Debug.Log("Wheel Force: " + wheelForce);
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

	private void OnGearShift(InputValue v)
	{
		int value = (int)v.Get<float>();

		if (_gear == -1 && value == -1) return;
		if (_gear == transmission.gears.Count && value == 1) return;

		_gear += value;

		if (_gear == -1)
			GearLabel = "R";
		else if (_gear == 0)
			GearLabel = "N";
		else
			GearLabel = _gear.ToString();


		Debug.Log("Gear Shift: " + _gear);
	}

	private float GetTireRadius()
	{
		if (wheels.Count == 0) return 0.3f;

		return wheels[0].TireRadius;
	}
}