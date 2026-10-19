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

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<UnityEngine.UI.RawImage>();
            if (numberLabel == null) numberLabel = GetComponentInChildren<TMP_Text>(true);
        }

        public void Initialize(StageData stage, int tileId, Action<int> pressed)
        {
            TileId = tileId;
            blank = tileId == stage.Columns * stage.Rows - 1;
            onPressed = pressed;
            image.texture = stage.Illustration;
            image.uvRect = stage.GetTileUV(tileId);
            image.raycastTarget = !blank;
            SetImageAlpha(blank ? 0f : 1f);
            if (numberLabel != null)
            {
                numberLabel.text = (tileId + 1).ToString();
                numberLabel.raycastTarget = false;
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
            if (numberLabel != null) numberLabel.fontSize = Mathf.Clamp(side * 0.24f, 12f, 40f);
        }

        public void MoveTo(Vector2 destination, float duration)
        {
            StopMotion();
            if (duration <= 0f || (rect.anchoredPosition - destination).sqrMagnitude < 0.01f)
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
