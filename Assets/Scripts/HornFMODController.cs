using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class HornMusicSegment
{
    public string segmentName = "Phrase";
    public int startTimeMs;
    public int endTimeMs = 6000;
    public float minDistance = 0.2f;
    public float maxDistance = 0.45f;
    public float minAngle = -10f;
    public float maxAngle = 20f;
}

public class HornFMODController : MonoBehaviour
{
    [Header("FMOD Event")]
    public EventReference hornEvent;

    [Header("VR References")]
    public Transform playerHead;
    public Transform hornObject;

    [Header("Segments")]
    public HornMusicSegment[] segments =
    {
        new HornMusicSegment
        {
            segmentName = "Phrase 1",
            startTimeMs = 0,
            endTimeMs = 6000,
            minDistance = 0.2f,
            maxDistance = 0.45f,
            minAngle = -10f,
            maxAngle = 20f
        },
        new HornMusicSegment
        {
            segmentName = "Phrase 2",
            startTimeMs = 6000,
            endTimeMs = 12000,
            minDistance = 0.35f,
            maxDistance = 0.6f,
            minAngle = 20f,
            maxAngle = 55f
        },
        new HornMusicSegment
        {
            segmentName = "Phrase 3",
            startTimeMs = 12000,
            endTimeMs = 18000,
            minDistance = 0.2f,
            maxDistance = 0.4f,
            minAngle = -55f,
            maxAngle = -20f
        },
        new HornMusicSegment
        {
            segmentName = "Phrase 4",
            startTimeMs = 18000,
            endTimeMs = 26000,
            minDistance = 0.45f,
            maxDistance = 0.75f,
            minAngle = 35f,
            maxAngle = 80f
        }
    };
    public int startSegmentIndex;

    [Header("Tracking")]
    public float maxDistance = 1.2f;
    public bool invertAngle;

    [Header("Smoothing")]
    public float distanceSmoothing = 8f;
    public float angleSmoothing = 8f;

    [Header("Horn Glow")]
    public Renderer[] hornRenderers;
    public Color glowColor = new Color(1f, 0.75f, 0.2f);
    public float glowIntensity = 3f;

    [Header("Guide UI")]
    public bool autoCreateGuideUI = true;
    public GameObject guidePanel;
    public Text segmentText;
    public Text distanceRangeText;
    public Text angleRangeText;
    public Text statusText;
    public Slider distanceSlider;
    public Slider angleSlider;
    public Image distanceFillImage;
    public Image angleFillImage;
    public RectTransform distanceTargetBand;
    public RectTransform angleTargetBand;
    public Color inRangeColor = new Color(0.2f, 0.8f, 0.35f);
    public Color outOfRangeColor = new Color(0.95f, 0.25f, 0.2f);
    public Color targetBandColor = new Color(1f, 0.8f, 0.2f, 0.35f);

    [Header("Debug")]
    public bool showDebugLogs = true;
    public float debugInterval = 0.5f;

    private EventInstance hornInstance;
    private bool isActivated;
    private bool eventCreated;
    private bool isPlaying;
    private bool isComplete;
    private int currentSegmentIndex;
    private float smoothedDistance = 1.2f;
    private float smoothedAngle;
    private float debugTimer;

    private HornMusicSegment CurrentSegment
    {
        get
        {
            if (segments == null || currentSegmentIndex < 0 || currentSegmentIndex >= segments.Length)
                return null;

            return segments[currentSegmentIndex];
        }
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

        CacheHornRenderersIfNeeded();
        SetGuideVisible(false);
    }

    private void Update()
    {
        if (!isActivated || isComplete || !eventCreated)
            return;

        if (playerHead == null || hornObject == null || CurrentSegment == null)
            return;

        UpdateDistanceAndAngle();
        SendParametersToFMOD();
        Update3DPosition();

        bool distanceOK = IsDistanceInRange(CurrentSegment);
        bool angleOK = IsAngleInRange(CurrentSegment);

        HandlePlayPause(distanceOK && angleOK);
        CheckSegmentEnd();
        UpdateGuideUI(distanceOK, angleOK);
        DebugRuntimeValues(distanceOK, angleOK);
    }

