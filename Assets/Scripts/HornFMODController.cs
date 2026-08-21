using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[System.Serializable]
public class HornMusicSegment
{
    public string segmentName = "Phrase";
    public int startTimeMs;
    public int endTimeMs = 6000;
    public float minAngle = -10f;
    public float maxAngle = 20f;
}

public class HornFMODController : MonoBehaviour
{
    public enum HornPlaybackMode
    {
        AngleSegmentMode,
        ShakeSpeedMode
    }

    [Header("FMOD Event")]
    public EventReference hornEvent;

    [Header("Audio 1 Volume - Controlled By Unity")]
    [Tooltip("FMOD parameter that controls only Audio 1 / main horn track volume. Create this parameter in FMOD and automate Audio 1 Volume with it.")]
    public string audio1VolumeParameterName = "Audio1Volume";

    [Tooltip("Maximum Audio 1 volume sent from Unity. 1 = normal volume.")]
    [Range(0f, 1f)]
    public float audio1Volume = 1f;

    [Tooltip("If true, Unity calculates Audio 1 volume from mouthpiece-to-head distance. If false, use Audio 1 Volume as a manual Inspector control.")]
    public bool useDistanceForAudio1Volume = false;

    [Tooltip("At or below this distance, Audio 1 reaches maximum volume.")]
    public float fullVolumeDistance = 0.04f;

    [Tooltip("At or beyond this distance, Audio 1 becomes silent.")]
    public float zeroVolumeDistance = 0.35f;

    [Tooltip("Smoothing speed for Unity-driven Audio 1 volume.")]
    public float volumeSmoothing = 8f;

    [Header("VR References")]
    public Transform playerHead;
    public Transform hornObject;

    [Header("Mouthpiece Pose")]
    public Transform mouthpieceTransform;
    public Transform instrumentTipTransform;
    public bool useHornLocalZAxisForAngle = true;
    public bool lockMouthpieceToHead = true;
    public Vector3 mouthpieceHeadLocalOffset = new Vector3(0f, -0.08f, 0.12f);
    public bool requireGrabBeforeMouthpieceLock = true;
    public float mouthpieceSnapDistance = 0.18f;
    public float mouthpieceUnsnapDistance = 0.25f;
    public XRGrabInteractable hornGrabInteractable;

    [Header("Segments")]
    public HornMusicSegment[] segments =
    {
        new HornMusicSegment
        {
            segmentName = "Phrase 1",
            startTimeMs = 0,
            endTimeMs = 6000,
            minAngle = -10f,
            maxAngle = 20f
        },
        new HornMusicSegment
        {
            segmentName = "Phrase 2",
            startTimeMs = 6000,
            endTimeMs = 12000,
            minAngle = 20f,
            maxAngle = 55f
        },
        new HornMusicSegment
        {
            segmentName = "Phrase 3",
            startTimeMs = 12000,
            endTimeMs = 18000,
            minAngle = -55f,
            maxAngle = -20f
        },
        new HornMusicSegment
        {
            segmentName = "Phrase 4",
            startTimeMs = 18000,
            endTimeMs = 26000,
            minAngle = 35f,
            maxAngle = 80f
        }
    };
    public int startSegmentIndex;

    [Header("Round Settings")]
    public int totalRounds = 4;
    public int currentRoundIndex;
    public HornPlaybackMode currentPlaybackMode;
    public UnityEvent onApplauseRequested;

    [Header("Shake Speed Mode - Rounds 2 to 4")]
    public Transform shakeTrackedObject;
    public Transform shakeReferenceHead;
    public float minSideShakeSpeed = 0.05f;
    public float maxSideShakeSpeed = 1.2f;
    public float minPlaybackRate = 0.4f;
    public float maxPlaybackRate = 1.4f;
    public float shakeSmoothing = 8f;
    public int wholeMusicStartMs;
    [Tooltip("0 uses the end time of the last existing segment.")]
    public int wholeMusicEndMs;

    [Header("Shake Speed Runtime")]
    [SerializeField] private float rawSideShakeSpeed;
    [SerializeField] private float shakeSpeed01;
    [SerializeField] private float currentPlaybackRate = 1f;

    [Header("Extra Layers")]
    [Tooltip("Temporarily enable every configured extra layer when the first shake-speed round starts.")]
    public bool unlockAllExtraLayersOnFirstSwingRound;
    public ExtraLayerUnlock[] extraLayers =
    {
        new ExtraLayerUnlock
        {
            layerName = "Extra Layer 1",
            unlockRoundIndex = 2,
            fmodParameterName = "ExtraLayer1Active"
        },
        new ExtraLayerUnlock
        {
            layerName = "Extra Layer 2",
            unlockRoundIndex = 3,
            fmodParameterName = "ExtraLayer2Active"
        },
        new ExtraLayerUnlock
        {
            layerName = "Extra Layer 3",
            unlockRoundIndex = 4,
            fmodParameterName = "ExtraLayer3Active"
        }
    };

    [Header("Tracking")]
    public bool invertAngle;

    [Header("Smoothing")]
    public float angleSmoothing = 8f;

    [Header("Horn Glow")]
    public Renderer[] hornRenderers;
    public Color glowColor = new Color(1f, 0.75f, 0.2f);
    public float glowIntensity = 3f;

    [Header("Guide UI")]
    public bool autoCreateGuideUI = true;
    public GameObject guidePanel;
    public Text segmentText;
    public Text mouthpieceStatusText;
    public Text angleRangeText;
    public Text statusText;
    public Slider angleSlider;
    public Image angleFillImage;
    public RectTransform angleTargetBand;
    public Color inRangeColor = new Color(0.2f, 0.8f, 0.35f);
    public Color outOfRangeColor = new Color(0.95f, 0.25f, 0.2f);
    public Color targetBandColor = new Color(1f, 0.8f, 0.2f, 0.35f);

    [Header("First Pickup Tutorial")]
    public bool playFirstPickupTutorial = true;
    public Texture2D firstPickupTutorialImage;
    public GameObject firstPickupTutorialCanvas;
    public RawImage firstPickupTutorialRawImage;
    public Text firstPickupTutorialInstructionText;
    public string firstPickupTutorialInstruction = "Tilt the horn up or down to find the right angle to play.";
    public float firstPickupTutorialDuration = 10f;
    public Vector3 firstPickupTutorialHeadLocalPosition = new Vector3(0f, 0f, 1.1f);
    public Vector2 firstPickupTutorialSize = new Vector2(600f, 900f);
    public float firstPickupTutorialInstructionWidth = 360f;

    [Header("Shake Speed Tutorial")]
    public bool playShakeSpeedTutorial = true;
    public Texture2D shakeSpeedTutorialImage;
    public string shakeSpeedTutorialInstruction = "Shake the horn left and right. Faster shakes make the music play faster.";
    public float shakeSpeedTutorialDuration = 10f;

    [Header("Mouthpiece Snap Prompt")]
    public bool autoCreateSnapPromptUI = true;
    public GameObject snapPromptCanvas;
    public Text snapPromptText;
    public string snapMatchedText = "MOUTHPIECE MATCH";
    public string snapSearchText = "ALIGN HORN TO MOUTH";
    public Vector3 snapPromptHeadLocalPosition = new Vector3(0f, 0.22f, 1.15f);
    public Vector2 snapPromptSize = new Vector2(480f, 80f);
    public int snapPromptFontSize = 30;
    public Color snapMatchedColor = new Color(0.25f, 0.9f, 0.45f);
    public Color snapSearchColor = new Color(1f, 0.82f, 0.22f);

