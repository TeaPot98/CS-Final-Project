using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

internal class WheelData
{
    public float Rpm;
    public float AngularVelocity;
    public Wheel Wheel;
}

public class Car : MonoBehaviour
{
    private List<Wheel> _wheels;
    public Wheel wheelFL;
    public Wheel wheelFR;
    public Wheel wheelRL;
    public Wheel wheelRR;
    public EngineSO engine;
    public TransmissionSO transmission;
    public SuspensionSO suspension;

    public float maxSteeringAngle = 30f;

    public string GearLabel = "N";
    public string RpmLabel = "1000 RPM";
    public string SpeedLabel = "0 km/h";

    public string FLWheelRpm = "0";
    public string FRWheelRpm = "0";
    public string RLWheelRpm = "0";
    public string RRWheelRpm = "0";

    private Rigidbody _rb;

    private int _gear = 0;
    private float _speed = 0f;
    private float _throttle = 0f;
    private float _brake = 0f;
    private float _steering = 0f;

    public float _brakingForce = 30000f;

    public float tireGripFactor = 0.9f;
    public float tireMass = 10f;

    private float currentEngineRpm = 3000f;

    private float _rpmDiffRate = 0.01f;


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _wheels = new List<Wheel> { wheelFL, wheelFR, wheelRL, wheelRR };
    }

    private void Update()
    {
        FLWheelRpm = wheelFL.AngularVelocity.ToString();
        FRWheelRpm = wheelFR.AngularVelocity.ToString();
        RLWheelRpm = wheelRL.AngularVelocity.ToString();
        RRWheelRpm = wheelRR.AngularVelocity.ToString();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        int drivenWheelCount = _wheels.Count(wheel => wheel.isDriving && wheel.IsGrounded);
        if (drivenWheelCount == 0)
        {
            UpdateLabels();
            return;
        }

        float engineTorque = engine.GetTorque(currentEngineRpm) * _throttle;
        float wheelTorque = transmission.GetTorque(engineTorque, _gear);


        foreach (Wheel wheel in _wheels)
        {
            Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
            float wheelLinearVelocity = Vector3.Dot(wheel.transform.forward, wheelWorldVelocity);

            float rollingWheelAngularVelocity = wheelLinearVelocity / wheel.TireRadius;

            if (wheel.canSteer)
            {
                // Calculate the rotation angle based on input
                float steeringRotationAngle =
                    Utils.RemapToRange(_steering, -1f, 1f, -maxSteeringAngle, maxSteeringAngle);

                // Debug.Log("Steering Rotation Angle: " + steeringRotationAngle);

                // Set the steering rotation of the wheel (transform and mesh) around its local up axis
                wheel.transform.localRotation = Quaternion.Euler(0.0f, steeringRotationAngle, 0.0f);
            }

            if (wheel.IsGrounded)
            {
                // world-space direction of the steering force
                Vector3 steeringDir = wheel.transform.right;


                if (wheel.isDriving)
                {
                }

                // TODO: Check if it's necessary to implement loaded tire radius
                float loadedTireRadius = wheel.TireRadius;
                float slipRatio = rollingWheelAngularVelocity * loadedTireRadius / wheelLinearVelocity - 1;


                // what it's the tire's velocity in the steering direction ?
                // note that steeringDir is a unit vector, so this returns the magnitude of wheelWorldVelocity
                // as projected onto steeringDir
                float steeringVel = Vector3.Dot(steeringDir, wheelWorldVelocity);

                // the change in velocity that we're looking for is -steeringVel * gripFactor
                // gripFactor is in range 0-1, 0 means no grip, 1 means full grip
                float desiredVelChange = -steeringVel * tireGripFactor;

                // turn change in velocity into an acceleration (acceleration = change in vel / time)
                // this will produce the acceleration necessary to change the velocity by desiredVelChange in 1 physics step
                float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                Vector3 steeringForce = tireMass * desiredAccel * steeringDir;

                float slipAngle = Vector3.SignedAngle(wheel.transform.forward, wheelWorldVelocity, wheel.transform.up);
                float lateralForce = Utils.ComputeLateralTireForce(slipAngle);
                float longitudinalForce = Utils.ComputeLongitudinalTireForce(1f);

                Vector3 totalTireForce = lateralForce * Mathf.Sign(slipAngle) * wheel.transform.right +
                                         longitudinalForce * wheel.transform.forward;

                // Force = Mass * Acceleration, so multiply by the mass of the tire and apply as a force
                _rb.AddForceAtPosition(steeringForce, wheel.transform.position);

                Debug.DrawRay(wheel.transform.position, steeringForce, Color.green);
            }

            if (wheel.isDriving && wheel.IsGrounded)
            {
                // Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
                // float wheelLinearVelocity = Vector3.Dot(wheelWorldVelocity, wheel.transform.forward);
                //
                // float angularVelocity = wheelLinearVelocity / wheel.TireRadius;


                if (_gear == 0)
                {
                    // float engineTorque = engine.GetTorque(_throttle);
                    //
                    // currentEngineRpm += Utils.ComputeEngineFreeDeltaRpm(
                    //     currentEngineRpm,
                    //     engineTorque,
                    //     _throttle,
                    //     dt
                    // );
                    //
                    // currentEngineRpm = Mathf.Clamp(currentEngineRpm, engine.IdleRpm, engine.MaxRpm);
                    //
                    // ApplyBrakeForce(drivenWheelCount);
                    UpdateLabels();
                    return;
                }

                float expectedEngineRpm = Mathf.Abs(
                    Utils.ComputeExpectedRpmAtWheelAngularVelocity(
                        wheel.AngularVelocity,
                        transmission,
                        _gear
                    )
                );
                float expectedWheelAngularVelocity =
                    Utils.ComputeWheelAngularVelocityAtEngineRpm(currentEngineRpm, _gear, transmission);


                // float engineRpmDelta = Utils.ComputeEngineDeltaRpm(1f, wheelTorque, _throttle, 0.2f, 0.2f,
                //     expectedEngineRpm, currentEngineRpm);
                currentEngineRpm = Utils.ComputeEngineDeltaRpm(1f, wheelTorque, _throttle, 0.2f, 0.2f,
                    expectedEngineRpm, currentEngineRpm);
                wheel.AngularVelocity = Utils.ComputeWheelDeltaAngularVelocity(_throttle, wheelTorque, 1f,
                    0.01f, _brake, expectedWheelAngularVelocity, wheel.AngularVelocity);

                // currentEngineRpm += engineRpmDelta * _rpmDiffRate;
                // wheel.AngularVelocity -= wheelAngularVelocityDelta * _rpmDiffRate;


                // TODO: Check if it's necessary to implement loaded tire radius
                float loadedTireRadius = wheel.TireRadius;
                float slipRatio = wheel.AngularVelocity * loadedTireRadius / wheelLinearVelocity;

                // float slipRatio = (wheel.AngularVelocity * loadedTireRadius - wheelLinearVelocity) /
                //                   Mathf.Max(Mathf.Abs(wheelLinearVelocity), 0.1f);

                // currentWheelAngularVelocity = 0f;
                // float engineTorque = engine.GetTorque(currentEngineRpm) * _throttle;
                // float wheelTorque = transmission.GetTorque(engineTorque, _gear);

                // TODO: Check if the division is matching the real torque
                // float torquePerWheel = wheelTorque / drivenWheelCount;
                // float forcePerWheel = torquePerWheel / wheel.TireRadius;

                // if (_brake != 0f && _speed < 0.1f) forcePerWheel = -_brakingForce;

                // TODO: Dynamically compute tire load
                float tireLoad = _rb.mass / 4;

                Debug.Log("Slip Ratio: " + slipRatio);

                // TODO: Compute slip ratio
                float forceValue =
                    Utils.ComputePacejkaMagicFormula(slipRatio, tireLoad, wheel.tire.GetPacejkaMagicFormulaParams());
                Vector3 force = Time.fixedDeltaTime * forceValue * wheel.forceVector;

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

    // private void HandleTire

    private float GetTireRadius()
    {
        if (_wheels.Count == 0) return 0.3f;

        return _wheels[0].TireRadius;
    }

    private void UpdateLabels()
    {
        _speed = _rb.linearVelocity.magnitude;

        RpmLabel = $"{(int)currentEngineRpm} RPM";
        SpeedLabel = $"{(int)(_speed * 3.6f)} km/h";
    }
}