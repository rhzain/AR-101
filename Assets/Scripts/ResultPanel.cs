using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultPanel : MonoBehaviour
{
    public Image resultGraphic;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI coinText; // Jika ada sistem koin
    public Button retryButton;

    public void Show(int correctCount, int totalRounds, Sprite resultSprite)
    {
        gameObject.SetActive(true); // Aktifkan seluruh panel
        
        if (resultGraphic != null) resultGraphic.sprite = resultSprite;
        if (resultText != null) resultText.text = $"{correctCount} dari {totalRounds}";
        if (coinText != null) coinText.text = $"+{correctCount * 10}"; // Contoh perhitungan koin
        retryButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