    public void ActivateHorn()
    {
        Log("ActivateHorn() called.");
        EnsureSegments();

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
        isComplete = false;
        isPlaying = false;
        currentSegmentIndex = Mathf.Clamp(startSegmentIndex, 0, segments.Length - 1);

        CacheHornRenderersIfNeeded();
        TurnOnGlow();
        EnsureGuideUI();
        CreateAndPrimeFMODEvent();

        if (!eventCreated)
            return;

        SetGuideVisible(true);
        SetGuideSegment();
        JumpToCurrentSegmentStart();
        PauseEvent();

        Log("Horn activated. FMOD event is paused at the current segment start.");
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
                minDistance = 0.2f,
                maxDistance = 0.45f,
                minAngle = -10f,
                maxAngle = 20f
            },
            new HornMusicSegment
            {
                segmentName = "Phrase 2",
                startTimeMs = 6000,
                endTimeMs = 12000,
                minDistance = 0.35f,
                maxDistance = 0.6f,
                minAngle = 20f,
                maxAngle = 55f
            },
            new HornMusicSegment
            {
                segmentName = "Phrase 3",
                startTimeMs = 12000,
                endTimeMs = 18000,
                minDistance = 0.2f,
                maxDistance = 0.4f,
                minAngle = -55f,
                maxAngle = -20f
            },
            new HornMusicSegment
            {
                segmentName = "Phrase 4",
                startTimeMs = 18000,
                endTimeMs = 26000,
                minDistance = 0.45f,
                maxDistance = 0.75f,
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
        CheckFMODResult(hornInstance.start(), "start");
        CheckFMODResult(hornInstance.setPaused(true), "initial setPaused(true)");
        Update3DPosition();
        Log("FMOD Event created and started in paused state.");
    }

    private void JumpToCurrentSegmentStart()
    {
        if (!eventCreated || CurrentSegment == null)
            return;

        CheckFMODResult(hornInstance.setTimelinePosition(CurrentSegment.startTimeMs), "setTimelinePosition to segment start");
        Log("Jumped to segment " + (currentSegmentIndex + 1) + ": " + CurrentSegment.segmentName + ", start = " + CurrentSegment.startTimeMs + " ms");
    }

    private void UpdateDistanceAndAngle()
    {
        float rawDistance = Vector3.Distance(hornObject.position, playerHead.position);
        rawDistance = Mathf.Clamp(rawDistance, 0f, maxDistance);

        Vector3 forward = hornObject.forward.normalized;
        float rawAngle = Mathf.Asin(Vector3.Dot(forward, Vector3.up)) * Mathf.Rad2Deg;

        if (invertAngle)
            rawAngle = -rawAngle;

        rawAngle = Mathf.Clamp(rawAngle, -90f, 90f);

        smoothedDistance = Mathf.Lerp(smoothedDistance, rawDistance, Time.deltaTime * distanceSmoothing);
        smoothedAngle = Mathf.Lerp(smoothedAngle, rawAngle, Time.deltaTime * angleSmoothing);
    }

    private void SendParametersToFMOD()
    {
        CheckFMODResult(hornInstance.setParameterByName("DistanceToHead", smoothedDistance), "setParameterByName DistanceToHead");
        CheckFMODResult(hornInstance.setParameterByName("HornAngle", smoothedAngle), "setParameterByName HornAngle");
    }

    private void Update3DPosition()
    {
        if (!eventCreated || hornObject == null)
            return;

        CheckFMODResult(hornInstance.set3DAttributes(RuntimeUtils.To3DAttributes(hornObject)), "set3DAttributes");
    }

    private bool IsDistanceInRange(HornMusicSegment segment)
    {
        return smoothedDistance >= segment.minDistance && smoothedDistance <= segment.maxDistance;
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
            CompleteExperience();
            return;
        }

