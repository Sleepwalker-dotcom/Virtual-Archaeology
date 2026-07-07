using UnityEngine;

public class ExtraLayerTrigger : MonoBehaviour
{
    public HornFMODController hornController;

    [Tooltip("0 = ExtraLayer1, 1 = ExtraLayer2, 2 = ExtraLayer3")]
    public int layerIndex;

    public bool triggerOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[ExtraLayerTrigger] Entered by: " + other.name + " | Tag: " + other.tag);

        if (triggerOnce && hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (hornController == null)
        {
            Debug.LogError("[ExtraLayerTrigger] Horn controller missing.");
            return;
        }

        if (!hornController.ActivateExtraLayer(layerIndex))
            return;

        hasTriggered = true;
    }
}
