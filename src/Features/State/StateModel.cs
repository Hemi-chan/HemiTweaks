using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace HemiTweaks
{
    internal enum StateStatKind
    {
        Progress,
        Accuracy,
        XAccuracy,
        MusicTime,
        MapTime,
        TileBpm,
        CurBpm,
        Kps,
        Checkpoints,
        Attempts,
        TimingScale,
        Fps,
        Text,
        Image,

        JudgementCounter,

        CurCheckPoint,
        TotalCheckPoints,
        StartTile,
        CurTile,
        LeftTile,
        TotalTile,
        Timing,
        TimingAvg,
        SongTitle,
        Author,

        TotalAttempts
    }

    internal enum StateAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    internal sealed class StateStat
    {
        public StateStatKind Kind { get; set; }

        public string Label { get; set; } = "";

        public string Suffix { get; set; } = "";

        public string Separator { get; set; }

        public bool AsPercent { get; set; }

        public bool RemoveRichText { get; set; }

        internal string ResolveSeparator()
        {
            return Separator ?? StateGroupStore.DefaultSeparator(Kind);
        }

        public string Text { get; set; } = "";

        public string ImagePath { get; set; } = "";

        public float ImageHeight { get; set; } = 32f;

        public string ColorHex { get; set; } = "";

        public int Decimals { get; set; } = 1;

        public float RefreshInterval { get; set; }

        public bool Enabled { get; set; } = true;

        public bool PlayingOnly { get; set; } = true;
    }

    internal enum StateSlideEdge
    {
        Left,
        Right,
        Top,
        Bottom
    }

    internal sealed class StateMotion
    {
        public const float MinimumSeconds = 0.05f;
        public const float MaximumSeconds = 3f;
        public const string DefaultEase = "OutCubic";

        public bool Enabled { get; set; }
        public StateSlideEdge Edge { get; set; } = StateSlideEdge.Left;
        public string Ease { get; set; } = DefaultEase;
        public float Seconds { get; set; } = 0.5f;

        internal DG.Tweening.Ease ResolveEase()
        {
            return HemiEases.Parse(Ease);
        }

        internal void Normalize()
        {
            if (Edge < StateSlideEdge.Left || Edge > StateSlideEdge.Bottom)
                Edge = StateSlideEdge.Left;
            if (HemiEases.IndexOf(Ease) < 0)
                Ease = DefaultEase;
            Seconds = float.IsNaN(Seconds) || float.IsInfinity(Seconds)
                ? 0.5f
                : Mathf.Clamp(Seconds, MinimumSeconds, MaximumSeconds);
        }
    }

    internal sealed class StateGroup
    {
        public string Name { get; set; } = "Group";
        public bool Enabled { get; set; } = true;
        public StateAnchor Anchor { get; set; } = StateAnchor.TopLeft;
        public float OffsetX { get; set; } = 16f;
        public float OffsetY { get; set; } = 16f;
        public int FontSize { get; set; } = 22;
        public string ColorHex { get; set; } = "#FFFFFFFF";

        public bool Horizontal { get; set; }

        public float Spacing { get; set; } = 2f;

        public float ShadowX { get; set; } = 0.5f;

        public float ShadowY { get; set; } = -0.5f;

        public string ShadowColorHex { get; set; } = "#00000080";

        public List<StateStat> Stats { get; set; } = new List<StateStat>();

        public StateMotion Entrance { get; set; } = new StateMotion();

        public StateMotion Exit { get; set; } = new StateMotion();

        internal void NormalizeMotion()
        {
            if (Entrance == null)
                Entrance = new StateMotion();
            if (Exit == null)
                Exit = new StateMotion();
            Entrance.Normalize();
            Exit.Normalize();
        }

        internal void NormalizeNumbers()
        {
            OffsetX = HemiNumberSafety.FiniteOr(OffsetX, 16f);
            OffsetY = HemiNumberSafety.FiniteOr(OffsetY, 16f);
            Spacing = HemiNumberSafety.FiniteOr(Spacing, 2f);
            FontSize = Mathf.Clamp(FontSize, StateOverlay.MinimumFontSize, StateOverlay.MaximumFontSize);
            ShadowX = HemiNumberSafety.Clamp(ShadowX, -HemiTextMaterial.MaximumShadowOffset, HemiTextMaterial.MaximumShadowOffset, 0.5f);
            ShadowY = HemiNumberSafety.Clamp(ShadowY, -HemiTextMaterial.MaximumShadowOffset, HemiTextMaterial.MaximumShadowOffset, -0.5f);

            foreach (StateStat stat in Stats)
            {
                if (stat == null)
                    continue;

                stat.Decimals = Mathf.Clamp(stat.Decimals, 0, 6);
                stat.ImageHeight = HemiNumberSafety.Clamp(stat.ImageHeight, 4f, 1024f, 32f);
                stat.RefreshInterval = HemiNumberSafety.Clamp(stat.RefreshInterval, 0f, 2f,
                    StateGroupStore.DefaultRefreshInterval(stat.Kind));
            }
        }

        internal Color ResolveColor()
        {
            return StateGroupStore.ParseColor(ColorHex, Color.white);
        }

        internal Color ResolveShadowColor()
        {
            return StateGroupStore.ParseColor(ShadowColorHex, new Color(0f, 0f, 0f, 0.5f));
        }
    }

    internal static class StateGroupLayout
    {
        internal static Vector2 AnchorVector(StateAnchor anchor)
        {
            switch (anchor)
            {
                case StateAnchor.TopLeft: return new Vector2(0f, 1f);
                case StateAnchor.TopCenter: return new Vector2(0.5f, 1f);
                case StateAnchor.TopRight: return new Vector2(1f, 1f);
                case StateAnchor.MiddleLeft: return new Vector2(0f, 0.5f);
                case StateAnchor.MiddleCenter: return new Vector2(0.5f, 0.5f);
                case StateAnchor.MiddleRight: return new Vector2(1f, 0.5f);
                case StateAnchor.BottomLeft: return new Vector2(0f, 0f);
                case StateAnchor.BottomCenter: return new Vector2(0.5f, 0f);
                case StateAnchor.BottomRight: return new Vector2(1f, 0f);
                default: return new Vector2(0f, 1f);
            }
        }

        internal static Vector2 InwardSign(Vector2 anchorPoint)
        {
            return new Vector2(
                anchorPoint.x > 0.999f ? -1f : 1f,
                anchorPoint.y > 0.999f ? -1f : 1f);
        }

        internal static Vector2 ToAnchoredPosition(Vector2 anchorPoint, Vector2 offset)
        {
            Vector2 sign = InwardSign(anchorPoint);
            return new Vector2(offset.x * sign.x, offset.y * sign.y);
        }

        internal static void Place(RectTransform rect, StateAnchor anchor, Vector2 offset)
        {
            if (rect == null)
                return;

            Vector2 anchorPoint = AnchorVector(anchor);
            if (!rect.anchorMin.Equals(anchorPoint))
                rect.anchorMin = anchorPoint;
            if (!rect.anchorMax.Equals(anchorPoint))
                rect.anchorMax = anchorPoint;
            if (!rect.pivot.Equals(anchorPoint))
                rect.pivot = anchorPoint;

            Vector2 position = ToAnchoredPosition(anchorPoint, offset);
            if (!rect.anchoredPosition.Equals(position))
                rect.anchoredPosition = position;
        }

        internal static Vector2 ToOffset(Vector2 anchorPoint, Vector2 anchoredPosition)
        {
            return ToAnchoredPosition(anchorPoint, anchoredPosition);
        }

        internal static Vector2 AnchoredPositionFor(StateGroup group)
        {
            Vector2 anchorPoint = AnchorVector(group.Anchor);
            return ToAnchoredPosition(anchorPoint, new Vector2(group.OffsetX, group.OffsetY));
        }

        internal static TextAnchor ChildAlignment(StateAnchor anchor)
        {
            switch (anchor)
            {
                case StateAnchor.TopRight:
                case StateAnchor.MiddleRight:
                case StateAnchor.BottomRight:
                    return TextAnchor.UpperRight;
                case StateAnchor.TopCenter:
                case StateAnchor.MiddleCenter:
                case StateAnchor.BottomCenter:
                    return TextAnchor.UpperCenter;
                default:
                    return TextAnchor.UpperLeft;
            }
        }

        internal static string DisplayName(StateAnchor anchor)
        {
            switch (anchor)
            {
                case StateAnchor.TopLeft: return Interface.HemiLang.Get("SET_ANCHOR_TOP_LEFT");
                case StateAnchor.TopCenter: return Interface.HemiLang.Get("SET_ANCHOR_TOP_CENTER");
                case StateAnchor.TopRight: return Interface.HemiLang.Get("SET_ANCHOR_TOP_RIGHT");
                case StateAnchor.MiddleLeft: return Interface.HemiLang.Get("SET_ANCHOR_MIDDLE_LEFT");
                case StateAnchor.MiddleCenter: return Interface.HemiLang.Get("SET_ANCHOR_MIDDLE_CENTER");
                case StateAnchor.MiddleRight: return Interface.HemiLang.Get("SET_ANCHOR_MIDDLE_RIGHT");
                case StateAnchor.BottomLeft: return Interface.HemiLang.Get("SET_ANCHOR_BOTTOM_LEFT");
                case StateAnchor.BottomCenter: return Interface.HemiLang.Get("SET_ANCHOR_BOTTOM_CENTER");
                case StateAnchor.BottomRight: return Interface.HemiLang.Get("SET_ANCHOR_BOTTOM_RIGHT");
                default: return anchor.ToString();
            }
        }

        internal static string[] AnchorOptions()
        {
            return new[]
            {
                DisplayName(StateAnchor.TopLeft), DisplayName(StateAnchor.TopCenter), DisplayName(StateAnchor.TopRight),
                DisplayName(StateAnchor.MiddleLeft), DisplayName(StateAnchor.MiddleCenter), DisplayName(StateAnchor.MiddleRight),
                DisplayName(StateAnchor.BottomLeft), DisplayName(StateAnchor.BottomCenter), DisplayName(StateAnchor.BottomRight)
            };
        }
    }

    internal static class StateGroupStore
    {
        private const int CurrentVersion = 1;

        private static readonly List<StateGroup> groups = new List<StateGroup>();
        private static readonly HemiSaveDebounce writer = new HemiSaveDebounce(Save);
        private static string filePath;
        private static bool loaded;

        internal static IReadOnlyList<StateGroup> Groups => groups;

        internal static int Revision { get; private set; }

        internal static string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    string directory = Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name);
                    Directory.CreateDirectory(directory);
                    filePath = Path.Combine(directory, "StateGroups.json");
                }
                return filePath;
            }
        }

        internal static void Load()
        {
            if (loaded)
                return;
            loaded = true;

            try
            {
                if (File.Exists(FilePath))
                {
                    StateGroupFile file = JsonConvert.DeserializeObject<StateGroupFile>(File.ReadAllText(FilePath));
                    if (file != null && file.Groups != null)
                    {
                        groups.Clear();
                        foreach (StateGroup group in file.Groups)
                        {
                            if (group == null)
                                continue;
                            if (group.Stats == null)
                                group.Stats = new List<StateStat>();
                            AdoptRenamedLabels(group);
                            group.NormalizeNumbers();
                            group.NormalizeMotion();
                            groups.Add(group);
                        }
                        Revision++;
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not read state groups, starting from defaults: " + exception.Message);
            }

            groups.Clear();
            groups.Add(CreateDefaultGroup());
            Revision++;
            Save();
        }

        private static void AdoptRenamedLabels(StateGroup group)
        {
            for (int i = 0; i < group.Stats.Count; i++)
            {
                StateStat stat = group.Stats[i];
                if (stat == null)
                    continue;

                if (stat.Kind == StateStatKind.Attempts && string.Equals(stat.Label, "Try ", StringComparison.Ordinal))
                    stat.Label = DefaultLabel(StateStatKind.Attempts);
                else if (stat.Kind == StateStatKind.Checkpoints && string.Equals(stat.Label, "CP ", StringComparison.Ordinal))
                    stat.Label = DefaultLabel(StateStatKind.Checkpoints);
                else if (stat.Kind == StateStatKind.TimingScale && string.Equals(stat.Label, "Timing ", StringComparison.Ordinal))
                    stat.Label = DefaultLabel(StateStatKind.TimingScale);
            }
        }

        internal static void Reload()
        {
            loaded = false;
            groups.Clear();
            Load();
        }

        internal static void Save()
        {
            try
            {
                StateGroupFile file = new StateGroupFile { Version = CurrentVersion, Groups = new List<StateGroup>(groups) };
                HemiAtomicFile.WriteAllText(
                    FilePath, JsonConvert.SerializeObject(file, Formatting.Indented));
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not save state groups: " + exception.Message);
            }
        }

        internal static void MarkChanged()
        {
            Revision++;
            writer.Request();
        }

        internal static void MarkStyleChanged()
        {
            writer.Request();
        }

        internal static void TickSave()
        {
            writer.Tick();
        }

        internal static void FlushSave()
        {
            writer.Flush();
        }

        internal static StateGroup AddGroup()
        {
            StateGroup group = new StateGroup { Name = "Group " + (groups.Count + 1) };
            groups.Add(group);
            MarkChanged();
            return group;
        }

        internal static void RemoveGroup(StateGroup group)
        {
            if (group != null && groups.Remove(group))
                MarkChanged();
        }

        internal static void MoveGroup(StateGroup group, int delta)
        {
            if (Move(groups, group, delta))
                MarkChanged();
        }

        internal static void MoveStat(StateGroup group, StateStat stat, int delta)
        {
            if (group != null && group.Stats != null && Move(group.Stats, stat, delta))
                MarkChanged();
        }

        private static bool Move<T>(List<T> list, T item, int delta)
        {
            int index = list.IndexOf(item);
            if (index < 0)
                return false;
            int target = Mathf.Clamp(index + delta, 0, list.Count - 1);
            if (target == index)
                return false;
            list.RemoveAt(index);
            list.Insert(target, item);
            return true;
        }

        private static readonly Dictionary<string, Color> parsedColors = new Dictionary<string, Color>(StringComparer.Ordinal);

        internal static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
                return fallback;

            if (parsedColors.TryGetValue(hex, out Color cached))
                return cached;

            if (!ColorUtility.TryParseHtmlString(hex, out Color parsed))
                return fallback;

            parsedColors[hex] = parsed;
            return parsed;
        }

        internal static string ToHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        internal static string DefaultLabel(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Progress: return "Progress ";
                case StateStatKind.Accuracy: return "Acc ";
                case StateStatKind.XAccuracy: return "XAcc ";
                case StateStatKind.MusicTime: return "Music ";
                case StateStatKind.MapTime: return "Map ";
                case StateStatKind.TileBpm: return "TBPM ";
                case StateStatKind.CurBpm: return "CBPM ";
                case StateStatKind.Kps: return "KPS ";
                case StateStatKind.Checkpoints: return "Checkpoints ";
                case StateStatKind.Attempts: return "Attempts ";
                case StateStatKind.TotalAttempts: return "TotalAttempts ";
                case StateStatKind.TimingScale: return "Timing Scale ";
                case StateStatKind.Fps: return "FPS ";
                case StateStatKind.CurCheckPoint: return "CurCheckPoint ";
                case StateStatKind.TotalCheckPoints: return "TotalCheckPoints ";
                case StateStatKind.StartTile: return "StartTile ";
                case StateStatKind.CurTile: return "CurTile ";
                case StateStatKind.LeftTile: return "LeftTile ";
                case StateStatKind.TotalTile: return "TotalTile ";
                case StateStatKind.Timing: return "Timing ";
                case StateStatKind.TimingAvg: return "TimingAvg ";
                case StateStatKind.Author: return "Author ";
                default: return "";
            }
        }

        internal static string DefaultSeparator(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Text:
                case StateStatKind.Image:
                case StateStatKind.JudgementCounter:
                case StateStatKind.SongTitle:
                    return "";
                case StateStatKind.TimingScale:
                    return "- ";
                default:
                    return "| ";
            }
        }

        internal static bool SupportsPercent(StateStatKind kind)
        {
            return kind == StateStatKind.TimingScale;
        }

        internal static bool SupportsRemoveRichText(StateStatKind kind)
        {
            return kind == StateStatKind.SongTitle || kind == StateStatKind.Author;
        }

        internal static string DefaultSuffix(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Progress:
                case StateStatKind.Accuracy:
                case StateStatKind.XAccuracy:
                    return "%";
                case StateStatKind.Timing:
                case StateStatKind.TimingAvg:
                    return "ms";
                default:
                    return "";
            }
        }

        internal static bool SupportsDecimals(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Progress:
                case StateStatKind.Accuracy:
                case StateStatKind.XAccuracy:
                case StateStatKind.TileBpm:
                case StateStatKind.CurBpm:
                case StateStatKind.Kps:
                case StateStatKind.TimingScale:
                case StateStatKind.Fps:
                case StateStatKind.Timing:
                case StateStatKind.TimingAvg:
                    return true;
                default:
                    return false;
            }
        }

        internal static string DisplayName(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Progress: return Interface.HemiLang.Get("STATE_KIND_PROGRESS");
                case StateStatKind.Accuracy: return Interface.HemiLang.Get("STATE_KIND_ACCURACY");
                case StateStatKind.XAccuracy: return Interface.HemiLang.Get("STATE_KIND_XACCURACY");
                case StateStatKind.MusicTime: return Interface.HemiLang.Get("STATE_KIND_MUSIC_TIME");
                case StateStatKind.MapTime: return Interface.HemiLang.Get("STATE_KIND_MAP_TIME");
                case StateStatKind.TileBpm: return Interface.HemiLang.Get("STATE_KIND_TILE_BPM");
                case StateStatKind.CurBpm: return Interface.HemiLang.Get("STATE_KIND_CUR_BPM");
                case StateStatKind.Kps: return Interface.HemiLang.Get("STATE_KIND_KPS");
                case StateStatKind.Checkpoints: return Interface.HemiLang.Get("STATE_KIND_CHECKPOINTS");
                case StateStatKind.Attempts: return Interface.HemiLang.Get("STATE_KIND_ATTEMPTS");
                case StateStatKind.TotalAttempts: return Interface.HemiLang.Get("STATE_KIND_TOTAL_ATTEMPTS");
                case StateStatKind.TimingScale: return Interface.HemiLang.Get("STATE_KIND_TIMING_SCALE");
                case StateStatKind.Fps: return Interface.HemiLang.Get("STATE_KIND_FPS");
                case StateStatKind.Text: return Interface.HemiLang.Get("STATE_KIND_TEXT");
                case StateStatKind.Image: return Interface.HemiLang.Get("STATE_KIND_IMAGE");
                case StateStatKind.JudgementCounter: return Interface.HemiLang.Get("STATE_KIND_JUDGEMENT_COUNTER");
                case StateStatKind.CurCheckPoint: return Interface.HemiLang.Get("STATE_KIND_CUR_CHECKPOINT");
                case StateStatKind.TotalCheckPoints: return Interface.HemiLang.Get("STATE_KIND_TOTAL_CHECKPOINTS");
                case StateStatKind.StartTile: return Interface.HemiLang.Get("STATE_KIND_START_TILE");
                case StateStatKind.CurTile: return Interface.HemiLang.Get("STATE_KIND_CUR_TILE");
                case StateStatKind.LeftTile: return Interface.HemiLang.Get("STATE_KIND_LEFT_TILE");
                case StateStatKind.TotalTile: return Interface.HemiLang.Get("STATE_KIND_TOTAL_TILE");
                case StateStatKind.Timing: return Interface.HemiLang.Get("STATE_KIND_TIMING");
                case StateStatKind.TimingAvg: return Interface.HemiLang.Get("STATE_KIND_TIMING_AVG");
                case StateStatKind.SongTitle: return Interface.HemiLang.Get("STATE_KIND_SONG_TITLE");
                case StateStatKind.Author: return Interface.HemiLang.Get("STATE_KIND_AUTHOR");
                default: return kind.ToString();
            }
        }

        internal static string Category(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Progress:
                case StateStatKind.Accuracy:
                case StateStatKind.XAccuracy:
                case StateStatKind.JudgementCounter:
                case StateStatKind.Timing:
                case StateStatKind.TimingAvg:
                    return "Accuracy";
                case StateStatKind.MusicTime:
                case StateStatKind.MapTime:
                    return "Time";
                case StateStatKind.TileBpm:
                case StateStatKind.CurBpm:
                    return "BPM";
                case StateStatKind.StartTile:
                case StateStatKind.CurTile:
                case StateStatKind.LeftTile:
                case StateStatKind.TotalTile:
                    return "Tile";
                case StateStatKind.Kps:
                case StateStatKind.Checkpoints:
                case StateStatKind.CurCheckPoint:
                case StateStatKind.TotalCheckPoints:
                case StateStatKind.Attempts:
                case StateStatKind.TotalAttempts:
                case StateStatKind.TimingScale:
                case StateStatKind.Fps:
                    return "Play";
                case StateStatKind.SongTitle:
                case StateStatKind.Author:
                    return "Level";
                default:
                    return "Custom";
            }
        }

        internal static string CategoryDisplayName(string category)
        {
            switch (category)
            {
                case "Accuracy": return Interface.HemiLang.Get("STATE_CATEGORY_ACCURACY");
                case "Time": return Interface.HemiLang.Get("STATE_CATEGORY_TIME");
                case "BPM": return Interface.HemiLang.Get("STATE_CATEGORY_BPM");
                case "Tile": return Interface.HemiLang.Get("STATE_CATEGORY_TILE");
                case "Play": return Interface.HemiLang.Get("STATE_CATEGORY_PLAY");
                case "Level": return Interface.HemiLang.Get("STATE_CATEGORY_LEVEL");
                case "Custom": return Interface.HemiLang.Get("STATE_CATEGORY_CUSTOM");
                default: return category ?? "";
            }
        }

        internal static bool SupportsAlwaysVisible(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Fps:
                case StateStatKind.Text:
                case StateStatKind.Image:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsAlwaysVisible(StateStat stat)
        {
            return stat != null && !stat.PlayingOnly && SupportsAlwaysVisible(stat.Kind);
        }

        private static readonly string[] CategoryOrder = { "Accuracy", "Time", "BPM", "Tile", "Play", "Level", "Custom" };

        internal static List<StateStatKind> PickerOrder()
        {
            List<StateStatKind> ordered = new List<StateStatKind>();
            foreach (string category in CategoryOrder)
            {
                foreach (StateStatKind kind in Enum.GetValues(typeof(StateStatKind)))
                {
                    if (string.Equals(Category(kind), category, StringComparison.Ordinal))
                        ordered.Add(kind);
                }
            }
            return ordered;
        }

        internal static StateStat CreateStat(StateStatKind kind)
        {
            return new StateStat
            {
                Kind = kind,
                Label = DefaultLabel(kind),
                Suffix = DefaultSuffix(kind),
                Decimals = kind == StateStatKind.Fps ? 0 : 1,
                RefreshInterval = DefaultRefreshInterval(kind),
                Text = kind == StateStatKind.Text ? "Text" : ""
            };
        }

        internal static float DefaultRefreshInterval(StateStatKind kind)
        {
            switch (kind)
            {
                case StateStatKind.Fps:
                case StateStatKind.Kps:
                    return 0.25f;
                default:
                    return 0f;
            }
        }

        private static StateGroup CreateDefaultGroup()
        {
            StateGroup group = new StateGroup { Name = "Default" };
            group.Stats.Add(CreateStat(StateStatKind.Progress));
            group.Stats.Add(CreateStat(StateStatKind.XAccuracy));
            group.Stats.Add(CreateStat(StateStatKind.TileBpm));
            group.Stats.Add(CreateStat(StateStatKind.CurBpm));
            return group;
        }

        private sealed class StateGroupFile
        {
            public int Version { get; set; }
            public List<StateGroup> Groups { get; set; }
        }
    }

    internal static class LevelStats
    {
        private static int startTile;

        private static readonly List<int> checkpoints = new List<int>();

        private static bool checkpointsKnown;

        internal static int StartTile => startTile;

        internal static int CurTile
        {
            get
            {
                try
                {
                    scrController controller = ADOBase.controller;
                    return controller == null ? 0 : Math.Max(0, controller.currentSeqID);
                }
                catch { return 0; }
            }
        }

        internal static int TotalTile
        {
            get
            {
                try
                {
                    List<scrFloor> floors = ADOBase.lm?.listFloors;
                    return floors == null ? 0 : Math.Max(0, floors.Count - 1);
                }
                catch { return 0; }
            }
        }

        internal static int LeftTile => Math.Max(0, TotalTile - CurTile);

        internal static int CurCheckPoint
        {
            get
            {
                try
                {
                    EnsureCheckpoints();
                    int tile = CurTile;
                    int reached = 0;
                    for (int i = 0; i < checkpoints.Count; i++)
                    {
                        if (checkpoints[i] <= tile)
                            reached++;
                    }
                    return reached;
                }
                catch { return 0; }
            }
        }

        internal static int TotalCheckPoints
        {
            get
            {
                try
                {
                    EnsureCheckpoints();
                    return checkpoints.Count;
                }
                catch { return 0; }
            }
        }

        internal static string SongTitle(bool plain)
        {
            string artist = artistField.Get(plain);
            string title = titleField.Get(plain);
            return (plain ? plainTitleCache : richTitleCache).Get(artist, title);
        }

        internal static string Author(bool plain)
        {
            return authorField.Get(plain);
        }

        private sealed class LevelField
        {
            private readonly Func<ADOFAI.LevelData, string> read;
            private string raw = "";
            private string clean = "";

            internal LevelField(Func<ADOFAI.LevelData, string> read)
            {
                this.read = read;
            }

            internal string Get(bool plain)
            {
                string value;
                try
                {
                    ADOFAI.LevelData data = ADOBase.customLevel?.levelData;
                    value = data == null ? "" : read(data) ?? "";
                }
                catch
                {
                    value = "";
                }

                if (!plain)
                    return value.Trim();

                if (!string.Equals(value, raw, StringComparison.Ordinal))
                {
                    raw = value;
                    clean = HemiTextSafety.PlainSingleLine(value);
                }
                return clean;
            }
        }

        private sealed class TitleCache
        {
            private string lastArtist;
            private string lastTitle;
            private string composed = "";

            internal string Get(string artist, string title)
            {
                if (ReferenceEquals(artist, lastArtist) && ReferenceEquals(title, lastTitle))
                    return composed;

                lastArtist = artist;
                lastTitle = title;
                if (string.IsNullOrEmpty(artist))
                    composed = title;
                else if (string.IsNullOrEmpty(title))
                    composed = artist;
                else
                    composed = artist + " - " + title;
                return composed;
            }
        }

        private static readonly LevelField artistField = new LevelField(data => data.artist);
        private static readonly LevelField titleField = new LevelField(data => data.song);
        private static readonly LevelField authorField = new LevelField(data => data.author);
        private static readonly TitleCache plainTitleCache = new TitleCache();
        private static readonly TitleCache richTitleCache = new TitleCache();

        internal static void Rewind()
        {
            checkpointsKnown = false;
            checkpoints.Clear();

            try { startTile = Math.Max(0, GCS.checkpointNum); }
            catch { startTile = 0; }
        }

        private static void EnsureCheckpoints()
        {
            if (checkpointsKnown)
                return;

            checkpointsKnown = true;
            checkpoints.Clear();

            List<scrFloor> floors = ADOBase.lm?.listFloors;
            if (floors == null)
                return;

            for (int i = 0; i < floors.Count; i++)
            {
                scrFloor floor = floors[i];
                if (floor == null)
                    continue;

                ffxCheckpoint checkpoint = floor.GetComponent<ffxCheckpoint>();
                if (checkpoint == null)
                    continue;

                checkpoints.Add(Math.Max(0, floor.seqID + checkpoint.checkpointTileOffset));
            }

            checkpoints.Sort();
        }
    }
}
