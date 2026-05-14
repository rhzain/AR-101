using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pasang script ini di GameObject meja (area drop).
/// Pastikan BoxCollider di meja di-set sebagai IS TRIGGER.
/// Script ini menghitung apel yang berada di area meja.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DropZone : MonoBehaviour
{
    private HashSet<GameObject> applesInZone = new HashSet<GameObject>();

    /// <summary>Jumlah apel yang saat ini ada di atas meja (aktif dan belum dihancurkan).</summary>
    public int AppleCount
    {
        get
        {
            // Bersihkan entri null (apel yang sudah dihancurkan) dari set
            // Ini penting karena Unity tidak selalu fire OnTriggerExit saat objek di-Destroy
            applesInZone.RemoveWhere(apple => apple == null);
            return applesInZone.Count;
        }
    }

    void Awake()
    {
        // Pastikan collider ini berfungsi sebagai trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Apple"))
        {
            applesInZone.Add(other.gameObject);
            Debug.Log($"Apel masuk DropZone. Total: {AppleCount}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Apple"))
        {
            applesInZone.Remove(other.gameObject);
            Debug.Log($"Apel keluar DropZone. Total: {AppleCount}");
        }
    }

    /// <summary>
    /// Bersihkan semua apel di dalam zona (panggil tiap ronde baru).
    /// </summary>
    public void ClearApples()
    {
        // Buat salinan list untuk dihapus agar aman saat iterasi
        var toDelete = new List<GameObject>(applesInZone);
        foreach (GameObject apple in toDelete)
        {
            if (apple != null)
                Destroy(apple);
        }
        applesInZone.Clear();
        Debug.Log("DropZone: semua apel dibersihkan.");
    }
}
