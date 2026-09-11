using System;
using System.Globalization;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HemiTweaks
{
    internal static class JudgementCounter
    {
        private const float PollInterval = 0.5f;

        private enum Shade
        {
            TooEarly,
            VeryEarly,
            EarlyPerfect,
            Perfect,
            XPerfect,
            LatePerfect,
            VeryLate,
            TooLate,
            Fail
        }

        private readonly struct Slot
        {
            internal Slot(Shade shade, params HitMargin[] margins)
            {
                Shade = shade;
                Margins = margins;
            }

            internal Shade Shade { get; }

            internal HitMargin[] Margins { get; }
        }

        private static readonly Slot[] Merged =
        {
            new Slot(Shade.Fail, HitMargin.FailOverload),
            new Slot(Shade.TooEarly, HitMargin.TooEarly),
            new Slot(Shade.VeryEarly, HitMargin.VeryEarly),
            new Slot(Shade.EarlyPerfect, HitMargin.EarlyPerfect),
            new Slot(Shade.Perfect, HitMargin.PerfectMinus, HitMargin.XPerfect, HitMargin.PerfectPlus),
            new Slot(Shade.LatePerfect, HitMargin.LatePerfect),
            new Slot(Shade.VeryLate, HitMargin.VeryLate),
            new Slot(Shade.TooLate, HitMargin.TooLate),
            new Slot(Shade.Fail, HitMargin.FailMiss)
        };

        private static readonly Slot[] Split =
        {
            new Slot(Shade.Fail, HitMargin.FailOverload),
            new Slot(Shade.TooEarly, HitMargin.TooEarly),
            new Slot(Shade.VeryEarly, HitMargin.VeryEarly),
            new Slot(Shade.EarlyPerfect, HitMargin.EarlyPerfect),
            new Slot(Shade.Perfect, HitMargin.PerfectMinus),
            new Slot(Shade.XPerfect, HitMargin.XPerfect),
            new Slot(Shade.Perfect, HitMargin.PerfectPlus),
            new Slot(Shade.LatePerfect, HitMargin.LatePerfect),
            new Slot(Shade.VeryLate, HitMargin.VeryLate),
            new Slot(Shade.TooLate, HitMargin.TooLate),
            new Slot(Shade.Fail, HitMargin.FailMiss)
        };

        private static readonly string[] FallbackHex =
        {
            "FF0000FF",
            "FF6F4EFF",
            "A0FF4EFF",
            "60FF4EFF",
            "FFFFFFFF",
            "A0FF4EFF",
            "FF6F4EFF",
            "FF0000FF",
            "D958FFFF"
        };

        private static readonly string[] hex = (string[])FallbackHex.Clone();
        private static readonly Color32[] paletteColors = new Color32[FallbackHex.Length];
        private static readonly bool[] paletteColorsKnown = new bool[FallbackHex.Length];

        private const string WhiteHex = "FFFFFFFF";

        private const string GradientName = "HEMITWEAKSXPERFECT";

        private static readonly Color32 GradientTop = new Color32(0xFC, 0xB9, 0xBA, 0xFF);

        private static readonly Color32 GradientBottom = new Color32(0x9A, 0xCE, 0xFF, 0xFF);

        private const int GradientProbeLimit = 20;

        private static bool gradientReady;
        private static bool gradientChecked;
        private static int gradientProbes;

        private static int[] lastCounts = new int[0];
        private static string cached = "";
        private static bool splitPerfect;
        private static bool lastSplit;
        private static bool lastGradient;
        private static int paletteRevision;
        private static int lastPaletteRevision = -1;
        private static float nextPoll;

        internal static bool IsSplit
        {
            get
            {
                Poll();
                return splitPerfect;
            }
        }

        internal static string Format()
        {
            Poll();

            bool split = splitPerfect;
            Slot[] slots = split ? Split : Merged;

            if (lastCounts.Length != slots.Length)
                lastCounts = new int[slots.Length];

            bool gradient = gradientReady
                && string.Equals(hex[(int)Shade.XPerfect], WhiteHex, StringComparison.Ordinal);

            int[] counts = Counts();
            bool changed = split != lastSplit
                || paletteRevision != lastPaletteRevision
                || gradient != lastGradient;
            for (int i = 0; i < slots.Length; i++)
            {
                int count = Count(counts, slots[i]);
                if (lastCounts[i] == count)
                    continue;
                lastCounts[i] = count;
                changed = true;
            }

            if (!changed)
                return cached;

            lastSplit = split;
            lastPaletteRevision = paletteRevision;
            lastGradient = gradient;

            StringBuilder builder = new StringBuilder(slots.Length * 24);
            for (int i = 0; i < slots.Length; i++)
            {
                if (i > 0)
                    builder.Append(' ');

                string count = lastCounts[i].ToString(CultureInfo.InvariantCulture);
                if (gradient && slots[i].Shade == Shade.XPerfect)
                {
                    builder.Append("<gradient=\"").Append(GradientName).Append("\">")
                        .Append(count).Append("</gradient>");
                    continue;
                }

                builder
                    .Append("<color=#").Append(hex[(int)slots[i].Shade]).Append('>')
                    .Append(count)
                    .Append("</color>");
            }

            cached = builder.ToString();
            return cached;
        }

        internal static string Describe()
        {
            return IsSplit
                ? "Absolute perfect is on, so -Perfect, XPerfect and +Perfect are counted separately."
                : "Absolute perfect is off, so the three perfect judgements are counted as one.";
        }

        internal static void Reset()
        {
            cached = "";
            lastCounts = new int[0];
            lastPaletteRevision = -1;
            nextPoll = 0f;
        }

        private static void EnsureGradient()
        {
            if (gradientChecked)
                return;

            try
            {
                int hash = TagHash(GradientName);
                if (!MaterialReferenceManager.TryGetColorGradientPreset(hash, out TMP_ColorGradient preset) || preset == null)
                {
                    preset = ScriptableObject.CreateInstance<TMP_ColorGradient>();
                    preset.name = GradientName;
                    preset.colorMode = ColorMode.VerticalGradient;
                    preset.topLeft = GradientTop;
                    preset.topRight = GradientTop;
                    preset.bottomLeft = GradientBottom;
                    preset.bottomRight = GradientBottom;
                    preset.hideFlags = HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(preset);
                    MaterialReferenceManager.AddColorGradientPreset(hash, preset);
                }

                bool? parsed = ProbeGradientTag(out int characters);

                if (parsed == null && hash == TMP_TextUtilities.GetSimpleHashCode(GradientName))
                    parsed = true;

                if (parsed == null)
                {
                    if (++gradientProbes < GradientProbeLimit)
                        return;
                    gradientChecked = true;
                    gradientReady = false;
                    return;
                }

                gradientChecked = true;
                gradientReady = parsed.Value;
                if (!gradientReady)
                {
                    MelonLoader.MelonLogger.Warning(
                        "TextMeshPro did not resolve the judgement counter's gradient tag (laid out " +
                        characters.ToString(CultureInfo.InvariantCulture) +
                        " characters, expected 1); XPerfect falls back to flat white.");
                }
            }
            catch (Exception exception)
            {
                gradientChecked = true;
                gradientReady = false;
                MelonLoader.MelonLogger.Warning(
                    "Judgement counter falls back to flat white for XPerfect: " + exception.Message);
            }
        }

        private static bool? ProbeGradientTag(out int characters)
        {
            characters = 0;
            GameObject probe = null;
            try
            {
                probe = new GameObject("HemiGradientProbe");
                probe.hideFlags = HideFlags.HideAndDontSave;

                TextMeshPro text = probe.AddComponent<TextMeshPro>();
                TMP_FontAsset font = StateOverlay.GetFontAsset();
                if (font != null)
                    text.font = font;
                if (text.font == null)
                    return null;

                text.richText = true;

                TMP_TextInfo info = text.GetTextInfo("<gradient=\"" + GradientName + "\">0</gradient>");
                if (info == null)
                    return null;

                characters = info.characterCount;
                if (characters == 0)
                    return null;
                return characters == 1;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (probe != null)
                    UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private static int TagHash(string name)
        {
            int hash = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c >= 'a' && c <= 'z')
                    c = (char)(c - 32);
                hash = ((hash << 5) + hash) ^ c;
            }
            return hash;
        }

        private static void Poll()
        {
            float now = Time.unscaledTime;
            if (now < nextPoll)
                return;
            nextPoll = now + PollInterval;

            EnsureGradient();

            try
            {
                splitPerfect = HitMarginHelper.IsShowXPerfect(Persistence.hitMarginPerfectText);
            }
            catch
            {
                splitPerfect = false;
            }

            try
            {
                ColourSchemeHitMargin scheme = RDC.hitMarginColoursBySettings;
                if (scheme == null)
                    return;

                Adopt(Shade.TooEarly, scheme.SelectByHitMargin(HitMargin.TooEarly));
                Adopt(Shade.VeryEarly, scheme.SelectByHitMargin(HitMargin.VeryEarly));
                Adopt(Shade.EarlyPerfect, scheme.SelectByHitMargin(HitMargin.EarlyPerfect));
                Adopt(Shade.Perfect, scheme.SelectByHitMargin(HitMargin.PerfectMinus));
                Adopt(Shade.XPerfect, scheme.SelectByHitMargin(HitMargin.XPerfect));
                Adopt(Shade.LatePerfect, scheme.SelectByHitMargin(HitMargin.LatePerfect));
                Adopt(Shade.VeryLate, scheme.SelectByHitMargin(HitMargin.VeryLate));
                Adopt(Shade.TooLate, scheme.SelectByHitMargin(HitMargin.TooLate));
                Adopt(Shade.Fail, scheme.SelectByHitMargin(HitMargin.FailMiss));
            }
            catch
            {
            }
        }

        private static void Adopt(Shade shade, Color color)
        {
            int index = (int)shade;
            Color32 quantized = color;
            if (paletteColorsKnown[index] && paletteColors[index].Equals(quantized))
                return;

            paletteColors[index] = quantized;
            paletteColorsKnown[index] = true;
            string value = ColorUtility.ToHtmlStringRGBA(color);
            if (string.Equals(hex[index], value, StringComparison.Ordinal))
                return;
            hex[index] = value;
            paletteRevision++;
        }

        private static int[] Counts()
        {
            try
            {
                return ADOBase.controller?.playerOne?.marginTracker?.hitMarginsCount;
            }
            catch
            {
                return null;
            }
        }

        private static int Count(int[] counts, Slot slot)
        {
            if (counts == null)
                return 0;

            int total = 0;
            for (int i = 0; i < slot.Margins.Length; i++)
            {
                int index = (int)slot.Margins[i];
                if (index >= 0 && index < counts.Length)
                    total += counts[index];
            }
            return total;
        }
    }

    internal static class HitTiming
    {
        private static double last;
        private static double total;
        private static int count;

        internal static float Last => (float)last;

        internal static float Average => count == 0 ? 0f : (float)(total / count);

        internal static event Action<double, HitMargin> Recorded;

        internal static void Rewind()
        {
            last = 0.0;
            total = 0.0;
            count = 0;
        }

        private static void Record(scrPlanet planet, scrFloor hitFloor, HitMargin margin)
        {
            if (planet == null || hitFloor == null)
                return;

            scrConductor conductor = planet.conductor;
            if (conductor == null || conductor.song == null)
                return;

            double divisor = Math.PI * conductor.bpm * hitFloor.speed * conductor.song.pitch;
            if (Math.Abs(divisor) < 0.000001)
                return;

            double milliseconds =
                (planet.cachedAngle - planet.targetExitAngle) * (hitFloor.isCCW ? -1.0 : 1.0) * 60000.0 / divisor;

            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds))
                return;

            last = milliseconds;
            total += milliseconds;
            count++;
            Recorded?.Invoke(milliseconds, margin);
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.UpdateHitErrorMeter))]
        private static class ScrControllerUpdateHitErrorMeterPatch
        {
            private static void Postfix(
                scrController __instance, scrFloor hitFloor, scrPlayer player, scrPlanet priorChosenPlanet)
            {
                try
                {
                    if (__instance == null || !__instance.gameworld || hitFloor == null || player == null)
                        return;

                    if (player.midspinInfiniteMargin)
                        return;

                    HitMargin margin = player.marginTracker?.lastAddedHitMargin ?? HitMargin.XPerfect;
                    if ((margin == HitMargin.Auto || player.auto) && !RDC.useOldAuto)
                        return;

                    Record(priorChosenPlanet, hitFloor, margin);
                }
                catch
                {
                }
            }
        }
    }
}
