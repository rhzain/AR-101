using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ML1UIManager : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI countApples;
    public TMP_Text roundText;
    public Button retryButton;
    public Sprite[] resultSprites; // Masukkan 6 gambar (0-5 benar)
    public ResultPanel resultPanel;
    
    private ML1GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<ML1GameManager>();
        
        if (resultPanel != null) resultPanel.Hide();
        
        UpdateUI();
        
        // Setup retry button
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
            retryButton.onClick.AddListener(() => 
            {
                retryButton.gameObject.SetActive(false);
                UpdateUI();
                gameManager.RestartGame();
            });
        }
    }

    void UpdateUI()
    {
        if (questionText != null)
        {
            questionText.text = "Pilih sisi yang lebih banyak!";
        }

        // Sembunyikan teks jumlah buah saat belum menjawab
        if (countApples != null) countApples.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.Hide();
    }

    public void UpdateRoundInfo(int currentRound, int maxRounds)
    {
        if (roundText != null)
        {
            roundText.text = $"Soal {currentRound} dari 5";
        }
        
        // Reset UI tiap kali soal baru dimulai
        UpdateUI();
    }

    void ShowCounts(string answer)
    {
        if (countApples != null && gameManager != null)
        {
            countApples.text = answer;
            countApples.gameObject.SetActive(true);
            if (gameManager.applesOnLeft > gameManager.applesOnRight) {
                countApples.text += $" Kiri ({gameManager.applesOnLeft}) lebih dari Kanan ({gameManager.applesOnRight})";
            } else {
                countApples.text += $" Kanan ({gameManager.applesOnRight}) lebih dari Kiri ({gameManager.applesOnLeft})";
            }
        }
    }

    public void OnAnswerCorrect()
    {   
        ShowCounts("Benar!");
        questionText.text = "";
    }

    public void OnAnswerWrong()
    {
            
        ShowCounts("Salah!");
        questionText.text = "";
    }

    public void ShowFinalResult(int correctAnswers, int totalRounds)
    {
        Debug.Log($"ShowFinalResult dipanggil: {correctAnswers}/{totalRounds}");
        Debug.Log($"resultPanel null? {resultPanel == null}");
        Debug.Log($"resultSprites null? {resultSprites == null}, length: {resultSprites?.Length}");

        if (resultPanel != null && resultSprites != null)
        {
            Debug.Log($"Sprite index {correctAnswers}: {resultSprites[correctAnswers]?.name}");
            resultPanel.Show(correctAnswers, totalRounds, resultSprites[correctAnswers]);
        }
    }

}
