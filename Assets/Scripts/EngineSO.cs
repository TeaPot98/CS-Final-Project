using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Engine", menuName = "Items/Powertrain/Engine")]
public class EngineSO : ScriptableObject
{
	public float Power;
	public float maxRpm = 7000f;

	public float curveBaselineY = 100f;
	public float peakMagnitude = 700f;
	public float endsSlope = 1.2f;
	public float peakXPosition = 6000f;
	public float endsSlope2 = 4000f;

	private TorqueCurveParams _curveParams;

	public TorqueCurveParams GetTorqueCurveParams()
	{
		_curveParams ??= new TorqueCurveParams(curveBaselineY, peakMagnitude, endsSlope, peakXPosition, endsSlope2);

		return _curveParams;
	}

	private float _rpm = 1000f;

	public float GetTorque(float throttle)
	{
		_rpm = Utils.RemapToRange(throttle, 0f, 1f, 1000f, maxRpm);
		return Utils.ComputeTorque(_rpm,
			GetTorqueCurveParams());
	}

	public float GetRpm()
	{
		return _rpm;
	}
}