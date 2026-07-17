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

        Label gearText = _carStatsContainer.Query<Label>("Gear");
        Label rpmText = _carStatsContainer.Query<Label>("RPM");
        Label speedText = _carStatsContainer.Query<Label>("Speed");


        BindToLabel(gearText, car, nameof(car.GearLabel));
        BindToLabel(rpmText, car, nameof(car.RpmLabel));
        BindToLabel(speedText, car, nameof(car.SpeedLabel));

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