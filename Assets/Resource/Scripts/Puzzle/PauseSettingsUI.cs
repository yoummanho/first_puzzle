using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    /// <summary>Extends the authored pause and result panels with Phase 2/3 settings and Lobby navigation.</summary>
    public static class PauseSettingsUI
    {
        public static void Install(
            PuzzleGame game,
            out TMP_Text bgmLabel,
            out TMP_Text sfxLabel,
            out TMP_Text orientationLabel,
            out Button pauseLobbyButton,
            out Button resultLobbyButton)
        {
            var parent = game.pauseControls.transform;
            var template = game.lineLabel.GetComponentInParent<Button>(true);
            var restart = parent.Find("Restart puzzle").GetComponent<Button>();

            bgmLabel = AddOrFind(template, parent, "Music", game.ToggleBgm);
            sfxLabel = AddOrFind(template, parent, "Sound effects", game.ToggleSfx);
            orientationLabel = AddOrFind(template, parent, "Orientation", game.ToggleOrientation);
            pauseLobbyButton = AddOrFindButton(restart, parent, "Lobby", "Back to lobby", game.OpenLobby);

            var resultParent = game.resultControls.transform;
            var playAgain = resultParent.Find("Play this again").GetComponent<Button>();
            resultLobbyButton = AddOrFindButton(playAgain, resultParent, "Lobby", "Back to lobby", game.OpenLobby);

            ApplyLayout(game, PlayOrientation.Portrait);
        }

        public static void ApplyLayout(PuzzleGame game, PlayOrientation orientation)
        {
            if (game == null || game.pauseControls == null || game.resultControls == null) return;

            var parent = (RectTransform)game.pauseControls.transform;
            var lineBtn = game.lineLabel.GetComponentInParent<Button>(true);
            var animBtn = game.animationLabel.GetComponentInParent<Button>(true);
            var musicBtn = parent.Find("Music")?.GetComponent<Button>();
            var sfxBtn = parent.Find("Sound effects")?.GetComponent<Button>();
            var orientBtn = parent.Find("Orientation")?.GetComponent<Button>();
            var restartBtn = parent.Find("Restart puzzle")?.GetComponent<Button>();
            var lobbyBtn = parent.Find("Lobby")?.GetComponent<Button>();

            var ordered = new[]
            {
                game.resumeButton,
                musicBtn,
                sfxBtn,
                lineBtn,
                animBtn,
                orientBtn,
                restartBtn,
                lobbyBtn
            };

            if (orientation == PlayOrientation.Landscape)
            {
                game.message.rectTransform.anchorMin = new Vector2(.08f, .74f);
                game.message.rectTransform.anchorMax = new Vector2(.92f, .94f);
                game.message.rectTransform.offsetMin = game.message.rectTransform.offsetMax = Vector2.zero;

                parent.anchorMin = new Vector2(.10f, .08f);
                parent.anchorMax = new Vector2(.90f, .70f);
                parent.offsetMin = parent.offsetMax = Vector2.zero;

                int rows = 4;
                for (int i = 0; i < ordered.Length; i++)
                {
                    if (ordered[i] == null) continue;
                    int col = i % 2;
                    int row = i / 2;
                    var rect = (RectTransform)ordered[i].transform;
                    float x0 = col == 0 ? 0f : 0.515f;
                    float x1 = col == 0 ? 0.485f : 1f;
                    float y1 = 1f - row / (float)rows - 0.018f;
                    float y0 = 1f - (row + 1) / (float)rows + 0.018f;
                    rect.anchorMin = new Vector2(x0, y0);
                    rect.anchorMax = new Vector2(x1, y1);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }

                var resultRect = (RectTransform)game.resultControls.transform;
                resultRect.anchorMin = new Vector2(.18f, .10f);
                resultRect.anchorMax = new Vector2(.82f, .52f);
                resultRect.offsetMin = resultRect.offsetMax = Vector2.zero;
            }
            else
            {
                game.message.rectTransform.anchorMin = new Vector2(.08f, .68f);
                game.message.rectTransform.anchorMax = new Vector2(.92f, .90f);
                game.message.rectTransform.offsetMin = game.message.rectTransform.offsetMax = Vector2.zero;

                parent.anchorMin = new Vector2(.10f, .06f);
                parent.anchorMax = new Vector2(.90f, .65f);
                parent.offsetMin = parent.offsetMax = Vector2.zero;

                int count = ordered.Length;
                for (int i = 0; i < count; i++)
                {
                    if (ordered[i] == null) continue;
                    var rect = (RectTransform)ordered[i].transform;
                    rect.anchorMin = new Vector2(0f, 1f - (i + 1f) / count + .008f);
                    rect.anchorMax = new Vector2(1f, 1f - i / (float)count - .008f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }

                var resultRect = (RectTransform)game.resultControls.transform;
                resultRect.anchorMin = new Vector2(.12f, .14f);
                resultRect.anchorMax = new Vector2(.88f, .54f);
                resultRect.offsetMin = resultRect.offsetMax = Vector2.zero;
            }

            var resultButtons = new[]
            {
                game.nextButton,
                game.resultControls.transform.Find("Play this again")?.GetComponent<Button>(),
                game.resultControls.transform.Find("Lobby")?.GetComponent<Button>()
            };
            for (int i = 0; i < resultButtons.Length; i++)
            {
                if (resultButtons[i] == null) continue;
                var rect = (RectTransform)resultButtons[i].transform;
                rect.anchorMin = new Vector2(0f, 1f - (i + 1f) / resultButtons.Length + 0.025f);
                rect.anchorMax = new Vector2(1f, 1f - i / (float)resultButtons.Length - 0.025f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
        }

        private static TMP_Text AddOrFind(Button template, Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var existing = parent.Find(name);
            var button = existing != null ? existing.GetComponent<Button>() : Object.Instantiate(template, parent);
            button.name = name;
            // Replace the cloned persistent callback, not just its runtime listeners.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            return button.GetComponentInChildren<TMP_Text>(true);
        }

        private static Button AddOrFindButton(Button template, Transform parent, string name, string label, UnityEngine.Events.UnityAction action)
        {
            var existing = parent.Find(name);
            var button = existing != null ? existing.GetComponent<Button>() : Object.Instantiate(template, parent);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
            return button;
        }
    }
}
