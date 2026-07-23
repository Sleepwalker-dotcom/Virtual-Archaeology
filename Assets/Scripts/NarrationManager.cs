using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public sealed class NarrationManager : MonoBehaviour
{
    [Header("FMOD Narration Events")]
    [SerializeField] private EventReference introNarrationEvent;
    [SerializeField] private EventReference restorationNarrationEvent;
    [SerializeField] private EventReference restoreVoiceOverEvent;
    [SerializeField] private EventReference pickupVoiceOverEvent;
    [SerializeField] private EventReference gardenVoiceOverEvent;

    [Header("Playback")]
    [SerializeField] private bool allowFadeoutWhenInterrupted = true;

    private EventInstance currentNarration;

    public void PlayIntroNarration()
    {
        PlayNarration(introNarrationEvent, "Intro");
    }

    public void PlayRestorationNarration()
    {
        PlayNarration(restorationNarrationEvent, "Restoration");
    }

    public void PlayRestoreVoiceOver()
    {
        PlayNarration(restoreVoiceOverEvent, "Restore VO");
    }

    public void PlayPickupVoiceOver()
    {
        PlayNarration(pickupVoiceOverEvent, "Pickup VO");
    }

    public void PlayGardenVoiceOver()
    {
        PlayNarration(gardenVoiceOverEvent, "Garden VO");
    }

    public void StopNarration()
    {
        if (!currentNarration.isValid())
            return;

        FMOD.Studio.STOP_MODE stopMode = allowFadeoutWhenInterrupted
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
            : FMOD.Studio.STOP_MODE.IMMEDIATE;

        currentNarration.stop(stopMode);
        currentNarration.release();
        currentNarration.clearHandle();
    }

    private void PlayNarration(EventReference narrationEvent, string label)
    {
        if (narrationEvent.IsNull)
        {
            Debug.LogWarning(
                "[NarrationManager] " + label + " narration event is not assigned.",
                this
            );
            return;
        }

        StopNarration();
        currentNarration = RuntimeManager.CreateInstance(narrationEvent);

        FMOD.RESULT result = currentNarration.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(
                "[NarrationManager] Failed to start " +
                narrationEvent.Path + ": " + result,
                this
            );
            currentNarration.release();
            currentNarration.clearHandle();
            return;
        }

        Debug.Log(
            "[NarrationManager] Playing " + label +
            " narration: " + narrationEvent.Path,
            this
        );
    }

    private void OnDisable()
    {
        StopNarration();
    }
}
