using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Canvas")]
    [Tooltip("Canvas header — selalu tampil sebelum dan selama gameplay")]
    public GameObject canvasHeader;
    [Tooltip("Canvas soal (main) — hanya tampil setelah meja di-place")]
    public GameObject canvasSoal;
    [Tooltip("Canvas recap akhir game — hanya tampil saat game selesai")]
    public GameObject canvasRecap;

    [Header("Result Panel")]
    public ResultPanel resultPanel;
    public Sprite[] resultSprites; // 6 gambar: index 0-5 benar

    private ML2GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<ML2GameManager>();

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

        // State awal: hanya header yang muncul
        ShowHeaderOnly();
    }

    // ─── Helper Toggle Canvas ──────────────────────────────────

    /// <summary>
    /// STATE 1 — Sebelum meja di-place:
    /// Header ON | Soal OFF | Recap OFF
    /// </summary>
    private void ShowHeaderOnly()
    {
        if (canvasHeader != null) canvasHeader.SetActive(true);
        if (canvasSoal != null)   canvasSoal.SetActive(false);
        if (canvasRecap != null)  canvasRecap.SetActive(false);
        if (resultPanel != null)  resultPanel.Hide();
    }

    /// <summary>
    /// STATE 2 — Saat gameplay berlangsung:
    /// Header ON | Soal ON | Recap OFF
    /// </summary>
    private void ShowGameplayCanvas()
    {
        if (canvasHeader != null) canvasHeader.SetActive(true);
        if (canvasSoal != null)   canvasSoal.SetActive(true);
        if (canvasRecap != null)  canvasRecap.SetActive(false);
    }

    /// <summary>
    /// STATE 3 — Saat game selesai:
    /// Header OFF | Soal OFF | Recap ON
    /// </summary>
    private void ShowRecapCanvas()
    {
        if (canvasHeader != null) canvasHeader.SetActive(false);
        if (canvasSoal != null)   canvasSoal.SetActive(false);
        if (canvasRecap != null)  canvasRecap.SetActive(true);
    }

    // ─── Dipanggil oleh ML2GameManager ────────────────────────

    /// <summary>Tampilkan soal baru di UI.</summary>
    public void ShowQuestion(string questionDisplay, int currentRound, int maxRounds)
    {
        if (resultPanel != null) resultPanel.Hide();

        // State 2: header + soal aktif
        ShowGameplayCanvas();

        if (questionText != null)   questionText.text = questionDisplay;
        if (roundText != null)      roundText.text = $"Soal {currentRound}/{maxRounds}";
        if (feedbackText != null)   feedbackText.text = "Susun apel sejumlah jawaban!";
        if (appleCountText != null) appleCountText.text = "Jumlah: 0";
        if (submitButton != null)   submitButton.gameObject.SetActive(true);
    }

    /// <summary>Update teks jumlah apel di meja secara real-time.</summary>
    public void UpdateAppleCount(int count)
    {
        if (appleCountText != null)
            appleCountText.text = $"Jumlah: {count}";
    }

    /// <summary>Tampilkan feedback benar/salah per jawaban — tetap di canvas soal.</summary>
    public void ShowFeedback(bool isCorrect, int playerAnswer, int correctAnswer)
    {
        // Tetap di canvas soal — hanya update teks feedback
        if (feedbackText != null)
        {
            feedbackText.text = isCorrect
                ? $"Benar! Jawaban: {correctAnswer}"
                : $"Salah. Jawabannya {correctAnswer}, kamu menyusun {playerAnswer} apel.";
        }
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }

    /// <summary>Tampilkan layar recap akhir game (canvas coin/hasil).</summary>
    public void ShowFinalResult(int correctAnswers, int totalRounds)
    {
        Debug.Log($"[ML2UIManager] ShowFinalResult dipanggil: {correctAnswers}/{totalRounds}");
        Debug.Log($"[ML2UIManager] canvasRecap null? {canvasRecap == null}");
        Debug.Log($"[ML2UIManager] resultPanel null? {resultPanel == null}");
        Debug.Log($"[ML2UIManager] resultSprites null? {resultSprites == null} | length: {resultSprites?.Length}");

        // State 3: hanya recap
        ShowRecapCanvas();

        if (resultPanel == null)
        {
            Debug.LogError("[ML2UIManager] resultPanel BELUM DI-ASSIGN di Inspector!");
            return;
        }

        if (resultSprites == null || resultSprites.Length == 0)
        {
            Debug.LogError("[ML2UIManager] resultSprites KOSONG! Assign 6 sprite di Inspector.");
            return;
        }

        if (correctAnswers >= resultSprites.Length)
        {
            Debug.LogError($"[ML2UIManager] Index {correctAnswers} melebihi panjang resultSprites ({resultSprites.Length})!");
            return;
        }

        bool isPassed = gameManager != null && correctAnswers >= gameManager.minimumCorrectToPass;
        string buttonText = isPassed ? "Level Berikutnya" : "Coba Lagi";

        resultPanel.Show(correctAnswers, totalRounds, resultSprites[correctAnswers], isPassed, buttonText, () =>
        {
            if (isPassed)
                SceneManager.LoadScene("MathLevel");
            else if (gameManager != null)
                gameManager.RestartGame();
        });

        Debug.Log("[ML2UIManager] resultPanel.Show() berhasil dipanggil.");
    }

    /// <summary>Reset UI ke state awal (sebelum meja di-place) — dipakai saat retry.</summary>
    public void ResetUI()
    {
        // Kembali ke State 1: hanya header
        ShowHeaderOnly();

        if (questionText != null)   questionText.text = "";
        if (roundText != null)      roundText.text = "";
        if (feedbackText != null)   feedbackText.text = "";
        if (appleCountText != null) appleCountText.text = "";
        if (submitButton != null)   submitButton.gameObject.SetActive(false);
    }
}
