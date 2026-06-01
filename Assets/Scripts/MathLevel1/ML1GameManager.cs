using UnityEngine;

public class ML1GameManager : MonoBehaviour
{
    public GameObject applePrefab;

    [Tooltip("Radius area scatter apel di setiap sisi meja (meter)")]
    public float spawnRadius = 0.25f;
    [Tooltip("Offset tinggi apel di atas permukaan meja saat spawn (meter)")]
    public float spawnHeightOffset = 0.05f;
    [Tooltip("Jarak minimal dari pusat apel lain saat spawn")]
    public float appleCollisionRadius = 0.08f;
    [Tooltip("Jumlah percobaan mencari posisi kosong untuk setiap apel")]
    public int maxSpawnAttempts = 30;

    private GameObject currentMeja;
    public int applesOnLeft;
    public int applesOnRight;
    private bool questionActive = false;
    private ML1UIManager uiManager;
    
    private int currentRound = 0;
    private const int MAX_ROUNDS = 5;
    private int correctAnswers = 0;

    [Header("Progress")]
    public string progressSubject = "Math";
    public int progressLevelNumber = 1;
    public int minimumCorrectToPass = 4;

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
            Vector3 spawnPos = GetClearSpawnPosition(spawnArea);

            // Rotasi ikut meja agar tidak miring di AR
            Instantiate(applePrefab, spawnPos, spawnArea.rotation, spawnArea);
        }
    }

    Vector3 GetClearSpawnPosition(Transform spawnArea)
    {
        Vector3 fallbackPosition = spawnArea.position + spawnArea.up * spawnHeightOffset;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Offset menggunakan local axis spawnArea agar sejajar rotasi meja
            float rx = Random.Range(-spawnRadius, spawnRadius);
            float rz = Random.Range(-spawnRadius, spawnRadius);

            Vector3 candidatePosition = spawnArea.position
                                      + spawnArea.right * rx
                                      + spawnArea.forward * rz
                                      + spawnArea.up * spawnHeightOffset;

            if (!IsBlockedByApple(candidatePosition))
                return candidatePosition;

            fallbackPosition = candidatePosition;
        }

        Debug.LogWarning("Tidak menemukan posisi spawn apel yang kosong. Perbesar spawnRadius atau kecilkan appleCollisionRadius.");
        return fallbackPosition;
    }

    bool IsBlockedByApple(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(
            position,
            appleCollisionRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Apple"))
                return true;
        }

        return false;
    }

    // Visualisasi area scatter di Scene view
    void OnDrawGizmosSelected()
    {
        if (currentMeja == null) return;
        Transform left  = currentMeja.transform.Find("Left");
        Transform right = currentMeja.transform.Find("Right");

        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        if (left  != null) DrawLocalSquare(left,  spawnRadius);
        if (right != null) DrawLocalSquare(right, spawnRadius);
    }

    void DrawLocalSquare(Transform t, float radius)
    {
        // Gambar persegi berdasarkan local axis agar sejajar rotasi meja
        Vector3 c  = t.position;
        Vector3 r  = t.right   * radius;
        Vector3 f  = t.forward * radius;
        Vector3 p0 = c - r - f;
        Vector3 p1 = c + r - f;
        Vector3 p2 = c + r + f;
        Vector3 p3 = c - r + f;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
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

        LevelProgress.SaveResult(progressSubject, progressLevelNumber, correctAnswers, minimumCorrectToPass);
        
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
