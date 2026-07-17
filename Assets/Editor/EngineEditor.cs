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

        var asset = (EngineSO)target;

        var rect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

        ChartUtils.DrawEquationPreview(asset.idleRpm, asset.maxRpm, rect,
            (x) => Utils.ComputeTorque(x, asset.GetTorqueCurveParams()));
    }
}