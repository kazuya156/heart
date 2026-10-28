using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 1枚の共通背景を表示する。切替時刻・フェード・PhaseはGameFlowControllerが管理する。
    public sealed class BackgroundView : MonoBehaviour
    {
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private Image matte;
        [SerializeField] private Image artwork;
        [SerializeField] private TMP_Text placeholderLabel;
        public bool IsConfigured => catalog != null && matte != null && artwork != null
            && matte != artwork && placeholderLabel != null;
        public string CurrentBackgroundId { get; private set; } = "";

        public void Configure(ContentCatalog content, Image backing, Image picture, TMP_Text fallback)
        {
            catalog = content;
            matte = backing;
            artwork = picture;
            placeholderLabel = fallback;
            if (!IsConfigured) return;
            matte.raycastTarget = false;
            artwork.raycastTarget = false;
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = true;
            placeholderLabel.raycastTarget = false;
        }

        // Uses the actual aspect-fitted artwork rectangle, not the narrower UI panel.
        // Origin is bottom-left of the visible image. Does not change parent or listeners.
        public bool TryPositionHotspot(RectTransform target, Rect area)
        {
            if (!IsConfigured || target == null || artwork.sprite == null || !artwork.enabled
                || !(target.parent is RectTransform parent)) return false;
            Rect source = artwork.rectTransform.rect;
            Vector2 spriteSize = artwork.sprite.rect.size;
            if (source.width <= 0f || source.height <= 0f || spriteSize.x <= 0f || spriteSize.y <= 0f) return false;
            if (artwork.preserveAspect)
            {
                float fit = Mathf.Min(source.width / spriteSize.x, source.height / spriteSize.y);
                Vector2 size = spriteSize * fit;
                source = new Rect(source.min + Vector2.Scale(source.size - size, artwork.rectTransform.pivot), size);
            }
            Vector2 localMin = source.min + Vector2.Scale(source.size, area.min);
            Vector2 localMax = source.min + Vector2.Scale(source.size, area.max);
            Vector3 min = parent.InverseTransformPoint(artwork.rectTransform.TransformPoint(localMin));
            Vector3 max = parent.InverseTransformPoint(artwork.rectTransform.TransformPoint(localMax));
            target.anchorMin = target.anchorMax = Vector2.zero;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
            target.anchoredPosition = new Vector2((min.x + max.x) * 0.5f - parent.rect.xMin,
                (min.y + max.y) * 0.5f - parent.rect.yMin);
            return true;
        }

        public bool SetBackground(string id)
        {
            if (string.IsNullOrEmpty(id)) return true; // 空の指示は表示維持。
            if (!IsConfigured || !ContentCatalog.IsBackgroundId(id) || !catalog.HasSpriteId(id))
            {
                Debug.LogError("[SeaOfMemories] Background references or ID are missing: " + id, this);
                return false;
            }
            Sprite sprite = catalog.GetSprite(id);
            CurrentBackgroundId = id;
            // 未受領の画像へ切り替えた時も、直前のSpriteを必ず外す。
            artwork.sprite = sprite;
            artwork.color = Color.white;
            artwork.enabled = sprite != null;
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            matte.color = sprite != null ? Color.black : PlaceholderColor(id);
            matte.raycastTarget = false;
            placeholderLabel.text = PlaceholderName(id) + " / BACKGROUND PLACEHOLDER";
            placeholderLabel.color = id == "BG_08" ? Color.black : Color.white;
            placeholderLabel.gameObject.SetActive(sprite == null);
            return true;
        }

        private static string PlaceholderName(string id)
        {
            switch (id)
            {
                case "BG_01": return "TITLE LOGO";
                case "BG_02": return "SEA";
                case "BG_03": return "ROOM";
                case "BG_04": return "CELL BARS";
                case "BG_05": return "DESK";
                case "BG_06": return "BED";
                case "BG_07": return "WINDOW";
                case "BG_08": return "MORNING";
                case "BG_09": return "MIDNIGHT";
                default: return "";
            }
        }

        private static Color PlaceholderColor(string id)
        {
            switch (id)
            {
                case "BG_TITLE": return new Color(0.025f, 0.075f, 0.12f);
                case "BG_01": return new Color(0.10f, 0.12f, 0.14f);
                case "BG_02": return new Color(0.05f, 0.14f, 0.18f);
                case "BG_03": return new Color(0.18f, 0.13f, 0.09f);
                case "BG_04": return new Color(0.10f, 0.14f, 0.23f);
                case "BG_05": return new Color(0.05f, 0.22f, 0.27f);
                case "BG_06": return Color.black;
                case "BG_07": return Color.white;
                default: return Color.black;
            }
        }
    }
}
