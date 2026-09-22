using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Car))]
public class CarEditor : Editor
{
    private const int TickCount = 5;
    private const int SamplesCount = 60;

    private Dictionary<int, Color> gearsColors = new()
    {
        [1] = Color.forestGreen,
        [2] = Color.limeGreen,
        [3] = Color.greenYellow,
        [4] = Color.darkOrange,
        [5] = Color.indianRed,
        [6] = Color.crimson
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Car asset = (Car)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Gears RPM/Speed", EditorStyles.boldLabel);

        Rect gearsRpmRect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(gearsRpmRect, new Color(0.12f, 0.12f, 0.12f));

        float wheelRadius = asset.wheelFL.TireRadius;
        if (wheelRadius <= 0f && asset.wheelFL.tireMesh != null &&
            asset.wheelFL.tireMesh.TryGetComponent(out Renderer tireRenderer))
            wheelRadius = 0.5f * tireRenderer.bounds.size.y;

        if (wheelRadius <= 0f) return;

        float minSpeed = Utils.GetCarMinSpeed(asset.engine, asset.transmission, wheelRadius);
        float maxSpeed = Utils.GetCarMaxSpeed(asset.engine, asset.transmission, wheelRadius);

        asset.topSpeed = Utils.FromMetersPerSecondToKmPerHour(maxSpeed);

        List<(float, float)> gearsSpeedIntervals =
            Utils.GetGearsSpeedIntervals(asset.engine, asset.transmission, wheelRadius);

        List<ChartFunction> rpmChartFunctions = new();

        for (int i = 0; i < gearsSpeedIntervals.Count; i++)
        {
            int gear = i + 1;
            (float min, float max) interval = gearsSpeedIntervals[i];

            rpmChartFunctions.Add(new ChartFunction(
                (x) => x < interval.min || x > interval.max
                    ? null
                    : Utils.ComputeExpectedRpmAtWheelRpm(Utils.ComputeWheelRpmAtCarSpeed(x, wheelRadius),
                        asset.transmission, gear), gearsColors.GetValueOrDefault(gear, Color.dodgerBlue)));
        }

        ChartUtils.DrawEquationPreview(minSpeed, maxSpeed, gearsRpmRect,
            rpmChartFunctions, (x) => Utils.FromMetersPerSecondToKmPerHour(x).ToString("0. km/h")
        );

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Gears Torque/Speed", EditorStyles.boldLabel);

        Rect gearsTorqueRect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(gearsTorqueRect, new Color(0.12f, 0.12f, 0.12f));

        List<(float, float)> overlappingGearSpeedIntervals =
            Utils.GetGearsSpeedIntervals(asset.engine, asset.transmission, wheelRadius, true);

        List<ChartFunction> torqueChartFunctions = new();

        for (int i = 0; i < overlappingGearSpeedIntervals.Count; i++)
        {
            int gear = i + 1;
            (float min, float max) interval = overlappingGearSpeedIntervals[i];

            torqueChartFunctions.Add(new ChartFunction(
                (x) => x < interval.min || x > interval.max
                    ? null
                    : Utils.ComputeTransmissionTorque(Utils.ComputeTorque(Utils.ComputeExpectedRpmAtWheelRpm(
                        Utils.ComputeWheelRpmAtCarSpeed(x, wheelRadius),
                        asset.transmission, gear), asset.engine.GetTorqueCurveParams()), gear, asset.transmission),
                gearsColors.GetValueOrDefault(gear, Color.dodgerBlue)));
        }

        ChartUtils.DrawEquationPreview(minSpeed, maxSpeed, gearsTorqueRect,
            torqueChartFunctions, (x) => Utils.FromMetersPerSecondToKmPerHour(x).ToString("0. km/h")
        );
    }
}