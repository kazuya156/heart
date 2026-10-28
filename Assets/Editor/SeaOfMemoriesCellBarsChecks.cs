using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesCellBarsChecks
    {
        [MenuItem("Sea of Memories/Checks/Cell Bars")]
        public static void Run()
        {
            if (Application.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
            GameObject root = null;
            Sprite sprite = null;
            Texture2D texture = null;
            try
            {
                root = new GameObject("CellBarsCheckTemporary", typeof(RectTransform));
                var frame = root.GetComponent<RectTransform>(); frame.sizeDelta = new Vector2(2048f, 1143f);
                var catalog = root.AddComponent<ContentCatalog>();
                var background = root.AddComponent<BackgroundView>();
                texture = new Texture2D(128, 72);
                sprite = Sprite.Create(texture, new Rect(0, 0, 128, 72), Vector2.one * 0.5f);
                catalog.ConfigureSprites(new[] { new SpriteCatalogEntry { id = "BG_04", sprite = sprite }, new SpriteCatalogEntry { id = "BG_03", sprite = sprite } });
                Image matte = Child("Matte", frame).gameObject.AddComponent<Image>();
                Image picture = Child("Picture", frame).gameObject.AddComponent<Image>();
                var fallback = Child("Fallback", frame).gameObject.AddComponent<TextMeshProUGUI>();
                background.Configure(catalog, matte, picture, fallback);
                Require(background.SetBackground("BG_04"), "Background fixture failed.");
                RectTransform portrait = Child("Portrait", frame);
                portrait.gameObject.AddComponent<RectMask2D>();
                RectTransform bars = Child("Bars", portrait);
                bars.gameObject.AddComponent<CanvasRenderer>();
                var overlay = bars.gameObject.AddComponent<CellBarsOverlay>();
                overlay.Configure(background, catalog);
                Require(!overlay.raycastTarget && overlay.maskable, "Foreground must be masked and non-interactive.");
                List<UIVertex> mesh = Mesh(overlay);
                Rect rect = overlay.rectTransform.rect;
                Require(Covers(mesh, rect, 780, 200), "A vertical rail failed to occlude the guard.");
                Require(Covers(mesh, rect, 700, 510), "Cross rail is missing.");
                Require(Covers(mesh, rect, 1500, 650), "Lock plate is missing.");
                Require(Covers(mesh, rect, 100, 400), "Wall/frame is missing.");
                Require(!Covers(mesh, rect, 700, 200) && !Covers(mesh, rect, 1000, 600), "An opening incorrectly hides the guard.");
                foreach (UIVertex vertex in mesh)
                {
                    Vector2 normalized = new Vector2((vertex.position.x - rect.xMin) / rect.width,
                        (vertex.position.y - rect.yMin) / rect.height);
                    Require(Vector2.Distance(normalized, new Vector2(vertex.uv0.x, vertex.uv0.y)) < 0.001f,
                        "Foreground texture coordinates do not match the background.");
                }
                Vector3 before = overlay.rectTransform.TransformPoint(rect.min);
                portrait.anchoredPosition += Vector2.right * 8f;
                overlay.Configure(background, catalog);
                Vector3 after = overlay.rectTransform.TransformPoint(overlay.rectTransform.rect.min);
                Require(Vector3.Distance(before, after) < 0.01f, "Bars moved with portrait shake.");
                background.SetBackground("BG_03");
                overlay.Configure(background, catalog);
                Require(Mesh(overlay).Count == 0, "Bars leaked into another background.");
                Debug.Log("[SeaOfMemories] Cell bars checks passed: rails/lock/frame, open gaps, matching UV, non-interactive mask, shake alignment and background switching. Verify the local BG_04 visually in Play mode.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        private static RectTransform Child(string name, RectTransform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static List<UIVertex> Mesh(CellBarsOverlay overlay)
        {
            var result = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                typeof(CellBarsOverlay).GetMethod("OnPopulateMesh",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(overlay, new object[] { vh });
                vh.GetUIVertexStream(result);
            }
            return result;
        }
        private static bool Covers(List<UIVertex> mesh, Rect rect, float x, float y)
        {
            Vector2 point = rect.min + Vector2.Scale(rect.size, new Vector2(x / 2048f, 1f - y / 1143f));
            for (int i = 0; i < mesh.Count; i += 3)
            {
                float a = Cross(mesh[i].position, mesh[i + 1].position, point);
                float b = Cross(mesh[i + 1].position, mesh[i + 2].position, point);
                float c = Cross(mesh[i + 2].position, mesh[i].position, point);
                if ((a >= 0f && b >= 0f && c >= 0f) || (a <= 0f && b <= 0f && c <= 0f)) return true;
            }
            return false;
        }
        private static float Cross(Vector2 a, Vector2 b, Vector2 p) { return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x); }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Cell bars check: " + message);
        }
    }
}
