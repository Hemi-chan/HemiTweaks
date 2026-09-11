using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiMainScreen
    {
        private const float LogoSize = 150f;

        internal static void Build(RectTransform parent)
        {
            HemiColors colors = HemiTheme.Colors;

            HemiOverlayEditor editor = HemiOverlayEditor.Create(parent);

            float titleTop = LogoSize + 6f;
            float hintTop = titleTop + HemiTheme.Row(44f);
            float buttonsTop = hintTop + HemiTheme.Row(34f);

            RectTransform column = HemiKit.Rect("Centre", parent);
            column.anchorMin = new Vector2(0.5f, 0.5f);
            column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(HemiTheme.Col(420f), buttonsTop + HemiTheme.Row(52f) + 34f);

            RectTransform logoHolder = HemiKit.Rect("LogoHolder", column);
            TopBand(logoHolder, LogoSize, LogoSize, 0f);
            HemiIcons.DrawLogo(logoHolder, LogoSize);

            TextMeshProUGUI title = HemiKit.Text("Title", column, BuildInfo.Name, 30f, colors.Text, true, TextAlignmentOptions.Center);
            TopBand(title.rectTransform, HemiTheme.Col(400f), HemiTheme.Row(44f), -titleTop);

            TextMeshProUGUI hint = HemiKit.Text(
                "Hint",
                column,
                editor.HandleCount > 0
                    ? HemiLang.Get("UI_MAIN_HINT_DRAG")
                    : HemiLang.Get("UI_MAIN_HINT_EMPTY"),
                13f,
                colors.Muted,
                false,
                TextAlignmentOptions.Center);
            TopBand(hint.rectTransform, HemiTheme.Col(420f), HemiTheme.Row(22f), -hintTop);

            RectTransform buttons = HemiKit.HBox("Buttons", column, HemiTheme.Gap, new RectOffset(0, 0, 0, 0));
            TopBand(buttons, HemiTheme.Col(360f), HemiTheme.Row(52f), -buttonsTop);

            Button features = HemiKit.Button(
                "Features",
                buttons,
                HemiLang.Get("UI_MAIN_FEATURES"),
                colors.Accent,
                Color.white,
                delegate { HemiRoot.Instance?.Go(HemiScreen.Features); },
                17f);
            HemiKit.Size(features.gameObject, HemiTheme.Col(180f), HemiTheme.Row(52f), 1f);

            Button preference = HemiKit.Button(
                "Preference",
                buttons,
                HemiLang.Get("UI_MAIN_PREFERENCE"),
                colors.Card,
                colors.Text,
                delegate { HemiRoot.Instance?.Go(HemiScreen.Preference); },
                17f);
            HemiKit.Size(preference.gameObject, HemiTheme.Col(150f), HemiTheme.Row(52f), 1f);

            Animate(column);
        }

        private static void TopBand(RectTransform rect, float width, float height, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void Animate(RectTransform column)
        {
            if (!HemiRoot.AnimationsEnabled)
                return;

            CanvasGroup group = column.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            HemiTween.To(column, "fade", 0f, 1f, 0.22f, HemiEase.OutQuad, value =>
            {
                if (group != null)
                    group.alpha = value;
            });

            Vector2 target = column.anchoredPosition;
            HemiTween.To(column, "rise", target.y - 16f, target.y, 0.40f, HemiEase.Spring, value =>
            {
                if (column != null)
                    column.anchoredPosition = new Vector2(target.x, value);
            });
        }
    }

    internal sealed class HemiFeature
    {
        public string Id;

        public string NameKey;
        public string SummaryKey;

        public string Name => HemiLang.Get(NameKey);
        public string Summary => HemiLang.Get(SummaryKey);

        public string Glyph;
        public Color Tint;

        public Func<bool> IsEnabled;
        public Action<bool> SetEnabled;

        public Action<RectTransform> BuildSettings;

        public bool CustomLayout;

        public bool Toggleable => IsEnabled != null && SetEnabled != null;

        public string[] Keywords = new string[0];
    }

    internal static class HemiFeatureRegistry
    {
        private static List<HemiFeature> features;

        internal static IReadOnlyList<HemiFeature> All
        {
            get
            {
                if (features == null)
                    features = Build();
                return features;
            }
        }

        internal static HemiFeature Find(string id)
        {
            IReadOnlyList<HemiFeature> all = All;
            for (int i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            }
            return null;
        }

        internal static bool Matches(HemiFeature feature, string query)
        {
            if (string.IsNullOrEmpty(query))
                return true;

            if (Contains(feature.Name, query) || Contains(feature.Id, query) || Contains(feature.Summary, query))
                return true;

            for (int i = 0; i < feature.Keywords.Length; i++)
            {
                if (Contains(feature.Keywords[i], query))
                    return true;
            }
            return false;
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<HemiFeature> Build()
        {
            return new List<HemiFeature>
            {
                new HemiFeature
                {
                    Id = "state",
                    NameKey = "FEATURE_STATE",
                    SummaryKey = "FEATURE_STATE_SUMMARY",
                    Glyph = "◆",
                    Tint = HemiTheme.Hex(0x6C63FF),
                    IsEnabled = () => StateOverlay.Enabled,
                    SetEnabled = StateOverlay.SetEnabled,
                    BuildSettings = HemiStatePage.Build,
                    Keywords = new[] { "overlay", "progress", "accuracy", "bpm", "kps", "fps", "group", "panel" }
                },
                new HemiFeature
                {
                    Id = "keyviewer",
                    NameKey = "FEATURE_KEYVIEWER",
                    SummaryKey = "FEATURE_KEYVIEWER_SUMMARY",
                    Glyph = "■",
                    Tint = HemiTheme.Hex(0x4AC8FF),
                    IsEnabled = () => KeyViewer.Enabled,
                    SetEnabled = KeyViewer.SetEnabled,
                    BuildSettings = HemiKeyViewerPage.Build,
                    CustomLayout = true,
                    Keywords = new[] { "overlay", "keys", "input", "rain", "note", "counter" }
                },
                new HemiFeature
                {
                    Id = "planetcolor",
                    NameKey = "FEATURE_PLANETCOLOR",
                    SummaryKey = "FEATURE_PLANETCOLOR_SUMMARY",
                    Glyph = "●",
                    Tint = HemiTheme.Hex(0xFF6B4A),
                    IsEnabled = () => HemiTweaksMod.EnablePlanetColorChanger,
                    SetEnabled = HemiTweaksMod.SetEnablePlanetColorChanger,
                    BuildSettings = HemiVisualPages.PlanetColor,
                    Keywords = new[] { "visual", "colour", "color", "planet", "ring", "tail" }
                },
                new HemiFeature
                {
                    Id = "tilecorner",
                    NameKey = "FEATURE_TILECORNER",
                    SummaryKey = "FEATURE_TILECORNER_SUMMARY",
                    Glyph = "◎",
                    Tint = HemiTheme.Hex(0xE0A83F),
                    IsEnabled = () => HemiTweaksMod.EnableTileCornerCurvature,
                    SetEnabled = HemiTweaksMod.SetEnableTileCornerCurvature,
                    BuildSettings = HemiVisualPages.TileCorner,
                    Keywords = new[] { "visual", "floor", "curve", "mesh" }
                },
                new HemiFeature
                {
                    Id = "buildname",
                    NameKey = "FEATURE_BUILDNAME",
                    SummaryKey = "FEATURE_BUILDNAME_SUMMARY",
                    Glyph = "T",
                    Tint = HemiTheme.Hex(0x9B8CFF),
                    IsEnabled = () => HemiTweaksMod.EnableChangeBuildName,
                    SetEnabled = HemiTweaksMod.SetEnableChangeBuildName,
                    BuildSettings = HemiVisualPages.BuildName,
                    Keywords = new[] { "utility", "text", "version" }
                },
                new HemiFeature
                {
                    Id = "tuf",
                    NameKey = "FEATURE_TUF",
                    SummaryKey = "FEATURE_TUF_SUMMARY",
                    Glyph = "▼",
                    Tint = HemiTheme.Hex(0x5B8CFF),
                    IsEnabled = () => HemiTweaksMod.TufEnabled,
                    SetEnabled = HemiTweaksMod.SetTuf,
                    BuildSettings = HemiVisualPages.Tuf,
                    Keywords = new[] { "level", "download", "browse", "forums", "search", "pack", "online" }
                },
                new HemiFeature
                {
                    Id = "tileinfo",
                    NameKey = "FEATURE_TILEINFO",
                    SummaryKey = "FEATURE_TILEINFO_SUMMARY",
                    Glyph = "◇",
                    Tint = HemiTheme.Hex(0xE0A83F),
                    IsEnabled = () => HemiTweaksMod.TileInfoEnabled,
                    SetEnabled = HemiTweaksMod.SetTileInfo,
                    BuildSettings = HemiVisualPages.TileInfo,
                    Keywords = new[] { "editor", "tile", "angle", "beat", "bpm", "select", "timing" }
                },
                new HemiFeature
                {
                    Id = "overloadbar",
                    NameKey = "FEATURE_OVERLOADBAR",
                    SummaryKey = "FEATURE_OVERLOADBAR_SUMMARY",
                    Glyph = "▮",
                    Tint = HemiTheme.Hex(0xE0574F),
                    IsEnabled = () => OverloadBarOverlay.Enabled,
                    SetEnabled = OverloadBarOverlay.SetEnabled,
                    BuildSettings = HemiVisualPages.OverloadBar,
                    Keywords = new[] { "overlay", "overload", "bar", "fail", "miss", "multipress" }
                },
                new HemiFeature
                {
                    Id = "progressbar",
                    NameKey = "FEATURE_PROGRESSBAR",
                    SummaryKey = "FEATURE_PROGRESSBAR_SUMMARY",
                    Glyph = "▬",
                    Tint = HemiTheme.Hex(0x4FC98A),
                    IsEnabled = () => ProgressBarOverlay.Enabled,
                    SetEnabled = ProgressBarOverlay.SetEnabled,
                    BuildSettings = HemiVisualPages.ProgressBar,
                    Keywords = new[] { "overlay", "progress", "bar", "song", "time", "tile" }
                },
                new HemiFeature
                {
                    Id = "combo",
                    NameKey = "FEATURE_COMBO",
                    SummaryKey = "FEATURE_COMBO_SUMMARY",
                    Glyph = "◈",
                    Tint = HemiTheme.Hex(0x59C2FF),
                    IsEnabled = () => ComboOverlay.Enabled,
                    SetEnabled = ComboOverlay.SetEnabled,
                    BuildSettings = HemiVisualPages.Combo,
                    Keywords = new[] { "overlay", "perfect", "streak", "chain", "judgement", "xperfect" }
                },
                new HemiFeature
                {
                    Id = "hideui",
                    NameKey = "FEATURE_HIDEUI",
                    SummaryKey = "FEATURE_HIDEUI_SUMMARY",
                    Glyph = "◐",
                    Tint = HemiTheme.Hex(0x7F8B9E),
                    IsEnabled = () => HemiTweaksMod.HideUiEnabled,
                    SetEnabled = HemiTweaksMod.SetHideUi,
                    BuildSettings = HemiVisualPages.HideUi,
                    Keywords = new[] { "visual", "hud", "title", "autoplay", "results", "error meter", "clean" }
                },
                new HemiFeature
                {
                    Id = "nonscroll",
                    NameKey = "FEATURE_NONSCROLL",
                    SummaryKey = "FEATURE_NONSCROLL_SUMMARY",
                    Glyph = "○",
                    Tint = HemiTheme.Hex(0x4FC28A),
                    IsEnabled = () => HemiTweaksMod.NonScrollEnabled,
                    SetEnabled = HemiTweaksMod.SetNonScroll,
                    Keywords = new[] { "utility", "camera", "zoom", "wheel", "editor", "scroll" }
                },
                new HemiFeature
                {
                    Id = "effectremover",
                    NameKey = "FEATURE_EFFECTREMOVER",
                    SummaryKey = "FEATURE_EFFECTREMOVER_SUMMARY",
                    Glyph = "▣",
                    Tint = HemiTheme.Hex(0x7A6FD8),
                    IsEnabled = () => HemiTweaksMod.EffectRemoverEnabled,
                    SetEnabled = HemiTweaksMod.SetEffectRemover,
                    BuildSettings = HemiVisualPages.EffectRemover,
                    Keywords = new[] { "visual", "effect", "decoration", "filter", "particle", "background", "clean", "lag", "performance" }
                },
                new HemiFeature
                {
                    Id = "nocheckpoint",
                    NameKey = "FEATURE_NOCHECKPOINT",
                    SummaryKey = "FEATURE_NOCHECKPOINT_SUMMARY",
                    Glyph = "▲",
                    Tint = HemiTheme.Hex(0xD98A3A),
                    IsEnabled = () => HemiTweaksMod.NoCheckpointEnabled,
                    SetEnabled = HemiTweaksMod.SetNoCheckpoint,
                    BuildSettings = HemiVisualPages.NoCheckpoint,
                    Keywords = new[] { "utility", "checkpoint", "practice", "restart", "run", "death" }
                },
                new HemiFeature
                {
                    Id = "autooffset",
                    NameKey = "FEATURE_AUTOOFFSET",
                    SummaryKey = "FEATURE_AUTOOFFSET_SUMMARY",
                    Glyph = "◑",
                    Tint = HemiTheme.Hex(0x5B8DEF),
                    IsEnabled = () => AutoOffset.Enabled,
                    SetEnabled = AutoOffset.SetEnabled,
                    BuildSettings = HemiVisualPages.AutoInputOffset,
                    Keywords = new[] { "utility", "offset", "calibration", "calibrate", "timing", "input", "latency", "delay", "hit", "judgement", "judgment" }
                },
                new HemiFeature
                {
                    Id = "unlocklimits",
                    NameKey = "FEATURE_UNLOCKLIMITS",
                    SummaryKey = "FEATURE_UNLOCKLIMITS_SUMMARY",
                    Glyph = "★",
                    Tint = HemiTheme.Hex(0xE05563),
                    IsEnabled = () => HemiTweaksMod.UnlockLimitsEnabled,
                    SetEnabled = HemiTweaksMod.SetUnlockLimits,
                    BuildSettings = HemiVisualPages.UnlockLimits,
                    Keywords = new[] { "utility", "editor", "speed", "value" }
                }
            };
        }
    }

    internal static class HemiFeaturesScreen
    {
        private static float SideWidth => HemiTheme.Col(200f);

        private static readonly Vector2 CardSize = new Vector2(250f, 158f);

        private static string search = "";
        private static string openFeatureId;

        private static string renaming;

        private static RectTransform gridRect;
        private static RectTransform gridContent;
        private static GameObject clearButton;
        private static TextMeshProUGUI emptyLabel;
        private static TMP_InputField searchField;

        internal static bool TryCloseSettings()
        {
            if (string.IsNullOrEmpty(openFeatureId))
                return false;

            openFeatureId = null;
            HemiStatePage.Reset();
            HemiKeyViewerPage.Reset();
            HemiRoot.Instance?.Refresh(HemiTransition.Back);
            return true;
        }

        internal static void FocusSearch()
        {
            if (searchField != null)
                searchField.ActivateInputField();
        }

        internal static void Build(RectTransform body)
        {
            if (!string.IsNullOrEmpty(openFeatureId))
            {
                BuildFeatureSettings(body);
                return;
            }

            BuildProfiles(body);

            RectTransform right = HemiKit.Rect("Right", body);
            right.anchorMin = Vector2.zero;
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(SideWidth, 0f);
            right.offsetMax = Vector2.zero;

            BuildSearch(right);
            BuildGrid(right);
        }

        private static void BuildProfiles(RectTransform body)
        {
            HemiColors colors = HemiTheme.Colors;

            Image side = HemiKit.Panel("Profiles", body, colors.Panel, 0f);
            RectTransform sideRect = side.rectTransform;
            Frame(sideRect, 0f, 0f, 0f, 1f, 0f, 0.5f);
            sideRect.sizeDelta = new Vector2(SideWidth, 0f);

            Image divider = HemiKit.Panel("Divider", body, colors.Line, 0f);
            divider.raycastTarget = false;
            RectTransform dividerRect = divider.rectTransform;
            Frame(dividerRect, 0f, 0f, 0f, 1f, 0f, 0.5f);
            dividerRect.anchoredPosition = new Vector2(SideWidth, 0f);
            dividerRect.sizeDelta = new Vector2(1f, 0f);

            string headingText = HemiLang.Get("UI_PROFILES");
            TextMeshProUGUI heading = HemiKit.Text(
                "Heading", sideRect, headingText, 11f, colors.Faint, true,
                TextAlignmentOptions.Left, true);
            heading.characterSpacing = 4f;
            heading.overflowMode = TextOverflowModes.Overflow;
            float headingWidth = Mathf.Max(1f, sideRect.rect.width - 28f);
            float headingHeight = HemiKit.PreferredTextHeight(
                heading, headingText, headingWidth, HemiTheme.Row(20f));
            const float headingTop = 12f;
            float listTop = headingTop + headingHeight + 6f;
            RectTransform headingRect = heading.rectTransform;
            Frame(headingRect, 0f, 1f, 1f, 1f, 0.5f, 1f);
            headingRect.offsetMin = new Vector2(14f, -headingTop - headingHeight);
            headingRect.offsetMax = new Vector2(-14f, -headingTop);

            float transferTop = 16f + HemiTheme.Row(24f);
            float addTop = transferTop + 8f + HemiTheme.Row(30f);
            string transferStatus = HemiProfileTransfer.LastStatus;
            TextMeshProUGUI statusText = null;
            float naturalStatusHeight = 0f;
            float visibleStatusHeight = 0f;
            if (!string.IsNullOrEmpty(transferStatus))
            {
                statusText = HemiKit.Text("TransferStatusText", sideRect, transferStatus, 11f, colors.Muted,
                    false, TextAlignmentOptions.Left, true);
                statusText.overflowMode = TextOverflowModes.Overflow;
                float statusWidth = Mathf.Max(1f, sideRect.rect.width - 28f);
                naturalStatusHeight = HemiKit.PreferredTextHeight(
                    statusText, transferStatus, statusWidth, HemiTheme.Row(34f));

                float maximumStatusHeight = naturalStatusHeight;
                if (sideRect.rect.height > 1f)
                {
                    maximumStatusHeight = Mathf.Max(
                        1f,
                        sideRect.rect.height - listTop - addTop - 14f - HemiTheme.Row(60f));
                }
                visibleStatusHeight = Mathf.Min(naturalStatusHeight, maximumStatusHeight);
            }
            float statusHeight = statusText == null ? 0f : visibleStatusHeight + 6f;

            RectTransform listArea = HemiKit.Rect("ListArea", sideRect);
            listArea.anchorMin = Vector2.zero;
            listArea.anchorMax = Vector2.one;
            listArea.offsetMin = new Vector2(10f, addTop + 8f + statusHeight);
            listArea.offsetMax = new Vector2(-10f, -listTop);

            RectTransform list = HemiKit.Scroll(listArea, out ScrollRect _);

            string active = HemiProfiles.Active;
            List<string> names = HemiProfiles.List();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                bool selected = string.Equals(name, active, StringComparison.OrdinalIgnoreCase);

                RectTransform row = HemiKit.HBox("Profile", list, 4f, new RectOffset(12, 8, 0, 0));
                HemiKit.Size(row.gameObject, -1f, HemiTheme.Row(30f), 1f);
                Image background = row.gameObject.AddComponent<Image>();
                background.color = selected ? colors.AccentSoft : new Color(0f, 0f, 0f, 0f);
                background.sprite = HemiSprites.Rounded(8);
                background.type = Image.Type.Sliced;

                Button select = row.gameObject.AddComponent<Button>();
                select.targetGraphic = background;
                select.transition = Selectable.Transition.None;
                select.onClick.AddListener(delegate
                {
                    HemiProfiles.Apply(name);
                    HemiRoot.Instance?.Refresh();
                });
                row.gameObject.AddComponent<HemiHover>().Initialize(background, background.color);

                if (string.Equals(renaming, name, StringComparison.Ordinal))
                {
                    BuildRenameField(row, name);
                    continue;
                }

                TextMeshProUGUI label = HemiKit.Text("Name", row, name, 13f, colors.Text, selected);
                HemiKit.Size(label.gameObject, 60f, HemiTheme.Row(24f), 1f);

                if (selected)
                {
                    RectTransform tick = HemiKit.Rect("Tick", row);
                    HemiKit.Size(tick.gameObject, HemiTheme.Col(14f), HemiTheme.Row(24f));
                    HemiIcons.DrawCheck(tick, Color.Lerp(colors.Accent, Color.white, 0.55f), HemiTheme.Row(11f));
                }

                Button rename = HemiKit.Button("Rename", row, "", new Color(0f, 0f, 0f, 0f), colors.Muted, delegate
                {
                    renaming = name;
                    HemiRoot.Instance?.Refresh();
                }, 12f, 6f);
                HemiKit.Size(rename.gameObject, HemiTheme.Col(22f), HemiTheme.Row(22f));
                DrawPencil(rename.transform, selected ? colors.Accent : colors.Faint, HemiTheme.Row(13f));

                if (selected)
                    continue;

                Button remove = HemiKit.Button("Remove", row, "", new Color(0f, 0f, 0f, 0f), colors.Muted, delegate
                {
                    HemiPopup.ConfirmDestructive(row,
                        HemiLang.Get("UI_DELETE_PROFILE_CONFIRM", name),
                        HemiLang.Get("UI_DELETE"),
                        delegate
                        {
                            HemiProfiles.Delete(name);
                            HemiRoot.Instance?.Refresh();
                        });
                }, 12f, 6f);
                HemiKit.Size(remove.gameObject, HemiTheme.Col(22f), HemiTheme.Row(22f));
                HemiIcons.DrawCross(remove.transform, colors.Faint, HemiTheme.Row(9f));
            }

            Button add = HemiKit.Button("Add", sideRect, "+ " + HemiLang.Get("UI_ADD_PROFILE"), colors.Accent, Color.white, delegate
            {
                HemiProfiles.Create("Profile");
                HemiRoot.Instance?.Refresh();
            }, 12.5f, 8f);
            RectTransform addRect = add.GetComponent<RectTransform>();
            Frame(addRect, 0f, 0f, 1f, 0f, 0.5f, 0f);
            addRect.offsetMin = new Vector2(12f, transferTop + 8f);
            addRect.offsetMax = new Vector2(-12f, addTop);

            RectTransform transfer = HemiKit.HBox("Transfer", sideRect, 8f, new RectOffset(0, 0, 0, 0));
            Frame(transfer, 0f, 0f, 1f, 0f, 0.5f, 0f);
            transfer.offsetMin = new Vector2(12f, 16f);
            transfer.offsetMax = new Vector2(-12f, transferTop);

            Button export = HemiKit.Button("Export", transfer, HemiLang.Get("UI_EXPORT_PROFILE"), colors.Button, colors.Text, delegate
            {
                HemiProfileTransfer.Export();
                HemiRoot.Instance?.Refresh();
            }, 12.5f, 8f);
            HemiKit.Size(export.gameObject, -1f, HemiTheme.Row(24f), 1f);

            Button import = HemiKit.Button("Import", transfer, HemiLang.Get("UI_IMPORT_PROFILE"), colors.Button, colors.Text, delegate
            {
                HemiProfileTransfer.Import();
                HemiRoot.Instance?.Refresh();
            }, 12.5f, 8f);
            HemiKit.Size(import.gameObject, -1f, HemiTheme.Row(24f), 1f);

            if (statusText != null)
            {
                RectTransform statusViewport = HemiKit.Rect("TransferStatus", sideRect);
                Frame(statusViewport, 0f, 0f, 1f, 0f, 0.5f, 0f);
                statusViewport.offsetMin = new Vector2(14f, addTop + 6f);
                statusViewport.offsetMax = new Vector2(-14f, addTop + 6f + visibleStatusHeight);
                statusViewport.gameObject.AddComponent<RectMask2D>();

                RectTransform statusRect = statusText.rectTransform;
                statusRect.SetParent(statusViewport, false);
                Frame(statusRect, 0f, 1f, 1f, 1f, 0.5f, 1f);
                statusRect.sizeDelta = new Vector2(0f, naturalStatusHeight);
                statusRect.anchoredPosition = Vector2.zero;

                if (naturalStatusHeight > visibleStatusHeight + 0.5f)
                    HemiKit.AttachScroll(statusViewport, statusRect);
            }
        }

        private static void BuildRenameField(RectTransform row, string name)
        {
            TMP_InputField field = HemiKit.Field(row, name, name, 120f);
            HemiKit.Size(field.gameObject, -1f, HemiTheme.Row(26f), 1f);

            bool submitted = false;
            field.onSubmit.AddListener(delegate { submitted = true; });
            field.onEndEdit.AddListener(delegate (string text)
            {
                renaming = null;
                if (submitted && !field.wasCanceled)
                    HemiProfiles.Rename(name, text);
                HemiRoot.Instance?.Refresh();
            });

            field.ActivateInputField();
        }

        private static void DrawPencil(Transform parent, Color tint, float size)
        {
            RectTransform root = HemiKit.Rect("Pencil", parent);
            Frame(root, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
            root.sizeDelta = new Vector2(size, size);
            root.localEulerAngles = new Vector3(0f, 0f, 45f);

            Image body = HemiKit.Panel("Body", root, tint, size * 0.12f);
            body.raycastTarget = false;
            RectTransform bodyRect = body.rectTransform;
            Frame(bodyRect, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(size * 0.28f, size * 0.62f);
            bodyRect.anchoredPosition = new Vector2(0f, size * 0.13f);

            Image tip = HemiKit.Panel("Tip", root, tint, 0f);
            tip.sprite = HemiSprites.Triangle();
            tip.type = Image.Type.Simple;
            tip.raycastTarget = false;
            RectTransform tipRect = tip.rectTransform;
            Frame(tipRect, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
            tipRect.sizeDelta = new Vector2(size * 0.28f, size * 0.26f);
            tipRect.anchoredPosition = new Vector2(0f, -size * 0.31f);
            tipRect.localEulerAngles = new Vector3(0f, 0f, 180f);
        }

        private static void BuildSearch(RectTransform parent)
        {
            HemiColors colors = HemiTheme.Colors;

            Image bar = HemiKit.Panel("SearchRow", parent, colors.Field, 9f);
            HemiKit.Border(bar.transform, colors.Line, 9f);
            RectTransform row = bar.rectTransform;
            Frame(row, 0f, 1f, 1f, 1f, 0.5f, 1f);
            row.offsetMin = new Vector2(22f, -HemiTheme.Row(28f) - 20f);
            row.offsetMax = new Vector2(-22f, -20f);

            TMP_InputField field = HemiKit.SearchBar(bar, search, HemiLang.Get("UI_SEARCH_FEATURES"), HemiTheme.Col(88f));
            searchField = field;

            field.onValueChanged.AddListener(delegate (string value)
            {
                search = value ?? "";
                RefreshGrid();
                if (clearButton != null)
                    clearButton.SetActive(!string.IsNullOrEmpty(search));
            });

            Image chip = HemiKit.Panel("ShortcutChip", row, new Color(0f, 0f, 0f, 0f), 5f);
            chip.raycastTarget = false;
            HemiKit.Border(chip.transform, colors.Border, 5f);
            RectTransform chipRect = chip.rectTransform;
            Frame(chipRect, 1f, 0.5f, 1f, 0.5f, 1f, 0.5f);
            chipRect.anchoredPosition = new Vector2(-8f, 0f);
            chipRect.sizeDelta = new Vector2(HemiTheme.Col(48f), HemiTheme.Row(20f));
            TextMeshProUGUI chipLabel = HemiKit.Text(
                "Label", chipRect, HemiLang.Get("UI_SEARCH_SHORTCUT"), 10.5f, colors.Faint, false, TextAlignmentOptions.Center);
            HemiKit.Stretch(chipLabel.rectTransform);

            Button clear = HemiKit.Button("Clear", row, "", new Color(0f, 0f, 0f, 0f), colors.Muted, delegate
            {
                search = "";
                HemiRoot.Instance?.Refresh();
            }, 12f, 6f);
            RectTransform clearRect = clear.GetComponent<RectTransform>();
            Frame(clearRect, 1f, 0.5f, 1f, 0.5f, 1f, 0.5f);
            clearRect.anchoredPosition = new Vector2(-8f - HemiTheme.Col(52f), 0f);
            clearRect.sizeDelta = new Vector2(20f, 20f);
            HemiIcons.DrawCross(clearRect, colors.Faint, 9f);
            clearButton = clear.gameObject;
            clearButton.SetActive(!string.IsNullOrEmpty(search));
        }

        private static void BuildGrid(RectTransform parent)
        {
            RectTransform area = HemiKit.Rect("GridArea", parent);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(22f, 20f);
            area.offsetMax = new Vector2(-22f, -HemiTheme.Row(28f) - 39f);

            gridContent = HemiKit.Scroll(area, out ScrollRect _);
            float desiredWidth = HemiTheme.Col(CardSize.x);
            float availableWidth = area.rect.width;
            if (availableWidth <= 1f)
                availableWidth = parent.rect.width - 44f;
            if (availableWidth <= 1f)
                availableWidth = HemiWindowChrome.Width - SideWidth - 44f;
            gridRect = HemiKit.Grid(
                "Grid", gridContent,
                new Vector2(Mathf.Min(desiredWidth, availableWidth), HemiTheme.Row(CardSize.y)),
                12f, new RectOffset(0, 0, 0, 0));
            emptyLabel = null;
            RefreshGrid();
        }

        private static void RefreshGrid()
        {
            if (gridRect == null || gridContent == null)
                return;

            for (int i = gridRect.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(gridRect.GetChild(i).gameObject);
            if (emptyLabel != null)
            {
                UnityEngine.Object.Destroy(emptyLabel.gameObject);
                emptyLabel = null;
            }

            GridLayoutGroup layout = gridRect.GetComponent<GridLayoutGroup>();
            float baseCardHeight = HemiTheme.Row(CardSize.y);
            float cardWidth = layout == null ? HemiTheme.Col(CardSize.x) : layout.cellSize.x;

            int shown = 0;
            float cardHeight = baseCardHeight;
            IReadOnlyList<HemiFeature> all = HemiFeatureRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (!HemiFeatureRegistry.Matches(all[i], search))
                    continue;
                cardHeight = Mathf.Max(cardHeight, BuildCard(gridRect, all[i], cardWidth, baseCardHeight));
                shown++;
            }

            if (layout != null)
                layout.cellSize = new Vector2(layout.cellSize.x, cardHeight);

            if (shown != 0)
                return;

            emptyLabel = HemiKit.Text(
                "Empty", gridContent, HemiLang.Get("UI_NO_FEATURE_MATCH", search), 14f,
                HemiTheme.Colors.Muted, false, TextAlignmentOptions.Center);
            HemiKit.Size(emptyLabel.gameObject, -1f, HemiTheme.Row(60f), 1f);
        }

        private static float BuildCard(RectTransform parent, HemiFeature feature, float cardWidth, float baseCardHeight)
        {
            HemiColors colors = HemiTheme.Colors;

            Image card = HemiKit.Panel("Card_" + feature.Id, parent, colors.Card, HemiTheme.CardRadius);
            RectTransform cardRect = card.rectTransform;
            HemiKit.Border(cardRect, colors.Border, HemiTheme.CardRadius);
            card.gameObject.AddComponent<HemiHover>().Initialize(card, colors.Card);

            Button open = card.gameObject.AddComponent<Button>();
            open.targetGraphic = card;
            open.transition = Selectable.Transition.None;
            open.onClick.AddListener(delegate
            {
                openFeatureId = feature.Id;
                HemiRoot.Instance?.Refresh(HemiTransition.Forward);
            });

            float insetX = HemiTheme.Col(14f);

            RectTransform icon = HemiKit.Rect("Icon", cardRect);
            Frame(icon, 0f, 1f, 0f, 1f, 0f, 1f);
            icon.anchoredPosition = new Vector2(insetX, -HemiTheme.Row(14f));
            icon.sizeDelta = new Vector2(HemiTheme.Row(42f), HemiTheme.Row(42f));
            HemiIcons.Draw(icon, feature.Glyph, feature.Tint, 42f);

            float textWidth = Mathf.Max(1f, cardWidth - insetX * 2f);
            float nameTop = HemiTheme.Row(64f);
            float nameHeight = TopText(cardRect, "Name", feature.Name, 14.5f, colors.Text, true, insetX, nameTop, textWidth, HemiTheme.Row(20f));
            float summaryTop = nameTop + nameHeight + HemiTheme.Row(2f);
            float summaryHeight = TopText(cardRect, "Summary", feature.Summary, 11.5f, colors.Muted, false, insetX, summaryTop, textWidth, HemiTheme.Row(34f));

            float requiredHeight = Mathf.Max(
                baseCardHeight,
                summaryTop + summaryHeight + HemiTheme.Row(48f));

            if (!feature.Toggleable)
                return requiredHeight;

            TextMeshProUGUI state = HemiKit.Text(
                "State", cardRect,
                feature.IsEnabled() ? HemiLang.Get("UI_ENABLED") : HemiLang.Get("UI_DISABLED"),
                11.5f, colors.Faint);
            RectTransform stateRect = state.rectTransform;
            Frame(stateRect, 0f, 0f, 0f, 0f, 0f, 0f);
            stateRect.anchoredPosition = new Vector2(insetX, HemiTheme.Row(15f));
            stateRect.sizeDelta = new Vector2(HemiTheme.Col(80f), HemiTheme.Row(16f));

            RectTransform switchHolder = HemiKit.Rect("SwitchHolder", cardRect);
            Frame(switchHolder, 1f, 0f, 1f, 0f, 1f, 0f);
            switchHolder.anchoredPosition = new Vector2(-insetX, HemiTheme.Row(12f));
            switchHolder.sizeDelta = new Vector2(HemiTheme.Col(54f), HemiTheme.Row(24f));
            RectTransform sw = HemiKit.Switch(switchHolder, feature.IsEnabled, value => ApplyFeatureSwitch(feature, value, state));
            Frame(sw, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
            sw.anchoredPosition = Vector2.zero;
            sw.sizeDelta = new Vector2(HemiTheme.Col(54f), HemiTheme.Row(24f));
            return requiredHeight;
        }

        private static void Frame(RectTransform rect, float minX, float minY, float maxX, float maxY, float pivotX, float pivotY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(pivotX, pivotY);
        }

        private static float TopText(RectTransform parent, string objectName, string value, float size, Color color, bool bold, float inset, float top, float width, float minimum)
        {
            TextMeshProUGUI label = HemiKit.Text(objectName, parent, value, size, color, bold, TextAlignmentOptions.TopLeft, true);
            label.overflowMode = TextOverflowModes.Overflow;
            float height = HemiKit.PreferredTextHeight(label, value, width, minimum);
            RectTransform rect = label.rectTransform;
            Frame(rect, 0f, 1f, 1f, 1f, 0.5f, 1f);
            rect.offsetMin = new Vector2(inset, -top - height);
            rect.offsetMax = new Vector2(-inset, -top);
            return height;
        }

        private static void ApplyFeatureSwitch(HemiFeature feature, bool value, TextMeshProUGUI state)
        {
            feature.SetEnabled(value);

            if (string.Equals(feature.Id, "tuf", StringComparison.OrdinalIgnoreCase))
            {
                HemiRoot.Instance?.RefreshShell();
                return;
            }

            if (state != null)
                state.text = value ? HemiLang.Get("UI_ENABLED") : HemiLang.Get("UI_DISABLED");
        }

        private static void BuildFeatureSettings(RectTransform parent)
        {
            HemiColors colors = HemiTheme.Colors;
            HemiFeature feature = HemiFeatureRegistry.Find(openFeatureId);
            if (feature == null)
            {
                openFeatureId = null;
                return;
            }

            RectTransform header = HemiKit.HBox("SettingsHeader", parent, HemiTheme.Gap, new RectOffset(0, 0, 0, 0));
            Frame(header, 0f, 1f, 1f, 1f, 0.5f, 1f);
            header.offsetMin = new Vector2(22f, -HemiTheme.Row(30f) - 16f);
            header.offsetMax = new Vector2(-22f, -16f);

            Button back = HemiKit.Button("Back", header, HemiLang.Get("UI_BACK_FEATURES"), colors.Card, colors.Accent, delegate
            {
                TryCloseSettings();
            }, 13f, 9f);
            HemiKit.Size(back.gameObject, HemiTheme.Col(84f), HemiTheme.Row(28f));

            RectTransform titleIcon = HemiKit.Rect("TitleIcon", header);
            HemiKit.Size(titleIcon.gameObject, HemiTheme.Row(30f), HemiTheme.Row(30f));
            HemiIcons.Draw(titleIcon, feature.Glyph, feature.Tint, 30f);

            TextMeshProUGUI title = HemiKit.Text("Title", header, feature.Name, 17f, colors.Text, true);
            HemiKit.Size(title.gameObject, 160f, HemiTheme.Row(30f), 1f);

            if (feature.Toggleable)
                HemiKit.Switch(header, feature.IsEnabled, value => ApplyFeatureSwitch(feature, value, null));

            RectTransform area = HemiKit.Rect("SettingsArea", parent);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(22f, 16f);
            area.offsetMax = new Vector2(-22f, -HemiTheme.Row(30f) - 28f);

            if (feature.BuildSettings != null && feature.CustomLayout)
            {
                feature.BuildSettings(area);
                return;
            }

            RectTransform content = HemiKit.Scroll(area, out ScrollRect _);

            if (feature.BuildSettings != null)
            {
                feature.BuildSettings(content);
                return;
            }

            TextMeshProUGUI note = HemiKit.Text(
                "Note",
                content,
                HemiLang.Get("UI_SETTINGS_PENDING", feature.Summary),
                14f,
                colors.Muted,
                false,
                TextAlignmentOptions.TopLeft,
                true);
            HemiKit.Size(note.gameObject, -1f, HemiTheme.Row(90f), 1f);
        }
    }

    internal static class HemiPreferenceScreen
    {
        private static string[] ThemeOptions =>
            new string[] { HemiLang.Get("PREF_THEME_DARK"), HemiLang.Get("PREF_THEME_LIGHT") };

        private static void BuildLanguageRow(RectTransform content)
        {
            IReadOnlyList<string> codes = HemiLang.Codes;

            List<string> labels = new List<string> { HemiLang.Get("LANGUAGE_AUTO") };
            List<string> values = new List<string> { "" };

            for (int i = 0; i < codes.Count; i++)
            {
                labels.Add(HemiLang.NameOf(codes[i]));
                values.Add(codes[i]);
            }

            int selected = values.IndexOf(HemiTweaksMod.LanguageSetting);
            if (selected < 0)
                selected = 0;

            HemiRows.DropdownRow(content, HemiLang.Get("LANGUAGE"), labels, selected, delegate (int index)
            {
                if (index >= 0 && index < values.Count)
                    HemiTweaksMod.SetLanguage(values[index]);
            });

            HemiRows.ActionRow(content, HemiLang.Get("LANGUAGE"), "", HemiLang.Get("LANGUAGE_RELOAD"), delegate
            {
                HemiLang.Reload();
                HemiRoot.Instance?.RefreshShell();
            }, HemiLang.Get("DESC_LANGUAGE"));
        }

        internal static void Build(RectTransform body)
        {
            RectTransform area = HemiKit.Rect("Area", body);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(24f, 18f);
            area.offsetMax = new Vector2(-24f, -18f);

            RectTransform content = HemiKit.Scroll(area, out ScrollRect _);

            HemiRows.Heading(content, HemiLang.Get("PREF_SECTION_INTERFACE"));

            BuildLanguageRow(content);

            HemiRows.DropdownRow(content, HemiLang.Get("PREF_THEME"), ThemeOptions, HemiTweaksMod.IsDarkMode ? 0 : 1, delegate (int index)
            {
                HemiTweaksMod.SetDarkMode(index == 0);
                HemiRoot.Instance?.Refresh();
            });

            HemiRows.SliderRow(content, HemiLang.Get("PREF_FONT_SIZE"), HemiTweaksMod.UIFontSize, 12f, 40f, true, delegate (float value)
            {
                HemiTweaksMod.SetUIFontSize(value);
                HemiRoot.Instance?.Refresh();
            }, HemiLang.Get("DESC_PREF_FONT_SIZE"), 2, null, false);

            HemiRows.ToggleRow(content, HemiLang.Get("PREF_TRANSITION_ANIMATIONS"), HemiLang.Get("DESC_PREF_TRANSITION_ANIMATIONS"),
                () => HemiTweaksMod.TransitionAnimationsEnabled,
                HemiTweaksMod.SetTransitionAnimations);

            HemiRows.ToggleRow(content, HemiLang.Get("PREF_SMOOTH_SCROLLING"), HemiLang.Get("DESC_PREF_SMOOTH_SCROLLING"),
                () => HemiTweaksMod.SmoothScrollingEnabled,
                HemiTweaksMod.SetSmoothScrolling);

            HemiRows.Heading(content, HemiLang.Get("PREF_SECTION_HOTKEY"));

            HemiRows.KeyRow(
                content,
                HemiLang.Get("PREF_OPEN_INTERFACE"),
                HemiTweaksMod.ToggleSettingsKey,
                HemiTweaksMod.IsWaitingForHotkey,
                delegate
                {
                    if (HemiTweaksMod.IsWaitingForHotkey)
                        HemiTweaksMod.CancelHotkeyCapture();
                    else
                        HemiTweaksMod.StartHotkeyCapture();
                    HemiRoot.Instance?.Refresh();
                },
                HemiLang.Get("DESC_PREF_OPEN_INTERFACE"));

            HemiRows.Heading(content, HemiLang.Get("PREF_SECTION_PROFILES"));

            HemiRows.ActionRow(content, HemiLang.Get("PREF_ACTIVE_PROFILE"), HemiProfiles.Active, HemiLang.Get("PREF_SAVE"), delegate
            {
                HemiProfiles.Save(HemiProfiles.Active);
                HemiRoot.Instance?.Refresh();
            }, HemiLang.Get("DESC_PREF_ACTIVE_PROFILE"));

            HemiRows.NoteRow(
                content,
                HemiLang.Get("PREF_PROFILE_NOTE"));

            BuildUpdateSection(content);
        }

        private static void BuildUpdateSection(RectTransform content)
        {
            HemiRows.Heading(content, HemiLang.Get("PREF_SECTION_UPDATE"));

            HemiRows.ActionRow(content, HemiLang.Get("PREF_CURRENT_VERSION"), HemiUpdater.CurrentVersion,
                HemiLang.Get("UPD_CHECK"), delegate
                {
                    HemiUpdater.Check();
                    HemiRoot.Instance?.Refresh();
                });

            string status = UpdateStatusText();
            if (!string.IsNullOrEmpty(status))
                HemiRows.NoteRow(content, status);

            if (HemiUpdater.UpdateAvailable)
            {
                HemiRows.ActionRow(content, HemiLang.Get("PREF_LATEST_VERSION"), HemiUpdater.LatestVersion,
                    HemiLang.Get("UPD_UPDATE"), delegate
                    {
                        HemiUpdater.Install();
                        HemiRoot.Instance?.Refresh();
                    }, null, HemiTheme.Colors.Accent);
            }

            HemiRows.ToggleRow(content, HemiLang.Get("PREF_UPDATE_ON_STARTUP"), HemiLang.Get("DESC_PREF_UPDATE_ON_STARTUP"),
                () => HemiUpdater.CheckOnStartup,
                HemiUpdater.SetCheckOnStartup);

            HemiRows.TextRow(content, HemiLang.Get("PREF_UPDATE_TOKEN"), HemiUpdater.Token, "ghp_...",
                HemiUpdater.SetToken, HemiLang.Get("DESC_PREF_UPDATE_TOKEN"), secret: true);
        }

        private static string UpdateStatusText() => HemiUpdater.State switch
        {
            HemiUpdateState.Checking => HemiLang.Get("UPD_STATUS_CHECKING"),
            HemiUpdateState.UpToDate => HemiLang.Get("UPD_STATUS_UP_TO_DATE"),
            HemiUpdateState.Available => HemiLang.Get("UPD_STATUS_AVAILABLE", HemiUpdater.LatestVersion),
            HemiUpdateState.CheckFailed => HemiLang.Get("UPD_STATUS_FAILED", HemiUpdater.Error),
            HemiUpdateState.Downloading => HemiLang.Get("UPD_STATUS_DOWNLOADING"),
            HemiUpdateState.Installed => string.IsNullOrEmpty(HemiUpdater.Error)
                ? HemiLang.Get("UPD_STATUS_INSTALLED")
                : HemiLang.Get("UPD_STATUS_RESTART_FAILED", HemiUpdater.Error),
            HemiUpdateState.InstallFailed => HemiLang.Get("UPD_STATUS_INSTALL_FAILED", HemiUpdater.Error),
            _ => null
        };
    }

    internal sealed class HemiSliderCommit : MonoBehaviour, UnityEngine.EventSystems.IPointerUpHandler
    {
        private Slider slider;
        private Action<float> setter;

        internal void Initialize(Slider value, Action<float> commit)
        {
            slider = value;
            setter = commit;
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (slider != null && setter != null)
                setter(slider.value);
        }
    }
}
