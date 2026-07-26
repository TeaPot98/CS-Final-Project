using UnityEngine;

[CreateAssetMenu(fileName = "Tire", menuName = "Items/Tire")]
public class TireSO : ScriptableObject
{
    [Header("Longitudinal Parameters")] public float b_0 = 1.65f;
    public float b_1;
    public float b_2 = 1688f;
    public float b_3;
    public float b_4 = 229f;
    public float b_5;
    public float b_6;
    public float b_7;
    public float b_8 = -10f;
    public float b_9;
    public float b_10;

    [Space(10)] [Header("Lateral Parameters")]
    public float a_0 = 1.799f;

    public float a_1;
    public float a_2 = 1688f;
    public float a_3 = 4140f;
    public float a_4 = 6.026f;
    public float a_5;
    public float a_6 = -0.3589f;
    public float a_7 = 1;
    public float a_8 = -10f;
    public float a_9 = -6.111f / 1000f;
    public float a_10 = -3.224f / 100f;
    public float a_11;
    public float a_12;
    public float a_13;
    public float a_14;


    private PacejkaMagicFormulaParams _magicFormulaParams;
    private PacejkaLateralMagicFormulaParams _lateralMagicFormulaParams;

    public PacejkaMagicFormulaParams GetPacejkaMagicFormulaParams()
    {
        _magicFormulaParams ??= new PacejkaMagicFormulaParams(b_0, b_1, b_2, b_3, b_4, b_5, b_6, b_7, b_8, b_9, b_10);
        return _magicFormulaParams;
    }

    public PacejkaLateralMagicFormulaParams GetPacejkaLateralMagicFormulaParams()
    {
        _lateralMagicFormulaParams ??= new PacejkaLateralMagicFormulaParams(a_0, a_1, a_2, a_3, a_4, a_5, a_6, a_7, a_8,
            a_9, a_10, a_11, a_12, a_13, a_14);
        return _lateralMagicFormulaParams;
    }

    // Update Editor's chart on params change
    private void OnValidate()
    {
        _magicFormulaParams = null;
        _lateralMagicFormulaParams = null;
    }
}