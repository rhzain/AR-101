using UnityEngine;

public class MejaSetup : MonoBehaviour
{
    public GameObject leftIndicator;
    public GameObject rightIndicator;

    void Awake()
    {
        // Fix Rigidbody supaya Meja tidak tenggelam
        FixRigidbody();
        
        // Setup spawn areas kalau belum ada
        SetupSpawnAreas();
        
        // Matikan indicators di awal (aktif saat game dimulai)
        if (leftIndicator != null)
            leftIndicator.SetActive(false);
        if (rightIndicator != null)
            rightIndicator.SetActive(false);
    }

    public void ActivateIndicators()
    {
        if (leftIndicator != null)
            leftIndicator.SetActive(true);
        if (rightIndicator != null)
            rightIndicator.SetActive(true);
        Debug.Log("Indicators activated");
    }

    void SetupSpawnAreas()
    {
        // Cek apakah SpawnAreaLeft dan SpawnAreaRight sudah ada
        Transform spawnAreaLeft = transform.Find("Left");
        Transform spawnAreaRight = transform.Find("Right");

        // Kalau belum ada, buat
        if (spawnAreaLeft == null)
        {
            GameObject leftArea = new GameObject("Left");
            leftArea.transform.SetParent(transform);
            leftArea.transform.localPosition = new Vector3(-0.3f, 0.05f, 0);
            Debug.Log("Left created");
        }

        if (spawnAreaRight == null)
        {
            GameObject rightArea = new GameObject("Right");
            rightArea.transform.SetParent(transform);
            rightArea.transform.localPosition = new Vector3(0.3f, 0.05f, 0);
            Debug.Log("Right created");
        }
    }

    void FixRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Fix settings supaya Meja tidak tenggelam
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | 
                         RigidbodyConstraints.FreezeRotation;
        
        Debug.Log("Meja Rigidbody fixed - tidak akan tenggelam lagi");
    }
}
