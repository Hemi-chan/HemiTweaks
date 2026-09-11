using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ADOFAI;
using HarmonyLib;
using MelonLoader;
using UnityEngine.UI;

namespace HemiTweaks
{
    internal sealed class EffectRemoverOption
    {
        internal string Key;

        internal string Group;

        internal string LangKey;

        internal LevelEventType[] Events = new LevelEventType[0];

        internal Action<LevelData> Edit;

        internal MelonPreferences_Entry<bool> Entry;

        internal bool On => Entry != null && Entry.Value;

        internal string Label => Interface.HemiLang.Get(LangKey);

        internal string Hint => Interface.HemiLang.Get("DESC_" + LangKey);
    }

    internal static class EffectRemover
    {
        private static readonly List<EffectRemoverOption> options = new List<EffectRemoverOption>();

        private static MelonPreferences_Entry<float> cameraZoomEntry;

        internal static IReadOnlyList<EffectRemoverOption> Options => options;

        internal static float CameraZoom => cameraZoomEntry == null ? 250f : cameraZoomEntry.Value;

        internal static MelonPreferences_Entry<float> CameraZoomEntry => cameraZoomEntry;

        internal static void RegisterOptions(MelonPreferences_Category category)
        {
            if (category == null || options.Count > 0)
                return;

            Add(category, "DrawnOver", "Filter", "ER_FILTER", "Filters", "Flash, bloom, screen shake, hall of mirrors, tiling and scrolling.", true,
                LevelEventType.Flash, LevelEventType.SetFilter, LevelEventType.HallOfMirrors,
                LevelEventType.ShakeScreen, LevelEventType.Bloom, LevelEventType.ScreenTile,
                LevelEventType.ScreenScroll);

            Add(category, "DrawnOver", "AdvancedFilter", "ER_ADVANCED_FILTER", "Advanced filters", "The advanced filter event.", true,
                LevelEventType.SetFilterAdvanced);

            Add(category, "DrawnOver", "Particles", "ER_PARTICLES", "Particles", "Particle emitters and their settings.", true,
                LevelEventType.AddParticle, LevelEventType.SetParticle, LevelEventType.EmitParticle);

            Add(category, "DrawnOver", "Decorations", "ER_DECORATIONS", "Decorations and text", "Sprites, text and objects placed over the level.", true,
                LevelEventType.AddDecoration, LevelEventType.MoveDecorations, LevelEventType.AddText,
                LevelEventType.SetText, LevelEventType.SetDefaultText, LevelEventType.AddObject,
                LevelEventType.SetObject);

            Add(category, "DrawnOver", "Background", "ER_BACKGROUND", "Background", "Custom backgrounds, including the background video.", true,
                LevelEventType.CustomBackground).Edit = ResetBackground;

            Add(category, "DrawnOver", "FrameRate", "ER_FRAME_RATE", "Frame rate", "Events that drive the frame rate for effect.", true,
                LevelEventType.SetFrameRate);

            Add(category, "DrawnOver", "Scripting", "ER_SCRIPTING", "Components and calls", "AddComponent and CallMethod, which exist to drive the above.", true,
                LevelEventType.AddComponent, LevelEventType.CallMethod);

            Add(category, "Gameplay", "Camera", "ER_CAMERA", "Camera movement", "Camera pans and zooms. Changes what you can see, not just how it looks.", false,
                LevelEventType.MoveCamera);

            Add(category, "Gameplay", "RepeatEvents", "ER_REPEAT_EVENTS", "Repeat events", "The event that replays other events. Also stops it repeating anything left.", false,
                LevelEventType.RepeatEvents);

            Add(category, "Gameplay", "Hide", "ER_HIDE", "Hidden icons and judgements", "The Hide event. Removing it puts back whatever the level was hiding.", false,
                LevelEventType.Hide);

            Add(category, "Gameplay", "HitSound", "ER_HIT_SOUND", "Hit sounds", "Per-tile hitsounds and one-off sounds. Timing cues, so off by default.", false,
                LevelEventType.SetHitsound, LevelEventType.PlaySound);

            Add(category, "Gameplay", "HoldSound", "ER_HOLD_SOUND", "Hold sounds", "Sounds set for hold tiles.", false,
                LevelEventType.SetHoldSound);

            Add(category, "Gameplay", "Checkpoint", "ER_CHECKPOINT", "Checkpoints", "Checkpoint events. No Checkpoint does the same without touching the level.", false,
                LevelEventType.Checkpoint);

            Add(category, "Planets", "PlanetOrbit", "ER_PLANET_ORBIT", "Planet orbit", "Planet rotation events.", false,
                LevelEventType.SetPlanetRotation);
            Add(category, "Planets", "PlanetScale", "ER_PLANET_SCALE", "Planet scale", "Planet size events.", false,
                LevelEventType.ScalePlanets);
            Add(category, "Planets", "PlanetRadius", "ER_PLANET_RADIUS", "Planet radius", "Orbit radius events.", false,
                LevelEventType.ScaleRadius);

            Add(category, "Track", "TrackAnimate", "ER_TRACK_ANIMATE", "Track animation", "Tile appear and disappear animations.", false,
                LevelEventType.AnimateTrack);
            Add(category, "Track", "TrackMove", "ER_TRACK_MOVE", "Track movement", "Tiles moving.", false,
                LevelEventType.MoveTrack);
            Add(category, "Track", "TrackPosition", "ER_TRACK_POSITION", "Track position", "Tiles being repositioned.", false,
                LevelEventType.PositionTrack);
            Add(category, "Track", "TrackColor", "ER_TRACK_COLOR", "Track colour", "Tile colouring and recolouring.", false,
                LevelEventType.ColorTrack, LevelEventType.RecolorTrack);

            Add(category, "Replace", "AllDecorations", "ER_ALL_DECORATIONS",
                "Remove every decoration", "Clears the decoration list outright, not only the events that place them.", false).Edit = ClearDecorations;

            Add(category, "Replace", "TrackOpacity", "ER_TRACK_OPACITY",
                "Track opacity to 100%", "Puts every faded tile back to solid.", false).Edit = ResetTrackOpacity;

            Add(category, "Zoom", "CameraZoom", "ER_CAMERA_ZOOM",
                "Override camera zoom", "Replaces the level's camera settings with a fixed zoom.", false).Edit = SetCameraZoom;

            Add(category, "Replace", "DefaultTrackAnimation", "ER_DEFAULT_TRACK_ANIMATION",
                "Track animation to default", "Fade in and out, eight beats ahead.", false).Edit = ResetTrackAnimation;

            Add(category, "Replace", "DefaultTrackColour", "ER_DEFAULT_TRACK_COLOUR",
                "Track colour to default", "The standard single-colour track.", false).Edit = ResetTrackColour;

            cameraZoomEntry = Claim(category, "EffectRemoverCameraZoomValue", 250f,
                "Effect Remover: Camera Zoom", "Zoom used when the camera zoom override is on.");
        }

