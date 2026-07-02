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

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.08f);
        Gizmos.DrawCube(box.center, box.size);
    }
}
