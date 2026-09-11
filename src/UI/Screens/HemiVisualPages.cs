using System;
using System.Collections.Generic;
using System.Globalization;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiVisualPages
    {
        private static string[] ImageExtensions => GCS.SupportedImageFiles;

        internal static void PlanetColor(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_PLANETCOLOR_PLANETS"));
            ColorRow(content, HemiLang.Get("SET_PLANETCOLOR_RED_PLANET"), PlanetColorTarget.RedPlanet);
            ColorRow(content, HemiLang.Get("SET_PLANETCOLOR_BLUE_PLANET"), PlanetColorTarget.BluePlanet);

            HemiRows.Heading(content, HemiLang.Get("SET_PLANETCOLOR_RINGS"));
            ColorRow(content, HemiLang.Get("SET_PLANETCOLOR_RED_RING"), PlanetColorTarget.RedRing);
            ColorRow(content, HemiLang.Get("SET_PLANETCOLOR_BLUE_RING"), PlanetColorTarget.BlueRing);

            HemiRows.Heading(content, HemiLang.Get("SET_PLANETCOLOR_TAILS"));
            ColorRow(content, HemiLang.Get("SET_PLANETCOLOR_RED_TAIL"), PlanetColorTarget.RedTail);
            ColorRow(content, HemiLang.Get("SET_PLANETCOLOR_BLUE_TAIL"), PlanetColorTarget.BlueTail);

            HemiRows.Heading(content, HemiLang.Get("SET_PLANETCOLOR_OVERLAY"));
            HemiRows.ToggleRow(
                content,
                HemiLang.Get("SET_PLANETCOLOR_OVERLAY_ENABLE"),
                HemiLang.Get("DESC_SET_PLANETCOLOR_OVERLAY_ENABLE"),
                () => HemiTweaksMod.PlanetOverlayEnabled,
                HemiTweaksMod.SetPlanetOverlayEnabled);

            OverlayRows(content, true, HemiLang.Get("SET_PLANETCOLOR_RED_OVERLAY"), HemiTweaksMod.RedOverlayPath, HemiTweaksMod.RedOverlayScale);
            OverlayRows(content, false, HemiLang.Get("SET_PLANETCOLOR_BLUE_OVERLAY"), HemiTweaksMod.BlueOverlayPath, HemiTweaksMod.BlueOverlayScale);

            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_PLANETCOLOR_OVERLAY_NOTE"));
        }

        internal static void TileCorner(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_TILECORNER_CURVATURE"));
            HemiRows.SliderRow(
                content,
                HemiLang.Get("SET_TILECORNER_STRENGTH"),
                HemiTweaksMod.TileCornerCurvatureAmount * 100f,
                0f,
                100f,
                true,
                value => HemiTweaksMod.SetTileCornerCurvatureAmount(value / 100f),
                HemiLang.Get("DESC_SET_TILECORNER_STRENGTH"));

            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_TILECORNER_NOTE"));
        }

        internal static void BuildName(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_BUILDNAME_SECTION"));
            HemiRows.TextRow(
                content,
                HemiLang.Get("SET_BUILDNAME_NAME"),
                HemiTweaksMod.BuildName,
                "HemiTweaks",
                HemiTweaksMod.SetBuildName,
                HemiLang.Get("DESC_SET_BUILDNAME_NAME"));

            HemiRows.NoteRow(content, HemiLang.Get("SET_BUILDNAME_NOTE"));
        }

        internal static void HideUi(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_HIDEUI_ELEMENTS"));

            EntryToggle(content, HemiLang.Get("SET_HIDEUI_LEVELTITLE"), HemiLang.Get("DESC_SET_HIDEUI_LEVELTITLE"),
                HemiTweaksMod.HideLevelTitleEntry, HemiTweaksMod.SetHideUiOption);
            EntryToggle(content, HemiLang.Get("SET_HIDEUI_AUTOPLAY"), HemiLang.Get("DESC_SET_HIDEUI_AUTOPLAY"),
                HemiTweaksMod.HideAutoplayEntry, HemiTweaksMod.SetHideUiOption);
            EntryToggle(content, HemiLang.Get("SET_HIDEUI_NOFAIL"), HemiLang.Get("DESC_SET_HIDEUI_NOFAIL"),
                HemiTweaksMod.HideNoFailEntry, HemiTweaksMod.SetHideUiOption);
            EntryToggle(content, HemiLang.Get("SET_HIDEUI_RESULTS"), HemiLang.Get("DESC_SET_HIDEUI_RESULTS"),
                HemiTweaksMod.HideResultsEntry, HemiTweaksMod.SetHideUiOption);
            EntryToggle(content, HemiLang.Get("SET_HIDEUI_ERRORMETER"), HemiLang.Get("DESC_SET_HIDEUI_ERRORMETER"),
                HemiTweaksMod.HideErrorMeterEntry, HemiTweaksMod.SetHideUiOption);

            HemiRows.NoteRow(content, HemiLang.Get("SET_HIDEUI_NOTE"));
        }

        private static void EntryToggle(RectTransform content, string label, string hint,
            MelonPreferences_Entry<bool> entry, Action<MelonPreferences_Entry<bool>, bool> apply)
        {
            if (entry == null)
                return;

            HemiRows.ToggleRow(content, label, hint, () => entry.Value, value => apply(entry, value));
        }

        private static string[] OverlayAnchorOptions => StateGroupLayout.AnchorOptions();

        private static string[] ProgressBarSourceOptions =>
            new[] { HemiLang.Get("SET_PROGRESSBAR_SOURCE_TILES"), HemiLang.Get("SET_PROGRESSBAR_SOURCE_SONGTIME") };

        private static string[] BarOrientationOptions =>
            new[] { HemiLang.Get("SET_ORIENT_HORIZONTAL"), HemiLang.Get("SET_ORIENT_VERTICAL") };

        private static string[] OverloadFillOptions =>
            new[] { HemiLang.Get("SET_OVERLOADBAR_FILL_UP"), HemiLang.Get("SET_OVERLOADBAR_FILL_DRAIN") };

        internal static void OverloadBar(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_OVERLOADBAR_POSITION"));

            HemiRows.DropdownRow(content, HemiLang.Get("SET_OVERLOADBAR_ANCHOR"), OverlayAnchorOptions, (int)OverloadBarOverlay.Anchor,
                index => OverloadBarOverlay.SetAnchor((StateAnchor)index));

            OffsetRows(content, () => OverloadBarOverlay.OffsetX, () => OverloadBarOverlay.OffsetY,
                OverloadBarOverlay.SetOffset, HemiLang.Get("DESC_SET_OVERLOADBAR_OFFSET"));

            HemiRows.Heading(content, HemiLang.Get("SET_OVERLOADBAR_SHAPE"));

            HemiRows.DropdownRow(content, HemiLang.Get("SET_OVERLOADBAR_DIRECTION"), BarOrientationOptions, (int)OverloadBarOverlay.Orientation,
                index => OverloadBarOverlay.SetOrientation((HemiBarOrientation)index));

            HemiRows.SliderRow(content, HemiLang.Get("SET_OVERLOADBAR_LENGTH"), OverloadBarOverlay.Length,
                OverloadBarOverlay.MinimumLength, OverloadBarOverlay.MaximumLength, true,
                OverloadBarOverlay.SetLength, HemiLang.Get("DESC_SET_OVERLOADBAR_LENGTH"), 0);

            HemiRows.SliderRow(content, HemiLang.Get("SET_OVERLOADBAR_THICKNESS"), OverloadBarOverlay.Thickness,
                OverloadBarOverlay.MinimumThickness, OverloadBarOverlay.MaximumThickness, true,
                OverloadBarOverlay.SetThickness, null, 0);

            HemiRows.Heading(content, HemiLang.Get("SET_OVERLOADBAR_BAR"));

            HemiRows.DropdownRow(content, HemiLang.Get("SET_OVERLOADBAR_OVERLOAD"), OverloadFillOptions, (int)OverloadBarOverlay.FillBehaviour,
                index => OverloadBarOverlay.SetFillBehaviour((OverloadBarFill)index));

            HemiRows.ColorRow(content, HemiLang.Get("SET_OVERLOADBAR_COLOR"), OverloadBarOverlay.ResolveColor(), OverloadBarOverlay.SetColor);

            HemiRows.ToggleRow(content, HemiLang.Get("SET_OVERLOADBAR_SHOW_REST"), HemiLang.Get("DESC_SET_OVERLOADBAR_SHOW_REST"),
                () => OverloadBarOverlay.TrackShown,
                value => { OverloadBarOverlay.SetTrackShown(value); HemiRoot.Instance?.Refresh(); });

            if (OverloadBarOverlay.TrackShown)
            {
                HemiRows.ColorRow(content, HemiLang.Get("SET_OVERLOADBAR_REST_COLOR"), OverloadBarOverlay.ResolveTrackColor(),
                    OverloadBarOverlay.SetTrackColor);
            }

            HemiRows.NoteRow(content, HemiLang.Get("SET_OVERLOADBAR_NOTE"));
        }

        internal static void ProgressBar(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_PROGRESSBAR_MEASURE"));

            HemiRows.DropdownRow(content, HemiLang.Get("SET_PROGRESSBAR_SOURCE"), ProgressBarSourceOptions, (int)ProgressBarOverlay.Source,
                index => ProgressBarOverlay.SetSource((ProgressBarSource)index));

            HemiRows.NoteRow(content, HemiLang.Get("SET_PROGRESSBAR_SOURCE_NOTE"));

            HemiRows.Heading(content, HemiLang.Get("SET_PROGRESSBAR_POSITION"));

            HemiRows.DropdownRow(content, HemiLang.Get("SET_PROGRESSBAR_ANCHOR"), OverlayAnchorOptions, (int)ProgressBarOverlay.Anchor,
                index => ProgressBarOverlay.SetAnchor((StateAnchor)index));

            OffsetRows(content, () => ProgressBarOverlay.OffsetX, () => ProgressBarOverlay.OffsetY,
                ProgressBarOverlay.SetOffset, HemiLang.Get("DESC_SET_PROGRESSBAR_OFFSET"));

            HemiRows.Heading(content, HemiLang.Get("SET_PROGRESSBAR_SIZE"));

            HemiRows.SliderRow(content, HemiLang.Get("SET_PROGRESSBAR_WIDTH"), ProgressBarOverlay.Width,
                ProgressBarOverlay.MinimumWidth, ProgressBarOverlay.MaximumWidth, true,
                ProgressBarOverlay.SetWidth, HemiLang.Get("DESC_SET_PROGRESSBAR_WIDTH"), 0);

            HemiRows.SliderRow(content, HemiLang.Get("SET_PROGRESSBAR_HEIGHT"), ProgressBarOverlay.Height,
                ProgressBarOverlay.MinimumHeight, ProgressBarOverlay.MaximumHeight, true,
                ProgressBarOverlay.SetHeight, null, 0);

            HemiRows.Heading(content, HemiLang.Get("SET_PROGRESSBAR_BAR"));

            HemiRows.ColorRow(content, HemiLang.Get("SET_PROGRESSBAR_COLOR"), ProgressBarOverlay.ResolveColor(), ProgressBarOverlay.SetColor);

            HemiRows.ToggleRow(content, HemiLang.Get("SET_PROGRESSBAR_SHOW_REST"), HemiLang.Get("DESC_SET_PROGRESSBAR_SHOW_REST"),
                () => ProgressBarOverlay.TrackShown,
                value => { ProgressBarOverlay.SetTrackShown(value); HemiRoot.Instance?.Refresh(); });

            if (ProgressBarOverlay.TrackShown)
            {
                HemiRows.ColorRow(content, HemiLang.Get("SET_PROGRESSBAR_REST_COLOR"), ProgressBarOverlay.ResolveTrackColor(),
                    ProgressBarOverlay.SetTrackColor);
            }

            HemiRows.Heading(content, HemiLang.Get("SET_PROGRESSBAR_OUTLINE"));

            HemiRows.ToggleRow(content, HemiLang.Get("SET_PROGRESSBAR_OUTLINE_TOGGLE"), HemiLang.Get("DESC_SET_PROGRESSBAR_OUTLINE_TOGGLE"),
                () => ProgressBarOverlay.OutlineShown,
                value => { ProgressBarOverlay.SetOutlineShown(value); HemiRoot.Instance?.Refresh(); });

            if (ProgressBarOverlay.OutlineShown)
            {
                HemiRows.SliderRow(content, HemiLang.Get("SET_PROGRESSBAR_OUTLINE_THICKNESS"), ProgressBarOverlay.OutlineSize, 0f, 12f, false,
                    ProgressBarOverlay.SetOutlineSize, null, 1);

                HemiRows.ColorRow(content, HemiLang.Get("SET_PROGRESSBAR_OUTLINE_COLOR"), ProgressBarOverlay.ResolveOutlineColor(),
                    ProgressBarOverlay.SetOutlineColor);
            }
        }

        private static readonly ComboKind[] ComboKindOrder = { ComboKind.XCombo, ComboKind.Perfect, ComboKind.Combo };

        internal static void Combo(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_COMBO_COUNTING"));

            string[] kinds =
            {
                HemiLang.Get("SET_COMBO_KIND_X"),
                HemiLang.Get("SET_COMBO_KIND_PERFECT"),
                HemiLang.Get("SET_COMBO_KIND_COMBO")
            };
            HemiRows.DropdownRow(content, HemiLang.Get("SET_COMBO_KIND"), kinds,
                Mathf.Max(0, Array.IndexOf(ComboKindOrder, ComboOverlay.Kind)),
                index => ComboOverlay.SetKind(ComboKindOrder[index]));
            HemiRows.NoteRow(content, HemiLang.Get("SET_COMBO_KIND_NOTE"));

            HemiRows.Heading(content, HemiLang.Get("SET_COMBO_POSITION"));

            HemiRows.DropdownRow(content, HemiLang.Get("SET_COMBO_ANCHOR"), OverlayAnchorOptions, (int)ComboOverlay.Anchor,
                index => ComboOverlay.SetAnchor((StateAnchor)index));

            OffsetRows(content, () => ComboOverlay.OffsetX, () => ComboOverlay.OffsetY, ComboOverlay.SetOffset,
                HemiLang.Get("DESC_SET_COMBO_OFFSET"));

            HemiRows.Heading(content, HemiLang.Get("SET_COMBO_TEXT"));

            HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_SIZE"), ComboOverlay.FontSize,
                ComboOverlay.MinimumFontSize, ComboOverlay.MaximumFontSize, true,
                value => ComboOverlay.SetFontSize(Mathf.RoundToInt(value)),
                HemiLang.Get("DESC_SET_COMBO_SIZE"), 0);

            HemiRows.ColorRow(content, HemiLang.Get("SET_COMBO_LABEL_COLOR"), ComboOverlay.ResolveLabelColor(), ComboOverlay.SetLabelColor);
            HemiRows.ColorRow(content, HemiLang.Get("SET_COMBO_COLOR"), ComboOverlay.ResolveColor(), ComboOverlay.SetColor);

            HemiRows.Heading(content, HemiLang.Get("SET_COMBO_ANIMATION"));

            HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_PULSE_SIZE"), ComboOverlay.PulseAmount * 100f, 0f, 150f, true,
                value => ComboOverlay.SetPulse(value / 100f, ComboOverlay.PulseDuration),
                HemiLang.Get("DESC_SET_COMBO_PULSE_SIZE"), 0, "%");

            HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_PULSE_TIME"), ComboOverlay.PulseDuration, 0f, 1f, false,
                value => ComboOverlay.SetPulse(ComboOverlay.PulseAmount, value),
                HemiLang.Get("DESC_SET_COMBO_PULSE_TIME"), 2);

            HemiRows.DropdownRow(content, HemiLang.Get("SET_COMBO_GROW_EASE"), HemiEases.Names,
                HemiEases.IndexOf(ComboOverlay.PulseGrowEase.ToString()), index => ComboOverlay.SetPulseGrowEase(HemiEases.Names[index]));
            HemiRows.DropdownRow(content, HemiLang.Get("SET_COMBO_RETURN_EASE"), HemiEases.Names,
                HemiEases.IndexOf(ComboOverlay.PulseReturnEase.ToString()), index => ComboOverlay.SetPulseReturnEase(HemiEases.Names[index]));

            HemiRows.ToggleRow(content, HemiLang.Get("SET_COMBO_FADE"), HemiLang.Get("DESC_SET_COMBO_FADE"),
                () => ComboOverlay.FadeEnabled,
                value =>
                {
                    ComboOverlay.SetFadeEnabled(value);
                    HemiRoot.Instance?.Refresh();
                });

            if (ComboOverlay.FadeEnabled)
            {
                HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_FADE_IN"), ComboOverlay.FadeIn, 0f, 2f, false,
                    value => ComboOverlay.SetFade(value, ComboOverlay.FadeHold, ComboOverlay.FadeOut), null, 2, "s");

                HemiRows.DropdownRow(content, HemiLang.Get("SET_COMBO_FADE_IN_EASE"), HemiEases.Names,
                    HemiEases.IndexOf(ComboOverlay.FadeInEase.ToString()), index => ComboOverlay.SetFadeInEase(HemiEases.Names[index]));

                HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_FADE_HOLD"), ComboOverlay.FadeHold, 0f, 5f, false,
                    value => ComboOverlay.SetFade(ComboOverlay.FadeIn, value, ComboOverlay.FadeOut),
                    HemiLang.Get("DESC_SET_COMBO_FADE_HOLD"), 2, "s");

                HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_FADE_OUT"), ComboOverlay.FadeOut, 0f, 3f, false,
                    value => ComboOverlay.SetFade(ComboOverlay.FadeIn, ComboOverlay.FadeHold, value), null, 2, "s");

                HemiRows.DropdownRow(content, HemiLang.Get("SET_COMBO_FADE_OUT_EASE"), HemiEases.Names,
                    HemiEases.IndexOf(ComboOverlay.FadeOutEase.ToString()), index => ComboOverlay.SetFadeOutEase(HemiEases.Names[index]));
            }

            HemiRows.Heading(content, HemiLang.Get("SET_COMBO_SHADOW"));

            const float comboShadowRange = HemiTextMaterial.MaximumShadowOffset;

            HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_SHADOW_X"), ComboOverlay.ShadowX, -comboShadowRange, comboShadowRange, false,
                value => ComboOverlay.SetShadow(value, ComboOverlay.ShadowY),
                HemiLang.Get("DESC_SET_COMBO_SHADOW_X"), 2);

            HemiRows.SliderRow(content, HemiLang.Get("SET_COMBO_SHADOW_Y"), ComboOverlay.ShadowY, -comboShadowRange, comboShadowRange, false,
                value => ComboOverlay.SetShadow(ComboOverlay.ShadowX, value), null, 2);

            HemiRows.ColorRow(content, HemiLang.Get("SET_COMBO_SHADOW_COLOR"), ComboOverlay.ResolveShadowColor(),
                ComboOverlay.SetShadowColor,
                HemiLang.Get("DESC_SET_COMBO_SHADOW_COLOR"));

            HemiRows.NoteRow(content, HemiLang.Get("SET_COMBO_NOTE"));
        }

        internal static void TileInfo(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_TILEINFO_SHOW"));

            EntryToggle(content, HemiLang.Get("SET_TILEINFO_ANGLE"), HemiLang.Get("DESC_SET_TILEINFO_ANGLE"),
                HemiTweaksMod.TileInfoAngleEntry, HemiTweaksMod.SetTileInfoOption);
            EntryToggle(content, HemiLang.Get("SET_TILEINFO_BEATS"), HemiLang.Get("DESC_SET_TILEINFO_BEATS"),
                HemiTweaksMod.TileInfoBeatsEntry, HemiTweaksMod.SetTileInfoOption);
            EntryToggle(content, HemiLang.Get("SET_TILEINFO_COUNT"), HemiLang.Get("DESC_SET_TILEINFO_COUNT"),
                HemiTweaksMod.TileInfoCountEntry, HemiTweaksMod.SetTileInfoOption);
            EntryToggle(content, HemiLang.Get("SET_TILEINFO_SECONDS"), HemiLang.Get("DESC_SET_TILEINFO_SECONDS"),
                HemiTweaksMod.TileInfoSecondsEntry, HemiTweaksMod.SetTileInfoOption);

            HemiRows.Heading(content, HemiLang.Get("SET_TILEINFO_RANGE"));
            EntryToggle(content, HemiLang.Get("SET_TILEINFO_EXCLUDE_LAST"),
                HemiLang.Get("DESC_SET_TILEINFO_EXCLUDE_LAST"),
                HemiTweaksMod.TileInfoExcludeLastEntry, HemiTweaksMod.SetTileInfoOption);

            HemiRows.Heading(content, HemiLang.Get("SET_TILEINFO_TEXT"));
            HemiRows.SliderRow(content, HemiLang.Get("SET_TILEINFO_SIZE"), HemiTweaksMod.TileInfoScale * 100f, 15f, 200f, true,
                value => HemiTweaksMod.SetTileInfoScale(value / 100f),
                HemiLang.Get("DESC_SET_TILEINFO_SIZE"), 0, "%");

            HemiRows.Heading(content, HemiLang.Get("SET_TILEINFO_SHADOW"));

            const float shadowRange = HemiTextMaterial.MaximumShadowOffset;

            HemiRows.SliderRow(content, HemiLang.Get("SET_TILEINFO_SHADOW_X"), HemiTweaksMod.TileInfoShadowX, -shadowRange, shadowRange, false,
                value => HemiTweaksMod.SetTileInfoShadow(value, HemiTweaksMod.TileInfoShadowY),
                HemiLang.Get("DESC_SET_TILEINFO_SHADOW_X"), 2);

            HemiRows.SliderRow(content, HemiLang.Get("SET_TILEINFO_SHADOW_Y"), HemiTweaksMod.TileInfoShadowY, -shadowRange, shadowRange, false,
                value => HemiTweaksMod.SetTileInfoShadow(HemiTweaksMod.TileInfoShadowX, value), null, 2);

            HemiRows.ColorRow(content, HemiLang.Get("SET_TILEINFO_SHADOW_COLOR"), HemiTweaksMod.TileInfoShadowColor,
                HemiTweaksMod.SetTileInfoShadowColor,
                HemiLang.Get("DESC_SET_TILEINFO_SHADOW_COLOR"));

            HemiRows.NoteRow(content, HemiLang.Get("SET_TILEINFO_NOTE"));
        }

        private static string tufFolderNotice;

        internal static void Tuf(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_TUF_FOLDER_SECTION"));

            string folder = HemiTweaksMod.TufFolder;
            HemiRows.TextRow(content, HemiLang.Get("SET_TUF_FOLDER"),
                string.IsNullOrWhiteSpace(folder) ? HemiTweaks.Tuf.TufLibrary.DefaultRoot : folder,
                HemiTweaks.Tuf.TufLibrary.DefaultRoot,
                delegate (string value)
                {
                    tufFolderNotice = HemiTweaksMod.SetTufFolder(value, out string error) ? null : error;
                    HemiRoot.Instance?.Refresh();
                },
                HemiLang.Get("DESC_SET_TUF_FOLDER"), 380f);

            if (!string.IsNullOrEmpty(tufFolderNotice))
                HemiRows.ErrorRow(content, tufFolderNotice);

            HemiRows.ActionRow(content, HemiLang.Get("SET_TUF_FOLDER"), "", HemiLang.Get("SET_TUF_FOLDER_OPEN"), delegate
            {
                if (HemiTweaks.Tuf.TufLibrary.EnsureRoot(out string _))
                    HemiShell.OpenFolder(HemiTweaks.Tuf.TufLibrary.Root);
            });

            HemiRows.ActionRow(content, HemiLang.Get("SET_TUF_FOLDER"), "", HemiLang.Get("SET_TUF_FOLDER_RESET"), delegate
            {
                tufFolderNotice = null;
                HemiTweaksMod.SetTufFolder("", out string _);
                HemiRoot.Instance?.Refresh();
            });

            HemiRows.Heading(content, HemiLang.Get("SET_TUF_SHORTCUT"));
            HemiRows.ActionRow(
                content,
                HemiLang.Get("SET_TUF_OPEN_BROWSER"),
                HemiTweaksMod.IsCapturingTufKey ? HemiLang.Get("SET_TUF_PRESS_KEY") : HemiTweaksMod.TufKey.ToString(),
                HemiTweaksMod.IsCapturingTufKey ? HemiLang.Get("SET_TUF_CANCEL") : HemiLang.Get("SET_TUF_CHANGE"),
                delegate
                {
                    HemiTweaksMod.BeginTufKeyCapture(!HemiTweaksMod.IsCapturingTufKey);
                    HemiRoot.Instance?.Refresh();
                },
                HemiLang.Get("DESC_SET_TUF_OPEN_BROWSER"));

            HemiRows.Heading(content, HemiLang.Get("SET_TUF_GUEST"));
            HemiRows.NoteRow(content, HemiLang.Get("SET_TUF_GUEST_NOTE"));
        }

        internal static void EffectRemover(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_WHAT"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_EFFECTREMOVER_WHAT_NOTE"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_EFFECTREMOVER_RELOAD_NOTE"));

            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_DRAWN_OVER"));
            EffectRemoverGroup(content, "DrawnOver");

            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_GAMEPLAY"));
            HemiRows.NoteRow(content, HemiLang.Get("SET_EFFECTREMOVER_GAMEPLAY_NOTE"));
            EffectRemoverGroup(content, "Gameplay");

            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_PLANETS"));
            EffectRemoverGroup(content, "Planets");

            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_TRACK"));
            EffectRemoverGroup(content, "Track");

            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_REPLACE"));
            EffectRemoverGroup(content, "Replace");
            EffectRemoverGroup(content, "Zoom");
            HemiRows.SliderRow(content, HemiLang.Get("SET_EFFECTREMOVER_CAMERA_ZOOM"), HemiTweaks.EffectRemover.CameraZoom, 100f, 1000f, true,
                value => HemiTweaksMod.SetEffectRemoverCameraZoom(value),
                HemiLang.Get("DESC_SET_EFFECTREMOVER_CAMERA_ZOOM"));

            HemiRows.Heading(content, HemiLang.Get("SET_EFFECTREMOVER_EDITOR"));
            EntryToggle(content, HemiLang.Get("SET_EFFECTREMOVER_BLOCK_SAVE"),
                HemiLang.Get("DESC_SET_EFFECTREMOVER_BLOCK_SAVE"),
                HemiTweaksMod.EffectRemoverBlockSaveEntry, HemiTweaksMod.SetEffectRemoverOption);
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_EFFECTREMOVER_BLOCK_SAVE_NOTE"));
        }

        private static void EffectRemoverGroup(RectTransform content, string group)
        {
            foreach (EffectRemoverOption option in HemiTweaks.EffectRemover.Options)
            {
                if (option.Group == group)
                    EntryToggle(content, option.Label, option.Hint, option.Entry, HemiTweaksMod.SetEffectRemoverOption);
            }
        }

        internal static void NoCheckpoint(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_NOCHECKPOINT_WHAT"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_NOCHECKPOINT_WHAT_NOTE"));

            HemiRows.Heading(content, HemiLang.Get("SET_NOCHECKPOINT_NOTES"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_NOCHECKPOINT_NOTE_ICON"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_NOCHECKPOINT_NOTE_STAT"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_NOCHECKPOINT_NOTE_MODES"));
        }

        internal static void AutoInputOffset(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_AUTOOFFSET_OFFSET"));

            HemiRows.TextRow(content, HemiLang.Get("SET_AUTOOFFSET_CURRENT"),
                AutoOffset.Format(AutoOffset.CurrentOffset), "0",
                text =>
                {
                    if (float.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                        AutoOffset.ApplyOffset(AutoOffset.DecimalEnabled ? value : Mathf.RoundToInt(value));
                    HemiRoot.Instance?.Refresh();
                },
                HemiLang.Get("DESC_SET_AUTOOFFSET_CURRENT", AutoOffset.CurrentDeviceName), 140f);

            if (AutoOffset.RunHits > 0)
            {
                HemiRows.NoteRow(content, HemiLang.Get("SET_AUTOOFFSET_RUN", AutoOffset.RunHits,
                    AutoOffset.RunAverage.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture)));
            }

            HemiRows.Heading(content, HemiLang.Get("SET_AUTOOFFSET_CALIBRATION"));

            HemiRows.ToggleRow(content, HemiLang.Get("SET_AUTOOFFSET_POPUP"), HemiLang.Get("DESC_SET_AUTOOFFSET_POPUP"),
                () => AutoOffset.PopupEnabled,
                value =>
                {
                    AutoOffset.SetPopup(value);
                    HemiRoot.Instance?.Refresh();
                });

            if (AutoOffset.PopupEnabled)
            {
                OffsetRows(content, () => AutoOffset.PopupX, () => AutoOffset.PopupY, AutoOffset.SetPopupOffset,
                    HemiLang.Get("DESC_SET_AUTOOFFSET_POPUP_OFFSET"));

                HemiRows.SliderRow(content, HemiLang.Get("SET_AUTOOFFSET_POPUP_SCALE"), AutoOffset.PopupScale,
                    AutoOffset.MinimumPopupScale, AutoOffset.MaximumPopupScale, true,
                    value => AutoOffset.SetPopupScale(Mathf.RoundToInt(value)),
                    HemiLang.Get("DESC_SET_AUTOOFFSET_POPUP_SCALE"), 0, "%");
            }

            HemiRows.ToggleRow(content, HemiLang.Get("SET_AUTOOFFSET_RECORD"), HemiLang.Get("DESC_SET_AUTOOFFSET_RECORD"),
                () => AutoOffset.RecordEnabled, AutoOffset.SetRecord);

            HemiRows.ToggleRow(content, HemiLang.Get("SET_AUTOOFFSET_DECIMAL"), HemiLang.Get("DESC_SET_AUTOOFFSET_DECIMAL"),
                () => AutoOffset.DecimalEnabled,
                value =>
                {
                    AutoOffset.SetDecimal(value);
                    HemiRoot.Instance?.Refresh();
                });

            if (AutoOffset.DecimalEnabled)
                HemiRows.NoteRow(content, HemiLang.Get("SET_AUTOOFFSET_DECIMAL_NOTE"));

            HemiRows.Heading(content, HemiLang.Get("SET_AUTOOFFSET_RECORDS"));

            IReadOnlyList<AutoOffsetRecord> records = AutoOffset.Records;
            if (records.Count == 0)
            {
                HemiRows.NoteRow(content, HemiLang.Get("SET_AUTOOFFSET_NO_RECORDS"));
            }
            else
            {
                foreach (AutoOffsetRecord record in records)
                    AutoOffsetRecordRow(content, record);

                HemiRows.ActionRow(content, "", null, HemiLang.Get("SET_AUTOOFFSET_CLEAR"), delegate
                {
                    AutoOffset.ClearRecords();
                    HemiRoot.Instance?.Refresh();
                }, null, HemiTheme.Colors.Negative);
            }

            HemiRows.NoteRow(content, HemiLang.Get("SET_AUTOOFFSET_NOTE"));
        }

        private static void AutoOffsetRecordRow(RectTransform parent, AutoOffsetRecord record)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiRows.Card(parent);

            string title = string.IsNullOrEmpty(record.Level) ? HemiLang.Get("SET_AUTOOFFSET_UNKNOWN_LEVEL") : record.Level;
            string detail = HemiLang.Get("SET_AUTOOFFSET_RECORD_LINE",
                record.Time ?? "",
                AutoOffset.ReasonText(record.Reason),
                record.Hits,
                record.Average.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture),
                AutoOffset.Format(record.Target));
            HemiRows.Label(row, title, detail, 160f, 1f);

            Button apply = HemiKit.Button("Apply", row, HemiLang.Get("SET_AUTOOFFSET_APPLY"), colors.Accent, Color.white, delegate
            {
                AutoOffset.ApplyOffset(record.Target);
                HemiRoot.Instance?.Refresh();
            }, 13f, 8f);
            HemiKit.Size(apply.gameObject, 84f, HemiTheme.Row(32f));

            Button remove = HemiKit.Button("Remove", row, "X", colors.Field, colors.Muted, delegate
            {
                AutoOffset.RemoveRecord(record);
                HemiRoot.Instance?.Refresh();
            }, 13f, 8f);
            HemiKit.Size(remove.gameObject, 32f, HemiTheme.Row(32f));
        }

        internal static void UnlockLimits(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("SET_UNLOCKLIMITS_COVERS"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_UNLOCKLIMITS_COVERS_NOTE"));

            HemiRows.Heading(content, HemiLang.Get("SET_UNLOCKLIMITS_WARNING"));
            HemiRows.NoteRow(
                content,
                HemiLang.Get("SET_UNLOCKLIMITS_WARNING_NOTE"));
        }

        private static void OffsetRows(RectTransform content, Func<float> x, Func<float> y,
            Action<float, float> set, string hint)
        {
            HemiRows.SliderRow(content, "X", x(), -960f, 960f, false, value => set(value, y()), hint, 0);
            HemiRows.SliderRow(content, "Y", y(), -540f, 540f, false, value => set(x(), value), null, 0);
        }

        private static void ColorRow(RectTransform content, string label, PlanetColorTarget target)
        {
            HemiRows.ColorRow(
                content,
                label,
                HemiTweaksMod.GetPlanetColor(target),
                value => HemiTweaksMod.SetPlanetColor(target, value));
        }

        private static void OverlayRows(RectTransform content, bool red, string label, string path, float scale)
        {
            HemiRows.FileRow(
                content,
                label,
                path,
                HemiLang.Get("SET_PLANETCOLOR_OVERLAY_FILTER"),
                ImageExtensions,
                HemiLang.Get("SET_PLANETCOLOR_OVERLAY_PICK", label.ToLowerInvariant()),
                value =>
                {
                    HemiTweaksMod.SetPlanetOverlayPath(red, value);
                    HemiRoot.Instance?.Refresh();
                });

            HemiRows.SliderRow(
                content,
                HemiLang.Get("SET_PLANETCOLOR_OVERLAY_SCALE", label),
                scale,
                0.0001f,
                5f,
                false,
                value => HemiTweaksMod.SetPlanetOverlayScale(red, value),
                null,
                3);
        }
    }

    internal static class HemiStatePage
    {
        private static string[] AnchorOptions => StateGroupLayout.AnchorOptions();

        private static string[] ImageExtensions => GCS.SupportedImageFiles;
        private static readonly string[] FontExtensions = { "ttf", "otf", "ttc" };

        private static int openGroup = -1;

        private static void OpenGroup(int index)
        {
            if (openGroup == index)
                return;

            openGroup = index;
        }

        private static Action<T> StyleSetter<T>(Action<T> apply) => value => { apply(value); StateGroupStore.MarkStyleChanged(); };

        private static Action<T> ChangeSetter<T>(Action<T> apply) => value => { apply(value); StateGroupStore.MarkChanged(); };

        internal static void Build(RectTransform content)
        {
            IReadOnlyList<StateGroup> groups = StateGroupStore.Groups;
            if (openGroup >= 0 && openGroup < groups.Count)
            {
                BuildGroup(content, groups[openGroup]);
                return;
            }

            OpenGroup(-1);
            BuildList(content, groups);
        }

        internal static void Reset()
        {
            OpenGroup(-1);
        }

        private static void BuildList(RectTransform content, IReadOnlyList<StateGroup> groups)
        {
            HemiRows.Heading(content, HemiLang.Get("STATE_FONT"));
            HemiRows.ActionRow(
                content,
                HemiLang.Get("STATE_TYPEFACE"),
                StateOverlay.FontDisplayName,
                HemiLang.Get("STATE_CHANGE"),
                delegate { OpenFontPicker(content); },
                HemiLang.Get("DESC_STATE_TYPEFACE"));

            HemiRows.Heading(content, HemiLang.Get("STATE_GROUPS"));

            if (groups.Count == 0)
                HemiRows.NoteRow(content, HemiLang.Get("STATE_NO_GROUPS"));

            for (int i = 0; i < groups.Count; i++)
                BuildGroupRow(content, groups[i], i, groups.Count);

            AddCard(content, "STATE_ADD_GROUP", "STATE_ADD_GROUP_BUTTON", delegate
            {
                StateGroupStore.AddGroup();
                OpenGroup(StateGroupStore.Groups.Count - 1);
                HemiRoot.Instance?.Refresh(HemiTransition.Forward);
            });
        }

        private static void AddCard(RectTransform content, string labelKey, string buttonKey, Action<RectTransform> onClick)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiRows.Card(content);
            TextMeshProUGUI label = HemiKit.Text("Label", row, HemiLang.Get(labelKey), 15f, colors.Text, true);
            HemiKit.Size(label.gameObject, 150f, HemiTheme.Row(30f), 1f);

            Button add = HemiKit.Button("Add", row, HemiLang.Get(buttonKey), colors.Accent, Color.white,
                delegate { onClick(row); }, 13f, 8f);
            HemiKit.Size(add.gameObject, 104f, HemiTheme.Row(32f));
        }

        private static void BuildGroupRow(RectTransform content, StateGroup group, int index, int total)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiRows.Card(content, 52f);

            HemiKit.Switch(row, () => group.Enabled, ChangeSetter<bool>(value => group.Enabled = value), 44f, 24f);

            TwoLineColumn(row, 180f, group.Name, true, "Detail",
                HemiLang.Get("STATE_GROUP_DETAIL", CountStats(group), StateGroupLayout.DisplayName(group.Anchor)));

            int captured = index;
            MoveButtons(row, index, total, delta => { StateGroupStore.MoveGroup(group, delta); OpenGroup(-1); });

            Button edit = HemiKit.Button("Edit", row, HemiLang.Get("STATE_EDIT"), colors.Accent, Color.white, delegate
            {
                OpenGroup(captured);
                HemiRoot.Instance?.Refresh(HemiTransition.Forward);
            }, 13f, 8f);
            HemiKit.Size(edit.gameObject, 66f, HemiTheme.Row(32f));

            Button remove = HemiKit.Button("Remove", row, "X", colors.Field, colors.Negative, delegate
            {
                HemiPopup.ConfirmDestructive(row,
                    HemiLang.Get("STATE_DELETE_GROUP_CONFIRM", group.Name),
                    HemiLang.Get("UI_DELETE"),
                    delegate
                    {
                        StateGroupStore.RemoveGroup(group);
                        OpenGroup(-1);
                        HemiRoot.Instance?.Refresh();
                    });
            }, 13f, 8f);
            HemiKit.Size(remove.gameObject, 32f, HemiTheme.Row(32f));
        }

        private static void TwoLineColumn(RectTransform row, float width, string title, bool literalTitle,
            string detailName, string detail)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform column = HemiKit.VBox("Text", row, 0f, new RectOffset(0, 0, 0, 0), false);
            HemiKit.Size(column.gameObject, width, HemiTheme.Row(42f), 1f);

            TextMeshProUGUI name = HemiKit.Text("Name", column, title, 15f, colors.Text, true);
            if (literalTitle)
                HemiKit.ShowMarkupLiterally(name);
            HemiKit.Size(name.gameObject, -1f, HemiTheme.Row(24f), 1f);

            TextMeshProUGUI second = HemiKit.Text(detailName, column, detail, 12f, colors.Muted);
            HemiKit.Size(second.gameObject, -1f, HemiTheme.Row(18f), 1f);
        }

        private static void BuildGroup(RectTransform content, StateGroup group)
        {
            HemiColors colors = HemiTheme.Colors;

            RectTransform back = HemiRows.Card(content);
            Button button = HemiKit.Button("Back", back, HemiLang.Get("STATE_BACK_ALL_GROUPS"), colors.Field, colors.Text, delegate
            {
                OpenGroup(-1);
                HemiRoot.Instance?.Refresh(HemiTransition.Back);
            }, 13f, 8f);
            HemiKit.Size(button.gameObject, 130f, HemiTheme.Row(32f));

            TextMeshProUGUI title = HemiKit.Text("Title", back, group.Name, 16f, colors.Text, true, TextAlignmentOptions.Right);
            HemiKit.ShowMarkupLiterally(title);
            HemiKit.Size(title.gameObject, 150f, HemiTheme.Row(30f), 1f);

            HemiRows.Heading(content, HemiLang.Get("STATE_GROUP"));
            HemiRows.TextRow(content, HemiLang.Get("STATE_NAME"), group.Name, HemiLang.Get("STATE_GROUP"), value =>
            {
                group.Name = string.IsNullOrWhiteSpace(value) ? "Group" : value;
                StateGroupStore.MarkChanged();
                HemiRoot.Instance?.Refresh();
            });

            HemiRows.ToggleRow(content, HemiLang.Get("STATE_ENABLED"), HemiLang.Get("DESC_STATE_ENABLED"),
                () => group.Enabled, ChangeSetter<bool>(value => group.Enabled = value));

            HemiRows.DropdownRow(content, HemiLang.Get("STATE_ANCHOR"), AnchorOptions, (int)group.Anchor, index =>
            {
                group.Anchor = (StateAnchor)index;
                StateGroupStore.MarkChanged();
                HemiRoot.Instance?.Refresh();
            });

            HemiRows.SliderRow(content, HemiLang.Get("STATE_X_OFFSET"), group.OffsetX, -HemiTweaksMod.DefaultCanvasWidth, HemiTweaksMod.DefaultCanvasWidth, true,
                StyleSetter<float>(value => group.OffsetX = value), HemiLang.Get("DESC_STATE_X_OFFSET"));

            HemiRows.SliderRow(content, HemiLang.Get("STATE_Y_OFFSET"), group.OffsetY, -HemiTweaksMod.DefaultCanvasHeight, HemiTweaksMod.DefaultCanvasHeight, true,
                StyleSetter<float>(value => group.OffsetY = value));

            HemiRows.SliderRow(content, HemiLang.Get("STATE_FONT_SIZE"), group.FontSize,
                StateOverlay.MinimumFontSize, StateOverlay.MaximumFontSize, true,
                StyleSetter<float>(value => group.FontSize = Mathf.RoundToInt(value)));

            HemiRows.ColorRow(content, HemiLang.Get("STATE_COLOR"), group.ResolveColor(),
                StyleSetter<Color>(value => group.ColorHex = StateGroupStore.ToHex(value)), HemiLang.Get("DESC_STATE_COLOR"));

            HemiRows.ToggleRow(content, HemiLang.Get("STATE_HORIZONTAL"), HemiLang.Get("DESC_STATE_HORIZONTAL"),
                () => group.Horizontal, ChangeSetter<bool>(value => group.Horizontal = value));

            HemiRows.SliderRow(content, HemiLang.Get("STATE_SPACING"), group.Spacing, 0f, 100f, true,
                StyleSetter<float>(value => group.Spacing = value), null, 0, "px");

            HemiRows.Heading(content, HemiLang.Get("STATE_SHADOW"));

            const float shadowRange = HemiTextMaterial.MaximumShadowOffset;

            HemiRows.SliderRow(content, HemiLang.Get("STATE_SHADOW_X"), group.ShadowX, -shadowRange, shadowRange, false,
                StyleSetter<float>(value => group.ShadowX = value), HemiLang.Get("DESC_STATE_SHADOW_X"), 2);

            HemiRows.SliderRow(content, HemiLang.Get("STATE_SHADOW_Y"), group.ShadowY, -shadowRange, shadowRange, false,
                StyleSetter<float>(value => group.ShadowY = value), null, 2);

            HemiRows.ColorRow(content, HemiLang.Get("STATE_SHADOW_COLOR"), group.ResolveShadowColor(),
                StyleSetter<Color>(value => group.ShadowColorHex = StateGroupStore.ToHex(value)), HemiLang.Get("DESC_STATE_SHADOW_COLOR"));

            HemiRows.Heading(content, HemiLang.Get("STATE_MOTION"));
            MotionRows(content, group.Entrance, true);
            MotionRows(content, group.Exit, false);

            HemiRows.Heading(content, HemiLang.Get("STATE_STATS"));

            List<StateStat> stats = group.Stats;
            if (stats.Count == 0)
                HemiRows.NoteRow(content, HemiLang.Get("STATE_GROUP_EMPTY"));

            for (int i = 0; i < stats.Count; i++)
                BuildStatRow(content, group, stats[i], i, stats.Count);

            AddCard(content, "STATE_ADD_STAT", "STATE_ADD_STAT_BUTTON", row => OpenStatPicker(row, group));
        }

        private static void MotionRows(RectTransform content, StateMotion motion, bool entrance)
        {
            HemiRows.ToggleRow(content,
                HemiLang.Get(entrance ? "STATE_ENTRANCE" : "STATE_EXIT"),
                HemiLang.Get(entrance ? "DESC_STATE_ENTRANCE" : "DESC_STATE_EXIT"),
                () => motion.Enabled,
                value =>
                {
                    motion.Enabled = value;
                    StateGroupStore.MarkStyleChanged();
                    HemiRoot.Instance?.Refresh();
                });

            if (!motion.Enabled)
                return;

            HemiRows.DropdownRow(content, HemiLang.Get("STATE_MOTION_DIRECTION"),
                entrance ? EntranceEdgeOptions : ExitEdgeOptions, (int)motion.Edge,
                StyleSetter<int>(index => motion.Edge = (StateSlideEdge)index));

            HemiRows.DropdownRow(content, HemiLang.Get("STATE_MOTION_EASE"), HemiEases.Names,
                Mathf.Max(0, HemiEases.IndexOf(motion.Ease)), StyleSetter<int>(index => motion.Ease = HemiEases.Names[index]));

            HemiRows.SliderRow(content, HemiLang.Get("STATE_MOTION_SECONDS"), motion.Seconds,
                StateMotion.MinimumSeconds, StateMotion.MaximumSeconds, false,
                StyleSetter<float>(value => motion.Seconds = value), HemiLang.Get("DESC_STATE_MOTION_SECONDS"), 2, "s");
        }

        private static string[] EntranceEdgeOptions => new[]
        {
            HemiLang.Get("STATE_FROM_LEFT"), HemiLang.Get("STATE_FROM_RIGHT"),
            HemiLang.Get("STATE_FROM_TOP"), HemiLang.Get("STATE_FROM_BOTTOM")
        };

        private static string[] ExitEdgeOptions => new[]
        {
            HemiLang.Get("STATE_TO_LEFT"), HemiLang.Get("STATE_TO_RIGHT"),
            HemiLang.Get("STATE_TO_TOP"), HemiLang.Get("STATE_TO_BOTTOM")
        };

        private static void BuildStatRow(RectTransform content, StateGroup group, StateStat stat, int index, int total)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiRows.Card(content, 52f);

            HemiKit.Switch(row, () => stat.Enabled, ChangeSetter<bool>(value => stat.Enabled = value), 44f, 24f);

            TwoLineColumn(row, 170f, StateGroupStore.DisplayName(stat.Kind), false, "Preview", Preview(stat));

            MoveButtons(row, index, total, delta => StateGroupStore.MoveStat(group, stat, delta));

            Button edit = HemiKit.Button("Edit", row, HemiLang.Get("STATE_EDIT"), colors.Field, colors.Text, delegate
            {
                OpenStatEditor(row, group, stat);
            }, 13f, 8f);
            HemiKit.Size(edit.gameObject, 66f, HemiTheme.Row(32f));

            Button remove = HemiKit.Button("Remove", row, "X", colors.Field, colors.Negative, delegate
            {
                group.Stats.Remove(stat);
                StateGroupStore.MarkChanged();
                HemiRoot.Instance?.Refresh();
            }, 13f, 8f);
            HemiKit.Size(remove.gameObject, 32f, HemiTheme.Row(32f));
        }

        private static void MoveButtons(RectTransform row, int index, int total, Action<int> move)
        {
            HemiColors colors = HemiTheme.Colors;

            Button up = HemiKit.Button("Up", row, "▲", index > 0 ? colors.Field : colors.Card,
                index > 0 ? colors.Text : colors.Muted,
                delegate
                {
                    if (index <= 0)
                        return;
                    move(-1);
                    HemiRoot.Instance?.Refresh();
                }, 11f, 6f);
            HemiKit.Size(up.gameObject, 30f, HemiTheme.Row(32f));

            Button down = HemiKit.Button("Down", row, "▼", index < total - 1 ? colors.Field : colors.Card,
                index < total - 1 ? colors.Text : colors.Muted,
                delegate
                {
                    if (index >= total - 1)
                        return;
                    move(1);
                    HemiRoot.Instance?.Refresh();
                }, 11f, 6f);
            HemiKit.Size(down.gameObject, 30f, HemiTheme.Row(32f));
        }

        private static void PlayingOnlyRow(RectTransform body, StateStat stat)
        {
            if (!StateGroupStore.SupportsAlwaysVisible(stat.Kind))
                return;

            HemiRows.ToggleRow(body, HemiLang.Get("STATE_PLAYING_ONLY"), HemiLang.Get("DESC_STATE_PLAYING_ONLY"),
                () => stat.PlayingOnly, ChangeSetter<bool>(value => stat.PlayingOnly = value));
        }

        private static void OpenStatPicker(RectTransform anchor, StateGroup group)
        {
            HemiColors colors = HemiTheme.Colors;
            HemiPopup.OpenList(anchor, HemiTheme.Col(240f), 320f, body =>
            {
                string category = null;
                foreach (StateStatKind kind in StateGroupStore.PickerOrder())
                {
                    string next = StateGroupStore.Category(kind);
                    if (!string.Equals(next, category, StringComparison.Ordinal))
                    {
                        category = next;
                        TextMeshProUGUI heading = HemiKit.Text(
                            "Category", body, StateGroupStore.CategoryDisplayName(category).ToUpperInvariant(),
                            11f, colors.Muted, true);
                        HemiKit.Size(heading.gameObject, -1f, HemiTheme.Row(22f), 1f);
                    }

                    StateStatKind captured = kind;
                    Button pick = HemiKit.Button(
                        kind.ToString(), body, StateGroupStore.DisplayName(kind), colors.Card, colors.Text,
                        delegate
                        {
                            group.Stats.Add(StateGroupStore.CreateStat(captured));
                            StateGroupStore.MarkChanged();
                            HemiPopup.Close();
                            HemiRoot.Instance?.Refresh();
                        }, 13f, 6f);
                    HemiKit.Size(pick.gameObject, -1f, HemiTheme.Row(30f), 1f);
                }
            });
        }

        private static void OpenStatEditor(RectTransform anchor, StateGroup group, StateStat stat)
        {
            HemiPopup.OpenList(anchor, 400f, 340f, body =>
            {
                HemiRows.NoteRow(body, StateGroupStore.DisplayName(stat.Kind));

                if (stat.Kind == StateStatKind.JudgementCounter)
                    HemiRows.NoteRow(body, JudgementCounter.Describe());

                if (stat.Kind == StateStatKind.Image)
                {
                    HemiRows.FileRow(body, HemiLang.Get("STATE_IMAGE"), stat.ImagePath, HemiLang.Get("STATE_IMAGE"),
                        ImageExtensions, HemiLang.Get("STATE_IMAGE_PICK"), value =>
                    {
                        StateImageCache.Invalidate(stat.ImagePath);
                        stat.ImagePath = value;
                        StateGroupStore.MarkChanged();
                        HemiPopup.Close();
                        HemiRoot.Instance?.Refresh();
                    });

                    HemiRows.SliderRow(body, HemiLang.Get("STATE_HEIGHT"), stat.ImageHeight, 4f, 512f, true,
                        ChangeSetter<float>(value => stat.ImageHeight = value), null, 0, "px");

                    PlayingOnlyRow(body, stat);
                    return;
                }

                if (stat.Kind == StateStatKind.Text)
                {
                    HemiRows.TextRow(body, HemiLang.Get("STATE_TEXT"), stat.Text, HemiLang.Get("STATE_TEXT"),
                        StyleSetter<string>(value => stat.Text = value), null, 170f);
                }

                HemiRows.TextRow(body, HemiLang.Get("STATE_PREFIX"), stat.Label, "",
                    StyleSetter<string>(value => stat.Label = value), null, 150f);

                HemiRows.TextRow(body, HemiLang.Get("STATE_SEPARATOR"), stat.ResolveSeparator(), "",
                    StyleSetter<string>(value => stat.Separator = value), HemiLang.Get("DESC_STATE_SEPARATOR"), 150f);

                HemiRows.TextRow(body, HemiLang.Get("STATE_SUFFIX"), stat.Suffix, "",
                    StyleSetter<string>(value => stat.Suffix = value), null, 150f);

                if (StateGroupStore.SupportsPercent(stat.Kind))
                {
                    HemiRows.ToggleRow(body, HemiLang.Get("STATE_AS_PERCENT"), HemiLang.Get("DESC_STATE_AS_PERCENT"),
                        () => stat.AsPercent, StyleSetter<bool>(value => stat.AsPercent = value));
                }

                if (StateGroupStore.SupportsRemoveRichText(stat.Kind))
                {
                    HemiRows.ToggleRow(body, HemiLang.Get("STATE_REMOVE_RICH_TEXT"), HemiLang.Get("DESC_STATE_REMOVE_RICH_TEXT"),
                        () => stat.RemoveRichText, StyleSetter<bool>(value => stat.RemoveRichText = value));
                }

                if (StateGroupStore.SupportsDecimals(stat.Kind))
                {
                    HemiRows.SliderRow(body, HemiLang.Get("STATE_DECIMALS"), stat.Decimals, 0f, 6f, true,
                        StyleSetter<float>(value => stat.Decimals = Mathf.RoundToInt(value)));
                }

                if (stat.Kind != StateStatKind.Text)
                {
                    HemiRows.SliderRow(body, HemiLang.Get("STATE_REFRESH"), stat.RefreshInterval, 0f, 2f, false,
                        StyleSetter<float>(value => stat.RefreshInterval = value), HemiLang.Get("DESC_STATE_REFRESH"), 2, "s");
                }

                PlayingOnlyRow(body, stat);

                bool hasOwnColor = !string.IsNullOrEmpty(stat.ColorHex);
                HemiRows.ColorRow(body, HemiLang.Get("STATE_COLOR"), hasOwnColor ? StateGroupStore.ParseColor(stat.ColorHex, group.ResolveColor()) : group.ResolveColor(),
                    StyleSetter<Color>(value => stat.ColorHex = StateGroupStore.ToHex(value)));

                if (!hasOwnColor)
                    return;

                HemiRows.ActionRow(body, HemiLang.Get("STATE_COLOR"), HemiLang.Get("STATE_COLOR_OVERRIDDEN"),
                    HemiLang.Get("STATE_USE_GROUP_COLOR"), delegate
                {
                    stat.ColorHex = "";
                    StateGroupStore.MarkChanged();
                    HemiPopup.Close();
                    HemiRoot.Instance?.Refresh();
                });
            });
        }

        private static void OpenFontPicker(RectTransform anchor)
        {
            HemiColors colors = HemiTheme.Colors;
            HemiPopup.OpenList(anchor, 280f, 340f, body =>
            {
                Button standard = HemiKit.Button("Default", body, HemiLang.Get("STATE_FONT_DEFAULT"), colors.Card, colors.Text, delegate
                {
                    StateOverlay.SelectDefaultFont();
                    HemiPopup.Close();
                    HemiRoot.Instance?.Refresh();
                }, 13f, 6f);
                HemiKit.Size(standard.gameObject, -1f, HemiTheme.Row(30f), 1f);

                Button file = HemiKit.Button("File", body, HemiLang.Get("STATE_FONT_CHOOSE_FILE"), colors.Card, colors.Text, delegate
                {
                    string picked = HemiFilePicker.Pick(HemiLang.Get("STATE_FONT"), FontExtensions, HemiLang.Get("STATE_FONT_PICK"));
                    if (!string.IsNullOrEmpty(picked))
                        StateOverlay.SelectFontFile(picked);
                    HemiPopup.Close();
                    HemiRoot.Instance?.Refresh();
                }, 13f, 6f);
                HemiKit.Size(file.gameObject, -1f, HemiTheme.Row(30f), 1f);

                TextMeshProUGUI heading = HemiKit.Text(
                    "Installed", body, HemiLang.Get("STATE_FONT_INSTALLED").ToUpperInvariant(), 11f, colors.Muted, true);
                HemiKit.Size(heading.gameObject, -1f, HemiTheme.Row(24f), 1f);

                IReadOnlyList<string> names = StateOverlay.InstalledFontNames;
                for (int i = 0; i < names.Count; i++)
                {
                    string name = names[i];
                    bool selected = StateOverlay.IsSystemFontSelected(name);
                    Button item = HemiKit.Button(
                        "Font", body, name, selected ? colors.AccentSoft : colors.Card,
                        selected ? colors.Accent : colors.Text,
                        delegate
                        {
                            StateOverlay.SelectSystemFont(name);
                            HemiPopup.Close();
                            HemiRoot.Instance?.Refresh();
                        }, 13f, 6f);
                    HemiKit.Size(item.gameObject, -1f, HemiTheme.Row(28f), 1f);
                }
            });
        }

        private static string Preview(StateStat stat)
        {
            string text = StateGroupVisual.ComposeText(stat);
            return string.IsNullOrEmpty(text) ? HemiLang.Get("STATE_PREVIEW_EMPTY") : text;
        }

        private static int CountStats(StateGroup group)
        {
            int count = 0;
            for (int i = 0; i < group.Stats.Count; i++)
            {
                if (group.Stats[i] != null && group.Stats[i].Enabled)
                    count++;
            }
            return count;
        }
    }
}
