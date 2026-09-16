using System;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public bool isDriving;
    public bool canSteer;

    public float AngularVelocity { get; set; }
    public float SlipAngle { get; set; }
    public float SlipRatio { get; set; }

    public GameObject wheelModel;
    public GameObject tireMesh;
    public GameObject carObject;
    public TireSO tire;

    public GameObject particleEmitter;
    private ParticleSystem _smokeRenderer;
    private TrailRenderer _skidmarkRenderer;

    public float slipAngleSkidmarkThreshold = 0.5f;
    public float slipRatioSkidmarkThreshold = 0.35f;

    [SerializeField] private LayerMask layerMask;

    private Car _car;
    private SuspensionSO _suspension;

    public bool IsGrounded { get; private set; }
    public Vector3 LongitudinalForceDir { get; private set; }
    public Vector3 LateralForceDir { get; private set; }
    public Vector3 ContactPoint { get; private set; }
    public float NormalLoad { get; private set; }

    private Rigidbody _carRigidBody;

    public float TireRadius { get; private set; }
    private Transform _tireTransform;
    private Vector3 _tireTransformPosition;
    private Renderer _tireMeshRenderer;

    private float _steeringWheelTurningSpeed = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _carRigidBody = carObject.GetComponent<Rigidbody>();
        _car = carObject.GetComponent<Car>();
        _suspension = _car.suspension;

        _tireMeshRenderer = tireMesh.GetComponent<Renderer>();

        _tireTransform = GetComponent<Transform>();
        _tireTransformPosition = _tireTransform.position;

        _skidmarkRenderer = particleEmitter.GetComponent<TrailRenderer>();
        _smokeRenderer = particleEmitter.GetComponent<ParticleSystem>();

        TireRadius = 0.5f * _tireMeshRenderer.bounds.size.y;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!Mathf.Approximately(AngularVelocity, 0f))
        {
            Quaternion deltaRotation =
                Quaternion.AngleAxis(AngularVelocity * Mathf.Rad2Deg * Time.deltaTime, wheelModel.transform.right);

            wheelModel.transform.rotation = deltaRotation * wheelModel.transform.rotation;
        }

        RenderSkidmark();
    }

    public void HandleSteeringRotation(float steeringValue, float maxSteeringAngle)
    {
        if (!canSteer) return;


        float targetAngle = Mathf.Lerp(
            -maxSteeringAngle,
            maxSteeringAngle,
            (steeringValue + 1f) * 0.5f
        );

        float currentAngle = transform.localEulerAngles.y;

        if (currentAngle > 180f) currentAngle -= 360f;


        float smoothAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref _steeringWheelTurningSpeed,
            0.12f
        );

        transform.localRotation = Quaternion.Euler(0.0f, smoothAngle, 0.0f);
    }

    public void SimulateContactAndSuspension()
    {
        _tireTransformPosition = _tireTransform.position;
        Vector3 tireWorldVel = _carRigidBody.GetPointVelocity(_tireTransformPosition);

        Ray ray = new(_tireTransformPosition, -_tireTransform.up);
        RaycastHit tireRay;
        bool rayDidHit =
            Physics.Raycast(ray, out tireRay, _suspension.suspensionRestDist + _suspension.suspensionTravel, layerMask,
                QueryTriggerInteraction.Ignore);

        IsGrounded = rayDidHit;

        // ------ Spring force-------
        if (rayDidHit)
        {
            // world-space direction of the spring force
            Vector3 springDir = _tireTransform.up;

            // calculate offset from the raycast
            float offset = _suspension.suspensionRestDist - tireRay.distance;

            // calculate velocity along the spring direction
            // note that springDir is a unit vector, so this returns the magnitude of tireWorldVel
            // as projected onto springDir
            float vel = Vector3.Dot(springDir, tireWorldVel);

            // calculate the magnitude of the dampened spring force
            float forceMagnitude = offset * _suspension.springStrength - vel * _suspension.springDamper;
            forceMagnitude = Mathf.Clamp(forceMagnitude, 0f, Mathf.Abs(forceMagnitude));

            Vector3 force = springDir * forceMagnitude;

            NormalLoad = Mathf.Max(0f, Vector3.Dot(force, tireRay.normal));

            // apply the force at the location of this tire
            // in the direction of the suspension
            _carRigidBody.AddForceAtPosition(force, _tireTransformPosition);

            LongitudinalForceDir = Vector3.ProjectOnPlane(_tireTransform.forward, tireRay.normal).normalized;
            LateralForceDir = Vector3.ProjectOnPlane(_tireTransform.right, tireRay.normal).normalized;
            ContactPoint = tireRay.point;

            Debug.DrawRay(_tireTransformPosition, force / 1000f, Color.dodgerBlue);
        }

        if (rayDidHit)
            wheelModel.transform.localPosition =
                new Vector3(0f, -tireRay.distance + TireRadius, 0f);
        else

            // Reset the wheel mesh position to the suspension rest distance
            wheelModel.transform.localPosition =
                new Vector3(0f, -_suspension.suspensionRestDist + TireRadius, 0f);
    }

    private void RenderSkidmark()
    {
        if (SlipAngle >= slipAngleSkidmarkThreshold || SlipRatio >= slipRatioSkidmarkThreshold)
        {
            if (!_skidmarkRenderer.emitting) _skidmarkRenderer.emitting = true;
            if (!_smokeRenderer.isPlaying) _smokeRenderer.Play();
        }
        else
        {
            if (_skidmarkRenderer.emitting) _skidmarkRenderer.emitting = false;
            if (_smokeRenderer.isPlaying) _smokeRenderer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}