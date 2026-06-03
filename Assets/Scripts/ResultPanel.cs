using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ResultPanel : MonoBehaviour
{
    public Image resultGraphic;
    public TextMeshProUGUI statusLevelText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI coinText; // Jika ada sistem koin
    public Button retryButton;
    public string completeStatusText = "Kamu Berhasil";
    public string incompleteStatusText = "Coba Lagi";

    public void Show(int correctCount, int totalRounds, Sprite resultSprite)
    {
        Show(correctCount, totalRounds, resultSprite, false, "Coba Lagi", null);
    }

    public void Show(int correctCount, int totalRounds, Sprite resultSprite, bool isPassed, string buttonText, UnityAction buttonAction)
    {
        gameObject.SetActive(true); // Aktifkan seluruh panel
        
        if (resultGraphic != null) resultGraphic.sprite = resultSprite;
        if (statusLevelText != null) statusLevelText.text = isPassed ? completeStatusText : incompleteStatusText;
        if (resultText != null) resultText.text = $"Benar {correctCount} dari {totalRounds}";
        if (coinText != null) coinText.text = $"+{correctCount * 10}"; // Contoh perhitungan koin

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
            retryButton.onClick.RemoveAllListeners();

            if (buttonAction != null)
                retryButton.onClick.AddListener(buttonAction);

            TextMeshProUGUI tmpText = retryButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = buttonText;
            else
            {
                Text legacyText = retryButton.GetComponentInChildren<Text>();
                if (legacyText != null)
                    legacyText.text = buttonText;
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
