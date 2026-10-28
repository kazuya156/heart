using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    // 実際の演出クラスを一時Canvasで確認。ゲームの所持・質問・台本は操作しない。
    public static class SeaOfMemoriesQuestionVisualChecks
    {
        private const string Menu = "Sea of Memories/Checks/Question Visuals (Play mode)";
        private static GameObject host;
        private static ScreenEffects effects;
        private static GuardPortraitView portrait;
        private static Image vignette;
        private static RectTransform portraitRoot;
        private static Vector2 rest;
        private static float savedTimeScale, weak;
        private static double startedAt, deadline;
        private static int stage;

        [MenuItem(Menu, true)]
        public static bool CanRun() { return EditorApplication.isPlaying && !EditorApplication.isPaused && host == null; }

        [MenuItem(Menu)]
        public static void Run()
        {
            if (!CanRun()) return;
            savedTimeScale = Time.timeScale;
            try
            {
                host = new GameObject("TemporaryQuestionVisualChecks", typeof(RectTransform), typeof(Canvas));
                Canvas canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32700;
                var catalog = host.AddComponent<ContentCatalog>();
                catalog.ConfigureSprites(new[]
                {
                    new SpriteCatalogEntry {id = "GUARD_01"}, new SpriteCatalogEntry {id = "GUARD_02"}, new SpriteCatalogEntry {id = "GUARD_03"},
                    new SpriteCatalogEntry {id = "GUARD_04"}, new SpriteCatalogEntry {id = "GUARD_05"}, new SpriteCatalogEntry {id = "GUARD_06"}
                });
                Image fade = ImageChild("Fade", host.transform);
                effects = host.AddComponent<ScreenEffects>();
                effects.Configure(fade, fade.gameObject.AddComponent<CanvasGroup>());
                vignette = ImageChild("Vignette", host.transform);
                effects.ConfigureVignette(vignette);
                Image guard = ImageChild("Guard", host.transform);
                portraitRoot = guard.rectTransform;
                portraitRoot.anchorMin = new Vector2(0.3f, 0.3f);
                portraitRoot.anchorMax = new Vector2(0.7f, 0.7f);
                var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelRoot.transform.SetParent(guard.transform, false);
                var label = labelRoot.GetComponent<TextMeshProUGUI>();
                label.font = TMP_Settings.defaultFontAsset;
                label.raycastTarget = false;
                portrait = host.AddComponent<GuardPortraitView>();
                portrait.Configure(catalog, portraitRoot, guard, label);
                portrait.SetExpression("GUARD_03");
                Require(portrait.CurrentExpression == "GUARD_03", "Expression alias resolution.");
                portrait.SetExpression("");
                Require(portrait.CurrentExpression == "GUARD_03", "Empty expression changed the portrait.");
                effects.SetComboLevel(0);
                Require(effects.VignetteAlpha == 0f, "Combo zero is not clear.");
                effects.SetComboLevel(1); weak = effects.VignetteAlpha;
                effects.SetComboLevel(2); float medium = effects.VignetteAlpha;
                effects.SetComboLevel(3); float strong = effects.VignetteAlpha;
                Require(0f < weak && weak < medium && medium < strong && strong <= 1f, "Combo intensity order.");
                Require(!effects.IsBusy && !vignette.raycastTarget, "Vignette blocks progression/input.");
                Time.timeScale = 0f;
                rest = portraitRoot.anchoredPosition;
                effects.FadeOutVignette(0.08f);
                portrait.Shake(0.08f, 8f);
                stage = 0;
                startedAt = EditorApplication.timeSinceStartup;
                deadline = startedAt + 5d;
                EditorApplication.update += Tick;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            catch (Exception error) { Fail(error); }
        }

        private static void Tick()
        {
            if (host == null) { Cleanup(); return; }
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Animation timed out at timeScale=0.");
                Require(Time.timeScale == 0f, "Unexpected time scale during the test.");
                if (stage == 0)
                {
                    if (effects.IsVignetteFading || portrait.IsShaking) return;
                    Require(effects.VignetteAlpha == 0f && portraitRoot.anchoredPosition == rest, "Animation did not restore alpha/position.");
                    effects.SetComboLevel(3);
                    effects.FadeOutVignette(0.2f);
                    portrait.Shake(0.3f, 8f);
                    startedAt = EditorApplication.timeSinceStartup;
                    stage = 1;
                    return;
                }
                if (stage == 1)
                {
                    if (EditorApplication.timeSinceStartup - startedAt < 0.05d) return;
                    effects.SetComboLevel(1); // 古い消去処理を止めて新しい有効質問の赤を残す。
                    portrait.StopShake();
                    Require(!portrait.IsShaking && portraitRoot.anchoredPosition == rest, "Interrupted shake left an offset.");
                    startedAt = EditorApplication.timeSinceStartup;
                    stage = 2;
                    return;
                }
                if (EditorApplication.timeSinceStartup - startedAt < 0.25d) return;
                Require(!effects.IsVignetteFading && effects.VignetteAlpha == weak, "Old fade erased the new combo.");
                effects.FadeOutVignette(0f);
                portrait.Shake(0f, 8f);
                portrait.ResetView();
                Require(effects.VignetteAlpha == 0f && !portrait.IsShaking && portrait.CurrentExpression == "GUARD_02",
                    "Zero-duration/reset failed.");
                Cleanup();
                Debug.Log("[SeaOfMemories] Question visual checks passed: intensity levels, expression IDs, timeScale=0, fade/shake completion, interruption and reset.");
            }
            catch (Exception error) { Fail(error); }
        }

        private static Image ImageChild(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        }

        private static void Fail(Exception error) { Debug.LogException(error); Cleanup(); }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Time.timeScale = savedTimeScale;
            if (effects != null) { effects.SetComboLevel(0); effects.CancelFade(); }
            if (portrait != null) portrait.StopShake();
            if (host != null) UnityEngine.Object.Destroy(host);
            host = null;
            effects = null;
            portrait = null;
            vignette = null;
            portraitRoot = null;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("[SeaOfMemories] Question visual check failed: " + message);
        }
    }
}
