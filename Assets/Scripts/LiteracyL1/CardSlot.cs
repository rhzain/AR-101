using UnityEngine;

/// <summary>
/// Slot target di area jawaban. Kartu bisa di-snap ke slot ini.
/// Pasang script ini di objek SlotPrefab yang ada di sisi kanan meja.
/// </summary>
public class CardSlot : MonoBehaviour
{
    [HideInInspector] public int slotIndex; // Urutan slot (0, 1, 2, ...)

    private DraggableCard occupant = null;
    private Renderer slotRenderer;

    [Header("Visual")]
    public Color emptyColor  = new Color(1f, 1f, 1f, 0.3f);
    public Color filledColor = new Color(0.4f, 0.9f, 0.4f, 0.6f);

    void Awake()
    {
        slotRenderer = GetComponent<Renderer>();
        UpdateVisual();
    }

    // ─── Properties ───────────────────────────────────────────
    public bool IsEmpty => occupant == null;
    public DraggableCard Occupant => occupant;

    /// <summary>Isi kartu di slot ini dalam huruf kapital. Kosong jika tidak ada kartu.</summary>
    public string GetContent() => occupant != null ? occupant.content.ToUpper() : "";

    // ─── Slot Management ──────────────────────────────────────
    public void PlaceCard(DraggableCard card)
    {
        // Kalau slot sudah ada kartu lain, kembalikan ke source
        if (occupant != null && occupant != card)
            occupant.ReturnToSource();

        occupant = card;

        // Snap kartu ke posisi tengah slot
        card.transform.position = transform.position + Vector3.up * 0.005f;

        UpdateVisual();
        Debug.Log($"Slot {slotIndex} diisi oleh: '{card.content}'");
    }

    public void ClearSlot()
    {
        occupant = null;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (slotRenderer != null)
            slotRenderer.material.color = IsEmpty ? emptyColor : filledColor;
    }

    // Gizmos untuk memudahkan posisi slot di Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = IsEmpty ? Color.white : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
