using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MathLevel3
{
    public enum ML3StepInteraction
    {
        DragItems,
        SelectSlot,
        ObserveOnly,
        TextAnswer
    }

    [RequireComponent(typeof(AudioSource))]
    public class ML3GameManager : MonoBehaviour
    {
        [System.Serializable]
        public class SlotSetup
        {
            public ML3SlotId slot = ML3SlotId.Left;
            public bool active = true;
            public GameObject characterPrefab;
            public string label;
            public int itemCount;
            public ItemDropZone.ZoneMode zoneMode = ItemDropZone.ZoneMode.CountItems;
            public bool showTableModel = true;
        }

        [System.Serializable]
        public class QuestionStep
        {
            public string questionText;

            [TextArea(2, 4)]
            public string hintText;

            [Header("Audio")]
            [Tooltip("Audio instruksi untuk langkah ini. Isi 2 clip per soal lewat masing-masing step.")]
            public AudioClip instructionAudioClip;

            public ML3StepInteraction interaction = ML3StepInteraction.DragItems;
            public ML3SlotId targetSlot = ML3SlotId.Left;
            public ML3SlotId correctSelectedSlot = ML3SlotId.None;
            public bool validateTargetCount = true;
            public int targetCountAnswer;
            public bool validateSecondaryTargetCount = false;
            public ML3SlotId secondaryTargetSlot = ML3SlotId.None;
            public int secondaryTargetCountAnswer;
            public string answer;
        }

        [System.Serializable]
        public class StoryQuestion
        {
            [TextArea(2, 5)]
            public string storyText;

            [Header("Item")]
            public GameObject itemPrefab;
            public string itemId = "Item";

            [Header("Slot Awal")]
            public SlotSetup[] slots;

            [Header("Step Pertanyaan")]
            public QuestionStep[] steps;
        }

        [Header("Layout")]
        public ML3TableLayout tableLayout;

        [Header("Prefab Item")]
        public GameObject applePrefab;
        public GameObject bookPrefab;
        public GameObject fishPrefab;
        public GameObject candyPrefab;
        public GameObject orangePrefab;

        [Header("Prefab Karakter")]
        public GameObject budiPrefab;
        public GameObject ibuPrefab;
        public GameObject ayahPrefab;
        public GameObject sitiPrefab;

        [Header("Audio")]
        public AudioSource audioSource;
        [Tooltip("Audio yang diputar saat jawaban benar.")]
        public AudioClip jawabanBenarClip;
        [Tooltip("Audio yang diputar saat jawaban salah.")]
        public AudioClip jawabanSalahClip;
        [Tooltip("Audio yang diputar saat level selesai dan pemain lulus.")]
        public AudioClip levelCompleteClip;
        [Tooltip("Audio yang diputar saat level selesai tetapi pemain belum lulus.")]
        public AudioClip levelIncompleteClip;

        [Header("Data Soal")]
        [Tooltip("Aktifkan agar 5 soal Level 3 dibuat dari kode setiap StartGame.")]
        public bool buildQuestionsFromCode = true;
        public StoryQuestion[] questions;

        [Header("Spawn")]
        [Tooltip("Radius area scatter item di setiap spawn area.")]
        public float spawnRadius = 0.25f;
        [Tooltip("Offset tinggi item di atas spawn area.")]
        public float spawnHeightOffset = 0.05f;
        [Tooltip("Jarak minimal dari pusat item lain saat spawn.")]
        public float itemCollisionRadius = 0.08f;
        [Tooltip("Jumlah percobaan mencari posisi kosong untuk setiap item.")]
        public int maxSpawnAttempts = 30;

        [Header("Progress")]
        public string progressSubject = "Math";
        public int progressLevelNumber = 3;
        public int minimumCorrectToPass = 4;

        [Header("Flow")]
        public float nextStepDelay = 1.25f;
        public float nextQuestionDelay = 1.75f;

        [Tooltip("Untuk soal numerik, target count yang sudah benar ikut diterima sebagai jawaban. Berguna kalau input field belum commit saat tombol submit ditekan.")]
        public bool acceptCorrectTargetCountForNumericAnswers = true;

        private ML3UIManager uiManager;
        private int currentQuestionIndex;
        private int currentStepIndex;
        private int correctQuestions;
        private bool questionActive;
        private bool currentQuestionHadWrongAnswer;
        private Camera cam;

        private int MaxQuestions => questions == null ? 0 : questions.Length;
        private StoryQuestion CurrentQuestion => questions[currentQuestionIndex];
        private QuestionStep CurrentStep => CurrentQuestion.steps[currentStepIndex];

        private void Reset()
        {
            tableLayout = FindFirstObjectByType<ML3TableLayout>();
            FillDefaultQuestionTemplates();
        }

        private void OnValidate()
        {
            if (buildQuestionsFromCode && ShouldRefreshCodeQuestions())
                FillDefaultQuestionTemplates();
        }

        private void Awake()
        {
            uiManager = FindFirstObjectByType<ML3UIManager>();
            cam = Camera.main;

            if (tableLayout == null)
                tableLayout = FindFirstObjectByType<ML3TableLayout>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (!questionActive || MaxQuestions == 0 || CurrentStep.interaction != ML3StepInteraction.SelectSlot)
                return;

            if (WasPointerPressedThisFrame() && TryGetPointerPosition(out Vector2 pointerPosition))
                TrySelectSlot(pointerPosition);
        }

        [ContextMenu("Isi Template 5 Soal Level 3")]
        public void FillDefaultQuestionTemplates()
        {
            AudioClip[][] existingInstructionAudioClips = GetExistingInstructionAudioClips();

            questions = new[]
            {
                new StoryQuestion
                {
                    storyText = "Budi memiliki 5 apel. Ibu memberi Budi 3 apel lagi.",
                    itemPrefab = applePrefab,
                    itemId = "Apple",
                    slots = new[]
                    {
                        new SlotSetup { slot = ML3SlotId.Left, active = true, characterPrefab = budiPrefab, label = "Budi", itemCount = 5 },
                        new SlotSetup { slot = ML3SlotId.Center, active = true, characterPrefab = ibuPrefab, label = "Ibu", itemCount = 6 },
                        new SlotSetup { slot = ML3SlotId.Right, active = false, label = "", itemCount = 0 }
                    },
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Berapa apel yang dimiliki Budi sekarang?",
                            hintText = "Pindahkan 3 apel dari meja Ibu ke meja Budi.",
                            interaction = ML3StepInteraction.DragItems,
                            targetSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 8,
                            answer = "8"
                        },
                        new QuestionStep
                        {
                            questionText = "Apakah jumlah apel Budi lebih dari 7?",
                            hintText = "Hitung apel Budi lalu bandingkan dengan angka 7.",
                            interaction = ML3StepInteraction.TextAnswer,
                            targetSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 8,
                            answer = "ya|iya"
                        }
                    }
                },
                new StoryQuestion
                {
                    storyText = "Di meja ada 9 buku. Siti mengambil 2 buku.",
                    itemPrefab = bookPrefab,
                    itemId = "Book",
                    slots = new[]
                    {
                        new SlotSetup { slot = ML3SlotId.Left, active = true, characterPrefab = sitiPrefab, label = "Siti", itemCount = 0 },
                        new SlotSetup { slot = ML3SlotId.Center, active = true, label = "Meja", itemCount = 9 },
                        new SlotSetup { slot = ML3SlotId.Right, active = false, label = "", itemCount = 0 }
                    },
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Berapa buku yang tersisa di meja?",
                            hintText = "Berikan 2 buku kepada Siti.",
                            interaction = ML3StepInteraction.DragItems,
                            targetSlot = ML3SlotId.Center,
                            validateTargetCount = true,
                            targetCountAnswer = 7,
                            answer = "7"
                        },
                        new QuestionStep
                        {
                            questionText = "Apakah jumlah buku yang tersisa kurang dari 8?",
                            hintText = "Bandingkan jumlah buku yang tersisa dengan angka 8.",
                            interaction = ML3StepInteraction.TextAnswer,
                            targetSlot = ML3SlotId.Center,
                            validateTargetCount = true,
                            targetCountAnswer = 7,
                            answer = "ya|iya"
                        }
                    }
                },
                new StoryQuestion
                {
                    storyText = "Ibu memiliki 6 ikan. Ayah memberi Ibu 4 ikan lagi.",
                    itemPrefab = fishPrefab,
                    itemId = "Fish",
                    slots = new[]
                    {
                        new SlotSetup { slot = ML3SlotId.Left, active = true, characterPrefab = ibuPrefab, label = "Ibu", itemCount = 6 },
                        new SlotSetup { slot = ML3SlotId.Center, active = true, characterPrefab = ayahPrefab, label = "Ayah", itemCount = 5 },
                        new SlotSetup { slot = ML3SlotId.Right, active = false, label = "", itemCount = 0 }
                    },
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Mana yang lebih banyak, ikan Ibu atau ikan Ayah?",
                            hintText = "Ketuk meja yang ikannya lebih banyak.",
                            interaction = ML3StepInteraction.SelectSlot,
                            targetSlot = ML3SlotId.Left,
                            correctSelectedSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 6,
                            answer = "ibu|ikan ibu"
                        },
                        new QuestionStep
                        {
                            questionText = "Berapa ikan yang dimiliki Ibu sekarang?",
                            hintText = "Pindahkan 4 ikan dari meja Ayah ke meja Ibu.",
                            interaction = ML3StepInteraction.DragItems,
                            targetSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 10,
                            answer = "10"
                        }
                    }
                },
                new StoryQuestion
                {
                    storyText = "Budi memiliki 7 apel. Siti memiliki 5 apel.",
                    itemPrefab = applePrefab,
                    itemId = "Apple",
                    slots = new[]
                    {
                        new SlotSetup { slot = ML3SlotId.Left, active = true, characterPrefab = budiPrefab, label = "Budi", itemCount = 7 },
                        new SlotSetup { slot = ML3SlotId.Center, active = true, label = "Keranjang", itemCount = 0, zoneMode = ItemDropZone.ZoneMode.RemoveItems, showTableModel = false },
                        new SlotSetup { slot = ML3SlotId.Right, active = true, characterPrefab = sitiPrefab, label = "Siti", itemCount = 5 }
                    },
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Siapa yang memiliki apel lebih banyak?",
                            hintText = "Ketuk meja yang apelnya lebih banyak.",
                            interaction = ML3StepInteraction.SelectSlot,
                            targetSlot = ML3SlotId.Left,
                            correctSelectedSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 7,
                            answer = "budi"
                        },
                        new QuestionStep
                        {
                            questionText = "Berapa selisih jumlah apel mereka?",
                            hintText = "Buang satu persatu apel dari kedua meja untuk mendapat selisih",
                            interaction = ML3StepInteraction.DragItems,
                            targetSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 2,
                            validateSecondaryTargetCount = true,
                            secondaryTargetSlot = ML3SlotId.Right,
                            secondaryTargetCountAnswer = 0,
                            answer = "2"
                        }
                    }
                },
                new StoryQuestion
                {
                    storyText = "Di meja ada 8 wortel. Ibu menambahkan 2 wortel ke meja.",
                    itemPrefab = orangePrefab,
                    itemId = "Orange",
                    slots = new[]
                    {
                        new SlotSetup { slot = ML3SlotId.Left, active = true, label = "Meja", itemCount = 8 },
                        new SlotSetup { slot = ML3SlotId.Center, active = true, characterPrefab = ibuPrefab, label = "Ibu", itemCount = 5 },
                        new SlotSetup { slot = ML3SlotId.Right, active = false, label = "", itemCount = 0 }
                    },
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Berapa jumlah wortel di meja sekarang?",
                            hintText = "Pindahkan 2 wortel dari meja Ibu ke meja utama.",
                            interaction = ML3StepInteraction.DragItems,
                            targetSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 10,
                            answer = "10"
                        },
                        new QuestionStep
                        {
                            questionText = "Apakah jumlah wortel di meja lebih dari 9?",
                            hintText = "Bandingkan jumlah wortel di meja dengan angka 9.",
                            interaction = ML3StepInteraction.TextAnswer,
                            targetSlot = ML3SlotId.Left,
                            validateTargetCount = true,
                            targetCountAnswer = 10,
                            answer = "ya|iya"
                        }
                    }
                }
            };

            RestoreInstructionAudioClips(existingInstructionAudioClips);
        }

        private bool ShouldRefreshCodeQuestions()
        {
            if (questions == null || questions.Length != 5)
                return true;

            foreach (StoryQuestion question in questions)
            {
                if (question == null || question.slots == null || question.slots.Length == 0 || question.steps == null || question.steps.Length == 0)
                    return true;
            }

            if (questions[2].steps[0].interaction != ML3StepInteraction.SelectSlot
                || questions[3].steps[0].interaction != ML3StepInteraction.SelectSlot
                || questions[0].steps[1].interaction != ML3StepInteraction.TextAnswer
                || questions[1].steps[1].interaction != ML3StepInteraction.TextAnswer
                || questions[4].steps[1].interaction != ML3StepInteraction.TextAnswer
                || questions[0].slots[1].slot != ML3SlotId.Center
                || questions[0].slots[1].active == false
                || questions[0].slots[1].itemCount != 6
                || questions[2].slots[1].itemCount != 5
                || questions[4].slots[1].itemCount != 5
                || questions[3].steps[1].validateSecondaryTargetCount == false
                || questions[3].steps[1].secondaryTargetSlot != ML3SlotId.Right
                || questions[3].steps[1].secondaryTargetCountAnswer != 0
                || questions[3].slots[1].zoneMode != ItemDropZone.ZoneMode.RemoveItems
                || questions[3].slots[1].showTableModel
                || questions[3].slots[2].slot != ML3SlotId.Right
                || questions[3].slots[2].active == false)
                return true;

            return questions[0].itemPrefab != applePrefab
                || questions[1].itemPrefab != bookPrefab
                || questions[2].itemPrefab != fishPrefab
                || questions[3].itemPrefab != applePrefab
                || questions[4].itemPrefab != orangePrefab;
        }

        public void StartGame()
        {
            if (buildQuestionsFromCode)
                FillDefaultQuestionTemplates();

            currentQuestionIndex = 0;
            currentStepIndex = 0;
            correctQuestions = 0;
            questionActive = false;
            currentQuestionHadWrongAnswer = false;

            if (uiManager != null)
                uiManager.ResetUI();

            StartCurrentQuestion();
        }

        public void SubmitAnswer()
        {
            if (!questionActive || MaxQuestions == 0 || CurrentQuestion.steps == null || CurrentQuestion.steps.Length == 0)
                return;

            QuestionStep step = CurrentStep;
            string playerAnswer = uiManager != null ? uiManager.GetAnswer() : "";

            bool needsTextAnswer = StepNeedsTextAnswer(step);
            bool answerCorrect = !needsTextAnswer || IsAnswerCorrect(playerAnswer, step.answer);
            int playerCount = GetTargetCount(step);
            bool targetCorrect = IsTargetCountCorrect(step, playerCount);
            if (needsTextAnswer && !answerCorrect && acceptCorrectTargetCountForNumericAnswers && targetCorrect && IsNumericAnswer(step.answer, step.targetCountAnswer))
                answerCorrect = true;

            bool isCorrect = answerCorrect && targetCorrect;

            if (!isCorrect)
                currentQuestionHadWrongAnswer = true;

            StopAudio();
            PlayAudio(isCorrect ? jawabanBenarClip : jawabanSalahClip);

            if (uiManager != null)
            {
                uiManager.ShowFeedback(
                    isCorrect,
                    answerCorrect,
                    targetCorrect,
                    playerAnswer,
                    step.answer,
                    playerCount,
                    step.targetCountAnswer
                );

            }

            if (isCorrect)
            {
                questionActive = false;
                StartCoroutine(AdvanceAfterDelay());
            }
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            StopAudio();
            ClearLevelItems();
            StartGame();
        }

        private void StartCurrentQuestion()
        {
            if (MaxQuestions == 0)
            {
                Debug.LogError("[ML3GameManager] Data questions masih kosong.");
                return;
            }

            if (tableLayout == null)
            {
                Debug.LogError("[ML3GameManager] Table Layout belum di-assign.");
                return;
            }

            if (currentQuestionIndex >= MaxQuestions)
            {
                ShowFinalResult();
                return;
            }

            currentStepIndex = 0;
            currentQuestionHadWrongAnswer = false;

            StoryQuestion question = CurrentQuestion;
            ClearLevelItems();
            SetupQuestionLayout(question);
            ShowCurrentStep();

            Debug.Log($"[ML3GameManager] Soal {currentQuestionIndex + 1}: {question.storyText}");
        }

        private void SetupQuestionLayout(StoryQuestion question)
        {
            tableLayout.ApplyTableSurface();

            SetSlotActive(ML3SlotId.Left, false);
            SetSlotActive(ML3SlotId.Center, false);
            SetSlotActive(ML3SlotId.Right, false);

            if (question.slots == null)
                return;

            foreach (SlotSetup slotSetup in question.slots)
            {
                ML3Slot slot = tableLayout.GetSlot(slotSetup.slot);
                if (slot == null)
                {
                    Debug.LogWarning($"[ML3GameManager] Slot {slotSetup.slot} tidak ditemukan di TableLayout.");
                    continue;
                }

                slot.SetActive(slotSetup.active);
                if (!slotSetup.active)
                    continue;

                if (slotSetup.characterPrefab == null && !string.IsNullOrWhiteSpace(slotSetup.label))
                    Debug.LogWarning($"[ML3GameManager] Character prefab untuk label '{slotSetup.label}' belum di-assign.");

                slot.Setup(slotSetup.label, slotSetup.characterPrefab, question.itemId, slotSetup.zoneMode, slotSetup.showTableModel);
                SpawnItemsAtSlot(question.itemPrefab, question.itemId, slot, slotSetup.itemCount);
            }
        }

        private void ShowCurrentStep()
        {
            questionActive = true;
            SetDraggingEnabled(CurrentStep.interaction == ML3StepInteraction.DragItems);

            if (uiManager != null)
            {
                uiManager.ShowStep(
                    CurrentQuestion.storyText,
                    CurrentStep,
                    currentQuestionIndex + 1,
                    MaxQuestions,
                    currentStepIndex + 1,
                    CurrentQuestion.steps.Length,
                    GetTargetSlot(CurrentStep)
                );
            }

            PlayInstructionAudio(CurrentStep);
        }

        private void TrySelectSlot(Vector2 screenPosition)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || tableLayout == null)
                return;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, Physics.AllLayers, QueryTriggerInteraction.Collide))
                return;

            ML3Slot selectedSlot = hit.collider.GetComponentInParent<ML3Slot>();
            if (selectedSlot == null)
                return;

            ML3SlotId selectedSlotId = GetSlotId(selectedSlot);
            if (selectedSlotId == ML3SlotId.None)
                return;

            QuestionStep step = CurrentStep;
            bool selectionCorrect = selectedSlotId == step.correctSelectedSlot;
            int playerCount = GetTargetCount(step);
            bool targetCorrect = IsTargetCountCorrect(step, playerCount);
            bool isCorrect = selectionCorrect && targetCorrect;

            if (!isCorrect)
                currentQuestionHadWrongAnswer = true;

            StopAudio();
            PlayAudio(isCorrect ? jawabanBenarClip : jawabanSalahClip);

            if (uiManager != null)
            {
                uiManager.ShowSelectionFeedback(isCorrect, GetSlotLabel(step.correctSelectedSlot), GetSlotLabel(selectedSlotId));

            }

            if (isCorrect)
            {
                questionActive = false;
                StartCoroutine(AdvanceAfterDelay());
            }
        }

        private IEnumerator AdvanceAfterDelay()
        {
            bool isLastStep = currentStepIndex >= CurrentQuestion.steps.Length - 1;
            yield return new WaitForSeconds(isLastStep ? nextQuestionDelay : nextStepDelay);

            if (isLastStep)
            {
                correctQuestions++;

                currentQuestionIndex++;
                StartCurrentQuestion();
            }
            else
            {
                currentStepIndex++;
                ShowCurrentStep();
            }
        }

        private void SpawnItemsAtSlot(GameObject prefab, string itemId, ML3Slot slot, int count)
        {
            if (count <= 0)
                return;

            if (slot == null)
            {
                Debug.LogWarning($"[ML3GameManager] Gagal spawn {itemId}: slot null.");
                return;
            }

            if (slot.spawnArea == null)
            {
                Debug.LogWarning($"[ML3GameManager] Gagal spawn {itemId}: SpawnArea di slot '{slot.name}' belum di-assign / nama child bukan SpawnArea.");
                return;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[ML3GameManager] Gagal spawn {itemId}: prefab item belum di-assign di ML3GameManager.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = GetClearSpawnPosition(slot.spawnArea);
                GameObject itemObject = Instantiate(prefab, spawnPos, slot.spawnArea.rotation, slot.spawnArea);
                itemObject.SetActive(true);

                DraggableItem draggable = itemObject.GetComponent<DraggableItem>();
                if (draggable == null)
                    draggable = itemObject.AddComponent<DraggableItem>();

                draggable.itemId = string.IsNullOrWhiteSpace(itemId) ? "Item" : itemId;
                slot.TrackRuntimeObject(itemObject);
            }
        }

        private Vector3 GetClearSpawnPosition(Transform spawnArea)
        {
            Vector3 fallbackPosition = spawnArea.position + spawnArea.up * spawnHeightOffset;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                float rx = UnityEngine.Random.Range(-spawnRadius, spawnRadius);
                float rz = UnityEngine.Random.Range(-spawnRadius, spawnRadius);

                Vector3 candidatePosition = spawnArea.position
                                          + spawnArea.right * rx
                                          + spawnArea.forward * rz
                                          + spawnArea.up * spawnHeightOffset;

                if (!IsBlockedByItem(candidatePosition))
                    return candidatePosition;

                fallbackPosition = candidatePosition;
            }

            Debug.LogWarning("[ML3GameManager] Tidak menemukan posisi spawn kosong. Perbesar spawnRadius atau kecilkan itemCollisionRadius.");
            return fallbackPosition;
        }

        private bool IsBlockedByItem(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(
                position,
                itemCollisionRadius,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                if (hit.GetComponentInParent<DraggableItem>() != null)
                    return true;
            }

            return false;
        }

        private ML3Slot GetTargetSlot(QuestionStep step)
        {
            return tableLayout != null ? tableLayout.GetSlot(step.targetSlot) : null;
        }

        private int GetTargetCount(QuestionStep step)
        {
            ML3Slot targetSlot = GetTargetSlot(step);
            return targetSlot != null ? targetSlot.ItemCount : -1;
        }

        private int GetSlotCount(ML3SlotId slotId)
        {
            ML3Slot slot = tableLayout != null ? tableLayout.GetSlot(slotId) : null;
            return slot != null ? slot.ItemCount : -1;
        }

        private bool IsTargetCountCorrect(QuestionStep step, int playerCount)
        {
            bool primaryTargetCorrect = !step.validateTargetCount || playerCount == step.targetCountAnswer;
            if (!primaryTargetCorrect)
                return false;

            if (!step.validateSecondaryTargetCount)
                return true;

            return GetSlotCount(step.secondaryTargetSlot) == step.secondaryTargetCountAnswer;
        }

        private void PlayInstructionAudio(QuestionStep step)
        {
            if (step == null)
                return;

            PlayAudio(step.instructionAudioClip, true);
        }

        private AudioClip[][] GetExistingInstructionAudioClips()
        {
            if (questions == null)
                return null;

            AudioClip[][] clips = new AudioClip[questions.Length][];
            for (int questionIndex = 0; questionIndex < questions.Length; questionIndex++)
            {
                QuestionStep[] steps = questions[questionIndex]?.steps;
                if (steps == null)
                    continue;

                clips[questionIndex] = new AudioClip[steps.Length];
                for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
                    clips[questionIndex][stepIndex] = steps[stepIndex]?.instructionAudioClip;
            }

            return clips;
        }

        private void RestoreInstructionAudioClips(AudioClip[][] clips)
        {
            if (clips == null || questions == null)
                return;

            int questionCount = Mathf.Min(questions.Length, clips.Length);
            for (int questionIndex = 0; questionIndex < questionCount; questionIndex++)
            {
                QuestionStep[] steps = questions[questionIndex]?.steps;
                AudioClip[] stepClips = clips[questionIndex];
                if (steps == null || stepClips == null)
                    continue;

                int stepCount = Mathf.Min(steps.Length, stepClips.Length);
                for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
                {
                    if (steps[stepIndex] != null && stepClips[stepIndex] != null)
                        steps[stepIndex].instructionAudioClip = stepClips[stepIndex];
                }
            }
        }

        private void PlayAudio(AudioClip clip, bool stopCurrentAudio = false)
        {
            if (audioSource == null || clip == null)
                return;

            if (ButtonSfxManager.Instance != null && !ButtonSfxManager.Instance.IsSoundOn())
                return;

            if (stopCurrentAudio)
                audioSource.Stop();

            audioSource.PlayOneShot(clip);
        }

        private void StopAudio()
        {
            if (audioSource != null)
                audioSource.Stop();
        }

        private ML3SlotId GetSlotId(ML3Slot slot)
        {
            if (tableLayout == null || slot == null)
                return ML3SlotId.None;

            if (slot == tableLayout.leftSlot) return ML3SlotId.Left;
            if (slot == tableLayout.centerSlot) return ML3SlotId.Center;
            if (slot == tableLayout.rightSlot) return ML3SlotId.Right;
            return ML3SlotId.None;
        }

        private string GetSlotLabel(ML3SlotId slotId)
        {
            ML3Slot slot = tableLayout != null ? tableLayout.GetSlot(slotId) : null;
            if (slot == null || slot.labelText == null || string.IsNullOrWhiteSpace(slot.labelText.text))
                return slotId.ToString();

            return slot.labelText.text;
        }

        private void SetDraggingEnabled(bool isEnabled)
        {
            DraggableItem[] items = FindObjectsByType<DraggableItem>(FindObjectsSortMode.None);
            foreach (DraggableItem item in items)
            {
                if (item != null)
                    item.enabled = isEnabled;
            }
        }

        private void SetSlotActive(ML3SlotId slotId, bool isActive)
        {
            ML3Slot slot = tableLayout != null ? tableLayout.GetSlot(slotId) : null;
            if (slot != null)
                slot.SetActive(isActive);
        }

        private bool IsAnswerCorrect(string playerAnswer, string correctAnswer)
        {
            if (string.IsNullOrWhiteSpace(correctAnswer))
                return true;

            string player = NormalizeAnswer(playerAnswer);
            string[] acceptedAnswers = correctAnswer.Split(new[] { '|', '/', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string acceptedAnswer in acceptedAnswers)
            {
                if (player == NormalizeAnswer(acceptedAnswer))
                    return true;
            }

            return false;
        }

        private string NormalizeAnswer(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToLowerInvariant();
        }

        private bool IsNumericAnswer(string answer, int expectedNumber)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return false;

            string[] acceptedAnswers = answer.Split(new[] { '|', '/', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string acceptedAnswer in acceptedAnswers)
            {
                if (int.TryParse(NormalizeAnswer(acceptedAnswer), out int parsedAnswer) && parsedAnswer == expectedNumber)
                    return true;
            }

            return false;
        }

        private bool StepNeedsTextAnswer(QuestionStep step)
        {
            return step.interaction == ML3StepInteraction.TextAnswer
                || (step.interaction == ML3StepInteraction.ObserveOnly && !string.IsNullOrWhiteSpace(step.answer));
        }

        private bool TryGetPointerPosition(out Vector2 pos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }

            pos = default;
            return false;
        }

        private bool WasPointerPressedThisFrame()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void ShowFinalResult()
        {
            ClearLevelItems();
            questionActive = false;
            StopAudio();
            PlayAudio(correctQuestions >= minimumCorrectToPass ? levelCompleteClip : levelIncompleteClip);

            LevelProgress.SaveResult(progressSubject, progressLevelNumber, correctQuestions, minimumCorrectToPass);

            if (uiManager != null)
                uiManager.ShowFinalResult(correctQuestions, MaxQuestions);

            Debug.Log($"[ML3GameManager] Game selesai. Benar: {correctQuestions}/{MaxQuestions}");
        }

        private void ClearLevelItems()
        {
            if (tableLayout != null)
                tableLayout.ClearRuntimeObjects();

            DraggableItem[] items = FindObjectsByType<DraggableItem>(FindObjectsSortMode.None);
            foreach (DraggableItem item in items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            ItemDropZone[] dropZones = FindObjectsByType<ItemDropZone>(FindObjectsSortMode.None);
            foreach (ItemDropZone dropZone in dropZones)
            {
                if (dropZone != null)
                    dropZone.ClearItems(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (tableLayout == null)
                return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
            DrawSlotSpawnArea(tableLayout.leftSlot);
            DrawSlotSpawnArea(tableLayout.centerSlot);
            DrawSlotSpawnArea(tableLayout.rightSlot);
        }

        private void DrawSlotSpawnArea(ML3Slot slot)
        {
            if (slot == null || slot.spawnArea == null)
                return;

            Transform area = slot.spawnArea;
            Vector3 center = area.position;
            Vector3 right = area.right * spawnRadius;
            Vector3 forward = area.forward * spawnRadius;

            Vector3 p0 = center - right - forward;
            Vector3 p1 = center + right - forward;
            Vector3 p2 = center + right + forward;
            Vector3 p3 = center - right + forward;

            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);
        }
    }
}
