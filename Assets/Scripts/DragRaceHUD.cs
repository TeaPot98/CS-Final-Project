using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class DragRaceHUD : MonoBehaviour
{
    [SerializeField] protected UIDocument document;
    [SerializeField] protected DragRaceManager dragRaceManager;

    private VisualElement _root;
    private VisualElement _dragStatsContainer;

    private void Start()
    {
        StartCoroutine(InitializeView());
    }

    public IEnumerator InitializeView()
    {
        _root = document.rootVisualElement;
        _dragStatsContainer = _root.Query<VisualElement>("DragStats");

        Label hundredTimeText = _dragStatsContainer.Query<Label>("0-100");
        Label twoHundredTimeText = _dragStatsContainer.Query<Label>("0-200");
        Label hundredToTwoTimeText = _dragStatsContainer.Query<Label>("100-200");
        Label eighthTimeText = _dragStatsContainer.Query<Label>("1-8");
        Label quarterTimeText = _dragStatsContainer.Query<Label>("1-4");
        Label timerText = _dragStatsContainer.Query<Label>("Timer");
        Label stateInfoText = _dragStatsContainer.Query<Label>("StateInfo");

        UIUtils.BindToLabel(hundredTimeText, dragRaceManager, nameof(DragRaceManager.hundredTimeLabel));
        UIUtils.BindToLabel(twoHundredTimeText, dragRaceManager, nameof(DragRaceManager.twoHundredTime));
        UIUtils.BindToLabel(hundredToTwoTimeText, dragRaceManager, nameof(DragRaceManager.hundredToTwoTime));
        UIUtils.BindToLabel(eighthTimeText, dragRaceManager, nameof(DragRaceManager.eighthTime));
        UIUtils.BindToLabel(quarterTimeText, dragRaceManager, nameof(DragRaceManager.quarterTime));
        UIUtils.BindToLabel(timerText, dragRaceManager, nameof(DragRaceManager.timeLabel));
        UIUtils.BindToLabel(stateInfoText, dragRaceManager, nameof(DragRaceManager.stateInfo));

        yield return null;
    }
}