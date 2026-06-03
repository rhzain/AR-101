using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RecycleBin : MonoBehaviour
{
    void Awake()
    {
        // Pastikan keranjang bisa mendeteksi tabrakan
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        MathLevel3.DraggableItem draggableItem = other.GetComponentInParent<MathLevel3.DraggableItem>();
        if (draggableItem != null)
        {
            draggableItem.CancelDrag();
            Destroy(draggableItem.gameObject);
            Debug.Log($"Item {draggableItem.itemId} dibuang ke keranjang dan dihancurkan.");
            return;
        }

        // Jika yang menyentuh keranjang adalah apel
        if (other.CompareTag("Apple"))
        {
            // Reset flag drag SEBELUM dihancurkan
            // agar AppleBasket bisa spawn apel baru kembali
            DraggableApple draggable = other.GetComponent<DraggableApple>();
            if (draggable != null) draggable.CancelDrag();

            Destroy(other.gameObject);
            Debug.Log("Apel dikembalikan ke keranjang dan dihancurkan.");
        }
    }
}
