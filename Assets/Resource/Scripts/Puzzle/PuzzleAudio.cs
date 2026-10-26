using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Separate music/effects channels; optional authored clips replace the quiet synth defaults.</summary>
    public sealed class PuzzleAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip musicClip, moveClip, clearClip;
        private AudioSource music, effects;
        private AudioClip generatedMusic, generatedMove, generatedClear;
        private SettingsManager settings;
        private BoardManager board;
        private int previousMoves, previousUndo;

        public void Initialize(SettingsManager preferences, BoardManager puzzle)
        {
            settings = preferences;
            board = puzzle;
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = effects.playOnAwake = false;
            music.loop = true;
            music.volume = .12f;
            effects.volume = .2f;
            if (musicClip == null) musicClip = generatedMusic = Tone("Puzzle ambience", new[] { 261.63f, 329.63f, 392f, 329.63f }, 1.5f);
            if (moveClip == null) moveClip = generatedMove = Tone("Tile move", new[] { 660f }, .06f);
            if (clearClip == null) clearClip = generatedClear = Tone("Puzzle clear", new[] { 523.25f, 659.25f, 783.99f }, .16f);
            music.clip = musicClip;
            settings.Changed += Apply;
            board.StateChanged += OnBoardChanged;
            board.PuzzleSolved += OnSolved;
            Apply();
            music.Play();
        }

        private void Apply()
        {
            music.mute = !settings.Bgm;
            effects.mute = !settings.Sfx;
        }

        private void OnBoardChanged()
        {
            if (board.MoveCount > previousMoves || board.UndoUsed > previousUndo)
                effects.PlayOneShot(moveClip);
            previousMoves = board.MoveCount;
            previousUndo = board.UndoUsed;
        }

        private void OnSolved() => effects.PlayOneShot(clearClip);

        private static AudioClip Tone(string name, float[] notes, float secondsPerNote)
        {
            const int rate = 22050;
            int samplesPerNote = Mathf.RoundToInt(rate * secondsPerNote);
            var samples = new float[samplesPerNote * notes.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (i % samplesPerNote) / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * t / secondsPerNote);
                samples[i] = Mathf.Sin(2 * Mathf.PI * notes[i / samplesPerNote] * t) * envelope * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (settings != null) settings.Changed -= Apply;
            if (board != null)
            {
                board.StateChanged -= OnBoardChanged;
                board.PuzzleSolved -= OnSolved;
            }
            if (generatedMusic != null) Destroy(generatedMusic);
            if (generatedMove != null) Destroy(generatedMove);
            if (generatedClear != null) Destroy(generatedClear);
        }
    }
}
