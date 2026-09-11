using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiRows
    {
        private static readonly char[] PathSeparators = { '\\', '/' };
        private static readonly string[] NumberFormats = { "F0", "F1", "F2", "F3", "F4" };

        private static RectTransform groupParent;
        private static RectTransform group;
        private static int groupRows;

        private static RectTransform GroupFor(RectTransform parent)
        {
            if (group != null && groupParent == parent && group.parent == parent
                && group.GetSiblingIndex() == parent.childCount - 1)
                return group;

            HemiColors colors = HemiTheme.Colors;
            RectTransform box = HemiKit.VBox("Group", parent, 0f, new RectOffset(0, 0, 0, 0), false);
            Image background = box.gameObject.AddComponent<Image>();
            background.color = colors.Card;
            background.sprite = HemiSprites.Rounded(Mathf.RoundToInt(HemiTheme.CardRadius));
            background.type = Image.Type.Sliced;
            HemiKit.Border(box, colors.Border, HemiTheme.CardRadius);

            groupParent = parent;
            group = box;
            groupRows = 0;
            return box;
        }

        private static void EndGroup()
        {
            groupParent = null;
            group = null;
            groupRows = 0;
        }

        private static void AddHairline(RectTransform box)
        {
            RectTransform holder = HemiKit.Rect("Hairline", box);
            HemiKit.Size(holder.gameObject, -1f, 1f, 1f);
            Image line = HemiKit.Panel("Line", holder, HemiTheme.Colors.Line, 0f);
            line.raycastTarget = false;
            HemiKit.Stretch(line.rectTransform, 14f, 14f, 0f, 0f);
        }

        internal static void Heading(RectTransform parent, string text)
        {
            EndGroup();
            TextMeshProUGUI heading = HemiKit.Text(
                "Heading", parent, text, 11.5f, HemiTheme.Colors.Faint, true,
                TextAlignmentOptions.BottomLeft, true);
            heading.characterSpacing = 4f;
            heading.overflowMode = TextOverflowModes.Overflow;
            LayoutElement layout = HemiKit.Size(heading.gameObject, -1f, -1f, 1f);
            layout.minHeight = HemiTheme.Row(24f);
        }

        internal static void NoteRow(RectTransform parent, string text)
            => Note(parent, "Note", text, HemiTheme.Colors.Muted);

        internal static void ErrorRow(RectTransform parent, string text)
            => Note(parent, "ErrorNote", text, HemiTheme.Colors.Negative);

        private static void Note(RectTransform parent, string name, string text, Color ink)
        {
            EndGroup();
            TextMeshProUGUI note = HemiKit.Text(
                name, parent, text, 13f, ink, false, TextAlignmentOptions.TopLeft, true);
            note.overflowMode = TextOverflowModes.Overflow;
            LayoutElement layout = HemiKit.Size(note.gameObject, -1f, -1f, 1f);
            layout.minHeight = HemiTheme.Row(38f);
        }

        internal static RectTransform Card(RectTransform parent, float height = HemiTheme.RowHeight)
        {
            RectTransform box = GroupFor(parent);
            if (groupRows > 0)
                AddHairline(box);
            groupRows++;

            RectTransform row = HemiKit.Rect("Row", box);
            HemiAdaptiveRow rowLayout = row.gameObject.AddComponent<HemiAdaptiveRow>();
            rowLayout.Spacing = HemiTheme.Gap;
            rowLayout.VerticalGap = HemiTheme.Row(6f);
            rowLayout.BaseHeight = HemiTheme.Row(height);
            int verticalPadding = Mathf.RoundToInt(HemiTheme.Row(4f));
            rowLayout.padding = new RectOffset(14, 14, verticalPadding, verticalPadding);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            LayoutElement layout = HemiKit.Size(row.gameObject, -1f, -1f, 1f);
            layout.minHeight = HemiTheme.Row(height);
            return row;
        }

        internal static void Label(RectTransform row, string label, string hint = null, float width = 160f, float flexible = 1f)
        {
            HemiColors colors = HemiTheme.Colors;

            width = HemiTheme.Col(width);

            RectTransform column = HemiKit.Rect("Label", row);
            TextMeshProUGUI title = HemiKit.Text(
                "Title", column, label, 13.5f, colors.Text, false, TextAlignmentOptions.Left, true);
            title.overflowMode = TextOverflowModes.Overflow;

            TextMeshProUGUI detail = null;
            if (!string.IsNullOrEmpty(hint))
            {
                detail = HemiKit.Text(
                    "Hint", column, hint, 11f, colors.Faint, false, TextAlignmentOptions.Left, true);
                detail.overflowMode = TextOverflowModes.Overflow;
            }

            HemiRowLabel adaptive = column.gameObject.AddComponent<HemiRowLabel>();
            adaptive.Initialize(
                title,
                detail,
                width,
                flexible,
                HemiTheme.Row(string.IsNullOrEmpty(hint) ? 30f : 36f));
        }

        internal static void ToggleRow(RectTransform parent, string label, string hint, Func<bool> getter, Action<bool> setter)
        {
            RectTransform row = Card(parent);
            Label(row, label, hint);
            HemiKit.Switch(row, getter, setter);
        }

        internal static void DropdownRow(
            RectTransform parent,
            string label,
            IReadOnlyList<string> options,
            int selectedIndex,
            Action<int> setter)
        {
            HemiColors colors = HemiTheme.Colors;

            RectTransform row = Card(parent);
            Label(row, label);

            int selected = selectedIndex;

            string current = selected >= 0 && selected < options.Count ? options[selected] : "";

            Image chip = HemiKit.Panel("PopUp", row, colors.Button, 7f);
            LayoutElement chipLayout = HemiKit.Size(chip.gameObject, HemiTheme.Col(150f), HemiTheme.Row(24f));
            HemiKit.Border(chip.transform, colors.Border, 7f);

            TextMeshProUGUI valueText = HemiKit.Text("Value", chip.transform, current, 12.5f, colors.Text);
            HemiKit.Stretch(valueText.rectTransform, 10f, 24f, 0f, 0f);
            float contentWidth = Mathf.Ceil(valueText.GetPreferredValues(current).x) + 40f;
            chipLayout.minWidth = Mathf.Min(chipLayout.preferredWidth, Mathf.Max(HemiTheme.Col(72f), contentWidth));

            TextMeshProUGUI arrow = HemiKit.Text("Arrow", chip.transform, "▼", 8f, colors.Faint, false, TextAlignmentOptions.Center);
            RectTransform arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20f, 0f);
            arrowRect.anchoredPosition = new Vector2(-2f, 0f);

            Button button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chip;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate
            {
                HemiPopup.Open(row, options, selected, index =>
                {
                    if (index >= 0 && index < options.Count)
                    {
                        selected = index;
                        if (valueText != null)
                            valueText.text = options[index];
                    }

                    setter(index);
                });
                arrow.text = "▲";
                HemiPopup.OnClosed = delegate { if (arrow != null) arrow.text = "▼"; };
            });

            chip.gameObject.AddComponent<HemiHover>().Initialize(chip, colors.Button);
        }

        internal static HemiSliderHandle SliderRow(
            RectTransform parent,
            string label,
            float value,
            float minimum,
            float maximum,
            bool wholeNumbers,
            Action<float> setter,
            string hint = null,
            int decimals = 2,
            string unit = null,
            bool live = true)
        {
            RectTransform row = Card(parent);
            Label(row, label, hint, 160f, 0f);

            RectTransform spacer = HemiKit.Rect("Spacer", row);
            HemiKit.Size(spacer.gameObject, 0f, 0f, 1f);

            return SliderField(row, value, minimum, maximum, wholeNumbers, wholeNumbers ? 0 : decimals, unit, setter, live, trackWidth: 220f);
        }

        internal static HemiSliderHandle SliderField(
            RectTransform row,
            float value,
            float minimum,
            float maximum,
            bool wholeNumbers,
            int decimals,
            string unit,
            Action<float> setter,
            bool live = true,
            float height = 30f,
            float valueWidth = 62f,
            float trackWidth = -1f)
        {
            HemiColors colors = HemiTheme.Colors;
            float trackHeight = HemiTheme.Row(height);
            bool hasUnit = !string.IsNullOrEmpty(unit);

            Image track = HemiKit.Panel("Slider", row, new Color(0f, 0f, 0f, 0.001f), 0f);
            LayoutElement trackLayout;
            if (trackWidth > 0f)
                trackLayout = HemiKit.Size(track.gameObject, HemiTheme.Col(trackWidth), trackHeight);
            else
                trackLayout = HemiKit.Size(track.gameObject, 120f, trackHeight, 1f);
            trackLayout.minWidth = 48f;
            RectTransform trackRect = track.rectTransform;

            float railHeight = 4f;
            Image rail = HemiKit.Panel("Rail", trackRect, HemiTweaksMod.IsDarkMode
                ? new Color(1f, 1f, 1f, 0.20f)
                : new Color(0f, 0f, 0f, 0.14f), 2f);
            CenterBand(rail.rectTransform, railHeight);
            rail.raycastTarget = false;

            RectTransform fillArea = HemiKit.Rect("FillArea", trackRect);
            CenterBand(fillArea, railHeight);
            Image fill = HemiKit.Panel("Fill", fillArea, colors.Accent, 2f);
            HemiKit.Stretch(fill.rectTransform);
            fill.raycastTarget = false;

            float knobSize = Mathf.Min(trackHeight, HemiTheme.Row(16f));
            RectTransform handleArea = HemiKit.Rect("HandleArea", trackRect);
            HemiKit.Stretch(handleArea, knobSize * 0.5f, knobSize * 0.5f, 0f, 0f);
            Image knob = HemiKit.Circle("Knob", handleArea, Color.white);
            knob.raycastTarget = false;
            RectTransform knobRect = knob.rectTransform;
            knobRect.sizeDelta = new Vector2(knobSize, knobSize);

            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = knobRect;
            slider.targetGraphic = track;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.wholeNumbers = wholeNumbers;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, minimum, maximum));

            RectTransform valueCell = HemiKit.Rect("ValueCell", row);
            float unitWidth = 0f;

            if (hasUnit)
            {
                TextMeshProUGUI unitText = HemiKit.Text("Unit", valueCell, unit, 12f, colors.Faint, false, TextAlignmentOptions.Right);
                unitWidth = Mathf.Ceil(unitText.GetPreferredValues(unit).x) + 4f;
                RectTransform unitRect = unitText.rectTransform;
                unitRect.anchorMin = new Vector2(1f, 0f);
                unitRect.anchorMax = new Vector2(1f, 1f);
                unitRect.pivot = new Vector2(1f, 0.5f);
                unitRect.sizeDelta = new Vector2(unitWidth, 0f);
                unitRect.anchoredPosition = Vector2.zero;
            }

            string displayed = Display(slider.value, decimals);
            TMP_InputField field = ValueField(valueCell, displayed, unitWidth, decimals);
            float numberWidth = Mathf.Max(
                valueWidth,
                PreferredNumberWidth(field.textComponent, displayed, minimum, maximum, decimals) + 8f);
            float valueCellWidth = numberWidth + unitWidth;
            LayoutElement valueLayout = HemiKit.Size(valueCell.gameObject, valueCellWidth, trackHeight);
            valueLayout.minWidth = valueCellWidth;

            slider.onValueChanged.AddListener(delegate (float next)
            {
                if (field != null)
                    field.SetTextWithoutNotify(Display(next, decimals));
                if (live)
                    setter(next);
            });

            track.gameObject.AddComponent<HemiSliderCommit>().Initialize(slider, setter);

            field.onEndEdit.AddListener(delegate (string text)
            {
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                    || float.IsNaN(parsed) || float.IsInfinity(parsed))
                {
                    field.SetTextWithoutNotify(Display(slider.value, decimals));
                    return;
                }

                float next = parsed;
                slider.SetValueWithoutNotify(next);
                field.SetTextWithoutNotify(Display(next, decimals));
                setter(next);
            });

            return new HemiSliderHandle { Slider = slider, Field = field, Decimals = decimals };
        }

        private static void CenterBand(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, -height * 0.5f);
            rect.offsetMax = new Vector2(0f, height * 0.5f);
        }

        private static TMP_InputField ValueField(
            RectTransform parent, string value, float rightInset, int decimals)
        {
            HemiColors colors = HemiTheme.Colors;

            Image hit = HemiKit.Panel("Value", parent, new Color(0f, 0f, 0f, 0.001f), 0f);
            RectTransform rect = hit.rectTransform;
            HemiKit.Stretch(rect, 0f, rightInset, 0f, 0f);

            RectTransform area = HemiKit.Rect("Area", rect);
            HemiKit.Stretch(area, 2f, 2f, 2f, 2f);
            RectMask2D mask = area.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(-2f, -2f, -2f, -2f);

            TextMeshProUGUI text = HemiKit.Text("Text", area, value, 12.5f, colors.Muted, false, TextAlignmentOptions.Right);
            text.overflowMode = TextOverflowModes.Overflow;
            HemiKit.Stretch(text.rectTransform);
            HemiKit.ShowMarkupLiterally(text);

            hit.gameObject.SetActive(false);
            TMP_InputField input = hit.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.text = value;
            input.transition = Selectable.Transition.None;
            input.contentType = decimals > 0
                ? TMP_InputField.ContentType.DecimalNumber
                : TMP_InputField.ContentType.IntegerNumber;
            input.customCaretColor = true;
            input.caretColor = colors.Accent;
            input.selectionColor = colors.AccentSoft;
            hit.gameObject.SetActive(true);
            return input;
        }

        private static float PreferredNumberWidth(
            TMP_Text text,
            string current,
            float minimum,
            float maximum,
            int decimals)
        {
            float width = text.GetPreferredValues(current ?? "").x;
            width = Mathf.Max(width, text.GetPreferredValues(Display(minimum, decimals)).x);
            width = Mathf.Max(width, text.GetPreferredValues(Display(maximum, decimals)).x);
            return float.IsNaN(width) || float.IsInfinity(width) ? 62f : Mathf.Ceil(width);
        }

        internal static void TextRow(
            RectTransform parent,
            string label,
            string value,
            string placeholder,
            Action<string> setter,
            string hint = null,
            float width = 260f,
            bool secret = false)
        {
            RectTransform row = Card(parent);
            Label(row, label, hint, 150f);

            TMP_InputField field = HemiKit.Field(row, value, placeholder, width, secret);
            HemiKit.Size(field.gameObject, HemiTheme.Col(width), HemiTheme.Row(34f), 1f);
            field.onEndEdit.AddListener(delegate (string text) { setter(text ?? ""); });
        }

        internal static void TextAreaRow(
            RectTransform parent,
            string label,
            string value,
            string placeholder,
            Action<string> setter,
            string hint = null,
            float width = 380f)
        {
            RectTransform row = Card(parent, 126f);
            Label(row, label, hint, 150f, 0f);

            TMP_InputField field = HemiKit.Field(row, value, placeholder, width);
            HemiKit.Size(field.gameObject, HemiTheme.Col(width), HemiTheme.Row(108f), 1f);
            field.lineType = TMP_InputField.LineType.MultiLineNewline;
            field.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            field.textComponent.alignment = TextAlignmentOptions.TopLeft;
            field.verticalScrollbar = null;
            field.onEndEdit.AddListener(delegate (string text) { setter(text ?? ""); });
        }

        internal const float RangeRowHeight = 44f;

        internal static void RangeRow(
            RectTransform parent,
            string label,
            IReadOnlyList<string> names,
            IReadOnlyList<string> colors,
            int minIndex,
            int maxIndex,
            Action<int, int> setter,
            Action<int, int> commit = null)
        {
            if (names == null || names.Count == 0)
                return;

            RectTransform row = Card(parent, RangeRowHeight);
            Label(row, label, null, 130f, 0f);
            HemiRangeBar.Create(row, names, colors, minIndex, maxIndex, setter, commit);
        }

        internal static void ActionRow(
            RectTransform parent,
            string label,
            string value,
            string actionLabel,
            Action action,
            string hint = null,
            Color? actionColor = null)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = Card(parent);
            Label(row, label, hint, 150f);

            if (value != null)
            {
                TextMeshProUGUI valueText = HemiKit.Text(
                    "Value", row, value, 12.5f, colors.Muted, false, TextAlignmentOptions.Right, true);
                valueText.overflowMode = TextOverflowModes.Overflow;
                LayoutElement valueLayout = HemiKit.Size(valueText.gameObject, 150f, -1f, 1f);
                valueLayout.minHeight = HemiTheme.Row(30f);
            }

            Color background = actionColor ?? colors.Button;
            Color ink = background.a < 0.5f ? colors.Text : Color.white;
            Button button = HemiKit.Button(
                "Action", row, actionLabel, background, ink, action, 12.5f, 7f);
            HemiKit.Size(button.gameObject, HemiTheme.Col(110f), HemiTheme.Row(26f));
        }

        internal static void ColorRow(RectTransform parent, string label, Color value, Action<Color> setter, string hint = null)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = Card(parent);
            Label(row, label, hint, 150f);

            TextMeshProUGUI hex = HemiKit.Text(
                "Hex", row, "#" + ColorUtility.ToHtmlStringRGBA(value), 12.5f, colors.Muted, false, TextAlignmentOptions.Right);
            HemiKit.Size(hex.gameObject, 120f, HemiTheme.Row(30f), 1f);

            Image plate = HemiKit.Panel("Plate", row, new Color(1f, 1f, 1f, 0.14f), 6f);
            HemiKit.Size(plate.gameObject, HemiTheme.Col(44f), HemiTheme.Row(24f));

            Image swatch = HemiKit.Panel("Swatch", plate.rectTransform, value, 6f);
            HemiKit.Stretch(swatch.rectTransform, 2f, 2f, 2f, 2f);
            swatch.raycastTarget = false;

            Button button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate
            {
                Color live = swatch.color;
                HemiPopup.OpenContent(plate.rectTransform, 268f, HemiTheme.Row(536f), body =>
                {
                    BuildColorPicker(body, live, next =>
                    {
                        if (swatch != null)
                            swatch.color = next;
                        if (hex != null)
                            hex.text = "#" + ColorUtility.ToHtmlStringRGBA(next);
                        setter(next);
                    });
                });
            });
        }

        internal static void KeyRow(
            RectTransform parent,
            string label,
            KeyCode key,
            bool capturing,
            Action begin,
            string hint = null)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = Card(parent);
            Label(row, label, hint, 150f);

            Button button = HemiKit.Button(
                "Key",
                row,
                capturing ? HemiLang.Get("ROW_PRESS_KEY") : key.ToString(),
                capturing ? colors.Accent : colors.Button,
                capturing ? Color.white : colors.Text,
                begin,
                12.5f,
                7f);
            HemiKit.Size(button.gameObject, HemiTheme.Col(130f), HemiTheme.Row(26f));
        }

        internal static void FileRow(
            RectTransform parent,
            string label,
            string path,
            string filterName,
            string[] extensions,
            string title,
            Action<string> setter,
            string hint = null)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = Card(parent);
            Label(row, label, hint, 150f);

            TextMeshProUGUI value = HemiKit.Text(
                "Value", row, ShortPath(path), 13f, colors.Muted, false, TextAlignmentOptions.Right);
            HemiKit.Size(value.gameObject, 150f, HemiTheme.Row(30f), 1f);

            Button browse = HemiKit.Button("Browse", row, HemiLang.Get("ROW_BROWSE"), colors.Accent, Color.white, delegate
            {
                string picked = HemiFilePicker.Pick(filterName, extensions, title);
                if (!string.IsNullOrEmpty(picked))
                    setter(picked);
            }, 12.5f, 7f);
            HemiKit.Size(browse.gameObject, HemiTheme.Col(92f), HemiTheme.Row(26f));

            if (string.IsNullOrEmpty(path))
                return;

            Button clear = HemiKit.Button("Clear", row, "X", colors.Field, colors.Muted,
                delegate { setter(""); }, 12.5f, 7f);
            HemiKit.Size(clear.gameObject, HemiTheme.Col(34f), HemiTheme.Row(26f));
        }

        internal static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return HemiLang.Get("ROW_NOT_SET");
            int slash = path.LastIndexOfAny(PathSeparators);
            return slash >= 0 && slash < path.Length - 1 ? path.Substring(slash + 1) : path;
        }

        internal static string Display(float value, int decimals)
        {
            decimals = Mathf.Clamp(decimals, 0, 4);
            return value.ToString(NumberFormats[decimals], CultureInfo.InvariantCulture);
        }

        private static readonly string[] Palette =
        {
            "FFFFFF", "D9D9D9", "A6A6A6", "737373", "404040", "262626", "121212", "000000",
            "FF4D4D", "FF9233", "FFD633", "8FE04D", "4DD6C1", "4DA6FF", "8C6BFF", "FF5CC8",
            "E62E2E", "E67320", "E6B800", "5FBF2E", "25B3A0", "1F7FE6", "6B47D9", "E63BA6",
            "7F1414", "8C4410", "8C6B00", "2E7314", "0F6B5F", "0F4C8C", "3D1F99", "8C1F66"
        };

        private const int PaletteColumns = 8;
        private const float SwatchSize = 26f;
        private const float SwatchGap = 4f;

        private const float WheelSize = 176f;

        private const float RingThickness = 0.22f;

        private static void BuildColorPicker(RectTransform body, Color start, Action<Color> onChange)
        {
            Color live = start;
            HemiColors colors = HemiTheme.Colors;

            Color.RGBToHSV(live, out float hue, out float saturation, out float value);

            RectTransform column = HemiKit.VBox("Picker", body, 6f, new RectOffset(0, 0, 0, 0), false);
            HemiKit.Stretch(column);

            RectTransform wheelRow = HemiKit.Rect("Wheel", column);
            HemiKit.Size(wheelRow.gameObject, -1f, WheelSize, 1f);

            RectTransform ring = HemiKit.Rect("Ring", wheelRow);
            ring.anchorMin = new Vector2(0.5f, 0.5f);
            ring.anchorMax = new Vector2(0.5f, 0.5f);
            ring.pivot = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = new Vector2(WheelSize, WheelSize);

            RawImage ringImage = ring.gameObject.AddComponent<RawImage>();
            ringImage.texture = HemiSprites.HueRing(192, RingThickness);

            RectTransform ringHandle = Handle(ring);

            float inner = WheelSize * (1f - RingThickness);
            float squareSide = Mathf.Floor(inner / Mathf.Sqrt(2f)) - 6f;

            RectTransform square = HemiKit.Rect("Square", wheelRow);
            square.anchorMin = new Vector2(0.5f, 0.5f);
            square.anchorMax = new Vector2(0.5f, 0.5f);
            square.pivot = new Vector2(0.5f, 0.5f);
            square.sizeDelta = new Vector2(squareSide, squareSide);

            RawImage squareImage = square.gameObject.AddComponent<RawImage>();
            squareImage.texture = HemiSprites.SaturationValue();

            RectTransform squareHandle = Handle(square);

            RectTransform hexRow = HemiKit.HBox("Hex", column, 8f, new RectOffset(0, 0, 0, 0));
            HemiKit.Size(hexRow.gameObject, -1f, HemiTheme.Row(34f), 1f);

            TextMeshProUGUI hexLabel = HemiKit.Text("Name", hexRow, HemiLang.Get("ROW_HEX"), 13f, colors.Muted, true);
            HemiKit.Size(hexLabel.gameObject, 30f, HemiTheme.Row(26f));

            TMP_InputField hexField = HemiKit.Field(hexRow, ToHex(live), "#FFFFFFFF", 150f);
            HemiKit.Size(hexField.gameObject, HemiTheme.Col(150f), HemiTheme.Row(30f), 1f);

            HemiSliderHandle[] channels = new HemiSliderHandle[4];

            Action<bool> push = null;

            channels[0] = Channel(column, "R", live.r, next => { live.r = next; push(false); });
            channels[1] = Channel(column, "G", live.g, next => { live.g = next; push(false); });
            channels[2] = Channel(column, "B", live.b, next => { live.b = next; push(false); });
            channels[3] = Channel(column, "A", live.a, next => { live.a = next; push(false); });

            BuildPalette(column, picked =>
            {
                live = new Color(picked.r, picked.g, picked.b, live.a);
                Color.RGBToHSV(live, out hue, out saturation, out value);
                push(true);
            });

            push = delegate (bool refreshChannels)
            {
                if (hexField != null)
                    hexField.SetTextWithoutNotify(ToHex(live));

                if (squareImage != null)
                    squareImage.color = Color.HSVToRGB(hue, 1f, 1f);

                PlaceRingHandle(ringHandle, hue);
                PlaceSquareHandle(squareHandle, saturation, value);

                if (refreshChannels)
                {
                    channels[0].SetValue(live.r);
                    channels[1].SetValue(live.g);
                    channels[2].SetValue(live.b);
                    channels[3].SetValue(live.a);
                }

                onChange(live);
            };

            ring.gameObject.AddComponent<HemiColorPad>().Initialize(ring, normalised =>
            {
                Vector2 fromCentre = normalised - new Vector2(0.5f, 0.5f);
                if (fromCentre.sqrMagnitude < 0.0001f)
                    return;

                hue = Mathf.Repeat(Mathf.Atan2(fromCentre.x, fromCentre.y) / (Mathf.PI * 2f), 1f);
                live = Color.HSVToRGB(hue, saturation, value).WithAlpha(live.a);
                push(true);
            });

            square.gameObject.AddComponent<HemiColorPad>().Initialize(square, normalised =>
            {
                saturation = Mathf.Clamp01(normalised.x);
                value = Mathf.Clamp01(normalised.y);
                live = Color.HSVToRGB(hue, saturation, value).WithAlpha(live.a);
                push(true);
            });

            push(true);

            hexField.onEndEdit.AddListener(delegate (string text)
            {
                if (TryParseHex(text, out Color parsed))
                {
                    live = parsed;
                    Color.RGBToHSV(live, out hue, out saturation, out value);
                    push(true);
                    return;
                }

                hexField.SetTextWithoutNotify(ToHex(live));
            });
        }

        private static string ToHex(Color value)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(value);
        }

        private static RectTransform Handle(RectTransform parent)
        {
            Image outer = HemiKit.Circle("Handle", parent, new Color(0f, 0f, 0f, 0.85f));
            outer.raycastTarget = false;
            RectTransform rect = outer.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(14f, 14f);

            Image inner = HemiKit.Circle("Inner", rect, Color.white);
            inner.raycastTarget = false;
            HemiKit.Stretch(inner.rectTransform, 3f, 3f, 3f, 3f);
            return rect;
        }

        private static void PlaceRingHandle(RectTransform handle, float hue)
        {
            if (handle == null)
                return;

            float radius = WheelSize * 0.5f * (1f - RingThickness * 0.5f);
            float angle = hue * Mathf.PI * 2f;
            handle.anchoredPosition = new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
        }

        private static void PlaceSquareHandle(RectTransform handle, float saturation, float value)
        {
            if (handle == null)
                return;

            Vector2 size = handle.parent is RectTransform parent ? parent.rect.size : Vector2.zero;
            handle.anchoredPosition = new Vector2(
                (saturation - 0.5f) * size.x,
                (value - 0.5f) * size.y);
        }

        private static bool TryParseHex(string text, out Color result)
        {
            result = Color.white;
            if (string.IsNullOrEmpty(text))
                return false;

            string trimmed = text.Trim();
            if (!trimmed.StartsWith("#", StringComparison.Ordinal))
                trimmed = "#" + trimmed;

            return ColorUtility.TryParseHtmlString(trimmed, out result);
        }

        private static void BuildPalette(RectTransform parent, Action<Color> pick)
        {
            int rows = Mathf.CeilToInt(Palette.Length / (float)PaletteColumns);
            RectTransform grid = HemiKit.Grid(
                "Palette", parent, new Vector2(SwatchSize, SwatchSize), SwatchGap, new RectOffset(0, 0, 2, 0));
            HemiKit.Size(grid.gameObject, -1f, rows * SwatchSize + (rows - 1) * SwatchGap + 2f, 1f);

            for (int i = 0; i < Palette.Length; i++)
            {
                if (!ColorUtility.TryParseHtmlString("#" + Palette[i], out Color swatchColor))
                    continue;

                Image swatch = HemiKit.Panel("Swatch", grid, swatchColor, 5f);
                Color captured = swatchColor;

                Button button = swatch.gameObject.AddComponent<Button>();
                button.targetGraphic = swatch;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(delegate { pick(captured); });

                swatch.gameObject.AddComponent<HemiHover>().Initialize(swatch, swatchColor);
            }
        }

        private static HemiSliderHandle Channel(RectTransform parent, string name, float value, Action<float> setter)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = HemiKit.HBox(name, parent, 8f, new RectOffset(0, 0, 0, 0));
            HemiKit.Size(row.gameObject, -1f, HemiTheme.Row(30f), 1f);

            TextMeshProUGUI label = HemiKit.Text("Name", row, name, 13f, colors.Muted, true);
            HemiKit.Size(label.gameObject, 16f, HemiTheme.Row(26f));

            TMP_InputField field = HemiKit.Field(row, Byte(value), "", 46f);
            HemiKit.Size(field.gameObject, HemiTheme.Col(46f), HemiTheme.Row(28f));
            field.contentType = TMP_InputField.ContentType.IntegerNumber;

            Image track = HemiKit.Panel("Track", row, colors.Field, 3f);
            HemiKit.Size(track.gameObject, 120f, 6f, 1f);

            RectTransform fillArea = HemiKit.Rect("FillArea", track.rectTransform);
            HemiKit.Stretch(fillArea);
            Image fill = HemiKit.Panel("Fill", fillArea, colors.Accent, 3f);
            HemiKit.Stretch(fill.rectTransform);
            fill.raycastTarget = false;

            RectTransform handleArea = HemiKit.Rect("HandleArea", track.rectTransform);
            HemiKit.Stretch(handleArea);
            Image handle = HemiKit.Circle("Handle", handleArea, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(14f, 14f);

            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));

            slider.onValueChanged.AddListener(delegate (float next)
            {
                if (field != null)
                    field.SetTextWithoutNotify(Byte(next));
                setter(next);
            });

            field.onEndEdit.AddListener(delegate (string text)
            {
                float next;
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int typed))
                    next = Mathf.Clamp01(typed / 255f);
                else
                    next = slider.value;

                field.SetTextWithoutNotify(Byte(next));
                slider.SetValueWithoutNotify(next);
                setter(next);
            });

            return new HemiSliderHandle { Slider = slider, Field = field, Decimals = 0, Scale = 255f };
        }

        private static string Byte(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 255f).ToString(CultureInfo.InvariantCulture);
        }
    }

    internal struct HemiSliderHandle
    {
        public Slider Slider;
        public TMP_InputField Field;
        public int Decimals;

        public float Scale;

        public void SetValue(float value)
        {
            if (Slider != null)
                Slider.SetValueWithoutNotify(value);
            if (Field != null)
                Field.SetTextWithoutNotify(HemiRows.Display(value * (Scale <= 0f ? 1f : Scale), Decimals));
        }
    }

    internal static class HemiFilePicker
    {
        internal static string Pick(string filterName, string[] extensions, string title)
        {
            try
            {
                return UnityFileDialog.FileBrowser.PickFile(null, filterName, extensions, title);
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Warning("Could not open the file picker: " + exception.Message);
                return null;
            }
        }

        internal static string Save(string defaultFileName, string filterName, string[] extensions, string title)
        {
            try
            {
                return UnityFileDialog.FileBrowser.SaveFile(null, defaultFileName, filterName, extensions, title);
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Warning("Could not open the save dialog: " + exception.Message);
                return null;
            }
        }
    }

    internal sealed class HemiAdaptiveRow : LayoutGroup
    {
        internal float Spacing { get; set; }
        internal float VerticalGap { get; set; }
        internal float BaseHeight { get; set; }

        private int labelIndex = -1;
        private HemiRowLabel labelElement;
        private bool stacked;
        private bool controlsWrapped;
        private readonly List<float> lineMinimums = new List<float>();
        private readonly List<float> linePreferred = new List<float>();
        private readonly List<float> lineFlexible = new List<float>();

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float minimum = padding.horizontal;
            float preferred = padding.horizontal;
            int count = 0;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                minimum += LayoutUtility.GetMinWidth(child);
                preferred += LayoutUtility.GetPreferredWidth(child);
                count++;
            }
            if (count > 1)
            {
                minimum += Spacing * (count - 1);
                preferred += Spacing * (count - 1);
            }
            SetLayoutInputForAxis(0f, Mathf.Max(minimum, preferred), 1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            PrepareMode();
            float preferred;
            if (controlsWrapped)
            {
                preferred = padding.vertical;
                int visible = 0;
                if (labelIndex >= 0)
                {
                    preferred += ChildHeight(rectChildren[labelIndex]);
                    visible++;
                }
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    if (i == labelIndex || IsSpacer(rectChildren[i]))
                        continue;
                    preferred += ChildHeight(rectChildren[i]);
                    visible++;
                }
                if (visible > 1)
                    preferred += VerticalGap * (visible - 1);
            }
            else if (stacked)
            {
                preferred = padding.vertical + ChildHeight(rectChildren[labelIndex]) + VerticalGap;
                preferred += ControlLineHeight();
            }
            else
            {
                preferred = padding.vertical + LineHeight(-1);
            }

            preferred = Mathf.Max(BaseHeight, preferred);
            SetLayoutInputForAxis(BaseHeight, preferred, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            PrepareMode();
            float left = padding.left;
            float width = Mathf.Max(0f, rectTransform.rect.width - padding.horizontal);

            if (controlsWrapped)
            {
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    RectTransform child = rectChildren[i];
                    if (IsSpacer(child))
                        SetChildAlongAxis(child, 0, left, 0f);
                    else
                        SetChildAlongAxis(child, 0, left, width);
                }
                RefreshLabelMeasurement();
                return;
            }

            if (stacked)
            {
                SetChildAlongAxis(rectChildren[labelIndex], 0, left, width);
                LayoutLine(left, width, labelIndex);
                RefreshLabelMeasurement();
                return;
            }

            LayoutLine(left, width, -1);
            RefreshLabelMeasurement();
        }

        public override void SetLayoutVertical()
        {
            PrepareMode();
            float available = Mathf.Max(0f, rectTransform.rect.height - padding.vertical);
            float top = padding.top;

            if (controlsWrapped)
            {
                if (labelIndex >= 0)
                {
                    float height = Mathf.Min(available, ChildHeight(rectChildren[labelIndex]));
                    SetChildAlongAxis(rectChildren[labelIndex], 1, top, height);
                    top += height + VerticalGap;
                }

                for (int i = 0; i < rectChildren.Count; i++)
                {
                    RectTransform child = rectChildren[i];
                    if (i == labelIndex)
                        continue;
                    if (IsSpacer(child))
                    {
                        SetChildAlongAxis(child, 1, top, 0f);
                        continue;
                    }
                    float height = ChildHeight(child);
                    SetChildAlongAxis(child, 1, top, height);
                    top += height + VerticalGap;
                }
                return;
            }

            if (stacked)
            {
                float labelHeight = ChildHeight(rectChildren[labelIndex]);
                SetChildAlongAxis(rectChildren[labelIndex], 1, top, labelHeight);
                float controlTop = top + labelHeight + VerticalGap;
                float controlHeight = ControlLineHeight();
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    if (i == labelIndex)
                        continue;
                    RectTransform child = rectChildren[i];
                    float height = Mathf.Min(controlHeight, ChildHeight(child));
                    SetChildAlongAxis(child, 1, controlTop + (controlHeight - height) * 0.5f, height);
                }
                return;
            }

            float lineHeight = LineHeight(-1);
            float lineTop = top + Mathf.Max(0f, (available - lineHeight) * 0.5f);
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float height = Mathf.Min(lineHeight, ChildHeight(child));
                SetChildAlongAxis(child, 1, lineTop + (lineHeight - height) * 0.5f, height);
            }
        }

        private void PrepareMode()
        {
            labelIndex = -1;
            labelElement = null;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                HemiRowLabel found = rectChildren[i].GetComponent<HemiRowLabel>();
                if (found != null)
                {
                    labelIndex = i;
                    labelElement = found;
                    break;
                }
            }

            float width = Mathf.Max(0f, rectTransform.rect.width - padding.horizontal);
            stacked = labelIndex >= 0 && rectChildren.Count > 1 && LineWidth(-1, false) > width;
            if (stacked)
                controlsWrapped = LineWidth(labelIndex, true) > width;
            else
                controlsWrapped = labelIndex < 0 && LineWidth(-1, false) > width;
        }

        private void RefreshLabelMeasurement()
        {
            if (labelIndex < 0)
                return;
            if (labelElement != null)
                labelElement.CalculateLayoutInputVertical();
        }

        private void LayoutLine(float left, float width, int excluded)
        {
            int count = ChildCount(excluded);
            if (count == 0)
                return;

            float spacing = Spacing * Mathf.Max(0, count - 1);
            float available = Mathf.Max(0f, width - spacing);
            float totalMin = 0f;
            float totalPreferred = 0f;
            float totalFlexible = 0f;
            lineMinimums.Clear();
            linePreferred.Clear();
            lineFlexible.Clear();
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (i == excluded)
                    continue;
                RectTransform child = rectChildren[i];
                float minimum = LayoutUtility.GetMinWidth(child);
                float preferred = Mathf.Max(minimum, LayoutUtility.GetPreferredWidth(child));
                float flexible = Mathf.Max(0f, LayoutUtility.GetFlexibleWidth(child));
                lineMinimums.Add(minimum);
                linePreferred.Add(preferred);
                lineFlexible.Add(flexible);
                totalMin += minimum;
                totalPreferred += preferred;
                totalFlexible += flexible;
            }

            float interpolate = totalPreferred > totalMin
                ? Mathf.Clamp01((available - totalMin) / (totalPreferred - totalMin))
                : 0f;
            float surplus = Mathf.Max(0f, available - totalPreferred);
            float position = left;
            int slot = 0;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (i == excluded)
                    continue;
                RectTransform child = rectChildren[i];
                float size = Mathf.Lerp(lineMinimums[slot], linePreferred[slot], interpolate);
                if (surplus > 0f && totalFlexible > 0f)
                    size += surplus * lineFlexible[slot] / totalFlexible;
                SetChildAlongAxis(child, 0, position, size);
                position += size + Spacing;
                slot++;
            }
        }

        private float LineWidth(int excluded, bool minimum)
        {
            float total = 0f;
            int count = 0;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (i == excluded)
                    continue;
                RectTransform child = rectChildren[i];
                if (minimum)
                {
                    if (!IsSpacer(child))
                        total += LayoutUtility.GetMinWidth(child);
                }
                else
                {
                    total += LayoutUtility.GetPreferredWidth(child);
                }
                count++;
            }
            return total + Spacing * Mathf.Max(0, count - 1);
        }

        private int ChildCount(int excluded)
        {
            return rectChildren.Count - (excluded >= 0 ? 1 : 0);
        }

        private float LineHeight(int excluded)
        {
            float height = 0f;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (i != excluded)
                    height = Mathf.Max(height, ChildHeight(rectChildren[i]));
            }
            return height;
        }

        private float ControlLineHeight()
        {
            return LineHeight(labelIndex);
        }

        private static float ChildHeight(RectTransform child)
        {
            return Mathf.Max(LayoutUtility.GetMinHeight(child), LayoutUtility.GetPreferredHeight(child));
        }

        private static bool IsSpacer(RectTransform child)
        {
            return LayoutUtility.GetPreferredWidth(child) <= 0f && LayoutUtility.GetPreferredHeight(child) <= 0f;
        }
    }

    internal sealed class HemiRowLabel : MonoBehaviour, ILayoutElement, ILayoutController
    {
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private float requestedWidth;
        private float requestedFlexible;
        private float minimumHeight;
        private float titleHeight;
        private float detailHeight;

        internal void Initialize(
            TextMeshProUGUI titleText,
            TextMeshProUGUI detailText,
            float width,
            float flexible,
            float baseHeight)
        {
            title = titleText;
            detail = detailText;
            requestedWidth = width;
            requestedFlexible = flexible;
            minimumHeight = baseHeight;
            CalculateLayoutInputVertical();
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
            float width = Mathf.Max(1f, ((RectTransform)transform).rect.width);
            titleHeight = PreferredHeight(title, width);
            detailHeight = PreferredHeight(detail, width);
        }

        public void SetLayoutHorizontal()
        {
            SetHorizontal(title);
            SetHorizontal(detail);
        }

        public void SetLayoutVertical()
        {
            float gap = detail == null ? 0f : HemiTheme.Row(2f);
            SetVertical(title, 0f, titleHeight);
            SetVertical(detail, titleHeight + gap, detailHeight);
        }

        public float minWidth => 0f;
        public float preferredWidth => requestedWidth;
        public float flexibleWidth => requestedFlexible;
        public float minHeight => minimumHeight;
        public float preferredHeight => Mathf.Max(minimumHeight,
            titleHeight + (detail == null ? 0f : HemiTheme.Row(2f) + detailHeight));
        public float flexibleHeight => 0f;
        public int layoutPriority => 2;

        private static float PreferredHeight(TextMeshProUGUI text, float width)
        {
            if (text == null)
                return 0f;
            float height = text.GetPreferredValues(text.text, width, float.PositiveInfinity).y;
            return float.IsNaN(height) || float.IsInfinity(height) ? HemiTheme.Row(20f) : Mathf.Max(1f, height);
        }

        private static void SetHorizontal(TextMeshProUGUI text)
        {
            if (text == null)
                return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }

        private static void SetVertical(TextMeshProUGUI text, float top, float height)
        {
            if (text == null)
                return;
            RectTransform rect = text.rectTransform;
            rect.offsetMin = new Vector2(rect.offsetMin.x, -top - height);
            rect.offsetMax = new Vector2(rect.offsetMax.x, -top);
        }
    }

    internal sealed class HemiRangeBar : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler
    {
        private const float HandleWidth = 7f;

        private RectTransform track;
        private RectTransform minHandle;
        private RectTransform maxHandle;
        private Image leftDim;
        private Image rightDim;
        private TextMeshProUGUI readout;

        private IReadOnlyList<string> names;
        private Action<int, int> setter;

        private Action<int, int> commit;
        private int minIndex;
        private int maxIndex;

        private bool movingMax;

        internal static HemiRangeBar Create(
            RectTransform parent,
            IReadOnlyList<string> names,
            IReadOnlyList<string> colors,
            int minIndex,
            int maxIndex,
            Action<int, int> setter,
            Action<int, int> commit = null)
        {
            TextMeshProUGUI readout = HemiKit.Text(
                "Range", parent, "", 13f, HemiTheme.Colors.Text, true, TextAlignmentOptions.Left, true);
            readout.overflowMode = TextOverflowModes.Overflow;
            LayoutElement readoutLayout = HemiKit.Size(
                readout.gameObject, HemiTheme.Col(118f), -1f);
            readoutLayout.minHeight = HemiTheme.Row(30f);

            RectTransform host = HemiKit.Rect("RangeBar", parent);
            LayoutElement hostLayout = HemiKit.Size(host.gameObject, 200f, HemiTheme.Row(34f), 1f);
            hostLayout.minWidth = 48f;

            HemiRangeBar bar = host.gameObject.AddComponent<HemiRangeBar>();
            bar.readout = readout;
            bar.names = names;
            bar.setter = setter;
            bar.commit = commit;
            bar.minIndex = Mathf.Clamp(minIndex, 0, names.Count - 1);
            bar.maxIndex = Mathf.Clamp(maxIndex, 0, names.Count - 1);

            Image surface = HemiKit.Panel("Surface", host, new Color(0f, 0f, 0f, 0.001f), 0f);
            HemiKit.Stretch(surface.rectTransform);

            Image rail = HemiKit.Panel("Track", host, Color.white, 4f);
            rail.sprite = HemiSprites.Gradient(colors);
            rail.type = Image.Type.Simple;
            rail.raycastTarget = false;
            bar.track = rail.rectTransform;
            bar.track.anchorMin = new Vector2(0f, 0.5f);
            bar.track.anchorMax = new Vector2(1f, 0.5f);
            bar.track.pivot = new Vector2(0.5f, 0.5f);
            bar.track.offsetMin = new Vector2(0f, -11f);
            bar.track.offsetMax = new Vector2(0f, 11f);

            bar.leftDim = Dim(bar.track);
            bar.rightDim = Dim(bar.track);

            bar.minHandle = Handle(bar.track);
            bar.maxHandle = Handle(bar.track);

            bar.Apply();
            float longest = 0f;
            for (int i = 0; i < names.Count; i++)
                longest = Mathf.Max(longest, readout.GetPreferredValues(names[i]).x);
            float separator = readout.GetPreferredValues(" – ").x;
            readoutLayout.minWidth = Mathf.Ceil(longest * 2f + separator + 4f);
            return bar;
        }

        private static Image Dim(RectTransform parent)
        {
            Image image = HemiKit.Panel("Dim", parent, new Color(0f, 0f, 0f, 0.6f), 4f);
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            Span(rect, 0f, 1f);
            return image;
        }

        private static RectTransform Handle(RectTransform parent)
        {
            RectTransform holder = HemiKit.Rect("Handle", parent);
            holder.pivot = new Vector2(0.5f, 0.5f);
            Place(holder, 0f);

            Image bar = HemiKit.Panel("Bar", holder, Color.white, 3f);
            HemiKit.Stretch(bar.rectTransform);
            bar.raycastTarget = false;

            return holder;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!TryIndex(eventData, out int index))
                return;

            movingMax = Mathf.Abs(index - maxIndex) < Mathf.Abs(index - minIndex);
            Move(index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            commit?.Invoke(minIndex, maxIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TryIndex(eventData, out int index))
                Move(index);
        }

        private bool TryIndex(PointerEventData eventData, out int index)
        {
            index = 0;
            if (track == null || names == null || names.Count == 0)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    track, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return false;
            }

            float width = track.rect.width;
            if (width <= 0.01f)
                return false;

            float t = Mathf.Clamp01((local.x + width * 0.5f) / width);
            index = Mathf.RoundToInt(t * (names.Count - 1));
            return true;
        }

        private void Move(int index)
        {
            index = Mathf.Clamp(index, 0, names.Count - 1);

            if (movingMax)
            {
                maxIndex = index;
                if (minIndex > maxIndex)
                    minIndex = maxIndex;
            }
            else
            {
                minIndex = index;
                if (maxIndex < minIndex)
                    maxIndex = minIndex;
            }

            Apply();
            setter?.Invoke(minIndex, maxIndex);
        }

        private void Apply()
        {
            if (track == null || names == null || names.Count == 0)
                return;

            int last = Mathf.Max(1, names.Count - 1);
            float minT = (float)minIndex / last;
            float maxT = (float)maxIndex / last;

            Place(minHandle, minT);
            Place(maxHandle, maxT);

            Span(leftDim.rectTransform, 0f, minT);
            Span(rightDim.rectTransform, maxT, 1f);

            if (readout != null)
                readout.text = names[minIndex] + " – " + names[maxIndex];
        }

        private static void Place(RectTransform rect, float t)
        {
            rect.anchorMin = new Vector2(t, 0f);
            rect.anchorMax = new Vector2(t, 1f);
            rect.offsetMin = new Vector2(-HandleWidth * 0.5f, -3f);
            rect.offsetMax = new Vector2(HandleWidth * 0.5f, 3f);
        }

        private static void Span(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal sealed class HemiColorPad : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform area;
        private Action<Vector2> report;

        internal void Initialize(RectTransform rect, Action<Vector2> onMoved)
        {
            area = rect;
            report = onMoved;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Send(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Send(eventData);
        }

        private void Send(PointerEventData eventData)
        {
            if (area == null || report == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    area, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Rect rect = area.rect;
            if (rect.width <= 0.0001f || rect.height <= 0.0001f)
                return;

            report(new Vector2(
                (local.x - rect.xMin) / rect.width,
                (local.y - rect.yMin) / rect.height));
        }
    }
}
