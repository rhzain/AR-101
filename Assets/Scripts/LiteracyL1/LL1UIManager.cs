using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Canvas")]
    [Tooltip("Canvas header — selalu tampil sebelum dan selama gameplay")]
    public GameObject canvasHeader;
    [Tooltip("Canvas soal (main) — hanya tampil setelah meja di-place")]
    public GameObject canvasSoal;
    [Tooltip("Canvas recap akhir game — hanya tampil saat game selesai")]
    public GameObject canvasRecap;

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

    // ─── Dipanggil oleh LL1GameManager ───────────────────────

    /// <summary>Tampilkan soal baru — aktifkan canvas soal jika belum aktif.</summary>
    public void ShowQuestion(string question, string roundInfo)
    {
        if (resultPanel != null) resultPanel.Hide();

        // State 2: header + soal aktif
        ShowGameplayCanvas();

        if (questionText != null) { questionText.gameObject.SetActive(true); questionText.text = question; }
        if (roundText != null)    { roundText.gameObject.SetActive(true);    roundText.text = roundInfo; }
        if (feedbackText != null) feedbackText.text = "";
        if (submitButton != null) submitButton.gameObject.SetActive(true);
    }

    /// <summary>Tampilkan pesan transisi antar bagian — tetap di canvas soal.</summary>
    public void ShowTransition(string message)
    {
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (roundText != null)    roundText.gameObject.SetActive(false);
        if (feedbackText != null) feedbackText.text = message;
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }

    /// <summary>Tampilkan feedback benar/salah per jawaban — tetap di canvas soal.</summary>
    public void ShowFeedback(bool isCorrect, string correctAnswerDisplay)
    {
        if (questionText != null) questionText.gameObject.SetActive(false);
        if (feedbackText != null)
        {
            feedbackText.text = isCorrect
                ? "Benar!"
                : $"Salah. Jawaban: {correctAnswerDisplay}";
        }
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }

    /// <summary>Tampilkan layar recap akhir game (canvas coin/hasil).</summary>
    public void ShowFinalResult(int correctAnswers, int totalRounds)
    {
        Debug.Log($"[LL1UIManager] ShowFinalResult dipanggil: {correctAnswers}/{totalRounds}");
        Debug.Log($"[LL1UIManager] canvasRecap null? {canvasRecap == null}");
        Debug.Log($"[LL1UIManager] resultPanel null? {resultPanel == null}");
        Debug.Log($"[LL1UIManager] resultSprites null? {resultSprites == null} | length: {resultSprites?.Length}");

        // State 3: hanya recap
        ShowRecapCanvas();

        if (resultPanel == null)
        {
            Debug.LogError("[LL1UIManager] resultPanel BELUM DI-ASSIGN di Inspector!");
            return;
        }

        if (resultSprites == null || resultSprites.Length == 0)
        {
            Debug.LogError("[LL1UIManager] resultSprites KOSONG! Assign sprite di Inspector.");
            return;
        }

        int resultSpriteIndex = GetResultSpriteIndex(correctAnswers, totalRounds);

        if (resultSpriteIndex >= resultSprites.Length)
        {
            Debug.LogError($"[LL1UIManager] Index {resultSpriteIndex} melebihi panjang resultSprites ({resultSprites.Length})!");
            return;
        }

        bool isPassed = gameManager != null && correctAnswers >= gameManager.minimumCorrectToPass;
        string buttonText = isPassed ? "Level Berikutnya" : "Coba Lagi";

        resultPanel.Show(correctAnswers, totalRounds, resultSprites[resultSpriteIndex], isPassed, buttonText, () =>
        {
            if (isPassed)
                SceneManager.LoadScene("LiteracyLevel");
            else if (gameManager != null)
                gameManager.RestartGame();
        });

        Debug.Log("[LL1UIManager] resultPanel.Show() berhasil dipanggil.");
    }

    private int GetResultSpriteIndex(int correctAnswers, int totalRounds)
    {
        if (resultSprites.Length > totalRounds)
            return Mathf.Clamp(correctAnswers, 0, resultSprites.Length - 1);

        int maxSpriteIndex = resultSprites.Length - 1;
        float normalizedScore = totalRounds <= 0 ? 0f : (float)correctAnswers / totalRounds;
        return Mathf.Clamp(Mathf.RoundToInt(normalizedScore * maxSpriteIndex), 0, maxSpriteIndex);
    }

    /// <summary>Reset UI ke state awal (sebelum meja di-place) — dipakai saat retry.</summary>
    public void ResetUI()
    {
        // Kembali ke State 1: hanya header
        ShowHeaderOnly();

        if (questionText != null)  { questionText.gameObject.SetActive(true); questionText.text = ""; }
        if (roundText != null)     { roundText.gameObject.SetActive(true);    roundText.text = ""; }
        if (feedbackText != null)  feedbackText.text = "";
        if (submitButton != null)  submitButton.gameObject.SetActive(false);
    }
}
