using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HemiTweaks.Interface
{
    internal struct HemiColors
    {
        public Color Backdrop;

        public Color Window;

        public Color Header;

        public Color Panel;

        public Color Card;

        public Color Field;

        public Color Line;

        public Color Border;

        public Color BorderStrong;

        public Color Text;

        public Color Muted;

        public Color Faint;

        public Color Chip;

        public Color Button;

        public Color Accent;
        public Color AccentSoft;
        public Color Positive;
        public Color Negative;
    }

    internal static class HemiTheme
    {
        internal const float Radius = 10f;

        internal const float RowHeight = 44f;

        internal const float CardRadius = 16f;
        internal const float Gap = 8f;

        private static TMP_FontAsset regular;
        private static TMP_FontAsset gameBold;
        private static readonly HemiColors DarkColors = Dark();
        private static readonly HemiColors LightColors = Light();

        internal static HemiColors Colors => HemiTweaksMod.IsDarkMode ? DarkColors : LightColors;

        internal static float Scale => Mathf.Clamp(HemiTweaksMod.UIFontSize / 16f, 0.6f, 3f);

        internal static float Font(float baseSize)
        {
            return Mathf.Clamp(baseSize * Scale, 8f, 120f);
        }

        internal static float Row(float baseHeight)
        {
            return baseHeight * Mathf.Max(1f, Scale);
        }

        internal static float Col(float baseWidth)
        {
            return Row(baseWidth);
        }

        internal static TMP_FontAsset Regular
        {
            get
            {
                if (regular == null)
                    regular = Load("HemiTweaks.Resources.NotoSansKR.Regular.ttf", "NotoSansKR-Regular.ttf", "Hemi Regular");
                return regular != null ? regular : TMP_Settings.defaultFontAsset;
            }
        }

        internal static TMP_FontAsset Bold => Regular;

        internal static TMP_FontAsset GameBold
        {
            get
            {
                if (gameBold == null)
                    gameBold = Load("HemiTweaks.Resources.MapleStory.Bold.otf", "Maplestory OTF Bold.otf", "Hemi Game Bold");
                return gameBold != null ? gameBold : Regular;
            }
        }

        internal static void Dispose()
        {
            if (regular != null && regular != TMP_Settings.defaultFontAsset)
                UnityEngine.Object.Destroy(regular);
            if (gameBold != null && gameBold != regular && gameBold != TMP_Settings.defaultFontAsset)
                UnityEngine.Object.Destroy(gameBold);
            regular = null;
            gameBold = null;
            HemiSprites.Dispose();
        }

        private static HemiColors Dark()
        {
            return new HemiColors
            {
                Backdrop = new Color(0f, 0f, 0f, 0.45f),
                Window = Hex(0x1C1C22, 0.72f),
                Header = Hex(0x222229, 0.88f),
                Panel = Hex(0x121218, 0.55f),
                Card = new Color(1f, 1f, 1f, 0.055f),
                Field = new Color(0f, 0f, 0f, 0.28f),
                Line = new Color(1f, 1f, 1f, 0.06f),
                Border = new Color(1f, 1f, 1f, 0.07f),
                BorderStrong = new Color(1f, 1f, 1f, 0.12f),
                Text = Hex(0xF5F5F7),
                Muted = Hex(0xEBEBF5, 0.60f),
                Faint = Hex(0xEBEBF5, 0.38f),
                Chip = new Color(1f, 1f, 1f, 0.14f),
                Button = new Color(1f, 1f, 1f, 0.08f),
                Accent = Hex(0x0A84FF),
                AccentSoft = Hex(0x0A84FF, 0.24f),
                Positive = Hex(0x32D74B),
                Negative = Hex(0xFF453A)
            };
        }

        private static HemiColors Light()
        {
            return new HemiColors
            {
                Backdrop = new Color(0f, 0f, 0f, 0.30f),
                Window = Hex(0xF2F2F7, 0.88f),
                Header = Hex(0xFFFFFF, 0.92f),
                Panel = Hex(0xE9E9EF, 0.60f),
                Card = new Color(0f, 0f, 0f, 0.045f),
                Field = new Color(0f, 0f, 0f, 0.06f),
                Line = new Color(0f, 0f, 0f, 0.08f),
                Border = new Color(0f, 0f, 0f, 0.08f),
                BorderStrong = new Color(0f, 0f, 0f, 0.12f),
                Text = Hex(0x1D1D1F),
                Muted = Hex(0x3C3C43, 0.60f),
                Faint = Hex(0x3C3C43, 0.38f),
                Chip = new Color(1f, 1f, 1f, 0.95f),
                Button = new Color(0f, 0f, 0f, 0.05f),
                Accent = Hex(0x007AFF),
                AccentSoft = Hex(0x007AFF, 0.18f),
                Positive = Hex(0x28A745),
                Negative = Hex(0xE0342B)
            };
        }

        internal static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha);
        }

        private static TMP_FontAsset Load(string resource, string fileName, string assetName)
        {
            try
            {
                string directory = Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, "Fonts");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, fileName);

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    Assembly assembly = typeof(HemiTheme).Assembly;
                    using (Stream input = assembly.GetManifestResourceStream(resource))
                    {
                        if (input == null)
                            return null;
                        using (FileStream output = File.Create(path))
                            input.CopyTo(output);
                    }
                }

                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (asset == null)
                    return null;

                asset.name = assetName;
                asset.hideFlags = HideFlags.HideAndDontSave;
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                UnityEngine.Object.DontDestroyOnLoad(asset);
                return asset;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Interface font could not be loaded (" + assetName + "): " + exception.Message);
                return null;
            }
        }
    }

    internal enum HemiEase
    {
        Linear,
        OutQuad,
        OutCubic,
        InOutQuad,

        Spring,

        SpringGentle,

        SpringSoft,

        SpringBouncy
    }

    internal static class HemiTween
    {
        private sealed class Entry
        {
            public object Owner;
            public string Channel;
            public float From;
            public float To;
            public float Duration;
            public float Elapsed;
            public HemiEase Ease;
            public Action<float> Step;
            public Action Done;
            public bool Dead;
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly List<Entry> buffer = new List<Entry>();

        internal static void To(
            object owner,
            string channel,
            float from,
            float to,
            float duration,
            HemiEase ease,
            Action<float> step,
            Action done = null)
        {
            if (step == null)
                return;

            Kill(owner, channel);

            if (duration <= 0.0001f)
            {
                step(to);
                done?.Invoke();
                return;
            }

            entries.Add(new Entry
            {
                Owner = owner,
                Channel = channel,
                From = from,
                To = to,
                Duration = duration,
                Ease = ease,
                Step = step,
                Done = done
            });
            step(from);
        }

        internal static void Kill(object owner, string channel)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Owner == owner && string.Equals(entry.Channel, channel, StringComparison.Ordinal))
                    entry.Dead = true;
            }
        }

        internal static void KillAll(object owner)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Owner == owner)
                    entries[i].Dead = true;
            }
        }

        internal static void Clear()
        {
            entries.Clear();

            buffer.Clear();
        }

        internal static void Tick(float unscaledDeltaTime)
        {
            if (entries.Count == 0)
                return;

            buffer.Clear();
            buffer.AddRange(entries);

            for (int i = 0; i < buffer.Count; i++)
            {
                Entry entry = buffer[i];
                if (entry.Dead)
                    continue;

                if (entry.Owner is UnityEngine.Object unityOwner && unityOwner == null)
                {
                    entry.Dead = true;
                    continue;
                }

                entry.Elapsed += unscaledDeltaTime;
                float t = Mathf.Clamp01(entry.Elapsed / entry.Duration);
                entry.Step(Mathf.LerpUnclamped(entry.From, entry.To, Evaluate(entry.Ease, t)));

                if (t < 1f)
                    continue;

                entry.Dead = true;
                entry.Done?.Invoke();
            }

            entries.RemoveAll(entry => entry.Dead);

            buffer.Clear();
        }

        private static float Evaluate(HemiEase ease, float t)
        {
            switch (ease)
            {
                case HemiEase.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case HemiEase.OutCubic:
                {
                    float inverted = 1f - t;
                    return 1f - inverted * inverted * inverted;
                }
                case HemiEase.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case HemiEase.Spring:
                {
                    const float a = 8f;
                    return 1f - (1f + a * t) * Mathf.Exp(-a * t);
                }
                case HemiEase.SpringGentle:
                    return Underdamped(t, 0.85f);
                case HemiEase.SpringSoft:
                    return Underdamped(t, 0.8f);
                case HemiEase.SpringBouncy:
                    return Underdamped(t, 0.75f);
                default:
                    return t;
            }
        }

        private static float Underdamped(float t, float damping)
        {
            const float decay = 6f;
            float b = decay * Mathf.Sqrt(1f - damping * damping) / damping;
            return 1f - Mathf.Exp(-decay * t) * (Mathf.Cos(b * t) + decay / b * Mathf.Sin(b * t));
        }
    }
}
