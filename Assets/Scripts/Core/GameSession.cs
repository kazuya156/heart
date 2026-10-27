using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SeaOfMemories
{
    // Unity の表示には触れない、プレイ中だけのデータ。
    // IDと最大数は詳細設計書の固定ルール。Unityの表示やアセットには依存しない。
    public sealed class GameSession
    {
        private readonly List<string> itemIds = new List<string>();
        private readonly HashSet<string> askedIds = new HashSet<string>();
        private readonly ReadOnlyCollection<string> itemView;
        private static readonly HashSet<string> KnownItemIds = new HashSet<string>
        {
            "ITEM_01", "ITEM_02", "ITEM_03", "ITEM_04", "ITEM_05", "ITEM_06"
        };
        public const int MaxItemCount = 4;

        public GameSession() { itemView = itemIds.AsReadOnly(); }

        public IReadOnlyList<string> ItemIds => itemView;
        public int QuestionCount { get; private set; }
        public int ComboCount { get; private set; }
        public int Agitation { get; private set; }
        public GamePhase Phase { get; internal set; } = GamePhase.Title;
        public string CurrentFurnitureId { get; internal set; } = "";

        public bool HasItem(string itemId) { return itemIds.Contains(itemId); }
        public bool HasAsked(string itemId) { return askedIds.Contains(itemId); }

        public QuestionResult TryApplyQuestion(ItemDefinition item)
        {
            QuestionStatus status;
            if (item == null || !KnownItemIds.Contains(item.Id) || !HasItem(item.Id)) status = QuestionStatus.NotOwned;
            else if (HasAsked(item.Id)) status = QuestionStatus.AlreadyAsked;
            else if (QuestionCount >= 3) status = QuestionStatus.LimitReached;
            else status = QuestionStatus.Accepted;
            if (status != QuestionStatus.Accepted)
                return new QuestionResult(status, 0, ComboCount, Agitation, QuestionCount, false, EndingType.None);

            askedIds.Add(item.Id);
            QuestionCount++;
            int delta = 0;
            if (item.IsValid)
            {
                ComboCount++;
                delta = ComboCount == 1 ? 20 : ComboCount == 2 ? 30 : 50;
                Agitation += delta;
            }
            else ComboCount = 0; // 累積した動揺度は消さない。
            bool final = QuestionCount == 3;
            EndingType ending = !final ? EndingType.None : Agitation == 100 ? EndingType.True : EndingType.Bad;
            return new QuestionResult(status, delta, ComboCount, Agitation, QuestionCount, final, ending);
        }

        public bool TryAddItem(string itemId)
        {
            if (!KnownItemIds.Contains(itemId) || HasItem(itemId) || itemIds.Count >= MaxItemCount) return false;
            itemIds.Add(itemId);
            return true;
        }

        public bool IsInventoryFull() { return itemIds.Count == MaxItemCount; }

        public void Reset()
        {
            itemIds.Clear();
            askedIds.Clear();
            QuestionCount = 0;
            ComboCount = 0;
            Agitation = 0;
            CurrentFurnitureId = "";
            // Phase は GameFlowController が更新する。
        }
    }
}
