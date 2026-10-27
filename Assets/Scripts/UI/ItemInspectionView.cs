using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 初回調査の画像表示。所持品の読み返し用ItemDetailsViewerとは別の部品。
    public sealed class ItemInspectionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image largeImage;
        [SerializeField] private TMP_Text placeholder;
        public bool IsConfigured => title != null && largeImage != null && placeholder != null;

        public void Configure(TMP_Text heading, Image image, TMP_Text fallback)
        {
            title = heading;
            largeImage = image;
            placeholder = fallback;
        }

        public void Show(ItemDefinition item)
        {
            title.text = item.DisplayName;
            largeImage.sprite = item.LargeImage;
            largeImage.enabled = item.LargeImage != null;
            placeholder.text = item.DisplayName + "\n[PLACEHOLDER IMAGE]";
            placeholder.gameObject.SetActive(item.LargeImage == null);
            gameObject.SetActive(true);
        }

        public void Hide() { gameObject.SetActive(false); }
    }
}
