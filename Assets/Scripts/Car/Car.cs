using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car : MonoBehaviour
{
    [Header("Car Components")] public Wheel wheelFL;
    public Wheel wheelFR;
    public Wheel wheelRL;
    public Wheel wheelRR;
    public EngineSO engine;
    public TransmissionSO transmission;
    public SuspensionSO suspension;

    public List<Light> brakeLights;

    [Header("Engine and wheel inertia")] public float engineInertia = 0.35f;
    public float drivenWheelInertia = 1.2f;
    public float freeWheelInertia = 1.9f;

    [Header("Engine and wheel resisting forces")]
    public float engineFriction = 0.15f;

    public float wheelRollingResistance = 0.08f;

    [Header("TSC (Traction Stability Control)")] [InspectorName("TSC Enabled")]
    public bool tscEnabled = true;

    [InspectorName("TSC Target Slip")] public float tscTargetSlip = 0.1f;
    [InspectorName("TSC Sensitivity")] public float tscSensitivity = 10f;

    [Header("ABS (Anti Block System)")] [InspectorName("ABS Enabled")]
    public bool absEnabled = true;

    [InspectorName("ABS Target Slip")] public float absTargetSlip = 0.1f;
    [InspectorName("ABS Sensitivity")] public float absSensitivity = 3f;

    [Header("Speed-Sensitive Steering")]
    public AnimationCurve maxSteeringAngleBySpeed = AnimationCurve.Linear(0f, 30f, 100f, 5f);

    [Header("Other")] public bool automaticTransmission = true;
    public float wheelBrakeTorque = 100f;
    public float minSlipSpeed = 0.5f;

    [HideInInspector] public string GearLabel = "N";
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

    [HideInInspector] public float Throttle;
    [HideInInspector] public float Brake;
    [HideInInspector] public float AppliedThrottle;
    [HideInInspector] public float AppliedBrake;
    [HideInInspector] public float Steering;
    [HideInInspector] public string SideslipAngle;

    private Rigidbody _rb;
    private List<Wheel> _wheels;

    private int _gear;
    private bool _handbrake;
    private float _currentEngineRpm = 1000f;

    public float Speed { get; private set; }

    private const float LockSpeedThreshold = 0.05f; // m/s
    private const float LockAngularThreshold = 0.15f; // rad/s
    private const float BrakeLockThreshold = 0.05f;

    private const float CombinedSlipCoefficient = 4f;
    private const float LateralVelocityHoldThreshold = 2f;
    private const float BrakeHoldVelocityThreshold = 0.5f;

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
        bool shouldHoldCar = Brake >= 0.99 && Speed <= BrakeHoldVelocityThreshold;

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

        if (_handbrake)
        {
            wheelRL.AngularVelocity = 0;
            wheelRR.AngularVelocity = 0;
        }

        // 2nd Pass: Compute and apply wheel forces
        foreach (Wheel wheel in _wheels)
        {
            float slipError = wheel.SlipRatio - tscTargetSlip; // ~0.08–0.12
            float torqueMultiplier = tscEnabled ? Mathf.Clamp01(1f - tscSensitivity * slipError) : 1f;
            AppliedThrottle = torqueMultiplier * Throttle;

            float scaledWheelTorque = driveTorquePerWheel * torqueMultiplier;

            float driveTorque = wheel.isDriving ? scaledWheelTorque : 0f;

            float wheelInertia = wheel.isDriving && wheel.IsGrounded ? coupledDrivenWheelInertia : freeWheelInertia;

            if (!shouldHoldCar) SimulateWheelForces(wheel, driveTorque, wheelInertia, dt);
        }

        if (shouldHoldCar) HoldCar();

        // if (_rb.linearVelocity.x <= LateralVelocityHoldThreshold) HoldCarLaterally();

        if (_gear != 0 && drivenWheels.Count > 0)
            SyncEngineRpmToDrivenWheels(drivenWheels, Utils.GetTotalGearRatio(_gear, transmission));
        else UpdateFreeEngine(dt);

        Speed = _rb.linearVelocity.magnitude;

        if (automaticTransmission) HandleAutoGearShift();
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

        return rpm < engine.idleRpm ? engine.idleRpm : rpm;

        // return Mathf.Clamp(rpm, engine.idleRpm, engine.maxRpm);
    }

    private void UpdateFreeEngine(float dt)
    {
        float engineTorque = engine.GetTorque(_currentEngineRpm) * Throttle;
        float engineAngularVelocity = Utils.FromRpmToAngularVelocity(_currentEngineRpm);
        float engineAngularAcceleration = engineTorque / Mathf.Max(engineInertia, 0.001f) -
                                          engineFriction * engineAngularVelocity;

        _currentEngineRpm = Mathf.Clamp(ClampEngineRpm(
            Utils.FromAngularVelocityToRpm(engineAngularVelocity + engineAngularAcceleration * dt)
        ), engine.idleRpm, engine.maxRpm);
    }

    private void SimulateWheelForces(Wheel wheel, float wheelDriveTorque, float wheelInertia, float dt)
    {
        Vector3 pointVelocity = _rb.GetPointVelocity(wheel.ContactPoint);
        float longitudinalSpeed = Vector3.Dot(pointVelocity, wheel.LongitudinalForceDir);

        if (ShouldLockWheel(Brake, longitudinalSpeed, wheel.AngularVelocity)) wheel.AngularVelocity = 0f;

        float surfaceSpeed = wheel.AngularVelocity * wheel.TireRadius;
        wheel.SlipRatio = Utils.ComputeSlipRatio(surfaceSpeed, longitudinalSpeed, minSlipSpeed);

        Vector3 steeringDir = wheel.transform.right;
        float steeringVelocity = Vector3.Dot(steeringDir, pointVelocity);

        float speedForSlipAngle = Mathf.Max(Mathf.Abs(longitudinalSpeed), minSlipSpeed);
        wheel.SlipAngle = Mathf.Atan2(steeringVelocity, speedForSlipAngle) * Mathf.Rad2Deg;

        // Tire load in Kilo Newtons
        float tireLoad = wheel.NormalLoad / 1000f;

        // Simple elliptical combined slip
        float longitudinalForceMagnitude =
            Utils.ComputePacejkaMagicFormula(wheel.SlipRatio, tireLoad, wheel.tire.GetPacejkaMagicFormulaParams());
        float lateralForceMagnitude = Utils.ComputeLateralPacejkaMagicFormula(wheel.SlipAngle, tireLoad, 0f,
            wheel.tire.GetPacejkaLateralMagicFormulaParams());

        float maxLongitudinalForce = Utils.ComputePacejkaMagicFormula(wheel.MaxForceSlipRatio, tireLoad,
            wheel.tire.GetPacejkaMagicFormulaParams());
        float maxLateralForce = Utils.ComputeLateralPacejkaMagicFormula(wheel.MaxForceSlipAngle, tireLoad, 0f,
            wheel.tire.GetPacejkaLateralMagicFormulaParams());

        float combinedSlip = Mathf.Pow(
            Mathf.Pow(longitudinalForceMagnitude / maxLongitudinalForce, CombinedSlipCoefficient) +
            Mathf.Pow(lateralForceMagnitude / maxLateralForce, CombinedSlipCoefficient), 1 / CombinedSlipCoefficient);

        if (combinedSlip > 1f)
        {
            longitudinalForceMagnitude /= combinedSlip;
            lateralForceMagnitude /= combinedSlip;
        }

        Vector3 longitudinalForce = longitudinalForceMagnitude * wheel.LongitudinalForceDir;
        Vector3 lateralForce = lateralForceMagnitude * wheel.LateralForceDir;

        float lateralForceMultiplier =
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, LateralVelocityHoldThreshold, Speed));

        // if (_rb.linearVelocity.x <= LateralVelocityHoldThreshold)
        //     lateralForce = new Vector3(0f, lateralForce.y, lateralForce.z);

        Vector3 totalForce = longitudinalForce - lateralForce * lateralForceMultiplier;

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
                Color.darkGray
            );

            Debug.DrawRay(
                wheel.ContactPoint,
                longitudinalForce / 1000f,
                longitudinalForceMagnitude > 0 ? Color.deepSkyBlue : Color.deepPink
            );

            Debug.DrawRay(
                wheel.ContactPoint,
                -lateralForce / 1000f,
                Color.yellowNice
            );
        }

        float contactTorque = wheel.IsGrounded ? longitudinalForceMagnitude * wheel.TireRadius : 0f;
        float netTorque = wheelDriveTorque - ComputeBrakeTorque(wheel) -
                          wheelRollingResistance * wheel.AngularVelocity - contactTorque;

        wheel.AngularVelocity += netTorque * dt / Mathf.Max(wheelInertia, 0.001f);
    }

    private float ComputeDriveTorquePerWheel(List<Wheel> drivenWheels, float dt)
    {
        if (drivenWheels.Count == 0) return 0f;

        float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);

        if (Mathf.Approximately(gearRatio, 0f)) return 0f;

        float refOvershootCoefficient = _currentEngineRpm >= engine.maxRpm
            ? Mathf.InverseLerp(0f, 100f, _currentEngineRpm - engine.maxRpm)
            : 0f;

        float maxEngineTorque = engine.GetTorque(_currentEngineRpm);
        float engineTorque = maxEngineTorque * Throttle * (1f - refOvershootCoefficient);

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
        float brakeMultiplier = Mathf.SmoothStep(0.4f, 1f,
            Mathf.InverseLerp(0f, 20f, Mathf.Abs(wheel.AngularVelocity)));

        float brakeTorque = wheelBrakeTorque * Brake * Mathf.Sign(wheel.AngularVelocity) * brakeMultiplier;

        float slipError = Mathf.Abs(wheel.SlipRatio) - absTargetSlip;
        float brakeScale = absEnabled ? Mathf.Clamp01(1f - absSensitivity * slipError) : 1f;

        AppliedBrake = Brake * brakeScale;

        float brakeTorqueWithAbs = brakeTorque * brakeScale;

        if (Mathf.Approximately(wheel.AngularVelocity, 0f)) return 0f;

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

        if (Brake > 0)
            brakeLights.ForEach(l =>
            {
                if (!l.enabled) l.enabled = true;
            });

        if (Brake == 0)
            brakeLights.ForEach(l =>
            {
                if (l.enabled) l.enabled = false;
            });
    }

    private void OnGearShift(InputValue v)
    {
        int value = (int)v.Get<float>();

        if (_gear == -1 && value == -1) return;
        if (_gear == transmission.gears.Count && value == 1) return;

        _gear += value;


        Debug.Log("Gear Shift: " + _gear);
    }

    private void OnHandbrake(InputValue v)
    {
        _handbrake = v.isPressed;
        Debug.Log("Handbrake");
    }

    private void UpdateLabels()
    {
        FLWheelRpm = GetWheelRpmLabel(wheelFL);
        FRWheelRpm = GetWheelRpmLabel(wheelFR);
        RLWheelRpm = GetWheelRpmLabel(wheelRL);
        RRWheelRpm = GetWheelRpmLabel(wheelRR);

        SideslipAngle = "Sideslip Angle: " + Utils.ComputeVehicleSideslipAngle(_rb.linearVelocity, transform)
            .ToString("0.#°");

        RpmLabel = $"{(int)_currentEngineRpm} RPM";
        SpeedLabel = $"{(int)Utils.FromMetersPerSecondToKmPerHour(Speed)} km/h";

        FLSlipAngle = $"SA {wheelFL.SlipAngle:F1}";
        FRSlipAngle = $"SA {wheelFR.SlipAngle:F1}";
        RLSlipAngle = $"SA {wheelRL.SlipAngle:F1}";
        RRSlipAngle = $"SA {wheelRR.SlipAngle:F1}";

        FLSlipRatio = $"SR {wheelFL.SlipRatio:F1}";
        FRSlipRatio = $"SR {wheelFR.SlipRatio:F1}";
        RLSlipRatio = $"SR {wheelRL.SlipRatio:F1}";
        RRSlipRatio = $"SR {wheelRR.SlipRatio:F1}";

        if (_gear == -1)
            GearLabel = "R";
        else if (_gear == 0)
            GearLabel = "N";
        else
            GearLabel = _gear.ToString();
    }

    private void HoldCar()
    {
        _rb.linearVelocity = Vector3.Project(_rb.linearVelocity, transform.up);
        _rb.angularVelocity -= Vector3.Project(_rb.angularVelocity, transform.up);
    }

    private void HoldCarLaterally()
    {
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, _rb.linearVelocity.z);
    }

    private void HandleAutoGearShift()
    {
        if (_gear == 0) return;

        if (_gear < transmission.gears.Count && _currentEngineRpm >= engine.maxRpm - engine.maxRpm * .05) _gear++;
        if (_gear > 1 && _currentEngineRpm <= engine.idleRpm + engine.maxRpm * .3) _gear--;
    }
}