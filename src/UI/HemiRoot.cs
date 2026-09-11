using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal enum HemiScreen
    {
        Main,
        Features,
        Preference,

        Tuf
    }

    internal enum HemiTransition
    {
        None,

        Fade,

        Pop,

        Forward,

        Back
    }

    internal sealed class HemiRoot : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasGroup rootGroup;
        private Image backdrop;
        private RectTransform contentLayer;

        private HemiScreen screen = HemiScreen.Main;
        private bool visible;

        private GameObject currentPage;

        private HemiWindowChrome chrome;

        private bool chromeIsStale;

        private bool rebuildQueued;
        private HemiTransition queuedTransition;

        internal static HemiRoot Instance { get; private set; }

        internal Canvas Canvas => canvas;

        internal HemiScreen Screen => screen;


        internal static bool AnimationsEnabled => HemiTweaksMod.TransitionAnimationsEnabled;

        internal static HemiRoot Create()
        {
            GameObject host = new GameObject("HemiTweaks.Interface");
            DontDestroyOnLoad(host);
            HemiRoot root = host.AddComponent<HemiRoot>();
            root.Build();
            return root;
        }

        internal bool IsVisible => visible;

        internal void Toggle()
        {
            SetVisible(!visible);
        }

        internal void SetVisible(bool value)
        {
            if (visible == value)
                return;

            visible = value;
            HemiPopup.CloseAll();
            if (!value)
                HemiTweaksMod.CancelAllCaptures();

            if (value)
            {
                screen = HemiScreen.Main;
                canvas.enabled = true;
                BuildScreen(HemiTransition.None);
                HemiTween.To(this, "show", rootGroup.alpha, 1f, 0.18f, HemiEase.OutQuad, Apply);
                return;
            }

            HemiTween.To(this, "show", rootGroup.alpha, 0f, 0.14f, HemiEase.OutQuad, Apply, delegate
            {
                if (!visible && canvas != null)
                    canvas.enabled = false;
            });

            Cursor.lockState = CursorLockMode.None;
            HandCursorBackToGame();
        }

        private static MethodInfo gameCursorRule;
        private static bool gameCursorRuleResolved;

        private static void HandCursorBackToGame()
        {
            if (!gameCursorRuleResolved)
            {
                gameCursorRuleResolved = true;
                gameCursorRule = typeof(PauseMenu).GetMethod(
                    "UpdateCursorVisibility", BindingFlags.NonPublic | BindingFlags.Static);
                if (gameCursorRule == null)
                    MelonLoader.MelonLogger.Warning(
                        "PauseMenu.UpdateCursorVisibility was not found; the cursor stays visible after the interface closes.");
            }

            if (gameCursorRule == null)
                return;

            try
            {
                gameCursorRule.Invoke(null, null);
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Warning(
                    "Could not hand the cursor back to the game: " + exception.Message);
            }
        }

        internal void OpenAt(HemiScreen target)
        {
            if (visible)
            {
                Go(target);
                return;
            }

            SetVisible(true);
            Go(target);
        }

        internal void Go(HemiScreen target)
        {
            if (screen == target)
                return;

            HemiTransition transition;
            if (screen == HemiScreen.Main)
                transition = HemiTransition.Pop;
            else if (target == HemiScreen.Main)
                transition = HemiTransition.Fade;
            else
                transition = target > screen ? HemiTransition.Forward : HemiTransition.Back;

            screen = target;
            HemiPopup.CloseAll();
            BuildScreen(transition);
        }

        internal void Refresh(HemiTransition transition = HemiTransition.None)
        {
            rebuildQueued = true;
            if (transition != HemiTransition.None)
                queuedTransition = transition;
        }

        internal void RefreshShell(HemiTransition transition = HemiTransition.None)
        {
            chromeIsStale = true;
            Refresh(transition);
        }

        private void Apply(float value)
        {
            if (rootGroup == null)
                return;
            rootGroup.alpha = value;
            rootGroup.interactable = value > 0.5f;
            rootGroup.blocksRaycasts = value > 0.05f;
            if (backdrop != null)
            {
                Color color = HemiTheme.Colors.Backdrop;
                backdrop.color = new Color(color.r, color.g, color.b, color.a * value);
            }
        }

        private void Update()
        {
            HemiTween.Tick(Time.unscaledDeltaTime);

            if (visible)
            {
                if (!Cursor.visible)
                    Cursor.visible = true;
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;
            }

            if (rebuildQueued)
            {
                rebuildQueued = false;
                HemiTransition transition = queuedTransition;
                queuedTransition = HemiTransition.None;
                if (visible)
                    BuildScreen(transition);
            }

            if (!visible)
                return;

            if (screen == HemiScreen.Tuf)
                HemiTufScreen.Tick();

            if (PumpKeyCapture())
                return;

            if (IsTextFieldFocused())
                fieldFocusedFrame = Time.frameCount;

            if (screen == HemiScreen.Features && HemiModifiers.Command && Input.GetKeyDown(KeyCode.F))
                HemiFeaturesScreen.FocusSearch();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (fieldFocusedFrame >= Time.frameCount - 1)
                    return;

                if (HemiPopup.IsOpen)
                {
                    HemiPopup.Close();
                    return;
                }
                if (screen != HemiScreen.Main)
                {
                    if (screen == HemiScreen.Features && HemiFeaturesScreen.TryCloseSettings())
                        return;
                    Go(HemiScreen.Main);
                    return;
                }

                HemiOverlayEditor editor = currentPage == null
                    ? null
                    : currentPage.GetComponentInChildren<HemiOverlayEditor>(true);
                if (editor != null && editor.HasSelection)
                {
                    editor.Select(null);
                    return;
                }

                SetVisible(false);
            }
        }

        internal bool TryCloseWithHotkey()
        {
            if (!visible || IsCapturingKey)
                return false;

            if (captureFinishedFrame == Time.frameCount)
                return false;

            if (IsTextFieldFocused())
                return false;

            SetVisible(false);
            return true;
        }

        private static bool IsTextFieldFocused()
        {
            EventSystem events = EventSystem.current;
            GameObject focused = events == null ? null : events.currentSelectedGameObject;
            TMPro.TMP_InputField field = focused == null ? null : focused.GetComponent<TMPro.TMP_InputField>();
            return field != null && field.isFocused;
        }

        private bool PumpKeyCapture()
        {
            bool wasCapturing = IsCapturingKey;
            if (!wasCapturing)
                return false;

            HemiTweaksMod.CaptureTufKey();
            KeyViewer.CaptureRegistrationInput();
            HemiTweaksMod.CaptureHotkeyForInterface();

            if (wasCapturing != IsCapturingKey)
            {
                captureFinishedFrame = Time.frameCount;
                Refresh();
            }
            return true;
        }

        private int captureFinishedFrame = -1;

        private int fieldFocusedFrame = -1;

        private static bool IsCapturingKey =>
            HemiTweaksMod.IsWaitingForHotkey ||
            HemiTweaksMod.IsCapturingTufKey ||
            KeyViewer.IsRegistering;

        private void OnDestroy()
        {
            HemiPopup.CloseAll();
            HemiTween.Clear();
            HemiKit.PopupLayer = null;
            HemiTheme.Dispose();
            if (Instance == this)
                Instance = null;
        }

        private void Build()
        {
            Instance = this;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            HemiTweaksMod.ApplyDefaultScaling(scaler);
            gameObject.AddComponent<GraphicRaycaster>();

            HemiKit.Stretch(GetComponent<RectTransform>());
            rootGroup = gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;

            backdrop = HemiKit.Panel("Backdrop", transform, HemiTheme.Colors.Backdrop, 0f);
            HemiKit.Stretch(backdrop.rectTransform);

            contentLayer = HemiKit.Rect("Content", transform);
            HemiKit.Stretch(contentLayer);

            RectTransform popupLayer = HemiKit.Rect("Popups", transform);
            HemiKit.Stretch(popupLayer);
            HemiKit.PopupLayer = popupLayer;

            canvas.enabled = false;
        }

        private const float SlideDistance = 40f;
        private const float SlideDuration = 0.35f;

        private void BuildScreen(HemiTransition transition)
        {
            if (!AnimationsEnabled)
                transition = HemiTransition.None;


            GameObject previousPage = currentPage;
            bool chromeIsNew = false;
            RectTransform pageParent;

            if (screen == HemiScreen.Main)
            {
                if (chrome != null)
                {
                    chrome.Destroy(transition != HemiTransition.None);
                    chrome = null;
                    previousPage = null;
                }
                pageParent = contentLayer;
            }
            else
            {
                if (chrome != null && chromeIsStale)
                {
                    chrome.Destroy(false);
                    chrome = null;
                    previousPage = null;
                }
                chromeIsStale = false;

                if (chrome == null)
                {
                    chrome = HemiWindowChrome.Create(contentLayer);
                    chromeIsNew = true;
                }
                chrome.SetActiveTab(screen);
                pageParent = chrome.Body;
            }

            if (transition == HemiTransition.None && previousPage != null)
                HemiScrollTop.Carry(previousPage);
            else
                HemiScrollTop.DropCarry();

            currentPage = HemiKit.Obj("Page_" + screen, pageParent);
            RectTransform rect = currentPage.GetComponent<RectTransform>();
            HemiKit.Stretch(rect);

            try
            {
                switch (screen)
                {
                    case HemiScreen.Features:
                        HemiFeaturesScreen.Build(rect);
                        break;
                    case HemiScreen.Preference:
                        HemiPreferenceScreen.Build(rect);
                        break;
                    case HemiScreen.Tuf:
                        HemiTufScreen.Build(rect);
                        break;
                    default:
                        HemiMainScreen.Build(rect);
                        break;
                }
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Error("Interface screen '" + screen + "' failed to build: " + exception);
            }


            HemiScrollTop.DropCarry();

            if (chromeIsNew && transition == HemiTransition.Pop)
                chrome.Pop();

            if (transition == HemiTransition.None)
            {
                if (previousPage != null)
                {
                    previousPage.SetActive(false);
                    Destroy(previousPage);
                }
                return;
            }

            FadeIn(currentPage, rect, transition);
            FadeOut(previousPage, transition);
        }

        private static void FadeIn(GameObject screenObject, RectTransform rect, HemiTransition transition)
        {
            CanvasGroup group = screenObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            HemiTween.To(screenObject, "in", 0f, 1f, 0.20f, HemiEase.OutQuad, value =>
            {
                if (group != null)
                    group.alpha = value;
            });

            float from = SlideOffset(transition);
            if (Mathf.Approximately(from, 0f))
                return;

            HemiTween.To(rect, "slide", from, 0f, SlideDuration, HemiEase.Spring, value =>
            {
                if (rect != null)
                    rect.anchoredPosition = new Vector2(value, 0f);
            });
        }

        internal static void FadeOutAndDestroy(GameObject target, string channel, float duration)
        {
            if (target == null)
                return;

            CanvasGroup group = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            HemiTween.To(target, channel, group.alpha, 0f, duration, HemiEase.OutQuad,
                value =>
                {
                    if (group != null)
                        group.alpha = value;
                },
                delegate
                {
                    if (target != null)
                    {
                        target.SetActive(false);
                        Destroy(target);
                    }
                });
        }

        private static void FadeOut(GameObject previous, HemiTransition transition)
        {
            if (previous == null)
                return;

            FadeOutAndDestroy(previous, "out", 0.16f);

            float to = -SlideOffset(transition);
            if (Mathf.Approximately(to, 0f))
                return;

            RectTransform rect = previous.GetComponent<RectTransform>();
            HemiTween.To(rect, "slide", rect.anchoredPosition.x, to, SlideDuration, HemiEase.Spring, value =>
            {
                if (rect != null)
                    rect.anchoredPosition = new Vector2(value, 0f);
            });
        }

        private static float SlideOffset(HemiTransition transition)
        {
            switch (transition)
            {
                case HemiTransition.Forward:
                    return SlideDistance;
                case HemiTransition.Back:
                    return -SlideDistance;
                default:
                    return 0f;
            }
        }
    }

    internal sealed class HemiWindowChrome
    {
        internal const float Width = 1280f;
        internal const float Height = 700f;
        internal const float Radius = 22f;

        private static float HeaderHeight => HemiTheme.Row(52f);

        private readonly List<Tab> tabs = new List<Tab>();
        private RectTransform windowRect;

        internal RectTransform Root { get; private set; }

        internal RectTransform Body { get; private set; }

        internal static HemiWindowChrome Create(RectTransform parent)
        {
            HemiColors colors = HemiTheme.Colors;
            Color edge = HemiTweaksMod.IsDarkMode ? new Color(1f, 1f, 1f, 0.10f) : new Color(0f, 0f, 0f, 0.10f);
            HemiWindowChrome chrome = new HemiWindowChrome();

            chrome.Root = HemiKit.Rect("Chrome", parent);
            HemiKit.Stretch(chrome.Root);

            Image shadow = HemiKit.Panel("Shadow", chrome.Root, new Color(0f, 0f, 0f, 0.45f), 0f);
            shadow.sprite = HemiSprites.Glow();
            shadow.raycastTarget = false;
            Centre(shadow.rectTransform, Width + 110f, Height + 110f);

            RectTransform frame = HemiKit.Rect("Frame", chrome.Root);
            chrome.windowRect = frame;
            Centre(frame, Width, Height);

            Color plateColor = colors.Window;
            plateColor.a = HemiTweaksMod.IsDarkMode ? 0.94f : 0.96f;

            Image window = HemiKit.Panel("Window", frame, plateColor, Radius);
            RectTransform plate = window.rectTransform;
            HemiKit.Stretch(plate);

            Mask mask = window.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            HemiKit.Border(plate, edge, Radius);

            RectTransform header = HemiKit.Rect("Header", plate);
            TopStrip(header, 0f, -HeaderHeight);

            Image hairline = HemiKit.Panel("Hairline", plate, colors.Line, 0f);
            RectTransform hairRect = hairline.rectTransform;
            hairline.raycastTarget = false;
            TopStrip(hairRect, -HeaderHeight, -HeaderHeight - 1f);

            RectTransform logo = HemiIcons.DrawChromeLogo(header, 22f);
            logo.anchorMin = new Vector2(0f, 0.5f);
            logo.anchorMax = new Vector2(0f, 0.5f);
            logo.pivot = new Vector2(0.5f, 0.5f);
            logo.anchoredPosition = new Vector2(31f, 0f);

            TextMeshProUGUI title = HemiKit.Text("Title", header, BuildInfo.Name, 14f, colors.Text, true);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(54f, 0f);
            titleRect.sizeDelta = new Vector2(HemiTheme.Col(220f), HemiTheme.Row(22f));

            chrome.BuildTabBar(header);

            Image closePlate = HemiKit.Panel("Close", header, colors.Card, 14f);
            HemiKit.Border(closePlate.transform, edge, 14f);
            RectTransform closeRect = closePlate.rectTransform;
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-14f, 0f);
            closeRect.sizeDelta = new Vector2(28f, 28f);
            HemiIcons.DrawCross(closeRect, colors.Muted, 10f);

            Button closeButton = closePlate.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closePlate;
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(delegate { HemiRoot.Instance?.Go(HemiScreen.Main); });
            closePlate.gameObject.AddComponent<HemiHover>().Initialize(closePlate, colors.Card);

            chrome.Body = HemiKit.Rect("Body", plate);
            chrome.Body.anchorMin = Vector2.zero;
            chrome.Body.anchorMax = Vector2.one;
            chrome.Body.offsetMin = Vector2.zero;
            chrome.Body.offsetMax = new Vector2(0f, -HeaderHeight);

            return chrome;
        }

        private void BuildTabBar(RectTransform header)
        {
            HemiColors colors = HemiTheme.Colors;

            int count = HemiTweaksMod.TufEnabled ? 3 : 2;
            float segment = HemiTheme.Col(92f);
            float width = count * segment + (count - 1) * 2f + 6f;
            float height = HemiTheme.Row(30f);

            Image bar = HemiKit.Panel("Tabs", header, colors.Field, 11f);
            HemiKit.Border(bar.transform, colors.Line, 11f);
            RectTransform barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(width, height);

            RectTransform row = HemiKit.HBox("Segments", barRect, 2f, new RectOffset(3, 3, 3, 3));
            HemiKit.Stretch(row);

            AddTab(row, HemiLang.Get("UI_TAB_FEATURES"), HemiScreen.Features, segment, "FEATURES");
            AddTab(row, HemiLang.Get("UI_TAB_PREFERENCE"), HemiScreen.Preference, segment, "PREFERENCE");
            if (HemiTweaksMod.TufEnabled)
                AddTab(row, HemiLang.Get("FEATURE_TUF"), HemiScreen.Tuf, segment, "TUF");
        }

        internal void SetActiveTab(HemiScreen screen)
        {
            HemiColors colors = HemiTheme.Colors;
            for (int i = 0; i < tabs.Count; i++)
            {
                Tab tab = tabs[i];
                bool selected = tab.Target == screen;
                Color background = selected ? colors.Chip : new Color(0f, 0f, 0f, 0f);

                tab.Background.color = background;
                tab.Label.color = selected ? colors.Text : colors.Muted;
                tab.Hover.Initialize(tab.Background, background);
            }
        }

        internal void Pop()
        {
            if (!HemiRoot.AnimationsEnabled || windowRect == null)
                return;

            RectTransform rect = windowRect;
            HemiTween.To(rect, "pop", 0.96f, 1f, 0.35f, HemiEase.SpringSoft, value =>
            {
                if (rect != null)
                    rect.localScale = new Vector3(value, value, 1f);
            });

            GameObject root = Root == null ? null : Root.gameObject;
            if (root != null)
            {
                CanvasGroup group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
                HemiTween.To(root, "pop-alpha", 0f, 1f, 0.18f, HemiEase.OutQuad, value =>
                {
                    if (group != null)
                        group.alpha = value;
                });
            }
        }

        internal void Destroy(bool animate)
        {
            if (Root == null)
                return;

            GameObject root = Root.gameObject;
            RectTransform window = windowRect;
            Root = null;
            Body = null;
            windowRect = null;
            tabs.Clear();

            if (!animate || !HemiRoot.AnimationsEnabled)
            {
                root.SetActive(false);
                UnityEngine.Object.Destroy(root);
                return;
            }

            HemiTween.Kill(root, "pop-alpha");

            if (window != null)
            {
                HemiTween.To(window, "pop", window.localScale.x, 0.96f, 0.25f, HemiEase.Spring, value =>
                {
                    if (window != null)
                        window.localScale = new Vector3(value, value, 1f);
                });
            }

            HemiRoot.FadeOutAndDestroy(root, "out", 0.18f);
        }

        private void AddTab(RectTransform parent, string label, HemiScreen target, float width, string name = null)
        {
            HemiColors colors = HemiTheme.Colors;
            Image background = HemiKit.Panel(name ?? label, parent, new Color(0f, 0f, 0f, 0f), 8f);
            HemiKit.Size(background.gameObject, width, -1f, 0f, 1f);

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate { HemiRoot.Instance?.Go(target); });

            TextMeshProUGUI text = HemiKit.Text("Label", background.transform, label, 13f, colors.Muted, true, TextAlignmentOptions.Center);
            HemiKit.Stretch(text.rectTransform, 6f, 6f, 1f, 1f);

            HemiHover hover = background.gameObject.AddComponent<HemiHover>();
            hover.Initialize(background, background.color);

            tabs.Add(new Tab { Target = target, Background = background, Label = text, Hover = hover });
        }

        private static void TopStrip(RectTransform rect, float top, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(0f, top);
        }

        private static void Centre(RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private struct Tab
        {
            public HemiScreen Target;
            public Image Background;
            public TextMeshProUGUI Label;
            public HemiHover Hover;
        }
    }
}
