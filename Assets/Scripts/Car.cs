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
    public float Throttle = 0f;
    public float Brake = 0f;
    public float Steering = 0f;

    public float _brakingForce = 30000f;

    public float tireGripFactor = 0.9f;
    public float tireMass = 10f;

    private float _currentEngineRpm = 1000f;

    public float engineInertia = 0.35f;
    public float drivenWheelInertia = 1.2f;
    public float engineFriction = 0.15f;
    public float wheelRollingResistance = 0.08f;
    public float wheelBrakeTorque = 2500f;
    public float minSlipSpeed = 0.5f;


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _wheels = new List<Wheel> { wheelFL, wheelFR, wheelRL, wheelRR };
    }

    private void Update()
    {
        FLWheelRpm = GetWheelRpmLabel(wheelFL);
        FRWheelRpm = GetWheelRpmLabel(wheelFR);
        RLWheelRpm = GetWheelRpmLabel(wheelRL);
        RRWheelRpm = GetWheelRpmLabel(wheelRR);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        List<Wheel> drivenWheels = _wheels.Where(wheel => wheel.isDriving && wheel.IsGrounded).ToList();
        bool hasGroundedDrivenWheels = drivenWheels.Count > 0;

        float maxEngineTorque = engine.GetTorque(_currentEngineRpm);
        float engineTorque = maxEngineTorque * Throttle;

        if (_gear == 0 || !hasGroundedDrivenWheels)
            UpdateFreeEngine(dt);
        else
            ApplyDrivetrainInertia(drivenWheels, engineTorque, dt);


        foreach (Wheel wheel in _wheels)
        {
            Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
            float wheelLinearVelocity = Vector3.Dot(wheel.transform.forward, wheelWorldVelocity);

            float rollingWheelAngularVelocity = GetRollingWheelAngularVelocity(wheel, wheelLinearVelocity);

            if (wheel.canSteer)
            {
                // Calculate the rotation angle based on input
                float steeringRotationAngle =
                    Utils.RemapToRange(Steering, -1f, 1f, -maxSteeringAngle, maxSteeringAngle);

                // Debug.Log("Steering Rotation Angle: " + steeringRotationAngle);

                // Set the steering rotation of the wheel (transform and mesh) around its local up axis
                wheel.transform.localRotation = Quaternion.Euler(0.0f, steeringRotationAngle, 0.0f);
            }

            if (wheel.IsGrounded)
            {
                // world-space direction of the steering force
                Vector3 steeringDir = wheel.transform.right;


                // TODO: Check if it's necessary to implement loaded tire radius
                float loadedTireRadius = wheel.TireRadius;
                float slipRatio = (rollingWheelAngularVelocity * loadedTireRadius - wheelLinearVelocity) /
                                  Mathf.Max(Mathf.Abs(wheelLinearVelocity), minSlipSpeed);


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

            if ((!wheel.isDriving || !hasGroundedDrivenWheels || _gear == 0) && wheel.IsGrounded)
                wheel.AngularVelocity = Mathf.MoveTowards(
                    wheel.AngularVelocity,
                    rollingWheelAngularVelocity,
                    wheelRollingResistance * dt
                );

            if (wheel.isDriving && wheel.IsGrounded)
            {
                // Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
                // float wheelLinearVelocity = Vector3.Dot(wheelWorldVelocity, wheel.transform.forward);
                //
                // float angularVelocity = wheelLinearVelocity / wheel.TireRadius;


                // float brakeAngularVelocityDelta = wheelBrakeTorque / Mathf.Max(drivenWheelInertia, 0.001f) * Brake * dt;
                // wheel.AngularVelocity = Mathf.MoveTowards(wheel.AngularVelocity, 0f, brakeAngularVelocityDelta);


                // TODO: Check if it's necessary to implement loaded tire radius
                float loadedTireRadius = wheel.TireRadius;
                float wheelSurfaceSpeed = wheel.AngularVelocity * loadedTireRadius;
                float slipRatio = (wheelSurfaceSpeed - wheelLinearVelocity) /
                                  Mathf.Max(Mathf.Abs(wheelLinearVelocity), minSlipSpeed);
                slipRatio = Mathf.Clamp(slipRatio, -1f, 1f);

                // float slipRatio = (wheel.AngularVelocity * loadedTireRadius - wheelLinearVelocity) /
                //                   Mathf.Max(Mathf.Abs(wheelLinearVelocity), 0.1f);

                // currentWheelAngularVelocity = 0f;
                // float engineTorque = engine.GetTorque(_currentEngineRpm) * Throttle;
                // float wheelTorque = transmission.GetTorque(engineTorque, _gear);

                // TODO: Check if the division is matching the real torque
                // float torquePerWheel = wheelTorque / drivenWheelCount;
                // float forcePerWheel = torquePerWheel / wheel.TireRadius;

                // if (Brake != 0f && _speed < 0.1f) forcePerWheel = -_brakingForce;

                // TODO: Dynamically compute tire load
                float tireLoad = _rb.mass / 4;

                Debug.Log("Slip Ratio: " + slipRatio + "; Angular Velocity: " + wheel.AngularVelocity);

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

    private void ApplyDrivetrainInertia(IReadOnlyList<Wheel> drivenWheels, float engineTorque, float dt)
    {
        float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);
        if (Mathf.Approximately(gearRatio, 0f))
        {
            UpdateFreeEngine(dt);
            return;
        }

        float maxWheelAngularVelocity = GetMaxWheelAngularVelocity(gearRatio);
        float engineAngularVelocity = Utils.FromRpmToAngularVelocity(_currentEngineRpm);
        float engineFrictionTorque = engineFriction * engineAngularVelocity;
        float drivelineTorque = (engineTorque - engineFrictionTorque) * gearRatio * transmission.transmissionEfficiency;
        float torquePerWheel = drivelineTorque / drivenWheels.Count;
        float reflectedEngineInertia = engineInertia * gearRatio * gearRatio / drivenWheels.Count;
        float wheelInertia = Mathf.Max(drivenWheelInertia + reflectedEngineInertia, 0.001f);

        foreach (Wheel wheel in drivenWheels)
        {
            float brakeTorque = wheelBrakeTorque * Brake * Mathf.Sign(wheel.AngularVelocity);
            float rollingResistanceTorque = wheelRollingResistance * wheel.AngularVelocity;
            float wheelAngularAcceleration =
                (torquePerWheel - brakeTorque - rollingResistanceTorque) / wheelInertia;

            wheel.AngularVelocity += wheelAngularAcceleration * dt;
            wheel.AngularVelocity = ClampWheelAngularVelocity(wheel.AngularVelocity, maxWheelAngularVelocity);
        }

        SyncEngineRpmToDrivenWheels(drivenWheels, gearRatio);
    }

    private void SyncEngineRpmToDrivenWheels(IReadOnlyList<Wheel> drivenWheels, float gearRatio)
    {
        float averageWheelAngularVelocity = 0f;
        foreach (Wheel wheel in drivenWheels)
            averageWheelAngularVelocity += wheel.AngularVelocity;
        averageWheelAngularVelocity /= drivenWheels.Count;

        float engineRpmFromWheels = Mathf.Abs(Utils.FromAngularVelocityToRpm(averageWheelAngularVelocity * gearRatio));
        _currentEngineRpm = ClampEngineRpm(engineRpmFromWheels);
    }

    private float GetMaxWheelAngularVelocity(float gearRatio)
    {
        float absoluteGearRatio = Mathf.Abs(gearRatio);
        if (Mathf.Approximately(absoluteGearRatio, 0f)) return 0f;

        return Utils.FromRpmToAngularVelocity(engine.maxRpm / absoluteGearRatio);
    }

    private float ClampWheelAngularVelocity(float angularVelocity, float maxAngularVelocity)
    {
        if (float.IsNaN(angularVelocity) || float.IsInfinity(angularVelocity)) return 0f;

        return Mathf.Clamp(angularVelocity, -maxAngularVelocity, maxAngularVelocity);
    }

    private float GetRollingWheelAngularVelocity(Wheel wheel, float wheelLinearVelocity)
    {
        if (wheel.TireRadius <= 0.001f) return 0f;

        return wheelLinearVelocity / wheel.TireRadius;
    }

    private string GetWheelRpmLabel(Wheel wheel)
    {
        float rpm = Utils.FromAngularVelocityToRpm(wheel.AngularVelocity);
        if (float.IsNaN(rpm) || float.IsInfinity(rpm)) return "0";

        return ((int)rpm).ToString();
    }

    private float ClampEngineRpm(float rpm)
    {
        if (float.IsNaN(rpm) || float.IsInfinity(rpm)) return engine.idleRpm;

        return Mathf.Clamp(rpm, engine.idleRpm, engine.maxRpm);
    }

    private void UpdateFreeEngine(float dt)
    {
        float engineTorque = engine.GetTorque(_currentEngineRpm) * Throttle;
        float engineAngularVelocity = Utils.FromRpmToAngularVelocity(_currentEngineRpm);
        float engineAngularAcceleration = engineTorque / Mathf.Max(engineInertia, 0.001f) -
                                          engineFriction * engineAngularVelocity;

        _currentEngineRpm = ClampEngineRpm(
            Utils.FromAngularVelocityToRpm(engineAngularVelocity + engineAngularAcceleration * dt)
        );
    }


    private void OnSteering(InputValue value)
    {
        Steering = value.Get<float>();
    }

    private void OnThrottle(InputValue value)
    {
        Throttle = value.Get<float>();
    }

    private void OnBrake(InputValue value)
    {
        Brake = value.Get<float>();
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

        RpmLabel = $"{(int)ClampEngineRpm(_currentEngineRpm)} RPM";
        SpeedLabel = $"{(int)(_speed * 3.6f)} km/h";
    }
}