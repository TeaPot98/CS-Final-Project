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

	public static float RemapToRange(float input, float min, float max, float newMin, float newMax)
	{
		float t = Mathf.InverseLerp(min, max, input);
		return Mathf.Lerp(newMin, newMax, t);
	}
}