using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    /// <summary>Phase 3 Lobby (difficulty tabs + stage cards) and Illustration Gallery (collection + full-screen viewer).</summary>
    public sealed class PuzzleLobbyGalleryUI : MonoBehaviour
    {
        private static readonly Color PanelBg = new Color(0.98f, 0.98f, 0.95f, 0.96f);
        private static readonly Color CardUnlocked = new Color(0.55f, 0.86f, 0.79f, 1f);
        private static readonly Color CardLocked = new Color(0.78f, 0.78f, 0.75f, 1f);
        private static readonly Color TabActive = new Color(0.43f, 0.81f, 0.92f, 1f);
        private static readonly Color TabInactive = new Color(0.88f, 0.88f, 0.85f, 1f);
        private static readonly Color TextLight = new Color(0.07f, 0.07f, 0.06f, 1f);

        private PuzzleGame game;
        private RectTransform titlePanel;
        private RectTransform lobbyPanel;
        private RectTransform galleryPanel;
        private RectTransform viewerModal;

        // Title UI elements
        private RectTransform titleCard;
        private TMP_Text titleMainLabel;
        private TMP_Text titleSubLabel;
        private Button titleStartButton;

        // Lobby UI elements
        private RectTransform lobbyHeader;
        private TMP_Text lobbyTitle;
        private Button galleryOpenButton;
        private Button orientationButton;
        private TMP_Text orientationButtonLabel;
        private Button resumePlayButton;
        private RectTransform tabBar;
        private RectTransform cardsArea;
        private RectTransform pagerBar;
        private Button prevPageButton;
        private Button nextPageButton;
        private TMP_Text pageLabel;

        // Gallery UI elements
        private RectTransform galleryHeader;
        private TMP_Text galleryTitle;
        private Button galleryBackButton;
        private RectTransform galleryCardsArea;
        private RectTransform galleryPagerBar;
        private Button galleryPrevButton;
        private Button galleryNextButton;
        private TMP_Text galleryPageLabel;

        // Fullscreen Viewer UI elements
        private RawImage viewerImage;
        private AspectRatioFitter viewerFitter;
        private TMP_Text viewerCaption;
        private Button viewerCloseButton;

        private readonly List<string> tabFilters = new List<string>();
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<StageCardView> stageCards = new List<StageCardView>();
        private readonly List<GalleryCardView> galleryCards = new List<GalleryCardView>();
        private readonly List<int> filteredStageIndices = new List<int>();

        private string activeFilter = "ALL";
        private int lobbyPage;
        private int galleryPage;
        private PlayOrientation currentOrientation = PlayOrientation.Portrait;

        public bool IsTitleOpen => titlePanel != null && titlePanel.gameObject.activeSelf;
        public bool IsLobbyOpen => lobbyPanel != null && lobbyPanel.gameObject.activeSelf;
        public bool IsGalleryOpen => galleryPanel != null && galleryPanel.gameObject.activeSelf;
        public bool IsViewerOpen => viewerModal != null && viewerModal.gameObject.activeSelf;
        public string ActiveFilter => activeFilter;
        public int FilteredStageCount => filteredStageIndices.Count;
        public int LobbyPage => lobbyPage;
        public int GalleryPage => galleryPage;
        public RawImage ViewerImage => viewerImage;

        private sealed class StageCardView
        {
            public RectTransform Root;
            public Image Background;
            public RawImage Thumbnail;
            public AspectRatioFitter Fitter;
            public GameObject LockOverlay;
            public TMP_Text Title;
            public TMP_Text Status;
            public Button Button;
            public int StageIndex = -1;
        }

        private sealed class GalleryCardView
        {
            public RectTransform Root;
            public Image Background;
            public RawImage Thumbnail;
            public AspectRatioFitter Fitter;
            public GameObject LockOverlay;
            public TMP_Text Title;
            public TMP_Text Badge;
            public Button Button;
            public int StageIndex = -1;
        }

        public static PuzzleLobbyGalleryUI Install(PuzzleGame owner, Transform safeAreaRoot)
        {
            var existing = safeAreaRoot.Find("LobbyAndGallery")?.GetComponent<PuzzleLobbyGalleryUI>();
            if (existing != null)
            {
                existing.Initialize(owner);
                return existing;
            }

            var host = new GameObject("LobbyAndGallery", typeof(RectTransform), typeof(PuzzleLobbyGalleryUI));
            var rect = host.GetComponent<RectTransform>();
            rect.SetParent(safeAreaRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            var ui = host.GetComponent<PuzzleLobbyGalleryUI>();
            ui.Initialize(owner);
            return ui;
        }

        private void Initialize(PuzzleGame owner)
        {
            game = owner;
            BuildFilters();
            if (titlePanel == null) BuildTitlePanel();
            if (lobbyPanel == null) BuildLobbyPanel();
            if (galleryPanel == null) BuildGalleryPanel();
            if (viewerModal == null) BuildViewerModal();
            CloseAll();
            ApplyLayout(game.Settings != null ? game.Settings.Orientation : PlayOrientation.Portrait);
        }

        private void BuildFilters()
        {
            tabFilters.Clear();
            tabFilters.Add("ALL");
            if (game == null || game.stages == null) return;
            var seen = new HashSet<string>();
            foreach (StageData stage in game.stages)
            {
                if (stage == null) continue;
                string key = $"{stage.Columns}x{stage.Rows}";
                if (seen.Add(key)) tabFilters.Add(key);
            }
        }

        public void OpenTitle()
        {
            if (titlePanel == null) return;
            if (viewerModal != null) viewerModal.gameObject.SetActive(false);
            if (galleryPanel != null) galleryPanel.gameObject.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.gameObject.SetActive(false);
            titlePanel.gameObject.SetActive(true);
            SetInGameBackdropVisible(false);
        }

        public void OpenLobby()
        {
            if (lobbyPanel == null) return;
            if (titlePanel != null) titlePanel.gameObject.SetActive(false);
            viewerModal.gameObject.SetActive(false);
            galleryPanel.gameObject.SetActive(false);
            lobbyPanel.gameObject.SetActive(true);
            SetInGameBackdropVisible(false);
            RefreshLobby();
        }

        public void OpenGallery()
        {
            if (galleryPanel == null) return;
            if (titlePanel != null) titlePanel.gameObject.SetActive(false);
            viewerModal.gameObject.SetActive(false);
            lobbyPanel.gameObject.SetActive(false);
            galleryPanel.gameObject.SetActive(true);
            SetInGameBackdropVisible(false);
            RefreshGallery();
        }

        public void CloseAll()
        {
            if (titlePanel != null) titlePanel.gameObject.SetActive(false);
            if (viewerModal != null) viewerModal.gameObject.SetActive(false);
            if (galleryPanel != null) galleryPanel.gameObject.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.gameObject.SetActive(false);
            SetInGameBackdropVisible(true);
        }

        private void SetInGameBackdropVisible(bool visible)
        {
            if (game == null) return;
            Transform hudContent = game.ResponsiveLayout != null && game.ResponsiveLayout.ContentRect != null
                ? (Transform)game.ResponsiveLayout.ContentRect
                : (game.pauseButton != null ? game.pauseButton.transform.parent : null);
            if (hudContent != null)
                hudContent.gameObject.SetActive(visible);

            if (!visible && game.curtain != null)
                game.curtain.SetActive(false);
        }

        public void SelectFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter) || !tabFilters.Contains(filter)) filter = "ALL";
            activeFilter = filter;
            lobbyPage = 0;
            RefreshLobby();
        }

        public void NextLobbyPage()
        {
            int pageSize = CardsPerPage;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(filteredStageIndices.Count / (float)pageSize));
            if (lobbyPage >= totalPages - 1) return;
            lobbyPage = Mathf.Min(totalPages - 1, lobbyPage + 1);
            RefreshLobby();
        }

        public void PrevLobbyPage()
        {
            if (lobbyPage <= 0) return;
            lobbyPage = Mathf.Max(0, lobbyPage - 1);
            RefreshLobby();
        }

        public void NextGalleryPage()
        {
            int pageSize = CardsPerPage;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((game.stages?.Length ?? 0) / (float)pageSize));
            if (galleryPage >= totalPages - 1) return;
            galleryPage = Mathf.Min(totalPages - 1, galleryPage + 1);
            RefreshGallery();
        }

        public void PrevGalleryPage()
        {
            if (galleryPage <= 0) return;
            galleryPage = Mathf.Max(0, galleryPage - 1);
            RefreshGallery();
        }

        public bool TrySelectStageFromLobby(int stageIndex)
        {
            if (game == null || game.stages == null || !PuzzleProgress.IsUnlocked(game.stages, stageIndex))
                return false;
            CloseAll();
            game.Load(stageIndex);
            return true;
        }

        public bool TryOpenOriginalViewer(int stageIndex)
        {
            if (game == null || game.stages == null || stageIndex < 0 || stageIndex >= game.stages.Length)
                return false;
            StageData stage = game.stages[stageIndex];
            if (!PuzzleProgress.IsCleared(stage) || stage.Illustration == null)
                return false;

            viewerImage.texture = stage.Illustration;
            // Full original texture without cropping (uvRect = 0,0,1,1)
            viewerImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            viewerFitter.aspectRatio = (float)stage.Illustration.width / Mathf.Max(1, stage.Illustration.height);
            int bestMoves = PuzzleProgress.GetBestMoves(stage);
            float bestTime = PuzzleProgress.GetBestTime(stage);
            string record = bestMoves < int.MaxValue
                ? $"Best: {bestMoves} moves  |  {FormatTime(bestTime)}"
                : "Cleared";
            viewerCaption.text = $"STAGE {stageIndex + 1:00}  ({stage.Columns}x{stage.Rows})  —  {record}\nOriginal Full Illustration ({stage.Illustration.width}x{stage.Illustration.height})";
            viewerModal.gameObject.SetActive(true);
            return true;
        }

        public void CloseViewer()
        {
            if (viewerModal != null) viewerModal.gameObject.SetActive(false);
        }

        private int CardsPerPage => 6;

        public void RefreshLobby()
        {
            if (game == null || game.stages == null || lobbyPanel == null) return;

            filteredStageIndices.Clear();
            for (int i = 0; i < game.stages.Length; i++)
            {
                StageData stage = game.stages[i];
                if (stage == null) continue;
                string key = $"{stage.Columns}x{stage.Rows}";
                if (activeFilter == "ALL" || activeFilter == key)
                    filteredStageIndices.Add(i);
            }

            int clearedTotal = PuzzleProgress.ClearedCount(game.stages);
            lobbyTitle.text = $"STAGE LOBBY    ({clearedTotal:00} / {game.stages.Length:00} CLEARED)";
            orientationButtonLabel.text = "View: " + (game.Settings.IsLandscape ? "LANDSCAPE" : "PORTRAIT");

            for (int i = 0; i < tabButtons.Count; i++)
            {
                string filter = tabFilters[i];
                var image = tabButtons[i].GetComponent<Image>();
                image.color = filter == activeFilter ? TabActive : TabInactive;
            }

            int pageSize = CardsPerPage;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(filteredStageIndices.Count / (float)pageSize));
            lobbyPage = Mathf.Clamp(lobbyPage, 0, totalPages - 1);
            pageLabel.text = $"PAGE {lobbyPage + 1} / {totalPages}  ({filteredStageIndices.Count} STAGES)";
            prevPageButton.interactable = lobbyPage > 0;
            nextPageButton.interactable = lobbyPage < totalPages - 1;

            int start = lobbyPage * pageSize;
            for (int slot = 0; slot < stageCards.Count; slot++)
            {
                var card = stageCards[slot];
                int listIndex = start + slot;
                if (listIndex >= filteredStageIndices.Count)
                {
                    card.Root.gameObject.SetActive(false);
                    card.StageIndex = -1;
                    continue;
                }

                card.Root.gameObject.SetActive(true);
                int stageIndex = filteredStageIndices[listIndex];
                card.StageIndex = stageIndex;
                StageData stage = game.stages[stageIndex];
                bool unlocked = PuzzleProgress.IsUnlocked(game.stages, stageIndex);
                bool cleared = PuzzleProgress.IsCleared(stage);

                card.Background.color = unlocked ? CardUnlocked : CardLocked;
                card.Button.interactable = unlocked;
                card.Thumbnail.texture = unlocked ? stage.Illustration : null;
                card.Thumbnail.uvRect = stage.GetImageUV();
                card.Thumbnail.color = unlocked ? Color.white : Color.clear;
                card.Fitter.aspectRatio = (float)stage.Columns / stage.Rows;
                if (card.LockOverlay != null) card.LockOverlay.SetActive(!unlocked);

                card.Title.text = $"#{stageIndex + 1:00}   [{stage.Columns}x{stage.Rows}]";
                if (cleared)
                {
                    int best = PuzzleProgress.GetBestMoves(stage);
                    float time = PuzzleProgress.GetBestTime(stage);
                    card.Status.text = best < int.MaxValue
                        ? $"CLEARED  •  {best}M / {FormatTime(time)}"
                        : "CLEARED";
                }
                else if (unlocked)
                {
                    card.Status.text = "UNLOCKED  •  TAP TO PLAY";
                }
                else
                {
                    card.Status.text = "LOCKED";
                }
            }
        }

        public void RefreshGallery()
        {
            if (game == null || game.stages == null || galleryPanel == null) return;

            int clearedCount = PuzzleProgress.ClearedCount(game.stages);
            galleryTitle.text = $"ILLUSTRATION GALLERY    ({clearedCount:00} / {game.stages.Length:00} UNLOCKED)";

            int pageSize = CardsPerPage;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(game.stages.Length / (float)pageSize));
            galleryPage = Mathf.Clamp(galleryPage, 0, totalPages - 1);
            galleryPageLabel.text = $"GALLERY PAGE {galleryPage + 1} / {totalPages}";
            galleryPrevButton.interactable = galleryPage > 0;
            galleryNextButton.interactable = galleryPage < totalPages - 1;

            int start = galleryPage * pageSize;
            for (int slot = 0; slot < galleryCards.Count; slot++)
            {
                var card = galleryCards[slot];
                int stageIndex = start + slot;
                if (stageIndex >= game.stages.Length)
                {
                    card.Root.gameObject.SetActive(false);
                    card.StageIndex = -1;
                    continue;
                }

                card.Root.gameObject.SetActive(true);
                card.StageIndex = stageIndex;
                StageData stage = game.stages[stageIndex];
                bool cleared = PuzzleProgress.IsCleared(stage);

                card.Background.color = cleared ? CardUnlocked : CardLocked;
                card.Button.interactable = cleared;
                card.Thumbnail.texture = cleared ? stage.Illustration : null;
                card.Thumbnail.uvRect = new Rect(0f, 0f, 1f, 1f); // Full aspect in gallery
                card.Thumbnail.color = cleared ? Color.white : Color.clear;
                card.Fitter.aspectRatio = stage.Illustration != null
                    ? (float)stage.Illustration.width / Mathf.Max(1, stage.Illustration.height)
                    : 1f;
                if (card.LockOverlay != null) card.LockOverlay.SetActive(!cleared);

                card.Title.text = $"ILLUSTRATION #{stageIndex + 1:00}";
                card.Badge.text = cleared ? "VIEW FULL ART" : "LOCKED (CLEAR STAGE)";
            }
        }

        public void ApplyLayout(PlayOrientation orientation)
        {
            currentOrientation = orientation;
            if (lobbyPanel == null || galleryPanel == null || viewerModal == null) return;

            bool landscape = orientation == PlayOrientation.Landscape;
            int cols = landscape ? 3 : 2;
            int rows = landscape ? 2 : 3;

            if (landscape)
            {
                if (titleCard != null) SetAnchors(titleCard, 0f, 0f, 1f, 1f);
                if (titleMainLabel != null) SetAnchors(titleMainLabel.rectTransform, 0.08f, 0.54f, 0.92f, 0.80f);
                if (titleSubLabel != null) SetAnchors(titleSubLabel.rectTransform, 0.12f, 0.42f, 0.88f, 0.53f);
                if (titleStartButton != null) SetAnchors((RectTransform)titleStartButton.transform, 0.30f, 0.18f, 0.70f, 0.34f);

                SetAnchors(lobbyHeader, 0.03f, 0.87f, 0.97f, 0.97f);
                SetAnchors(lobbyTitle.rectTransform, 0.00f, 0.00f, 0.46f, 1.00f);
                SetAnchors((RectTransform)galleryOpenButton.transform, 0.48f, 0.05f, 0.64f, 0.95f);
                SetAnchors((RectTransform)orientationButton.transform, 0.66f, 0.05f, 0.82f, 0.95f);
                SetAnchors((RectTransform)resumePlayButton.transform, 0.84f, 0.05f, 1.00f, 0.95f);

                SetAnchors(tabBar, 0.03f, 0.76f, 0.97f, 0.85f);
                SetAnchors(cardsArea, 0.03f, 0.13f, 0.97f, 0.74f);
                SetAnchors(pagerBar, 0.03f, 0.02f, 0.97f, 0.11f);

                SetAnchors(galleryHeader, 0.03f, 0.86f, 0.97f, 0.97f);
                SetAnchors(galleryTitle.rectTransform, 0.00f, 0.00f, 0.74f, 1.00f);
                SetAnchors((RectTransform)galleryBackButton.transform, 0.76f, 0.05f, 1.00f, 0.95f);
                SetAnchors(galleryCardsArea, 0.03f, 0.13f, 0.97f, 0.84f);
                SetAnchors(galleryPagerBar, 0.03f, 0.02f, 0.97f, 0.11f);
            }
            else
            {
                if (titleCard != null) SetAnchors(titleCard, 0f, 0f, 1f, 1f);
                if (titleMainLabel != null) SetAnchors(titleMainLabel.rectTransform, 0.06f, 0.56f, 0.94f, 0.78f);
                if (titleSubLabel != null) SetAnchors(titleSubLabel.rectTransform, 0.08f, 0.45f, 0.92f, 0.55f);
                if (titleStartButton != null) SetAnchors((RectTransform)titleStartButton.transform, 0.18f, 0.18f, 0.82f, 0.30f);

                SetAnchors(lobbyHeader, 0.04f, 0.86f, 0.96f, 0.98f);
                SetAnchors(lobbyTitle.rectTransform, 0.00f, 0.52f, 1.00f, 1.00f);
                SetAnchors((RectTransform)galleryOpenButton.transform, 0.00f, 0.02f, 0.32f, 0.48f);
                SetAnchors((RectTransform)orientationButton.transform, 0.34f, 0.02f, 0.66f, 0.48f);
                SetAnchors((RectTransform)resumePlayButton.transform, 0.68f, 0.02f, 1.00f, 0.48f);

                SetAnchors(tabBar, 0.04f, 0.78f, 0.96f, 0.845f);
                SetAnchors(cardsArea, 0.04f, 0.11f, 0.96f, 0.765f);
                SetAnchors(pagerBar, 0.04f, 0.02f, 0.96f, 0.095f);

                SetAnchors(galleryHeader, 0.04f, 0.87f, 0.96f, 0.98f);
                SetAnchors(galleryTitle.rectTransform, 0.00f, 0.50f, 1.00f, 1.00f);
                SetAnchors((RectTransform)galleryBackButton.transform, 0.20f, 0.02f, 0.80f, 0.46f);
                SetAnchors(galleryCardsArea, 0.04f, 0.11f, 0.96f, 0.85f);
                SetAnchors(galleryPagerBar, 0.04f, 0.02f, 0.96f, 0.095f);
            }

            LayoutTabs();
            LayoutCardGrid(stageCards.ConvertAll(c => c.Root), cols, rows);
            LayoutCardGrid(galleryCards.ConvertAll(c => c.Root), cols, rows);

            if (IsLobbyOpen) RefreshLobby();
            if (IsGalleryOpen) RefreshGallery();
        }

        private void LayoutTabs()
        {
            int count = tabButtons.Count;
            if (count == 0) return;
            float gap = 0.008f;
            float width = (1f - gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                float x0 = i * (width + gap);
                SetAnchors((RectTransform)tabButtons[i].transform, x0, 0f, x0 + width, 1f);
            }
        }

        private static void LayoutCardGrid(List<RectTransform> cards, int cols, int rows)
        {
            float gapX = 0.02f;
            float gapY = 0.025f;
            float cellW = (1f - gapX * (cols - 1)) / cols;
            float cellH = (1f - gapY * (rows - 1)) / rows;
            for (int i = 0; i < cards.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x0 = col * (cellW + gapX);
                float y1 = 1f - row * (cellH + gapY);
                float y0 = y1 - cellH;
                SetAnchors(cards[i], x0, y0, x0 + cellW, y1);
            }
        }

        private void BuildTitlePanel()
        {
            // Fullscreen title screen over the casual background (in-game HUD/board is hidden via SetInGameBackdropVisible)
            titlePanel = Panel("TitlePanel", transform, 0f, 0f, 1f, 1f, Color.clear, true);
            titleCard = Panel("TitleCard", titlePanel, 0f, 0f, 1f, 1f, Color.clear, true);
            titleMainLabel = Label("MainTitle", titleCard, 0.06f, 0.56f, 0.94f, 0.78f, "PICTURE PUZZLE", 68);
            titleMainLabel.fontStyle = FontStyles.Bold;
            titleSubLabel = Label("SubTitle", titleCard, 0.08f, 0.45f, 0.92f, 0.55f, "COLLECTIBLE ILLUSTRATION\nSLIDING PUZZLE", 24);
            titleStartButton = MakeButton(titleCard, "StartButton", "TAP TO START", 0.18f, 0.18f, 0.82f, 0.30f, () => game.EnterLobbyFromTitle());
            var startImg = titleStartButton.GetComponent<Image>();
            if (startImg != null) startImg.color = new Color(0.16f, 0.52f, 0.62f, 0.96f);
        }

        private void BuildLobbyPanel()
        {
            lobbyPanel = Panel("LobbyPanel", transform, 0f, 0f, 1f, 1f, PanelBg, true);
            lobbyHeader = Rect("Header", lobbyPanel, 0.04f, 0.86f, 0.96f, 0.98f);
            lobbyTitle = Label("Title", lobbyHeader, 0f, 0.52f, 1f, 1f, "STAGE LOBBY", 34);
            galleryOpenButton = MakeButton(lobbyHeader, "GalleryButton", "Gallery", 0f, 0.02f, 0.32f, 0.48f, OpenGallery);
            orientationButton = MakeButton(lobbyHeader, "OrientationButton", "View: PORTRAIT", 0.34f, 0.02f, 0.66f, 0.48f, () =>
            {
                game.ToggleOrientation();
                RefreshLobby();
            });
            orientationButtonLabel = orientationButton.GetComponentInChildren<TMP_Text>(true);
            resumePlayButton = MakeButton(lobbyHeader, "PlayButton", "Play Stage", 0.68f, 0.02f, 1f, 0.48f, () => game.EnterInGameFromLobby());

            tabBar = Rect("DifficultyTabs", lobbyPanel, 0.04f, 0.78f, 0.96f, 0.845f);
            tabButtons.Clear();
            foreach (string filter in tabFilters)
            {
                string captured = filter;
                var btn = MakeButton(tabBar, "Tab_" + captured, captured, 0f, 0f, 1f, 1f, () => SelectFilter(captured));
                tabButtons.Add(btn);
            }

            cardsArea = Rect("StageCards", lobbyPanel, 0.04f, 0.11f, 0.96f, 0.765f);
            stageCards.Clear();
            for (int i = 0; i < CardsPerPage; i++)
            {
                int slot = i;
                var cardRoot = Panel($"StageCard_{slot + 1:00}", cardsArea, 0f, 0f, 1f, 1f, CardUnlocked, true);
                var button = cardRoot.gameObject.AddComponent<Button>();
                button.targetGraphic = cardRoot.GetComponent<Image>();

                var thumbFrame = Panel("ThumbFrame", cardRoot, 0.05f, 0.28f, 0.95f, 0.95f, new Color(0.02f, 0.04f, 0.06f, 0.85f), false);
                var thumbRect = Rect("Thumbnail", thumbFrame, 0f, 0f, 1f, 1f);
                var raw = thumbRect.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                var fitter = thumbRect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1f;
                var lockOverlay = BuildLockOverlay(thumbFrame);

                var title = Label("CardTitle", cardRoot, 0.05f, 0.14f, 0.95f, 0.27f, "#01 [3x3]", 22);
                var status = Label("CardStatus", cardRoot, 0.05f, 0.02f, 0.95f, 0.14f, "UNLOCKED", 18);

                var view = new StageCardView
                {
                    Root = cardRoot,
                    Background = cardRoot.GetComponent<Image>(),
                    Thumbnail = raw,
                    Fitter = fitter,
                    LockOverlay = lockOverlay,
                    Title = title,
                    Status = status,
                    Button = button
                };
                button.onClick.AddListener(() =>
                {
                    if (view.StageIndex >= 0) TrySelectStageFromLobby(view.StageIndex);
                });
                stageCards.Add(view);
            }

            pagerBar = Rect("Pager", lobbyPanel, 0.04f, 0.02f, 0.96f, 0.095f);
            prevPageButton = MakeButton(pagerBar, "PrevPage", "< Prev", 0f, 0.05f, 0.24f, 0.95f, PrevLobbyPage);
            pageLabel = Label("PageLabel", pagerBar, 0.26f, 0f, 0.74f, 1f, "PAGE 1 / 1", 22);
            nextPageButton = MakeButton(pagerBar, "NextPage", "Next >", 0.76f, 0.05f, 1f, 0.95f, NextLobbyPage);
        }

        private void BuildGalleryPanel()
        {
            galleryPanel = Panel("GalleryPanel", transform, 0f, 0f, 1f, 1f, PanelBg, true);
            galleryHeader = Rect("Header", galleryPanel, 0.04f, 0.87f, 0.96f, 0.98f);
            galleryTitle = Label("Title", galleryHeader, 0f, 0.50f, 1f, 1f, "ILLUSTRATION GALLERY", 34);
            galleryBackButton = MakeButton(galleryHeader, "BackToLobby", "Back to Stage Lobby", 0.20f, 0.02f, 0.80f, 0.46f, OpenLobby);

            galleryCardsArea = Rect("GalleryCards", galleryPanel, 0.04f, 0.11f, 0.96f, 0.85f);
            galleryCards.Clear();
            for (int i = 0; i < CardsPerPage; i++)
            {
                int slot = i;
                var cardRoot = Panel($"GalleryCard_{slot + 1:00}", galleryCardsArea, 0f, 0f, 1f, 1f, CardUnlocked, true);
                var button = cardRoot.gameObject.AddComponent<Button>();
                button.targetGraphic = cardRoot.GetComponent<Image>();

                var thumbFrame = Panel("ThumbFrame", cardRoot, 0.05f, 0.26f, 0.95f, 0.95f, new Color(0.02f, 0.04f, 0.06f, 0.85f), false);
                var thumbRect = Rect("Thumbnail", thumbFrame, 0f, 0f, 1f, 1f);
                var raw = thumbRect.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                var fitter = thumbRect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1f;
                var lockOverlay = BuildLockOverlay(thumbFrame);

                var title = Label("CardTitle", cardRoot, 0.05f, 0.13f, 0.95f, 0.25f, "ILLUSTRATION #01", 21);
                var badge = Label("CardBadge", cardRoot, 0.05f, 0.02f, 0.95f, 0.13f, "VIEW FULL ART", 17);

                var view = new GalleryCardView
                {
                    Root = cardRoot,
                    Background = cardRoot.GetComponent<Image>(),
                    Thumbnail = raw,
                    Fitter = fitter,
                    LockOverlay = lockOverlay,
                    Title = title,
                    Badge = badge,
                    Button = button
                };
                button.onClick.AddListener(() =>
                {
                    if (view.StageIndex >= 0) TryOpenOriginalViewer(view.StageIndex);
                });
                galleryCards.Add(view);
            }

            galleryPagerBar = Rect("GalleryPager", galleryPanel, 0.04f, 0.02f, 0.96f, 0.095f);
            galleryPrevButton = MakeButton(galleryPagerBar, "PrevPage", "< Prev", 0f, 0.05f, 0.24f, 0.95f, PrevGalleryPage);
            galleryPageLabel = Label("PageLabel", galleryPagerBar, 0.26f, 0f, 0.74f, 1f, "GALLERY PAGE 1 / 1", 22);
            galleryNextButton = MakeButton(galleryPagerBar, "NextPage", "Next >", 0.76f, 0.05f, 1f, 0.95f, NextGalleryPage);
        }

        private void BuildViewerModal()
        {
            viewerModal = Panel("OriginalViewerModal", transform, 0f, 0f, 1f, 1f, new Color(0.01f, 0.02f, 0.04f, 0.98f), true);
            var artArea = Rect("ArtContainer", viewerModal, 0.03f, 0.14f, 0.97f, 0.96f);
            var rawRect = Rect("OriginalIllustration", artArea, 0f, 0f, 1f, 1f);
            viewerImage = rawRect.gameObject.AddComponent<RawImage>();
            viewerImage.raycastTarget = false;
            viewerFitter = rawRect.gameObject.AddComponent<AspectRatioFitter>();
            viewerFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            viewerFitter.aspectRatio = 1f;

            viewerCaption = Label("ViewerCaption", viewerModal, 0.04f, 0.065f, 0.72f, 0.13f, "", 20);
            viewerCloseButton = MakeButton(viewerModal, "CloseViewer", "Close Full View", 0.74f, 0.04f, 0.96f, 0.12f, CloseViewer);
        }

        private static GameObject BuildLockOverlay(RectTransform thumbFrame)
        {
            // Full opaque cover in front of the thumbnail so the original illustration is completely hidden when locked.
            var overlay = Panel("LockOverlay", thumbFrame, 0f, 0f, 1f, 1f, new Color(0.03f, 0.06f, 0.09f, 1f), false);
            overlay.SetAsLastSibling();

            var iconBox = Rect("PadlockIcon", overlay, 0.25f, 0.22f, 0.75f, 0.82f);
            var fitter = iconBox.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            overlay.GetComponent<Image>().color = new Color32(233,233,228,255);
            ConceptTheme.Art("LockArtwork", iconBox, "icon_lock", 0f, 0f, 1f, 1f);

            return overlay.gameObject;
        }

        private static string FormatTime(float seconds)
        {
            if (seconds >= float.MaxValue * 0.5f) return "--:--";
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchors(rect, x0, y0, x1, y1);
            return rect;
        }

        private static void SetAnchors(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Panel(string name, Transform parent, float x0, float y0, float x1, float y1, Color color, bool raycast)
        {
            var rect = Rect(name, parent, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, float x0, float y0, float x1, float y1, string text, int size)
        {
            var rect = Rect(name, parent, x0, y0, x1, y1);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(11f, size * 0.55f);
            label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextLight;
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(Transform parent, string name, string text, float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(name, parent, x0, y0, x1, y1, new Color(0.10f, 0.28f, 0.32f, 1f), true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Label("Label", rect, 0.04f, 0.04f, 0.96f, 0.96f, text, 24);
            if (action != null) button.onClick.AddListener(action);
            return button;
        }
    }
}

