using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using MelonLoader;
using TMPro;
using UnityEngine;

namespace HemiTweaks
{
    internal enum ComboKind
    {
        Perfect,

        XCombo,

        Combo
    }

    internal static class ComboCounter
    {
        private enum Verdict
        {
            Continue,

            Break,

            Ignore
        }

        private static List<HitMargin> tracked;
        private static int consumed;
        private static int combo;
        private static ComboKind countedKind;

        internal static int Value => combo;

        internal static void Poll(ComboKind kind)
        {
            List<HitMargin> hits;
            try
            {
                hits = ADOBase.controller?.playerOne?.marginTracker?.hitMargins;
            }
            catch
            {
                hits = null;
            }

            if (hits == null)
            {
                tracked = null;
                consumed = 0;
                combo = 0;
                return;
            }

            if (!ReferenceEquals(hits, tracked) || hits.Count < consumed || kind != countedKind)
            {
                tracked = hits;
                countedKind = kind;
                consumed = hits.Count;
                combo = CountBack(hits, kind);
                return;
            }

            for (int i = consumed; i < hits.Count; i++)
            {
                switch (Classify(hits[i], kind))
                {
                    case Verdict.Continue:
                        combo++;
                        break;
                    case Verdict.Break:
                        combo = 0;
                        break;
                }
            }

            consumed = hits.Count;
        }

        internal static void Reset()
        {
            tracked = null;
            consumed = 0;
            combo = 0;
        }

        private static int CountBack(List<HitMargin> hits, ComboKind kind)
        {
            int run = 0;
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                Verdict verdict = Classify(hits[i], kind);
                if (verdict == Verdict.Break)
                    break;
                if (verdict == Verdict.Continue)
                    run++;
            }
            return run;
        }

