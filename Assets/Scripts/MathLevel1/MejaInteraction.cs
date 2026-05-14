using UnityEngine;
using UnityEngine.InputSystem;

public class MejaInteraction : MonoBehaviour
{
    private ML1GameManager gameManager;
    private string sideType; // "Left" atau "Right"
    private Camera mainCamera;
    private Collider clickCollider;

    void Start()
    {
        gameManager = FindFirstObjectByType<ML1GameManager>();
        mainCamera = Camera.main;
        clickCollider = GetComponent<Collider>();

        if (clickCollider == null)
        {
            Debug.LogError($"MejaInteraction: {gameObject.name} tidak punya Collider!");
            return;
        }

        // Detect sisi (Left atau Right) dari nama object atau parent
        if (gameObject.name.Contains("Left") || (transform.parent != null && transform.parent.name.Contains("Left")))
        {
            sideType = "Left";
        }
        else
        {
            sideType = "Right";
        }
        Debug.Log($"MejaInteraction initialized: {gameObject.name} as {sideType} side");
    }

    void Update()
    {
        // Support Mouse (Editor/XR Simulation) - Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick();
        }

        // Support Touch (Android/Mobile) - Input System
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            HandleTouchClick();
        }
    }

    void HandleMouseClick()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager tidak ditemukan!");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        int interactableLayer = LayerMask.GetMask("Interactable");

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactableLayer))  
        {
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log($"Clicked on {sideType} side of Meja (Mouse) - Distance: {hit.distance}");
                TriggerSelection();
            }
        }
    }

    void HandleTouchClick()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager tidak ditemukan!");
            return;
        }

        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(touchPos);
        
        // 1. Ambil LayerMask
        int interactableLayer = LayerMask.GetMask("Interactable");

        // 2. Tembakkan Raycast khusus ke layer tersebut
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactableLayer))  
        {
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log($"Clicked on {sideType} side of Meja (Touch) - Distance: {hit.distance}");
                TriggerSelection();
            }
        }
    }


    void TriggerSelection()
    {
        if (sideType == "Left")
        {
            gameManager.SelectLeftSide();
        }
        else if (sideType == "Right")
        {
            gameManager.SelectRightSide();
        }
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            string currentSide = sideType;
            if (string.IsNullOrEmpty(currentSide))
            {
                if (gameObject.name.Contains("Left") || (transform.parent != null && transform.parent.name.Contains("Left")))
                    currentSide = "Left";
                else
                    currentSide = "Right";
            }

            // Warna merah transparan untuk Kiri, Biru transparan untuk Kanan
            Gizmos.color = (currentSide == "Left") ? new Color(1, 0, 0, 0.4f) : new Color(0, 0, 1, 0.4f);
            
            // Gambar hitbox sesuai batas collider di dunia nyata
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            
            // Garis pinggir agar lebih jelas
            Gizmos.color = (currentSide == "Left") ? Color.red : Color.blue;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
