using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Mengelola semua tampilan UI untuk Math Level 2.
/// Pasang ke GameObject UI Manager di scene MathLevel2.
/// </summary>
public class ML2UIManager : MonoBehaviour
{
    [Header("Teks UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI appleCountText;

    [Header("Tombol")]
    public Button submitButton;

    [Header("Result Panel")]
    public ResultPanel resultPanel;
    public Sprite[] resultSprites; // 6 gambar: index 0-5 benar

    private ML2GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<ML2GameManager>();

        // Sembunyikan result panel saat mulai
        if (resultPanel != null) resultPanel.Hide();

        // Setup tombol submit
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

    // ─── Dipanggil oleh ML2GameManager ────────────────────────

    /// <summary>Tampilkan soal baru di UI.</summary>
    public void ShowQuestion(string questionDisplay, int currentRound, int maxRounds)
    {
        if (resultPanel != null) resultPanel.Hide();

        // Tampilkan kembali UI soal (mungkin disembunyikan saat feedback)
        if (questionText != null)  { questionText.gameObject.SetActive(true); questionText.text = questionDisplay; }
        if (roundText != null)     { roundText.gameObject.SetActive(true); roundText.text = $"Soal {currentRound}/{maxRounds}"; }
        if (feedbackText != null)  feedbackText.text = "Susun apel sejumlah jawaban!";
        if (appleCountText != null) appleCountText.text = "Jumlah: 0";
        if (submitButton != null)  submitButton.gameObject.SetActive(true);
    }

    /// <summary>Update teks jumlah apel di meja secara real-time.</summary>
    public void UpdateAppleCount(int count)
    {
        if (appleCountText != null)
            appleCountText.text = $"Jumlah: {count}";
    }

    /// <summary>Tampilkan feedback setelah jawaban disubmit.</summary>
    public void ShowFeedback(bool isCorrect, int playerAnswer, int correctAnswer)
    {
        // Sembunyikan UI soal agar tidak overlap dengan feedback
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (roundText != null)    roundText.gameObject.SetActive(false);

        if (feedbackText != null)
        {
            feedbackText.text = isCorrect
                ? $"Benar! Jawaban: {correctAnswer}"
                : $"Salah. Jawabannya {correctAnswer}, kamu menyusun {playerAnswer} apel.";
        }
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }

    /// <summary>Tampilkan layar hasil akhir game.</summary>
    public void ShowFinalResult(int correctAnswers, int totalRounds)
    {
        Debug.Log($"[ML2UIManager] ShowFinalResult: {correctAnswers}/{totalRounds}");

        if (questionText != null)   questionText.text = "";
        if (roundText != null)      roundText.text = "Game Over";
        if (feedbackText != null)   feedbackText.text = "";
        if (appleCountText != null) appleCountText.text = "";
        if (submitButton != null)   submitButton.gameObject.SetActive(false);

        if (resultPanel != null && resultSprites != null && correctAnswers < resultSprites.Length)
        {
            resultPanel.Show(correctAnswers, totalRounds, resultSprites[correctAnswers]);
        }
    }

    /// <summary>Reset semua UI ke kondisi awal (dipakai saat retry).</summary>
    public void ResetUI()
    {
        if (questionText != null)   { questionText.gameObject.SetActive(true); questionText.text = ""; }
        if (roundText != null)      { roundText.gameObject.SetActive(true); roundText.text = ""; }
        if (feedbackText != null)   feedbackText.text = "";
        if (appleCountText != null) appleCountText.text = "";
        if (submitButton != null)   submitButton.gameObject.SetActive(false);
        if (resultPanel != null)    resultPanel.Hide();
    }
}
