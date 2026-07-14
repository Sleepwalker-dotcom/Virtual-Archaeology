using Gsplat;
using UnityEngine;

public class GsplatSegmentRevealController : MonoBehaviour
{
    [Header("Scene Segments")]
    [SerializeField] private GsplatRenderer streetRenderer;
    [SerializeField] private GsplatRenderer outdoorRenderer;
    [SerializeField] private GsplatRenderer indoorRenderer;

    [Header("Persistent Effect")]
    [SerializeField] private GsplatEffectType persistentEffect = GsplatEffectType.PerlinWave;
    [SerializeField, Range(0f, 1f)] private float persistentIntensity = 0.18f;
    [SerializeField, Range(-0.2f, 0.2f)] private float persistentWaveAmplitude = 0.025f;
    [SerializeField, Range(0f, 2f)] private float persistentWaveSpeed = 0.22f;

    private void Awake()
    {
        if (streetRenderer == null || outdoorRenderer == null || indoorRenderer == null)
        {
            GsplatRenderer[] renderers = FindObjectsByType<GsplatRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GsplatRenderer renderer in renderers)
            {
                if (streetRenderer == null && renderer.name == "GSplat_Street")
                    streetRenderer = renderer;
                else if (outdoorRenderer == null && renderer.name == "GSplat_Outdoor")
                    outdoorRenderer = renderer;
                else if (indoorRenderer == null && renderer.name == "GSplat_Indoor")
                    indoorRenderer = renderer;
            }
        }
    }

    private void Start()
    {
        ShowSegment(streetRenderer);
        ShowSegment(outdoorRenderer);
        ShowSegment(indoorRenderer);
    }

    private void ShowSegment(GsplatRenderer renderer)
    {
        if (renderer == null)
            return;

        renderer.gameObject.SetActive(true);
        renderer.useSplitMask = false;
        renderer.useBoxMask = false;
        renderer.effectType = persistentEffect;
        renderer.intensity = persistentIntensity;
        renderer.waveAmplitude = persistentWaveAmplitude;
        renderer.waveSpeed = persistentWaveSpeed;
        renderer.blendScale = 1f;
        renderer.resetAnimationTime();
    }
}
