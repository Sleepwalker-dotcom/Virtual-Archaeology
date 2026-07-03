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

    [Header("Transition SFX")]
    [SerializeField] private EventReference transitionSfx;
    [SerializeField] private float transitionMinTime = 2.0f;

    [Header("Fade Settings")]
    [SerializeField] private float defaultFadeOutTime = 1.0f;

    private EventInstance currentBgmInstance;
    private string currentMusicSceneName = "";
    private bool isLoadingScene = false;

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

        Debug.Log("[FMOD Audio Manager] Initialized.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        EnsureBanksLoaded();

        string activeSceneName = SceneManager.GetActiveScene().name;
        PlayMusicForScene(activeSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[FMOD Audio Manager] Scene loaded: " + scene.name);
        PlayMusicForScene(scene.name);
    }

    public void LoadSceneWithTransition(string targetSceneName)
    {
        if (isLoadingScene)
        {
            Debug.LogWarning("[FMOD Audio Manager] Scene is already loading.");
            return;
        }

        StartCoroutine(LoadSceneWithTransitionRoutine(targetSceneName));
    }

    private IEnumerator LoadSceneWithTransitionRoutine(string targetSceneName)
    {
        isLoadingScene = true;

        Debug.Log("[FMOD Audio Manager] Start transition to scene: " + targetSceneName);

        // 1. 播放转场音效
        EnsureBanksLoaded();

        if (!transitionSfx.IsNull)
        {
            RuntimeManager.PlayOneShot(transitionSfx, transform.position);
            Debug.Log("[FMOD Audio Manager] Transition SFX played.");
        }
        else
        {
            Debug.LogWarning("[FMOD Audio Manager] Transition SFX is not assigned.");
        }

        // 2. 淡出当前背景音乐
        if (currentBgmInstance.isValid())
        {
            StartCoroutine(FadeOutAndStop(currentBgmInstance, defaultFadeOutTime));
            Debug.Log("[FMOD Audio Manager] Current BGM fading out.");
        }

        // 3. 异步加载目标场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;

        float timer = 0f;

        while (asyncLoad.progress < 0.9f || timer < transitionMinTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 4. 允许切换到新场景
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("[FMOD Audio Manager] Scene transition complete: " + targetSceneName);

        isLoadingScene = false;
    }

    private void PlayMusicForScene(string sceneName)
    {
        SceneMusicEntry entry = FindSceneMusic(sceneName);

        if (entry == null)
        {
            Debug.LogWarning("[FMOD Audio Manager] No BGM assigned for scene: " + sceneName);
            return;
        }

        if (currentMusicSceneName == sceneName && currentBgmInstance.isValid())
        {
            Debug.Log("[FMOD Audio Manager] BGM already playing for scene: " + sceneName);
            return;
        }

        if (currentSwitchCoroutine != null)
        {
            StopCoroutine(currentSwitchCoroutine);
        }

        currentSwitchCoroutine = StartCoroutine(SwitchBgmRoutine(entry));
    }

    private IEnumerator SwitchBgmRoutine(SceneMusicEntry entry)
    {
        Debug.Log("[FMOD Audio Manager] Switching BGM to: " + entry.sceneName);

        EnsureBanksLoaded();

        if (entry.bgmEvent.IsNull)
        {
            Debug.LogError("[FMOD Audio Manager] BGM event is not assigned for scene: " + entry.sceneName);
            yield break;
        }

        EventInstance oldBgm = currentBgmInstance;

        // 1. 淡出旧 BGM
        if (oldBgm.isValid())
        {
            StartCoroutine(FadeOutAndStop(oldBgm, defaultFadeOutTime));
        }

        // 2. 创建新 BGM
        currentBgmInstance = RuntimeManager.CreateInstance(entry.bgmEvent);
        currentBgmInstance.setVolume(0f);
        currentBgmInstance.start();

        currentMusicSceneName = entry.sceneName;

        Debug.Log("[FMOD Audio Manager] New BGM started: " + entry.sceneName);

        // 3. 淡入新 BGM
        float timer = 0f;

        while (timer < entry.fadeInTime)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Clamp01(timer / entry.fadeInTime);
            currentBgmInstance.setVolume(volume);
            yield return null;
        }

        currentBgmInstance.setVolume(1f);

        Debug.Log("[FMOD Audio Manager] BGM fade-in complete: " + entry.sceneName);
    }

    private IEnumerator FadeOutAndStop(EventInstance instance, float fadeOutTime)
    {
        if (!instance.isValid())
        {
            yield break;
        }

        float timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            instance.setVolume(volume);
            yield return null;
        }

        instance.setVolume(0f);
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();

        Debug.Log("[FMOD Audio Manager] Old BGM stopped and released.");
    }

    private SceneMusicEntry FindSceneMusic(string sceneName)
    {
        foreach (SceneMusicEntry entry in sceneMusicList)
        {
            if (entry.sceneName == sceneName)
            {
                return entry;
            }
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

        Debug.Log("[FMOD Audio Manager] Required banks loaded.");
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
