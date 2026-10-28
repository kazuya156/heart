using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SeaOfMemories
{
    public sealed class ConversationHistory
    {
        public sealed class Entry
        {
            public string Speaker { get; }
            public string Text { get; internal set; }
            internal Entry(string speaker, string text) { Speaker = speaker ?? ""; Text = text; }
        }
        private readonly List<Entry> entries = new List<Entry>();
        private readonly Dictionary<string, int> indices = new Dictionary<string, int>();
        private readonly ReadOnlyCollection<Entry> view;
        public IReadOnlyList<Entry> Entries => view;
        public ConversationHistory() { view = entries.AsReadOnly(); }

        // Updating a partly revealed page must not duplicate it or discard an earlier page.
        public void Record(string key, string speaker, string visibleText)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(visibleText)) return;
            if (indices.TryGetValue(key, out int index)) entries[index].Text = visibleText;
            else { indices.Add(key, entries.Count); entries.Add(new Entry(speaker, visibleText)); }
        }

        public void Clear() { entries.Clear(); indices.Clear(); }
        public string Format()
        {
            var result = new StringBuilder();
            foreach (Entry entry in entries)
            {
                if (result.Length > 0) result.Append("\n\n");
                if (!string.IsNullOrEmpty(entry.Speaker)) result.Append(entry.Speaker).Append('\n');
                result.Append(entry.Text);
            }
            return result.Length == 0 ? "まだ履歴はありません。" : result.ToString();
        }
    }
}
