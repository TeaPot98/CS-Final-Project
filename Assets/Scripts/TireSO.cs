using UnityEngine;

[CreateAssetMenu(fileName = "Tire", menuName = "Items/Tire")]
public class TireSO : ScriptableObject
{
    public float b_0 = 1.65f;
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


    private PacejkaMagicFormulaParams _magicFormulaParams;

    public PacejkaMagicFormulaParams GetPacejkaMagicFormulaParams()
    {
        _magicFormulaParams ??= new PacejkaMagicFormulaParams(b_0, b_1, b_2, b_3, b_4, b_5, b_6, b_7, b_8, b_9, b_10);
        return _magicFormulaParams;
    }

    // Update Editor's chart on params change
    private void OnValidate()
    {
        _magicFormulaParams = null;
    }
}