using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 本編とは別の再生位置を持つ。説明のlineだけを読み、ゲーム進行・音・表情には触れない。
    public sealed class ItemDetailsViewer : MonoBehaviour
    {
        [SerializeField] private ScenarioRepository repository;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text imagePlaceholder;
        [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
        private readonly List<ScenarioNode> lines = new List<ScenarioNode>();
        private Action onClose;
        private int lineIndex;
        private readonly PagedText pages = new PagedText();
        public int CurrentPage => pages.PageNumber;
        public int PageCount => pages.PageCount;
        public bool IsOpen { get; private set; }
        public bool IsConfigured => repository != null && titleText != null && speakerText != null
            && bodyText != null && itemImage != null && imagePlaceholder != null;

        public void Configure(ScenarioRepository scenarios, TMP_Text title, TMP_Text speaker, TMP_Text body,
            Image image, TMP_Text placeholder)
        {
            repository = scenarios;
            titleText = title;
            speakerText = speaker;
            bodyText = body;
            itemImage = image;
            imagePlaceholder = placeholder;
        }

        public void ShowItem(ItemDefinition item)
        {
            if (!IsConfigured || item == null) return;
            titleText.text = item.DisplayName;
            itemImage.sprite = item.LargeImage;
            itemImage.enabled = item.LargeImage != null;
            imagePlaceholder.gameObject.SetActive(item.LargeImage == null);
            imagePlaceholder.text = item.DisplayName + "\n[IMAGE PLACEHOLDER]";
        }

        public void Open(string sequenceId, Action closed)
        {
            if (IsOpen || !IsConfigured) return;
            lines.Clear();
            // 次シーケンスも含めて先に検証。選択肢は表示・実行しない。
            var visited = new HashSet<string>();
            string next = sequenceId;
            while (!string.IsNullOrEmpty(next))
            {
                if (!visited.Add(next)) { Reject("Description cycle: " + next); return; }
                ScenarioSequence sequence = repository.Find(next);
                if (sequence == null || sequence.nodes == null || sequence.nodes.Length == 0)
                { Reject("Description is missing or empty: " + next); return; }
                foreach (ScenarioNode node in sequence.nodes)
                {
                    if (node == null || node.type != "line" || node.text == null)
                    { Reject("Reread accepts line nodes only: " + next); return; }
                    lines.Add(node);
                }
                next = sequence.nextSequenceId;
            }
            if (lines.Count == 0) { Reject("Description ID is empty."); return; }
            onClose = closed;
            lineIndex = 0;
            IsOpen = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowLine();
        }

        private void Update()
        {
            if (!IsOpen) return;
            pages.Tick(Time.unscaledDeltaTime, charactersPerSecond);
        }

        public void RequestAdvance()
        {
            if (!IsOpen) return;
            if (!pages.IsPageRevealed)
            {
                pages.RevealPage();
                return;
            }
            if (pages.TryNextPage()) return;
            lineIndex++;
            if (lineIndex == lines.Count) { Close(); return; }
            ShowLine();
        }

        private void ShowLine()
        {
            ScenarioNode node = lines[lineIndex];
            speakerText.text = node.speaker == "guard" ? "GUARD" : node.speaker == "hero" ? "YOU" : "";
            pages.Show(bodyText, node.text);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Action closed = onClose;
            onClose = null;
            lines.Clear();
            pages.Clear();
            gameObject.SetActive(false);
            closed?.Invoke();
        }

        private void OnDisable()
        {
            // 外から非表示にされても本編を停止したままにしない。
            if (IsOpen) Close();
        }

        private void Reject(string message)
        {
            lines.Clear();
            Debug.LogError("[SeaOfMemories] " + message, this);
        }
    }
}

