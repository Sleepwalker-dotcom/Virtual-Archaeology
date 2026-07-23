using FMODUnity;
using UnityEngine;

public class FMOD2DSFXPlayer : MonoBehaviour
{
    [SerializeField] private EventReference sceneTransitionSfx;
    [SerializeField] private EventReference positiveFeedbackSfx;
    [SerializeField] private EventReference applauseSfx;

    public void PlaySceneTransition()
    {
        PlayOneShot(sceneTransitionSfx, "Scene Transition");
    }

    public void PlayPositiveFeedback()
    {
        PlayOneShot(positiveFeedbackSfx, "Positive Feedback");
    }

    public void PlayApplause()
    {
        PlayOneShot(applauseSfx, "Applause");
    }

    private void PlayOneShot(EventReference eventReference, string label)
    {
        if (eventReference.IsNull)
        {
            Debug.LogWarning("[FMOD2DSFXPlayer] Missing event: " + label, this);
            return;
        }

        RuntimeManager.PlayOneShot(eventReference);
        Debug.Log(
            "[FMOD2DSFXPlayer] Playing " +
            label + ": " + eventReference.Path,
            this
        );
    }
}
