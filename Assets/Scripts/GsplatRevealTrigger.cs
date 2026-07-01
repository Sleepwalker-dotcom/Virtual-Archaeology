using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GsplatRevealTrigger : MonoBehaviour
{
    [SerializeField] private GsplatSegmentRevealController revealController;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        if (revealController == null)
            revealController = FindFirstObjectByType<GsplatSegmentRevealController>();
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((triggerOnce && hasTriggered) || !other.CompareTag("Player"))
            return;

        hasTriggered = true;
        revealController?.RevealIndoor();
    }
}
