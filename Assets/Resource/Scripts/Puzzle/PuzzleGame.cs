using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstPuzzle
{
    /// <summary>Offline game flow shared by the authored game scene.</summary>
    public sealed class PuzzleGame : MonoBehaviour
    {
        public BoardManager board;
        public StageData[] stages;
        public TMP_Text heading, stats, message, numbersLabel, lineLabel, animationLabel;
        public UnityEngine.UI.Button undoButton, pauseButton, nextButton, resumeButton;
        public GameObject curtain, pauseControls, resultControls;
        public UnityEngine.UI.RawImage preview;
        private int index;
        private float elapsed;
        private bool countdown;
        private Coroutine resumeRoutine;
        public float Elapsed => elapsed;

        private void OnEnable()
        {
            board.StateChanged += Refresh;
            board.ClearPresentationFinished += Completed;
        }

        private void Start() => Load(0);

        private void OnDisable()
        {
            board.StateChanged -= Refresh;
            board.ClearPresentationFinished -= Completed;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            countdown = false;
        }

        private void Update()
        {
            if (board.IsReady && !board.IsPaused && !board.IsSolved)
                elapsed += Time.deltaTime;
            RefreshStats();
        }

        public void Load(int stageIndex)
        {
            if (stages == null || stages.Length == 0) return;
            if (resumeRoutine != null) StopCoroutine(resumeRoutine);
            countdown = false;
            index = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
            elapsed = 0;
            curtain.SetActive(false);
            preview.gameObject.SetActive(false);
            board.SetMultiPush(PlayerPrefs.GetInt("PREF_MULTIPUSH", 1) != 0);
            board.SetSlideAnimation(PlayerPrefs.GetInt("PREF_SLIDE_ANIM", 1) != 0);
            board.SetNumberGuide(PlayerPrefs.GetInt("PUZZLE_NUMBERS", 1) != 0);
            board.LoadStage(stages[index]);
            preview.texture = stages[index].Illustration;
            preview.uvRect = stages[index].GetImageUV();
            var aspect = preview.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectRatio = (float)stages[index].Columns / stages[index].Rows;
            Refresh();
        }

        public void Undo() => board.TryUndo();
        public void Restart() => Load(index);
        public void Next() { if (board.IsSolved) Load((index + 1) % stages.Length); }
        public void Numbers()
        {
            board.SetNumberGuide(!board.NumberGuide);
            PlayerPrefs.SetInt("PUZZLE_NUMBERS", board.NumberGuide ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void Lines()
        {
            board.SetMultiPush(!board.MultiPush);
            PlayerPrefs.SetInt("PREF_MULTIPUSH", board.MultiPush ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void Animation()
        {
            board.SetSlideAnimation(!board.SlideAnimation);
            PlayerPrefs.SetInt("PREF_SLIDE_ANIM", board.SlideAnimation ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void ShowPreview(bool show)
        {
            preview.gameObject.SetActive(show && board.CanInteract && !countdown);
        }
        public void Pause()
        {
            if (!board.IsReady || board.IsSolved || board.IsPaused) return;
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
            string key = "PUZZLE_BEST_" + stages[index].StageId;
            int best = PlayerPrefs.GetInt(key, int.MaxValue);
            PlayerPrefs.SetInt(key, Mathf.Min(best, board.MoveCount));
            PlayerPrefs.SetInt("PUZZLE_CLEAR_" + stages[index].StageId, 1);
            PlayerPrefs.Save();
            pauseControls.SetActive(false);
            resultControls.SetActive(true);
            curtain.SetActive(true);
            message.text = $"BEAUTIFULLY DONE\n\n{board.MoveCount} moves  /  {TimeText(elapsed)}\nBest: {Mathf.Min(best, board.MoveCount)} moves";
            nextButton.GetComponentInChildren<TMP_Text>().text =
                index == stages.Length - 1 ? "Play again" : "Next illustration";
        }
        private void Refresh()
        {
            heading.text = $"PICTURE PUZZLE\n{index + 1:00} / {stages.Length:00}    |    {board.Stage.Columns} x {board.Stage.Rows}";
            undoButton.interactable = board.CanUndo;
            pauseButton.interactable = !board.IsPaused && !board.IsSolved;
            numbersLabel.text = "Numbers: " + (board.NumberGuide ? "ON" : "OFF");
            lineLabel.text = "Line slide: " + (board.MultiPush ? "ON" : "OFF");
            animationLabel.text = "Animation: " + (board.SlideAnimation ? "ON" : "OFF");
            RefreshStats();
        }
        private void RefreshStats() => stats.text = $"{TimeText(elapsed)}    /    {board.MoveCount} MOVES    /    {board.UndoRemaining} UNDO";
        private static string TimeText(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
        private void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
        private void OnApplicationPause(bool paused) { if (paused) Pause(); }
    }
}
