using System.Collections;
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
        public float wrongFeedbackDuration = 1.25f;

        [Header("Input Jawaban")]
        [Tooltip("Optional. Pakai kalau label 'Jawaban' terpisah dari TMP_InputField.")]
        public TextMeshProUGUI answerInputLabel;
        [Tooltip("Optional. Isi parent/group input kalau mau label + input ikut hide/show bareng.")]
        public GameObject answerInputContainer;
        public TMP_InputField answerInput;

        [Header("Jawaban Ya/Tidak")]
        [Tooltip("Container tombol Ya dan Tidak. Akan muncul hanya untuk soal jawaban ya/tidak.")]
        public GameObject yesNoButtonContainer;
        public Button yesButton;
        public Button noButton;
        public string yesAnswerValue = "ya";
        public string noAnswerValue = "tidak";

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
        public string completeButtonText = "Selesai";
        public string incompleteButtonText = "Coba Lagi";

        private ML3GameManager gameManager;
        private ML3Slot activeTargetSlot;
        private bool showTargetCount;
        private bool currentUsesYesNoButtons;
        private string selectedButtonAnswer = "";
        private string currentHintText;
        private Coroutine restoreHintCoroutine;

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

            if (yesButton != null)
                yesButton.onClick.AddListener(SubmitYesAnswer);

            if (noButton != null)
                noButton.onClick.AddListener(SubmitNoAnswer);

            SetYesNoButtonsVisible(false);

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
            currentHintText = step.hintText;
            StopRestoreHintCoroutine();

            bool needsTextAnswer = StepNeedsTextAnswer(step);
            currentUsesYesNoButtons = StepUsesYesNoButtons(step);
            bool needsSubmitButton = step.interaction != ML3StepInteraction.SelectSlot;
            showTargetCount = step.validateTargetCount && step.interaction != ML3StepInteraction.SelectSlot && targetSlot != null;
            selectedButtonAnswer = "";

            SetTargetCountVisible(showTargetCount);
            if (targetCountText != null)
                targetCountText.text = showTargetCount ? $"Jumlah: {targetSlot.ItemCount}" : "";

            if (answerInput != null)
                answerInput.text = "";

            SetAnswerInputVisible(needsTextAnswer && !currentUsesYesNoButtons);
            SetYesNoButtonsVisible(currentUsesYesNoButtons);

            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(needsSubmitButton);
                submitButton.interactable = needsSubmitButton && !currentUsesYesNoButtons;
            }
        }

        public string GetAnswer()
        {
            if (currentUsesYesNoButtons)
                return selectedButtonAnswer;

            if (answerInput == null)
            {
                Debug.LogWarning("[ML3UIManager] Answer Input belum di-assign di Inspector.");
                return "";
            }

            return answerInput != null ? answerInput.text : "";
        }

        public void SubmitYesAnswer()
        {
            SubmitButtonAnswer(yesAnswerValue);
        }

        public void SubmitNoAnswer()
        {
            SubmitButtonAnswer(noAnswerValue);
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
                    feedbackText.text = $"Belum tepat.";
                }
                else if (!answerCorrect)
                {
                    feedbackText.text = $"Jumlah sudah tepat, tapi pilihan jawaban belum tepat. Jawaban: {FormatAnswer(correctAnswer)}.";
                }
                else
                {
                    feedbackText.text = "Belum tepat. Coba perhatikan lagi ceritanya.";
                }
            }

            if (isCorrect)
                StopRestoreHintCoroutine();
            else
                ScheduleHintRestore();

            if (submitButton != null)
            {
                if (currentUsesYesNoButtons)
                {
                    submitButton.gameObject.SetActive(true);
                    submitButton.interactable = false;
                }
                else
                {
                    submitButton.gameObject.SetActive(!isCorrect);
                    submitButton.interactable = !isCorrect;
                }
            }
        }

        public void ShowSelectionFeedback(bool isCorrect, string correctSlotLabel, string selectedSlotLabel)
        {
            if (feedbackText != null)
            {
                feedbackText.text = isCorrect
                    ? "Benar!"
                    : $"Belum tepat. Kamu memilih {selectedSlotLabel}. Pilih meja {correctSlotLabel}.";
            }

            if (isCorrect)
                StopRestoreHintCoroutine();
            else
                ScheduleHintRestore();
        }

        public void ShowHint(string hint)
        {
            if (hintText == null)
                return;

            currentHintText = hint;
            hintText.gameObject.SetActive(true);
            hintText.text = hint;
        }

        private void ScheduleHintRestore()
        {
            StopRestoreHintCoroutine();
            restoreHintCoroutine = StartCoroutine(RestoreHintAfterDelay());
        }

        private IEnumerator RestoreHintAfterDelay()
        {
            yield return new WaitForSeconds(wrongFeedbackDuration);
            ShowHint(currentHintText);
            restoreHintCoroutine = null;
        }

        private void StopRestoreHintCoroutine()
        {
            if (restoreHintCoroutine == null)
                return;

            StopCoroutine(restoreHintCoroutine);
            restoreHintCoroutine = null;
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
            string buttonText = isPassed ? completeButtonText : incompleteButtonText;

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
            currentHintText = "";
            StopRestoreHintCoroutine();
            if (targetCountText != null) targetCountText.text = "";
            SetTargetCountVisible(false);

            if (answerInput != null)
                answerInput.text = "";

            SetAnswerInputVisible(false);
            SetYesNoButtonsVisible(false);
            currentUsesYesNoButtons = false;
            selectedButtonAnswer = "";

            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
                submitButton.interactable = true;
            }
        }

        private void SubmitButtonAnswer(string answer)
        {
            selectedButtonAnswer = answer;

            if (gameManager != null)
                gameManager.SubmitAnswer();
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

        private void SetYesNoButtonsVisible(bool isVisible)
        {
            if (yesNoButtonContainer != null)
            {
                yesNoButtonContainer.SetActive(isVisible);
                return;
            }

            if (yesButton != null)
                yesButton.gameObject.SetActive(isVisible);

            if (noButton != null)
                noButton.gameObject.SetActive(isVisible);
        }

        private bool StepNeedsTextAnswer(ML3GameManager.QuestionStep step)
        {
            return step.interaction == ML3StepInteraction.TextAnswer
                || (step.interaction == ML3StepInteraction.ObserveOnly && !string.IsNullOrWhiteSpace(step.answer));
        }

        private bool StepUsesYesNoButtons(ML3GameManager.QuestionStep step)
        {
            if (!StepNeedsTextAnswer(step) || string.IsNullOrWhiteSpace(step.answer))
                return false;

            string[] acceptedAnswers = step.answer.Split(new[] { '|', '/', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string acceptedAnswer in acceptedAnswers)
            {
                string normalizedAnswer = acceptedAnswer.Trim().ToLowerInvariant();
                if (normalizedAnswer == "ya"
                    || normalizedAnswer == "iya"
                    || normalizedAnswer == "y"
                    || normalizedAnswer == "tidak"
                    || normalizedAnswer == "nggak"
                    || normalizedAnswer == "enggak"
                    || normalizedAnswer == "no")
                {
                    return true;
                }
            }

            return false;
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
