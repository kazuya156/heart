using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // Redraw only the BG_04 foreground geometry using the existing local sprite.
    // Native coordinates describe the supplied 2048 x 1143 drawing; no image is copied or generated.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CellBarsOverlay : MaskableGraphic
    {
        private BackgroundView background;
        private ContentCatalog catalog;
        private Sprite source;
        public override Texture mainTexture => source != null ? source.texture : Texture2D.whiteTexture;

        public void Configure(BackgroundView view, ContentCatalog content)
        {
            background = view; catalog = content;
            raycastTarget = false;
            maskable = true;
            Refresh();
        }

        protected override void OnEnable() { base.OnEnable(); Refresh(); }
        private void LateUpdate() { Refresh(); }
        private void Refresh()
        {
            Sprite next = background != null && background.CurrentBackgroundId == "BG_04" && catalog != null
                ? catalog.GetSprite("BG_04") : null;
            if (source != next) { source = next; SetMaterialDirty(); SetVerticesDirty(); }
            if (source != null) background.TryPositionHotspot(rectTransform, new Rect(0f, 0f, 1f, 1f));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (source == null) return;
            // Dark outlines are included, so the actor cannot bleed around the rail edges.
            float[] y = { 0f, 200f, 450f, 550f, 700f, 820f, 1100f, 1143f };
            Strip(vh, y, new float[] { 592, 601, 614, 619, 626, 630, 645, 647 }, new float[] { 629, 638, 650, 654, 660, 663, 675, 678 });
            Strip(vh, y, new float[] { 754, 760, 768, 771, 775, 780, 789, 791 }, new float[] { 795, 800, 802, 805, 809, 814, 822, 824 });
            Strip(vh, y, new float[] { 910, 914, 917, 918, 922, 925, 930, 931 }, new float[] { 950, 952, 953, 955, 959, 962, 966, 968 });
            Strip(vh, y, new float[] { 1079, 1081, 1081, 1082, 1080, 1077, 1074, 1074 }, new float[] { 1121, 1121, 1121, 1119, 1116, 1117, 1113, 1113 });
            Strip(vh, y, new float[] { 1248, 1251, 1252, 1253, 1254, 1255, 1258, 1259 }, new float[] { 1289, 1290, 1290, 1291, 1292, 1292, 1294, 1294 });
            Strip(vh, y, new float[] { 1428, 1422, 1416, 1414, 1408, 1403, 1392, 1390 }, new float[] { 1471, 1464, 1457, 1455, 1445, 1442, 1433, 1431 });
            // Side walls/frame, cross rails and the lock plate are also in front of the guard.
            float[] frameY = { 0, 496, 530, 750, 792, 1143 };
            Strip(vh, frameY, new float[] { 0, 0, 0, 0, 0, 0 }, new float[] { 451, 488, 492, 505, 509, 529 });
            Strip(vh, frameY, new float[] { 1590, 1582, 1581, 1577, 1575, 1551 }, new float[] { 2048, 2048, 2048, 2048, 2048, 2048 });
            Quad(vh, new Vector2(476, 494), new Vector2(1596, 492), new Vector2(1595, 532), new Vector2(480, 528));
            Quad(vh, new Vector2(490, 746), new Vector2(1591, 757), new Vector2(1588, 795), new Vector2(494, 788));
            Quad(vh, new Vector2(1409, 525), new Vector2(1596, 528), new Vector2(1592, 770), new Vector2(1408, 768));
        }

        private void Strip(VertexHelper vh, float[] y, float[] left, float[] right)
        {
            for (int i = 0; i < y.Length - 1; i++)
                Quad(vh, new Vector2(left[i], y[i]), new Vector2(right[i], y[i]),
                    new Vector2(right[i + 1], y[i + 1]), new Vector2(left[i + 1], y[i + 1]));
        }

        private void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int start = vh.currentVertCount;
            Vertex(vh, a); Vertex(vh, b); Vertex(vh, c); Vertex(vh, d);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private void Vertex(VertexHelper vh, Vector2 pixel)
        {
            Vector2 normalized = new Vector2(pixel.x / 2048f, 1f - pixel.y / 1143f);
            Rect rect = rectTransform.rect;
            Vector4 uv = DataUtility.GetOuterUV(source);
            var vertex = UIVertex.simpleVert;
            vertex.position = rect.min + Vector2.Scale(rect.size, normalized);
            vertex.uv0 = new Vector2(Mathf.Lerp(uv.x, uv.z, normalized.x), Mathf.Lerp(uv.y, uv.w, normalized.y));
            vertex.color = color;
            vh.AddVert(vertex);
        }
    }
}
