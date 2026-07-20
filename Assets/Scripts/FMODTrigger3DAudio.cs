using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FMODTrigger3DAudio : MonoBehaviour
{
    [Header("FMOD Event")]
    [SerializeField] private EventReference audioEvent;
    [SerializeField] private Transform soundSource;

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool playOnce;
    [SerializeField] private bool stopOnExit = true;
    [SerializeField] private bool allowFadeout = true;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
    private EventInstance instance;
    private bool hasPlayed;

    private void Awake()
    {
        if (soundSource == null)
            soundSource = transform;
    }

    private void Update()
    {
        if (instance.isValid())
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(soundSource));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!BelongsToPlayer(other) || !playerColliders.Add(other))
            return;

        if (playerColliders.Count == 1 && (!playOnce || !hasPlayed))
            PlayAudio();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other))
            return;

        if (stopOnExit && playerColliders.Count == 0)
            StopAudio();
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

    public void PlayAudio()
    {
        if (instance.isValid() || audioEvent.IsNull)
            return;

        instance = RuntimeManager.CreateInstance(audioEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(soundSource));

        FMOD.RESULT result = instance.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("[FMODTrigger3DAudio] Failed to start " + audioEvent.Path + ": " + result, this);
            instance.release();
            instance.clearHandle();
            return;
        }

        hasPlayed = true;
    }

    public void StopAudio()
    {
        if (!instance.isValid())
            return;

        instance.stop(allowFadeout
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
            : FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
        instance.clearHandle();
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        StopAudio();
    }
}
