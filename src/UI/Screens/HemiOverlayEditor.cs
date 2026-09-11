using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal interface IHemiOverlayTarget
    {
        string Id { get; }
        string Name { get; }

        bool Active { get; }

        Vector2 AnchorPoint { get; }

        Vector2 Offset { get; set; }

        float Size { get; set; }
        Vector2 SizeRange { get; }
        float SizeStep { get; }
        string SizeLabel { get; }

        bool SupportsAnchor { get; }
        StateAnchor Anchor { get; set; }

        RectTransform BuildGhost(RectTransform host);

        void Commit();
    }

    internal interface IHemiOverlayResizeSession
    {
        bool ResizeInProgress { get; }
        float ResizeReference { get; }
        void BeginResize();
        void EndResize();
    }

    internal sealed class HemiOverlayEditor : MonoBehaviour
    {
        private RectTransform layer;
        private RectTransform inspectorHost;
        private readonly List<HemiOverlayHandle> handles = new List<HemiOverlayHandle>();
        private IHemiOverlayTarget selected;
        private HemiOverlayInspector inspector;
        private bool rebuildQueued;

        internal static HemiOverlayEditor Create(RectTransform parent)
        {
            GameObject host = HemiKit.Obj("OverlayEditor", parent);
            RectTransform rect = host.GetComponent<RectTransform>();
            HemiKit.Stretch(rect);

            HemiOverlayEditor editor = host.AddComponent<HemiOverlayEditor>();
            editor.layer = rect;
            editor.Build();
            return editor;
        }

        internal int HandleCount => handles.Count;

        internal bool HasSelection => selected != null;

        internal void Refresh()
        {
            rebuildQueued = true;
        }

        internal void Select(IHemiOverlayTarget target)
        {
            selected = target;
            for (int i = 0; i < handles.Count; i++)
                handles[i].SetSelected(handles[i].Target == target);
            BuildInspector(true);
        }

        internal void RefreshInspector()
        {
            BuildInspector(false);
        }

        internal HemiOverlayHandle FindHandle(IHemiOverlayTarget target)
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i] != null && handles[i].Target == target)
                    return handles[i];
            }
            return null;
        }

        internal void NotifyMoved(IHemiOverlayTarget target)
        {
            if (target == selected)
                inspector?.SyncFromTarget();
        }

        private void Update()
        {
            if (!rebuildQueued)
                return;
            rebuildQueued = false;
            Build();
        }

        private void Build()
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i] != null)
                    Destroy(handles[i].gameObject);
            }
            handles.Clear();
            inspector = null;

            if (inspectorHost != null)
                Destroy(inspectorHost.gameObject);
            inspectorHost = null;

            Image catcher = layer.Find("Deselect")?.GetComponent<Image>();
            if (catcher == null)
            {
                catcher = HemiKit.Panel("Deselect", layer, new Color(0f, 0f, 0f, 0.001f), 0f);
                HemiKit.Stretch(catcher.rectTransform);
                Button button = catcher.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(delegate { Select(null); });
            }
            catcher.transform.SetAsFirstSibling();

            List<IHemiOverlayTarget> targets = HemiOverlayTargets.Collect();
            bool selectionSurvives = false;

            for (int i = 0; i < targets.Count; i++)
            {
                HemiOverlayHandle handle = HemiOverlayHandle.Create(this, layer, targets[i]);
                if (handle == null)
                    continue;
                handles.Add(handle);
                if (selected != null && string.Equals(targets[i].Id, selected.Id, StringComparison.Ordinal))
                {
                    selected = targets[i];
                    selectionSurvives = true;
                }
            }

            if (!selectionSurvives)
                selected = null;

            for (int i = 0; i < handles.Count; i++)
                handles[i].SetSelected(handles[i].Target == selected);

            BuildInspector(false);
        }

        private void BuildInspector(bool animate)
        {
            if (inspectorHost != null)
            {
                inspectorHost.gameObject.SetActive(false);
                Destroy(inspectorHost.gameObject);
                inspectorHost = null;
                inspector = null;
            }

            if (selected == null)
                return;

            inspectorHost = HemiKit.Rect("Inspector", layer);
            inspectorHost.anchorMin = Vector2.zero;
            inspectorHost.anchorMax = Vector2.zero;
            inspectorHost.pivot = Vector2.zero;
            inspectorHost.anchoredPosition = new Vector2(26f, 26f);
            inspector = HemiOverlayInspector.Build(this, inspectorHost, selected, FindHandle(selected), animate);
        }
    }

    internal static class HemiOverlayTargets
    {
        internal static List<IHemiOverlayTarget> Collect()
        {
            List<IHemiOverlayTarget> targets = new List<IHemiOverlayTarget>();

            IReadOnlyList<StateGroup> groups = StateGroupStore.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                StateGroup group = groups[i];
                if (group == null || group.Stats == null || !HasDrawableStat(group))
                    continue;
                targets.Add(new StateGroupOverlayTarget(group, i));
            }

            if (ProgressBarOverlay.Enabled)
                targets.Add(new ProgressBarOverlayTarget());

            if (OverloadBarOverlay.Enabled)
                targets.Add(new OverloadBarOverlayTarget());

            if (ComboOverlay.Enabled)
                targets.Add(new ComboOverlayTarget());

            if (AutoOffset.Enabled && AutoOffset.PopupEnabled)
                targets.Add(new AutoOffsetPopupOverlayTarget());

            if (KeyViewer.Config.Keys.Count > 0)
                targets.Add(new KeyViewerOverlayTarget());

            return targets;
        }

        private static bool HasDrawableStat(StateGroup group)
        {
            for (int i = 0; i < group.Stats.Count; i++)
            {
                StateStat stat = group.Stats[i];
                if (stat != null && stat.Enabled)
                    return true;
            }
            return false;
        }

        internal static float PixelsPerUnit
        {
            get
            {
                Canvas canvas = HemiRoot.Instance == null ? null : HemiRoot.Instance.Canvas;
                float factor = canvas == null ? 1f : canvas.scaleFactor;
                return factor <= 0.0001f ? 1f : factor;
            }
        }

        internal static RectTransform Pin(RectTransform content, Vector2 anchor)
        {
            content.anchorMin = anchor;
            content.anchorMax = anchor;
            content.pivot = anchor;
            content.anchoredPosition = Vector2.zero;
            return content;
        }
    }

    internal sealed class StateGroupOverlayTarget : IHemiOverlayTarget
    {
        private readonly StateGroup group;
        private readonly int index;

        internal StateGroupOverlayTarget(StateGroup value, int groupIndex)
        {
            group = value;
            index = groupIndex;
        }

        public string Id => "state:" + index;

        public string Name => string.IsNullOrEmpty(group.Name) ? HemiLang.Get("OVL_GROUP_FALLBACK", index + 1) : group.Name;

        public bool Active => StateOverlay.Enabled && group.Enabled;

        public Vector2 AnchorPoint => StateGroupLayout.AnchorVector(group.Anchor);

        public Vector2 Offset
        {
            get => new Vector2(group.OffsetX, group.OffsetY);
            set
            {
                group.OffsetX = Mathf.Round(value.x);
                group.OffsetY = Mathf.Round(value.y);
            }
        }

        public float Size
        {
            get => group.FontSize;
            set => group.FontSize = Mathf.Clamp(Mathf.RoundToInt(value), StateOverlay.MinimumFontSize, StateOverlay.MaximumFontSize);
        }

        public Vector2 SizeRange => new Vector2(StateOverlay.MinimumFontSize, StateOverlay.MaximumFontSize);

        public float SizeStep => 1f;

        public string SizeLabel => HemiLang.Get("OVL_SIZE_FONT");

        public bool SupportsAnchor => true;

        public StateAnchor Anchor
        {
            get => group.Anchor;
            set => group.Anchor = value;
        }

        public RectTransform BuildGhost(RectTransform host)
        {
            RectTransform content = HemiOverlayTargets.Pin(StateGroupVisual.CreateRoot(group, host, false), AnchorPoint);

            HemiOverlayGhostTicker ticker = content.gameObject.AddComponent<HemiOverlayGhostTicker>();
            ticker.Initialize(group);
            for (int i = 0; i < group.Stats.Count; i++)
            {
                StateStat stat = group.Stats[i];
                if (stat == null || !stat.Enabled)
                    continue;

                if (stat.Kind == StateStatKind.Image)
                {
                    StateGroupVisual.CreateImage(stat, content);
                    continue;
                }

                ticker.Track(stat, StateGroupVisual.CreateText(stat, group, content));
            }

            return content;
        }

        public void Commit()
        {
            StateGroupStore.MarkStyleChanged();
        }
    }

    internal sealed class ProgressBarOverlayTarget : IHemiOverlayTarget
    {
        public string Id => "progressbar";

        public string Name => HemiLang.Get("FEATURE_PROGRESSBAR");

        public bool Active => ProgressBarOverlay.Enabled;

        public Vector2 AnchorPoint => StateGroupLayout.AnchorVector(ProgressBarOverlay.Anchor);

        public Vector2 Offset
        {
            get => new Vector2(ProgressBarOverlay.OffsetX, ProgressBarOverlay.OffsetY);
            set => ProgressBarOverlay.SetOffset(Mathf.Round(value.x), Mathf.Round(value.y));
        }

        public float Size
        {
            get => ProgressBarOverlay.Width;
            set => ProgressBarOverlay.SetWidth(Mathf.Round(value));
        }

        public Vector2 SizeRange => new Vector2(ProgressBarOverlay.MinimumWidth, ProgressBarOverlay.MaximumWidth);

        public float SizeStep => 10f;

        public string SizeLabel => HemiLang.Get("SET_PROGRESSBAR_WIDTH");

        public bool SupportsAnchor => true;

        public StateAnchor Anchor
        {
            get => ProgressBarOverlay.Anchor;
            set => ProgressBarOverlay.SetAnchor(value);
        }

        public RectTransform BuildGhost(RectTransform host)
        {
            HemiBarBlock block = HemiBarBlock.Create(host);
            RectTransform content = HemiOverlayTargets.Pin(block.Root, AnchorPoint);
            content.gameObject.AddComponent<ProgressBarGhostTicker>().Initialize(block);
            return content;
        }

        public void Commit()
        {
        }
    }

    internal sealed class OverloadBarOverlayTarget : IHemiOverlayTarget
    {
        public string Id => "overloadbar";

        public string Name => HemiLang.Get("FEATURE_OVERLOADBAR");

        public bool Active => OverloadBarOverlay.Enabled;

        public Vector2 AnchorPoint => StateGroupLayout.AnchorVector(OverloadBarOverlay.Anchor);

        public Vector2 Offset
        {
            get => new Vector2(OverloadBarOverlay.OffsetX, OverloadBarOverlay.OffsetY);
            set => OverloadBarOverlay.SetOffset(Mathf.Round(value.x), Mathf.Round(value.y));
        }

        public float Size
        {
            get => OverloadBarOverlay.Length;
            set => OverloadBarOverlay.SetLength(Mathf.Round(value));
        }

        public Vector2 SizeRange => new Vector2(OverloadBarOverlay.MinimumLength, OverloadBarOverlay.MaximumLength);

        public float SizeStep => 10f;

        public string SizeLabel => HemiLang.Get("SET_OVERLOADBAR_LENGTH");

        public bool SupportsAnchor => true;

        public StateAnchor Anchor
        {
            get => OverloadBarOverlay.Anchor;
            set => OverloadBarOverlay.SetAnchor(value);
        }

        public RectTransform BuildGhost(RectTransform host)
        {
            HemiBarBlock block = HemiBarBlock.Create(host);
            RectTransform content = HemiOverlayTargets.Pin(block.Root, AnchorPoint);
            content.gameObject.AddComponent<OverloadBarGhostTicker>().Initialize(block);
            return content;
        }

        public void Commit()
        {
        }
    }

    internal sealed class ComboOverlayTarget : IHemiOverlayTarget
    {
        public string Id => "combo";

        public string Name => HemiLang.Get("FEATURE_COMBO");

        public bool Active => ComboOverlay.Enabled;

        public Vector2 AnchorPoint => StateGroupLayout.AnchorVector(ComboOverlay.Anchor);

        public Vector2 Offset
        {
            get => new Vector2(ComboOverlay.OffsetX, ComboOverlay.OffsetY);
            set => ComboOverlay.SetOffset(Mathf.Round(value.x), Mathf.Round(value.y));
        }

        public float Size
        {
            get => ComboOverlay.FontSize;
            set => ComboOverlay.SetFontSize(Mathf.RoundToInt(value));
        }

        public Vector2 SizeRange => new Vector2(ComboOverlay.MinimumFontSize, ComboOverlay.MaximumFontSize);

        public float SizeStep => 1f;

        public string SizeLabel => HemiLang.Get("OVL_SIZE_FONT");

        public bool SupportsAnchor => true;

        public StateAnchor Anchor
        {
            get => ComboOverlay.Anchor;
            set => ComboOverlay.SetAnchor(value);
        }

        public RectTransform BuildGhost(RectTransform host)
        {
            ComboBlock block = ComboBlock.Create(host);
            RectTransform content = HemiOverlayTargets.Pin(block.Root, AnchorPoint);
            content.gameObject.AddComponent<ComboGhostTicker>().Initialize(block);
            return content;
        }

        public void Commit()
        {
        }
    }

    internal sealed class AutoOffsetPopupOverlayTarget : IHemiOverlayTarget
    {
        public string Id => "autooffset";

        public string Name => HemiLang.Get("FEATURE_AUTOOFFSET");

        public bool Active => AutoOffset.Enabled && AutoOffset.PopupEnabled;

        public Vector2 AnchorPoint => new Vector2(0.5f, 0.5f);

        public Vector2 Offset
        {
            get => new Vector2(AutoOffset.PopupX, AutoOffset.PopupY);
            set => AutoOffset.SetPopupOffset(Mathf.Round(value.x), Mathf.Round(value.y));
        }

        public float Size
        {
            get => AutoOffset.PopupScale;
            set => AutoOffset.SetPopupScale(Mathf.RoundToInt(value));
        }

        public Vector2 SizeRange => new Vector2(AutoOffset.MinimumPopupScale, AutoOffset.MaximumPopupScale);

        public float SizeStep => 1f;

        public string SizeLabel => HemiLang.Get("OVL_SIZE_SCALE");

        public bool SupportsAnchor => false;

        public StateAnchor Anchor
        {
            get => StateAnchor.MiddleCenter;
            set { }
        }

        public RectTransform BuildGhost(RectTransform host)
        {
            float scale = AutoOffset.PopupScaleFactor;

            RectTransform footprint = HemiOverlayTargets.Pin(HemiKit.Rect("Popup", host), new Vector2(0.5f, 0.5f));
            footprint.sizeDelta = HemiAutoOffsetPopup.ScaledSize(scale);

            RectTransform card = HemiAutoOffsetPopup.BuildCard(footprint,
                HemiLang.Get("AO_POPUP_QUESTION", "10", "13"), null, null);
            card.localScale = new Vector3(scale, scale, 1f);

            foreach (Graphic graphic in footprint.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;

            return footprint;
        }

        public void Commit()
        {
        }
    }

    internal sealed class KeyViewerOverlayTarget : IHemiOverlayTarget, IHemiOverlayResizeSession
    {
        private const float MinimumKeyWidth = 16f;
        private const float MaximumKeyWidth = 400f;

        private readonly List<int> resizeIndices = new List<int>();
        private readonly List<Vector2> resizePositions = new List<Vector2>();
        private readonly List<Vector2> resizeSizes = new List<Vector2>();
        private readonly List<Vector2> transformedPositions = new List<Vector2>();
        private readonly List<Vector2> transformedSizes = new List<Vector2>();
        private Rect resizeBounds;
        private float resizeAverageWidth;
        private bool resizeInProgress;
        private bool historyRecorded;
        private KeyViewerHistory.Gesture resizeHistory;

        public string Id => "keyviewer";

        public string Name => HemiLang.Get("FEATURE_KEYVIEWER");

        public bool Active => KeyViewer.Enabled;

        public Vector2 AnchorPoint => StateGroupLayout.AnchorVector(KeyViewer.Config.BoardAnchor);

        public Vector2 Offset
        {
            get
            {
                float scale = HemiOverlayTargets.PixelsPerUnit;
                return KeyViewer.Config.BoardOffset / scale;
            }
            set
            {
                float scale = HemiOverlayTargets.PixelsPerUnit;
                KeyViewer.SetBoardOffset(value * scale);
            }
        }

        public float Size
        {
            get
            {
                IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
                if (keys.Count == 0)
                    return 60f;

                float total = 0f;
                for (int i = 0; i < keys.Count; i++)
                    total += keys[i].Size.x;
                return total / keys.Count;
            }
            set
            {
                float requested = Mathf.Clamp(value, MinimumKeyWidth, MaximumKeyWidth);
                if (Mathf.Abs(requested - Size) < 0.0005f)
                    return;

                if (!resizeInProgress)
                    BeginResize();
                if (resizeAverageWidth <= 0.01f || resizeIndices.Count == 0)
                    return;

                float factor = KeyViewerGeometry.ClampUniformScale(
                    resizePositions,
                    resizeSizes,
                    resizeBounds,
                    requested / resizeAverageWidth,
                    KeyViewer.MinimumKeySize,
                    KeyViewer.MaximumBoardSize);
                Rect target = new Rect(
                    resizeBounds.xMin,
                    resizeBounds.yMin,
                    resizeBounds.width * factor,
                    resizeBounds.height * factor);
                KeyViewerGeometry.Transform(
                    resizePositions,
                    resizeSizes,
                    resizeBounds,
                    target,
                    transformedPositions,
                    transformedSizes);
                if (!GeometryChanged())
                    return;

                if (!historyRecorded)
                {
                    resizeHistory = KeyViewerHistory.BeginGesture();
                    if (!KeyViewerHistory.Record(resizeHistory, "overlay-uniform-resize"))
                    {
                        resizeHistory = null;
                        return;
                    }
                    historyRecorded = true;
                }
                KeyViewer.SetKeyBoundsBatch(
                    resizeIndices,
                    transformedPositions,
                    transformedSizes,
                    false);
            }
        }

        public Vector2 SizeRange => new Vector2(MinimumKeyWidth, MaximumKeyWidth);

        public float SizeStep => 2f;

        public string SizeLabel => HemiLang.Get("OVL_SIZE_KEY");

        public bool SupportsAnchor => true;

        public bool ResizeInProgress => resizeInProgress;

        public float ResizeReference
        {
            get
            {
                Rect bounds = Bounds();
                float scale = HemiOverlayTargets.PixelsPerUnit;
                return Mathf.Max(60f, Mathf.Max(bounds.width, bounds.height) / scale);
            }
        }

        public StateAnchor Anchor
        {
            get => KeyViewer.Config.BoardAnchor;
            set => KeyViewer.SetBoardAnchor(value);
        }

        public RectTransform BuildGhost(RectTransform host)
        {
            float scale = HemiOverlayTargets.PixelsPerUnit;
            Rect bounds = KeyViewer.ContentBounds();

            RectTransform content = HemiOverlayTargets.Pin(HemiKit.Rect("KeyViewerGhost", host), AnchorPoint);
            content.sizeDelta = bounds.size / scale;

            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                KeyViewerKeyConfig key = keys[i];
                float radius = Mathf.Clamp(key.CornerRadius / scale, 0f, 24f);

                Image plate = HemiKit.Panel("Key", content, key.BackgroundColor, radius);
                RectTransform rect = plate.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = key.Size / scale;

                rect.anchoredPosition = new Vector2(
                    (key.Position.x - bounds.xMin) / scale,
                    -(key.Position.y - bounds.yMin) / scale);
                plate.raycastTarget = false;

                Image outline = HemiKit.Panel("Border", rect, key.BorderColor, 0f);
                outline.sprite = HemiSprites.RoundedOutline(Mathf.Max(1, Mathf.RoundToInt(radius)), Mathf.Max(1, Mathf.RoundToInt(key.BorderWidth / scale)));
                outline.type = Image.Type.Sliced;
                outline.raycastTarget = false;
                HemiKit.Stretch(outline.rectTransform);

                TextMeshProUGUI label = HemiKit.Text(
                    "Label", rect, KeyViewerKeyNames.GetDisplayName(key.Key), 13f, key.TextColor, true, TextAlignmentOptions.Center);
                HemiKit.Stretch(label.rectTransform, 2f, 2f, 2f, 2f);
            }

            return content;
        }

        public void Commit()
        {
            if (resizeInProgress)
            {
                FinishResize();
                return;
            }
            KeyViewer.CommitLayout();
        }

        public void BeginResize()
        {
            if (resizeInProgress)
                return;
            CaptureResizeSnapshot();
            resizeInProgress = true;
        }

        public void EndResize()
        {
            if (!resizeInProgress)
                return;
            FinishResize();
        }

        private void CaptureResizeSnapshot()
        {
            ClearResizeSnapshot();

            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                KeyViewerKeyConfig key = keys[i];
                if (key == null)
                    continue;
                resizeIndices.Add(i);
                resizePositions.Add(key.Position);
                resizeSizes.Add(key.Size);
                resizeAverageWidth += key.Size.x;
            }
            if (resizeIndices.Count > 0)
                resizeAverageWidth /= resizeIndices.Count;
            resizeBounds = KeyViewerGeometry.Bounds(resizePositions, resizeSizes);
        }

        private bool GeometryChanged()
        {
            if (transformedPositions.Count != resizeIndices.Count || transformedSizes.Count != resizeIndices.Count)
                return false;

            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            for (int i = 0; i < resizeIndices.Count; i++)
            {
                int index = resizeIndices[i];
                if (index < 0 || index >= keys.Count || keys[index] == null)
                    continue;
                if (!keys[index].Position.Equals(transformedPositions[i]) ||
                    !keys[index].Size.Equals(transformedSizes[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private bool GeometryMatchesSnapshot()
        {
            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            for (int i = 0; i < resizeIndices.Count; i++)
            {
                int index = resizeIndices[i];
                if (index < 0 || index >= keys.Count || keys[index] == null)
                    return false;
                if ((keys[index].Position - resizePositions[i]).sqrMagnitude > 0.000001f ||
                    (keys[index].Size - resizeSizes[i]).sqrMagnitude > 0.000001f)
                {
                    return false;
                }
            }
            return true;
        }

        private void FinishResize()
        {
            resizeInProgress = false;
            if (historyRecorded && GeometryMatchesSnapshot())
            {
                KeyViewer.SetKeyBoundsBatch(resizeIndices, resizePositions, resizeSizes, false);
                KeyViewerHistory.CancelGesture(resizeHistory);
            }
            else if (historyRecorded)
            {
                KeyViewer.CommitLayout();
                KeyViewerHistory.CommitGesture(resizeHistory);
            }
            ClearResizeSnapshot();
        }

        private void ClearResizeSnapshot()
        {
            resizeIndices.Clear();
            resizePositions.Clear();
            resizeSizes.Clear();
            transformedPositions.Clear();
            transformedSizes.Clear();
            resizeAverageWidth = 0f;
            historyRecorded = false;
            resizeHistory = null;
        }

        private static Rect Bounds()
        {
            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            if (keys.Count == 0)
                return new Rect(0f, 0f, 1f, 1f);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < keys.Count; i++)
            {
                KeyViewerKeyConfig key = keys[i];
                minX = Mathf.Min(minX, key.Position.x);
                minY = Mathf.Min(minY, key.Position.y);
                maxX = Mathf.Max(maxX, key.Position.x + key.Size.x);
                maxY = Mathf.Max(maxY, key.Position.y + key.Size.y);
            }

            return new Rect(minX, minY, Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxY - minY));
        }
    }

    internal sealed class HemiOverlayGhostTicker : MonoBehaviour
    {
        private readonly List<StateStat> stats = new List<StateStat>();
        private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        private readonly List<StateStatText> drivers = new List<StateStatText>();
        private StateGroup group;

        private readonly HemiTextMaterialSlot materialSlot = new HemiTextMaterialSlot();

        internal void Initialize(StateGroup value)
        {
            group = value;
        }

        private void OnDestroy()
        {
            materialSlot.Destroy();
        }

        internal void Track(StateStat stat, TextMeshProUGUI label)
        {
            if (stat == null || label == null)
                return;

            stats.Add(stat);
            labels.Add(label);
            drivers.Add(new StateStatText(stat, label));
        }

        private void Update()
        {
            if (!HemiTweaksMod.IsInterfaceOpen)
                return;

            StateValues.Poll();
            for (int i = 0; i < drivers.Count; i++)
            {
                StateGroupVisual.ApplyTextStyle(stats[i], group, labels[i]);
                StateGroupVisual.ApplyMaterial(group, materialSlot, labels[i]);
                drivers[i].Tick();
            }
        }
    }

    internal sealed class HemiOverlayHandle : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float MinimumHitSize = 28f;

        private HemiOverlayEditor editor;
        private RectTransform layer;
        private RectTransform host;
        private RectTransform content;
        private Image fill;
        private Image outline;
        private RectTransform chip;
        private RectTransform grip;
        private CanvasGroup group;

        private Vector2 dragStartLocal;
        private Vector2 dragStartOffset;
        private bool dragging;
        private bool selected;

        internal IHemiOverlayTarget Target { get; private set; }

        internal static HemiOverlayHandle Create(HemiOverlayEditor owner, RectTransform layer, IHemiOverlayTarget target)
        {
            GameObject obj = HemiKit.Obj("Handle_" + target.Id, layer);
            RectTransform host = obj.GetComponent<RectTransform>();

            Vector2 anchor = target.AnchorPoint;
            host.anchorMin = anchor;
            host.anchorMax = anchor;
            host.pivot = anchor;
            host.anchoredPosition = StateGroupLayout.ToAnchoredPosition(anchor, target.Offset);

            HemiOverlayHandle handle = obj.AddComponent<HemiOverlayHandle>();
            handle.editor = owner;
            handle.layer = layer;
            handle.host = host;
            handle.Target = target;
            handle.Build();
            return handle;
        }

        internal void SetSelected(bool value)
        {
            selected = value;
            HemiColors colors = HemiTheme.Colors;

            if (outline != null)
            {
                outline.color = value ? colors.Accent : new Color(1f, 1f, 1f, 0.30f);
                outline.sprite = HemiSprites.RoundedOutline(6, value ? 3 : 2);
            }

            if (fill != null)
            {
                Color target = value
                    ? new Color(colors.Accent.r, colors.Accent.g, colors.Accent.b, 0.12f)
                    : new Color(1f, 1f, 1f, 0.05f);

                if (!HemiRoot.AnimationsEnabled)
                {
                    fill.color = target;
                }
                else
                {
                    Color from = fill.color;
                    HemiTween.To(this, "fill", 0f, 1f, 0.12f, HemiEase.OutQuad, t =>
                    {
                        if (fill != null)
                            fill.color = Color.LerpUnclamped(from, target, t);
                    });
                }
            }

            if (grip != null)
                grip.gameObject.SetActive(value);
        }

        internal void RebuildContent()
        {
            if (Target == null || host == null)
                return;

            if (content != null)
            {
                content.gameObject.SetActive(false);
                Destroy(content.gameObject);
            }

            content = Target.BuildGhost(host);
            content.SetSiblingIndex(1);
        }

        internal void RebuildAll()
        {
            if (Target == null || host == null)
                return;

            for (int i = host.childCount - 1; i >= 0; i--)
            {
                GameObject child = host.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            content = null;
            Build();
            SetSelected(selected);
        }

        internal void ApplyAnchor(StateAnchor next)
        {
            if (Target == null || !Target.SupportsAnchor)
                return;

            Vector2 canvas = layer.rect.size;
            Vector2 size = host.rect.size;
            Vector2 oldAnchor = Target.AnchorPoint;
            Vector2 oldPivotPosition = new Vector2(oldAnchor.x * canvas.x, oldAnchor.y * canvas.y) + host.anchoredPosition;
            Vector2 centre = oldPivotPosition + new Vector2((0.5f - oldAnchor.x) * size.x, (0.5f - oldAnchor.y) * size.y);

            Target.Anchor = next;

            Vector2 newAnchor = Target.AnchorPoint;
            Vector2 newPivotPosition = centre - new Vector2((0.5f - newAnchor.x) * size.x, (0.5f - newAnchor.y) * size.y);
            Vector2 anchored = newPivotPosition - new Vector2(newAnchor.x * canvas.x, newAnchor.y * canvas.y);

            Target.Offset = StateGroupLayout.ToOffset(newAnchor, anchored);
            Target.Commit();

            host.anchorMin = newAnchor;
            host.anchorMax = newAnchor;
            host.pivot = newAnchor;

            RebuildAll();
            editor.NotifyMoved(Target);
            editor.RefreshInspector();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            editor.Select(Target);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!TryGetLocal(eventData, out dragStartLocal))
                return;
            dragStartOffset = Target.Offset;
            dragging = true;
            editor.Select(Target);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || !TryGetLocal(eventData, out Vector2 local))
                return;

            Vector2 delta = local - dragStartLocal;
            Vector2 sign = StateGroupLayout.InwardSign(Target.AnchorPoint);
            Target.Offset = dragStartOffset + new Vector2(delta.x * sign.x, delta.y * sign.y);
            editor.NotifyMoved(Target);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;
            dragging = false;
            Target.Commit();
        }

        private void LateUpdate()
        {
            if (Target == null || host == null)
                return;

            if (!HemiTweaksMod.IsInterfaceOpen)
                return;

            Vector2 anchor = Target.AnchorPoint;
            host.anchoredPosition = StateGroupLayout.ToAnchoredPosition(anchor, Target.Offset);

            if (content != null)
            {
                Vector2 size = content.rect.size;
                host.sizeDelta = new Vector2(Mathf.Max(size.x, MinimumHitSize), Mathf.Max(size.y, MinimumHitSize));
            }

            if (group != null)
                group.alpha = Target.Active ? 1f : 0.45f;
        }

        private void OnDestroy()
        {
            HemiTween.KillAll(this);
        }

        private void Build()
        {
            HemiColors colors = HemiTheme.Colors;
            group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            fill = HemiKit.Panel("Fill", host, new Color(1f, 1f, 1f, 0.05f), 6f);
            HemiKit.Stretch(fill.rectTransform, -6f, -6f, -6f, -6f);

            content = Target.BuildGhost(host);

            outline = HemiKit.Panel("Outline", host, new Color(1f, 1f, 1f, 0.30f), 0f);
            outline.sprite = HemiSprites.RoundedOutline(6, 2);
            outline.type = Image.Type.Sliced;
            outline.raycastTarget = false;
            HemiKit.Stretch(outline.rectTransform, -6f, -6f, -6f, -6f);

            BuildChip();
            BuildGrip(colors);
        }

        private void BuildChip()
        {
            Image plate = HemiKit.Panel("Chip", host, new Color(0f, 0f, 0f, 0.62f), 6f);
            chip = plate.rectTransform;
            plate.raycastTarget = false;

            bool below = Target.AnchorPoint.y > 0.9f;
            chip.anchorMin = new Vector2(0f, below ? 0f : 1f);
            chip.anchorMax = new Vector2(0f, below ? 0f : 1f);
            chip.pivot = new Vector2(0f, below ? 1f : 0f);
            chip.anchoredPosition = new Vector2(0f, below ? -10f : 10f);
            chip.sizeDelta = new Vector2(140f, 22f);

            TextMeshProUGUI label = HemiKit.Text("Text", chip, Target.Name, 12f, Color.white, true, TextAlignmentOptions.Center);
            HemiKit.ShowMarkupLiterally(label);
            HemiKit.Stretch(label.rectTransform, 8f, 8f, 0f, 0f);
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void BuildGrip(HemiColors colors)
        {
            Image plate = HemiKit.Panel("Grip", host, colors.Accent, 4f);
            grip = plate.rectTransform;
            grip.anchorMin = new Vector2(1f, 0f);
            grip.anchorMax = new Vector2(1f, 0f);
            grip.pivot = new Vector2(0.5f, 0.5f);
            grip.anchoredPosition = new Vector2(4f, -4f);
            grip.sizeDelta = new Vector2(18f, 18f);

            plate.gameObject.AddComponent<HemiOverlayResizeGrip>().Initialize(this);
            grip.gameObject.SetActive(false);
        }

        internal bool TryGetLocal(PointerEventData eventData, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layer, eventData.position, eventData.pressEventCamera, out local);
        }

        internal float HostExtent => Mathf.Max(host.rect.width, host.rect.height);
    }

    internal sealed class HemiOverlayResizeGrip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private HemiOverlayHandle handle;
        private Vector2 startLocal;
        private float startSize;
        private float reference;
        private bool dragging;

        internal void Initialize(HemiOverlayHandle owner)
        {
            handle = owner;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (handle == null || handle.Target == null || !handle.TryGetLocal(eventData, out startLocal))
                return;

            IHemiOverlayResizeSession session = handle.Target as IHemiOverlayResizeSession;
            session?.BeginResize();
            startSize = handle.Target.Size;
            reference = session == null ? Mathf.Max(60f, handle.HostExtent) : session.ResizeReference;
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || !handle.TryGetLocal(eventData, out Vector2 local))
                return;

            Vector2 delta = local - startLocal;
            float factor = 1f + (delta.x - delta.y) / reference;
            Vector2 range = handle.Target.SizeRange;
            float next = Mathf.Clamp(startSize * factor, range.x, range.y);

            float before = handle.Target.Size;
            handle.Target.Size = next;
            if (Mathf.Abs(handle.Target.Size - before) > 0.0001f)
                handle.RebuildContent();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;
            dragging = false;
            IHemiOverlayResizeSession session = handle.Target as IHemiOverlayResizeSession;
            if (session != null)
                session.EndResize();
            else
                handle.Target.Commit();
        }
    }

    internal sealed class HemiOverlayResizeSessionEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private IHemiOverlayResizeSession session;
        private bool endPending;

        internal void Initialize(IHemiOverlayResizeSession value)
        {
            session = value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            endPending = false;
            session?.BeginResize();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            endPending = true;
        }

        private void LateUpdate()
        {
            if (session == null || !session.ResizeInProgress)
                return;
            if (!endPending && Input.GetMouseButton(0))
                return;
            endPending = false;
            session.EndResize();
        }

        private void OnDisable()
        {
            endPending = false;
            if (session != null && session.ResizeInProgress)
                session.EndResize();
        }
    }

    internal sealed class HemiOverlayInspector
    {
        private const float Width = 420f;
        private const float RowHeight = 34f;
        private const float AnchorCell = 20f;
        private const float AnchorGap = 3f;
        private const float AnchorGridSize = AnchorCell * 3f + AnchorGap * 2f;
        private const float Spacing = 6f;
        private const float HeaderHeight = 44f;
        private const float BottomPad = 12f;

        private IHemiOverlayTarget target;
        private HemiSliderHandle xSlider;
        private HemiSliderHandle ySlider;
        private HemiSliderHandle sizeSlider;

        internal static HemiOverlayInspector Build(
            HemiOverlayEditor editor,
            RectTransform host,
            IHemiOverlayTarget target,
            HemiOverlayHandle handle,
            bool animate)
        {
            HemiColors colors = HemiTheme.Colors;
            HemiOverlayInspector inspector = new HemiOverlayInspector { target = target };

            RectTransform canvas = host.parent as RectTransform;
            float availableWidth = canvas != null ? canvas.rect.width - host.anchoredPosition.x - 26f : Width;
            float availableHeight = canvas != null ? canvas.rect.height - host.anchoredPosition.y - 26f : 900f;
            float width = Mathf.Min(HemiTheme.Col(Width), Mathf.Max(120f, availableWidth));

            Image panel = HemiKit.Panel("Panel", host, colors.Window, 12f);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(width, Mathf.Max(120f, availableHeight));

            Image edge = HemiKit.Panel("Outline", panelRect, colors.Line, 0f);
            edge.sprite = HemiSprites.RoundedOutline(12, 2);
            edge.type = Image.Type.Sliced;
            edge.raycastTarget = false;
            HemiKit.Stretch(edge.rectTransform);

            RectTransform header = HemiKit.HBox("Header", panelRect, HemiTheme.Gap, new RectOffset(14, 10, 0, 0));
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMax = Vector2.zero;

            float closeSize = HemiTheme.Row(28f);
            TextMeshProUGUI title = HemiKit.Text("Title", header, target.Name, 16f, colors.Text, true,
                TextAlignmentOptions.Left, true);
            HemiKit.ShowMarkupLiterally(title);
            title.overflowMode = TextOverflowModes.Overflow;
            float titleHeight = title.GetPreferredValues(title.text,
                Mathf.Max(1f, width - 24f - HemiTheme.Gap - closeSize), Mathf.Infinity).y;
            float headerHeight = Mathf.Max(HemiTheme.Row(HeaderHeight), titleHeight + 12f);
            header.offsetMin = new Vector2(0f, -headerHeight);
            HemiKit.Size(title.gameObject, 0f, Mathf.Max(closeSize, titleHeight), 1f);

            Button close = HemiKit.Button("Close", header, "X", colors.Card, colors.Muted,
                delegate { editor.Select(null); }, 13f, 6f);
            HemiKit.Size(close.gameObject, closeSize, closeSize);

            ScrollRect bodyScroll;
            RectTransform list = HemiKit.Scroll(panelRect, out bodyScroll);
            bodyScroll.viewport.offsetMin = new Vector2(0f, BottomPad);
            bodyScroll.viewport.offsetMax = new Vector2(0f, -headerHeight);
            VerticalLayoutGroup bodyLayout = list.GetComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = Spacing;
            bodyLayout.padding = new RectOffset(12, 12, 0, 0);

            inspector.xSlider = inspector.OffsetField(list, "X", target, true, HemiTweaksMod.DefaultCanvasWidth);
            inspector.ySlider = inspector.OffsetField(list, "Y", target, false, HemiTweaksMod.DefaultCanvasHeight);

            Vector2 range = target.SizeRange;
            IHemiOverlayResizeSession resizeSession = target as IHemiOverlayResizeSession;
            inspector.sizeSlider = inspector.Field(list, target.SizeLabel, target.Size, range.x, range.y, value =>
            {
                target.Size = value;
                if (resizeSession == null || !resizeSession.ResizeInProgress)
                    target.Commit();
                handle?.RebuildContent();
            }, "Size");
            if (resizeSession != null && inspector.sizeSlider.Slider != null)
            {
                inspector.sizeSlider.Slider.gameObject.AddComponent<HemiOverlayResizeSessionEvents>()
                    .Initialize(resizeSession);
            }

            if (target.SupportsAnchor)
                inspector.BuildAnchorGrid(editor, list, target, handle);

            TextMeshProUGUI note = HemiKit.Text(
                "Hint", list, HemiLang.Get("OVL_HINT"), 12f, colors.Muted, false,
                TextAlignmentOptions.TopLeft, true);
            note.overflowMode = TextOverflowModes.Overflow;
            HemiKit.Size(note.gameObject, -1f, -1f, 1f).minHeight = HemiTheme.Row(28f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(header);
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            float height = headerHeight + LayoutUtility.GetPreferredHeight(list) + BottomPad;
            panelRect.sizeDelta = new Vector2(width, Mathf.Min(height, Mathf.Max(120f, availableHeight)));

            if (animate && HemiRoot.AnimationsEnabled)
            {
                HemiTween.To(panelRect, "rise", -14f, 0f, 0.20f, HemiEase.OutCubic, value =>
                {
                    if (panelRect != null)
                        panelRect.anchoredPosition = new Vector2(0f, value);
                });
            }

            return inspector;
        }

        internal void SyncFromTarget()
        {
            if (target == null)
                return;

            Vector2 offset = target.Offset;
            xSlider.SetValue(offset.x);
            ySlider.SetValue(offset.y);
            sizeSlider.SetValue(target.Size);
        }

        private HemiSliderHandle OffsetField(RectTransform parent, string label, IHemiOverlayTarget target, bool horizontal, float limit)
        {
            return Field(parent, label, horizontal ? target.Offset.x : target.Offset.y, -limit, limit, value =>
            {
                Vector2 offset = target.Offset;
                target.Offset = horizontal ? new Vector2(value, offset.y) : new Vector2(offset.x, value);
                target.Commit();
            });
        }

        private HemiSliderHandle Field(
            RectTransform parent,
            string label,
            float value,
            float minimum,
            float maximum,
            Action<float> setter,
            string objectName = null)
        {
            RectTransform row = AdaptiveRow(parent, "Row_" + (objectName ?? label), HemiTheme.Row(RowHeight));
            HemiRows.Label(row, label, null, 46f, 0f);

            return HemiRows.SliderField(row, value, minimum, maximum, true, 0, null, setter, true, RowHeight - 4f, 54f);
        }

        private void BuildAnchorGrid(
            HemiOverlayEditor editor,
            RectTransform parent,
            IHemiOverlayTarget target,
            HemiOverlayHandle handle)
        {
            HemiColors colors = HemiTheme.Colors;
            RectTransform row = AdaptiveRow(parent, "Anchor", Mathf.Max(HemiTheme.Row(RowHeight), AnchorGridSize + 4f));
            HemiRows.Label(row, HemiLang.Get("OVL_ANCHOR"), null, 46f, 1f);

            RectTransform grid = HemiKit.Grid(
                "Grid", row, new Vector2(AnchorCell, AnchorCell), AnchorGap, new RectOffset(0, 0, 0, 0));
            HemiKit.Size(grid.gameObject, AnchorGridSize, AnchorGridSize).minWidth = AnchorGridSize;
            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            StateAnchor[] order =
            {
                StateAnchor.TopLeft, StateAnchor.TopCenter, StateAnchor.TopRight,
                StateAnchor.MiddleLeft, StateAnchor.MiddleCenter, StateAnchor.MiddleRight,
                StateAnchor.BottomLeft, StateAnchor.BottomCenter, StateAnchor.BottomRight
            };

            for (int i = 0; i < order.Length; i++)
            {
                StateAnchor anchor = order[i];
                bool active = target.Anchor == anchor;
                HemiKit.Button(
                    anchor.ToString(),
                    grid,
                    "",
                    active ? colors.Accent : colors.Field,
                    colors.Text,
                    delegate { handle?.ApplyAnchor(anchor); },
                    12f,
                    4f);
            }

            TextMeshProUGUI current = HemiKit.Text(
                "Current", row, StateGroupLayout.DisplayName(target.Anchor), 12f, colors.Accent, false,
                TextAlignmentOptions.Right, true);
            current.overflowMode = TextOverflowModes.Overflow;
            float currentWidth = Mathf.Max(HemiTheme.Col(96f), current.GetPreferredValues(current.text).x);
            LayoutElement currentLayout = HemiKit.Size(current.gameObject, currentWidth, -1f, 1f);
            currentLayout.minWidth = currentWidth;
            currentLayout.minHeight = HemiTheme.Row(30f);
        }

        private static RectTransform AdaptiveRow(RectTransform parent, string name, float minimumHeight)
        {
            RectTransform row = HemiKit.Rect(name, parent);
            HemiAdaptiveRow layout = row.gameObject.AddComponent<HemiAdaptiveRow>();
            layout.Spacing = Spacing;
            layout.VerticalGap = HemiTheme.Row(6f);
            layout.BaseHeight = minimumHeight;
            layout.childAlignment = TextAnchor.MiddleLeft;
            HemiKit.Size(row.gameObject, -1f, -1f, 1f).minHeight = minimumHeight;
            return row;
        }
    }
}
