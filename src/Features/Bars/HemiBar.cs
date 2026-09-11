using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks
{
    internal enum HemiBarOrientation
    {
        Horizontal,
        Vertical
    }

    internal struct HemiBarStyle
    {
        internal float Length;

        internal float Thickness;

        internal HemiBarOrientation Orientation;
        internal Color Fill;
        internal Color Track;
        internal bool TrackShown;
        internal bool OutlineShown;
        internal float OutlineSize;
        internal Color Outline;
    }

    internal static class HemiOverlayObjects
    {
        internal static RectTransform New(string name, Transform parent)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            return holder.AddComponent<RectTransform>();
        }

        internal static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            HemiTweaksMod.ApplyDefaultScaling(scaler);

            canvas.enabled = false;
            return canvas;
        }
    }

    internal static class HemiBarPrefs
    {
        internal static StateAnchor Anchor(MelonPreferences_Entry<int> entry) => entry == null
            ? StateAnchor.TopCenter
            : (StateAnchor)Mathf.Clamp(entry.Value, (int)StateAnchor.TopLeft, (int)StateAnchor.BottomRight);

        internal static bool Set(MelonPreferences_Entry<bool> entry, bool value)
        {
            if (entry == null || entry.Value == value)
                return false;

            entry.Value = value;
            return true;
        }

        internal static bool Set(MelonPreferences_Entry<int> entry, int value)
        {
            if (entry == null || entry.Value == value)
                return false;

            entry.Value = value;
            return true;
        }

        internal static bool SetClamped(MelonPreferences_Entry<float> entry, float value, float minimum, float maximum)
        {
            if (entry == null)
                return false;

            entry.Value = Mathf.Clamp(value, minimum, maximum);
            return true;
        }

        internal static bool SetColor(MelonPreferences_Entry<string> entry, Color value)
        {
            if (entry == null)
                return false;

            entry.Value = StateGroupStore.ToHex(value);
            return true;
        }
    }

    internal sealed class HemiBarBlock
    {
        private Image track;
        private Image fill;
        private Outline outline;
        private float amount;
        private HemiBarOrientation orientation = HemiBarOrientation.Horizontal;

        internal RectTransform Root { get; private set; }

        internal static HemiBarBlock Create(Transform parent)
        {
            HemiBarBlock block = new HemiBarBlock();
            block.Root = HemiOverlayObjects.New("Bar", parent);

            block.track = Rectangle("Track", block.Root);
            RectTransform trackRect = block.track.rectTransform;
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;

            block.fill = Rectangle("Fill", block.Root);
            block.outline = block.fill.gameObject.AddComponent<Outline>();
            block.outline.enabled = false;

            block.Pin(HemiBarOrientation.Horizontal);
            return block;
        }

        internal void SetFill(float value)
        {
            amount = HemiNumberSafety.Clamp(value, 0f, 1f, 0f);
        }

        internal void Apply(HemiBarStyle style)
        {
            if (orientation != style.Orientation)
                Pin(style.Orientation);

            bool horizontal = style.Orientation == HemiBarOrientation.Horizontal;
            Vector2 rootSize = horizontal
                ? new Vector2(style.Length, style.Thickness)
                : new Vector2(style.Thickness, style.Length);
            if (!Root.sizeDelta.Equals(rootSize))
                Root.sizeDelta = rootSize;

            float filled = style.Length * amount;
            Vector2 fillSize = horizontal ? new Vector2(filled, 0f) : new Vector2(0f, filled);
            if (!fill.rectTransform.sizeDelta.Equals(fillSize))
                fill.rectTransform.sizeDelta = fillSize;

            if (!fill.color.Equals(style.Fill))
                fill.color = style.Fill;
            if (!track.color.Equals(style.Track))
                track.color = style.Track;
            if (track.enabled != style.TrackShown)
                track.enabled = style.TrackShown;

            bool outlineShown = style.OutlineShown && style.OutlineSize > 0f;
            if (outline.enabled != outlineShown)
                outline.enabled = outlineShown;
            if (outlineShown)
            {
                if (!outline.effectColor.Equals(style.Outline))
                    outline.effectColor = style.Outline;
                Vector2 distance = new Vector2(style.OutlineSize, -style.OutlineSize);
                if (!outline.effectDistance.Equals(distance))
                    outline.effectDistance = distance;
            }
        }

        private void Pin(HemiBarOrientation value)
        {
            orientation = value;
            RectTransform rect = fill.rectTransform;

            if (value == HemiBarOrientation.Horizontal)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
            }

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image Rectangle(string name, Transform parent)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.AddComponent<RectTransform>();

            Image image = holder.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }

    internal enum ProgressBarSource
    {
        Tiles,

        Time
    }

    internal static class ProgressBarValue
    {
        internal static float Fill
        {
            get
            {
                try
                {
                    if (ProgressBarOverlay.Source == ProgressBarSource.Tiles)
                        return StateValues.ProgressFraction;

                    scrController controller = ADOBase.controller;
                    List<scrFloor> floors = ADOBase.lm?.listFloors;
                    if (controller == null || floors == null || floors.Count == 0)
                        return 0f;

                    scrConductor conductor = ADOBase.conductor;
                    scrFloor last = floors[floors.Count - 1];
                    if (conductor == null || last == null)
                        return 0f;

                    double total = last.entryTime;
                    double position = conductor.songposition_minusi;
                    if (double.IsNaN(total) || double.IsInfinity(total)
                        || double.IsNaN(position) || double.IsInfinity(position)
                        || total <= 0.000001 || position <= 0d)
                        return 0f;

                    return position >= total ? 1f : (float)(position / total);
                }
                catch
                {
                    return 0f;
                }
            }
        }
    }

    internal static class ProgressBarOverlay
    {
        internal const float MinimumWidth = 80f;
        internal const float MaximumWidth = 1920f;
        internal const float MinimumHeight = 2f;
        internal const float MaximumHeight = 120f;

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> enabledEntry;
        private static MelonPreferences_Entry<int> sourceEntry;
        private static MelonPreferences_Entry<int> anchorEntry;
        private static MelonPreferences_Entry<float> offsetXEntry;
        private static MelonPreferences_Entry<float> offsetYEntry;
        private static MelonPreferences_Entry<float> widthEntry;
        private static MelonPreferences_Entry<float> heightEntry;
        private static MelonPreferences_Entry<string> colorEntry;
        private static MelonPreferences_Entry<bool> trackEntry;
        private static MelonPreferences_Entry<string> trackColorEntry;
        private static MelonPreferences_Entry<bool> outlineEntry;
        private static MelonPreferences_Entry<float> outlineSizeEntry;
        private static MelonPreferences_Entry<string> outlineColorEntry;
        private static GameObject behaviourObject;

        private static readonly HemiSaveDebounce writer = new HemiSaveDebounce(Write);

        internal static bool Enabled => enabledEntry != null && enabledEntry.Value;

        internal static ProgressBarSource Source => sourceEntry == null
            ? ProgressBarSource.Time
            : (ProgressBarSource)Mathf.Clamp(sourceEntry.Value, (int)ProgressBarSource.Tiles, (int)ProgressBarSource.Time);

        internal static StateAnchor Anchor => HemiBarPrefs.Anchor(anchorEntry);

        internal static float OffsetX => offsetXEntry == null ? 0f : offsetXEntry.Value;

        internal static float OffsetY => offsetYEntry == null ? 0f : offsetYEntry.Value;

        internal static float Width => widthEntry == null
            ? MaximumWidth
            : Mathf.Clamp(widthEntry.Value, MinimumWidth, MaximumWidth);

        internal static float Height => heightEntry == null
            ? 24f
            : Mathf.Clamp(heightEntry.Value, MinimumHeight, MaximumHeight);

        internal static bool TrackShown => trackEntry != null && trackEntry.Value;

        internal static bool OutlineShown => outlineEntry != null && outlineEntry.Value;

        internal static float OutlineSize => outlineSizeEntry == null
            ? 3f
            : Mathf.Clamp(outlineSizeEntry.Value, 0f, 12f);

        internal static Color ResolveColor()
        {
            return StateGroupStore.ParseColor(colorEntry == null ? "" : colorEntry.Value, Color.white);
        }

        internal static Color ResolveTrackColor()
        {
            return StateGroupStore.ParseColor(
                trackColorEntry == null ? "" : trackColorEntry.Value, new Color(1f, 1f, 1f, 0.5f));
        }

        internal static Color ResolveOutlineColor()
        {
            return StateGroupStore.ParseColor(
                outlineColorEntry == null ? "" : outlineColorEntry.Value, Color.black);
        }

        internal static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            enabledEntry = category.CreateEntry("EnableProgressBar", false, "Enable Progress Bar",
                "Draws how far through the level the run is.");
            sourceEntry = category.CreateEntry("ProgressBarSource", (int)ProgressBarSource.Time,
                "Progress Bar Source", "Tiles passed, or where the song has got to.");
            anchorEntry = category.CreateEntry("ProgressBarAnchor", (int)StateAnchor.TopCenter,
                "Progress Bar Anchor", "Which corner or edge of the screen the bar is pinned to.");
            offsetXEntry = category.CreateEntry("ProgressBarOffsetX", 0f, "Progress Bar X",
                "Distance inward from the anchored edge.");
            offsetYEntry = category.CreateEntry("ProgressBarOffsetY", 0f, "Progress Bar Y",
                "Distance inward from the anchored edge.");
            widthEntry = category.CreateEntry("ProgressBarWidth", MaximumWidth, "Progress Bar Width", "");
            heightEntry = category.CreateEntry("ProgressBarHeight", 24f, "Progress Bar Height", "");
            colorEntry = category.CreateEntry("ProgressBarColor", "#FFFFFFFF", "Progress Bar Colour", "");
            trackEntry = category.CreateEntry("ProgressBarTrack", false, "Progress Bar Track",
                "Draws the part of the bar still to come.");
            trackColorEntry = category.CreateEntry("ProgressBarTrackColor", "#FFFFFF80",
                "Progress Bar Track Colour", "");
            outlineEntry = category.CreateEntry("ProgressBarOutline", false, "Progress Bar Outline", "");
            outlineSizeEntry = category.CreateEntry("ProgressBarOutlineSize", 3f, "Progress Bar Outline Size", "");
            outlineColorEntry = category.CreateEntry("ProgressBarOutlineColor", "#000000FF",
                "Progress Bar Outline Colour", "");

            if (Enabled)
                EnsureBehaviourObject();
        }

        internal static void UpdateLifecycle()
        {
            writer.Tick();

            if (Enabled)
                EnsureBehaviourObject();
            else if (behaviourObject != null && behaviourObject.activeSelf)
                behaviourObject.SetActive(false);
        }

        internal static void SetEnabled(bool value)
        {
            if (!HemiBarPrefs.Set(enabledEntry, value))
                return;

            UpdateLifecycle();
            Save();
        }

        internal static void SetSource(ProgressBarSource value)
        {
            if (HemiBarPrefs.Set(sourceEntry, (int)value))
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

        internal static void SetWidth(float value)
        {
            if (HemiBarPrefs.SetClamped(widthEntry, value, MinimumWidth, MaximumWidth))
                Save();
        }

        internal static void SetHeight(float value)
        {
            if (HemiBarPrefs.SetClamped(heightEntry, value, MinimumHeight, MaximumHeight))
                Save();
        }

        internal static void SetColor(Color value)
        {
            if (HemiBarPrefs.SetColor(colorEntry, value))
                Save();
        }

        internal static void SetTrackShown(bool value)
        {
            if (HemiBarPrefs.Set(trackEntry, value))
                Save();
        }

        internal static void SetTrackColor(Color value)
        {
            if (HemiBarPrefs.SetColor(trackColorEntry, value))
                Save();
        }

        internal static void SetOutlineShown(bool value)
        {
            if (HemiBarPrefs.Set(outlineEntry, value))
                Save();
        }

        internal static void SetOutlineSize(float value)
        {
            if (HemiBarPrefs.SetClamped(outlineSizeEntry, value, 0f, 12f))
                Save();
        }

        internal static void SetOutlineColor(Color value)
        {
            if (HemiBarPrefs.SetColor(outlineColorEntry, value))
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
            HemiOverlayHost.Ensure<ProgressBarBehaviour>(ref behaviourObject, "HemiTweaks_ProgressBar");
        }
    }

    internal static class ProgressBarStyle
    {
        internal static HemiBarStyle Current => new HemiBarStyle
        {
            Length = ProgressBarOverlay.Width,
            Thickness = ProgressBarOverlay.Height,
            Orientation = HemiBarOrientation.Horizontal,
            Fill = ProgressBarOverlay.ResolveColor(),
            Track = ProgressBarOverlay.ResolveTrackColor(),
            TrackShown = ProgressBarOverlay.TrackShown,
            OutlineShown = ProgressBarOverlay.OutlineShown,
            OutlineSize = ProgressBarOverlay.OutlineSize,
            Outline = ProgressBarOverlay.ResolveOutlineColor()
        };
    }

    internal sealed class ProgressBarBehaviour : MonoBehaviour
    {
        private Canvas canvas;
        private HemiBarBlock block;
        private bool wasVisible;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            if (canvas == null)
                return;

            bool visible = ProgressBarOverlay.Enabled && StateValues.IsPlaying;
            if (visible != wasVisible)
            {
                wasVisible = visible;
                canvas.enabled = visible;
            }

            if (!visible)
                return;

            StateGroupLayout.Place(block.Root, ProgressBarOverlay.Anchor, new Vector2(ProgressBarOverlay.OffsetX, ProgressBarOverlay.OffsetY));
            block.SetFill(ProgressBarValue.Fill);
            block.Apply(ProgressBarStyle.Current);
        }

        private void Build()
        {
            canvas = HemiOverlayObjects.CreateCanvas(transform, "ProgressBarCanvas", short.MaxValue - 65);
            block = HemiBarBlock.Create(canvas.transform);
        }
    }

    internal sealed class ProgressBarGhostTicker : MonoBehaviour
    {
        private HemiBarBlock block;

        internal void Initialize(HemiBarBlock value)
        {
            block = value;
        }

        private void Update()
        {
            if (block == null)
                return;

            if (!HemiTweaksMod.IsInterfaceOpen)
                return;

            float fill = ProgressBarValue.Fill;
            block.SetFill(StateValues.IsPlaying && fill > 0f ? fill : 0.5f);
            block.Apply(ProgressBarStyle.Current);
        }
    }
}
