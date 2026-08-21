using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class FMOD2DSFXPlayer : MonoBehaviour
{
    [SerializeField] private EventReference sceneTransitionSfx;
    [SerializeField] private EventReference positiveFeedbackSfx;
    [SerializeField] private EventReference applauseSfx;
    [SerializeField] private EventReference harpSfx;
    [SerializeField] private UnityEvent onApplauseFinished;
    [SerializeField] private UnityEvent onHarpFinished;

    private EventInstance applauseInstance;
    private Coroutine applauseCompletion;
    private EventInstance harpInstance;
    private Coroutine harpCompletion;

    public void PlaySceneTransition()
    {
        PlayOneShot(sceneTransitionSfx, "Scene Transition");
    }

    public void PlayPositiveFeedback()
    {
        PlayOneShot(positiveFeedbackSfx, "Positive Feedback");
    }

    public void PlayApplause()
    {
        StopTrackedApplause();

        if (applauseSfx.IsNull)
        {
            Debug.LogWarning("[FMOD2DSFXPlayer] Missing event: Applause", this);
            return;
        }

        applauseInstance = RuntimeManager.CreateInstance(applauseSfx);
        FMOD.RESULT result = applauseInstance.start();

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(
                "[FMOD2DSFXPlayer] Failed to start " +
                applauseSfx.Path + ": " + result,
                this
            );
            StopTrackedApplause();
            return;
        }

        applauseCompletion = StartCoroutine(WaitForApplauseToFinish());
        Debug.Log(
            "[FMOD2DSFXPlayer] Playing Applause: " + applauseSfx.Path,
            this
        );
    }

    public void PlayHarp()
    {
        StopTrackedHarp();

        if (harpSfx.IsNull)
        {
            Debug.LogWarning("[FMOD2DSFXPlayer] Missing event: Harp", this);
            onHarpFinished?.Invoke();
            return;
        }

        harpInstance = RuntimeManager.CreateInstance(harpSfx);
        FMOD.RESULT result = harpInstance.start();

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(
                "[FMOD2DSFXPlayer] Failed to start " +
                harpSfx.Path + ": " + result,
                this
            );
            StopTrackedHarp();
            onHarpFinished?.Invoke();
            return;
        }

        harpCompletion = StartCoroutine(WaitForHarpToFinish());
        Debug.Log(
            "[FMOD2DSFXPlayer] Playing Harp: " + harpSfx.Path,
            this
        );
    }

    private IEnumerator WaitForHarpToFinish()
    {
        while (harpInstance.isValid())
        {
            FMOD.RESULT result = harpInstance.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "[FMOD2DSFXPlayer] Failed to read Harp playback state: " +
                    result,
                    this
                );
                StopTrackedHarp();
                onHarpFinished?.Invoke();
                yield break;
            }

            if (playbackState == PLAYBACK_STATE.STOPPED)
                break;

            yield return null;
        }

        ReleaseTrackedHarp();
        harpCompletion = null;
        onHarpFinished?.Invoke();
    }

    private IEnumerator WaitForApplauseToFinish()
    {
        while (applauseInstance.isValid())
        {
            FMOD.RESULT result = applauseInstance.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "[FMOD2DSFXPlayer] Failed to read Applause playback state: " +
                    result,
                    this
                );
                StopTrackedApplause();
                yield break;
            }

            if (playbackState == PLAYBACK_STATE.STOPPED)
                break;

            yield return null;
        }

        ReleaseTrackedApplause();
        applauseCompletion = null;
        onApplauseFinished?.Invoke();
    }

    private void StopTrackedApplause()
    {
        if (applauseCompletion != null)
        {
            StopCoroutine(applauseCompletion);
            applauseCompletion = null;
        }

        if (!applauseInstance.isValid())
            return;

        applauseInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        ReleaseTrackedApplause();
    }

    private void ReleaseTrackedApplause()
    {
        if (!applauseInstance.isValid())
            return;

        applauseInstance.release();
        applauseInstance.clearHandle();
    }

    private void StopTrackedHarp()
    {
        if (harpCompletion != null)
        {
            StopCoroutine(harpCompletion);
            harpCompletion = null;
        }

        if (!harpInstance.isValid())
            return;

        harpInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        ReleaseTrackedHarp();
    }

    private void ReleaseTrackedHarp()
    {
        if (!harpInstance.isValid())
            return;

        harpInstance.release();
        harpInstance.clearHandle();
    }

    private void OnDisable()
    {
        StopTrackedApplause();
        StopTrackedHarp();
    }

    private void PlayOneShot(EventReference eventReference, string label)
    {
        if (eventReference.IsNull)
        {
            Debug.LogWarning("[FMOD2DSFXPlayer] Missing event: " + label, this);
            return;
        }

        RuntimeManager.PlayOneShot(eventReference);
        Debug.Log(
            "[FMOD2DSFXPlayer] Playing " +
            label + ": " + eventReference.Path,
            this
        );
    }
}
