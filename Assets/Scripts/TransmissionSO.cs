using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Engine", menuName = "Items/Powertrain/Transmission")]
public class TransmissionSO : ScriptableObject
{
	public float transmissionEfficiency = 0.7f;
	public float differentialRatio = 0.41f;

	public float R = -3.2f;
	public float N = 0f;
	public List<float> gears = new() { 2.5f, 1.61f, 1.1f, 0.81f, 0.68f };
}