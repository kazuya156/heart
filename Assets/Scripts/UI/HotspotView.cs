using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 家具/アイテムButtonの見た目だけを担当。取得や画面遷移は呼ばない。
    public sealed class HotspotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private CursorOverlay cursor;
        [SerializeField] private Button button;
        [SerializeField] private GameObject highlightFrame;
        [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 1f, 0.45f);
        [SerializeField, Range(0f, 1f)] private float fallbackBrightness = 0.15f;
        private bool available = true;
        private bool pointerInside;
        private bool capturedColor;
        private Color baseColor;
        public bool IsHighlighted { get; private set; }
        public bool IsConfigured => flow != null && cursor != null && cursor.IsConfigured && button != null && button.targetGraphic != null;

        public void Configure(GameFlowController gameFlow, CursorOverlay overlay, Button target, GameObject frame)
        {
            flow = gameFlow;
            cursor = overlay;
            button = target;
            highlightFrame = frame;
            capturedColor = false;
            available = target.interactable;
            SetHighlight(false);
        }

        public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; RefreshHighlight(); }
        public void OnPointerExit(PointerEventData eventData) { pointerInside = false; SetHighlight(false); }
        private void LateUpdate() { RefreshHighlight(); }

        public void SetAvailable(bool value)
        {
            available = value;
            if (button != null) button.interactable = value;
            RefreshHighlight();
        }

        private bool CanHighlight()
        {
            return IsConfigured && available && isActiveAndEnabled && button.isActiveAndEnabled && button.IsInteractable()
                && flow.CanInteractWithWorld() && cursor.CanShow;
        }

        private void RefreshHighlight()
        {
            bool inside = pointerInside && CanHighlight() && cursor.TryGetPointer(out Vector2 screen)
                && RectTransformUtility.RectangleContainsScreenPoint(button.GetComponent<RectTransform>(), screen, cursor.EventCamera);
            SetHighlight(inside);
        }

        public void SetHighlight(bool value)
        {
            value = value && CanHighlight();
            bool changed = IsHighlighted != value;
            IsHighlighted = value;
            if (highlightFrame != null)
            {
                if (value && changed)
                    foreach (Image edge in highlightFrame.GetComponentsInChildren<Image>(true)) edge.color = highlightColor;
                highlightFrame.SetActive(value);
            }
            else if (button != null && button.targetGraphic != null)
            {
                if (!capturedColor) { baseColor = button.targetGraphic.color; capturedColor = true; }
                button.targetGraphic.color = !value ? baseColor : new Color(Mathf.Min(1f, baseColor.r + fallbackBrightness),
                    Mathf.Min(1f, baseColor.g + fallbackBrightness), Mathf.Min(1f, baseColor.b + fallbackBrightness), baseColor.a);
            }
            // 同じ状態でも通知し、フォーカス復帰や複数ヒット時のEnter/Exit順に依存しない。
            if (cursor != null) cursor.RequestHover(this, value);
        }

        private void OnDisable() { pointerInside = false; SetHighlight(false); }
    }
}
