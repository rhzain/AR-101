using UnityEngine;
using UnityEngine.InputSystem;

namespace MathLevel3
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class DraggableItem : MonoBehaviour
    {
        private Camera cam;
        private Rigidbody rb;
        private Collider ownCollider;

        private bool isDragging;
        private Vector3 dragOffset;
        private Plane dragPlane;

        public static bool AnyItemDragging { get; private set; } = false;

        [Tooltip("Tinggi permukaan meja/area main dalam world space.")]
        public static float TableSurfaceY { get; set; } = 0f;

        [Header("Identitas Item")]
        [Tooltip("Contoh: Apple, Book, Fish, Candy, Orange. Dipakai DropZone untuk filter item.")]
        public string itemId = "Item";

        [Header("Drag")]
        [Tooltip("Offset tinggi item di atas permukaan saat di-drag.")]
        public float dragHoverHeight = 0.05f;

        [Header("Fisika AR")]
        [Tooltip("Kalau aktif, item tidak jatuh karena gravity dan tetap stabil di atas layout AR.")]
        public bool keepStableOnTable = true;

        [Header("Aturan Drop Zone")]
        [Tooltip("Kalau aktif, item hilang saat dilepas di luar ItemDropZone.")]
        public bool destroyWhenDroppedOutsideDropZone = false;

        [Tooltip("Toleransi area ItemDropZone agar item yang sedikit di tepi tetap dihitung masuk.")]
        public float dropZoneCheckPadding = 0.25f;

        public bool IsDragging => isDragging;

        private void Awake()
        {
            cam = Camera.main;
            rb = GetComponent<Rigidbody>();
            ownCollider = GetComponent<Collider>();

            ApplyStablePhysics();
        }

        private void Update()
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

            if (hit.rigidbody != rb && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                return;

            float planeY = TableSurfaceY + dragHoverHeight;
            dragPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

            if (!dragPlane.Raycast(ray, out float enter))
                return;

            Vector3 hitPoint = ray.GetPoint(enter);
            dragOffset = transform.position - hitPoint;

            rb.isKinematic = true;
            isDragging = true;
            AnyItemDragging = true;
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
            AnyItemDragging = false;

            if (destroyWhenDroppedOutsideDropZone && !IsInsideDropZone())
            {
                Destroy(gameObject);
                return;
            }

            rb.isKinematic = keepStableOnTable;
        }

        public void CancelDrag()
        {
            isDragging = false;
            AnyItemDragging = false;

            if (rb != null)
                rb.isKinematic = keepStableOnTable;
        }

        private void ApplyStablePhysics()
        {
            if (rb == null || !keepStableOnTable)
                return;

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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
            AnyItemDragging = true;
        }

        private bool IsInsideDropZone()
        {
            if (ownCollider == null)
                return false;

            Physics.SyncTransforms();

            if (IsTouchingDropZoneCollider())
                return true;

            ItemDropZone[] dropZones = FindObjectsByType<ItemDropZone>(FindObjectsSortMode.None);
            foreach (ItemDropZone dropZone in dropZones)
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

                if (hit.GetComponentInParent<ItemDropZone>() != null)
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
}
