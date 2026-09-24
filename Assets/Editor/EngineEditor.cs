using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EngineSO))]
public class EngineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EngineSO asset = (EngineSO)target;

        EditorGUILayout.Space(10);

        float peakPowerRpm = Utils.ComputeFunctionMaximum((x) =>
                Utils.ComputePower(x, Utils.ComputeTorque(x, asset.GetTorqueCurveParams())), asset.idleRpm,
            asset.maxRpm,
            25f);
        float power = Utils
            .ComputePower(peakPowerRpm, Utils.ComputeTorque(peakPowerRpm, asset.GetTorqueCurveParams()));

        EditorGUILayout.LabelField($"Power (kW): " + (power / 1000).ToString("0.##"), EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Horsepower (HP): " + Utils.ComputeHorsePower(power).ToString("0.##"),
            EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Peak Power RPM: " + (int)peakPowerRpm, EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Torque Curve", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));


        ChartUtils.DrawEquationPreview(asset.idleRpm, asset.maxRpm, rect,
            new List<ChartFunction>
            {
                new((x) => Utils.ComputeTorque(x, asset.GetTorqueCurveParams()), Color.cornflowerBlue)
            }
        );

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Power Curve", EditorStyles.boldLabel);

        Rect powerRect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(powerRect, new Color(0.12f, 0.12f, 0.12f));


        ChartUtils.DrawEquationPreview(asset.idleRpm, asset.maxRpm, powerRect,
            new List<ChartFunction>
            {
                new((x) =>
                    Utils.ComputePower(x, Utils.ComputeTorque(x, asset.GetTorqueCurveParams())), Color.salmon)
            });
    }
}