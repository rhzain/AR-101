using System.Collections.Generic;
using UnityEngine;

namespace MathLevel3
{
    [RequireComponent(typeof(Collider))]
    public class ItemDropZone : MonoBehaviour
    {
        public enum ZoneMode
        {
            CountItems,
            RemoveItems
        }

        [Header("Mode")]
        public ZoneMode mode = ZoneMode.CountItems;

        [Header("Filter")]
        [Tooltip("Kosongkan untuk menerima semua item. Isi contoh: Apple, Book, Fish, Candy, Orange.")]
        public string acceptedItemId = "";

        private readonly HashSet<DraggableItem> itemsInZone = new HashSet<DraggableItem>();

        public int ItemCount
        {
            get
            {
                itemsInZone.RemoveWhere(item => item == null);
                return itemsInZone.Count;
            }
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            DraggableItem item = other.GetComponentInParent<DraggableItem>();
            if (item == null || !Accepts(item))
                return;

            if (mode == ZoneMode.RemoveItems)
            {
                item.CancelDrag();
                Destroy(item.gameObject);
                Debug.Log($"[ItemDropZone] Item {item.itemId} dihapus oleh {name}.");
                return;
            }

            itemsInZone.Add(item);
            Debug.Log($"[ItemDropZone] {item.itemId} masuk {name}. Total: {ItemCount}");
        }

        private void OnTriggerExit(Collider other)
        {
            DraggableItem item = other.GetComponentInParent<DraggableItem>();
            if (item == null)
                return;

            itemsInZone.Remove(item);
            Debug.Log($"[ItemDropZone] {item.itemId} keluar {name}. Total: {ItemCount}");
        }

        public void ClearItems(bool destroyItems)
        {
            List<DraggableItem> toClear = new List<DraggableItem>(itemsInZone);

            if (destroyItems)
            {
                foreach (DraggableItem item in toClear)
                {
                    if (item != null)
                        Destroy(item.gameObject);
                }
            }

            itemsInZone.Clear();
        }

        private bool Accepts(DraggableItem item)
        {
            return string.IsNullOrWhiteSpace(acceptedItemId) || item.itemId == acceptedItemId;
        }
    }
}
