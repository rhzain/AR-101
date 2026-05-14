using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Mengelola semua tampilan UI untuk Literacy Level 1.
/// Pasang ke GameObject UI Manager di scene LiteracyLevel1.
/// </summary>
public class LL1UIManager : MonoBehaviour
{
    [Header("Teks UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI feedbackText;

    [Header("Tombol")]
    public Button submitButton;

    [Header("Result Panel")]
    public ResultPanel resultPanel;
    public Sprite[] resultSprites; // index = jumlah benar (0 - 10, total 2 bagian x 5 soal)

    private LL1GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<LL1GameManager>();

        if (resultPanel != null) resultPanel.Hide();

        // Setup tombol Submit
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(() =>
            {
                if (gameManager != null) gameManager.SubmitAnswer();
            });
            submitButton.gameObject.SetActive(false);
        }

        // Setup retry button di ResultPanel
        if (resultPanel != null && resultPanel.retryButton != null)
        {
            resultPanel.retryButton.onClick.AddListener(() =>
            {
                if (gameManager != null) gameManager.RestartGame();
            });
        }

        ResetUI();
    }

    // ─── Dipanggil oleh LL1GameManager ───────────────────────

    /// <summary>Tampilkan soal baru.</summary>
    public void ShowQuestion(string question, string roundInfo)
    {
        if (resultPanel != null) resultPanel.Hide();

        if (questionText != null) { questionText.gameObject.SetActive(true); questionText.text = question; }
        if (roundText != null)    { roundText.gameObject.SetActive(true);    roundText.text = roundInfo; }
        if (feedbackText != null) feedbackText.text = "";
        if (submitButton != null) submitButton.gameObject.SetActive(true);
    }

    /// <summary>Tampilkan pesan transisi antar bagian (misal: "Bagian 2 dimulai!").</summary>
    public void ShowTransition(string message)
    {
        if (questionText != null) { questionText.gameObject.SetActive(false); }
        if (roundText != null)    { roundText.gameObject.SetActive(false); }
        if (feedbackText != null) feedbackText.text = message;
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }

    /// <summary>Tampilkan feedback benar/salah setelah jawaban.</summary>
    public void ShowFeedback(bool isCorrect, string correctAnswerDisplay)
    {
        // Sembunyikan UI soal agar tidak overlap dengan feedback
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (roundText != null)    roundText.gameObject.SetActive(false);

        if (feedbackText != null)
        {
            feedbackText.text = isCorrect
                ? "Benar!"
                : $"Salah. Jawaban: {correctAnswerDisplay}";
        }
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }

    /// <summary>Tampilkan hasil akhir game dengan result panel.</summary>
    public void ShowFinalResult(int correctAnswers, int totalRounds)
    {
        Debug.Log($"[LL1UIManager] ShowFinalResult: {correctAnswers}/{totalRounds}");

        if (questionText != null)  { questionText.gameObject.SetActive(false); }
        if (roundText != null)     { roundText.gameObject.SetActive(false); }
        if (feedbackText != null)  feedbackText.text = "";
        if (submitButton != null)  submitButton.gameObject.SetActive(false);

        if (resultPanel != null && resultSprites != null && correctAnswers < resultSprites.Length)
        {
            resultPanel.Show(correctAnswers, totalRounds, resultSprites[correctAnswers]);
        }
    }

    /// <summary>Reset semua UI ke kondisi awal.</summary>
    public void ResetUI()
    {
        if (questionText != null)  { questionText.gameObject.SetActive(true); questionText.text = ""; }
        if (roundText != null)     { roundText.gameObject.SetActive(true);    roundText.text = ""; }
        if (feedbackText != null)  feedbackText.text = "";
        if (submitButton != null)  submitButton.gameObject.SetActive(false);
        if (resultPanel != null)   resultPanel.Hide();
    }
}
