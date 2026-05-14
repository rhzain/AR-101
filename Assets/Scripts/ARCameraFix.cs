using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Pasang script ini ke GameObject "AR Camera" (bukan AR Session).
/// Script ini memaksa ARCameraBackground untuk restart saat scene AR dimuat ulang,
/// sehingga layar tidak menjadi hitam.
/// </summary>
[RequireComponent(typeof(ARCameraBackground))]
public class ARCameraFix : MonoBehaviour
{
    private ARCameraBackground cameraBackground;

    void Awake()
    {
        cameraBackground = GetComponent<ARCameraBackground>();
    }

    void OnEnable()
    {
        StartCoroutine(RestartCameraBackground());
    }

    IEnumerator RestartCameraBackground()
    {
        // Matikan dulu background renderer
        if (cameraBackground != null)
            cameraBackground.enabled = false;

        // Tunggu 2 frame agar AR Session mulai proses reset
        yield return null;
        yield return null;

        // Tunggu sampai ARSession siap — menerima Ready (XR Simulation/Editor)
        // atau SessionTracking (device fisik)
        float timeout = 5f;
        float elapsed = 0f;
        while (ARSession.state != ARSessionState.SessionTracking &&
               ARSession.state != ARSessionState.Ready)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[ARCameraFix] Timeout menunggu ARSession. State: " + ARSession.state);
                break;
            }
            yield return null;
        }

        // Tunggu 1 frame lagi untuk stabilitas
        yield return null;

        // Nyalakan kembali untuk me-rebind feed kamera
        if (cameraBackground != null)
            cameraBackground.enabled = true;

        Debug.Log("[ARCameraFix] ARCameraBackground berhasil di-restart. State: " + ARSession.state);
    }
}