    [Header("Debug")]
    public bool showDebugLogs = true;
    public float debugInterval = 0.5f;

    private EventInstance hornInstance;
    private bool isActivated;
    private bool eventCreated;
    private bool isPlaying;
    private bool isComplete;
    private int currentSegmentIndex;
    private float smoothedAngle;
    private float debugTimer;
    private bool isHornHeld;
    private bool isMouthpieceSnapped;
    private float currentAudio1Volume = 0f;
    private float lastAppliedAudio1Volume = -1f;
    private float lastAudio1VolumeDistance = 0f;
    private Vector3 previousShakePosition;
    private float manualTimelinePositionMs;
    private RectTransform anglePointer;
    private NarrationManager narrationManager;
    private bool hasShownFirstPickupTutorial;
    private bool hasPlayedHornIntroVoiceOver;
    private Coroutine firstPickupTutorialRoutine;
    private bool hasShownShakeSpeedTutorial;
    private Coroutine shakeSpeedTutorialRoutine;
    private Texture2D activeTutorialImage;
    private string activeTutorialInstruction;

    private bool IsFirstPickupTutorialBlocking =>
        playFirstPickupTutorial && !hasShownFirstPickupTutorial;

    private bool IsTutorialRunning =>
        firstPickupTutorialRoutine != null ||
        shakeSpeedTutorialRoutine != null;

    private HornMusicSegment CurrentSegment
    {
        get
        {
            if (segments == null || currentSegmentIndex < 0 || currentSegmentIndex >= segments.Length)
                return null;

            return segments[currentSegmentIndex];
        }
    }

    private void Awake()
    {
        CacheGrabInteractableIfNeeded();
        narrationManager = FindFirstObjectByType<NarrationManager>(
            FindObjectsInactive.Include
        );
    }

    private void OnValidate()
    {
        totalRounds = Mathf.Max(1, totalRounds);

        if (extraLayers == null)
            return;

        for (int i = 0; i < extraLayers.Length; i++)
        {
            if (extraLayers[i] != null)
                extraLayers[i].unlockRoundIndex = i + 1;
        }
    }

    private void OnEnable()
    {
        CacheGrabInteractableIfNeeded();

        if (hornGrabInteractable != null)
        {
            hornGrabInteractable.selectEntered.AddListener(OnHornSelectEntered);
            hornGrabInteractable.selectExited.AddListener(OnHornSelectExited);
            SyncGrabState();
        }
    }

    private void OnDisable()
    {
        if (hornGrabInteractable != null)
        {
            hornGrabInteractable.selectEntered.RemoveListener(OnHornSelectEntered);
            hornGrabInteractable.selectExited.RemoveListener(OnHornSelectExited);
        }

        StopFirstPickupTutorial();
        StopShakeSpeedTutorial();
    }

    private void Start()
    {
        EnsureSegments();
        Log("HornFMODController started on: " + gameObject.name);

        if (hornEvent.IsNull)
            Debug.LogError("[HornFMODController] FMOD Event is not assigned.");

        if (playerHead == null)
            Debug.LogWarning("[HornFMODController] Player Head is not assigned.");

        if (hornObject == null)
            Debug.LogWarning("[HornFMODController] Horn Object is not assigned.");

        if (lockMouthpieceToHead && mouthpieceTransform == null)
            Debug.LogWarning("[HornFMODController] Mouthpiece Transform is not assigned. Horn Object pivot will be used as the mouthpiece fallback.");

        if (instrumentTipTransform == null)
            Debug.LogWarning("[HornFMODController] Instrument Tip Transform is not assigned. Horn Object forward will be used for angle fallback.");

        if (requireGrabBeforeMouthpieceLock && hornGrabInteractable == null)
            Debug.LogWarning("[HornFMODController] Horn Grab Interactable is not assigned. Mouthpiece lock will wait forever unless this is assigned.");

        CacheHornRenderersIfNeeded();

        if (!isActivated)
        {
            SetGuideVisible(false);
            SetSnapPromptVisible(false);
        }
    }

    private void LateUpdate()
    {
        if (IsTutorialRunning)
        {
            if (isPlaying)
                PauseEvent();

            return;
        }

        if (!isActivated || isComplete || !eventCreated)
            return;

        if (playerHead == null || hornObject == null)
            return;

        ApplyMouthpieceLock();
        UpdateAngle();
        ApplyAudio1VolumeFromUnity();
        SendParametersToFMOD();
        Update3DPosition();

        bool mouthpieceOK = IsMouthpieceOK();
        if (lockMouthpieceToHead)
            UpdateSnapPrompt(mouthpieceOK);
        else
            SetSnapPromptVisible(false);

        if (currentPlaybackMode == HornPlaybackMode.AngleSegmentMode)
        {
            if (CurrentSegment == null)
                return;

            bool angleOK = IsAngleInRange(CurrentSegment);
            HandlePlayPause(mouthpieceOK && angleOK);
            CheckSegmentEnd();
            UpdateGuideUI(mouthpieceOK, angleOK);
            DebugRuntimeValues(mouthpieceOK, angleOK);
        }
        else
        {
            UpdateShakeSpeedMode(mouthpieceOK);
            DebugRuntimeValues(mouthpieceOK, true);
        }
    }

    public void ActivateHorn()
    {
        Log("ActivateHorn() called.");
        EnsureSegments();

        if (IsFirstPickupTutorialBlocking)
        {
            Log("First pickup tutorial is pending. Horn activation delayed.");
            return;
        }

        if (isComplete)
        {
            Log("Horn interaction is already complete. Ignoring ActivateHorn().");
            return;
        }

        if (isActivated)
        {
            Log("Horn already activated.");
            return;
        }

        if (segments == null || segments.Length == 0)
        {
            Debug.LogError("[HornFMODController] Cannot activate: no music segments are configured.");
            return;
        }

        isActivated = true;
        isPlaying = false;
        totalRounds = Mathf.Max(1, totalRounds);
        currentRoundIndex = 0;
        currentSegmentIndex = Mathf.Clamp(startSegmentIndex, 0, segments.Length - 1);

        CacheHornRenderersIfNeeded();
        TurnOnGlow();
        EnsureGuideUI();
        EnsureSnapPromptUI();
        CreateAndPrimeFMODEvent();

        if (!eventCreated)
            return;

        ResetExtraLayers();
        StartRound(0);
        SetGuideVisible(isHornHeld);

        Log("Horn activated. FMOD event is paused at the current segment start.");
    }

    public void SetPlayerHead(Transform head)
    {
        playerHead = head;
    }

    public void ActivateHornAndShowCanvas()
    {
        ActivateHorn();

        if (isActivated)
        {
            SetGuideVisible(true);
        }
    }

