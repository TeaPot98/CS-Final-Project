using System;
using System.Collections.Generic;
using UnityEngine;

public class DragRaceManager : MonoBehaviour
{
    [SerializeField] private List<MilestoneCollider> milestoneColliders;

    public float eighthTime;
    public float quarterTime;
    public float hundredTime;
    public float twoHundredTime;
    public float hundredToTwoTime;
    public float time;
    public string stateInfo;

    public Car car;
    private Rigidbody _carRb;

    private bool _hasStarted;

    private void Start()
    {
        _carRb = car.GetComponent<Rigidbody>();

        foreach (MilestoneCollider milestone in milestoneColliders) milestone.OnMilestoneReach += OnMilestoneReach;
    }


    private void OnMilestoneReach(DragRaceMilestone milestone)
    {
        switch (milestone)
        {
            case DragRaceMilestone.Start:
                _hasStarted = true;
                break;
            case DragRaceMilestone.Eighth:
                if (!_hasStarted) break;

                eighthTime = time;
                break;
            case DragRaceMilestone.Quarter:
                if (!_hasStarted) break;

                quarterTime = time;
                _hasStarted = false;
                break;
            default: break;
        }
    }

    private void FixedUpdate()
    {
        if (_hasStarted) time += Time.fixedDeltaTime;

        float carSpeed = Utils.FromMetersPerSecondToKmPerHour(_carRb.linearVelocity.magnitude);

        if (Mathf.Approximately(hundredTime, 0f) && carSpeed >= 100) hundredTime = time;

        if (Mathf.Approximately(twoHundredTime, 0f) && carSpeed >= 200)
        {
            twoHundredTime = time;
            hundredToTwoTime = time - hundredTime;
        }
    }
}