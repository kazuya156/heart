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
            placeholderLabel.color = id == "BG_07" ? Color.black : Color.white;
            placeholderLabel.gameObject.SetActive(sprite == null);
            return true;
        }

        private static string PlaceholderName(string id)
        {
            switch (id)
            {
                case "BG_TITLE": return "TITLE";
                case "BG_01": return "CELL BARS";
                case "BG_02": return "ROOM";
                case "BG_03": return "DESK";
                case "BG_04": return "BED";
                case "BG_05": return "WINDOW";
                case "BG_06": return "BAD END";
                case "BG_07": return "TRUE END";
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
