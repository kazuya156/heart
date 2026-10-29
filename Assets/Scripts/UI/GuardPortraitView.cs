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
        private BackgroundView stageBackground;
        private CellBarsOverlay bars;
        private bool narrativeStage;
        private bool narrativeVisible;
        private bool explicitVisibility;
        public void SetVisible(bool visible)
        {
            explicitVisibility = true;
            narrativeVisible = visible;
            if (!visible && portraitRoot != null) { StopShake(); portraitRoot.gameObject.SetActive(false); }
            else if (narrativeStage && portraitRoot != null && stageBackground != null)
                portraitRoot.gameObject.SetActive(visible && stageBackground.CurrentBackgroundId == "BG_04");
        }
        private Vector2 restPosition;

        private RectTransform sceneLayer;
        public void ApplyStage(GamePhase phase, Transform conversation)
        {
            if (!IsConfigured || !Application.isPlaying || conversation == null) return;
            EnsureBarsOverlay();
            if (sceneLayer == null)
            {
                var reference = conversation as RectTransform;
                var canvas = conversation.GetComponentInParent<Canvas>();
                if (reference == null || canvas == null) return;
                sceneLayer = new GameObject("GuardSceneLayer", typeof(RectTransform)).GetComponent<RectTransform>();
                sceneLayer.SetParent(canvas.transform, false);
                sceneLayer.pivot = reference.pivot;
                sceneLayer.anchorMin = reference.anchorMin; sceneLayer.anchorMax = reference.anchorMax;
                sceneLayer.offsetMin = reference.offsetMin; sceneLayer.offsetMax = reference.offsetMax;
                Transform background = canvas.transform.Find("BackgroundImage");
                sceneLayer.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 1);
            }
            bool questioning = phase == GamePhase.QuestionSelection || phase == GamePhase.QuestionPlayback;
            bool visiblePhase = questioning || phase == GamePhase.Intro || phase == GamePhase.GuardReturn
                || phase == GamePhase.InterrogationIntro || phase == GamePhase.BadEnding || phase == GamePhase.TrueEnding;
            if (portraitRoot.parent != sceneLayer)
            {
                StopShake();
                portraitRoot.SetParent(sceneLayer, false);
                portraitRoot.pivot = new Vector2(0.5f, 0.5f);
                ApplyNarrativeLayout();
            }
            narrativeStage = visiblePhase;
            if (questioning) narrativeVisible = true;
            if (!visiblePhase) narrativeVisible = false;
            portraitRoot.gameObject.SetActive(visiblePhase && narrativeVisible
                && stageBackground != null && stageBackground.CurrentBackgroundId == "BG_04");
            portraitImage.raycastTarget = false;
            placeholder.raycastTarget = false;
        }

        private void ApplyNarrativeLayout()
        {
            // Narrative portraits: align the face between the second and third BG_04 rails.
            // DialoguePanel occupies the left 80% of the Canvas; this shifts about 3.6% of the image width.
            float shift = 0.045f;
            portraitRoot.anchorMin = new Vector2(0.18f + shift, 0.24f);
            portraitRoot.anchorMax = new Vector2(0.76f + shift, 0.94f);
            portraitRoot.offsetMin = portraitRoot.offsetMax = Vector2.zero;
        }

        private void EnsureBarsOverlay()
        {
            if (stageBackground == null) stageBackground = GetComponent<BackgroundView>();
            if (bars != null || stageBackground == null) return;
            // Clip the foreground redraw to the portrait area; it must never cover other UI.
            if (portraitRoot.GetComponent<RectMask2D>() == null) portraitRoot.gameObject.AddComponent<RectMask2D>();
            var layer = new GameObject("CellBarsForeground", typeof(RectTransform), typeof(CanvasRenderer), typeof(CellBarsOverlay));
            layer.transform.SetParent(portraitRoot, false);
            bars = layer.GetComponent<CellBarsOverlay>();
            bars.Configure(stageBackground, catalog);
        }

        private void LateUpdate()
        {
            // The first offscreen guard line precedes BG_04 in the authored intro.
            if (narrativeStage && stageBackground != null && portraitRoot != null)
                portraitRoot.gameObject.SetActive(narrativeVisible && stageBackground.CurrentBackgroundId == "BG_04");
        }

        public void HideNarrative()
        {
            narrativeVisible = false;
            if (narrativeStage && portraitRoot != null) { StopShake(); portraitRoot.gameObject.SetActive(false); }
        }

        public void ShowSpeakingGuard()
        {
            if (!explicitVisibility && narrativeStage && !narrativeVisible) SetExpression("GUARD_02");
        }
        private float elapsed, duration, amplitude;
        public string CurrentExpression { get; private set; } = "GUARD_02";
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
            if (!explicitVisibility) narrativeVisible = true;
            if (narrativeStage)
            {
                StopShake();
                ApplyNarrativeLayout();
                portraitRoot.gameObject.SetActive(narrativeVisible);
            }
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
            explicitVisibility = false;
            StopShake();
            SetExpression("GUARD_02");
            HideNarrative();
        }

        private void OnDisable() { StopShake(); }
    }
}
