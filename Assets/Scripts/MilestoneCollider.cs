using System;
using UnityEngine;

public enum DragRaceMilestone
{
    Start,
    Eighth,
    Quarter
}

public class MilestoneCollider : MonoBehaviour
{
    public DragRaceMilestone identifier;
    public event Action<DragRaceMilestone> OnMilestoneReach;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerCar")) OnMilestoneReach.Invoke(identifier);
    }
}