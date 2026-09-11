using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HarmonyLib;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HemiTweaks
{
    internal enum StateFontSource
    {
        Default,
        System,
        File
    }

    internal static class StateOverlay
    {
        public const int MinimumFontSize = 8;
        public const int MaximumFontSize = 100;


        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> enabledEntry;
        private static MelonPreferences_Entry<int> fontSourceEntry;
        private static MelonPreferences_Entry<string> systemFontNameEntry;
        private static MelonPreferences_Entry<string> fontFilePathEntry;
        private static GameObject behaviourObject;
        private static List<string> installedFontNames;
        private static TMP_FontAsset runtimeFontAsset;
        private static Font runtimeSourceFont;
        private static StateFontSource runtimeFontSource = StateFontSource.Default;
        private static string runtimeFontName;
        private static int fontRevision;

        public static bool Enabled => enabledEntry != null && enabledEntry.Value;

        public static StateFontSource FontSource => fontSourceEntry == null
            ? StateFontSource.Default
            : (StateFontSource)Mathf.Clamp(
                fontSourceEntry.Value,
                (int)StateFontSource.Default,
                (int)StateFontSource.File);
        public static string SystemFontName => systemFontNameEntry == null
            ? ""
            : systemFontNameEntry.Value ?? "";
        public static string FontFilePath => fontFilePathEntry == null
            ? ""
            : fontFilePathEntry.Value ?? "";
        public static int FontRevision => fontRevision;
        public static IReadOnlyList<string> InstalledFontNames
        {
            get
            {
                EnsureInstalledFontNames();
                return installedFontNames;
            }
        }
        public static string FontDisplayName
        {
            get
            {
                if (FontSource == StateFontSource.System && !string.IsNullOrWhiteSpace(SystemFontName))
                    return SystemFontName;

                if (FontSource == StateFontSource.File && !string.IsNullOrWhiteSpace(FontFilePath))
                    return Path.GetFileNameWithoutExtension(FontFilePath);

                return Interface.HemiLang.Get("STATE_FONT_DEFAULT");
            }
        }

        public static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            enabledEntry = category.CreateEntry(
                "EnableState",
                false,
                "Enable State",
                "Displays live level progress, XAccuracy, TileBPM, and CurBPM.");
            category.CreateEntry(
                "StateFontSize",
                22,
                "State Font Size",
                "TextMeshPro font size used by the State overlay.");
            fontSourceEntry = category.CreateEntry(
                "StateFontSource",
                (int)StateFontSource.Default,
                "State Font Source",
                "Source used for the State TextMeshPro font.");
            systemFontNameEntry = category.CreateEntry(
                "StateSystemFontName",
                "",
                "State System Font Name",
                "Installed operating-system font selected for State.");
            fontFilePathEntry = category.CreateEntry(
                "StateFontFilePath",
                "",
                "State Font File Path",
                "TTF, OTF, or TTC file selected for State.");

            StateGroupStore.Load();

            if (Enabled)
                EnsureBehaviourObject();
        }

        public static void UpdateLifecycle()
        {
            if (Enabled)
            {
                EnsureBehaviourObject();
            }
            else if (behaviourObject != null && behaviourObject.activeSelf)
            {
                behaviourObject.SetActive(false);
            }
        }

        public static void SetEnabled(bool value)
        {
            if (enabledEntry == null || enabledEntry.Value == value)
                return;

            enabledEntry.Value = value;
            UpdateLifecycle();
            category?.SaveToFile(false);
        }

        public static bool IsSystemFontSelected(string fontName)
        {
            return FontSource == StateFontSource.System &&
                string.Equals(SystemFontName, fontName, StringComparison.CurrentCultureIgnoreCase);
        }

        public static void SelectDefaultFont()
        {
            if (FontSource == StateFontSource.Default)
                return;

            fontSourceEntry.Value = (int)StateFontSource.Default;
            ReplaceRuntimeFont(null, null, StateFontSource.Default, null);
            fontRevision++;
            category?.SaveToFile(false);
        }

        public static bool SelectSystemFont(string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return false;

            if (!TryCreateSystemFontAsset(fontName, out TMP_FontAsset fontAsset, out Font sourceFont))
                return false;

            systemFontNameEntry.Value = fontName;
            fontSourceEntry.Value = (int)StateFontSource.System;
            ReplaceRuntimeFont(fontAsset, sourceFont, StateFontSource.System, fontName);
            fontRevision++;
            category?.SaveToFile(false);
            return true;
        }

        public static bool SelectFontFile(string path)
        {
            if (!HemiAssetSafety.IsFontPath(path))
            {
                MelonLogger.Warning("State font file must use a local TTF, OTF, or TTC path.");
                return false;
            }

            try
            {
                string fullPath = Path.GetFullPath(path);
                TMP_FontAsset fontAsset = TryCreateFileFontAsset(fullPath);
                if (fontAsset == null)
                    return false;

                fontFilePathEntry.Value = fullPath;
                fontSourceEntry.Value = (int)StateFontSource.File;
                ReplaceRuntimeFont(fontAsset, null, StateFontSource.File, fullPath);
                fontRevision++;
                category?.SaveToFile(false);
                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Failed to select State font file: " + exception.Message);
                return false;
            }
        }

        public static TMP_FontAsset GetFontAsset()
        {
            StateFontSource source = FontSource;
            if (source == StateFontSource.Default)
                return TMP_Settings.defaultFontAsset;

            string name = source == StateFontSource.System ? SystemFontName : FontFilePath;
            if (source == runtimeFontSource && string.Equals(runtimeFontName, name, StringComparison.Ordinal))
                return runtimeFontAsset != null ? runtimeFontAsset : TMP_Settings.defaultFontAsset;

            TMP_FontAsset fontAsset = null;
            Font sourceFont = null;
            if (source == StateFontSource.System)
                TryCreateSystemFontAsset(SystemFontName, out fontAsset, out sourceFont);
            else if (source == StateFontSource.File)
                fontAsset = TryCreateFileFontAsset(FontFilePath);

            ReplaceRuntimeFont(fontAsset, sourceFont, source, name);
            return runtimeFontAsset != null ? runtimeFontAsset : TMP_Settings.defaultFontAsset;
        }

        public static void Shutdown()
        {
            if (behaviourObject != null)
                UnityEngine.Object.Destroy(behaviourObject);

            behaviourObject = null;
            DestroyRuntimeFont();
            installedFontNames = null;
        }

        private static void EnsureBehaviourObject()
        {
            HemiOverlayHost.Ensure<StateGroupOverlayBehaviour>(ref behaviourObject, "HemiTweaks_State");
        }

        private static void EnsureInstalledFontNames()
        {
            if (installedFontNames != null)
                return;

            installedFontNames = new List<string>();
            try
            {
                string[] names = Font.GetOSInstalledFontNames() ?? Array.Empty<string>();
                HashSet<string> uniqueNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i]?.Trim();
                    if (!string.IsNullOrEmpty(name) && uniqueNames.Add(name))
                        installedFontNames.Add(name);
                }

                installedFontNames.Sort(StringComparer.CurrentCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to enumerate installed fonts: " + ex.Message);
            }
        }

        private static bool TryCreateSystemFontAsset(
            string fontName,
            out TMP_FontAsset fontAsset,
            out Font sourceFont)
        {
            fontAsset = null;
            sourceFont = null;
            try
            {
                sourceFont = Font.CreateDynamicFontFromOSFont(fontName, 90);
                if (sourceFont == null)
                    throw new InvalidOperationException("Unity could not create the system font.");

                fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (fontAsset == null)
                    throw new InvalidOperationException("TextMeshPro could not create a font asset.");

                fontAsset.name = "HemiTweaks State - " + fontName;
                fontAsset.isMultiAtlasTexturesEnabled = true;
                return true;
            }
            catch (Exception ex)
            {
                if (fontAsset != null)
                    UnityEngine.Object.Destroy(fontAsset);
                if (sourceFont != null)
                    UnityEngine.Object.Destroy(sourceFont);

                fontAsset = null;
                sourceFont = null;
                MelonLogger.Warning("Failed to load State system font '" + fontName + "': " + ex.Message);
                return false;
            }
        }

        private static TMP_FontAsset TryCreateFileFontAsset(string path)
        {
            try
            {
                if (!HemiAssetSafety.IsFontPath(path))
                    throw new InvalidDataException("Font files must use a local TTF, OTF, or TTC path.");
                if (!File.Exists(path))
                    throw new FileNotFoundException("The font file was not found.");

                TMP_FontAsset fontAsset;
                using (Stream input = HemiStreamSafety.OpenRead(path, HemiAssetSafety.MaximumFontBytes))
                {
                    if (input.Length == 0)
                        throw new InvalidDataException("The font file is empty.");
                    fontAsset = TMP_FontAsset.CreateFontAsset(
                        path,
                        0,
                        90,
                        9,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024);
                }
                if (fontAsset == null)
                    throw new InvalidOperationException("TextMeshPro could not read the selected font file.");

                fontAsset.name = "HemiTweaks State - " + Path.GetFileNameWithoutExtension(path);
                fontAsset.isMultiAtlasTexturesEnabled = true;
                return fontAsset;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to load State font file '" + path + "': " + ex.Message);
                return null;
            }
        }

        private static void ReplaceRuntimeFont(TMP_FontAsset fontAsset, Font sourceFont, StateFontSource source, string name)
        {
            DestroyRuntimeFont();
            runtimeFontAsset = fontAsset;
            runtimeSourceFont = sourceFont;
            runtimeFontSource = source;
            runtimeFontName = name;
        }

        private static void DestroyRuntimeFont()
        {
            if (runtimeFontAsset != null)
                UnityEngine.Object.Destroy(runtimeFontAsset);
            if (runtimeSourceFont != null)
                UnityEngine.Object.Destroy(runtimeSourceFont);

            runtimeFontAsset = null;
            runtimeSourceFont = null;
            runtimeFontSource = StateFontSource.Default;
            runtimeFontName = null;
        }
    }

    internal static class StateValues
    {
        private const float KpsWindowSeconds = 1f;

        private const float FpsWindowSeconds = 0.5f;

        private static readonly Queue<float> keyPressTimes = new Queue<float>();
        private static readonly Queue<float> frameTimes = new Queue<float>();
        private static float frameTimeSum;
        private static int lastPolledFrame = -1;
        private static int musicTimeCurrentSecond = -1;
        private static int musicTimeTotalSecond = -1;
        private static string musicTimeText;
        private static int mapTimeCurrentSecond = -1;
        private static int mapTimeTotalSecond = -1;
        private static string mapTimeText;

        internal static bool IsPlaying
        {
            get
            {
                try
                {
                    scrController controller = ADOBase.controller;
                    if (controller == null || controller.paused)
                        return false;

                    scnEditor editor = ADOBase.editor;
                    if (editor != null)
                        return !editor.inStrictlyEditingMode && IsDisplayState(controller.state);

                    return controller.gameworld && IsDisplayState(controller.state);
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool IsDisplayState(States state)
        {
            return state == States.Start ||
                state == States.Countdown ||
                state == States.Checkpoint ||
                state == States.PlayerControl ||
                state == States.Fail ||
                state == States.Fail2 ||
                state == States.Won;
        }

        internal static void Poll()
        {
            if (lastPolledFrame == Time.frameCount)
                return;
            lastPolledFrame = Time.frameCount;

            float delta = Time.unscaledDeltaTime;
            if (delta > 0.000001f)
            {
                frameTimes.Enqueue(delta);
                frameTimeSum += delta;
                while (frameTimes.Count > 1 && frameTimeSum - frameTimes.Peek() >= FpsWindowSeconds)
                    frameTimeSum -= frameTimes.Dequeue();
            }

            float now = Time.unscaledTime;
            try
            {
                List<AnyKeyCode> pressed = RDInput.GetMainPressKeys();
                if (pressed != null)
                {
                    for (int i = 0; i < pressed.Count; i++)
                        keyPressTimes.Enqueue(now);
                }
            }
            catch
            {
            }

            while (keyPressTimes.Count > 0 && now - keyPressTimes.Peek() > KpsWindowSeconds)
                keyPressTimes.Dequeue();
        }

        internal static void Reset()
        {
            keyPressTimes.Clear();
            frameTimes.Clear();
            frameTimeSum = 0f;
            musicTimeCurrentSecond = -1;
            musicTimeTotalSecond = -1;
            musicTimeText = null;
            mapTimeCurrentSecond = -1;
            mapTimeTotalSecond = -1;
            mapTimeText = null;
            JudgementCounter.Reset();
            HitTiming.Rewind();
            LevelStats.Rewind();
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.Start_Rewind))]
        private static class ScrControllerStartRewindPatch
        {
            private static void Postfix()
            {
                Reset();
                StateGroupOverlayBehaviour.NotifyRunStarted();
                AutoOffset.OnRunStarted();
                RunCounters.NotifyRunStarted();
            }
        }

        internal static string Format(StateStat stat)
        {
            if (stat == null)
                return "";

            switch (stat.Kind)
            {
                case StateStatKind.Text:
                    return stat.Text ?? "";
                case StateStatKind.Progress:
                    return Number(Progress, stat.Decimals);
                case StateStatKind.Accuracy:
                    return Number(Accuracy, stat.Decimals);
                case StateStatKind.XAccuracy:
                    return Number(XAccuracy, stat.Decimals);
                case StateStatKind.MusicTime:
                    return MusicTime;
                case StateStatKind.MapTime:
                    return MapTime;
                case StateStatKind.TileBpm:
                    return Number(TileBpm, stat.Decimals);
                case StateStatKind.CurBpm:
                    return Number(CurBpm, stat.Decimals);
                case StateStatKind.Kps:
                    return Number(Kps, stat.Decimals);
                case StateStatKind.Checkpoints:
                    return Checkpoints.ToString(CultureInfo.InvariantCulture);
                case StateStatKind.Attempts:
                    return Attempts.ToString(CultureInfo.InvariantCulture);
                case StateStatKind.TotalAttempts:
                    return TotalAttempts.ToString(CultureInfo.InvariantCulture);
                case StateStatKind.TimingScale:
                    return stat.AsPercent
                        ? Number(TimingScale * 100f, stat.Decimals) + "%"
                        : Number(TimingScale, Math.Max(stat.Decimals, 2));
                case StateStatKind.Fps:
                    return Number(Fps, stat.Decimals);
                case StateStatKind.JudgementCounter:
                    return JudgementCounter.Format();
                case StateStatKind.CurCheckPoint:
                    return Whole(LevelStats.CurCheckPoint);
                case StateStatKind.TotalCheckPoints:
                    return Whole(LevelStats.TotalCheckPoints);
                case StateStatKind.StartTile:
                    return Whole(LevelStats.StartTile);
                case StateStatKind.CurTile:
                    return Whole(LevelStats.CurTile);
                case StateStatKind.LeftTile:
                    return Whole(LevelStats.LeftTile);
                case StateStatKind.TotalTile:
                    return Whole(LevelStats.TotalTile);
                case StateStatKind.Timing:
                    return Number(HitTiming.Last, stat.Decimals);
                case StateStatKind.TimingAvg:
                    return Number(HitTiming.Average, stat.Decimals);
                case StateStatKind.SongTitle:
                    return LevelStats.SongTitle(stat.RemoveRichText);
                case StateStatKind.Author:
                    return LevelStats.Author(stat.RemoveRichText);
                default:
                    return "";
            }
        }

        internal static float Progress
        {
            get
            {
                return ProgressFraction * 100f;
            }
        }

        internal static float ProgressFraction
        {
            get
            {
                try
                {
                    scrController controller = ADOBase.controller;
                    if (controller == null || ADOBase.lm == null || ADOBase.lm.listFloors == null || ADOBase.lm.listFloors.Count == 0)
                        return 0f;
                    return Mathf.Clamp01(controller.percentComplete);
                }
                catch { return 0f; }
            }
        }

        internal static float Accuracy => AccuracyPercent(false);

        internal static float XAccuracy => AccuracyPercent(true);

        private static float AccuracyPercent(bool extended)
        {
            try
            {
                scrMistakesManager manager = ADOBase.controller?.mistakesManager;
                if (manager == null)
                    return 100f;

                float value = extended ? manager.percentXAcc : manager.percentAcc;
                return !IsFinite(value) ? 100f : Mathf.Max(0f, value * 100f);
            }
            catch { return 100f; }
        }

        internal static string MusicTime
        {
            get
            {
                try
                {
                    scrConductor conductor = ADOBase.conductor;
                    AudioSource song = conductor?.song;
                    if (song == null || song.clip == null)
                        return "0:00 / 0:00";
                    float now = Mathf.Max(0f, (float)conductor.songposition_minusi);
                    return ClockPair(
                        now,
                        Mathf.Max(0f, song.clip.length),
                        ref musicTimeCurrentSecond,
                        ref musicTimeTotalSecond,
                        ref musicTimeText);
                }
                catch { return "0:00 / 0:00"; }
            }
        }

        internal static string MapTime
        {
            get
            {
                try
                {
                    scrController controller = ADOBase.controller;
                    List<scrFloor> floors = ADOBase.lm?.listFloors;
                    if (controller == null || floors == null || floors.Count == 0)
                        return "0:00 / 0:00";

                    scrFloor current = controller.currFloor;
                    scrFloor last = floors[floors.Count - 1];
                    double now = current == null ? 0.0 : current.entryTime;
                    double total = last == null ? 0.0 : last.entryTime;
                    return ClockPair(
                        (float)Math.Max(0.0, now),
                        (float)Math.Max(0.0, total),
                        ref mapTimeCurrentSecond,
                        ref mapTimeTotalSecond,
                        ref mapTimeText);
                }
                catch { return "0:00 / 0:00"; }
            }
        }

        internal static float TileBpm
        {
            get
            {
                GetBpm(out float tile, out float _);
                return tile;
            }
        }

        internal static float CurBpm
        {
            get
            {
                GetBpm(out float _, out float current);
                return current;
            }
        }

        internal static float Kps => keyPressTimes.Count / KpsWindowSeconds;

        internal static int Checkpoints => RunCounters.Checkpoints;

        internal static int Attempts => RunCounters.Attempts;

        internal static int TotalAttempts => RunCounters.TotalAttempts;

        internal static float TimingScale
        {
            get
            {
                try
                {
                    scrFloor floor = ADOBase.controller?.currFloor;
                    if (floor == null)
                        return 1f;
                    float value = (float)floor.marginScale;
                    return IsFinite(value) ? value : 1f;
                }
                catch { return 1f; }
            }
        }

        internal static float Fps => frameTimeSum > 0.00001f ? frameTimes.Count / frameTimeSum : 0f;

        private static int bpmFrame = -1;
        private static float bpmTile;
        private static float bpmCurrent;

        private static void GetBpm(out float tileBpm, out float currentBpm)
        {
            if (bpmFrame == Time.frameCount)
            {
                tileBpm = bpmTile;
                currentBpm = bpmCurrent;
                return;
            }

            ComputeBpm(out tileBpm, out currentBpm);
            bpmFrame = Time.frameCount;
            bpmTile = tileBpm;
            bpmCurrent = currentBpm;
        }

        private static void ComputeBpm(out float tileBpm, out float currentBpm)
        {
            tileBpm = 0f;
            currentBpm = 0f;
            try
            {
                scrController controller = ADOBase.controller;
                scrConductor conductor = ADOBase.conductor;
                if (controller == null || conductor == null)
                    return;

                float pitch = conductor.song != null ? conductor.song.pitch : 1f;
                if (!IsFinite(pitch) || pitch <= 0f)
                    pitch = 1f;

                scrFloor floor = controller.currFloor;
                float speed = floor != null && IsFinite(floor.speed) ? floor.speed : 1f;
                tileBpm = Mathf.Max(0f, conductor.bpm * speed * pitch);
                currentBpm = tileBpm;

                if (floor != null && floor.nextfloor != null)
                {
                    double interval = floor.nextfloor.entryTime - floor.entryTime;
                    if (interval > 0.000001 && !double.IsNaN(interval) && !double.IsInfinity(interval))
                        currentBpm = Mathf.Max(0f, (float)(60.0 / interval) * pitch);
                }
            }
            catch
            {
            }
        }

        private static string ClockPair(
            float current,
            float total,
            ref int cachedCurrent,
            ref int cachedTotal,
            ref string cachedText)
        {
            int currentSecond = ClockSecond(current);
            int totalSecond = ClockSecond(total);
            if (cachedText != null && cachedCurrent == currentSecond && cachedTotal == totalSecond)
                return cachedText;

            cachedCurrent = currentSecond;
            cachedTotal = totalSecond;
            cachedText = Clock(currentSecond) + " / " + Clock(totalSecond);
            return cachedText;
        }

        private static int ClockSecond(float seconds)
        {
            if (!IsFinite(seconds) || seconds < 0f)
                seconds = 0f;
            return (int)seconds;
        }

        private static string Clock(int seconds)
        {
            return (seconds / 60).ToString(CultureInfo.InvariantCulture) + ":" +
                (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static readonly string[] NumberFormats = { "F0", "F1", "F2", "F3", "F4", "F5", "F6" };

        private static string Number(float value, int decimals)
        {
            if (!IsFinite(value))
                value = 0f;
            decimals = Mathf.Clamp(decimals, 0, 6);
            return value.ToString(NumberFormats[decimals], CultureInfo.InvariantCulture);
        }

        private static string Whole(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
