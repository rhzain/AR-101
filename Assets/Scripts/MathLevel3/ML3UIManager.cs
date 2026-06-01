using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MathLevel3
{
    public class ML3UIManager : MonoBehaviour
    {
        [Header("Teks UI")]
        public TextMeshProUGUI storyText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI feedbackText;
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI hintText;
        public TextMeshProUGUI targetCountText;

        [Header("Input Jawaban")]
        [Tooltip("Optional. Pakai kalau label 'Jawaban' terpisah dari TMP_InputField.")]
        public TextMeshProUGUI answerInputLabel;
        [Tooltip("Optional. Isi parent/group input kalau mau label + input ikut hide/show bareng.")]
        public GameObject answerInputContainer;
        public TMP_InputField answerInput;

        [Header("Count Label")]
        [Tooltip("Optional. Isi parent/group count kalau mau label jumlah ikut hide/show bareng.")]
        public GameObject targetCountContainer;

        [Header("Tombol")]
        public Button submitButton;

        [Header("Canvas")]
        public GameObject canvasHeader;
        public GameObject canvasSoal;
        public GameObject canvasRecap;

        [Header("Result Panel")]
        public ResultPanel resultPanel;
        public Sprite[] resultSprites;

        private ML3GameManager gameManager;
        private ML3Slot activeTargetSlot;
        private bool showTargetCount;

        private void Start()
        {
            gameManager = FindFirstObjectByType<ML3GameManager>();

            if (resultPanel != null)
                resultPanel.Hide();

            if (submitButton != null)
            {
                submitButton.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                        gameManager.SubmitAnswer();
                });
                submitButton.gameObject.SetActive(false);
            }

            ShowHeaderOnly();
        }

        private void Update()
        {
            if (targetCountText == null)
                return;

            if (!showTargetCount || activeTargetSlot == null)
            {
                targetCountText.text = "";
                return;
            }

            targetCountText.text = $"Jumlah: {activeTargetSlot.ItemCount}";
        }

        public void ShowStep(
            string story,
            ML3GameManager.QuestionStep step,
            int currentQuestion,
            int maxQuestions,
            int currentStep,
            int maxSteps,
            ML3Slot targetSlot)
        {
            activeTargetSlot = targetSlot;

            if (resultPanel != null)
                resultPanel.Hide();

            ShowGameplayCanvas();

            if (storyText != null) storyText.text = story;
            if (roundText != null) roundText.text = $"Soal {currentQuestion}/{maxQuestions} - Langkah {currentStep}/{maxSteps}";
            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = step.questionText;
            if (hintText != null) hintText.text = step.hintText;

            bool needsTextAnswer = StepNeedsTextAnswer(step);
            bool needsSubmitButton = step.interaction != ML3StepInteraction.SelectSlot;
            showTargetCount = step.validateTargetCount && step.interaction != ML3StepInteraction.SelectSlot && targetSlot != null;

            SetTargetCountVisible(showTargetCount);
            if (targetCountText != null)
                targetCountText.text = showTargetCount ? $"Jumlah: {targetSlot.ItemCount}" : "";

            if (answerInput != null)
                answerInput.text = "";

            SetAnswerInputVisible(needsTextAnswer);

            if (submitButton != null)
                submitButton.gameObject.SetActive(needsSubmitButton);
        }

        public string GetAnswer()
        {
            if (answerInput == null)
            {
                Debug.LogWarning("[ML3UIManager] Answer Input belum di-assign di Inspector.");
                return "";
            }

            return answerInput != null ? answerInput.text : "";
        }

        public void ShowFeedback(
            bool isCorrect,
            bool answerCorrect,
            bool targetCorrect,
            string playerAnswer,
            string correctAnswer,
            int playerCount,
            int correctCount)
        {
            if (feedbackText != null)
            {
                if (isCorrect)
                {
                    feedbackText.text = "Benar!";
                }
                else if (!targetCorrect && playerCount >= 0)
                {
                    feedbackText.text = $"Belum tepat. Jumlah area target: {playerCount}, jawaban target: {correctCount}.";
                }
                else if (!answerCorrect)
                {
                    feedbackText.text = $"Jumlah sudah tepat, tapi jawaban teks belum tepat. Jawaban: {FormatAnswer(correctAnswer)}.";
                }
                else
                {
                    feedbackText.text = "Belum tepat. Coba perhatikan lagi ceritanya.";
                }
            }

            if (submitButton != null)
                submitButton.gameObject.SetActive(!isCorrect);
        }

        public void ShowSelectionFeedback(bool isCorrect, string correctSlotLabel, string selectedSlotLabel)
        {
            if (feedbackText != null)
            {
                feedbackText.text = isCorrect
                    ? "Benar!"
                    : $"Belum tepat. Kamu memilih {selectedSlotLabel}. Pilih meja {correctSlotLabel}.";
            }
        }

        private string FormatAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return "";

            return answer.Replace("|", " / ");
        }

        public void ShowFinalResult(int correctAnswers, int totalRounds)
        {
            activeTargetSlot = null;
            showTargetCount = false;
            ShowRecapCanvas();

            if (resultPanel == null || resultSprites == null || resultSprites.Length == 0)
                return;

            int spriteIndex = Mathf.Clamp(correctAnswers, 0, resultSprites.Length - 1);
            bool isPassed = gameManager != null && correctAnswers >= gameManager.minimumCorrectToPass;
            string buttonText = isPassed ? "Level Berikutnya" : "Coba Lagi";

            resultPanel.Show(correctAnswers, totalRounds, resultSprites[spriteIndex], isPassed, buttonText, () =>
            {
                if (isPassed)
                    SceneManager.LoadScene("MathLevel");
                else if (gameManager != null)
                    gameManager.RestartGame();
            });
        }

        public void ResetUI()
        {
            activeTargetSlot = null;
            showTargetCount = false;
            ShowHeaderOnly();

            if (storyText != null) storyText.text = "";
            if (roundText != null) roundText.text = "";
            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = "";
            if (hintText != null) hintText.text = "";
            if (targetCountText != null) targetCountText.text = "";
            SetTargetCountVisible(false);

            if (answerInput != null)
                answerInput.text = "";

            SetAnswerInputVisible(false);

            if (submitButton != null)
                submitButton.gameObject.SetActive(false);
        }

        private void SetAnswerInputVisible(bool isVisible)
        {
            if (answerInputContainer != null)
            {
                answerInputContainer.SetActive(isVisible);
                return;
            }

            if (answerInputLabel != null)
                answerInputLabel.gameObject.SetActive(isVisible);

            if (answerInput != null)
                answerInput.gameObject.SetActive(isVisible);
        }

        private void SetTargetCountVisible(bool isVisible)
        {
            if (targetCountContainer != null)
            {
                targetCountContainer.SetActive(isVisible);
                return;
            }

            if (targetCountText != null)
                targetCountText.gameObject.SetActive(isVisible);
        }

        private bool StepNeedsTextAnswer(ML3GameManager.QuestionStep step)
        {
            return step.interaction == ML3StepInteraction.TextAnswer
                || (step.interaction == ML3StepInteraction.ObserveOnly && !string.IsNullOrWhiteSpace(step.answer));
        }

        private void ShowHeaderOnly()
        {
            if (canvasHeader != null) canvasHeader.SetActive(true);
            if (canvasSoal != null) canvasSoal.SetActive(false);
            if (canvasRecap != null) canvasRecap.SetActive(false);
            if (resultPanel != null) resultPanel.Hide();
        }

        private void ShowGameplayCanvas()
        {
            if (canvasHeader != null) canvasHeader.SetActive(true);
            if (canvasSoal != null) canvasSoal.SetActive(true);
            if (canvasRecap != null) canvasRecap.SetActive(false);
        }

        private void ShowRecapCanvas()
        {
            if (canvasHeader != null) canvasHeader.SetActive(false);
            if (canvasSoal != null) canvasSoal.SetActive(false);
            if (canvasRecap != null) canvasRecap.SetActive(true);
        }
    }
}
