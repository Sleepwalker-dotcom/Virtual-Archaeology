using System.Collections;
using Gsplat;
using UnityEngine;

public class GsplatSegmentRevealController : MonoBehaviour
{
    [Header("Scene Segments")]
    [SerializeField] private GsplatRenderer outdoorRenderer;
    [SerializeField] private GsplatRenderer indoorRenderer;

    [Header("Reveal")]
    [SerializeField] private GsplatEffectType outdoorRevealEffect = GsplatEffectType.Rain;
    [SerializeField] private GsplatEffectType indoorRevealEffect = GsplatEffectType.Spread;
    [SerializeField, Min(0.1f)] private float outdoorRevealDuration = 10.5f;
    [SerializeField, Min(0.1f)] private float indoorRevealDuration = 10.5f;

    [Header("Persistent Effect")]
    [SerializeField] private GsplatEffectType persistentEffect = GsplatEffectType.PerlinWave;
    [SerializeField, Range(0f, 1f)] private float persistentIntensity = 0.18f;
    [SerializeField, Range(-0.2f, 0.2f)] private float persistentWaveAmplitude = 0.025f;
    [SerializeField, Range(0f, 2f)] private float persistentWaveSpeed = 0.22f;

    [Header("Audio")]
    [SerializeField] private HornTransitionAudio hornAudio;

    private bool indoorRevealStarted;

    private void Awake()
    {
        if (outdoorRenderer == null || indoorRenderer == null)
        {
            GsplatRenderer[] renderers = FindObjectsByType<GsplatRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GsplatRenderer renderer in renderers)
            {
                if (outdoorRenderer == null && renderer.name == "GSplat_Outdoor")
                    outdoorRenderer = renderer;
                else if (indoorRenderer == null && renderer.name == "GSplat_Indoor")
                    indoorRenderer = renderer;
            }
        }

        if (hornAudio == null)
            hornAudio = FindFirstObjectByType<HornTransitionAudio>(
                FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (indoorRenderer != null)
            indoorRenderer.gameObject.SetActive(false);

        if (outdoorRenderer != null)
            StartCoroutine(RevealSegment(outdoorRenderer, outdoorRevealEffect, outdoorRevealDuration));

        hornAudio?.PlayQuietCue();
    }

    public void RevealIndoor()
    {
        if (indoorRevealStarted || indoorRenderer == null)
            return;

        indoorRevealStarted = true;
        indoorRenderer.gameObject.SetActive(true);
        hornAudio?.PlayRevealCue();
        StartCoroutine(RevealSegment(indoorRenderer, indoorRevealEffect, indoorRevealDuration));
    }

    private IEnumerator RevealSegment(
        GsplatRenderer renderer,
        GsplatEffectType revealEffect,
        float duration)
    {
        renderer.blendScale = 1f;
        renderer.effectType = revealEffect;
        renderer.resetAnimationTime();

        yield return new WaitForSeconds(duration);

        renderer.effectType = persistentEffect;
        renderer.intensity = persistentIntensity;
        renderer.waveAmplitude = persistentWaveAmplitude;
        renderer.waveSpeed = persistentWaveSpeed;
        renderer.blendScale = 1f;
        renderer.resetAnimationTime();
    }
}
