using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class SingleSceneAudioEnvironmentManager : MonoBehaviour
{
    public enum EnvironmentState
    {
        Museum,
        Tavern
    }

    [Header("Startup")]
    [SerializeField] private EnvironmentState startupEnvironment = EnvironmentState.Museum;
    [SerializeField] private bool playMuseumBgmOnStart = true;
    [SerializeField] private bool restartMuseumBgmWhenBackToMuseum = true;

    [Header("Environment Roots")]
    [SerializeField] private GameObject museumEnvironmentRoot;
    [SerializeField] private GameObject tavernEnvironmentRoot;

    [Header("Optional Audio Zone Roots")]
    [SerializeField] private GameObject scene3DAudioZonesRoot;

    [Header("FMOD Events")]
    [SerializeField] private EventReference museumBgmEvent;
    [SerializeField] private EventReference sceneTransitionEvent;

    [Header("Timing")]
    [SerializeField] private float museumBgmFadeOutTime = 1f;
    [SerializeField] private float environmentSwitchDelay = 0.4f;

    [Header("Debug")]
    [SerializeField] private EnvironmentState currentEnvironment;
    [SerializeField] private bool museumBgmIsPlaying;
    [SerializeField] private bool isSwitching;

    private EventInstance museumBgmInstance;
    private Coroutine switchRoutine;

    private void Start()
    {
        currentEnvironment = startupEnvironment;
        ApplyEnvironmentImmediate(startupEnvironment);

        if (startupEnvironment == EnvironmentState.Museum && playMuseumBgmOnStart)
            StartMuseumBGM();
    }

    public void StartMuseumBGM()
    {
        if (museumBgmIsPlaying || museumBgmEvent.IsNull)
            return;

        museumBgmInstance = RuntimeManager.CreateInstance(museumBgmEvent);
        if (!museumBgmInstance.isValid())
        {
            Debug.LogError("[SingleSceneAudioEnvironmentManager] Museum BGM instance is invalid.", this);
            return;
        }

        museumBgmInstance.setVolume(1f);
        FMOD.RESULT result = museumBgmInstance.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("[SingleSceneAudioEnvironmentManager] Failed to start Museum BGM: " + result, this);
            museumBgmInstance.release();
            museumBgmInstance.clearHandle();
            return;
        }

        museumBgmIsPlaying = true;
    }

    public void StopMuseumBGM()
    {
        if (museumBgmIsPlaying)
            StartCoroutine(FadeOutAndStopMuseumBGM());
    }

    public void PlaySceneTransition()
    {
        if (sceneTransitionEvent.IsNull)
        {
            Debug.LogWarning("[SingleSceneAudioEnvironmentManager] Scene Transition event is not assigned.", this);
            return;
        }

        RuntimeManager.PlayOneShot(sceneTransitionEvent);
    }

    public void SwitchToMuseum()
    {
        SwitchEnvironment(EnvironmentState.Museum);
    }

    public void SwitchToTavern()
    {
        SwitchEnvironment(EnvironmentState.Tavern);
    }

    public void SwitchEnvironment(EnvironmentState targetEnvironment)
    {
        if (isSwitching || targetEnvironment == currentEnvironment)
            return;

        switchRoutine = StartCoroutine(SwitchEnvironmentRoutine(targetEnvironment));
    }

    private IEnumerator SwitchEnvironmentRoutine(EnvironmentState targetEnvironment)
    {
        isSwitching = true;

        if (currentEnvironment == EnvironmentState.Museum && museumBgmIsPlaying)
            yield return FadeOutAndStopMuseumBGM();

        PlaySceneTransition();

        if (environmentSwitchDelay > 0f)
            yield return new WaitForSeconds(environmentSwitchDelay);

        ApplyEnvironmentImmediate(targetEnvironment);
        currentEnvironment = targetEnvironment;

        if (targetEnvironment == EnvironmentState.Museum && restartMuseumBgmWhenBackToMuseum)
            StartMuseumBGM();

        isSwitching = false;
        switchRoutine = null;
    }

    private IEnumerator FadeOutAndStopMuseumBGM()
    {
        if (!museumBgmIsPlaying || !museumBgmInstance.isValid())
            yield break;

        float fadeDuration = Mathf.Max(0f, museumBgmFadeOutTime);
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            museumBgmInstance.setVolume(1f - Mathf.Clamp01(timer / fadeDuration));
            yield return null;
        }

        museumBgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        museumBgmInstance.release();
        museumBgmInstance.clearHandle();
        museumBgmIsPlaying = false;
    }

    private void ApplyEnvironmentImmediate(EnvironmentState environmentState)
    {
        bool museumActive = environmentState == EnvironmentState.Museum;

        if (museumEnvironmentRoot != null)
            museumEnvironmentRoot.SetActive(museumActive);

        if (tavernEnvironmentRoot != null)
            tavernEnvironmentRoot.SetActive(!museumActive);

        if (scene3DAudioZonesRoot != null)
            scene3DAudioZonesRoot.SetActive(!museumActive);
    }

    private void OnDestroy()
    {
        if (switchRoutine != null)
            StopCoroutine(switchRoutine);

        if (!museumBgmInstance.isValid())
            return;

        museumBgmInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        museumBgmInstance.release();
        museumBgmInstance.clearHandle();
        museumBgmIsPlaying = false;
    }
}
