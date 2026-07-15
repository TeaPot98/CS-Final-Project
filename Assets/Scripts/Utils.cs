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
		if (gear <= -1) return inputTorque * transmission.R * transmission.differentialRatio;
		if (gear == 0) return 0;

		if (gear > transmission.gears.Count)
			return inputTorque * transmission.gears[^1] * transmission.differentialRatio;

		return inputTorque * transmission.gears[gear - 1] * transmission.differentialRatio;
	}

	public static float ComputeWheelAngularVelocityAtEngineRpm(float engineRpm, int gear, TransmissionSO transmission)
	{
		if (gear <= -1) return engineRpm / (transmission.R * transmission.differentialRatio);
		if (gear == 0) return 0.1f;

		if (gear > transmission.gears.Count)
			return engineRpm / (transmission.gears[^1] * transmission.differentialRatio);

		return engineRpm / (transmission.gears[gear - 1] * transmission.differentialRatio);
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

	public static float ComputeExpectedRpmAtWheelAngularVelocity(float wheelAngularVelocity, TransmissionSO transmission,
		int gear)
	{
		float wheelRpm = FromAngularVelocityToRpm(wheelAngularVelocity);

		if (gear <= -1) return wheelRpm * transmission.R * transmission.differentialRatio;
		if (gear == 0) return 0.1f;

		if (gear > transmission.gears.Count)
			return wheelRpm * transmission.gears[^1] * transmission.differentialRatio;

		return wheelRpm * transmission.gears[gear - 1] * transmission.differentialRatio;
	}

	public static float RemapToRange(float input, float min, float max, float newMin, float newMax)
	{
		float t = Mathf.InverseLerp(min, max, input);
		return Mathf.Lerp(newMin, newMax, t);
	}

	public static float FromAngularVelocityToRpm(float angularVelocity)
	{
		return angularVelocity * 60f / (2f * (float)Math.PI);
	}

	public static float FromRpmToAngularVelocity(float rpm)
	{
		return rpm * 2f * (float)Math.PI / 60f;
	}

	public static float ComputeLongitudinalTireForce(float slipRatio, float a = 9.625f, float b = 31.0f, float p = 2.375f)
	{
		// Brian Beckman's Magic Trick
		return (b * slipRatio) / (1 + Mathf.Pow(Mathf.Abs(a * slipRatio), p));
	}
	
	public static float ComputeLateralTireForce(float slipAngle, float a = 9.625f, float b = 31.0f, float p = 2.375f)
	{
		// Brian Beckman's Magic Trick
		return (b * slipAngle) / (1 + Mathf.Pow(Mathf.Abs(a * slipAngle), p));
	}
}