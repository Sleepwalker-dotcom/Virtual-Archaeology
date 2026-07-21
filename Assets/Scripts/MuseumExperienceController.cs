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
    [SerializeField] private PlayableDirector museumIntroDirector;
    [SerializeField] private PlayableDirector hornRestorationDirector;
    [SerializeField] private PlayableDirector environmentTransitionDirector;

    [Header("Piece Interaction")]
    [SerializeField] private XRGrabInteractable pieceGrab;
    [SerializeField] private XRSocketInteractor pieceSocket;
    [SerializeField] private Transform pieceRoot;
    [SerializeField] private Transform pieceSnapTarget;
    [SerializeField] private Rigidbody pieceRigidbody;
    [SerializeField] private Collider[] pieceColliders;

    [Header("Horn Interaction")]
    [SerializeField] private XRGrabInteractable completeHornGrab;
    [SerializeField] private GameObject completeHornRoot;
    [SerializeField] private HornFMODController hornPerformanceController;

    [Header("Presentation")]
    [SerializeField] private GameObject restorationVfxRoot;
    [SerializeField] private Light pieceGuideLight;
    [SerializeField] private Light socketGuideLight;
    [SerializeField] private Light completeHornGuideLight;
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField, Min(0f)] private float guideLightFadeDuration = 1f;
    [SerializeField, Min(0f)] private float transitionFadeDuration = 0.6f;
    [SerializeField] private float guideLightIntensity = 3f;

    [Header("Existing Audio and Environment System")]
    [SerializeField] private SingleSceneAudioEnvironmentManager audioEnvironmentManager;

    [Header("Optional Timeline Audio Hooks")]
    [SerializeField] private UnityEvent onPlayIntroNarration;
    [SerializeField] private UnityEvent onPlayRestorationSound;
    [SerializeField] private UnityEvent onPlayRestorationNarration;
    [SerializeField] private UnityEvent onFreeExplorationStarted;

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
            pieceGrab.selectEntered.AddListener(HandlePieceSelected);

        if (pieceSocket != null)
            pieceSocket.selectEntered.AddListener(HandleSocketSelected);

        if (completeHornGrab != null)
            completeHornGrab.selectEntered.AddListener(HandleCompleteHornSelected);
    }

    private void Start()
    {
        CurrentState = MuseumExperienceState.MuseumIntro;

        if (museumIntroDirector != null && museumIntroDirector.playableAsset != null)
            museumIntroDirector.Play();
        else
            UnlockPiece();
    }

    private void OnDisable()
    {
        if (pieceGrab != null)
            pieceGrab.selectEntered.RemoveListener(HandlePieceSelected);

        if (pieceSocket != null)
            pieceSocket.selectEntered.RemoveListener(HandleSocketSelected);

        if (completeHornGrab != null)
            completeHornGrab.selectEntered.RemoveListener(HandleCompleteHornSelected);
    }

    private void InitializeExperience()
    {
        CurrentState = MuseumExperienceState.Initializing;
        piecePickupHandled = false;
        pieceInsertionHandled = false;
        hornPickupHandled = false;
        assemblyCommitted = false;

        SetEnabled(pieceGrab, false);
        SetEnabled(pieceSocket, false);
        SetEnabled(completeHornGrab, false);

        if (pieceRoot != null)
            pieceRoot.gameObject.SetActive(true);

        if (completeHornRoot != null)
            completeHornRoot.SetActive(false);

        if (hornPerformanceController != null)
            hornPerformanceController.enabled = false;

        if (restorationVfxRoot != null)
            restorationVfxRoot.SetActive(false);

        SetLightIntensity(pieceGuideLight, 0f);
        SetLightIntensity(socketGuideLight, 0f);
        SetLightIntensity(completeHornGuideLight, 0f);

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    public void PlayIntroNarration()
    {
        onPlayIntroNarration?.Invoke();
    }

    public void ShowPieceGuide()
    {
        FadeGuidanceLight(ref pieceLightCoroutine, pieceGuideLight, guideLightIntensity);
    }

    public void UnlockPiece()
    {
        if (CurrentState != MuseumExperienceState.MuseumIntro)
            return;

        SetEnabled(pieceGrab, true);
        CurrentState = MuseumExperienceState.WaitForPiecePickup;
    }

    private void HandlePieceSelected(SelectEnterEventArgs args)
    {
        if (CurrentState != MuseumExperienceState.WaitForPiecePickup || piecePickupHandled)
            return;

        piecePickupHandled = true;
        CurrentState = MuseumExperienceState.WaitForPieceInsertion;
        SetEnabled(pieceSocket, true);
        FadeGuidanceLight(ref pieceLightCoroutine, pieceGuideLight, 0f);
        FadeGuidanceLight(ref socketLightCoroutine, socketGuideLight, guideLightIntensity);
    }

    private void HandleSocketSelected(SelectEnterEventArgs args)
    {
        if (CurrentState != MuseumExperienceState.WaitForPieceInsertion || pieceInsertionHandled)
            return;

        if (pieceGrab == null || args.interactableObject.transform != pieceGrab.transform)
            return;

        pieceInsertionHandled = true;
        CurrentState = MuseumExperienceState.HornRestoration;
        LockPieceIntoSocket();
        FadeGuidanceLight(ref socketLightCoroutine, socketGuideLight, 0f);

        if (hornRestorationDirector != null && hornRestorationDirector.playableAsset != null)
            hornRestorationDirector.Play();
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
                    pieceCollider.enabled = false;
            }
        }

        if (pieceRoot != null && pieceSnapTarget != null)
        {
            pieceRoot.SetParent(pieceSnapTarget, false);
            pieceRoot.localPosition = Vector3.zero;
            pieceRoot.localRotation = Quaternion.identity;
        }
    }

    public void ShowRestorationVfx()
    {
        if (restorationVfxRoot != null)
            restorationVfxRoot.SetActive(true);
    }

    public void HideRestorationVfx()
    {
        if (restorationVfxRoot != null)
            restorationVfxRoot.SetActive(false);
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
            return;

        assemblyCommitted = true;

        if (pieceRoot != null)
            pieceRoot.gameObject.SetActive(false);

        if (completeHornRoot != null)
            completeHornRoot.SetActive(true);
    }

    public void ShowCompleteHornGuide()
    {
        FadeGuidanceLight(ref hornLightCoroutine, completeHornGuideLight, guideLightIntensity);
    }

    public void CompleteHornRestoration()
    {
        if (CurrentState != MuseumExperienceState.HornRestoration)
            return;

        CommitHornAssembly();
        HideRestorationVfx();
        SetEnabled(completeHornGrab, true);
        CurrentState = MuseumExperienceState.WaitForHornPickup;
    }

    private void HandleCompleteHornSelected(SelectEnterEventArgs args)
    {
        if (CurrentState != MuseumExperienceState.WaitForHornPickup || hornPickupHandled)
            return;

        hornPickupHandled = true;
        CurrentState = MuseumExperienceState.EnvironmentTransition;
        FadeGuidanceLight(ref hornLightCoroutine, completeHornGuideLight, 0f);

        if (environmentTransitionDirector != null && environmentTransitionDirector.playableAsset != null)
            environmentTransitionDirector.Play();
        else
        {
            BeginEnvironmentAudioTransition();
            CompleteEnvironmentTransition();
        }
    }

    public void BeginEnvironmentAudioTransition()
    {
        FadeTransitionCanvas(1f);

        if (audioEnvironmentManager != null)
            audioEnvironmentManager.SwitchToTavern();
    }

    public void FadeFromBlack()
    {
        FadeTransitionCanvas(0f);
    }

    public void CompleteEnvironmentTransition()
    {
        if (CurrentState != MuseumExperienceState.EnvironmentTransition)
            return;

        CurrentState = MuseumExperienceState.FreeExploration;

        if (hornPerformanceController != null)
        {
            hornPerformanceController.enabled = true;
            hornPerformanceController.ActivateHorn();
        }

        onFreeExplorationStarted?.Invoke();
    }

    private void FadeGuidanceLight(ref Coroutine routine, Light targetLight, float targetIntensity)
    {
        if (targetLight == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeLight(targetLight, targetIntensity));
    }

    private IEnumerator FadeLight(Light targetLight, float targetIntensity)
    {
        float startIntensity = targetLight.intensity;
        float elapsed = 0f;

        while (elapsed < guideLightFadeDuration)
        {
            elapsed += Time.deltaTime;
            targetLight.intensity = Mathf.Lerp(startIntensity, targetIntensity,
                guideLightFadeDuration <= 0f ? 1f : elapsed / guideLightFadeDuration);
            yield return null;
        }

        targetLight.intensity = targetIntensity;
    }

    private void FadeTransitionCanvas(float targetAlpha)
    {
        if (transitionCanvasGroup == null)
            return;

        if (transitionFadeCoroutine != null)
            StopCoroutine(transitionFadeCoroutine);

        transitionFadeCoroutine = StartCoroutine(FadeCanvas(targetAlpha));
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = transitionCanvasGroup.alpha;
        float elapsed = 0f;
        transitionCanvasGroup.blocksRaycasts = targetAlpha > 0f;

        while (elapsed < transitionFadeDuration)
        {
            elapsed += Time.deltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha,
                transitionFadeDuration <= 0f ? 1f : elapsed / transitionFadeDuration);
            yield return null;
        }

        transitionCanvasGroup.alpha = targetAlpha;
        transitionCanvasGroup.blocksRaycasts = targetAlpha > 0f;
        transitionFadeCoroutine = null;
    }

    private static void SetEnabled(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null)
            behaviour.enabled = enabled;
    }

    private static void SetLightIntensity(Light targetLight, float intensity)
    {
        if (targetLight != null)
            targetLight.intensity = intensity;
    }
}
