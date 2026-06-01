using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pasang script ini di GameObject "Keranjang" (sumber apel).
/// Ketika pemain menyentuh/klik keranjang ini, apel baru akan di-spawn
/// dan langsung mengikuti jari pemain (drag). Saat dilepas, apel jatuh.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AppleBasket : MonoBehaviour
{
    [Header("Pengaturan")]
    public GameObject applePrefab;

    [Tooltip("Tinggi spawn apel di atas posisi keranjang")]
    public float spawnHeight = 0.1f;

    [Tooltip("Radius area kecil untuk mencari posisi spawn kosong di sekitar keranjang")]
    public float spawnSearchRadius = 0.12f;

    [Tooltip("Jarak minimal dari pusat apel lain saat spawn")]
    public float appleCollisionRadius = 0.08f;

    [Tooltip("Jumlah percobaan mencari posisi kosong saat spawn")]
    public int maxSpawnAttempts = 20;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Cek sentuhan jari (Android)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            TrySpawnApple(touchPos);
        }

        // Cek klik mouse (Editor)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySpawnApple(Mouse.current.position.ReadValue());
        }
    }

    void TrySpawnApple(Vector2 screenPos)
    {
        // Jangan spawn apel baru jika sedang ada apel yang di-drag
        if (DraggableApple.AnyAppleDragging) return;

        Ray ray = cam.ScreenPointToRay(screenPos);

        // Hanya spawn apel jika yang ditekan adalah keranjang ini
        int basketLayer = LayerMask.GetMask("Interactable");
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, basketLayer))
            return;

        // Cek apakah yang tertabrak adalah bagian dari keranjang ini
        // Menggunakan transform.IsChildOf untuk mengantisipasi jika yang tertabrak adalah mesh/child keranjang
        if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            return;

        Vector3 spawnPos = GetClearSpawnPosition();
        GameObject apple = Instantiate(applePrefab, spawnPos, Quaternion.identity);
        
        // Pastikan objeknya aktif (berguna jika master/prefab-nya dalam kondisi deactive di scene)
        apple.SetActive(true);

        // Langsung berikan kontrol drag ke apel yang baru di-spawn
        DraggableApple draggable = apple.GetComponent<DraggableApple>();
        if (draggable != null)
        {
            draggable.StartDragImmediately(screenPos);
        }

        Debug.Log("Apel baru di-spawn dari keranjang!");
    }

    Vector3 GetClearSpawnPosition()
    {
        Vector3 fallbackPosition = transform.position + transform.up * spawnHeight;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float rx = Random.Range(-spawnSearchRadius, spawnSearchRadius);
            float rz = Random.Range(-spawnSearchRadius, spawnSearchRadius);

            Vector3 candidatePosition = transform.position
                                      + transform.right * rx
                                      + transform.forward * rz
                                      + transform.up * spawnHeight;

            if (!IsBlockedByApple(candidatePosition))
                return candidatePosition;

            fallbackPosition = candidatePosition;
        }

        Debug.LogWarning("Tidak menemukan posisi spawn apel kosong di keranjang.");
        return fallbackPosition;
    }

    bool IsBlockedByApple(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(
            position,
            appleCollisionRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Apple"))
                return true;
        }

        return false;
    }

    void OnDrawGizmos()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            // Warna Hijau transparan untuk hitbox keranjang
            Gizmos.color = new Color(0, 1, 0, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            
            // Garis pinggir agar lebih jelas
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}