    private void EnsureSegments()
    {
        if (segments != null && segments.Length > 0)
            return;

        segments = new HornMusicSegment[]
        {
            new HornMusicSegment
            {
                segmentName = "Phrase 1",
                startTimeMs = 0,
                endTimeMs = 6000,
                minAngle = -10f,
                maxAngle = 20f
            },
            new HornMusicSegment
            {
                segmentName = "Phrase 2",
                startTimeMs = 6000,
                endTimeMs = 12000,
                minAngle = 20f,
                maxAngle = 55f
            },
            new HornMusicSegment
            {
                segmentName = "Phrase 3",
                startTimeMs = 12000,
                endTimeMs = 18000,
                minAngle = -55f,
                maxAngle = -20f
            },
            new HornMusicSegment
            {
                segmentName = "Phrase 4",
                startTimeMs = 18000,
                endTimeMs = 26000,
                minAngle = 35f,
                maxAngle = 80f
            }
        };
    }

    private void CreateAndPrimeFMODEvent()
    {
        if (eventCreated)
            return;

        hornInstance = RuntimeManager.CreateInstance(hornEvent);

        if (!hornInstance.isValid())
        {
            Debug.LogError("[HornFMODController] FMOD EventInstance is invalid. Check event path, bank assignment, and built banks.");
            return;
        }

        eventCreated = true;

        // Keep the whole FMOD event at normal volume.
        // Audio 1 is controlled separately through the Audio1Volume FMOD parameter.
        CheckFMODResult(hornInstance.setVolume(1f), "set event master volume to 1");
        ApplyAudio1VolumeFromUnity(true);

        CheckFMODResult(hornInstance.start(), "start");
        CheckFMODResult(hornInstance.setPaused(true), "initial setPaused(true)");
        Update3DPosition();
        Log("FMOD Event created and started in paused state.");
    }

    private void ApplyAudio1VolumeFromUnity(bool force = false)
    {
        if (!eventCreated)
            return;

        if (string.IsNullOrEmpty(audio1VolumeParameterName))
        {
            Debug.LogWarning("[HornFMODController] Audio 1 volume parameter name is empty.");
            return;
        }

        float targetVolume = Mathf.Clamp01(audio1Volume);

        if (useDistanceForAudio1Volume)
        {
            lastAudio1VolumeDistance = GetMouthpieceDistanceToHeadAnchor();

            // Distance close to fullVolumeDistance => 1.
            // Distance close to zeroVolumeDistance => 0.
            targetVolume = Mathf.InverseLerp(zeroVolumeDistance, fullVolumeDistance, lastAudio1VolumeDistance);
            targetVolume = Mathf.Clamp01(targetVolume) * Mathf.Clamp01(audio1Volume);
        }

        currentAudio1Volume = force
            ? targetVolume
            : Mathf.Lerp(currentAudio1Volume, targetVolume, Time.deltaTime * volumeSmoothing);

        if (!force && Mathf.Abs(currentAudio1Volume - lastAppliedAudio1Volume) < 0.001f)
            return;

        lastAppliedAudio1Volume = currentAudio1Volume;
        CheckFMODResult(
            hornInstance.setParameterByName(audio1VolumeParameterName, currentAudio1Volume),
            "setParameterByName " + audio1VolumeParameterName
        );
    }

    private float GetMouthpieceDistanceToHeadAnchor()
    {
        if (playerHead == null)
            return zeroVolumeDistance;

        return Vector3.Distance(GetMouthpiecePosition(), GetMouthpieceAnchorPosition());
    }

    private void ResetExtraLayers()
    {
        if (extraLayers == null)
            return;

        for (int i = 0; i < extraLayers.Length; i++)
        {
            ExtraLayerUnlock layer = extraLayers[i];

            if (layer == null)
                continue;

            layer.unlocked = false;
            layer.activated = false;

            if (layer.triggerObject != null)
                layer.triggerObject.SetActive(false);

            if (layer.newItemObject != null)
                layer.newItemObject.SetActive(false);

            if (eventCreated)
                SetExtraLayerParameter(layer, 0f, "Reset " + layer.fmodParameterName);
        }

        Log("All extra layers reset.");
    }

    private void StartRound(int roundIndex)
    {
        currentRoundIndex = roundIndex;
        currentSegmentIndex = 0;

        if (currentRoundIndex == 0)
        {
            currentPlaybackMode = HornPlaybackMode.AngleSegmentMode;
            JumpToCurrentSegmentStart();
            PauseEvent();
            SetGuideSegment();
            Log("Round 1 started: existing angle segment mode preserved.");
            return;
        }

        currentPlaybackMode = HornPlaybackMode.ShakeSpeedMode;

        if (roundIndex == 1 &&
            playShakeSpeedTutorial &&
            !hasShownShakeSpeedTutorial)
        {
            StartShakeSpeedTutorial();
            return;
        }

        RevealItemAndUnlockLayerForRound(currentRoundIndex);
        JumpToWholeMusicStart();
        InitializeShakeTracking();
        PauseEvent();
        UpdateShakeGuide();

        Log(
            "Round " + (currentRoundIndex + 1) +
            " started: shake speed mode."
        );
    }

    private void RevealItemAndUnlockLayerForRound(int roundIndex)
    {
        if (unlockAllExtraLayersOnFirstSwingRound && roundIndex == 1)
        {
            if (extraLayers == null)
                return;

            for (int i = 0; i < extraLayers.Length; i++)
                RevealItemAndUnlockLayer(i);

            return;
        }

        int layerIndex = roundIndex - 1;

        if (extraLayers == null ||
            layerIndex < 0 ||
            layerIndex >= extraLayers.Length)
        {
            Debug.LogWarning(
                "[HornFMODController] No extra layer configured for round " +
                (roundIndex + 1) + ".",
                this
            );
            return;
        }

        RevealItemAndUnlockLayer(layerIndex);
    }

    private void RevealItemAndUnlockLayer(int layerIndex)
    {
        ExtraLayerUnlock layer = extraLayers[layerIndex];

        if (layer == null)
            return;

        if (layer.newItemObject != null)
            layer.newItemObject.SetActive(true);

        layer.unlocked = true;

        if (eventCreated &&
            SetExtraLayerParameter(
                layer,
                1f,
                "Unlock " + layer.fmodParameterName
            ))
        {
            layer.activated = true;
        }

        Log("Revealed item and unlocked layer: " + layer.layerName);
    }

    private void JumpToWholeMusicStart()
    {
        manualTimelinePositionMs = wholeMusicStartMs;

        if (!eventCreated)
            return;

        CheckFMODResult(
            hornInstance.setTimelinePosition(wholeMusicStartMs),
            "setTimelinePosition to whole music start"
        );
    }

    private void InitializeShakeTracking()
    {
        if (shakeTrackedObject == null)
            shakeTrackedObject = hornObject;

        if (shakeReferenceHead == null)
            shakeReferenceHead = playerHead;

        previousShakePosition = shakeTrackedObject != null
            ? shakeTrackedObject.position
            : Vector3.zero;

        rawSideShakeSpeed = 0f;
        shakeSpeed01 = 0f;
        currentPlaybackRate = minPlaybackRate;
    }

    private void UpdateShakeSpeedMode(bool mouthpieceOK)
    {
        UpdateShakeSpeed();
        HandlePlayPause(mouthpieceOK);

        if (!mouthpieceOK)
        {
            UpdateShakeGuide();
            return;
        }

        AdvanceTimelineByShakeSpeed();
        CheckWholeMusicEnd();
        UpdateShakeGuide();
    }

