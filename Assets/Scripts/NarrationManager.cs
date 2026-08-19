using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteAlways]
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
    [SerializeField] private Transform sceneSubtitleRoot;
    [SerializeField] private int sceneSubtitleFontSize = 38;
    [SerializeField] private FontStyle sceneSubtitleFontStyle = FontStyle.Normal;
    [SerializeField] private Color sceneSubtitleColor = Color.white;
    [SerializeField] private Vector2 sceneSubtitleCanvasSize =
        new Vector2(900f, 180f);
    [SerializeField] private float sceneSubtitleCanvasScale = 0.002f;
    [SerializeField] private bool showSubtitlePreviewInEditMode = true;

    private EventInstance currentNarration;
    private readonly ConcurrentQueue<string> pendingSubtitleMarkers = new();
    private EVENT_CALLBACK narrationCallback;
    private GCHandle callbackHandle;
    private readonly CanvasGroup[] sceneSubtitleCanvasGroups =
        new CanvasGroup[4];
    private readonly Text[] sceneSubtitleTexts = new Text[4];
    private readonly Coroutine[] sceneSubtitleFades = new Coroutine[4];
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
        ClearSubtitleImmediate();

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
        if (Application.isPlaying)
            ClearSubtitleImmediate();
        else
            RefreshSubtitlePreview();
    }

    private void OnValidate()
    {
        if (!HasAllSubtitleSlots())
            return;

        ApplySubtitleStyle();
        RefreshSubtitlePreview();
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
                ShowSubtitle(0, subtitle01);
                break;
            case "SUBTITLE_02":
                ShowSubtitle(1, subtitle02);
                break;
            case "SUBTITLE_03":
                ShowSubtitle(2, subtitle03);
                break;
            case "SUBTITLE_04":
                ShowSubtitle(3, subtitle04);
                break;
            case "SUBTITLE_CLEAR_01":
                ClearSubtitle(0);
                break;
            case "SUBTITLE_CLEAR_02":
                ClearSubtitle(1);
                break;
            case "SUBTITLE_CLEAR_03":
                ClearSubtitle(2);
                break;
            case "SUBTITLE_CLEAR_04":
                ClearSubtitle(3);
                break;
        }
    }

    private void ShowSubtitle(int index, string text)
    {
        EnsureSubtitleUI();
        if (!HasSubtitleSlot(index))
            return;

        sceneSubtitleTexts[index].text = text;
        StartSubtitleFade(index, 1f, subtitleFadeInSeconds, false);
    }

    private void ClearSubtitle(int index)
    {
        if (!HasSubtitleSlot(index))
            return;

        StartSubtitleFade(index, 0f, subtitleFadeOutSeconds, true);
    }

    private void StartSubtitleFade(
        int index,
        float targetAlpha,
        float duration,
        bool clearTextWhenComplete
    )
    {
        if (sceneSubtitleFades[index] != null)
            StopCoroutine(sceneSubtitleFades[index]);

        sceneSubtitleFades[index] = StartCoroutine(
            FadeSubtitle(index, targetAlpha, duration, clearTextWhenComplete)
        );
    }

    private IEnumerator FadeSubtitle(
        int index,
        float targetAlpha,
        float duration,
        bool clearTextWhenComplete
    )
    {
        if (!HasSubtitleSlot(index))
            yield break;

        CanvasGroup canvasGroup = sceneSubtitleCanvasGroups[index];
        Text text = sceneSubtitleTexts[index];
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                duration <= 0f ? 1f : elapsed / duration
            );
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (clearTextWhenComplete)
            text.text = string.Empty;

        sceneSubtitleFades[index] = null;
    }

    private void EnsureSubtitleUI()
    {
        if (HasAllSubtitleSlots())
        {
            ApplySubtitleStyle();
            return;
        }

        if (sceneSubtitleRoot == null)
        {
            GameObject rootObject = GameObject.Find("SceneSubtitleRoot");
            sceneSubtitleRoot = rootObject != null
                ? rootObject.transform
                : new GameObject("SceneSubtitleRoot").transform;
        }

        for (int i = 0; i < sceneSubtitleCanvasGroups.Length; i++)
            EnsureSceneSubtitleSlot(i);

        ApplySubtitleStyle();
    }

    private void EnsureSceneSubtitleSlot(int index)
    {
        string anchorName = "Subtitle_" + (index + 1).ToString("00") + "_Anchor";
        Transform anchor = sceneSubtitleRoot.Find(anchorName);

        if (anchor == null)
        {
            anchor = new GameObject(anchorName).transform;
            anchor.SetParent(sceneSubtitleRoot, false);
            anchor.localPosition = new Vector3(
                index % 2 == 0 ? -1.2f : 1.2f,
                1.6f,
                2.2f + index * 0.35f
            );
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup =
            anchor.GetComponentInChildren<CanvasGroup>(true);
        Text text = anchor.GetComponentInChildren<Text>(true);

        if (canvasGroup == null || text == null)
        {
            GameObject canvasObject = new(
                "SubtitleCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup)
            );
            canvasObject.transform.SetParent(anchor, false);
            canvasObject.transform.localPosition = Vector3.zero;
            canvasObject.transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);
            canvasObject.transform.localScale =
                Vector3.one * sceneSubtitleCanvasScale;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = sceneSubtitleCanvasSize;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            GameObject textObject = new(
                "SubtitleText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(35f, 15f);
            textRect.offsetMax = new Vector2(-35f, -15f);

            text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
            text.fontSize = sceneSubtitleFontSize;
            text.fontStyle = sceneSubtitleFontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = sceneSubtitleColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        sceneSubtitleCanvasGroups[index] = canvasGroup;
        sceneSubtitleTexts[index] = text;
    }

    private bool HasAllSubtitleSlots()
    {
        for (int i = 0; i < sceneSubtitleCanvasGroups.Length; i++)
        {
            if (!HasSubtitleSlot(i))
                return false;
        }

        return true;
    }

    private bool HasSubtitleSlot(int index)
    {
        return index >= 0 &&
            index < sceneSubtitleCanvasGroups.Length &&
            sceneSubtitleCanvasGroups[index] != null &&
            sceneSubtitleTexts[index] != null;
    }

    private void ApplySubtitleStyle()
    {
        for (int i = 0; i < sceneSubtitleTexts.Length; i++)
        {
            if (!HasSubtitleSlot(i))
                continue;

            sceneSubtitleTexts[i].fontSize = sceneSubtitleFontSize;
            sceneSubtitleTexts[i].fontStyle = sceneSubtitleFontStyle;
            sceneSubtitleTexts[i].color = sceneSubtitleColor;

            RectTransform canvasRect =
                sceneSubtitleCanvasGroups[i].GetComponent<RectTransform>();
            if (canvasRect != null)
                canvasRect.sizeDelta = sceneSubtitleCanvasSize;

            sceneSubtitleCanvasGroups[i].transform.localScale =
                Vector3.one * sceneSubtitleCanvasScale;
            sceneSubtitleCanvasGroups[i].transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);
        }
    }

    private void RefreshSubtitlePreview()
    {
        if (Application.isPlaying || !showSubtitlePreviewInEditMode)
            return;

        string[] subtitles =
        {
            subtitle01,
            subtitle02,
            subtitle03,
            subtitle04
        };

        for (int i = 0; i < subtitles.Length; i++)
        {
            if (!HasSubtitleSlot(i))
                continue;

            sceneSubtitleTexts[i].text = subtitles[i];
            sceneSubtitleCanvasGroups[i].alpha = 1f;
        }
    }

    private void ClearSubtitleImmediate()
    {
        for (int i = 0; i < sceneSubtitleFades.Length; i++)
        {
            if (sceneSubtitleFades[i] != null)
            {
                StopCoroutine(sceneSubtitleFades[i]);
                sceneSubtitleFades[i] = null;
            }

            if (sceneSubtitleCanvasGroups[i] != null)
                sceneSubtitleCanvasGroups[i].alpha = 0f;
            if (sceneSubtitleTexts[i] != null)
                sceneSubtitleTexts[i].text = string.Empty;
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
        if (Application.isPlaying)
            StopNarration();
    }
}
