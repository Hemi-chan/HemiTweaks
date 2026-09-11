using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal sealed class HemiKeyViewerEditor : MonoBehaviour
    {
        internal enum KeyTab
        {
            Key,
            Note,
            Counter
        }

        private const float PanelWidth = 330f;
        private const float ToolbarHeight = 44f;

        private const float PositionSliderReach = 1200f;

        private static string[] ImageFitOptions => new[]
        {
            HemiLang.Get("KVE_FIT_COVER"), HemiLang.Get("KVE_FIT_CONTAIN"),
            HemiLang.Get("KVE_FIT_FILL"), HemiLang.Get("KVE_FIT_NONE")
        };

        private static string[] NoteAlignOptions => new[]
        {
            HemiLang.Get("KVE_ALIGN_LEFT"), HemiLang.Get("KVE_ALIGN_CENTER"), HemiLang.Get("KVE_ALIGN_RIGHT")
        };

        private static string[] StatOptions =>
            new[]
            {
                HemiLang.Get("KVE_STAT_KPS"), HemiLang.Get("KVE_STAT_AVG"),
                HemiLang.Get("KVE_STAT_MAX"), HemiLang.Get("KVE_STAT_TOTAL")
            };

        private static string[] GraphTypeOptions =>
            new[] { HemiLang.Get("KVE_GRAPH_LINE"), HemiLang.Get("KVE_GRAPH_BAR") };

        private static string[] KeyMatchOptions =>
            new[] { HemiLang.Get("KVE_MATCH_ANY"), HemiLang.Get("KVE_MATCH_ALL") };

        private static string[] BorderSideOptions => new[]
        {
            HemiLang.Get("KVE_SIDE_ALL"), HemiLang.Get("SET_ORIENT_VERTICAL"), HemiLang.Get("SET_ORIENT_HORIZONTAL")
        };

        private static string[] PlacementOptions =>
            new[] { HemiLang.Get("KVE_PLACE_INSIDE"), HemiLang.Get("KVE_PLACE_OUTSIDE") };

        private static string[] CounterAlignOptions => new[]
        {
            HemiLang.Get("KVE_ALIGN_TOP"), HemiLang.Get("KVE_ALIGN_BOTTOM"),
            HemiLang.Get("KVE_ALIGN_LEFT"), HemiLang.Get("KVE_ALIGN_RIGHT")
        };

        private static string[] AlignModeOptions =>
            new[] { HemiLang.Get("KVE_ALIGNMODE_CENTRED"), HemiLang.Get("KVE_ALIGNMODE_SPREAD") };

        private static string[] ImageExtensions => GCS.SupportedImageFiles;
        private static readonly string[] FontExtensions = { "ttf", "otf" };

        private static string[] AnchorOptions => StateGroupLayout.AnchorOptions();

        private RectTransform panelHost;
        private HemiKeyCanvas canvas;
        private TextMeshProUGUI zoomLabel;
        private bool framePending;
        private Button addButton;
        private TextMeshProUGUI addLabel;
        private Image undoIcon;
        private Image redoIcon;
        private KeyTab tab = KeyTab.Key;

        private int geometryIndex = -1;
        private HemiSliderHandle xSlider;
        private HemiSliderHandle ySlider;
        private HemiSliderHandle widthSlider;
        private HemiSliderHandle heightSlider;
        private Vector2 shownPosition;
        private Vector2 shownSize;
        private int shownZoomPercent = int.MinValue;

        internal static HemiKeyViewerEditor Create(RectTransform area)
        {
            GameObject host = HemiKit.Obj("KeyViewerEditor", area);
            RectTransform rect = host.GetComponent<RectTransform>();
            HemiKit.Stretch(rect);

            HemiKeyViewerEditor editor = host.AddComponent<HemiKeyViewerEditor>();
            editor.Build(rect);
            return editor;
        }

        private void Build(RectTransform root)
        {
            RectTransform left = HemiKit.Rect("Left", root);
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(1f, 1f);
            left.offsetMin = Vector2.zero;
            left.offsetMax = new Vector2(-PanelWidth - HemiTheme.Gap, 0f);

            RectTransform canvasArea = HemiKit.Rect("CanvasArea", left);
            canvasArea.anchorMin = Vector2.zero;
            canvasArea.anchorMax = Vector2.one;
            canvasArea.offsetMin = new Vector2(0f, HemiTheme.Row(ToolbarHeight) + HemiTheme.Gap);
            canvasArea.offsetMax = Vector2.zero;

            canvas = HemiKeyCanvas.Create(canvasArea, OnSelectionChanged);

            BuildToolbar(left);

            panelHost = HemiKit.Rect("Panel", root);
            panelHost.anchorMin = new Vector2(1f, 0f);
            panelHost.anchorMax = new Vector2(1f, 1f);
            panelHost.pivot = new Vector2(1f, 0.5f);
            panelHost.sizeDelta = new Vector2(PanelWidth, 0f);
            panelHost.anchoredPosition = Vector2.zero;

            BuildPanelContents();
        }

        private void BuildToolbar(RectTransform parent)
        {
            HemiColors colors = HemiTheme.Colors;

            RectTransform bar = HemiKit.HBox("Toolbar", parent, HemiTheme.Gap, new RectOffset(10, 10, 0, 0));
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, HemiTheme.Row(ToolbarHeight));

            Image background = bar.gameObject.AddComponent<Image>();
            background.color = colors.Panel;
            background.sprite = HemiSprites.Rounded(10);
            background.type = Image.Type.Sliced;

            Button add = HemiKit.Button(
                "Add",
                bar,
                KeyViewer.IsAddingKey ? HemiLang.Get("KVE_PRESS_KEY") : HemiLang.Get("KVE_ADD_KEY"),
                colors.Accent,
                Color.white,
                delegate
                {
                    if (KeyViewer.IsAddingKey)
                        KeyViewer.StopRegistration();
                    else
                        KeyViewer.BeginAddingKey();
                    RebuildAll();
                },
                13f,
                8f);
            HemiKit.Size(add.gameObject, 150f, HemiTheme.Row(30f));

            addButton = add;
            addLabel = add.GetComponentInChildren<TextMeshProUGUI>();

            AddElementButton(bar, "AddStat", HemiLang.Get("KVE_ADD_STAT"), 86f, () => KeyViewer.AddStat(KeyViewerStat.Kps));
            AddElementButton(bar, "AddGraph", HemiLang.Get("KVE_ADD_GRAPH"), 76f, () => KeyViewer.AddGraph(KeyViewerStat.Kps));
            AddElementButton(bar, "AddKnob", HemiLang.Get("KVE_ADD_KNOB"), 72f, KeyViewer.AddKnob);

            Button remove = HemiKit.Button("Delete", bar, HemiLang.Get("KVE_DELETE"), colors.Field, colors.Negative, delegate
            {
                if (canvas.Selection.Count == 0)
                    return;
                KeyViewer.RemoveKeys(new List<int>(canvas.Selection));
                canvas.Select(-1);
                RebuildAll();
            }, 13f, 8f);
            HemiKit.Size(remove.gameObject, 84f, HemiTheme.Row(30f));

            ToolButton(bar, "Front", "▲", 34f, delegate { Reorder(1); });

            ToolButton(bar, "Back", "▼", 34f, delegate { Reorder(-1); });

            undoIcon = HistoryButton(bar, "Undo", false, delegate
            {
                if (KeyViewerHistory.Undo())
                    AfterHistory();
            });

            redoIcon = HistoryButton(bar, "Redo", true, delegate
            {
                if (KeyViewerHistory.Redo())
                    AfterHistory();
            });

            ToolButton(bar, "Reset", HemiLang.Get("KVE_RESET_LAYOUT"), 116f, delegate
            {
                KeyViewer.ResetLayout();
                RebuildAll();
            });

            RectTransform spacer = HemiKit.Rect("Spacer", bar);
            HemiKit.Size(spacer.gameObject, 10f, 10f, 1f);

            ToolButton(bar, "ZoomOut", "-", 32f, delegate { StepZoom(-0.1f); }, 16f);

            zoomLabel = HemiKit.Text("Zoom", bar, "", 13f, colors.Text, true, TextAlignmentOptions.Center);
            HemiKit.Size(zoomLabel.gameObject, 56f, HemiTheme.Row(30f));

            ToolButton(bar, "ZoomIn", "+", 32f, delegate { StepZoom(0.1f); }, 16f);

            ToolButton(bar, "Fit", HemiLang.Get("KVE_FIT"), 54f, delegate { canvas.ResetView(); });
            framePending = true;
        }

        private void AddElementButton(RectTransform bar, string name, string label, float width, Func<int> add)
        {
            HemiColors colors = HemiTheme.Colors;
            Button button = HemiKit.Button(name, bar, label, colors.Field, colors.Text, delegate
            {
                int index = add();
                if (index < 0)
                    return;
                RebuildAll();
                canvas.Select(index);
            }, 13f, 8f);
            HemiKit.Size(button.gameObject, width, HemiTheme.Row(30f));
        }

        private static void ToolButton(RectTransform bar, string name, string label, float width, Action action, float fontSize = 13f)
        {
            HemiColors colors = HemiTheme.Colors;
            Button button = HemiKit.Button(name, bar, label, colors.Field, colors.Text, action, fontSize, 8f);
            HemiKit.Size(button.gameObject, width, HemiTheme.Row(30f));
        }

        private static Image HistoryButton(RectTransform bar, string name, bool mirrored, Action action)
        {
            HemiColors colors = HemiTheme.Colors;

            Button button = HemiKit.Button(name, bar, "", colors.Field, colors.Text, action, 13f, 0f);
            HemiKit.Size(button.gameObject, 40f, HemiTheme.Row(30f));

            Image icon = HemiKit.Panel("Icon", button.GetComponent<RectTransform>(), colors.Text, 0f);
            icon.sprite = HemiSprites.CircularArrow();
            icon.raycastTarget = false;

            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(19f, 19f);
            rect.anchoredPosition = Vector2.zero;
            if (mirrored)
                rect.localScale = new Vector3(-1f, 1f, 1f);

            return icon;
        }

        private void StepZoom(float delta)
        {
            canvas.SetZoom(canvas.Zoom + delta);
        }

        private List<int> Selection()
        {
            return canvas == null ? new List<int>() : new List<int>(canvas.Selection);
        }

        private void AfterHistory()
        {
            canvas.Select(-1);
            RebuildAll();
        }

        private void Reorder(int delta)
        {
            if (canvas == null || canvas.Selection.Count == 0)
                return;

            List<int> indices = new List<int>(canvas.Selection);
            if (!KeyViewer.ReorderKeys(indices, delta))
                return;

            canvas.SelectMany(indices);
            RebuildAll();
        }

        private void Update()
        {
            if (zoomLabel == null || canvas == null)
                return;

            if (HemiTweaksMod.IsInterfaceOpen)
            {
                int percent = Mathf.RoundToInt(canvas.Scale * 100f);
                if (percent != shownZoomPercent)
                {
                    shownZoomPercent = percent;
                    zoomLabel.text = percent.ToString(CultureInfo.InvariantCulture) + "%";
                }

                if (framePending && canvas.HasArea)
                {
                    framePending = false;
                    canvas.ResetView();
                }

                Dim(undoIcon, KeyViewerHistory.CanUndo);
                Dim(redoIcon, KeyViewerHistory.CanRedo);
                FollowRegistration();

                FollowGeometry();
            }

            PumpHistoryShortcuts();
        }

        private void FollowRegistration()
        {
            if (addLabel == null)
                return;

            bool waiting = KeyViewer.IsAddingKey;
            string text = waiting ? HemiLang.Get("KVE_PRESS_ANY_KEY") : HemiLang.Get("KVE_ADD_KEY");
            if (addLabel.text != text)
                addLabel.text = text;

            Color tint = waiting ? HemiTheme.Colors.Positive : HemiTheme.Colors.Accent;
            if (addButton != null && addButton.targetGraphic != null && addButton.targetGraphic.color != tint)
                addButton.targetGraphic.color = tint;
        }

        private void FollowGeometry()
        {
            if (geometryIndex < 0)
                return;

            KeyViewerKeyConfig key = KeyViewer.GetKey(geometryIndex);
            if (key == null)
                return;

            if (key.Position != shownPosition)
            {
                shownPosition = key.Position;
                xSlider.SetValue(key.Position.x);
                ySlider.SetValue(key.Position.y);
            }

            if (key.Size != shownSize)
            {
                shownSize = key.Size;
                widthSlider.SetValue(key.Size.x);
                heightSlider.SetValue(key.Size.y);
            }
        }

        private static void Dim(Image icon, bool available)
        {
            if (icon == null)
                return;
            Color color = available ? HemiTheme.Colors.Text : HemiTheme.Colors.Muted;
            if (icon.color != color)
                icon.color = color;
        }

        private void PumpHistoryShortcuts()
        {
            if (!HemiModifiers.Command)
                return;

            if (KeyViewer.IsRegistering)
                return;

            GameObject focused = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (focused != null && focused.GetComponent<TMP_InputField>() != null)
                return;

            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (KeyViewerHistory.Undo())
                    AfterHistory();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                if (KeyViewerHistory.Redo())
                    AfterHistory();
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                KeyViewer.CopyKeys(canvas.Selection);
                return;
            }

            if (Input.GetKeyDown(KeyCode.V))
                Paste();
        }

        private void Paste()
        {
            List<int> added = KeyViewer.PasteKeys();
            if (added.Count == 0)
                return;

            canvas.Rebuild();
            canvas.SelectMany(added);
        }

        private void OnSelectionChanged(int index)
        {
            RebuildPanel(false);
        }

        internal void RebuildAll()
        {
            canvas.Rebuild();
            RebuildPanel();
        }

        internal void RebuildPanel(bool preserveScroll = true)
        {
            if (panelHost == null)
                return;

            if (preserveScroll)
                HemiScrollTop.Carry(panelHost.gameObject);
            else
                HemiScrollTop.DropCarry();

            try
            {
                BuildPanelContents();
            }
            finally
            {
                HemiScrollTop.DropCarry();
            }
        }

        private void BuildPanelContents()
        {
            if (panelHost == null)
                return;

            KeyViewerHistory.Break();

            geometryIndex = -1;

            for (int i = panelHost.childCount - 1; i >= 0; i--)
            {
                GameObject child = panelHost.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            HemiColors colors = HemiTheme.Colors;
            Image panel = HemiKit.Panel("Surface", panelHost, colors.Panel, 10f);
            HemiKit.Stretch(panel.rectTransform);

            int index = canvas == null ? -1 : canvas.Selected;
            KeyViewerKeyConfig key = KeyViewer.GetKey(index);

            int count = canvas == null ? 0 : canvas.Selection.Count;
            string heading = key == null
                ? HemiLang.Get("KVE_SHARED_SETTINGS")
                : count > 1
                    ? HemiLang.Get("KVE_KEYS_SELECTED", count.ToString(CultureInfo.InvariantCulture))
                    : key.ResolveLabel();

            float panelWidth = panelHost.rect.width > 1f ? panelHost.rect.width : PanelWidth;
            float titleWidth = Mathf.Max(1f, panelWidth - 26f);
            TextMeshProUGUI title = HemiKit.Text(
                "Title", panel.rectTransform, heading, 16f, colors.Text, true,
                TextAlignmentOptions.Left, true);
            title.overflowMode = TextOverflowModes.Overflow;
            float titleHeight = HemiKit.PreferredTextHeight(title, heading, titleWidth, HemiTheme.Row(30f));
            float headerHeight = Mathf.Max(HemiTheme.Row(44f), titleHeight + HemiTheme.Row(8f));

            RectTransform header = HemiKit.HBox(
                "Header", panel.rectTransform, HemiTheme.Gap, new RectOffset(14, 12, 0, 0));
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -headerHeight);
            header.offsetMax = Vector2.zero;
            title.transform.SetParent(header, false);
            HemiKit.Size(title.gameObject, -1f, titleHeight, 1f);

            float top = headerHeight;

            if (key != null)
            {
                float tabsBaseHeight = HemiTheme.Row(40f);
                RectTransform tabs = HemiKit.Rect("Tabs", panel.rectTransform);
                tabs.anchorMin = new Vector2(0f, 1f);
                tabs.anchorMax = new Vector2(1f, 1f);
                tabs.pivot = new Vector2(0.5f, 1f);
                tabs.offsetMin = new Vector2(0f, -top - tabsBaseHeight);
                tabs.offsetMax = new Vector2(0f, -top);
                HemiAdaptiveRow tabsLayout = tabs.gameObject.AddComponent<HemiAdaptiveRow>();
                tabsLayout.Spacing = 6f;
                tabsLayout.VerticalGap = HemiTheme.Row(6f);
                tabsLayout.BaseHeight = tabsBaseHeight;
                int tabPadding = Mathf.RoundToInt(HemiTheme.Row(4f));
                tabsLayout.padding = new RectOffset(12, 12, tabPadding, tabPadding);
                tabsLayout.childAlignment = TextAnchor.MiddleLeft;

                Tab(tabs, HemiLang.Get("KVE_KEY"), KeyTab.Key, "Key");

                if (key.Kind == KeyViewerElementKind.Key)
                {
                    Tab(tabs, HemiLang.Get("KVE_NOTE"), KeyTab.Note, "Note");
                    Tab(tabs, HemiLang.Get("KVE_COUNTER"), KeyTab.Counter, "Counter");
                }
                else if (tab != KeyTab.Key)
                {
                    tab = KeyTab.Key;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(tabs);
                float tabsHeight = Mathf.Max(tabsBaseHeight, LayoutUtility.GetPreferredHeight(tabs));
                tabs.offsetMin = new Vector2(0f, -top - tabsHeight);
                LayoutRebuilder.ForceRebuildLayoutImmediate(tabs);
                top += tabsHeight;
            }

            RectTransform area = HemiKit.Rect("Body", panel.rectTransform);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(12f, 12f);
            area.offsetMax = new Vector2(-12f, -top - 4f);
            RectTransform content = HemiKit.Scroll(area, out ScrollRect _);

            try
            {
                if (key == null)
                {
                    BuildSharedSettings(content);
                    return;
                }

                switch (tab)
                {
                    case KeyTab.Note:
                        BuildNoteTab(content, index, key);
                        break;
                    case KeyTab.Counter:
                        BuildCounterTab(content, index, key);
                        break;
                    default:
                        BuildKeyTab(content, index, key);
                        break;
                }
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Error("Key Viewer panel failed to build: " + exception);
            }
        }

        private void Tab(RectTransform row, string label, KeyTab target, string name = null)
        {
            HemiColors colors = HemiTheme.Colors;
            bool selected = tab == target;
            Button button = HemiKit.Button(
                name ?? label,
                row,
                label,
                selected ? colors.Accent : colors.Field,
                selected ? Color.white : colors.Muted,
                delegate
                {
                    tab = target;
                    RebuildPanel(false);
                },
                13f,
                8f);
            HemiKit.Size(button.gameObject, HemiTheme.Col(80f), HemiTheme.Row(32f), 1f);
        }

        private void BuildSharedSettings(RectTransform content)
        {
            HemiColors colors = HemiTheme.Colors;
            KeyViewerConfig config = KeyViewer.Config;

            HemiRows.NoteRow(content, HemiLang.Get("KVE_NOTE_CANVAS"));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_OVERLAY_AREA"));
            HemiRows.NoteRow(content, HemiLang.Get("KVE_NOTE_OVERLAY_AREA"));
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_ANCHOR"), AnchorOptions, (int)config.BoardAnchor, value =>
            {
                KeyViewer.SetBoardAnchor((StateAnchor)value);
                RebuildPanel();
            });
            HemiRows.SliderRow(content, HemiLang.Get("KVE_OFFSET_X"), config.BoardOffset.x, -2000f, 2000f, true,
                value => KeyViewer.SetBoardOffset(new Vector2(value, KeyViewer.Config.BoardOffset.y)),
                HemiLang.Get("DESC_KVE_OFFSET_X"), 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_OFFSET_Y"), config.BoardOffset.y, -1200f, 1200f, true,
                value => KeyViewer.SetBoardOffset(new Vector2(KeyViewer.Config.BoardOffset.x, value)), null, 0, "px");
            HemiRows.ColorRow(content, HemiLang.Get("KVE_BOARD_BACKGROUND"), config.BoardBackgroundColor,
                value => EditShared(c => c.BoardBackgroundColor = value));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_PRESS_ANIMATION"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FONT_SIZE"), config.FontSize, 8f, 100f, true,
                value => KeyViewer.SetFontSize(Mathf.RoundToInt(value)),
                HemiLang.Get("DESC_KVE_FONT_SIZE"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_PRESSED_SCALE"), config.PressedScale, 0.5f, 1f, false,
                KeyViewer.SetPressedScale, HemiLang.Get("DESC_KVE_PRESSED_SCALE"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_RESPONSE_SPEED"), config.AnimationSpeed, 1f, 30f, false,
                KeyViewer.SetAnimationSpeed, null, 1);
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_EASING"), HemiEases.Names,
                KeyViewerEasings.CatalogIndex(config.Easing), index =>
            {
                KeyViewer.SetEasing(KeyViewerEasings.FromCatalogIndex(index));
                RebuildPanel();
            });

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_NOTE_TRACK"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_SHOW_NOTES"), HemiLang.Get("DESC_KVE_SHOW_NOTES"),
                () => KeyViewer.Config.ShowNotes,
                value =>
                {
                    KeyViewer.SetShowNotes(value);
                    RebuildPanel();
                });
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FRAME_LIMIT"), config.NoteFrameLimit, 0f, 240f, true,
                value => EditShared(c => c.NoteFrameLimit = Mathf.RoundToInt(value)), null, 0, "fps");
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_DELAYED_NOTE"), HemiLang.Get("DESC_KVE_DELAYED_NOTE"),
                () => KeyViewer.Config.DelayedNoteEnabled,
                value => { EditShared(c => c.DelayedNoteEnabled = value); RebuildPanel(); });
            if (config.DelayedNoteEnabled)
            {
                HemiRows.SliderRow(content, HemiLang.Get("KVE_SHORT_NOTE_THRESHOLD"), config.ShortNoteThresholdMs, 0f, 2000f, true,
                    value => EditShared(c => c.ShortNoteThresholdMs = value), null, 0, "ms");
            }
            HemiRows.SliderRow(content, HemiLang.Get("KVE_KEY_DISPLAY_DELAY"), config.KeyDisplayDelayMs, 0f, 30000f, true,
                value => EditShared(c => c.KeyDisplayDelayMs = value), null, 0, "ms");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FADE_TOP"), config.NoteFadeTop, 0f, KeyViewer.MaximumNoteFade, true,
                value => EditShared(c => c.NoteFadeTop = value), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FADE_BOTTOM"), config.NoteFadeBottom, 0f, KeyViewer.MaximumNoteFade, true,
                value => EditShared(c => c.NoteFadeBottom = value), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_REVERSE_FADE_TOP"), config.ReverseNoteFadeTop, 0f, KeyViewer.MaximumNoteFade, true,
                value => EditShared(c => c.ReverseNoteFadeTop = value), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_REVERSE_FADE_BOTTOM"), config.ReverseNoteFadeBottom, 0f, KeyViewer.MaximumNoteFade, true,
                value => EditShared(c => c.ReverseNoteFadeBottom = value), null, 0, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_GRID"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_ALIGNMENT_GUIDES"), null,
                () => KeyViewer.Config.GridAlignmentGuides,
                value => EditShared(c => c.GridAlignmentGuides = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_SPACING_GUIDES"), null,
                () => KeyViewer.Config.GridSpacingGuides,
                value => EditShared(c => c.GridSpacingGuides = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_SIZE_GUIDES"), null,
                () => KeyViewer.Config.GridSizeMatchGuides,
                value => EditShared(c => c.GridSizeMatchGuides = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_MINIMAP"), null,
                () => KeyViewer.Config.GridMinimap,
                value => EditShared(c => c.GridMinimap = value));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_GRID_SNAP"), config.GridSnapSize, 1f, 10f, true,
                value => EditShared(c => c.GridSnapSize = value), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_GRID_PADDING"), config.GridOverlayPadding, 0f, 30f, true,
                value => EditShared(c => c.GridOverlayPadding = value), null, 0, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_DM_STYLE"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_USE_CUSTOM_CSS"), HemiLang.Get("DESC_KVE_CUSTOM_CSS"),
                () => KeyViewer.Config.UseCustomCss,
                value => { EditShared(c => c.UseCustomCss = value); RebuildPanel(); });
            if (config.UseCustomCss)
            {
                HemiRows.TextAreaRow(content, HemiLang.Get("KVE_CUSTOM_CSS"), config.CustomCss, "", value =>
                    EditShared(c => c.CustomCss = value), HemiLang.Get("DESC_KVE_CUSTOM_CSS_STORE"));
            }
            HemiRows.TextRow(content, HemiLang.Get("KVE_LAYER_GROUPS"), SerializeLayerGroups(config.LayerGroups), "id:name", value =>
            {
                List<KeyViewerLayerGroup> parsed = ParseLayerGroups(value);
                EditShared(c =>
                {
                    c.LayerGroups.Clear();
                    c.LayerGroups.AddRange(parsed);
                });
                RebuildPanel();
            }, HemiLang.Get("DESC_KVE_LAYER_GROUPS"));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_COUNTERS"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_SHOW_COUNTERS"), HemiLang.Get("DESC_KVE_SHOW_COUNTERS"),
                () => KeyViewer.Config.ShowCounters,
                value =>
                {
                    KeyViewer.SetShowCounters(value);
                    RebuildPanel();
                });
            HemiRows.ActionRow(content, HemiLang.Get("KVE_ALL_COUNTS"), null, HemiLang.Get("KVE_RESET"), delegate
            {
                KeyViewer.ResetCounters();
                RebuildPanel();
            }, HemiLang.Get("DESC_KVE_ALL_COUNTS"), colors.Negative);

            BuildProfiles(content);
        }

        private void BuildKeyTab(RectTransform content, int index, KeyViewerKeyConfig key)
        {
            List<int> sel = Selection();

            if (key.Kind == KeyViewerElementKind.Stat || key.Kind == KeyViewerElementKind.Graph)
            {
                HemiRows.DropdownRow(content, HemiLang.Get("KVE_STAT_SHOWS"), StatOptions, (int)key.Stat, value =>
                {
                    KeyViewer.SetStat(sel, (KeyViewerStat)value);
                    RebuildPanel();
                });
                HemiRows.NoteRow(content, HemiLang.Get("KVE_NOTE_STAT"));
                if (key.Kind == KeyViewerElementKind.Graph)
                {
                    HemiRows.DropdownRow(content, HemiLang.Get("KVE_GRAPH_TYPE"), GraphTypeOptions, (int)key.GraphType, value =>
                    {
                        KeyViewer.EditKeys(sel, k => k.GraphType = (KeyViewerGraphType)value);
                        RebuildPanel();
                    });
                    HemiRows.SliderRow(content, HemiLang.Get("KVE_GRAPH_HISTORY"), key.GraphSpeedSeconds, 0.5f, 5f, false,
                        value => KeyViewer.EditKeys(sel, k => k.GraphSpeedSeconds = value), null, 1, "s");
                    HemiRows.ColorRow(content, HemiLang.Get("KVE_GRAPH_COLOR"), key.GraphColor,
                        value => KeyViewer.EditKeys(sel, k => k.GraphColor = value));
                    HemiRows.ToggleRow(content, HemiLang.Get("KVE_GRAPH_AVG_LINE"), null,
                        () => Live(index, key).GraphShowAverage,
                        value => KeyViewer.EditKeys(sel, k => k.GraphShowAverage = value));
                    HemiRows.ToggleRow(content, HemiLang.Get("KVE_GRAPH_ANIMATION"), null,
                        () => Live(index, key).GraphAnimationEnabled,
                        value => KeyViewer.EditKeys(sel, k => k.GraphAnimationEnabled = value));
                }
            }
            else if (key.Kind == KeyViewerElementKind.Knob)
            {
                HemiRows.TextRow(content, HemiLang.Get("KVE_KNOB_AXIS"), key.KnobAxisId, "HIDA:...", value =>
                    KeyViewer.EditKeys(sel, k => k.KnobAxisId = value), HemiLang.Get("DESC_KVE_KNOB_AXIS"));
                HemiRows.SliderRow(content, HemiLang.Get("KVE_KNOB_SENSITIVITY"), key.KnobSensitivity, 0.01f, 10f, false,
                    value => KeyViewer.EditKeys(sel, k => k.KnobSensitivity = value), null, 2);
                HemiRows.ToggleRow(content, HemiLang.Get("KVE_REVERSE"), null,
                    () => Live(index, key).KnobReverse,
                    value => KeyViewer.EditKeys(sel, k => k.KnobReverse = value));
            }
            else
            {
                HemiRows.ActionRow(
                    content,
                    HemiLang.Get("KVE_KEY_MAPPING"),
                    KeyViewerKeyNames.GetDisplayName(key.Key),
                    KeyViewer.RemappingKeyIndex == index ? HemiLang.Get("KVE_CANCEL") : HemiLang.Get("KVE_REMAP"),
                    delegate
                    {
                        if (KeyViewer.RemappingKeyIndex == index)
                            KeyViewer.StopRegistration();
                        else
                            KeyViewer.BeginRemappingKey(index);
                        RebuildPanel();
                    });
                HemiRows.TextRow(content, HemiLang.Get("KVE_MULTI_KEYS"), SerializeKeys(key), "A, S, D", value =>
                {
                    ApplyKeyList(sel, value);
                    RebuildPanel();
                }, HemiLang.Get("DESC_KVE_MULTI_KEYS"));
                HemiRows.DropdownRow(content, HemiLang.Get("KVE_MULTI_MATCH"), KeyMatchOptions, (int)key.KeyMatch, value =>
                {
                    KeyViewer.EditKeys(sel, k => k.KeyMatch = (KeyViewerKeyMatch)value);
                    RebuildPanel();
                });
            }

            HemiRows.TextRow(content, HemiLang.Get("KVE_LABEL"), key.DisplayText, key.ResolveLabel(),
                value =>
                {
                    KeyViewer.EditKeys(sel, k => k.DisplayText = value);
                    RebuildPanel();
                },
                HemiLang.Get("DESC_KVE_LABEL"));
            HemiRows.TextRow(content, HemiLang.Get("KVE_LAYER_NAME"), key.LayerName, "", value =>
                KeyViewer.EditKeys(sel, k => k.LayerName = value));
            HemiRows.TextRow(content, HemiLang.Get("KVE_GROUP_ID"), key.GroupId, "", value =>
                KeyViewer.EditKeys(sel, k => k.GroupId = value));
            HemiRows.TextRow(content, HemiLang.Get("KVE_CSS_CLASS"), key.CssClass, "", value =>
                KeyViewer.EditKeys(sel, k => k.CssClass = value), HemiLang.Get("DESC_KVE_CSS_METADATA"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_INLINE_STYLES"), HemiLang.Get("DESC_KVE_INLINE_STYLES"),
                () => Live(index, key).UseInlineStyles,
                value => KeyViewer.EditKeys(sel, k => k.UseInlineStyles = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_HIDDEN"), null,
                () => Live(index, key).Hidden,
                value => KeyViewer.EditKeys(sel, k => k.Hidden = value));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_POSITION"));
            geometryIndex = index;
            shownPosition = key.Position;
            shownSize = key.Size;

            float reach = Mathf.Max(PositionSliderReach, Mathf.Max(key.Position.x, key.Position.y));
            xSlider = HemiRows.SliderRow(content, "X", key.Position.x, 0f, reach, true,
                value => KeyViewer.SetKeyPosition(sel, new Vector2(value, Live(index, key).Position.y), true), null, 0, "px");
            ySlider = HemiRows.SliderRow(content, "Y", key.Position.y, 0f, reach, true,
                value => KeyViewer.SetKeyPosition(sel, new Vector2(Live(index, key).Position.x, value), true), null, 0, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_SIZE"));
            widthSlider = HemiRows.SliderRow(content, HemiLang.Get("KVE_WIDTH"), key.Size.x, KeyViewer.MinimumKeySize, 400f, true,
                value => KeyViewer.SetKeySize(sel, new Vector2(value, Live(index, key).Size.y), true), null, 0, "px");
            heightSlider = HemiRows.SliderRow(content, HemiLang.Get("KVE_HEIGHT"), key.Size.y, KeyViewer.MinimumKeySize, 400f, true,
                value => KeyViewer.SetKeySize(sel, new Vector2(Live(index, key).Size.x, value), true), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_CORNER_RADIUS"), key.CornerRadius, 0f,
                Mathf.Min(100f, Mathf.Min(key.Size.x, key.Size.y) * 0.5f), false,
                value => KeyViewer.SetKeyCornerRadius(sel, value), null, 1, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_BORDER_WIDTH"), key.BorderWidth, 0f,
                Mathf.Min(20f, Mathf.Min(key.Size.x, key.Size.y) * 0.5f), false,
                value => KeyViewer.SetKeyBorderWidth(sel, value), null, 1, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_IDLE"));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_BACKGROUND"), key.BackgroundColor,
                value => KeyViewer.SetKeyBackgroundColor(sel, value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_BORDER"), key.BorderColor,
                value => KeyViewer.SetKeyBorderColor(sel, value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_TEXT"), key.TextColor,
                value => KeyViewer.SetKeyTextColor(sel, value));
            BuildGradientEditor(content, HemiLang.Get("KVE_BACKGROUND_GRADIENT"), index, key,
                k => k.BackgroundGradient, (k, value) => k.BackgroundGradient = value, sel);
            BuildGradientEditor(content, HemiLang.Get("KVE_BORDER_GRADIENT"), index, key,
                k => k.BorderGradient, (k, value) => k.BorderGradient = value, sel);
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_TRANSPARENT"), HemiLang.Get("DESC_KVE_TRANSPARENT_IDLE"),
                () => Live(index, key).IdleTransparent,
                value => KeyViewer.EditKeys(sel, k => k.IdleTransparent = value));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_PRESSED"));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_BACKGROUND"), key.ActiveBackgroundColor,
                value => KeyViewer.EditKeys(sel, k => k.ActiveBackgroundColor = value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_BORDER"), key.ActiveBorderColor,
                value => KeyViewer.EditKeys(sel, k => k.ActiveBorderColor = value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_TEXT"), key.ActiveTextColor,
                value => KeyViewer.EditKeys(sel, k => k.ActiveTextColor = value));
            BuildGradientEditor(content, HemiLang.Get("KVE_BACKGROUND_GRADIENT"), index, key,
                k => k.ActiveBackgroundGradient, (k, value) => k.ActiveBackgroundGradient = value, sel);
            BuildGradientEditor(content, HemiLang.Get("KVE_BORDER_GRADIENT"), index, key,
                k => k.ActiveBorderGradient, (k, value) => k.ActiveBorderGradient = value, sel);
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_TRANSPARENT"), HemiLang.Get("DESC_KVE_TRANSPARENT_ACTIVE"),
                () => Live(index, key).ActiveTransparent,
                value => KeyViewer.EditKeys(sel, k => k.ActiveTransparent = value));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_TYPOGRAPHY"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FONT_SIZE"), key.FontSize, 0f, 100f, true,
                value => KeyViewer.EditKeys(sel, k => k.FontSize = value),
                HemiLang.Get("DESC_KVE_FONT_SIZE_KEY"), 0, "px");
            HemiRows.TextRow(content, HemiLang.Get("KVE_FONT_FAMILY"), key.FontFamily, "Arial", value =>
            {
                KeyViewer.EditKeys(sel, k => k.FontFamily = value);
                KeyViewer.RefreshFonts();
            });
            HemiRows.FileRow(content, HemiLang.Get("KVE_FONT_FILE"), key.FontFilePath,
                HemiLang.Get("KVE_FONT_FILTER"), FontExtensions, HemiLang.Get("KVE_PICK_FONT"), value =>
                {
                    KeyViewer.EditKeys(sel, k => k.FontFilePath = value);
                    KeyViewer.RefreshFonts();
                    RebuildPanel();
                });
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FONT_WEIGHT"), key.FontWeight, 100f, 900f, true,
                value => KeyViewer.EditKeys(sel, k => k.FontWeight = Mathf.RoundToInt(value)), null, 0);
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_ITALIC"), null,
                () => Live(index, key).FontItalic, value => KeyViewer.EditKeys(sel, k => k.FontItalic = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_UNDERLINE"), null,
                () => Live(index, key).FontUnderline, value => KeyViewer.EditKeys(sel, k => k.FontUnderline = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_STRIKETHROUGH"), null,
                () => Live(index, key).FontStrikethrough, value => KeyViewer.EditKeys(sel, k => k.FontStrikethrough = value));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_CUSTOM_IMAGE"));
            HemiRows.FileRow(content, HemiLang.Get("KVE_IDLE"), key.IdleImagePath,
                HemiLang.Get("KVE_IMAGE_FILTER"), ImageExtensions,
                HemiLang.Get("KVE_PICK_IDLE_IMAGE"), value =>
                {
                    KeyViewerImageCache.Invalidate(Live(index, key).IdleImagePath);
                    KeyViewer.EditKeys(sel, k => k.IdleImagePath = value);
                    RebuildPanel();
                });
            HemiRows.FileRow(content, HemiLang.Get("KVE_PRESSED"), key.ActiveImagePath,
                HemiLang.Get("KVE_IMAGE_FILTER"), ImageExtensions,
                HemiLang.Get("KVE_PICK_ACTIVE_IMAGE"), value =>
                {
                    KeyViewerImageCache.Invalidate(Live(index, key).ActiveImagePath);
                    KeyViewer.EditKeys(sel, k => k.ActiveImagePath = value);
                    RebuildPanel();
                });
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_IDLE_IMAGE_DISPLAY"), ImageFitOptions, (int)key.IdleImageFit, value =>
            {
                KeyViewer.EditKeys(sel, k => k.IdleImageFit = (KeyViewerImageFit)value);
                RebuildPanel();
            });
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_ACTIVE_IMAGE_DISPLAY"), ImageFitOptions, (int)key.ActiveImageFit, value =>
            {
                KeyViewer.EditKeys(sel, k => k.ActiveImageFit = (KeyViewerImageFit)value);
                RebuildPanel();
            });

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_SHADOW"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_ENABLE_SHADOW"), HemiLang.Get("DESC_KVE_ENABLE_SHADOW"),
                () => Live(index, key).ShadowEnabled,
                value =>
                {
                    KeyViewer.EditKeys(sel, k =>
                    {
                        k.ShadowEnabled = value;
                        k.ActiveShadowEnabled = value;
                    });
                    RebuildPanel();
                });

            if (!key.ShadowEnabled && !key.ActiveShadowEnabled)
                return;
            BuildShadowEditor(content, index, key, false, sel);
            BuildShadowEditor(content, index, key, true, sel);
        }

        private void BuildNoteTab(RectTransform content, int index, KeyViewerKeyConfig key)
        {
            List<int> sel = Selection();

            HemiRows.ToggleRow(content, HemiLang.Get("KVE_NOTE_EFFECT"), HemiLang.Get("DESC_KVE_NOTE_EFFECT"),
                () => Live(index, key).RainingEffect,
                value =>
                {
                    KeyViewer.SetKeyRainingEffect(sel, value);
                    RebuildPanel();
                });

            if (!key.RainingEffect)
                return;

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_TRACK"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_HEIGHT"), key.RainHeight,
                KeyViewer.MinimumRainingTrackHeight, KeyViewer.MaximumRainingTrackHeight, true,
                value => KeyViewer.SetKeyRainHeight(sel, value), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_SPEED"), key.RainSpeed,
                KeyViewer.MinimumRainingSpeed, 2000f, true,
                value => KeyViewer.SetKeyRainSpeed(sel, value), null, 0, "px/s");
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_REVERSE"), HemiLang.Get("DESC_KVE_REVERSE"),
                () => Live(index, key).RainReverse,
                value => KeyViewer.SetKeyRainReverse(sel, value));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_MINIMUM_LENGTH"), key.RainMinimumLength,
                KeyViewer.MinimumRainNoteLength, 400f, true,
                value => KeyViewer.SetKeyRainMinimumLength(sel, value), HemiLang.Get("DESC_KVE_MINIMUM_LENGTH"), 0, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_SHAPE"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_NOTE_WIDTH"), key.NoteWidth, 0f, 400f, true,
                value => KeyViewer.EditKeys(sel, k => k.NoteWidth = value),
                HemiLang.Get("DESC_KVE_NOTE_WIDTH"), 0, "px");
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_ALIGNMENT"), NoteAlignOptions, (int)key.NoteAlignment, value =>
            {
                KeyViewer.EditKeys(sel, k => k.NoteAlignment = (KeyViewerNoteAlignment)value);
                RebuildPanel();
            });
            OffsetRows(content, key.NoteOffset, -200f, 200f, sel, k => k.NoteOffset, (k, value) => k.NoteOffset = value);
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_AUTO_Y_CORRECTION"), HemiLang.Get("DESC_KVE_AUTO_Y_CORRECTION"),
                () => Live(index, key).NoteAutoYCorrection,
                value => KeyViewer.EditKeys(sel, k => k.NoteAutoYCorrection = value));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_CORNER_RADIUS"), key.RainCornerRadius, 0f, 100f, false,
                value => KeyViewer.SetKeyRainCornerRadius(sel, value), null, 1, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_COLOR"));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_NOTE"), key.RainColor,
                value => KeyViewer.SetKeyRainColor(sel, value));
            PercentRow(content, HemiLang.Get("KVE_OPACITY"), key.RainOpacity, value => KeyViewer.SetKeyRainOpacity(sel, value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_GRADIENT"), HemiLang.Get("DESC_KVE_GRADIENT"),
                () => Live(index, key).RainGradient,
                value =>
                {
                    KeyViewer.EditKeys(sel, k => k.RainGradient = value);
                    RebuildPanel();
                });

            if (key.RainGradient)
            {
                HemiRows.ColorRow(content, HemiLang.Get("KVE_BOTTOM_COLOR"), key.RainColorBottom,
                    value => KeyViewer.EditKeys(sel, k => k.RainColorBottom = value));
                PercentRow(content, HemiLang.Get("KVE_BOTTOM_OPACITY"), key.RainOpacityBottom, value => KeyViewer.EditKeys(sel, k => k.RainOpacityBottom = value));
            }

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_BORDER"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_WIDTH"), key.RainBorderWidth, 0f, 20f, false,
                value => KeyViewer.SetKeyRainBorderWidth(sel, value), null, 1, "px");
            HemiRows.ColorRow(content, HemiLang.Get("KVE_COLOR"), key.RainBorderColor,
                value => KeyViewer.SetKeyRainBorderColor(sel, value));
            PercentRow(content, HemiLang.Get("KVE_OPACITY"), key.RainBorderOpacity, value => KeyViewer.EditKeys(sel, k => k.RainBorderOpacity = value));
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_SIDES"), BorderSideOptions, (int)key.RainBorderSide, value =>
            {
                KeyViewer.EditKeys(sel, k => k.RainBorderSide = (KeyViewerNoteBorderSide)value);
                RebuildPanel();
            });

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_GLOW"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_GLOW_EFFECT"), HemiLang.Get("DESC_KVE_GLOW_EFFECT"),
                () => Live(index, key).GlowEnabled,
                value =>
                {
                    KeyViewer.EditKeys(sel, k => k.GlowEnabled = value);
                    RebuildPanel();
                });

            if (!key.GlowEnabled)
                return;

            HemiRows.ColorRow(content, HemiLang.Get("KVE_COLOR"), key.GlowColor,
                value => KeyViewer.EditKeys(sel, k => k.GlowColor = value));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_SIZE"), key.GlowSize, 0f, KeyViewer.MaximumGlowSize, true,
                value => KeyViewer.EditKeys(sel, k => k.GlowSize = value), null, 0, "px");
            PercentRow(content, HemiLang.Get("KVE_OPACITY"), key.GlowOpacity, value => KeyViewer.EditKeys(sel, k => k.GlowOpacity = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_GRADIENT"), HemiLang.Get("DESC_KVE_GRADIENT"),
                () => Live(index, key).GlowGradient,
                value =>
                {
                    KeyViewer.EditKeys(sel, k => k.GlowGradient = value);
                    RebuildPanel();
                });
            if (key.GlowGradient)
            {
                HemiRows.ColorRow(content, HemiLang.Get("KVE_BOTTOM_COLOR"), key.GlowColorBottom,
                    value => KeyViewer.EditKeys(sel, k => k.GlowColorBottom = value));
                PercentRow(content, HemiLang.Get("KVE_BOTTOM_OPACITY"), key.GlowOpacityBottom, value => KeyViewer.EditKeys(sel, k => k.GlowOpacityBottom = value));
            }
        }

        private static void PercentRow(RectTransform content, string label, float fraction, Action<float> apply) =>
            HemiRows.SliderRow(content, label, fraction * 100f, 0f, 100f, true, value => apply(value / 100f), null, 0, "%");

        private void BuildCounterTab(RectTransform content, int index, KeyViewerKeyConfig key)
        {
            List<int> sel = Selection();
            HemiColors colors = HemiTheme.Colors;

            if (!KeyViewer.Config.ShowCounters)
                HemiRows.NoteRow(content, HemiLang.Get("KVE_NOTE_COUNTERS_OFF"));

            HemiRows.ToggleRow(content, HemiLang.Get("KVE_ENABLE_COUNTER"), HemiLang.Get("DESC_KVE_ENABLE_COUNTER"),
                () => Live(index, key).CounterEnabled,
                value =>
                {
                    KeyViewer.EditKeys(sel, k => k.CounterEnabled = value);
                    RebuildPanel();
                });

            HemiRows.ActionRow(content, HemiLang.Get("KVE_COUNT"), key.Count.ToString(CultureInfo.InvariantCulture),
                HemiLang.Get("KVE_RESET"), delegate
            {
                KeyViewer.EditKeys(sel, k => k.Count = 0);
                RebuildPanel();
            }, null, colors.Field);

            if (!key.CounterEnabled)
                return;

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_PLACEMENT"));
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_AREA"), PlacementOptions, (int)key.CounterPlacement, value =>
            {
                KeyViewer.EditKeys(sel, k => k.CounterPlacement = (KeyViewerCounterPlacement)value);
                RebuildPanel();
            });
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_ALIGNMENT"), CounterAlignOptions, (int)key.CounterAlign, value =>
            {
                KeyViewer.EditKeys(sel, k => k.CounterAlign = (KeyViewerCounterAlign)value);
                RebuildPanel();
            });
            HemiRows.DropdownRow(content, HemiLang.Get("KVE_LAYOUT"), AlignModeOptions, (int)key.CounterAlignMode, value =>
            {
                KeyViewer.EditKeys(sel, k => k.CounterAlignMode = (KeyViewerCounterAlignMode)value);
                RebuildPanel();
            });
            HemiRows.SliderRow(content, HemiLang.Get("KVE_GAP"), key.CounterGap, 0f, 60f, true,
                value => KeyViewer.EditKeys(sel, k => k.CounterGap = value),
                HemiLang.Get("DESC_KVE_GAP"), 0, "px");

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_FONT"));
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FONT_SIZE"), key.CounterFontSize, 6f, 60f, true,
                value => KeyViewer.EditKeys(sel, k => k.CounterFontSize = value), null, 0, "px");
            HemiRows.ColorRow(content, HemiLang.Get("KVE_IDLE"), key.CounterIdleColor,
                value => KeyViewer.EditKeys(sel, k => k.CounterIdleColor = value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_PRESSED"), key.CounterActiveColor,
                value => KeyViewer.EditKeys(sel, k => k.CounterActiveColor = value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_STROKE_IDLE"), key.CounterIdleStrokeColor,
                value => KeyViewer.EditKeys(sel, k => k.CounterIdleStrokeColor = value));
            HemiRows.ColorRow(content, HemiLang.Get("KVE_STROKE_ACTIVE"), key.CounterActiveStrokeColor,
                value => KeyViewer.EditKeys(sel, k => k.CounterActiveStrokeColor = value));
            BuildGradientEditor(content, HemiLang.Get("KVE_IDLE_FILL_GRADIENT"), index, key,
                k => k.CounterIdleGradient, (k, value) => k.CounterIdleGradient = value, sel);
            BuildGradientEditor(content, HemiLang.Get("KVE_ACTIVE_FILL_GRADIENT"), index, key,
                k => k.CounterActiveGradient, (k, value) => k.CounterActiveGradient = value, sel);
            HemiRows.TextRow(content, HemiLang.Get("KVE_FONT_FAMILY"), key.CounterFontFamily, "Arial", value =>
            {
                KeyViewer.EditKeys(sel, k => k.CounterFontFamily = value);
                KeyViewer.RefreshFonts();
            });
            HemiRows.FileRow(content, HemiLang.Get("KVE_FONT_FILE"), key.CounterFontFilePath,
                HemiLang.Get("KVE_FONT_FILTER"), FontExtensions, HemiLang.Get("KVE_PICK_FONT"), value =>
                {
                    KeyViewer.EditKeys(sel, k => k.CounterFontFilePath = value);
                    KeyViewer.RefreshFonts();
                    RebuildPanel();
                });
            HemiRows.SliderRow(content, HemiLang.Get("KVE_FONT_WEIGHT"), key.CounterFontWeight, 100f, 900f, true,
                value => KeyViewer.EditKeys(sel, k => k.CounterFontWeight = Mathf.RoundToInt(value)), null, 0);
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_ITALIC"), null,
                () => Live(index, key).CounterFontItalic, value => KeyViewer.EditKeys(sel, k => k.CounterFontItalic = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_UNDERLINE"), null,
                () => Live(index, key).CounterFontUnderline, value => KeyViewer.EditKeys(sel, k => k.CounterFontUnderline = value));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_STRIKETHROUGH"), null,
                () => Live(index, key).CounterFontStrikethrough, value => KeyViewer.EditKeys(sel, k => k.CounterFontStrikethrough = value));

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_MOTION"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_COUNTER_MOTION"), HemiLang.Get("DESC_KVE_COUNTER_MOTION"),
                () => Live(index, key).CounterAnimationEnabled,
                value =>
                {
                    KeyViewer.EditKeys(sel, k => k.CounterAnimationEnabled = value);
                    RebuildPanel();
                });

            if (!key.CounterAnimationEnabled)
                return;

            HemiRows.SliderRow(content, HemiLang.Get("KVE_SCALE"), key.CounterAnimationScale, 1f, 3f, false,
                value => KeyViewer.EditKeys(sel, k => k.CounterAnimationScale = value), null, 2);
            HemiRows.SliderRow(content, HemiLang.Get("KVE_DURATION"), key.CounterAnimationSeconds * 1000f, 20f, 2000f, true,
                value => KeyViewer.EditKeys(sel, k => k.CounterAnimationSeconds = value / 1000f), null, 0, "ms");
            HemiRows.TextRow(content, HemiLang.Get("KVE_BEZIER"), SerializeBezier(key.CounterAnimationBezier), "0.34,1.56,0.64,1", value =>
            {
                if (!TryParseBezier(value, out Vector4 curve))
                    return;
                KeyViewer.EditKeys(sel, k => k.CounterAnimationBezier = curve);
            }, HemiLang.Get("DESC_KVE_BEZIER"));
        }

        private static void EditShared(Action<KeyViewerConfig> edit)
        {
            if (edit == null)
                return;
            edit(KeyViewer.Config);
            KeyViewer.CommitLayout();
        }

        private static string SerializeKeys(KeyViewerKeyConfig key)
        {
            List<string> names = new List<string> { KeyViewerKeyNames.GetDisplayName(key.Key) };
            for (int i = 0; i < key.AdditionalKeys.Count; i++)
                names.Add(KeyViewerKeyNames.GetDisplayName(key.AdditionalKeys[i]));
            return string.Join(", ", names.ToArray());
        }

        private static void ApplyKeyList(IList<int> selection, string text)
        {
            string[] names = (text ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<KeyCode> keys = new List<KeyCode>();
            for (int i = 0; i < names.Length && keys.Count < 8; i++)
            {
                KeyCode code = KeyViewerDmNote.ResolveKeyCode(names[i]);
                if (code != KeyCode.None && !keys.Contains(code))
                    keys.Add(code);
            }
            if (keys.Count == 0)
                return;
            KeyViewer.EditKeys(selection, key =>
            {
                key.Key = keys[0];
                key.AdditionalKeys.Clear();
                for (int i = 1; i < keys.Count; i++)
                    key.AdditionalKeys.Add(keys[i]);
            });
        }

        private void BuildGradientEditor(
            RectTransform content,
            string label,
            int index,
            KeyViewerKeyConfig key,
            Func<KeyViewerKeyConfig, KeyViewerGradient> get,
            Action<KeyViewerKeyConfig, KeyViewerGradient> set,
            IList<int> selection)
        {
            KeyViewerGradient gradient = get(key);
            HemiRows.ToggleRow(content, label, HemiLang.Get("DESC_KVE_GRADIENT_STOPS"),
                () => get(Live(index, key)) != null,
                enabled =>
                {
                    KeyViewer.EditKeys(selection, target =>
                    {
                        KeyViewerGradient next = enabled ? get(target) : null;
                        if (enabled && next == null)
                        {
                            next = new KeyViewerGradient { Angle = 90f };
                            next.Stops.Add(new KeyViewerGradientStop(Color.white, 0f));
                            next.Stops.Add(new KeyViewerGradientStop(Color.black, 1f));
                        }
                        set(target, next);
                    });
                    RebuildPanel();
                });
            if (gradient == null)
                return;
            HemiRows.SliderRow(content, HemiLang.Get("KVE_GRADIENT_ANGLE"), gradient.Angle, 0f, 360f, true,
                value => KeyViewer.EditKeys(selection, target =>
                {
                    KeyViewerGradient current = get(target);
                    if (current != null) current.Angle = value;
                }), null, 0, "°");
            HemiRows.TextRow(content, HemiLang.Get("KVE_GRADIENT_STOPS"), SerializeGradientStops(gradient),
                "0:#FFFFFFFF;1:#000000FF", value =>
                {
                    KeyViewerGradient parsed = ParseGradientStops(value, gradient.Angle);
                    if (parsed == null)
                        return;
                    KeyViewer.EditKeys(selection, target => set(target, KeyViewerGradient.Clone(parsed)));
                }, HemiLang.Get("DESC_KVE_GRADIENT_STOPS"));
        }

        private static string SerializeGradientStops(KeyViewerGradient gradient)
        {
            if (gradient == null)
                return "";
            List<string> values = new List<string>();
            for (int i = 0; i < gradient.Stops.Count; i++)
            {
                KeyViewerGradientStop stop = gradient.Stops[i];
                values.Add(stop.Position.ToString("0.###", CultureInfo.InvariantCulture)
                    + ":#" + ColorUtility.ToHtmlStringRGBA(stop.Color));
            }
            return string.Join(";", values.ToArray());
        }

        private static KeyViewerGradient ParseGradientStops(string text, float angle)
        {
            string[] entries = (text ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length < 2)
                return null;
            KeyViewerGradient result = new KeyViewerGradient { Angle = angle };
            for (int i = 0; i < entries.Length && result.Stops.Count < 8; i++)
            {
                int split = entries[i].IndexOf(':');
                if (split <= 0
                    || !float.TryParse(entries[i].Substring(0, split), NumberStyles.Float, CultureInfo.InvariantCulture, out float position))
                    continue;
                Color color = KeyViewerDmNote.ParseColor(entries[i].Substring(split + 1), 1f);
                result.Stops.Add(new KeyViewerGradientStop(color, Mathf.Clamp01(position)));
            }
            result.Normalize();
            return result.Stops.Count >= 2 ? result : null;
        }

        private static void BuildShadowEditor(
            RectTransform content,
            int index,
            KeyViewerKeyConfig key,
            bool active,
            IList<int> selection)
        {
            HemiRows.Heading(content, active ? HemiLang.Get("KVE_HEAD_PRESSED") : HemiLang.Get("KVE_HEAD_IDLE"));
            HemiRows.ToggleRow(content, HemiLang.Get("KVE_ENABLE_SHADOW"), null,
                () => active ? Live(index, key).ActiveShadowEnabled : Live(index, key).ShadowEnabled,
                value => KeyViewer.EditKeys(selection, target =>
                {
                    if (active) target.ActiveShadowEnabled = value; else target.ShadowEnabled = value;
                }));
            bool enabled = active ? key.ActiveShadowEnabled : key.ShadowEnabled;
            if (!enabled)
                return;
            HemiRows.ColorRow(content, HemiLang.Get("KVE_COLOR"), active ? key.ActiveShadowColor : key.ShadowColor,
                value => KeyViewer.EditKeys(selection, target =>
                {
                    if (active) target.ActiveShadowColor = value; else target.ShadowColor = value;
                }));
            float blur = active ? key.ActiveShadowBlur : key.ShadowBlur;
            Vector2 offset = active ? key.ActiveShadowOffset : key.ShadowOffset;
            HemiRows.SliderRow(content, HemiLang.Get("KVE_BLUR"), blur, 0f, 100f, true,
                value => KeyViewer.EditKeys(selection, target =>
                {
                    if (active) target.ActiveShadowBlur = value; else target.ShadowBlur = value;
                }), null, 0, "px");
            OffsetRows(content, offset, -100f, 100f, selection,
                active ? (Func<KeyViewerKeyConfig, Vector2>)(k => k.ActiveShadowOffset) : k => k.ShadowOffset,
                active ? (Action<KeyViewerKeyConfig, Vector2>)((k, value) => k.ActiveShadowOffset = value) : (k, value) => k.ShadowOffset = value);
        }

        private static void OffsetRows(RectTransform content, Vector2 offset, float minimum, float maximum,
            IList<int> selection, Func<KeyViewerKeyConfig, Vector2> get, Action<KeyViewerKeyConfig, Vector2> set)
        {
            HemiRows.SliderRow(content, HemiLang.Get("KVE_OFFSET_X"), offset.x, minimum, maximum, true,
                value => KeyViewer.EditKeys(selection, k => set(k, new Vector2(value, get(k).y))), null, 0, "px");
            HemiRows.SliderRow(content, HemiLang.Get("KVE_OFFSET_Y"), offset.y, minimum, maximum, true,
                value => KeyViewer.EditKeys(selection, k => set(k, new Vector2(get(k).x, value))), null, 0, "px");
        }

        private static string SerializeBezier(Vector4 curve)
        {
            return string.Join(",", new[]
            {
                curve.x.ToString("0.###", CultureInfo.InvariantCulture),
                curve.y.ToString("0.###", CultureInfo.InvariantCulture),
                curve.z.ToString("0.###", CultureInfo.InvariantCulture),
                curve.w.ToString("0.###", CultureInfo.InvariantCulture)
            });
        }

        private static bool TryParseBezier(string text, out Vector4 curve)
        {
            curve = Vector4.zero;
            string[] values = (text ?? "").Split(',');
            if (values.Length != 4)
                return false;
            float[] parsed = new float[4];
            for (int i = 0; i < parsed.Length; i++)
            {
                if (!float.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed[i]))
                    return false;
            }
            curve = new Vector4(parsed[0], parsed[1], parsed[2], parsed[3]);
            return true;
        }

        private static string SerializeLayerGroups(IList<KeyViewerLayerGroup> groups)
        {
            List<string> values = new List<string>();
            for (int i = 0; i < groups.Count; i++)
                values.Add((groups[i].Id ?? "") + ":" + (groups[i].Name ?? ""));
            return string.Join(";", values.ToArray());
        }

        private static List<KeyViewerLayerGroup> ParseLayerGroups(string text)
        {
            List<KeyViewerLayerGroup> result = new List<KeyViewerLayerGroup>();
            string[] values = (text ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i++)
            {
                int split = values[i].IndexOf(':');
                string id = split >= 0 ? values[i].Substring(0, split).Trim() : values[i].Trim();
                string name = split >= 0 ? values[i].Substring(split + 1).Trim() : id;
                if (!string.IsNullOrWhiteSpace(id))
                    result.Add(new KeyViewerLayerGroup { Id = id, Name = name });
            }
            return result;
        }

        private void BuildProfiles(RectTransform content)
        {
            HemiColors colors = HemiTheme.Colors;
            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_KEY_LAYOUTS"));

            RectTransform row = HemiRows.Card(content);
            HemiRows.Label(
                row,
                HemiLang.Get("KVE_PROFILE_LAYOUT"),
                HemiLang.Get("KVE_LAYOUT_POSITION",
                    KeyViewerProfiles.SelectedName, KeyViewerProfiles.SelectedDisplayIndex, KeyViewerProfiles.Count),
                110f);

            Button previous = HemiKit.Button("Previous", row, "<", colors.Field, colors.Text, delegate
            {
                KeyViewerProfiles.SelectPrevious();
                RebuildPanel();
            }, 14f, 8f);
            HemiKit.Size(previous.gameObject, 32f, HemiTheme.Row(30f));

            Button next = HemiKit.Button("Next", row, ">", colors.Field, colors.Text, delegate
            {
                KeyViewerProfiles.SelectNext();
                RebuildPanel();
            }, 14f, 8f);
            HemiKit.Size(next.gameObject, 32f, HemiTheme.Row(30f));

            Button load = HemiKit.Button(
                "Load",
                row,
                KeyViewerProfiles.SelectedIsActive ? HemiLang.Get("KVE_ACTIVE") : HemiLang.Get("KVE_LOAD"),
                KeyViewerProfiles.SelectedIsActive ? colors.Field : colors.Accent,
                KeyViewerProfiles.SelectedIsActive ? colors.Muted : Color.white,
                delegate
                {
                    if (!KeyViewerProfiles.SelectedIsActive)
                        KeyViewerProfiles.LoadSelected();
                    canvas.Select(-1);
                    RebuildAll();
                },
                13f,
                8f);
            HemiKit.Size(load.gameObject, 70f, HemiTheme.Row(30f));

            RectTransform createRow = HemiRows.Card(content);
            HemiRows.Label(createRow, HemiLang.Get("KVE_NEW"), null, 60f);

            TMP_InputField nameField = HemiKit.Field(createRow, "", HemiLang.Get("KVE_LAYOUT_NAME"), 130f);
            HemiKit.Size(nameField.gameObject, 130f, HemiTheme.Row(32f), 1f);

            Button create = HemiKit.Button("Create", createRow, HemiLang.Get("KVE_ADD"), colors.Accent, Color.white, delegate
            {
                KeyViewerProfiles.Create(nameField.text);
                RebuildPanel();
            }, 13f, 8f);
            HemiKit.Size(create.gameObject, 64f, HemiTheme.Row(30f));

            RectTransform actionRow = HemiRows.Card(content);
            ProfileButton(actionRow, HemiLang.Get("KVE_SAVE"), KeyViewerProfiles.SaveSelected, colors.Field, colors.Text, false, "Save");
            ProfileButton(actionRow, HemiLang.Get("KVE_IMPORT"), KeyViewerProfiles.ImportFromFile, colors.Field, colors.Text, true, "Import");
            ProfileButton(actionRow, HemiLang.Get("KVE_EXPORT"), KeyViewerProfiles.ExportSelected, colors.Field, colors.Text, false, "Export");
            ProfileButton(actionRow, HemiLang.Get("KVE_DELETE"), KeyViewerProfiles.DeleteSelected, colors.Field, colors.Negative, true, "Delete");

            TextMeshProUGUI status = HemiKit.Text(
                "Status",
                content,
                KeyViewerProfiles.StatusMessage,
                12f,
                KeyViewerProfiles.StatusIsError ? colors.Negative : colors.Muted,
                false,
                TextAlignmentOptions.Left,
                true);
            HemiKit.Size(status.gameObject, -1f, HemiTheme.Row(28f), 1f);

            HemiRows.Heading(content, HemiLang.Get("KVE_HEAD_DM_NOTE"));
            RectTransform importRow = HemiRows.Card(content);
            HemiRows.Label(importRow, HemiLang.Get("KVE_PRESET"), HemiLang.Get("DESC_KVE_PRESET"), 150f);
            Button import = HemiKit.Button("Import", importRow, HemiLang.Get("KVE_IMPORT"), colors.Accent, Color.white,
                delegate { BeginDmNoteImport(importRow); }, 13f, 8f);
            HemiKit.Size(import.gameObject, 104f, HemiTheme.Row(32f));

            if (string.IsNullOrEmpty(dmNoteResult.Message))
            {
                HemiRows.NoteRow(content, HemiLang.Get("KVE_NOTE_DM_NOTE"));
                return;
            }

            TextMeshProUGUI report = HemiKit.Text(
                "DmNoteStatus",
                content,
                dmNoteResult.Message,
                12f,
                dmNoteResult.Success ? colors.Muted : colors.Negative,
                false,
                TextAlignmentOptions.Left,
                true);
            HemiKit.Size(report.gameObject, -1f, HemiTheme.Row(40f), 1f);
        }

        private static KeyViewerDmNote.Result dmNoteResult;

        private void BeginDmNoteImport(RectTransform anchor)
        {
            string path = HemiFilePicker.Pick(
                HemiLang.Get("KVE_DM_NOTE_FILTER"), KeyViewerDmNote.FileExtensions, HemiLang.Get("KVE_PICK_DM_NOTE"));
            if (string.IsNullOrEmpty(path))
                return;

            if (!KeyViewerDmNote.TryRead(path, out KeyViewerDmNote.Preset preset, out string error))
            {
                dmNoteResult = new KeyViewerDmNote.Result { Success = false, Message = error };
                RebuildPanel();
                return;
            }

            if (preset.Tabs.Count == 1)
            {
                ApplyDmNote(preset, preset.Tabs[0].Id);
                return;
            }

            string[] options = new string[preset.Tabs.Count];
            int selected = 0;
            for (int i = 0; i < preset.Tabs.Count; i++)
            {
                KeyViewerDmNote.TabInfo tab = preset.Tabs[i];
                options[i] = HemiLang.Get("KVE_DM_NOTE_TAB", tab.Name, tab.Keys);
                if (string.Equals(tab.Id, preset.PreferredTab, StringComparison.Ordinal))
                    selected = i;
            }

            HemiPopup.Open(anchor, options, selected, index =>
            {
                if (index >= 0 && index < preset.Tabs.Count)
                    ApplyDmNote(preset, preset.Tabs[index].Id);
            });
        }

        private void ApplyDmNote(KeyViewerDmNote.Preset preset, string tab)
        {
            dmNoteResult = KeyViewerDmNote.Apply(preset, tab);
            canvas.Select(-1);
            RebuildAll();
        }

        private void ProfileButton(RectTransform row, string label, Func<bool> action, Color background, Color text, bool rebuildCanvas, string name = null)
        {
            Button button = HemiKit.Button(name ?? label, row, label, background, text, delegate
            {
                action();
                if (rebuildCanvas)
                {
                    canvas.Select(-1);
                    RebuildAll();
                    return;
                }
                RebuildPanel();
            }, 12f, 8f);
            HemiKit.Size(button.gameObject, 62f, HemiTheme.Row(30f), 1f);
        }

        private static KeyViewerKeyConfig Live(int index, KeyViewerKeyConfig fallback)
        {
            return KeyViewer.GetKey(index) ?? fallback;
        }
    }

    internal static class HemiKeyViewerPage
    {
        internal static void Build(RectTransform area)
        {
            HemiKeyViewerEditor.Create(area);
        }

        internal static void Reset()
        {
        }
    }
}
