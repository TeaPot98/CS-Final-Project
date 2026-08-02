using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EngineSO))]
public class EngineEditor : Editor
{
    private const int TickCount = 5;

    private const int SamplesCount = 60;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EngineSO asset = (EngineSO)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Torque Curve", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));


        ChartUtils.DrawEquationPreview(asset.idleRpm, asset.maxRpm, rect,
            (x) => Utils.ComputeTorque(x, asset.GetTorqueCurveParams()));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Power Curve", EditorStyles.boldLabel);

        Rect powerRect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(powerRect, new Color(0.12f, 0.12f, 0.12f));


        float peakPowerRpm = ChartUtils.DrawEquationPreview(asset.idleRpm, asset.maxRpm, powerRect,
            (x) => Utils.ComputePower(x, Utils.ComputeTorque(x, asset.GetTorqueCurveParams())));

        asset.power = Utils.ComputePower(peakPowerRpm, Utils.ComputeTorque(peakPowerRpm, asset.GetTorqueCurveParams()));
        asset.horsepower = Utils.ComputeHorsePower(asset.power);
        asset.peakPowerRpm = (int)peakPowerRpm;
    }
}