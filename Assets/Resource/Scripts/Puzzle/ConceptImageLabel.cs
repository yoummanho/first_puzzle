using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Keeps the existing state label as a binding, displaying authored image lettering when available.</summary>
    public sealed class ConceptImageLabel : MonoBehaviour
    {
        private TMP_Text source;
        private UnityEngine.UI.Image picture;
        private string lastText;
        public void Initialize(TMP_Text label)
        {
            source = label;
            picture = ConceptTheme.Art("ImageLettering", transform, null, 0, 0, 1, 1);
            Refresh();
        }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (source == null || source.text == lastText) return;
            lastText = source.text;
            string key = "label_" + Regex.Replace(lastText.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
            var sprite = ConceptTheme.Load(key);
            picture.sprite = sprite;
            picture.enabled = sprite != null;
            source.enabled = sprite == null;
        }
    }
}
