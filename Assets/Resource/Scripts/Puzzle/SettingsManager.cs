using System;
using UnityEngine;

namespace FirstPuzzle
{
    public enum PlayOrientation
    {
        Portrait = 0,
        Landscape = 1
    }

    /// <summary>Persistent Phase 2/3 preferences. One instance is owned by each game session.</summary>
    public sealed class SettingsManager
    {
        public bool Bgm { get; private set; }
        public bool Sfx { get; private set; }
        public bool MultiPush { get; private set; }
        public bool SlideAnimation { get; private set; }
        public PlayOrientation Orientation { get; private set; }
        public bool IsLandscape => Orientation == PlayOrientation.Landscape;
        public event Action Changed;

        public SettingsManager() => Reload();

        public void Reload()
        {
            Bgm = PlayerPrefs.GetInt("PREF_BGM", 1) != 0;
            Sfx = PlayerPrefs.GetInt("PREF_SFX", 1) != 0;
            MultiPush = PlayerPrefs.GetInt("PREF_MULTIPUSH", 1) != 0;
            SlideAnimation = PlayerPrefs.GetInt("PREF_SLIDE_ANIM", 1) != 0;
            Orientation = PlayerPrefs.GetInt("PREF_ORIENTATION", 0) == (int)PlayOrientation.Landscape
                ? PlayOrientation.Landscape
                : PlayOrientation.Portrait;
            ApplyScreenOrientation();
            Changed?.Invoke();
        }

        public void SetBgm(bool value) { Bgm = value; Save(); }
        public void SetSfx(bool value) { Sfx = value; Save(); }
        public void SetMultiPush(bool value) { MultiPush = value; Save(); }
        public void SetSlideAnimation(bool value) { SlideAnimation = value; Save(); }
        public void SetOrientation(PlayOrientation value)
        {
            Orientation = value == PlayOrientation.Landscape ? PlayOrientation.Landscape : PlayOrientation.Portrait;
            ApplyScreenOrientation();
            Save();
        }

        public void ToggleOrientation() =>
            SetOrientation(IsLandscape ? PlayOrientation.Portrait : PlayOrientation.Landscape);

        private void ApplyScreenOrientation()
        {
            Screen.autorotateToPortrait = !IsLandscape;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = IsLandscape;
            Screen.autorotateToLandscapeRight = IsLandscape;
            Screen.orientation = IsLandscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
        }

        private void Save()
        {
            PlayerPrefs.SetInt("PREF_BGM", Bgm ? 1 : 0);
            PlayerPrefs.SetInt("PREF_SFX", Sfx ? 1 : 0);
            PlayerPrefs.SetInt("PREF_MULTIPUSH", MultiPush ? 1 : 0);
            PlayerPrefs.SetInt("PREF_SLIDE_ANIM", SlideAnimation ? 1 : 0);
            PlayerPrefs.SetInt("PREF_ORIENTATION", (int)Orientation);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
