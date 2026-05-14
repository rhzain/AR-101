using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;

public class MarkerHandler : MonoBehaviour
{
    public string targetScene; // Isi di Inspector: "MathLevel" atau "LiteracyLevel"
    public GameObject panelUI;

    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        observer.OnTargetStatusChanged += OnStatusChanged;

        panelUI.SetActive(false);
    }

    void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED;

        panelUI.SetActive(isTracked);
        PlayerPrefs.SetString("MODE", targetScene);
    }
}