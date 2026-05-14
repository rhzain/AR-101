using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DraggableApple : MonoBehaviour
{
    private Camera cam;
    private Rigidbody rb;

    private bool isDragging;
    private Vector3 dragOffset;
    private Plane dragPlane;

    // Flag statis: memberitahu script lain (AppleBasket) bahwa ada apel yang sedang di-drag
    public static bool AnyAppleDragging { get; private set; } = false;

    // Ketinggian permukaan meja dalam world space — diset oleh ML2GameManager setelah meja di-spawn
    public static float TableSurfaceY { get; set; } = 0f;

    [Tooltip("Offset tinggi apel di atas permukaan meja saat di-drag")]
    public float dragHoverHeight = 0.05f;

    void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (isDragging && !IsPointerPressed())
        {
            StopDrag();
            return;
        }

        if (!TryGetPointerPosition(out Vector2 pointerPos))
            return;

        if (!isDragging && WasPointerPressedThisFrame())
        {
            TryStartDrag(pointerPos);
        }
        else if (isDragging && IsPointerPressed())
        {
            Drag(pointerPos);
        }
        else if (isDragging && WasPointerReleasedThisFrame())
        {
            StopDrag();
        }
    }

    private void TryStartDrag(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.SphereCast(ray, 0.25f, out RaycastHit hit, 1000f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;

        if (hit.rigidbody != rb && hit.collider.gameObject != gameObject)
            return;

        float planeY = TableSurfaceY + dragHoverHeight;
        dragPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

        if (!dragPlane.Raycast(ray, out float enter))
            return;

        Vector3 hitPoint = ray.GetPoint(enter);
        dragOffset = transform.position - hitPoint;

        rb.isKinematic = true;
        isDragging = true;
        AnyAppleDragging = true;
    }

    private void Drag(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!dragPlane.Raycast(ray, out float enter))
            return;

        Vector3 hitPoint = ray.GetPoint(enter);
        transform.position = hitPoint + dragOffset;
    }

    private void StopDrag()
    {
        isDragging = false;
        rb.isKinematic = false;
        AnyAppleDragging = false;
    }

    /// <summary>
    /// Dipanggil dari luar (misal RecycleBin) untuk membatalkan drag
    /// dan mereset flag sebelum objek dihancurkan.
    /// </summary>
    public void CancelDrag()
    {
        isDragging = false;
        AnyAppleDragging = false;
    }

    /// <summary>
    /// Dipanggil oleh AppleBasket agar apel yang baru di-spawn
    /// langsung mulai di-drag mengikuti posisi pointer saat ini.
    /// </summary>
    public void StartDragImmediately(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        dragPlane = new Plane(Vector3.up, transform.position);
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            dragOffset = transform.position - hitPoint;
        }
        else
        {
            dragOffset = Vector3.zero;
        }

        rb.isKinematic = true;
        isDragging = true;
    }

    private bool TryGetPointerPosition(out Vector2 pos)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        pos = default;
        return false;
    }

    private bool IsPointerPressed()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;

        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool WasPointerReleasedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            return true;

        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }
}