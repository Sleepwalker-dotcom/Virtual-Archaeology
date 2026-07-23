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

    public enum TestStartPoint
    {
        Normal,
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

    [Header("Horn Restoration")]
    [SerializeField]
    private XRGrabInteractable hornPickupGrab;

    [SerializeField]
    private Rigidbody hornPickupRigidbody;

    [SerializeField]
    private Renderer hornMouthRenderer;

    [SerializeField, Min(0)]
    private int hornMouthMaterialSlot;

    [SerializeField]
    private Material hornMat;

    [SerializeField]
    private UnityEvent onHornPieceInsertedVfx;

    [SerializeField]
    private UnityEvent onHornPieceInsertedSound;

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

    [Header("Debug / Testing")]
    [Tooltip("Start Play Mode directly from the selected experience stage.")]
    [SerializeField]
    private bool enableTestMode;

    [SerializeField]
    private TestStartPoint testStartPoint = TestStartPoint.Normal;

    [SerializeField]
    private bool logStateChanges = true;

    private bool piecePickupHandled;
    private bool pieceInsertionHandled;
    private bool hornPickupHandled;
    private bool assemblyCommitted;
    private bool hornGuideStarted;

    private Coroutine pieceLightCoroutine;
    private Coroutine socketLightCoroutine;
    private Coroutine hornLightCoroutine;
    private Coroutine transitionFadeCoroutine;
    private GentleHoverEffect hornHoverEffect;
    private Material[] originalHornMouthMaterials;
    private Vector3 completeHornTavernLocalPosition;
    private Quaternion completeHornTavernLocalRotation;
    private bool hasCompleteHornTavernPose;

    private void Awake()
    {
        CaptureCompleteHornTavernPose();
        PrepareHornRestorationObjects();
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

        if (hornPickupGrab != null)
        {
            hornPickupGrab.selectEntered.AddListener(
                HandleCompleteHornSelected
            );
        }
    }

    private void Start()
    {
        if (enableTestMode &&
            testStartPoint != TestStartPoint.Normal)
        {
            ApplyTestStartPoint(testStartPoint);
            return;
        }

        StartNormalExperience();
    }

    private void StartNormalExperience()
    {
        SetState(MuseumExperienceState.MuseumIntro);

        if (museumIntroDirector != null &&
            museumIntroDirector.playableAsset != null)
        {
            museumIntroDirector.time = 0d;
            museumIntroDirector.Play();
        }
        else
        {
            UnlockPiece();
        }
    }

    private void ApplyTestStartPoint(TestStartPoint startPoint)
    {
        StopAllTimelines();
        SetTransitionCanvasImmediate(0f);

        switch (startPoint)
        {
            case TestStartPoint.WaitForPiecePickup:
                StartCoroutine(SetupWaitForPiecePickup());
                break;
            case TestStartPoint.WaitForPieceInsertion:
                SetupWaitForPieceInsertion();
                break;
            case TestStartPoint.HornRestoration:
                SetupHornRestoration();
                break;
            case TestStartPoint.WaitForHornPickup:
                SetupWaitForHornPickup();
                break;
            case TestStartPoint.EnvironmentTransition:
                SetupEnvironmentTransition();
                break;
            case TestStartPoint.FreeExploration:
                SetupFreeExploration();
                break;
            default:
                StartNormalExperience();
                break;
        }
    }

    private IEnumerator SetupWaitForPiecePickup()
    {
        ResetProgressFlags();
        SetCompleteHornState(false, false, false);
        SetPieceSocketActive(false);
        SetGuideLights(0f, 0f, 0f);
        FadeGuidanceLight(
            ref pieceLightCoroutine,
            pieceGuideLight,
            pieceGuideIntensity
        );

        if (guideLightFadeDuration > 0f)
        {
            yield return new WaitForSeconds(
                guideLightFadeDuration
            );
        }

        RestorePieceInteractionState();
        SetState(MuseumExperienceState.WaitForPiecePickup);
    }

    private void SetupWaitForPieceInsertion()
    {
        ResetProgressFlags();
        piecePickupHandled = true;
        RestorePieceInteractionState();
        SetCompleteHornState(false, false, false);
        SetGuideLights(0f, 0f, 0f);
        SetState(MuseumExperienceState.HornRestoration);
        ShowCompleteHornGuide();
    }

    private void SetupHornRestoration()
    {
        ResetProgressFlags();
        piecePickupHandled = true;
        RestorePieceInteractionState();
        SetCompleteHornState(false, false, false);
        SetState(MuseumExperienceState.HornRestoration);
        SetGuideLights(0f, 0f, 0f);

        if (hornRestorationDirector != null &&
            hornRestorationDirector.playableAsset != null)
        {
            hornRestorationDirector.time = 0d;
            hornRestorationDirector.Play();
        }
        else
        {
            CommitHornAssembly();
            CompleteHornRestoration();
        }
    }

    private void SetupWaitForHornPickup()
    {
        ResetProgressFlags();
        piecePickupHandled = true;
        pieceInsertionHandled = true;
        SetState(MuseumExperienceState.HornRestoration);
        CommitHornAssembly();
        ReplaceHornMouthMaterial();
        HideRestorationVfx();
        SetCompleteHornState(true, true, false);
        SetCompleteHornPhysicsLocked(true);
        if (hornHoverEffect != null)
        {
            hornHoverEffect.Play();
        }
        SetGuideLights(0f, 0f, completeHornGuideIntensity);
        SetState(MuseumExperienceState.WaitForHornPickup);
    }

    private void SetupEnvironmentTransition()
    {
        ResetProgressFlags();
        piecePickupHandled = true;
        pieceInsertionHandled = true;
        hornPickupHandled = true;
        SetState(MuseumExperienceState.HornRestoration);
        CommitHornAssembly();
        SetCompleteHornState(true, false, false);
        SetGuideLights(0f, 0f, 0f);
        SetState(MuseumExperienceState.EnvironmentTransition);

        if (environmentTransitionDirector != null &&
            environmentTransitionDirector.playableAsset != null)
        {
            environmentTransitionDirector.time = 0d;
            environmentTransitionDirector.Play();
        }
        else
        {
            BeginEnvironmentAudioTransition();
            CompleteEnvironmentTransition();
        }
    }

    private void SetupFreeExploration()
    {
        ResetProgressFlags();
        piecePickupHandled = true;
        pieceInsertionHandled = true;
        hornPickupHandled = true;
        SetState(MuseumExperienceState.HornRestoration);
        CommitHornAssembly();
        SetCompleteHornState(true, true, false);
        SetGuideLights(0f, 0f, 0f);

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }

        SetState(MuseumExperienceState.EnvironmentTransition);

        if (audioEnvironmentManager != null)
        {
            audioEnvironmentManager.SwitchToTavern();
        }

        CompleteEnvironmentTransition();
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

        if (hornPickupGrab != null)
        {
            hornPickupGrab.selectEntered.RemoveListener(
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
        hornGuideStarted = false;

        SetEnabled(pieceGrab, false);
        SetPieceSocketActive(false);
        SetEnabled(completeHornGrab, false);
        SetEnabled(hornPickupGrab, false);
        SetPickupHornPhysicsLocked(true);

        if (hornHoverEffect != null)
        {
            hornHoverEffect.StopImmediately();
        }

        RestoreHornMouthMaterial();

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
            SetTransitionCanvasImmediate(1f);
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
        FadeTransitionCanvas(0f);

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

        SetState(MuseumExperienceState.HornRestoration);
        Debug.Log(
            "[MuseumExperience] Horn_Piece picked up; starting HornRestorationDirector.",
            this
        );

        FadeGuidanceLight(
            ref pieceLightCoroutine,
            pieceGuideLight,
            0f
        );

        FadeGuidanceLight(
            ref socketLightCoroutine,
            socketGuideLight,
            0f
        );

        if (hornRestorationDirector != null &&
            hornRestorationDirector.playableAsset != null)
        {
            hornRestorationDirector.time = 0d;
            hornRestorationDirector.Play();
        }
        else
        {
            CommitHornAssembly();
            CompleteHornRestoration();
        }
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
        if (args == null ||
            args.interactorObject.transform !=
                pieceSocket.transform ||
            args.interactableObject.transform !=
                pieceGrab.transform)
        {
            return;
        }

        pieceInsertionHandled = true;

        LockPieceIntoSocket();
        CommitHornAssembly();
        ReplaceHornMouthMaterial();
        UnlockCompleteHornForPickup();
        Debug.Log(
            "[MuseumExperience] Horn_Piece inserted; material replaced, Horn_incomplete unlocked, restoration resumed.",
            this
        );
        onHornPieceInsertedVfx?.Invoke();
        onHornPieceInsertedSound?.Invoke();

        FadeGuidanceLight(
            ref socketLightCoroutine,
            socketGuideLight,
            0f
        );

        if (hornRestorationDirector != null &&
            hornRestorationDirector.playableAsset != null)
        {
            hornRestorationDirector.Resume();
        }
        else
        {
            CompleteHornRestoration();
        }
    }

    private void LockPieceIntoSocket()
    {
        SetEnabled(pieceGrab, false);
        SetPieceSocketActive(false);

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
        if (hornGuideStarted || pieceInsertionHandled)
        {
            return;
        }

        hornGuideStarted = true;

        if (completeHornRoot != null)
        {
            completeHornRoot.SetActive(true);
        }

        SetCompleteHornPhysicsLocked(true);
        SetEnabled(completeHornGrab, false);
        SetPieceSocketActive(true);

        if (hornHoverEffect != null)
        {
            hornHoverEffect.Play();
        }

        FadeGuidanceLight(
            ref hornLightCoroutine,
            completeHornGuideLight,
            completeHornGuideIntensity
        );

        SetState(MuseumExperienceState.WaitForPieceInsertion);
        Debug.Log(
            "[MuseumExperience] ShowCompleteHornGuide; hover started, Socket opened, restoration paused.",
            this
        );

        if (hornRestorationDirector != null)
        {
            hornRestorationDirector.Pause();
        }
    }

    private void PrepareHornRestorationObjects()
    {
        if (completeHornRoot == null)
        {
            return;
        }

        if (hornMouthRenderer == null)
        {
            hornMouthRenderer =
                completeHornRoot.GetComponentInChildren<Renderer>(
                    true
                );
        }

        if (hornMouthRenderer != null)
        {
            originalHornMouthMaterials =
                hornMouthRenderer.sharedMaterials;
        }

        PrepareHornPickupCollider();

        Transform hornTransform = completeHornRoot.transform;
        Transform pivotTransform = hornTransform.parent;

        if (pivotTransform == null ||
            pivotTransform.name != "HornGuidePivot")
        {
            GameObject pivotObject =
                new GameObject("HornGuidePivot");
            pivotTransform = pivotObject.transform;
            pivotTransform.SetParent(
                hornTransform.parent,
                true
            );
            pivotTransform.SetPositionAndRotation(
                hornTransform.position,
                hornTransform.rotation
            );
            pivotTransform.localScale = Vector3.one;
            hornTransform.SetParent(pivotTransform, true);
        }

        hornTransform.SetLocalPositionAndRotation(
            Vector3.zero,
            Quaternion.identity
        );

        hornHoverEffect =
            pivotTransform.GetComponent<GentleHoverEffect>();

        if (hornHoverEffect == null)
        {
            hornHoverEffect =
                pivotTransform.gameObject
                    .AddComponent<GentleHoverEffect>();
        }

        hornHoverEffect.CaptureCurrentPoseAsBase();
    }

    private void SetPieceSocketActive(bool active)
    {
        if (pieceSocket == null)
        {
            return;
        }

        pieceSocket.enabled = true;
        pieceSocket.socketActive = active;
    }

    private void PrepareHornPickupCollider()
    {
        if (hornPickupGrab == null)
        {
            return;
        }

        Collider pickupCollider =
            hornPickupGrab.GetComponent<Collider>();

        if (pickupCollider == null)
        {
            Renderer pickupRenderer =
                hornPickupGrab.GetComponentInChildren<Renderer>(
                    true
                );

            if (pickupRenderer == null)
            {
                Debug.LogWarning(
                    "[MuseumExperience] Horn_incomplete has no Renderer for pickup collider bounds.",
                    this
                );
                return;
            }

            Transform rootTransform =
                hornPickupGrab.transform;
            Bounds bounds = pickupRenderer.bounds;
            Vector3 scale = rootTransform.lossyScale;
            BoxCollider boxCollider =
                hornPickupGrab.gameObject
                    .AddComponent<BoxCollider>();

            boxCollider.center =
                rootTransform.InverseTransformPoint(
                    bounds.center
                );
            boxCollider.size = new Vector3(
                bounds.size.x /
                    Mathf.Max(
                        Mathf.Abs(scale.x),
                        0.0001f
                    ),
                bounds.size.y /
                    Mathf.Max(
                        Mathf.Abs(scale.y),
                        0.0001f
                    ),
                bounds.size.z /
                    Mathf.Max(
                        Mathf.Abs(scale.z),
                        0.0001f
                    )
            );
            pickupCollider = boxCollider;
        }

        if (!hornPickupGrab.colliders.Contains(
                pickupCollider
            ))
        {
            hornPickupGrab.colliders.Add(pickupCollider);
        }
    }

    private void ReplaceHornMouthMaterial()
    {
        if (hornMouthRenderer == null || hornMat == null)
        {
            Debug.LogWarning(
                "[MuseumExperience] Horn Mouth Renderer or Horn_Mat is not assigned.",
                this
            );
            return;
        }

        Material[] materials =
            hornMouthRenderer.sharedMaterials;

        if (hornMouthMaterialSlot < 0 ||
            hornMouthMaterialSlot >= materials.Length)
        {
            Debug.LogError(
                "[MuseumExperience] Horn Mouth material slot is invalid.",
                this
            );
            return;
        }

        string previousMaterialName =
            materials[hornMouthMaterialSlot] != null
                ? materials[hornMouthMaterialSlot].name
                : "None";

        materials[hornMouthMaterialSlot] = hornMat;
        hornMouthRenderer.sharedMaterials = materials;

        Debug.Log(
            "[MuseumExperience] Horn Mouth material changed: " +
            previousMaterialName + " -> " + hornMat.name +
            " on " + hornMouthRenderer.name + ".",
            this
        );
    }

    private void RestoreHornMouthMaterial()
    {
        if (hornMouthRenderer == null ||
            originalHornMouthMaterials == null)
        {
            return;
        }

        hornMouthRenderer.sharedMaterials =
            originalHornMouthMaterials;
    }

    private void UnlockCompleteHornForPickup()
    {
        SetEnabled(completeHornGrab, false);

        if (hornPickupGrab != null)
        {
            hornPickupGrab.gameObject.SetActive(true);
        }

        SetPickupHornPhysicsLocked(true);
        SetEnabled(hornPickupGrab, false);
        SetEnabled(hornPickupGrab, true);
        SetState(MuseumExperienceState.WaitForHornPickup);

        Debug.Log(
            "[MuseumExperience] Horn_incomplete pickup ready. " +
            "Active=" +
            hornPickupGrab.gameObject.activeInHierarchy +
            ", GrabEnabled=" + hornPickupGrab.enabled +
            ", Colliders=" +
            hornPickupGrab.colliders.Count +
            ", IsKinematic=" +
            hornPickupRigidbody.isKinematic + ".",
            this
        );
    }

    private void StopHornHoverWithoutMovingHorn()
    {
        if (hornHoverEffect == null ||
            completeHornRoot == null)
        {
            return;
        }

        Transform hornTransform = completeHornRoot.transform;
        Vector3 worldPosition = hornTransform.position;
        Quaternion worldRotation = hornTransform.rotation;

        hornHoverEffect.StopImmediately();
        hornTransform.SetPositionAndRotation(
            worldPosition,
            worldRotation
        );
    }

    /// <summary>
    /// 由 Horn Restoration Timeline 最后的 Signal 调用。
    /// 解锁完整 Horn。
    /// </summary>
    public void CompleteHornRestoration()
    {
        if (CurrentState !=
                MuseumExperienceState.HornRestoration &&
            CurrentState !=
                MuseumExperienceState.WaitForHornPickup)
        {
            return;
        }

        CommitHornAssembly();
        HideRestorationVfx();

        UnlockCompleteHornForPickup();
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

        StopHornHoverWithoutMovingHorn();
        SetPickupHornPhysicsLocked(false);

        SetState(MuseumExperienceState.EnvironmentTransition);
        Debug.Log(
            "[MuseumExperience] Horn_incomplete first grabbed; EnvironmentTransitionDirector restarted.",
            this
        );

        // 玩家拿起完整 Horn 后关闭提示灯。
        FadeGuidanceLight(
            ref hornLightCoroutine,
            completeHornGuideLight,
            0f
        );

        if (environmentTransitionDirector != null &&
            environmentTransitionDirector.playableAsset != null)
        {
            if (hornRestorationDirector != null &&
                hornRestorationDirector.state ==
                    PlayState.Playing)
            {
                hornRestorationDirector.Pause();
            }

            environmentTransitionDirector.Stop();
            environmentTransitionDirector.time = 0d;
            environmentTransitionDirector.Evaluate();
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

        HideHornPickup();
        SetState(MuseumExperienceState.FreeExploration);

        RestoreCompleteHornTavernPose();

        if (completeHornRoot != null)
        {
            completeHornRoot.SetActive(true);
        }

        SetCompleteHornPhysicsLocked(true);
        SetEnabled(completeHornGrab, true);

        if (hornPerformanceController != null)
        {
            hornPerformanceController.enabled = true;
            hornPerformanceController.ActivateHorn();
        }

        onFreeExplorationStarted?.Invoke();
    }

    private void HideHornPickup()
    {
        if (hornPickupGrab == null)
        {
            return;
        }

        SetEnabled(hornPickupGrab, false);
        hornPickupGrab.gameObject.SetActive(false);

        Debug.Log(
            "[MuseumExperience] Horn_incomplete hidden at the end of the Museum transition.",
            this
        );
    }

    private void CaptureCompleteHornTavernPose()
    {
        if (completeHornRoot == null)
        {
            return;
        }

        Transform hornTransform = completeHornRoot.transform;
        Transform poseTransform =
            hornTransform.parent != null &&
            hornTransform.parent.name == "HornGuidePivot"
                ? hornTransform.parent
                : hornTransform;

        completeHornTavernLocalPosition =
            poseTransform.localPosition;
        completeHornTavernLocalRotation =
            poseTransform.localRotation;
        hasCompleteHornTavernPose = true;
    }

    private void RestoreCompleteHornTavernPose()
    {
        if (!hasCompleteHornTavernPose ||
            completeHornRoot == null)
        {
            return;
        }

        Transform hornTransform = completeHornRoot.transform;
        Transform poseTransform =
            hornHoverEffect != null
                ? hornHoverEffect.transform
                : hornTransform;

        poseTransform.SetLocalPositionAndRotation(
            completeHornTavernLocalPosition,
            completeHornTavernLocalRotation
        );

        if (poseTransform != hornTransform)
        {
            hornTransform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity
            );
        }

        Debug.Log(
            "[MuseumExperience] French Natural Horn restored to its Tavern position.",
            this
        );
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

    private void SetTransitionCanvasImmediate(float targetAlpha)
    {
        if (transitionCanvasGroup == null)
        {
            return;
        }

        if (transitionFadeCoroutine != null)
        {
            StopCoroutine(transitionFadeCoroutine);
            transitionFadeCoroutine = null;
        }

        transitionCanvasGroup.alpha = targetAlpha;
        transitionCanvasGroup.blocksRaycasts = targetAlpha > 0f;
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

    private void StopAllTimelines()
    {
        StopTimeline(museumIntroDirector);
        StopTimeline(hornRestorationDirector);
        StopTimeline(environmentTransitionDirector);
    }

    private static void StopTimeline(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        if (director.state == PlayState.Playing)
        {
            director.Stop();
        }

        director.time = 0d;
    }

    private void ResetProgressFlags()
    {
        piecePickupHandled = false;
        pieceInsertionHandled = false;
        hornPickupHandled = false;
        assemblyCommitted = false;
        hornGuideStarted = false;

        SetPieceSocketActive(false);
        RestoreHornMouthMaterial();

        if (hornHoverEffect != null)
        {
            hornHoverEffect.StopImmediately();
        }

        if (restorationVfxRoot != null)
        {
            restorationVfxRoot.SetActive(false);
        }
    }

    private void RestorePieceInteractionState()
    {
        if (pieceRoot != null)
        {
            pieceRoot.gameObject.SetActive(true);
        }

        if (pieceRigidbody != null)
        {
            pieceRigidbody.isKinematic = false;
        }

        if (pieceColliders != null)
        {
            foreach (Collider pieceCollider in pieceColliders)
            {
                if (pieceCollider != null)
                {
                    pieceCollider.enabled = true;
                }
            }
        }

        SetEnabled(pieceGrab, true);
    }

    private void SetCompleteHornState(
        bool visible,
        bool grabEnabled,
        bool performanceEnabled
    )
    {
        if (completeHornRoot != null)
        {
            completeHornRoot.SetActive(visible);
        }

        SetEnabled(completeHornGrab, grabEnabled);
        SetCompleteHornPhysicsLocked(visible && grabEnabled);

        if (hornPerformanceController != null)
        {
            hornPerformanceController.enabled = performanceEnabled;
        }
    }

    private void SetGuideLights(
        float pieceIntensity,
        float socketIntensity,
        float hornIntensity
    )
    {
        SetLightIntensity(pieceGuideLight, pieceIntensity);
        SetLightIntensity(socketGuideLight, socketIntensity);
        SetLightIntensity(completeHornGuideLight, hornIntensity);
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

    private void SetCompleteHornPhysicsLocked(bool locked)
    {
        if (completeHornGrab == null)
        {
            return;
        }

        Rigidbody body = completeHornGrab.GetComponent<Rigidbody>();

        if (body == null)
        {
            return;
        }

        if (locked)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.useGravity = !locked;
        body.isKinematic = locked;
    }

    private void SetPickupHornPhysicsLocked(bool locked)
    {
        if (hornPickupRigidbody == null)
        {
            return;
        }

        if (locked)
        {
            hornPickupRigidbody.linearVelocity =
                Vector3.zero;
            hornPickupRigidbody.angularVelocity =
                Vector3.zero;
        }

        hornPickupRigidbody.useGravity = false;
        hornPickupRigidbody.isKinematic = locked;
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
