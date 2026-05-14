using UnityEngine;

public class ML1GameManager : MonoBehaviour
{
    public GameObject applePrefab;

    private GameObject currentMeja;
    public int applesOnLeft;
    public int applesOnRight;
    private bool questionActive = false;
    private ML1UIManager uiManager;
    
    private int currentRound = 0;
    private const int MAX_ROUNDS = 5;
    private int correctAnswers = 0;

    void Awake()
    {
        uiManager = FindFirstObjectByType<ML1UIManager>();
    }

    public void SetMeja(GameObject meja)
    {
        currentMeja = meja;
        Debug.Log("Meja diterima GameManager");
    }

    public void StartGame()
    {
        Debug.Log("Game Started");
        
        // Aktifkan indicators di Meja
        if (currentMeja != null)
        {
            MejaSetup mejaSetup = currentMeja.GetComponent<MejaSetup>();
            if (mejaSetup != null)
                mejaSetup.ActivateIndicators();
        }
        
        StartQuestion();
    }

    void StartQuestion()
    {
        // Check apakah sudah 5 round
        if (currentRound >= MAX_ROUNDS)
        {
            ShowFinalResult();
            return;
        }

        currentRound++;
        Debug.Log($"Soal ke-{currentRound} mulai");

        // Random jumlah buah kanan dan kiri (berbeda)
        applesOnLeft = Random.Range(2, 9);
        applesOnRight = Random.Range(2, 9);

        // Pastikan berbeda
        while (applesOnRight == applesOnLeft)
        {
            applesOnRight = Random.Range(3, 8);
        }

        Debug.Log($"Round {currentRound}: Buah Kiri: {applesOnLeft}, Buah Kanan: {applesOnRight}");

        SpawnApplesOnBothSides(applesOnLeft, applesOnRight);
        questionActive = true;
        
        // Update UI dengan round info
        if (uiManager != null)
            uiManager.UpdateRoundInfo(currentRound, MAX_ROUNDS);
    }

    void SpawnApplesOnBothSides(int leftCount, int rightCount)
    {
        if (currentMeja == null)
        {
            Debug.LogError("Meja belum ada!");
            return;
        }

        Transform spawnAreaLeft = currentMeja.transform.Find("Left");
        Transform spawnAreaRight = currentMeja.transform.Find("Right");

        if (spawnAreaLeft == null || spawnAreaRight == null)
        {
            Debug.LogError("SpawnAreaLeft atau SpawnAreaRight tidak ditemukan!");
            return;
        }

        // Spawn buah di sisi kiri
        SpawnApplesAtArea(spawnAreaLeft, leftCount);

        // Spawn buah di sisi kanan
        SpawnApplesAtArea(spawnAreaRight, rightCount);
    }

    void SpawnApplesAtArea(Transform spawnArea, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                0,
                Random.Range(-0.3f, 0.3f)
            );

            Vector3 spawnPos = spawnArea.position + randomOffset;

            // Spawn buah sebagai child dari spawnArea
            Instantiate(applePrefab, spawnPos, Quaternion.identity, spawnArea);
        }
    }

    // Method untuk handle user memilih sisi kiri
    public void SelectLeftSide()
    {
        if (!questionActive) return;

        if (applesOnLeft > applesOnRight)
        {
            Debug.Log("BENAR! Sisi kiri lebih banyak");
            OnAnswerCorrect();
        }
        else
        {
            Debug.Log("SALAH! Sisi kiri lebih sedikit");
            OnAnswerWrong();
        }
    }

    // Method untuk handle user memilih sisi kanan
    public void SelectRightSide()
    {
        if (!questionActive) return;

        if (applesOnRight > applesOnLeft)
        {
            Debug.Log("BENAR! Sisi kanan lebih banyak");
            OnAnswerCorrect();
        }
        else
        {
            Debug.Log("SALAH! Sisi kanan lebih sedikit");
            OnAnswerWrong();
        }
    }

    void OnAnswerCorrect()
    {
        Debug.Log("Jawaban Benar!");
        correctAnswers++;
        questionActive = false;
        
        if (uiManager != null)
            uiManager.OnAnswerCorrect();
        
        ClearApples();
        
        // Delay sebelum soal berikutnya
        Invoke(nameof(StartQuestion), 2f);
    }

    void OnAnswerWrong()
    {
        Debug.Log("Jawaban Salah!");
        questionActive = false;
        
        if (uiManager != null)
            uiManager.OnAnswerWrong();
        
        ClearApples();
        
        // Delay sebelum soal berikutnya
        Invoke(nameof(StartQuestion), 2f);
    }

    void ShowFinalResult()
    {
        Debug.Log($"=== GAME OVER ===");
        Debug.Log($"Benar: {correctAnswers} dari {MAX_ROUNDS}");
        
        if (uiManager != null)
            uiManager.ShowFinalResult(correctAnswers, MAX_ROUNDS);
        
        ClearApples();
    }

    void ClearApples()
    {
        // Hapus semua apple dari scene
        GameObject[] apples = GameObject.FindGameObjectsWithTag("Apple");
        foreach (GameObject apple in apples)
        {
            Destroy(apple);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Game Restarted!");
        
        // Reset semua variabel
        currentRound = 0;
        correctAnswers = 0;
        questionActive = false;
        
        // Clear apples
        ClearApples();
        
        // Mulai game lagi
        StartGame();
    }
}