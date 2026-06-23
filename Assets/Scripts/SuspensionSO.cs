using UnityEngine;

[CreateAssetMenu(fileName = "Suspension", menuName = "Items/Suspension")]
public class SuspensionSO : ScriptableObject
{
	public float suspensionRestDist = 1f;
	public float suspensionTravel = 0.5f;
	public float springStrength = 7000f;
	public float springDamper = 250f;
}