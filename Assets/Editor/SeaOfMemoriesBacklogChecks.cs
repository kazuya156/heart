using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.Editor
{
    public static class SeaOfMemoriesBacklogChecks
    {
        [MenuItem("Sea of Memories/Checks/Backlog")]
        public static void Run()
        {
            var history = new ConversationHistory();
            history.Record("page1", "ケン", "最");
            history.Record("page1", "ケン", "最初の台詞");
            Require(history.Entries.Count == 1 && history.Entries[0].Text == "最初の台詞", "Partial-page update duplicated text.");
            history.Record("page2", "GUARD", "返答");
            history.Record("", "", "ignored");
            history.Record("empty", "", " ");
            Require(history.Entries.Count == 2, "Empty text must not enter history.");
            Require(history.Format() == "ケン\n最初の台詞\n\nGUARD\n返答", "Speaker snapshot/order was lost.");
            history.Record("choice", "選択", "選んだ答え");
            Require(history.Entries[2].Speaker == "選択", "Chosen option missing.");
            history.Clear();
            Require(history.Entries.Count == 0 && !history.Format().Contains("ケン"), "New-game history was retained.");
            history.Record("page1", "新しい名前", "new run");
            Require(history.Entries.Count == 1 && history.Entries[0].Speaker == "新しい名前", "Reset left stale page keys.");

            var root = new GameObject("BacklogPageCheck", typeof(RectTransform), typeof(Canvas));
            try
            {
                var child = new GameObject("Page", typeof(RectTransform));
                child.transform.SetParent(root.transform, false);
                var rect = child.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(160f, 60f);
                var text = child.AddComponent<TextMeshProUGUI>();
                text.font = TMP_Settings.defaultFontAsset;
                Require(text.font != null, "TMP default font is missing.");
                text.fontSize = 24f; text.richText = false;
                var pages = new PagedText();
                const string value = "First page has several words. Later pages must not appear in history before being read. This final sentence is still in the future.";
                pages.Show(text, value);
                Require(pages.VisiblePageText == "", "Unrevealed text leaked into history.");
                pages.Tick(0.1f, 10f);
                Require(pages.VisiblePageText == "F", "Typewriter history must contain only the revealed prefix.");
                pages.RevealPage();
                string first = pages.VisiblePageText;
                Require(pages.PageCount > 1 && first.Length > 1 && first.Length < value.Length, "Page fixture must not reveal future pages.");
                int page = pages.PageNumber;
                Require(pages.VisiblePageText == first && pages.PageNumber == page, "Reading history advanced the page.");
                Require(pages.TryNextPage() && pages.VisiblePageText == "", "Next page started with future text revealed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Debug.Log("[SeaOfMemories] Backlog checks passed: partial-page replacement, order, speaker names, choice, reset and no future-page text. Verify pause, scroll and close controls in Play mode.");
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Backlog check: " + message);
        }
    }
}
