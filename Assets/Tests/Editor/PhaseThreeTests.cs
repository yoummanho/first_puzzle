using System.Collections;
using System.Reflection;
using FirstPuzzle;
using FirstPuzzle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FirstPuzzleTests
{
    public class PhaseThreeTests
    {
        private const string Prefix = "PhaseThreeTests.Backup.";
        private static readonly string[] PrefKeys =
        {
            "PREF_BGM", "PREF_SFX", "PREF_MULTIPUSH", "PREF_SLIDE_ANIM", "PREF_ORIENTATION"
        };

        [SetUp]
        public void BackupPreferences()
        {
            foreach (string key in PrefKeys)
            {
                SessionState.SetBool(Prefix + key + ".exists", PlayerPrefs.HasKey(key));
                SessionState.SetInt(Prefix + key, PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void RestorePreferences()
        {
            foreach (string key in PrefKeys)
            {
                if (SessionState.GetBool(Prefix + key + ".exists", false))
                    PlayerPrefs.SetInt(key, SessionState.GetInt(Prefix + key, 0));
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void OrientationPreferenceDefaultsToPortraitAndPersists()
        {
            var settings = new SettingsManager();
            Assert.AreEqual(PlayOrientation.Portrait, settings.Orientation);
            Assert.IsFalse(settings.IsLandscape);

            settings.SetOrientation(PlayOrientation.Landscape);
            Assert.AreEqual(PlayOrientation.Landscape, settings.Orientation);
            Assert.IsTrue(settings.IsLandscape);

            var restored = new SettingsManager();
            Assert.AreEqual(PlayOrientation.Landscape, restored.Orientation);
            restored.ToggleOrientation();
            Assert.AreEqual(PlayOrientation.Portrait, new SettingsManager().Orientation);
        }

        [Test]
        public void StageProgressionUnlocksFirstOfEachDifficultyAndNextAfterClear()
        {
            StageData s1 = MakeTempStage("test-stage-a", 3, 4);
            StageData s2 = MakeTempStage("test-stage-b", 3, 4);
            StageData s3 = MakeTempStage("test-stage-c", 3, 5);
            PlayerPrefs.DeleteKey("PUZZLE_CLEAR_test-stage-a");
            PlayerPrefs.DeleteKey("PUZZLE_CLEAR_test-stage-b");
            PlayerPrefs.DeleteKey("PUZZLE_CLEAR_test-stage-c");
            try
            {
                var list = new[] { s1, s2, s3 };
                Assert.IsTrue(PuzzleProgress.IsUnlocked(list, 0), "First stage must be unlocked.");
                Assert.IsFalse(PuzzleProgress.IsUnlocked(list, 1), "Second stage of same grid starts locked.");
                Assert.IsTrue(PuzzleProgress.IsUnlocked(list, 2), "First stage of a new difficulty grid starts unlocked.");

                PuzzleProgress.RecordClear(s1, 18, 25.5f);
                Assert.IsTrue(PuzzleProgress.IsCleared(s1));
                Assert.AreEqual(18, PuzzleProgress.GetBestMoves(s1));
                Assert.That(PuzzleProgress.GetBestTime(s1), Is.EqualTo(25.5f).Within(0.01f));
                Assert.IsTrue(PuzzleProgress.IsUnlocked(list, 1), "Clearing stage 1 unlocks stage 2.");
            }
            finally
            {
                PlayerPrefs.DeleteKey("PUZZLE_CLEAR_test-stage-a");
                PlayerPrefs.DeleteKey("PUZZLE_BEST_test-stage-a");
                PlayerPrefs.DeleteKey("PUZZLE_BEST_TIME_test-stage-a");
                Object.DestroyImmediate(s1);
                Object.DestroyImmediate(s2);
                Object.DestroyImmediate(s3);
            }
        }

        [UnityTest]
        public IEnumerator LobbyGalleryViewerOrientationAndSafeAreaWorkAcrossAspectRatios()
        {
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
            SessionState.SetString(Prefix + "scene", SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene(PuzzleGameSceneBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return null;
            yield return VerifyPhaseThreeFlow();
        }

        private static IEnumerator VerifyPhaseThreeFlow()
        {
            var game = Object.FindFirstObjectByType<PuzzleGame>();
            Assert.IsNotNull(game);
            Assert.IsNotNull(game.Lobby);
            Assert.IsNotNull(game.ResponsiveLayout);

            string firstId = game.stages[0].StageId;
            string secondId = game.stages[1].StageId;
            bool hadFirst = PlayerPrefs.HasKey("PUZZLE_CLEAR_" + firstId);
            int prevFirst = PlayerPrefs.GetInt("PUZZLE_CLEAR_" + firstId, 0);
            bool hadSecond = PlayerPrefs.HasKey("PUZZLE_CLEAR_" + secondId);
            int prevSecond = PlayerPrefs.GetInt("PUZZLE_CLEAR_" + secondId, 0);

            try
            {
                PlayerPrefs.DeleteKey("PUZZLE_CLEAR_" + firstId);
                PlayerPrefs.DeleteKey("PUZZLE_CLEAR_" + secondId);

                // 1. Verify initial Title Screen -> Lobby -> In-Game flow, Content hierarchy cleanup, and Inspector AudioClips
                var hudContent = game.pauseButton.transform.parent;
                Assert.IsNull(hudContent.Find("Lobby"), "In-game HUD must not have a top Lobby button.");
                Assert.IsNull(hudContent.Find("Undo"), "Undo must not remain under PortraitFrame/Content.");
                Assert.IsNull(hudContent.Find("Hold to preview"), "Hold to preview must not remain under PortraitFrame/Content.");
                Assert.IsNull(hudContent.Find("Numbers: ON"), "Numbers: ON must not remain under PortraitFrame/Content.");
                Assert.IsNull(hudContent.Find("Restart"), "Duplicate Restart must not remain under PortraitFrame/Content.");

                Assert.IsNotNull(game.Audio.TitleMusicClip, "titleMusicClip must be assigned in Inspector.");
                Assert.IsNotNull(game.Audio.LobbyMusicClip, "lobbyMusicClip must be assigned in Inspector.");
                Assert.IsNotNull(game.Audio.InGameMusicClip, "musicClip (In-Game BGM) must be assigned in Inspector.");
                Assert.IsNotNull(game.Audio.GameStartClip, "gameStartClip must be assigned in Inspector.");
                Assert.IsNotNull(game.Audio.MoveClip, "moveClip must be assigned in Inspector.");
                Assert.IsNotNull(game.Audio.TileCorrectClip, "tileCorrectClip must be assigned in Inspector.");
                Assert.IsNotNull(game.Audio.ClearClip, "clearClip must be assigned in Inspector.");

                Assert.IsTrue(game.Lobby.IsTitleOpen, "Game must start on the Title screen.");
                Assert.IsFalse(hudContent.gameObject.activeSelf, "In-game HUD/board Content must be hidden while Title screen is open.");
                Assert.IsFalse(game.curtain.activeSelf, "Paused curtain must be hidden while Title screen is open.");
                var titleCardRect = game.Lobby.transform.Find("TitlePanel/TitleCard") as RectTransform;
                Assert.IsNotNull(titleCardRect);
                Assert.AreEqual(Vector2.zero, titleCardRect.anchorMin, "TitleCard must be full-screen.");
                Assert.AreEqual(Vector2.one, titleCardRect.anchorMax, "TitleCard must be full-screen.");
                Assert.AreEqual(MusicContext.Title, game.Audio.ActiveMusicContext, "Title BGM must play on Title screen.");
                AudioClip titleClip = game.Audio.CurrentMusicClip;
                Assert.IsNotNull(titleClip);

                game.EnterLobbyFromTitle();
                Assert.IsFalse(game.Lobby.IsTitleOpen);
                Assert.IsTrue(game.Lobby.IsLobbyOpen, "Entering from Title must open the Stage Lobby.");
                Assert.IsFalse(hudContent.gameObject.activeSelf, "In-game HUD/board Content must be hidden while Stage Lobby is open.");
                Assert.IsFalse(game.curtain.activeSelf, "PAUSED screen (curtain) must NOT be visible behind Stage Lobby.");
                Assert.AreEqual(MusicContext.Lobby, game.Audio.ActiveMusicContext, "Lobby BGM must play in Lobby.");
                AudioClip lobbyClip = game.Audio.CurrentMusicClip;
                Assert.IsNotNull(lobbyClip);
                Assert.AreNotSame(titleClip, lobbyClip, "Title BGM and Lobby BGM must be distinct.");
                Assert.IsTrue(game.board.IsPaused);
                game.ShowPreview(true);
                Assert.IsFalse(game.preview.gameObject.activeSelf, "Preview must remain blocked while Lobby is open.");

                Assert.AreEqual(0, game.Lobby.LobbyPage);
                game.Lobby.PrevLobbyPage();
                Assert.AreEqual(0, game.Lobby.LobbyPage, "Lobby Prev must not wrap from first page to last page.");

                game.Lobby.SelectFilter("3x3");
                Assert.AreEqual("3x3", game.Lobby.ActiveFilter);
                game.Lobby.NextLobbyPage();
                Assert.AreEqual(0, game.Lobby.LobbyPage, "Single-page filter must not advance page.");

                game.Lobby.SelectFilter("3x4");
                Assert.AreEqual("3x4", game.Lobby.ActiveFilter);
                Assert.Greater(game.Lobby.FilteredStageCount, 0);
                game.Lobby.SelectFilter("ALL");
                for (int p = 0; p < 10; p++) game.Lobby.NextLobbyPage();
                Assert.AreEqual(3, game.Lobby.LobbyPage, "Lobby Next must clamp at the last page (page index 3 for 24 stages) without wrapping.");
                for (int p = 0; p < 10; p++) game.Lobby.PrevLobbyPage();
                Assert.AreEqual(0, game.Lobby.LobbyPage);

                // Stage 0 is first 3x3 (unlocked), Stage 1 is second 3x3 so Stage 1 is locked when Stage 0 is uncleared!
                var card1Lock = game.Lobby.transform.Find("LobbyPanel/StageCards/StageCard_01/ThumbFrame/LockOverlay");
                var card2Lock = game.Lobby.transform.Find("LobbyPanel/StageCards/StageCard_02/ThumbFrame/LockOverlay");
                var card2Thumb = game.Lobby.transform.Find("LobbyPanel/StageCards/StageCard_02/ThumbFrame/Thumbnail").GetComponent<RawImage>();
                Assert.IsNotNull(card1Lock);
                Assert.IsNotNull(card2Lock);
                Assert.IsFalse(card1Lock.gameObject.activeSelf, "Unlocked stage card must hide LockOverlay.");
                Assert.IsTrue(card2Lock.gameObject.activeSelf, "Locked stage card must show LockOverlay with padlock icon.");
                Assert.IsNull(card2Thumb.texture, "Locked stage card must not expose original illustration texture.");

                Assert.IsFalse(game.Lobby.TrySelectStageFromLobby(1), "Locked stage must not load.");
                Assert.IsTrue(game.Lobby.IsLobbyOpen);

                // 2. Gallery lock & full original illustration viewer after clear
                game.OpenGallery();
                Assert.IsTrue(game.Lobby.IsGalleryOpen);
                Assert.IsFalse(game.Lobby.TryOpenOriginalViewer(0), "Uncleared stage must not open full viewer.");

                PuzzleProgress.RecordClear(game.stages[0], 14, 19.2f);
                game.Lobby.RefreshGallery();
                Assert.IsTrue(game.Lobby.TryOpenOriginalViewer(0), "Cleared stage must open full original viewer.");
                Assert.IsTrue(game.Lobby.IsViewerOpen);
                Assert.AreSame(game.stages[0].Illustration, game.Lobby.ViewerImage.texture);
                Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), game.Lobby.ViewerImage.uvRect, "Gallery viewer must display uncropped full original texture.");
                game.Lobby.CloseViewer();
                Assert.IsFalse(game.Lobby.IsViewerOpen);

                // Select unlocked stage from lobby -> plays Game Start SFX and switches to In-Game loop BGM
                int startsBefore = game.Audio.GameStartPlayCount;
                game.OpenLobby();
                Assert.IsFalse(game.curtain.activeSelf, "PAUSED curtain must remain hidden in Lobby.");
                Assert.IsTrue(game.Lobby.TrySelectStageFromLobby(0));
                Assert.IsFalse(game.Lobby.IsLobbyOpen);
                Assert.IsTrue(hudContent.gameObject.activeSelf, "In-game HUD/board Content must be visible when entering In-Game.");
                Assert.IsFalse(game.board.IsPaused);
                Assert.AreEqual(startsBefore + 1, game.Audio.GameStartPlayCount, "Entering In-Game must trigger Game Start SFX.");
                Assert.AreEqual(MusicContext.InGame, game.Audio.ActiveMusicContext, "In-Game loop BGM must play when stage starts.");
                Assert.AreNotSame(lobbyClip, game.Audio.CurrentMusicClip, "In-Game BGM must be distinct from Lobby BGM.");

                // Verify crisp tile-correct SFX when a tile moves into its exact home cell
                var oneMoveStage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Resource/PuzzlePhaseOne/test-one-move.asset");
                Assert.IsNotNull(oneMoveStage);
                int correctBefore = game.Audio.TileCorrectPlayCount;
                Assert.IsTrue(game.board.LoadStage(oneMoveStage));
                var oneMoveModel = (PuzzleBoardState)typeof(BoardManager)
                    .GetField("state", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(game.board);
                Assert.IsTrue(game.board.TryMoveTile(oneMoveModel.GetTileAt(8)));
                Assert.Greater(game.Audio.TileCorrectPlayCount, correctBefore, "Placing a tile into its home position must play the crisp tile-correct SFX.");

                // Reload stage 0 for mid-game orientation test
                game.Load(0);
                game.board.SetSlideAnimation(false);
                var model = (PuzzleBoardState)typeof(BoardManager)
                    .GetField("state", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(game.board);
                int adj = model.BlankCell % model.Columns == 0 ? model.BlankCell + 1 : model.BlankCell - 1;
                int tileId = model.GetTileAt(adj);
                Assert.IsTrue(game.board.TryMoveTile(tileId));
                yield return null;
                int[] cellsBeforeRotate = model.CopyCells();
                int movesBeforeRotate = game.board.MoveCount;
                int undoBeforeRotate = game.board.UndoRemaining;
                float elapsedBeforeRotate = game.Elapsed;

                game.ToggleOrientation();
                Assert.AreEqual(PlayOrientation.Landscape, game.Settings.Orientation);
                Assert.IsTrue(game.board.IsPaused, "Mid-game orientation change must pause the board.");
                Assert.IsTrue(game.curtain.activeSelf, "Blind mask must cover the board during orientation change.");
                CollectionAssert.AreEqual(cellsBeforeRotate, model.CopyCells(), "Board arrangement must be preserved.");
                Assert.AreEqual(movesBeforeRotate, game.board.MoveCount, "Move count must be preserved.");
                Assert.AreEqual(undoBeforeRotate, game.board.UndoRemaining, "Undo remaining must be preserved.");
                Assert.That(game.Elapsed, Is.EqualTo(elapsedBeforeRotate).Within(0.02f), "Timer must be preserved.");

                // 4. Verify Safe Area and aspect ratios (16:9, 19.5:9, 20:9 phones and 4:3 tablet in Portrait & Landscape)
                // plus 3x3~6x8 boards under both orientations
                Vector2[] testResolutions =
                {
                    new Vector2(1080, 1920), // 16:9 Portrait
                    new Vector2(1080, 2340), // 19.5:9 Portrait
                    new Vector2(1080, 2400), // 20:9 Portrait
                    new Vector2(1536, 2048), // 4:3 Tablet Portrait
                    new Vector2(1920, 1080), // 16:9 Landscape
                    new Vector2(2340, 1080), // 19.5:9 Landscape
                    new Vector2(2400, 1080), // 20:9 Landscape
                    new Vector2(2048, 1536)  // 4:3 Tablet Landscape
                };

                string[] testStagePaths =
                {
                    "Assets/Resource/PuzzlePhaseOne/test-3x3.asset",
                    "Assets/Resource/PuzzlePhaseOne/test-3x4.asset",
                    "Assets/Resource/PuzzlePhaseOne/test-4x4.asset",
                    "Assets/Resource/PuzzlePhaseOne/test-4x5.asset",
                    "Assets/Resource/PuzzlePhaseOne/test-5x6.asset",
                    "Assets/Resource/PuzzlePhaseOne/test-6x8.asset"
                };

                try
                {
                    foreach (Vector2 res in testResolutions)
                    {
                        bool isLandscapeRes = res.x > res.y;
                        game.Settings.SetOrientation(isLandscapeRes ? PlayOrientation.Landscape : PlayOrientation.Portrait);
                        // Simulate notch + home indicator safe area inset
                        game.ResponsiveLayout.SetSimulatedSafeArea(
                            isLandscapeRes
                                ? new Rect(0.04f, 0.05f, 0.92f, 0.90f)
                                : new Rect(0.00f, 0.03f, 1.00f, 0.93f));

                        foreach (PuzzleTile tile in game.board.GetComponentsInChildren<PuzzleTile>())
                        {
                            Assert.That(tile.Rect.rect.width, Is.EqualTo(tile.Rect.rect.height).Within(0.01f),
                                $"Tiles must remain square at {res} ({game.Settings.Orientation}).");
                            Assert.Greater(tile.Rect.rect.width, 10f);
                        }
                    }

                    foreach (PlayOrientation orientation in new[] { PlayOrientation.Portrait, PlayOrientation.Landscape })
                    {
                        game.Settings.SetOrientation(orientation);
                        foreach (string stagePath in testStagePaths)
                        {
                            var testStage = AssetDatabase.LoadAssetAtPath<StageData>(stagePath);
                            Assert.IsNotNull(testStage, $"Missing test stage at {stagePath}");
                            Assert.IsTrue(game.board.LoadStage(testStage));
                            foreach (PuzzleTile tile in game.board.GetComponentsInChildren<PuzzleTile>())
                            {
                                Assert.That(tile.Rect.rect.width, Is.EqualTo(tile.Rect.rect.height).Within(0.01f),
                                    $"Tiles must remain square for {testStage.Columns}x{testStage.Rows} in {orientation}.");
                                Assert.Greater(tile.Rect.rect.width, 8f);
                            }
                        }
                    }
                }
                finally
                {
                    game.ResponsiveLayout.SetSimulatedSafeArea(new Rect(0, 0, 1, 1));
                    game.Settings.SetOrientation(PlayOrientation.Portrait);
                }
            }
            finally
            {
                if (hadFirst) PlayerPrefs.SetInt("PUZZLE_CLEAR_" + firstId, prevFirst);
                else PlayerPrefs.DeleteKey("PUZZLE_CLEAR_" + firstId);
                if (hadSecond) PlayerPrefs.SetInt("PUZZLE_CLEAR_" + secondId, prevSecond);
                else PlayerPrefs.DeleteKey("PUZZLE_CLEAR_" + secondId);
                PlayerPrefs.Save();
            }
        }

        private static StageData MakeTempStage(string id, int cols, int rows)
        {
            var stage = ScriptableObject.CreateInstance<StageData>();
            var so = new SerializedObject(stage);
            so.FindProperty("stageId").stringValue = id;
            so.FindProperty("columns").intValue = cols;
            so.FindProperty("rows").intValue = rows;
            so.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            string scene = SessionState.GetString(Prefix + "scene", "");
            SessionState.EraseString(Prefix + "scene");
            if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene);
        }
    }
}
