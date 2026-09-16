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

    [HideInInspector] public string GearLabel = "N";
    [HideInInspector] public string GearRatioLabel = "Gear Ratio: 0";
    [HideInInspector] public string RpmLabel = "1000 RPM";
    [HideInInspector] public string SpeedLabel = "0 km/h";

    [HideInInspector] public string FLWheelRpm = "0";
    [HideInInspector] public string FRWheelRpm = "0";
    [HideInInspector] public string RLWheelRpm = "0";
    [HideInInspector] public string RRWheelRpm = "0";

    [HideInInspector] public string FLSlipAngle = "0";
    [HideInInspector] public string FRSlipAngle = "0";
    [HideInInspector] public string RLSlipAngle = "0";
    [HideInInspector] public string RRSlipAngle = "0";

    [HideInInspector] public string FLSlipRatio = "0";
    [HideInInspector] public string FRSlipRatio = "0";
    [HideInInspector] public string RLSlipRatio = "0";
    [HideInInspector] public string RRSlipRatio = "0";

    [HideInInspector] public string TscLabel = "";

    private Rigidbody _rb;

    private int _gear = 0;
    private float _speed = 0f;
    [HideInInspector] public float Throttle = 0f;
    [HideInInspector] public float Brake = 0f;
    [HideInInspector] public float Steering = 0f;

    private float _currentEngineRpm = 1000f;

    public float engineInertia = 0.35f;
    public float drivenWheelInertia = 1.2f;
    public float freeWheelInertia = 1.9f;
    public float engineFriction = 0.15f;
    public float wheelRollingResistance = 0.08f;
    public float wheelBrakeTorque = 100f;
    public float minSlipSpeed = 0.5f;

    public bool tscEnabled = true;
    public float tscTargetSlip = 0.1f;
    public float tscSensitivity = 10f;

    public bool absEnabled = true;
    public float absTargetSlip = -0.1f;
    public float absSensitivity = 3f;

    [Header("Speed-Sensitive Steering")]
    public AnimationCurve maxSteeringAngleBySpeed = AnimationCurve.Linear(0f, 30f, 100f, 5f);

    private float lowSpeedBlendStart = 4.0f; // m/s
    private float lowSpeedBlendEnd = 0.5f; // m/s

    private const float LockSpeedThreshold = 0.05f; // m/s
    private const float LockAngularThreshold = 0.15f; // rad/s
    private const float BrakeLockThreshold = 0.05f;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _wheels = new List<Wheel> { wheelFL, wheelFR, wheelRL, wheelRR };
    }

    private void Update()
    {
        UpdateLabels();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;


        // 1st Pass: Update steering wheel angle, compute contact point and handle suspension
        foreach (Wheel wheel in _wheels)
        {
            Vector3 wheelWorldVelocity = _rb.GetPointVelocity(transform.position);
            float maxSteeringAngle =
                maxSteeringAngleBySpeed.Evaluate(wheelWorldVelocity.magnitude);

            wheel.HandleSteeringRotation(Steering, maxSteeringAngle);
            wheel.SimulateContactAndSuspension();
        }

        // With the up-to-date IsGrounded flag, compute parameters for future applied forces
        List<Wheel> drivenWheels = _wheels.Where(wheel => wheel.isDriving && wheel.IsGrounded).ToList();
        float driveTorquePerWheel = ComputeDriveTorquePerWheel(drivenWheels, dt);
        float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);
        float reflectedEngineInertia =
            drivenWheels.Count > 0 ? engineInertia * gearRatio * gearRatio / drivenWheels.Count : 0f;
        float coupledDrivenWheelInertia = drivenWheelInertia + reflectedEngineInertia;

        // 2nd Pass: Compute and apply wheel forces
        foreach (Wheel wheel in _wheels)
        {
            float slipError = wheel.SlipRatio - tscTargetSlip; // ~0.08–0.12
            float torqueScale = tscEnabled ? Mathf.Clamp01(1f - tscSensitivity * slipError) : 1f;
            float scaledWheelTorque = driveTorquePerWheel * torqueScale;

            float driveTorque = wheel.isDriving ? scaledWheelTorque : 0f;

            float wheelInertia = wheel.isDriving && wheel.IsGrounded ? coupledDrivenWheelInertia : freeWheelInertia;

            SimulateWheelForces(wheel, driveTorque, wheelInertia, dt);
        }

        if (_gear != 0 && drivenWheels.Count > 0)
            SyncEngineRpmToDrivenWheels(drivenWheels, Utils.GetTotalGearRatio(_gear, transmission));
        else UpdateFreeEngine(dt);
    }

    private void SyncEngineRpmToDrivenWheels(IReadOnlyList<Wheel> drivenWheels, float gearRatio)
    {
        float averageWheelAngularVelocity = 0f;
        foreach (Wheel wheel in drivenWheels)
            averageWheelAngularVelocity += wheel.AngularVelocity;
        averageWheelAngularVelocity /= drivenWheels.Count;

        float engineRpmFromWheels = Mathf.Abs(Utils.FromAngularVelocityToRpm(averageWheelAngularVelocity * gearRatio));

        // Debug.Log(engineRpmFromWheels);
        _currentEngineRpm = ClampEngineRpm(engineRpmFromWheels);
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

    private void SimulateWheelForces(Wheel wheel, float wheelDriveTorque, float wheelInertia, float dt)
    {
        // if (!wheel.IsGrounded) return;

        Vector3 pointVelocity = _rb.GetPointVelocity(wheel.ContactPoint);
        float longitudinalSpeed = Vector3.Dot(pointVelocity, wheel.LongitudinalForceDir);

        if (ShouldLockWheel(Brake, longitudinalSpeed, wheel.AngularVelocity)) wheel.AngularVelocity = 0f;

        // wheel.SlipRatio = 0f;
        // wheel.SlipAngle = 0f;
        // return;
        float surfaceSpeed = wheel.AngularVelocity * wheel.TireRadius;
        wheel.SlipRatio = Utils.ComputeSlipRatio(surfaceSpeed, longitudinalSpeed, minSlipSpeed);

        Vector3 steeringDir = wheel.transform.right;
        float steeringVelocity = Vector3.Dot(steeringDir, pointVelocity);

        float speedForSlipAngle = Mathf.Max(Mathf.Abs(longitudinalSpeed), minSlipSpeed);
        wheel.SlipAngle = Mathf.Atan2(steeringVelocity, speedForSlipAngle) * Mathf.Rad2Deg;

        // Tire load in Kilo Newtons
        float tireLoad = wheel.NormalLoad / 1000f;

        const float minCombinedSlip = 1e-4f;

        float normalizedSlipRatio = wheel.SlipRatio / wheel.tire.maxForceSlipRatio;
        float normalizedSlipAngle = wheel.SlipAngle / wheel.tire.maxForceSlipAngle;
        float combinedSlip = Mathf.Sqrt(normalizedSlipRatio * normalizedSlipRatio +
                                        normalizedSlipAngle * normalizedSlipAngle);
        combinedSlip = Mathf.Max(combinedSlip, minCombinedSlip);

        float equivalentSlipRatio = wheel.tire.maxForceSlipRatio * combinedSlip;
        float equivalentSlipAngle = wheel.tire.maxForceSlipAngle * combinedSlip;

        float longitudinalForceLimit = Utils.ComputePacejkaMagicFormula(equivalentSlipRatio, tireLoad,
            wheel.tire.GetPacejkaMagicFormulaParams());
        float lateralForceLimit = Utils.ComputeLateralPacejkaMagicFormula(equivalentSlipAngle, tireLoad,
            0f,
            wheel.tire.GetPacejkaLateralMagicFormulaParams());

        longitudinalForceLimit = Mathf.Abs(normalizedSlipRatio / combinedSlip * longitudinalForceLimit);
        lateralForceLimit = Mathf.Abs(normalizedSlipAngle / combinedSlip * lateralForceLimit);

        longitudinalForceLimit = Mathf.Max(longitudinalForceLimit, 1f);
        lateralForceLimit = Mathf.Max(lateralForceLimit, 1f);

        float wheelEffectiveMass = wheelInertia / (wheel.TireRadius * wheel.TireRadius);
        float carEffectiveMass = wheel.NormalLoad / 9.81f;
        float longitudinalEffectiveMass = 1f / (1f / wheelEffectiveMass + 1f / carEffectiveMass);
        float lateralEffectiveMass = carEffectiveMass;

        float longitudinalSlipVelocity = surfaceSpeed - longitudinalSpeed;
        float lateralSlipVelocity = steeringVelocity;

        float desiredLongitudinalForce = longitudinalSlipVelocity * longitudinalEffectiveMass / dt;
        float desiredLateralForce = lateralSlipVelocity * lateralEffectiveMass / dt;

        Vector2 desiredNorm = new(desiredLongitudinalForce / longitudinalForceLimit,
            desiredLateralForce / lateralForceLimit);
        float desiredMagNorm = desiredNorm.magnitude;
        float scale = desiredMagNorm > 1f ? 1f / desiredMagNorm : 1f;

        float longitudinalForce = desiredLongitudinalForce * scale;
        float lateralForce = desiredLateralForce * scale;

        float speedForBlend = Mathf.Abs(longitudinalSpeed);
        float lowSpeedFade = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(lowSpeedBlendEnd, lowSpeedBlendStart, speedForBlend));

        // lateralForce *= lowSpeedFade;

        Vector3 totalForce = longitudinalForce * wheel.LongitudinalForceDir -
                             lateralForce * wheel.LateralForceDir;

        if (wheel.IsGrounded)
        {
            _rb.AddForceAtPosition(
                totalForce,
                wheel.ContactPoint,
                ForceMode.Force
            );

            Debug.DrawRay(
                wheel.ContactPoint,
                totalForce / 1000f,
                Color.green
            );
        }

        float contactTorque = wheel.IsGrounded ? longitudinalForce * wheel.TireRadius : 0f;
        float netTorque = wheelDriveTorque - ComputeBrakeTorque(wheel) -
                          wheelRollingResistance * wheel.AngularVelocity - contactTorque;

        // Debug.Log(wheelRollingResistance * wheel.AngularVelocity);
        // Debug.Log("contactTorque: " + contactTorque);

        wheel.AngularVelocity += netTorque * dt / Mathf.Max(wheelInertia, 0.001f);
    }

    private float ComputeDriveTorquePerWheel(List<Wheel> drivenWheels, float dt)
    {
        if (drivenWheels.Count == 0) return 0f;

        float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);

        if (Mathf.Approximately(gearRatio, 0f)) return 0f;

        float maxEngineTorque = engine.GetTorque(_currentEngineRpm);
        float engineTorque = maxEngineTorque * Throttle;

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


        float engineAngularVelocity = Utils.FromRpmToAngularVelocity(_currentEngineRpm);
        float engineFrictionTorque = engineFriction * engineAngularVelocity * engineFrictionFade;
        float drivelineTorque = (engineTorque + crawlTorque - engineFrictionTorque) * gearRatio *
                                transmission.transmissionEfficiency;

        return drivelineTorque / drivenWheels.Count;
    }

    private bool ShouldLockWheel(float brake, float groundSpeed, float angularVelocity)
    {
        return brake > BrakeLockThreshold &&
               Mathf.Abs(groundSpeed) < LockSpeedThreshold &&
               Mathf.Abs(angularVelocity) < LockAngularThreshold;
    }

    private float ComputeBrakeTorque(Wheel wheel)
    {
        float brakeMultiplier = Mathf.SmoothStep(0.01f, 1f,
            Mathf.InverseLerp(0f, 20f, Mathf.Abs(wheel.AngularVelocity)));

        float brakeTorque = wheelBrakeTorque * Brake * Mathf.Sign(wheel.AngularVelocity) * brakeMultiplier;

        float slipError = Mathf.Abs(wheel.SlipRatio - absTargetSlip);
        float brakeScale = absEnabled ? Mathf.Clamp01(1f - absSensitivity * slipError) : 1f;
        float brakeTorqueWithAbs = brakeTorque * brakeScale;

        if (Mathf.Approximately(wheel.AngularVelocity, 0f)) return 0f;

        // Debug.Log("BrakeTorque: " + brakeTorque);

        return brakeTorqueWithAbs;
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
        FLWheelRpm = GetWheelRpmLabel(wheelFL);
        FRWheelRpm = GetWheelRpmLabel(wheelFR);
        RLWheelRpm = GetWheelRpmLabel(wheelRL);
        RRWheelRpm = GetWheelRpmLabel(wheelRR);

        _speed = _rb.linearVelocity.magnitude;

        RpmLabel = $"{(int)_currentEngineRpm} RPM";
        SpeedLabel = $"{(int)Utils.FromMetersPerSecondToKmPerHour(_speed)} km/h";

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