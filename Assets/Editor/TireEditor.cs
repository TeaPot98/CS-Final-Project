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

                Rect rect = GUILayoutUtility.GetRect(200, 120);
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

                // TODO: Implement dynamic tire load
                ChartUtils.DrawEquationPreview(-0.5f, 0.5f, rect,
                    (x) => Utils.ComputePacejkaMagicFormula(x, asset.referenceTireLoad,
                        asset.GetPacejkaMagicFormulaParams()));
            }

            if (prop.name == "a_14")
            {
                TireSO asset = (TireSO)target;

                Rect rect = GUILayoutUtility.GetRect(200, 120);
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

                // TODO: Implement dynamic tire load
                float peakX = ChartUtils.DrawEquationPreview(-20f, 20f, rect,
                    (x) => Utils.ComputeLateralPacejkaMagicFormula(x, asset.referenceTireLoad, 0f,
                        asset.GetPacejkaLateralMagicFormulaParams()));

                asset.maxSlipAngle = peakX;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}