using UnityEngine;

public sealed class HornActivationZone : MonoBehaviour
{
    private HornFMODController hornController;
    private NarrationManager narrationManager;
    private Collider areaCollider;
    private CharacterController playerController;
    private bool activated;

    public void Initialize(HornFMODController controller)
    {
        hornController = controller;
        narrationManager = FindFirstObjectByType<NarrationManager>(
            FindObjectsInactive.Include
        );
        areaCollider = GetComponent<Collider>();
        activated = false;
        enabled = false;
    }

    public void EnableActivation()
    {
        FindPlayerController();
        enabled = true;
    }

    private void Update()
    {
        if (activated || areaCollider == null)
        {
            return;
        }

        if (playerController == null)
        {
            FindPlayerController();
        }

        if (playerController != null &&
            areaCollider.bounds.Intersects(playerController.bounds))
        {
            ActivateHorn();
        }
    }

    private void ActivateHorn()
    {
        if (activated)
        {
            return;
        }

        if (hornController == null)
        {
            Debug.LogError(
                "[HornActivationZone] HornFMODController is not assigned.",
                this
            );
            return;
        }

        activated = true;
        hornController.enabled = true;
        hornController.ActivateHornAndShowCanvas();
        narrationManager?.PlayGrabHornVoiceOver();

        Debug.Log(
            "[HornActivationZone] Player entered HornInteractionArea; HornFMODController and interaction Canvas activated.",
            this
        );
    }

    private void FindPlayerController()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerController = player != null
            ? player.GetComponent<CharacterController>()
            : null;
    }

}
