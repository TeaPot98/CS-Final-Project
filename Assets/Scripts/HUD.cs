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


        Label gearText = _carStatsContainer.Query<Label>("Gear");
        Label rpmText = _carStatsContainer.Query<Label>("RPM");
        Label speedText = _carStatsContainer.Query<Label>("Speed");

        Label flWheelText = _wheelStatsContainer.Query<Label>("FL");
        Label frWheelText = _wheelStatsContainer.Query<Label>("FR");
        Label rlWheelText = _wheelStatsContainer.Query<Label>("RL");
        Label rrWheelText = _wheelStatsContainer.Query<Label>("RR");

        Label throttleText = _userInputContainer.Query<Label>("throttle");
        Label brakeText = _userInputContainer.Query<Label>("braking");
        Label steeringText = _userInputContainer.Query<Label>("steering");


        BindToLabel(gearText, car, nameof(car.GearLabel));
        BindToLabel(rpmText, car, nameof(car.RpmLabel));
        BindToLabel(speedText, car, nameof(car.SpeedLabel));

        BindToLabel(flWheelText, car, nameof(car.FLWheelRpm));
        BindToLabel(frWheelText, car, nameof(car.FRWheelRpm));
        BindToLabel(rlWheelText, car, nameof(car.RLWheelRpm));
        BindToLabel(rrWheelText, car, nameof(car.RRWheelRpm));

        BindToLabel(throttleText, car, nameof(car.Throttle));
        BindToLabel(brakeText, car, nameof(car.Brake));
        BindToLabel(steeringText, car, nameof(car.Steering));

        yield return null;
    }

    private void BindToLabel(Label label, object source, string propertyPath)
    {
        label.dataSource = source;
        label.SetBinding(nameof(Label.text), new DataBinding
        {
            dataSourcePath = new PropertyPath(propertyPath),
            bindingMode = BindingMode.ToTarget
        });
    }
}