    private void UpdateShakeSpeed()
    {
        if (shakeTrackedObject == null || shakeReferenceHead == null)
            return;

        Vector3 currentPosition = shakeTrackedObject.position;
        Vector3 velocity =
            (currentPosition - previousShakePosition) /
            Mathf.Max(Time.deltaTime, 0.0001f);

        rawSideShakeSpeed = Mathf.Abs(
            Vector3.Dot(velocity, shakeReferenceHead.right)
        );

        float target01 = Mathf.Clamp01(
            Mathf.InverseLerp(
                minSideShakeSpeed,
                maxSideShakeSpeed,
                rawSideShakeSpeed
            )
        );

        shakeSpeed01 = Mathf.Lerp(
            shakeSpeed01,
            target01,
            Time.deltaTime * shakeSmoothing
        );

        currentPlaybackRate = Mathf.Lerp(
            minPlaybackRate,
            maxPlaybackRate,
            shakeSpeed01
        );

        previousShakePosition = currentPosition;
    }

    private void AdvanceTimelineByShakeSpeed()
    {
        manualTimelinePositionMs +=
            Time.deltaTime * 1000f * currentPlaybackRate;

        int resolvedEndMs = GetWholeMusicEndMs();

        manualTimelinePositionMs = Mathf.Min(
            manualTimelinePositionMs,
            resolvedEndMs
        );

        CheckFMODResult(
            hornInstance.setTimelinePosition(
                Mathf.RoundToInt(manualTimelinePositionMs)
            ),
            "setTimelinePosition from shake speed"
        );
    }

    private void CheckWholeMusicEnd()
    {
        if (manualTimelinePositionMs < GetWholeMusicEndMs())
            return;

        GoToNextRoundOrComplete();
    }

    private int GetWholeMusicEndMs()
    {
        if (wholeMusicEndMs > wholeMusicStartMs)
            return wholeMusicEndMs;

        if (segments != null && segments.Length > 0)
            return Mathf.Max(wholeMusicStartMs, segments[segments.Length - 1].endTimeMs);

        return wholeMusicStartMs;
    }

    private void UpdateShakeGuide()
    {
        ConfigureGuideForShakeSpeed();

        if (segmentText != null)
        {
            segmentText.text =
                "Round " + (currentRoundIndex + 1) + "/" + totalRounds +
                " - Shake Speed Mode";
        }

        if (angleRangeText != null)
        {
            angleRangeText.text =
                "Side speed: " + rawSideShakeSpeed.ToString("F2") +
                " m/s | Playback: " + currentPlaybackRate.ToString("F2") + "x";
        }

        if (statusText != null)
        {
            statusText.text = "Shake left and right";
        }

        if (angleSlider != null)
            angleSlider.value = shakeSpeed01;

        if (angleFillImage != null)
            angleFillImage.color = inRangeColor;

        UpdateSpeedPointerPosition(shakeSpeed01);
    }

    private void UpdateRoundState()
    {
        Log("Current Round: " + (currentRoundIndex + 1) + " / " + totalRounds);

        if (extraLayers == null)
            return;

        for (int i = 0; i < extraLayers.Length; i++)
        {
            ExtraLayerUnlock layer = extraLayers[i];

            if (layer == null)
                continue;

            if (!layer.unlocked && currentRoundIndex >= layer.unlockRoundIndex)
                UnlockExtraLayer(i);
        }
    }

    private void UnlockExtraLayer(int layerIndex)
    {
        if (extraLayers == null || layerIndex < 0 || layerIndex >= extraLayers.Length)
            return;

        ExtraLayerUnlock layer = extraLayers[layerIndex];

        if (layer == null)
            return;

        layer.unlocked = true;

        if (layer.triggerObject != null)
            layer.triggerObject.SetActive(true);

        TurnOnExtraLayerGlow(layer);
        Log("Unlocked extra layer: " + layer.layerName);
    }

