using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioReverbFilter))]
[RequireComponent(typeof(AudioEchoFilter))]
public class HornTransitionAudio : MonoBehaviour
{
    [Header("Replaceable Audio")]
    [SerializeField] private AudioClip hornCueClip;
    [SerializeField] private bool generatePlaceholderWhenMissing = true;

    [Header("Levels")]
    [SerializeField, Range(0f, 1f)] private float quietVolume = 0.08f;
    [SerializeField, Range(0f, 1f)] private float revealVolume = 0.42f;
    [SerializeField, Min(1f)] private float quietRepeatInterval = 7f;
    [SerializeField] private bool repeatQuietCue = true;

    private AudioSource audioSource;
    private Coroutine quietCueRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        ConfigureFilters();

        if (hornCueClip == null && generatePlaceholderWhenMissing)
            hornCueClip = CreatePlaceholderHornCue();
    }

    private void OnEnable()
    {
        if (repeatQuietCue)
            quietCueRoutine = StartCoroutine(RepeatQuietCue());
    }

    private void OnDisable()
    {
        if (quietCueRoutine != null)
            StopCoroutine(quietCueRoutine);
    }

    public void PlayQuietCue()
    {
        PlayCue(quietVolume);
    }

    public void PlayRevealCue()
    {
        PlayCue(revealVolume);
    }

    private void PlayCue(float volume)
    {
        if (hornCueClip != null)
            audioSource.PlayOneShot(hornCueClip, volume);
    }

    private IEnumerator RepeatQuietCue()
    {
        yield return new WaitForSeconds(1f);

        while (enabled)
        {
            PlayQuietCue();
            yield return new WaitForSeconds(quietRepeatInterval);
        }
    }

    private void ConfigureFilters()
    {
        AudioReverbFilter reverb = GetComponent<AudioReverbFilter>();
        reverb.reverbPreset = AudioReverbPreset.Cave;
        reverb.dryLevel = -900f;
        reverb.reverbLevel = -250f;
        reverb.decayTime = 5.5f;
        reverb.reflectionsDelay = 0.08f;

        AudioEchoFilter echo = GetComponent<AudioEchoFilter>();
        echo.delay = 310f;
        echo.decayRatio = 0.38f;
        echo.dryMix = 0.72f;
        echo.wetMix = 0.38f;
    }

    private static AudioClip CreatePlaceholderHornCue()
    {
        const int sampleRate = 44100;
        const float duration = 1.35f;
        const float fundamental = 174.61f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float attack = Mathf.Clamp01(time / 0.08f);
            float release = Mathf.Clamp01((duration - time) / 0.65f);
            float envelope = attack * release * release;

            float tone =
                Mathf.Sin(2f * Mathf.PI * fundamental * time) * 0.72f +
                Mathf.Sin(2f * Mathf.PI * fundamental * 2f * time) * 0.20f +
                Mathf.Sin(2f * Mathf.PI * fundamental * 3f * time) * 0.08f;

            samples[i] = tone * envelope * 0.55f;
        }

        return AudioClip.Create(
            "Generated Horn Transition Cue",
            sampleCount,
            1,
            sampleRate,
            false,
            data =>
            {
                int copyLength = Mathf.Min(data.Length, samples.Length);
                System.Array.Copy(samples, data, copyLength);
            });
    }
}
