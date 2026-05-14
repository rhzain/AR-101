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

        // Spawn apel di posisi keranjang + offset tinggi
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeight;
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
