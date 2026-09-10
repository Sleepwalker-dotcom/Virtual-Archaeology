using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public sealed class TapToggleGlow : MonoBehaviour
{
    private enum ObjectIntroVoiceOver
    {
        None,
        Bottle,
        Domino,
        Whistle
    }

    [SerializeField] private Renderer[] renderers;
    [SerializeField, ColorUsage(true, true)] private Color glowColor =
        new Color(1f, 0.65f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float glowIntensity = 2f;
    [Header("Tap Feedback")]
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.55f;
    [SerializeField, Min(0f)] private float hapticDuration = 0.08f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.01f;
    [SerializeField, Min(0f)] private float shakeDuration = 0.2f;
    [SerializeField, Min(0f)] private float shakeFrequency = 35f;
    [Header("Playing Horn Layer")]
    [SerializeField] private HornFMODController hornController;
    [SerializeField, Range(0, 2)] private int extraLayerIndex;
    [Header("Interaction Modules")]
    [SerializeField] private NarrationManager narrationManager;
    [SerializeField] private ObjectIntroVoiceOver objectIntroVoiceOver;
    [SerializeField] private UnityEvent onPerformanceTouch;
    [Header("Performance Touch Effect")]
    [SerializeField] private GameObject performanceTouchEffectPrefab;
    [SerializeField, Min(0f)] private float effectSurfaceOffset = 0.01f;
    [SerializeField, Min(0f)] private float effectCooldown = 0.15f;
    [SerializeField] private Vector2 effectScaleRange = new Vector2(0.008f, 0.012f);

    private MaterialPropertyBlock propertyBlock;
    private bool isGlowing;
    private Coroutine shakeCoroutine;
    private Vector3 restingLocalPosition;
    private XRGrabInteractable grabInteractable;
    private bool postMelodyInteractionDisabled;
    private IXRSelectFilter performanceSelectFilter;
    private float lastEffectTime = float.NegativeInfinity;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        propertyBlock = new MaterialPropertyBlock();
        restingLocalPosition = transform.localPosition;
        EnsureTriggerRigidbody();
        EnsureGrabInteraction();
        performanceSelectFilter = new XRSelectFilterDelegate(
            (_, _) => hornController == null || !hornController.IsFullMelodyRepeatPlaying);
        grabInteractable.selectFilters.Add(performanceSelectFilter);
        EnableEmissionOnRendererMaterials();
        ApplyGlow(false);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(HandleGrabbed);
            grabInteractable.hoverEntered.RemoveListener(HandleHoverEntered);
            if (performanceSelectFilter != null)
                grabInteractable.selectFilters.Remove(performanceSelectFilter);
        }
    }

    private void Update()
    {
        if (postMelodyInteractionDisabled || hornController == null ||
            !hornController.HasFinishedFullMelodyRepeats)
        {
            return;
        }

        postMelodyInteractionDisabled = true;
        isGlowing = false;
        ApplyGlow(false);
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.localPosition = restingLocalPosition;
    }

    private void HandleHoverEntered(HoverEnterEventArgs args)
    {
        if (hornController != null && hornController.HasFinishedFullMelodyRepeats)
            return;

        XRBaseInputInteractor inputInteractor =
            args.interactorObject as XRBaseInputInteractor;
        if (inputInteractor == null)
            return;

        if (hornController != null && hornController.IsFullMelodyRepeatPlaying)
        {
            SpawnPerformanceTouchEffect(inputInteractor.transform.position);
            onPerformanceTouch?.Invoke();
            TryRunLayerInteraction();
            inputInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
            return;
        }

        isGlowing = !isGlowing;
        ApplyGlow(isGlowing);
        TryRunLayerInteraction();

        inputInteractor?.SendHapticImpulse(hapticAmplitude, hapticDuration);

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        transform.localPosition = restingLocalPosition;
        shakeCoroutine = StartCoroutine(Shake());
    }

    private void SpawnPerformanceTouchEffect(Vector3 handPosition)
    {
        if (performanceTouchEffectPrefab == null || Time.unscaledTime - lastEffectTime < effectCooldown)
            return;

        lastEffectTime = Time.unscaledTime;
        Vector3 point = transform.position;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider targetCollider in GetComponentsInChildren<Collider>())
        {
            Vector3 candidate = targetCollider.ClosestPoint(handPosition);
            float distance = (candidate - handPosition).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                point = candidate;
            }
        }

        Vector3 outward = (handPosition - point).normalized;
        Quaternion rotation = Quaternion.Euler(
            Random.Range(-20f, 20f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f));
        GameObject effect = Instantiate(
            performanceTouchEffectPrefab,
            point + outward * effectSurfaceOffset,
            rotation);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.startRotation = Random.Range(0f, Mathf.PI * 2f);
        }
        float scale = Random.Range(
            Mathf.Min(effectScaleRange.x, effectScaleRange.y),
            Mathf.Max(effectScaleRange.x, effectScaleRange.y));
        effect.transform.localScale *= scale;
        Destroy(effect, GetEffectLifetime(effect));
    }

    private static float GetEffectLifetime(GameObject effect)
    {
        float lifetime = 2f;
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            lifetime = Mathf.Max(
                lifetime,
                main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
        }
        return lifetime;
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float wave = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f);
            transform.localPosition = restingLocalPosition +
                Random.insideUnitSphere * (wave * shakeDistance * strength);
            yield return null;
        }

        transform.localPosition = restingLocalPosition;
        shakeCoroutine = null;
    }

    private void ApplyGlow(bool enabled)
    {
        if (renderers == null || propertyBlock == null)
            return;

        Color emission = enabled ? glowColor * glowIntensity : Color.black;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.HasProperty("_EmissionColor"))
                    continue;

                targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                propertyBlock.SetColor("_EmissionColor", emission);
                targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                propertyBlock.Clear();
            }
        }
    }

    private void EnableEmissionOnRendererMaterials()
    {
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            foreach (Material material in targetRenderer.materials)
            {
                if (material != null && material.HasProperty("_EmissionColor"))
                    material.EnableKeyword("_EMISSION");
            }
        }
    }

    private void EnsureTriggerRigidbody()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;
    }

    private void EnsureGrabInteraction()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        grabInteractable.hoverEntered.AddListener(HandleHoverEntered);
        grabInteractable.selectEntered.AddListener(HandleGrabbed);
    }

    private void HandleGrabbed(SelectEnterEventArgs args)
    {
        if (hornController != null && hornController.HasFinishedFullMelodyRepeats)
            PlayObjectIntroVoiceOver();
    }

    private void TryRunLayerInteraction()
    {
        if (hornController != null && hornController.IsFullMelodyRepeatPlaying &&
            hornController.SetExtraLayerActive(extraLayerIndex, true))
        {
            Debug.Log(
                "[TapToggleGlow] Layer unlock triggered: " + extraLayerIndex,
                this
            );
        }
    }

    private void PlayObjectIntroVoiceOver()
    {
        if (narrationManager == null)
            return;

        switch (objectIntroVoiceOver)
        {
            case ObjectIntroVoiceOver.Bottle:
                narrationManager.PlayBottleIntroVoiceOver();
                break;
            case ObjectIntroVoiceOver.Domino:
                narrationManager.PlayDominoIntroVoiceOver();
                break;
            case ObjectIntroVoiceOver.Whistle:
                narrationManager.PlayWhistleIntroVoiceOver();
                break;
        }
    }

}
