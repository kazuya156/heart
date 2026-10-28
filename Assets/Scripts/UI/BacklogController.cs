using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SeaOfMemories
{
    public sealed class BacklogController : MonoBehaviour
    {
        private GameFlowController flow;
        private DialoguePlayer dialogue;
        private UiInputRouter input;
        private Button launcher;
        private Button closeButton;
        private GameObject modal;
        private ScrollRect scroll;
        private TMP_Text body;
        private RectTransform viewport;
        private RectTransform content;
        private bool wasPaused;
        private Vector2 measuredSize;
        public bool IsOpen { get; private set; }

        public static BacklogController Create(Canvas canvas, GameFlowController flow, DialoguePlayer dialogue, UiInputRouter input)
        {
            RectTransform root = Rect("ConversationBacklog", canvas.transform, Vector2.zero, Vector2.one);
            var result = root.gameObject.AddComponent<BacklogController>();
            result.flow = flow; result.dialogue = dialogue; result.input = input;
            TMP_FontAsset font = dialogue.DialogueFont;
            result.launcher = MakeButton("OpenBacklog", root, font, "履歴", new Vector2(0.67f, 0.92f), new Vector2(0.79f, 0.98f));
            result.launcher.onClick.AddListener(result.Open);
            RectTransform modal = Rect("BacklogModal", root, Vector2.zero, Vector2.one);
            modal.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.04f, 0.055f, 0.98f);
            result.modal = modal.gameObject;
            Label("Heading", modal, font, "会話履歴", 32f, new Vector2(0.06f, 0.86f), new Vector2(0.70f, 0.96f));
            result.closeButton = MakeButton("CloseBacklog", modal, font, "閉じる", new Vector2(0.77f, 0.87f), new Vector2(0.94f, 0.96f));
            result.closeButton.onClick.AddListener(result.Close);
            Label("Hint", modal, font, "ホイール・右のバーでスクロール ／ Escで閉じる", 20f,
                new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.10f));
            RectTransform scrollRoot = Rect("Scroll", modal, new Vector2(0.06f, 0.14f), new Vector2(0.92f, 0.83f));
            result.scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            result.viewport = Rect("Viewport", scrollRoot, Vector2.zero, Vector2.one);
            result.viewport.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.08f, 0.10f, 1f);
            result.viewport.gameObject.AddComponent<RectMask2D>();
            result.content = Rect("Content", result.viewport, new Vector2(0f, 1f), Vector2.one);
            result.content.pivot = new Vector2(0.5f, 1f);
            result.body = Label("HistoryText", result.content, font, "", 27f, Vector2.zero, Vector2.one);
            result.body.alignment = TextAlignmentOptions.TopLeft;
            result.body.margin = new Vector4(18f, 16f, 18f, 16f);
            result.scroll.viewport = result.viewport;
            result.scroll.content = result.content;
            result.scroll.horizontal = false;
            result.scroll.vertical = true;
            result.scroll.inertia = false;
            result.scroll.scrollSensitivity = 42f;
            result.scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform track = Rect("Scrollbar", modal, new Vector2(0.93f, 0.14f), new Vector2(0.95f, 0.83f));
            track.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.20f, 0.23f, 1f);
            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
            RectTransform handle = Rect("Handle", track, Vector2.zero, Vector2.one);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.40f, 0.74f, 0.77f, 1f);
            bar.handleRect = handle; bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            result.scroll.verticalScrollbar = bar;
            result.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            modal.gameObject.SetActive(false);
            result.launcher.gameObject.SetActive(false);
            return result;
        }

        private void Update()
        {
            if (flow == null) return;
            launcher.gameObject.SetActive(!IsOpen && flow.CanOpenBacklog);
            if (!IsOpen) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
#endif
            if (measuredSize != viewport.rect.size) LayoutText(false);
        }

        public void Open()
        {
            if (IsOpen || !flow.CanOpenBacklog || input.IsConsumedThisFrame) return;
            input.SuppressForCurrentFrame();
            dialogue.CaptureBacklog();
            wasPaused = dialogue.IsPaused;
            dialogue.Pause();
            IsOpen = true;
            modal.SetActive(true);
            transform.SetAsLastSibling();
            launcher.gameObject.SetActive(false);
            body.text = dialogue.History.Format();
            Canvas.ForceUpdateCanvases();
            LayoutText(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            flow.RefreshInteraction();
        }

        public void Close()
        {
            if (!IsOpen) return;
            input.SuppressForCurrentFrame();
            scroll.StopMovement();
            IsOpen = false;
            modal.SetActive(false);
            if (!wasPaused) dialogue.Resume();
            // Do not restore an old selected world button under the closing click/Enter.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            flow.RefreshInteraction();
        }

        private void LayoutText(bool newest)
        {
            float position = scroll.verticalNormalizedPosition;
            measuredSize = viewport.rect.size;
            float width = Mathf.Max(1f, measuredSize.x);
            float height = body.GetPreferredValues(body.text, width, Mathf.Infinity).y + 32f;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(measuredSize.y, height));
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = newest ? 0f : position;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value, float size, Vector2 min, Vector2 max)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Color.white;
            text.richText = false; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            return text;
        }
        private static Button MakeButton(string name, Transform parent, TMP_FontAsset font, string label, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.09f, 0.29f, 0.33f, 1f);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", rect, font, label, 23f, Vector2.zero, Vector2.one);
            return button;
        }
    }
}
