using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    /// <summary>Safe-area and dedicated Portrait/Landscape layout manager for HUD, Board, Lobby and Gallery.</summary>
    public sealed class PuzzleResponsiveLayout : MonoBehaviour
    {
        private PuzzleGame game;
        private RectTransform safeAreaRect;
        private RectTransform portraitFrame;
        private RectTransform contentRect;
        private RectTransform boardFrame;
        private RectTransform instructionsRect;
        private RectTransform footerRect;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private PlayOrientation lastOrientation = (PlayOrientation)(-1);

        public RectTransform SafeAreaRect => safeAreaRect;
        public RectTransform PortraitFrame => portraitFrame;
        public RectTransform ContentRect => contentRect;
        public PlayOrientation ActiveOrientation => lastOrientation;

        public void Initialize(PuzzleGame owner)
        {
            game = owner;
            EnsureSafeAreaContainer();
            CachePlayHudNodes();
            Apply(true);
        }

        public void SetSimulatedSafeArea(Rect normalizedRect)
        {
            if (safeAreaRect == null) return;
            safeAreaRect.anchorMin = new Vector2(Mathf.Clamp01(normalizedRect.xMin), Mathf.Clamp01(normalizedRect.yMin));
            safeAreaRect.anchorMax = new Vector2(Mathf.Clamp01(normalizedRect.xMax), Mathf.Clamp01(normalizedRect.yMax));
            safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            game.board.SendMessage("RefreshLayout", SendMessageOptions.DontRequireReceiver);
        }

        private void EnsureSafeAreaContainer()
        {
            var existing = game.transform.Find("SafeArea") as RectTransform;
            if (existing == null)
            {
                existing = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
                existing.SetParent(game.transform, false);
                existing.anchorMin = Vector2.zero;
                existing.anchorMax = Vector2.one;
                existing.offsetMin = existing.offsetMax = Vector2.zero;
                existing.SetSiblingIndex(1); // Right above Background.
            }
            safeAreaRect = existing;

            portraitFrame = game.transform.Find("PortraitFrame") as RectTransform
                ?? safeAreaRect.Find("PortraitFrame") as RectTransform;

            if (portraitFrame != null && portraitFrame.parent != safeAreaRect)
                portraitFrame.SetParent(safeAreaRect, false);
        }

        private void CachePlayHudNodes()
        {
            Transform root = portraitFrame != null ? (Transform)portraitFrame : game.transform;
            contentRect = root.Find("Content") as RectTransform;
            if (contentRect == null) return;

            boardFrame = contentRect.Find("BoardFrame") as RectTransform;
            instructionsRect = contentRect.Find("Instructions") as RectTransform;
            footerRect = contentRect.Find("Footer") as RectTransform;

            var legacyLobby = contentRect.Find("Lobby");
            if (legacyLobby != null) legacyLobby.gameObject.SetActive(false);

            StyleHudLabel(game.heading, new Color(0.11f, 0.24f, 0.34f, 1f), true);
            StyleHudLabel(game.stats, new Color(0.14f, 0.28f, 0.38f, 0.96f), true);
            if (instructionsRect != null)
                StyleHudLabel(instructionsRect.GetComponent<TMP_Text>(), new Color(0.14f, 0.28f, 0.38f, 0.94f), false);
            if (footerRect != null)
                StyleHudLabel(footerRect.GetComponent<TMP_Text>(), new Color(0.12f, 0.24f, 0.32f, 0.92f), false);
        }

        private static void StyleHudLabel(TMP_Text label, Color textColor, bool bold)
        {
            if (label == null) return;
            label.color = textColor;
            if (bold) label.fontStyle |= FontStyles.Bold;
        }

        private void LateUpdate()
        {
            if (game == null || game.Settings == null) return;
            Apply(false);
        }

        public void Apply(bool force)
        {
            if (game == null || game.Settings == null) return;

            Vector2Int screenSize = new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
                safe = new Rect(0, 0, screenSize.x, screenSize.y);

            PlayOrientation orientation = game.Settings.Orientation;
            if (!force && screenSize == lastScreenSize && safe == lastSafeArea && orientation == lastOrientation)
                return;

            lastScreenSize = screenSize;
            lastSafeArea = safe;
            lastOrientation = orientation;

            ApplySafeArea(screenSize, safe);
            ApplyScalerAndFrame(orientation);
            ApplyHudLayout(orientation);
            PauseSettingsUI.ApplyLayout(game, orientation);
            if (game.Lobby != null) game.Lobby.ApplyLayout(orientation);
            game.GetComponent<ConceptTheme>()?.ApplyLayout();
            Canvas.ForceUpdateCanvases();
        }

        private void ApplySafeArea(Vector2Int screenSize, Rect safe)
        {
            if (safeAreaRect == null) return;
            Vector2 min = new Vector2(
                Mathf.Clamp01(safe.xMin / screenSize.x),
                Mathf.Clamp01(safe.yMin / screenSize.y));
            Vector2 max = new Vector2(
                Mathf.Clamp01(safe.xMax / screenSize.x),
                Mathf.Clamp01(safe.yMax / screenSize.y));
            if (max.x <= min.x) { min.x = 0f; max.x = 1f; }
            if (max.y <= min.y) { min.y = 0f; max.y = 1f; }
            safeAreaRect.anchorMin = min;
            safeAreaRect.anchorMax = max;
            safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
        }

        private void ApplyScalerAndFrame(PlayOrientation orientation)
        {
            var scaler = game.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = orientation == PlayOrientation.Landscape
                    ? new Vector2(1920f, 1080f)
                    : new Vector2(1080f, 1920f);
            }

            if (portraitFrame != null)
            {
                var fitter = portraitFrame.GetComponent<AspectRatioFitter>();
                if (fitter != null) fitter.enabled = false;
                portraitFrame.anchorMin = Vector2.zero;
                portraitFrame.anchorMax = Vector2.one;
                portraitFrame.offsetMin = portraitFrame.offsetMax = Vector2.zero;
            }
        }

        private void ApplyHudLayout(PlayOrientation orientation)
        {
            if (contentRect == null) return;

            if (orientation == PlayOrientation.Landscape)
            {
                contentRect.anchorMin = new Vector2(0.025f, 0.03f);
                contentRect.anchorMax = new Vector2(0.975f, 0.97f);
                contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

                // Left side: Puzzle BoardFrame
                SetAnchors(boardFrame, 0.00f, 0.02f, 0.62f, 0.98f);

                // Right side: Pause (Hamburger icon), Title, Stats, Instructions, Footer
                SetAnchors(game.pauseButton?.transform as RectTransform, 0.65f, 0.86f, 0.77f, 0.97f);
                SetAnchors(game.heading?.rectTransform, 0.65f, 0.66f, 1.00f, 0.82f);
                SetAnchors(game.stats?.rectTransform, 0.65f, 0.50f, 1.00f, 0.62f);
                SetAnchors(instructionsRect, 0.65f, 0.04f, 1.00f, 0.16f);
                SetAnchors(footerRect, 0.65f, 0.04f, 1.00f, 0.16f);
            }
            else
            {
                contentRect.anchorMin = new Vector2(0.04f, 0.025f);
                contentRect.anchorMax = new Vector2(0.96f, 0.975f);
                contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

                // Top header row: Hamburger Pause on Top-Left, Title centered across header
                SetAnchors(game.pauseButton?.transform as RectTransform, 0.00f, 0.905f, 0.16f, 0.985f);
                SetAnchors(game.heading?.rectTransform, 0.18f, 0.895f, 1.00f, 0.99f);
                SetAnchors(game.stats?.rectTransform, 0.00f, 0.825f, 1.00f, 0.885f);

                // Center Board expanded since bottom controls are now inside the Pause menu
                SetAnchors(boardFrame, 0.00f, 0.26f, 1.00f, 0.81f);
                SetAnchors(instructionsRect, 0.00f, 0.05f, 1.00f, 0.095f);
                SetAnchors(footerRect, 0.00f, 0.005f, 1.00f, 0.042f);
            }
        }

        private static void SetAnchors(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

