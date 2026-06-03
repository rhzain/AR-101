using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LiteracyLevel2
{
    public enum LL2InteractionType
    {
        SelectOne,
        MultipleAnswer
    }

    public enum LL2AnswerSlotType
    {
        Auto,
        Small,
        Medium,
        Large
    }

    [RequireComponent(typeof(AudioSource))]
    public class LL2GameManager : MonoBehaviour
    {
        [Serializable]
        public class QuestionStep
        {
            public string questionText;

            [TextArea(2, 4)]
            public string hintText;

            public LL2InteractionType interaction = LL2InteractionType.SelectOne;
            public LL2AnswerSlotType slotType = LL2AnswerSlotType.Auto;

            [Tooltip("Isi satu ID untuk SelectOne, atau beberapa ID dipisah koma untuk MultipleAnswer.")]
            public string correctObjectIds;

            [Tooltip("ID object pengecoh, dipisah koma.")]
            public string distractorObjectIds;
        }

        [Serializable]
        public class StoryQuestion
        {
            [TextArea(2, 4)]
            public string sentenceText;

            public string connectorWord;
            public AudioClip audioClip;
            public QuestionStep[] steps;
        }

        [Header("Object Prefabs")]
        public GameObject boyPrefab;
        public GameObject girlPrefab;
        public GameObject mathPrefab;
        public GameObject booksPrefab;
        public GameObject housePrefab;
        public GameObject shopPrefab;
        public GameObject ricePrefab;
        public GameObject donutPrefab;
        public GameObject ballPrefab;
        public GameObject lollipopPrefab;
        public GameObject motherPrefab;
        public GameObject fatherPrefab;
        public GameObject marketPrefab;
        public GameObject chickenPrefab;
        public GameObject cabbagePrefab;
        public GameObject fishPrefab;

        [Header("Answer Layout")]
        public LL2AnswerLayout answerLayout;
        public Transform runtimeParent;

        [Tooltip("Fallback kalau prefab ARQuestionLayout belum punya AnswerSlots.")]
        public Transform choiceAreaCenter;
        public float spawnHeightOffset = 0.03f;
        public float fallbackChoiceSpacing = 0.22f;
        public float fallbackRowSpacing = 0.18f;
        public Vector3 objectScale = Vector3.one;

        [Header("Selected Effect")]
        public Material selectedMaterial;
        public Color selectedColor = new Color(1f, 0.92f, 0.25f, 1f);
        public float selectedScaleMultiplier = 1.05f;
        [Tooltip("Nonaktifkan untuk menjaga material asli object. Selected hanya memakai scale.")]
        public bool useMaterialSelectionEffect = false;

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
        public bool buildQuestionsFromCode = true;
        public StoryQuestion[] questions;

        [Header("Flow")]
        public float sentenceReadDelay = 1.25f;
        public float connectorHighlightDuration = 1.25f;
        public float nextStepDelay = 1.25f;
        public float nextQuestionDelay = 1.5f;
        public string connectorHighlightColor = "#FFD84D";

        [Header("Progress")]
        public string progressSubject = "Literacy";
        public int progressLevelNumber = 2;
        public int minimumCorrectToPass = 4;

        private readonly List<GameObject> runtimeStepObjects = new List<GameObject>();
        private readonly List<LL2AnswerObject> spawnedAnswers = new List<LL2AnswerObject>();
        private readonly List<LL2AnswerObject> selectedAnswers = new List<LL2AnswerObject>();

        private LL2UIManager uiManager;
        private Camera cam;
        private int currentQuestionIndex;
        private int currentStepIndex;
        private int correctStepAnswers;
        private bool stepActive;
        private bool currentStepHadWrongAnswer;
        private Coroutine flowCoroutine;

        private int MaxQuestions => questions == null ? 0 : questions.Length;
        private StoryQuestion CurrentQuestion => questions[currentQuestionIndex];
        private QuestionStep CurrentStep => CurrentQuestion.steps[currentStepIndex];

        private void Reset()
        {
            FillDefaultQuestionTemplates();
        }

        private void OnValidate()
        {
            if (buildQuestionsFromCode && ShouldRefreshCodeQuestions())
                FillDefaultQuestionTemplates();
        }

        private void Awake()
        {
            uiManager = FindFirstObjectByType<LL2UIManager>();
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
            if (!stepActive || MaxQuestions == 0)
                return;

            if (WasPointerPressedThisFrame() && TryGetPointerPosition(out Vector2 pointerPosition))
                TryToggleAnswerObject(pointerPosition);
        }

        [ContextMenu("Isi Template 5 Soal Level 2 Literasi")]
        public void FillDefaultQuestionTemplates()
        {
            questions = new[]
            {
                new StoryQuestion
                {
                    sentenceText = "Budi pergi ke kamar lalu belajar Matematika.",
                    connectorWord = "lalu",
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Siapa yang pergi ke kamar?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "boy",
                            distractorObjectIds = "girl"
                        },
                        new QuestionStep
                        {
                            questionText = "Apa yang Budi pelajari?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "math",
                            distractorObjectIds = "books"
                        }
                    }
                },
                new StoryQuestion
                {
                    sentenceText = "Budi pulang ke rumah untuk makan nasi.",
                    connectorWord = "untuk",
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Ke mana Budi pulang?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Large,
                            correctObjectIds = "house",
                            distractorObjectIds = "shop"
                        },
                        new QuestionStep
                        {
                            questionText = "Apa yang Budi makan?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Small,
                            correctObjectIds = "rice",
                            distractorObjectIds = "donut"
                        }
                    }
                },
                new StoryQuestion
                {
                    sentenceText = "Budi tidur di rumah dan bermain bola di siang hari.",
                    connectorWord = "dan",
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Di mana Budi tidur?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Large,
                            correctObjectIds = "house",
                            distractorObjectIds = "shop"
                        },
                        new QuestionStep
                        {
                            questionText = "Apa yang Budi mainkan?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "ball",
                            distractorObjectIds = "lollipop"
                        }
                    }
                },
                new StoryQuestion
                {
                    sentenceText = "Siti menemani ibu untuk belanja di pasar.",
                    connectorWord = "untuk",
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Siapa yang Siti temani?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "mother",
                            distractorObjectIds = "father"
                        },
                        new QuestionStep
                        {
                            questionText = "Ke mana Siti pergi?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Large,
                            correctObjectIds = "market",
                            distractorObjectIds = "house"
                        }
                    }
                },
                new StoryQuestion
                {
                    sentenceText = "Ibu pergi ke pasar membeli ayam, kubis, dan ikan.",
                    connectorWord = "dan",
                    steps = new[]
                    {
                        new QuestionStep
                        {
                            questionText = "Siapa yang pergi ke pasar?",
                            hintText = "Pilih jawabanmu, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.SelectOne,
                            slotType = LL2AnswerSlotType.Medium,
                            correctObjectIds = "mother",
                            distractorObjectIds = "father"
                        },
                        new QuestionStep
                        {
                            questionText = "Apa saja yang Ibu beli?",
                            hintText = "Pilih semua jawaban yang benar, lalu tekan Cek Jawaban.",
                            interaction = LL2InteractionType.MultipleAnswer,
                            slotType = LL2AnswerSlotType.Small,
                            correctObjectIds = "chicken,cabbage,fish",
                            distractorObjectIds = "donut,lollipop"
                        }
                    }
                }
            };
        }

        public void StartGame()
        {
            if (buildQuestionsFromCode)
                FillDefaultQuestionTemplates();

            if (uiManager == null)
                uiManager = FindFirstObjectByType<LL2UIManager>();

            if (answerLayout == null)
                answerLayout = FindFirstObjectByType<LL2AnswerLayout>();

            currentQuestionIndex = 0;
            currentStepIndex = 0;
            correctStepAnswers = 0;
            currentStepHadWrongAnswer = false;
            stepActive = false;

            if (uiManager != null)
                uiManager.ResetUI();

            Debug.Log($"[LL2GameManager] StartGame dipanggil. Questions={MaxQuestions}, AnswerLayout={(answerLayout != null ? answerLayout.name : "null")}");
            StartCurrentQuestion();
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            ClearRuntimeObjects();
            StartGame();
        }

        public void ReplayCurrentAudio()
        {
            if (MaxQuestions == 0 || currentQuestionIndex >= MaxQuestions)
                return;

            PlayAudio(CurrentQuestion.audioClip);
        }

        public void SubmitAnswer()
        {
            if (!stepActive || MaxQuestions == 0)
                return;

            if (selectedAnswers.Count == 0)
            {
                if (uiManager != null)
                    uiManager.ShowFeedback(false, "Pilih objek jawaban.");

                PlayAudio(jawabanSalahClip);
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

            if (!isCorrect)
                currentStepHadWrongAnswer = true;

            PlayAudio(isCorrect ? jawabanBenarClip : jawabanSalahClip);

            if (uiManager != null)
            {
                uiManager.ShowFeedback(isCorrect, isCorrect ? "Benar!" : "Salah");
                uiManager.SetCheckButtonInteractable(false);
            }

            if (isCorrect)
            {
                if (!currentStepHadWrongAnswer)
                    correctStepAnswers++;
            }

            CompleteCurrentStep();
        }

        private void StartCurrentQuestion()
        {
            if (MaxQuestions == 0)
            {
                Debug.LogError("[LL2GameManager] Data questions masih kosong.");
                return;
            }

            if (currentQuestionIndex >= MaxQuestions)
            {
                ShowFinalResult();
                return;
            }

            currentStepIndex = 0;
            currentStepHadWrongAnswer = false;

            if (flowCoroutine != null)
                StopCoroutine(flowCoroutine);

            flowCoroutine = StartCoroutine(QuestionIntroFlow());
        }

        private IEnumerator QuestionIntroFlow()
        {
            ClearRuntimeObjects();
            stepActive = false;

            StoryQuestion question = CurrentQuestion;
            string highlightedSentence = BuildHighlightedSentence(question.sentenceText, question.connectorWord);

            if (uiManager != null)
                uiManager.ShowSentence(question.sentenceText, highlightedSentence, currentQuestionIndex + 1, MaxQuestions, false);

            PlayAudio(question.audioClip);

            float audioWait = question.audioClip != null ? question.audioClip.length : 0f;
            yield return new WaitForSeconds(Mathf.Max(sentenceReadDelay, audioWait));

            if (uiManager != null)
                uiManager.ShowSentence(question.sentenceText, highlightedSentence, currentQuestionIndex + 1, MaxQuestions, true);

            yield return new WaitForSeconds(connectorHighlightDuration);
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            ClearStepObjects();
            currentStepHadWrongAnswer = false;
            Debug.Log($"[LL2GameManager] ShowCurrentStep Soal {currentQuestionIndex + 1}, Step {currentStepIndex + 1}: {CurrentStep.questionText}");
            SpawnStepObjects(CurrentStep);
            stepActive = true;

            if (uiManager != null)
            {
                uiManager.ShowQuestion(
                    CurrentStep,
                    currentQuestionIndex + 1,
                    MaxQuestions,
                    currentStepIndex + 1,
                    CurrentQuestion.steps.Length
                );
                uiManager.SetCheckButtonInteractable(false);
            }
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

            if (CurrentStep.interaction == LL2InteractionType.SelectOne)
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

        private void CompleteCurrentStep()
        {
            stepActive = false;
            StartCoroutine(AdvanceAfterDelay());
        }

        private IEnumerator AdvanceAfterDelay()
        {
            bool isLastStep = currentStepIndex >= CurrentQuestion.steps.Length - 1;
            yield return new WaitForSeconds(isLastStep ? nextQuestionDelay : nextStepDelay);

            if (isLastStep)
            {
                currentQuestionIndex++;
                StartCurrentQuestion();
            }
            else
            {
                currentStepIndex++;
                ShowCurrentStep();
            }
        }

        private void SpawnStepObjects(QuestionStep step)
        {
            List<string> objectIds = new List<string>();
            objectIds.AddRange(SplitIds(step.correctObjectIds));
            objectIds.AddRange(SplitIds(step.distractorObjectIds));
            Shuffle(objectIds);

            LL2AnswerSlotType slotType = step.slotType == LL2AnswerSlotType.Auto ? InferSlotType(objectIds) : step.slotType;
            if (answerLayout != null)
                answerLayout.ShowSlotGroup(slotType);

            Transform[] slots = answerLayout != null ? answerLayout.GetSlots(slotType) : null;
            Debug.Log($"[LL2GameManager] SpawnStepObjects slotType={slotType}, slots={(slots != null ? slots.Length : 0)}, choices={string.Join(", ", objectIds)}");

            if (slots == null || slots.Length < objectIds.Count)
            {
                int slotCount = slots == null ? 0 : slots.Length;
                Debug.LogWarning($"[LL2GameManager] Slot {slotType} hanya {slotCount}, pilihan ada {objectIds.Count}. Sisa object pakai fallback position.");
            }

            for (int i = 0; i < objectIds.Count; i++)
            {
                string objectId = objectIds[i];
                GameObject prefab = GetPrefabForId(objectId);
                if (prefab == null)
                {
                    Debug.LogWarning($"[LL2GameManager] Prefab untuk objectId '{objectId}' belum di-assign di LL2GameManager.");
                    continue;
                }

                Transform slot = slots != null && i < slots.Length ? slots[i] : null;
                Vector3 position = slot != null ? slot.position : GetFallbackChoicePosition(i, objectIds.Count);
                Quaternion rotation = slot != null ? slot.rotation : GetFallbackRotation();
                Transform parent = slot != null ? slot : runtimeParent;

                GameObject instance = Instantiate(prefab, position, rotation, parent);
                instance.name = $"LL2_{objectId}";
                instance.SetActive(true);

                if (objectScale != Vector3.one)
                    instance.transform.localScale = Vector3.Scale(instance.transform.localScale, objectScale);

                EnsureCollider(instance);
                EnsureStableRigidbody(instance);

                LL2AnswerObject answerObject = instance.GetComponent<LL2AnswerObject>();
                if (answerObject == null)
                    answerObject = instance.AddComponent<LL2AnswerObject>();

                if (answerObject == null)
                {
                    Debug.LogError($"[LL2GameManager] Gagal menambahkan LL2AnswerObject ke '{instance.name}'.");
                    Destroy(instance);
                    continue;
                }

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
                Debug.Log($"[LL2GameManager] Spawned {instance.name} di {(slot != null ? slot.name : "fallback position")}.");
            }

            Debug.Log($"[LL2GameManager] Total answer spawned: {spawnedAnswers.Count}/{objectIds.Count}");
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

        private void PlayAudio(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }

        private string BuildHighlightedSentence(string sentence, string connectorWord)
        {
            if (string.IsNullOrWhiteSpace(sentence) || string.IsNullOrWhiteSpace(connectorWord))
                return sentence;

            int index = sentence.IndexOf(connectorWord, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
                return sentence;

            string before = sentence.Substring(0, index);
            string matched = sentence.Substring(index, connectorWord.Length);
            string after = sentence.Substring(index + connectorWord.Length);
            return $"{before}<mark={connectorHighlightColor}AA>{matched}</mark>{after}";
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
                case "boy": return boyPrefab;
                case "girl": return girlPrefab;
                case "math": return mathPrefab;
                case "books": return booksPrefab;
                case "house": return housePrefab;
                case "shop": return shopPrefab;
                case "rice": return ricePrefab;
                case "donut": return donutPrefab;
                case "ball": return ballPrefab;
                case "lollipop": return lollipopPrefab;
                case "mother": return motherPrefab;
                case "father": return fatherPrefab;
                case "market": return marketPrefab;
                case "chicken": return chickenPrefab;
                case "cabbage": return cabbagePrefab;
                case "fish": return fishPrefab;
                default: return null;
            }
        }

        private string GetLabelForId(string objectId)
        {
            switch (objectId)
            {
                case "boy": return "Budi";
                case "girl": return "Siti";
                case "math": return "Matematika";
                case "books": return "Literasi";
                case "house": return "Rumah";
                case "shop": return "Toko";
                case "rice": return "Nasi";
                case "donut": return "Donat";
                case "ball": return "Bola";
                case "lollipop": return "Permen";
                case "mother": return "Ibu";
                case "father": return "Ayah";
                case "market": return "Pasar";
                case "chicken": return "Ayam";
                case "cabbage": return "Kubis";
                case "fish": return "Ikan";
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
            return objectId == "boy"
                || objectId == "girl"
                || objectId == "math"
                || objectId == "books"
                || objectId == "mother"
                || objectId == "father";
        }

        private bool ShouldRefreshCodeQuestions()
        {
            if (questions == null || questions.Length != 5)
                return true;

            foreach (StoryQuestion question in questions)
            {
                if (question == null || question.steps == null || question.steps.Length != 2)
                    return true;
            }

            return questions[4].steps[1].interaction != LL2InteractionType.MultipleAnswer
                || questions[4].steps[1].slotType != LL2AnswerSlotType.Small;
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
            ClearRuntimeObjects();
            stepActive = false;

            int finalScore = correctStepAnswers / 2;
            PlayAudio(finalScore >= minimumCorrectToPass ? levelCompleteClip : levelIncompleteClip);
            LevelProgress.SaveResult(progressSubject, progressLevelNumber, finalScore, minimumCorrectToPass);

            if (uiManager != null)
                uiManager.ShowFinalResult(finalScore, MaxQuestions, minimumCorrectToPass);

            Debug.Log($"[LL2GameManager] Game selesai. Step benar: {correctStepAnswers}/10, skor akhir: {finalScore}/{MaxQuestions}");
        }

        private void ClearRuntimeObjects()
        {
            ClearStepObjects();
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

            if (answerLayout != null)
                answerLayout.HideAllSlotGroups();
        }

        private void OnDrawGizmosSelected()
        {
            if (choiceAreaCenter == null)
                return;

            Gizmos.color = new Color(0f, 0.75f, 1f, 0.4f);
            Gizmos.DrawWireCube(choiceAreaCenter.position, new Vector3(fallbackChoiceSpacing * 3f, 0.02f, fallbackRowSpacing * 2f));
        }
    }

}
