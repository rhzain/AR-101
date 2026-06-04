using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LaporanManager : MonoBehaviour
{
    [System.Serializable]
    public class SubjectReportView
    {
        [Header("Progress")]
        public string subject = "Math";
        public int totalLevels = 3;
        [Tooltip("Jumlah soal maksimum per level. Contoh Math: 5,5,5. Literacy: 10,5,5.")]
        public int[] maxScoresPerLevel = { 5, 5, 5 };
        public Image[] starImages;
        public Sprite filledStarSprite;
        public Sprite emptyStarSprite;
        public TMP_Text completedText;

        [Header("Sertifikat")]
        public Button certificateButton;
        public Sprite certificateBackgroundSprite;
    }

    [Header("Subject Reports")]
    public SubjectReportView mathReport = new SubjectReportView
    {
        subject = "Math",
        totalLevels = 3,
        maxScoresPerLevel = new[] { 5, 5, 5 }
    };

    public SubjectReportView literacyReport = new SubjectReportView
    {
        subject = "Literacy",
        totalLevels = 3,
        maxScoresPerLevel = new[] { 10, 5, 5 }
    };

    [Header("Overall Progress")]
    public TMP_Text overallProgressText;
    [Tooltip("Image Fill dengan Type = Filled dan Fill Method = Horizontal.")]
    public Image overallProgressFill;

    [Header("Certificate Panel")]
    public GameObject certificatePanel;
    public Image certificateBackgroundImage;
    public TMP_Text certificateNameText;
    public TMP_Text certificateClassText;
    public TMP_Text certificateSubjectText;
    public TMP_Text certificatePercentageText;
    public Button certificateCloseButton;

    [Header("Format Text")]
    public string completedFormat = "{0} dari {1} level selesai";
    public string overallFormat = "{0}%";
    public string certificatePercentFormat = "{0}%";
    public string classFormat = "Kelas {0}";

    private void Awake()
    {
        WireButtons();

        if (certificatePanel != null)
            certificatePanel.SetActive(false);
    }

    private void OnEnable()
    {
        RefreshReport();
    }

    public void RefreshReport()
    {
        int mathPassed = RefreshSubject(mathReport);
        int literacyPassed = RefreshSubject(literacyReport);

        int totalPassed = mathPassed + literacyPassed;
        int totalLevels = Mathf.Max(0, mathReport.totalLevels) + Mathf.Max(0, literacyReport.totalLevels);
        float overallProgress = totalLevels <= 0 ? 0f : (float)totalPassed / totalLevels;

        if (overallProgressFill != null)
            overallProgressFill.fillAmount = Mathf.Clamp01(overallProgress);

        if (overallProgressText != null)
            overallProgressText.text = string.Format(overallFormat, Mathf.RoundToInt(overallProgress * 100f));
    }

    public void OpenMathCertificate()
    {
        OpenCertificate(mathReport, "Matematika");
    }

    public void OpenLiteracyCertificate()
    {
        OpenCertificate(literacyReport, "Literasi");
    }

    public void CloseCertificate()
    {
        if (certificatePanel != null)
            certificatePanel.SetActive(false);
    }

    private void WireButtons()
    {
        if (mathReport != null && mathReport.certificateButton != null)
        {
            mathReport.certificateButton.onClick.RemoveListener(OpenMathCertificate);
            mathReport.certificateButton.onClick.AddListener(OpenMathCertificate);
        }

        if (literacyReport != null && literacyReport.certificateButton != null)
        {
            literacyReport.certificateButton.onClick.RemoveListener(OpenLiteracyCertificate);
            literacyReport.certificateButton.onClick.AddListener(OpenLiteracyCertificate);
        }

        if (certificateCloseButton != null)
        {
            certificateCloseButton.onClick.RemoveListener(CloseCertificate);
            certificateCloseButton.onClick.AddListener(CloseCertificate);
        }
    }

    private int RefreshSubject(SubjectReportView report)
    {
        if (report == null)
            return 0;

        int passedCount = LevelProgress.CountPassedLevels(report.subject, report.totalLevels);

        if (report.completedText != null)
            report.completedText.text = string.Format(completedFormat, passedCount, report.totalLevels);

        RefreshStars(report, passedCount);

        if (report.certificateButton != null)
            report.certificateButton.interactable = IsCertificateUnlocked(report);

        return passedCount;
    }

    private void RefreshStars(SubjectReportView report, int passedCount)
    {
        if (report.starImages == null)
            return;

        for (int i = 0; i < report.starImages.Length; i++)
        {
            Image starImage = report.starImages[i];
            if (starImage == null)
                continue;

            bool filled = i < passedCount;
            Sprite targetSprite = filled ? report.filledStarSprite : report.emptyStarSprite;
            if (targetSprite != null)
                starImage.sprite = targetSprite;
        }
    }

    private void OpenCertificate(SubjectReportView report, string displaySubject)
    {
        RefreshReport();

        if (!IsCertificateUnlocked(report))
            return;

        if (certificatePanel != null)
            certificatePanel.SetActive(true);

        if (certificateBackgroundImage != null && report.certificateBackgroundSprite != null)
            certificateBackgroundImage.sprite = report.certificateBackgroundSprite;

        if (certificateNameText != null)
            certificateNameText.text = PlayerPrefs.GetString("Nama", "Player");

        if (certificateClassText != null)
            certificateClassText.text = string.Format(classFormat, PlayerPrefs.GetInt("Kelas", 0));

        if (certificateSubjectText != null)
            certificateSubjectText.text = displaySubject;

        if (certificatePercentageText != null)
            certificatePercentageText.text = string.Format(certificatePercentFormat, Mathf.RoundToInt(GetScorePercentage(report) * 100f));
    }

    private bool IsCertificateUnlocked(SubjectReportView report)
    {
        return report != null && LevelProgress.CountPassedLevels(report.subject, report.totalLevels) >= report.totalLevels;
    }

    private float GetScorePercentage(SubjectReportView report)
    {
        if (report == null || report.totalLevels <= 0)
            return 0f;

        int totalScore = 0;
        int maxScore = 0;

        for (int level = LevelProgress.FirstLevel; level <= report.totalLevels; level++)
        {
            totalScore += LevelProgress.GetBestScore(report.subject, level);
            maxScore += GetMaxScoreForLevel(report, level);
        }

        return maxScore <= 0 ? 0f : Mathf.Clamp01((float)totalScore / maxScore);
    }

    private int GetMaxScoreForLevel(SubjectReportView report, int levelNumber)
    {
        int index = levelNumber - LevelProgress.FirstLevel;

        if (report.maxScoresPerLevel != null
            && index >= 0
            && index < report.maxScoresPerLevel.Length
            && report.maxScoresPerLevel[index] > 0)
        {
            return report.maxScoresPerLevel[index];
        }

        return 5;
    }
}
