using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstPuzzle
{
    public sealed class HoldPreview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public PuzzleGame game;
        public void OnPointerDown(PointerEventData data) => game.SetPauseMenuPreviewHold(true);
        public void OnPointerUp(PointerEventData data) => game.SetPauseMenuPreviewHold(false);
        public void OnPointerExit(PointerEventData data) => game.SetPauseMenuPreviewHold(false);
        private void OnDisable() { if (game != null && game.preview != null) game.SetPauseMenuPreviewHold(false); }
    }
}
