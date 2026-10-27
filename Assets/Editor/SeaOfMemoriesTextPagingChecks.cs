using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    // 実際のTMPとプレイヤーを使う。本編のPhase/所持品/台本には触れない。
    public static class SeaOfMemoriesTextPagingChecks
    {
        private const string Menu = "Sea of Memories/Checks/Text Paging (Play mode)";

        [MenuItem(Menu, true)]
        private static bool CanRun() { return EditorApplication.isPlaying && !EditorApplication.isPaused; }

        [MenuItem(Menu)]
        public static void Run()
        {
            if (!CanRun()) return;
            GameObject host = null;
            TextAsset json = null;
            try
            {
                host = new GameObject("TemporaryTextPagingChecks", typeof(RectTransform), typeof(Canvas));
                host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                string longText = "";
                for (int i = 0; i < 25; i++) longText += "Line wrapping keeps every word readable. ";
                json = new TextAsset("{\"sequences\":[{\"id\":\"LONG\",\"nodes\":[{\"type\":\"line\","
                    + "\"speaker\":\"narration\",\"text\":\"" + longText + "\",\"seId\":\"SE_05\"}]}]}");
                var repository = host.AddComponent<ScenarioRepository>();
                Require(repository.Load(json), "Scenario load.");
                var catalog = host.AddComponent<ContentCatalog>();
                var entries = new AudioCatalogEntry[ContentCatalog.RequiredAudioIds.Length];
                for (int i = 0; i < entries.Length; i++) entries[i] = new AudioCatalogEntry { id = ContentCatalog.RequiredAudioIds[i] };
                catalog.ConfigureAudio(entries);
                var audio = host.AddComponent<AudioController>();
                audio.Configure(catalog, Source(host.transform, "A"), Source(host.transform, "B"), Source(host.transform, "SE"), true);
                TMP_Text body = Label(host.transform, "Body");
                var dialogue = host.AddComponent<DialoguePlayer>();
                dialogue.Configure(repository, Label(host.transform, "Speaker"), body);
                dialogue.ConfigureAudio(audio);
                int completed = 0;
                dialogue.Play("LONG", () => completed++);
                int pages = dialogue.PageCount;
                Require(pages > 1, "Long text did not produce multiple pages.");
                Require(body.overflowMode == TextOverflowModes.Page && body.text == longText, "Text was changed or not paged.");
                Require(audio.SePlayCount == 1 && audio.LastSeId == "SE_05", "Node SE once.");
                dialogue.RequestAdvance();
                Require(dialogue.CurrentPage == 1 && completed == 0 && audio.SePlayCount == 1, "Reveal advanced or played sound.");
                int visible = body.maxVisibleCharacters;
                dialogue.Pause();
                dialogue.RequestAdvance();
                dialogue.Resume();
                Require(dialogue.CurrentPage == 1 && body.maxVisibleCharacters == visible, "Pause lost page/reveal position.");
                for (int i = 2; i <= pages; i++)
                {
                    dialogue.RequestAdvance();
                    Require(dialogue.CurrentPage == i && completed == 0, "Page skipped or completed early.");
                    Require(audio.SePlayCount == i, "Page SE repeated a node command.");
                    if (i == 2)
                    {
                        int pageStart = body.maxVisibleCharacters;
                        dialogue.Pause();
                        dialogue.RequestAdvance();
                        dialogue.Resume();
                        Require(dialogue.CurrentPage == 2 && body.maxVisibleCharacters == pageStart,
                            "Pause lost the second page position.");
                    }
                    dialogue.RequestAdvance();
                    Require(dialogue.CurrentPage == i && audio.SePlayCount == i, "Page reveal changed position/sound.");
                }
                dialogue.RequestAdvance();
                dialogue.RequestAdvance();
                Require(completed == 1 && audio.SePlayCount == pages + 1, "Final completion/SE not once.");

                // 即時心情文はページごとに全文表示し、最終ページで一度だけ操作を解放する。
                int presented = 0;
                dialogue.PlayImmediate("LONG", () => presented++);
                Require(presented == 0, "Immediate prompt unlocked before its last page.");
                int immediatePages = dialogue.PageCount;
                for (int i = 2; i <= immediatePages; i++)
                {
                    Require(dialogue.TryAdvanceImmediatePage() && dialogue.CurrentPage == i, "Immediate page advance.");
                    Require(presented == (i == immediatePages ? 1 : 0), "Immediate ready timing.");
                }
                Require(!dialogue.TryAdvanceImmediatePage() && presented == 1, "Immediate message completed twice.");
                int cancelled = 0;
                dialogue.PlayImmediate("LONG", () => cancelled++);
                dialogue.StopPlayback();
                Require(!dialogue.TryAdvanceImmediatePage() && cancelled == 0, "Stopped message retained its callback.");

                // 読み返しも最終ページまで閉じず、台本内のSEを実行しない。
                var panel = new GameObject("Details", typeof(RectTransform));
                panel.transform.SetParent(host.transform, false);
                var details = panel.AddComponent<ItemDetailsViewer>();
                var image = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                image.transform.SetParent(panel.transform, false);
                details.Configure(repository, Label(panel.transform, "Title"), Label(panel.transform, "Speaker"),
                    Label(panel.transform, "Body"), image.GetComponent<Image>(), Label(panel.transform, "Placeholder"));
                int closed = 0, soundCount = audio.SePlayCount;
                details.Open("LONG", () => closed++);
                int detailPages = details.PageCount;
                Require(detailPages > 1, "Details did not paginate.");
                for (int i = 1; i <= detailPages; i++)
                {
                    details.RequestAdvance();
                    Require(details.IsOpen && details.CurrentPage == i && closed == 0, "Details closed on reveal.");
                    details.RequestAdvance();
                    Require(i == detailPages ? !details.IsOpen : details.CurrentPage == i + 1, "Details page progression.");
                }
                Require(closed == 1 && audio.SePlayCount == soundCount, "Details close count or node sound replay.");

                // 改行/空文字/タグ、次ページの文字送り開始位置を実TMPで確認する。
                var paging = new PagedText();
                paging.Show(body, "", true);
                Require(paging.PageCount == 1 && paging.IsPageRevealed && !paging.TryNextPage(), "Empty text.");
                paging.Show(body, "<b>One</b>\nTwo\nThree", true);
                Require(body.text == "<b>One</b>\nTwo\nThree" && paging.IsPageRevealed, "Tags/newlines changed.");
                paging.Show(body, longText, true);
                Require(paging.TryNextPage(), "Missing next page.");
                int start = body.maxVisibleCharacters;
                paging.Tick(0.25f, 40f);
                Require(body.maxVisibleCharacters > start, "Second-page typing did not advance.");
                paging.RevealPage();
                Require(paging.IsPageRevealed, "Second-page reveal.");
                Debug.Log("[SeaOfMemories] Text paging checks passed: per-page reveal, completion once, page/node SE, pause/resume, immediate prompts, cancellation, independent reread, empty text, tags and newlines.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (json != null) UnityEngine.Object.DestroyImmediate(json);
            }
        }

        private static TMP_Text Label(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            TMP_Text label = root.GetComponent<TextMeshProUGUI>();
            label.rectTransform.sizeDelta = new Vector2(320f, 96f);
            label.font = TMP_Settings.defaultFontAsset;
            Require(label.font != null, "Default TMP font is missing.");
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.richText = true;
            label.color = Color.clear;
            label.raycastTarget = false;
            return label;
        }

        private static AudioSource Source(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.mute = true;
            return source;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[SeaOfMemories] Text paging check failed: " + message);
        }
    }
}
