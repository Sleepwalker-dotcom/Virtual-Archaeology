using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class LoadSceneOnGrab : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Tavern";
    [SerializeField, Min(0f)] private float loadDelay = 0.15f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isLoading;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!isLoading)
            StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        isLoading = true;

        if (loadDelay > 0f)
            yield return new WaitForSeconds(loadDelay);

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                $"[{nameof(LoadSceneOnGrab)}] Scene '{targetSceneName}' is not available in Build Settings.",
                this);
            isLoading = false;
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
    }
}
