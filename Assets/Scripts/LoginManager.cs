using UnityEngine;

public class LoginManager : MonoBehaviour
{
    [Header("Popup")]
    public GameObject aboutUsPopup;

    private void Start()
    {
        if (aboutUsPopup != null)
            aboutUsPopup.SetActive(false);
    }

    public void OpenAboutUs()
    {
        SetAboutUsPopup(true);
    }

    public void CloseAboutUs()
    {
        SetAboutUsPopup(false);
    }

    private void SetAboutUsPopup(bool isActive)
    {
        if (aboutUsPopup == null)
        {
            Debug.LogWarning("About Us Popup belum dihubungkan di Inspector.");
            return;
        }

        aboutUsPopup.SetActive(isActive);
    }
}
