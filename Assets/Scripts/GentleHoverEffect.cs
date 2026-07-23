using UnityEngine;

[DisallowMultipleComponent]
public sealed class GentleHoverEffect : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float verticalAmplitude = 0.012f;

    [SerializeField, Min(0f)]
    private float verticalFrequency = 0.55f;

    [SerializeField, Min(0f)]
    private float yawAmplitude = 1.2f;

    [SerializeField, Min(0f)]
    private float yawFrequency = 0.35f;

    [SerializeField, Min(0.01f)]
    private float blendDuration = 0.25f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private bool playing;
    private float elapsedTime;
    private float weight;

    private void Awake()
    {
        CaptureCurrentPoseAsBase();
    }

    private void LateUpdate()
    {
        float targetWeight = playing ? 1f : 0f;
        float blendSpeed = 1f / Mathf.Max(0.01f, blendDuration);

        weight = Mathf.MoveTowards(
            weight,
            targetWeight,
            blendSpeed * Time.deltaTime
        );

        if (!playing && weight <= 0.0001f)
        {
            ApplyBasePose();
            return;
        }

        elapsedTime += Time.deltaTime;

        float verticalOffset =
            Mathf.Sin(
                elapsedTime *
                Mathf.PI *
                2f *
                verticalFrequency
            ) *
            verticalAmplitude *
            weight;

        float yawOffset =
            Mathf.Sin(
                elapsedTime *
                Mathf.PI *
                2f *
                yawFrequency
            ) *
            yawAmplitude *
            weight;

        transform.localPosition =
            baseLocalPosition +
            Vector3.up * verticalOffset;

        transform.localRotation =
            baseLocalRotation *
            Quaternion.Euler(0f, yawOffset, 0f);
    }

    public void Play()
    {
        if (!playing && weight <= 0.0001f)
        {
            elapsedTime = 0f;
        }

        playing = true;
    }

    public void StopImmediately()
    {
        playing = false;
        elapsedTime = 0f;
        weight = 0f;
        ApplyBasePose();
    }

    public void CaptureCurrentPoseAsBase()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    private void ApplyBasePose()
    {
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
    }
}
