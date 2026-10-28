using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesArtworkHotspotChecks
    {
        [MenuItem("Sea of Memories/Checks/Artwork Hotspots")]
        public static void Run()
        {
            GameObject root = null;
            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                if (Application.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
                root = new GameObject("HotspotCheckTemporary", typeof(RectTransform));
                RectTransform frame = root.GetComponent<RectTransform>();
                var catalog = root.AddComponent<ContentCatalog>();
                var view = root.AddComponent<BackgroundView>();
                Image matte = ImageAt("Matte", frame);
                Image picture = ImageAt("Picture", frame);
                var labelObject = new GameObject("Fallback", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(frame, false);
                view.Configure(catalog, matte, picture, labelObject.GetComponent<TextMeshProUGUI>());
                texture = new Texture2D(160, 90);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 160f, 90f), new Vector2(0.5f, 0.5f));
                picture.sprite = sprite;
                picture.enabled = true;
                var parentObject = new GameObject("NarrowUiParent", typeof(RectTransform));
                RectTransform parent = parentObject.GetComponent<RectTransform>();
                parent.SetParent(frame, false);
                parent.anchorMin = Vector2.zero;
                parent.anchorMax = new Vector2(0.8f, 1f);
                parent.offsetMin = parent.offsetMax = Vector2.zero;
                Image target = ImageAt("ClickTarget", parent);
                Rect area = new Rect(0.25f, 0.3f, 0.15f, 0.2f);
                int cases = 0;
                foreach (Vector2 size in new[] { new Vector2(1280f, 720f), new Vector2(1024f, 768f), new Vector2(2100f, 900f) })
                {
                    frame.sizeDelta = size;
                    frame.ForceUpdateRectTransforms();
                    if (!view.TryPositionHotspot(target.rectTransform, area)) throw new Exception("Placement failed.");
                    // Expected center/corners from an independent 16:9 contain calculation.
                    float width = Mathf.Min(size.x, size.y * 16f / 9f);
                    float height = width * 9f / 16f;
                    Vector2 origin = new Vector2(-width / 2f, -height / 2f);
                    Vector3[] corners = new Vector3[4];
                    target.rectTransform.GetWorldCorners(corners);
                    Vector3 low = frame.InverseTransformPoint(corners[0]);
                    Vector3 high = frame.InverseTransformPoint(corners[2]);
                    Vector2 expectedLow = origin + new Vector2(width * area.xMin, height * area.yMin);
                    Vector2 expectedHigh = origin + new Vector2(width * area.xMax, height * area.yMax);
                    if (Vector2.Distance(new Vector2(low.x, low.y), expectedLow) > 0.1f
                        || Vector2.Distance(new Vector2(high.x, high.y), expectedHigh) > 0.1f)
                        throw new Exception("Artwork/UI-parent alignment failed: " + size);
                    cases++;
                }
                Debug.Log("[SeaOfMemories] Artwork hotspot checks passed: " + cases + " aspect ratios, including narrow UI parent and letterboxing. Verify object alignment visually in MainScene.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        private static Image ImageAt(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go.GetComponent<Image>();
        }
    }
}
