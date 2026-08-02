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

    public string TscLabel = "";

    private Rigidbody _rb;

    private int _gear = 0;
    private float _speed = 0f;
    public float Throttle = 0f;
    public float Brake = 0f;
    public float Steering = 0f;

    private float _currentEngineRpm = 1000f;

    public float engineInertia = 0.35f;
    public float drivenWheelInertia = 1.2f;
    public float freeWheelInertia = 1.9f;
    public float engineFriction = 0.15f;
    public float wheelRollingResistance = 0.08f;
    public float wheelBrakeTorque = 100f;
    public float minSlipSpeed = 0.5f;

    public float tscTargetSlip = 0.1f;
    public float tscSensitivity = 10f;

    public float steeringWheelTurningSpeed = 10f;

    // public float maxSteeringAngle = 30f;
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

        List<Wheel> drivenWheels = _wheels.Where(wheel => wheel.isDriving && wheel.IsGrounded).ToList();
        float driveTorquePerWheel = ComputeDriveTorquePerWheel(drivenWheels, dt);
        float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);
        float reflectedEngineInertia = engineInertia * gearRatio * gearRatio / drivenWheels.Count;
        float coupledDrivenWheelInertia = drivenWheelInertia + reflectedEngineInertia;

        foreach (Wheel wheel in _wheels)
        {
            if (wheel.canSteer)
            {
                Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
                float maxSteeringAngle =
                    maxSteeringAngleBySpeed.Evaluate(wheelWorldVelocity.magnitude);

                float targetAngle = Mathf.Lerp(
                    -maxSteeringAngle,
                    maxSteeringAngle,
                    (Steering + 1f) * 0.5f
                );

                float currentAngle = wheel.transform.localEulerAngles.y;

                if (currentAngle > 180f) currentAngle -= 360f;


                float smoothAngle = Mathf.SmoothDampAngle(
                    currentAngle,
                    targetAngle,
                    ref steeringWheelTurningSpeed,
                    0.12f
                );

                wheel.transform.localRotation = Quaternion.Euler(0.0f, smoothAngle, 0.0f);
            }

            float driveTorque = wheel.isDriving ? driveTorquePerWheel : 0f;

            float wheelInertia = wheel.isDriving && wheel.IsGrounded ? coupledDrivenWheelInertia : freeWheelInertia;

            SimulateWheel(wheel, driveTorque, wheelInertia, dt);
        }

        if (_gear != 0 && drivenWheels.Count > 0)
            SyncEngineRpmToDrivenWheels(drivenWheels, Utils.GetTotalGearRatio(_gear, transmission));
        else UpdateFreeEngine(dt);
    }

    // private void FixedUpdate()
    // {
    //     float dt = Time.fixedDeltaTime;
    //
    //     List<Wheel> drivenWheels = _wheels.Where(wheel => wheel.isDriving && wheel.IsGrounded).ToList();
    //     List<Wheel> groundedWheels = _wheels.Where(wheel => wheel.IsGrounded).ToList();
    //
    //
    //     bool hasGroundedDrivenWheels = drivenWheels.Count > 0;
    //
    //     float maxEngineTorque = engine.GetTorque(_currentEngineRpm);
    //     float engineTorque = maxEngineTorque * Throttle;
    //
    //     if (_gear == 0 || !hasGroundedDrivenWheels)
    //         UpdateFreeEngine(dt);
    //     else
    //         ApplyDrivetrainInertia(drivenWheels, engineTorque, dt);
    //
    //
    //     foreach (Wheel wheel in _wheels)
    //     {
    //         Vector3 wheelWorldVelocity = _rb.GetPointVelocity(wheel.transform.position);
    //         float wheelLinearVelocity = Vector3.Dot(wheel.transform.forward, wheelWorldVelocity);
    //
    //         float rollingWheelAngularVelocity = GetRollingWheelAngularVelocity(wheel, wheelLinearVelocity);
    //
    //         float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);
    //         float maxWheelAngularVelocity = GetMaxWheelAngularVelocity(gearRatio);
    //         float brakeTorque = ComputeBrakeTorque(wheel);
    //         float rollingResistanceTorque = wheelRollingResistance * wheel.AngularVelocity;
    //         float wheelAngularAcceleration =
    //             -brakeTorque / Mathf.Max(drivenWheelInertia, 0.001f);
    //
    //         if (wheel.IsGrounded && !wheel.isDriving)
    //         {
    //             // Debug.Log("wheelAngularAcceleration: " + wheelAngularAcceleration + ";\n rollingResistanceTorque: " +
    //             //           rollingResistanceTorque);
    //             // Debug.Log("rollingWheelAngularVelocity: " + rollingWheelAngularVelocity);
    //
    //             wheel.AngularVelocity = rollingWheelAngularVelocity >= 0f
    //                 ? Mathf.Clamp(rollingWheelAngularVelocity + wheelAngularAcceleration * dt,
    //                     0f, rollingWheelAngularVelocity)
    //                 : Mathf.Clamp(rollingWheelAngularVelocity + wheelAngularAcceleration * dt,
    //                     -rollingWheelAngularVelocity, 0f);
    //             wheel.AngularVelocity =
    //                 ClampWheelAngularVelocity(wheel.AngularVelocity, maxWheelAngularVelocity);
    //         }
    //
    //         // TODO: Dynamically compute tire load
    //         float tireLoad = _rb.mass / 4f * 9.81f / 1000f;
    //
    //         float loadedTireRadius = wheel.TireRadius;
    //         float wheelSurfaceSpeed = wheel.AngularVelocity * loadedTireRadius;
    //
    //         wheel.SlipRatio = (wheelSurfaceSpeed - wheelLinearVelocity) /
    //                           Mathf.Max(Mathf.Abs(wheelLinearVelocity), minSlipSpeed);
    //         wheel.SlipRatio = Mathf.Clamp(wheel.SlipRatio, -1f, 1f);
    //
    //         Vector3 steeringDir = wheel.transform.right;
    //         float steeringVel = Vector3.Dot(steeringDir, wheelWorldVelocity);
    //
    //         float speedForSlipAngle = Mathf.Max(Mathf.Abs(wheelLinearVelocity), minSlipSpeed);
    //         wheel.SlipAngle = Mathf.Atan2(steeringVel, speedForSlipAngle) * Mathf.Rad2Deg;
    //
    //
    //         if (wheel.canSteer)
    //         {
    //             float maxSteeringAngle =
    //                 maxSteeringAngleBySpeed.Evaluate(wheelWorldVelocity.magnitude);
    //
    //             float targetAngle = Mathf.Lerp(
    //                 -maxSteeringAngle,
    //                 maxSteeringAngle,
    //                 (Steering + 1f) * 0.5f
    //             );
    //
    //             float currentAngle = wheel.transform.localEulerAngles.y;
    //
    //             if (currentAngle > 180f) currentAngle -= 360f;
    //
    //
    //             float smoothAngle = Mathf.SmoothDampAngle(
    //                 currentAngle,
    //                 targetAngle,
    //                 ref steeringWheelTurningSpeed,
    //                 0.12f
    //             );
    //
    //             // float maxSteeringAngle = maxSteeringAngleBySpeed.Evaluate(wheelWorldVelocity.magnitude);
    //
    //             // Calculate the rotation angle based on input
    //             // float targetSteeringRotationAngle =
    //             //     Utils.RemapToRange(Steering, -1f, 1f, -maxSteeringAngle, maxSteeringAngle);
    //             // float steeringRotationAngle = Mathf.MoveTowards(wheel.transform.localRotation.y,
    //             //     targetSteeringRotationAngle,
    //             //     steeringWheelTurningSpeed * dt);
    //
    //             // Set the steering rotation of the wheel (transform and mesh) around its local up axis
    //             wheel.transform.localRotation = Quaternion.Euler(0.0f, smoothAngle, 0.0f);
    //         }
    //
    //         if (wheel.IsGrounded)
    //         {
    //             const float minCombinedSlip = 1e-4f;
    //
    //             float normalizedSlipRatio = wheel.SlipRatio / wheel.tire.maxForceSlipRatio;
    //             float normalizedSlipAngle = wheel.SlipAngle / wheel.tire.maxForceSlipAngle;
    //             float combinedSlip = Mathf.Sqrt(normalizedSlipRatio * normalizedSlipRatio +
    //                                             normalizedSlipAngle * normalizedSlipAngle);
    //
    //             float equivalentSlipRatio = wheel.tire.maxForceSlipRatio * combinedSlip;
    //             float equivalentSlipAngle = wheel.tire.maxForceSlipAngle * combinedSlip;
    //
    //             float inverseMagnitude = 1f / Mathf.Max(combinedSlip, minCombinedSlip);
    //
    //             float longitudinalForce =
    //                 normalizedSlipRatio * inverseMagnitude * Utils.ComputePacejkaMagicFormula(equivalentSlipRatio,
    //                     tireLoad,
    //                     wheel.tire.GetPacejkaMagicFormulaParams());
    //
    //             float lateralForce =
    //                 normalizedSlipAngle * inverseMagnitude * Utils.ComputeLateralPacejkaMagicFormula(
    //                     equivalentSlipAngle, tireLoad, 0f,
    //                     wheel.tire.GetPacejkaLateralMagicFormulaParams());
    //
    //
    //             float speedForBlend = Mathf.Abs(wheelLinearVelocity);
    //             float lowSpeedFade = Mathf.SmoothStep(0f, 1f,
    //                 Mathf.InverseLerp(lowSpeedBlendEnd, lowSpeedBlendStart, speedForBlend));
    //
    //             lateralForce *= lowSpeedFade;
    //
    //             // Debug.Log("Lon Force: " + longitudinalForce);
    //             // Debug.Log("Lat Force: " + lateralForce);
    //
    //             Vector3 totalForce = longitudinalForce * wheel.LongitudinalForceDir -
    //                                  lateralForce * wheel.LateralForceDir;
    //
    //             _rb.AddForceAtPosition(
    //                 totalForce,
    //                 wheel.ContactPoint,
    //                 ForceMode.Force
    //             );
    //
    //             Debug.DrawRay(
    //                 wheel.ContactPoint,
    //                 totalForce / 1000f,
    //                 Color.green
    //             );
    //         }
    //
    //
    //         if (Mathf.Approximately(Brake, 0f) && (!wheel.isDriving || !hasGroundedDrivenWheels || _gear == 0) &&
    //             wheel.IsGrounded)
    //             wheel.AngularVelocity = Mathf.MoveTowards(
    //                 wheel.AngularVelocity,
    //                 rollingWheelAngularVelocity,
    //                 wheelRollingResistance * dt
    //             );
    //
    //         bool areAllWheelsNotSpinning = groundedWheels.Count > 1 && groundedWheels.TrueForAll(wheel =>
    //             Mathf.Abs(wheel.AngularVelocity) < 0.2f);
    //
    //         if (Brake > 0.1f && areAllWheelsNotSpinning) wheel.AngularVelocity = 0f;
    //     }
    // }

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


        // Debug.Log("Driveline torque: " + drivelineTorque + "; engineFrictionTorque: " + engineFrictionTorque +
        // ";\n crawlTorque: " + crawlTorque + "; engineTorque: " + engineTorque);


        foreach (Wheel wheel in drivenWheels)
        {
            float slipError = wheel.SlipRatio - tscTargetSlip;
            float torqueScale = Mathf.Clamp01(1f - tscSensitivity * slipError);

            float wheelTorque = torquePerWheel * torqueScale;

            float brakeTorque = ComputeBrakeTorque(wheel);
            float rollingResistanceTorque = wheelRollingResistance * wheel.AngularVelocity;
            float wheelAngularAcceleration =
                (wheelTorque - brakeTorque - rollingResistanceTorque) / wheelInertia;

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
        Debug.Log(engineRpmFromWheels);
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

    private void SimulateWheel(Wheel wheel, float wheelDriveTorque, float wheelInertia, float dt)
    {
        if (!wheel.IsGrounded) return;

        Vector3 pointVelocity = _rb.GetPointVelocity(wheel.ContactPoint);
        float longitudinalSpeed = Vector3.Dot(pointVelocity, wheel.LongitudinalForceDir);

        if (ShouldLockWheel(Brake, longitudinalSpeed, wheel.AngularVelocity))
        {
            wheel.AngularVelocity = 0f;
            wheel.SlipRatio = 0f;
            wheel.SlipAngle = 0f;
            return;
        }

        float surfaceSpeed = wheel.AngularVelocity * wheel.TireRadius;
        wheel.SlipRatio = Utils.ComputeSlipRatio(surfaceSpeed, longitudinalSpeed);

        Vector3 steeringDir = wheel.transform.right;
        float steeringVelocity = Vector3.Dot(steeringDir, pointVelocity);

        float speedForSlipAngle = Mathf.Max(Mathf.Abs(longitudinalSpeed), minSlipSpeed);
        wheel.SlipAngle = Mathf.Atan2(steeringVelocity, speedForSlipAngle) * Mathf.Rad2Deg;

        // Tire load in Kilo Newtons
        // TODO: dynamically comptue it considering suspension state
        float tireLoad = _rb.mass / 4f * 9.81f / 1000f;

        const float minCombinedSlip = 1e-4f;

        float normalizedSlipRatio = wheel.SlipRatio / wheel.tire.maxForceSlipRatio;
        float normalizedSlipAngle = wheel.SlipAngle / wheel.tire.maxForceSlipAngle;
        float combinedSlip = Mathf.Sqrt(normalizedSlipRatio * normalizedSlipRatio +
                                        normalizedSlipAngle * normalizedSlipAngle);

        float equivalentSlipRatio = wheel.tire.maxForceSlipRatio * combinedSlip;
        float equivalentSlipAngle = wheel.tire.maxForceSlipAngle * combinedSlip;

        float inverseMagnitude = 1f / Mathf.Max(combinedSlip, minCombinedSlip);


        float longitudinalForce =
            normalizedSlipRatio * inverseMagnitude * Utils.ComputePacejkaMagicFormula(equivalentSlipRatio,
                tireLoad,
                wheel.tire.GetPacejkaMagicFormulaParams());

        // Debug.Log("normalizedSlipRatio: " + normalizedSlipRatio);
        // Debug.Log("combinedSlip: " + combinedSlip);
        // Debug.Log("equivalentSlipRatio: " + equivalentSlipRatio);

        float lateralForce =
            normalizedSlipAngle * inverseMagnitude * Utils.ComputeLateralPacejkaMagicFormula(
                equivalentSlipAngle, tireLoad, 0f,
                wheel.tire.GetPacejkaLateralMagicFormulaParams());


        float speedForBlend = Mathf.Abs(longitudinalSpeed);
        float lowSpeedFade = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(lowSpeedBlendEnd, lowSpeedBlendStart, speedForBlend));

        lateralForce *= lowSpeedFade;

        Vector3 totalForce = longitudinalForce * wheel.LongitudinalForceDir -
                             lateralForce * wheel.LateralForceDir;

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

        float contactTorque = longitudinalForce * wheel.TireRadius;
        float netTorque = wheelDriveTorque - ComputeBrakeTorque(wheel) -
                          wheelRollingResistance * wheel.AngularVelocity - contactTorque;

        Debug.Log(wheelRollingResistance * wheel.AngularVelocity);
        Debug.Log("contactTorque: " + contactTorque);

        wheel.AngularVelocity += netTorque * dt / Mathf.Max(wheelInertia, 0.001f);
    }

    private float ComputeDriveTorquePerWheel(List<Wheel> drivenWheels, float dt)
    {
        if (drivenWheels.Count == 0) return 0f;

        float maxEngineTorque = engine.GetTorque(_currentEngineRpm);
        float engineTorque = maxEngineTorque * Throttle;

        float gearRatio = Utils.GetTotalGearRatio(_gear, transmission);

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

        if (Mathf.Approximately(wheel.AngularVelocity, 0f)) return 0f;

        // Debug.Log("BrakeTorque: " + brakeTorque);

        return brakeTorque;
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