using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TireSO))]
public class TireEditor : Editor
{
    private const int TickCount = 5;

    private const int SamplesCount = 60;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty prop = serializedObject.GetIterator();

        prop.NextVisible(true); // skip script field
        while (prop.NextVisible(false))
        {
            EditorGUILayout.PropertyField(prop, true);
            if (prop.name == "b_10")
            {
                TireSO asset = (TireSO)target;

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Max Force Slip Ratio: " + Utils.ComputeFunctionMaximum((x) =>
                        Utils.ComputePacejkaMagicFormula(x, asset.referenceTireLoad,
                            asset.GetPacejkaMagicFormulaParams()), -0.5f, 0.5f, 0.005f).ToString("0.###"),
                    EditorStyles.boldLabel);

                EditorGUILayout.Space(10);

                Rect rect = GUILayoutUtility.GetRect(200, 120);
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

                // TODO: Implement dynamic tire load
                ChartUtils.DrawEquationPreview(-0.5f, 0.5f, rect, new List<ChartFunction>
                {
                    new((x) => Utils.ComputePacejkaMagicFormula(x, asset.referenceTireLoad,
                        asset.GetPacejkaMagicFormulaParams()), Color.cyan)
                });
            }

            if (prop.name == "a_14")
            {
                TireSO asset = (TireSO)target;

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Max Force Slip Angle: " + Utils.ComputeFunctionMaximum((x) =>
                        Utils.ComputeLateralPacejkaMagicFormula(x, asset.referenceTireLoad, 0f,
                            asset.GetPacejkaLateralMagicFormulaParams()), -90f, 90f, 0.25f).ToString("0.###"),
                    EditorStyles.boldLabel);

                EditorGUILayout.Space(10);

                Rect rect = GUILayoutUtility.GetRect(200, 120);
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

                // TODO: Implement dynamic tire load
                ChartUtils.DrawEquationPreview(-90f, 90f, rect, new List<ChartFunction>
                    {
                        new((x) => Utils.ComputeLateralPacejkaMagicFormula(x, asset.referenceTireLoad, 0f,
                            asset.GetPacejkaLateralMagicFormulaParams()), Color.cyan)
                    }
                );
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}