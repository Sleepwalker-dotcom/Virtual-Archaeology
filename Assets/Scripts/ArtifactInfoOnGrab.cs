using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class ArtifactInfoOnGrab : MonoBehaviour
{
    [SerializeField] private ArtifactInfoPanel panel;
    [SerializeField] private string title;
    [TextArea(4, 12)]
    [SerializeField] private string description;
    [SerializeField] private bool requireFinishedMelody;
    [SerializeField] private HornFMODController hornController;
    [SerializeField] private NarrationManager completionManager;
    [SerializeField] private string completionKey;
    [SerializeField] private bool countsForEnding;

    private XRGrabInteractable grab;
    private bool shown;

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
    }

    private void OnEnable()
    {
        grab.selectEntered.AddListener(Show);
        grab.selectExited.AddListener(Hide);
    }

    private void OnDisable()
    {
        grab.selectEntered.RemoveListener(Show);
        grab.selectExited.RemoveListener(Hide);
        if (shown) panel?.Hide(this);
        shown = false;
    }

    private void Show(SelectEnterEventArgs args)
    {
        if (!CanShow) return;
        shown = true;
        if (countsForEnding) completionManager?.RegisterObjectCompletion(completionKey);
        panel?.Show(title, description, this);
    }

    private void Hide(SelectExitEventArgs args)
    {
        if (grab.isSelected) return;
        if (shown) panel?.Hide(this);
        shown = false;
    }
}
