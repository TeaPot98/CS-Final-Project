using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TireSO))]
public class TireEditor : Editor
{
    private const int TickCount = 5;

    private const int SamplesCount = 60;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var asset = (TireSO)target;

        var rect = GUILayoutUtility.GetRect(200, 120);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

        // TODO: Implement dynamic tire load
        ChartUtils.DrawEquationPreview(-0.5f, 0.5f, rect,
            (x) => Utils.ComputePacejkaMagicFormula(x, 5f, asset.GetPacejkaMagicFormulaParams()));
    }
}