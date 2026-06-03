using UnityEngine;
using System.Collections;

public class ML2GameManager : MonoBehaviour
{
    [Header("Referensi Scene")]
    public DropZone dropZone;
    public AppleBasket appleBasket;
    public GameObject mejaPrefab;

    // ─── Data Soal ───────────────────────────────────────────
    private struct Question
    {
        public string display;   // Teks yang ditampilkan ke pemain
        public int answer;       // Jawaban yang benar
    }

    // Soal untuk mode Susun Hasil (Part 1)
    private Question[] arrangeQuestions = new Question[]
    {
        new Question { display = "5 + 3 = ?",   answer = 8  },
        new Question { display = "4 + 4 = ?",   answer = 8  },
        new Question { display = "10 - 6 = ?",  answer = 4  },
        new Question { display = "14 - 7 = ?",  answer = 7  },
        new Question { display = "20 - 16 = ?", answer = 4  },
    };

    private Question[] activeQuestions;
    private int currentRound = 0;
    private const int MAX_ROUNDS = 5;
    private int correctAnswers = 0;
    private bool questionActive = false;

    [Header("Progress")]
    public string progressSubject = "Math";
    public int progressLevelNumber = 2;
    public int minimumCorrectToPass = 4;

    private ML2UIManager uiManager;

    // ─── Lifecycle ────────────────────────────────────────────
    void Start()
    {
        uiManager = FindFirstObjectByType<ML2UIManager>();
        activeQuestions = arrangeQuestions;

        Debug.Log("[ML2GameManager] Start - Mode: Susun Hasil");
    }

    void Update()
    {
        // Update teks jumlah apel secara real-time via UIManager
        if (uiManager != null && dropZone != null)
        {
            uiManager.UpdateAppleCount(dropZone.AppleCount);
        }
    }

    // ─── Game Flow ────────────────────────────────────────────
    public void StartGame()
    {
        currentRound = 0;
        correctAnswers = 0;
        questionActive = false;
        if (uiManager != null) uiManager.ResetUI();
        StartNextQuestion();
    }

    void StartNextQuestion()
    {
        if (currentRound >= MAX_ROUNDS)
        {
            ShowFinalResult();
            return;
        }

        currentRound++;
        Question q = activeQuestions[currentRound - 1];

        ClearAllApples();
        questionActive = true;

        if (uiManager != null)
            uiManager.ShowQuestion(q.display, currentRound, MAX_ROUNDS);

        Debug.Log($"[ML2GameManager] Ronde {currentRound}: {q.display} | Jawaban: {q.answer}");
    }

    public void SubmitAnswer()
    {
        if (!questionActive || dropZone == null) return;

        int playerAnswer = dropZone.AppleCount;
        int correctAnswer = activeQuestions[currentRound - 1].answer;
        bool isCorrect = (playerAnswer == correctAnswer);

        questionActive = false;

        if (isCorrect) correctAnswers++;

        if (uiManager != null)
            uiManager.ShowFeedback(isCorrect, playerAnswer, correctAnswer);

        Debug.Log(isCorrect
            ? $"[ML2GameManager] BENAR! Jawaban: {correctAnswer}"
            : $"[ML2GameManager] SALAH. Player: {playerAnswer}, Benar: {correctAnswer}");

        StartCoroutine(NextQuestionDelay());
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(2f);
        StartNextQuestion();
    }

    void ShowFinalResult()
    {
        ClearAllApples();

        LevelProgress.SaveResult(progressSubject, progressLevelNumber, correctAnswers, minimumCorrectToPass);

        if (uiManager != null)
            uiManager.ShowFinalResult(correctAnswers, MAX_ROUNDS);

        Debug.Log($"[ML2GameManager] === GAME OVER === Benar: {correctAnswers}/{MAX_ROUNDS}");
    }

    void ClearAllApples()
    {
        GameObject[] apples = GameObject.FindGameObjectsWithTag("Apple");
        foreach (GameObject apple in apples)
            Destroy(apple);

        if (dropZone != null)
            dropZone.ClearApples();
    }

    public void RestartGame()
    {
        StartGame();
    }
}
