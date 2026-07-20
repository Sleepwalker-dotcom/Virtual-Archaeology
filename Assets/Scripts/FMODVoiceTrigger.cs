using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FMODVoiceTrigger : MonoBehaviour
{
    [SerializeField] private EventReference voiceEvent;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool stopOnExit;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
    private EventInstance voiceInstance;
    private bool hasPlayed;

    private void OnTriggerEnter(Collider other)
    {
        if (!BelongsToPlayer(other) || !playerColliders.Add(other))
            return;

        if (playerColliders.Count == 1 && (!playOnce || !hasPlayed))
            PlayVoice();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other))
            return;

        if (stopOnExit && playerColliders.Count == 0)
            StopVoice();
    }

    private bool BelongsToPlayer(Collider other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    public void PlayVoice()
    {
        if (voiceEvent.IsNull)
        {
            Debug.LogWarning("[FMODVoiceTrigger] Voice event is not assigned.", this);
            return;
        }

        StopVoice();
        voiceInstance = RuntimeManager.CreateInstance(voiceEvent);
        FMOD.RESULT result = voiceInstance.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("[FMODVoiceTrigger] Failed to start " + voiceEvent.Path + ": " + result, this);
            voiceInstance.release();
            voiceInstance.clearHandle();
            return;
        }

        hasPlayed = true;
    }

    public void StopVoice()
    {
        if (!voiceInstance.isValid())
            return;

        voiceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        voiceInstance.release();
        voiceInstance.clearHandle();
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        StopVoice();
    }
}
