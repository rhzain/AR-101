using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class LL1GameManager : MonoBehaviour
{
    // ─── Referensi Scene ──────────────────────────────────────
    [Header("Prefabs")]
    public GameObject cardPrefab;        // Prefab kartu huruf (Bagian 1)
    public GameObject cardPrefabWord;    // Prefab kartu kata (Bagian 2) — opsional, jika kosong pakai cardPrefab
    public GameObject slotPrefab;        // Prefab slot jawaban

    [Header("Titik Spawn di Meja")]
    [Tooltip("Transform titik pusat source area (sisi kiri meja)")]
    public Transform sourceAreaCenter;
    [Tooltip("Transform titik pusat slot area (sisi kanan meja)")]
    public Transform slotAreaCenter;

    [Header("Ukuran Kartu")]
    [Tooltip("Ukuran kartu huruf (kotak 1:1) dalam meter")]
    public float letterSize = 0.8f;
    [Tooltip("Lebar per karakter untuk kartu kata")]
    public float charWidth = 0.4f;
    [Tooltip("Panjang (depth) kartu kata")]
    public float wordCardDepth = 0.12f;
    [Tooltip("Gap antar kartu/slot")]
    public float itemGap = 0.015f;

    [Header("UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI feedbackText;
    public Button submitButton;
    public Button retryButton;

    // ─── Data Soal ────────────────────────────────────────────
    private struct Question
    {
        public string answer;   // Jawaban yang benar (huruf/kata dipisah spasi atau per karakter)
        public bool isWord;     // true = soal huruf→kata, false = soal kata→kalimat
    }

    // Bagian 1: Susun Huruf → Kata
    private Question[] spellQuestions = new Question[]
    {
        new Question { answer = "APEL",    isWord = true },
        new Question { answer = "BOLA",    isWord = true },
        new Question { answer = "BUDI",    isWord = true },
        new Question { answer = "PASAR",   isWord = true },
        new Question { answer = "BINTANG", isWord = true },
    };

    // Bagian 2: Susun Kata → Kalimat
    private Question[] sentenceQuestions = new Question[]
    {
        new Question { answer = "BUDI PERGI KE PASAR",   isWord = false },
        new Question { answer = "AGUS BERMAIN BOLA",     isWord = false },
        new Question { answer = "IBU MEMBELI IKAN",      isWord = false },
        new Question { answer = "AYAH TIDUR DI KAMAR",   isWord = false },
        new Question { answer = "KAKAK MAKAN NASI",      isWord = false },
    };

    // ─── State ────────────────────────────────────────────────
    private enum GamePhase { Spell, Sentence }
    private GamePhase currentPhase = GamePhase.Spell;

    private int currentRound  = 0;
    private const int ROUNDS_PER_PHASE = 5;
    private int correctAnswers = 0;
    private bool questionActive = false;

    private Question[] CurrentQuestions =>
        currentPhase == GamePhase.Spell ? spellQuestions : sentenceQuestions;

    // Daftar objek yang di-spawn tiap ronde (untuk dibersihkan)
    private System.Collections.Generic.List<GameObject> spawnedCards = new();
    private System.Collections.Generic.List<GameObject> spawnedSlots = new();

    // ─── Lifecycle ────────────────────────────────────────────
    void Start()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitAnswer);
            submitButton.gameObject.SetActive(false);
        }
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RestartGame);
            retryButton.gameObject.SetActive(false);
        }
        if (feedbackText != null) feedbackText.text = "";
    }

    // ─── Game Flow ────────────────────────────────────────────
    public void StartGame()
    {
        currentPhase   = GamePhase.Spell;
        currentRound   = 0;
        correctAnswers = 0;
        questionActive = false;
        StartNextQuestion();
    }

    void StartNextQuestion()
    {
        if (currentRound >= ROUNDS_PER_PHASE)
        {
            if (currentPhase == GamePhase.Spell)
            {
                // Bagian 1 selesai → lanjut ke Bagian 2 (Susun Kalimat)
                currentPhase = GamePhase.Sentence;
                currentRound = 0;
                if (feedbackText != null) feedbackText.text = "Bagian 2: Susun Kalimat!";
                if (roundText != null) roundText.text = "";
                StartCoroutine(TransitionDelay());
                return;
            }
            else
            {
                ShowFinalResult();
                return;
            }
        }

        ClearSpawnedObjects();
        currentRound++;

        Question q = CurrentQuestions[currentRound - 1];
        string[] tokens = q.isWord
            ? GetLetters(q.answer)
            : q.answer.Split(' ');

        if (questionText != null)
            questionText.text = q.isWord
                ? "Susun huruf-huruf menjadi kata!"
                : $"Susun kalimat yang benar!";

        string phaseLabel = currentPhase == GamePhase.Spell ? "Kata" : "Kalimat";
        if (roundText != null)
            roundText.text = $"Bagian {(currentPhase == GamePhase.Spell ? 1 : 2)} - Soal {currentRound}/{ROUNDS_PER_PHASE}";
        if (feedbackText != null)
            feedbackText.text = "";

        SpawnCards(tokens);
        SpawnSlots(tokens.Length);

        questionActive = true;
        if (submitButton != null) submitButton.gameObject.SetActive(true);

        Debug.Log($"[{currentPhase}] Ronde {currentRound}: '{q.answer}' ({tokens.Length} token)");
    }

    IEnumerator TransitionDelay()
    {
        yield return new WaitForSeconds(2f);
        StartNextQuestion();
    }

    public void SubmitAnswer()
    {
        if (!questionActive) return;

        CardSlot[] slots = FindObjectsByType<CardSlot>(FindObjectsSortMode.None);
        System.Array.Sort(slots, (a, b) => a.slotIndex.CompareTo(b.slotIndex));

        string[] playerTokens = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            playerTokens[i] = slots[i].GetContent();

        string playerAnswer  = string.Join(" ", playerTokens).Trim();
        Question q           = CurrentQuestions[currentRound - 1];
        string correctAnswer = q.answer.ToUpper().Trim();

        if (q.isWord)
        {
            playerAnswer  = playerAnswer.Replace(" ", "");
            correctAnswer = correctAnswer.Replace(" ", "");
        }

        questionActive = false;
        if (submitButton != null) submitButton.gameObject.SetActive(false);

        if (playerAnswer == correctAnswer)
        {
            correctAnswers++;
            if (feedbackText != null) feedbackText.text = "Benar!";
        }
        else
        {
            string display = q.isWord ? correctAnswer : q.answer;
            if (feedbackText != null) feedbackText.text = $"Salah. Jawaban: {display}";
        }

        StartCoroutine(NextQuestionDelay());
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(2f);
        StartNextQuestion();
    }

    void ShowFinalResult()
    {
        ClearSpawnedObjects();
        if (questionText != null)  questionText.text  = "";
        if (roundText != null)     roundText.text     = "Selesai!";
        if (feedbackText != null)  feedbackText.text  = $"{correctAnswers}/{ROUNDS_PER_PHASE * 2} Benar";
        if (submitButton != null)  submitButton.gameObject.SetActive(false);
        if (retryButton != null)   retryButton.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        StartGame();
    }

    // ─── Spawning ─────────────────────────────────────────────
    /// <summary>Hitung lebar seragam untuk slot berdasarkan kata terpanjang.</summary>
    private float CalcWordWidth(string[] tokens)
    {
        float maxW = 0f;
        foreach (string t in tokens)
        {
            float w = Mathf.Max(0.12f, Mathf.Pow(t.Length, 2) * 0.01f);
            if (w > maxW) maxW = w;
        }
        return maxW;
    }

    void SpawnCards(string[] tokens)
    {
        if (cardPrefab == null || sourceAreaCenter == null) return;

        bool isSpell = CurrentQuestions[currentRound - 1].isWord;
        GameObject activePrefab = (!isSpell && cardPrefabWord != null) ? cardPrefabWord : cardPrefab;

        // Hitung lebar masing-masing kartu
        float[] widths = new float[tokens.Length];
        float[] depths = new float[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            if (isSpell)
            {
                widths[i] = letterSize;
                depths[i] = letterSize;
            }
            else
            {
                // Setiap kata punya lebar sendiri (koefisien 0.8^jumlahHuruf, minimum 0.12)
                widths[i] = Mathf.Max(0.12f, Mathf.Pow(tokens[i].Length, 2)*0.01f);
                depths[i] = wordCardDepth;
            }
        }

        // Acak urutan token (dan ukurannya ikut)
        string[] shuffled = (string[])tokens.Clone();
        float[] shuffledW = (float[])widths.Clone();
        float[] shuffledD = (float[])depths.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            (shuffledW[i], shuffledW[j]) = (shuffledW[j], shuffledW[i]);
            (shuffledD[i], shuffledD[j]) = (shuffledD[j], shuffledD[i]);
        }

        // Hitung total lebar (setiap kartu bisa beda ukuran)
        float totalWidth = 0f;
        foreach (float w in shuffledW) totalWidth += w;
        totalWidth += (shuffled.Length - 1) * itemGap;

        // Posisi dihitung sepanjang sumbu right meja agar sejajar
        Vector3 right   = sourceAreaCenter.right;
        Vector3 forward = sourceAreaCenter.forward;
        Quaternion rot  = sourceAreaCenter.rotation;
        Vector3 center  = sourceAreaCenter.position;

        // Batas lebar 1 baris (bisa di-tweak di Inspector)
        float maxRowWidth = 0.5f;

        if (totalWidth <= maxRowWidth || shuffled.Length <= 2)
        {
            // === SATU BARIS ===
            Vector3 start = center - right * (totalWidth / 2f);
            float cursor  = 0f;
            for (int i = 0; i < shuffled.Length; i++)
            {
                Vector3 pos = start + right * (cursor + shuffledW[i] / 2f);
                pos.y = DraggableCard.TableSurfaceY + 0.005f;
                SpawnSingleCard(activePrefab, shuffled[i], shuffledW[i], shuffledD[i], pos, rot);
                cursor += shuffledW[i] + itemGap;
            }
        }
        else
        {
            // === DUA BARIS ===
            int half = shuffled.Length / 2;
            float rowSpacing = wordCardDepth * 1.5f + itemGap; // jarak antar baris

            for (int row = 0; row < 2; row++)
            {
                int startIdx = (row == 0) ? 0 : half;
                int endIdx   = (row == 0) ? half : shuffled.Length;

                // Hitung lebar baris ini
                float rowWidth = 0f;
                for (int i = startIdx; i < endIdx; i++) rowWidth += shuffledW[i];
                rowWidth += (endIdx - startIdx - 1) * itemGap;

                // Offset baris: baris 0 ke belakang, baris 1 ke depan
                Vector3 rowCenter = center + forward * ((row == 0 ? 1 : -1) * rowSpacing / 2f);
                Vector3 start = rowCenter - right * (rowWidth / 2f);
                float cursor  = 0f;

                for (int i = startIdx; i < endIdx; i++)
                {
                    Vector3 pos = start + right * (cursor + shuffledW[i] / 2f);
                    pos.y = DraggableCard.TableSurfaceY + 0.005f;
                    SpawnSingleCard(activePrefab, shuffled[i], shuffledW[i], shuffledD[i], pos, rot);
                    cursor += shuffledW[i] + itemGap;
                }
            }
        }
    }

    private void SpawnSingleCard(GameObject prefab, string token, float w, float d, Vector3 pos, Quaternion rot)
    {
        GameObject card = Instantiate(prefab, pos, rot);
        card.SetActive(true);
        DraggableCard dc = card.GetComponent<DraggableCard>();
        if (dc != null)
        {
            dc.ResizeToWidth(w, d);
            dc.Initialize(token, pos);
        }
        spawnedCards.Add(card);
    }

    void SpawnSlots(int count)
    {
        if (slotPrefab == null || slotAreaCenter == null) return;

        bool isSpell = CurrentQuestions[currentRound - 1].isWord;
        float slotW, slotD;
        if (isSpell)
        {
            slotW = letterSize;
            slotD = letterSize;
        }
        else
        {
            string[] tokens = CurrentQuestions[currentRound - 1].answer.Split(' ');
            slotW = CalcWordWidth(tokens);
            slotD = wordCardDepth;
        }

        float totalWidth = count * slotW + (count - 1) * itemGap;

        Vector3 right  = slotAreaCenter.right;
        Quaternion rot  = slotAreaCenter.rotation;
        Vector3 center  = slotAreaCenter.position;
        Vector3 start   = center - right * (totalWidth / 2f) + right * (slotW / 2f);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = start + right * (i * (slotW + itemGap));
            pos.y = DraggableCard.TableSurfaceY + 0.002f;

            GameObject slot = Instantiate(slotPrefab, pos, rot);
            slot.SetActive(true);

            // Resize slot lebar dan depth, Y tetap dari prefab
            float sy = slot.transform.localScale.y;
            slot.transform.localScale = new Vector3(slotW, sy, slotD);

            CardSlot cs = slot.GetComponent<CardSlot>();
            if (cs != null) cs.slotIndex = i;

            spawnedSlots.Add(slot);
        }
    }

    void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedCards) if (obj != null) Destroy(obj);
        foreach (var obj in spawnedSlots) if (obj != null) Destroy(obj);
        spawnedCards.Clear();
        spawnedSlots.Clear();
    }

    // ─── Utils ────────────────────────────────────────────────
    /// <summary>Ubah string kata menjadi array per huruf. Contoh: "APEL" → ["A","P","E","L"]</summary>
    private string[] GetLetters(string word)
    {
        string upper = word.ToUpper();
        string[] letters = new string[upper.Length];
        for (int i = 0; i < upper.Length; i++)
            letters[i] = upper[i].ToString();
        return letters;
    }
}
