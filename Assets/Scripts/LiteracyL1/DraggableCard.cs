using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Kartu huruf/kata 3D yang bisa di-drag di atas meja AR.
/// Saat dilepas, otomatis snap ke CardSlot terdekat atau kembali ke posisi asal.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DraggableCard : MonoBehaviour
{
    [HideInInspector] public string content;       // Isi kartu (huruf/kata)
    [HideInInspector] public Vector3 sourcePosition; // Posisi awal di source area

    [Header("Snap Settings")]
    [Tooltip("Jarak maksimum agar kartu bisa snap ke slot terdekat")]
    public float snapRadius = 0.12f;

    [Header("Text Layout")]
    [SerializeField] private float textPaddingX = 0.04f;
    [SerializeField] private float textPaddingZ = 0.02f;
    [SerializeField] private float textFontSizeMin = 0.01f;
    [SerializeField] private float textFontSizeMax = 2f;

    /// <summary>Lebar kartu dalam meter (diset oleh LiteracyGameManager).</summary>
    public float CardWidth { get; private set; }

    /// <summary>Set ukuran kartu. Width = lebar (X), Depth = panjang (Z).
    /// Y (ketebalan) tetap dari prefab.</summary>
    public void ResizeToWidth(float width, float depth)
    {
        CardWidth  = width;
        snapRadius = Mathf.Max(width, depth) * 1.2f;
        float y    = transform.localScale.y;
        transform.localScale = new Vector3(width, y, depth);
        ApplyTextLayout(width, depth);
    }

    // ─── State ────────────────────────────────────────────────
    private Camera cam;
    private Rigidbody rb;
    private CardSlot currentSlot;
    private TextMeshPro cardText;
    private RectTransform cardTextRect;
    private bool isDragging;
    private Plane dragPlane;

    public static bool AnyCardDragging { get; private set; } = false;
    public static float TableSurfaceY  { get; set; } = 0f;

    [Tooltip("Tinggi kartu mengambang di atas meja saat di-drag")]
    public float dragHoverHeight = 0.03f;

    // ─── Init ─────────────────────────────────────────────────
    void Awake()
    {
        cam = Camera.main;
        rb  = GetComponent<Rigidbody>();

        // Kartu selalu kinematic (tidak perlu gravity)
        rb.isKinematic = true;
        rb.useGravity  = false;

        cardText = GetComponentInChildren<TextMeshPro>();
        if (cardText != null)
            cardTextRect = cardText.rectTransform;
    }

    /// <summary>Inisialisasi isi kartu dan posisi awal di source area.</summary>
    public void Initialize(string cardContent, Vector3 spawnPos)
    {
        content        = cardContent.ToUpper();
        sourcePosition = spawnPos;

        if (cardText != null)
        {
            cardText.text = content;
            ApplyTextLayout(transform.localScale.x, transform.localScale.z);
        }

        transform.position = spawnPos;
    }

    private void ApplyTextLayout(float width, float depth)
    {
        if (cardText == null) return;
        if (cardTextRect == null) cardTextRect = cardText.rectTransform;

        float scaleX = Mathf.Max(Mathf.Abs(transform.localScale.x), 0.0001f);
        float scaleZ = Mathf.Max(Mathf.Abs(transform.localScale.z), 0.0001f);

        // TMP adalah child dari kartu, jadi lawan skala parent agar glyph tidak stretch.
        cardText.transform.localScale = new Vector3(1f / scaleX, 1f / scaleZ, 1f);

        float textWidth  = Mathf.Max(0.01f, width - textPaddingX * 2f);
        float textHeight = Mathf.Max(0.01f, depth - textPaddingZ * 2f);

        cardTextRect.sizeDelta = new Vector2(textWidth, textHeight);
        cardText.enableAutoSizing = true;
        cardText.fontSizeMin = textFontSizeMin;
        cardText.fontSizeMax = textFontSizeMax;
        cardText.enableWordWrapping = false;
        cardText.overflowMode = TextOverflowModes.Ellipsis;
        cardText.alignment = TextAlignmentOptions.Center;
        cardText.ForceMeshUpdate();
    }

    // ─── Update ───────────────────────────────────────────────
    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Cek release LEBIH DULU sebelum cek posisi pointer.
        // Di mobile, saat jari dilepas touch.isPressed sudah false sehingga
        // TryGetPointerPosition() gagal → StopDrag() tidak pernah dipanggil.
        if (isDragging && WasPointerReleasedThisFrame())
        {
            StopDrag();
            return;
        }

        if (!TryGetPointerPosition(out Vector2 pointerPos)) return;

        if (!isDragging && WasPointerPressedThisFrame())
            TryStartDrag(pointerPos);
        else if (isDragging && IsPointerPressed())
            Drag(pointerPos);
    }

    // ─── Drag Logic ───────────────────────────────────────────
    private void TryStartDrag(Vector2 screenPos)
    {
        if (AnyCardDragging) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.SphereCast(ray, 0.05f, out RaycastHit hit, 1000f,
            Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;

        // Pastikan yang diklik adalah kartu ini (atau child-nya)
        if (hit.collider.gameObject != gameObject &&
            !hit.collider.transform.IsChildOf(transform))
            return;

        float planeY = TableSurfaceY + dragHoverHeight;
        dragPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

        isDragging     = true;
        AnyCardDragging = true;

        // Lepaskan dari slot saat mulai drag
        if (currentSlot != null)
        {
            currentSlot.ClearSlot();
            currentSlot = null;
        }
    }

    private void Drag(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!dragPlane.Raycast(ray, out float enter)) return;

        transform.position = ray.GetPoint(enter);
    }

    private void StopDrag()
    {
        isDragging      = false;
        AnyCardDragging = false;

        // Coba snap ke slot terdekat
        CardSlot nearest = FindNearestSlot();
        if (nearest != null)
        {
            currentSlot = nearest;
            nearest.PlaceCard(this);
        }
        else
        {
            // Tidak ada slot terdekat → kembalikan ke source
            ReturnToSource();
        }
    }

    // ─── Helpers ──────────────────────────────────────────────
    private CardSlot FindNearestSlot()
    {
        CardSlot[] slots = FindObjectsByType<CardSlot>(FindObjectsSortMode.None);
        CardSlot nearest = null;
        float minDist = snapRadius;

        foreach (CardSlot slot in slots)
        {
            float dist = Vector3.Distance(transform.position, slot.transform.position);
            // Slot bisa dipilih jika kosong ATAU sudah ditempati kartu ini sendiri
            if (dist < minDist && (slot.IsEmpty || slot.Occupant == this))
            {
                minDist = dist;
                nearest = slot;
            }
        }
        return nearest;
    }

    public void ReturnToSource()
    {
        if (currentSlot != null)
        {
            currentSlot.ClearSlot();
            currentSlot = null;
        }
        transform.position = sourcePosition;
    }

    public void CancelDrag()
    {
        isDragging      = false;
        AnyCardDragging = false;
    }

    // ─── Input Helpers ────────────────────────────────────────
    private bool TryGetPointerPosition(out Vector2 pos)
    {
        // Touchscreen: baca posisi terakhir baik saat pressed maupun tidak
        // (ReadValue() tetap valid meski jari sudah diangkat di frame yang sama)
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.isPressed || touch.press.wasReleasedThisFrame)
            {
                pos = touch.position.ReadValue();
                return true;
            }
        }
        if (Mouse.current != null)
        { pos = Mouse.current.position.ReadValue(); return true; }
        pos = default; return false;
    }

    private bool IsPointerPressed()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool WasPointerReleasedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }
}
