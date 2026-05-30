using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectMenu : MonoBehaviour
{
    [System.Serializable]
    public class LevelButtonView
    {
        public int levelNumber = 1;
        public string sceneName;

        [Header("Button")]
        public Button button;

        [Header("Sprite Element")]
        public Image elementImage;
        public Sprite unlockedElementSprite;
        public Sprite lockedElementSprite;

        [Header("Background")]
        public Image backgroundImage;
        public Sprite unlockedBackgroundSprite;
        public Sprite lockedBackgroundSprite;

        [Header("Star")]
        public Image passedStarImage;
    }

    [Header("Progress")]
    [Tooltip("Contoh: Math atau Literacy. Harus sama dengan subject di GameManager level.")]
    public string subject = "Math";

    [Header("Level Buttons")]
    public LevelButtonView[] levels;

    [Header("Star Summary")]
    public Image[] passedLevelStars;
    public Sprite emptyStarSprite;
    public Sprite filledStarSprite;

    [Header("Button Visual")]
    [Tooltip("Jika aktif, button locked tidak bisa dipencet tetapi tidak terkena efek dim bawaan Unity.")]
    public bool keepLockedButtonVisual = true;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        int totalLevels = levels == null ? 0 : levels.Length;
        int passedCount = LevelProgress.CountPassedLevels(subject, totalLevels);

        for (int i = 0; i < totalLevels; i++)
            ApplyLevelState(levels[i]);

        if (passedLevelStars == null)
            return;

        for (int i = 0; i < passedLevelStars.Length; i++)
        {
            if (passedLevelStars[i] == null)
                continue;

            bool filled = i < passedCount;
            passedLevelStars[i].sprite = filled ? filledStarSprite : emptyStarSprite;
        }
    }

    private void ApplyLevelState(LevelButtonView view)
    {
        if (view == null)
            return;

        bool unlocked = LevelProgress.IsLevelUnlocked(subject, view.levelNumber);
        bool passed = LevelProgress.IsLevelPassed(subject, view.levelNumber);

        if (view.button != null)
        {
            view.button.onClick.RemoveAllListeners();
            view.button.interactable = unlocked;

            if (keepLockedButtonVisual)
            {
                ColorBlock colors = view.button.colors;
                colors.disabledColor = colors.normalColor;
                view.button.colors = colors;
            }

            if (unlocked)
                view.button.onClick.AddListener(() => LoadLevel(view.sceneName));
        }

        if (view.elementImage != null)
            view.elementImage.sprite = unlocked ? view.unlockedElementSprite : view.lockedElementSprite;

        if (view.backgroundImage != null)
            view.backgroundImage.sprite = unlocked ? view.unlockedBackgroundSprite : view.lockedBackgroundSprite;

        if (view.passedStarImage != null)
            view.passedStarImage.sprite = passed ? filledStarSprite : emptyStarSprite;
    }

    private void LoadLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LevelSelectMenu] Scene name belum diisi di Inspector.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
