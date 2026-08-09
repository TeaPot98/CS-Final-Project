using System;
using System.Collections.Generic;
using UnityEngine;

public class DragRaceManager : MonoBehaviour
{
    [SerializeField] private List<MilestoneCollider> milestoneColliders;

    public string eighthTime = "1/8 Miles: N/A";
    public string quarterTime = "1/4 Miles: N/A";
    public string hundredTimeLabel = "0-100km/h: N/A";
    public string twoHundredTime = "0-200km/h: N/A";
    public string hundredToTwoTime = "100-100km/h: N/A";
    public string timeLabel = "00.000s";
    public string stateInfo;

    public Car car;
    private Rigidbody _carRb;
    private float _time;
    private float _hundredTime;
    private float _twoHundredTime;

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
                ResetRun();
                break;
            case DragRaceMilestone.Eighth:
                if (!_hasStarted) break;

                eighthTime = "1/8 Miles: " + $"{_time:F3}" + "s";
                break;
            case DragRaceMilestone.Quarter:
                if (!_hasStarted) break;

                quarterTime = "1/4 Miles: " + $"{_time:F3}" + "s";
                _hasStarted = false;
                break;
            default: break;
        }
    }

    private void FixedUpdate()
    {
        if (_hasStarted)
        {
            _time += Time.fixedDeltaTime;
            timeLabel = _time + "s";
        }

        float carSpeed = Utils.FromMetersPerSecondToKmPerHour(_carRb.linearVelocity.magnitude);

        if (Mathf.Approximately(_hundredTime, 0f) && carSpeed >= 100)
        {
            _hundredTime = _time;
            hundredTimeLabel = "0-100km/h: " + $"{_time:F3}" + "s";
        }

        if (Mathf.Approximately(_twoHundredTime, 0f) && carSpeed >= 200)
        {
            _twoHundredTime = _time;
            twoHundredTime = "0-200km/h: " + $"{_time:F3}" + "s";
            hundredToTwoTime = "100-200km/h: " + $"{_time - _hundredTime:F3}" + "s";
        }
    }

    private void OnDestroy()
    {
        foreach (MilestoneCollider milestone in milestoneColliders) milestone.OnMilestoneReach -= OnMilestoneReach;
    }

    private void ResetRun()
    {
        eighthTime = "1/8 Miles: N/A";
        quarterTime = "1/4 Miles: N/A";
        hundredTimeLabel = "0-100km/h: N/A";
        twoHundredTime = "0-200km/h: N/A";
        hundredToTwoTime = "100-100km/h: N/A";
        timeLabel = "00.000s";

        _time = 0;
        _hundredTime = 0;
        _twoHundredTime = 0;

        _hasStarted = true;
    }
}