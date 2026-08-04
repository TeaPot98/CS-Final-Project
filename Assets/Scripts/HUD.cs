using System.Collections;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class HUD : MonoBehaviour
{
    [SerializeField] protected UIDocument document;
    [SerializeField] protected Car car;

    private VisualElement _root;
    private VisualElement _container;
    private VisualElement _carStatsContainer;
    private VisualElement _wheelStatsContainer;
    private VisualElement _userInputContainer;

    private void Start()
    {
        StartCoroutine(InitializeView());
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public IEnumerator InitializeView()
    {
        _root = document.rootVisualElement;
        _container = _root.Query<VisualElement>("container");
        _carStatsContainer = _container.Query<VisualElement>("car-stats-container");
        _wheelStatsContainer = _container.Query<VisualElement>("wheel-debug-container");
        _userInputContainer = _container.Query<VisualElement>("user-input-container");

        VisualElement flWheelContainer = _wheelStatsContainer.Query<VisualElement>("FL");
        VisualElement frWheelContainer = _wheelStatsContainer.Query<VisualElement>("FR");
        VisualElement rlWheelContainer = _wheelStatsContainer.Query<VisualElement>("RL");
        VisualElement rrWheelContainer = _wheelStatsContainer.Query<VisualElement>("RR");

        Label gearText = _carStatsContainer.Query<Label>("Gear");
        Label gearRatioText = _carStatsContainer.Query<Label>("GearRatio");
        Label rpmText = _carStatsContainer.Query<Label>("RPM");
        Label speedText = _carStatsContainer.Query<Label>("Speed");

        Label flWheelRpmText = flWheelContainer.Query<Label>("RPM");
        Label frWheelRpmText = frWheelContainer.Query<Label>("RPM");
        Label rlWheelRpmText = rlWheelContainer.Query<Label>("RPM");
        Label rrWheelRpmText = rrWheelContainer.Query<Label>("RPM");

        Label flWheelSlipAngleText = flWheelContainer.Query<Label>("SlipAngle");
        Label frWheelSlipAngleText = frWheelContainer.Query<Label>("SlipAngle");
        Label rlWheelSlipAngleText = rlWheelContainer.Query<Label>("SlipAngle");
        Label rrWheelSlipAngleText = rrWheelContainer.Query<Label>("SlipAngle");

        Label flWheelSlipRatioText = flWheelContainer.Query<Label>("SlipRatio");
        Label frWheelSlipRatioText = frWheelContainer.Query<Label>("SlipRatio");
        Label rlWheelSlipRatioText = rlWheelContainer.Query<Label>("SlipRatio");
        Label rrWheelSlipRatioText = rrWheelContainer.Query<Label>("SlipRatio");

        Label throttleText = _userInputContainer.Query<Label>("throttle");
        Label brakeText = _userInputContainer.Query<Label>("braking");
        Label steeringText = _userInputContainer.Query<Label>("steering");


        UIUtils.BindToLabel(gearText, car, nameof(Car.GearLabel));
        UIUtils.BindToLabel(gearRatioText, car, nameof(Car.GearRatioLabel));
        UIUtils.BindToLabel(rpmText, car, nameof(Car.RpmLabel));
        UIUtils.BindToLabel(speedText, car, nameof(Car.SpeedLabel));

        UIUtils.BindToLabel(flWheelRpmText, car, nameof(Car.FLWheelRpm));
        UIUtils.BindToLabel(frWheelRpmText, car, nameof(Car.FRWheelRpm));
        UIUtils.BindToLabel(rlWheelRpmText, car, nameof(Car.RLWheelRpm));
        UIUtils.BindToLabel(rrWheelRpmText, car, nameof(Car.RRWheelRpm));

        UIUtils.BindToLabel(flWheelSlipAngleText, car, nameof(Car.FLSlipAngle));
        UIUtils.BindToLabel(frWheelSlipAngleText, car, nameof(Car.FRSlipAngle));
        UIUtils.BindToLabel(rlWheelSlipAngleText, car, nameof(Car.RLSlipAngle));
        UIUtils.BindToLabel(rrWheelSlipAngleText, car, nameof(Car.RRSlipAngle));

        UIUtils.BindToLabel(flWheelSlipRatioText, car, nameof(Car.FLSlipRatio));
        UIUtils.BindToLabel(frWheelSlipRatioText, car, nameof(Car.FRSlipRatio));
        UIUtils.BindToLabel(rlWheelSlipRatioText, car, nameof(Car.RLSlipRatio));
        UIUtils.BindToLabel(rrWheelSlipRatioText, car, nameof(Car.RRSlipRatio));

        UIUtils.BindToLabel(throttleText, car, nameof(car.Throttle));
        UIUtils.BindToLabel(brakeText, car, nameof(car.Brake));
        UIUtils.BindToLabel(steeringText, car, nameof(car.Steering));

        yield return null;
    }
}