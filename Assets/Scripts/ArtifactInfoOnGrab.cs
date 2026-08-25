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

    private XRGrabInteractable grab;

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
    }

    private void Show(SelectEnterEventArgs args)
    {
        panel?.Show(title, description);
    }

    private void Hide(SelectExitEventArgs args)
    {
        panel?.Hide();
    }
}
