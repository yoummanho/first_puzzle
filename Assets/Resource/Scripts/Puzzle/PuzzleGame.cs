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

        public int CurrentStageIndex => index;
        public float Elapsed => elapsed;
        public SettingsManager Settings { get; private set; }
        public PuzzleLobbyGalleryUI Lobby { get; private set; }
        public PuzzleResponsiveLayout ResponsiveLayout { get; private set; }
        public bool IsCountingDown => countdown;

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

            var audio = GetComponent<PuzzleAudio>();
            if (audio == null) audio = gameObject.AddComponent<PuzzleAudio>();
            audio.Initialize(Settings, board);
        }

        private void OnEnable()
        {
            board.StateChanged += Refresh;
            board.ClearPresentationFinished += Completed;
            Settings.Changed += ApplySettings;
        }

        private void Start() => Load(0);

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
            bool overlayOpen = Lobby != null && (Lobby.IsLobbyOpen || Lobby.IsGalleryOpen || Lobby.IsViewerOpen);
            if (board.IsReady && !board.IsPaused && !board.IsSolved && !overlayOpen)
                elapsed += Time.deltaTime;
            RefreshStats();
        }

        public void Load(int stageIndex)
        {
            if (stages == null || stages.Length == 0) return;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null;
            countdown = false;
            index = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
            elapsed = 0f;
            curtain.SetActive(false);
            preview.gameObject.SetActive(false);
            if (Lobby != null) Lobby.CloseAll();
            ApplySettings();
            board.SetNumberGuide(PlayerPrefs.GetInt("PUZZLE_NUMBERS", 0) != 0);
            board.LoadStage(stages[index]);
            preview.texture = stages[index].Illustration;
            preview.uvRect = stages[index].GetImageUV();
            var aspect = preview.GetComponent<AspectRatioFitter>();
            aspect.aspectRatio = (float)stages[index].Columns / stages[index].Rows;
            Refresh();
        }

        public void Undo() => board.TryUndo();
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
            ShowPreview(false);
            if (board.IsReady && !board.IsSolved)
            {
                board.SetPaused(true);
                curtain.SetActive(true);
                pauseControls.SetActive(true);
                resultControls.SetActive(false);
                message.text = "PAUSED";
            }
            Lobby.OpenLobby();
            Refresh();
        }

        public void OpenGallery()
        {
            if (Lobby == null) return;
            OpenLobby();
            Lobby.OpenGallery();
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
                                (Lobby == null || (!Lobby.IsLobbyOpen && !Lobby.IsGalleryOpen && !Lobby.IsViewerOpen));
            if (inActivePlay && !board.IsPaused)
                Pause();
            Settings.ToggleOrientation();
        }

        private void ApplySettings()
        {
            board.SetMultiPush(Settings.MultiPush);
            board.SetSlideAnimation(Settings.SlideAnimation);
            if (bgmLabel != null) bgmLabel.text = "Music: " + (Settings.Bgm ? "ON" : "OFF");
            if (sfxLabel != null) sfxLabel.text = "Sound effects: " + (Settings.Sfx ? "ON" : "OFF");
            if (orientationLabel != null)
                orientationLabel.text = "Orientation: " + (Settings.IsLandscape ? "LANDSCAPE" : "PORTRAIT");
            if (ResponsiveLayout != null) ResponsiveLayout.Apply(true);
        }

        public void ShowPreview(bool show)
        {
            bool overlayOpen = Lobby != null && (Lobby.IsLobbyOpen || Lobby.IsGalleryOpen || Lobby.IsViewerOpen);
            preview.gameObject.SetActive(show && board.CanInteract && !countdown && !overlayOpen);
        }

        public void Pause()
        {
            if (!board.IsReady || board.IsSolved) return;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            resumeRoutine = null;
            countdown = false;
            ShowPreview(false);
            board.SetPaused(true);
            curtain.SetActive(true);
            pauseControls.SetActive(true);
            resultControls.SetActive(false);
            message.text = "PAUSED";
        }

        public void Resume()
        {
            if (!board.IsPaused || countdown) return;
            if (Lobby != null) Lobby.CloseAll();
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
            undoButton.interactable = board.CanUndo;
            pauseButton.interactable = !board.IsPaused && !board.IsSolved;
            numbersLabel.text = "Numbers: " + (board.NumberGuide ? "ON" : "OFF");
            lineLabel.text = "Line slide: " + (board.MultiPush ? "ON" : "OFF");
            animationLabel.text = "Animation: " + (board.SlideAnimation ? "ON" : "OFF");
            if (orientationLabel != null && Settings != null)
                orientationLabel.text = "Orientation: " + (Settings.IsLandscape ? "LANDSCAPE" : "PORTRAIT");
            RefreshStats();
        }

        private void RefreshStats() => stats.text = $"{TimeText(elapsed)}    /    {board.MoveCount} MOVES    /    {board.UndoRemaining} UNDO";
        private static string TimeText(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
        private void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
        private void OnApplicationPause(bool paused) { if (paused) Pause(); }
    }
}
