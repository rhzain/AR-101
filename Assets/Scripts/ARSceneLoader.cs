using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Gunakan script ini untuk pindah ke scene AR.
/// Ini akan me-reset ARSession terlebih dahulu agar kamera AR tidak hitam.
/// Pasang ke GameObject mana saja, lalu hubungkan tombol ke LoadARScene().
/// </summary>
public class ARSceneLoader : MonoBehaviour
{
    public void LoadARScene(string sceneName)
    {
        StartCoroutine(LoadWithARReset(sceneName));
    }

    IEnumerator LoadWithARReset(string sceneName)
    {
        // Reset ARSession via instance
        ARSession arSession = FindFirstObjectByType<ARSession>();
        if (arSession != null)
        {
            arSession.Reset();
        }

        // Tunggu 1 frame agar reset selesai
        yield return null;

        SceneManager.LoadScene(sceneName);
    }
}
