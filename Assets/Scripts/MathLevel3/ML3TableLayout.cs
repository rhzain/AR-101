using UnityEngine;

namespace MathLevel3
{
    public enum ML3SlotId
    {
        None,
        Left,
        Center,
        Right
    }

    public class ML3TableLayout : MonoBehaviour
    {
        [Header("Slot")]
        public ML3Slot leftSlot;
        public ML3Slot centerSlot;
        public ML3Slot rightSlot;

        [Header("AR Drag Plane")]
        [Tooltip("Optional. Kalau diisi, Y posisi ini dipakai sebagai tinggi permukaan drag item.")]
        public Transform tableSurfaceReference;

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
            ML3Slot foundLeft = FindSlot("LeftSlot");
            ML3Slot foundCenter = FindSlot("CenterSlot");
            ML3Slot foundRight = FindSlot("RightSlot");

            if (leftSlot == null) leftSlot = foundLeft;
            if (centerSlot == null) centerSlot = foundCenter;
            if (rightSlot == null) rightSlot = foundRight;
        }

        public ML3Slot GetSlot(ML3SlotId slotId)
        {
            switch (slotId)
            {
                case ML3SlotId.Left:
                    return leftSlot;
                case ML3SlotId.Center:
                    return centerSlot;
                case ML3SlotId.Right:
                    return rightSlot;
                default:
                    return null;
            }
        }

        public void ClearRuntimeObjects()
        {
            if (leftSlot != null) leftSlot.ClearRuntimeObjects();
            if (centerSlot != null) centerSlot.ClearRuntimeObjects();
            if (rightSlot != null) rightSlot.ClearRuntimeObjects();
        }

        public void ApplyTableSurface()
        {
            if (tableSurfaceReference != null)
                DraggableItem.TableSurfaceY = tableSurfaceReference.position.y;
        }

        private ML3Slot FindSlot(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return null;

            ML3Slot slot = child.GetComponent<ML3Slot>();
            if (slot == null)
                slot = child.gameObject.AddComponent<ML3Slot>();

            slot.AutoAssignChildren();
            return slot;
        }
    }
}
