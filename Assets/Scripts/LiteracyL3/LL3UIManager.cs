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
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI selectedObjectText;

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

        private LL3GameManager gameManager;

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
            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = "";
            if (instructionText != null) instructionText.text = "";
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

            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = "Susun urutan cerita";
            if (instructionText != null) instructionText.text = cardsReady
                ? "Geser kartu kalimat ke slot sesuai urutan cerita, lalu tekan Cek Jawaban."
                : "Dengarkan dan baca cerita.";

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

            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = step.questionText;
            if (instructionText != null) instructionText.text = GetInstructionText(step);
            SetSelectedObjectText("");

            SetSubmitButtonText("Cek Jawaban");
            SetCheckButtonVisible(true);
            SetCheckButtonInteractable(false);
        }

        public void ShowFeedback(bool isCorrect, string message)
        {
            if (feedbackText != null)
                feedbackText.text = isCorrect ? "Benar!" : message;
        }

        public void ClearFeedback()
        {
            if (feedbackText != null)
                feedbackText.text = "";
        }

        public void SetSelectedObjectText(string selectedText)
        {
            if (selectedObjectText == null)
                return;

            selectedObjectText.text = string.IsNullOrWhiteSpace(selectedText)
                ? "Dipilih: -"
                : $"Dipilih: {selectedText}";
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
            string buttonText = isPassed ? "Level Berikutnya" : "Coba Lagi";

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
