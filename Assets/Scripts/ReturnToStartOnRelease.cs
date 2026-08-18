using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class ReturnToStartOnRelease : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody body;
    private Transform startParent;
    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        startParent = transform.parent;
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        grabInteractable.selectExited.AddListener(ReturnAfterRelease);
    }

    private void OnDisable()
    {
        grabInteractable.selectExited.RemoveListener(ReturnAfterRelease);
    }

    private void ReturnAfterRelease(SelectExitEventArgs args)
    {
        StartCoroutine(ReturnIfStillReleased());
    }

    private IEnumerator ReturnIfStillReleased()
    {
        yield return null;

        if (grabInteractable.isSelected)
            yield break;

        if (transform.parent != startParent)
            yield break;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        transform.SetParent(startParent, false);
        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;
    }
}
