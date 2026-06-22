using System;
using UnityEngine;


public class TorqueCurveParams
{
	public readonly float CurveBaselineY;
	public readonly float PeakMagnitude;
	public readonly float EndsSlope;
	public readonly float PeakXPosition;
	public readonly float EndsSlope2;

	public TorqueCurveParams(float curveBaselineY, float peakMagnitude, float endsSlope, float peakXPosition,
		float endsSlope2)
	{
		CurveBaselineY = curveBaselineY;
		PeakMagnitude = peakMagnitude;
		EndsSlope = endsSlope;
		PeakXPosition = peakXPosition;
		EndsSlope2 = endsSlope2;
	}
}

public static class Utils
{
	public static float ComputeTorque(float rpm, TorqueCurveParams curveParams)
	{
		return (float)((curveParams.CurveBaselineY - curveParams.PeakMagnitude) *
		               Math.Pow(Math.E,
			               -Math.Pow(curveParams.EndsSlope * (rpm - curveParams.PeakXPosition), 2) /
			               Math.Pow(curveParams.EndsSlope2, 2)) +
		               curveParams.PeakMagnitude);
	}

	public static float ComputeTransmissionTorque(float inputTorque, int gear, TransmissionSO transmission)
	{
		if (gear <= -1) return inputTorque * (1 / transmission.R) * transmission.differentialRatio;
		if (gear == 0) return 0;

		if (gear > transmission.gears.Count)
			return inputTorque * (1 / transmission.gears[-1]) * transmission.differentialRatio;

		return inputTorque * (1 / transmission.gears[gear - 1]) * transmission.differentialRatio;
	}

	public static float ComputeEngineDeltaRpm(float torqueCurveMtp, float torqueCurveOut, float throttleInput,
		float engineBrakingForceMtp,
		float engineFrictionMtp, float expectedRpm, float currentRpm)
	{
		return torqueCurveMtp * torqueCurveOut * throttleInput + engineBrakingForceMtp * (expectedRpm - currentRpm) -
		       engineFrictionMtp * currentRpm;
	}

	public static float ComputeWheelDeltaAngularVelocity(float torqueCurveMtp, float torqueCurveOut,
		float brakingForceMtp, float roadFrictionMtp, float brakeInput, float expectedAngularVelocity,
		float currentAngularVelocity)
	{
		return torqueCurveMtp * torqueCurveOut * (expectedAngularVelocity + currentAngularVelocity) -
		       brakingForceMtp * brakeInput - roadFrictionMtp * currentAngularVelocity;
	}

	public static float RemapToRange(float input, float min, float max, float newMin, float newMax)
	{
		float t = Mathf.InverseLerp(min, max, input);
		return Mathf.Lerp(newMin, newMax, t);
	}
}