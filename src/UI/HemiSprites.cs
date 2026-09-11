using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiSprites
    {
        private const int RoundedSize = 64;

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly List<Texture2D> textures = new List<Texture2D>();
        private static readonly Sprite[] rounded = new Sprite[RoundedSize / 2 + 1];
        private static readonly Sprite[,] outlines = new Sprite[RoundedSize / 2 + 1, RoundedSize / 2 + 1];

        internal static Sprite Rounded(int radius = 10)
        {
            radius = Mathf.Clamp(radius, 0, RoundedSize / 2);
            return rounded[radius] ?? (rounded[radius] = CreateRounded(radius));
        }

        private static Sprite CreateRounded(int radius)
        {
            string key = "rounded:" + radius;

            Texture2D texture = CreateTexture(RoundedSize, RoundedSize, (x, y) =>
                RoundedCoverage(x, y, RoundedSize, RoundedSize, radius));

            float border = radius + 1f;
            return Register(key, texture, new Vector4(border, border, border, border));
        }

        internal static Sprite RoundedOutline(int radius = 10, int thickness = 2)
        {
            radius = Mathf.Clamp(radius, 0, RoundedSize / 2);
            thickness = Mathf.Clamp(thickness, 1, radius <= 0 ? 8 : radius);
            return outlines[radius, thickness] ?? (outlines[radius, thickness] = CreateRoundedOutline(radius, thickness));
        }

        private static Sprite CreateRoundedOutline(int radius, int thickness)
        {
            string key = "outline:" + radius + ":" + thickness;

            Texture2D texture = CreateTexture(RoundedSize, RoundedSize, (x, y) =>
            {
                float outer = RoundedCoverage(x, y, RoundedSize, RoundedSize, radius);
                float inner = RoundedCoverage(
                    x - thickness,
                    y - thickness,
                    RoundedSize - thickness * 2,
                    RoundedSize - thickness * 2,
                    Mathf.Max(0, radius - thickness));
                return Mathf.Clamp01(outer - inner);
            });

            float border = radius + thickness + 1f;
            return Register(key, texture, new Vector4(border, border, border, border));
        }

        internal static Sprite Circle()
        {
            const string key = "circle";
            if (cache.TryGetValue(key, out Sprite cached))
                return cached;

            const int size = 128;
            Texture2D texture = CreateTexture(size, size, (x, y) => CircleCoverage(x, y, size, size * 0.5f));
            return Register(key, texture, Vector4.zero);
        }

        internal static Sprite Ring(float thicknessRatio = 0.18f)
        {
            string key = "ring:" + thicknessRatio.ToString("0.00");
            if (cache.TryGetValue(key, out Sprite cached))
                return cached;

            const int size = 128;
            float outer = size * 0.5f;
            float inner = outer * (1f - Mathf.Clamp(thicknessRatio, 0.02f, 0.9f));
            Texture2D texture = CreateTexture(size, size, (x, y) =>
                Mathf.Clamp01(CircleCoverage(x, y, size, outer) - CircleCoverage(x, y, size, inner)));
            return Register(key, texture, Vector4.zero);
        }

        internal static Sprite Triangle()
        {
            const string key = "triangle";
            if (cache.TryGetValue(key, out Sprite cached))
                return cached;

            const int size = 64;
            Texture2D texture = CreateTexture(size, size, (x, y) =>
            {
                float px = (x + 0.5f) / size;
                float py = (y + 0.5f) / size;
                float halfWidth = py * 0.5f;
                float distance = Mathf.Abs(px - 0.5f);
                return Mathf.Clamp01((halfWidth - distance) * size * 0.5f);
            });
            return Register(key, texture, Vector4.zero);
        }

        internal static Sprite CircularArrow()
        {
            const string key = "circularArrow";
            if (cache.TryGetValue(key, out Sprite cached))
                return cached;

            const int size = 128;
            const float radius = 0.29f;
            const float halfWidth = 0.052f;

            const float headDegrees = -55f;
            const float sweepDegrees = 250f;

            float headRadians = headDegrees * Mathf.Deg2Rad;
            Vector2 headCentre = new Vector2(Mathf.Cos(headRadians), Mathf.Sin(headRadians)) * radius;

            Vector2 headDirection = new Vector2(Mathf.Sin(headRadians), -Mathf.Cos(headRadians));
            Vector2 headNormal = new Vector2(-headDirection.y, headDirection.x);

            const float headLength = 0.15f;
            const float headHalf = 0.105f;

            Vector2 headBase = headCentre - headDirection * (headLength * 0.45f);

            Texture2D texture = CreateTexture(size, size, (x, y) =>
            {
                float px = (x + 0.5f) / size - 0.5f;
                float py = (y + 0.5f) / size - 0.5f;

                float distance = Mathf.Sqrt(px * px + py * py);
                float band = halfWidth - Mathf.Abs(distance - radius);

                float angle = Mathf.Atan2(py, px) * Mathf.Rad2Deg;
                float travelled = Mathf.Repeat(headDegrees - angle, 360f);
                float arc = travelled <= sweepDegrees ? band : -1f;

                float ox = px - headBase.x;
                float oy = py - headBase.y;
                float along = ox * headDirection.x + oy * headDirection.y;
                float across = Mathf.Abs(ox * headNormal.x + oy * headNormal.y);

                float head = Mathf.Min(
                    Mathf.Min(along, headLength - along),
                    headHalf * (1f - along / headLength) - across);

                return Mathf.Clamp01(Mathf.Max(arc, head) * size * 0.5f);
            });

            return Register(key, texture, Vector4.zero);
        }

        internal static Sprite Glow()
        {
            const string key = "glow";
            if (cache.TryGetValue(key, out Sprite cached))
                return cached;

            const int size = 128;
            float half = size * 0.5f;
            Texture2D texture = CreateTexture(size, size, (x, y) =>
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float value = Mathf.Clamp01(1f - distance);
                return value * value;
            });
            return Register(key, texture, Vector4.zero);
        }

        private const int GradientWidth = 512;

        internal static Sprite Gradient(IReadOnlyList<string> colors)
        {
            int count = colors == null ? 0 : colors.Count;
            if (count == 0)
                return null;

            StringBuilder keyBuilder = new StringBuilder("gradient");
            for (int i = 0; i < count; i++)
            {
                string value = colors[i];
                if (string.IsNullOrEmpty(value))
                    value = "808080";
                keyBuilder.Append(':');
                if (!value.StartsWith("#", StringComparison.Ordinal))
                    keyBuilder.Append('#');
                keyBuilder.Append(value);
            }

            string key = keyBuilder.ToString();
            if (cache.TryGetValue(key, out Sprite cached))
                return cached;

            Color[] stops = new Color[count];
            for (int i = 0; i < count; i++)
            {
                string value = string.IsNullOrEmpty(colors[i]) ? "808080" : colors[i];
                if (!value.StartsWith("#", StringComparison.Ordinal))
                    value = "#" + value;
                if (!ColorUtility.TryParseHtmlString(value, out stops[i]))
                    stops[i] = Color.grey;
            }

            int last = count - 1;
            Texture2D texture = CreateColorTexture(GradientWidth, 1, (x, y) =>
            {
                float t = last == 0 ? 0f : (float)x / (GradientWidth - 1) * last;
                int index = Mathf.Min(last, Mathf.FloorToInt(t));
                int next = Mathf.Min(last, index + 1);
                return Color.Lerp(stops[index], stops[next], t - index);
            });

            return Register(key, texture, Vector4.zero);
        }

        internal static Texture2D GridTexture(int cell = 16)
        {
            cell = Mathf.Clamp(cell, 4, 128);
            string key = "gridtex:" + cell;
            if (rawTextures.TryGetValue(key, out Texture2D cached))
                return cached;

            Texture2D texture = CreateTexture(cell, cell, (x, y) => x == 0 || y == cell - 1 ? 1f : 0f);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;
            rawTextures[key] = texture;
            return texture;
        }

        internal static Texture2D HueRing(int size = 192, float thicknessRatio = 0.22f)
        {
            string key = "huering:" + size + ":" + thicknessRatio.ToString("0.###");
            if (rawTextures.TryGetValue(key, out Texture2D cached))
                return cached;

            float half = size * 0.5f;
            float outer = half - 1f;
            float inner = outer * (1f - Mathf.Clamp(thicknessRatio, 0.05f, 0.9f));

            Texture2D texture = CreateColorTexture(size, size, (x, y) =>
            {
                float dx = x + 0.5f - half;
                float dy = y + 0.5f - half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01(outer - distance + 0.5f) * Mathf.Clamp01(distance - inner + 0.5f);
                if (alpha <= 0f)
                    return new Color(0f, 0f, 0f, 0f);

                float hue = Mathf.Repeat(Mathf.Atan2(dx, dy) / (Mathf.PI * 2f), 1f);
                Color rgb = Color.HSVToRGB(hue, 1f, 1f);
                return new Color(rgb.r, rgb.g, rgb.b, alpha);
            });

            rawTextures[key] = texture;
            return texture;
        }

        internal static Texture2D SaturationValue(int size = 128)
        {
            string key = "satval:" + size;
            if (rawTextures.TryGetValue(key, out Texture2D cached))
                return cached;

            Texture2D texture = CreateColorTexture(size, size, (x, y) =>
            {
                float saturation = size <= 1 ? 0f : x / (float)(size - 1);
                float value = size <= 1 ? 1f : y / (float)(size - 1);

                float channel = Mathf.Lerp(1f, 0f, saturation);
                return new Color(channel, channel, channel, 1f) * value;
            });

            texture.wrapMode = TextureWrapMode.Clamp;
            rawTextures[key] = texture;
            return texture;
        }

        private static readonly Dictionary<string, Texture2D> rawTextures = new Dictionary<string, Texture2D>();

        private static Texture2D CreateColorTexture(int width, int height, Func<int, int, Color> shade)
        {
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                    pixels[row + x] = shade(x, y);
            }

            return UploadTexture(width, height, pixels);
        }

        internal static void Dispose()
        {
            foreach (Sprite sprite in cache.Values)
            {
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
            }
            cache.Clear();
            Array.Clear(rounded, 0, rounded.Length);
            Array.Clear(outlines, 0, outlines.Length);

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] != null)
                    UnityEngine.Object.Destroy(textures[i]);
            }
            textures.Clear();

            rawTextures.Clear();
        }

        private static float RoundedCoverage(float x, float y, float width, float height, float radius)
        {
            if (width <= 0f || height <= 0f)
                return 0f;

            float px = x + 0.5f;
            float py = y + 0.5f;
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(halfWidth, halfHeight));

            float dx = Mathf.Abs(px - halfWidth) - (halfWidth - radius);
            float dy = Mathf.Abs(py - halfHeight) - (halfHeight - radius);
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            float distance = outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
            return Mathf.Clamp01(0.5f - distance);
        }

        private static float CircleCoverage(float x, float y, float size, float radius)
        {
            float half = size * 0.5f;
            float dx = x + 0.5f - half;
            float dy = y + 0.5f - half;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - distance + 0.5f);
        }

        private static Texture2D CreateTexture(int width, int height, Func<int, int, float> coverage) =>
            CreateColorTexture(width, height, (x, y) => new Color(1f, 1f, 1f, coverage(x, y)));

        private static Texture2D UploadTexture(int width, int height, Color[] pixels)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                name = "HemiSprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            UnityEngine.Object.DontDestroyOnLoad(texture);
            textures.Add(texture);
            return texture;
        }

        private static Sprite Register(string key, Texture2D texture, Vector4 border)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = "HemiSprite " + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            cache[key] = sprite;
            return sprite;
        }
    }

    internal static class HemiIcons
    {
        internal static void Draw(Transform parent, string glyph, Color tint, float size)
        {
            float scaled = HemiTheme.Row(size);
            Image plate = HemiKit.Panel("IconPlate", parent, new Color(tint.r, tint.g, tint.b, 0.16f), scaled * 0.28f);
            RectTransform plateRect = plate.rectTransform;
            Centre(plateRect);
            plateRect.sizeDelta = new Vector2(scaled, scaled);
            plate.raycastTarget = false;

            TextMeshProUGUI symbol = HemiKit.Text("IconGlyph", plateRect, glyph, size * 0.46f, tint, true, TextAlignmentOptions.Center);
            HemiKit.Stretch(symbol.rectTransform);
            symbol.raycastTarget = false;
        }

        internal static RectTransform DrawLogo(Transform parent, float size)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform root = HemiKit.Rect("Logo", parent);
            root.sizeDelta = new Vector2(size, size);

            Image orbit = Shape("Orbit", root, new Color(colors.Text.r, colors.Text.g, colors.Text.b, 0.18f), HemiSprites.Ring(0.045f));
            HemiKit.Stretch(orbit.rectTransform, size * 0.06f, size * 0.06f, size * 0.06f, size * 0.06f);

            Image glow = Shape("Glow", root, new Color(colors.Accent.r, colors.Accent.g, colors.Accent.b, 0.30f), HemiSprites.Glow());
            HemiKit.Stretch(glow.rectTransform, -size * 0.18f, -size * 0.18f, -size * 0.18f, -size * 0.18f);
            glow.transform.SetAsFirstSibling();

            AddPlanet(root, HemiTheme.Hex(0xFF6B4A), new Vector2(-size * 0.27f, 0f), size * 0.30f);
            AddPlanet(root, HemiTheme.Hex(0x4AC8FF), new Vector2(size * 0.27f, 0f), size * 0.30f);

            root.gameObject.AddComponent<HemiLogoSpin>();
            return root;
        }

        internal static RectTransform DrawChromeLogo(Transform parent, float size)
        {
            RectTransform root = HemiKit.Rect("Logo", parent);
            root.sizeDelta = new Vector2(size, size);

            Image orbit = Shape("Orbit", root, new Color(1f, 1f, 1f, 0.22f), HemiSprites.Ring(0.14f));
            HemiKit.Stretch(orbit.rectTransform);

            float radius = size * 0.5f;
            AddPlanet(root, HemiTheme.Hex(0xFF6B4A), new Vector2(-radius, 0f), size * 0.32f);
            AddPlanet(root, HemiTheme.Hex(0x4AC8FF), new Vector2(radius, 0f), size * 0.32f);

            root.gameObject.AddComponent<HemiLogoSpin>();
            return root;
        }

        internal static void DrawCross(Transform parent, Color tint, float size)
        {
            RectTransform root = HemiKit.Rect("Cross", parent);
            Centre(root);
            root.sizeDelta = new Vector2(size, size);

            Bar(root, "Bar", tint, size * 0.08f, new Vector2(size * 0.16f, size * 1.1f), Vector2.zero, 45f);
            Bar(root, "Bar", tint, size * 0.08f, new Vector2(size * 0.16f, size * 1.1f), Vector2.zero, -45f);
        }

        internal static void DrawMagnifier(RectTransform bar, Color tint, float size, float leftInset)
        {
            RectTransform root = HemiKit.Rect("Magnifier", bar);
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(leftInset + size * 0.5f, 0f);
            root.sizeDelta = new Vector2(size, size);

            Image ring = Shape("Ring", root, tint, HemiSprites.Ring(0.22f));
            RectTransform ringRect = ring.rectTransform;
            ringRect.anchorMin = new Vector2(0f, 1f);
            ringRect.anchorMax = new Vector2(0f, 1f);
            ringRect.pivot = new Vector2(0f, 1f);
            ringRect.anchoredPosition = Vector2.zero;
            ringRect.sizeDelta = new Vector2(size * 0.72f, size * 0.72f);

            Image handle = HemiKit.Panel("Handle", root, tint, size * 0.06f);
            handle.raycastTarget = false;
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(1f, 0f);
            handleRect.anchorMax = new Vector2(1f, 0f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = new Vector2(-size * 0.17f, size * 0.17f);
            handleRect.sizeDelta = new Vector2(size * 0.13f, size * 0.42f);
            handleRect.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        internal static void DrawFolder(Transform parent, Color tint, float size)
        {
            RectTransform root = HemiKit.Rect("Folder", parent);
            Centre(root);
            root.sizeDelta = new Vector2(size, size * 0.8f);

            Image tab = HemiKit.Panel("Tab", root, tint, size * 0.08f);
            tab.raycastTarget = false;
            RectTransform tabRect = tab.rectTransform;
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(0f, 1f);
            tabRect.pivot = new Vector2(0f, 1f);
            tabRect.anchoredPosition = Vector2.zero;
            tabRect.sizeDelta = new Vector2(size * 0.44f, size * 0.26f);

            Image body = HemiKit.Panel("Body", root, tint, size * 0.10f);
            body.raycastTarget = false;
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = new Vector2(0f, -size * 0.14f);
        }

        internal static void DrawCheck(Transform parent, Color tint, float size)
        {
            RectTransform root = HemiKit.Rect("Check", parent);
            Centre(root);
            root.sizeDelta = new Vector2(size, size);

            Bar(root, "Short", tint, size * 0.07f, new Vector2(size * 0.14f, size * 0.45f), new Vector2(-size * 0.26f, -size * 0.10f), -40f);
            Bar(root, "Long", tint, size * 0.07f, new Vector2(size * 0.14f, size * 0.85f), new Vector2(size * 0.13f, 0f), 38f);
        }

        private static void AddPlanet(RectTransform parent, Color color, Vector2 offset, float diameter)
        {
            Image halo = Shape("Halo", parent, new Color(color.r, color.g, color.b, 0.35f), HemiSprites.Glow());
            RectTransform haloRect = halo.rectTransform;
            Centre(haloRect);
            haloRect.anchoredPosition = offset;
            haloRect.sizeDelta = new Vector2(diameter * 2.1f, diameter * 2.1f);

            Image planet = HemiKit.Circle("Planet", parent, color);
            planet.raycastTarget = false;
            RectTransform rect = planet.rectTransform;
            Centre(rect);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(diameter, diameter);
        }

        private static Image Shape(string name, Transform parent, Color color, Sprite sprite)
        {
            Image image = HemiKit.Panel(name, parent, color, 0f);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void Centre(RectTransform rect)
        {
            Vector2 middle = new Vector2(0.5f, 0.5f);
            rect.anchorMin = middle;
            rect.anchorMax = middle;
            rect.pivot = middle;
        }

        private static void Bar(RectTransform root, string name, Color tint, float radius, Vector2 size, Vector2 offset, float angle)
        {
            Image bar = HemiKit.Panel(name, root, tint, radius);
            bar.raycastTarget = false;
            RectTransform rect = bar.rectTransform;
            Centre(rect);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    internal sealed class HemiLogoSpin : MonoBehaviour
    {
        private const float DegreesPerSecond = 14f;
        private float angle;

        private void Update()
        {
            if (!HemiTweaksMod.IsInterfaceOpen || !HemiRoot.AnimationsEnabled)
                return;

            angle = (angle + DegreesPerSecond * Time.unscaledDeltaTime) % 360f;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
