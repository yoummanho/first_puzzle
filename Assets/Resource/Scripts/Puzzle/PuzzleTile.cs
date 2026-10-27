using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstPuzzle
{
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.RawImage))]
    public sealed class PuzzleTile : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text numberLabel;
        private RectTransform rect;
        private UnityEngine.UI.RawImage image;
        private Action<int> onPressed;
        private Coroutine motion;
        private bool blank;

        public int TileId { get; private set; }
        public bool IsMoving { get; private set; }
        public bool MotionPaused { get; set; }
        public RectTransform Rect => rect;
        public TMP_Text NumberLabel => numberLabel;
        public float NumberOutlineWidth => numberLabel != null && numberLabel.fontSharedMaterial != null && numberLabel.fontSharedMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth) ? numberLabel.fontSharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth) : 0f;

        private void Awake() => CacheComponents();

        private void CacheComponents()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<UnityEngine.UI.RawImage>();
            if (numberLabel == null) numberLabel = GetComponentInChildren<TMP_Text>(true);
        }

        public void Initialize(StageData stage, int tileId, Action<int> pressed)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            Rect uv = stage.GetTileUV(tileId);
            // Inactive prefab instances have not necessarily received Awake yet.
            CacheComponents();
            StopMotion();
            MotionPaused = false;
            TileId = tileId;
            blank = tileId == stage.Columns * stage.Rows - 1;
            onPressed = pressed;
            image.texture = stage.Illustration;
            image.uvRect = uv;
            image.raycastTarget = !blank;
            SetImageAlpha(blank ? 0f : 1f);
            if (numberLabel != null)
            {
                numberLabel.text = (tileId + 1).ToString();
                numberLabel.raycastTarget = false;
                ApplyNumberOutlineStyle();
            }
        }

        public void SetNumbersVisible(bool visible)
        {
            if (numberLabel != null) numberLabel.gameObject.SetActive(visible && !blank);
        }

        public void SetImageAlpha(float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public void SetGeometry(Vector2 position, float side)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(side, side);
            rect.anchoredPosition = position;
            if (numberLabel != null) numberLabel.fontSize = Mathf.Clamp(side * 0.26f, 14f, 44f);
        }

        private static Material sharedOutlineMaterial;

        private void ApplyNumberOutlineStyle()
        {
            if (numberLabel == null) return;
            numberLabel.fontStyle |= FontStyles.Bold;
            numberLabel.color = new Color(1f, 1f, 1f, 0.96f);
            numberLabel.extraPadding = true;

            Material baseMat = numberLabel.fontSharedMaterial != null ? numberLabel.fontSharedMaterial : numberLabel.fontMaterial;
            if (baseMat == null) return;

            if (sharedOutlineMaterial == null || sharedOutlineMaterial.shader != baseMat.shader)
            {
                sharedOutlineMaterial = new Material(baseMat);
                sharedOutlineMaterial.name = baseMat.name + " (TileNumberOutline)";
                sharedOutlineMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                if (sharedOutlineMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
                    sharedOutlineMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
                if (sharedOutlineMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
                    sharedOutlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.24f);
                if (sharedOutlineMaterial.HasProperty(ShaderUtilities.ID_OutlineSoftness))
                    sharedOutlineMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.04f);
                if (sharedOutlineMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
                    sharedOutlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.04f, 0.07f, 0.12f, 0.96f));
            }
            numberLabel.fontSharedMaterial = sharedOutlineMaterial;
        }

        public void MoveTo(Vector2 destination, float duration)
        {
            StopMotion();
            if (!isActiveAndEnabled || duration <= 0f || (rect.anchoredPosition - destination).sqrMagnitude < 0.01f)
            {
                rect.anchoredPosition = destination;
                return;
            }
            IsMoving = true;
            motion = StartCoroutine(Slide(destination, duration));
        }

        private IEnumerator Slide(Vector2 destination, float duration)
        {
            Vector2 origin = rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!MotionPaused)
                {
                    elapsed += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(origin, destination,
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                }
                yield return null;
            }
            rect.anchoredPosition = destination;
            IsMoving = false;
            motion = null;
        }

        private void OnDisable() => StopMotion();

        public void StopMotion()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            IsMoving = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!blank && eventData.button == PointerEventData.InputButton.Left) onPressed?.Invoke(TileId);
        }
    }
}
