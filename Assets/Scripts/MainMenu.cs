using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Button _startRaceTrackSimulationButton;
    private Button _startRaceTrackSimCadeButton;
    private Button _startDriftTrackArcadeButton;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _startRaceTrackSimulationButton = _uiDocument.rootVisualElement.Q<Button>("race-track-simulation-button");
        _startRaceTrackSimCadeButton = _uiDocument.rootVisualElement.Q<Button>("race-track-sim-cade-button");
        _startDriftTrackArcadeButton = _uiDocument.rootVisualElement.Q<Button>("drift-track-arcade-button");

        _startRaceTrackSimulationButton.RegisterCallback<ClickEvent>(StarRaceTrackSimulationGame);
        _startRaceTrackSimCadeButton.RegisterCallback<ClickEvent>(StarRaceTrackSimCadeGame);
        _startDriftTrackArcadeButton.RegisterCallback<ClickEvent>(StarDriftTrackArcadeGame);
    }

    private void StarRaceTrackSimulationGame(ClickEvent evt)
    {
        SceneManager.LoadScene(1);
    }

    private void StarRaceTrackSimCadeGame(ClickEvent evt)
    {
        SceneManager.LoadScene(1);
    }

    private void StarDriftTrackArcadeGame(ClickEvent evt)
    {
        SceneManager.LoadScene(2);
    }
}