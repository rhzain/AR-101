using UnityEngine;

public class AppleCounter : MonoBehaviour
{
    private int count = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Apple")) count++;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Apple")) count--;
    }

    public int GetCount() => count;

    public void ResetCount() => count = 0;
}