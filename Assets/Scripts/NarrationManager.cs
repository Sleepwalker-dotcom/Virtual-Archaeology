using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private UnityEvent onGardenVoiceOverFinished;

    [Header("Intro Subtitles")]
    [TextArea(2, 5)]
    [SerializeField] private string subtitle01 =
        "Can you hear them? The clinking of bottles, the laughter, and the rhythm of life... How I miss those sounds.";
    [TextArea(2, 5)]
    [SerializeField] private string subtitle02 =
        "This place was once a tavern, where people played the horn and sang until the first light of dawn. But those sounds faded long ago.";
    [TextArea(2, 5)]
    [SerializeField] private string subtitle03 =
        "I\u2019m Cecilia. I once kept this tavern; now I guard its last echoes.";
    [TextArea(2, 5)]
    [SerializeField] private string subtitle04 =
        "You came for the horn, didn\u2019t you? Only a fragment of it remains here. Take it and return it to where it belongs. Perhaps the tavern will sing again.";
    [SerializeField] private float subtitleFadeInSeconds = 0.35f;
    [SerializeField] private float subtitleFadeOutSeconds = 0.35f;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private Text subtitleText;

    private EventInstance currentNarration;
    private readonly ConcurrentQueue<string> pendingSubtitleMarkers = new();
    private EVENT_CALLBACK narrationCallback;
    private GCHandle callbackHandle;
    private Coroutine subtitleFade;
    private Coroutine gardenVoiceOverCompletion;

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

        if (currentNarration.isValid())
            gardenVoiceOverCompletion = StartCoroutine(WaitForGardenVoiceOver());
    }

    public void StopNarration()
    {
        if (gardenVoiceOverCompletion != null)
        {
            StopCoroutine(gardenVoiceOverCompletion);
            gardenVoiceOverCompletion = null;
        }

        if (!currentNarration.isValid())
            return;

        currentNarration.setCallback(null, 0);
        currentNarration.setUserData(System.IntPtr.Zero);

        FMOD.Studio.STOP_MODE stopMode = allowFadeoutWhenInterrupted
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
            : FMOD.Studio.STOP_MODE.IMMEDIATE;

        currentNarration.stop(stopMode);
        currentNarration.release();
        currentNarration.clearHandle();
        ReleaseCallbackHandle();
        ClearSubtitleImmediate();
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

        narrationCallback ??= NarrationEventCallback;
        callbackHandle = GCHandle.Alloc(this);
        currentNarration.setUserData(GCHandle.ToIntPtr(callbackHandle));
        currentNarration.setCallback(
            narrationCallback,
            EVENT_CALLBACK_TYPE.TIMELINE_MARKER
        );

        FMOD.RESULT result = currentNarration.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(
                "[NarrationManager] Failed to start " +
                narrationEvent.Path + ": " + result,
                this
            );
            currentNarration.setCallback(null, 0);
            currentNarration.setUserData(System.IntPtr.Zero);
            currentNarration.release();
            currentNarration.clearHandle();
            ReleaseCallbackHandle();
            return;
        }

        Debug.Log(
            "[NarrationManager] Playing " + label +
            " narration: " + narrationEvent.Path,
            this
        );
    }

    private IEnumerator WaitForGardenVoiceOver()
    {
        while (currentNarration.isValid())
        {
            FMOD.RESULT result = currentNarration.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "[NarrationManager] Failed to read Garden VO playback state: " +
                    result,
                    this
                );
                gardenVoiceOverCompletion = null;
                yield break;
            }

            if (playbackState == PLAYBACK_STATE.STOPPED)
                break;

            yield return null;
        }

        gardenVoiceOverCompletion = null;
        onGardenVoiceOverFinished?.Invoke();
    }

    private void Awake()
    {
        EnsureSubtitleUI();
        ClearSubtitleImmediate();
    }

    private void Update()
    {
        while (pendingSubtitleMarkers.TryDequeue(out string markerName))
            HandleSubtitleMarker(markerName);
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT NarrationEventCallback(
        EVENT_CALLBACK_TYPE type,
        System.IntPtr eventInstancePtr,
        System.IntPtr parameters
    )
    {
        if (type != EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
            return FMOD.RESULT.OK;

        EventInstance eventInstance = new(eventInstancePtr);
        FMOD.RESULT userDataResult =
            eventInstance.getUserData(out System.IntPtr userData);

        if (userDataResult != FMOD.RESULT.OK || userData == System.IntPtr.Zero)
            return FMOD.RESULT.OK;

        GCHandle handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is not NarrationManager manager)
            return FMOD.RESULT.OK;

        TIMELINE_MARKER_PROPERTIES marker =
            Marshal.PtrToStructure<TIMELINE_MARKER_PROPERTIES>(parameters);
        manager.pendingSubtitleMarkers.Enqueue(marker.name);
        return FMOD.RESULT.OK;
    }

    private void HandleSubtitleMarker(string markerName)
    {
        switch (markerName)
        {
            case "SUBTITLE_01":
                ShowSubtitle(subtitle01);
                break;
            case "SUBTITLE_02":
                ShowSubtitle(subtitle02);
                break;
            case "SUBTITLE_03":
                ShowSubtitle(subtitle03);
                break;
            case "SUBTITLE_04":
                ShowSubtitle(subtitle04);
                break;
            case "SUBTITLE_CLEAR_01":
                ClearSubtitle(subtitle01);
                break;
            case "SUBTITLE_CLEAR_02":
                ClearSubtitle(subtitle02);
                break;
            case "SUBTITLE_CLEAR_03":
                ClearSubtitle(subtitle03);
                break;
            case "SUBTITLE_CLEAR_04":
                ClearSubtitle(subtitle04);
                break;
        }
    }

    private void ShowSubtitle(string text)
    {
        EnsureSubtitleUI();
        if (subtitleCanvasGroup == null || subtitleText == null)
            return;

        subtitleText.text = text;
        StartSubtitleFade(1f, subtitleFadeInSeconds, false);
    }

    private void ClearSubtitle(string expectedText)
    {
        if (subtitleText == null || subtitleText.text != expectedText)
            return;

        StartSubtitleFade(0f, subtitleFadeOutSeconds, true);
    }

    private void StartSubtitleFade(
        float targetAlpha,
        float duration,
        bool clearTextWhenComplete
    )
    {
        if (subtitleFade != null)
            StopCoroutine(subtitleFade);

        subtitleFade = StartCoroutine(
            FadeSubtitle(targetAlpha, duration, clearTextWhenComplete)
        );
    }

    private IEnumerator FadeSubtitle(
        float targetAlpha,
        float duration,
        bool clearTextWhenComplete
    )
    {
        if (subtitleCanvasGroup == null || subtitleText == null)
            yield break;

        float startAlpha = subtitleCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            subtitleCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                duration <= 0f ? 1f : elapsed / duration
            );
            yield return null;
        }

        subtitleCanvasGroup.alpha = targetAlpha;
        if (clearTextWhenComplete)
            subtitleText.text = string.Empty;

        subtitleFade = null;
    }

    private void EnsureSubtitleUI()
    {
        if (subtitleCanvasGroup != null && subtitleText != null)
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogWarning(
                "[NarrationManager] Main Camera not found; subtitle UI was not created.",
                this
            );
            return;
        }

        GameObject canvasObject = new(
            "IntroSubtitleCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup)
        );
        canvasObject.transform.SetParent(targetCamera.transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, -0.32f, 1.2f);
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.0015f;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900f, 180f);
        subtitleCanvasGroup = canvasObject.GetComponent<CanvasGroup>();

        GameObject textObject = new(
            "SubtitleText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)
        );
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(35f, 15f);
        textRect.offsetMax = new Vector2(-35f, -15f);

        subtitleText = textObject.GetComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 38;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = Color.white;
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void ClearSubtitleImmediate()
    {
        if (subtitleFade != null)
        {
            StopCoroutine(subtitleFade);
            subtitleFade = null;
        }

        if (subtitleCanvasGroup != null)
            subtitleCanvasGroup.alpha = 0f;
        if (subtitleText != null)
            subtitleText.text = string.Empty;

        while (pendingSubtitleMarkers.TryDequeue(out _))
        {
        }
    }

    private void ReleaseCallbackHandle()
    {
        if (callbackHandle.IsAllocated)
            callbackHandle.Free();
    }

    private void OnDisable()
    {
        StopNarration();
    }
}
