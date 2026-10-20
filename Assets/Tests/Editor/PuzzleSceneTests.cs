using System.Collections;
using System.Reflection;
using FirstPuzzle;
using FirstPuzzle.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FirstPuzzleTests
{
    public class PuzzleSceneTests
    {
        private const string RestoreKey = "FirstPuzzle.Tests.OriginalScene";

        [UnityTest]
        public IEnumerator ConnectedSceneHandlesMotionUndoRewardsAndCompletion()
        {
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty, "Save the current scene before running integration tests.");
            SessionState.SetString(RestoreKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene(PhaseOneSceneBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return null;

            BoardManager board = Object.FindFirstObjectByType<BoardManager>();
            Assert.IsNotNull(board);
            Assert.IsTrue(board.IsReady);
            Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<UnityEngine.UI.GraphicRaycaster>());
            Object.FindFirstObjectByType<PhaseOneDemo>().enabled = false; // Use explicit mock ad callbacks below.
            Assert.AreEqual(3, board.UndoRemaining);
            Assert.AreEqual(0, board.UndoUsed);

            foreach (PuzzleTile tile in board.GetComponentsInChildren<PuzzleTile>())
                Assert.That(tile.Rect.rect.width, Is.EqualTo(tile.Rect.rect.height).Within(0.001f));

            PuzzleBoardState model = Model(board);
            int adjacent = model.BlankCell % model.Columns == 0 ? model.BlankCell + 1 : model.BlankCell - 1;
            int tileId = model.GetTileAt(adjacent);
            int[] original = model.CopyCells();
            PuzzleTile clickedTile = System.Array.Find(board.GetComponentsInChildren<PuzzleTile>(), tile => tile.TileId == tileId);
            // Verify the actual EventSystem pointer route, not just a direct model call.
            ExecuteEvents.Execute(clickedTile.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(1, board.MoveCount);
            Assert.IsTrue(board.IsBusy);
            Assert.IsFalse(board.TryMoveTile(tileId));
            Assert.IsFalse(board.TryUndo());
            board.SetPaused(true);
            Vector2 pausedPosition = clickedTile.Rect.anchoredPosition;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual(pausedPosition, clickedTile.Rect.anchoredPosition);
            Assert.IsTrue(board.IsBusy);
            board.SetSlideAnimation(false);
            Assert.IsFalse(clickedTile.IsMoving);
            Assert.AreNotEqual(pausedPosition, clickedTile.Rect.anchoredPosition);
            Assert.IsTrue(board.IsBusy, "Turning animation off must not bypass pause.");
            board.SetPaused(false);
            yield return WaitForBoard(board);
            Assert.IsTrue(board.TryUndo());
            yield return WaitForBoard(board);
            CollectionAssert.AreEqual(original, model.CopyCells());
            Assert.AreEqual(1, board.MoveCount);

            board.SetSlideAnimation(false);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(board.TryMoveTile(tileId));
                yield return WaitForBoard(board);
                Assert.IsTrue(board.TryUndo());
                yield return WaitForBoard(board);
            }
            Assert.AreEqual(0, board.UndoRemaining);
            Assert.AreEqual(3, board.UndoUsed);
            Assert.AreEqual(3, board.MoveCount);
            Assert.IsFalse(board.TryUndo());
            int ticket = 0;
            board.UndoRewardRequested += value => ticket = value;
            Assert.IsTrue(board.RequestUndoReward());
            Assert.IsFalse(board.RequestUndoReward());
            Assert.IsFalse(board.TryMoveTile(tileId));
            Assert.IsFalse(board.CompleteUndoReward(ticket, false));
            Assert.AreEqual(0, board.UndoRemaining);
            Assert.IsTrue(board.RequestUndoReward());
            Assert.IsTrue(board.CompleteUndoReward(ticket, true));
            Assert.IsFalse(board.CompleteUndoReward(ticket, true));
            Assert.AreEqual(3, board.UndoRemaining);
            Assert.AreEqual(3, board.UndoUsed);

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(board.TryMoveTile(tileId));
                yield return WaitForBoard(board);
                Assert.IsTrue(board.TryUndo());
                yield return WaitForBoard(board);
            }
            Assert.IsTrue(board.RequestUndoReward());
            int staleTicket = ticket;
            board.Restart();
            Assert.IsFalse(board.CompleteUndoReward(staleTicket, true));
            Assert.AreEqual(3, board.UndoRemaining);
            Assert.AreEqual(0, board.UndoUsed);
            Assert.AreEqual(0, board.MoveCount);
            yield return null; // Retire the previous board's Destroy calls.

            StageData largeStage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Resource/PuzzlePhaseOne/test-6x8.asset");
            Assert.IsTrue(board.LoadStage(largeStage));
            yield return null;
            Assert.AreEqual(48, board.GetComponentsInChildren<PuzzleTile>().Length);
            foreach (PuzzleTile tile in board.GetComponentsInChildren<PuzzleTile>())
                Assert.That(tile.Rect.rect.width, Is.EqualTo(tile.Rect.rect.height).Within(0.001f));

            StageData finishStage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Resource/PuzzlePhaseOne/test-one-move.asset");
            Assert.IsTrue(board.LoadStage(finishStage));
            yield return null;
            int solvedEvents = 0;
            int presentedEvents = 0;
            board.PuzzleSolved += () => solvedEvents++;
            board.ClearPresentationFinished += () => presentedEvents++;
            board.SetSlideAnimation(true);
            Assert.IsTrue(board.TryMoveTile(Model(board).GetTileAt(8)));
            Assert.AreEqual(1, solvedEvents);
            Assert.AreEqual(0, presentedEvents);
            yield return WaitForBoard(board);
            Assert.AreEqual(1, presentedEvents);
            Assert.IsTrue(board.IsSolved);
            Assert.IsFalse(board.TryUndo());
            PuzzleTile[] finishedTiles = board.GetComponentsInChildren<PuzzleTile>();
            foreach (PuzzleTile tile in finishedTiles)
            {
                Assert.That(tile.GetComponent<UnityEngine.UI.RawImage>().color.a, Is.EqualTo(1f));
                Assert.IsFalse(tile.GetComponentInChildren<TMP_Text>(true).gameObject.activeSelf);
            }
            float gaplessPitch = Vector2.Distance(finishedTiles[0].Rect.anchoredPosition, finishedTiles[1].Rect.anchoredPosition);
            Assert.That(finishedTiles[0].Rect.rect.width, Is.EqualTo(gaplessPitch).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator RestartFromClearNotificationDoesNotFinishTheNewBoard()
        {
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
            SessionState.SetString(RestoreKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene(PhaseOneSceneBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return null;
            BoardManager board = Object.FindFirstObjectByType<BoardManager>();
            Object.FindFirstObjectByType<PhaseOneDemo>().enabled = false;
            StageData finishStage = AssetDatabase.LoadAssetAtPath<StageData>(
                "Assets/Resource/PuzzlePhaseOne/test-one-move.asset");
            Assert.IsTrue(board.LoadStage(finishStage));
            yield return null;
            board.SetSlideAnimation(false);
            bool solved = false;
            bool restarted = false;
            int presentations = 0;
            board.PuzzleSolved += () => solved = true;
            board.ClearPresentationFinished += () => presentations++;
            board.StateChanged += () =>
            {
                if (!solved || restarted) return;
                restarted = true;
                board.Restart();
            };
            Assert.IsTrue(board.TryMoveTile(Model(board).GetTileAt(8)));
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsTrue(restarted);
            Assert.IsFalse(board.IsSolved);
            Assert.IsFalse(board.IsBusy);
            Assert.AreEqual(0, board.MoveCount);
            Assert.AreEqual(0, presentations);
            foreach (PuzzleTile tile in board.GetComponentsInChildren<PuzzleTile>())
            {
                if (tile.TileId == Model(board).BlankTile)
                    Assert.AreEqual(0f, tile.GetComponent<UnityEngine.UI.RawImage>().color.a);
            }
        }

        private static PuzzleBoardState Model(BoardManager board) =>
            (PuzzleBoardState)typeof(BoardManager).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(board);

        private static IEnumerator WaitForBoard(BoardManager board)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (board.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(board.IsBusy, "Board animation did not complete.");
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            string original = SessionState.GetString(RestoreKey, "");
            SessionState.EraseString(RestoreKey);
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        }
    }
}
