using System;
using System.Collections.Generic;
using System.Globalization;
using HemiTweaks.Tuf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiTufScreen
    {
        private enum Mode
        {
            Levels,
            Packs
        }

        private static readonly string[] SortKeys = { "TUF_SORT_RECENT", "TUF_SORT_DIFFICULTY", "TUF_SORT_CLEARS", "TUF_SORT_LIKES" };
        private static readonly string[] PackSortKeys = { "TUF_PACKSORT_RECENT", "TUF_PACKSORT_NAME", "TUF_PACKSORT_LEVELS" };

        private static Mode mode = Mode.Levels;
        private static string query = "";
        private static TufSort sort = TufSort.Recent;
        private static TufPackSort packSort = TufPackSort.Recent;
        private static bool ascending;
        private static bool showQuantum;
        private static bool installedOnly;
        private static TufDifficultyFilter filter = TufDifficultyFilter.All;

        private static readonly List<TufLevel> levels = new List<TufLevel>();
        private static readonly List<TufPack> packs = new List<TufPack>();
        private static TufListState state = TufListState.Idle;
        private static string error;
        private static bool hasMore;
        private static int offset;
        private static long awaiting;

        private static TufJob watched;
        private static TufJobState watchedState;
        private static float nextProgressRefresh;

        private static TextMeshProUGUI busyLabel;

        private static RectTransform busyFill;

        private static TufJob busyJob;

        private static string officialNotice;

        internal static void Build(RectTransform parent)
        {

            busyLabel = null;
            busyFill = null;
            busyJob = null;

            RectTransform root = HemiKit.Rect("Tuf", parent);
            HemiKit.Stretch(root, 24f, 24f, 20f, 16f);

            float headerHeight = HeaderHeight();
            BuildHeader(root, headerHeight);

            RectTransform listArea = HemiKit.Rect("ListArea", root);
            listArea.anchorMin = Vector2.zero;
            listArea.anchorMax = Vector2.one;
            listArea.offsetMin = Vector2.zero;
            listArea.offsetMax = new Vector2(0f, -(headerHeight + HemiTheme.Gap));

            RectTransform content = HemiKit.Scroll(listArea, out ScrollRect scrollRect);
            BuildList(content);

            scrollRect.gameObject.AddComponent<HemiScrollEnd>().Initialize(scrollRect, LoadMore);

            if (state == TufListState.Idle)
                Search(true);
        }

        private const float HeaderGap = 8f;
        private const float SearchRowHeight = 28f;
        private const float ToolRowHeight = 28f;
        private const float SpecialRowHeight = 36f;

        private const float RangeRowHeight = HemiRows.RangeRowHeight;

        private static float HeaderHeight()
        {
            float height = HemiTheme.Row(SearchRowHeight) + HeaderGap + HemiTheme.Row(ToolRowHeight);
            if (mode != Mode.Levels)
                return height;

            height += HeaderGap + HemiTheme.Row(RangeRowHeight) + 1f;
            if (showQuantum)
                height += HemiTheme.Row(RangeRowHeight) + 1f;
            height += HemiTheme.Row(SpecialRowHeight);
            return height;
        }

        private static void BuildHeader(RectTransform root, float headerHeight)
        {
            HemiColors colors = HemiTheme.Colors;

            RectTransform header = HemiKit.VBox("Header", root, HeaderGap, new RectOffset(0, 0, 0, 0), false);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -headerHeight);
            header.offsetMax = Vector2.zero;

            Image searchBar = HemiKit.Panel("SearchRow", header, colors.Field, 9f);
            HemiKit.Border(searchBar.transform, colors.Line, 9f);
            HemiKit.Size(searchBar.gameObject, -1f, HemiTheme.Row(SearchRowHeight), 1f);

            TMP_InputField searchInput = HemiKit.SearchBar(searchBar, query,
                mode == Mode.Levels ? HemiLang.Get("TUF_SEARCH_LEVELS") : HemiLang.Get("TUF_SEARCH_PACKS"),
                HemiTheme.Col(84f));

            searchInput.onValueChanged.AddListener(delegate (string value) { query = value ?? ""; });
            searchInput.onSubmit.AddListener(delegate { Search(true); });

            Image refreshChip = HemiKit.Panel("Refresh", searchBar.rectTransform, new Color(0f, 0f, 0f, 0f), 5f);
            HemiKit.Border(refreshChip.transform, colors.Border, 5f);
            RectTransform refreshRect = refreshChip.rectTransform;
            refreshRect.anchorMin = new Vector2(1f, 0.5f);
            refreshRect.anchorMax = new Vector2(1f, 0.5f);
            refreshRect.pivot = new Vector2(1f, 0.5f);
            refreshRect.anchoredPosition = new Vector2(-8f, 0f);
            refreshRect.sizeDelta = new Vector2(HemiTheme.Col(68f), HemiTheme.Row(20f));
            TextMeshProUGUI refreshLabel = HemiKit.Text(
                "Label", refreshRect, HemiLang.Get("TUF_REFRESH"), 10.5f, colors.Faint, false, TextAlignmentOptions.Center);
            HemiKit.Stretch(refreshLabel.rectTransform);
            Button refreshButton = refreshChip.gameObject.AddComponent<Button>();
            refreshButton.targetGraphic = refreshChip;
            refreshButton.transition = Selectable.Transition.None;
            refreshButton.onClick.AddListener(delegate
            {
                TufApi.ClearCache();
                Search(true);
            });
            refreshChip.gameObject.AddComponent<HemiHover>().Initialize(refreshChip, refreshChip.color);

            RectTransform row = HemiKit.HBox("Tools", header, HemiTheme.Gap, new RectOffset(0, 0, 0, 0));
            HemiKit.Size(row.gameObject, -1f, HemiTheme.Row(ToolRowHeight), 1f);

            BuildModeSegments(row);

            HemiKit.Size(HemiKit.Rect("Gap", row).gameObject, 4f, 10f);

            if (mode == Mode.Levels)
                BuildSortChip(row, SortKeys, (int)sort, index =>
                {
                    sort = (TufSort)index;
                    Search(true);
                });
            else
                BuildSortChip(row, PackSortKeys, (int)packSort, index =>
                {
                    packSort = (TufPackSort)index;
                    Search(true);
                });

            Toggle(row, ascending ? "▲" : "▼", false, delegate
            {
                ascending = !ascending;
                Search(true);
            });

            if (mode == Mode.Levels)
            {
                Toggle(row, HemiLang.Get("TUF_INSTALLED"), installedOnly, delegate
                {
                    installedOnly = !installedOnly;
                    HemiRoot.Instance?.Refresh();
                }, "Installed");

                Toggle(row, HemiLang.Get("TUF_QUANTUM"), showQuantum, delegate
                {
                    showQuantum = !showQuantum;
                    if (!showQuantum)
                    {
                        filter = filter.WithoutQuantum();
                        Search(true);
                    }
                    else
                    {
                        HemiRoot.Instance?.Refresh();
                    }
                }, "Quantum");

                BuildFilters(header);
            }
        }

        private static void BuildFilters(RectTransform header)
        {
            HemiRows.RangeRow(header, HemiLang.Get("TUF_DIFFICULTY_RANGE"),
                TufDifficultyFilter.RankedNames, TufDifficultyFilter.RankedColors,
                filter.MinIndex, filter.MaxIndex,
                (min, max) => filter = filter.WithRange(min, max),
                (min, max) => Search(true));

            if (showQuantum)
            {
                HemiRows.RangeRow(header, HemiLang.Get("TUF_QUANTUM_RANGE"),
                    TufDifficultyFilter.QuantumNames, TufDifficultyFilter.QuantumColors,
                    filter.QuantumMinIndex, filter.QuantumMaxIndex,
                    (min, max) => filter = filter.WithQuantumRange(min, max),
                    (min, max) => Search(true));
            }

            RectTransform specials = HemiRows.Card(header, SpecialRowHeight);
            HemiRows.Label(specials, HemiLang.Get("TUF_SPECIAL"), null, 110f);
            for (int i = 0; i < TufDifficultyFilter.SpecialNames.Count; i++)
            {
                string name = TufDifficultyFilter.SpecialNames[i];
                Toggle(specials, name, filter.IsSelected(name), delegate
                {
                    filter = filter.Toggle(name);
                    Search(true);
                });
            }
        }

        private static void BuildModeSegments(RectTransform row)
        {
            HemiColors colors = HemiTheme.Colors;

            Image bar = HemiKit.Panel("Mode", row, colors.Field, 10f);
            HemiKit.Border(bar.transform, colors.Line, 10f);
            HemiKit.Size(bar.gameObject, HemiTheme.Col(108f), HemiTheme.Row(28f));

            RectTransform segments = HemiKit.HBox("Segments", bar.rectTransform, 2f, new RectOffset(3, 3, 3, 3));
            HemiKit.Stretch(segments);

            Segment(segments, HemiLang.Get("TUF_MODE_LEVELS"), mode == Mode.Levels, delegate { SetMode(Mode.Levels); }, "Levels");
            Segment(segments, HemiLang.Get("TUF_MODE_PACKS"), mode == Mode.Packs, delegate { SetMode(Mode.Packs); }, "Packs");
        }

        private static void Segment(RectTransform parent, string label, bool active, Action action, string name)
        {
            HemiColors colors = HemiTheme.Colors;
            Image chip = HemiKit.Panel(name, parent, active ? colors.Chip : new Color(0f, 0f, 0f, 0f), 7f);
            HemiKit.Size(chip.gameObject, 10f, -1f, 1f, 1f);

            Button button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chip;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate { action(); });
            chip.gameObject.AddComponent<HemiHover>().Initialize(chip, chip.color);

            TextMeshProUGUI text = HemiKit.Text(
                "Label", chip.transform, label, 12f, active ? colors.Text : colors.Muted, active, TextAlignmentOptions.Center);
            HemiKit.Stretch(text.rectTransform, 4f, 4f, 0f, 0f);
        }

        private static void BuildSortChip(RectTransform row, string[] keys, int selected, Action<int> pick)
        {
            HemiColors colors = HemiTheme.Colors;

            Image chip = HemiKit.Panel("Sort", row, colors.Button, 7f);
            HemiKit.Border(chip.transform, colors.Border, 7f);
            HemiKit.Size(chip.gameObject, HemiTheme.Col(118f), HemiTheme.Row(24f));

            string[] options = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                options[i] = HemiLang.Get(keys[i]);

            TextMeshProUGUI value = HemiKit.Text("Value", chip.transform,
                selected >= 0 && selected < options.Length ? options[selected] : "", 12f, colors.Text);
            HemiKit.Stretch(value.rectTransform, 10f, 22f, 0f, 0f);

            TextMeshProUGUI arrow = HemiKit.Text("Arrow", chip.transform, "▼", 8f, colors.Faint, false, TextAlignmentOptions.Center);
            RectTransform arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(18f, 0f);
            arrowRect.anchoredPosition = new Vector2(-2f, 0f);

            RectTransform anchor = chip.rectTransform;
            Button button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chip;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate
            {
                HemiPopup.Open(anchor, options, selected, index =>
                {
                    if (index >= 0 && index < options.Length)
                        pick(index);
                });
            });
            chip.gameObject.AddComponent<HemiHover>().Initialize(chip, colors.Button);
        }

        private static void Toggle(RectTransform row, string label, bool active, Action action, string name = null)
        {
            HemiColors colors = HemiTheme.Colors;
            Button button = HemiKit.Button(name ?? label, row, label,
                active ? colors.Chip : new Color(0f, 0f, 0f, 0.01f),
                active ? colors.Text : colors.Muted,
                delegate { action(); }, 12f, 7f);
            if (!active)
                HemiKit.Border(button.transform, colors.Border, 7f);
            HemiKit.Size(button.gameObject, HemiTheme.Col(Mathf.Max(40f, CaptionWidth(label) * 8f + 22f)), HemiTheme.Row(24f));
        }

        private static float CaptionWidth(string label)
        {
            if (string.IsNullOrEmpty(label))
                return 0f;

            float width = 0f;
            foreach (char character in label)
            {
                bool wide = (character >= 0x1100 && character <= 0x115F)
                    || (character >= 0x2E80 && character <= 0xA4CF)
                    || (character >= 0xAC00 && character <= 0xD7A3)
                    || (character >= 0xF900 && character <= 0xFAFF)
                    || (character >= 0xFE30 && character <= 0xFE6F)
                    || (character >= 0xFF00 && character <= 0xFF60)
                    || (character >= 0xFFE0 && character <= 0xFFE6);

                width += wide ? 2f : 1f;
            }

            return width;
        }

        private static void SetMode(Mode value)
        {
            if (mode == value)
                return;

            mode = value;
            levels.Clear();
            packs.Clear();
            state = TufListState.Idle;
            offset = 0;

            officialNotice = null;
            HemiRoot.Instance?.Refresh();
        }

        private static void BuildList(RectTransform content)
        {
            if (mode == Mode.Levels && installedOnly)
            {
                BuildInstalledList(content);
                return;
            }

            if (state == TufListState.Loading && levels.Count == 0 && packs.Count == 0)
            {
                HemiRows.NoteRow(content, HemiLang.Get("TUF_LOADING"));
                return;
            }

            if (state == TufListState.Error)
            {
                HemiRows.ErrorRow(content, error ?? HemiLang.Get("TUF_ERROR_UNREACHABLE"));
                return;
            }

            if (!string.IsNullOrEmpty(officialNotice))
                HemiRows.ErrorRow(content, officialNotice);

            if (mode == Mode.Packs)
            {
                if (packs.Count == 0)
                {
                    HemiRows.NoteRow(content, HemiLang.Get("TUF_NO_PACKS"));
                    return;
                }

                for (int i = 0; i < packs.Count; i++)
                    BuildPackRow(content, packs[i]);
            }
            else
            {
                if (levels.Count == 0)
                {
                    HemiRows.NoteRow(content, HemiLang.Get("TUF_NO_LEVELS"));
                    return;
                }

                HashSet<int> installedIds = null;
                for (int i = 0; i < levels.Count; i++)
                    BuildLevelRow(content, levels[i], ref installedIds);
            }

            if (state == TufListState.Loading && (levels.Count > 0 || packs.Count > 0))
                TailNote(content, HemiLang.Get("TUF_LOADING_MORE"));
            else if (hasMore)
                TailNote(content, HemiLang.Get("TUF_SCROLL_MORE"));
        }

        private static void TailNote(RectTransform content, string text)
        {
            TextMeshProUGUI note = HemiKit.Text(
                "TailNote", content, text, 12f, HemiTheme.Colors.Faint, false, TextAlignmentOptions.Center);
            HemiKit.Size(note.gameObject, -1f, HemiTheme.Row(28f), 1f);
        }

        private static void BuildInstalledList(RectTransform content)
        {
            if (!string.IsNullOrEmpty(officialNotice))
                HemiRows.ErrorRow(content, officialNotice);

            List<TufLevel> installed = TufLibrary.InstalledLevels();
            if (installed.Count == 0)
            {
                HemiRows.NoteRow(content, HemiLang.Get("TUF_NOTHING_INSTALLED"));
                return;
            }

            HashSet<int> installedIds = TufLibrary.InstalledIds();
            for (int i = 0; i < installed.Count; i++)
                BuildLevelRow(content, installed[i], ref installedIds);
        }

        private static void BuildLevelRow(RectTransform content, TufLevel level, ref HashSet<int> installedIds)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiRows.Card(content, 64f);

            RectTransform badge = HemiKit.Rect("Badge", row);
            HemiKit.Size(badge.gameObject, HemiTheme.Col(58f), HemiTheme.Row(40f));
            Color plateColor = ParseColor(level.DifficultyColor);
            Image plate = HemiKit.Panel("Plate", badge, plateColor, 9f);
            HemiKit.Stretch(plate.rectTransform);
            plate.raycastTarget = false;

            Color ink = plateColor.grayscale > 0.6f ? HemiTheme.Hex(0x1B1B23) : Color.white;
            TextMeshProUGUI difficulty = HemiKit.Text("Diff", plate.rectTransform,
                level.DifficultyName, 13f, ink, true, TextAlignmentOptions.Center);
            HemiKit.ShowMarkupLiterally(difficulty);
            HemiKit.Stretch(difficulty.rectTransform, 2f, 2f, 2f, 2f);

            RectTransform column = HemiKit.VBox("Text", row, 0f, new RectOffset(0, 0, 0, 0), false);
            HemiKit.Size(column.gameObject, 300f, HemiTheme.Row(42f), 1f);

            TextMeshProUGUI song = HemiKit.Text("Song", column, level.Song, 14f, colors.Text, true);
            HemiKit.ShowMarkupLiterally(song);
            HemiKit.Size(song.gameObject, -1f, HemiTheme.Row(22f), 1f);

            TextMeshProUGUI detail = HemiKit.Text("Detail", column,
                HemiLang.Get("TUF_LEVEL_DETAIL", level.Artist, level.Creator, level.Clears, level.Likes),
                12f, colors.Muted);
            HemiKit.ShowMarkupLiterally(detail);
            HemiKit.Size(detail.gameObject, -1f, HemiTheme.Row(18f), 1f);

            BuildLevelAction(row, level, ref installedIds);
        }

        private static void BuildLevelAction(RectTransform row, TufLevel level, ref HashSet<int> installedIds)
        {
            HemiColors colors = HemiTheme.Colors;

            string world = TufOfficial.WorldKeyOf(level);
            if (world != null)
            {
                string blocked = TufOfficial.BlockedReason(world);
                Button play = HemiKit.Button("Play", row, blocked == null ? HemiLang.Get("TUF_PLAY") : HemiLang.Get("TUF_UNAVAILABLE"),
                    blocked == null ? colors.Accent : colors.Button,
                    blocked == null ? Color.white : colors.Muted,
                    delegate
                    {
                        bool opened = TufOfficial.Open(world, out string reason);
                        AfterOpen(opened, reason);
                    }, 12.5f, 7f);
                HemiKit.Size(play.gameObject, HemiTheme.Col(116f), HemiTheme.Row(24f));
                return;
            }

            TufJob job = TufDownloads.Find(level.Id);

            if (job != null && (job.State == TufJobState.Downloading || job.State == TufJobState.Extracting))
            {
                RectTransform strip = HemiKit.Rect("Busy", row);
                HemiKit.Size(strip.gameObject, HemiTheme.Col(158f), HemiTheme.Row(24f));

                Image railImage = HemiKit.Panel("Rail", strip, HemiTweaksMod.IsDarkMode
                    ? new Color(1f, 1f, 1f, 0.14f)
                    : new Color(0f, 0f, 0f, 0.12f), 5f);
                RectTransform rail = railImage.rectTransform;
                rail.anchorMin = new Vector2(0f, 0.5f);
                rail.anchorMax = new Vector2(1f, 0.5f);
                rail.pivot = new Vector2(0.5f, 0.5f);
                rail.offsetMin = new Vector2(0f, -5f);
                rail.offsetMax = new Vector2(-HemiTheme.Col(42f), 5f);
                railImage.raycastTarget = false;

                Image fill = HemiKit.Panel("Fill", rail, colors.Accent, 5f);
                RectTransform fillRect = fill.rectTransform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(Mathf.Clamp01(job.Progress), 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fill.raycastTarget = false;

                TextMeshProUGUI busy = HemiKit.Text("Label", strip, BusyText(job), 12f, colors.Muted, false, TextAlignmentOptions.Right);
                RectTransform busyRect = busy.rectTransform;
                busyRect.anchorMin = new Vector2(1f, 0f);
                busyRect.anchorMax = new Vector2(1f, 1f);
                busyRect.pivot = new Vector2(1f, 0.5f);
                busyRect.sizeDelta = new Vector2(HemiTheme.Col(38f), 0f);
                busyRect.anchoredPosition = Vector2.zero;

                busyLabel = busy;
                busyFill = fillRect;
                busyJob = job;
                Button cancel = HemiKit.Button("Cancel", row, HemiLang.Get("UI_CANCEL"),
                    colors.Button, colors.Muted, () => TufDownloads.Cancel(job), 12f, 7f);
                HemiKit.Size(cancel.gameObject, HemiTheme.Col(64f), HemiTheme.Row(24f));
                return;
            }

            if (installedIds == null)
                installedIds = TufLibrary.InstalledIds();

            if (installedIds.Contains(level.Id))
            {
                Button folderButton = HemiKit.Button("Folder", row, "", colors.Button, colors.Muted, delegate
                {
                    HemiShell.OpenFolder(TufLibrary.FolderOf(level.Id));
                }, 12.5f, 8f);
                HemiKit.Size(folderButton.gameObject, HemiTheme.Col(36f), HemiTheme.Row(24f));
                HemiIcons.DrawFolder(folderButton.transform, colors.Muted, 15f);

                Button play = null;
                play = HemiKit.Button("Play", row, HemiLang.Get("TUF_PLAY"), colors.Accent, Color.white, delegate
                {
                    StartLevel(level, play == null ? null : play.GetComponent<RectTransform>());
                }, 12.5f, 7f);
                HemiKit.Size(play.gameObject, HemiTheme.Col(90f), HemiTheme.Row(24f));
                return;
            }

            if (level.Download == null)
            {
                TextMeshProUGUI none = HemiKit.Text("None", row, HemiLang.Get("TUF_NO_DOWNLOAD"), 12f, colors.Muted, false, TextAlignmentOptions.Center);
                HemiKit.Size(none.gameObject, HemiTheme.Col(116f), HemiTheme.Row(24f));
                return;
            }

            bool busyElsewhere = TufDownloads.IsBusy;
            Button download = HemiKit.Button("Download", row,
                job != null && job.State == TufJobState.Failed ? HemiLang.Get("TUF_RETRY") : HemiLang.Get("TUF_DOWNLOAD"),
                busyElsewhere ? colors.Button : colors.Accent,
                busyElsewhere ? colors.Muted : Color.white,
                delegate
                {
                    if (TufDownloads.IsBusy)
                        return;

                    watched = TufDownloads.Start(level);
                    watchedState = watched == null ? TufJobState.Failed : watched.State;
                    HemiRoot.Instance?.Refresh();
                }, 12.5f, 7f);
            HemiKit.Size(download.gameObject, HemiTheme.Col(90f), HemiTheme.Row(24f));
        }

        private static void BuildPackRow(RectTransform content, TufPack pack)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiRows.Card(content, 52f);

            RectTransform column = HemiKit.VBox("Text", row, 0f, new RectOffset(0, 0, 0, 0), false);
            HemiKit.Size(column.gameObject, 300f, HemiTheme.Row(42f), 1f);

            TextMeshProUGUI name = HemiKit.Text("Name", column, pack.Name, 14f, colors.Text, true);
            HemiKit.ShowMarkupLiterally(name);
            HemiKit.Size(name.gameObject, -1f, HemiTheme.Row(22f), 1f);

            string detail = HemiLang.Get("TUF_PACK_LEVELS", pack.LevelCount);
            if (!string.IsNullOrEmpty(pack.Owner))
                detail += "  ·  " + pack.Owner;
            if (pack.Likes > 0)
                detail += "   ♥ " + pack.Likes;

            TextMeshProUGUI info = HemiKit.Text("Detail", column, detail, 12f, colors.Muted);
            HemiKit.ShowMarkupLiterally(info);
            HemiKit.Size(info.gameObject, -1f, HemiTheme.Row(18f), 1f);
        }

        private static void StartLevel(TufLevel level, RectTransform anchor)
        {
            string folder = TufLibrary.FolderOf(level.Id);
            List<string> charts = TufLauncher.ChartsIn(folder);

            if (charts.Count == 0)
            {
                officialNotice = HemiLang.Get("TUF_NO_CHART");
                HemiRoot.Instance?.Refresh();
                return;
            }

            if (charts.Count == 1)
            {
                Launch(charts[0]);
                return;
            }

            HemiColors colors = HemiTheme.Colors;
            HemiPopup.OpenList(anchor, HemiTheme.Col(320f),
                Mathf.Min(HemiTheme.Row(340f), HemiTheme.Row(56f + charts.Count * 38f)), body =>
            {
                HemiRows.NoteRow(body, HemiLang.Get("TUF_WHICH_CHART"));

                for (int i = 0; i < charts.Count; i++)
                {
                    string chart = charts[i];
                    Button pick = HemiKit.Button(
                        "Chart" + i, body, System.IO.Path.GetFileName(chart), colors.Card, colors.Text,
                        delegate
                        {
                            HemiPopup.Close();
                            Launch(chart);
                        }, 13f, 6f);
                    HemiKit.ShowMarkupLiterally(pick.GetComponentInChildren<TMP_Text>());
                    HemiKit.Size(pick.gameObject, -1f, HemiTheme.Row(32f), 1f);
                }
            });
        }

        private static void Launch(string chart)
        {
            bool opened = TufLauncher.Open(chart, out string reason);
            AfterOpen(opened, reason);
        }

        private static void AfterOpen(bool opened, string reason)
        {
            if (!opened)
            {
                officialNotice = reason;
                HemiRoot.Instance?.Refresh();
                return;
            }

            officialNotice = null;
            HemiRoot.Instance?.SetVisible(false);
        }

        private static Color ParseColor(string hex)
        {
            if (!string.IsNullOrEmpty(hex))
            {
                string value = hex.StartsWith("#", StringComparison.Ordinal) ? hex : "#" + hex;
                if (ColorUtility.TryParseHtmlString(value, out Color parsed))
                    return parsed;
            }
            return HemiTheme.Colors.Field;
        }

        private static void LoadMore()
        {
            if (installedOnly && mode == Mode.Levels)
                return;

            if (!hasMore || state == TufListState.Loading)
                return;

            offset += TufApi.PageSize;
            Search(false);
        }

        private static void Search(bool reset)
        {
            if (reset)
            {
                offset = 0;
                levels.Clear();
                packs.Clear();

                officialNotice = null;
            }

            state = TufListState.Loading;
            error = null;

            awaiting = mode == Mode.Levels
                ? TufApi.RequestLevels(query, sort, ascending, offset, filter)
                : TufApi.RequestPacks(query, packSort, ascending, offset);

            HemiRoot.Instance?.Refresh();
        }

        private static string BusyText(TufJob job)
        {
            if (job.State == TufJobState.Extracting)
                return HemiLang.Get("TUF_UNPACKING");

            return job.Progress >= 0f
                ? Mathf.RoundToInt(job.Progress * 100f).ToString(CultureInfo.InvariantCulture) + "%"
                : HemiLang.Get("TUF_DOWNLOADING");
        }

        private static bool FollowDownload()
        {
            if (watched == null)
                return false;

            if (watched.State != watchedState)
            {
                watchedState = watched.State;

                if (watched.State == TufJobState.Done)
                    TufLibrary.Rescan(true);

                if (watched.State == TufJobState.Done || watched.State == TufJobState.Failed)
                    watched = null;

                return true;
            }

            if (watched.State != TufJobState.Downloading || watched.Progress < 0f)
                return false;

            if (Time.unscaledTime < nextProgressRefresh)
                return false;

            nextProgressRefresh = Time.unscaledTime + 0.25f;

            if (busyLabel != null && ReferenceEquals(busyJob, watched))
            {
                busyLabel.text = BusyText(watched);
                if (busyFill != null)
                    busyFill.anchorMax = new Vector2(Mathf.Clamp01(watched.Progress), 1f);
            }

            return false;
        }

        internal static void Tick()
        {
            bool changed = FollowDownload();

            if (TufApi.TryTakeLevels(out TufPage page, out string levelError, out long token) && token == awaiting)
            {
                if (levelError != null)
                {
                    state = TufListState.Error;
                    error = levelError;
                }
                else
                {
                    levels.AddRange(page.Levels);
                    hasMore = page.HasMore;
                    state = levels.Count == 0 ? TufListState.Empty : TufListState.Ready;

                    TufLibrary.FillMissingDetails(page.Levels);
                }
                changed = true;
            }

            if (TufApi.TryTakePacks(out TufPackPage packPage, out string packError, out long packToken) && packToken == awaiting)
            {
                if (packError != null)
                {
                    state = TufListState.Error;
                    error = packError;
                }
                else
                {
                    packs.AddRange(packPage.Packs);
                    hasMore = packPage.HasMore;
                    state = packs.Count == 0 ? TufListState.Empty : TufListState.Ready;
                }
                changed = true;
            }

            if (changed)
                HemiRoot.Instance?.Refresh();
        }
    }
}
