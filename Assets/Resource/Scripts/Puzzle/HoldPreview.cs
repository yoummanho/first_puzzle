using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstPuzzle
{
    public sealed class HoldPreview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public PuzzleGame game;
        public void OnPointerDown(PointerEventData data) => game.ShowPreview(true);
        public void OnPointerUp(PointerEventData data) => game.ShowPreview(false);
        public void OnPointerExit(PointerEventData data) => game.ShowPreview(false);
        private void OnDisable() { if (game != null && game.preview != null) game.ShowPreview(false); }
    }
}
