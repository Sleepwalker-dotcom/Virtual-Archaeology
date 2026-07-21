using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class MuseumExperienceController : MonoBehaviour
{
    public enum MuseumExperienceState
    {
        Initializing,
        MuseumIntro,
        WaitForPiecePickup,
        WaitForPieceInsertion,
        HornRestoration,
        WaitForHornPickup,
        EnvironmentTransition,
        FreeExploration
    }

    public MuseumExperienceState CurrentState { get; private set; }

    [Header("Timeline Directors")]
    [SerializeField]
    private PlayableDirector museumIntroDirector;

    [SerializeField]
    private PlayableDirector hornRestorationDirector;

    [SerializeField]
    private PlayableDirector environmentTransitionDirector;

    [Header("Piece Interaction")]
    [SerializeField]
    private XRGrabInteractable pieceGrab;

    [SerializeField]
    private XRSocketInteractor pieceSocket;

    [SerializeField]
    private Transform pieceRoot;

    [SerializeField]
    private Transform pieceSnapTarget;

    [SerializeField]
    private Rigidbody pieceRigidbody;

    [SerializeField]
    private Collider[] pieceColliders;

    [Header("Horn Interaction")]
    [SerializeField]
    private XRGrabInteractable completeHornGrab;

    [SerializeField]
    private GameObject completeHornRoot;

    [SerializeField]
    private HornFMODController hornPerformanceController;

    [Header("Presentation Objects")]
    [SerializeField]
    private GameObject restorationVfxRoot;

    [SerializeField]
    private Light pieceGuideLight;

    [SerializeField]
    private Light socketGuideLight;

    [SerializeField]
    private Light completeHornGuideLight;

    [SerializeField]
    private CanvasGroup transitionCanvasGroup;

    [Header("Guide Light Settings")]
    [Tooltip("三盏引导灯共用的渐亮和渐暗时间。")]
    [SerializeField, Min(0f)]
    private float guideLightFadeDuration = 1f;

    [Tooltip("提示玩家拿起 Piece of Horn 的灯光最终亮度。")]
    [SerializeField, Min(0f)]
    private float pieceGuideIntensity = 3f;

    [Tooltip("提示玩家把 Piece 放入 Horn 的灯光最终亮度。")]
    [SerializeField, Min(0f)]
    private float socketGuideIntensity = 3f;

    [Tooltip("提示玩家拿起完整 Horn 的灯光最终亮度。")]
    [SerializeField, Min(0f)]
    private float completeHornGuideIntensity = 3f;

    [Header("Transition Settings")]
    [SerializeField, Min(0f)]
    private float transitionFadeDuration = 0.6f;

    [Header("Existing Audio and Environment System")]
    [SerializeField]
    private SingleSceneAudioEnvironmentManager audioEnvironmentManager;

    [Header("Optional Timeline Audio Hooks")]
    [SerializeField]
    private UnityEvent onPlayIntroNarration;

    [SerializeField]
    private UnityEvent onPlayRestorationSound;

    [SerializeField]
    private UnityEvent onPlayRestorationNarration;

    [SerializeField]
    private UnityEvent onFreeExplorationStarted;

    [Header("Debug")]
    [SerializeField]
    private bool logStateChanges = true;

    private bool piecePickupHandled;
    private bool pieceInsertionHandled;
    private bool hornPickupHandled;
    private bool assemblyCommitted;

    private Coroutine pieceLightCoroutine;
    private Coroutine socketLightCoroutine;
    private Coroutine hornLightCoroutine;
    private Coroutine transitionFadeCoroutine;

    private void Awake()
    {
        InitializeExperience();
    }

    private void OnEnable()
    {
        if (pieceGrab != null)
        {
            pieceGrab.selectEntered.AddListener(
                HandlePieceSelected
            );
        }

        if (pieceSocket != null)
        {
            pieceSocket.selectEntered.AddListener(
                HandleSocketSelected
            );
        }

        if (completeHornGrab != null)
        {
            completeHornGrab.selectEntered.AddListener(
                HandleCompleteHornSelected
            );
        }
    }

    private void Start()
    {
        SetState(MuseumExperienceState.MuseumIntro);

        if (museumIntroDirector != null &&
            museumIntroDirector.playableAsset != null)
        {
            museumIntroDirector.Play();
        }
        else
        {
            UnlockPiece();
        }
    }

    private void OnDisable()
    {
        if (pieceGrab != null)
        {
            pieceGrab.selectEntered.RemoveListener(
                HandlePieceSelected
            );
        }

        if (pieceSocket != null)
        {
            pieceSocket.selectEntered.RemoveListener(
                HandleSocketSelected
            );
        }

        if (completeHornGrab != null)
        {
            completeHornGrab.selectEntered.RemoveListener(
                HandleCompleteHornSelected
            );
        }
    }

    private void InitializeExperience()
    {
        SetState(MuseumExperienceState.Initializing);

        piecePickupHandled = false;
        pieceInsertionHandled = false;
        hornPickupHandled = false;
        assemblyCommitted = false;

        SetEnabled(pieceGrab, false);
        SetEnabled(pieceSocket, false);
        SetEnabled(completeHornGrab, false);

        if (pieceRoot != null)
        {
            pieceRoot.gameObject.SetActive(true);
        }

        if (completeHornRoot != null)
        {
            completeHornRoot.SetActive(false);
        }

        if (hornPerformanceController != null)
        {
            hornPerformanceController.enabled = false;
        }

        if (restorationVfxRoot != null)
        {
            restorationVfxRoot.SetActive(false);
        }

        // 三盏引导灯进入场景时全部关闭。
        SetLightIntensity(pieceGuideLight, 0f);
        SetLightIntensity(socketGuideLight, 0f);
        SetLightIntensity(completeHornGuideLight, 0f);

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    // =========================================================
    // Museum Intro Timeline 调用
    // =========================================================

    public void PlayIntroNarration()
    {
        onPlayIntroNarration?.Invoke();
    }

    /// <summary>
    /// 由 Museum Intro Timeline 的 Signal 调用。
    /// 点亮 Piece of Horn 的引导灯。
    /// </summary>
    public void ShowPieceGuide()
    {
        FadeGuidanceLight(
            ref pieceLightCoroutine,
            pieceGuideLight,
            pieceGuideIntensity
        );
    }

    /// <summary>
    /// 由 Museum Intro Timeline 最后的 Signal 调用。
    /// 解锁 Piece of Horn。
    /// </summary>
    public void UnlockPiece()
    {
        if (CurrentState != MuseumExperienceState.MuseumIntro)
        {
            return;
        }

        SetEnabled(pieceGrab, true);

        SetState(MuseumExperienceState.WaitForPiecePickup);
    }

    // =========================================================
    // Piece of Horn 拿取阶段
    // =========================================================

    private void HandlePieceSelected(
        SelectEnterEventArgs args
    )
    {
        if (CurrentState !=
            MuseumExperienceState.WaitForPiecePickup ||
            piecePickupHandled)
        {
            return;
        }

        piecePickupHandled = true;

        SetState(MuseumExperienceState.WaitForPieceInsertion);

        // 开启 Horn 缺口的 Socket。
        SetEnabled(pieceSocket, true);

        // Piece 灯熄灭。
        FadeGuidanceLight(
            ref pieceLightCoroutine,
            pieceGuideLight,
            0f
        );

        // Socket 灯按照独立亮度设置渐亮。
        FadeGuidanceLight(
            ref socketLightCoroutine,
            socketGuideLight,
            socketGuideIntensity
        );
    }

    // =========================================================
    // Piece 插入 Horn 阶段
    // =========================================================

    private void HandleSocketSelected(
        SelectEnterEventArgs args
    )
    {
        if (CurrentState !=
            MuseumExperienceState.WaitForPieceInsertion ||
            pieceInsertionHandled)
        {
            return;
        }

        // 确认插入 Socket 的物体是指定 Piece。
        if (pieceGrab == null ||
            args.interactableObject.transform !=
            pieceGrab.transform)
        {
            return;
        }

        pieceInsertionHandled = true;

        SetState(MuseumExperienceState.HornRestoration);

        LockPieceIntoSocket();

        // Piece 插入后关闭 Socket 提示灯。
        FadeGuidanceLight(
            ref socketLightCoroutine,
            socketGuideLight,
            0f
        );

        if (hornRestorationDirector != null &&
            hornRestorationDirector.playableAsset != null)
        {
            hornRestorationDirector.Play();
        }
        else
        {
            CommitHornAssembly();
            CompleteHornRestoration();
        }
    }

    private void LockPieceIntoSocket()
    {
        SetEnabled(pieceGrab, false);
        SetEnabled(pieceSocket, false);

        if (pieceRigidbody != null)
        {
            pieceRigidbody.linearVelocity = Vector3.zero;
            pieceRigidbody.angularVelocity = Vector3.zero;
            pieceRigidbody.isKinematic = true;
        }

        if (pieceColliders != null)
        {
            foreach (Collider pieceCollider in pieceColliders)
            {
                if (pieceCollider != null)
                {
                    pieceCollider.enabled = false;
                }
            }
        }

        if (pieceRoot != null && pieceSnapTarget != null)
        {
            pieceRoot.SetParent(pieceSnapTarget, false);
            pieceRoot.localPosition = Vector3.zero;
            pieceRoot.localRotation = Quaternion.identity;
        }
    }

    // =========================================================
    // Horn Restoration Timeline 调用
    // =========================================================

    public void ShowRestorationVfx()
    {
        if (restorationVfxRoot != null)
        {
            restorationVfxRoot.SetActive(true);
        }
    }

    public void HideRestorationVfx()
    {
        if (restorationVfxRoot != null)
        {
            restorationVfxRoot.SetActive(false);
        }
    }

    public void PlayRestorationSound()
    {
        onPlayRestorationSound?.Invoke();
    }

    public void PlayRestorationNarration()
    {
        onPlayRestorationNarration?.Invoke();
    }

    public void CommitHornAssembly()
    {
        if (assemblyCommitted)
        {
            return;
        }

        assemblyCommitted = true;

        if (pieceRoot != null)
        {
            pieceRoot.gameObject.SetActive(false);
        }

        if (completeHornRoot != null)
        {
            completeHornRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 由 Horn Restoration Timeline 的 Signal 调用。
    /// 点亮完整 Horn 的引导灯。
    /// </summary>
    public void ShowCompleteHornGuide()
    {
        FadeGuidanceLight(
            ref hornLightCoroutine,
            completeHornGuideLight,
            completeHornGuideIntensity
        );
    }

    /// <summary>
    /// 由 Horn Restoration Timeline 最后的 Signal 调用。
    /// 解锁完整 Horn。
    /// </summary>
    public void CompleteHornRestoration()
    {
        if (CurrentState !=
            MuseumExperienceState.HornRestoration)
        {
            return;
        }

        CommitHornAssembly();
        HideRestorationVfx();

        SetEnabled(completeHornGrab, true);

        SetState(MuseumExperienceState.WaitForHornPickup);
    }

    // =========================================================
    // 完整 Horn 拿取阶段
    // =========================================================

    private void HandleCompleteHornSelected(
        SelectEnterEventArgs args
    )
    {
        if (CurrentState !=
            MuseumExperienceState.WaitForHornPickup ||
            hornPickupHandled)
        {
            return;
        }

        hornPickupHandled = true;

        SetState(MuseumExperienceState.EnvironmentTransition);

        // 玩家拿起完整 Horn 后关闭提示灯。
        FadeGuidanceLight(
            ref hornLightCoroutine,
            completeHornGuideLight,
            0f
        );

        if (environmentTransitionDirector != null &&
            environmentTransitionDirector.playableAsset != null)
        {
            environmentTransitionDirector.Play();
        }
        else
        {
            BeginEnvironmentAudioTransition();
            CompleteEnvironmentTransition();
        }
    }

    // =========================================================
    // Environment Transition Timeline 调用
    // =========================================================

    public void BeginEnvironmentAudioTransition()
    {
        FadeTransitionCanvas(1f);

        if (audioEnvironmentManager != null)
        {
            audioEnvironmentManager.SwitchToTavern();
        }
    }

    public void FadeFromBlack()
    {
        FadeTransitionCanvas(0f);
    }

    public void CompleteEnvironmentTransition()
    {
        if (CurrentState !=
            MuseumExperienceState.EnvironmentTransition)
        {
            return;
        }

        SetState(MuseumExperienceState.FreeExploration);

        if (hornPerformanceController != null)
        {
            hornPerformanceController.enabled = true;
            hornPerformanceController.ActivateHorn();
        }

        onFreeExplorationStarted?.Invoke();
    }

    // =========================================================
    // 灯光渐变
    // =========================================================

    private void FadeGuidanceLight(
        ref Coroutine routine,
        Light targetLight,
        float targetIntensity
    )
    {
        if (targetLight == null)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(
            FadeLight(targetLight, targetIntensity)
        );
    }

    private IEnumerator FadeLight(
        Light targetLight,
        float targetIntensity
    )
    {
        if (targetLight == null)
        {
            yield break;
        }

        targetLight.enabled = true;

        float startIntensity = targetLight.intensity;
        float elapsed = 0f;

        if (guideLightFadeDuration <= 0f)
        {
            targetLight.intensity = targetIntensity;
            yield break;
        }

        while (elapsed < guideLightFadeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / guideLightFadeDuration
            );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            targetLight.intensity = Mathf.Lerp(
                startIntensity,
                targetIntensity,
                progress
            );

            yield return null;
        }

        targetLight.intensity = targetIntensity;
    }

    // =========================================================
    // 画面淡入淡出
    // =========================================================

    private void FadeTransitionCanvas(float targetAlpha)
    {
        if (transitionCanvasGroup == null)
        {
            return;
        }

        if (transitionFadeCoroutine != null)
        {
            StopCoroutine(transitionFadeCoroutine);
        }

        transitionFadeCoroutine = StartCoroutine(
            FadeCanvas(targetAlpha)
        );
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = transitionCanvasGroup.alpha;
        float elapsed = 0f;

        transitionCanvasGroup.blocksRaycasts =
            targetAlpha > 0f;

        if (transitionFadeDuration <= 0f)
        {
            transitionCanvasGroup.alpha = targetAlpha;
            transitionCanvasGroup.blocksRaycasts =
                targetAlpha > 0f;

            transitionFadeCoroutine = null;
            yield break;
        }

        while (elapsed < transitionFadeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / transitionFadeDuration
            );

            transitionCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            yield return null;
        }

        transitionCanvasGroup.alpha = targetAlpha;
        transitionCanvasGroup.blocksRaycasts =
            targetAlpha > 0f;

        transitionFadeCoroutine = null;
    }

    // =========================================================
    // 通用工具
    // =========================================================

    private static void SetEnabled(
        Behaviour behaviour,
        bool enabled
    )
    {
        if (behaviour != null)
        {
            behaviour.enabled = enabled;
        }
    }

    private static void SetLightIntensity(
        Light targetLight,
        float intensity
    )
    {
        if (targetLight == null)
        {
            return;
        }

        targetLight.enabled = true;
        targetLight.intensity = intensity;
    }

    private void SetState(MuseumExperienceState state)
    {
        CurrentState = state;

        if (logStateChanges)
        {
            Debug.Log(
                "[MuseumExperience] " + GetStateMessage(state),
                this
            );
        }
    }

    private static string GetStateMessage(
        MuseumExperienceState state
    )
    {
        switch (state)
        {
            case MuseumExperienceState.Initializing:
                return "步骤 0/7：初始化体验，暂时锁定交互。";
            case MuseumExperienceState.MuseumIntro:
                return "步骤 1/7：正在播放 Museum Intro Timeline。";
            case MuseumExperienceState.WaitForPiecePickup:
                return "步骤 2/7：Intro 完成，请拿起 Horn_Piece。";
            case MuseumExperienceState.WaitForPieceInsertion:
                return "步骤 3/7：已拿起 Horn_Piece，请放入 PieceSocket。";
            case MuseumExperienceState.HornRestoration:
                return "步骤 4/7：碎片已插入，正在修复 Horn。";
            case MuseumExperienceState.WaitForHornPickup:
                return "步骤 5/7：Horn 修复完成，请拿起完整 Horn。";
            case MuseumExperienceState.EnvironmentTransition:
                return "步骤 6/7：正在执行 Museum 到 Tavern 的 FMOD 过渡。";
            case MuseumExperienceState.FreeExploration:
                return "步骤 7/7：过渡完成，Horn 演奏和自由探索已启用。";
            default:
                return "未知体验状态：" + state;
        }
    }
}
