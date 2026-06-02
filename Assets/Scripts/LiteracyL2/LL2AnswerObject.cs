using UnityEngine;

namespace LiteracyLevel2
{
    public class LL2AnswerObject : MonoBehaviour
    {
        [Header("Identitas Jawaban")]
        public string objectId = "";
        public string displayLabel = "";

        public string Label => string.IsNullOrWhiteSpace(displayLabel) ? objectId : displayLabel;

        [Header("Efek Selected")]
        public Material selectedMaterial;
        public Color selectedColor = new Color(1f, 0.92f, 0.25f, 1f);
        public float selectedScaleMultiplier = 1.05f;

        private Renderer[] renderers;
        private Material[][] normalMaterials;
        private Vector3 normalScale;
        private bool isInitialized;

        public bool IsSelected { get; private set; }

        private void Awake()
        {
            CacheVisualState(false);
        }

        public void InitializeRuntime(string id, string label, Material highlightMaterial, Color highlightColor, float scaleMultiplier)
        {
            objectId = id;

            if (string.IsNullOrWhiteSpace(displayLabel))
                displayLabel = label;

            if (highlightMaterial != null)
            selectedMaterial = highlightMaterial;

            selectedColor = highlightColor;
            selectedScaleMultiplier = scaleMultiplier;
            CacheVisualState(true);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            CacheVisualState(false);

            IsSelected = selected;
            transform.localScale = selected ? normalScale * selectedScaleMultiplier : normalScale;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                    continue;

                if (selected)
                {
                    if (selectedMaterial != null)
                    {
                        Material[] highlighted = new Material[targetRenderer.sharedMaterials.Length];
                        for (int j = 0; j < highlighted.Length; j++)
                            highlighted[j] = selectedMaterial;

                        targetRenderer.materials = highlighted;
                    }
                    else
                    {
                        MaterialPropertyBlock block = new MaterialPropertyBlock();
                        targetRenderer.GetPropertyBlock(block);
                        block.SetColor("_BaseColor", selectedColor);
                        block.SetColor("_Color", selectedColor);
                        targetRenderer.SetPropertyBlock(block);
                    }
                }
                else
                {
                    targetRenderer.SetPropertyBlock(null);

                    if (normalMaterials != null && i < normalMaterials.Length && normalMaterials[i] != null)
                        targetRenderer.materials = normalMaterials[i];
                }
            }
        }

        private void CacheVisualState(bool force)
        {
            if (isInitialized && !force)
                return;

            normalScale = transform.localScale;
            renderers = GetComponentsInChildren<Renderer>(true);
            normalMaterials = new Material[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
                normalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : null;

            isInitialized = true;
        }
    }
}
