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

    // Soal untuk mode Isi Bagian Kosong (Part 2)
    private Question[] fillBlankQuestions = new Question[]
    {
        new Question { display = "10 + ___ = 24", answer = 14 },
        new Question { display = "12 + ___ = 30", answer = 18 },
        new Question { display = "25 - ___ = 13", answer = 12 },
        new Question { display = "23 - ___ = 11", answer = 12 },
        new Question { display = "___ - 30 = 17", answer = 47 },
    };

    // ─── State ────────────────────────────────────────────────
    private enum GameMode { MathArrange, MathFillBlank }
    private GameMode currentMode;

    private Question[] activeQuestions;
    private int currentRound = 0;
    private const int MAX_ROUNDS = 5;
    private int correctAnswers = 0;
    private bool questionActive = false;

    private ML2UIManager uiManager;

    // ─── Lifecycle ────────────────────────────────────────────
    void Start()
    {
        uiManager = FindFirstObjectByType<ML2UIManager>();

        // Tentukan mode dari PlayerPrefs (diatur oleh MarkerHandler)
        string mode = PlayerPrefs.GetString("MODE", "MATH_ARRANGE");
        currentMode = (mode == "MATH_FILLBLANK") ? GameMode.MathFillBlank : GameMode.MathArrange;
        activeQuestions = (currentMode == GameMode.MathFillBlank) ? fillBlankQuestions : arrangeQuestions;

        Debug.Log($"[ML2GameManager] Start - Mode: {currentMode}");
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
