using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    // ゲーム本体の所持・質問・Phaseを操作しない独立した再生テスト。
    public static class SeaOfMemoriesAudioChecks
    {
        private const string Menu = "Sea of Memories/Checks/Audio (Play mode)";
        private static GameObject host;
        private static AudioController controller;
        private static AudioSource first, second;
        private static TextAsset scenario;
        private static float savedTimeScale;
        private static double deadline;
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
                host = new GameObject("TemporaryAudioChecks", typeof(RectTransform), typeof(Canvas));
                var catalog = host.AddComponent<ContentCatalog>();
                var entries = new AudioCatalogEntry[ContentCatalog.RequiredAudioIds.Length];
                for (int i = 0; i < entries.Length; i++) entries[i] = new AudioCatalogEntry { id = ContentCatalog.RequiredAudioIds[i] };
                catalog.ConfigureAudio(entries);
                Require(catalog.ValidateAudio() && !ContentCatalog.IsSeId("SE_04"), "ID registration/retired SE_04.");
                first = Source("A");
                second = Source("B");
                AudioSource se = Source("SE");
                controller = host.AddComponent<AudioController>();
                controller.Configure(catalog, first, second, se, true);
                var repository = host.AddComponent<ScenarioRepository>();
                scenario = new TextAsset("{\"sequences\":[{\"id\":\"A\",\"nodes\":["
                    + "{\"type\":\"line\",\"speaker\":\"narration\",\"text\":\"Audio node starts once.\",\"bgmId\":\"BGM_01\",\"seId\":\"SE_05\"},"
                    + "{\"type\":\"line\",\"speaker\":\"narration\",\"text\":\"No audio on this node.\"}]}]}");
                Require(repository.Load(scenario) && repository.HasAudioCommands, "Scenario audio parsing.");
                var dialogue = host.AddComponent<DialoguePlayer>();
                dialogue.Configure(repository, Label("Speaker"), Label("Body"));
                dialogue.ConfigureAudio(controller);
                dialogue.Play("A", null);
                Require(controller.SePlayCount == 1 && controller.LastSeId == "SE_05", "Node SE did not run once.");
                dialogue.Pause();
                dialogue.RequestAdvance();
                dialogue.Resume();
                Require(controller.SePlayCount == 1, "Pause/resume replayed the node SE.");
                dialogue.RequestAdvance(); // 全文表示だけ。
                Require(controller.SePlayCount == 1, "Reveal-all played page SE.");
                dialogue.RequestAdvance(); // 実際に次の行へ。
                Require(controller.SePlayCount == 2 && controller.LastSeId == "SE_01", "Page advance did not play exactly one SE.");
                dialogue.StopPlayback();
                Time.timeScale = 0f;
                stage = 0;
                deadline = EditorApplication.timeSinceStartup + 5d;
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
                Require(EditorApplication.timeSinceStartup < deadline, "Fade timed out at timeScale=0.");
                if (controller.IsCrossFading) return;
                if (stage == 0)
                {
                    Require(controller.CurrentBgmId == "BGM_01", "Initial BGM.");
                    controller.PlayBgm("BGM_01");
                    Require(!controller.IsCrossFading, "Same-ID BGM started a new fade.");
                    controller.CrossFadeBgm("BGM_02", 0.08f);
                    controller.CrossFadeBgm("BGM_03", 0.08f);
                    stage = 1;
                    return;
                }
                Require(controller.CurrentBgmId == "BGM_03", "Interrupted fade chose an old target.");
                Require((first.isPlaying ? 1 : 0) + (second.isPlaying ? 1 : 0) == 1, "Old BGM source was left running.");
                controller.CrossFadeBgm("BGM_02", 1f);
                controller.StopAll();
                Require(!controller.IsCrossFading && controller.CurrentBgmId == "" && !first.isPlaying && !second.isPlaying,
                    "StopAll left audio or fade running.");
                controller.PlayBgm("BGM_01");
                controller.CrossFadeBgm("BGM_02", 1f);
                controller.enabled = false;
                Require(!controller.IsCrossFading && controller.CurrentBgmId == "", "Disable did not stop the fade.");
                controller.enabled = true;
                controller.CrossFadeBgm("BGM_04", 0f);
                Require(!controller.IsCrossFading && controller.CurrentBgmId == "BGM_04", "Zero-duration switch.");
                controller.PlaySe("SE_06");
                Require(controller.SePlayCount == 3 && controller.LastSeId == "SE_06", "Independent one-shot SE.");
                Cleanup();
                Debug.Log("[SeaOfMemories] Audio checks passed: node once, pause/resume, reveal/page SE, BGM timeScale=0, interruption, stop, disable and zero duration.");
            }
            catch (Exception error) { Fail(error); }
        }

        private static AudioSource Source(string objectName)
        {
            var sourceRoot = new GameObject(objectName);
            sourceRoot.transform.SetParent(host.transform, false);
            var source = sourceRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0f;
            return source;
        }

        private static TMP_Text Label(string objectName)
        {
            var labelRoot = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelRoot.transform.SetParent(host.transform, false);
            var label = labelRoot.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            // このチェックの短文は1ページに収め、改ページは専用チェックで検証する。
            label.rectTransform.sizeDelta = new Vector2(1000f, 240f);
            label.fontSize = 24f;
            label.color = Color.clear;
            label.raycastTarget = false;
            return label;
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
            if (controller != null) controller.StopAll();
            if (host != null) UnityEngine.Object.Destroy(host);
            if (scenario != null) UnityEngine.Object.Destroy(scenario);
            host = null;
            controller = null;
            first = second = null;
            scenario = null;
        }
        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("[SeaOfMemories] Audio check failed: " + message);
        }
    }
}

