using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    public sealed class InventoryPanel : MonoBehaviour
    {
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private TMP_Text heading;
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private GameFlowController flow;
        [SerializeField] private DialoguePlayer dialogue;
        [SerializeField] private ItemDetailsViewer details;
        [SerializeField] private ChoicePanel choices;
        [SerializeField] private UiInputRouter input;
        [SerializeField] private Button[] slotButtons;
        private readonly string[] shownItemIds = new string[GameSession.MaxItemCount];
        private bool inventoryEnabled = true;
        public bool IsDetailsOpen => details != null && details.IsOpen;
        public bool HasDetailsSetup => details != null;
        public bool IsDetailsConfigured
        {
            get
            {
                if (!IsConfigured || flow == null || dialogue == null || details == null || !details.IsConfigured
                    || choices == null || !choices.IsConfigured || input == null || slotButtons == null
                    || slotButtons.Length != GameSession.MaxItemCount) return false;
                foreach (Button button in slotButtons) if (button == null) return false;
                return true;
            }
        }
        public bool IsConfigured
        {
            get
            {
                if (catalog == null || heading == null || group == null || icons == null || labels == null
                    || icons.Length != GameSession.MaxItemCount || labels.Length != GameSession.MaxItemCount) return false;
                for (int i = 0; i < labels.Length; i++) if (icons[i] == null || labels[i] == null) return false;
                return true;
            }
        }

        public void Configure(ContentCatalog content, TMP_Text title, Image[] images, TMP_Text[] names, CanvasGroup canvasGroup)
        {
            catalog = content;
            heading = title;
            icons = images;
            labels = names;
            group = canvasGroup;
        }

        public void ConfigureDetails(GameFlowController gameFlow, DialoguePlayer player, ItemDetailsViewer viewer,
            ChoicePanel choicePanel, UiInputRouter router, Button[] buttons)
        {
            flow = gameFlow;
            dialogue = player;
            details = viewer;
            choices = choicePanel;
            input = router;
            slotButtons = buttons;
        }

        // シーンに保存されたButtonのイベントから、枠番号を受け取る。
        public void OpenSlot(int index)
        {
            if (index >= 0 && index < shownItemIds.Length) OpenDetails(shownItemIds[index]);
        }

        public void OpenDetails(string itemId)
        {
            if (!IsDetailsConfigured || !inventoryEnabled || input.IsConsumedThisFrame
                || !flow.CanReadInventory() || string.IsNullOrEmpty(itemId) || !flow.HasItem(itemId)) return;
            ItemDefinition item = catalog.GetItem(itemId);
            if (item == null) return;
            input.SuppressForCurrentFrame();
            dialogue.Pause();
            // TAKE/LEAVEはDialoguePlayer外から開くため、共通選択肢も明示的に停止する。
            if (choices.IsOpen) choices.SetInteractionEnabled(false);
            details.ShowItem(item);
            details.Open(item.DescriptionSequenceId, OnDetailsClosed);
            if (!details.IsOpen) { OnDetailsClosed(); return; }
            RefreshButtons();
            flow.RefreshInteraction();
        }

        public void CloseDetails()
        {
            if (input != null) input.RequestCloseDetails();
        }

        // タイトルへ戻す等の進行側のリセットは、入力フレーム制限に関係なく閉じる。
        public void DismissDetails()
        {
            if (details != null) details.Close();
        }

        private void OnDetailsClosed()
        {
            if (input != null) input.SuppressForCurrentFrame();
            if (dialogue != null) dialogue.Resume();
            if (choices != null && choices.IsOpen) choices.SetInteractionEnabled(true);
            RefreshButtons();
            if (flow != null) flow.RefreshInteraction();
        }

        public void Refresh(GameSession session)
        {
            if (!IsConfigured) return;
            heading.text = "INFORMATION (" + session.ItemIds.Count + "/" + GameSession.MaxItemCount + ")";
            for (int i = 0; i < labels.Length; i++)
            {
                shownItemIds[i] = i < session.ItemIds.Count ? session.ItemIds[i] : "";
                ItemDefinition item = string.IsNullOrEmpty(shownItemIds[i]) ? null : catalog.GetItem(shownItemIds[i]);
                bool hasImage = item != null && item.Icon != null;
                icons[i].sprite = item == null ? null : item.Icon;
                icons[i].enabled = hasImage;
                labels[i].text = item == null ? "" : item.DisplayName;
                labels[i].rectTransform.anchorMin = hasImage ? new Vector2(0.04f, 0.03f) : new Vector2(0.08f, 0.1f);
                labels[i].rectTransform.anchorMax = hasImage ? new Vector2(0.96f, 0.32f) : new Vector2(0.92f, 0.9f);
            }
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            if (slotButtons == null) return;
            bool canRead = IsDetailsConfigured && inventoryEnabled && flow.CanReadInventory();
            for (int i = 0; i < slotButtons.Length && i < shownItemIds.Length; i++)
                if (slotButtons[i] != null) slotButtons[i].interactable = canRead && !string.IsNullOrEmpty(shownItemIds[i]);
        }

        public void SetEnabled(bool value)
        {
            inventoryEnabled = value;
            if (group == null) return;
            group.alpha = value ? 1f : 0.45f;
            group.interactable = value;
            group.blocksRaycasts = value;
            if (!value) DismissDetails();
            RefreshButtons();
        }
    }
}
