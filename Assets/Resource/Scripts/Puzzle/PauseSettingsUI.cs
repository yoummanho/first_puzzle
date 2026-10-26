using TMPro;
using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Extends the authored pause panel, preserving its buttons and visual styling.</summary>
    public static class PauseSettingsUI
    {
        public static void Install(PuzzleGame game, out TMP_Text bgmLabel, out TMP_Text sfxLabel)
        {
            var parent = game.pauseControls.transform;
            var template = game.lineLabel.GetComponentInParent<UnityEngine.UI.Button>(true);
            var restart = parent.Find("Restart puzzle").GetComponent<UnityEngine.UI.Button>();
            bgmLabel = Add(template, parent, "Music", game.ToggleBgm);
            sfxLabel = Add(template, parent, "Sound effects", game.ToggleSfx);
            var ordered = new[] { game.resumeButton, bgmLabel.GetComponentInParent<UnityEngine.UI.Button>(true),
                sfxLabel.GetComponentInParent<UnityEngine.UI.Button>(true), template,
                game.animationLabel.GetComponentInParent<UnityEngine.UI.Button>(true), restart };
            var panel = (RectTransform)parent;
            panel.anchorMin = new Vector2(.12f, .08f);
            panel.anchorMax = new Vector2(.88f, .59f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            game.message.rectTransform.anchorMin = new Vector2(.08f, .63f);
            for (int i = 0; i < ordered.Length; i++)
            {
                var rect = (RectTransform)ordered[i].transform;
                rect.anchorMin = new Vector2(0, 1f - (i + 1) / 6f + .012f);
                rect.anchorMax = new Vector2(1, 1f - i / 6f - .012f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
        }

        private static TMP_Text Add(UnityEngine.UI.Button template, Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var button = Object.Instantiate(template, parent);
            button.name = name;
            // Replace the cloned persistent callback, not just its runtime listeners.
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            return button.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
