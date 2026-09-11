using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace HemiTweaks
{
    [DisallowMultipleComponent]
    internal sealed class KeyViewerVerticalGradient : BaseMeshEffect
    {
        private static readonly List<UIVertex> Buffer = new List<UIVertex>();

        private Color top = Color.white;
        private Color bottom = Color.white;

        internal void Apply(Color topColor, Color bottomColor)
        {
            if (top == topColor && bottom == bottomColor)
                return;

            top = topColor;
            bottom = bottomColor;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0)
                return;

            Buffer.Clear();
            helper.GetUIVertexStream(Buffer);

            float minimum = float.MaxValue;
            float maximum = float.MinValue;
            for (int i = 0; i < Buffer.Count; i++)
            {
                float y = Buffer[i].position.y;
                if (y < minimum)
                    minimum = y;
                if (y > maximum)
                    maximum = y;
            }

            float span = maximum - minimum;
            if (span <= 0.0001f)
                span = 1f;

            for (int i = 0; i < Buffer.Count; i++)
            {
                UIVertex vertex = Buffer[i];
                float t = Mathf.Clamp01((vertex.position.y - minimum) / span);
                vertex.color = Color.Lerp(bottom, top, t);
                Buffer[i] = vertex;
            }

            helper.Clear();
            helper.AddUIVertexTriangleStream(Buffer);
        }
    }

    internal sealed class KeyViewerSpriteFactory
    {
        private sealed class Entry
        {
            internal Dictionary<int, Entry> Owner;
            internal int Key;
            internal Sprite Sprite;
            internal Texture2D Texture;
            internal int Bytes;
            internal int LastUsedFrame;
        }

        private readonly struct RoundedShape
        {
            private readonly bool degenerate;
            private readonly float halfWidth;
            private readonly float halfHeight;
            private readonly float insetX;
            private readonly float insetY;
            private readonly float radius;

            internal RoundedShape(float width, float height, float cornerRadius)
            {
                degenerate = width <= 0f || height <= 0f;
                halfWidth = width * 0.5f;
                halfHeight = height * 0.5f;
                radius = Mathf.Min(cornerRadius, Mathf.Min(halfWidth, halfHeight));
                insetX = halfWidth - radius;
                insetY = halfHeight - radius;
            }

            internal float Distance(float x, float y)
            {
                if (degenerate)
                    return float.MaxValue;

                float px = x + 0.5f;
                float py = y + 0.5f;
                float dx = Mathf.Abs(px - halfWidth) - insetX;
                float dy = Mathf.Abs(py - halfHeight) - insetY;
                float ox = Mathf.Max(dx, 0f);
                float oy = Mathf.Max(dy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
            }

            internal float Coverage(float x, float y)
            {
                return Mathf.Clamp01(0.5f - Distance(x, y));
            }
        }

        private const int TextureBudgetBytes = 24 * 1024 * 1024;

        private readonly Dictionary<int, Entry> rounded = new Dictionary<int, Entry>();
        private readonly Dictionary<int, Entry> ring = new Dictionary<int, Entry>();
        private readonly Dictionary<int, Entry> soft = new Dictionary<int, Entry>();
        private readonly List<Entry> entries = new List<Entry>();
        private int cachedBytes;

        internal Sprite Rounded(int requestedRadius)
        {
            int radius = Mathf.Clamp(requestedRadius, 0, 100);
            if (TryTake(rounded, radius, out Sprite cached))
                return cached;

            int size = Mathf.Max(16, radius * 2 + 4);
            RoundedShape shape = new RoundedShape(size, size, radius);
            Texture2D texture = CreateTexture(size, "KeyViewerRounded_" + radius, (x, y) => shape.Coverage(x, y));

            float border = Mathf.Max(1f, radius + 1f);
            return Store(rounded, radius, CreateSprite(texture, border), texture);
        }

        internal Sprite Ring(int requestedRadius, int requestedThickness)
        {
            int radius = Mathf.Clamp(requestedRadius, 0, 100);
            int thickness = Mathf.Clamp(requestedThickness, 1, 100);
            int key = radius * 1000 + thickness;
            if (TryTake(ring, key, out Sprite cached))
                return cached;

            int size = Mathf.Max(16, (radius + thickness) * 2 + 4);
            RoundedShape outerShape = new RoundedShape(size, size, radius);
            RoundedShape innerShape = new RoundedShape(
                size - thickness * 2, size - thickness * 2, Mathf.Max(0, radius - thickness));
            Texture2D texture = CreateTexture(size, "KeyViewerRing_" + radius + "_" + thickness, (x, y) =>
            {
                float outer = outerShape.Coverage(x, y);
                float inner = innerShape.Coverage(x - thickness, y - thickness);
                return Mathf.Clamp01(outer - inner);
            });

            float border = Mathf.Max(1f, radius + thickness + 1f);
            return Store(ring, key, CreateSprite(texture, border), texture);
        }

        internal Sprite Soft(int requestedRadius, int requestedBlur)
        {
            int blur = Mathf.Clamp(requestedBlur, 1, 64);
            int radius = Mathf.Clamp(requestedRadius, 0, 100);
            int key = radius * 1000 + blur;
            if (TryTake(soft, key, out Sprite cached))
                return cached;

            int size = Mathf.Max(16, (radius + blur) * 2 + 4);
            int inset = blur;
            RoundedShape shape = new RoundedShape(size - inset * 2, size - inset * 2, radius);
            Texture2D texture = CreateTexture(size, "KeyViewerSoft_" + radius + "_" + blur, (x, y) =>
            {
                float distance = shape.Distance(x - inset, y - inset);
                if (distance <= 0f)
                    return 1f;
                float t = Mathf.Clamp01(1f - distance / blur);
                return t * t * (3f - 2f * t);
            });

            float border = Mathf.Max(1f, radius + blur + 1f);
            return Store(soft, key, CreateSprite(texture, border), texture);
        }

        private bool TryTake(Dictionary<int, Entry> owner, int key, out Sprite sprite)
        {
            sprite = null;
            if (!owner.TryGetValue(key, out Entry entry))
                return false;

            if (entry.Sprite == null)
            {
                Release(entry);
                return false;
            }

            entry.LastUsedFrame = Time.frameCount;
            sprite = entry.Sprite;
            return true;
        }

        private Sprite Store(Dictionary<int, Entry> owner, int key, Sprite sprite, Texture2D texture)
        {
            Entry entry = new Entry
            {
                Owner = owner,
                Key = key,
                Sprite = sprite,
                Texture = texture,
                Bytes = texture == null ? 0 : texture.width * texture.height * 4,
                LastUsedFrame = Time.frameCount
            };

            owner[key] = entry;
            entries.Add(entry);
            cachedBytes += entry.Bytes;
            Trim();
            return sprite;
        }

        private void Trim()
        {
            int frame = Time.frameCount;
            while (cachedBytes > TextureBudgetBytes)
            {
                int victim = -1;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].LastUsedFrame == frame)
                        continue;
                    if (victim < 0 || entries[i].LastUsedFrame < entries[victim].LastUsedFrame)
                        victim = i;
                }

                if (victim < 0)
                    return;

                Release(entries[victim]);
            }
        }

        private void Release(Entry entry)
        {
            if (entry.Owner != null && entry.Owner.TryGetValue(entry.Key, out Entry held) && held == entry)
                entry.Owner.Remove(entry.Key);

            entries.Remove(entry);
            cachedBytes -= entry.Bytes;

            if (entry.Sprite != null)
                UnityEngine.Object.Destroy(entry.Sprite);
            if (entry.Texture != null)
                UnityEngine.Object.Destroy(entry.Texture);

            entry.Sprite = null;
            entry.Texture = null;
        }

        internal void Dispose()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Sprite != null)
                    UnityEngine.Object.Destroy(entries[i].Sprite);
                if (entries[i].Texture != null)
                    UnityEngine.Object.Destroy(entries[i].Texture);
            }

            entries.Clear();
            rounded.Clear();
            ring.Clear();
            soft.Clear();
            cachedBytes = 0;
        }

        private Texture2D CreateTexture(int size, string name, System.Func<int, int, float> coverage)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                    pixels[row + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(coverage(x, y)));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture, float border)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    internal static class KeyViewerImageCache
    {
        private static readonly Dictionary<string, Sprite> sprites =
            new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> failed =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private static long cachedPixels;
        private static bool budgetWarning;

        internal static Sprite Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (sprites.TryGetValue(path, out Sprite cached))
                return cached;
            if (failed.Contains(path))
                return null;
            if (failed.Count >= HemiImageSafety.MaximumFailedPaths
                || !HemiImageSafety.CanCache(sprites.Count, cachedPixels, 1, 1))
            {
                WarnBudget();
                return null;
            }

            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                texture = HemiImageDecoder.LoadFile(path, "Key image", HemiImageSafety.MaximumCachePixels - cachedPixels);
                if (texture == null)
                {
                    failed.Add(path);
                    return null;
                }
                if (!HemiImageSafety.CanCache(sprites.Count, cachedPixels, texture.width, texture.height))
                {
                    failed.Add(path);
                    WarnBudget();
                    return null;
                }

                UnityEngine.Object.DontDestroyOnLoad(texture);
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                sprite.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(sprite);

                sprites[path] = sprite;
                cachedPixels += (long)texture.width * texture.height;
                texture = null;
                Sprite result = sprite;
                sprite = null;
                return result;
            }
            catch (System.Exception exception)
            {
                failed.Add(path);
                MelonLoader.MelonLogger.Warning("Key image failed to load (" + path + "): " + exception.Message);
                return null;
            }
            finally
            {
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
        }

        internal static void Invalidate(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            failed.Remove(path);
            budgetWarning = false;
            if (!sprites.TryGetValue(path, out Sprite sprite))
                return;

            sprites.Remove(path);
            if (sprite != null && sprite.texture != null)
                cachedPixels -= (long)sprite.texture.width * sprite.texture.height;
            Destroy(sprite);
        }

        internal static void Clear()
        {
            foreach (Sprite sprite in sprites.Values)
                Destroy(sprite);
            sprites.Clear();
            failed.Clear();
            cachedPixels = 0;
            budgetWarning = false;
        }

        private static void WarnBudget()
        {
            if (budgetWarning)
                return;
            budgetWarning = true;
            MelonLoader.MelonLogger.Warning("Key image cache reached its image or failed-path limit; existing images were retained.");
        }

        private static void Destroy(Sprite sprite)
        {
            if (sprite == null)
                return;

            Texture texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null)
                UnityEngine.Object.Destroy(texture);
        }
    }

    [Serializable]
    internal sealed class KeyViewerGradientStop
    {
        public Color Color;
        public float Position;

        public KeyViewerGradientStop()
        {
        }

        public KeyViewerGradientStop(Color color, float position)
        {
            Color = color;
            Position = position;
        }
    }

    [Serializable]
    internal sealed class KeyViewerGradient
    {
        public float Angle = 90f;
        public readonly List<KeyViewerGradientStop> Stops = new List<KeyViewerGradientStop>();

        public static KeyViewerGradient Clone(KeyViewerGradient source)
        {
            if (source == null || source.Stops.Count < 2)
                return null;

            KeyViewerGradient result = new KeyViewerGradient { Angle = source.Angle };
            for (int i = 0; i < source.Stops.Count; i++)
            {
                KeyViewerGradientStop stop = source.Stops[i];
                if (stop != null)
                    result.Stops.Add(new KeyViewerGradientStop(stop.Color, stop.Position));
            }
            return result.Stops.Count >= 2 ? result : null;
        }

        public Color Evaluate(float position)
        {
            if (Stops.Count == 0)
                return Color.white;

            float value = Mathf.Clamp01(position);
            KeyViewerGradientStop previous = Stops[0];
            if (value <= previous.Position)
                return previous.Color;

            for (int i = 1; i < Stops.Count; i++)
            {
                KeyViewerGradientStop next = Stops[i];
                if (value > next.Position)
                {
                    previous = next;
                    continue;
                }

                float span = Mathf.Max(0.0001f, next.Position - previous.Position);
                return Color.Lerp(previous.Color, next.Color, (value - previous.Position) / span);
            }

            return Stops[Stops.Count - 1].Color;
        }

        public void Normalize()
        {
            Angle = float.IsNaN(Angle) || float.IsInfinity(Angle) ? 90f : Mathf.Repeat(Angle, 360f);
            Stops.RemoveAll(stop => stop == null || float.IsNaN(stop.Position) || float.IsInfinity(stop.Position));
            Stops.Sort((left, right) => left.Position.CompareTo(right.Position));
            if (Stops.Count > 8)
                Stops.RemoveRange(8, Stops.Count - 8);
            for (int i = 0; i < Stops.Count; i++)
                Stops[i].Position = Mathf.Clamp01(Stops[i].Position);
        }
    }

    [Serializable]
    internal sealed class KeyViewerLayerGroup
    {
        public string Id;
        public string Name;

        public KeyViewerLayerGroup()
        {
        }

        public KeyViewerLayerGroup(KeyViewerLayerGroup source)
        {
            Id = source?.Id ?? "";
            Name = source?.Name ?? "";
        }
    }

    [Serializable]
    internal sealed class KeyViewerGradientData
    {
        public float angle;
        public KeyViewerGradientStopData[] stops;
    }

    [Serializable]
    internal sealed class KeyViewerGradientStopData
    {
        public string color;
        public float position;
    }

    [Serializable]
    internal sealed class KeyViewerAdvancedKeyData
    {
        public string[] additionalKeys;
        public string keyMatch;
        public string idleImageFit;
        public string activeImageFit;
        public bool activeShadowEnabled;
        public float activeShadowOffsetX;
        public float activeShadowOffsetY;
        public float activeShadowBlur;
        public string elementId;
        public bool hidden;
        public string layerName;
        public string groupId;
        public string cssClass;
        public bool useInlineStyles;
        public string fontFamily;
        public string fontFilePath;
        public int fontWeight;
        public bool fontItalic;
        public bool fontUnderline;
        public bool fontStrikethrough;
        public KeyViewerGradientData backgroundGradient;
        public KeyViewerGradientData activeBackgroundGradient;
        public KeyViewerGradientData borderGradient;
        public KeyViewerGradientData activeBorderGradient;
        public bool glowGradient;
        public string glowColorBottom;
        public float glowOpacityBottom;
        public bool noteAutoYCorrection;
        public KeyViewerGradientData counterIdleGradient;
        public KeyViewerGradientData counterActiveGradient;
        public string counterIdleStrokeColor;
        public string counterActiveStrokeColor;
        public string counterFontFamily;
        public string counterFontFilePath;
        public int counterFontWeight;
        public bool counterFontItalic;
        public bool counterFontUnderline;
        public bool counterFontStrikethrough;
        public float counterBezierX1;
        public float counterBezierY1;
        public float counterBezierX2;
        public float counterBezierY2;
        public string graphType;
        public float graphSpeedSeconds;
        public string graphColor;
        public bool graphShowAverage;
        public bool graphAnimationEnabled;
        public string knobAxisId;
        public float knobSensitivity;
        public bool knobReverse;
    }

    [Serializable]
    internal sealed class KeyViewerAdvancedConfigData
    {
        public int noteEffectMode;
        public int keyCounterMode;
        public int noteFrameLimit;
        public bool delayedNoteEnabled;
        public float shortNoteThresholdMs;
        public float keyDisplayDelayMs;
        public float noteFadeTop;
        public float noteFadeBottom;
        public float reverseNoteFadeTop;
        public float reverseNoteFadeBottom;
        public string boardBackgroundColor;
        public bool gridAlignmentGuides;
        public bool gridSpacingGuides;
        public bool gridSizeMatchGuides;
        public bool gridMinimap;
        public float gridSnapSize;
        public float gridOverlayPadding;
        public bool useCustomCss;
        public string customCss;
        public KeyViewerLayerGroup[] layerGroups;
    }

    internal static class KeyViewerAdvancedPersistence
    {
        internal static KeyViewerAdvancedKeyData Capture(KeyViewerKeyConfig key)
        {
            string[] additional = new string[key.AdditionalKeys.Count];
            for (int i = 0; i < additional.Length; i++)
                additional[i] = key.AdditionalKeys[i].ToString();

            return new KeyViewerAdvancedKeyData
            {
                additionalKeys = additional,
                keyMatch = key.KeyMatch.ToString(),
                idleImageFit = key.IdleImageFit.ToString(),
                activeImageFit = key.ActiveImageFit.ToString(),
                activeShadowEnabled = key.ActiveShadowEnabled,
                activeShadowOffsetX = key.ActiveShadowOffset.x,
                activeShadowOffsetY = key.ActiveShadowOffset.y,
                activeShadowBlur = key.ActiveShadowBlur,
                elementId = key.ElementId,
                hidden = key.Hidden,
                layerName = key.LayerName,
                groupId = key.GroupId,
                cssClass = key.CssClass,
                useInlineStyles = key.UseInlineStyles,
                fontFamily = key.FontFamily,
                fontFilePath = key.FontFilePath,
                fontWeight = key.FontWeight,
                fontItalic = key.FontItalic,
                fontUnderline = key.FontUnderline,
                fontStrikethrough = key.FontStrikethrough,
                backgroundGradient = CaptureGradient(key.BackgroundGradient),
                activeBackgroundGradient = CaptureGradient(key.ActiveBackgroundGradient),
                borderGradient = CaptureGradient(key.BorderGradient),
                activeBorderGradient = CaptureGradient(key.ActiveBorderGradient),
                glowGradient = key.GlowGradient,
                glowColorBottom = ColorText(key.GlowColorBottom),
                glowOpacityBottom = key.GlowOpacityBottom,
                noteAutoYCorrection = key.NoteAutoYCorrection,
                counterIdleGradient = CaptureGradient(key.CounterIdleGradient),
                counterActiveGradient = CaptureGradient(key.CounterActiveGradient),
                counterIdleStrokeColor = ColorText(key.CounterIdleStrokeColor),
                counterActiveStrokeColor = ColorText(key.CounterActiveStrokeColor),
                counterFontFamily = key.CounterFontFamily,
                counterFontFilePath = key.CounterFontFilePath,
                counterFontWeight = key.CounterFontWeight,
                counterFontItalic = key.CounterFontItalic,
                counterFontUnderline = key.CounterFontUnderline,
                counterFontStrikethrough = key.CounterFontStrikethrough,
                counterBezierX1 = key.CounterAnimationBezier.x,
                counterBezierY1 = key.CounterAnimationBezier.y,
                counterBezierX2 = key.CounterAnimationBezier.z,
                counterBezierY2 = key.CounterAnimationBezier.w,
                graphType = key.GraphType.ToString(),
                graphSpeedSeconds = key.GraphSpeedSeconds,
                graphColor = ColorText(key.GraphColor),
                graphShowAverage = key.GraphShowAverage,
                graphAnimationEnabled = key.GraphAnimationEnabled,
                knobAxisId = key.KnobAxisId,
                knobSensitivity = key.KnobSensitivity,
                knobReverse = key.KnobReverse
            };
        }

        internal static void Apply(KeyViewerKeyConfig key, KeyViewerAdvancedKeyData data)
        {
            ApplyDefaults(key);
            if (data == null)
                return;

            key.AdditionalKeys.Clear();
            if (data.additionalKeys != null)
            {
                for (int i = 0; i < data.additionalKeys.Length && key.AdditionalKeys.Count < 7; i++)
                {
                    if (Enum.TryParse(data.additionalKeys[i], true, out KeyCode code) && code != KeyCode.None &&
                        code != key.Key && !key.AdditionalKeys.Contains(code))
                    {
                        key.AdditionalKeys.Add(code);
                    }
                }
            }
            key.KeyMatch = data.keyMatch.ToEnum(KeyViewerKeyMatch.Any, showWarning: false);
            key.IdleImageFit = data.idleImageFit.ToEnum(key.ImageFit, showWarning: false);
            key.ActiveImageFit = data.activeImageFit.ToEnum(key.ImageFit, showWarning: false);
            key.ActiveShadowEnabled = data.activeShadowEnabled;
            key.ActiveShadowOffset = FiniteVector(data.activeShadowOffsetX, data.activeShadowOffsetY, key.ShadowOffset);
            key.ActiveShadowBlur = Finite(data.activeShadowBlur, 0f, 100f, key.ShadowBlur);
            key.ElementId = data.elementId ?? "";
            key.Hidden = data.hidden;
            key.LayerName = data.layerName ?? "";
            key.GroupId = data.groupId ?? "";
            key.CssClass = data.cssClass ?? "";
            key.UseInlineStyles = data.useInlineStyles;
            key.FontFamily = data.fontFamily ?? "";
            key.FontFilePath = data.fontFilePath ?? "";
            key.FontWeight = Mathf.Clamp(data.fontWeight == 0 ? 700 : data.fontWeight, 100, 900);
            key.FontItalic = data.fontItalic;
            key.FontUnderline = data.fontUnderline;
            key.FontStrikethrough = data.fontStrikethrough;
            key.BackgroundGradient = ReadGradient(data.backgroundGradient);
            key.ActiveBackgroundGradient = ReadGradient(data.activeBackgroundGradient);
            key.BorderGradient = ReadGradient(data.borderGradient);
            key.ActiveBorderGradient = ReadGradient(data.activeBorderGradient);
            key.GlowGradient = data.glowGradient;
            key.GlowColorBottom = ReadColor(data.glowColorBottom, key.GlowColor);
            key.GlowOpacityBottom = Finite(data.glowOpacityBottom, 0f, 1f, key.GlowOpacity);
            key.NoteAutoYCorrection = data.noteAutoYCorrection;
            key.CounterIdleGradient = ReadGradient(data.counterIdleGradient);
            key.CounterActiveGradient = ReadGradient(data.counterActiveGradient);
            key.CounterIdleStrokeColor = ReadColor(data.counterIdleStrokeColor, Color.clear);
            key.CounterActiveStrokeColor = ReadColor(data.counterActiveStrokeColor, Color.clear);
            key.CounterFontFamily = data.counterFontFamily ?? "";
            key.CounterFontFilePath = data.counterFontFilePath ?? "";
            key.CounterFontWeight = Mathf.Clamp(data.counterFontWeight == 0 ? 500 : data.counterFontWeight, 100, 900);
            key.CounterFontItalic = data.counterFontItalic;
            key.CounterFontUnderline = data.counterFontUnderline;
            key.CounterFontStrikethrough = data.counterFontStrikethrough;
            key.CounterAnimationBezier = new Vector4(
                Finite(data.counterBezierX1, 0f, 1f, 0.25f),
                Finite(data.counterBezierY1, -2f, 2f, 0.46f),
                Finite(data.counterBezierX2, 0f, 1f, 0.45f),
                Finite(data.counterBezierY2, -2f, 2f, 0.94f));
            key.GraphType = data.graphType.ToEnum(KeyViewerGraphType.Line, showWarning: false);
            key.GraphSpeedSeconds = Finite(data.graphSpeedSeconds, 0.05f, 60f, 1f);
            key.GraphColor = ReadColor(data.graphColor, Color.white);
            key.GraphShowAverage = data.graphShowAverage;
            key.GraphAnimationEnabled = data.graphAnimationEnabled;
            key.KnobAxisId = data.knobAxisId ?? "";
            key.KnobSensitivity = Finite(data.knobSensitivity, 0f, 100f, 1f);
            key.KnobReverse = data.knobReverse;
        }

        internal static void ApplyDefaults(KeyViewerKeyConfig key)
        {
            key.KeyMatch = KeyViewerKeyMatch.Any;
            key.IdleImageFit = key.ImageFit;
            key.ActiveImageFit = key.ImageFit;
            key.ActiveShadowEnabled = key.ShadowEnabled;
            key.ActiveShadowOffset = key.ShadowOffset;
            key.ActiveShadowBlur = key.ShadowBlur;
            key.UseInlineStyles = true;
            key.FontWeight = 700;
            key.GlowColorBottom = key.GlowColor;
            key.GlowOpacityBottom = key.GlowOpacity;
            key.NoteAutoYCorrection = true;
            key.CounterIdleStrokeColor = Color.clear;
            key.CounterActiveStrokeColor = Color.clear;
            key.CounterFontWeight = 500;
            key.CounterAnimationBezier = new Vector4(0.25f, 0.46f, 0.45f, 0.94f);
            key.GraphType = KeyViewerGraphType.Line;
            key.GraphSpeedSeconds = 1f;
            key.GraphColor = Color.white;
            key.GraphShowAverage = true;
            key.GraphAnimationEnabled = true;
            key.KnobSensitivity = 1f;
        }

        internal static KeyViewerAdvancedConfigData Capture(KeyViewerConfig config)
        {
            KeyViewerLayerGroup[] groups = new KeyViewerLayerGroup[config.LayerGroups.Count];
            for (int i = 0; i < groups.Length; i++)
                groups[i] = new KeyViewerLayerGroup(config.LayerGroups[i]);

            return new KeyViewerAdvancedConfigData
            {
                noteEffectMode = config.ShowNotes ? 2 : 1,
                keyCounterMode = config.ShowCounters ? 2 : 1,
                noteFrameLimit = config.NoteFrameLimit,
                delayedNoteEnabled = config.DelayedNoteEnabled,
                shortNoteThresholdMs = config.ShortNoteThresholdMs,
                keyDisplayDelayMs = config.KeyDisplayDelayMs,
                noteFadeTop = config.NoteFadeTop,
                noteFadeBottom = config.NoteFadeBottom,
                reverseNoteFadeTop = config.ReverseNoteFadeTop,
                reverseNoteFadeBottom = config.ReverseNoteFadeBottom,
                boardBackgroundColor = ColorText(config.BoardBackgroundColor),
                gridAlignmentGuides = config.GridAlignmentGuides,
                gridSpacingGuides = config.GridSpacingGuides,
                gridSizeMatchGuides = config.GridSizeMatchGuides,
                gridMinimap = config.GridMinimap,
                gridSnapSize = config.GridSnapSize,
                gridOverlayPadding = config.GridOverlayPadding,
                useCustomCss = config.UseCustomCss,
                customCss = config.CustomCss,
                layerGroups = groups
            };
        }

        internal static void Apply(KeyViewerConfig config, KeyViewerAdvancedConfigData data)
        {
            config.NoteFrameLimit = 0;
            config.DelayedNoteEnabled = false;
            config.ShortNoteThresholdMs = 50f;
            config.KeyDisplayDelayMs = 0f;
            config.NoteFadeTop = config.NoteFadeFar;
            config.NoteFadeBottom = config.NoteFadeNear;
            config.ReverseNoteFadeTop = config.NoteFadeNear;
            config.ReverseNoteFadeBottom = config.NoteFadeFar;
            config.BoardBackgroundColor = Color.clear;
            config.GridAlignmentGuides = true;
            config.GridSpacingGuides = true;
            config.GridSizeMatchGuides = true;
            config.GridMinimap = true;
            config.GridSnapSize = 5f;
            config.GridOverlayPadding = 0f;
            config.UseCustomCss = false;
            config.CustomCss = "";
            config.LayerGroups.Clear();
            if (data == null)
                return;

            if (data.noteEffectMode != 0)
                config.ShowNotes = data.noteEffectMode == 2;
            if (data.keyCounterMode != 0)
                config.ShowCounters = data.keyCounterMode == 2;

            config.NoteFrameLimit = Mathf.Clamp(data.noteFrameLimit, 0, 240);
            config.DelayedNoteEnabled = data.delayedNoteEnabled;
            config.ShortNoteThresholdMs = Finite(data.shortNoteThresholdMs, 0f, 2000f, 50f);
            config.KeyDisplayDelayMs = Finite(data.keyDisplayDelayMs, 0f, 30000f, 0f);
            config.NoteFadeTop = Finite(data.noteFadeTop, 0f, 400f, config.NoteFadeFar);
            config.NoteFadeBottom = Finite(data.noteFadeBottom, 0f, 400f, config.NoteFadeNear);
            config.ReverseNoteFadeTop = Finite(data.reverseNoteFadeTop, 0f, 400f, config.NoteFadeNear);
            config.ReverseNoteFadeBottom = Finite(data.reverseNoteFadeBottom, 0f, 400f, config.NoteFadeFar);
            config.BoardBackgroundColor = ReadColor(data.boardBackgroundColor, Color.clear);
            config.GridAlignmentGuides = data.gridAlignmentGuides;
            config.GridSpacingGuides = data.gridSpacingGuides;
            config.GridSizeMatchGuides = data.gridSizeMatchGuides;
            config.GridMinimap = data.gridMinimap;
            config.GridSnapSize = Finite(data.gridSnapSize, 1f, 10f, 5f);
            config.GridOverlayPadding = Finite(data.gridOverlayPadding, 0f, 30f, 0f);
            config.UseCustomCss = data.useCustomCss;
            config.CustomCss = data.customCss ?? "";
            if (data.layerGroups != null)
            {
                for (int i = 0; i < data.layerGroups.Length; i++)
                {
                    KeyViewerLayerGroup group = data.layerGroups[i];
                    if (group != null && !string.IsNullOrWhiteSpace(group.Id))
                        config.LayerGroups.Add(new KeyViewerLayerGroup(group));
                }
            }
        }

        internal static KeyViewerGradientData CaptureGradient(KeyViewerGradient gradient)
        {
            if (gradient == null || gradient.Stops.Count < 2)
                return null;
            KeyViewerGradientStopData[] stops = new KeyViewerGradientStopData[gradient.Stops.Count];
            for (int i = 0; i < stops.Length; i++)
            {
                stops[i] = new KeyViewerGradientStopData
                {
                    color = ColorText(gradient.Stops[i].Color),
                    position = gradient.Stops[i].Position
                };
            }
            return new KeyViewerGradientData { angle = gradient.Angle, stops = stops };
        }

        internal static KeyViewerGradient ReadGradient(KeyViewerGradientData data)
        {
            if (data?.stops == null || data.stops.Length < 2)
                return null;
            KeyViewerGradient gradient = new KeyViewerGradient { Angle = data.angle };
            for (int i = 0; i < data.stops.Length && i < 8; i++)
            {
                KeyViewerGradientStopData stop = data.stops[i];
                if (stop != null)
                    gradient.Stops.Add(new KeyViewerGradientStop(ReadColor(stop.color, Color.white), stop.position));
            }
            gradient.Normalize();
            return gradient.Stops.Count >= 2 ? gradient : null;
        }

        internal static string ColorText(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        internal static Color ReadColor(string text, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;
            return ColorUtility.TryParseHtmlString(text, out Color parsed)
                ? parsed
                : KeyViewerDmNote.ParseColor(text, fallback.a);
        }

        private static Vector2 FiniteVector(float x, float y, Vector2 fallback)
        {
            return new Vector2(Finite(x, -1000f, 1000f, fallback.x), Finite(y, -1000f, 1000f, fallback.y));
        }

        private static float Finite(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
        }
    }

    internal sealed class KeyViewerLinearGradient : BaseMeshEffect
    {
        private struct GradientVertex
        {
            internal UIVertex Vertex;
            internal float Position;

            internal GradientVertex(UIVertex vertex, float position)
            {
                Vertex = vertex;
                Position = position;
            }
        }

        private KeyViewerGradient gradient;
        private readonly List<UIVertex> sourceVertices = new List<UIVertex>(96);
        private readonly List<UIVertex> outputVertices = new List<UIVertex>(384);
        private readonly List<GradientVertex> clipA = new List<GradientVertex>(12);
        private readonly List<GradientVertex> clipB = new List<GradientVertex>(12);
        private readonly List<float> boundaries = new List<float>(10);
        private int appliedSignature;

        internal void Apply(KeyViewerGradient value)
        {
            int nextSignature = Signature(value);
            if (ReferenceEquals(gradient, value) && appliedSignature == nextSignature)
                return;
            gradient = value;
            appliedSignature = nextSignature;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        private static int Signature(KeyViewerGradient value)
        {
            if (value == null)
                return 0;
            unchecked
            {
                int hash = value.Angle.GetHashCode();
                for (int i = 0; i < value.Stops.Count; i++)
                {
                    KeyViewerGradientStop stop = value.Stops[i];
                    if (stop == null)
                    {
                        hash = hash * 31;
                        continue;
                    }
                    hash = hash * 31 + stop.Position.GetHashCode();
                    hash = hash * 31 + stop.Color.GetHashCode();
                }
                return hash;
            }
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || gradient == null || gradient.Stops.Count < 2 || helper.currentVertCount == 0)
                return;

            sourceVertices.Clear();
            helper.GetUIVertexStream(sourceVertices);
            if (sourceVertices.Count < 3)
                return;

            Vector2 minimum = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maximum = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < sourceVertices.Count; i++)
            {
                UIVertex vertex = sourceVertices[i];
                minimum = Vector2.Min(minimum, vertex.position);
                maximum = Vector2.Max(maximum, vertex.position);
            }

            float radians = gradient.Angle * Mathf.Deg2Rad;
            Vector2 axis = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            float p0 = Vector2.Dot(new Vector2(minimum.x, minimum.y), axis);
            float p1 = Vector2.Dot(new Vector2(minimum.x, maximum.y), axis);
            float p2 = Vector2.Dot(new Vector2(maximum.x, maximum.y), axis);
            float p3 = Vector2.Dot(new Vector2(maximum.x, minimum.y), axis);
            float low = Mathf.Min(Mathf.Min(p0, p1), Mathf.Min(p2, p3));
            float high = Mathf.Max(Mathf.Max(p0, p1), Mathf.Max(p2, p3));
            float span = Mathf.Max(0.0001f, high - low);

            boundaries.Clear();
            boundaries.Add(0f);
            for (int i = 0; i < gradient.Stops.Count; i++)
            {
                float stop = Mathf.Clamp01(gradient.Stops[i].Position);
                if (stop <= 0.0001f || stop >= 0.9999f ||
                    Mathf.Abs(stop - boundaries[boundaries.Count - 1]) <= 0.0001f)
                    continue;
                boundaries.Add(stop);
            }
            boundaries.Add(1f);

            outputVertices.Clear();
            for (int triangle = 0; triangle + 2 < sourceVertices.Count; triangle += 3)
            {
                UIVertex v0 = sourceVertices[triangle];
                UIVertex v1 = sourceVertices[triangle + 1];
                UIVertex v2 = sourceVertices[triangle + 2];
                GradientVertex g0 = Wrap(v0, axis, low, span);
                GradientVertex g1 = Wrap(v1, axis, low, span);
                GradientVertex g2 = Wrap(v2, axis, low, span);
                float triangleMin = Mathf.Min(Mathf.Min(g0.Position, g1.Position), g2.Position);
                float triangleMax = Mathf.Max(Mathf.Max(g0.Position, g1.Position), g2.Position);

                for (int band = 0; band + 1 < boundaries.Count; band++)
                {
                    float bandMin = boundaries[band];
                    float bandMax = boundaries[band + 1];
                    if (triangleMax < bandMin - 0.0001f || triangleMin > bandMax + 0.0001f)
                        continue;

                    clipA.Clear();
                    clipA.Add(g0);
                    clipA.Add(g1);
                    clipA.Add(g2);
                    List<GradientVertex> input = clipA;
                    List<GradientVertex> output = clipB;
                    Clip(input, output, bandMin, true);
                    Swap(ref input, ref output);
                    Clip(input, output, bandMax, false);
                    Swap(ref input, ref output);
                    if (input.Count < 3)
                        continue;

                    UIVertex first = Tinted(input[0]);
                    for (int i = 1; i + 1 < input.Count; i++)
                    {
                        outputVertices.Add(first);
                        outputVertices.Add(Tinted(input[i]));
                        outputVertices.Add(Tinted(input[i + 1]));
                    }
                }
            }

            if (outputVertices.Count == 0)
                return;
            helper.Clear();
            helper.AddUIVertexTriangleStream(outputVertices);
        }

        private static GradientVertex Wrap(UIVertex vertex, Vector2 axis, float low, float span)
        {
            float position = (Vector2.Dot(vertex.position, axis) - low) / span;
            return new GradientVertex(vertex, Mathf.Clamp01(position));
        }

        private static void Swap(ref List<GradientVertex> left, ref List<GradientVertex> right)
        {
            List<GradientVertex> temporary = left;
            left = right;
            right = temporary;
        }

        private static void Clip(
            List<GradientVertex> input,
            List<GradientVertex> output,
            float boundary,
            bool keepGreater)
        {
            output.Clear();
            if (input.Count == 0)
                return;

            GradientVertex previous = input[input.Count - 1];
            bool previousInside = keepGreater
                ? previous.Position >= boundary - 0.0001f
                : previous.Position <= boundary + 0.0001f;
            for (int i = 0; i < input.Count; i++)
            {
                GradientVertex current = input[i];
                bool currentInside = keepGreater
                    ? current.Position >= boundary - 0.0001f
                    : current.Position <= boundary + 0.0001f;
                if (currentInside != previousInside)
                {
                    float distance = current.Position - previous.Position;
                    float amount = Mathf.Abs(distance) <= 0.000001f
                        ? 0f
                        : Mathf.Clamp01((boundary - previous.Position) / distance);
                    output.Add(Lerp(previous, current, amount, boundary));
                }
                if (currentInside)
                    output.Add(current);

                previous = current;
                previousInside = currentInside;
            }
        }

        private static GradientVertex Lerp(
            GradientVertex from,
            GradientVertex to,
            float amount,
            float position)
        {
            UIVertex vertex = from.Vertex;
            vertex.position = Vector3.Lerp(from.Vertex.position, to.Vertex.position, amount);
            vertex.normal = Vector3.Lerp(from.Vertex.normal, to.Vertex.normal, amount);
            vertex.tangent = Vector4.Lerp(from.Vertex.tangent, to.Vertex.tangent, amount);
            vertex.color = (Color32)Color.Lerp((Color)from.Vertex.color, (Color)to.Vertex.color, amount);
            vertex.uv0 = Vector2.Lerp(from.Vertex.uv0, to.Vertex.uv0, amount);
            vertex.uv1 = Vector2.Lerp(from.Vertex.uv1, to.Vertex.uv1, amount);
            vertex.uv2 = Vector2.Lerp(from.Vertex.uv2, to.Vertex.uv2, amount);
            vertex.uv3 = Vector2.Lerp(from.Vertex.uv3, to.Vertex.uv3, amount);
            return new GradientVertex(vertex, position);
        }

        private UIVertex Tinted(GradientVertex source)
        {
            UIVertex vertex = source.Vertex;
            vertex.color = (Color32)(gradient.Evaluate(source.Position) * (Color)vertex.color);
            return vertex;
        }
    }

    internal static class KeyViewerTypography
    {
        private static readonly Dictionary<string, TMP_FontAsset> FileAssets =
            new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, TMP_FontAsset> SystemAssets =
            new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Font> SourceFonts = new List<Font>();
        private static bool fontBudgetWarning;

        internal static TMP_FontAsset Resolve(string family, string filePath)
        {
            if (string.IsNullOrWhiteSpace(family) && string.IsNullOrWhiteSpace(filePath))
                return TMP_Settings.defaultFontAsset;

            bool useFileKey = !string.IsNullOrWhiteSpace(filePath);
            Dictionary<string, TMP_FontAsset> assets = useFileKey ? FileAssets : SystemAssets;
            string key = useFileKey ? filePath : family;
            if (assets.TryGetValue(key, out TMP_FontAsset cached))
                return cached != null ? cached : TMP_Settings.defaultFontAsset;
            if (FileAssets.Count + SystemAssets.Count >= HemiAssetSafety.MaximumFontCacheEntries)
            {
                if (!fontBudgetWarning)
                {
                    fontBudgetWarning = true;
                    MelonLoader.MelonLogger.Warning("KeyViewer font cache reached its limit; existing fonts were retained.");
                }
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset asset = null;
            try
            {
                if (useFileKey && !HemiAssetSafety.IsFontPath(filePath))
                    throw new InvalidDataException("Font files must use a local TTF, OTF, or TTC path.");
                if (useFileKey && File.Exists(filePath))
                {
                    using (Stream input = HemiStreamSafety.OpenRead(filePath, HemiAssetSafety.MaximumFontBytes))
                    {
                        if (input.Length == 0)
                            throw new InvalidDataException("The font file is empty.");
                        asset = TMP_FontAsset.CreateFontAsset(filePath, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(family))
                {
                    Font source = Font.CreateDynamicFontFromOSFont(family, 90);
                    if (source != null)
                    {
                        SourceFonts.Add(source);
                        asset = TMP_FontAsset.CreateFontAsset(source);
                    }
                }
                if (asset != null)
                {
                    asset.name = "HemiTweaks KeyViewer - " + (!string.IsNullOrWhiteSpace(family)
                        ? family
                        : Path.GetFileNameWithoutExtension(filePath));
                    asset.isMultiAtlasTexturesEnabled = true;
                }
            }
            catch (Exception exception)
            {
                string source = useFileKey ? "file:" + key : "system:" + key;
                MelonLoader.MelonLogger.Warning("Failed to load KeyViewer font '" + source + "': " + exception.Message);
            }

            assets[key] = asset;
            return asset != null ? asset : TMP_Settings.defaultFontAsset;
        }

        internal static void ApplyStyle(TextMeshProUGUI text, int weight, bool italic, bool underline, bool strike)
        {
            FontStyles style = FontStyles.Normal;
            if (weight >= 600) style |= FontStyles.Bold;
            if (italic) style |= FontStyles.Italic;
            if (underline) style |= FontStyles.Underline;
            if (strike) style |= FontStyles.Strikethrough;
            if (text.fontStyle != style)
                text.fontStyle = style;
        }

        internal static void Clear()
        {
            HashSet<int> destroyed = new HashSet<int>();
            DestroyAssets(FileAssets, destroyed);
            DestroyAssets(SystemAssets, destroyed);

            for (int i = 0; i < SourceFonts.Count; i++)
            {
                Font source = SourceFonts[i];
                if (source != null && destroyed.Add(source.GetInstanceID()))
                    UnityEngine.Object.Destroy(source);
            }
            SourceFonts.Clear();
            fontBudgetWarning = false;
        }

        private static void DestroyAssets(Dictionary<string, TMP_FontAsset> assets, HashSet<int> destroyed)
        {
            foreach (TMP_FontAsset asset in assets.Values)
            {
                if (asset == null)
                    continue;
                Material material = asset.material;
                Texture2D[] atlases = asset.atlasTextures;
                if (material != null && destroyed.Add(material.GetInstanceID()))
                    UnityEngine.Object.Destroy(material);
                if (atlases != null)
                {
                    for (int i = 0; i < atlases.Length; i++)
                    {
                        Texture2D atlas = atlases[i];
                        if (atlas != null && destroyed.Add(atlas.GetInstanceID()))
                            UnityEngine.Object.Destroy(atlas);
                    }
                }
                if (destroyed.Add(asset.GetInstanceID()))
                    UnityEngine.Object.Destroy(asset);
            }
            assets.Clear();
        }
    }

    internal sealed class KeyViewerGraphGraphic : MaskableGraphic
    {
        private const int MaximumSamples = 256;

        private float[] samples;
        private float[] resizeBuffer;
        private int sampleHead;
        private int sampleCount;
        private KeyViewerGraphType graphType;
        private bool showAverage;
        private float average;
        private float maximum = 1f;
        private float valueSum;
        private int valueCount;
        private bool animateSamples;

        internal void Configure(KeyViewerGraphType type, Color graphColor, bool showAvg, bool animate)
        {
            bool colorChanged = !color.Equals(graphColor);
            if (graphType == type && !colorChanged && showAverage == showAvg && animateSamples == animate)
                return;

            graphType = type;
            showAverage = showAvg;
            animateSamples = animate;
            if (colorChanged)
                color = graphColor;
            else
                SetVerticesDirty();
        }

        internal void Push(float value, int maximumSamples)
        {
            float raw = Mathf.Max(0f, value);
            maximum = Mathf.Max(maximum, raw);
            if (raw > 0f)
            {
                valueSum += raw;
                valueCount++;
                average = Mathf.Round(valueSum / valueCount);
            }

            int limit = Mathf.Clamp(maximumSamples, 8, MaximumSamples);
            ResizeSamples(limit);

            float next = raw;
            if (animateSamples && sampleCount > 0)
                next = Mathf.Lerp(SampleAt(sampleCount - 1), next, 0.55f);
            samples[sampleHead] = next;
            sampleHead = (sampleHead + 1) % sampleCount;
            SetVerticesDirty();
        }

        internal void ResetHistory()
        {
            sampleHead = 0;
            sampleCount = 0;
            maximum = 1f;
            valueSum = 0f;
            valueCount = 0;
            average = 0f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect area = rectTransform.rect;
            if (sampleCount == 0 || area.width <= 1f || area.height <= 1f)
                return;

            float scaleMaximum = Mathf.Max(maximum, average);
            float step = sampleCount > 1 ? area.width / (sampleCount - 1) : area.width;

            if (graphType == KeyViewerGraphType.Bar)
            {
                float barWidth = Mathf.Max(1f, area.width / Mathf.Max(1, sampleCount) - 1f);
                for (int i = 0; i < sampleCount; i++)
                {
                    float x = area.xMin + (i + 0.5f) * area.width / sampleCount;
                    float height = SampleAt(i) / scaleMaximum * area.height;
                    AddQuad(vh, new Rect(x - barWidth * 0.5f, area.yMin, barWidth, height), color);
                }
            }
            else
            {
                for (int i = 1; i < sampleCount; i++)
                {
                    Vector2 a = new Vector2(area.xMin + (i - 1) * step, area.yMin + SampleAt(i - 1) / scaleMaximum * area.height);
                    Vector2 b = new Vector2(area.xMin + i * step, area.yMin + SampleAt(i) / scaleMaximum * area.height);
                    AddLine(vh, a, b, 2f, color);
                }
            }

            if (showAverage && average > 0f)
            {
                float y = area.yMin + average / scaleMaximum * area.height;
                AddQuad(vh, new Rect(area.xMin, y - 0.5f, area.width, 1f), new Color(1f, 1f, 1f, 0.55f));
            }
        }

        private void ResizeSamples(int limit)
        {
            if (sampleCount == limit)
                return;

            EnsureBuffers();
            Array.Clear(resizeBuffer, 0, limit);
            int keep = Mathf.Min(sampleCount, limit);
            int source = sampleCount - keep;
            int destination = limit - keep;
            for (int i = 0; i < keep; i++)
                resizeBuffer[destination + i] = SampleAt(source + i);

            float[] swap = samples;
            samples = resizeBuffer;
            resizeBuffer = swap;
            sampleHead = 0;
            sampleCount = limit;
        }

        private void EnsureBuffers()
        {
            if (samples != null)
                return;

            samples = new float[MaximumSamples];
            resizeBuffer = new float[MaximumSamples];
        }

        private float SampleAt(int index)
        {
            return samples[(sampleHead + index) % sampleCount];
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 normal = new Vector2(a.y - b.y, b.x - a.x).normalized * width * 0.5f;
            int start = vh.currentVertCount;
            vh.AddVert(a + normal, tint, Vector2.zero);
            vh.AddVert(b + normal, tint, Vector2.zero);
            vh.AddVert(b - normal, tint, Vector2.zero);
            vh.AddVert(a - normal, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddQuad(VertexHelper vh, Rect rect, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }

    internal static class KeyViewerCubicBezier
    {
        internal static float Evaluate(Vector4 curve, float time)
        {
            float x = Mathf.Clamp01(time);
            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 10; i++)
            {
                float t = (low + high) * 0.5f;
                float bx = Cubic(t, curve.x, curve.z);
                if (bx < x) low = t; else high = t;
            }
            return Cubic((low + high) * 0.5f, curve.y, curve.w);
        }

        private static float Cubic(float t, float p1, float p2)
        {
            float inverse = 1f - t;
            return 3f * inverse * inverse * t * p1 + 3f * inverse * t * t * p2 + t * t * t;
        }
    }
}
