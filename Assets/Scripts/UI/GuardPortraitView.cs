using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    public sealed class GuardPortraitView : MonoBehaviour
    {
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private RectTransform portraitRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text placeholder;
        [SerializeField, Min(0f)] private float shakeSeconds = 0.18f;
        [SerializeField, Min(0f)] private float shakePixels = 8f;
        private Vector2 restPosition;
        private float elapsed, duration, amplitude;
        public string CurrentExpression { get; private set; } = "CH_01_normal";
        public bool IsShaking { get; private set; }
        public bool IsConfigured => catalog != null && portraitRoot != null && portraitImage != null && placeholder != null;
        public float ShakeSeconds => shakeSeconds;
        public float ShakePixels => shakePixels;

        public void Configure(ContentCatalog content, RectTransform root, Image image, TMP_Text fallback)
        {
            catalog = content;
            portraitRoot = root;
            portraitImage = image;
            placeholder = fallback;
        }

        public void SetExpression(string id)
        {
            if (string.IsNullOrEmpty(id) || !IsConfigured) return;
            id = ContentCatalog.NormalizeExpressionId(id);
            if (!catalog.HasSpriteId(id))
            {
                Debug.LogError("[SeaOfMemories] Unknown portrait expression: " + id, this);
                return;
            }
            CurrentExpression = id;
            Sprite sprite = catalog.GetSprite(id);
            portraitImage.sprite = sprite;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            placeholder.gameObject.SetActive(sprite == null);
            if (sprite != null) { portraitImage.color = Color.white; return; }
            string label = "NORMAL";
            Color tint = new Color(0.06f, 0.14f, 0.18f);
            switch (id)
            {
                case "shaken1": label = "SHAKEN 1"; tint = new Color(0.18f, 0.17f, 0.20f); break;
                case "shaken2": label = "SHAKEN 2"; tint = new Color(0.25f, 0.13f, 0.17f); break;
                case "limit": label = "LIMIT"; tint = new Color(0.32f, 0.10f, 0.13f); break;
                case "smile": label = "SMILE"; tint = new Color(0.10f, 0.23f, 0.17f); break;
            }
            portraitImage.color = tint;
            placeholder.text = "GUARD\n[IMAGE PLACEHOLDER]\n" + label;
        }

        public void Shake(float seconds, float pixels)
        {
            StopShake();
            if (!IsConfigured || !isActiveAndEnabled || !portraitRoot.gameObject.activeInHierarchy
                || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || float.IsNaN(pixels) || float.IsInfinity(pixels)) return;
            restPosition = portraitRoot.anchoredPosition;
            duration = seconds;
            amplitude = Mathf.Max(0f, pixels);
            elapsed = 0f;
            IsShaking = true;
        }

        private void Update()
        {
            if (!IsShaking) return;
            if (portraitRoot == null || !portraitRoot.gameObject.activeInHierarchy) { StopShake(); return; }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= duration) { StopShake(); return; }
            float t = elapsed / duration;
            float offset = Mathf.Sin(t * 6f * Mathf.PI) * amplitude * (1f - t);
            portraitRoot.anchoredPosition = restPosition + Vector2.right * offset;
        }

        public void StopShake()
        {
            if (IsShaking && portraitRoot != null) portraitRoot.anchoredPosition = restPosition;
            IsShaking = false;
        }

        public void ResetView()
        {
            StopShake();
            SetExpression("CH_01_normal");
        }

        private void OnDisable() { StopShake(); }
    }
}
