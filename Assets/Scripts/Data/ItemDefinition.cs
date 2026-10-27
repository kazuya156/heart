using UnityEngine;

namespace SeaOfMemories
{
    [CreateAssetMenu(menuName = "Sea of Memories/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private bool isValid;
        [SerializeField] private string furnitureId;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite largeImage;
        [SerializeField] private string descriptionSequenceId;
        [SerializeField] private string questionSequenceId;
        [SerializeField] private string replySequenceId;
        [SerializeField] private string questionLabel;
        [Header("Exploration click area")]
        [SerializeField, Tooltip("Use this item's area instead of the shared scene button layout.")]
        private bool useCustomHotspot;
        [SerializeField, Tooltip("Normalized within the Closeup button parent. Origin: bottom left. X/Y/Width/Height are 0 to 1.")]
        private Rect hotspotRect = new Rect(0.1f, 0.2f, 0.3f, 0.4f);

        public string Id => id;
        public string DisplayName => displayName;
        public bool IsValid => isValid;
        public string FurnitureId => furnitureId;
        public Sprite Icon => icon;
        public Sprite LargeImage => largeImage;
        public string DescriptionSequenceId => descriptionSequenceId;
        public string QuestionSequenceId => questionSequenceId;
        public string ReplySequenceId => replySequenceId;
        public string QuestionLabel => questionLabel;
        public bool UseCustomHotspot => useCustomHotspot;
        public Rect HotspotRect => hotspotRect;
        public bool HasValidHotspot => !useCustomHotspot ||
            (IsFinite(hotspotRect.x) && IsFinite(hotspotRect.y)
            && IsFinite(hotspotRect.width) && IsFinite(hotspotRect.height)
            && hotspotRect.x >= 0f && hotspotRect.y >= 0f
            && hotspotRect.width > 0f && hotspotRect.height > 0f
            && hotspotRect.xMax <= 1f && hotspotRect.yMax <= 1f);

        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        public void ConfigureQuestion(string question, string reply, string label)
        {
            questionSequenceId = question;
            replySequenceId = reply;
            questionLabel = label;
        }

        // 初期データ作成用。本番画像と台本は後からInspectorで差し替えられる。
        public void Configure(string itemId, string name, bool valid, string owner, string description)
        {
            id = itemId;
            displayName = name;
            isValid = valid;
            furnitureId = owner;
            descriptionSequenceId = description;
        }
    }
}

