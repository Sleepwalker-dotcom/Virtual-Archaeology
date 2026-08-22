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
    private enum ObjectIntroVoiceOver
    {
        Bottle,
        Domino,
        Whistle
    }

    [Header("FMOD Narration Events")]
    [SerializeField] private EventReference introNarrationEvent;
    [SerializeField] private EventReference restoreVoiceOverEvent;
    [SerializeField] private EventReference pickupVoiceOverEvent;
    [SerializeField] private EventReference gardenVoiceOverEvent;
    [SerializeField] private EventReference tavernVoiceOverEvent;
    [SerializeField] private EventReference grabHornVoiceOverEvent;
    [SerializeField] private EventReference hornIntroVoiceOverEvent;
    [SerializeField] private EventReference musicIntroVoiceOverEvent;
    [SerializeField] private EventReference showObjectsVoiceOverEvent;
    [SerializeField] private EventReference bottleIntroVoiceOverEvent;
    [SerializeField] private EventReference dominoIntroVoiceOverEvent;
    [SerializeField] private EventReference whistleIntroVoiceOverEvent;
    [SerializeField] private EventReference tavernAliveVoiceOverEvent;
    [SerializeField] private EventReference endingBgmEvent;
    [SerializeField] private EventReference endVoiceOverEvent;

    [Header("Playback")]
    [SerializeField] private bool allowFadeoutWhenInterrupted = true;
    [SerializeField, Min(0f)] private float gardenToMusicianAmbientDelay;
    [SerializeField, Min(0f)] private float gardenToTavernVoiceOverDelay = 10f;
    [SerializeField] private FMODTrigger3DAudio musicianAmbientPlayer;
    [SerializeField] private GameObject objectGroup;
    [SerializeField] private FMOD2DSFXPlayer uiSfxPlayer;
    [SerializeField] private UnityEvent onGardenVoiceOverFinished;

    [Header("Post Show Objects Melody")]
    [SerializeField] private HornFMODController hornController;
    [SerializeField, Min(0f)] private float showObjectsToMelodyDelay = 5f;
    [SerializeField, Min(1)] private int fullMelodyRepeatCount = 2;

    [Header("Standalone Object Interaction Test")]
    [SerializeField] private bool standaloneObjectInteractionTest;
    [SerializeField, Min(0f)] private float standaloneTestMelodyDelay = 1f;

    [Header("Tavern Alive Completion")]
    [SerializeField] private FMODTrigger3DAudio tavernCrowdPlayer;
    [SerializeField] private FMODTrigger3DAudio streetOutsidePlayer;
    [SerializeField, Min(0f)] private float tavernAliveToEndingBgmDelay = 10f;
    [SerializeField, Min(0f)] private float endingBgmToEndVoiceOverDelay = 20f;

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

    [Header("Additional Voiceover Subtitles")]
    [TextArea(2, 5)]
    [SerializeField] private string guidanceSubtitle =
        "You\u2019re lucky, it has accepted you. Now, fit the fragment back into place.";
    [TextArea(2, 5)]
    [SerializeField] private string pickupSubtitle =
        "It\u2019s calling to you. Pick it up, maybe something magic will happen.";
    [TextArea(2, 5)]
    [SerializeField] private string gardenTransitionSubtitle =
        "Listen! The old ghosts of the tavern are waking up.";

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
        new CanvasGroup[7];
    private readonly Text[] sceneSubtitleTexts = new Text[7];
    private readonly Coroutine[] sceneSubtitleFades = new Coroutine[7];
    private Coroutine voiceOverSubtitleSequence;
    private Coroutine gardenVoiceOverCompletion;
    private Coroutine musicIntroSequence;
    private Coroutine showObjectsSequence;
    private Coroutine objectIntroCompletion;
    private Coroutine endingBgmSequence;
    private bool bottleIntroCompleted;
    private bool dominoIntroCompleted;
    private bool whistleIntroCompleted;
    private bool tavernAliveStarted;

    public void PlayIntroNarration()
    {
        PlayNarration(introNarrationEvent, "Intro");
    }

    public void PlayRestoreVoiceOver()
    {
        PlayNarration(restoreVoiceOverEvent, "Restore VO");
        PlaySingleVoiceOverSubtitle(4, guidanceSubtitle);
    }

    public void PlayPickupVoiceOver()
    {
        PlayNarration(pickupVoiceOverEvent, "Pickup VO");
        PlaySingleVoiceOverSubtitle(5, pickupSubtitle);
    }

    public void PlayGardenVoiceOver()
    {
        PlayNarration(gardenVoiceOverEvent, "Garden VO");
        PlaySingleVoiceOverSubtitle(6, gardenTransitionSubtitle);

        if (currentNarration.isValid())
            gardenVoiceOverCompletion = StartCoroutine(WaitForGardenVoiceOver());
    }

    public void PlayGrabHornVoiceOver()
    {
        PlayNarration(grabHornVoiceOverEvent, "Grab Horn VO");
    }

    public void PlayHornIntroVoiceOver()
    {
        PlayNarration(hornIntroVoiceOverEvent, "Horn Intro VO");
    }

    public void PlayBottleIntroVoiceOver()
    {
        PlayObjectIntroVoiceOver(
            bottleIntroVoiceOverEvent,
            "Bottle Intro VO",
            ObjectIntroVoiceOver.Bottle
        );
    }

    public void PlayDominoIntroVoiceOver()
    {
        PlayObjectIntroVoiceOver(
            dominoIntroVoiceOverEvent,
            "Domino Intro VO",
            ObjectIntroVoiceOver.Domino
        );
    }

    public void PlayWhistleIntroVoiceOver()
    {
        PlayObjectIntroVoiceOver(
            whistleIntroVoiceOverEvent,
            "Whistle Intro VO",
            ObjectIntroVoiceOver.Whistle
        );
    }

    private void PlayObjectIntroVoiceOver(
        EventReference eventReference,
        string label,
        ObjectIntroVoiceOver voiceOver
    )
    {
        PlayNarration(eventReference, label);

        if (currentNarration.isValid())
        {
            objectIntroCompletion = StartCoroutine(
                WaitForObjectIntroVoiceOver(voiceOver, label)
            );
        }
    }

    private IEnumerator WaitForObjectIntroVoiceOver(
        ObjectIntroVoiceOver voiceOver,
        string label
    )
    {
        while (currentNarration.isValid())
        {
            FMOD.RESULT result = currentNarration.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "[NarrationManager] Failed to read " + label +
                    " playback state: " + result,
                    this
                );
                objectIntroCompletion = null;
                yield break;
            }

            if (playbackState == PLAYBACK_STATE.STOPPED)
                break;

            yield return null;
        }

        StopCurrentNarration(FMOD.Studio.STOP_MODE.IMMEDIATE);
        objectIntroCompletion = null;

        switch (voiceOver)
        {
            case ObjectIntroVoiceOver.Bottle:
                bottleIntroCompleted = true;
                break;
            case ObjectIntroVoiceOver.Domino:
                dominoIntroCompleted = true;
                break;
            case ObjectIntroVoiceOver.Whistle:
                whistleIntroCompleted = true;
                break;
        }

        TryStartTavernAliveAudio();
    }

    private void TryStartTavernAliveAudio()
    {
        if (tavernAliveStarted || !bottleIntroCompleted ||
            !dominoIntroCompleted || !whistleIntroCompleted)
        {
            return;
        }

        tavernAliveStarted = true;
        tavernCrowdPlayer?.PlayAudio();
        streetOutsidePlayer?.PlayAudio();
        PlayNarration(tavernAliveVoiceOverEvent, "Tavern Alive VO");
        endingBgmSequence = StartCoroutine(PlayEndingBgmAfterDelay());
    }

    private IEnumerator PlayEndingBgmAfterDelay()
    {
        yield return new WaitForSeconds(tavernAliveToEndingBgmDelay);
        endingBgmSequence = null;

        if (endingBgmEvent.IsNull)
        {
            Debug.LogWarning(
                "[NarrationManager] Ending BGM event is not assigned.",
                this
            );
            yield break;
        }

        RuntimeManager.PlayOneShot(endingBgmEvent);
        yield return new WaitForSeconds(endingBgmToEndVoiceOverDelay);
        PlayNarration(endVoiceOverEvent, "End VO");
    }

    public void PlayMusicIntroThenShowObjects()
    {
        PlayNarration(musicIntroVoiceOverEvent, "Music Intro VO");

        if (currentNarration.isValid())
        {
            musicIntroSequence = StartCoroutine(
                WaitForMusicIntroThenShowObjects()
            );
        }
    }

    public void StopNarration()
    {
        ClearSubtitleImmediate();

        if (voiceOverSubtitleSequence != null)
        {
            StopCoroutine(voiceOverSubtitleSequence);
            voiceOverSubtitleSequence = null;
        }

        if (gardenVoiceOverCompletion != null)
        {
            StopCoroutine(gardenVoiceOverCompletion);
            gardenVoiceOverCompletion = null;
        }

        if (musicIntroSequence != null)
        {
            StopCoroutine(musicIntroSequence);
            musicIntroSequence = null;
        }

        if (showObjectsSequence != null)
        {
            StopCoroutine(showObjectsSequence);
            showObjectsSequence = null;
        }

        if (objectIntroCompletion != null)
        {
            StopCoroutine(objectIntroCompletion);
            objectIntroCompletion = null;
        }

        if (!currentNarration.isValid())
            return;

        FMOD.Studio.STOP_MODE stopMode = allowFadeoutWhenInterrupted
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
            : FMOD.Studio.STOP_MODE.IMMEDIATE;

        StopCurrentNarration(stopMode);
    }

    private void StopCurrentNarration(FMOD.Studio.STOP_MODE stopMode)
    {
        if (!currentNarration.isValid())
            return;

        currentNarration.setCallback(null, 0);
        currentNarration.setUserData(System.IntPtr.Zero);
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

        StopCurrentNarration(FMOD.Studio.STOP_MODE.IMMEDIATE);
        onGardenVoiceOverFinished?.Invoke();

        if (gardenToMusicianAmbientDelay > 0f)
        {
            yield return new WaitForSeconds(
                gardenToMusicianAmbientDelay
            );
        }

        musicianAmbientPlayer?.PlayAudio();

        yield return new WaitForSeconds(
            Mathf.Max(0f, gardenToTavernVoiceOverDelay)
        );

        gardenVoiceOverCompletion = null;
        PlayNarration(tavernVoiceOverEvent, "Tavern VO");
    }

    private void PlaySingleVoiceOverSubtitle(int index, string text)
    {
        if (!currentNarration.isValid() || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (voiceOverSubtitleSequence != null)
            StopCoroutine(voiceOverSubtitleSequence);

        ShowSubtitle(index, text);
        voiceOverSubtitleSequence = StartCoroutine(
            ClearSubtitleWhenVoiceEnds(index)
        );
    }

    private IEnumerator ClearSubtitleWhenVoiceEnds(int index)
    {
        while (currentNarration.isValid())
        {
            FMOD.RESULT result = currentNarration.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK ||
                playbackState == PLAYBACK_STATE.STOPPED)
            {
                break;
            }

            yield return null;
        }

        ClearSubtitle(index);
        voiceOverSubtitleSequence = null;
    }

    private IEnumerator WaitForMusicIntroThenShowObjects()
    {
        while (currentNarration.isValid())
        {
            FMOD.RESULT result = currentNarration.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "[NarrationManager] Failed to read Music Intro VO playback state: " +
                    result,
                    this
                );
                musicIntroSequence = null;
                yield break;
            }

            if (playbackState == PLAYBACK_STATE.STOPPED)
                break;

            yield return null;
        }

        StopCurrentNarration(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicIntroSequence = null;

        if (objectGroup != null)
            objectGroup.SetActive(true);

        if (uiSfxPlayer != null)
            uiSfxPlayer.PlayHarp();
        else
            PlayShowObjectsVoiceOver();
    }

    public void PlayShowObjectsVoiceOver()
    {
        PlayNarration(showObjectsVoiceOverEvent, "Show Objects VO");

        if (currentNarration.isValid())
        {
            showObjectsSequence = StartCoroutine(
                WaitForShowObjectsVoiceOver()
            );
        }
    }

    private IEnumerator WaitForShowObjectsVoiceOver()
    {
        while (currentNarration.isValid())
        {
            FMOD.RESULT result = currentNarration.getPlaybackState(
                out PLAYBACK_STATE playbackState
            );

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(
                    "[NarrationManager] Failed to read Show Objects VO playback state: " +
                    result,
                    this
                );
                showObjectsSequence = null;
                yield break;
            }

            if (playbackState == PLAYBACK_STATE.STOPPED)
                break;

            yield return null;
        }

        StopCurrentNarration(FMOD.Studio.STOP_MODE.IMMEDIATE);

        float delay = Mathf.Max(0f, showObjectsToMelodyDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        hornController?.PlayFullMelodyRepeats(fullMelodyRepeatCount);
        showObjectsSequence = null;
    }

    private void Awake()
    {
        if (Application.isPlaying && objectGroup != null)
            objectGroup.SetActive(standaloneObjectInteractionTest);

        EnsureSubtitleUI();
        if (Application.isPlaying)
            ClearSubtitleImmediate();
        else
            RefreshSubtitlePreview();
    }

    private IEnumerator Start()
    {
        if (!Application.isPlaying || !standaloneObjectInteractionTest)
            yield break;

        float delay = Mathf.Max(0f, standaloneTestMelodyDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        hornController?.PlayFullMelodyRepeats(fullMelodyRepeatCount);
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
