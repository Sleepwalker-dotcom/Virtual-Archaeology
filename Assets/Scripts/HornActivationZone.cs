using UnityEngine;

public class HornActivationZone : MonoBehaviour
{
    [Header("References")]
    public HornFMODController hornController;

    [Header("Settings")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Start()
    {
        Debug.Log("[HornActivationZone] Zone script started on: " + gameObject.name);

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[HornActivationZone] No Collider found on activation zone.");
        }
        else
        {
            Debug.Log("[HornActivationZone] Collider found. Is Trigger = " + col.isTrigger);
        }

        if (hornController == null)
        {
            Debug.LogWarning("[HornActivationZone] Horn Controller is not assigned in Inspector.");
        }
        else
        {
            Debug.Log("[HornActivationZone] Horn Controller assigned: " + hornController.name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[HornActivationZone] Something entered trigger: " + other.name + " | Tag: " + other.tag);

        if (triggerOnce && hasTriggered)
        {
            Debug.Log("[HornActivationZone] Already triggered once. Ignoring.");
            return;
        }

        if (!other.CompareTag("Player"))
        {
            Debug.Log("[HornActivationZone] Entered object is not tagged Player. Ignoring: " + other.name);
            return;
        }

        if (hornController == null)
        {
            Debug.LogError("[HornActivationZone] Cannot activate horn because Horn Controller is missing.");
            return;
        }

        Debug.Log("[HornActivationZone] Player entered zone. Activating horn now.");

        hornController.ActivateHorn();

        hasTriggered = true;
    }
}