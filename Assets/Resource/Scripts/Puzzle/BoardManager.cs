using System;
using System.Collections;
using UnityEngine;

namespace FirstPuzzle
{
    public sealed class BoardManager : MonoBehaviour
    {
        [SerializeField] private StageData stage;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private PuzzleTile tilePrefab;
        [SerializeField, Min(0)] private float tileGap = 4f;
        [SerializeField, Range(0.08f, 0.1f)] private float slideDuration = 0.09f;
        [SerializeField, Min(0.01f)] private float clearDuration = 0.4f;
        [SerializeField, Min(0)] private int initialUndoAllowance = 3;
        [SerializeField, Min(1)] private int rewardedUndoAllowance = 3;
        [SerializeField] private bool multiPush = true;
        [SerializeField] private bool slideAnimation = true;
        [SerializeField] private bool numberGuide = true;

        private PuzzleBoardState state;
        private PuzzleTile[] tiles;
        private Vector2 lastAreaSize;
        private bool busy;
        private bool paused;
        private float clearProgress;
        private int generation;
        private int rewardSequence;
        private int pendingReward;

        public StageData Stage => stage;
        public int MoveCount => state?.MoveCount ?? 0;
        public int UndoRemaining => state?.UndoRemaining ?? 0;
        public int UndoUsed => state?.UndoUsed ?? 0;
        public bool IsReady => state != null;
        public bool IsBusy => busy;
        public bool IsPaused => paused;
        public bool IsSolved => state != null && state.IsSolved;
        public bool MultiPush => multiPush;
        public bool SlideAnimation => slideAnimation;
        public bool NumberGuide => numberGuide;
        public bool IsRewardPending => pendingReward != 0;
        public bool CanInteract => isActiveAndEnabled && IsReady && !busy && !paused && !IsSolved && !IsRewardPending;
        public bool CanUndo => CanInteract && state.CanUndo;
        public bool CanRequestUndoReward => CanInteract && UndoRemaining == 0;

        public event Action StateChanged;
        public event Action PuzzleSolved;
        public event Action ClearPresentationFinished;
        /// <summary>The ad adapter must return this ticket with the verified reward/cancel callback.</summary>
        public event Action<int> UndoRewardRequested;

        private void Start()
        {
            if (state == null && stage != null) LoadStage(stage);
        }

        public bool LoadStage(StageData nextStage)
        {
            if (nextStage == null || !nextStage.Validate(out string _))
            {
                Debug.LogError("Cannot load puzzle: invalid StageData.", this);
                return false;
            }
            if (boardArea == null || tilePrefab == null)
            {
                Debug.LogError("Assign the board area and PuzzleTile prefab before loading a stage.", this);
                return false;
            }
            if (boardArea.TryGetComponent(out UnityEngine.UI.LayoutGroup layout) && layout.enabled)
            {
                Debug.LogError("The animated board area must not have an enabled LayoutGroup.", this);
                return false;
            }

            StopAllCoroutines();
            generation++;
            pendingReward = 0;
            if (tiles != null)
            {
                foreach (PuzzleTile tile in tiles)
                {
                    if (tile == null) continue;
                    tile.gameObject.SetActive(false);
                    Destroy(tile.gameObject);
                }
            }
            stage = nextStage;
            state = new PuzzleBoardState(stage.Columns, stage.Rows, stage.ShuffleSeed,
                stage.ShuffleCount, initialUndoAllowance);
            busy = false;
            paused = false;
            clearProgress = 0f;
            tiles = new PuzzleTile[state.TileCount];
            for (int id = 0; id < tiles.Length; id++)
            {
                PuzzleTile tile = Instantiate(tilePrefab, boardArea);
                tile.name = id == state.BlankTile ? "Blank" : $"Tile_{id + 1:00}";
                tile.Initialize(stage, id, OnTilePressed);
                tile.SetNumbersVisible(numberGuide);
                tiles[id] = tile;
            }
            Canvas.ForceUpdateCanvases();
            RefreshLayout();
            StateChanged?.Invoke();
            return true;
        }

        public void Restart() => LoadStage(stage);
        private void OnTilePressed(int tile) => TryMoveTile(tile);

        public bool TryMoveTile(int tile)
        {
            if (!CanInteract || tile < 0 || tile >= state.TileCount) return false;
            if (!state.TryMove(state.FindCell(tile), multiPush)) return false;
            BeginTransition();
            return true;
        }

        public bool TryUndo()
        {
            if (!CanUndo || !state.TryUndo()) return false;
            BeginTransition();
            return true;
        }

