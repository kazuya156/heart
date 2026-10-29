using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 家具の検索と拡大画面の表示を担当。PhaseはGameFlowControllerへ依頼する。
    public sealed class ExplorationController : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private FurnitureDefinition[] furniture;
        [SerializeField] private TMP_Text closeupTitle;
        [SerializeField] private Image closeupBackground;
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private DialoguePlayer dialogue;
        [SerializeField] private ItemInspectionView inspectionView;
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private ChoicePanel choices;
        [SerializeField] private Button[] furnitureButtons;
        [SerializeField] private Button returnButton;
        private BackgroundView artworkView;
        private void LateUpdate()
        {
            // Reapply on resolution/aspect changes as well as phase changes.
            RefreshArtworkLayout();
        }
        private BackgroundView ArtworkView
        {
            get
            {
                if (artworkView == null) artworkView = GetComponent<BackgroundView>();
                return artworkView;
            }
        }
        private void Start()
        {
            AddHeadingBacking(closeupTitle);
            Canvas canvas = closeupTitle == null ? null : closeupTitle.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            foreach (TMP_Text label in canvas.GetComponentsInChildren<TMP_Text>(true))
                if (label.gameObject.name == "RoomTitle") AddHeadingBacking(label);
        }

        // A separate non-interactive backing keeps headings readable on bright artwork.
        private static void AddHeadingBacking(TMP_Text label)
        {
            if (label == null || label.transform.parent == null) return;
            label.color = Color.white;
            label.raycastTarget = false;
            RectTransform textRect = label.rectTransform;
            textRect.anchorMin = new Vector2(0.03f, 0.88f);
            textRect.anchorMax = new Vector2(0.29f, 0.96f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            Transform parent = textRect.parent;
            string name = label.gameObject.name + "ContrastBacking";
            if (parent.Find(name) != null) return;
            var plate = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = plate.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = textRect.anchorMin;
            rect.anchorMax = textRect.anchorMax;
            rect.pivot = textRect.pivot;
            rect.anchoredPosition = textRect.anchoredPosition;
            rect.sizeDelta = textRect.sizeDelta + new Vector2(24f, 12f);
            rect.localScale = textRect.localScale;
            rect.localRotation = textRect.localRotation;
            rect.SetSiblingIndex(textRect.GetSiblingIndex());
            Image image = plate.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);
            image.raycastTarget = false;
        }

        private string inspectedItemId = "";
        private int navigationFrame = -1;
        private readonly Dictionary<RectTransform, HotspotLayout> originalItemLayouts =
            new Dictionary<RectTransform, HotspotLayout>();

        private struct HotspotLayout
        {
            public Vector2 anchorMin, anchorMax, offsetMin, offsetMax;
            public HotspotLayout(RectTransform target)
            {
                anchorMin = target.anchorMin;
                anchorMax = target.anchorMax;
                offsetMin = target.offsetMin;
                offsetMax = target.offsetMax;
            }
            public void Restore(RectTransform target)
            {
                target.anchorMin = anchorMin;
                target.anchorMax = anchorMax;
                target.offsetMin = offsetMin;
                target.offsetMax = offsetMax;
            }
        }
        private static readonly string[] FurnitureIds = { "DESK", "BED", "WINDOW" };
        public bool HasAcquisitionUI => HasInspectionUI && choices != null && choices.IsConfigured
            && furnitureButtons != null && furnitureButtons.Length == 3
            && furnitureButtons[0] != null && furnitureButtons[1] != null && furnitureButtons[2] != null && returnButton != null;

        public void ConfigureAcquisition(ChoicePanel panel, Button[] rooms, Button back)
        {
            choices = panel;
            furnitureButtons = rooms;
            returnButton = back;
        }

        public void ResetView()
        {
            inspectedItemId = "";
            navigationFrame = -1;
            if (choices != null) choices.Hide();
            if (inspectionView != null) inspectionView.Hide();
        }
        public bool HasInspectionUI => flow != null && catalog != null && dialogue != null && inspectionView != null
            && inspectionView.IsConfigured && itemButtons != null && itemButtons.Length == 2
            && itemButtons[0] != null && itemButtons[1] != null
            && itemButtons[0].GetComponentInChildren<TMP_Text>(true) != null
            && itemButtons[1].GetComponentInChildren<TMP_Text>(true) != null;

        public void ConfigureInspection(ContentCatalog content, DialoguePlayer player,
            ItemInspectionView view, Button[] buttons)
        {
            catalog = content;
            dialogue = player;
            inspectionView = view;
            itemButtons = buttons;
        }

        public bool IsConfigured
        {
            get
            {
                if (flow == null || closeupTitle == null || closeupBackground == null
                    || furniture == null || furniture.Length != 3) return false;
                var ids = new HashSet<string>();
                foreach (FurnitureDefinition definition in furniture)
                    if (definition == null || string.IsNullOrWhiteSpace(definition.BackgroundId)
                        || definition.ItemIds == null || definition.ItemIds.Count != 2
                        || !ids.Add(definition.Id)) return false;
                return ids.SetEquals(new[] { "DESK", "BED", "WINDOW" });
            }
        }

        public void Configure(GameFlowController gameFlow, FurnitureDefinition[] definitions,
            TMP_Text title, Image background)
        {
            flow = gameFlow;
            furniture = definitions;
            closeupTitle = title;
            closeupBackground = background;
        }

        public void OpenFurniture(string furnitureId)
        {
            if (!IsConfigured || !flow.CanInteractWithWorld() || flow.Phase != GamePhase.Room
                || navigationFrame == Time.frameCount) return;
            FurnitureDefinition found = FindFurniture(furnitureId);
            if (found == null)
            {
                Debug.LogError("[SeaOfMemories] Unknown furniture: " + furnitureId, this);
                return;
            }
            if (flow.IsInventoryFull() || (catalog != null && IsFurnitureComplete(found.Id))
                || !flow.TryEnterCloseup(found.Id)) return;
            navigationFrame = Time.frameCount;
            PlayNavigationSound();
        }

        private void PlayNavigationSound()
        {
            AudioController audio = GetComponent<AudioController>();
            if (audio == null) audio = GetComponentInChildren<AudioController>(true);
            if (audio != null) audio.PlaySe("SE_02");
        }

        private FurnitureDefinition FindFurniture(string furnitureId)
        {
            FurnitureDefinition found = catalog == null ? null : catalog.GetFurniture(furnitureId);
            // メニュー04をまだ適用していないM03のシーンも引き続き動かす。
            if (catalog == null)
                foreach (FurnitureDefinition definition in furniture)
                    if (definition.Id == furnitureId) { found = definition; break; }
            return found;
        }

        public string GetFurnitureBackgroundId(string furnitureId)
        {
            FurnitureDefinition found = FindFurniture(furnitureId);
            return found == null ? "" : found.BackgroundId;
        }

        // 暗幕が完全に覆った時にFlowから呼ぶ。ここではPhaseを変更しない。
        public void ShowFurniture(string furnitureId)
        {
            FurnitureDefinition found = FindFurniture(furnitureId);
            if (found == null) return;
            closeupTitle.text = found.Id;
            closeupBackground.color = catalog != null && catalog.GetSprite(found.BackgroundId) != null ? Color.clear : found.PlaceholderColor;
            RefreshHotspots();
        }

        public void InspectSlot(int slot)
        {
            if (!HasInspectionUI || flow.Phase != GamePhase.Closeup || slot < 0 || slot >= 2) return;
            FurnitureDefinition definition = catalog.GetFurniture(flow.CurrentFurnitureId);
            if (definition != null) InspectItem(definition.ItemIds[slot]);
        }

        public void InspectItem(string itemId)
        {
            if (!HasInspectionUI || !flow.CanInteractWithWorld() || flow.Phase != GamePhase.Closeup
                || navigationFrame == Time.frameCount || flow.HasItem(itemId)) return;
            ItemDefinition item = catalog.GetItem(itemId);
            FurnitureDefinition current = catalog.GetFurniture(flow.CurrentFurnitureId);
            if (item == null || current == null || item.FurnitureId != current.Id) return;
            bool belongsHere = false;
            foreach (string id in current.ItemIds) if (id == itemId) belongsHere = true;
            if (!belongsHere || !flow.TryStartInspection()) return;
            navigationFrame = Time.frameCount;
            inspectedItemId = item.Id;
            AudioController audio = GetComponentInChildren<AudioController>(true);
            if (audio != null) audio.PlaySe("SE_03");
            inspectionView.Show(item);
            dialogue.Play(item.DescriptionSequenceId, FinishInspection);
        }

        private void FinishInspection()
        {
            if (flow.Phase != GamePhase.Inspecting) return;
            navigationFrame = Time.frameCount;
            if (!HasAcquisitionUI)
            {
                // メニュー05未適用のM04シーンは、以前どおり説明再生を確認できる。
                inspectionView.Hide();
                inspectedItemId = "";
                flow.FinishInspection();
                return;
            }
            string requestedItem = inspectedItemId;
            flow.BeginItemChoice();
            choices.Show(new[] { new ChoiceOption("acquire", "取得する"), new ChoiceOption("decline", "取得しない") },
                id => DecideAcquisition(requestedItem, id == "acquire"));
        }

        public void DecideAcquisition(string itemId, bool acquire)
        {
            if (!HasAcquisitionUI || flow.IsItemDetailsOpen || flow.Phase != GamePhase.ItemChoice || string.IsNullOrEmpty(inspectedItemId)
                || inspectedItemId != itemId || navigationFrame == Time.frameCount) return;
            navigationFrame = Time.frameCount;
            choices.Hide();
            inspectionView.Hide();
            inspectedItemId = "";
            if (!acquire) { flow.FinishItemChoice(); return; }
            if (!flow.TryAcquireItem(itemId))
            {
                Debug.LogError("[SeaOfMemories] Acquisition rejected: " + itemId, this);
                flow.FinishItemChoice();
                return;
            }
            flow.PlayAcquisitionReaction();
        }

        public bool IsFurnitureComplete(string furnitureId)
        {
            if (catalog == null || flow == null) return false;
            FurnitureDefinition definition = catalog.GetFurniture(furnitureId);
            if (definition == null || definition.ItemIds == null || definition.ItemIds.Count != 2) return false;
            foreach (string id in definition.ItemIds) if (!flow.HasItem(id)) return false;
            return true;
        }

        public void FinishAcquisitionSequence(string furnitureId)
        {
            if (flow.Phase != GamePhase.PickupReaction) return;
            if (flow.IsInventoryFull()) { flow.StartGuardReturn(); return; }
            if (IsFurnitureComplete(furnitureId)) flow.PlayFurnitureEmpty();
            else flow.ResumeCloseupAfterPickup();
        }

        public void RefreshHotspots()
        {
            if (flow == null) return;
            RefreshArtworkLayout();
            if (furnitureButtons != null)
                for (int i = 0; i < furnitureButtons.Length && i < FurnitureIds.Length; i++)
                    if (furnitureButtons[i] != null)
                        SetHotspotAvailable(furnitureButtons[i], flow.Phase == GamePhase.Room && flow.CanInteractWithWorld()
                            && !flow.IsInventoryFull() && !IsFurnitureComplete(FurnitureIds[i]));
            if (returnButton != null) returnButton.interactable = flow.Phase == GamePhase.Closeup && flow.CanInteractWithWorld();
            if (!HasInspectionUI) return;
            if (string.IsNullOrEmpty(flow.CurrentFurnitureId))
            {
                foreach (Button button in itemButtons)
                {
                    SetHotspotAvailable(button, false);
                    button.gameObject.SetActive(false);
                }
                return;
            }
            FurnitureDefinition current = catalog.GetFurniture(flow.CurrentFurnitureId);
            if (current == null) return;
            for (int i = 0; i < itemButtons.Length; i++)
            {
                ItemDefinition item = catalog.GetItem(current.ItemIds[i]);
                if (item == null) { SetHotspotAvailable(itemButtons[i], false); itemButtons[i].gameObject.SetActive(false); continue; }
                itemButtons[i].GetComponentInChildren<TMP_Text>(true).text = item.DisplayName;
                ApplyItemHotspot(itemButtons[i], item);
                bool embeddedArtwork = catalog.GetSprite(current.BackgroundId) != null;
                HotspotView hotspot = itemButtons[i].GetComponent<HotspotView>();
                if (hotspot != null) hotspot.SetCursorOnly(embeddedArtwork);
                itemButtons[i].GetComponentInChildren<TMP_Text>(true).enabled = !embeddedArtwork;
                bool available = !flow.HasItem(item.Id);
                SetHotspotAvailable(itemButtons[i], available && flow.Phase == GamePhase.Closeup && flow.CanInteractWithWorld());
                itemButtons[i].gameObject.SetActive(available);
            }
        }

        private void RefreshArtworkLayout()
        {
            if (flow == null || catalog == null || ArtworkView == null) return;
            bool room = ArtworkView.CurrentBackgroundId == "BG_03";
            if (room && furnitureButtons != null)
                for (int i = 0; i < furnitureButtons.Length && i < FurnitureIds.Length; i++)
                {
                    Button button = furnitureButtons[i];
                    FurnitureDefinition definition = catalog.GetFurniture(FurnitureIds[i]);
                    if (button == null || definition == null || !definition.UseCustomRoomHotspot) continue;
                    bool placed = ArtworkView.TryPositionHotspot(button.GetComponent<RectTransform>(), definition.RoomHotspotRect);
                    HotspotView hotspot = button.GetComponent<HotspotView>();
                    if (hotspot != null) hotspot.SetCursorOnly(placed);
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.enabled = !placed;
                }
            if (string.IsNullOrEmpty(flow.CurrentFurnitureId) || itemButtons == null) return;
            FurnitureDefinition current = catalog.GetFurniture(flow.CurrentFurnitureId);
            if (current == null || ArtworkView.CurrentBackgroundId != current.BackgroundId) return;
            if (returnButton != null)
                ArtworkView.TryPositionHotspot(returnButton.GetComponent<RectTransform>(), new Rect(0.65f, 0.05f, 0.15f, 0.085f));
            for (int i = 0; i < itemButtons.Length && i < current.ItemIds.Count; i++)
            {
                if (itemButtons[i] == null) continue;
                ItemDefinition item = catalog.GetItem(current.ItemIds[i]);
                if (item != null) ApplyItemHotspot(itemButtons[i], item);
            }
        }

        private void ApplyItemHotspot(Button button, ItemDefinition item)
        {
            RectTransform target = button.GetComponent<RectTransform>();
            if (target == null) return;
            if (!originalItemLayouts.TryGetValue(target, out HotspotLayout original))
            {
                original = new HotspotLayout(target);
                originalItemLayouts.Add(target, original);
            }
            if (!item.UseCustomHotspot || !item.HasValidHotspot)
            {
                // 未設定の家具へ移った時も、直前のアイテムの座標を持ち越さない。
                original.Restore(target);
                return;
            }
            Rect area = item.HotspotRect;
            if (ArtworkView != null && ArtworkView.TryPositionHotspot(target, area)) return;
            target.anchorMin = area.min;
            target.anchorMax = area.max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static void SetHotspotAvailable(Button button, bool available)
        {
            button.interactable = available;
            HotspotView view = button.GetComponent<HotspotView>();
            if (view != null) view.SetAvailable(available);
        }

        public void ReturnToRoom()
        {
            if (flow == null || !flow.CanInteractWithWorld() || flow.Phase != GamePhase.Closeup
                || navigationFrame == Time.frameCount) return;
            navigationFrame = Time.frameCount;
            PlayNavigationSound();
            flow.StartExploration();
        }
    }
}

