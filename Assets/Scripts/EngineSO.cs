using System;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Engine", menuName = "Items/Powertrain/Engine")]
public class EngineSO : ScriptableObject
{
    public float maxRpm = 7000f;
    public float idleRpm = 1000f;

    public float curveBaselineY = 100f;
    public float peakMagnitude = 700f;
    public float endsSlope = 1.2f;
    public float peakXPosition = 6000f;
    public float endsSlope2 = 4000f;
    [Space(10)] public float power;
    public float horsepower;

    private TorqueCurveParams _curveParams;

    public TorqueCurveParams GetTorqueCurveParams()
    {
        _curveParams ??= new TorqueCurveParams(curveBaselineY, peakMagnitude, endsSlope, peakXPosition, endsSlope2);
        return _curveParams;
    }


    public float GetTorque(float rpm)
    {
        return Utils.ComputeTorque(rpm,
            GetTorqueCurveParams());
    }

    // Update Editor's chart on params change
    private void OnValidate()
    {
        _curveParams = null;
    }
}