        private void BeginTransition()
        {
            busy = true;
            int activeGeneration = generation;
            float duration = slideAnimation ? slideDuration : 0f;
            for (int id = 0; id < tiles.Length; id++)
                tiles[id].MoveTo(GetPosition(state.FindCell(id)), duration);
            StateChanged?.Invoke();
            if (generation != activeGeneration) return;
            if (state.IsSolved) PuzzleSolved?.Invoke();
            if (generation == activeGeneration) StartCoroutine(FinishTransition(activeGeneration));
        }

        private IEnumerator FinishTransition(int activeGeneration)
        {
            while (AnyTileMoving() || paused) yield return null;
            if (generation != activeGeneration) yield break;
            if (state.IsSolved)
            {
                SetNumberGuide(numberGuide);
                while (clearProgress < 1f)
                {
                    if (!paused)
                    {
                        clearProgress = Mathf.Min(1f, clearProgress + Time.deltaTime / clearDuration);
                        RefreshLayout();
                        tiles[state.BlankTile].SetImageAlpha(clearProgress);
                    }
                    yield return null;
                }
                busy = false;
                StateChanged?.Invoke();
                if (generation == activeGeneration) ClearPresentationFinished?.Invoke();
            }
            else
            {
                busy = false;
                StateChanged?.Invoke();
            }
        }

        private bool AnyTileMoving()
        {
            foreach (PuzzleTile tile in tiles) if (tile.IsMoving) return true;
            return false;
        }

        public void SetMultiPush(bool value) { multiPush = value; StateChanged?.Invoke(); }
        public void SetSlideAnimation(bool value) { slideAnimation = value; StateChanged?.Invoke(); }

        public void SetNumberGuide(bool value)
        {
            numberGuide = value;
            if (tiles != null)
                foreach (PuzzleTile tile in tiles) tile.SetNumbersVisible(value && !IsSolved);
            StateChanged?.Invoke();
        }

        /// <summary>Phase 2 must also cover the board and stop its timer while paused.</summary>
        public void SetPaused(bool value)
        {
            paused = value;
            if (tiles != null)
                foreach (PuzzleTile tile in tiles) tile.MotionPaused = value;
            StateChanged?.Invoke();
        }

        public bool RequestUndoReward()
        {
            if (!CanRequestUndoReward || UndoRewardRequested == null) return false;
            pendingReward = ++rewardSequence;
            if (pendingReward == 0) pendingReward = ++rewardSequence;
            int ticket = pendingReward;
            StateChanged?.Invoke();
            if (pendingReward == ticket) UndoRewardRequested?.Invoke(ticket);
            return true;
        }

        /// <summary>Only call with rewardEarned=true from the ad provider's reward callback.</summary>
        public bool CompleteUndoReward(int ticket, bool rewardEarned)
        {
            if (ticket == 0 || ticket != pendingReward) return false;
            pendingReward = 0;
            bool granted = rewardEarned && state != null && state.TryRefillUndo(rewardedUndoAllowance);
            StateChanged?.Invoke();
            return granted;
        }

        private float CellPitch => Mathf.Max(0f, Mathf.Min(boardArea.rect.width / state.Columns,
            boardArea.rect.height / state.Rows));

        private Vector2 GetPosition(int cell)
        {
            float pitch = CellPitch;
            return new Vector2((cell % state.Columns - (state.Columns - 1) * 0.5f) * pitch,
                ((state.Rows - 1) * 0.5f - cell / state.Columns) * pitch);
        }

        private void RefreshLayout()
        {
            if (state == null || boardArea == null) return;
            lastAreaSize = boardArea.rect.size;
            float side = Mathf.Max(0f, CellPitch - Mathf.Min(tileGap, CellPitch * 0.25f) * (1f - clearProgress));
            for (int id = 0; id < tiles.Length; id++)
                tiles[id].SetGeometry(GetPosition(state.FindCell(id)), side);
        }

        private void LateUpdate()
        {
            if (state == null || boardArea == null || lastAreaSize == boardArea.rect.size) return;
            foreach (PuzzleTile tile in tiles) tile.StopMotion();
            RefreshLayout();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (tiles != null) foreach (PuzzleTile tile in tiles) tile.StopMotion();
            pendingReward = 0;
            busy = false;
        }

        private void OnEnable()
        {
            if (state == null) return;
            RefreshLayout();
            if (state.IsSolved && clearProgress < 1f)
            {
                busy = true;
                StartCoroutine(FinishTransition(generation));
            }
        }
    }
}
