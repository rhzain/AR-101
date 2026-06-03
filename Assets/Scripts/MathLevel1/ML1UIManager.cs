using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengelola semua tampilan UI untuk Math Level 1.
/// Pasang ke GameObject UI Manager di scene MathLevel1.
/// </summary>
public class ML1UIManager : MonoBehaviour
{
    [Header("Teks UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI countApples;
    public TMP_Text roundText;
    public Sprite[] resultSprites; // Masukkan 6 gambar (0-5 benar)
    public ResultPanel resultPanel;

    [Header("Canvas")]
    [Tooltip("Canvas header — selalu tampil sebelum dan selama gameplay")]
    public GameObject canvasHeader;
    [Tooltip("Canvas soal (main) — hanya tampil setelah meja di-place")]
    public GameObject canvasSoal;
    [Tooltip("Canvas recap akhir game — hanya tampil saat game selesai")]
    public GameObject canvasRecap;

    private ML1GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<ML1GameManager>();

        if (resultPanel != null) resultPanel.Hide();

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

    // ─── Dipanggil oleh ML1GameManager ───────────────────────

    public void UpdateRoundInfo(int currentRound, int maxRounds, bool asksLess)
    {
        if (roundText != null)
            roundText.text = $"Soal {currentRound} dari {maxRounds}";

        // Pastikan canvas soal aktif dan reset elemen
        ShowGameplayCanvas();

        if (questionText != null)
            questionText.text = asksLess ? "Pilih sisi yang lebih sedikit!" : "Pilih sisi yang lebih banyak!";

        if (countApples != null)  countApples.gameObject.SetActive(false);
        if (resultPanel != null)  resultPanel.Hide();
    }

    void ShowCounts(string answer)
    {
        if (countApples != null && gameManager != null)
        {
            countApples.text = answer;
            countApples.gameObject.SetActive(true);
            countApples.text += " " + GetComparisonText();
        }
    }

    private string GetComparisonText()
    {
        if (gameManager == null)
            return "";

        bool leftMatchesPrompt = gameManager.currentQuestionAsksLess
            ? gameManager.applesOnLeft < gameManager.applesOnRight
            : gameManager.applesOnLeft > gameManager.applesOnRight;

        string selectedSide = leftMatchesPrompt ? "Kiri" : "Kanan";
        string otherSide = leftMatchesPrompt ? "Kanan" : "Kiri";
        int selectedCount = leftMatchesPrompt ? gameManager.applesOnLeft : gameManager.applesOnRight;
        int otherCount = leftMatchesPrompt ? gameManager.applesOnRight : gameManager.applesOnLeft;
        string comparison = gameManager.currentQuestionAsksLess ? "lebih sedikit dari" : "lebih banyak dari";

        return $"{selectedSide} ({selectedCount}) {comparison} {otherSide} ({otherCount})";
    }

    public void OnAnswerCorrect()
    {
        // Tetap di canvas soal — tampilkan hasil per jawaban
        ShowCounts("Benar!");
        if (questionText != null) questionText.text = "";
    }

    public void OnAnswerWrong()
    {
        // Tetap di canvas soal — tampilkan hasil per jawaban
        ShowCounts("Salah!");
        if (questionText != null) questionText.text = "";
    }

    public void ShowFinalResult(int correctAnswers, int totalRounds)
    {
        Debug.Log($"[ML1UIManager] ShowFinalResult dipanggil: {correctAnswers}/{totalRounds}");
        Debug.Log($"[ML1UIManager] canvasRecap null? {canvasRecap == null}");
        Debug.Log($"[ML1UIManager] resultPanel null? {resultPanel == null}");
        Debug.Log($"[ML1UIManager] resultSprites null? {resultSprites == null} | length: {resultSprites?.Length}");

        // State 3: hanya recap
        ShowRecapCanvas();

        if (resultPanel == null)
        {
            Debug.LogError("[ML1UIManager] resultPanel BELUM DI-ASSIGN di Inspector!");
            return;
        }

        if (resultSprites == null || resultSprites.Length == 0)
        {
            Debug.LogError("[ML1UIManager] resultSprites KOSONG! Assign sprite di Inspector.");
            return;
        }

        if (correctAnswers >= resultSprites.Length)
        {
            Debug.LogError($"[ML1UIManager] Index {correctAnswers} melebihi panjang resultSprites ({resultSprites.Length})!");
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

        Debug.Log("[ML1UIManager] resultPanel.Show() berhasil dipanggil.");
    }
}
