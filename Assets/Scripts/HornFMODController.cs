using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class HornFMODController : MonoBehaviour
{
    [Header("FMOD Event")]
    public EventReference hornEvent;

    [Header("VR References")]
    public Transform playerHead;
    public Transform hornObject;

    [Header("Distance Rules")]
    public float playDistance = 0.45f;
    public float pauseDistance = 0.55f;
    public float maxDistance = 1.2f;

    [Header("Smoothing")]
    public float distanceSmoothing = 8f;
    public float angleSmoothing = 8f;

    [Header("Horn Glow")]
    public Renderer[] hornRenderers;
    public Color glowColor = new Color(1f, 0.75f, 0.2f);
    public float glowIntensity = 3f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public float debugInterval = 0.5f;

    private EventInstance hornInstance;

    private bool isActivated = false;
    private bool eventCreated = false;
    private bool isPlaying = false;

    private float smoothedDistance = 1.2f;
    private float smoothedAngle = 0f;

    private float debugTimer = 0f;

    private void Start()
    {
        Log("HornFMODController started on: " + gameObject.name);

        if (hornEvent.IsNull)
        {
            Debug.LogError("[HornFMODController] FMOD Event is not assigned.");
        }
        else
        {
            Log("FMOD Event assigned: " + hornEvent.Path);
        }

        if (playerHead == null)
        {
            Debug.LogWarning("[HornFMODController] Player Head is not assigned.");
        }
        else
        {
            Log("Player Head assigned: " + playerHead.name);
        }

        if (hornObject == null)
        {
            Debug.LogWarning("[HornFMODController] Horn Object is not assigned.");
        }
        else
        {
            Log("Horn Object assigned: " + hornObject.name);
        }

        if (hornRenderers == null || hornRenderers.Length == 0)
        {
            Debug.LogWarning("[HornFMODController] No Horn Renderers assigned. Glow will not be visible.");
        }
        else
        {
            Log("Horn Renderers assigned: " + hornRenderers.Length);
        }
    }

    private void Update()
    {
        if (!isActivated)
            return;

        if (!eventCreated)
            return;

        if (playerHead == null || hornObject == null)
        {
            Debug.LogWarning("[HornFMODController] Missing Player Head or Horn Object. Cannot update FMOD parameters.");
            return;
        }

        UpdateDistanceAndAngle();
        SendParametersToFMOD();
        HandlePlayPauseByDistance();

        FMOD.RESULT result = hornInstance.set3DAttributes(RuntimeUtils.To3DAttributes(hornObject));
        CheckFMODResult(result, "set3DAttributes");

        DebugRuntimeValues();
    }

    public void ActivateHorn()
    {
        Log("ActivateHorn() called.");

        if (isActivated)
        {
            Log("Horn is already activated. Ignoring ActivateHorn().");
            return;
        }

        isActivated = true;

        Log("Horn activated.");

        TurnOnGlow();
        CreateFMODEventIfNeeded();

        if (!eventCreated)
        {
            Debug.LogError("[HornFMODController] FMOD Event was not created. Cannot start.");
            return;
        }

        FMOD.RESULT startResult = hornInstance.start();
        CheckFMODResult(startResult, "hornInstance.start()");

        FMOD.RESULT pauseResult = hornInstance.setPaused(true);
        CheckFMODResult(pauseResult, "hornInstance.setPaused(true)");

        isPlaying = false;

        Log("FMOD Event started but paused. Waiting for horn to reach play distance.");
    }

    private void CreateFMODEventIfNeeded()
    {
        if (eventCreated)
        {
            Log("FMOD Event already created.");
            return;
        }

        Log("Creating FMOD Event instance...");

        hornInstance = RuntimeManager.CreateInstance(hornEvent);

        if (!hornInstance.isValid())
        {
            Debug.LogError("[HornFMODController] FMOD EventInstance is invalid. Check Event path and Banks.");
            return;
        }

        Log("FMOD EventInstance created successfully.");

        if (hornObject != null)
        {
            FMOD.RESULT result = hornInstance.set3DAttributes(RuntimeUtils.To3DAttributes(hornObject));
            CheckFMODResult(result, "Initial set3DAttributes");
        }

        eventCreated = true;
    }

    private void UpdateDistanceAndAngle()
    {
        float rawDistance = Vector3.Distance(hornObject.position, playerHead.position);
        rawDistance = Mathf.Clamp(rawDistance, 0f, maxDistance);

        Vector3 forward = hornObject.forward.normalized;
        float rawAngle = Mathf.Asin(Vector3.Dot(forward, Vector3.up)) * Mathf.Rad2Deg;
        rawAngle = Mathf.Clamp(rawAngle, -90f, 90f);

        smoothedDistance = Mathf.Lerp(smoothedDistance, rawDistance, Time.deltaTime * distanceSmoothing);
        smoothedAngle = Mathf.Lerp(smoothedAngle, rawAngle, Time.deltaTime * angleSmoothing);
    }

    private void SendParametersToFMOD()
    {
        FMOD.RESULT distanceResult = hornInstance.setParameterByName("DistanceToHead", smoothedDistance);
        CheckFMODResult(distanceResult, "setParameterByName DistanceToHead");

        FMOD.RESULT angleResult = hornInstance.setParameterByName("HornAngle", smoothedAngle);
        CheckFMODResult(angleResult, "setParameterByName HornAngle");
    }

    private void HandlePlayPauseByDistance()
    {
        if (!isPlaying && smoothedDistance <= playDistance)
        {
            Log("Distance is close enough. Starting playback. Distance = " + smoothedDistance.ToString("F3"));

            FMOD.RESULT result = hornInstance.setPaused(false);
            CheckFMODResult(result, "hornInstance.setPaused(false)");

            isPlaying = true;
        }

        if (isPlaying && smoothedDistance >= pauseDistance)
        {
            Log("Distance is too far. Pausing playback. Distance = " + smoothedDistance.ToString("F3"));

            FMOD.RESULT result = hornInstance.setPaused(true);
            CheckFMODResult(result, "hornInstance.setPaused(true)");

            isPlaying = false;
        }
    }

    private void TurnOnGlow()
    {
        Log("Turning on horn glow...");

        if (hornRenderers == null || hornRenderers.Length == 0)
        {
            Debug.LogWarning("[HornFMODController] No renderers assigned. Glow skipped.");
            return;
        }

        Color emission = glowColor * glowIntensity;

        foreach (Renderer renderer in hornRenderers)
        {
            if (renderer == null)
            {
                Debug.LogWarning("[HornFMODController] One renderer is null. Skipping.");
                continue;
            }

            foreach (Material mat in renderer.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);

                Log("Emission enabled on material: " + mat.name);
            }
        }

        Log("Horn glow finished.");
    }

    private void DebugRuntimeValues()
    {
        if (!showDebugLogs)
            return;

        debugTimer += Time.deltaTime;

        if (debugTimer < debugInterval)
            return;

        debugTimer = 0f;

        Debug.Log(
            "[HornFMODController] Runtime Values | " +
            "Activated: " + isActivated +
            " | EventCreated: " + eventCreated +
            " | IsPlaying: " + isPlaying +
            " | DistanceToHead: " + smoothedDistance.ToString("F3") +
            " | HornAngle: " + smoothedAngle.ToString("F2")
        );
    }

    private void CheckFMODResult(FMOD.RESULT result, string operation)
    {
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError("[HornFMODController] FMOD error during " + operation + ": " + result);
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[HornFMODController] " + message);
        }
    }

    private void OnDestroy()
    {
        StopAndReleaseEvent();
    }

    private void StopAndReleaseEvent()
    {
        if (!eventCreated)
            return;

        Log("Stopping and releasing FMOD Event.");

        FMOD.RESULT stopResult = hornInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        CheckFMODResult(stopResult, "hornInstance.stop()");

        FMOD.RESULT releaseResult = hornInstance.release();
        CheckFMODResult(releaseResult, "hornInstance.release()");

        eventCreated = false;
        isPlaying = false;
    }
}