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

    private Car _car;
    private SuspensionSO _suspension;

    public bool IsGrounded { get; private set; }
    public Vector3 LongitudinalForceDir { get; private set; }
    public Vector3 LateralForceDir { get; private set; }
    public Vector3 ContactPoint { get; private set; }

    private Rigidbody _carRigidBody;

    private Transform _carTransform;

    public float TireRadius;
    private Transform _tireTransform;
    private Vector3 _tireTransformPosition;
    private Renderer _tireMeshRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _carRigidBody = carObject.GetComponent<Rigidbody>();
        _carTransform = carObject.GetComponent<Transform>();
        _car = carObject.GetComponent<Car>();
        _suspension = _car.suspension;

        _tireMeshRenderer = tireMesh.GetComponent<Renderer>();

        _tireTransform = GetComponent<Transform>();
        _tireTransformPosition = _tireTransform.position;

        TireRadius = 0.5f * _tireMeshRenderer.bounds.size.y;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!Mathf.Approximately(AngularVelocity, 0f))
        {
            Quaternion deltaRotation =
                Quaternion.AngleAxis(AngularVelocity * Mathf.Rad2Deg, wheelModel.transform.right);

            wheelModel.transform.rotation = deltaRotation * wheelModel.transform.rotation;
        }
    }

    public void Simulate()
    {
        Vector3 tireWorldVel = _carRigidBody.GetPointVelocity(_tireTransformPosition);
        _tireTransformPosition = _tireTransform.position;

        Ray ray = new(_tireTransformPosition, -_tireTransform.up);
        RaycastHit tireRay;
        bool rayDidHit =
            Physics.Raycast(ray, out tireRay, _suspension.suspensionRestDist + _suspension.suspensionTravel);

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
            float force = offset * _suspension.springStrength - vel * _suspension.springDamper;

            // apply the force at the location of this tire
            // in the direction of the suspension
            _carRigidBody.AddForceAtPosition(springDir * force, _tireTransformPosition);

            LongitudinalForceDir = Vector3.ProjectOnPlane(_tireTransform.forward, tireRay.normal).normalized;
            LateralForceDir = Vector3.ProjectOnPlane(_tireTransform.right, tireRay.normal).normalized;
            ContactPoint = tireRay.point;

            Debug.DrawRay(_tireTransformPosition, springDir * force, Color.dodgerBlue);
        }

        if (rayDidHit)
            wheelModel.transform.localPosition =
                new Vector3(0f, -tireRay.distance + TireRadius, 0f);
        else

            // Reset the wheel mesh position to the suspension rest distance
            wheelModel.transform.localPosition =
                new Vector3(0f, -_suspension.suspensionRestDist + TireRadius, 0f);
    }
}