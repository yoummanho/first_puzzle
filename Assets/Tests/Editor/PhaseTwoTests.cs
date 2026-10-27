using System.Collections;
using FirstPuzzle;
using FirstPuzzle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FirstPuzzleTests
{
    public class PhaseTwoTests
    {
        private static readonly string[] Keys = { "PREF_BGM", "PREF_SFX", "PREF_MULTIPUSH", "PREF_SLIDE_ANIM" };
        private const string Prefix = "PhaseTwoTests.Backup.";

        [SetUp]
        public void BackupPreferences()
        {
            foreach (string key in Keys)
            {
                SessionState.SetBool(Prefix + key + ".exists", PlayerPrefs.HasKey(key));
                SessionState.SetInt(Prefix + key, PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void RestorePreferences()
        {
            foreach (string key in Keys)
            {
                if (SessionState.GetBool(Prefix + key + ".exists", false))
                    PlayerPrefs.SetInt(key, SessionState.GetInt(Prefix + key, 0));
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void FourSettingsDefaultOnPersistAndNotify()
        {
            var settings = new SettingsManager();
            Assert.IsTrue(settings.Bgm && settings.Sfx && settings.MultiPush && settings.SlideAnimation);
            int notifications = 0;
            settings.Changed += () => notifications++;
            settings.SetBgm(false);
            settings.SetSfx(false);
            settings.SetMultiPush(false);
            settings.SetSlideAnimation(false);
            var restored = new SettingsManager();
            Assert.IsFalse(restored.Bgm || restored.Sfx || restored.MultiPush || restored.SlideAnimation);
            Assert.AreEqual(4, notifications);
        }

        [UnityTest]
        public IEnumerator PlayableSceneSettingsPausePreviewAndInterruptedCountdown()
        {
            Assert.IsFalse(SceneManager.GetActiveScene().isDirty);
            SessionState.SetString(Prefix + "scene", SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene(PuzzleGameSceneBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<PuzzleGame>();
            Assert.IsNotNull(game);
            Assert.IsTrue(game.board.IsReady);
            var frame = game.board.transform.parent.Find("ImageFrame") as RectTransform;
            Assert.IsNotNull(frame);
            Assert.IsNotNull(frame.GetComponent<UnityEngine.UI.Image>().sprite);
            Assert.IsFalse(frame.GetComponent<UnityEngine.UI.Image>().raycastTarget);
            Assert.AreEqual(24, game.stages.Length, "Duplicate stages must be removed, leaving 24 unique stages.");
            Assert.AreEqual(new Vector2Int(3, 3), new Vector2Int(game.stages[0].Columns, game.stages[0].Rows), "Stages must start at 3x3 difficulty.");
            var stageIds = new System.Collections.Generic.HashSet<string>();
            var illustrations = new System.Collections.Generic.HashSet<Texture2D>();
            int prevTileCount = 0;
            for (int stageIndex = 0; stageIndex < game.stages.Length; stageIndex++)
            {
                Assert.IsTrue(stageIds.Add(game.stages[stageIndex].StageId), "Stage IDs must remain unique.");
                Assert.IsTrue(illustrations.Add(game.stages[stageIndex].Illustration), "Stage illustrations must not overlap.");
                int tileCount = game.stages[stageIndex].Columns * game.stages[stageIndex].Rows;
                Assert.GreaterOrEqual(tileCount, prevTileCount, "Stages must be ordered by non-decreasing tile count.");
                prevTileCount = tileCount;
                Assert.IsTrue(game.stages[stageIndex].Validate(out string error), error);
                Assert.AreEqual(PuzzleImageLayout.ForTexture(game.stages[stageIndex].Illustration),
                    new Vector2Int(game.stages[stageIndex].Columns, game.stages[stageIndex].Rows));
                game.Load(stageIndex);
                yield return null;
                Assert.AreSame(game.stages[stageIndex], game.board.Stage);
                Assert.AreSame(game.stages[stageIndex].Illustration, game.preview.texture);
                var area = (RectTransform)game.board.transform;
                float pitch = Mathf.Min(area.rect.width / game.board.Stage.Columns, area.rect.height / game.board.Stage.Rows);
                Assert.That(frame.rect.width, Is.EqualTo(pitch * game.board.Stage.Columns + 30f).Within(.1f));
                Assert.That(frame.rect.height, Is.EqualTo(pitch * game.board.Stage.Rows + 30f).Within(.1f));
            }
            game.Load(0);
            // Switching window focus in Editor must NOT auto-pause before pressing Pause button
            game.SendMessage("OnApplicationFocus", false);
            Assert.IsFalse(game.board.IsPaused, "Losing window focus before pressing Pause must not pause the game.");
            Assert.IsNotNull(game.pauseButton.transform.Find("HamburgerIcon"), "Pause button must have the 3-horizontal-bar hamburger icon.");
            game.ShowPreview(true);
            Assert.IsTrue(game.preview.gameObject.activeSelf);
            game.Pause();
            float elapsed = game.Elapsed;
            Assert.IsTrue(game.curtain.activeSelf);
            Assert.AreEqual("X", game.resumeButton.GetComponentInChildren<TMPro.TMP_Text>(true).text, "Continue button must be styled as X.");
            Assert.AreEqual(PauseTab.Game, game.ActivePauseTab);
            Assert.IsNotNull(game.pauseControls.transform.Find("GameTab/Restart puzzle"));
            Assert.IsNotNull(game.pauseControls.transform.Find("GameTab/Undo"));
            Assert.IsNotNull(game.pauseControls.transform.Find("GameTab/Hold to preview"));
            game.ShowPreview(true);
            Assert.IsFalse(game.preview.gameObject.activeSelf);
            Assert.IsFalse(game.board.CanInteract);
            game.SelectPauseTab(PauseTab.System);
            Assert.AreEqual(PauseTab.System, game.ActivePauseTab);
            Assert.IsNull(game.pauseControls.transform.Find("SystemTab/Orientation"), "Orientation button must be removed from PAUSED System tab.");
            var musicButton = game.pauseControls.transform.Find("SystemTab/Music").GetComponent<UnityEngine.UI.Button>();
            musicButton.onClick.Invoke();
            Assert.IsFalse(game.Settings.Bgm);
            Assert.IsTrue(game.Settings.MultiPush, "Cloned button must not invoke the line-slide callback.");
            game.ToggleSfx();
            var sources = game.GetComponents<AudioSource>();
            Assert.AreEqual(2, sources.Length);
            Assert.IsTrue(sources[0].mute && sources[1].mute);
            game.Lines();
            game.Animation();
            Assert.IsFalse(game.board.MultiPush || game.board.SlideAnimation);
            game.Resume();
            Assert.IsTrue(game.IsCountingDown);
            game.ShowPreview(true);
            Assert.IsFalse(game.preview.gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(elapsed, game.Elapsed);
            game.SendMessage("OnApplicationPause", true);
            Assert.IsFalse(game.IsCountingDown);
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.IsTrue(game.board.IsPaused);
            Assert.AreEqual(elapsed, game.Elapsed);
            game.Resume();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (game.IsCountingDown && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.IsFalse(game.board.IsPaused);
            Assert.IsFalse(game.curtain.activeSelf);
            Assert.Greater(game.Elapsed, elapsed);
            game.Pause();
            game.Resume();
            game.Restart();
            Assert.IsFalse(game.IsCountingDown);
            Assert.IsFalse(game.board.IsPaused);
            Assert.AreEqual(0, game.board.MoveCount);
            Assert.IsFalse(game.board.MultiPush || game.board.SlideAnimation);
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
