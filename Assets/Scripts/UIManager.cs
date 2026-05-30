using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject settingsPopup;
    public GameObject pialaPopup;

    private bool isSoundOn = true;

    public void OpenSettings()
    {
        settingsPopup.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPopup.SetActive(false);
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("Nama");
        PlayerPrefs.DeleteKey("Kelas");
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Keluar aplikasi");
    }

    public void ToggleSound()
    {
        if (ButtonSfxManager.Instance == null) return;

        ButtonSfxManager.Instance.PlayButtonClick();
        ButtonSfxManager.Instance.ToggleSound();
    }

    public void GoToLearning()
    {
        SceneManager.LoadScene("LearningScene");
    }
    public void OpenPiala()
    {
        pialaPopup.SetActive(true);
    }

    public void ClosePiala()
    {
        pialaPopup.SetActive(false);
    }

}
