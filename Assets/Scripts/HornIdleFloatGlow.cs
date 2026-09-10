using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class HornIdleFloatGlow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color glowColor = new Color(1f, 0.75f, 0.25f);
    [SerializeField, Min(0f)] private float minGlowIntensity = 0.08f;
    [SerializeField, Min(0f)] private float maxGlowIntensity = 0.35f;
    [SerializeField] private bool glowOnlyAfterFirstGrab;
    [SerializeField, Min(0f)] private float floatHeight = 0.04f;
    [SerializeField, Min(0.01f)] private float floatSpeed = 0.8f;

    private XRGrabInteractable grabInteractable;
    private Material[] materials;
    private Vector3 startLocalPosition;
    private bool hasBeenGrabbed;

    private void Awake()
    {
        if (target == null)
            target = transform;

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        CacheMaterials();
        startLocalPosition = target.localPosition;
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
            grabInteractable.selectEntered.AddListener(HandleGrabbed);

        if (glowOnlyAfterFirstGrab)
            SetEmission(Color.black);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(HandleGrabbed);
    }

    private void Update()
    {
        if (hasBeenGrabbed || target == null)
            return;

        float wave = Mathf.Sin(Time.time * floatSpeed * Mathf.PI * 2f);
        target.localPosition = startLocalPosition + Vector3.up * (wave * floatHeight);

        if (glowOnlyAfterFirstGrab)
        {
            SetEmission(Color.black);
            return;
        }

        float glow = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, (wave + 1f) * 0.5f);
        SetEmission(glowColor * glow);
    }

    private void HandleGrabbed(SelectEnterEventArgs args)
    {
        DisableAppearanceGuide();
    }

    public void DisableAppearanceGuide()
    {
        hasBeenGrabbed = true;

        if (target != null)
            target.localPosition = startLocalPosition;

        SetEmission(Color.black);
        enabled = false;
    }

    private void CacheMaterials()
    {
        if (renderers == null || renderers.Length == 0)
            return;

        int materialCount = 0;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
                materialCount += targetRenderer.materials.Length;
        }

        materials = new Material[materialCount];

        int index = 0;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            foreach (Material material in targetRenderer.materials)
            {
                materials[index] = material;
                index++;
            }
        }
    }

    private void SetEmission(Color emission)
    {
        if (materials == null)
            return;

        foreach (Material material in materials)
        {
            if (material == null || !material.HasProperty("_EmissionColor"))
                continue;

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
    }

}
