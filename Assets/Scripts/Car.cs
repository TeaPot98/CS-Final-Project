using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car : MonoBehaviour
{
	public List<Wheel> wheels;
	public EngineSO engine;
	public TransmissionSO transmission;
	public SuspensionSO suspension;

	public float maxSteeringAngle = 30f;

	public string GearLabel = "N";
	public string RpmLabel = "1000 RPM";
	public string SpeedLabel = "0 km/h";

	private Rigidbody _rb;

	private int _gear = 0;
	private float _speed = 0f;
	private float _throttle = 0f;
	private float _brake = 0f;
	private float _steering = 0f;

	public float _brakingForce = 30000f;

	public float tireGripFactor = 0.9f;
	public float tireMass = 10f;

	private float currentRpm = 30000f;
	private float currentWheelAngularVelocity = 0f;


	private void Start()
	{
		_rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		float dt = Time.fixedDeltaTime;

		int drivenWheelCount = wheels.Count(wheel => wheel.IsDriving && wheel.IsGrounded);
		if (drivenWheelCount == 0)
		{
			UpdateLabels();
			return;
		}

		float tireRadius = GetTireRadius();
		float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);

		currentWheelAngularVelocity = forwardSpeed / tireRadius;

		if (_gear == 0)
		{
			// float engineTorque = engine.GetTorque(_throttle);
			//
			// currentRpm += Utils.ComputeEngineFreeDeltaRpm(
			//     currentRpm,
			//     engineTorque,
			//     _throttle,
			//     dt
			// );
			//
			// currentRpm = Mathf.Clamp(currentRpm, engine.IdleRpm, engine.MaxRpm);
			//
			// ApplyBrakeForce(drivenWheelCount);
			// UpdateLabels();
			// return;
		}

		float expectedEngineRpm = Mathf.Abs(
			Utils.ComputeExpectedRpmAtWheelAngularVelocity(
				currentWheelAngularVelocity,
				transmission,
				_gear
			)
		);

		currentRpm = Mathf.Clamp(expectedEngineRpm, engine.idleRpm, engine.maxRpm);

		float engineTorque = engine.GetTorque(currentRpm) * _throttle;
		float wheelTorque = transmission.GetTorque(engineTorque, _gear);

		float torquePerWheel = wheelTorque / drivenWheelCount;
		float forcePerWheel = torquePerWheel / tireRadius;

		if (_brake != 0f && _speed < 0.1f) forcePerWheel = -_brakingForce;

		foreach (Wheel wheel in wheels)
		{
			if (wheel.CanSteer)
			{
				// Calculate the rotation angle based on input
				float steeringRotationAngle = Utils.RemapToRange(_steering, -1f, 1f, -maxSteeringAngle, maxSteeringAngle);

				Debug.Log("Steering Rotation Angle: " + steeringRotationAngle);

				// Set the steering rotation of the wheel (transform and mesh) around its local up axis
				wheel.transform.localRotation = Quaternion.Euler(0.0f, steeringRotationAngle, 0.0f);
			}

			if (wheel.IsGrounded)
			{
				// world-space direction of the steering force
				Vector3 steeringDir = wheel.transform.right;

				Vector3 tireWorldVel = _rb.GetPointVelocity(wheel.transform.position);

				// what it's the tire's velocity in the steering direction ?
				// note that steeringDir is a unit vector, so this returns the magnitude of tireWorldVel
				// as projected onto steeringDir
				float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);

				// the change in velocity that we're looking for is -steeringVel * gripFactor
				// gripFactor is in range 0-1, 0 means no grip, 1 means full grip
				float desiredVelChange = -steeringVel * tireGripFactor;

				// turn change in velocity into an acceleration (acceleration = change in vel / time)
				// this will produce the acceleration necessary to change the velocity by desiredVelChange in 1 physics step
				float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

				Vector3 steeringForce = tireMass * desiredAccel * steeringDir;

				// Force = Mass * Acceleration, so multiply by the mass of the tire and apply as a force
				_rb.AddForceAtPosition(steeringForce, wheel.transform.position);

				Debug.DrawRay(wheel.transform.position, steeringForce, Color.green);
			}

			if (wheel.IsDriving && wheel.IsGrounded)
			{
				Vector3 force = wheel.forceVector * forcePerWheel;

				_rb.AddForceAtPosition(
					force,
					wheel.contactPoint,
					ForceMode.Force
				);

				Debug.DrawRay(
					wheel.contactPoint,
					force / 1000f,
					Color.green
				);
			}
		}

		UpdateLabels();
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

	private void UpdateLabels()
	{
		_speed = _rb.linearVelocity.magnitude;

		RpmLabel = $"{(int)currentRpm} RPM";
		SpeedLabel = $"{(int)(_speed * 3.6f)} km/h";
	}
}