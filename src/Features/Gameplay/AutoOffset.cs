using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HarmonyLib;
using HemiTweaks.Interface;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace HemiTweaks
{
    internal enum AutoOffsetRunEnd
    {
        Death,
        Pause,
        Clear
    }

    internal sealed class AutoOffsetRecord
    {
        public string Time;
        public string Level;
        public string Reason;
        public int Hits;
        public float Average;
        public float OffsetBefore;

        public float Target;
    }

    internal static class AutoOffset
    {
        private const int MaximumRecords = 30;

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> enabledEntry;
        private static MelonPreferences_Entry<bool> popupEntry;
        private static MelonPreferences_Entry<bool> recordEntry;
        private static MelonPreferences_Entry<bool> decimalEntry;
        private static MelonPreferences_Entry<string> decimalOffsetsEntry;
        private static MelonPreferences_Entry<float> popupXEntry;
        private static MelonPreferences_Entry<float> popupYEntry;
        private static MelonPreferences_Entry<int> popupScaleEntry;

        internal const int MinimumPopupScale = 50;
        internal const int MaximumPopupScale = 200;

        private static readonly HemiSaveDebounce writer = new HemiSaveDebounce(Write);

        private static double runTotal;
        private static int runHits;
        private static int hitsAtLastRecord;

        private static Dictionary<string, float> decimalOffsets;

        private static bool cachedValid;
        private static AudioOutputType cachedType;
        private static string cachedName;
        private static float cachedSeconds;

        private static List<AutoOffsetRecord> records;
        private static string recordsPath;

        internal static bool Enabled => enabledEntry != null && enabledEntry.Value;
        internal static bool PopupEnabled => popupEntry == null || popupEntry.Value;
        internal static bool RecordEnabled => recordEntry == null || recordEntry.Value;
        internal static bool DecimalEnabled => decimalEntry != null && decimalEntry.Value;

        internal static float PopupX => popupXEntry == null ? 0f : popupXEntry.Value;
        internal static float PopupY => popupYEntry == null ? 120f : popupYEntry.Value;

        internal static int PopupScale =>
            popupScaleEntry == null ? 100 : Mathf.Clamp(popupScaleEntry.Value, MinimumPopupScale, MaximumPopupScale);

        internal static float PopupScaleFactor => PopupScale / 100f;

        internal static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            enabledEntry = category.CreateEntry("EnableAutoOffset", false, "Enable Auto Input Offset",
                "Works out the input offset from how the player hits and offers to apply it.");
            popupEntry = category.CreateEntry("AutoOffsetPopup", true, "Auto Offset: Calibration Popup",
                "After a death, asks whether to change the input offset to the one the run measured.");
            recordEntry = category.CreateEntry("AutoOffsetRecord", true, "Auto Offset: Record Timings",
                "Keeps the average timing of every run that ends by death, pause or clear, so it can be applied later.");
            decimalEntry = category.CreateEntry("AutoOffsetDecimal", false, "Auto Offset: Decimal Offset",
                "Lets the input offset carry a fractional part. The game's own setting stays the rounded value.");
            decimalOffsetsEntry = category.CreateEntry("AutoOffsetDecimalOffsets", "", "Auto Offset: Decimal Offsets",
                "Fractional input offsets by audio output, as JSON. Managed by the mod.");
            popupXEntry = category.CreateEntry("AutoOffsetPopupX", 0f, "Auto Offset: Popup X",
                "Horizontal distance of the calibration popup from the centre of the screen.");
            popupYEntry = category.CreateEntry("AutoOffsetPopupY", 120f, "Auto Offset: Popup Y",
                "Vertical distance of the calibration popup from the centre of the screen.");
            popupScaleEntry = category.CreateEntry("AutoOffsetPopupScale", 100, "Auto Offset: Popup Size",
                "Size of the calibration popup, in percent.");

            HitTiming.Recorded += OnHit;
            ReloadFromPreferences();
        }

        internal static void Shutdown()
        {
            HitTiming.Recorded -= OnHit;
            writer.Flush();
        }

        internal static void Tick()
        {
            writer.Tick();
        }

        internal static void ReloadFromPreferences()
        {
            decimalOffsets = null;
            cachedValid = false;
            cachedName = null;
        }

        internal static void NotifySceneChanged()
        {
            HemiAutoOffsetPopup.HideNow();
        }

        internal static void SetEnabled(bool value)
        {
            if (enabledEntry == null || enabledEntry.Value == value)
                return;
            enabledEntry.Value = value;
            cachedValid = false;
            cachedName = null;
            if (!value)
                HemiAutoOffsetPopup.HideNow();
            Save();
        }

        internal static void SetPopup(bool value)
        {
            if (popupEntry == null || popupEntry.Value == value)
                return;
            popupEntry.Value = value;
            Save();
        }

        internal static void SetRecord(bool value)
        {
            if (recordEntry == null || recordEntry.Value == value)
                return;
            recordEntry.Value = value;
            Save();
        }

        internal static void SetDecimal(bool value)
        {
            if (decimalEntry == null || decimalEntry.Value == value)
                return;
            decimalEntry.Value = value;
            cachedValid = false;
            cachedName = null;
            Save();
        }

        internal static void SetPopupOffset(float x, float y)
        {
            if (popupXEntry == null || popupYEntry == null)
                return;
            if (Mathf.Approximately(popupXEntry.Value, x) && Mathf.Approximately(popupYEntry.Value, y))
                return;
            popupXEntry.Value = x;
            popupYEntry.Value = y;
            Save();
        }

        internal static void SetPopupScale(int percent)
        {
            if (popupScaleEntry == null)
                return;
            int next = Mathf.Clamp(percent, MinimumPopupScale, MaximumPopupScale);
            if (popupScaleEntry.Value == next)
                return;
            popupScaleEntry.Value = next;
            Save();
        }

        private static void Save()
        {
            writer.Request();
        }

        private static void Write()
        {
            category?.SaveToFile(false);
        }

        internal static float CurrentOffset
        {
            get
            {
                int whole = scrConductor.currentPreset.inputOffset;
                if (DecimalEnabled && TryGetDecimal(out float fraction) && Mathf.RoundToInt(fraction) == whole)
                    return fraction;
                return whole;
            }
        }

        internal static string CurrentDeviceName
        {
            get
            {
                try
                {
                    return scrConductor.currentPreset.ReadableOutputName() ?? "";
                }
                catch
                {
                    return "";
                }
            }
        }

        internal static string Format(float milliseconds)
        {
            if (DecimalEnabled)
                return milliseconds.ToString("0.##", CultureInfo.InvariantCulture);
            return Mathf.RoundToInt(milliseconds).ToString(CultureInfo.InvariantCulture);
        }

        internal static void ApplyOffset(float milliseconds)
        {
            if (float.IsNaN(milliseconds) || float.IsInfinity(milliseconds))
                return;

            milliseconds = (float)Math.Round(milliseconds, 2);
            int whole = Mathf.RoundToInt(milliseconds);

            try
            {
                scrConductor.currentPreset.inputOffset = whole;
                scrConductor.SaveCurrentPreset();
                Persistence.Save();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not write the input offset to the game: " + exception.Message);
                return;
            }

            if (DecimalEnabled)
                StoreDecimal(milliseconds);
            else
                StoreDecimal(null);

            cachedValid = false;
            cachedName = null;
            MelonLogger.Msg("Input offset set to " + Format(milliseconds) + " ms for " + CurrentDeviceName + ".");
        }

        private static string DeviceKey()
        {
            CalibrationPreset preset = scrConductor.currentPreset;
            return preset.outputType + "|" + (preset.outputName ?? "");
        }

        private static Dictionary<string, float> DecimalOffsets()
        {
            if (decimalOffsets != null)
                return decimalOffsets;

            decimalOffsets = new Dictionary<string, float>();
            string json = decimalOffsetsEntry?.Value;
            if (string.IsNullOrEmpty(json))
                return decimalOffsets;

            try
            {
                Dictionary<string, float> parsed = JsonConvert.DeserializeObject<Dictionary<string, float>>(json);
                if (parsed != null)
                    decimalOffsets = parsed;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Decimal offsets could not be read and were reset: " + exception.Message);
            }
            return decimalOffsets;
        }

        private static bool TryGetDecimal(out float value)
        {
            return DecimalOffsets().TryGetValue(DeviceKey(), out value);
        }

        private static void StoreDecimal(float? value)
        {
            Dictionary<string, float> table = DecimalOffsets();
            string key = DeviceKey();
            if (value.HasValue)
                table[key] = value.Value;
            else if (!table.Remove(key))
                return;

            if (decimalOffsetsEntry != null)
                decimalOffsetsEntry.Value = JsonConvert.SerializeObject(table);
            Save();
        }

        internal static bool TryDecimalSeconds(out float seconds)
        {
            seconds = 0f;
            if (!Enabled || !DecimalEnabled)
                return false;

            CalibrationPreset preset = scrConductor.currentPreset;
            if (!cachedValid || preset.outputType != cachedType || !string.Equals(preset.outputName, cachedName, StringComparison.Ordinal))
            {
                cachedValid = true;
                cachedType = preset.outputType;
                cachedName = preset.outputName;
                cachedSeconds = TryGetDecimal(out float fraction) ? fraction / 1000f : float.NaN;
            }

            if (float.IsNaN(cachedSeconds) || Mathf.RoundToInt(cachedSeconds * 1000f) != preset.inputOffset)
                return false;

            seconds = cachedSeconds;
            return true;
        }

        private static bool Counts(HitMargin margin)
        {
            switch (margin)
            {
                case HitMargin.VeryEarly:
                case HitMargin.EarlyPerfect:
                case HitMargin.PerfectMinus:
                case HitMargin.XPerfect:
                case HitMargin.PerfectPlus:
                case HitMargin.LatePerfect:
                case HitMargin.VeryLate:
                    return true;
                default:
                    return false;
            }
        }

        private static void OnHit(double milliseconds, HitMargin margin)
        {
            if (!Enabled || !Counts(margin))
                return;
            runTotal += milliseconds;
            runHits++;
        }

        internal static void OnRunStarted()
        {
            runTotal = 0.0;
            runHits = 0;
            hitsAtLastRecord = 0;
        }

        internal static float RunAverage => runHits == 0 ? 0f : (float)(runTotal / runHits);

        internal static int RunHits => runHits;

        internal static void OnRunEnded(AutoOffsetRunEnd reason)
        {
            if (!Enabled)
                return;

            scrController controller;
            try
            {
                controller = ADOBase.controller;
                if (controller == null || !controller.gameworld)
                    return;
            }
            catch
            {
                return;
            }

            if (runHits == 0)
                return;

            float average = RunAverage;
            float current = CurrentOffset;
            float target = (float)Math.Round(current + average, 2);

            if (RecordEnabled && runHits != hitsAtLastRecord)
            {
                hitsAtLastRecord = runHits;
                AddRecord(new AutoOffsetRecord
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    Level = LevelName(),
                    Reason = reason.ToString(),
                    Hits = runHits,
                    Average = (float)Math.Round(average, 2),
                    OffsetBefore = current,
                    Target = target
                });
            }

            if (reason == AutoOffsetRunEnd.Death && PopupEnabled && !controller.paused &&
                Format(current) != Format(target))
            {
                HemiAutoOffsetPopup.Show(current, target);
            }
        }

        private static string LevelName()
        {
            try
            {
                string title = LevelStats.SongTitle(true);
                return string.IsNullOrEmpty(title) ? "" : title;
            }
            catch
            {
                return "";
            }
        }

        private static string RecordsPath
        {
            get
            {
                if (string.IsNullOrEmpty(recordsPath))
                {
                    string directory = Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name);
                    Directory.CreateDirectory(directory);
                    recordsPath = Path.Combine(directory, "AutoOffsetRecords.json");
                }
                return recordsPath;
            }
        }

        internal static IReadOnlyList<AutoOffsetRecord> Records
        {
            get
            {
                LoadRecords();
                return records;
            }
        }

        private static void LoadRecords()
        {
            if (records != null)
                return;

            records = new List<AutoOffsetRecord>();
            try
            {
                if (!File.Exists(RecordsPath))
                    return;
                List<AutoOffsetRecord> loaded =
                    JsonConvert.DeserializeObject<List<AutoOffsetRecord>>(File.ReadAllText(RecordsPath));
                if (loaded != null)
                    records = loaded;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Timing records could not be read: " + exception.Message);
            }
        }

        private static void SaveRecords()
        {
            try
            {
                HemiAtomicFile.WriteAllText(RecordsPath, JsonConvert.SerializeObject(records, Formatting.Indented));
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not save timing records: " + exception.Message);
            }
        }

        private static void AddRecord(AutoOffsetRecord record)
        {
            LoadRecords();
            records.Insert(0, record);
            while (records.Count > MaximumRecords)
                records.RemoveAt(records.Count - 1);
            SaveRecords();
        }

        internal static void RemoveRecord(AutoOffsetRecord record)
        {
            LoadRecords();
            if (records.Remove(record))
                SaveRecords();
        }

        internal static void ClearRecords()
        {
            LoadRecords();
            if (records.Count == 0)
                return;
            records.Clear();
            SaveRecords();
        }

        internal static string ReasonText(string reason)
        {
            switch (reason)
            {
                case "Pause":
                    return HemiLang.Get("AO_REASON_PAUSE");
                case "Clear":
                    return HemiLang.Get("AO_REASON_CLEAR");
                default:
                    return HemiLang.Get("AO_REASON_DEATH");
            }
        }
    }

    [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.calibration_i), MethodType.Getter)]
    internal static class AutoOffsetCalibrationPatch
    {
        private static bool Prefix(ref float __result)
        {
            if (!AutoOffset.TryDecimalSeconds(out float seconds))
                return true;
            __result = seconds;
            return false;
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), nameof(SettingsMenu.UpdateSetting))]
    internal static class AutoOffsetSettingsMenuPatch
    {
        private static bool Prefix(
            PauseSettingButton setting,
            SettingsMenu.Interaction action,
            ref PauseSettingButton ___offsetButton)
        {
            if (!AutoOffset.Enabled || !AutoOffset.DecimalEnabled || setting == null || setting.name != "inputOffset")
                return true;
            if (action == SettingsMenu.Interaction.Activate || action == SettingsMenu.Interaction.ActivateInfo)
                return true;

            ___offsetButton = setting;
            float offset = AutoOffset.CurrentOffset;

            if (action == SettingsMenu.Interaction.Refresh)
            {
                setting.CachedValue = null;
                setting.initialValue = offset;
            }
            else
            {
                float step = 10f;
                if (RDInput.holdingShift)
                    step = 1f;
                if (RDInput.holdingControl)
                    step /= 100f;

                PauseMenu pauseMenu = scrController.instance != null ? scrController.instance.pauseMenu : null;

                if (action == SettingsMenu.Interaction.Increment)
                {
                    offset += step;
                    setting.PlayArrowAnimation(true);
                    if (pauseMenu != null)
                        pauseMenu.PlayMenuSfx(SfxSound.MenuIncrement);
                }
                else if (action == SettingsMenu.Interaction.Decrement)
                {
                    offset -= step;
                    setting.PlayArrowAnimation(false);
                    if (pauseMenu != null)
                        pauseMenu.PlayMenuSfx(SfxSound.MenuDecrement);
                }

                offset = (float)Math.Round(offset, 2);
                AutoOffset.ApplyOffset(offset);
            }

            if (setting.valueLabel != null)
            {
                string unit = "";
                if (!string.IsNullOrEmpty(setting.unit))
                {
                    string withCheck = RDString.GetWithCheck("editor.unit." + setting.unit, out bool exists);
                    unit = exists ? withCheck : setting.unit;
                }
                setting.valueLabel.text = AutoOffset.Format(offset) + unit;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.Fail2Action))]
    internal static class AutoOffsetFail2ActionPatch
    {
        private static void Prefix(scrController __instance, out bool __state)
        {
            try
            {
                __state = __instance != null && __instance.state == States.Fail &&
                          (__instance.gameworld || (__instance.currFloor != null && __instance.currFloor.freeroam));
            }
            catch
            {
                __state = false;
            }
        }

        private static void Postfix(bool __state)
        {
            if (__state)
                AutoOffset.OnRunEnded(AutoOffsetRunEnd.Death);
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.TogglePauseGame))]
    internal static class AutoOffsetTogglePausePatch
    {
        private static void Postfix(bool __result)
        {
            HemiAutoOffsetPopup.Dismiss();
            if (__result)
                AutoOffset.OnRunEnded(AutoOffsetRunEnd.Pause);
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.OnLandOnPortal))]
    internal static class AutoOffsetLandOnPortalPatch
    {
        private static void Postfix()
        {
            AutoOffset.OnRunEnded(AutoOffsetRunEnd.Clear);
        }
    }
}
