using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteracyLevel3
{
    public class LL3UIManager : MonoBehaviour
    {
        [Header("Teks UI")]
        public TextMeshProUGUI storyText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI feedbackText;
        public TextMeshProUGUI questionText;
        [Tooltip("Optional. Isi dengan parent object yang berisi background + questionText jika ingin disembunyikan bersama.")]
        public GameObject questionTextContainer;
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI selectedObjectText;
        public string selectObjectHintText = "Pilih objek jawaban.";

        [Header("Tombol")]
        public Button submitButton;
        public Button replayAudioButton;

        [Header("Canvas")]
        public GameObject canvasHeader;
        public GameObject canvasSoal;
        public GameObject canvasRecap;

        [Header("Result Panel")]
        public ResultPanel resultPanel;
        public Sprite[] resultSprites;
        public string nextSceneName = "LiteracyLevel";
        public string completeButtonText = "Selesai";
        public string incompleteButtonText = "Coba Lagi";

        private LL3GameManager gameManager;
        private string currentHintText = "";
        private string currentSelectedText = "";

        private void Start()
        {
            gameManager = FindFirstObjectByType<LL3GameManager>();

            if (resultPanel != null)
                resultPanel.Hide();

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                        gameManager.SubmitCurrentStage();
                });
                submitButton.gameObject.SetActive(false);
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                        gameManager.ReplayCurrentAudio();
                });
            }

            ShowHeaderOnly();
        }

        public void ResetUI()
        {
            ShowHeaderOnly();

            if (storyText != null) storyText.text = "";
            if (roundText != null) roundText.text = "";
            currentHintText = "";
            currentSelectedText = "";
            ClearFeedbackTextOnly();
            SetQuestionText("", false);
            SetInstructionText("");
            SetSelectedObjectText("");
            SetCheckButtonVisible(false);
        }

        public void ShowStoryOrder(LL3GameManager.StoryRound story, int currentStory, int maxStories, bool cardsReady)
        {
            ShowGameplayCanvas();

            if (storyText != null)
                storyText.text = story != null ? story.storyText : "";

            if (roundText != null)
                roundText.text = $"Cerita {currentStory}/{maxStories} - Susun Cerita";

            ClearFeedbackTextOnly();
            SetQuestionText("", false);
            SetInstructionText(cardsReady
                ? "Geser kartu kalimat ke slot sesuai urutan cerita."
                : "Dengarkan dan baca cerita.");

            SetSelectedObjectText("");
            SetSubmitButtonText("Cek Jawaban");
            SetCheckButtonVisible(cardsReady);
            SetCheckButtonInteractable(cardsReady);
        }

        public void ShowLiteralQuestion(LL3GameManager.LiteralStep step, int currentStory, int maxStories, int currentStep, int maxSteps)
        {
            ShowGameplayCanvas();

            if (roundText != null)
                roundText.text = $"Cerita {currentStory}/{maxStories} - Pertanyaan {currentStep}/{maxSteps}";

            ClearFeedbackTextOnly();
            SetQuestionText(step.questionText, true);
            SetInstructionText(GetInstructionText(step));
            SetSelectedObjectText("");

            SetSubmitButtonText("Cek Jawaban");
            SetCheckButtonVisible(true);
            SetCheckButtonInteractable(false);
        }

        public void ShowFeedback(bool isCorrect, string message)
        {
            string feedbackMessage = isCorrect ? "Benar!" : message;

            if (feedbackText != null)
                feedbackText.text = feedbackMessage;

            if (selectedObjectText != null)
                selectedObjectText.text = feedbackMessage;

            if (instructionText != null && (instructionText == feedbackText || instructionText == selectedObjectText))
                instructionText.text = feedbackMessage;

            if (isCorrect)
                SetCheckButtonInteractable(false);
        }

        public void ClearFeedback()
        {
            string statusText = GetSelectedOrHintText();

            if (feedbackText != null && feedbackText != selectedObjectText && feedbackText != instructionText)
                feedbackText.text = "";

            if (selectedObjectText != null)
                selectedObjectText.text = statusText;

            if (instructionText != null && (instructionText == feedbackText || instructionText == selectedObjectText))
                instructionText.text = statusText;
        }

        public void SetSelectedObjectText(string selectedText)
        {
            currentSelectedText = selectedText ?? "";

            if (selectedObjectText == null)
                return;

            selectedObjectText.text = GetSelectedOrHintText();
        }

        public void SetCheckButtonInteractable(bool isInteractable)
        {
            if (submitButton != null)
                submitButton.interactable = isInteractable;
        }

        public void SetSubmitButtonText(string buttonText)
        {
            if (submitButton == null)
                return;

            TextMeshProUGUI tmpText = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = buttonText;
                return;
            }

            Text legacyText = submitButton.GetComponentInChildren<Text>();
            if (legacyText != null)
                legacyText.text = buttonText;
        }

        public void ShowFinalResult(int correctAnswers, int totalRounds, int minimumCorrectToPass)
        {
            ShowRecapCanvas();

            if (resultPanel == null || resultSprites == null || resultSprites.Length == 0)
                return;

            int spriteIndex = Mathf.Clamp(correctAnswers, 0, resultSprites.Length - 1);
            bool isPassed = correctAnswers >= minimumCorrectToPass;
            string buttonText = isPassed ? completeButtonText : incompleteButtonText;

            resultPanel.Show(correctAnswers, totalRounds, resultSprites[spriteIndex], isPassed, buttonText, () =>
            {
                if (isPassed && !string.IsNullOrWhiteSpace(nextSceneName))
                    SceneManager.LoadScene(nextSceneName);
                else if (gameManager != null)
                    gameManager.RestartGame();
            });
        }

        private void SetCheckButtonVisible(bool isVisible)
        {
            if (submitButton == null)
                return;

            submitButton.gameObject.SetActive(isVisible);
            submitButton.interactable = isVisible;
        }

        private string GetInstructionText(LL3GameManager.LiteralStep step)
        {
            if (!string.IsNullOrWhiteSpace(step.hintText))
                return step.hintText;

            return step.interaction == LL3InteractionType.MultipleAnswer
                ? "Pilih semua jawaban yang benar, lalu tekan Cek Jawaban."
                : "Pilih jawabanmu, lalu tekan Cek Jawaban.";
        }

        private void SetInstructionText(string hintText)
        {
            currentHintText = hintText ?? "";

            if (instructionText != null)
                instructionText.text = currentHintText;
        }

        private void SetQuestionText(string text, bool isVisible)
        {
            GameObject targetObject = questionTextContainer != null
                ? questionTextContainer
                : questionText != null
                    ? questionText.gameObject
                    : null;

            if (targetObject != null)
                targetObject.SetActive(isVisible);

            if (questionText != null)
                questionText.text = isVisible ? text : "";
        }

        private string GetSelectedOrHintText()
        {
            if (!string.IsNullOrWhiteSpace(currentSelectedText))
                return $"Dipilih: {currentSelectedText}";

            return string.IsNullOrWhiteSpace(currentHintText)
                ? selectObjectHintText
                : currentHintText;
        }

        private void ClearFeedbackTextOnly()
        {
            if (feedbackText != null && feedbackText != selectedObjectText && feedbackText != instructionText)
                feedbackText.text = "";
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
