#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace BlockBlast.Editor.UI
{
    internal sealed class BlockPuzzleHDRaster
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Color[] _pixels;

        public BlockPuzzleHDRaster(int width, int height)
        {
            _width = width;
            _height = height;
            _pixels = new Color[width * height];
        }

        public int Width => _width;
        public int Height => _height;
        public float Unit => Mathf.Min(_width, _height) / 512f;
        public Vector2 Center => new Vector2(_width * 0.5f, _height * 0.5f);

        public void RoundedRect(Rect rect, float radius, Color bottom, Color top, float border, Color borderColor, float opacity = 1f)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(rect.xMin - 2f));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(rect.xMax + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(rect.yMin - 2f));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(rect.yMax + 2f));
            Vector2 center = rect.center;
            Vector2 half = rect.size * 0.5f;

            for (int y = minY; y <= maxY; y++)
            {
                float gradient = Mathf.InverseLerp(rect.yMin, rect.yMax, y);
                Color fill = Color.Lerp(bottom, top, gradient);
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = RoundedBoxDistance(new Vector2(x + 0.5f, y + 0.5f) - center, half, radius);
                    float coverage = Mathf.Clamp01(0.5f - distance) * opacity;
                    if (coverage <= 0f) continue;
                    float borderMix = border > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-border - 1f, -border + 1f, distance)) : 0f;
                    Blend(x, y, Color.Lerp(fill, borderColor, borderMix), coverage);
                }
            }
        }

        public void BeveledButton(Color face, Color edge, Color cream, bool pressed, bool circular = false)
        {
            float min = Mathf.Min(_width, _height);
            float padX = _width * (circular ? 0.12f : 0.04f);
            float padY = _height * 0.10f;
            float depth = _height * (pressed ? 0.065f : 0.13f);
            float radius = circular ? min * 0.38f : _height * 0.30f;
            Rect baseRect = new Rect(padX, padY, _width - padX * 2f, _height - padY * 2f - depth * 0.25f);
            RoundedRect(baseRect, radius, Darken(edge, 0.76f), edge, 0f, edge);
            Rect faceRect = new Rect(padX, padY + depth, _width - padX * 2f, _height - padY * 2f - depth);
            RoundedRect(faceRect, radius, Darken(face, pressed ? 0.82f : 0.94f), Lighten(face, pressed ? 0.06f : 0.18f), min * 0.025f, cream);
            Rect inset = Inset(faceRect, min * 0.075f, min * 0.065f);
            RoundedRect(inset, Mathf.Max(8f, radius - min * 0.07f), Darken(face, 0.96f), Lighten(face, 0.08f), 0f, face);
        }

        public void BeveledPanel(Color face, Color edge, Color rim, bool dark, float opacity = 1f)
        {
            float min = Mathf.Min(_width, _height);
            float pad = min * 0.055f;
            float depth = min * 0.055f;
            float radius = min * 0.14f;
            Rect baseRect = new Rect(pad, pad, _width - pad * 2f, _height - pad * 2f - depth * 0.1f);
            RoundedRect(baseRect, radius, Darken(edge, 0.72f), edge, 0f, edge, opacity);
            Rect faceRect = new Rect(pad, pad + depth, _width - pad * 2f, _height - pad * 2f - depth);
            RoundedRect(faceRect, radius, Darken(face, dark ? 0.84f : 0.97f), Lighten(face, dark ? 0.07f : 0.10f), min * 0.025f, rim, opacity);
            Rect quiet = Inset(faceRect, min * 0.11f, min * 0.11f);
            RoundedRect(quiet, radius * 0.55f, face, Lighten(face, dark ? 0.025f : 0.04f), 0f, face, opacity);
        }

        public void Circle(Vector2 center, float radius, Color color, float border = 0f, Color borderColor = default)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 2f));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(center.x + radius + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 2f));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(center.y + radius + 2f));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) - radius;
                    float coverage = Mathf.Clamp01(0.5f - d);
                    if (coverage <= 0f) continue;
                    float borderMix = border > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-border - 1f, -border + 1f, d)) : 0f;
                    Blend(x, y, Color.Lerp(color, borderColor, borderMix), coverage);
                }
            }
        }

        public void Ring(Vector2 center, float radius, float thickness, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 2f));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(center.x + radius + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 2f));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(center.y + radius + 2f));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Mathf.Abs(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) - radius) - thickness * 0.5f;
                    float coverage = Mathf.Clamp01(0.5f - d);
                    if (coverage > 0f) Blend(x, y, color, coverage);
                }
            }
        }

        public void Line(Vector2 from, Vector2 to, float thickness, Color color)
        {
            float radius = thickness * 0.5f;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - radius - 2f));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(Mathf.Max(from.x, to.x) + radius + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - radius - 2f));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(Mathf.Max(from.y, to.y) + radius + 2f));
            Vector2 delta = to - from;
            float lengthSq = Mathf.Max(0.0001f, delta.sqrMagnitude);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - from, delta) / lengthSq);
                    float d = Vector2.Distance(p, from + delta * t) - radius;
                    float coverage = Mathf.Clamp01(0.5f - d);
                    if (coverage > 0f) Blend(x, y, color, coverage);
                }
            }
        }

        public void Polygon(Vector2[] points, Color color)
        {
            float minXf = _width, minYf = _height, maxXf = 0f, maxYf = 0f;
            foreach (Vector2 p in points)
            {
                minXf = Mathf.Min(minXf, p.x); minYf = Mathf.Min(minYf, p.y);
                maxXf = Mathf.Max(maxXf, p.x); maxYf = Mathf.Max(maxYf, p.y);
            }
            int minX = Mathf.Max(0, Mathf.FloorToInt(minXf));
            int maxX = Mathf.Min(_width - 1, Mathf.CeilToInt(maxXf));
            int minY = Mathf.Max(0, Mathf.FloorToInt(minYf));
            int maxY = Mathf.Min(_height - 1, Mathf.CeilToInt(maxYf));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (InsidePolygon(new Vector2(x + 0.5f, y + 0.5f), points)) Blend(x, y, color, 1f);
        }

        public void Star(Vector2 center, float outer, float inner, int points, Color color, float rotation = 0f)
        {
            var polygon = new Vector2[points * 2];
            for (int i = 0; i < polygon.Length; i++)
            {
                float angle = rotation + i * Mathf.PI / points;
                float radius = (i & 1) == 0 ? outer : inner;
                polygon[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            Polygon(polygon, color);
        }

        public void BlockCell(Vector2 center, float size, Color face, Color edge, bool ghost = false)
        {
            float alpha = ghost ? 0.42f : 1f;
            RoundedRect(new Rect(center.x - size * 0.5f, center.y - size * 0.5f - size * 0.08f, size, size), size * 0.18f, Darken(edge, 0.75f), edge, 0f, edge, alpha);
            RoundedRect(new Rect(center.x - size * 0.5f, center.y - size * 0.42f, size, size * 0.92f), size * 0.18f, Darken(face, 0.92f), Lighten(face, 0.18f), size * 0.055f, Lighten(face, 0.32f), alpha);
        }

        public void Save(string assetPath)
        {
            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false, false);
            texture.SetPixels(_pixels);
            texture.Apply(false, false);
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        public static Color Lighten(Color color, float amount) => Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
        public static Color Darken(Color color, float multiplier) => new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);

        private void Blend(int x, int y, Color source, float coverage)
        {
            int index = y * _width + x;
            float sourceAlpha = Mathf.Clamp01(source.a * coverage);
            Color destination = _pixels[index];
            float outAlpha = sourceAlpha + destination.a * (1f - sourceAlpha);
            if (outAlpha <= 0.0001f) return;
            _pixels[index] = new Color(
                (source.r * sourceAlpha + destination.r * destination.a * (1f - sourceAlpha)) / outAlpha,
                (source.g * sourceAlpha + destination.g * destination.a * (1f - sourceAlpha)) / outAlpha,
                (source.b * sourceAlpha + destination.b * destination.a * (1f - sourceAlpha)) / outAlpha,
                outAlpha);
        }

        private static float RoundedBoxDistance(Vector2 p, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * radius;
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static Rect Inset(Rect rect, float x, float y) => new Rect(rect.x + x, rect.y + y, rect.width - x * 2f, rect.height - y * 2f);

        private static bool InsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool intersects = (a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / Mathf.Max(0.0001f, b.y - a.y) + a.x;
                if (intersects) inside = !inside;
            }
            return inside;
        }
    }
}
#endif
