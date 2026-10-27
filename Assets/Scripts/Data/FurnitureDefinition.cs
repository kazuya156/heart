using UnityEngine;

namespace SeaOfMemories
{
    // 画像が届く前も、設計書の家具ID・背景ID・所属アイテムIDを固定しておく。
    [CreateAssetMenu(menuName = "Sea of Memories/Furniture Definition")]
    public sealed class FurnitureDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string backgroundId;
        [SerializeField] private string[] itemIds;
        [SerializeField] private Color placeholderColor = new Color(0.06f, 0.16f, 0.21f);

        public string Id => id;
        public string BackgroundId => backgroundId;
        public System.Collections.Generic.IReadOnlyList<string> ItemIds => itemIds;
        public Color PlaceholderColor => placeholderColor;

        public void Configure(string furnitureId, string background, string firstItem, string secondItem, Color color)
        {
            id = furnitureId;
            backgroundId = background;
            itemIds = new[] { firstItem, secondItem };
            placeholderColor = color;
        }
    }
}
