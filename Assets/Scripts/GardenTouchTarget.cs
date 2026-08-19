using UnityEngine;

public sealed class GardenTouchTarget : MonoBehaviour
{
    private MuseumExperienceController owner;
    private bool touched;

    public void Initialize(MuseumExperienceController controller)
    {
        owner = controller;
        touched = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTouch(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTouch(collision.collider);
    }

    private void TryTouch(Collider other)
    {
        if (touched || owner == null || !BelongsToPlayer(other))
        {
            return;
        }

        touched = true;
        owner.HandleGardenObjectTouched(this);
    }

    private static bool BelongsToPlayer(Collider other)
    {
        Transform current = other != null ? other.transform : null;

        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
