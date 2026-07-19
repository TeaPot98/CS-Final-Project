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

public class PacejkaMagicFormulaParams
{
    public readonly float B_0;
    public readonly float B_1;
    public readonly float B_2;
    public readonly float B_3;
    public readonly float B_4;
    public readonly float B_5;
    public readonly float B_6;
    public readonly float B_7;
    public readonly float B_8;
    public readonly float B_9;
    public readonly float B_10;

    public PacejkaMagicFormulaParams(float b_0, float b_1, float b_2, float b_3, float b_4, float b_5, float b_6,
        float b_7, float b_8, float b_9, float b_10)
    {
        B_0 = b_0;
        B_1 = b_1;
        B_2 = b_2;
        B_3 = b_3;
        B_4 = b_4;
        B_5 = b_5;
        B_6 = b_6;
        B_7 = b_7;
        B_8 = b_8;
        B_9 = b_9;
        B_10 = b_10;
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
        if (gear <= -1)
            return inputTorque * transmission.R * transmission.differentialRatio *
                   transmission.transmissionEfficiency;
        if (gear == 0) return 0;

        if (gear > transmission.gears.Count)
            return inputTorque * transmission.gears[^1] * transmission.differentialRatio *
                   transmission.transmissionEfficiency;

        return inputTorque * transmission.gears[gear - 1] * transmission.differentialRatio *
               transmission.transmissionEfficiency;
    }

    public static float GetTotalGearRatio(int gear, TransmissionSO transmission)
    {
        if (gear <= -1) return transmission.R * transmission.differentialRatio;
        if (gear == 0) return 0f;

        if (gear > transmission.gears.Count)
            return transmission.gears[^1] * transmission.differentialRatio;

        return transmission.gears[gear - 1] * transmission.differentialRatio;
    }

    public static float ComputeWheelAngularVelocityAtEngineRpm(float engineRpm, int gear, TransmissionSO transmission)
    {
        float gearRatio = GetTotalGearRatio(gear, transmission);
        if (Mathf.Approximately(gearRatio, 0f)) return 0f;

        return FromRpmToAngularVelocity(engineRpm / gearRatio);
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

    public static float ComputeExpectedRpmAtWheelAngularVelocity(float wheelAngularVelocity,
        TransmissionSO transmission,
        int gear)
    {
        float gearRatio = GetTotalGearRatio(gear, transmission);
        if (Mathf.Approximately(gearRatio, 0f)) return 0f;

        return FromAngularVelocityToRpm(wheelAngularVelocity) * gearRatio;
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

    public static float ComputeLongitudinalTireForce(float slipRatio, float a = 9.625f, float b = 31.0f,
        float p = 2.375f)
    {
        // Brian Beckman's Magic Trick
        return b * slipRatio / (1 + Mathf.Pow(Mathf.Abs(a * slipRatio), p));
    }

    public static float ComputeLateralTireForce(float slipAngle, float a = 9.625f, float b = 31.0f, float p = 2.375f)
    {
        // Brian Beckman's Magic Trick
        return b * slipAngle / (1 + Mathf.Pow(Mathf.Abs(a * slipAngle), p));
    }

    public static float ComputePacejkaMagicFormula(float slipRatio, float tireLoad, PacejkaMagicFormulaParams p)
    {
        float mu_p = p.B_1 * tireLoad + p.B_2;
        float C = p.B_0;

        float B = (p.B_3 * tireLoad + p.B_4) * Mathf.Exp(-p.B_5 * tireLoad) / (C * mu_p);
        float D = mu_p * tireLoad;
        float E = p.B_6 * Mathf.Pow(tireLoad, 2) + p.B_7 * tireLoad + p.B_8;

        float S = 100 * slipRatio + p.B_9 * tireLoad + p.B_10;

        return D * Mathf.Sin(C * Mathf.Atan(B * S + E * (Mathf.Atan(B * S) - B * S)));
    }
}