        private static readonly HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);

        private static MelonPreferences_Entry<T> Claim<T>(MelonPreferences_Category category,
            string identifier, T fallback, string label, string hint)
        {
            if (!identifiers.Add(identifier))
            {
                MelonLogger.Error("Effect Remover asked for the preference '" + identifier
                    + "' twice. The second one is ignored; this is a bug in the option table.");
                return null;
            }

            try
            {
                return category.CreateEntry(identifier, fallback, label, hint);
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Effect Remover could not create the preference '" + identifier
                    + "': " + exception.Message);
                return null;
            }
        }

        private static EffectRemoverOption Add(MelonPreferences_Category category, string group, string key,
            string langKey, string label, string hint, bool fallback, params LevelEventType[] events)
        {
            EffectRemoverOption option = new EffectRemoverOption
            {
                Key = key,
                Group = group,
                LangKey = langKey,
                Events = events ?? new LevelEventType[0],
                Entry = Claim(category, "EffectRemover" + key, fallback, "Effect Remover: " + label, hint)
            };

            options.Add(option);
            return option;
        }

        private static readonly ConditionalWeakTable<LevelData, StrippedMark> strippedLevels =
            new ConditionalWeakTable<LevelData, StrippedMark>();

        private sealed class StrippedMark
        {
            internal bool BackupSkipLogged;
        }

        internal static void PreserveStrippedMark(LevelData source, LevelData copy)
        {
            if (source == null || copy == null || ReferenceEquals(source, copy)
                || !strippedLevels.TryGetValue(source, out _) || strippedLevels.TryGetValue(copy, out _))
                return;

            strippedLevels.Add(copy, new StrippedMark());
        }

        internal static void Apply(LevelData level)
        {
            if (level == null || level.levelEvents == null)
                return;

            strippedLevels.Remove(level);

            if (!HemiTweaksMod.EffectRemoverEnabled)
                return;

            HashSet<LevelEventType> hidden = new HashSet<LevelEventType>();
            int edits = 0;

            foreach (EffectRemoverOption option in options)
            {
                if (!option.On)
                    continue;

                foreach (LevelEventType type in option.Events)
                    hidden.Add(type);

                if (option.Edit == null)
                    continue;

                try
                {
                    option.Edit(level);
                    edits++;
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Effect Remover could not apply " + option.Key + ": " + exception.Message);
                }
            }

            int removed = hidden.Count == 0
                ? 0
                : level.levelEvents.RemoveAll(item => item != null && hidden.Contains(item.eventType));

            if (removed > 0 || edits > 0)
                strippedLevels.Add(level, new StrippedMark());

            MelonLogger.Msg("Effect Remover hid " + removed + " event(s) from the level in memory.");
        }

        private static LevelEvent DefaultSettings(LevelEventType type, string infoKey)
        {
            return new LevelEvent(0, type, GCS.settingsInfo[infoKey]);
        }

        private static void ResetBackground(LevelData level)
        {
            level.backgroundSettings = DefaultSettings(LevelEventType.BackgroundSettings, "BackgroundSettings");

            if (level.miscSettings != null)
                level.miscSettings["bgVideo"] = "";
        }

        private static void ClearDecorations(LevelData level)
        {
            level.decorations?.Clear();
            level.decorationSettings = DefaultSettings(LevelEventType.DecorationSettings, "DecorationSettings");
        }

        private static void ResetTrackOpacity(LevelData level)
        {
            foreach (LevelEvent item in level.levelEvents)
            {
                if (item == null)
                    continue;

                if (item.eventType != LevelEventType.MoveTrack && item.eventType != LevelEventType.PositionTrack)
                    continue;

                if (item.ContainsKey("opacity"))
                    item["opacity"] = 100f;
            }
        }

        private static void SetCameraZoom(LevelData level)
        {
            level.cameraSettings = DefaultSettings(LevelEventType.CameraSettings, "CameraSettings");
            level.cameraSettings["zoom"] = CameraZoom;
        }

        private static void ResetTrackAnimation(LevelData level)
        {
            if (level.trackSettings == null)
                return;

            level.trackSettings["trackAppearAnimation"] = TrackAnimationType.Fade;
            level.trackSettings["trackDisappearAnimation"] = TrackAnimationType.Fade;
            level.trackSettings["beatsAhead"] = 8f;
            level.trackSettings["beatsBehind"] = 0f;
        }

        private static void ResetTrackColour(LevelData level)
        {
            if (level.trackSettings == null)
                return;

            level.trackSettings["trackStyle"] = TrackStyle.Standard;
            level.trackSettings["trackColor"] = "debb7bff";
            level.trackSettings["trackColorType"] = TrackColorType.Single;
        }

        private static LevelData EditorLevel()
        {
            scnEditor editor = ADOBase.editor;
            if (editor == null)
                return null;

            scnGame level = editor.customLevel;
            return level == null ? null : level.levelData;
        }

        internal static bool ShouldBlockSave()
        {
            if (!HemiTweaksMod.EffectRemoverBlockSave)
                return false;

            LevelData current = EditorLevel();
            return current != null && strippedLevels.TryGetValue(current, out _);
        }

        internal static bool RefuseBackup()
        {
            if (!ShouldBlockSave())
                return false;

            LevelData current = EditorLevel();
            if (current != null && strippedLevels.TryGetValue(current, out StrippedMark mark) && !mark.BackupSkipLogged)
            {
                mark.BackupSkipLogged = true;
                MelonLogger.Msg("Effect Remover skipped the editor auto-backup: the loaded level is stripped in memory.");
            }

            return true;
        }

        internal static bool RefuseSave()
        {
            if (!ShouldBlockSave())
                return false;

            try
            {
                ADOBase.editor?.ShowNotification(
                    Interface.HemiLang.Get("ER_SAVE_BLOCKED"));
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not show the blocked-save notice: " + exception.Message);
            }

            return true;
        }

        internal static bool AllowExportFiles(ref List<string> files, ref bool uploadable, ref DLCManager[] requiredDlc)
        {
            if (!RefuseSave())
                return true;

            files = new List<string>();
            uploadable = false;
            requiredDlc = new DLCManager[0];
            return false;
        }

        private static bool refreshFailureLogged;

        internal static void RefreshEditorButtons()
        {
            scnEditor editor = ADOBase.editor;
            if (editor == null)
                return;

            try
            {
                bool allowed = !ShouldBlockSave();
                SetInteractable(editor.buttonSave, allowed);
                SetInteractable(editor.buttonSaveAs, allowed);
                SetInteractable(editor.popupUnsavedChangesSave, allowed);
                SetInteractable(editor.popupSaveSaveAs, allowed);
            }
            catch (Exception exception)
            {
                if (refreshFailureLogged)
                    return;

                refreshFailureLogged = true;
                MelonLogger.Warning("Could not match the editor save controls to the Effect Remover: " + exception.Message);
            }
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null)
                button.interactable = value;
        }
    }

    [HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
    internal static class EffectRemoverDecodePatch
    {
        private static void Postfix(LevelData __instance)
        {
            EffectRemover.Apply(__instance);

            EffectRemover.RefreshEditorButtons();
        }
    }

    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SaveLevel))]
    internal static class EffectRemoverSaveLevelPatch
    {
        private static bool Prefix()
        {
            return !EffectRemover.RefuseSave();
        }
    }

    [HarmonyPatch(typeof(LevelData), nameof(LevelData.Copy), new Type[0])]
    internal static class EffectRemoverCopyPatch
    {
        private static void Postfix(LevelData __instance, LevelData __result)
        {
            EffectRemover.PreserveStrippedMark(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SaveLevelAs))]
    internal static class EffectRemoverSaveLevelAsPatch
    {
        private static bool Prefix()
        {
            return !EffectRemover.RefuseSave();
        }
    }

    [HarmonyPatch(typeof(scnEditor), "SaveBackup")]
    internal static class EffectRemoverSaveBackupPatch
    {
        private static bool Prefix()
        {
            return !EffectRemover.RefuseBackup();
        }
    }

    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ExportLevel))]
    internal static class EffectRemoverExportLevelPatch
    {
        private static bool Prefix()
        {
            return !EffectRemover.RefuseSave();
        }
    }

    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ShowExportWindow), new[] { typeof(int) })]
    internal static class EffectRemoverShowExportWindowPatch
    {
        private static bool Prefix()
        {
            return !EffectRemover.RefuseSave();
        }
    }

    [HarmonyPatch(typeof(scnEditor), "GetExportLevelFiles",
        new[] { typeof(string), typeof(bool), typeof(DLCManager[]) },
        new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
    internal static class EffectRemoverGetExportLevelFilesPatch
    {
        private static bool Prefix(ref List<string> __result, ref bool uploadable, ref DLCManager[] requiredDLC)
        {
            return EffectRemover.AllowExportFiles(ref __result, ref uploadable, ref requiredDLC);
        }
    }

    [HarmonyPatch(typeof(scnEditor), "LoadGameScene")]
    internal static class EffectRemoverEditorButtonsPatch
    {
        private static void Postfix()
        {
            EffectRemover.RefreshEditorButtons();
        }
    }
}
