using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using FMODUnity;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class ArtifactInfoOnGrab : MonoBehaviour
{
    [SerializeField] private ArtifactInfoPanel panel;
    [SerializeField] private string title;
    [TextArea(4, 12)]
    [SerializeField] private string description;
    [Tooltip("Optional FMOD introduction played when this artefact is picked up.")]
    [SerializeField] private EventReference introVoiceOverEvent;
    [Tooltip("Optional one-shot feedback when touched during the full melody interaction.")]
    [SerializeField] private EventReference tapFeedbackEvent;
    [SerializeField] private bool requireFinishedMelody;
    [SerializeField] private HornFMODController hornController;
    [SerializeField] private NarrationManager completionManager;
    [SerializeField] private string completionKey;
    [SerializeField] private bool countsForEnding;

    private XRGrabInteractable grab;
    private bool shown;
    private IXRSelectFilter performanceSelectFilter;

    public bool CanShow =>
        (!requireFinishedMelody ||
         (hornController != null && hornController.HasFinishedFullMelodyRepeats)) &&
        (completionManager == null || !completionManager.IsObjectInteractionBlocked);

    public void ConfigureCompletion(NarrationManager manager, string key, bool requireMelody)
    {
        completionManager = manager;
        completionKey = key;
        countsForEnding = true;
        if (requireMelody)
        {
            requireFinishedMelody = true;
            hornController = manager != null ? manager.GetHornController() : null;
        }
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        performanceSelectFilter = new XRSelectFilterDelegate(
            (_, _) => tapFeedbackEvent.IsNull || hornController == null ||
                !hornController.IsFullMelodyRepeatPlaying);
    }

    private void OnEnable()
    {
        grab.selectEntered.AddListener(Show);
        grab.selectExited.AddListener(Hide);
        grab.hoverEntered.AddListener(PlayTapFeedback);
        grab.selectFilters.Add(performanceSelectFilter);
    }

    private void OnDisable()
    {
        grab.selectEntered.RemoveListener(Show);
        grab.selectExited.RemoveListener(Hide);
        grab.hoverEntered.RemoveListener(PlayTapFeedback);
        grab.selectFilters.Remove(performanceSelectFilter);
        if (shown) panel?.Hide(this);
        shown = false;
    }

    private void PlayTapFeedback(HoverEnterEventArgs args)
    {
        if (hornController == null || !hornController.IsFullMelodyRepeatPlaying ||
            tapFeedbackEvent.IsNull || !(args.interactorObject is XRBaseInputInteractor))
            return;

        RuntimeManager.PlayOneShotAttached(tapFeedbackEvent, gameObject);
    }

    private void Show(SelectEnterEventArgs args)
    {
        if (!CanShow) return;
        shown = true;
        if (!introVoiceOverEvent.IsNull && completionManager != null)
            completionManager.PlayArtifactIntroVoiceOver(
                introVoiceOverEvent, countsForEnding ? completionKey : null);
        panel?.Show(title, description, this);
    }

    private void Hide(SelectExitEventArgs args)
    {
        if (grab.isSelected) return;
        if (shown) panel?.Hide(this);
        shown = false;
    }
}
