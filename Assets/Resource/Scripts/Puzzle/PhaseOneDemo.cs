using TMPro;
using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Test-scene controls. Production settings, timers and ad SDK belong to later phases.</summary>
    public sealed class PhaseOneDemo : MonoBehaviour
    {
        [SerializeField] private BoardManager board;
        [SerializeField] private StageData[] stages;
        [SerializeField] private TMP_Text status;
        [SerializeField] private UnityEngine.UI.Button undoButton;
        [SerializeField] private UnityEngine.UI.Button rewardButton;
        [SerializeField] private TMP_Text multiLabel;
        [SerializeField] private TMP_Text animationLabel;
        [SerializeField] private TMP_Text numbersLabel;
        private int stageIndex;

        private void OnEnable()
        {
            board.StateChanged += Refresh;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            board.UndoRewardRequested += SimulateReward;
#endif
            Refresh();
        }

        private void OnDisable()
        {
            board.StateChanged -= Refresh;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            board.UndoRewardRequested -= SimulateReward;
#endif
        }

        public void Undo() => board.TryUndo();
        public void Reward() => board.RequestUndoReward();
        public void Restart() => board.Restart();
        public void ToggleMultiPush() => board.SetMultiPush(!board.MultiPush);
        public void ToggleAnimation() => board.SetSlideAnimation(!board.SlideAnimation);
        public void ToggleNumbers() => board.SetNumberGuide(!board.NumberGuide);

        public void NextStage()
        {
            if (stages == null || stages.Length == 0) return;
            stageIndex = (stageIndex + 1) % stages.Length;
            board.LoadStage(stages[stageIndex]);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void SimulateReward(int ticket)
        {
            // Explicitly a test-only stand-in; never grant production rewards on ad close/request.
            board.CompleteUndoReward(ticket, true);
        }
#endif

        private void Refresh()
        {
            StageData stage = board.Stage;
            string title = stage == null ? "PHASE 1" : $"{stage.StageId}  |  {stage.Columns} x {stage.Rows}";
            status.text = $"{title}\nMoves {board.MoveCount}    Undo left {board.UndoRemaining}    Undo used {board.UndoUsed}"
                + (board.IsSolved ? "\nCOMPLETE" : "");
            undoButton.interactable = board.CanUndo;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            rewardButton.interactable = board.CanRequestUndoReward;
#else
            rewardButton.interactable = false;
#endif
            multiLabel.text = "Line slide: " + (board.MultiPush ? "ON" : "OFF");
            animationLabel.text = "Animation: " + (board.SlideAnimation ? "ON" : "OFF");
            numbersLabel.text = "Numbers: " + (board.NumberGuide ? "ON" : "OFF");
        }
    }
}
