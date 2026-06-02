using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteracyLevel2
{
    public class LL2UIManager : MonoBehaviour
    {
        [Header("Teks UI")]
        public TextMeshProUGUI sentenceText;
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

        private LL2GameManager gameManager;

        private void Start()
        {
            gameManager = FindFirstObjectByType<LL2GameManager>();

            if (resultPanel != null)
                resultPanel.Hide();

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                        gameManager.SubmitAnswer();
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

            if (sentenceText != null) sentenceText.text = "";
            if (roundText != null) roundText.text = "";
            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = "";
            if (instructionText != null) instructionText.text = "";
            SetSelectedObjectText("");
            SetCheckButtonVisible(false);
        }

        public void ShowSentence(string sentence, string highlightedSentence, int currentQuestion, int maxQuestions, bool highlightConnector)
        {
            ShowGameplayCanvas();

            if (sentenceText != null)
                sentenceText.text = highlightConnector ? highlightedSentence : sentence;

            if (roundText != null)
                roundText.text = $"Soal {currentQuestion}/{maxQuestions}";

            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = "";
            if (instructionText != null) instructionText.text = "";
            SetSelectedObjectText("");
            SetCheckButtonVisible(false);
        }

        public void ShowQuestion(LL2GameManager.QuestionStep step, int currentQuestion, int maxQuestions, int currentStep, int maxSteps)
        {
            ShowGameplayCanvas();

            if (roundText != null)
                roundText.text = $"Soal {currentQuestion}/{maxQuestions} - Pertanyaan {currentStep}/{maxSteps}";

            if (feedbackText != null) feedbackText.text = "";
            if (questionText != null) questionText.text = step.questionText;
            if (instructionText != null) instructionText.text = GetInstructionText(step);
            SetSelectedObjectText("");

            SetCheckButtonVisible(true);
            SetCheckButtonInteractable(false);
        }

        public void ShowFeedback(bool isCorrect, string message)
        {
            if (feedbackText != null)
                feedbackText.text = isCorrect ? "Benar!" : message;

            if (isCorrect)
                SetCheckButtonInteractable(false);
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

            selectedObjectText.text = selectedText;
        }

        public void SetCheckButtonInteractable(bool isInteractable)
        {
            if (submitButton != null)
                submitButton.interactable = isInteractable;
        }

        private void SetCheckButtonVisible(bool isVisible)
        {
            if (submitButton == null)
                return;

            submitButton.gameObject.SetActive(isVisible);
            submitButton.interactable = false;
        }

        private string GetInstructionText(LL2GameManager.QuestionStep step)
        {
            if (!string.IsNullOrWhiteSpace(step.hintText))
                return step.hintText;

            return step.interaction == LL2InteractionType.MultipleAnswer
                ? "Pilih semua jawaban yang benar, lalu tekan Cek Jawaban."
                : "Pilih jawabanmu, lalu tekan Cek Jawaban.";
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
