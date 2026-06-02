using UnityEngine;

namespace LiteracyLevel2
{
    public class LL2AnswerLayout : MonoBehaviour
    {
        [Header("Slot Jawaban")]
        public Transform[] smallSlots;
        public Transform[] mediumSlots;
        public Transform[] largeSlots;

        private void Reset()
        {
            AutoAssignChildren();
        }

        private void Awake()
        {
            AutoAssignChildren();
        }

        [ContextMenu("Auto Assign Answer Slots")]
        public void AutoAssignChildren()
        {
            if (smallSlots == null || smallSlots.Length == 0)
                smallSlots = GetChildrenOf("AnswerArea/SmallAnswerSlots", "SmallAnswerSlots");

            if (mediumSlots == null || mediumSlots.Length == 0)
                mediumSlots = GetChildrenOf("AnswerArea/MediumAnswerSlots", "MediumAnswerSlots");

            if (largeSlots == null || largeSlots.Length == 0)
                largeSlots = GetChildrenOf("AnswerArea/LargeAnswerSlots", "LargeAnswerSlots");
        }

        public Transform[] GetSlots(LL2AnswerSlotType slotType)
        {
            switch (slotType)
            {
                case LL2AnswerSlotType.Small:
                    return smallSlots;
                case LL2AnswerSlotType.Medium:
                    return mediumSlots;
                case LL2AnswerSlotType.Large:
                    return largeSlots;
                default:
                    return null;
            }
        }

        public void ShowSlotGroup(LL2AnswerSlotType slotType)
        {
            gameObject.SetActive(true);
            SetPathActive("AnswerArea", true);
            SetGroupActive(slotType == LL2AnswerSlotType.Small, "AnswerArea/SmallAnswerSlots", "SmallAnswerSlots");
            SetGroupActive(slotType == LL2AnswerSlotType.Medium, "AnswerArea/MediumAnswerSlots", "MediumAnswerSlots");
            SetGroupActive(slotType == LL2AnswerSlotType.Large, "AnswerArea/LargeAnswerSlots", "LargeAnswerSlots");

            SetSlotsActive(smallSlots, slotType == LL2AnswerSlotType.Small);
            SetSlotsActive(mediumSlots, slotType == LL2AnswerSlotType.Medium);
            SetSlotsActive(largeSlots, slotType == LL2AnswerSlotType.Large);
        }

        public void HideAllSlotGroups()
        {
            SetGroupActive(false, "AnswerArea/SmallAnswerSlots", "SmallAnswerSlots");
            SetGroupActive(false, "AnswerArea/MediumAnswerSlots", "MediumAnswerSlots");
            SetGroupActive(false, "AnswerArea/LargeAnswerSlots", "LargeAnswerSlots");

            SetSlotsActive(smallSlots, false);
            SetSlotsActive(mediumSlots, false);
            SetSlotsActive(largeSlots, false);
        }

        private Transform[] GetChildrenOf(params string[] paths)
        {
            Transform parent = null;

            foreach (string path in paths)
            {
                parent = transform.Find(path);
                if (parent != null)
                    break;
            }

            if (parent == null)
                return new Transform[0];

            Transform[] children = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
                children[i] = parent.GetChild(i);

            return children;
        }

        private void SetGroupActive(bool isActive, params string[] paths)
        {
            Transform group = FindFirstPath(paths);
            if (group == null)
                return;

            group.gameObject.SetActive(isActive);

            for (int i = 0; i < group.childCount; i++)
                group.GetChild(i).gameObject.SetActive(isActive);
        }

        private void SetPathActive(string path, bool isActive)
        {
            Transform target = transform.Find(path);
            if (target != null)
                target.gameObject.SetActive(isActive);
        }

        private Transform FindFirstPath(params string[] paths)
        {
            foreach (string path in paths)
            {
                Transform found = transform.Find(path);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void SetSlotsActive(Transform[] slots, bool isActive)
        {
            if (slots == null)
                return;

            foreach (Transform slot in slots)
            {
                if (slot == null)
                    continue;

                if (isActive)
                    ActivateParentsUntilLayout(slot);

                slot.gameObject.SetActive(isActive);
            }
        }

        private void ActivateParentsUntilLayout(Transform child)
        {
            Transform current = child;
            while (current != null)
            {
                current.gameObject.SetActive(true);

                if (current == transform)
                    break;

                current = current.parent;
            }
        }
    }
}
