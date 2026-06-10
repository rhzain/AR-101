using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DraggableApple : MonoBehaviour
{
    private Camera cam;
    private Rigidbody rb;
    private Collider ownCollider;
    private Collider[] ownColliders;
    private readonly List<Collider> ignoredAppleColliders = new List<Collider>();

    private bool isDragging;
    private Vector3 dragOffset;
    private Plane dragPlane;

    // Flag statis: memberitahu script lain (AppleBasket) bahwa ada apel yang sedang di-drag
    public static bool AnyAppleDragging { get; private set; } = false;

    // Ketinggian permukaan meja dalam world space — diset oleh ML2GameManager setelah meja di-spawn
    public static float TableSurfaceY { get; set; } = 0f;

    [Tooltip("Offset tinggi apel di atas permukaan meja saat di-drag")]
    public float dragHoverHeight = 0.05f;

    [Header("Drag Physics")]
    [Tooltip("Cegah apel yang sedang di-drag mendorong apel lain sampai terlempar.")]
    public bool ignoreAppleCollisionsWhileDragging = true;

    [Tooltip("Jarak kecil untuk menggeser apel keluar dari overlap sebelum collision dinyalakan kembali.")]
    public float overlapResolvePadding = 0.01f;

    [Header("Aturan Drop Zone")]
    [Tooltip("Kalau aktif, apel langsung hilang saat dilepas di luar DropZone.")]
    public bool destroyWhenDroppedOutsideDropZone = false;

    [Tooltip("Toleransi area DropZone agar apel yang berada sedikit di atas/tepi DropZone tetap dihitung masuk.")]
    public float dropZoneCheckPadding = 0.25f;

    void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
        ownCollider = GetComponent<Collider>();
        ownColliders = GetComponentsInChildren<Collider>();
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

        BeginDragPhysics();
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
        AnyAppleDragging = false;

        if (destroyWhenDroppedOutsideDropZone && !IsInsideDropZone())
        {
            RestoreIgnoredAppleCollisions();
            Destroy(gameObject);
            return;
        }

        ResolveAppleOverlaps();
        RestoreIgnoredAppleCollisions();
        rb.isKinematic = false;
    }

    /// <summary>
    /// Dipanggil dari luar (misal RecycleBin) untuk membatalkan drag
    /// dan mereset flag sebelum objek dihancurkan.
    /// </summary>
    public void CancelDrag()
    {
        isDragging = false;
        AnyAppleDragging = false;
        RestoreIgnoredAppleCollisions();
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

        BeginDragPhysics();
    }

    private void BeginDragPhysics()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        isDragging = true;
        AnyAppleDragging = true;
        IgnoreAppleCollisions();
    }

    private void IgnoreAppleCollisions()
    {
        if (!ignoreAppleCollisionsWhileDragging || ownColliders == null)
            return;

        RestoreIgnoredAppleCollisions();

        GameObject[] apples = GameObject.FindGameObjectsWithTag("Apple");
        foreach (GameObject apple in apples)
        {
            if (apple == null || apple == gameObject)
                continue;

            Collider[] appleColliders = apple.GetComponentsInChildren<Collider>();
            foreach (Collider appleCollider in appleColliders)
            {
                if (appleCollider == null || !appleCollider.enabled)
                    continue;

                bool ignoredAnyPair = false;
                foreach (Collider own in ownColliders)
                {
                    if (own == null || !own.enabled || own == appleCollider)
                        continue;

                    Physics.IgnoreCollision(own, appleCollider, true);
                    ignoredAnyPair = true;
                }

                if (ignoredAnyPair && !ignoredAppleColliders.Contains(appleCollider))
                    ignoredAppleColliders.Add(appleCollider);
            }
        }
    }

    private void RestoreIgnoredAppleCollisions()
    {
        if (ownColliders == null || ignoredAppleColliders.Count == 0)
            return;

        foreach (Collider appleCollider in ignoredAppleColliders)
        {
            if (appleCollider == null)
                continue;

            foreach (Collider own in ownColliders)
            {
                if (own == null || own == appleCollider)
                    continue;

                Physics.IgnoreCollision(own, appleCollider, false);
            }
        }

        ignoredAppleColliders.Clear();
    }

    private void ResolveAppleOverlaps()
    {
        if (ownCollider == null || ownCollider.isTrigger)
            return;

        const int maxIterations = 5;

        for (int i = 0; i < maxIterations; i++)
        {
            bool moved = false;
            Physics.SyncTransforms();

            GameObject[] apples = GameObject.FindGameObjectsWithTag("Apple");
            foreach (GameObject apple in apples)
            {
                if (apple == null || apple == gameObject)
                    continue;

                Collider[] appleColliders = apple.GetComponentsInChildren<Collider>();
                foreach (Collider appleCollider in appleColliders)
                {
                    if (appleCollider == null || !appleCollider.enabled || appleCollider.isTrigger)
                        continue;

                    if (!Physics.ComputePenetration(
                        ownCollider,
                        ownCollider.transform.position,
                        ownCollider.transform.rotation,
                        appleCollider,
                        appleCollider.transform.position,
                        appleCollider.transform.rotation,
                        out Vector3 direction,
                        out float distance))
                    {
                        continue;
                    }

                    Vector3 horizontalDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
                    if (horizontalDirection.sqrMagnitude < 0.0001f)
                        horizontalDirection = Vector3.ProjectOnPlane(transform.position - appleCollider.transform.position, Vector3.up);

                    if (horizontalDirection.sqrMagnitude < 0.0001f)
                        horizontalDirection = transform.right;

                    transform.position += horizontalDirection.normalized * (distance + overlapResolvePadding);
                    moved = true;
                }
            }

            if (!moved)
                break;
        }
    }

    private bool IsInsideDropZone()
    {
        if (ownCollider == null)
            return false;

        Physics.SyncTransforms();

        if (IsTouchingDropZoneCollider())
            return true;

        DropZone[] dropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        foreach (DropZone dropZone in dropZones)
        {
            Collider dropZoneCollider = dropZone.GetComponent<Collider>();
            if (dropZoneCollider == null || !dropZoneCollider.enabled || !dropZoneCollider.gameObject.activeInHierarchy)
                continue;

            Bounds dropZoneBounds = dropZoneCollider.bounds;
            dropZoneBounds.Expand(dropZoneCheckPadding);

            if (dropZoneBounds.Contains(ownCollider.bounds.center))
                return true;
        }

        return false;
    }

    private bool IsTouchingDropZoneCollider()
    {
        Bounds bounds = ownCollider.bounds;
        Collider[] hits = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            Quaternion.identity,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            if (hit == ownCollider || hit.transform.IsChildOf(transform))
                continue;

            if (hit.GetComponentInParent<DropZone>() != null)
                return true;
        }

        return false;
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
