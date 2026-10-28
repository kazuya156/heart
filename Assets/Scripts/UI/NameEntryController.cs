using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // Built under the existing Canvas at runtime; no scene or production assets are rewritten.
    public sealed class NameEntryController : MonoBehaviour
    {
        private TMP_InputField field;
        private Button confirm;
        private TMP_Text hint;
        private Func<string, bool> onConfirm;
        private bool inputReady;
        public bool CanConfirm => inputReady && isActiveAndEnabled;

        public static NameEntryController Create(Canvas canvas, TMP_FontAsset font, Func<string, bool> accepted)
        {
            RectTransform root = Rect("NameEntryPanel", canvas.transform, Vector2.zero, Vector2.one);
            root.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.055f, 0.07f, 0.96f);
            var controller = root.gameObject.AddComponent<NameEntryController>();
            controller.onConfirm = accepted;
            RectTransform card = Rect("NameCard", root, new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.82f));
            card.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.14f, 0.17f, 1f);
            Label("Heading", card, font, "あなたの名前を教えてください", 34f, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.93f));
            RectTransform input = Rect("NameInput", card, new Vector2(0.10f, 0.43f), new Vector2(0.90f, 0.65f));
            Image inputImage = input.gameObject.AddComponent<Image>();
            inputImage.color = new Color(0.015f, 0.065f, 0.08f, 1f);
            controller.field = input.gameObject.AddComponent<TMP_InputField>();
            controller.field.targetGraphic = inputImage;
            RectTransform viewport = Rect("Viewport", input, Vector2.zero, Vector2.one);
            viewport.offsetMin = new Vector2(18f, 6f); viewport.offsetMax = new Vector2(-18f, -6f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text text = Label("Text", viewport, font, "", 32f, Vector2.zero, Vector2.one);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text placeholder = Label("Placeholder", viewport, font, "名前を入力", 32f, Vector2.zero, Vector2.one);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(0.55f, 0.68f, 0.72f, 1f);
            controller.field.textViewport = viewport;
            controller.field.textComponent = (TextMeshProUGUI)text;
            controller.field.placeholder = placeholder;
            controller.field.lineType = TMP_InputField.LineType.SingleLine;
            controller.field.contentType = TMP_InputField.ContentType.Standard;
            controller.field.richText = false;
            controller.field.characterLimit = 0; // Count user-perceived characters in GameSession, not UTF-16 units.
            controller.field.customCaretColor = true;
            controller.field.caretColor = Color.white;
            controller.hint = Label("Hint", card, font, "1〜12文字で入力してください", 23f, new Vector2(0.06f, 0.29f), new Vector2(0.94f, 0.41f));
            RectTransform button = Rect("ConfirmName", card, new Vector2(0.31f, 0.07f), new Vector2(0.69f, 0.25f));
            Image buttonImage = button.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.14f, 0.43f, 0.47f, 1f);
            controller.confirm = button.gameObject.AddComponent<Button>();
            controller.confirm.targetGraphic = buttonImage;
            Label("Label", button, font, "この名前で始める", 26f, Vector2.zero, Vector2.one);
            controller.field.onValueChanged.AddListener(_ => controller.RefreshValidation());
            controller.confirm.onClick.AddListener(controller.Submit);
            // Do not bind onSubmit: Enter may still be confirming Japanese IME composition.
            controller.ResetInput();
            return controller;
        }

        public void ResetInput()
        {
            inputReady = false;
            field.SetTextWithoutNotify("");
            field.interactable = false;
            RefreshValidation();
        }

        public void EnableInput()
        {
            if (!isActiveAndEnabled) return;
            inputReady = true;
            field.interactable = true;
            RefreshValidation();
            field.Select();
            field.ActivateInputField();
        }

        public void Hide()
        {
            inputReady = false;
            field.DeactivateInputField();
            gameObject.SetActive(false);
        }

        private void Submit()
        {
            if (!CanConfirm || !GameSession.IsValidPlayerName(field.text)) return;
            onConfirm?.Invoke(field.text);
        }

        private void RefreshValidation()
        {
            bool valid = GameSession.IsValidPlayerName(field.text);
            confirm.interactable = inputReady && valid;
            bool empty = string.IsNullOrWhiteSpace(field.text);
            hint.text = !empty && !valid ? "改行を使わず、1〜12文字で入力してください" : "1〜12文字で入力してください";
            hint.color = !empty && !valid ? new Color(1f, 0.72f, 0.35f) : new Color(0.73f, 0.85f, 0.87f);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value, float size, Vector2 min, Vector2 max)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = Color.white;
            text.richText = false;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
    }
}
