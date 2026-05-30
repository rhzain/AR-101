using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonSfxManager : MonoBehaviour
{
    public static ButtonSfxManager Instance;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioSource audioSource;

    private bool isSoundOn = true;

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

        if (buttonClickClip != null)
            buttonClickClip.LoadAudioData();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RegisterButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterButtons();
    }

    private void RegisterButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    public void PlayButtonClick()
    {
        if (!isSoundOn || buttonClickClip == null) return;

        audioSource.PlayOneShot(buttonClickClip);
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
}
