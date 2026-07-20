using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HornPieceEnvironmentSwitchTrigger : MonoBehaviour
{
    [Header("References")]
    public XRGrabInteractable hornPieceGrabInteractable;
    public SingleSceneAudioEnvironmentManager audioEnvironmentManager;

    [Header("Switch Settings")]
    public SingleSceneAudioEnvironmentManager.EnvironmentState targetEnvironment =
        SingleSceneAudioEnvironmentManager.EnvironmentState.Tavern;

    public bool triggerOnce = true;

    [Header("Debug")]
    public bool hasTriggered = false;

    private void Awake()
    {
        if (hornPieceGrabInteractable == null)
            hornPieceGrabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (hornPieceGrabInteractable != null)
            hornPieceGrabInteractable.selectEntered.AddListener(OnHornPiecePickedUp);
    }

    private void OnDisable()
    {
        if (hornPieceGrabInteractable != null)
            hornPieceGrabInteractable.selectEntered.RemoveListener(OnHornPiecePickedUp);
    }

    private void OnHornPiecePickedUp(SelectEnterEventArgs args)
    {
        Debug.Log("[HornPieceEnvironmentSwitchTrigger] Horn_Piece picked up by: " + args.interactorObject.transform.name);

        if (triggerOnce && hasTriggered)
            return;

        if (audioEnvironmentManager == null)
        {
            Debug.LogError("[HornPieceEnvironmentSwitchTrigger] Audio Environment Manager is missing.");
            return;
        }

        audioEnvironmentManager.SwitchEnvironment(targetEnvironment);

        hasTriggered = true;
    }

    [ContextMenu("Test Switch Now")]
    public void TestSwitchNow()
    {
        if (audioEnvironmentManager == null)
        {
            Debug.LogError("[HornPieceEnvironmentSwitchTrigger] Audio Environment Manager is missing.");
            return;
        }

        audioEnvironmentManager.SwitchEnvironment(targetEnvironment);
        hasTriggered = true;
    }
}