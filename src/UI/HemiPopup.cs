using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal static class HemiPopup
    {
        private const float ItemHeight = 24f;
        private const float MinWidth = 150f;
        private const float MaxHeight = 260f;

        private sealed class Entry
        {
            public GameObject Root;
            public Action Closed;
        }

        private static readonly List<Entry> stack = new List<Entry>();

        internal static Action OnClosed
        {
            set
            {
                if (stack.Count > 0)
                    stack[stack.Count - 1].Closed = value;
            }
        }

        internal static bool IsOpen => stack.Count > 0;

        internal static void Open(RectTransform anchor, IReadOnlyList<string> options, int selectedIndex, Action<int> onPick)
        {
            RectTransform layer = HemiKit.PopupLayer;
            if (anchor == null || layer == null || options == null || options.Count == 0)
                return;

            HemiColors colors = HemiTheme.Colors;
            float minWidth = HemiTheme.Col(MinWidth);
            float maximumWidth = Mathf.Max(1f, layer.rect.width - 16f);
            float textInsets = HemiTheme.Col(34f) + 8f;
            float width = Mathf.Min(maximumWidth, Mathf.Max(minWidth, anchor.rect.width * 0.5f));
            float[] rowHeights = new float[options.Count];
            float contentHeight = 0f;

            TextMeshProUGUI measurement = MeasurementText(layer, 12.5f, true);
            try
            {
                float preferredWidth = 0f;
                for (int i = 0; i < options.Count; i++)
                    preferredWidth = Mathf.Max(preferredWidth, measurement.GetPreferredValues(options[i] ?? "").x);
                width = Mathf.Min(maximumWidth, Mathf.Max(width, Mathf.Ceil(preferredWidth) + textInsets));

                float textWidth = Mathf.Max(1f, width - textInsets);
                float minimumRowHeight = HemiTheme.Row(ItemHeight);
                for (int i = 0; i < options.Count; i++)
                {
                    float preferredHeight = measurement.GetPreferredValues(
                        options[i] ?? "", textWidth, float.PositiveInfinity).y;
                    rowHeights[i] = Mathf.Max(minimumRowHeight, Mathf.Ceil(preferredHeight) + HemiTheme.Row(6f));
                    contentHeight += rowHeights[i];
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(measurement.gameObject);
            }

            float maximumHeight = Mathf.Min(HemiTheme.Row(MaxHeight), Mathf.Max(1f, layer.rect.height - 16f));
            float height = Mathf.Min(maximumHeight, contentHeight + 8f);

            RectTransform panel = BeginPanel(anchor, width, height);
            if (panel == null)
                return;

            RectTransform content = ScrollBody(panel, 4f, 0f, out RectTransform viewport);

            if (contentHeight > height - 8f + 0.5f)
                HemiKit.AttachScroll(viewport, content);

            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                bool selected = index == selectedIndex;
                Image item = HemiKit.Panel("Item", content, new Color(0f, 0f, 0f, 0f), 6f);
                HemiKit.Size(item.gameObject, -1f, rowHeights[i], 1f);

                Button itemButton = item.gameObject.AddComponent<Button>();
                itemButton.targetGraphic = item;
                itemButton.transition = Selectable.Transition.None;
                itemButton.onClick.AddListener(delegate
                {
                    Close();
                    onPick?.Invoke(index);
                });
                item.gameObject.AddComponent<HemiHover>().Initialize(item, item.color);

                if (selected)
                {
                    RectTransform tick = HemiKit.Rect("Tick", item.transform);
                    tick.anchorMin = new Vector2(0f, 0.5f);
                    tick.anchorMax = new Vector2(0f, 0.5f);
                    tick.pivot = new Vector2(0.5f, 0.5f);
                    tick.anchoredPosition = new Vector2(HemiTheme.Col(13f), 0f);
                    tick.sizeDelta = new Vector2(HemiTheme.Row(11f), HemiTheme.Row(11f));
                    HemiIcons.DrawCheck(tick, colors.Text, HemiTheme.Row(11f));
                }

                TextMeshProUGUI text = HemiKit.Text(
                    "Text", item.transform, options[index], 12.5f, colors.Text, selected,
                    TextAlignmentOptions.Left, true);
                text.overflowMode = TextOverflowModes.Overflow;
                HemiKit.Stretch(text.rectTransform, HemiTheme.Col(24f), HemiTheme.Col(10f), 0f, 0f);
            }

            FinishPanel(panel);
        }

        internal static void OpenContent(RectTransform anchor, float width, float height, Action<RectTransform> build)
        {
            RectTransform panel = BeginPanel(anchor, width, height);
            if (panel == null)
                return;

            RectTransform body = HemiKit.Rect("Body", panel);
            HemiKit.Stretch(body, 10f, 10f, 10f, 10f);

            try
            {
                build?.Invoke(body);
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Error("Popup content failed to build: " + exception);
            }

            FinishPanel(panel);
        }

        internal static void OpenList(RectTransform anchor, float width, float height, Action<RectTransform> build)
        {
            RectTransform panel = BeginPanel(anchor, width, height);
            if (panel == null)
                return;

            RectTransform content = ScrollBody(panel, 6f, 4f, out RectTransform viewport);
            HemiKit.AttachScroll(viewport, content);

            try
            {
                build?.Invoke(content);
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Error("Popup list failed to build: " + exception);
            }

            FinishPanel(panel);
        }

        internal static void Close()
        {
            if (stack.Count == 0)
                return;

            Entry entry = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            Dispose(entry, true);
        }

        internal static void CloseAll()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
                Dispose(stack[i], false);
            stack.Clear();
        }

        private static void Dispose(Entry entry, bool animate)
        {
            if (entry?.Root == null)
                return;

            GameObject closing = entry.Root;
            entry.Root = null;

            Action closed = entry.Closed;
            entry.Closed = null;

            if (!animate || !HemiRoot.AnimationsEnabled)
            {
                HemiTween.KillAll(closing);
                closing.SetActive(false);
                UnityEngine.Object.Destroy(closing);
                closed?.Invoke();
                return;
            }

            CanvasGroup group = closing.GetComponent<CanvasGroup>() ?? closing.AddComponent<CanvasGroup>();
            group.interactable = false;

            RectTransform panelRect = closing.transform.Find("Panel") as RectTransform;
            if (panelRect != null)
            {
                HemiTween.To(panelRect, "pop", panelRect.localScale.x, 0.97f, 0.18f, HemiEase.OutQuad, value =>
                {
                    if (panelRect != null)
                        panelRect.localScale = new Vector3(value, value, 1f);
                });
            }

            HemiRoot.FadeOutAndDestroy(closing, "fade", 0.18f);
            closed?.Invoke();
        }

        internal static void ConfirmDestructive(RectTransform anchor, string question, string confirmLabel, Action onConfirm)
        {
            RectTransform layer = HemiKit.PopupLayer;
            if (anchor == null || layer == null)
                return;

            question = HemiTextSafety.Literal(question);
            HemiColors colors = HemiTheme.Colors;
            float rowHeight = HemiTheme.Row(28f);
            float width = Mathf.Min(HemiTheme.Col(260f), Mathf.Max(1f, layer.rect.width - 16f));
            float questionWidth = Mathf.Max(1f, width - 24f);
            float naturalQuestionHeight;
            TextMeshProUGUI measurement = MeasurementText(layer, 13f, true);
            HemiKit.ShowMarkupLiterally(measurement);
            try
            {
                naturalQuestionHeight = Mathf.Max(
                    HemiTheme.Row(52f),
                    Mathf.Ceil(measurement.GetPreferredValues(
                        question ?? "", questionWidth, float.PositiveInfinity).y));
            }
            finally
            {
                UnityEngine.Object.Destroy(measurement.gameObject);
            }

            float fixedHeight = rowHeight + 8f + 24f;
            float maximumPanelHeight = Mathf.Max(1f, layer.rect.height - 16f);
            float questionHeight = Mathf.Min(
                naturalQuestionHeight,
                Mathf.Max(1f, maximumPanelHeight - fixedHeight));
            float panelHeight = Mathf.Min(maximumPanelHeight, questionHeight + fixedHeight);

            OpenContent(anchor, width, panelHeight, body =>
            {
                RectTransform column = HemiKit.VBox("Confirm", body, 8f, new RectOffset(0, 0, 0, 0), false);
                HemiKit.Stretch(column, 2f, 2f, 2f, 2f);

                RectTransform questionViewport = HemiKit.Rect("QuestionViewport", column);
                HemiKit.Size(questionViewport.gameObject, -1f, questionHeight, 1f);
                questionViewport.gameObject.AddComponent<RectMask2D>();

                TextMeshProUGUI text = HemiKit.Text(
                    "Question", questionViewport, question, 13f, colors.Text, true, TextAlignmentOptions.TopLeft, true);
                HemiKit.ShowMarkupLiterally(text);
                text.overflowMode = TextOverflowModes.Overflow;
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = new Vector2(0f, 1f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.sizeDelta = new Vector2(0f, naturalQuestionHeight);
                textRect.anchoredPosition = Vector2.zero;
                if (naturalQuestionHeight > questionHeight + 0.5f)
                    HemiKit.AttachScroll(questionViewport, textRect);

                RectTransform buttons = HemiKit.HBox("Buttons", column, 8f, new RectOffset(0, 0, 0, 0));
                HemiKit.Size(buttons.gameObject, -1f, rowHeight, 1f);

                Button cancel = HemiKit.Button("Cancel", buttons, HemiLang.Get("UI_CANCEL"), colors.Button, colors.Text,
                    Close, 12.5f, 8f);
                HemiKit.Size(cancel.gameObject, -1f, rowHeight, 1f);

                Button confirm = HemiKit.Button("Confirm", buttons, confirmLabel, colors.Accent, Color.white, delegate
                {
                    Close();
                    onConfirm?.Invoke();
                }, 12.5f, 8f);
                HemiKit.Size(confirm.gameObject, -1f, rowHeight, 1f);
            });
        }

        private static TextMeshProUGUI MeasurementText(RectTransform layer, float size, bool bold)
        {
            TextMeshProUGUI text = HemiKit.Text(
                "PopupMeasure", layer, "", size, Color.clear, bold, TextAlignmentOptions.Left, true);
            text.enabled = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static RectTransform BeginPanel(RectTransform anchor, float width, float height)
        {
            RectTransform layer = HemiKit.PopupLayer;
            if (layer == null || anchor == null)
                return null;

            HemiColors colors = HemiTheme.Colors;

            GameObject root = HemiKit.Obj("Popup", layer);
            stack.Add(new Entry { Root = root });
            RectTransform rootRect = root.GetComponent<RectTransform>();
            HemiKit.Stretch(rootRect);

            Image catcher = HemiKit.Panel("Catcher", rootRect, new Color(0f, 0f, 0f, 0.001f), 0f);
            HemiKit.Stretch(catcher.rectTransform);
            Button catcherButton = catcher.gameObject.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(Close);

            Vector2 position = AnchorPosition(anchor, layer, new Vector2(width, height));

            Image shadow = HemiKit.Panel("Shadow", rootRect, new Color(0f, 0f, 0f, 0.55f), 0f);
            shadow.sprite = HemiSprites.Glow();
            shadow.raycastTarget = false;
            PlaceFromTopLeft(shadow.rectTransform, new Vector2(width + 56f, height + 56f), position + new Vector2(28f, 12f));

            Image panel = HemiKit.Panel("Panel", rootRect, colors.Header, 12f);
            RectTransform panelRect = panel.rectTransform;
            PlaceFromTopLeft(panelRect, new Vector2(width, height), position);

            HemiKit.Border(panelRect, colors.BorderStrong, 12f);

            return panelRect;
        }

        private static void FinishPanel(RectTransform panelRect)
        {
            GameObject root = stack.Count == 0 ? null : stack[stack.Count - 1].Root;
            if (root == null || panelRect == null || !HemiRoot.AnimationsEnabled)
                return;

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            HemiTween.To(root, "fade", 0f, 1f, 0.12f, HemiEase.OutQuad, value =>
            {
                if (group != null)
                    group.alpha = value;
            });

            HemiTween.To(panelRect, "pop", 0.97f, 1f, 0.25f, HemiEase.Spring, value =>
            {
                if (panelRect != null)
                    panelRect.localScale = new Vector3(value, value, 1f);
            });
        }

        private static void PlaceFromTopLeft(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static RectTransform ScrollBody(RectTransform panel, float inset, float spacing, out RectTransform viewport)
        {
            viewport = HemiKit.Rect("Viewport", panel);
            HemiKit.Stretch(viewport, inset, inset, inset, inset);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = HemiKit.VBox("Items", viewport, spacing, new RectOffset(0, 0, 0, 0));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            return content;
        }

        private static Vector2 AnchorPosition(RectTransform anchor, RectTransform layer, Vector2 size)
        {
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);

            Vector3 bottomRight = corners[3];
            bottomRight.y = corners[0].y;

            Vector2 local = layer.InverseTransformPoint(bottomRight);
            Vector2 layerSize = layer.rect.size;

            float x = local.x + layerSize.x * 0.5f;
            float y = local.y - layerSize.y * 0.5f;

            x = Mathf.Clamp(x, Mathf.Min(size.x + 8f, layerSize.x - 8f), layerSize.x - 8f);

            float lowest = size.y - layerSize.y + 8f;
            if (y < lowest)
            {
                Vector2 topLocal = layer.InverseTransformPoint(corners[1]);
                y = topLocal.y - layerSize.y * 0.5f + size.y;
            }

            y = Mathf.Clamp(y, lowest, -8f);
            return new Vector2(x, y);
        }
    }

    internal sealed class HemiAutoOffsetPopup : MonoBehaviour
    {
        internal const float Width = 290f;
        internal const float Height = 150f;

        private const float ExitDrop = 40f;

        private static GameObject host;
        private static HemiAutoOffsetPopup instance;

        private Canvas canvas;
        private RectTransform slot;
        private RectTransform card;
        private CanvasGroup group;
        private float target;
        private bool cursorWasShown;
        private bool cursorCaptured;
        private bool leaving;
        private bool closeQueued;

        internal static void Ensure()
        {
            HemiOverlayHost.Ensure<HemiAutoOffsetPopup>(ref host, "HemiTweaks_AutoOffsetPopup");
        }

        internal static void Shutdown()
        {
            if (host != null)
                Destroy(host);
            host = null;
            instance = null;
        }


        internal static void Show(float current, float proposed)
        {
            Ensure();
            if (instance != null)
                instance.Open(current, proposed);
        }

        internal static void HideNow()
        {
            if (instance != null)
                instance.Remove();
        }

        internal static void Dismiss()
        {
            if (instance != null)
                instance.Leave();
        }

        internal static RectTransform BuildCard(Transform parent, string question, Action onYes, Action onNo)
        {
            HemiColors colors = HemiTheme.Colors;

            Image panel = HemiKit.Panel("OffsetCard", parent, colors.Header, 16f);
            RectTransform card = panel.rectTransform;
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(HemiTheme.Col(Width), HemiTheme.Row(Height));

            HemiKit.Border(card, colors.BorderStrong, 16f);

            float insetX = HemiTheme.Col(16f);

            TextMeshProUGUI caption = HemiKit.Text(
                "Caption", card, HemiLang.Get("FEATURE_AUTOOFFSET"), 11.5f, colors.Faint, true);
            caption.characterSpacing = 4f;
            RectTransform captionRect = caption.rectTransform;
            captionRect.anchorMin = new Vector2(0f, 1f);
            captionRect.anchorMax = new Vector2(1f, 1f);
            captionRect.pivot = new Vector2(0.5f, 1f);
            captionRect.offsetMin = new Vector2(insetX, -HemiTheme.Row(31f));
            captionRect.offsetMax = new Vector2(-insetX, -HemiTheme.Row(16f));

            TextMeshProUGUI text = HemiKit.Text(
                "Question", card, question, 13.5f, colors.Text, false, TextAlignmentOptions.TopLeft, true);
            RectTransform questionRect = text.rectTransform;
            questionRect.anchorMin = new Vector2(0f, 1f);
            questionRect.anchorMax = new Vector2(1f, 1f);
            questionRect.pivot = new Vector2(0.5f, 1f);
            questionRect.offsetMin = new Vector2(insetX, -HemiTheme.Row(82f));
            questionRect.offsetMax = new Vector2(-insetX, -HemiTheme.Row(38f));

            RectTransform buttons = HemiKit.HBox("Buttons", card, 8f, new RectOffset(0, 0, 0, 0));
            buttons.anchorMin = new Vector2(1f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(1f, 0f);
            buttons.anchoredPosition = new Vector2(-insetX, HemiTheme.Row(16f));
            buttons.sizeDelta = new Vector2(HemiTheme.Col(168f), HemiTheme.Row(24f));

            Button no = HemiKit.Button("No", buttons, HemiLang.Get("AO_POPUP_NO"), colors.Button, colors.Text, onNo, 12.5f, 7f);
            HemiKit.Size(no.gameObject, HemiTheme.Col(80f), HemiTheme.Row(24f));

            Button yes = HemiKit.Button("Yes", buttons, HemiLang.Get("AO_POPUP_YES"), colors.Accent, Color.white, onYes, 12.5f, 7f);
            HemiKit.Size(yes.gameObject, HemiTheme.Col(80f), HemiTheme.Row(24f));

            return card;
        }

        internal static Vector2 ScaledSize(float scale)
        {
            return new Vector2(HemiTheme.Col(Width) * scale, HemiTheme.Row(Height) * scale);
        }

        private void Awake()
        {
            instance = this;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue - 15;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            HemiTweaksMod.ApplyDefaultScaling(scaler);
            gameObject.AddComponent<GraphicRaycaster>();
            canvas.enabled = false;
        }

        private void Update()
        {
            if (card == null || leaving || closeQueued)
                return;

            bool stillDead;
            try
            {
                scrController controller = ADOBase.controller;
                stillDead = controller != null && controller.gameworld && !controller.paused &&
                            (controller.state == States.Fail || controller.state == States.Fail2);
            }
            catch
            {
                stillDead = false;
            }

            if (!stillDead)
            {
                closeQueued = true;
                StartCoroutine(LeaveAtEndOfFrame());
            }
        }

        private IEnumerator LeaveAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            closeQueued = false;
            Leave();
        }

        private void Open(float current, float proposed)
        {
            Remove();
            target = proposed;
            float scale = AutoOffset.PopupScaleFactor;

            slot = HemiKit.Rect("Slot", transform);
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(AutoOffset.PopupX, AutoOffset.PopupY);
            slot.sizeDelta = ScaledSize(scale);
            group = slot.gameObject.AddComponent<CanvasGroup>();

            card = BuildCard(slot,
                HemiLang.Get("AO_POPUP_QUESTION", AutoOffset.Format(current), AutoOffset.Format(proposed)),
                Accept, Leave);
            card.localScale = new Vector3(scale, scale, 1f);

            cursorWasShown = Cursor.visible;
            cursorCaptured = true;
            Cursor.visible = true;
            canvas.enabled = true;

            if (!HemiRoot.AnimationsEnabled)
                return;

            RectTransform grown = card;
            CanvasGroup faded = group;
            HemiTween.To(this, "enter", 0f, 1f, 0.30f, HemiEase.SpringBouncy, t =>
            {
                if (grown != null)
                {
                    float s = scale * Mathf.LerpUnclamped(0.8f, 1f, t);
                    grown.localScale = new Vector3(s, s, 1f);
                }
                if (faded != null)
                    faded.alpha = Mathf.Clamp01(t * 2f);
            });
        }

        private void Accept()
        {
            if (leaving)
                return;
            float value = target;
            AutoOffset.ApplyOffset(value);
            if (HemiTweaksMod.IsInterfaceOpen)
                HemiRoot.Instance?.Refresh();
            Leave();
        }

        private void Leave()
        {
            if (card == null || leaving)
                return;

            if (!HemiRoot.AnimationsEnabled)
            {
                Remove();
                return;
            }

            leaving = true;
            group.blocksRaycasts = false;
            group.interactable = false;
            RestoreCursor();

            RectTransform sunk = slot;
            CanvasGroup faded = group;
            float top = slot.anchoredPosition.y;
            float x = slot.anchoredPosition.x;

            float startAlpha = group.alpha;
            RectTransform grown = card;
            float startScale = card != null ? card.localScale.x : 0f;
            float fullScale = AutoOffset.PopupScaleFactor;
            bool settleScale = grown != null && !Mathf.Approximately(startScale, fullScale);

            HemiTween.Kill(this, "enter");
            HemiTween.To(this, "leave", 0f, 1f, 0.18f, HemiEase.OutQuad, t =>
            {
                if (sunk != null)
                    sunk.anchoredPosition = new Vector2(x, top - ExitDrop * t);
                if (settleScale && grown != null)
                {
                    float s = Mathf.Lerp(startScale, fullScale, Mathf.Min(1f, t * 3f));
                    grown.localScale = new Vector3(s, s, 1f);
                }
                if (faded != null)
                    faded.alpha = startAlpha * (1f - t);
            }, Remove);
        }

        private void Remove()
        {
            HemiTween.KillAll(this);
            closeQueued = false;
            StopAllCoroutines();

            if (slot != null)
                Destroy(slot.gameObject);
            slot = null;
            card = null;
            group = null;

            bool wasUp = leaving;
            leaving = false;
            if (canvas != null)
                canvas.enabled = false;

            if (!wasUp)
                RestoreCursor();
        }

        private void RestoreCursor()
        {
            if (!cursorCaptured)
                return;

            cursorCaptured = false;
            if (!cursorWasShown)
                Cursor.visible = false;
            cursorWasShown = true;
        }

        private void OnDestroy()
        {
            HemiTween.KillAll(this);
        }
    }

    internal sealed class HemiUpdatePopup : MonoBehaviour
    {
        private const float Width = 360f;
        private const float Height = 132f;
        private const float Margin = 16f;

        private const float EnterSlide = 12f;

        private static GameObject host;

        private Canvas canvas;
        private RectTransform card;
        private TextMeshProUGUI detail;
        private HemiUpdateState seen = HemiUpdateState.Idle;
        private bool dismissed;

        internal static void Ensure()
        {
            HemiOverlayHost.Ensure<HemiUpdatePopup>(ref host, "HemiTweaks_UpdatePopup");
        }

        internal static void Shutdown()
        {
            if (host != null)
                Destroy(host);
            host = null;
        }

        private void Awake()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue - 16;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            HemiTweaksMod.ApplyDefaultScaling(scaler);
            gameObject.AddComponent<GraphicRaycaster>();
            canvas.enabled = false;
        }

        private void Update()
        {
            HemiUpdateState state = HemiUpdater.State;
            if (state == seen)
                return;
            seen = state;

            if (HemiTweaksMod.IsInterfaceOpen)
                HemiRoot.Instance?.Refresh();

            switch (state)
            {
                case HemiUpdateState.Available:
                    if (!dismissed)
                        Show();
                    break;
                case HemiUpdateState.Downloading:
                    if (card != null)
                        SetDetail(HemiLang.Get("UPD_STATUS_DOWNLOADING"), false);
                    break;
                case HemiUpdateState.Installed:
                    if (card != null)
                        SetDetail(string.IsNullOrEmpty(HemiUpdater.Error)
                            ? HemiLang.Get("UPD_STATUS_INSTALLED")
                            : HemiLang.Get("UPD_STATUS_RESTART_FAILED", HemiUpdater.Error), !string.IsNullOrEmpty(HemiUpdater.Error));
                    break;
                case HemiUpdateState.InstallFailed:
                    if (card != null)
                        SetDetail(HemiLang.Get("UPD_STATUS_INSTALL_FAILED", HemiUpdater.Error), true);
                    break;
            }
        }

        private void Show()
        {
            Hide();
            HemiColors colors = HemiTheme.Colors;

            Image panel = HemiKit.Panel("UpdateCard", transform, colors.Header, 16f);
            card = panel.rectTransform;
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(0f, 1f);
            card.pivot = new Vector2(0f, 1f);
            card.anchoredPosition = new Vector2(Margin, -Margin);
            card.sizeDelta = new Vector2(HemiTheme.Col(Width), HemiTheme.Row(Height));

            HemiKit.Border(card, colors.BorderStrong, 16f);

            float insetX = HemiTheme.Col(16f);

            TextMeshProUGUI title = HemiKit.Text("Title", card, HemiLang.Get("UPD_TITLE"), 16f, colors.Text, true);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(insetX, -HemiTheme.Row(44f));
            titleRect.offsetMax = new Vector2(-HemiTheme.Col(44f), -HemiTheme.Row(12f));

            detail = HemiKit.Text("Detail", card, "", 13f, colors.Muted);
            detail.textWrappingMode = TextWrappingModes.Normal;
            RectTransform detailRect = detail.rectTransform;
            detailRect.anchorMin = new Vector2(0f, 1f);
            detailRect.anchorMax = new Vector2(1f, 1f);
            detailRect.pivot = new Vector2(0.5f, 1f);
            detailRect.offsetMin = new Vector2(insetX, -HemiTheme.Row(78f));
            detailRect.offsetMax = new Vector2(-insetX, -HemiTheme.Row(44f));
            SetDetail(HemiUpdater.CurrentVersion + "  →  " + HemiUpdater.LatestVersion, false);

            Button close = HemiKit.Button("Close", card, "", new Color(0f, 0f, 0f, 0f), colors.Muted, Dismiss, 12f, 6f);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);
            closeRect.sizeDelta = new Vector2(HemiTheme.Row(24f), HemiTheme.Row(24f));
            HemiIcons.DrawCross(closeRect, colors.Muted, HemiTheme.Row(9f));

            RectTransform buttons = HemiKit.HBox("Buttons", card, 8f, new RectOffset(0, 0, 0, 0));
            buttons.anchorMin = new Vector2(0f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(0.5f, 0f);
            buttons.offsetMin = new Vector2(insetX, HemiTheme.Row(12f));
            buttons.offsetMax = new Vector2(-insetX, HemiTheme.Row(12f) + HemiTheme.Row(28f));

            Button install = HemiKit.Button("Install", buttons, HemiLang.Get("UPD_INSTALL_NOW"), colors.Accent, Color.white, delegate
            {
                HemiUpdater.Install();
            }, 12.5f, 7f);
            HemiKit.Size(install.gameObject, -1f, HemiTheme.Row(28f), 1f);

            Button later = HemiKit.Button("Later", buttons, HemiLang.Get("UPD_LATER"), colors.Button, colors.Text, Dismiss, 12.5f, 7f);
            HemiKit.Size(later.gameObject, -1f, HemiTheme.Row(28f), 1f);

            canvas.enabled = true;

            if (!HemiRoot.AnimationsEnabled)
                return;

            RectTransform entering = card;
            CanvasGroup group = entering.gameObject.AddComponent<CanvasGroup>();
            Vector2 home = entering.anchoredPosition;
            HemiTween.To(entering, "card", 0f, 1f, 0.35f, HemiEase.SpringGentle, t =>
            {
                if (entering != null)
                {
                    entering.anchoredPosition = new Vector2(home.x, home.y + EnterSlide * (1f - t));
                    if (group != null)
                        group.alpha = Mathf.Clamp01(t * 2f);
                }
            });
        }

        private void SetDetail(string text, bool failure)
        {
            if (detail == null)
                return;
            detail.text = text;
            detail.color = failure ? HemiTheme.Colors.Negative : HemiTheme.Colors.Muted;
        }

        private void Dismiss()
        {
            dismissed = true;

            if (card == null || !HemiRoot.AnimationsEnabled)
            {
                Hide();
                return;
            }

            RectTransform leaving = card;
            card = null;
            detail = null;

            CanvasGroup group = leaving.GetComponent<CanvasGroup>() ?? leaving.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            float startAlpha = group.alpha;
            Vector2 home = leaving.anchoredPosition;
            HemiTween.To(leaving, "card", 0f, 1f, 0.14f, HemiEase.OutQuad,
                t =>
                {
                    if (leaving != null)
                    {
                        leaving.anchoredPosition = new Vector2(home.x, home.y + EnterSlide * t);
                        if (group != null)
                            group.alpha = startAlpha * (1f - t);
                    }
                },
                delegate
                {
                    if (leaving != null)
                        Destroy(leaving.gameObject);

                    if (canvas != null && card == null)
                        canvas.enabled = false;
                });
        }

        private void Hide()
        {
            if (card != null)
                Destroy(card.gameObject);
            card = null;
            detail = null;
            if (canvas != null)
                canvas.enabled = false;
        }
    }
}
