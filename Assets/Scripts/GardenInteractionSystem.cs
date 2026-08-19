using UnityEngine;

public sealed class GardenInteractionSystem : MonoBehaviour
{
    [SerializeField]
    private GameObject hornGuidePivotPrefab;

    [SerializeField]
    private Camera playerCamera;

    [SerializeField, Min(0f)]
    private float revealDistance = 0.5f;

    private GameObject spawnedHornGuidePivot;

    public GameObject ShowHornGuidePivot()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (hornGuidePivotPrefab == null || playerCamera == null)
        {
            Debug.LogWarning(
                "[GardenInteractionSystem] HornGuidePivot Prefab or Player Camera is missing.",
                this
            );
            return null;
        }

        Vector3 spawnPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward * revealDistance;

        if (spawnedHornGuidePivot == null)
        {
            spawnedHornGuidePivot = Instantiate(
                hornGuidePivotPrefab,
                spawnPosition,
                hornGuidePivotPrefab.transform.rotation
            );
        }
        else
        {
            spawnedHornGuidePivot.transform.position = spawnPosition;
            spawnedHornGuidePivot.SetActive(true);
        }

        HornFMODController hornController =
            spawnedHornGuidePivot.GetComponentInChildren<HornFMODController>(
                true
            );

        if (hornController != null)
        {
            hornController.enabled = false;
        }

        Debug.Log(
            "[GardenInteractionSystem] HornGuidePivot Prefab instantiated " +
            revealDistance + "m in front of the player.",
            this
        );

        return spawnedHornGuidePivot;
    }
}
