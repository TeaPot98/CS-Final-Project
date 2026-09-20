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

    private VisualElement _throttleTransparent;
    private VisualElement _throttleOpaque;
    private VisualElement _brakeTransparent;
    private VisualElement _brakeOpaque;

    private void Start()
    {
        StartCoroutine(InitializeView());
    }

    // Update is called once per frame
    private void Update()
    {
        float rawThrottleScale = Utils.MapToNewRange(car.Throttle, 0f, 1f, 0.1f, 1f);
        float appliedThrottleScale = Utils.MapToNewRange(car.AppliedThrottle, 0f, 1f, 0.1f, 1f);
        float rawBrakeScale = Utils.MapToNewRange(car.Brake, 0f, 1f, 0.1f, 1f);
        float appliedBrakeScale = Utils.MapToNewRange(car.AppliedBrake, 0f, 1f, 0.1f, 1f);

        _throttleTransparent.style.scale = new Scale(new Vector2(rawThrottleScale, rawThrottleScale));
        _throttleOpaque.style.scale = new Scale(new Vector2(appliedThrottleScale, appliedThrottleScale));
        _brakeTransparent.style.scale = new Scale(new Vector2(rawBrakeScale, rawBrakeScale));
        _brakeOpaque.style.scale = new Scale(new Vector2(appliedBrakeScale, appliedBrakeScale));
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

        VisualElement throttleContainer = _userInputContainer.Query<VisualElement>("throttle");
        VisualElement brakeContainer = _userInputContainer.Query<VisualElement>("brake");

        _throttleTransparent = throttleContainer.Query<VisualElement>(
            "semi-transparent");
        _throttleOpaque = throttleContainer.Query<VisualElement>(
            "opaque");

        _brakeTransparent = brakeContainer.Query<VisualElement>(
            "semi-transparent");
        _brakeOpaque = brakeContainer.Query<VisualElement>(
            "opaque");

        UIUtils.BindToLabel(gearText, car, nameof(Car.GearLabel));
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

        yield return null;
    }
}