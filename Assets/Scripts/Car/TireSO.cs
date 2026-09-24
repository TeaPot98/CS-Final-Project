using UnityEngine;

[CreateAssetMenu(fileName = "Tire", menuName = "Items/Tire")]
public class TireSO : ScriptableObject
{
    public float referenceTireLoad = 3.3f;

    [Header("Longitudinal Parameters")] [Range(-3.5f, 3.5f)]
    public float b_0 = 1.65f;

    [Range(-1000f, 1000f)] public float b_1;
    [Range(-2200f, 2200f)] public float b_2 = 1688f;
    [Range(-50f, 50f)] public float b_3;
    [Range(-0f, 500f)] public float b_4 = 229f;
    public float b_5;
    [Range(-5f, 0f)] public float b_6;
    [Range(-3f, 1.5f)] public float b_7;
    public float b_8 = -10f;
    public float b_9;
    public float b_10;

    [Space(10)] [Header("Lateral Parameters")] [Range(-3.5f, 3.5f)]
    public float a_0 = 1.799f;

    [Range(-1000f, 1000f)] public float a_1;
    [Range(-2200f, 2200f)] public float a_2 = 1688f;
    [Range(0f, 6000f)] public float a_3 = 4140f;
    [Range(-0f, 100f)] public float a_4 = 6.026f;
    public float a_5;
    [Range(-5f, 0f)] public float a_6 = -0.3589f;
    [Range(0f, 3f)] public float a_7 = 1;
    public float a_8;
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