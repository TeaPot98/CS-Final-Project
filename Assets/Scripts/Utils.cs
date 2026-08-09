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


public class PacejkaLateralMagicFormulaParams
{
    public readonly float A_0;
    public readonly float A_1;
    public readonly float A_2;
    public readonly float A_3;
    public readonly float A_4;
    public readonly float A_5;
    public readonly float A_6;
    public readonly float A_7;
    public readonly float A_8;
    public readonly float A_9;
    public readonly float A_10;
    public readonly float A_11;
    public readonly float A_12;
    public readonly float A_13;
    public readonly float A_14;

    public PacejkaLateralMagicFormulaParams(float a_0, float a_1, float a_2, float a_3, float a_4, float a_5, float a_6,
        float a_7, float a_8, float a_9, float a_10, float a_11, float a_12, float a_13, float a_14)
    {
        A_0 = a_0;
        A_1 = a_1;
        A_2 = a_2;
        A_3 = a_3;
        A_4 = a_4;
        A_5 = a_5;
        A_6 = a_6;
        A_7 = a_7;
        A_8 = a_8;
        A_9 = a_9;
        A_10 = a_10;
        A_11 = a_11;
        A_12 = a_12;
        A_13 = a_13;
        A_14 = a_14;
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

    public static float ComputePower(float rpm, float torque)
    {
        return torque * FromRpmToAngularVelocity(rpm);
    }

    public static float ComputeHorsePower(float power)
    {
        return power / 735.5f;
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

    public static float ComputeWheelDeltaAngularVelocity(float torqueCurveMtp, float torqueCurveOut, float throttle,
        float brakingForceMtp, float roadFrictionMtp, float brakeInput, float expectedAngularVelocity,
        float currentAngularVelocity)
    {
        return torqueCurveMtp * torqueCurveOut * throttle * (expectedAngularVelocity - currentAngularVelocity) -
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

    public static float ComputeSlipRatio(float surfaceSpeed, float groundSpeed, float minSlipSpeed = 0.5f)
    {
        if (Mathf.Abs(surfaceSpeed) < 0.02f &&
            Mathf.Abs(groundSpeed) < 0.02f)
            return 0f;

        float referenceSpeed = Mathf.Max(
            Mathf.Max(Mathf.Abs(surfaceSpeed), Mathf.Abs(groundSpeed)),
            minSlipSpeed);

        return Mathf.Clamp(
            (surfaceSpeed - groundSpeed) / referenceSpeed,
            -1f,
            1f);
    }


    public static float FromAngularVelocityToRpm(float angularVelocity)
    {
        return angularVelocity * 60f / (2f * (float)Math.PI);
    }

    public static float FromRpmToAngularVelocity(float rpm)
    {
        return rpm * 2f * (float)Math.PI / 60f;
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

    public static float ComputeLateralPacejkaMagicFormula(float slipAngle, float tireLoad, float camberAngle,
        PacejkaLateralMagicFormulaParams p)
    {
        float mu_yp = p.A_1 * tireLoad + p.A_2;

        float D = mu_yp * tireLoad;
        float B = p.A_3 * Mathf.Sin(2 * Mathf.Atan(tireLoad / p.A_4)) * (1 - p.A_5 * Mathf.Abs(camberAngle)) /
                  (p.A_0 * D);
        float E = p.A_6 * tireLoad + p.A_7;

        float S = slipAngle + p.A_8 * camberAngle + p.A_9 * tireLoad + p.A_10;
        float S_v = tireLoad * ((p.A_11 * tireLoad + p.A_12) * camberAngle + p.A_13) + p.A_14;

        return D * Mathf.Sin(p.A_0 * Mathf.Atan(B * S + E * (Mathf.Atan(B * S) - B * S))) + S_v;
    }

    public static float FromMetersPerSecondToKmPerHour(float speed)
    {
        return speed * 3.6f;
    }
}