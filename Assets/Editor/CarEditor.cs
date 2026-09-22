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

        float wheelRadius = asset.wheelFL.TireRadius;
        if (wheelRadius <= 0f && asset.wheelFL.tireMesh != null &&
            asset.wheelFL.tireMesh.TryGetComponent(out Renderer tireRenderer))
            wheelRadius = 0.5f * tireRenderer.bounds.size.y;

        if (wheelRadius <= 0f) return;

        float minSpeed = Utils.GetCarMinSpeed(asset.engine, asset.transmission, wheelRadius);
        float maxSpeed = Utils.GetCarMaxSpeed(asset.engine, asset.transmission, wheelRadius);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(
            $"Car's top speed (w/o road loads): {Utils.FromMetersPerSecondToKmPerHour(maxSpeed).ToString("0. km/h")}",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Engine RPM by vehicle speed and gear", EditorStyles.boldLabel);

        Rect gearsRpmRect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(gearsRpmRect, new Color(0.12f, 0.12f, 0.12f));

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
        EditorGUILayout.LabelField("Output torque by vehicle gear and speed", EditorStyles.boldLabel);

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

        EditorGUILayout.Space(10);

        int gearCount = asset.transmission.gears.Count;
        const float rowHeight = 24f;
        const float columnWidth = 100f;
        const float padding = 8f;
        const float swatchSize = 16f;

        int rows = (gearCount + 1) / 3;

        Rect gearsLegendRect = GUILayoutUtility.GetRect(
            200f,
            padding * 2f + rows * rowHeight);

        EditorGUI.DrawRect(
            gearsLegendRect,
            new Color(0.12f, 0.12f, 0.12f));

        for (int gear = 1; gear <= gearCount; gear++)
        {
            int index = gear - 1;
            int column = index % 4;
            int row = index / 4;

            float x = gearsLegendRect.x + padding + column * columnWidth;
            float y = gearsLegendRect.y + padding + row * rowHeight;

            Rect colorRect = new(x, y + 2f, swatchSize, swatchSize);
            Rect labelRect = new(
                x + swatchSize + 6f,
                y,
                columnWidth - swatchSize - 6f,
                rowHeight);

            EditorGUI.DrawRect(colorRect, gearsColors[gear]);
            EditorGUI.LabelField(labelRect, $"Gear {gear}");
        }
    }
}