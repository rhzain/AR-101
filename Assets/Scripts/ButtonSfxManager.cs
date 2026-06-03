using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonSfxManager : MonoBehaviour
{
    public static ButtonSfxManager Instance;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string defaultButtonClickResourcePath = "Audio/UI/button-sound";
    [Range(0f, 1f)]
    [SerializeField] private float buttonClickVolume = 0.45f;

    private bool isSoundOn = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        ButtonSfxManager existingManager = FindFirstObjectByType<ButtonSfxManager>();
        if (existingManager != null)
            return;

        GameObject managerObject = new GameObject("Button SFX Manager");
        managerObject.AddComponent<ButtonSfxManager>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        LoadDefaultClipIfNeeded();

        if (buttonClickClip != null)
            buttonClickClip.LoadAudioData();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RegisterButtons();
        StartCoroutine(RefreshButtonsPeriodically());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterButtons();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void RegisterButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null || button.GetComponent<ButtonClickSfx>() != null)
                continue;

            button.gameObject.AddComponent<ButtonClickSfx>();
        }
    }

    public void PlayButtonClick()
    {
        if (!isSoundOn || buttonClickClip == null) return;

        audioSource.PlayOneShot(buttonClickClip, buttonClickVolume);
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsSoundOn()
    {
        return isSoundOn;
    }

    private void LoadDefaultClipIfNeeded()
    {
        if (buttonClickClip != null || string.IsNullOrWhiteSpace(defaultButtonClickResourcePath))
            return;

        buttonClickClip = Resources.Load<AudioClip>(defaultButtonClickResourcePath);
    }

    private IEnumerator RefreshButtonsPeriodically()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);

        while (true)
        {
            RegisterButtons();
            yield return wait;
        }
    }

    private void OnValidate()
    {
        buttonClickVolume = Mathf.Clamp01(buttonClickVolume);
    }
}

[RequireComponent(typeof(Button))]
public class ButtonClickSfx : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        PlayClickSound();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickSound();
    }

    private void PlayClickSound()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button == null || !button.isActiveAndEnabled || !button.IsInteractable())
            return;

        ButtonSfxManager.Instance?.PlayButtonClick();
    }
}

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string defaultMusicResourcePath = "Audio/BGM/background5";
    [Range(0f, 1f)]
    [SerializeField] private float backgroundMusicVolume = 0.1f;

    [Header("Level Playback")]
    [Tooltip("Default mati. Aktifkan kalau BGM juga boleh menyala saat sedang mengerjakan level.")]
    [SerializeField] private bool playInLevelScenes = false;
    [SerializeField] private string[] levelSceneNames =
    {
        "MathL1",
        "MathL2",
        "MathL3",
        "LiteracyL1",
        "LiteracyL2",
        "LiteracyL3"
    };

    [Header("Sound Toggle")]
    [SerializeField] private bool followGlobalSoundToggle = true;

    private HashSet<string> levelSceneLookup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        BackgroundMusicManager existingManager = FindFirstObjectByType<BackgroundMusicManager>();
        if (existingManager != null)
            return;

        GameObject managerObject = new GameObject("Background Music Manager");
        managerObject.AddComponent<BackgroundMusicManager>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLevelSceneLookup();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = backgroundMusicVolume;

        LoadDefaultClipIfNeeded();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RefreshPlayback();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshPlayback(scene.name);
    }

    public void RefreshPlayback()
    {
        RefreshPlayback(SceneManager.GetActiveScene().name);
    }

    public void SetPlayInLevelScenes(bool canPlayInLevelScenes)
    {
        playInLevelScenes = canPlayInLevelScenes;
        RefreshPlayback();
    }

    public bool CanPlayInLevelScenes()
    {
        return playInLevelScenes;
    }

    private void RefreshPlayback(string sceneName)
    {
        if (audioSource == null)
            return;

        audioSource.volume = backgroundMusicVolume;

        if (!ShouldPlayMusic(sceneName))
        {
            audioSource.Stop();
            return;
        }

        if (audioSource.clip != backgroundMusicClip)
            audioSource.clip = backgroundMusicClip;

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private bool ShouldPlayMusic(string sceneName)
    {
        if (backgroundMusicClip == null)
            return false;

        if (followGlobalSoundToggle && PlayerPrefs.GetInt("SoundOn", 1) != 1)
            return false;

        return playInLevelScenes || !IsLevelScene(sceneName);
    }

    private bool IsLevelScene(string sceneName)
    {
        return levelSceneLookup != null && levelSceneLookup.Contains(sceneName);
    }

    private void BuildLevelSceneLookup()
    {
        levelSceneLookup = new HashSet<string>();

        if (levelSceneNames == null)
            return;

        foreach (string sceneName in levelSceneNames)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
                levelSceneLookup.Add(sceneName);
        }
    }

    private void LoadDefaultClipIfNeeded()
    {
        if (backgroundMusicClip != null || string.IsNullOrWhiteSpace(defaultMusicResourcePath))
            return;

        backgroundMusicClip = Resources.Load<AudioClip>(defaultMusicResourcePath);
    }

    private void OnValidate()
    {
        backgroundMusicVolume = Mathf.Clamp01(backgroundMusicVolume);
    }
}
