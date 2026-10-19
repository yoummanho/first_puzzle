using System;
using System.Linq;
using FirstPuzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FirstPuzzleTests
{
    public class PuzzleCoreTests
    {
        [TestCase(3, 3)] [TestCase(3, 4)] [TestCase(4, 4)]
        [TestCase(4, 5)] [TestCase(5, 6)] [TestCase(6, 8)]
        public void SeededBoardsAreReproducibleUnsolvedAndSolvable(int cols, int rows)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var board = new PuzzleBoardState(cols, rows, seed, 500);
                CollectionAssert.AreEqual(board.CopyCells(), new PuzzleBoardState(cols, rows, seed, 500).CopyCells());
                Assert.IsFalse(board.IsSolved);
                Assert.IsFalse(board.HasHistory);
                Assert.AreEqual(0, board.MoveCount);
                int[] cells = board.CopyCells();
                CollectionAssert.AreEquivalent(Enumerable.Range(0, cols * rows), cells);
                int inversions = 0;
                for (int i = 0; i < cells.Length; i++)
                    for (int j = i + 1; j < cells.Length; j++)
                        if (cells[i] != board.BlankTile && cells[j] != board.BlankTile && cells[i] > cells[j]) inversions++;
                int parity = cols % 2 == 1 ? inversions % 2 : (inversions + rows - board.BlankCell / cols) % 2;
                Assert.AreEqual(cols % 2 == 1 ? 0 : 1, parity, "Unsolvable permutation.");
            }
        }

        [Test]
        public void ShufflePreservesTheGlobalRandomStream()
        {
            UnityEngine.Random.State saved = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(81);
                UnityEngine.Random.State before = UnityEngine.Random.state;
                var board = new PuzzleBoardState(6, 8, 42, 1000);
                float actual = UnityEngine.Random.value;
                UnityEngine.Random.state = before;
                Assert.AreEqual(UnityEngine.Random.value, actual);
            }
            finally { UnityEngine.Random.state = saved; }
        }

        [Test]
        public void MultiPushIsOneMoveAndUndoRestoresTheEntireLine()
        {
            var board = new PuzzleBoardState(6, 8, 42, 500);
            int[] original = board.CopyCells();
            int row = board.BlankCell / board.Columns;
            int col = board.BlankCell % board.Columns;
            int target = row * board.Columns + (col < 3 ? 5 : 0);
            Assert.IsFalse(board.TryMove(target, false));
            Assert.IsTrue(board.TryMove(target, true));
            Assert.AreEqual(1, board.MoveCount);
            Assert.AreEqual(target, board.BlankCell);
            Assert.IsTrue(board.TryUndo());
            CollectionAssert.AreEqual(original, board.CopyCells());
            Assert.AreEqual(1, board.MoveCount);
            Assert.AreEqual(1, board.UndoUsed);
            Assert.AreEqual(2, board.UndoRemaining);
        }

        [Test]
        public void UndoBudgetOnlyConsumesSuccessfulUndosAndRefillsOnlyWhenEmpty()
        {
            var board = new PuzzleBoardState(3, 4, 42, 500);
            Assert.IsFalse(board.TryUndo());
            Assert.AreEqual(3, board.UndoRemaining);
            Assert.IsFalse(board.TryRefillUndo(3));
            int target = board.BlankCell % 3 == 0 ? board.BlankCell + 1 : board.BlankCell - 1;
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(board.TryMove(target, false));
                Assert.IsTrue(board.TryUndo());
            }
            Assert.IsTrue(board.TryMove(target, false));
            int[] beforeFailedUndo = board.CopyCells();
            Assert.IsFalse(board.TryUndo());
            CollectionAssert.AreEqual(beforeFailedUndo, board.CopyCells());
            Assert.AreEqual(3, board.UndoUsed);
            Assert.AreEqual(4, board.MoveCount);
            Assert.IsFalse(board.TryRefillUndo(0));
            Assert.IsTrue(board.TryRefillUndo(3));
            Assert.IsFalse(board.TryRefillUndo(3));
            Assert.IsTrue(board.TryUndo());
            Assert.AreEqual(4, board.UndoUsed);
            Assert.AreEqual(2, board.UndoRemaining);
        }

        [Test]
        public void InvalidMovesDoNotConsumeTurnsOrHistory()
        {
            var board = new PuzzleBoardState(3, 3, 17, 100);
            int diagonal = ((board.BlankCell / 3 + 1) % 3) * 3 + (board.BlankCell % 3 + 1) % 3;
            Assert.IsFalse(board.TryMove(-1, true));
            Assert.IsFalse(board.TryMove(9, true));
            Assert.IsFalse(board.TryMove(board.BlankCell, true));
            Assert.IsFalse(board.TryMove(diagonal, true));
            Assert.AreEqual(0, board.MoveCount);
            Assert.IsFalse(board.HasHistory);
        }

        [Test]
        public void OneShuffleMoveCanBeSolvedAndLocksFurtherMoves()
        {
            var board = new PuzzleBoardState(3, 3, 7, 1);
            Assert.IsTrue(board.TryMove(8, false));
            Assert.IsTrue(board.IsSolved);
            Assert.AreEqual(8, board.BlankCell);
            Assert.IsFalse(board.TryUndo());
            Assert.IsFalse(board.TryMove(7, true));
        }

        [TestCase(1200, 600, 3, 4)]
        [TestCase(600, 1200, 6, 8)]
        public void CroppedUVsAreSquareInSourcePixelsAndRunTopToBottom(int width, int height, int cols, int rows)
        {
            var texture = new Texture2D(width, height);
            StageData stage = ScriptableObject.CreateInstance<StageData>();
            try
            {
                var data = new SerializedObject(stage);
                data.FindProperty("illustration").objectReferenceValue = texture;
                data.FindProperty("columns").intValue = cols;
                data.FindProperty("rows").intValue = rows;
                data.ApplyModifiedPropertiesWithoutUndo();
                Rect first = stage.GetTileUV(0);
                Rect last = stage.GetTileUV(cols * rows - 1);
                Rect crop = stage.GetImageUV();
                Assert.That(first.width * width, Is.EqualTo(first.height * height).Within(0.001f));
                Assert.That(first.yMax, Is.EqualTo(crop.yMax).Within(0.00001f));
                Assert.That(last.yMin, Is.EqualTo(crop.yMin).Within(0.00001f));
                Assert.That(last.xMax, Is.EqualTo(crop.xMax).Within(0.00001f));
                Assert.That(crop.xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(crop.yMin, Is.GreaterThanOrEqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
