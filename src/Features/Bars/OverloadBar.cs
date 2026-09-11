using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace HemiTweaks
{
    internal enum OverloadBarFill
    {
        Grow,

        Shrink
    }

    internal static class OverloadBarValue
    {
        internal static float Ratio
        {
            get
            {
                try
                {
                    scrFailBar failBar = ADOBase.controller?.playerOne?.failBar;
                    return failBar == null ? 0f : Mathf.Clamp01(failBar.overloadCounter);
                }
                catch
                {
                    return 0f;
                }
            }
        }

        internal static bool Dead
        {
            get
            {
                try
                {
                    scrController controller = ADOBase.controller;
                    return controller != null
                        && Array.IndexOf(scrController.deathStates, controller.state) >= 0;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    internal static class OverloadBarOverlay
    {
        internal const float MinimumLength = 80f;
        internal const float MaximumLength = 1920f;
        internal const float MinimumThickness = 2f;
        internal const float MaximumThickness = 120f;

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> enabledEntry;
        private static MelonPreferences_Entry<int> anchorEntry;
        private static MelonPreferences_Entry<float> offsetXEntry;
        private static MelonPreferences_Entry<float> offsetYEntry;
        private static MelonPreferences_Entry<float> lengthEntry;
        private static MelonPreferences_Entry<float> thicknessEntry;
        private static MelonPreferences_Entry<int> orientationEntry;
        private static MelonPreferences_Entry<int> fillEntry;
        private static MelonPreferences_Entry<string> colorEntry;
        private static MelonPreferences_Entry<bool> trackEntry;
        private static MelonPreferences_Entry<string> trackColorEntry;
        private static GameObject behaviourObject;

        private static readonly HemiSaveDebounce writer = new HemiSaveDebounce(Write);

        private const float PeakFallSeconds = 0.9f;

        private static bool locked;

        private static float peak;

        internal static bool Enabled => enabledEntry != null && enabledEntry.Value;

        internal static StateAnchor Anchor => HemiBarPrefs.Anchor(anchorEntry);

        internal static float OffsetX => offsetXEntry == null ? 0f : offsetXEntry.Value;

        internal static float OffsetY => offsetYEntry == null ? 22f : offsetYEntry.Value;

        internal static float Length => lengthEntry == null
            ? 1152f
            : Mathf.Clamp(lengthEntry.Value, MinimumLength, MaximumLength);

        internal static float Thickness => thicknessEntry == null
            ? 22f
            : Mathf.Clamp(thicknessEntry.Value, MinimumThickness, MaximumThickness);

        internal static HemiBarOrientation Orientation => orientationEntry == null
            ? HemiBarOrientation.Horizontal
            : (HemiBarOrientation)Mathf.Clamp(
                orientationEntry.Value, (int)HemiBarOrientation.Horizontal, (int)HemiBarOrientation.Vertical);

        internal static OverloadBarFill FillBehaviour => fillEntry == null
            ? OverloadBarFill.Grow
            : (OverloadBarFill)Mathf.Clamp(fillEntry.Value, (int)OverloadBarFill.Grow, (int)OverloadBarFill.Shrink);

        internal static bool TrackShown => trackEntry == null || trackEntry.Value;

        internal static Color ResolveColor()
        {
            return StateGroupStore.ParseColor(
                colorEntry == null ? "" : colorEntry.Value, new Color32(0xFF, 0x40, 0x40, 0xFF));
        }

        internal static Color ResolveTrackColor()
        {
            return StateGroupStore.ParseColor(
                trackColorEntry == null ? "" : trackColorEntry.Value, new Color(1f, 1f, 1f, 0.5f));
        }

        internal static float Amount
        {
            get
            {
                float ratio = locked ? 1f : Mathf.Max(OverloadBarValue.Ratio, peak);
                return FillBehaviour == OverloadBarFill.Shrink ? 1f - ratio : ratio;
            }
        }

        private static void NoteOverload()
        {
            peak = 1f;
            locked = true;
        }

        internal static HemiBarStyle Style => new HemiBarStyle
        {
            Length = Length,
            Thickness = Thickness,
            Orientation = Orientation,
            Fill = ResolveColor(),
            Track = ResolveTrackColor(),
            TrackShown = TrackShown,
            OutlineShown = false,
            OutlineSize = 0f,
            Outline = Color.black
        };

        internal static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            enabledEntry = category.CreateEntry("EnableOverloadBar", false, "Enable Overload Bar",
                "Draws how close the run is to dying of overload.");
            anchorEntry = category.CreateEntry("OverloadBarAnchor", (int)StateAnchor.TopCenter,
                "Overload Bar Anchor", "Which corner or edge of the screen the bar is pinned to.");
            offsetXEntry = category.CreateEntry("OverloadBarOffsetX", 0f, "Overload Bar X", "");
            offsetYEntry = category.CreateEntry("OverloadBarOffsetY", 22f, "Overload Bar Y", "");
            lengthEntry = category.CreateEntry("OverloadBarLength", 1152f, "Overload Bar Length", "");
            thicknessEntry = category.CreateEntry("OverloadBarThickness", 22f, "Overload Bar Thickness", "");
            orientationEntry = category.CreateEntry("OverloadBarOrientation", (int)HemiBarOrientation.Horizontal,
                "Overload Bar Orientation", "");
            fillEntry = category.CreateEntry("OverloadBarFill", (int)OverloadBarFill.Grow,
                "Overload Bar Fill", "Whether the bar fills up with overload or drains away.");
            colorEntry = category.CreateEntry("OverloadBarColor", "#FF4040FF", "Overload Bar Colour", "");
            trackEntry = category.CreateEntry("OverloadBarTrack", true, "Overload Bar Track",
                "Draws the rest of the bar behind the part that moves.");
            trackColorEntry = category.CreateEntry("OverloadBarTrackColor", "#FFFFFF80",
                "Overload Bar Track Colour", "");

            if (Enabled)
                EnsureBehaviourObject();
        }

        internal static void UpdateLifecycle()
        {
            writer.Tick();

            if (locked && !OverloadBarValue.Dead)
                locked = false;

            if (peak > 0f)
                peak = Mathf.Max(0f, peak - Time.unscaledDeltaTime / PeakFallSeconds);

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

        internal static void SetLength(float value)
        {
            if (HemiBarPrefs.SetClamped(lengthEntry, value, MinimumLength, MaximumLength))
                Save();
        }

        internal static void SetThickness(float value)
        {
            if (HemiBarPrefs.SetClamped(thicknessEntry, value, MinimumThickness, MaximumThickness))
                Save();
        }

        internal static void SetOrientation(HemiBarOrientation value)
        {
            if (HemiBarPrefs.Set(orientationEntry, (int)value))
                Save();
        }

        internal static void SetFillBehaviour(OverloadBarFill value)
        {
            if (HemiBarPrefs.Set(fillEntry, (int)value))
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
            HemiOverlayHost.Ensure<OverloadBarBehaviour>(ref behaviourObject, "HemiTweaks_OverloadBar");
        }

        [HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Die))]
        private static class ScrPlayerDiePatch
        {
            private static void Prefix(bool overload)
            {
                try
                {
                    if (overload)
                        NoteOverload();
                }
                catch
                {
                }
            }
        }
    }

    internal sealed class OverloadBarBehaviour : MonoBehaviour
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

            bool visible = OverloadBarOverlay.Enabled && StateValues.IsPlaying;
            if (visible != wasVisible)
            {
                wasVisible = visible;
                canvas.enabled = visible;
            }

            if (!visible)
                return;

            StateGroupLayout.Place(block.Root, OverloadBarOverlay.Anchor, new Vector2(OverloadBarOverlay.OffsetX, OverloadBarOverlay.OffsetY));
            block.SetFill(OverloadBarOverlay.Amount);
            block.Apply(OverloadBarOverlay.Style);
        }

        private void Build()
        {
            canvas = HemiOverlayObjects.CreateCanvas(transform, "OverloadBarCanvas", short.MaxValue - 65);
            block = HemiBarBlock.Create(canvas.transform);
        }
    }

    internal sealed class OverloadBarGhostTicker : MonoBehaviour
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

            block.SetFill(StateValues.IsPlaying ? OverloadBarOverlay.Amount : 0.5f);
            block.Apply(OverloadBarOverlay.Style);
        }
    }
}