        JumpToCurrentSegmentStart();
        SetGuideSegment();
        Log("Moved to next segment: " + CurrentSegment.segmentName);
    }

    private void CompleteExperience()
    {
        isComplete = true;
        isPlaying = false;
        CheckFMODResult(hornInstance.setPaused(true), "final pause");

        if (statusText != null)
            statusText.text = "Complete";

        Log("All segments complete.");
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

    private void CreateAutoGuideUI()
    {
        Canvas canvas = new GameObject("HornGuideCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 360f);
        canvasRect.localScale = Vector3.one * 0.0018f;

        if (playerHead != null)
        {
            canvas.transform.SetParent(playerHead, false);
            canvas.transform.localPosition = new Vector3(0f, -0.18f, 1.25f);
            canvas.transform.localRotation = Quaternion.identity;
        }

        guidePanel = new GameObject("HornGuidePanel", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        guidePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = guidePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = guidePanel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.045f, 0.05f, 0.82f);

        VerticalLayoutGroup layout = guidePanel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        guidePanel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        segmentText = CreateText(guidePanel.transform, "SegmentText", 28, FontStyle.Bold);
        distanceRangeText = CreateText(guidePanel.transform, "DistanceRangeText", 22, FontStyle.Normal);
        distanceSlider = CreateSlider(guidePanel.transform, "DistanceSlider", 0f, maxDistance, out distanceFillImage, out distanceTargetBand);
        angleRangeText = CreateText(guidePanel.transform, "AngleRangeText", 22, FontStyle.Normal);
        angleSlider = CreateSlider(guidePanel.transform, "AngleSlider", -90f, 90f, out angleFillImage, out angleTargetBand);
        statusText = CreateText(guidePanel.transform, "StatusText", 24, FontStyle.Bold);

        Log("Auto guide UI created.");
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
        rootRect.sizeDelta = new Vector2(0f, 32f);

        LayoutElement layoutElement = root.GetComponent<LayoutElement>();
        layoutElement.minHeight = 32f;
        layoutElement.preferredHeight = 32f;

        GameObject background = CreateSliderImage(root.transform, "Background", new Color(0.18f, 0.18f, 0.2f, 1f));
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
        slider.direction = Slider.Direction.LeftToRight;

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

    private void SetGuideSegment()
    {
        HornMusicSegment segment = CurrentSegment;

        if (segment == null)
            return;

        if (segmentText != null)
            segmentText.text = "Segment " + (currentSegmentIndex + 1) + "/" + segments.Length + ": " + segment.segmentName;

        if (distanceRangeText != null)
            distanceRangeText.text = "Distance target: " + segment.minDistance.ToString("F2") + "m - " + segment.maxDistance.ToString("F2") + "m";

        if (angleRangeText != null)
            angleRangeText.text = "Angle target: " + segment.minAngle.ToString("F0") + "deg - " + segment.maxAngle.ToString("F0") + "deg";

        UpdateTargetBand(distanceTargetBand, segment.minDistance, segment.maxDistance, 0f, maxDistance);
        UpdateTargetBand(angleTargetBand, segment.minAngle, segment.maxAngle, -90f, 90f);
    }

    private void UpdateGuideUI(bool distanceOK, bool angleOK)
    {
        if (distanceSlider != null)
            distanceSlider.value = smoothedDistance;

        if (angleSlider != null)
            angleSlider.value = smoothedAngle;

        if (distanceFillImage != null)
            distanceFillImage.color = distanceOK ? inRangeColor : outOfRangeColor;

        if (angleFillImage != null)
            angleFillImage.color = angleOK ? inRangeColor : outOfRangeColor;

        if (statusText != null)
        {
            if (distanceOK && angleOK)
                statusText.text = isPlaying ? "Playing" : "Ready";
            else
                statusText.text = "Adjust horn";
        }
    }

    private void UpdateTargetBand(RectTransform band, float minValue, float maxValue, float sliderMin, float sliderMax)
    {
        if (band == null)
            return;

        float range = Mathf.Max(0.001f, sliderMax - sliderMin);
        float anchorMin = Mathf.Clamp01((minValue - sliderMin) / range);
        float anchorMax = Mathf.Clamp01((maxValue - sliderMin) / range);

        band.anchorMin = new Vector2(anchorMin, 0f);
        band.anchorMax = new Vector2(anchorMax, 1f);
        band.offsetMin = Vector2.zero;
        band.offsetMax = Vector2.zero;

        Image bandImage = band.GetComponent<Image>();
        if (bandImage != null)
            bandImage.color = targetBandColor;
    }

    private void DebugRuntimeValues(bool distanceOK, bool angleOK)
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
            " " + segmentName +
            " | Timeline: " + timelinePosition + " ms" +
            " | Distance: " + smoothedDistance.ToString("F3") + " OK:" + distanceOK +
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
