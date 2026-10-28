using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    public enum PauseTab
    {
        Game = 0,
        System = 1
    }

    /// <summary>Configures the Pause modal with GAME/SYSTEM tabs, an X close button, and the Hamburger icon on the HUD Pause button.</summary>
    public static class PauseSettingsUI
    {
        private static readonly Color TabActiveColor = ConceptTheme.Cyan;
        private static readonly Color TabInactiveColor = ConceptTheme.Locked;
        private static readonly Color CloseBtnColor = ConceptTheme.Pink;
        private static readonly Color IconColor = ConceptTheme.Ink;

        public static void Install(
            PuzzleGame game,
            out TMP_Text bgmLabel,
            out TMP_Text sfxLabel,
            out TMP_Text orientationLabel,
            out Button pauseLobbyButton,
            out Button resultLobbyButton)
        {
            var parent = game.pauseControls.transform;
            var template = game.lineLabel != null ? game.lineLabel.GetComponentInParent<Button>(true) : game.pauseButton;
            var restart = (parent.Find("Restart puzzle") ?? parent.Find("GameTab/Restart puzzle"))?.GetComponent<Button>();

            // 1. Style HUD Pause button with global 3-horizontal-bars hamburger icon
            SetupHamburgerIcon(game.pauseButton);

            // 2. Remove legacy duplicate HUD Restart button if present in Content
            var hudContent = game.pauseButton != null ? game.pauseButton.transform.parent : null;
            var hudRestart = hudContent != null ? hudContent.Find("Restart") : null;
            if (hudRestart != null && hudRestart != restart?.transform)
            {
                if (Application.isPlaying) Object.Destroy(hudRestart.gameObject);
                else Object.DestroyImmediate(hudRestart.gameObject);
            }

            // 3. Create header tabs (GAME / SYSTEM) and X close button (Continue)
            var gameTabBtn = AddOrFindButton(template, parent, "Tab_Game", "GAME", () => game.SelectPauseTab(PauseTab.Game));
            var sysTabBtn = AddOrFindButton(template, parent, "Tab_System", "SYSTEM", () => game.SelectPauseTab(PauseTab.System));

            SetupCloseXButton(game.resumeButton, parent, game.Resume);

            // 4. Create Game Tab & System Tab containers inside pauseControls
            var gameTabRoot = EnsureContainer(parent, "GameTab");
            var systemTabRoot = EnsureContainer(parent, "SystemTab");

            // Move HUD bottom controls (Undo, Hold to preview, Numbers) and Restart + Lobby into GameTab
            if (restart != null)
            {
                restart.transform.SetParent(gameTabRoot, false);
                restart.onClick = new Button.ButtonClickedEvent();
                restart.onClick.AddListener(game.Restart);
                var restartTxt = restart.GetComponentInChildren<TMP_Text>(true);
                if (restartTxt != null) restartTxt.text = "Restart";
            }

            if (game.undoButton != null)
            {
                game.undoButton.transform.SetParent(gameTabRoot, false);
                game.undoButton.onClick = new Button.ButtonClickedEvent();
                game.undoButton.onClick.AddListener(game.Undo);
            }

            var holdPreview = game.GetComponentInChildren<HoldPreview>(true);
            if (holdPreview != null)
            {
                holdPreview.transform.SetParent(gameTabRoot, false);
                var holdTxt = holdPreview.GetComponentInChildren<TMP_Text>(true);
                if (holdTxt != null) holdTxt.text = "Hold to preview";
            }

            var numbersBtn = game.numbersLabel != null ? game.numbersLabel.GetComponentInParent<Button>(true) : null;
            if (numbersBtn != null)
            {
                numbersBtn.transform.SetParent(gameTabRoot, false);
                numbersBtn.onClick = new Button.ButtonClickedEvent();
                numbersBtn.onClick.AddListener(game.Numbers);
            }

            pauseLobbyButton = AddOrFindButton(restart != null ? restart : template, gameTabRoot, "Lobby", "Back to lobby", game.OpenLobby);

            // System Tab controls: Music, Sound, Line slide, Animation (Orientation button removed from PAUSED-SYSTEM menu)
            bgmLabel = AddOrFind(template, systemTabRoot, "Music", game.ToggleBgm);
            sfxLabel = AddOrFind(template, systemTabRoot, "Sound", game.ToggleSfx);

            var lineBtn = game.lineLabel != null ? game.lineLabel.GetComponentInParent<Button>(true) : null;
            if (lineBtn != null)
            {
                lineBtn.transform.SetParent(systemTabRoot, false);
                lineBtn.name = "Line slide";
                lineBtn.onClick = new Button.ButtonClickedEvent();
                lineBtn.onClick.AddListener(game.Lines);
            }

            var animBtn = game.animationLabel != null ? game.animationLabel.GetComponentInParent<Button>(true) : null;
            if (animBtn != null)
            {
                animBtn.transform.SetParent(systemTabRoot, false);
                animBtn.name = "Animation";
                animBtn.onClick = new Button.ButtonClickedEvent();
                animBtn.onClick.AddListener(game.Animation);
            }

            var legacyOrient = systemTabRoot.Find("Orientation");
            if (legacyOrient != null)
            {
                if (Application.isPlaying) Object.Destroy(legacyOrient.gameObject);
                else Object.DestroyImmediate(legacyOrient.gameObject);
            }
            orientationLabel = null;

            // Result panel Lobby button
            var resultParent = game.resultControls.transform;
            var playAgain = resultParent.Find("Play this again")?.GetComponent<Button>() ?? template;
            resultLobbyButton = AddOrFindButton(playAgain, resultParent, "Lobby", "Back to lobby", game.OpenLobby);

            RefreshTabs(game);
            ApplyLayout(game, PlayOrientation.Portrait);
        }

        public static void RefreshTabs(PuzzleGame game)
        {
            if (game == null || game.pauseControls == null) return;
            var parent = game.pauseControls.transform;
            var gameTabRoot = parent.Find("GameTab");
            var systemTabRoot = parent.Find("SystemTab");
            bool isGameTab = game.ActivePauseTab == PauseTab.Game;

            if (gameTabRoot != null) gameTabRoot.gameObject.SetActive(isGameTab);
            if (systemTabRoot != null) systemTabRoot.gameObject.SetActive(!isGameTab);

            var tabGameImg = parent.Find("Tab_Game")?.GetComponent<Image>();
            var tabSysImg = parent.Find("Tab_System")?.GetComponent<Image>();
            if (tabGameImg != null) tabGameImg.color = isGameTab ? TabActiveColor : TabInactiveColor;
            if (tabSysImg != null) tabSysImg.color = !isGameTab ? TabActiveColor : TabInactiveColor;
        }

        private static void SetupHamburgerIcon(Button pauseButton)
        {
            if (pauseButton == null) return;
            var label = pauseButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "";

            var iconRoot = pauseButton.transform.Find("HamburgerIcon") as RectTransform;
            if (iconRoot == null)
            {
                iconRoot = new GameObject("HamburgerIcon", typeof(RectTransform)).GetComponent<RectTransform>();
                iconRoot.SetParent(pauseButton.transform, false);
                iconRoot.anchorMin = new Vector2(0.24f, 0.24f);
                iconRoot.anchorMax = new Vector2(0.76f, 0.76f);
                iconRoot.offsetMin = iconRoot.offsetMax = Vector2.zero;

                // Three horizontal bars stacked vertically
                CreateBar(iconRoot, "BarTop", 0.76f, 0.94f);
                CreateBar(iconRoot, "BarMid", 0.41f, 0.59f);
                CreateBar(iconRoot, "BarBot", 0.06f, 0.24f);
            }
        }

        private static void CreateBar(Transform parent, string name, float yMin, float yMax)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.anchorMin = new Vector2(0f, yMin);
            bar.anchorMax = new Vector2(1f, yMax);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            var img = bar.GetComponent<Image>();
            img.color = IconColor;
            img.raycastTarget = false;
        }

        private static void SetupCloseXButton(Button resumeButton, Transform pauseParent, UnityEngine.Events.UnityAction onResume)
        {
            if (resumeButton == null) return;
            resumeButton.transform.SetParent(pauseParent, false);
            resumeButton.name = "Continue";
            resumeButton.onClick = new Button.ButtonClickedEvent();
            resumeButton.onClick.AddListener(onResume);

            var bg = resumeButton.GetComponent<Image>();
            if (bg != null) bg.color = CloseBtnColor;

            var label = resumeButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "X";
                label.fontStyle = FontStyles.Bold;
            }
        }

        private static RectTransform EnsureContainer(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.84f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static void ApplyLayout(PuzzleGame game, PlayOrientation orientation)
        {
            if (game == null || game.pauseControls == null || game.resultControls == null) return;

            var parent = (RectTransform)game.pauseControls.transform;
            var tabGame = parent.Find("Tab_Game") as RectTransform;
            var tabSys = parent.Find("Tab_System") as RectTransform;
            var closeBtn = game.resumeButton != null ? (RectTransform)game.resumeButton.transform : null;
            var gameTabRoot = parent.Find("GameTab") as RectTransform;
            var sysTabRoot = parent.Find("SystemTab") as RectTransform;

            var holdPreview = gameTabRoot != null ? gameTabRoot.GetComponentInChildren<HoldPreview>(true) : null;
            var numbersBtn = game.numbersLabel != null ? game.numbersLabel.GetComponentInParent<Button>(true) : null;
            var restartBtn = gameTabRoot != null ? gameTabRoot.Find("Restart puzzle")?.GetComponent<Button>() : null;
            var lobbyBtn = gameTabRoot != null ? gameTabRoot.Find("Lobby")?.GetComponent<Button>() : null;

            var gameItems = new RectTransform[]
            {
                restartBtn != null ? (RectTransform)restartBtn.transform : null,
                game.undoButton != null ? (RectTransform)game.undoButton.transform : null,
                holdPreview != null ? (RectTransform)holdPreview.transform : null,
                numbersBtn != null ? (RectTransform)numbersBtn.transform : null,
                lobbyBtn != null ? (RectTransform)lobbyBtn.transform : null
            };

            var musicBtn = sysTabRoot != null ? sysTabRoot.Find("Music")?.GetComponent<Button>() : null;
            var sfxBtn = sysTabRoot != null ? sysTabRoot.Find("Sound")?.GetComponent<Button>() : null;
            var lineBtn = game.lineLabel != null ? game.lineLabel.GetComponentInParent<Button>(true) : null;
            var animBtn = game.animationLabel != null ? game.animationLabel.GetComponentInParent<Button>(true) : null;

            var sysItems = new RectTransform[]
            {
                musicBtn != null ? (RectTransform)musicBtn.transform : null,
                sfxBtn != null ? (RectTransform)sfxBtn.transform : null,
                lineBtn != null ? (RectTransform)lineBtn.transform : null,
                animBtn != null ? (RectTransform)animBtn.transform : null
            };

            if (orientation == PlayOrientation.Landscape)
            {
                SetAnchors(game.message.rectTransform, 0.08f, 0.78f, 0.92f, 0.94f);
                SetAnchors(parent, 0.10f, 0.08f, 0.90f, 0.75f);

                // Top row of PauseControls: GAME tab, SYSTEM tab, and X button on right
                SetAnchors(tabGame, 0.00f, 0.85f, 0.42f, 1.00f);
                SetAnchors(tabSys, 0.44f, 0.85f, 0.86f, 1.00f);
                SetAnchors(closeBtn, 0.88f, 0.85f, 1.00f, 1.00f);

                SetAnchors(gameTabRoot, 0.00f, 0.00f, 1.00f, 0.81f);
                SetAnchors(sysTabRoot, 0.00f, 0.00f, 1.00f, 0.81f);

                LayoutGrid(gameItems, 2, 3);
                LayoutGrid(sysItems, 2, 2);

                var resultRect = (RectTransform)game.resultControls.transform;
                SetAnchors(resultRect, 0.18f, 0.10f, 0.82f, 0.52f);
            }
            else
            {
                SetAnchors(game.message.rectTransform, 0.08f, 0.72f, 0.92f, 0.90f);
                SetAnchors(parent, 0.10f, 0.08f, 0.90f, 0.68f);

                // Top row of PauseControls: GAME tab, SYSTEM tab, and X button on right
                SetAnchors(tabGame, 0.00f, 0.86f, 0.41f, 1.00f);
                SetAnchors(tabSys, 0.43f, 0.86f, 0.84f, 1.00f);
                SetAnchors(closeBtn, 0.86f, 0.86f, 1.00f, 1.00f);

                SetAnchors(gameTabRoot, 0.00f, 0.00f, 1.00f, 0.82f);
                SetAnchors(sysTabRoot, 0.00f, 0.00f, 1.00f, 0.82f);

                LayoutVertical(gameItems);
                LayoutVertical(sysItems);

                var resultRect = (RectTransform)game.resultControls.transform;
                SetAnchors(resultRect, 0.12f, 0.14f, 0.88f, 0.54f);
            }

            var resultButtons = new[]
            {
                game.nextButton != null ? (RectTransform)game.nextButton.transform : null,
                game.resultControls.transform.Find("Play this again") as RectTransform,
                game.resultControls.transform.Find("Lobby") as RectTransform
            };
            LayoutVertical(resultButtons, 0.025f);
        }

        private static void LayoutVertical(RectTransform[] items, float pad = 0.012f)
        {
            int count = items.Length;
            if (count == 0) return;
            for (int i = 0; i < count; i++)
            {
                if (items[i] == null) continue;
                float y1 = 1f - i / (float)count - pad;
                float y0 = 1f - (i + 1f) / count + pad;
                SetAnchors(items[i], 0f, y0, 1f, y1);
            }
        }

        private static void LayoutGrid(RectTransform[] items, int cols, int rows)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                int col = i % cols;
                int row = i / cols;
                float x0 = col == 0 ? 0f : 0.515f;
                float x1 = col == 0 ? 0.485f : 1f;
                float y1 = 1f - row / (float)rows - 0.02f;
                float y0 = 1f - (row + 1f) / rows + 0.02f;
                SetAnchors(items[i], x0, y0, x1, y1);
            }
        }

        private static void SetAnchors(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static TMP_Text AddOrFind(Button template, Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var existing = parent.Find(name);
            var button = existing != null ? existing.GetComponent<Button>() : Object.Instantiate(template, parent);
            button.name = name;
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
