using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 台本の接続先やゲーム状態は持たない。表示と、選ばれたIDの通知だけを担当する。
    public sealed class ChoiceOption
    {
        public string Id { get; }
        public string Label { get; }

        public ChoiceOption(string id, string label) { Id = id; Label = label; }
    }

    public sealed class ChoicePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform optionContainer;
        [SerializeField] private Button optionTemplate;
        [SerializeField] private AudioController audioController;
        [SerializeField, Min(1f)] private float minimumOptionHeight = 84f;
        [SerializeField] private Vector2 optionPadding = new Vector2(24f, 14f);
        [SerializeField, Min(0f)] private float optionSpacing = 18f;
        public void ConfigureAudio(AudioController controller) { audioController = controller; }

        private readonly List<Button> buttons = new List<Button>();
        private Action<string> onPick;
        private int shownFrame;
        private int generation;
        private bool interactionEnabled;
        private RectTransform viewport;
        private ScrollRect scroll;
        private Scrollbar scrollbar;
        private Vector2 measuredSize;
        public bool IsOpen { get; private set; }
        public bool IsConfigured => optionContainer != null && optionTemplate != null
            && optionTemplate.GetComponentInChildren<TMP_Text>(true) != null;

        public void Configure(RectTransform container, Button template)
        {
            optionContainer = container;
            optionTemplate = template;
        }

        public void Show(IReadOnlyList<ChoiceOption> options, Action<string> picked)
        {
            if (!IsConfigured) throw new InvalidOperationException("ChoicePanel UI is not configured.");
            if (options == null || options.Count == 0) throw new ArgumentException("Choices are empty.", nameof(options));
            if (picked == null) throw new ArgumentNullException(nameof(picked));
            var ids = new HashSet<string>();
            foreach (ChoiceOption option in options)
                if (option == null || string.IsNullOrWhiteSpace(option.Id)
                    || string.IsNullOrWhiteSpace(option.Label) || !ids.Add(option.Id))
                    throw new ArgumentException("Choice IDs must be unique and labels must be present.", nameof(options));

            Hide();
            onPick = picked;
            IsOpen = true;
            interactionEnabled = true;
            shownFrame = Time.frameCount;
            int currentGeneration = generation;
            optionTemplate.gameObject.SetActive(false);
            EnsureViewport();
            foreach (ChoiceOption option in options)
            {
                Button button = Instantiate(optionTemplate, optionContainer);
                button.name = "Option_" + option.Id;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                label.richText = false;
                label.text = option.Label;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.RemoveAllListeners();
                string id = option.Id;
                button.onClick.AddListener(() => Pick(id, currentGeneration));
                button.interactable = true;
                button.gameObject.SetActive(true);
                buttons.Add(button);
            }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RebuildOptions();
            scroll.verticalNormalizedPosition = 1f;
        }

        private void EnsureViewport()
        {
            if (viewport != null) return;
            var root = new GameObject("ChoiceViewport", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport = root.GetComponent<RectTransform>();
            viewport.SetParent(optionContainer.parent, false);
            viewport.SetSiblingIndex(optionContainer.GetSiblingIndex());
            viewport.anchorMin = optionContainer.anchorMin;
            viewport.anchorMax = optionContainer.anchorMax;
            viewport.pivot = optionContainer.pivot;
            viewport.sizeDelta = optionContainer.sizeDelta;
            viewport.anchoredPosition = optionContainer.anchoredPosition;
            viewport.localRotation = optionContainer.localRotation;
            viewport.localScale = optionContainer.localScale;
            root.GetComponent<Image>().color = Color.clear;
            optionContainer.SetParent(viewport, false);
            optionContainer.anchorMin = new Vector2(0f, 1f);
            optionContainer.anchorMax = Vector2.one;
            optionContainer.pivot = new Vector2(0.5f, 1f);
            optionContainer.sizeDelta = Vector2.zero;
            optionContainer.anchoredPosition = Vector2.zero;
            optionContainer.localScale = Vector3.one;
            optionContainer.localRotation = Quaternion.identity;

            scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = optionContainer;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 40f;

            var track = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.SetParent(viewport, false);
            trackRect.anchorMin = new Vector2(1f, 0f);
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = new Vector2(-14f, 0f);
            trackRect.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.SetParent(trackRect, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = new Color(0.5f, 0.9f, 0.95f, 0.8f);
            scrollbar = track.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void LateUpdate()
        {
            if (IsOpen && viewport != null && (viewport.rect.size - measuredSize).sqrMagnitude > 0.25f)
                RebuildOptions();
        }

        private void RebuildOptions()
        {
            Canvas.ForceUpdateCanvases();
            VerticalLayoutGroup layout = optionContainer.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = optionContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = Mathf.Max(0f, optionSpacing);
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionContainer);
            Vector2 padding = new Vector2(Mathf.Max(0f, optionPadding.x), Mathf.Max(0f, optionPadding.y));
            float height = layout.padding.vertical + Mathf.Max(0, buttons.Count - 1) * layout.spacing;
            LayoutElement templateLayout = optionTemplate.GetComponent<LayoutElement>();
            float minimum = Mathf.Max(minimumOptionHeight, templateLayout == null ? 0f : templateLayout.minHeight);
            foreach (Button button in buttons)
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                label.enableAutoSizing = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.margin = Vector4.zero;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = padding;
                label.rectTransform.offsetMax = -padding;
                float width = Mathf.Max(1f, button.GetComponent<RectTransform>().rect.width - 2f * padding.x);
                float needed = Mathf.Ceil(label.GetPreferredValues(label.text, width, Mathf.Infinity).y + 2f * padding.y + 4f);
                LayoutElement element = button.GetComponent<LayoutElement>();
                if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = Mathf.Max(minimum, needed);
                element.flexibleHeight = 0f;
                height += element.preferredHeight;
            }
            optionContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(viewport.rect.height, height));
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionContainer);
            scroll.vertical = height > viewport.rect.height + 0.5f;
            scroll.enabled = interactionEnabled;
            scrollbar.interactable = interactionEnabled;
            measuredSize = viewport.rect.size;
        }

        private void Pick(string id, int sourceGeneration)
        {
            // 表示を起こした入力と選択入力を同一フレームで処理しない。
            // 閉じた選択肢の古いイベントからも再入できない。
            if (!IsOpen || !interactionEnabled || sourceGeneration != generation
                || shownFrame == Time.frameCount) return;
            Action<string> picked = onPick;
            Hide();
            if (audioController != null) audioController.PlaySe("SE_01");
            picked?.Invoke(id);
        }

        public void SetInteractionEnabled(bool value)
        {
            interactionEnabled = value;
            foreach (Button button in buttons) button.interactable = value;
            if (scroll != null) { scroll.StopMovement(); scroll.enabled = value && IsOpen; }
            if (scrollbar != null) scrollbar.interactable = value && IsOpen;
        }

        public void Hide()
        {
            generation++;
            IsOpen = false;
            interactionEnabled = false;
            onPick = null;
            if (scroll != null) scroll.StopMovement();
            foreach (Button button in buttons)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
            buttons.Clear();
            gameObject.SetActive(false);
        }
    }
}

