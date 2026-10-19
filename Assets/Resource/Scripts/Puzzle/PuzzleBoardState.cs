using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Integer board state; rendering, advertisements and timers live outside this class.</summary>
    public sealed class PuzzleBoardState
    {
        private readonly Stack<int[]> history = new Stack<int[]>();
        private int[] cells;

        public int Columns { get; }
        public int Rows { get; }
        public int TileCount => cells.Length;
        public int BlankTile => TileCount - 1;
        public int BlankCell { get; private set; }
        public int MoveCount { get; private set; }
        public int UndoRemaining { get; private set; }
        public int UndoUsed { get; private set; }
        public bool HasHistory => history.Count > 0;
        public bool CanUndo => HasHistory && UndoRemaining > 0 && !IsSolved;

        public bool IsSolved
        {
            get
            {
                for (int i = 0; i < cells.Length; i++)
                    if (cells[i] != i) return false;
                return true;
            }
        }

        public PuzzleBoardState(int columns, int rows, int seed, int shuffleCount, int undoAllowance = 3)
        {
            if (columns < 3 || columns > 6) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 3 || rows > 8) throw new ArgumentOutOfRangeException(nameof(rows));
            if (shuffleCount < 1) throw new ArgumentOutOfRangeException(nameof(shuffleCount));
            if (undoAllowance < 0) throw new ArgumentOutOfRangeException(nameof(undoAllowance));
            Columns = columns;
            Rows = rows;
            cells = new int[columns * rows];
            for (int i = 0; i < cells.Length; i++) cells[i] = i;
            BlankCell = BlankTile;
            UndoRemaining = undoAllowance;
            Shuffle(seed, shuffleCount);
        }

        public int GetTileAt(int cell) => cells[cell];
        public int FindCell(int tile) => Array.IndexOf(cells, tile);
        public int[] CopyCells() => (int[])cells.Clone();

        public bool TryMove(int cell, bool multiPush)
        {
            if (cell < 0 || cell >= TileCount || cell == BlankCell || IsSolved) return false;
            int dx = cell % Columns - BlankCell % Columns;
            int dy = cell / Columns - BlankCell / Columns;
            if (dx != 0 && dy != 0) return false;
            if (!multiPush && Math.Abs(dx) + Math.Abs(dy) != 1) return false;

            history.Push(CopyCells());
            int step = dx == 0 ? Math.Sign(dy) * Columns : Math.Sign(dx);
            while (BlankCell != cell) SwapBlank(BlankCell + step);
            MoveCount++;
            return true;
        }

        public bool TryUndo()
        {
            if (!CanUndo) return false;
            cells = history.Pop();
            BlankCell = FindCell(BlankTile);
            UndoRemaining--;
            UndoUsed++;
            // Undo is tracked separately and does not add to the cumulative move count.
            return true;
        }

        public bool TryRefillUndo(int amount)
        {
            if (amount <= 0 || UndoRemaining != 0 || IsSolved) return false;
            UndoRemaining = amount;
            return true;
        }

        private void Shuffle(int seed, int count)
        {
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(seed);
                int previousBlank = -1;
                var candidates = new List<int>(4);
                for (int i = 0; i < count || IsSolved; i++)
                {
                    candidates.Clear();
                    int col = BlankCell % Columns;
                    int row = BlankCell / Columns;
                    // Version 1 candidate order is fixed: up, right, down, left.
                    if (row > 0) candidates.Add(BlankCell - Columns);
                    if (col < Columns - 1) candidates.Add(BlankCell + 1);
                    if (row < Rows - 1) candidates.Add(BlankCell + Columns);
                    if (col > 0) candidates.Add(BlankCell - 1);
                    candidates.Remove(previousBlank);
                    int next = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    previousBlank = BlankCell;
                    SwapBlank(next);
                }
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
            }
        }

        private void SwapBlank(int next)
        {
            cells[BlankCell] = cells[next];
            cells[next] = BlankTile;
            BlankCell = next;
        }
    }
}
