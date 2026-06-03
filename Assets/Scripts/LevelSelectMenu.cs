using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

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

    [Header("Modul")]
    [Tooltip("Manager popup modul di scene ini. Bisa ModulMatematikaManager atau ModulLiterasiManager.")]
    public ModuleSlideshowManager moduleManager;
    [Tooltip("Optional. Tombol Modul di scene ini jika ingin listener dipasang otomatis.")]
    public Button moduleButton;

    private bool moduleButtonBound;

    private void Awake()
    {
        BindModuleButton();
    }

    private void OnEnable()
    {
        BindModuleButton();
    }

    private void Start()
    {
        BindModuleButton(true);

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

    public void OpenModule()
    {
        if (moduleManager == null)
            moduleManager = FindFirstObjectByType<ModuleSlideshowManager>(FindObjectsInactive.Include);

        if (moduleManager == null)
        {
            Debug.LogWarning("[LevelSelectMenu] Module manager belum di-assign di Inspector.");
            return;
        }

        moduleManager.OpenModul();
    }

    public void OpenModul()
    {
        OpenModule();
    }

    private void BindModuleButton(bool forceRebind = false)
    {
        if (moduleManager == null)
            moduleManager = FindFirstObjectByType<ModuleSlideshowManager>(FindObjectsInactive.Include);

        if (moduleButton == null)
            moduleButton = FindModuleButton();

        if (moduleButton == null || moduleButtonBound && !forceRebind)
            return;

        moduleButton.onClick.RemoveAllListeners();
        moduleButton.onClick.AddListener(OpenModule);
        moduleButtonBound = true;
    }

    private Button FindModuleButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (IsModuleLabel(label))
                return button;
        }

        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text label in labels)
        {
            if (!IsModuleLabel(label))
                continue;

            Button button = label.GetComponentInParent<Button>(true);
            if (button != null)
                return button;

            button = FindButtonNear(label.transform);
            if (button != null)
                return button;
        }

        return null;
    }

    private bool IsModuleLabel(TMP_Text label)
    {
        if (label == null || string.IsNullOrWhiteSpace(label.text))
            return false;

        if (!label.text.Contains("Modul"))
            return false;

        return moduleManager == null
            || moduleManager.popupRoot == null
            || !label.transform.IsChildOf(moduleManager.popupRoot.transform);
    }

    private Button FindButtonNear(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            Button button = current.GetComponent<Button>();
            if (button != null)
                return button;

            button = current.GetComponentInChildren<Button>(true);
            if (button != null)
                return button;

            current = current.parent;
        }

        return null;
    }
}

public class ModuleSlideshowManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupRoot;
    public Image pageImage;
    public TMP_Text pageCounter;
    public Button btnPrev;
    public Button btnNext;
    public Button btnClose;

    [Header("Halaman Modul")]
    public Sprite[] pages;

    [Header("Editor Auto Fill")]
    [Tooltip("Aktif saat di Editor agar halaman modul otomatis diisi dari folder asset.")]
    public bool autoLoadPagesInEditor = true;
    [Tooltip("Aktif saat runtime agar halaman modul otomatis di-load dari Assets/Resources.")]
    public bool autoLoadPagesFromResources = true;

    private int currentPage;
    private bool initialized;

    protected virtual string EditorPagesFolder => "";
    protected virtual string ResourcePagesFolder => "";

    protected virtual void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        AutoWireReferences();
        LoadPagesFromResourcesIfNeeded();
        WireButtons();

        if (popupRoot == null)
            popupRoot = gameObject;

        popupRoot.SetActive(false);
        initialized = true;
    }

    public void OpenModul()
    {
        EnsureInitialized();
        currentPage = 0;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshPage();
    }

    public void CloseModul()
    {
        EnsureInitialized();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void NextPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        currentPage = Mathf.Min(currentPage + 1, pages.Length - 1);
        RefreshPage();
    }

    public void PrevPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        currentPage = Mathf.Max(currentPage - 1, 0);
        RefreshPage();
    }

    private void AutoWireReferences()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (pageImage == null)
        {
            Transform pageImageTransform = FindChildRecursive(transform, "PageImage");
            if (pageImageTransform != null)
                pageImage = pageImageTransform.GetComponent<Image>();
        }

        if (pageCounter == null)
        {
            Transform pageCounterTransform = FindChildRecursive(transform, "PageCounter");
            if (pageCounterTransform != null)
                pageCounter = pageCounterTransform.GetComponent<TMP_Text>();
        }

        if (btnPrev == null)
            btnPrev = FindButton("BtnPrev");

        if (btnNext == null)
            btnNext = FindButton("BtnNext");

        if (btnClose == null)
            btnClose = FindButton("BtnClose");

        if (btnClose == null)
            btnClose = FindButton("BackButton");
    }

    private void WireButtons()
    {
        if (btnPrev != null)
        {
            btnPrev.onClick.RemoveListener(PrevPage);
            btnPrev.onClick.AddListener(PrevPage);
        }

        if (btnNext != null)
        {
            btnNext.onClick.RemoveListener(NextPage);
            btnNext.onClick.AddListener(NextPage);
        }

        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(CloseModul);
            btnClose.onClick.AddListener(CloseModul);
        }
    }

    private void RefreshPage()
    {
        bool hasPages = pages != null && pages.Length > 0;

        if (pageImage != null)
        {
            pageImage.enabled = hasPages;
            if (hasPages)
                pageImage.sprite = pages[Mathf.Clamp(currentPage, 0, pages.Length - 1)];
        }

        if (pageCounter != null)
            pageCounter.text = hasPages ? $"{currentPage + 1} / {pages.Length}" : "0 / 0";

        if (btnPrev != null)
            btnPrev.interactable = hasPages && currentPage > 0;

        if (btnNext != null)
            btnNext.interactable = hasPages && currentPage < pages.Length - 1;
    }

    private void LoadPagesFromResourcesIfNeeded()
    {
        if (!autoLoadPagesFromResources || pages != null && pages.Length > 0)
            return;

        string folder = ResourcePagesFolder;
        if (string.IsNullOrWhiteSpace(folder))
            return;

        Sprite[] loadedPages = Resources.LoadAll<Sprite>(folder);
        if (loadedPages == null || loadedPages.Length == 0)
            return;

        System.Array.Sort(loadedPages, (left, right) => string.CompareOrdinal(left.name, right.name));
        pages = loadedPages;
    }

    private Button FindButton(string childName)
    {
        Transform buttonTransform = FindChildRecursive(transform, childName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
                return nestedChild;
        }

        return null;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        AutoWireReferences();

        if (!autoLoadPagesInEditor || pages != null && pages.Length > 0)
            return;

        TryAutoLoadPagesInEditor();
    }

    [ContextMenu("Auto Fill Modul Pages")]
    private void TryAutoLoadPagesInEditor()
    {
        string folder = EditorPagesFolder;
        if (string.IsNullOrWhiteSpace(folder) || !AssetDatabase.IsValidFolder(folder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        List<Sprite> loadedPages = new List<Sprite>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                loadedPages.Add(sprite);
        }

        loadedPages.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        if (loadedPages.Count == 0)
            return;

        pages = loadedPages.ToArray();
        EditorUtility.SetDirty(this);
    }
#endif
}
