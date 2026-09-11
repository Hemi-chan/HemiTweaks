using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiKit
    {
        internal static RectTransform PopupLayer;

        internal static GameObject Obj(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return obj;
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            return Obj(name, parent).GetComponent<RectTransform>();
        }

        internal static Image Panel(string name, Transform parent, Color color, float radius = HemiTheme.Radius)
        {
            Image image = Obj(name, parent).AddComponent<Image>();
            image.color = color;
            if (radius > 0f)
            {
                image.sprite = HemiSprites.Rounded(Mathf.RoundToInt(radius));
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        internal static Image Circle(string name, Transform parent, Color color)
        {
            Image image = Obj(name, parent).AddComponent<Image>();
            image.sprite = HemiSprites.Circle();
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        internal static Image Border(Transform parent, Color color, float radius, int thickness = 1)
        {
            Image outline = Panel("Border", parent, color, 0f);
            outline.sprite = HemiSprites.RoundedOutline(Mathf.RoundToInt(radius), thickness);
            outline.type = Image.Type.Sliced;
            outline.raycastTarget = false;
            Stretch(outline.rectTransform);

            LayoutElement layout = outline.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return outline;
        }

        internal static TextMeshProUGUI Text(
            string name,
            Transform parent,
            string value,
            float size,
            Color color,
            bool boldFont = false,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            bool wrap = false)
        {
            TextMeshProUGUI text = Obj(name, parent).AddComponent<TextMeshProUGUI>();
            text.font = boldFont ? HemiTheme.Bold : HemiTheme.Regular;

            if (boldFont)
                text.fontStyle = FontStyles.Bold;
            text.text = value ?? "";
            text.fontSize = HemiTheme.Font(size);
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        internal static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static RectTransform VBox(string name, Transform parent, float spacing, RectOffset padding, bool fit = true)
        {
            RectTransform rect = Rect(name, parent);
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (fit)
            {
                ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return rect;
        }

        internal static RectTransform HBox(string name, Transform parent, float spacing, RectOffset padding)
        {
            RectTransform rect = Rect(name, parent);
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        internal static RectTransform Grid(string name, Transform parent, Vector2 cell, float spacing, RectOffset padding)
        {
            RectTransform rect = Rect(name, parent);
            GridLayoutGroup layout = rect.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cell;
            layout.spacing = new Vector2(spacing, spacing);
            layout.padding = padding;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.Flexible;

            return rect;
        }

        internal static LayoutElement Size(GameObject obj, float width = -1f, float height = -1f, float flexWidth = -1f, float flexHeight = -1f)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                layout.preferredWidth = width;
                layout.minWidth = 0f;
            }
            if (height >= 0f)
            {
                layout.preferredHeight = height;
                layout.minHeight = height;
            }
            if (flexWidth >= 0f)
                layout.flexibleWidth = flexWidth;
            if (flexHeight >= 0f)
                layout.flexibleHeight = flexHeight;
            return layout;
        }

        internal static Button Button(
            string name,
            Transform parent,
            string label,
            Color background,
            Color textColor,
            Action onClick,
            float fontSize = 15f,
            float radius = HemiTheme.Radius)
        {
            Image image = Panel(name, parent, background, radius);

            if (background.a > 0.02f && background.a < 0.5f)
                Border(image.transform, HemiTheme.Colors.Border, radius);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null)
                button.onClick.AddListener(delegate { onClick(); });

            if (!string.IsNullOrEmpty(label))
            {
                TextMeshProUGUI text = Text("Label", image.transform, label, fontSize, textColor, false, TextAlignmentOptions.Center);
                Stretch(text.rectTransform, 8f, 8f, 2f, 2f);
            }

            image.gameObject.AddComponent<HemiHover>().Initialize(image, background);
            return button;
        }

        internal static RectTransform Switch(Transform parent, Func<bool> getter, Action<bool> setter, float width = 54f, float height = 24f)
        {
            HemiColors colors = HemiTheme.Colors;

            width = HemiTheme.Col(width);
            height = HemiTheme.Row(height);

            Color offColor = HemiTweaksMod.IsDarkMode
                ? new Color(1f, 1f, 1f, 0.18f)
                : new Color(0f, 0f, 0f, 0.16f);

            Image track = Panel("Switch", parent, getter() ? colors.Accent : offColor, height * 0.5f);
            LayoutElement switchLayout = Size(track.gameObject, width, height);
            switchLayout.minWidth = width;
            switchLayout.minHeight = height;

            Image knob = Circle("Knob", track.transform, Color.white);
            RectTransform knobRect = knob.rectTransform;
            knobRect.anchorMin = new Vector2(0f, 0.5f);
            knobRect.anchorMax = new Vector2(0f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(height - 4f, height - 4f);
            knob.raycastTarget = false;

            float off = height * 0.5f;
            float on = width - height * 0.5f;

            Action apply = delegate
            {
                if (knobRect == null || track == null)
                    return;

                bool state = getter();

                float duration = HemiRoot.AnimationsEnabled ? 0.25f : 0f;
                HemiTween.To(knobRect, "x", knobRect.anchoredPosition.x, state ? on : off, duration, HemiEase.SpringGentle,
                    value =>
                    {
                        if (knobRect != null)
                            knobRect.anchoredPosition = new Vector2(value, 0f);
                    });
                track.color = state ? colors.Accent : offColor;
            };

            knobRect.anchoredPosition = new Vector2(getter() ? on : off, 0f);

            Button button = track.gameObject.AddComponent<Button>();
            button.targetGraphic = track;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate
            {
                setter(!getter());
                apply();
            });
            return track.rectTransform;
        }

        internal static void ShowMarkupLiterally(TMP_Text text)
        {
            if (text == null)
                return;

            text.richText = false;
            text.parseCtrlCharacters = false;
            text.text = HemiTextSafety.Literal(text.text);
        }

        internal static TMP_InputField Field(Transform parent, string value, string placeholder, float width = 160f, bool secret = false)
        {
            HemiColors colors = HemiTheme.Colors;
            Image background = Panel("Field", parent, colors.Field, 8f);
            Border(background.transform, colors.Border, 8f);

            Size(background.gameObject, HemiTheme.Col(width), HemiTheme.Row(34f));

            RectTransform area = Rect("Area", background.transform);
            Stretch(area, 10f, 10f, 4f, 4f);
            return Attach(background, area, value, placeholder, 14f, colors.Muted, secret);
        }

        internal static TMP_InputField SearchBar(Image bar, string value, string placeholder, float rightInset)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform barRect = bar.rectTransform;
            HemiIcons.DrawMagnifier(barRect, colors.Faint, 13f, 12f);

            RectTransform area = Rect("Area", barRect);
            Stretch(area, 32f, rightInset, 4f, 4f);
            return Attach(bar, area, value, placeholder, 12.5f, colors.Faint, false);
        }

        private static TMP_InputField Attach(
            Image surface,
            RectTransform area,
            string value,
            string placeholder,
            float fontSize,
            Color hintColor,
            bool secret)
        {
            HemiColors colors = HemiTheme.Colors;
            RectMask2D mask = area.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(-2f, -2f, -2f, -2f);

            string shown = secret ? new string('*', Math.Min(value?.Length ?? 0, 64)) : value;
            TextMeshProUGUI text = Text("Text", area, shown, fontSize, colors.Text);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            ShowMarkupLiterally(text);

            TextMeshProUGUI hint = Text("Placeholder", area, placeholder, fontSize, hintColor);
            Stretch(hint.rectTransform);
            hint.raycastTarget = false;

            surface.gameObject.SetActive(false);
            TMP_InputField input = surface.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = hint;
            if (secret)
                input.contentType = TMP_InputField.ContentType.Password;
            input.text = value ?? "";
            input.transition = Selectable.Transition.None;
            input.customCaretColor = true;
            input.caretColor = colors.Accent;
            input.selectionColor = colors.AccentSoft;
            surface.gameObject.SetActive(true);
            return input;
        }

        internal static RectTransform Scroll(Transform parent, out ScrollRect scrollRect)
        {
            RectTransform viewport = Rect("Viewport", parent);
            Stretch(viewport);
            Image raycast = viewport.gameObject.AddComponent<Image>();
            raycast.color = new Color(0f, 0f, 0f, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = VBox("Content", viewport, HemiTheme.Gap, new RectOffset(0, 0, 0, 0));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 42f;

            viewport.gameObject.AddComponent<HemiScrollTop>().Initialize(scrollRect);
            return content;
        }

        internal static void AttachScroll(RectTransform viewport, RectTransform content)
        {
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            Image raycast = viewport.gameObject.AddComponent<Image>();
            raycast.color = new Color(0f, 0f, 0f, 0.001f);
        }

        internal static float PreferredTextHeight(TMP_Text text, string value, float width, float minimum)
        {
            float height = text.GetPreferredValues(value ?? "", width, float.PositiveInfinity).y;
            if (float.IsNaN(height) || float.IsInfinity(height))
                return minimum;
            return Mathf.Max(minimum, Mathf.Ceil(height));
        }
    }

    internal sealed class HemiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Graphic target;
        private Color normal;
        private Color hover;
        private Color pressed;
        private bool inside;
        private bool held;

        internal void Initialize(Graphic graphic, Color baseColor)
        {
            HemiTween.Kill(this, "tint");

            target = graphic;
            normal = baseColor;
            hover = Color.Lerp(baseColor, Color.white, baseColor.a > 0.02f ? 0.14f : 0.10f);
            hover.a = Mathf.Max(baseColor.a, 0.12f);

            pressed = Color.Lerp(baseColor, Color.black, 0.14f);
            pressed.a = Mathf.Max(baseColor.a, 0.16f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            inside = true;
            if (held)
            {
                Press();
                return;
            }
            Fade(hover);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            inside = false;
            Fade(normal);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            held = true;
            Press();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            held = false;
            Fade(inside ? hover : normal);
            Release();
        }

        private void Press()
        {
            if (target == null)
                return;
            HemiTween.Kill(this, "tint");
            target.color = pressed;

            HemiTween.Kill(this, "press");
            RectTransform rect = target.rectTransform;
            if (rect != null)
                rect.localScale = new Vector3(0.97f, 0.97f, 1f);
        }

        private void Release()
        {
            if (target == null)
                return;

            RectTransform rect = target.rectTransform;
            if (rect == null)
                return;

            if (!HemiRoot.AnimationsEnabled)
            {
                rect.localScale = Vector3.one;
                return;
            }

            HemiTween.To(this, "press", rect.localScale.x, 1f, 0.25f, HemiEase.SpringGentle, value =>
            {
                if (rect != null)
                    rect.localScale = new Vector3(value, value, 1f);
            });
        }

        private void Fade(Color to)
        {
            if (target == null)
                return;
            Color from = target.color;
            HemiTween.To(this, "tint", 0f, 1f, 0.12f, HemiEase.OutQuad, t =>
            {
                if (target != null)
                    target.color = Color.LerpUnclamped(from, to, t);
            });
        }

        private void OnDestroy()
        {
            HemiTween.KillAll(this);
        }
    }

    internal sealed class HemiScrollTop : MonoBehaviour
    {
        private const int PinFrames = 6;

        private const int WaitFrames = 120;

        private const int PathDepth = 12;

        private static readonly Dictionary<string, float> carried = new Dictionary<string, float>(StringComparer.Ordinal);

        private ScrollRect scrollRect;
        private float target;
        private float placed;
        private bool hasPlaced;
        private bool finished;
        private int frames;
        private int waits;

        internal static void Carry(GameObject page)
        {
            carried.Clear();
            if (page == null)
                return;

            ScrollRect[] sources = page.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                ScrollRect source = sources[i];
                if (source == null || source.content == null)
                    continue;

                float offset = Mathf.Max(0f, source.content.anchoredPosition.y);
                if (offset > 0f)
                    carried[PathOf(source.transform)] = offset;
            }
        }

        internal static void DropCarry()
        {
            carried.Clear();
        }

        private static string PathOf(Transform transform)
        {
            StringBuilder path = new StringBuilder(64);
            Transform node = transform;
            for (int depth = 0; depth < PathDepth && node != null; depth++)
            {
                if (path.Length > 0)
                    path.Insert(0, '/');
                path.Insert(0, node.name);

                if (node.name.StartsWith("Page_", StringComparison.Ordinal))
                    break;

                node = node.parent;
            }
            return path.ToString();
        }

        internal void Initialize(ScrollRect value)
        {
            scrollRect = value;
            target = 0f;
            if (carried.Count > 0 && carried.TryGetValue(PathOf(value.transform), out float offset))
                target = offset;
        }

        private void LateUpdate()
        {
            if (finished)
                return;

            if (frames > PinFrames)
            {
                finished = true;
                return;
            }

            RectTransform content = scrollRect == null ? null : scrollRect.content;
            if (content == null || content.rect.height <= 0f)
            {
                if (++waits > WaitFrames)
                    finished = true;
                return;
            }

            if (hasPlaced && !Mathf.Approximately(content.anchoredPosition.y, placed))
            {
                finished = true;
                return;
            }

            frames++;

            float span = Mathf.Max(0f, content.rect.height - ViewportHeight());
            Vector2 position = content.anchoredPosition;
            position.y = Mathf.Clamp(target, 0f, span);
            content.anchoredPosition = position;
            placed = position.y;
            hasPlaced = true;
        }

        private float ViewportHeight()
        {
            RectTransform viewport = scrollRect.viewport;
            return viewport == null ? 0f : viewport.rect.height;
        }
    }

    internal sealed class HemiScrollEnd : MonoBehaviour
    {
        private const float Reach = 140f;

        private const float Release = 280f;

        private ScrollRect scrollRect;
        private Action reachedEnd;
        private bool armed = true;

        internal void Initialize(ScrollRect rect, Action onReachedEnd)
        {
            scrollRect = rect;
            reachedEnd = onReachedEnd;
            if (rect != null)
                rect.onValueChanged.AddListener(OnScrolled);
        }

        private void OnScrolled(Vector2 position)
        {
            if (scrollRect == null || reachedEnd == null)
                return;

            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;
            if (content == null || viewport == null)
                return;

            float overflow = content.rect.height - viewport.rect.height;

            if (overflow <= 1f)
                return;

            float remaining = overflow - content.anchoredPosition.y;

            if (!armed)
            {
                if (remaining > Release)
                    armed = true;
                return;
            }

            if (remaining <= Reach)
            {
                armed = false;
                reachedEnd();
            }
        }
    }
}
