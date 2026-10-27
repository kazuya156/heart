using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SeaOfMemories
{
    // 最後に描画位置と大きさを反映し、Raycastを遮らないUIカーソル。
    [DefaultExecutionOrder(1000)]
    public sealed class CursorOverlay : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image cursorImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField, Min(1f)] private float normalSize = 64f;
        [SerializeField, Min(1f)] private float hoverSize = 84f;
        [SerializeField] private Vector2 pointerPivot = new Vector2(50f / 128f, 78f / 128f);
        private readonly HashSet<HotspotView> hoverSources = new HashSet<HotspotView>();
        private bool visibleRequested = true;
        private bool hovered;
        private bool manualHover;
        private bool paused;
        private bool ownsOsCursor;
        private bool savedOsVisibility;
        private Texture2D placeholderTexture;
        private Sprite placeholderSprite;
        public bool IsConfigured => canvas != null && cursorImage != null && cursorImage.rectTransform.parent == canvas.transform;
        public Camera EventCamera => canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        public bool CanShow => Application.isPlaying && isActiveAndEnabled && visibleRequested && !paused
            && Application.isFocused && IsConfigured && TryGetPointer(out _);

        public void Configure(Canvas gameCanvas, Image image)
        {
            canvas = gameCanvas;
            cursorImage = image;
            cursorImage.raycastTarget = false;
            cursorImage.enabled = false;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (IsConfigured) { EnsureSprite(); ApplyAppearance(); }
        }

        private void LateUpdate()
        {
            if (!CanShow || !TryGetPointer(out Vector2 screen)) { SuspendDisplay(); return; }
            var rect = canvas.GetComponent<RectTransform>();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, EventCamera, out Vector2 local))
            { SuspendDisplay(); return; }
            EnsureSprite();
            hoverSources.RemoveWhere(source => source == null || !source.isActiveAndEnabled || !source.IsHighlighted);
            RefreshHover();
            cursorImage.rectTransform.localPosition = new Vector3(local.x, local.y, 0f);
            cursorImage.transform.SetAsLastSibling();
            cursorImage.raycastTarget = false;
            cursorImage.enabled = true;
            if (!ownsOsCursor)
            {
                savedOsVisibility = Cursor.visible;
                ownsOsCursor = true;
            }
            Cursor.visible = false;
        }

        public bool TryGetPointer(out Vector2 screen)
        {
            screen = default;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null || canvas == null) return false;
            screen = Mouse.current.position.ReadValue();
            if (screen.x < 0f || screen.y < 0f || screen.x >= Screen.width || screen.y >= Screen.height) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(canvas.GetComponent<RectTransform>(), screen, EventCamera);
#else
            return false;
#endif
        }

        public void SetHover(bool isHover)
        {
            manualHover = isHover;
            RefreshHover();
        }

        internal void RequestHover(HotspotView source, bool value)
        {
            if (value) hoverSources.Add(source);
            else hoverSources.Remove(source);
            RefreshHover();
        }

        private void RefreshHover()
        {
            hovered = manualHover || hoverSources.Count != 0;
            ApplyAppearance();
        }

        public void SetVisible(bool visible)
        {
            visibleRequested = visible;
            if (!visible)
            {
                hoverSources.Clear();
                SetHover(false);
                SuspendDisplay();
            }
        }

        private void ApplyAppearance()
        {
            if (cursorImage == null) return;
            Sprite normal = normalSprite != null ? normalSprite : placeholderSprite;
            cursorImage.sprite = hovered && hoverSprite != null ? hoverSprite : normal;
            cursorImage.rectTransform.pivot = pointerPivot;
            float size = Mathf.Max(1f, hovered ? hoverSize : normalSize);
            cursorImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            cursorImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        }

        private void SuspendDisplay()
        {
            if (cursorImage != null) cursorImage.enabled = false;
            if (ownsOsCursor)
            {
                Cursor.visible = savedOsVisibility;
                ownsOsCursor = false;
            }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) SuspendDisplay(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) SuspendDisplay(); }
        private void OnDisable() { SuspendDisplay(); }
        private void OnApplicationQuit() { SuspendDisplay(); }

        private void OnDestroy()
        {
            SuspendDisplay();
            if (cursorImage != null && cursorImage.sprite == placeholderSprite) cursorImage.sprite = null;
            if (Application.isPlaying)
            {
                if (placeholderSprite != null) Destroy(placeholderSprite);
                if (placeholderTexture != null) Destroy(placeholderTexture);
            }
            else
            {
                if (placeholderSprite != null) DestroyImmediate(placeholderSprite);
                if (placeholderTexture != null) DestroyImmediate(placeholderTexture);
            }
        }

        private void EnsureSprite()
        {
            if (normalSprite != null || placeholderSprite != null) return;
            // 本番素材未受領のため、白い虫眼鏡と黒い縁を一時的なSpriteとして作る。
            const int side = 128;
            var pixels = new Color[side * side];
            Vector2 lens = new Vector2(50f, 78f), start = new Vector2(70f, 57f), end = new Vector2(108f, 19f);
            Vector2 segment = end - start;
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float ring = Mathf.Abs(Vector2.Distance(point, lens) - 27f);
                    float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
                    float handle = Vector2.Distance(point, start + t * segment);
                    float alpha = Mathf.Clamp01(0.5f - Mathf.Min(ring - 7f, handle - 7f));
                    float white = Mathf.Clamp01(0.5f - Mathf.Min(ring - 3f, handle - 3.5f));
                    Color color = Color.Lerp(Color.black, Color.white, white);
                    color.a = alpha;
                    pixels[y * side + x] = color;
                }
            placeholderTexture = new Texture2D(side, side, TextureFormat.RGBA32, false)
            { name = "TemporaryMagnifier", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            placeholderTexture.SetPixels(pixels);
            placeholderTexture.Apply(false, false);
            placeholderSprite = Sprite.Create(placeholderTexture, new Rect(0f, 0f, side, side),
                new Vector2(0.5f, 0.5f), side, 0, SpriteMeshType.FullRect);
            placeholderSprite.name = "TemporaryMagnifier";
            placeholderSprite.hideFlags = HideFlags.DontSave;
            ApplyAppearance();
        }
    }
}
