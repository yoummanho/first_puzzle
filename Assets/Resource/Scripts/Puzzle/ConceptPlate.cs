using UnityEngine;
namespace FirstPuzzle
{
    public sealed class ConceptPlate : MonoBehaviour
    {
        public RectTransform target;
        private void LateUpdate()
        {
            if(target==null)return;
            var rect=(RectTransform)transform;
            rect.anchorMin=target.anchorMin;rect.anchorMax=target.anchorMax;
            rect.offsetMin=target.offsetMin;rect.offsetMax=target.offsetMax;
        }
    }
}
