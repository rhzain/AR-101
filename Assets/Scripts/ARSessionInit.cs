using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Pasang script ini ke GameObject "AR Session" di setiap scene AR.
/// Ini memastikan ARSession di-reset dan siap digunakan setiap kali scene AR dibuka.
/// </summary>
public class ARSessionInit : MonoBehaviour
{
    private ARSession arSession;

    void Awake()
    {
        arSession = GetComponent<ARSession>();
    }

    void OnEnable()
    {
        StartCoroutine(ResetAndInit());
    }

    System.Collections.IEnumerator ResetAndInit()
    {
        if (arSession != null)
        {
            arSession.Reset();
        }

        Debug.Log($"[ARSessionInit] ARSession di-reset. State sekarang: {ARSession.state}");
        yield return null;
    }
}