    private void TurnOnExtraLayerGlow(ExtraLayerUnlock layer)
    {
        if (layer.glowRenderers == null || layer.glowRenderers.Length == 0)
            return;

        Color emission = layer.glowColor * layer.glowIntensity;

        foreach (Renderer glowRenderer in layer.glowRenderers)
        {
            if (glowRenderer == null)
                continue;

            foreach (Material mat in glowRenderer.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }
    }

    public bool ActivateExtraLayer(int layerIndex)
    {
        if (extraLayers == null || layerIndex < 0 || layerIndex >= extraLayers.Length)
        {
            Debug.LogError("[HornFMODController] Invalid extra layer index: " + layerIndex);
            return false;
        }

        ExtraLayerUnlock layer = extraLayers[layerIndex];

        if (layer == null)
        {
            Debug.LogError("[HornFMODController] Extra layer is missing at index: " + layerIndex);
            return false;
        }

        if (!layer.unlocked)
        {
            Log("Layer touched but not unlocked yet: " + layer.layerName);
            return false;
        }

        if (layer.activated)
        {
            Log("Layer already activated: " + layer.layerName);
            return false;
        }

        if (!eventCreated)
        {
            Debug.LogWarning("[HornFMODController] FMOD event not created. Cannot activate layer.");
            return false;
        }

        if (!SetExtraLayerParameter(layer, 1f, "Activate " + layer.fmodParameterName))
            return false;

        layer.activated = true;

        if (layer.triggerObject != null)
            layer.triggerObject.SetActive(false);

        Log("Activated extra audio layer: " + layer.layerName);
        return true;
    }

    public bool SetExtraLayerActive(int layerIndex, bool active)
    {
        if (extraLayers == null || layerIndex < 0 || layerIndex >= extraLayers.Length)
        {
            Debug.LogError("[HornFMODController] Invalid extra layer index: " + layerIndex);
            return false;
        }

        ExtraLayerUnlock layer = extraLayers[layerIndex];
        if (layer == null)
        {
            Debug.LogError("[HornFMODController] Extra layer is missing at index: " + layerIndex);
            return false;
        }

        bool startLayerPlayback = !eventCreated;
        if (startLayerPlayback)
        {
            CreateAndPrimeFMODEvent();
            if (!eventCreated)
                return false;

            CheckFMODResult(
                hornInstance.setParameterByName(audio1VolumeParameterName, 0f),
                "mute Audio1Volume for object layers"
            );
        }

        float value = active ? 1f : 0f;
        if (!SetExtraLayerParameter(layer, value, "Set " + layer.fmodParameterName))
            return false;

        layer.activated = active;

        if (startLayerPlayback)
        {
            CheckFMODResult(
                hornInstance.setPaused(false),
                "start object layer playback"
            );
            isPlaying = true;
        }

        Log((active ? "Activated " : "Deactivated ") + layer.layerName);
        return true;
    }

    private bool SetExtraLayerParameter(ExtraLayerUnlock layer, float value, string operation)
    {
        if (string.IsNullOrEmpty(layer.fmodParameterName))
        {
            Debug.LogWarning("[HornFMODController] Extra layer FMOD parameter is empty for: " + layer.layerName);
            return false;
        }

        FMOD.RESULT result = hornInstance.setParameterByName(layer.fmodParameterName, value);
        CheckFMODResult(result, operation);

        return result == FMOD.RESULT.OK;
    }

    private void JumpToCurrentSegmentStart()
    {
        if (!eventCreated || CurrentSegment == null)
            return;

        CheckFMODResult(hornInstance.setTimelinePosition(CurrentSegment.startTimeMs), "setTimelinePosition to segment start");
        Log("Jumped to segment " + (currentSegmentIndex + 1) + ": " + CurrentSegment.segmentName + ", start = " + CurrentSegment.startTimeMs + " ms");
    }

    private void UpdateAngle()
    {
        Vector3 instrumentAxis = GetInstrumentAxis();
        float horizontalLength = new Vector2(instrumentAxis.x, instrumentAxis.z).magnitude;
        float rawAngle = Mathf.Atan2(instrumentAxis.y, horizontalLength) * Mathf.Rad2Deg;

        if (invertAngle)
            rawAngle = -rawAngle;

        rawAngle = Mathf.Clamp(rawAngle, -90f, 90f);

        smoothedAngle = Mathf.Lerp(smoothedAngle, rawAngle, Time.deltaTime * angleSmoothing);
    }

    private void ApplyMouthpieceLock()
    {
        if (!lockMouthpieceToHead)
        {
            isMouthpieceSnapped = true;
            return;
        }

        if (hornObject == null || playerHead == null)
            return;

        if (requireGrabBeforeMouthpieceLock && !isHornHeld)
        {
            isMouthpieceSnapped = false;
            return;
        }

        Vector3 targetPosition = GetMouthpieceAnchorPosition();
        Vector3 currentMouthpiecePosition = GetMouthpiecePosition();
        float distanceToAnchor =
            Vector3.Distance(
                currentMouthpiecePosition,
                targetPosition
            );

        if (isMouthpieceSnapped)
        {
            float releaseDistance = Mathf.Max(
                mouthpieceSnapDistance,
                mouthpieceUnsnapDistance
            );

            if (distanceToAnchor > releaseDistance)
            {
                isMouthpieceSnapped = false;
                Log(
                    "Mouthpiece moved beyond the unsnap distance. Music disabled."
                );
                return;
            }
        }
        else
        {
            if (distanceToAnchor > mouthpieceSnapDistance)
                return;

            isMouthpieceSnapped = true;
            Log("Mouthpiece snapped to head anchor.");
        }

        hornObject.position += targetPosition - currentMouthpiecePosition;
    }

    private void CacheGrabInteractableIfNeeded()
    {
        if (hornGrabInteractable != null)
            return;

        hornGrabInteractable = GetComponent<XRGrabInteractable>();

        if (hornGrabInteractable == null && hornObject != null)
            hornGrabInteractable = hornObject.GetComponentInParent<XRGrabInteractable>();
    }

    private void SyncGrabState()
    {
        isHornHeld = hornGrabInteractable != null && hornGrabInteractable.isSelected;

        if (!isHornHeld)
            isMouthpieceSnapped = false;
    }

    private void OnHornSelectEntered(SelectEnterEventArgs args)
    {
        if (isComplete)
            return;

        isHornHeld = true;
        isMouthpieceSnapped = false;

        if (!hasPlayedHornIntroVoiceOver)
        {
            hasPlayedHornIntroVoiceOver = true;
            narrationManager?.PlayHornIntroVoiceOver();
        }

        if (playFirstPickupTutorial &&
            !hasShownFirstPickupTutorial)
        {
            StartFirstPickupTutorial();
            return;
        }

        if (currentPlaybackMode == HornPlaybackMode.ShakeSpeedMode &&
            playShakeSpeedTutorial &&
            !hasShownShakeSpeedTutorial)
        {
            StartShakeSpeedTutorial();
            return;
        }

        ActivateHorn();
        SetGuideVisible(true);
        SetSnapPromptVisible(lockMouthpieceToHead);
        if (lockMouthpieceToHead)
            UpdateSnapPrompt(false);
        Log("Horn picked up.");
    }

    private void OnHornSelectExited(SelectExitEventArgs args)
    {
        SyncGrabState();
        SetGuideVisible(false);
        SetSnapPromptVisible(false);
        StopFirstPickupTutorial();
        StopShakeSpeedTutorial();
        Log("Horn released.");
    }

    private Vector3 GetMouthpieceAnchorPosition()
    {
        if (playerHead == null)
            return Vector3.zero;

        return playerHead.TransformPoint(mouthpieceHeadLocalOffset);
    }

    private Vector3 GetMouthpiecePosition()
    {
        if (mouthpieceTransform != null)
            return mouthpieceTransform.position;

        if (hornObject != null)
            return hornObject.position;

        return Vector3.zero;
    }

    private Vector3 GetInstrumentAxis()
    {
        if (useHornLocalZAxisForAngle && hornObject != null)
            return hornObject.forward.normalized;

        if (mouthpieceTransform != null && instrumentTipTransform != null)
        {
            Vector3 axis = instrumentTipTransform.position - mouthpieceTransform.position;
            if (axis.sqrMagnitude > 0.0001f)
                return axis.normalized;
        }

        if (hornObject != null)
            return hornObject.forward.normalized;

        return Vector3.forward;
    }

    private void SendParametersToFMOD()
    {
        // DistanceToHead has been removed from FMOD volume control.
        // Unity now controls Audio 1 volume through the Audio1Volume parameter.
        CheckFMODResult(hornInstance.setParameterByName("HornAngle", smoothedAngle), "setParameterByName HornAngle");
    }

    private void Update3DPosition()
    {
        if (!eventCreated || hornObject == null)
            return;

        CheckFMODResult(hornInstance.set3DAttributes(RuntimeUtils.To3DAttributes(hornObject)), "set3DAttributes");
    }

    private bool IsAngleInRange(HornMusicSegment segment)
    {
        return smoothedAngle >= segment.minAngle && smoothedAngle <= segment.maxAngle;
    }

    private void HandlePlayPause(bool postureOK)
    {
        if (postureOK && !isPlaying)
        {
            ResumeEvent();
        }
        else if (!postureOK && isPlaying)
        {
            PauseEvent();
        }
    }

    private void ResumeEvent()
    {
        CheckFMODResult(hornInstance.setPaused(false), "setPaused(false)");
        isPlaying = true;
        Log("Playback resumed.");
    }

    private void PauseEvent()
    {
        CheckFMODResult(hornInstance.setPaused(true), "setPaused(true)");
        isPlaying = false;
        Log("Playback paused.");
    }

    private void CheckSegmentEnd()
    {
        if (CurrentSegment == null)
            return;

        FMOD.RESULT result = hornInstance.getTimelinePosition(out int timelinePosition);
        CheckFMODResult(result, "getTimelinePosition");

        if (result != FMOD.RESULT.OK || timelinePosition < CurrentSegment.endTimeMs)
            return;

        Log("Segment ended: " + CurrentSegment.segmentName + ", timeline = " + timelinePosition + " ms");
        GoToNextSegment();
    }

    private void GoToNextSegment()
    {
        PauseEvent();
        currentSegmentIndex++;

        if (currentSegmentIndex >= segments.Length)
        {
            GoToNextRoundOrComplete();
            return;
        }

        JumpToCurrentSegmentStart();
        SetGuideSegment();
        Log("Moved to next segment: " + CurrentSegment.segmentName);
    }

    private void GoToNextRoundOrComplete()
    {
        currentRoundIndex++;

        if (currentRoundIndex == 1)
        {
            onApplauseRequested?.Invoke();
            Log(
                "First performance completed. FMOD applause requested."
            );
        }

        if (currentRoundIndex >= totalRounds)
        {
            if (currentRoundIndex != 1)
                onApplauseRequested?.Invoke();

            Log(
                "Final performance completed. FMOD applause requested."
            );
            CompleteExperience();
            return;
        }

        StartRound(currentRoundIndex);
    }

    private void CompleteExperience()
    {
        isComplete = true;
        isActivated = false;
        isPlaying = false;
        isHornHeld = false;
        isMouthpieceSnapped = false;

        if (statusText != null)
            statusText.text = "Complete";

        HideExtraLayerTriggers();
        SetGuideVisible(false);
        StopAndReleaseEvent();

        Log("All segments complete.");
    }

    private void HideExtraLayerTriggers()
    {
        if (extraLayers == null)
            return;

        foreach (ExtraLayerUnlock layer in extraLayers)
        {
            if (layer != null && layer.triggerObject != null)
                layer.triggerObject.SetActive(false);
        }
    }

    private void CacheHornRenderersIfNeeded()
    {
        if ((hornRenderers == null || hornRenderers.Length == 0) && hornObject != null)
            hornRenderers = hornObject.GetComponentsInChildren<Renderer>();
    }

    private void TurnOnGlow()
    {
        if (hornRenderers == null || hornRenderers.Length == 0)
        {
            Debug.LogWarning("[HornFMODController] No horn renderers assigned. Glow skipped.");
            return;
        }

        Color emission = glowColor * glowIntensity;

        foreach (Renderer hornRenderer in hornRenderers)
        {
            if (hornRenderer == null)
                continue;

            foreach (Material mat in hornRenderer.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }

        Log("Horn glow enabled.");
    }

    private void EnsureGuideUI()
    {
        if (guidePanel != null || !autoCreateGuideUI)
            return;

        CreateAutoGuideUI();
    }

    private void StartFirstPickupTutorial()
    {
        StopFirstPickupTutorial();
        activeTutorialImage = firstPickupTutorialImage;
        activeTutorialInstruction = firstPickupTutorialInstruction;
        EnsureFirstPickupTutorialUI();
        UpdateFirstPickupTutorialUI();
        SetGuideVisible(false);
        SetSnapPromptVisible(false);
        SetFirstPickupTutorialVisible(true);

        firstPickupTutorialRoutine = StartCoroutine(
            FirstPickupTutorialRoutine()
        );
    }

    private IEnumerator FirstPickupTutorialRoutine()
    {
        if (firstPickupTutorialDuration > 0f)
            yield return new WaitForSecondsRealtime(firstPickupTutorialDuration);

        hasShownFirstPickupTutorial = true;
        firstPickupTutorialRoutine = null;
        SetFirstPickupTutorialVisible(false);

        if (!isHornHeld || isComplete)
            yield break;

        ActivateHorn();
        SetGuideVisible(true);
        SetSnapPromptVisible(lockMouthpieceToHead);
        if (lockMouthpieceToHead)
            UpdateSnapPrompt(false);
    }

    private void StopFirstPickupTutorial()
    {
        if (firstPickupTutorialRoutine != null)
        {
            StopCoroutine(firstPickupTutorialRoutine);
            firstPickupTutorialRoutine = null;
        }

        SetFirstPickupTutorialVisible(false);
    }

    private void StartShakeSpeedTutorial()
    {
        StopShakeSpeedTutorial();
        activeTutorialImage = shakeSpeedTutorialImage;
        activeTutorialInstruction = shakeSpeedTutorialInstruction;
        EnsureFirstPickupTutorialUI();
        UpdateFirstPickupTutorialUI();
        SetGuideVisible(false);
        SetSnapPromptVisible(false);
        PauseEvent();
        SetFirstPickupTutorialVisible(true);

        shakeSpeedTutorialRoutine = StartCoroutine(
            ShakeSpeedTutorialRoutine()
        );
    }

    private IEnumerator ShakeSpeedTutorialRoutine()
    {
        if (shakeSpeedTutorialDuration > 0f)
            yield return new WaitForSecondsRealtime(shakeSpeedTutorialDuration);

        hasShownShakeSpeedTutorial = true;
        shakeSpeedTutorialRoutine = null;
        SetFirstPickupTutorialVisible(false);

        if (isComplete)
            yield break;

        StartRound(currentRoundIndex);
        SetGuideVisible(isHornHeld);
        SetSnapPromptVisible(lockMouthpieceToHead && isHornHeld);
        if (lockMouthpieceToHead)
            UpdateSnapPrompt(false);
    }

    private void StopShakeSpeedTutorial()
    {
        if (shakeSpeedTutorialRoutine != null)
        {
            StopCoroutine(shakeSpeedTutorialRoutine);
            shakeSpeedTutorialRoutine = null;
        }

        SetFirstPickupTutorialVisible(false);
    }

    private void EnsureFirstPickupTutorialUI()
    {
        if (firstPickupTutorialRawImage != null)
            return;

        Canvas canvas = new GameObject(
            "HornFirstPickupTutorialCanvas",
            typeof(Canvas),
            typeof(CanvasScaler)
        ).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one * 0.0015f;

        if (playerHead != null)
        {
            canvas.transform.SetParent(playerHead, false);
            canvas.transform.localPosition =
                firstPickupTutorialHeadLocalPosition;
            canvas.transform.localRotation = Quaternion.identity;
        }

        firstPickupTutorialCanvas = canvas.gameObject;

        RawImage image = new GameObject(
            "HornFirstPickupTutorialImage",
            typeof(RawImage)
        ).GetComponent<RawImage>();
        image.transform.SetParent(canvas.transform, false);
        image.texture = activeTutorialImage != null
            ? activeTutorialImage
            : firstPickupTutorialImage;
        image.color = Color.white;
        image.raycastTarget = false;

        RectTransform imageRect = image.GetComponent<RectTransform>();
        Stretch(imageRect);

        firstPickupTutorialRawImage = image;

        Text instruction = new GameObject(
            "HornFirstPickupTutorialInstruction",
            typeof(Text)
        ).GetComponent<Text>();
        instruction.transform.SetParent(canvas.transform, false);
        instruction.font = GetDefaultFont();
        instruction.fontSize = snapPromptFontSize;
        instruction.fontStyle = FontStyle.Bold;
        instruction.alignment = TextAnchor.MiddleCenter;
        instruction.horizontalOverflow = HorizontalWrapMode.Wrap;
        instruction.verticalOverflow = VerticalWrapMode.Overflow;
        instruction.color = Color.white;
        instruction.text = activeTutorialInstruction;
        instruction.raycastTarget = false;

        RectTransform instructionRect =
            instruction.GetComponent<RectTransform>();
        instructionRect.anchorMin = Vector2.zero;
        instructionRect.anchorMax = new Vector2(0f, 1f);
        instructionRect.pivot = new Vector2(0f, 0.5f);
        instructionRect.offsetMin = Vector2.zero;

        firstPickupTutorialInstructionText = instruction;
        ConfigureTutorialLayout();
        SetFirstPickupTutorialVisible(false);
    }

    private void UpdateFirstPickupTutorialUI()
    {
        if (firstPickupTutorialRawImage != null)
            firstPickupTutorialRawImage.texture =
                activeTutorialImage != null
                    ? activeTutorialImage
                    : firstPickupTutorialImage;

        if (firstPickupTutorialInstructionText != null)
            firstPickupTutorialInstructionText.text =
                activeTutorialInstruction;

        ConfigureTutorialLayout();
    }

    private Vector2 GetTutorialImageSize()
    {
        Texture2D tutorialImage =
            activeTutorialImage != null
                ? activeTutorialImage
                : firstPickupTutorialImage;

        if (tutorialImage == null ||
            firstPickupTutorialSize.x <= 0f ||
            firstPickupTutorialSize.y <= 0f)
        {
            return firstPickupTutorialSize;
        }

        float scale = Mathf.Min(
            firstPickupTutorialSize.x / tutorialImage.width,
            firstPickupTutorialSize.y / tutorialImage.height
        );

        return new Vector2(
            tutorialImage.width * scale,
            tutorialImage.height * scale
        );
    }

    private void ConfigureTutorialLayout()
    {
        if (firstPickupTutorialCanvas == null)
            return;

        Vector2 imageSize = GetTutorialImageSize();
        float instructionWidth =
            Mathf.Max(220f, firstPickupTutorialInstructionWidth);

        RectTransform canvasRect =
            firstPickupTutorialCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
            canvasRect.sizeDelta = new Vector2(
                imageSize.x + instructionWidth,
                Mathf.Max(imageSize.y, 220f)
            );

        if (firstPickupTutorialRawImage != null)
        {
            RectTransform imageRect =
                firstPickupTutorialRawImage.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0f, 0.5f);
            imageRect.anchorMax = new Vector2(0f, 0.5f);
            imageRect.pivot = new Vector2(0f, 0.5f);
            imageRect.anchoredPosition = new Vector2(instructionWidth, 0f);
            imageRect.sizeDelta = imageSize;
        }

        if (firstPickupTutorialInstructionText != null)
        {
            RectTransform instructionRect =
                firstPickupTutorialInstructionText.GetComponent<RectTransform>();
            instructionRect.anchorMin = Vector2.zero;
            instructionRect.anchorMax = new Vector2(0f, 1f);
            instructionRect.pivot = new Vector2(0f, 0.5f);
            instructionRect.offsetMin = Vector2.zero;
            instructionRect.offsetMax = new Vector2(instructionWidth, 0f);
        }
    }

    private void SetFirstPickupTutorialVisible(bool visible)
    {
        if (firstPickupTutorialCanvas != null)
            firstPickupTutorialCanvas.SetActive(visible);
        else if (firstPickupTutorialRawImage != null)
            firstPickupTutorialRawImage.gameObject.SetActive(visible);
    }

    private void CreateAutoGuideUI()
    {
        Canvas canvas = new GameObject("HornGuideCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(220f, 620f);
        canvasRect.localScale = Vector3.one * 0.0015f;

        if (playerHead != null)
        {
            canvas.transform.SetParent(playerHead, false);
            canvas.transform.localPosition = new Vector3(-0.48f, -0.06f, 1.2f);
            canvas.transform.localRotation = Quaternion.identity;
        }

        guidePanel = new GameObject("HornGuidePanel", typeof(RectTransform));
        guidePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = guidePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        angleSlider = CreateSlider(
            guidePanel.transform,
            "AngleSlider",
            -90f,
            90f,
            out angleFillImage,
            out angleTargetBand
        );

        CreateGuideImage(
            guidePanel.transform,
            "HornGuideDepth",
            "HornGuideFrame",
            new Color(0.09f, 0.065f, 0.035f, 0.65f),
            new Vector2(8f, -8f),
            Vector2.zero
        );

        CreateGuideImage(
            guidePanel.transform,
            "HornGuideFrame",
            "HornGuideFrame",
            Color.white,
            Vector2.zero,
            Vector2.zero
        );

        anglePointer = CreateGuideImage(
            guidePanel.transform,
            "HornGuidePointer",
            "HornGuidePointer",
            Color.white,
            Vector2.zero,
            new Vector2(92f, 68f)
        ).GetComponent<RectTransform>();

        UpdatePointerPosition(angleSlider.value);

        Log("Auto guide UI created.");
    }

    private RawImage CreateGuideImage(
        Transform parent,
        string objectName,
        string resourceName,
        Color color,
        Vector2 offset,
        Vector2 size
    )
    {
        RawImage image = new GameObject(objectName, typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(parent, false);

        Texture2D texture = Resources.Load<Texture2D>(resourceName);
        Sprite sprite = texture == null
            ? Resources.Load<Sprite>(resourceName)
            : null;
        image.texture = texture != null
            ? texture
            : sprite != null
                ? sprite.texture
                : null;
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.GetComponent<RectTransform>();

        if (size == Vector2.zero)
        {
            Stretch(rect);
            rect.anchoredPosition = offset;
        }
        else
        {
            rect.anchorMin = new Vector2(0.66f, 0.13f);
            rect.anchorMax = new Vector2(0.66f, 0.13f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }

        return image;
    }

    private Text CreateText(Transform parent, string name, int fontSize, FontStyle style)
    {
        Text text = new GameObject(name, typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement layoutElement = text.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = fontSize + 8f;

        return text;
    }

    private Slider CreateSlider(Transform parent, string name, float minValue, float maxValue, out Image fillImage, out RectTransform targetBand)
    {
        GameObject root = new GameObject(name, typeof(Slider), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.42f, 0.13f);
        rootRect.anchorMax = new Vector2(0.58f, 0.89f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject background = CreateSliderImage(root.transform, "Background", new Color(0.05f, 0.04f, 0.025f, 0.45f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        Stretch(backgroundRect);

        GameObject band = CreateSliderImage(root.transform, "TargetBand", targetBandColor);
        targetBand = band.GetComponent<RectTransform>();
        Stretch(targetBand);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect);

        GameObject fill = CreateSliderImage(fillArea.transform, "Fill", outOfRangeColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        fillImage = fill.GetComponent<Image>();

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = minValue;
        slider.interactable = false;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.BottomToTop;

        return slider;
    }

    private GameObject CreateSliderImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void SetGuideVisible(bool visible)
    {
        if (guidePanel != null)
            guidePanel.SetActive(visible);
    }

    private void EnsureSnapPromptUI()
    {
        if (!lockMouthpieceToHead)
            return;

        if (snapPromptText != null || !autoCreateSnapPromptUI)
            return;

        Canvas canvas = new GameObject(
            "HornSnapPromptCanvas",
            typeof(Canvas),
            typeof(CanvasScaler)
        ).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = snapPromptSize;
        canvasRect.localScale = Vector3.one * 0.0015f;

        if (playerHead != null)
        {
            canvas.transform.SetParent(playerHead, false);
            canvas.transform.localPosition =
                snapPromptHeadLocalPosition;
            canvas.transform.localRotation = Quaternion.identity;
        }

        snapPromptCanvas = canvas.gameObject;

        Text text = new GameObject(
            "HornSnapPromptText",
            typeof(Text)
        ).GetComponent<Text>();
        text.transform.SetParent(canvas.transform, false);
        text.font = GetDefaultFont();
        text.fontSize = snapPromptFontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        RectTransform textRect = text.GetComponent<RectTransform>();
        Stretch(textRect);

        snapPromptText = text;
        UpdateSnapPrompt(false);
        SetSnapPromptVisible(false);
    }

    private void SetSnapPromptVisible(bool visible)
    {
        visible = visible && lockMouthpieceToHead;

        if (snapPromptCanvas != null)
            snapPromptCanvas.SetActive(visible);
        else if (snapPromptText != null)
            snapPromptText.gameObject.SetActive(visible);
    }

    private void UpdateSnapPrompt(bool mouthpieceOK)
    {
        if (snapPromptText == null)
            return;

        snapPromptText.text =
            mouthpieceOK ? snapMatchedText : snapSearchText;
        snapPromptText.color =
            mouthpieceOK ? snapMatchedColor : snapSearchColor;
    }

    private void SetGuideSegment()
    {
        HornMusicSegment segment = CurrentSegment;

        if (segment == null)
            return;

        ConfigureGuideForAngleSegment();

        if (segmentText != null)
            segmentText.text = "Round " + (currentRoundIndex + 1) + "/" + totalRounds + " - Segment " + (currentSegmentIndex + 1) + "/" + segments.Length + ": " + segment.segmentName;

        if (angleRangeText != null)
            angleRangeText.text = "Angle target: " + segment.minAngle.ToString("F0") + "deg - " + segment.maxAngle.ToString("F0") + "deg";

        UpdateTargetBand(angleTargetBand, segment.minAngle, segment.maxAngle, -90f, 90f);
    }

    private void ConfigureGuideForAngleSegment()
    {
        RectTransform canvasRect = GetGuideCanvasRect();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = new Vector2(220f, 620f);
            canvasRect.localPosition = new Vector3(-0.48f, -0.06f, 1.2f);
            canvasRect.localRotation = Quaternion.identity;
        }

        if (angleSlider != null)
        {
            angleSlider.minValue = -90f;
            angleSlider.maxValue = 90f;
            angleSlider.direction = Slider.Direction.BottomToTop;

            RectTransform sliderRect =
                angleSlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.42f, 0.13f);
            sliderRect.anchorMax = new Vector2(0.58f, 0.89f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
        }

        if (angleTargetBand != null)
            angleTargetBand.gameObject.SetActive(true);

        if (anglePointer != null)
            anglePointer.gameObject.SetActive(true);
    }

    private void ConfigureGuideForShakeSpeed()
    {
        RectTransform canvasRect = GetGuideCanvasRect();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = new Vector2(220f, 620f);
            canvasRect.localPosition = new Vector3(0f, -0.38f, 1.2f);
            canvasRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        if (angleSlider != null)
        {
            angleSlider.minValue = 0f;
            angleSlider.maxValue = 1f;
            angleSlider.direction = Slider.Direction.BottomToTop;

            RectTransform sliderRect =
                angleSlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.42f, 0.13f);
            sliderRect.anchorMax = new Vector2(0.58f, 0.89f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
        }

        if (angleTargetBand != null)
            angleTargetBand.gameObject.SetActive(false);

        if (anglePointer != null)
            anglePointer.gameObject.SetActive(true);
    }

    private RectTransform GetGuideCanvasRect()
    {
        if (guidePanel == null ||
            guidePanel.transform.parent == null)
        {
            return null;
        }

        return guidePanel.transform.parent.GetComponent<RectTransform>();
    }

    private void UpdateGuideUI(bool mouthpieceOK, bool angleOK)
    {
        if (mouthpieceStatusText != null)
        {
            mouthpieceStatusText.gameObject.SetActive(lockMouthpieceToHead);
            if (lockMouthpieceToHead)
            {
                mouthpieceStatusText.text = mouthpieceOK ? "Mouthpiece: snapped" : "Mouthpiece: bring to mouth";
                mouthpieceStatusText.color = mouthpieceOK ? inRangeColor : outOfRangeColor;
            }
        }

        if (angleSlider != null)
        {
            angleSlider.value = smoothedAngle;
            UpdatePointerPosition(smoothedAngle);
        }

        if (angleFillImage != null)
            angleFillImage.color = angleOK ? inRangeColor : outOfRangeColor;

        if (statusText != null)
        {
            if (mouthpieceOK && angleOK)
                statusText.text = isPlaying ? "Playing" : "Ready";
            else
                statusText.text = lockMouthpieceToHead ? "Adjust mouthpiece and angle" : "Adjust angle";
        }
    }

    private bool IsMouthpieceOK()
    {
        return !lockMouthpieceToHead || isMouthpieceSnapped;
    }

    private void UpdateTargetBand(RectTransform band, float minValue, float maxValue, float sliderMin, float sliderMax)
    {
        if (band == null)
            return;

        float range = Mathf.Max(0.001f, sliderMax - sliderMin);
        float anchorMin = Mathf.Clamp01((minValue - sliderMin) / range);
        float anchorMax = Mathf.Clamp01((maxValue - sliderMin) / range);

        band.anchorMin = new Vector2(0f, anchorMin);
        band.anchorMax = new Vector2(1f, anchorMax);
        band.offsetMin = Vector2.zero;
        band.offsetMax = Vector2.zero;

        Image bandImage = band.GetComponent<Image>();
        if (bandImage != null)
            bandImage.color = targetBandColor;
    }

    private void UpdatePointerPosition(float value)
    {
        if (anglePointer == null)
            return;

        float normalized = Mathf.InverseLerp(-90f, 90f, value);
        anglePointer.anchorMin = new Vector2(0.66f, Mathf.Lerp(0.13f, 0.89f, normalized));
        anglePointer.anchorMax = anglePointer.anchorMin;
        anglePointer.anchoredPosition = Vector2.zero;
    }

    private void UpdateSpeedPointerPosition(float normalized)
    {
        if (anglePointer == null)
            return;

        float resolved = Mathf.Clamp01(normalized);
        anglePointer.anchorMin = new Vector2(
            0.66f,
            Mathf.Lerp(0.13f, 0.89f, resolved)
        );
        anglePointer.anchorMax = anglePointer.anchorMin;
        anglePointer.anchoredPosition = Vector2.zero;
    }

    private void DebugRuntimeValues(bool mouthpieceOK, bool angleOK)
    {
        if (!showDebugLogs)
            return;

        debugTimer += Time.deltaTime;

        if (debugTimer < debugInterval)
            return;

        debugTimer = 0f;

        int timelinePosition = -1;
        if (eventCreated)
            hornInstance.getTimelinePosition(out timelinePosition);

        string segmentName = CurrentSegment != null ? CurrentSegment.segmentName : "None";
        Debug.Log(
            "[HornFMODController] Segment: " + (currentSegmentIndex + 1) + "/" + segments.Length +
            " | Round: " + (currentRoundIndex + 1) + "/" + totalRounds +
            " " + segmentName +
            " | Timeline: " + timelinePosition + " ms" +
            " | Mouthpiece OK:" + mouthpieceOK +
            " | Audio1 Distance: " + lastAudio1VolumeDistance.ToString("F3") +
            " | Audio1 Volume: " + currentAudio1Volume.ToString("F2") +
            " | Angle: " + smoothedAngle.ToString("F2") + " OK:" + angleOK +
            " | Playing: " + isPlaying
        );
    }

    private void CheckFMODResult(FMOD.RESULT result, string operation)
    {
        if (result != FMOD.RESULT.OK)
            Debug.LogError("[HornFMODController] FMOD error during " + operation + ": " + result);
    }

    private void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log("[HornFMODController] " + message);
    }

    private void OnDestroy()
    {
        StopAndReleaseEvent();
    }

    private void StopAndReleaseEvent()
    {
        if (!eventCreated)
            return;

        hornInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        hornInstance.release();
        eventCreated = false;
        isPlaying = false;
    }
}
