using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;

[System.Serializable]
public class SceneMusicEntry
{
    public string sceneName;
    public EventReference bgmEvent;
    public float fadeInTime = 1.5f;
}

public class FmodGlobalAudioManager : MonoBehaviour
{
    private const string MasterBankName = "Master";
    private const string ContentBankName = "Tower Hamlet's March";

    public static FmodGlobalAudioManager Instance { get; private set; }

    [Header("Scene BGM Settings")]
    [SerializeField] private List<SceneMusicEntry> sceneMusicList = new List<SceneMusicEntry>();
    [SerializeField] private string startupMusicSceneName;

    [Header("Fade Settings")]
    [SerializeField] private float defaultFadeOutTime = 1.0f;

    private EventInstance currentBgmInstance;
    private string currentMusicSceneName = "";
    private Coroutine currentSwitchCoroutine;
    private bool banksLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureBanksLoaded();
        PlayMusicForScene(string.IsNullOrWhiteSpace(startupMusicSceneName)
            ? SceneManager.GetActiveScene().name
            : startupMusicSceneName);
    }

    public void PlayMusicForScene(string sceneName)
    {
        SceneMusicEntry entry = FindSceneMusic(sceneName);
        if (entry == null)
            return;

        if (currentMusicSceneName == sceneName && currentBgmInstance.isValid())
            return;

        if (currentSwitchCoroutine != null)
            StopCoroutine(currentSwitchCoroutine);

        currentSwitchCoroutine = StartCoroutine(SwitchBgmRoutine(entry));
    }

    private IEnumerator SwitchBgmRoutine(SceneMusicEntry entry)
    {
        EnsureBanksLoaded();

        if (entry.bgmEvent.IsNull)
        {
            Debug.LogError("[FMOD Audio Manager] BGM event is not assigned for scene: " + entry.sceneName);
            yield break;
        }

        EventInstance oldBgm = currentBgmInstance;
        if (oldBgm.isValid())
            StartCoroutine(FadeOutAndStop(oldBgm, defaultFadeOutTime));

        currentBgmInstance = RuntimeManager.CreateInstance(entry.bgmEvent);
        currentBgmInstance.setVolume(0f);
        currentBgmInstance.start();
        currentMusicSceneName = entry.sceneName;

        float timer = 0f;
        while (timer < entry.fadeInTime)
        {
            timer += Time.deltaTime;
            currentBgmInstance.setVolume(Mathf.Clamp01(timer / entry.fadeInTime));
            yield return null;
        }

        currentBgmInstance.setVolume(1f);
    }

    private IEnumerator FadeOutAndStop(EventInstance instance, float fadeOutTime)
    {
        if (!instance.isValid())
            yield break;

        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            instance.setVolume(Mathf.Lerp(1f, 0f, timer / fadeOutTime));
            yield return null;
        }

        instance.setVolume(0f);
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
    }

    private SceneMusicEntry FindSceneMusic(string sceneName)
    {
        foreach (SceneMusicEntry entry in sceneMusicList)
        {
            if (entry.sceneName == sceneName)
                return entry;
        }

        return null;
    }

    private void EnsureBanksLoaded()
    {
        if (banksLoaded)
            return;

        RuntimeManager.LoadBank(MasterBankName + ".strings");
        RuntimeManager.LoadBank(MasterBankName);
        RuntimeManager.LoadBank(ContentBankName, true);
        banksLoaded = true;
    }

    private void OnDestroy()
    {
        if (currentBgmInstance.isValid())
        {
            currentBgmInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentBgmInstance.release();
        }
    }
}
