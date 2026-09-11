using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace HemiTweaks
{
    internal static class TileInfo
    {
        private const string AngleColor = "#FF6B6B";
        private const string BeatColor = "#5BB0FF";
        private const string CountColor = "#9AA3AF";

        private static scrFloor borrowed;
        private static string applied;

        private static TMPro.TextMeshProUGUI overlay;

        private static readonly HemiTextMaterialSlot materialSlot = new HemiTextMaterialSlot();

        internal static bool Active => HemiTweaksMod.TileInfoEnabled;

        internal static void Tick()
        {
            scnEditor editor = ADOBase.editor;
            if (!Active || editor == null)
            {
                Release();
                return;
            }

            List<scrFloor> selection = editor.selectedFloors;
            if (selection == null || selection.Count == 0)
            {
                Release();
                return;
            }

            scrFloor host = selection[0];
            scrLetterPress badge = host == null ? null : host.editorNumText;
            UnityEngine.UI.Text label = badge == null ? null : badge.letterText;
            if (label == null)
            {
                Release();
                return;
            }

            if (!ReferenceEquals(borrowed, host))
            {
                Release();
                borrowed = host;
            }

            string text = Compose(editor, selection);
            if (text == null)
            {
                Release();
                return;
            }

            if (!EnsureOverlay(label))
            {
                Release();
                return;
            }

            if (label.enabled)
                label.enabled = false;

            ApplyStyle(label);

            if (!string.Equals(applied, text, StringComparison.Ordinal) || overlay.text != text)
            {
                applied = text;
                overlay.text = text;
            }

            GameObject badgeObject = badge.gameObject;
            if (!badgeObject.activeSelf)
                badgeObject.SetActive(true);
        }

        private static void ApplyStyle(UnityEngine.UI.Text label)
        {
            float size = Mathf.Max(1f, label.fontSize * HemiTweaksMod.TileInfoScale);
            if (!Mathf.Approximately(overlay.fontSize, size))
                overlay.fontSize = size;

            Material material = materialSlot.Resolve(
                overlay.font,
                HemiTweaksMod.TileInfoShadowColor,
                HemiTweaksMod.TileInfoShadowX,
                HemiTweaksMod.TileInfoShadowY,
                out bool rewritten);
            if (material == null)
                return;

            bool assigned = overlay.fontSharedMaterial != material;
            if (assigned)
                overlay.fontSharedMaterial = material;

            if (assigned || rewritten)
                overlay.UpdateMeshPadding();
        }

        private static bool EnsureOverlay(UnityEngine.UI.Text label)
        {
            if (overlay != null && overlay.transform.parent == label.transform)
                return true;

            DestroyOverlay();

            try
            {
                GameObject obj = new GameObject("HemiTileInfo");
                obj.transform.SetParent(label.transform, false);

                RectTransform rect = obj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;

                TMPro.TextMeshProUGUI text = obj.AddComponent<TMPro.TextMeshProUGUI>();
                text.font = Interface.HemiTheme.GameBold;
                text.fontSize = Mathf.Max(1f, label.fontSize * HemiTweaksMod.TileInfoScale);
                text.color = Color.white;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                text.overflowMode = TMPro.TextOverflowModes.Overflow;
                text.richText = true;
                text.raycastTarget = false;

                overlay = text;
                return true;
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Warning("Tile info could not build its label: " + exception.Message);
                overlay = null;
                return false;
            }
        }

        private static void DestroyOverlay()
        {
            if (overlay == null)
                return;

            materialSlot.Destroy();

            try
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
            }
            catch
            {
            }
            overlay = null;
        }

        private static readonly StringBuilder Scratch = new StringBuilder(64);
        private static bool hasComposition;
        private static CompositionKey lastComposition;
        private static string lastComposedText;

        private static string Compose(scnEditor editor, List<scrFloor> selection)
        {
            int count = selection.Count;
            int counted = count;

            if (HemiTweaksMod.TileInfoExcludeLast && counted > 1)
                counted--;

            int terminal = int.MaxValue;
            List<scrFloor> floors = ADOBase.lm != null ? ADOBase.lm.listFloors : null;
            if (floors != null)
                terminal = floors.Count - 1;

            float bpm = 100f;
            scrConductor conductor = ADOBase.conductor;
            if (conductor != null && conductor.bpm > 0.0001f)
                bpm = conductor.bpm;

            double secondsPerBeat = 60.0 / bpm;

            double radians = 0.0;
            double beats = 0.0;
            double seconds = 0.0;

            for (int i = 0; i < counted && i < selection.Count; i++)
            {
                scrFloor floor = selection[i];
                if (floor == null || floor.seqID >= terminal)
                    continue;

                double tileBeats = floor.angleLength / Math.PI + floor.extraBeats;
                radians += floor.angleLength;
                beats += tileBeats;

                double speed = floor.speed > 0.0001f ? floor.speed : 1.0;
                seconds += tileBeats * secondsPerBeat / speed;
            }

            CompositionKey key = new CompositionKey(
                radians,
                beats,
                count,
                seconds,
                HemiTweaksMod.TileInfoAngle,
                HemiTweaksMod.TileInfoBeats,
                HemiTweaksMod.TileInfoCount,
                HemiTweaksMod.TileInfoSeconds);
            if (hasComposition && lastComposition.Equals(key))
                return lastComposedText;

            StringBuilder builder = Scratch;
            builder.Length = 0;

            if (HemiTweaksMod.TileInfoAngle)
                Line(builder, AngleColor, (radians * 180.0 / Math.PI).ToString("0.####", CultureInfo.InvariantCulture) + "°");

            if (HemiTweaksMod.TileInfoBeats)
                Line(builder, BeatColor, beats.ToString("0.####", CultureInfo.InvariantCulture) + "♪");

            if (HemiTweaksMod.TileInfoCount)
                Line(builder, CountColor, count.ToString(CultureInfo.InvariantCulture) + "#");

            if (HemiTweaksMod.TileInfoSeconds)
                Line(builder, null, seconds.ToString("0.####", CultureInfo.InvariantCulture) + "s");

            lastComposition = key;
            lastComposedText = builder.Length == 0 ? null : builder.ToString();
            hasComposition = true;
            return lastComposedText;
        }

        private readonly struct CompositionKey : IEquatable<CompositionKey>
        {
            private readonly double radians;
            private readonly double beats;
            private readonly int count;
            private readonly double seconds;
            private readonly bool showAngle;
            private readonly bool showBeats;
            private readonly bool showCount;
            private readonly bool showSeconds;

            internal CompositionKey(
                double radians,
                double beats,
                int count,
                double seconds,
                bool showAngle,
                bool showBeats,
                bool showCount,
                bool showSeconds)
            {
                this.radians = radians;
                this.beats = beats;
                this.count = count;
                this.seconds = seconds;
                this.showAngle = showAngle;
                this.showBeats = showBeats;
                this.showCount = showCount;
                this.showSeconds = showSeconds;
            }

            public bool Equals(CompositionKey other)
            {
                return radians.Equals(other.radians)
                    && beats.Equals(other.beats)
                    && count == other.count
                    && seconds.Equals(other.seconds)
                    && showAngle == other.showAngle
                    && showBeats == other.showBeats
                    && showCount == other.showCount
                    && showSeconds == other.showSeconds;
            }
        }

        private static void Line(StringBuilder builder, string color, string value)
        {
            if (builder.Length > 0)
                builder.Append('\n');

            if (string.IsNullOrEmpty(color))
            {
                builder.Append(value);
                return;
            }

            builder.Append("<color=").Append(color).Append('>').Append(value).Append("</color>");
        }

        internal static void Release()
        {
            DestroyOverlay();

            if (borrowed == null)
            {
                applied = null;
                return;
            }

            try
            {
                if (borrowed.editorNumText != null)
                {
                    if (borrowed.editorNumText.letterText != null)
                    {
                        borrowed.editorNumText.letterText.enabled = true;
                        borrowed.editorNumText.letterText.text = borrowed.seqID.ToString(CultureInfo.InvariantCulture);
                    }

                    bool show = ADOBase.editor != null && ADOBase.editor.showFloorNums;
                    if (borrowed.editorNumText.gameObject.activeSelf != show)
                        borrowed.editorNumText.gameObject.SetActive(show);
                }
            }
            catch
            {
            }

            borrowed = null;
            applied = null;
        }
    }

    [HarmonyPatch(typeof(scnEditor), "Update")]
    internal static class TileInfoUpdatePatch
    {
        private static void Postfix()
        {
            TileInfo.Tick();
        }
    }
}
