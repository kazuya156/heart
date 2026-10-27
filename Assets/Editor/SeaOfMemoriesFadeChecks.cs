using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    // Play中に一時的なCanvasで実際のScreenEffectsを検証。既存シーン/所持/質問は変更しない。
    public static class SeaOfMemoriesFadeChecks
    {
        private static GameObject host;
        private static ScreenEffects effects;
        private static Image overlay;
        private static CanvasGroup group;
        private static float savedTimeScale;
        private static int stage, covered, completed, cancelled, startedFrame;
        private static double startedAt, deadline;
        private const string Menu = "Sea of Memories/Checks/Fade Timing (Play mode)";

        [MenuItem(Menu, true)]
        public static bool CanRun() { return EditorApplication.isPlaying && !EditorApplication.isPaused && host == null; }

        [MenuItem(Menu)]
        public static void Run()
        {
            if (!CanRun()) return;
            savedTimeScale = Time.timeScale;
            try
            {
                host = new GameObject("TemporaryFadeChecks", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                Canvas canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32700;
                var child = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                child.transform.SetParent(host.transform, false);
                var rect = child.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                overlay = child.GetComponent<Image>();
                group = child.GetComponent<CanvasGroup>();
                effects = host.AddComponent<ScreenEffects>();
                effects.Configure(overlay, group);
                effects.Covered += OnCovered;
                effects.Completed += OnCompleted;
                effects.Cancelled += OnCancelled;
                stage = 0;
                ResetCounts();
                effects.FadeBackground("ZERO", 0f);
                Require(covered == 1 && completed == 1 && cancelled == 0, "Zero-duration event counts.");
                AssertReleased();
                Time.timeScale = 0f;
                BeginTimed(1);
                EditorApplication.update += Tick;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            catch (Exception error) { Fail(error); }
        }

        private static void BeginTimed(int nextStage)
        {
            stage = nextStage;
            ResetCounts();
            startedAt = EditorApplication.timeSinceStartup;
            deadline = startedAt + 5d;
            startedFrame = Time.frameCount;
            if (stage == 1) effects.FadeBackground("BG_02", 0.08f);
            else effects.PlayEndingFade(true, 0.08f);
            Require(effects.IsBusy && group.blocksRaycasts && overlay.gameObject.activeSelf, "Input shield not active.");
        }

        private static void Tick()
        {
            if (host == null) { Cleanup(); return; }
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Fade timed out at timeScale=0.");
                if (completed == 0) return;
                Require(covered == 1 && completed == 1 && cancelled == 0, "Timed fade event counts.");
                Require(Time.frameCount > startedFrame && Time.timeScale == 0f, "Fade did not run across paused game frames.");
                Require(EditorApplication.timeSinceStartup - startedAt >= 0.06d, "Fade completed too early.");
                AssertReleased();
                if (stage == 1) { BeginTimed(2); return; }
                stage = 3;
                ResetCounts();
                effects.FadeBackground("CANCEL", 1f);
                Require(effects.IsBusy && group.blocksRaycasts, "Cancel setup did not acquire the shield.");
                effects.CancelFade();
                Require(covered == 0 && completed == 0 && cancelled == 1, "Cancel fired completion or missed cancellation.");
                AssertReleased();
                Cleanup();
                Debug.Log("[SeaOfMemories] Fade checks passed: zero duration, black/white at timeScale=0, midpoint opacity, one completion, cancellation and input shield release.");
            }
            catch (Exception error) { Fail(error); }
        }

        private static void OnCovered(string id)
        {
            covered++;
            try
            {
                Require(effects.IsBusy && group.alpha == 1f && group.blocksRaycasts, "Midpoint is not opaque and locked.");
                Require(id == (stage == 0 ? "ZERO" : stage == 1 ? "BG_02" : "TRUE_END"), "Wrong covered target.");
                Require(overlay.color == (stage == 2 ? Color.white : Color.black), "Wrong fade color.");
            }
            catch (Exception error) { Fail(error); }
        }

        private static void OnCompleted() { completed++; }
        private static void OnCancelled() { cancelled++; }
        private static void ResetCounts() { covered = completed = cancelled = 0; }
        private static void AssertReleased()
        {
            Require(!effects.IsBusy && group.alpha == 0f && !group.blocksRaycasts && !overlay.gameObject.activeSelf,
                "Fade left an active input shield.");
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        }

        private static void Fail(Exception error)
        {
            Debug.LogException(error);
            Cleanup();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Time.timeScale = savedTimeScale;
            if (effects != null)
            {
                effects.Covered -= OnCovered;
                effects.Completed -= OnCompleted;
                effects.Cancelled -= OnCancelled;
                effects.CancelFade();
            }
            if (host != null) UnityEngine.Object.Destroy(host);
            host = null;
            effects = null;
            overlay = null;
            group = null;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("[SeaOfMemories] Fade check failed: " + message);
        }
    }
}
