using System;
using System.Collections;
using System.Collections.Generic;
using LiteracyLevel2;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LiteracyLevel3
{
    public enum LL3InteractionType
    {
        SelectOne,
        MultipleAnswer
    }

    [RequireComponent(typeof(AudioSource))]
    public class LL3GameManager : MonoBehaviour
    {
        private const int TasksPerFinalScorePoint = 2;

        [Serializable]
        public class LiteralStep
        {
            public string questionText;

            [TextArea(2, 4)]
            public string hintText;

            [Header("Audio")]
            [Tooltip("Audio untuk pertanyaan object ini. Diputar saat pertanyaan dimulai.")]
            public AudioClip questionAudioClip;

            public LL3InteractionType interaction = LL3InteractionType.SelectOne;
            public LL2AnswerSlotType slotType = LL2AnswerSlotType.Auto;

            [Tooltip("Isi satu ID untuk SelectOne, atau beberapa ID dipisah koma untuk MultipleAnswer.")]
            public string correctObjectIds;

            [Tooltip("ID object pengecoh, dipisah koma.")]
            public string distractorObjectIds;
        }

        [Serializable]
        public class StoryRound
        {
            [TextArea(3, 8)]
            public string storyText;

            [Tooltip("Kalimat cerita dengan urutan benar. Kartu akan diacak saat spawn.")]
            [TextArea(1, 3)]
            public string[] orderedSentences;

            public LiteralStep[] literalSteps;
        }

        [Header("Story Arrange Layout")]
        public GameObject arrangeStoryLayout;
        public Transform sentenceCardSpawnPoint;
        public Transform sentenceSlotSpawnPoint;
        public Transform storyRuntimeParent;
        public GameObject sentenceCardPrefab;
        public GameObject sentenceSlotPrefab;

        [Header("Ukuran Kartu Cerita")]
        public float sentenceMinWidth = 0.45f;
        public float sentenceMaxWidth = 0.95f;
        public float sentenceCharWidth = 0.012f;
        public float sentenceCardDepth = 0.11f;
        public float sentenceRowSpacing = 0.14f;
        [Tooltip("Jika aktif, SentenceCardSpawnPoint dan SentenceSlotSpawnPoint menjadi posisi baris pertama. Jika nonaktif, spawn point menjadi titik tengah daftar.")]
        public bool sentenceSpawnPointAsFirstRow = true;

        [Header("Object Answer Layout")]
        public GameObject objectAnswerLayout;
        public LL2AnswerLayout answerLayout;
        public Transform runtimeParent;
        public Transform choiceAreaCenter;
        public float spawnHeightOffset = 0.03f;
        public float fallbackChoiceSpacing = 0.22f;
        public float fallbackRowSpacing = 0.18f;
        public Vector3 objectScale = Vector3.one;

        [Header("Object Prefabs")]
        public GameObject marketPrefab;
        public GameObject housePrefab;
        public GameObject shopPrefab;
        public GameObject clothPrefab;
        public GameObject clothesPrefab;
        public GameObject booksPrefab;
        public GameObject ballPrefab;
        public GameObject lollipopPrefab;
        public GameObject donutPrefab;
        public GameObject ricePrefab;
        public GameObject chickenPrefab;

        [Header("Material Warna")]
        public Material redMaterial;
        public Material blueMaterial;
        public Material yellowMaterial;
        public Color redColor = new Color(0.9f, 0.12f, 0.12f, 1f);
        public Color blueColor = new Color(0.12f, 0.32f, 0.9f, 1f);
        public Color yellowColor = new Color(1f, 0.86f, 0.18f, 1f);

        [Header("Selected Effect")]
        public Material selectedMaterial;
        public Color selectedColor = new Color(1f, 0.92f, 0.25f, 1f);
        public float selectedScaleMultiplier = 1.05f;
        [Tooltip("Nonaktifkan untuk menjaga material asli object. Selected hanya memakai scale.")]
        public bool useMaterialSelectionEffect = false;

        [Header("Audio")]
        public AudioSource audioSource;
        [Tooltip("Audio saat tahap susun kalimat/cerita dimulai.")]
        public AudioClip arrangeStoryInstructionClip;
        [Tooltip("Audio saat tahap pilih object dimulai.")]
        public AudioClip objectQuestionInstructionClip;
        [Tooltip("Audio yang diputar saat susun cerita atau jawaban pertanyaan benar.")]
        public AudioClip jawabanBenarClip;
        [Tooltip("Audio yang diputar saat susun cerita atau jawaban pertanyaan salah.")]
        public AudioClip jawabanSalahClip;
        [Tooltip("Audio yang diputar saat level selesai dan pemain lulus.")]
        public AudioClip levelCompleteClip;
        [Tooltip("Audio yang diputar saat level selesai tetapi pemain belum lulus.")]
        public AudioClip levelIncompleteClip;

        [Header("Data Cerita")]
        public bool buildStoriesFromCode = true;
        public StoryRound[] stories;

        [Header("Flow")]
        public bool waitForStoryAudioBeforeCards = false;
        public float storyIntroDelay = 0.75f;
        public float nextStageDelay = 1.25f;
        public float nextStepDelay = 1.25f;
        public float nextStoryDelay = 1.5f;

        [Header("Progress")]
        public string progressSubject = "Literacy";
        public int progressLevelNumber = 3;
        public int minimumCorrectToPass = 4;

        private readonly List<GameObject> spawnedStoryCards = new List<GameObject>();
        private readonly List<GameObject> spawnedStorySlots = new List<GameObject>();
        private readonly List<GameObject> runtimeStepObjects = new List<GameObject>();
        private readonly List<LL2AnswerObject> spawnedAnswers = new List<LL2AnswerObject>();
        private readonly List<LL2AnswerObject> selectedAnswers = new List<LL2AnswerObject>();

        private LL3UIManager uiManager;
        private Camera cam;
        private int currentStoryIndex;
        private int currentStepIndex;
        private int correctTaskAnswers;
        private bool storyOrderActive;
        private bool objectStepActive;
        private bool lastStorySlotsFilled;
        private int audioPlaybackToken;
        private Coroutine flowCoroutine;

        private int MaxStories => stories == null ? 0 : stories.Length;
        private StoryRound CurrentStory => stories[currentStoryIndex];
        private LiteralStep CurrentStep => CurrentStory.literalSteps[currentStepIndex];

        private void Reset()
        {
            FillDefaultStoryTemplates();
        }

        private void OnValidate()
        {
            if (buildStoriesFromCode && ShouldRefreshCodeStories())
                FillDefaultStoryTemplates();
        }

        private void Awake()
        {
            uiManager = FindFirstObjectByType<LL3UIManager>();
            cam = Camera.main;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (MaxStories == 0)
                return;

            if (storyOrderActive)
            {
                UpdateStoryOrderSubmitState();
                return;
            }

            if (!objectStepActive)
                return;

            if (WasPointerPressedThisFrame() && TryGetPointerPosition(out Vector2 pointerPosition))
                TryToggleAnswerObject(pointerPosition);
        }

        [ContextMenu("Isi Template Literasi Level 3")]
        public void FillDefaultStoryTemplates()
        {
            AudioClip[][] existingQuestionAudioClips = GetExistingQuestionAudioClips();

            stories = new[]
            {
                new StoryRound
                {
                    storyText = "Ibu pergi ke pasar kain.\nIbu melihat kain merah dan kain biru.\nIbu membeli kain merah.\nKain itu akan dibuat menjadi baju.",
                    orderedSentences = new[]
                    {
                        "Ibu pergi ke pasar kain.",
                        "Ibu melihat kain merah dan kain biru.",
                        "Ibu membeli kain merah.",
                        "Kain itu akan dibuat menjadi baju."
                    },
                    literalSteps = new[]
                    {
                        new LiteralStep
                        {
                            questionText = "Ibu pergi ke mana?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Large,
                            correctObjectIds = "market",
                            distractorObjectIds = "house"
                        },
                        new LiteralStep
                        {
                            questionText = "Ibu melihat kain warna apa saja?",
                            hintText = "Pilih semua jawaban yang benar, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.MultipleAnswer,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "cloth_red,cloth_blue",
                            distractorObjectIds = "cloth_yellow"
                        },
                        new LiteralStep
                        {
                            questionText = "Ibu membeli kain warna apa?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "cloth_red",
                            distractorObjectIds = "cloth_blue"
                        },
                        new LiteralStep
                        {
                            questionText = "Kain itu akan dibuat menjadi apa?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "clothes_red",
                            distractorObjectIds = "books,ball"
                        }
                    }
                },
                new StoryRound
                {
                    storyText = "Budi berada di rumah.\nIa melihat buku di meja.\nBudi membaca buku lalu bermain bola.\nSetelah bermain, Budi makan donat.",
                    orderedSentences = new[]
                    {
                        "Budi berada di rumah.",
                        "Ia melihat buku di meja.",
                        "Budi membaca buku lalu bermain bola.",
                        "Setelah bermain, Budi makan donat."
                    },
                    literalSteps = new[]
                    {
                        new LiteralStep
                        {
                            questionText = "Budi berada di mana?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Large,
                            correctObjectIds = "house",
                            distractorObjectIds = "shop"
                        },
                        new LiteralStep
                        {
                            questionText = "Budi melihat apa di meja?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "books",
                            distractorObjectIds = "ball,lollipop"
                        },
                        new LiteralStep
                        {
                            questionText = "Setelah membaca, Budi bermain apa?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "ball",
                            distractorObjectIds = "books,lollipop"
                        },
                        new LiteralStep
                        {
                            questionText = "Setelah bermain, Budi makan apa?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL3InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Small,
                            correctObjectIds = "donut",
                            distractorObjectIds = "chicken,lollipop"
                        }
                    }
                }
            };

            RestoreQuestionAudioClips(existingQuestionAudioClips);
        }

        public void ConfigureSpawnedLayout(GameObject spawnedRoot)
        {
            if (spawnedRoot == null)
                return;

            Transform root = spawnedRoot.transform;
            Transform previousCardSpawnPoint = IsInsideRoot(sentenceCardSpawnPoint, root) ? sentenceCardSpawnPoint : null;
            Transform previousSlotSpawnPoint = IsInsideRoot(sentenceSlotSpawnPoint, root) ? sentenceSlotSpawnPoint : null;
            LL2AnswerLayout previousAnswerLayout = answerLayout != null && IsInsideRoot(answerLayout.transform, root) ? answerLayout : null;
            Transform previousChoiceArea = IsInsideRoot(choiceAreaCenter, root) ? choiceAreaCenter : null;

            arrangeStoryLayout = IsInsideRoot(arrangeStoryLayout != null ? arrangeStoryLayout.transform : null, root) ? arrangeStoryLayout : null;
            objectAnswerLayout = IsInsideRoot(objectAnswerLayout != null ? objectAnswerLayout.transform : null, root) ? objectAnswerLayout : null;
            sentenceCardSpawnPoint = previousCardSpawnPoint;
            sentenceSlotSpawnPoint = previousSlotSpawnPoint;
            storyRuntimeParent = null;
            answerLayout = previousAnswerLayout;
            choiceAreaCenter = previousChoiceArea;
            runtimeParent = spawnedRoot.transform;

            Transform arrange = FindDeepChild(root, "ArrangeStoryLayout");
            if (arrange != null)
                arrangeStoryLayout = arrange.gameObject;

            Transform objectLayout = FindDeepChild(root, "ObjectAnswerLayout");
            if (objectLayout != null)
                objectAnswerLayout = objectLayout.gameObject;

            Transform spawnedCardSpawnPoint = FindDeepChild(root, "SentenceCardSpawnPoint");
            if (spawnedCardSpawnPoint != null)
                sentenceCardSpawnPoint = spawnedCardSpawnPoint;

            Transform spawnedSlotSpawnPoint = FindDeepChild(root, "SentenceSlotSpawnPoint");
            if (spawnedSlotSpawnPoint != null)
                sentenceSlotSpawnPoint = spawnedSlotSpawnPoint;

            Transform arrangeParent = arrange != null ? arrange : root;
            if (sentenceCardSpawnPoint == null)
                Debug.LogWarning("[LL3GameManager] SentenceCardSpawnPoint tidak ditemukan di LiteracyL3Root. Buat child dengan nama persis 'SentenceCardSpawnPoint' di ArrangeStoryLayout.");

            if (sentenceSlotSpawnPoint == null)
                Debug.LogWarning("[LL3GameManager] SentenceSlotSpawnPoint tidak ditemukan di LiteracyL3Root. Buat child dengan nama persis 'SentenceSlotSpawnPoint' di ArrangeStoryLayout.");

            storyRuntimeParent = arrangeParent;

            LL2AnswerLayout spawnedAnswerLayout = spawnedRoot.GetComponentInChildren<LL2AnswerLayout>(true);
            if (spawnedAnswerLayout != null)
                answerLayout = spawnedAnswerLayout;

            Transform answerParent = objectLayout != null ? objectLayout : root;
            if (answerLayout == null)
            {
                answerLayout = answerParent.gameObject.AddComponent<LL2AnswerLayout>();
                Debug.LogWarning("[LL3GameManager] LL2AnswerLayout tidak ditemukan, komponen fallback ditambahkan.");
            }

            if (objectAnswerLayout == null && answerLayout != null && answerLayout.transform != root)
                objectAnswerLayout = answerLayout.gameObject;

            answerLayout.AutoAssignChildren();
            choiceAreaCenter = FindOrCreateChild(root, "ChoiceAreaCenter", new Vector3(0f, 0.06f, -0.08f));

            Debug.Log($"[LL3GameManager] Layout tersambung. Arrange={(arrangeStoryLayout != null ? arrangeStoryLayout.name : "null")}, Object={(objectAnswerLayout != null ? objectAnswerLayout.name : "null")}, Small={answerLayout.smallSlots?.Length ?? 0}, Medium={answerLayout.mediumSlots?.Length ?? 0}, Large={answerLayout.largeSlots?.Length ?? 0}");
        }

        public void StartGame()
        {
            if (buildStoriesFromCode)
                FillDefaultStoryTemplates();

            if (uiManager == null)
                uiManager = FindFirstObjectByType<LL3UIManager>();

            if (answerLayout == null)
                answerLayout = FindFirstObjectByType<LL2AnswerLayout>();

            currentStoryIndex = 0;
            currentStepIndex = 0;
            correctTaskAnswers = 0;
            storyOrderActive = false;
            objectStepActive = false;
            lastStorySlotsFilled = false;
            audioPlaybackToken++;

            if (uiManager != null)
                uiManager.ResetUI();

            Debug.Log($"[LL3GameManager] StartGame. Stories={MaxStories}");
            StartCurrentStory();
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            ClearRuntimeObjects();
            StartGame();
        }

        public void ReplayCurrentAudio()
        {
            if (MaxStories == 0 || currentStoryIndex >= MaxStories)
                return;

            if (objectStepActive)
            {
                PlayAudio(CurrentStep.questionAudioClip != null ? CurrentStep.questionAudioClip : objectQuestionInstructionClip);
                return;
            }

            PlayAudio(arrangeStoryInstructionClip);
        }

        public void SubmitCurrentStage()
        {
            if (storyOrderActive)
            {
                SubmitStoryOrder();
                return;
            }

            if (objectStepActive)
                SubmitAnswer();
        }

        private void StartCurrentStory()
        {
            if (MaxStories == 0)
            {
                Debug.LogError("[LL3GameManager] Data stories masih kosong.");
                return;
            }

            if (currentStoryIndex >= MaxStories)
            {
                ShowFinalResult();
                return;
            }

            currentStepIndex = 0;

            if (flowCoroutine != null)
                StopCoroutine(flowCoroutine);

            flowCoroutine = StartCoroutine(StoryIntroFlow());
        }

        private IEnumerator StoryIntroFlow()
        {
            ClearRuntimeObjects();
            SetLayoutMode(true, false);

            StoryRound story = CurrentStory;

            if (uiManager != null)
                uiManager.ShowStoryOrder(story, currentStoryIndex + 1, MaxStories, false);

            PlayAudio(arrangeStoryInstructionClip);

            float audioWait = arrangeStoryInstructionClip != null ? arrangeStoryInstructionClip.length : 0f;
            float waitDuration = waitForStoryAudioBeforeCards ? Mathf.Max(storyIntroDelay, audioWait) : storyIntroDelay;
            if (waitDuration > 0f)
                yield return new WaitForSeconds(waitDuration);

            SpawnStoryOrder(story);
            storyOrderActive = true;
            lastStorySlotsFilled = false;

            if (uiManager != null)
            {
                uiManager.ShowStoryOrder(story, currentStoryIndex + 1, MaxStories, true);
                uiManager.SetCheckButtonInteractable(false);
            }
        }

        private void SubmitStoryOrder()
        {
            if (!storyOrderActive)
                return;

            if (!AreAllStorySlotsFilled())
            {
                PlayAudio(jawabanSalahClip);

                if (uiManager != null)
                    uiManager.ShowFeedback(false, "Susun semua kalimat dulu.");
                return;
            }

            bool isCorrect = IsStoryOrderCorrect();
            if (!isCorrect)
            {
                lastStorySlotsFilled = AreAllStorySlotsFilled();
                PlayAudio(jawabanSalahClip);

                if (uiManager != null)
                {
                    uiManager.ShowFeedback(false, "Salah");
                    uiManager.SetSubmitButtonText("Cek Jawaban");
                    uiManager.SetCheckButtonInteractable(lastStorySlotsFilled);
                }

                return;
            }

            correctTaskAnswers++;
            PlayAudio(jawabanBenarClip);

            storyOrderActive = false;
            SetStoryCardsInteractable(false);

            if (uiManager != null)
            {
                uiManager.ShowFeedback(true, "Benar!");
                uiManager.SetSubmitButtonText("Cek Jawaban");
                uiManager.SetCheckButtonInteractable(false);
            }

            StartCoroutine(AdvanceToObjectQuestionsAfterDelay());
        }

        private void UpdateStoryOrderSubmitState()
        {
            bool allSlotsFilled = AreAllStorySlotsFilled();
            if (allSlotsFilled == lastStorySlotsFilled)
                return;

            lastStorySlotsFilled = allSlotsFilled;

            if (uiManager != null)
            {
                uiManager.SetCheckButtonInteractable(allSlotsFilled);

                if (allSlotsFilled)
                    uiManager.ClearFeedback();
            }
        }

        private IEnumerator AdvanceToObjectQuestionsAfterDelay()
        {
            yield return new WaitForSeconds(nextStageDelay);
            ShowCurrentLiteralStep();
        }

        private void ShowCurrentLiteralStep()
        {
            ClearStepObjects();
            SetLayoutMode(true, true);

            if (CurrentStory.literalSteps == null || CurrentStory.literalSteps.Length == 0)
            {
                currentStoryIndex++;
                StartCurrentStory();
                return;
            }

            SpawnStepObjects(CurrentStep);
            objectStepActive = true;

            if (uiManager != null)
            {
                uiManager.ShowLiteralQuestion(
                    CurrentStep,
                    currentStoryIndex + 1,
                    MaxStories,
                    currentStepIndex + 1,
                    CurrentStory.literalSteps.Length
                );
                uiManager.SetCheckButtonInteractable(false);
            }

            PlayCurrentLiteralStepAudio();
        }

        private void SubmitAnswer()
        {
            if (!objectStepActive || MaxStories == 0)
                return;

            audioPlaybackToken++;

            if (selectedAnswers.Count == 0)
            {
                PlayAudio(jawabanSalahClip);

                if (uiManager != null)
                    uiManager.ShowFeedback(false, "Pilih jawaban dulu.");
                return;
            }

            HashSet<string> correctIds = new HashSet<string>(SplitIds(CurrentStep.correctObjectIds), StringComparer.OrdinalIgnoreCase);
            HashSet<string> selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (LL2AnswerObject selectedAnswer in selectedAnswers)
            {
                if (selectedAnswer != null && !string.IsNullOrWhiteSpace(selectedAnswer.objectId))
                    selectedIds.Add(selectedAnswer.objectId.Trim());
            }

            bool isCorrect = correctIds.SetEquals(selectedIds);
            if (isCorrect)
                correctTaskAnswers++;

            PlayAudio(isCorrect ? jawabanBenarClip : jawabanSalahClip);

            objectStepActive = false;

            if (uiManager != null)
            {
                uiManager.ShowFeedback(isCorrect, isCorrect ? "Benar!" : "Salah");
                uiManager.SetCheckButtonInteractable(false);
            }

            StartCoroutine(AdvanceAfterLiteralDelay());
        }

        private void PlayCurrentLiteralStepAudio()
        {
            int token = ++audioPlaybackToken;
            AudioClip questionClip = CurrentStep.questionAudioClip;

            if (currentStepIndex == 0 && objectQuestionInstructionClip != null)
            {
                PlayAudio(objectQuestionInstructionClip);
                if (questionClip != null)
                    StartCoroutine(PlayAudioAfterDelay(questionClip, objectQuestionInstructionClip.length + 0.2f, token));

                return;
            }

            PlayAudio(questionClip);
        }

        private IEnumerator PlayAudioAfterDelay(AudioClip clip, float delay, int token)
        {
            yield return new WaitForSeconds(delay);

            if (objectStepActive && token == audioPlaybackToken)
                PlayAudio(clip);
        }

        private IEnumerator AdvanceAfterLiteralDelay()
        {
            bool isLastStep = currentStepIndex >= CurrentStory.literalSteps.Length - 1;
            yield return new WaitForSeconds(isLastStep ? nextStoryDelay : nextStepDelay);

            if (isLastStep)
            {
                currentStoryIndex++;
                StartCurrentStory();
            }
            else
            {
                currentStepIndex++;
                ShowCurrentLiteralStep();
            }
        }

        private void SpawnStoryOrder(StoryRound story)
        {
            ClearStoryObjects();

            if (sentenceCardPrefab == null || sentenceSlotPrefab == null)
            {
                Debug.LogWarning("[LL3GameManager] Sentence card/slot prefab belum di-assign.");
                return;
            }

            if (sentenceCardSpawnPoint == null || sentenceSlotSpawnPoint == null)
            {
                Debug.LogWarning("[LL3GameManager] SentenceCardSpawnPoint atau SentenceSlotSpawnPoint belum di-assign.");
                return;
            }

            if (story.orderedSentences == null || story.orderedSentences.Length == 0)
                return;

            DraggableCard.TableSurfaceY = sentenceCardSpawnPoint.position.y;

            float cardWidth = CalcSentenceWidth(story.orderedSentences);
            SpawnSentenceSlots(story.orderedSentences.Length, cardWidth);
            SpawnSentenceCards(story.orderedSentences, cardWidth);
        }

        private float CalcSentenceWidth(string[] sentences)
        {
            int longest = 0;
            foreach (string sentence in sentences)
            {
                if (!string.IsNullOrWhiteSpace(sentence))
                    longest = Mathf.Max(longest, sentence.Length);
            }

            return Mathf.Clamp(sentenceMinWidth + longest * sentenceCharWidth, sentenceMinWidth, sentenceMaxWidth);
        }

        private void SpawnSentenceCards(string[] sentences, float cardWidth)
        {
            string[] shuffled = (string[])sentences.Clone();
            Shuffle(shuffled);

            Quaternion rotation = sentenceCardSpawnPoint.rotation;
            Transform parent = storyRuntimeParent != null ? storyRuntimeParent : arrangeStoryLayout != null ? arrangeStoryLayout.transform : transform;

            for (int i = 0; i < shuffled.Length; i++)
            {
                Vector3 position = GetSentenceRowPosition(sentenceCardSpawnPoint, i, shuffled.Length);
                position.y = DraggableCard.TableSurfaceY + 0.005f;

                GameObject card = Instantiate(sentenceCardPrefab, position, rotation, parent);
                card.name = $"LL3_StoryCard_{i + 1}";
                card.SetActive(true);

                DraggableCard draggableCard = card.GetComponent<DraggableCard>();
                if (draggableCard == null)
                    draggableCard = card.AddComponent<DraggableCard>();

                draggableCard.ResizeToWidth(cardWidth, sentenceCardDepth);
                draggableCard.Initialize(shuffled[i], position);

                spawnedStoryCards.Add(card);
            }
        }

        private void SpawnSentenceSlots(int count, float slotWidth)
        {
            Quaternion rotation = sentenceSlotSpawnPoint.rotation;
            Transform parent = storyRuntimeParent != null ? storyRuntimeParent : arrangeStoryLayout != null ? arrangeStoryLayout.transform : transform;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetSentenceRowPosition(sentenceSlotSpawnPoint, i, count);
                position.y = DraggableCard.TableSurfaceY + 0.002f;

                GameObject slot = Instantiate(sentenceSlotPrefab, position, rotation, parent);
                slot.name = $"LL3_StorySlot_{i + 1}";
                slot.SetActive(true);

                Vector3 scale = slot.transform.localScale;
                slot.transform.localScale = new Vector3(slotWidth, scale.y, sentenceCardDepth);

                CardSlot cardSlot = slot.GetComponent<CardSlot>();
                if (cardSlot == null)
                    cardSlot = slot.AddComponent<CardSlot>();

                cardSlot.slotIndex = i;
                spawnedStorySlots.Add(slot);
            }
        }

        private Vector3 GetSentenceRowPosition(Transform spawnPoint, int index, int count)
        {
            Vector3 rowDirection = -spawnPoint.forward;
            Vector3 startPosition = sentenceSpawnPointAsFirstRow
                ? spawnPoint.position
                : spawnPoint.position - rowDirection * ((count - 1) * sentenceRowSpacing / 2f);

            return startPosition + rowDirection * (index * sentenceRowSpacing);
        }

        private bool AreAllStorySlotsFilled()
        {
            foreach (GameObject slotObject in spawnedStorySlots)
            {
                if (slotObject == null)
                    return false;

                CardSlot cardSlot = slotObject.GetComponent<CardSlot>();
                if (cardSlot == null || cardSlot.IsEmpty)
                    return false;
            }

            return spawnedStorySlots.Count > 0;
        }

        private bool IsStoryOrderCorrect()
        {
            if (CurrentStory.orderedSentences == null || spawnedStorySlots.Count != CurrentStory.orderedSentences.Length)
                return false;

            spawnedStorySlots.Sort((a, b) =>
            {
                CardSlot slotA = a != null ? a.GetComponent<CardSlot>() : null;
                CardSlot slotB = b != null ? b.GetComponent<CardSlot>() : null;
                int indexA = slotA != null ? slotA.slotIndex : 0;
                int indexB = slotB != null ? slotB.slotIndex : 0;
                return indexA.CompareTo(indexB);
            });

            for (int i = 0; i < spawnedStorySlots.Count; i++)
            {
                CardSlot cardSlot = spawnedStorySlots[i].GetComponent<CardSlot>();
                string playerSentence = NormalizeSentence(cardSlot.GetContent());
                string correctSentence = NormalizeSentence(CurrentStory.orderedSentences[i]);

                if (!string.Equals(playerSentence, correctSentence, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void TryToggleAnswerObject(Vector2 screenPosition)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, Physics.AllLayers, QueryTriggerInteraction.Collide))
                return;

            LL2AnswerObject answerObject = hit.collider.GetComponentInParent<LL2AnswerObject>();
            if (answerObject == null || !spawnedAnswers.Contains(answerObject))
                return;

            if (CurrentStep.interaction == LL3InteractionType.SelectOne)
            {
                foreach (LL2AnswerObject answer in spawnedAnswers)
                    SetAnswerSelected(answer, answer == answerObject);
            }
            else
            {
                SetAnswerSelected(answerObject, !answerObject.IsSelected);
            }

            if (uiManager != null)
            {
                uiManager.ClearFeedback();
                uiManager.SetSelectedObjectText(GetSelectedAnswerLabels());
                uiManager.SetCheckButtonInteractable(selectedAnswers.Count > 0);
            }
        }

        private void SetAnswerSelected(LL2AnswerObject answerObject, bool selected)
        {
            if (answerObject == null)
                return;

            answerObject.SetSelected(selected);

            if (selected)
            {
                if (!selectedAnswers.Contains(answerObject))
                    selectedAnswers.Add(answerObject);
            }
            else
            {
                selectedAnswers.Remove(answerObject);
            }
        }

        private string GetSelectedAnswerLabels()
        {
            if (selectedAnswers.Count == 0)
                return "";

            List<string> labels = new List<string>();
            foreach (LL2AnswerObject answer in selectedAnswers)
            {
                if (answer != null)
                    labels.Add(answer.Label);
            }

            return string.Join(", ", labels);
        }

        private void SpawnStepObjects(LiteralStep step)
        {
            List<string> objectIds = new List<string>();
            objectIds.AddRange(SplitIds(step.correctObjectIds));
            objectIds.AddRange(SplitIds(step.distractorObjectIds));
            Shuffle(objectIds);

            LL2AnswerSlotType slotType = step.slotType == LL2AnswerSlotType.Auto ? InferSlotType(objectIds) : step.slotType;
            if (answerLayout != null)
                answerLayout.ShowSlotGroup(slotType);

            Transform[] slots = answerLayout != null ? answerLayout.GetSlots(slotType) : null;
            if (slots == null || slots.Length < objectIds.Count)
            {
                int slotCount = slots == null ? 0 : slots.Length;
                Debug.LogWarning($"[LL3GameManager] Slot {slotType} hanya {slotCount}, pilihan ada {objectIds.Count}. Sisa object pakai fallback position.");
            }

            for (int i = 0; i < objectIds.Count; i++)
            {
                string objectId = objectIds[i];
                GameObject prefab = GetPrefabForId(objectId);
                if (prefab == null)
                {
                    Debug.LogWarning($"[LL3GameManager] Prefab untuk objectId '{objectId}' belum di-assign.");
                    continue;
                }

                Transform slot = slots != null && i < slots.Length ? slots[i] : null;
                Vector3 position = slot != null ? slot.position : GetFallbackChoicePosition(i, objectIds.Count);
                Quaternion rotation = slot != null ? slot.rotation : GetFallbackRotation();
                Transform parent = slot != null ? slot : runtimeParent;

                GameObject instance = Instantiate(prefab, position, rotation, parent);
                instance.name = $"LL3_{objectId}";
                instance.SetActive(true);

                if (objectScale != Vector3.one)
                    instance.transform.localScale = Vector3.Scale(instance.transform.localScale, objectScale);

                ApplyObjectMaterial(instance, objectId);
                EnsureCollider(instance);
                EnsureStableRigidbody(instance);

                LL2AnswerObject answerObject = instance.GetComponent<LL2AnswerObject>();
                if (answerObject == null)
                    answerObject = instance.AddComponent<LL2AnswerObject>();

                answerObject.InitializeRuntime(
                    objectId,
                    GetLabelForId(objectId),
                    selectedMaterial,
                    selectedColor,
                    selectedScaleMultiplier,
                    useMaterialSelectionEffect
                );

                runtimeStepObjects.Add(instance);
                spawnedAnswers.Add(answerObject);
            }

            Debug.Log($"[LL3GameManager] Total answer spawned: {spawnedAnswers.Count}/{objectIds.Count}");
        }

        private Vector3 GetFallbackChoicePosition(int index, int total)
        {
            Transform area = choiceAreaCenter != null
                ? choiceAreaCenter
                : answerLayout != null ? answerLayout.transform : transform;

            int columns = total <= 3 ? total : Mathf.CeilToInt(total / 2f);
            int row = total <= 3 ? 0 : index / columns;
            int col = total <= 3 ? index : index % columns;
            int rowCount = total <= 3 ? 1 : Mathf.CeilToInt(total / (float)columns);

            float xOffset = (col - (columns - 1) * 0.5f) * fallbackChoiceSpacing;
            float zOffset = ((rowCount - 1) * 0.5f - row) * fallbackRowSpacing;

            return area.position
                + area.right * xOffset
                + area.forward * zOffset
                + area.up * spawnHeightOffset;
        }

        private Quaternion GetFallbackRotation()
        {
            if (choiceAreaCenter != null)
                return choiceAreaCenter.rotation;

            if (answerLayout != null)
                return answerLayout.transform.rotation;

            return transform.rotation;
        }

        private void ApplyObjectMaterial(GameObject instance, string objectId)
        {
            Material material = null;
            Color fallbackColor = Color.white;
            bool hasColor = true;

            if (objectId.EndsWith("_red", StringComparison.OrdinalIgnoreCase))
            {
                material = redMaterial;
                fallbackColor = redColor;
            }
            else if (objectId.EndsWith("_blue", StringComparison.OrdinalIgnoreCase))
            {
                material = blueMaterial;
                fallbackColor = blueColor;
            }
            else if (objectId.EndsWith("_yellow", StringComparison.OrdinalIgnoreCase))
            {
                material = yellowMaterial;
                fallbackColor = yellowColor;
            }
            else
            {
                hasColor = false;
            }

            if (!hasColor)
                return;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                    continue;

                Material[] materials = targetRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (material != null)
                    {
                        materials[i] = material;
                    }
                    else if (materials[i] != null)
                    {
                        if (materials[i].HasProperty("_BaseColor"))
                            materials[i].SetColor("_BaseColor", fallbackColor);

                        if (materials[i].HasProperty("_Color"))
                            materials[i].SetColor("_Color", fallbackColor);
                    }
                }

                targetRenderer.materials = materials;
            }
        }

        private void EnsureStableRigidbody(GameObject instance)
        {
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb == null)
                rb = instance.AddComponent<Rigidbody>();

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        private void EnsureCollider(GameObject instance)
        {
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null)
                return;

            BoxCollider box = instance.AddComponent<BoxCollider>();
            Bounds bounds = CalculateRendererBounds(instance);

            if (bounds.size == Vector3.zero)
            {
                box.size = Vector3.one * 0.12f;
                return;
            }

            box.center = instance.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = instance.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private Bounds CalculateRendererBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(instance.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private GameObject GetPrefabForId(string objectId)
        {
            switch (objectId)
            {
                case "market": return marketPrefab;
                case "house": return housePrefab;
                case "shop": return shopPrefab;
                case "cloth_red":
                case "cloth_blue":
                case "cloth_yellow":
                    return clothPrefab;
                case "clothes_red":
                case "clothes_blue":
                case "clothes_yellow":
                    return clothesPrefab;
                case "books": return booksPrefab;
                case "ball": return ballPrefab;
                case "lollipop": return lollipopPrefab;
                case "donut": return donutPrefab;
                case "rice": return ricePrefab;
                case "chicken": return chickenPrefab;
                default: return null;
            }
        }

        private string GetLabelForId(string objectId)
        {
            switch (objectId)
            {
                case "market": return "Pasar";
                case "house": return "Rumah";
                case "shop": return "Toko";
                case "cloth_red": return "Kain Merah";
                case "cloth_blue": return "Kain Biru";
                case "cloth_yellow": return "Kain Kuning";
                case "clothes_red": return "Baju";
                case "clothes_blue": return "Baju Biru";
                case "clothes_yellow": return "Baju Kuning";
                case "books": return "Buku";
                case "ball": return "Bola";
                case "lollipop": return "Permen";
                case "donut": return "Donat";
                case "rice": return "Nasi";
                case "chicken": return "Ayam";
                default: return objectId;
            }
        }

        private LL2AnswerSlotType InferSlotType(List<string> objectIds)
        {
            foreach (string objectId in objectIds)
            {
                if (IsLargeObject(objectId))
                    return LL2AnswerSlotType.Large;
            }

            foreach (string objectId in objectIds)
            {
                if (IsMediumObject(objectId))
                    return LL2AnswerSlotType.Medium;
            }

            return LL2AnswerSlotType.Small;
        }

        private bool IsLargeObject(string objectId)
        {
            return objectId == "house" || objectId == "shop" || objectId == "market";
        }

        private bool IsMediumObject(string objectId)
        {
            return objectId == "cloth_red"
                || objectId == "cloth_blue"
                || objectId == "cloth_yellow"
                || objectId == "clothes_red"
                || objectId == "clothes_blue"
                || objectId == "clothes_yellow"
                || objectId == "books"
                || objectId == "ball";
        }

        private bool ShouldRefreshCodeStories()
        {
            if (stories == null || stories.Length != 2)
                return true;

            foreach (StoryRound story in stories)
            {
                if (story == null || story.orderedSentences == null || story.orderedSentences.Length != 4)
                    return true;

                if (story.literalSteps == null || story.literalSteps.Length != 4)
                    return true;
            }

            return false;
        }

        private void SetLayoutMode(bool showArrange, bool showObjectAnswer)
        {
            if (arrangeStoryLayout != null)
                arrangeStoryLayout.SetActive(showArrange);

            if (objectAnswerLayout != null && objectAnswerLayout.transform != runtimeParent)
                objectAnswerLayout.SetActive(showObjectAnswer);

            if (showObjectAnswer && answerLayout != null)
            {
                ActivateParentsUntilRoot(answerLayout.transform);
                answerLayout.gameObject.SetActive(true);
            }

            if (!showObjectAnswer && answerLayout != null)
                answerLayout.HideAllSlotGroups();
        }

        private void PlayAudio(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }

        private AudioClip[][] GetExistingQuestionAudioClips()
        {
            if (stories == null)
                return null;

            AudioClip[][] clips = new AudioClip[stories.Length][];
            for (int storyIndex = 0; storyIndex < stories.Length; storyIndex++)
            {
                LiteralStep[] steps = stories[storyIndex]?.literalSteps;
                if (steps == null)
                    continue;

                clips[storyIndex] = new AudioClip[steps.Length];
                for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
                    clips[storyIndex][stepIndex] = steps[stepIndex]?.questionAudioClip;
            }

            return clips;
        }

        private void RestoreQuestionAudioClips(AudioClip[][] clips)
        {
            if (clips == null || stories == null)
                return;

            int storyCount = Mathf.Min(stories.Length, clips.Length);
            for (int storyIndex = 0; storyIndex < storyCount; storyIndex++)
            {
                LiteralStep[] steps = stories[storyIndex]?.literalSteps;
                AudioClip[] stepClips = clips[storyIndex];
                if (steps == null || stepClips == null)
                    continue;

                int stepCount = Mathf.Min(steps.Length, stepClips.Length);
                for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
                {
                    if (steps[stepIndex] != null && stepClips[stepIndex] != null)
                        steps[stepIndex].questionAudioClip = stepClips[stepIndex];
                }
            }
        }

        private void ShowFinalResult()
        {
            ClearRuntimeObjects();
            SetLayoutMode(false, false);

            int totalTasks = CountTotalTasks();
            int maxScore = Mathf.Max(1, totalTasks / TasksPerFinalScorePoint);
            int finalScore = Mathf.Clamp(correctTaskAnswers / TasksPerFinalScorePoint, 0, maxScore);
            bool isComplete = finalScore >= minimumCorrectToPass;

            LevelProgress.SaveResult(progressSubject, progressLevelNumber, finalScore, minimumCorrectToPass);
            PlayAudio(isComplete ? levelCompleteClip : levelIncompleteClip);

            if (uiManager != null)
                uiManager.ShowFinalResult(finalScore, maxScore, minimumCorrectToPass);

            Debug.Log($"[LL3GameManager] Game selesai. Task benar: {correctTaskAnswers}/{totalTasks}, skor akhir: {finalScore}/{maxScore}");
        }

        private int CountTotalTasks()
        {
            int total = 0;
            if (stories == null)
                return total;

            foreach (StoryRound story in stories)
            {
                if (story == null)
                    continue;

                total++;

                if (story.literalSteps != null)
                    total += story.literalSteps.Length;
            }

            return total;
        }

        private void ClearRuntimeObjects()
        {
            ClearStoryObjects();
            ClearStepObjects();
        }

        private void ClearStoryObjects()
        {
            foreach (GameObject card in spawnedStoryCards)
            {
                if (card != null)
                {
                    DraggableCard draggableCard = card.GetComponent<DraggableCard>();
                    if (draggableCard != null)
                        draggableCard.CancelDrag();

                    Destroy(card);
                }
            }

            foreach (GameObject slot in spawnedStorySlots)
            {
                if (slot != null)
                    Destroy(slot);
            }

            spawnedStoryCards.Clear();
            spawnedStorySlots.Clear();
            storyOrderActive = false;
        }

        private void SetStoryCardsInteractable(bool isInteractable)
        {
            foreach (GameObject card in spawnedStoryCards)
            {
                if (card == null)
                    continue;

                DraggableCard draggableCard = card.GetComponent<DraggableCard>();
                if (draggableCard != null)
                    draggableCard.enabled = isInteractable;
            }
        }

        private void ClearStepObjects()
        {
            foreach (LL2AnswerObject answer in spawnedAnswers)
            {
                if (answer != null)
                    answer.SetSelected(false);
            }

            foreach (GameObject runtimeObject in runtimeStepObjects)
            {
                if (runtimeObject != null)
                    Destroy(runtimeObject);
            }

            runtimeStepObjects.Clear();
            spawnedAnswers.Clear();
            selectedAnswers.Clear();
            objectStepActive = false;

            if (answerLayout != null)
                answerLayout.HideAllSlotGroups();
        }

        private string NormalizeSentence(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();
        }

        private List<string> SplitIds(string value)
        {
            List<string> ids = new List<string>();
            if (string.IsNullOrWhiteSpace(value))
                return ids;

            string[] parts = value.Split(new[] { ',', '|', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string id = part.Trim();
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
            }

            return ids;
        }

        private void Shuffle(List<string> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        private void Shuffle(string[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
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

        private Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private Transform FindOrCreateChild(Transform parent, string childName, Vector3 localPosition)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                return child;

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            childObject.transform.localPosition = localPosition;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;
            return childObject.transform;
        }

        private bool IsInsideRoot(Transform candidate, Transform root)
        {
            if (candidate == null || root == null)
                return false;

            return candidate == root || candidate.IsChildOf(root);
        }

        private void ActivateParentsUntilRoot(Transform child)
        {
            if (child == null)
                return;

            Transform root = runtimeParent != null ? runtimeParent : transform;
            Transform current = child;

            while (current != null)
            {
                current.gameObject.SetActive(true);

                if (current == root)
                    break;

                current = current.parent;
            }
        }
    }
}
