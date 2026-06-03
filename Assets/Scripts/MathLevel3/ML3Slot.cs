using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MathLevel3
{
    public class ML3Slot : MonoBehaviour
    {
        [Header("Object Tetap di Layout")]
        public GameObject tableModel;
        public GameObject basketModel;
        public Transform characterAnchor;
        public TMP_Text labelText;
        public Transform spawnArea;
        public ItemDropZone dropZone;

        private readonly List<GameObject> runtimeObjects = new List<GameObject>();

        private void Reset()
        {
            AutoAssignChildren();
        }

        private void Awake()
        {
            AutoAssignChildren();
        }

        [ContextMenu("Auto Assign Children")]
        public void AutoAssignChildren()
        {
            Transform table = transform.Find("TableModel");
            Transform basket = FindFirstChild("BasketModel", "Keranjang", "Basket");
            Transform character = transform.Find("CharacterAnchor");
            Transform label = transform.Find("LabelAnchor");
            Transform spawn = transform.Find("SpawnArea");
            Transform zone = transform.Find("DropZone");

            if (tableModel == null && table != null) tableModel = table.gameObject;
            if (basketModel == null && basket != null) basketModel = basket.gameObject;
            if (characterAnchor == null && character != null) characterAnchor = character;
            if (spawnArea == null && spawn != null) spawnArea = spawn;

            if (labelText == null && label != null)
                labelText = label.GetComponentInChildren<TMP_Text>(true);

            if (dropZone == null && zone != null)
            {
                dropZone = zone.GetComponent<ItemDropZone>();
                if (dropZone == null)
                    dropZone = zone.gameObject.AddComponent<ItemDropZone>();
            }
        }

        public int ItemCount => dropZone != null ? dropZone.ItemCount : 0;

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void Setup(
            string label,
            GameObject characterPrefab,
            string acceptedItemId,
            ItemDropZone.ZoneMode zoneMode = ItemDropZone.ZoneMode.CountItems,
            bool showTableModel = true)
        {
            ClearRuntimeObjects();

            if (tableModel != null)
                tableModel.SetActive(showTableModel);

            if (basketModel != null)
                basketModel.SetActive(!showTableModel);

            if (labelText != null)
                labelText.text = label;

            if (dropZone != null)
            {
                dropZone.mode = zoneMode;
                dropZone.acceptedItemId = acceptedItemId;
                dropZone.ClearItems(false);
            }

            SpawnRuntimeObject(characterPrefab, characterAnchor);
        }

        public void SpawnRuntimeObject(GameObject prefab, Transform anchor)
        {
            if (prefab == null || anchor == null)
                return;

            GameObject instance = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            instance.SetActive(true);
            runtimeObjects.Add(instance);
        }

        public void TrackRuntimeObject(GameObject instance)
        {
            if (instance != null)
                runtimeObjects.Add(instance);
        }

        public void ClearRuntimeObjects()
        {
            foreach (GameObject runtimeObject in runtimeObjects)
            {
                if (runtimeObject != null)
                    Destroy(runtimeObject);
            }

            runtimeObjects.Clear();

            if (dropZone != null)
                dropZone.ClearItems(false);
        }

        private Transform FindFirstChild(params string[] childNames)
        {
            foreach (string childName in childNames)
            {
                Transform child = transform.Find(childName);
                if (child != null)
                    return child;
            }

            return null;
        }
    }
}
