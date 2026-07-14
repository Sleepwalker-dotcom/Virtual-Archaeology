using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class LoadSceneOnGrab : MonoBehaviour
{
    [SerializeField] private GameObject museumRoot;
    [SerializeField] private GameObject tavernRoot;
    [SerializeField] private GameObject hornObject;
    [SerializeField] private GameObject[] hiddenDuringMuseum;
    [SerializeField] private GameObject transitionEffect;
    [SerializeField, Min(0f)] private float revealDelay;
    [SerializeField] private string museumMusicSceneName = "Museum";
    [SerializeField] private string tavernMusicSceneName = "Tavern";
    [SerializeField] private EventReference tavernRevealSfx;
    [SerializeField, Range(-80f, 6f)] private float tavernRevealVolumeDb = -18f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool hasRevealed;
    private Material originalSkybox;
    private Color originalAmbientSkyColor;
    private Color originalAmbientEquatorColor;
    private Color originalAmbientGroundColor;
    private float originalAmbientIntensity;
    private bool originalFog;
    private Color originalFogColor;
    private float originalFogDensity;
    private float originalReflectionIntensity;
    private Camera mainCamera;
    private CameraClearFlags originalCameraClearFlags;
    private Color originalCameraBackgroundColor;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        mainCamera = Camera.main;
        CaptureTavernEnvironment();
    }

    private void Start()
    {
        SetActiveIfAssigned(tavernRoot, false);
        SetActiveIfAssigned(hornObject, false);
        SetObjectsActive(hiddenDuringMuseum, false);
        SetActiveIfAssigned(transitionEffect, false);
        ApplyMuseumEnvironment();
        FmodGlobalAudioManager.Instance?.PlayMusicForScene(museumMusicSceneName);
    }

    private void Update()
    {
        if (!hasRevealed && grabInteractable != null && grabInteractable.isSelected)
            StartCoroutine(RevealTavern());
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!hasRevealed)
            StartCoroutine(RevealTavern());
    }

    private IEnumerator RevealTavern()
    {
        hasRevealed = true;
        SetActiveIfAssigned(transitionEffect, true);

        if (revealDelay > 0f)
            yield return new WaitForSeconds(revealDelay);

        SetActiveIfAssigned(tavernRoot, true);
        SetActiveIfAssigned(hornObject, true);
        SetObjectsActive(hiddenDuringMuseum, true);
        RestoreTavernEnvironment();
        PlayTavernRevealAudio();
        FmodGlobalAudioManager.Instance?.PlayMusicForScene(tavernMusicSceneName);
        SetActiveIfAssigned(museumRoot, false);
        SetActiveIfAssigned(transitionEffect, false);
        SetActiveIfAssigned(gameObject, false);
    }

    private void CaptureTavernEnvironment()
    {
        originalSkybox = RenderSettings.skybox;
        originalAmbientSkyColor = RenderSettings.ambientSkyColor;
        originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
        originalAmbientGroundColor = RenderSettings.ambientGroundColor;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalFog = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;

        if (mainCamera != null)
        {
            originalCameraClearFlags = mainCamera.clearFlags;
            originalCameraBackgroundColor = mainCamera.backgroundColor;
        }
    }

    private void ApplyMuseumEnvironment()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientSkyColor = Color.black;
        RenderSettings.ambientEquatorColor = Color.black;
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.08f;
        RenderSettings.reflectionIntensity = 0f;
        ApplyCameraBackground(CameraClearFlags.SolidColor, Color.black);
        DynamicGI.UpdateEnvironment();
    }

    private void RestoreTavernEnvironment()
    {
        RenderSettings.skybox = originalSkybox;
        RenderSettings.ambientSkyColor = originalAmbientSkyColor;
        RenderSettings.ambientEquatorColor = originalAmbientEquatorColor;
        RenderSettings.ambientGroundColor = originalAmbientGroundColor;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        RenderSettings.fog = originalFog;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
        ApplyCameraBackground(originalCameraClearFlags, originalCameraBackgroundColor);
        DynamicGI.UpdateEnvironment();
    }

    private void PlayTavernRevealAudio()
    {
        if (tavernRevealSfx.IsNull || tavernRevealVolumeDb <= -80f)
            return;

        EventInstance instance = RuntimeManager.CreateInstance(tavernRevealSfx);
        float volume = Mathf.Pow(10f, tavernRevealVolumeDb / 20f);
        instance.setVolume(volume);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.start();
        instance.setVolume(volume);
        instance.release();
    }

    private void ApplyCameraBackground(CameraClearFlags clearFlags, Color backgroundColor)
    {
        if (mainCamera == null)
            return;

        mainCamera.clearFlags = clearFlags;
        mainCamera.backgroundColor = backgroundColor;
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
            SetActiveIfAssigned(obj, active);
    }

    private static void SetActiveIfAssigned(GameObject obj, bool active)
    {
        if (obj)
            obj.SetActive(active);
    }
}