        private static Verdict Classify(HitMargin hit, ComboKind kind)
        {
            switch (hit)
            {
                case HitMargin.XPerfect:
                    return Verdict.Continue;
                case HitMargin.PerfectMinus:
                case HitMargin.PerfectPlus:
                    return kind == ComboKind.XCombo ? Verdict.Break : Verdict.Continue;
                case HitMargin.EarlyPerfect:
                case HitMargin.LatePerfect:
                case HitMargin.VeryEarly:
                case HitMargin.VeryLate:
                    return kind == ComboKind.Combo ? Verdict.Continue : Verdict.Break;
                case HitMargin.Auto:
                case HitMargin.Midspin:
                    return Verdict.Ignore;
                default:
                    return Verdict.Break;
            }
        }
    }

    internal static class ComboOverlay
    {
        internal const int MinimumFontSize = 8;
        internal const int MaximumFontSize = 160;

        private const string DefaultColorHex = "#FFFFFFFF";

        private const int CurrentDefaults = 2;

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<int> defaultsEntry;
        private static MelonPreferences_Entry<bool> enabledEntry;
        private static MelonPreferences_Entry<int> anchorEntry;
        private static MelonPreferences_Entry<float> offsetXEntry;
        private static MelonPreferences_Entry<float> offsetYEntry;
        private static MelonPreferences_Entry<int> fontSizeEntry;
        private static MelonPreferences_Entry<int> kindEntry;
        private static MelonPreferences_Entry<string> colorEntry;
        private static MelonPreferences_Entry<string> labelColorEntry;
        private static MelonPreferences_Entry<string> shadowColorEntry;
        private static MelonPreferences_Entry<float> shadowXEntry;
        private static MelonPreferences_Entry<float> shadowYEntry;
        private static MelonPreferences_Entry<float> pulseAmountEntry;
        private static MelonPreferences_Entry<float> pulseDurationEntry;
        private static MelonPreferences_Entry<string> pulseGrowEaseEntry;
        private static MelonPreferences_Entry<string> pulseReturnEaseEntry;
        private static MelonPreferences_Entry<bool> fadeEnabledEntry;
        private static MelonPreferences_Entry<float> fadeInEntry;
        private static MelonPreferences_Entry<float> fadeHoldEntry;
        private static MelonPreferences_Entry<float> fadeOutEntry;
        private static MelonPreferences_Entry<string> fadeInEaseEntry;
        private static MelonPreferences_Entry<string> fadeOutEaseEntry;
        private static GameObject behaviourObject;

        private static readonly HemiSaveDebounce writer = new HemiSaveDebounce(Write);

        internal static bool Enabled => enabledEntry != null && enabledEntry.Value;

        internal static StateAnchor Anchor => HemiBarPrefs.Anchor(anchorEntry);

        internal static float OffsetX => offsetXEntry == null ? 0f : offsetXEntry.Value;

        internal static float OffsetY => offsetYEntry == null ? 140f : offsetYEntry.Value;

        internal static int FontSize => fontSizeEntry == null
            ? 34
            : Mathf.Clamp(fontSizeEntry.Value, MinimumFontSize, MaximumFontSize);

        internal static ComboKind Kind
        {
            get
            {
                if (kindEntry == null)
                    return ComboKind.Perfect;

                int stored = kindEntry.Value;
                return stored >= (int)ComboKind.Perfect && stored <= (int)ComboKind.Combo
                    ? (ComboKind)stored
                    : ComboKind.Perfect;
            }
        }

        internal static string ColorHex => colorEntry == null ? DefaultColorHex : colorEntry.Value ?? "";

        internal static string LabelColorHex => labelColorEntry == null ? DefaultColorHex : labelColorEntry.Value ?? "";

        internal static string ShadowColorHex => shadowColorEntry == null ? "#00000080" : shadowColorEntry.Value ?? "";

        internal static float ShadowX => shadowXEntry == null ? 0.5f : shadowXEntry.Value;

        internal static float ShadowY => shadowYEntry == null ? -0.5f : shadowYEntry.Value;

        internal static float PulseAmount => pulseAmountEntry == null
            ? 0.35f
            : Mathf.Clamp(pulseAmountEntry.Value, 0f, 2f);

        internal static float PulseDuration => pulseDurationEntry == null
            ? 0.25f
            : Mathf.Clamp(pulseDurationEntry.Value, 0f, 2f);

        internal static Ease PulseGrowEase => HemiEases.Parse(pulseGrowEaseEntry?.Value, Ease.OutExpo);
        internal static Ease PulseReturnEase => HemiEases.Parse(pulseReturnEaseEntry?.Value, Ease.Linear);
        internal static Ease FadeInEase => HemiEases.Parse(fadeInEaseEntry?.Value, Ease.Linear);
        internal static Ease FadeOutEase => HemiEases.Parse(fadeOutEaseEntry?.Value, Ease.Linear);

        internal static bool FadeEnabled => fadeEnabledEntry != null && fadeEnabledEntry.Value;

        internal static float FadeIn => fadeInEntry == null ? 0.15f : Mathf.Clamp(fadeInEntry.Value, 0f, 2f);

        internal static float FadeHold => fadeHoldEntry == null ? 1f : Mathf.Clamp(fadeHoldEntry.Value, 0f, 5f);

        internal static float FadeOut => fadeOutEntry == null ? 0.5f : Mathf.Clamp(fadeOutEntry.Value, 0f, 3f);

        internal static Color ResolveColor()
        {
            return StateGroupStore.ParseColor(ColorHex, Color.white);
        }

        internal static Color ResolveLabelColor()
        {
            return StateGroupStore.ParseColor(LabelColorHex, Color.white);
        }

        internal static string LabelKey(ComboKind kind)
        {
            switch (kind)
            {
                case ComboKind.XCombo:
                    return "BAR_COMBO_LABEL_X";
                case ComboKind.Perfect:
                    return "BAR_COMBO_LABEL_PERFECT";
                default:
                    return "BAR_COMBO_LABEL";
            }
        }

        internal static Color ResolveShadowColor()
        {
            return StateGroupStore.ParseColor(ShadowColorHex, new Color(0f, 0f, 0f, 0.5f));
        }

        internal static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            defaultsEntry = category.CreateEntry("ComboDefaults", 0, "Combo Defaults Version",
                "Which set of shipped defaults these settings were written against.");
            enabledEntry = category.CreateEntry("EnableCombo", false, "Enable Combo",
                "Shows how many perfect hits have landed in a row.");
            anchorEntry = category.CreateEntry("ComboAnchor", (int)StateAnchor.TopCenter, "Combo Anchor",
                "Which corner or edge of the screen the readout is pinned to.");
            offsetXEntry = category.CreateEntry("ComboOffsetX", 0f, "Combo X",
                "Distance inward from the anchored edge.");
            offsetYEntry = category.CreateEntry("ComboOffsetY", 140f, "Combo Y",
                "Distance inward from the anchored edge.");
            fontSizeEntry = category.CreateEntry("ComboFontSize", 34, "Combo Font Size", "");
            kindEntry = category.CreateEntry("ComboKind", (int)ComboKind.Perfect, "Combo Kind",
                "0 = the three perfects, 1 = absolute perfect only, 2 = every landed hit.");
            colorEntry = category.CreateEntry("ComboColor", DefaultColorHex, "Combo Color", "Colour of the number.");
            labelColorEntry = category.CreateEntry("ComboLabelColor", DefaultColorHex, "Combo Label Color", "Colour of the word above the number.");
            shadowColorEntry = category.CreateEntry("ComboShadowColor", "#00000080", "Combo Shadow Color", "");
            shadowXEntry = category.CreateEntry("ComboShadowX", 0.5f, "Combo Shadow X", "");
            shadowYEntry = category.CreateEntry("ComboShadowY", -0.5f, "Combo Shadow Y", "");
            pulseAmountEntry = category.CreateEntry("ComboPulseAmount", 0.35f, "Combo Pulse Amount",
                "How much bigger the number gets when it changes.");
            pulseDurationEntry = category.CreateEntry("ComboPulseDuration", 0.25f, "Combo Pulse Duration",
                "How long the pulse takes, in seconds.");
            pulseGrowEaseEntry = category.CreateEntry("ComboPulseGrowEase", "OutExpo", "Combo Grow Ease", "DOTween curve used while the number grows.");
            pulseReturnEaseEntry = category.CreateEntry("ComboPulseReturnEase", "Linear", "Combo Return Ease", "DOTween curve used while the number returns to its resting size.");
            fadeEnabledEntry = category.CreateEntry("ComboFadeEnabled", false, "Combo Fade On Change",
                "Hidden until the combo goes up; then fades in, holds, and fades out.");
            fadeInEntry = category.CreateEntry("ComboFadeIn", 0.15f, "Combo Fade In", "Seconds to fade in.");
            fadeHoldEntry = category.CreateEntry("ComboFadeHold", 1f, "Combo Fade Hold", "Seconds fully visible before fading out.");
            fadeOutEntry = category.CreateEntry("ComboFadeOut", 0.5f, "Combo Fade Out", "Seconds to fade out.");
            fadeInEaseEntry = category.CreateEntry("ComboFadeInEase", "Linear", "Combo Fade In Ease", "DOTween curve used while the number appears.");
            fadeOutEaseEntry = category.CreateEntry("ComboFadeOutEase", "Linear", "Combo Fade Out Ease", "DOTween curve used while the number disappears.");

            AdoptNewDefaults();

            if (Enabled)
                EnsureBehaviourObject();
        }

        internal static void AdoptNewDefaults()
        {
            if (defaultsEntry == null || defaultsEntry.Value >= CurrentDefaults)
                return;

            if (defaultsEntry.Value < 1
                && shadowXEntry != null && Mathf.Approximately(shadowXEntry.Value, 0f)
                && shadowYEntry != null && Mathf.Approximately(shadowYEntry.Value, 0f))
            {
                shadowXEntry.Value = 0.5f;
                shadowYEntry.Value = -0.5f;
            }

            if (defaultsEntry.Value < 2 && colorEntry != null && labelColorEntry != null
                && string.Equals(labelColorEntry.Value, DefaultColorHex, StringComparison.Ordinal))
                labelColorEntry.Value = colorEntry.Value;

            defaultsEntry.Value = CurrentDefaults;
            Save();
        }

        internal static void UpdateLifecycle()
        {
            writer.Tick();

            if (Enabled)
            {
                EnsureBehaviourObject();
            }
            else if (behaviourObject != null && behaviourObject.activeSelf)
            {
                behaviourObject.SetActive(false);

                ComboCounter.Reset();
            }
        }

        internal static void SetEnabled(bool value)
        {
            if (!HemiBarPrefs.Set(enabledEntry, value))
                return;

            UpdateLifecycle();
            Save();
        }

        internal static void SetAnchor(StateAnchor value)
        {
            if (HemiBarPrefs.Set(anchorEntry, (int)value))
                Save();
        }

        internal static void SetOffset(float x, float y)
        {
            if (offsetXEntry == null || offsetYEntry == null)
                return;

            offsetXEntry.Value = x;
            offsetYEntry.Value = y;
            Save();
        }

        internal static void SetFontSize(int value)
        {
            if (HemiBarPrefs.Set(fontSizeEntry, Mathf.Clamp(value, MinimumFontSize, MaximumFontSize)))
                Save();
        }

        internal static void SetKind(ComboKind value)
        {
            if (!HemiBarPrefs.Set(kindEntry, (int)value))
                return;

            ComboCounter.Reset();
            Save();
        }

        internal static void SetColor(Color value)
        {
            if (HemiBarPrefs.SetColor(colorEntry, value))
                Save();
        }

        internal static void SetLabelColor(Color value)
        {
            if (HemiBarPrefs.SetColor(labelColorEntry, value))
                Save();
        }

        internal static void SetShadowColor(Color value)
        {
            if (HemiBarPrefs.SetColor(shadowColorEntry, value))
                Save();
        }

        internal static void SetShadow(float x, float y)
        {
            if (shadowXEntry == null || shadowYEntry == null)
                return;

            shadowXEntry.Value = Mathf.Clamp(x, -HemiTextMaterial.MaximumShadowOffset, HemiTextMaterial.MaximumShadowOffset);
            shadowYEntry.Value = Mathf.Clamp(y, -HemiTextMaterial.MaximumShadowOffset, HemiTextMaterial.MaximumShadowOffset);
            Save();
        }

        internal static void SetPulse(float amount, float duration)
        {
            if (pulseAmountEntry == null || pulseDurationEntry == null)
                return;

            pulseAmountEntry.Value = Mathf.Clamp(amount, 0f, 2f);
            pulseDurationEntry.Value = Mathf.Clamp(duration, 0f, 2f);
            Save();
        }

        internal static void SetFadeEnabled(bool value)
        {
            if (HemiBarPrefs.Set(fadeEnabledEntry, value))
                Save();
        }

        internal static void SetFade(float fadeIn, float hold, float fadeOut)
        {
            if (fadeInEntry == null || fadeHoldEntry == null || fadeOutEntry == null)
                return;

            fadeInEntry.Value = Mathf.Clamp(fadeIn, 0f, 2f);
            fadeHoldEntry.Value = Mathf.Clamp(hold, 0f, 5f);
            fadeOutEntry.Value = Mathf.Clamp(fadeOut, 0f, 3f);
            Save();
        }

        internal static void SetPulseGrowEase(string value) => SetEase(pulseGrowEaseEntry, value);
        internal static void SetPulseReturnEase(string value) => SetEase(pulseReturnEaseEntry, value);
        internal static void SetFadeInEase(string value) => SetEase(fadeInEaseEntry, value);
        internal static void SetFadeOutEase(string value) => SetEase(fadeOutEaseEntry, value);

        private static void SetEase(MelonPreferences_Entry<string> entry, string value)
        {
            if (entry == null || entry.Value == value)
                return;
            if (HemiEases.IndexOf(value) < 0)
            {
                MelonLogger.Warning("Unknown Combo easing curve: " + value);
                return;
            }
            entry.Value = value;
            Save();
        }

        internal static void Shutdown()
        {
            writer.Flush();

            if (behaviourObject == null)
                return;

            UnityEngine.Object.Destroy(behaviourObject);
            behaviourObject = null;
        }

        private static void Save()
        {
            writer.Request();
        }

        private static void Write()
        {
            category?.SaveToFile(false);
        }

        private static void EnsureBehaviourObject()
        {
            HemiOverlayHost.Ensure<ComboBehaviour>(ref behaviourObject, "HemiTweaks_Combo");
        }
    }

    internal sealed class ComboBlock
    {
        private const float LabelShare = 0.62f;

        private const float LineHeight = 1.25f;

        private readonly HemiTextMaterialSlot materials = new HemiTextMaterialSlot();

        private RectTransform labelHolder;
        private RectTransform valueHolder;
        private CanvasGroup group;
        private TextMeshProUGUI label;
        private TextMeshProUGUI value;
        private int appliedFontRevision = -1;
        private int written = int.MinValue;
        private string writtenLabel;

        internal RectTransform Root { get; private set; }

        internal static ComboBlock Create(Transform parent)
        {
            ComboBlock block = new ComboBlock();

            block.Root = HemiOverlayObjects.New("Combo", parent);

            block.group = block.Root.gameObject.AddComponent<CanvasGroup>();
            block.group.blocksRaycasts = false;
            block.group.interactable = false;

            block.labelHolder = HemiOverlayObjects.New("Label", block.Root);
            block.labelHolder.anchorMin = new Vector2(0f, 1f);
            block.labelHolder.anchorMax = new Vector2(1f, 1f);
            block.labelHolder.pivot = new Vector2(0.5f, 1f);
            block.writtenLabel = Interface.HemiLang.Get(ComboOverlay.LabelKey(ComboOverlay.Kind));
            block.label = Text(block.labelHolder, block.writtenLabel);

            block.valueHolder = HemiOverlayObjects.New("Value", block.Root);
            block.valueHolder.anchorMin = new Vector2(0f, 1f);
            block.valueHolder.anchorMax = new Vector2(1f, 1f);
            block.valueHolder.pivot = new Vector2(0.5f, 0.5f);
            block.value = Text(block.valueHolder, "0");

            return block;
        }

        internal void SetValue(int count)
        {
            if (written == count)
                return;

            written = count;
            value.text = count.ToString(CultureInfo.InvariantCulture);
        }

        internal void SetPulse(float scale)
        {
            Vector3 next = new Vector3(scale, scale, 1f);
            if (!valueHolder.localScale.Equals(next))
                valueHolder.localScale = next;
        }

        internal void SetAlpha(float alpha)
        {
            if (group != null && !Mathf.Approximately(group.alpha, alpha))
                group.alpha = alpha;
        }

        internal void Destroy()
        {
            materials.Destroy();
        }

        internal void ApplyStyle()
        {
            string caption = Interface.HemiLang.Get(ComboOverlay.LabelKey(ComboOverlay.Kind));
            if (!string.Equals(writtenLabel, caption, StringComparison.Ordinal))
            {
                writtenLabel = caption;
                label.text = caption;
            }

            float size = ComboOverlay.FontSize;
            float labelSize = size * LabelShare;
            if (label.fontSize != labelSize)
                label.fontSize = labelSize;
            if (value.fontSize != size)
                value.fontSize = size;

            Color labelColor = ComboOverlay.ResolveLabelColor();
            if (!label.color.Equals(labelColor))
                label.color = labelColor;
            Color color = ComboOverlay.ResolveColor();
            if (!value.color.Equals(color))
                value.color = color;

            TMP_FontAsset font = StateOverlay.GetFontAsset();
            if (appliedFontRevision != StateOverlay.FontRevision)
            {
                appliedFontRevision = StateOverlay.FontRevision;
                if (font != null)
                {
                    label.font = font;
                    value.font = font;
                }
            }

            Material material = materials.Resolve(
                font, ComboOverlay.ResolveShadowColor(), ComboOverlay.ShadowX, ComboOverlay.ShadowY, out bool rewritten);
            if (material != null)
            {
                Assign(label, material, rewritten);
                Assign(value, material, rewritten);
            }

            Layout(size);
        }

        private string layoutLabelText;
        private string layoutValueText;
        private float layoutSize = -1f;
        private int layoutFontId;
        private float layoutWidth;

        private void Layout(float size)
        {
            float labelHeight = size * LabelShare * LineHeight;
            float valueHeight = size * LineHeight;

            int fontId = label.font == null ? 0 : label.font.GetInstanceID();
            if (layoutSize != size
                || layoutFontId != fontId
                || !string.Equals(layoutLabelText, label.text, StringComparison.Ordinal)
                || !string.Equals(layoutValueText, value.text, StringComparison.Ordinal))
            {
                layoutSize = size;
                layoutFontId = fontId;
                layoutLabelText = label.text;
                layoutValueText = value.text;
                layoutWidth = Mathf.Max(label.preferredWidth, value.preferredWidth);
            }

            Vector2 rootSize = new Vector2(layoutWidth, labelHeight + valueHeight);
            if (!Root.sizeDelta.Equals(rootSize))
                Root.sizeDelta = rootSize;

            Vector2 labelSize = new Vector2(0f, labelHeight);
            if (!labelHolder.sizeDelta.Equals(labelSize))
                labelHolder.sizeDelta = labelSize;
            if (!labelHolder.anchoredPosition.Equals(Vector2.zero))
                labelHolder.anchoredPosition = Vector2.zero;

            Vector2 valueSize = new Vector2(0f, valueHeight);
            if (!valueHolder.sizeDelta.Equals(valueSize))
                valueHolder.sizeDelta = valueSize;
            Vector2 valuePosition = new Vector2(0f, -labelHeight - valueHeight * 0.5f);
            if (!valueHolder.anchoredPosition.Equals(valuePosition))
                valueHolder.anchoredPosition = valuePosition;
        }

        private static void Assign(TextMeshProUGUI text, Material material, bool rewritten)
        {
            if (text.fontSharedMaterial != material)
                text.fontSharedMaterial = material;
            else if (rewritten)
                text.UpdateMeshPadding();
        }

        private static TextMeshProUGUI Text(RectTransform parent, string content)
        {
            GameObject holder = new GameObject("Text");
            holder.transform.SetParent(parent, false);

            RectTransform rect = holder.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = holder.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }

    internal sealed class ComboPulse
    {
        private const float GrowShare = 0.3f;

        private float started = -1f;

        private float fromScale = 1f;

        internal void Start()
        {
            fromScale = Scale();
            started = ComboOverlay.PulseDuration > 0f && ComboOverlay.PulseAmount > 0f
                ? Time.unscaledTime
                : -1f;
        }

        internal void Clear()
        {
            started = -1f;
            fromScale = 1f;
        }

        internal float Scale()
        {
            float duration = ComboOverlay.PulseDuration;
            float amount = ComboOverlay.PulseAmount;
            if (started < 0f || duration <= 0f || amount <= 0f)
                return 1f;

            float elapsed = Time.unscaledTime - started;
            if (elapsed < 0f || elapsed >= duration)
            {
                started = -1f;
                return 1f;
            }

            float peak = 1f + amount;
            float growth = duration * GrowShare;
            if (elapsed <= growth)
            {
                float t = growth <= 0f ? 1f : elapsed / growth;
                return Interpolate(fromScale, peak, ComboOverlay.PulseGrowEase, t, amount);
            }

            float settle = duration - growth;
            return Interpolate(peak, 1f, ComboOverlay.PulseReturnEase,
                settle <= 0f ? 1f : (elapsed - growth) / settle, amount);
        }

        private static float Interpolate(float from, float to, Ease ease, float progress, float amount)
        {
            float scale = Mathf.LerpUnclamped(from, to, HemiEases.Evaluate(ease, progress));
            return Mathf.Clamp(scale, 0.05f, 1f + amount * 2f);
        }
    }

    internal sealed class ComboFade
    {
        private float started = -1f;
        private float from;
        private float current;

        internal void Clear()
        {
            started = -1f;
            current = 0f;
        }

        internal void Trigger()
        {
            if (!ComboOverlay.FadeEnabled)
                return;
            from = current;
            started = Time.unscaledTime;
        }

        internal float Alpha()
        {
            if (!ComboOverlay.FadeEnabled)
            {
                current = 1f;
                return 1f;
            }
            if (started < 0f)
            {
                current = 0f;
                return 0f;
            }

            float fadeIn = ComboOverlay.FadeIn;
            float hold = ComboOverlay.FadeHold;
            float fadeOut = ComboOverlay.FadeOut;
            float elapsed = Time.unscaledTime - started;

            float inTime = fadeIn * (1f - from);
            if (elapsed < inTime)
                current = Mathf.Lerp(from, 1f, HemiEases.Evaluate(ComboOverlay.FadeInEase, elapsed / inTime));
            else if (elapsed < inTime + hold)
                current = 1f;
            else if (elapsed < inTime + hold + fadeOut)
                current = Mathf.Lerp(1f, 0f, HemiEases.Evaluate(ComboOverlay.FadeOutEase, (elapsed - inTime - hold) / fadeOut));
            else
            {
                started = -1f;
                current = 0f;
            }
            return current;
        }
    }

    internal sealed class ComboBehaviour : MonoBehaviour
    {
        private readonly ComboPulse pulse = new ComboPulse();
        private readonly ComboFade fade = new ComboFade();

        private Canvas canvas;
        private ComboBlock block;
        private bool wasVisible;
        private int shown = -1;

        private void Awake()
        {
            Build();
        }

        private void OnDestroy()
        {
            block?.Destroy();
        }

        private void Update()
        {
            if (canvas == null)
                return;

            ComboCounter.Poll(ComboOverlay.Kind);

            bool visible = ComboOverlay.Enabled && StateValues.IsPlaying;
            if (visible != wasVisible)
            {
                wasVisible = visible;
                canvas.enabled = visible;

                if (visible)
                {
                    shown = ComboCounter.Value;
                    pulse.Clear();
                    fade.Clear();
                    block.SetValue(shown);
                }
            }

            if (!visible)
                return;

            StateGroupLayout.Place(block.Root, ComboOverlay.Anchor, new Vector2(ComboOverlay.OffsetX, ComboOverlay.OffsetY));
            block.ApplyStyle();

            int current = ComboCounter.Value;
            if (current != shown)
            {
                if (current > shown)
                    fade.Trigger();
                shown = current;
                pulse.Start();
                block.SetValue(shown);
            }

            block.SetPulse(pulse.Scale());
            block.SetAlpha(fade.Alpha());
        }

        private void Build()
        {
            canvas = HemiOverlayObjects.CreateCanvas(transform, "ComboCanvas", short.MaxValue - 64);
            block = ComboBlock.Create(canvas.transform);
        }
    }

    internal sealed class ComboGhostTicker : MonoBehaviour
    {
        private readonly ComboPulse pulse = new ComboPulse();
        private ComboBlock block;
        private int shown = int.MinValue;

        internal void Initialize(ComboBlock value)
        {
            block = value;
            block.SetAlpha(1f);
        }

        private void OnDestroy()
        {
            block?.Destroy();
        }

        private void Update()
        {
            if (block == null)
                return;

            if (!HemiTweaksMod.IsInterfaceOpen)
                return;

            ComboCounter.Poll(ComboOverlay.Kind);
            block.ApplyStyle();

            int current = ComboCounter.Value;
            if (current != shown)
            {
                if (shown != int.MinValue)
                    pulse.Start();
                shown = current;
                block.SetValue(shown);
            }

            block.SetPulse(pulse.Scale());
        }
    }
}
