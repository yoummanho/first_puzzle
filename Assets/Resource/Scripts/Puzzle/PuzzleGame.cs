using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    /// <summary>Offline game flow shared by the authored game scene (Phase 2 & Phase 3 integrated).</summary>
    public sealed class PuzzleGame : MonoBehaviour
    {
        public BoardManager board;
        public StageData[] stages;
        public TMP_Text heading, stats, message, numbersLabel, lineLabel, animationLabel;
        public Button undoButton, pauseButton, nextButton, resumeButton;
        public GameObject curtain, pauseControls, resultControls;
        public RawImage preview;

        private int index;
        private float elapsed;
        private bool countdown;
        private Coroutine resumeRoutine;
        private TMP_Text bgmLabel, sfxLabel, orientationLabel;
        private Button pauseLobbyButton, resultLobbyButton;
        private bool previewHeldFromPauseMenu;

        public int CurrentStageIndex => index;
        public float Elapsed => elapsed;
        public SettingsManager Settings { get; private set; }
        public PuzzleLobbyGalleryUI Lobby { get; private set; }
        public PuzzleResponsiveLayout ResponsiveLayout { get; private set; }
        public PuzzleAudio Audio { get; private set; }
        public bool IsCountingDown => countdown;
        public PauseTab ActivePauseTab { get; private set; } = PauseTab.Game;

        private void Awake()
        {
            Settings = new SettingsManager();
            PauseSettingsUI.Install(
                this,
                out bgmLabel,
                out sfxLabel,
                out orientationLabel,
                out pauseLobbyButton,
                out resultLobbyButton);

            ResponsiveLayout = GetComponent<PuzzleResponsiveLayout>();
            if (ResponsiveLayout == null) ResponsiveLayout = gameObject.AddComponent<PuzzleResponsiveLayout>();
            ResponsiveLayout.Initialize(this);

            Lobby = PuzzleLobbyGalleryUI.Install(this, ResponsiveLayout.SafeAreaRect != null ? ResponsiveLayout.SafeAreaRect : transform);

            Audio = GetComponent<PuzzleAudio>();
            if (Audio == null) Audio = gameObject.AddComponent<PuzzleAudio>();
            Audio.Initialize(Settings, board);
            ConceptTheme.Install(this);
        }

        private void OnEnable()
        {
            board.StateChanged += Refresh;
            board.ClearPresentationFinished += Completed;
            Settings.Changed += ApplySettings;
        }

        private void Start() => OpenTitleScreen();

        private void OnDisable()
        {
            board.StateChanged -= Refresh;
            board.ClearPresentationFinished -= Completed;
            Settings.Changed -= ApplySettings;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            countdown = false;
        }

        private void Update()
        {
            bool overlayOpen = Lobby != null && (Lobby.IsTitleOpen || Lobby.IsLobbyOpen || Lobby.IsGalleryOpen || Lobby.IsViewerOpen);
            if (board.IsReady && !board.IsPaused && !board.IsSolved && !overlayOpen)
                elapsed += Time.deltaTime;
            RefreshStats();
        }

        public void OpenTitleScreen()
        {
            if (stages != null && stages.Length > 0 && !board.IsReady)
                LoadStageInternal(0, false);
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null;
            countdown = false;
            previewHeldFromPauseMenu = false;
            ShowPreview(false);
            if (board.IsReady) board.SetPaused(true);
            curtain.SetActive(false);
            if (Lobby != null) Lobby.OpenTitle();
            if (Audio != null) Audio.SetMusicContext(MusicContext.Title);
            Refresh();
        }

        public void EnterLobbyFromTitle() => OpenLobby();

        public void EnterInGameFromLobby() => Load(index);

        public void Load(int stageIndex) => LoadStageInternal(stageIndex, true);

        private void LoadStageInternal(int stageIndex, bool enterInGame)
        {
            if (stages == null || stages.Length == 0) return;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null;
            countdown = false;
            previewHeldFromPauseMenu = false;
            index = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
            elapsed = 0f;
            curtain.SetActive(false);
            preview.gameObject.SetActive(false);
            if (enterInGame && Lobby != null) Lobby.CloseAll();
            ApplySettings();
            board.SetNumberGuide(PlayerPrefs.GetInt("PUZZLE_NUMBERS", 0) != 0);
            board.LoadStage(stages[index]);
            preview.texture = stages[index].Illustration;
            preview.uvRect = stages[index].GetImageUV();
            var aspect = preview.GetComponent<AspectRatioFitter>();
            aspect.aspectRatio = (float)stages[index].Columns / stages[index].Rows;
            if (enterInGame && Audio != null)
            {
                Audio.PlayGameStartThenInGameMusic();
            }
            Refresh();
        }

        public void Undo()
        {
            if (board.IsPaused && !countdown)
            {
                board.SetPaused(false);
                bool undone = board.TryUndo();
                board.SetPaused(true);
                if (undone) Resume();
                else Refresh();
                return;
            }
            board.TryUndo();
        }

        public void Restart() => Load(index);
        public void Next()
        {
            if (!board.IsSolved || stages == null || stages.Length == 0) return;
            Load(PuzzleProgress.NextPlayableIndex(stages, index));
        }

        public void OpenLobby()
        {
            if (Lobby == null) return;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null;
            countdown = false;
            previewHeldFromPauseMenu = false;
            ShowPreview(false);
            if (board.IsReady && !board.IsSolved)
                board.SetPaused(true);
            curtain.SetActive(false);
            Lobby.OpenLobby();
            if (Audio != null) Audio.SetMusicContext(MusicContext.Lobby);
            Refresh();
        }

        public void OpenGallery()
        {
            if (Lobby == null) return;
            OpenLobby();
            Lobby.OpenGallery();
        }

        public void SelectPauseTab(PauseTab tab)
        {
            ActivePauseTab = tab;
            PauseSettingsUI.RefreshTabs(this);
        }

        public void SetPauseMenuPreviewHold(bool holding)
        {
            if (!holding)
            {
                if (previewHeldFromPauseMenu)
                {
                    previewHeldFromPauseMenu = false;
                    if (board.IsPaused && !countdown)
                    {
                        curtain.SetActive(true);
                        pauseControls.SetActive(true);
                    }
                }
                preview.gameObject.SetActive(false);
                return;
            }

            bool overlayOpen = Lobby != null && (Lobby.IsTitleOpen || Lobby.IsLobbyOpen || Lobby.IsGalleryOpen || Lobby.IsViewerOpen);
            if (!board.IsReady || board.IsSolved || countdown || overlayOpen) return;

            if (board.IsPaused && pauseControls.activeInHierarchy)
            {
                previewHeldFromPauseMenu = true;
                curtain.SetActive(false);
                preview.gameObject.SetActive(true);
            }
            else if (board.CanInteract)
            {
                preview.gameObject.SetActive(true);
            }
        }

        public void Numbers()
        {
            board.SetNumberGuide(!board.NumberGuide);
            PlayerPrefs.SetInt("PUZZLE_NUMBERS", board.NumberGuide ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Lines() => Settings.SetMultiPush(!Settings.MultiPush);
        public void Animation() => Settings.SetSlideAnimation(!Settings.SlideAnimation);
        public void ToggleBgm() => Settings.SetBgm(!Settings.Bgm);
        public void ToggleSfx() => Settings.SetSfx(!Settings.Sfx);

        public void ToggleOrientation()
        {
            // SPEC Phase 3: Mid-game orientation change happens while paused, preserving board/moves/undo/time, resuming via 3-2-1.
            bool inActivePlay = board != null && board.IsReady && !board.IsSolved &&
                                (Lobby == null || (!Lobby.IsTitleOpen && !Lobby.IsLobbyOpen && !Lobby.IsGalleryOpen && !Lobby.IsViewerOpen));
            if (inActivePlay && !board.IsPaused)
                Pause();
            Settings.ToggleOrientation();
        }

        private void ApplySettings()
        {
            board.SetMultiPush(Settings.MultiPush);
            board.SetSlideAnimation(Settings.SlideAnimation);
            if (bgmLabel != null) bgmLabel.text = "Music: " + (Settings.Bgm ? "ON" : "OFF");
            if (sfxLabel != null) sfxLabel.text = "Sound: " + (Settings.Sfx ? "ON" : "OFF");
            if (orientationLabel != null)
                orientationLabel.text = "Orientation: " + (Settings.IsLandscape ? "LANDSCAPE" : "PORTRAIT");
            if (ResponsiveLayout != null) ResponsiveLayout.Apply(true);
        }

        public void ShowPreview(bool show)
        {
            if (!show && previewHeldFromPauseMenu)
            {
                SetPauseMenuPreviewHold(false);
                return;
            }
            bool overlayOpen = Lobby != null && (Lobby.IsTitleOpen || Lobby.IsLobbyOpen || Lobby.IsGalleryOpen || Lobby.IsViewerOpen);
            preview.gameObject.SetActive(show && board.CanInteract && !countdown && !overlayOpen);
        }

        public void Pause()
        {
            if (!board.IsReady || board.IsSolved) return;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null;
            countdown = false;
            previewHeldFromPauseMenu = false;
            ShowPreview(false);
            board.SetPaused(true);
            curtain.SetActive(true);
            pauseControls.SetActive(true);
            resultControls.SetActive(false);
            message.text = "PAUSED";
            PauseSettingsUI.RefreshTabs(this);
            Refresh();
        }

        public void Resume()
        {
            if (!board.IsPaused || countdown) return;
            previewHeldFromPauseMenu = false;
            preview.gameObject.SetActive(false);
            curtain.SetActive(true);
            if (Lobby != null) Lobby.CloseAll();
            if (Audio != null) Audio.SetMusicContext(MusicContext.InGame);
            resumeRoutine = StartCoroutine(Countdown());
        }

        private IEnumerator Countdown()
        {
            countdown = true;
            pauseControls.SetActive(false);
            for (int i = 3; i > 0; i--)
            {
                message.text = i.ToString();
                yield return new WaitForSecondsRealtime(1);
            }
            curtain.SetActive(false);
            countdown = false;
            board.SetPaused(false);
            resumeRoutine = null;
        }

        private void Completed()
        {
            int previousBest = PuzzleProgress.GetBestMoves(stages[index]);
            PuzzleProgress.RecordClear(stages[index], board.MoveCount, elapsed);
            int bestMoves = Mathf.Min(previousBest, board.MoveCount);
            pauseControls.SetActive(false);
            resultControls.SetActive(true);
            curtain.SetActive(true);
            message.text = $"BEAUTIFULLY DONE\n\n{board.MoveCount} moves  /  {TimeText(elapsed)}\nBest: {bestMoves} moves";
            nextButton.GetComponentInChildren<TMP_Text>().text =
                index == stages.Length - 1 ? "Play again" : "Next illustration";
            if (Lobby != null && Lobby.IsLobbyOpen) Lobby.RefreshLobby();
        }

        private void Refresh()
        {
            if (board == null || board.Stage == null) return;
            heading.text = $"PICTURE PUZZLE\n{index + 1:00} / {stages.Length:00}    |    {board.Stage.Columns} x {board.Stage.Rows}";
            bool canUndoNow = board.CanUndo || (board.IsPaused && !countdown && !board.IsBusy && !board.IsSolved && board.UndoRemaining > 0 && board.MoveCount > 0);
            if (undoButton != null) undoButton.interactable = canUndoNow;
            if (pauseButton != null) pauseButton.interactable = !board.IsPaused && !board.IsSolved;
            if (numbersLabel != null) numbersLabel.text = "Number: " + (board.NumberGuide ? "ON" : "OFF");
            if (lineLabel != null) lineLabel.text = "Line slide: " + (board.MultiPush ? "ON" : "OFF");
            if (animationLabel != null) animationLabel.text = "Animation: " + (board.SlideAnimation ? "ON" : "OFF");
            if (orientationLabel != null && Settings != null)
                orientationLabel.text = "Orientation: " + (Settings.IsLandscape ? "LANDSCAPE" : "PORTRAIT");
            RefreshStats();
        }

        private void RefreshStats() => stats.text = $"{TimeText(elapsed)}    /    {board.MoveCount} MOVES    /    {board.UndoRemaining} UNDO";
        private static string TimeText(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";

        private void OnApplicationFocus(bool focused)
        {
            // Do not auto-pause on window focus change before pressing the Pause button (especially in Unity Editor).
            if (!focused && countdown) Pause();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            if (Application.isMobilePlatform || countdown) Pause();
        }
    }
}
