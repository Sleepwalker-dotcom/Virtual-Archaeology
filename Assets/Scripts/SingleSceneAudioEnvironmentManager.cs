using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

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
    [SerializeField] private GameObject scene3DAudioSourcesRoot;
    [SerializeField] private GameObject scene3DAudioZonesRoot;

    [Header("FMOD Events")]
    [SerializeField] private EventReference museumBgmEvent;
    [SerializeField] private EventReference sceneTransitionEvent;

    [Header("Timing")]
    [SerializeField] private float museumBgmFadeOutTime = 1f;
    [SerializeField] private float environmentSwitchDelay = 0.4f;

    [Header("Transition Events")]
    [SerializeField] private UnityEvent onSceneTransitionFinished;

    [Header("Debug")]
    [SerializeField] private EnvironmentState currentEnvironment;
    [SerializeField] private bool museumBgmIsPlaying;
    [SerializeField] private bool isSwitching;

    private EventInstance museumBgmInstance;
    private EventInstance sceneTransitionInstance;
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
        SetScene3DAudioActive(false);

        if (currentEnvironment == EnvironmentState.Museum && museumBgmIsPlaying)
            yield return FadeOutAndStopMuseumBGM();

        bool transitionStarted = StartTrackedSceneTransition();

        if (environmentSwitchDelay > 0f)
            yield return new WaitForSeconds(environmentSwitchDelay);

        ApplyEnvironmentRoots(targetEnvironment);
        currentEnvironment = targetEnvironment;

        if (transitionStarted)
            yield return WaitForSceneTransitionToFinish();

        if (targetEnvironment == EnvironmentState.Tavern)
            onSceneTransitionFinished?.Invoke();

        SetScene3DAudioActive(
            targetEnvironment == EnvironmentState.Tavern
        );

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
        ApplyEnvironmentRoots(environmentState);
        SetScene3DAudioActive(
            environmentState == EnvironmentState.Tavern
        );
    }

    private void ApplyEnvironmentRoots(EnvironmentState environmentState)
    {
        bool museumActive = environmentState == EnvironmentState.Museum;

        if (museumEnvironmentRoot != null)
            museumEnvironmentRoot.SetActive(museumActive);

        if (tavernEnvironmentRoot != null)
            tavernEnvironmentRoot.SetActive(!museumActive);
    }

    private void SetScene3DAudioActive(bool active)
    {
        if (scene3DAudioSourcesRoot != null)
            scene3DAudioSourcesRoot.SetActive(active);

        if (scene3DAudioZonesRoot != null)
            scene3DAudioZonesRoot.SetActive(active);
    }

    private bool StartTrackedSceneTransition()
    {
        if (sceneTransitionEvent.IsNull)
        {
            Debug.LogWarning(
                "[SingleSceneAudioEnvironmentManager] Scene Transition event is not assigned.",
                this
            );
            return false;
        }

        sceneTransitionInstance =
            RuntimeManager.CreateInstance(sceneTransitionEvent);

        if (!sceneTransitionInstance.isValid())
        {
            Debug.LogError(
                "[SingleSceneAudioEnvironmentManager] Scene Transition instance is invalid.",
                this
            );
            return false;
        }

        FMOD.RESULT result = sceneTransitionInstance.start();

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(
                "[SingleSceneAudioEnvironmentManager] Failed to start Scene Transition: " +
                result,
                this
            );
            sceneTransitionInstance.release();
            sceneTransitionInstance.clearHandle();
            return false;
        }

        return true;
    }

    private IEnumerator WaitForSceneTransitionToFinish()
    {
        while (sceneTransitionInstance.isValid())
        {
            FMOD.RESULT result =
                sceneTransitionInstance.getPlaybackState(
                    out PLAYBACK_STATE playbackState
                );

            if (result != FMOD.RESULT.OK ||
                playbackState == PLAYBACK_STATE.STOPPED)
            {
                break;
            }

            yield return null;
        }

        if (sceneTransitionInstance.isValid())
        {
            sceneTransitionInstance.release();
            sceneTransitionInstance.clearHandle();
        }

        Debug.Log(
            "[SingleSceneAudioEnvironmentManager] Scene Transition finished; Scene3D audio enabled.",
            this
        );
    }

    private void OnDestroy()
    {
        if (switchRoutine != null)
            StopCoroutine(switchRoutine);

        if (sceneTransitionInstance.isValid())
        {
            sceneTransitionInstance.stop(
                FMOD.Studio.STOP_MODE.IMMEDIATE
            );
            sceneTransitionInstance.release();
            sceneTransitionInstance.clearHandle();
        }

        if (!museumBgmInstance.isValid())
            return;

        museumBgmInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        museumBgmInstance.release();
        museumBgmInstance.clearHandle();
        museumBgmIsPlaying = false;
    }
}
