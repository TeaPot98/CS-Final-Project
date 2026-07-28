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
    public string GearRatioLabel = "Gear Ratio: 0";
    public string RpmLabel = "1000 RPM";
    public string SpeedLabel = "0 km/h";

    public string FLWheelRpm = "0";
    public string FRWheelRpm = "0";
    public string RLWheelRpm = "0";
    public string RRWheelRpm = "0";

    public string FLSlipAngle = "0";
    public string FRSlipAngle = "0";
    public string RLSlipAngle = "0";
    public string RRSlipAngle = "0";

    public string FLSlipRatio = "0";
    public string FRSlipRatio = "0";
    public string RLSlipRatio = "0";
    public string RRSlipRatio = "0";

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

    private float lowSpeedBlendStart = 4.0f; // m/s
    private float lowSpeedBlendEnd = 0.5f; // m/s

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
        float engineTorque = maxEngineTorque * Mathf.Max(Throttle, 0.03f);

        if (_gear == 0 || !hasGroundedDrivenWheels)
            UpdateFreeEngine(dt);
        else
            ApplyDrivetrainInertia(drivenWheels, engineTorque, dt);


        foreach (Wheel wheel in _wheels)
        {
            Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
            float wheelLinearVelocity = Vector3.Dot(wheel.transform.forward, wheelWorldVelocity);

            float rollingWheelAngularVelocity = GetRollingWheelAngularVelocity(wheel, wheelLinearVelocity);


            float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);
            float maxWheelAngularVelocity = GetMaxWheelAngularVelocity(gearRatio);
            float brakeTorque = wheelBrakeTorque * Brake * Mathf.Sign(wheel.AngularVelocity);
            float rollingResistanceTorque = wheelRollingResistance * wheel.AngularVelocity;
            float wheelAngularAcceleration = -brakeTorque - rollingResistanceTorque;

            if (wheel.IsGrounded && !wheel.isDriving)
            {
                wheel.AngularVelocity = rollingWheelAngularVelocity + wheelAngularAcceleration * dt;
                wheel.AngularVelocity =
                    ClampWheelAngularVelocity(wheel.AngularVelocity, maxWheelAngularVelocity);
            }

            // TODO: Dynamically compute tire load
            float tireLoad = _rb.mass / 4f * 9.81f / 1000f;

            float loadedTireRadius = wheel.TireRadius;
            float wheelSurfaceSpeed = wheel.AngularVelocity * loadedTireRadius;

            wheel.SlipRatio = (wheelSurfaceSpeed - wheelLinearVelocity) /
                              Mathf.Max(Mathf.Abs(wheelLinearVelocity), minSlipSpeed);
            wheel.SlipRatio = Mathf.Clamp(wheel.SlipRatio, -1f, 1f);

            Vector3 steeringDir = wheel.transform.right;
            float steeringVel = Vector3.Dot(steeringDir, wheelWorldVelocity);

            float speedForSlipAngle = Mathf.Max(Mathf.Abs(wheelLinearVelocity), minSlipSpeed);
            wheel.SlipAngle = Mathf.Atan2(steeringVel, speedForSlipAngle) * Mathf.Rad2Deg;


            if (wheel.canSteer)
            {
                // Calculate the rotation angle based on input
                float steeringRotationAngle =
                    Utils.RemapToRange(Steering, -1f, 1f, -maxSteeringAngle, maxSteeringAngle);

                // Set the steering rotation of the wheel (transform and mesh) around its local up axis
                wheel.transform.localRotation = Quaternion.Euler(0.0f, steeringRotationAngle, 0.0f);
            }

            if (wheel.IsGrounded)
            {
                const float minCombinedParam = 1e-4f;

                float combinedParam = Mathf.Sqrt(wheel.SlipRatio * wheel.SlipRatio + wheel.SlipAngle * wheel.SlipAngle);

                Vector3 totalForce = Vector3.zero;

                if (combinedParam > minCombinedParam)
                {
                    float longitudinalForce =
                        wheel.SlipRatio * Utils.ComputePacejkaMagicFormula(combinedParam, tireLoad,
                            wheel.tire.GetPacejkaMagicFormulaParams()) / combinedParam;

                    float lateralForce =
                        wheel.SlipAngle * Utils.ComputeLateralPacejkaMagicFormula(combinedParam, tireLoad, 0f,
                            wheel.tire.GetPacejkaLateralMagicFormulaParams()) / combinedParam;


                    float speedForBlend = Mathf.Abs(wheelLinearVelocity);
                    float lowSpeedFade = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(lowSpeedBlendEnd, lowSpeedBlendStart, speedForBlend));

                    lateralForce *= lowSpeedFade;

                    // Debug.Log("Lon Force: " + longitudinalForce);
                    // Debug.Log("Lat Force: " + lateralForce);

                    totalForce = longitudinalForce * wheel.longitudinalForceVector -
                                 lateralForce * wheel.lateralForceVector;
                }

                _rb.AddForceAtPosition(
                    totalForce,
                    wheel.contactPoint,
                    ForceMode.Force
                );

                Debug.DrawRay(
                    wheel.contactPoint,
                    totalForce / 1000f,
                    Color.green
                );
            }

            if ((!wheel.isDriving || !hasGroundedDrivenWheels || _gear == 0) && wheel.IsGrounded)
                wheel.AngularVelocity = Mathf.MoveTowards(
                    wheel.AngularVelocity,
                    rollingWheelAngularVelocity,
                    wheelRollingResistance * dt
                );
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

        float averageWheelAngularVelocity = 0f;
        foreach (Wheel wheel in drivenWheels)
            averageWheelAngularVelocity += wheel.AngularVelocity;
        averageWheelAngularVelocity /= drivenWheels.Count;

        float crawlSpeedWheelAngularVelocity =
            Utils.ComputeWheelAngularVelocityAtEngineRpm(engine.idleRpm, _gear, transmission);

        float engineFrictionFade = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(engine.idleRpm + 50f, engine.idleRpm + 250f, _currentEngineRpm));
        float crawlTorqueMultiplier = Mathf.Approximately(Brake, 0f)
            ? Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(crawlSpeedWheelAngularVelocity, 0f, averageWheelAngularVelocity))
            : 0f;

        float crawlTorque = engine.GetTorque(engine.idleRpm) * crawlTorqueMultiplier;


        float maxWheelAngularVelocity = GetMaxWheelAngularVelocity(gearRatio);
        float engineAngularVelocity = Utils.FromRpmToAngularVelocity(_currentEngineRpm);
        float engineFrictionTorque = engineFriction * engineAngularVelocity * engineFrictionFade;
        float drivelineTorque = (engineTorque + crawlTorque - engineFrictionTorque) * gearRatio *
                                transmission.transmissionEfficiency;
        float torquePerWheel = drivelineTorque / drivenWheels.Count;
        float reflectedEngineInertia = engineInertia * gearRatio * gearRatio / drivenWheels.Count;
        float wheelInertia = Mathf.Max(drivenWheelInertia + reflectedEngineInertia, 0.001f);

        Debug.Log("Driveline torque: " + drivelineTorque + "; engineFrictionTorque: " + engineFrictionTorque);


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

        GearRatioLabel = $"Gear Ratio: {Utils.GetTotalGearRatio(_gear, transmission):F3}";

        Debug.Log("Gear Shift: " + _gear);
    }

    private void UpdateLabels()
    {
        _speed = _rb.linearVelocity.magnitude;

        RpmLabel = $"{(int)_currentEngineRpm} RPM";
        SpeedLabel = $"{(int)(_speed * 3.6f)} km/h";

        FLSlipAngle = $"SA {wheelFL.SlipAngle:F3}";
        FRSlipAngle = $"SA {wheelFR.SlipAngle:F3}";
        RLSlipAngle = $"SA {wheelRL.SlipAngle:F3}";
        RRSlipAngle = $"SA {wheelRR.SlipAngle:F3}";

        FLSlipRatio = $"SR {wheelFL.SlipRatio:F1}";
        FRSlipRatio = $"SR {wheelFR.SlipRatio:F1}";
        RLSlipRatio = $"SR {wheelRL.SlipRatio:F1}";
        RRSlipRatio = $"SR {wheelRR.SlipRatio:F1}";
    }
}