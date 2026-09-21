using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Car))]
public class CarEditor : Editor
{
    private const int TickCount = 5;

    private const int SamplesCount = 60;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Car asset = (Car)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Gear Ratios", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

        float wheelRadius = asset.wheelFL.TireRadius;

        float minSpeed = Utils.GetCarMinSpeed(asset.engine, asset.transmission, wheelRadius);
        float maxSpeed = Utils.GetCarMaxSpeed(asset.engine, asset.transmission, wheelRadius);

        Debug.Log("MinSpeed: " + minSpeed);
        Debug.Log("MaxSpeed: " + maxSpeed);

        List<(float, float)> gearsSpeedIntervals =
            Utils.GetGearsSpeedIntervals(asset.engine, asset.transmission, wheelRadius);

        List<ChartFunction> chartFunctions = new();

        for (int i = 0; i < gearsSpeedIntervals.Count; i++)
        {
            int idx = i;
            chartFunctions.Add(new ChartFunction(
                (x) => x < gearsSpeedIntervals[idx].Item1 || x > gearsSpeedIntervals[idx].Item2
                    ? null
                    : Utils.ComputeExpectedRpmAtWheelRpm(Utils.ComputeWheelRpmAtCarSpeed(x, wheelRadius),
                        asset.transmission, idx), Color.goldenRod));
        }

        ChartUtils.DrawEquationPreview(minSpeed, maxSpeed, rect,
            chartFunctions
        );
    }
}