using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HemiTweaks.Interface
{
    internal sealed class HemiKeyCanvas : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        private const float MinimumKeySize = KeyViewer.MinimumKeySize;
        private const float HandleSize = 9f;

        private const float HandleGrabRadius = 11f;

        private const float FitMargin = 0.92f;

        private const int GripCount = 8;

        private static readonly Vector2 CanvasArea = new Vector2(2000f, 1200f);

        private static readonly Vector2[] GripOffsets =
        {
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f)
        };

        private static int GridCell => Mathf.Max(1, Mathf.RoundToInt(KeyViewer.GridSize));

        private RectTransform grid;
        private RawImage gridImage;
        private int renderedGridCell;

        private void SyncGrid(Vector2 area, Vector2 boardSize)
        {
            if (gridImage == null)
                return;

            if (renderedGridCell != GridCell)
            {
                renderedGridCell = GridCell;
                gridImage.texture = HemiSprites.GridTexture(renderedGridCell);
            }

            float cell = GridCell * Scale;
            if (cell < 2f || area.x <= 0f || area.y <= 0f)
            {
                gridImage.enabled = false;
                return;
            }

            gridImage.enabled = true;

            Vector2 origin = area * 0.5f + pan - boardSize * Scale * 0.5f;

            gridImage.uvRect = new Rect(
                -origin.x / cell,
                -origin.y / cell,
                area.x / cell,
                area.y / cell);
        }

        private enum DragMode
        {
            None,
            Move,
            Resize,
            GroupResize,
            Pan,

            Marquee
        }

        private RectTransform viewport;
        private RectTransform board;
        private RectTransform keyLayer;
        private RectTransform selectionLayer;
        private readonly List<KeyBox> boxes = new List<KeyBox>();
        private readonly List<RectTransform> grips = new List<RectTransform>();
        private Image groupOutline;

        private readonly List<Image> outlines = new List<Image>();

        private Image marquee;
        private Image verticalGuide;
        private Image horizontalGuide;
        private RectTransform minimap;
        private Image minimapViewport;
        private readonly List<Image> minimapKeys = new List<Image>();
        private Action<int> onSelect;

        private readonly List<int> selection = new List<int>();

        private float zoom = 1f;

        private Vector2 pan;

        private DragMode mode;
        private Vector2 dragStart;
        private Vector2 panStart;
        private Vector2 startPosition;
        private Vector2 startSize;

        private readonly List<Vector2> dragOrigins = new List<Vector2>();
        private readonly List<Vector2> movePositions = new List<Vector2>();

        private readonly List<int> resizeIndices = new List<int>();
        private readonly List<Vector2> resizeOrigins = new List<Vector2>();
        private readonly List<Vector2> resizeSizes = new List<Vector2>();
        private readonly List<Vector2> resizedPositions = new List<Vector2>();
        private readonly List<Vector2> resizedSizes = new List<Vector2>();
        private Rect resizeGroupStart;
        private KeyViewerHistory.Gesture resizeGesture;
        private bool resizedDiffersFromStart;

        private readonly List<int> marqueeHits = new List<int>();
        private Vector2 marqueeStart;
        private Vector2 marqueeEnd;

        private readonly float[] subjectX = new float[3];
        private readonly float[] subjectY = new float[3];
        private readonly float[] otherX = new float[3];
        private readonly float[] otherY = new float[3];

        private int resizeHorizontal;
        private int resizeVertical;
        private bool pressedEmptySpace;

        internal float Scale { get; private set; } = 1f;

        internal int Selected => selection.Count > 0 ? selection[selection.Count - 1] : -1;

        internal IReadOnlyList<int> Selection => selection;

        internal float Zoom => zoom;

        internal bool HasArea => viewport != null && viewport.rect.width > 1f && viewport.rect.height > 1f;

        internal static HemiKeyCanvas Create(RectTransform parent, Action<int> selectionChanged)
        {
            HemiColors colors = HemiTheme.Colors;

            Image frame = HemiKit.Panel("Canvas", parent, colors.Field, 10f);
            HemiKit.Stretch(frame.rectTransform);
            frame.raycastTarget = false;

            RectTransform viewport = HemiKit.Rect("Viewport", frame.rectTransform);
            HemiKit.Stretch(viewport, 1f, 1f, 1f, 1f);
            viewport.gameObject.AddComponent<RectMask2D>();

            HemiKeyCanvas canvas = viewport.gameObject.AddComponent<HemiKeyCanvas>();
            canvas.viewport = viewport;
            canvas.onSelect = selectionChanged;
            canvas.Build();
            return canvas;
        }

        private const float MinimumScale = 0.1f;
        private const float MaximumScale = 6f;

        private float fitScale = 1f;

        private void UpdateFitScale(Vector2 area)
        {
            fitScale = Mathf.Max(0.0001f, Mathf.Min(area.x / CanvasArea.x, area.y / CanvasArea.y) * FitMargin);
        }

        internal void SetZoom(float value)
        {
            float minimum = MinimumScale / fitScale;
            float maximum = MaximumScale / fitScale;
            zoom = Mathf.Clamp(value, Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum));
        }

        internal void ResetView()
        {
            zoom = 1f;
            pan = Vector2.zero;

            if (viewport == null)
                return;

            Vector2 area = viewport.rect.size;
            if (area.x <= 1f || area.y <= 1f)
                return;

            UpdateFitScale(area);
            float baseScale = fitScale;

            Rect bounds = KeyViewer.ContentBounds();
            Vector2 size = bounds.size;
            if (size.x <= 1f || size.y <= 1f)
                return;

            SetZoom(Mathf.Min(area.x / (size.x * baseScale), area.y / (size.y * baseScale)));

            float scale = baseScale * zoom;
            Vector2 centre = bounds.center;
            pan = new Vector2(
                (CanvasArea.x * 0.5f - centre.x) * scale,
                (centre.y - CanvasArea.y * 0.5f) * scale);
        }

        internal void Select(int index)
        {
            Select(index, false);
        }

        internal void Select(int index, bool additive)
        {
            if (index >= KeyViewer.Config.Keys.Count)
                index = -1;

            if (index < 0)
            {
                if (selection.Count == 0)
                    return;
                selection.Clear();
                onSelect?.Invoke(-1);
                return;
            }

            if (!additive)
            {
                if (selection.Count == 1 && selection[0] == index)
                    return;
                selection.Clear();
                selection.Add(index);
                onSelect?.Invoke(index);
                return;
            }

            selection.Remove(index);
            selection.Add(index);
            onSelect?.Invoke(index);
        }

        internal void SelectMany(IList<int> indices)
        {
            selection.Clear();
            int count = KeyViewer.Config.Keys.Count;
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index >= 0 && index < count && !selection.Contains(index))
                    selection.Add(index);
            }
            onSelect?.Invoke(Selected);
        }

        internal void Rebuild()
        {
            for (int i = 0; i < boxes.Count; i++)
                boxes[i].Destroy();
            boxes.Clear();

            int count = KeyViewer.Config.Keys.Count;
            for (int i = 0; i < count; i++)
                boxes.Add(KeyBox.Create(keyLayer, i));
            SyncMinimapKeyCount(count);

            bool dropped = false;
            for (int i = selection.Count - 1; i >= 0; i--)
            {
                if (selection[i] < count)
                    continue;
                selection.RemoveAt(i);
                dropped = true;
            }
            if (dropped)
                onSelect?.Invoke(Selected);
        }

        private static void TopLeft(RectTransform rect, Vector2 pivot)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = pivot;
        }

        private void Build()
        {
            Image surface = HemiKit.Panel("Surface", viewport, new Color(0f, 0f, 0f, 0.001f), 0f);
            HemiKit.Stretch(surface.rectTransform);
            surface.transform.SetAsFirstSibling();

            grid = HemiKit.Rect("Grid", viewport);
            HemiKit.Stretch(grid);
            gridImage = grid.gameObject.AddComponent<RawImage>();
            gridImage.texture = HemiSprites.GridTexture(GridCell);
            renderedGridCell = GridCell;
            gridImage.color = new Color(1f, 1f, 1f, 0.07f);
            gridImage.raycastTarget = false;

            board = HemiKit.Rect("Board", viewport);
            board.anchorMin = new Vector2(0.5f, 0.5f);
            board.anchorMax = new Vector2(0.5f, 0.5f);
            board.pivot = new Vector2(0.5f, 0.5f);

            keyLayer = HemiKit.Rect("Keys", board);
            HemiKit.Stretch(keyLayer);

            RectTransform guideLayer = HemiKit.Rect("Guides", board);
            HemiKit.Stretch(guideLayer);
            verticalGuide = HemiKit.Panel("VerticalGuide", guideLayer, HemiTheme.Colors.Accent, 0f);
            verticalGuide.raycastTarget = false;
            verticalGuide.enabled = false;
            TopLeft(verticalGuide.rectTransform, new Vector2(0.5f, 1f));
            horizontalGuide = HemiKit.Panel("HorizontalGuide", guideLayer, HemiTheme.Colors.Accent, 0f);
            horizontalGuide.raycastTarget = false;
            horizontalGuide.enabled = false;
            TopLeft(horizontalGuide.rectTransform, new Vector2(0f, 0.5f));

            selectionLayer = HemiKit.Rect("Selection", board);
            HemiKit.Stretch(selectionLayer);

            groupOutline = HemiKit.Panel("GroupOutline", selectionLayer, HemiTheme.Colors.Accent, 0f);
            groupOutline.sprite = HemiSprites.RoundedOutline(4, 2);
            groupOutline.type = Image.Type.Sliced;
            groupOutline.raycastTarget = false;
            groupOutline.enabled = false;
            TopLeft(groupOutline.rectTransform, new Vector2(0f, 1f));

            for (int i = 0; i < GripCount; i++)
            {
                Image grip = HemiKit.Panel("Grip", selectionLayer, Color.white, 2f);
                grip.raycastTarget = false;
                TopLeft(grip.rectTransform, new Vector2(0.5f, 0.5f));
                grip.rectTransform.sizeDelta = new Vector2(HandleSize, HandleSize);
                grips.Add(grip.rectTransform);
            }

            marquee = HemiKit.Panel("Marquee", selectionLayer, new Color(
                HemiTheme.Colors.Accent.r, HemiTheme.Colors.Accent.g, HemiTheme.Colors.Accent.b, 0.18f), 2f);
            marquee.raycastTarget = false;
            TopLeft(marquee.rectTransform, new Vector2(0f, 1f));
            marquee.enabled = false;

            Image minimapSurface = HemiKit.Panel("Minimap", viewport, new Color(0.04f, 0.05f, 0.08f, 0.88f), 7f);
            minimap = minimapSurface.rectTransform;
            minimap.anchorMin = new Vector2(1f, 1f);
            minimap.anchorMax = new Vector2(1f, 1f);
            minimap.pivot = new Vector2(1f, 1f);
            minimap.anchoredPosition = new Vector2(-12f, -12f);
            minimap.sizeDelta = new Vector2(160f, 96f);
            minimapSurface.raycastTarget = false;
            minimapViewport = HemiKit.Panel("VisibleArea", minimap, new Color(
                HemiTheme.Colors.Accent.r, HemiTheme.Colors.Accent.g, HemiTheme.Colors.Accent.b, 0.18f), 1f);
            minimapViewport.raycastTarget = false;
            TopLeft(minimapViewport.rectTransform, new Vector2(0f, 1f));

            Rebuild();
        }

        private Image OutlineAt(int slot)
        {
            while (outlines.Count <= slot)
            {
                Image outline = HemiKit.Panel("Outline", selectionLayer, HemiTheme.Colors.Accent, 0f);
                outline.sprite = HemiSprites.RoundedOutline(4, 2);
                outline.type = Image.Type.Sliced;
                outline.raycastTarget = false;
                TopLeft(outline.rectTransform, new Vector2(0f, 1f));
                outline.rectTransform.SetSiblingIndex(0);
                outlines.Add(outline);
            }
            return outlines[slot];
        }

        private void LateUpdate()
        {
            if (viewport == null || board == null)
                return;

            if (!HemiTweaksMod.IsInterfaceOpen)
                return;

            Vector2 area = viewport.rect.size;
            Vector2 boardSize = CanvasArea;

            UpdateFitScale(area);
            SetZoom(zoom);
            Scale = Mathf.Max(0.01f, fitScale * zoom);

            board.sizeDelta = boardSize * Scale;
            ClampPan();
            board.anchoredPosition = pan;
            SyncGrid(area, boardSize);
            SyncGuides();
            SyncMinimap(area);

            if (boxes.Count != KeyViewer.Config.Keys.Count)
                Rebuild();

            for (int i = 0; i < boxes.Count; i++)
                boxes[i].Sync(Scale, selection.Contains(i));

            SyncSelection();
            SyncMarquee();
        }

        private void SyncGuides()
        {
            if (verticalGuide == null || horizontalGuide == null)
                return;
            verticalGuide.enabled = false;
            horizontalGuide.enabled = false;
            if (mode != DragMode.Move && mode != DragMode.Resize && mode != DragMode.GroupResize)
                return;

            KeyViewerConfig config = KeyViewer.Config;
            if (!config.GridAlignmentGuides && !config.GridSizeMatchGuides && !config.GridSpacingGuides)
                return;
            KeyViewerKeyConfig primary = KeyViewer.GetKey(Selected);
            if (primary == null)
                return;

            Rect subject = new Rect(primary.Position, primary.Size);
            if (mode == DragMode.GroupResize && TryGetSelectionBounds(out Rect groupBounds))
                subject = groupBounds;

            float tolerance = Mathf.Max(0.5f, 2f / Mathf.Max(0.01f, Scale));
            subjectX[0] = subject.xMin;
            subjectX[1] = subject.center.x;
            subjectX[2] = subject.xMax;
            subjectY[0] = subject.yMin;
            subjectY[1] = subject.center.y;
            subjectY[2] = subject.yMax;
            for (int i = 0; i < KeyViewer.Config.Keys.Count; i++)
            {
                if (selection.Contains(i))
                    continue;
                KeyViewerKeyConfig other = KeyViewer.GetKey(i);
                if (other == null || other.Hidden)
                    continue;
                otherX[0] = other.Position.x;
                otherX[1] = other.Position.x + other.Size.x * 0.5f;
                otherX[2] = other.Position.x + other.Size.x;
                otherY[0] = other.Position.y;
                otherY[1] = other.Position.y + other.Size.y * 0.5f;
                otherY[2] = other.Position.y + other.Size.y;
                if (config.GridAlignmentGuides)
                {
                    if (!verticalGuide.enabled && TryMatchingCoordinate(subjectX, otherX, tolerance, out float x))
                        ShowVerticalGuide(x);
                    if (!horizontalGuide.enabled && TryMatchingCoordinate(subjectY, otherY, tolerance, out float y))
                        ShowHorizontalGuide(y);
                }
                if (config.GridSizeMatchGuides &&
                    (mode == DragMode.Resize || mode == DragMode.GroupResize))
                {
                    if (!verticalGuide.enabled && Mathf.Abs(subject.width - other.Size.x) <= tolerance)
                        ShowVerticalGuide(subject.xMax);
                    if (!horizontalGuide.enabled && Mathf.Abs(subject.height - other.Size.y) <= tolerance)
                        ShowHorizontalGuide(subject.yMax);
                }
                if (verticalGuide.enabled && horizontalGuide.enabled)
                    break;
            }
            if (config.GridSpacingGuides)
            {
                ResolveEqualSpacing(subject, tolerance, out bool horizontal, out bool vertical);
                if (horizontal && !verticalGuide.enabled)
                    ShowVerticalGuide(subject.center.x);
                if (vertical && !horizontalGuide.enabled)
                    ShowHorizontalGuide(subject.center.y);
            }
        }

        private void ResolveEqualSpacing(
            Rect subject,
            float tolerance,
            out bool horizontal,
            out bool vertical)
        {
            float leftGap = float.MaxValue;
            float rightGap = float.MaxValue;
            float topGap = float.MaxValue;
            float bottomGap = float.MaxValue;
            float left = subject.xMin;
            float right = subject.xMax;
            float top = subject.yMin;
            float bottom = subject.yMax;

            for (int i = 0; i < KeyViewer.Config.Keys.Count; i++)
            {
                if (selection.Contains(i))
                    continue;
                KeyViewerKeyConfig other = KeyViewer.GetKey(i);
                if (other == null || other.Hidden)
                    continue;
                float otherLeft = other.Position.x;
                float otherRight = otherLeft + other.Size.x;
                float otherTop = other.Position.y;
                float otherBottom = otherTop + other.Size.y;
                bool overlapsY = otherBottom >= top && otherTop <= bottom;
                bool overlapsX = otherRight >= left && otherLeft <= right;
                if (overlapsY && otherRight <= left)
                    leftGap = Mathf.Min(leftGap, left - otherRight);
                if (overlapsY && otherLeft >= right)
                    rightGap = Mathf.Min(rightGap, otherLeft - right);
                if (overlapsX && otherBottom <= top)
                    topGap = Mathf.Min(topGap, top - otherBottom);
                if (overlapsX && otherTop >= bottom)
                    bottomGap = Mathf.Min(bottomGap, otherTop - bottom);
            }

            horizontal = leftGap < float.MaxValue && rightGap < float.MaxValue
                && Mathf.Abs(leftGap - rightGap) <= tolerance;
            vertical = topGap < float.MaxValue && bottomGap < float.MaxValue
                && Mathf.Abs(topGap - bottomGap) <= tolerance;
        }

        private static bool TryMatchingCoordinate(float[] first, float[] second, float tolerance, out float value)
        {
            for (int i = 0; i < first.Length; i++)
            {
                for (int j = 0; j < second.Length; j++)
                {
                    if (Mathf.Abs(first[i] - second[j]) <= tolerance)
                    {
                        value = second[j];
                        return true;
                    }
                }
            }
            value = 0f;
            return false;
        }

        private void ShowVerticalGuide(float x)
        {
            verticalGuide.enabled = true;
            verticalGuide.rectTransform.anchoredPosition = new Vector2(x * Scale, 0f);
            verticalGuide.rectTransform.sizeDelta = new Vector2(1f, CanvasArea.y * Scale);
        }

        private void ShowHorizontalGuide(float y)
        {
            horizontalGuide.enabled = true;
            horizontalGuide.rectTransform.anchoredPosition = new Vector2(0f, -y * Scale);
            horizontalGuide.rectTransform.sizeDelta = new Vector2(CanvasArea.x * Scale, 1f);
        }

        private void SyncMinimapKeyCount(int count)
        {
            if (minimap == null)
                return;
            while (minimapKeys.Count < count)
            {
                Image image = HemiKit.Panel("Key", minimap, new Color(1f, 1f, 1f, 0.5f), 1f);
                image.raycastTarget = false;
                TopLeft(image.rectTransform, new Vector2(0f, 1f));
                image.rectTransform.SetSiblingIndex(0);
                minimapKeys.Add(image);
            }
            for (int i = 0; i < minimapKeys.Count; i++)
                minimapKeys[i].gameObject.SetActive(i < count);
        }

        private void SyncMinimap(Vector2 viewportSize)
        {
            if (minimap == null)
                return;
            bool visible = KeyViewer.Config.GridMinimap;
            if (minimap.gameObject.activeSelf != visible)
                minimap.gameObject.SetActive(visible);
            if (!visible)
                return;

            SyncMinimapKeyCount(KeyViewer.Config.Keys.Count);
            Vector2 area = minimap.rect.size;
            Vector2 ratio = new Vector2(area.x / CanvasArea.x, area.y / CanvasArea.y);
            for (int i = 0; i < KeyViewer.Config.Keys.Count; i++)
            {
                KeyViewerKeyConfig key = KeyViewer.GetKey(i);
                Image image = minimapKeys[i];
                image.enabled = key != null && !key.Hidden;
                if (!image.enabled)
                    continue;
                image.color = key.BackgroundColor.a > 0.08f
                    ? new Color(key.BackgroundColor.r, key.BackgroundColor.g, key.BackgroundColor.b, 0.75f)
                    : new Color(1f, 1f, 1f, 0.45f);
                image.rectTransform.anchoredPosition = new Vector2(key.Position.x * ratio.x, -key.Position.y * ratio.y);
                image.rectTransform.sizeDelta = new Vector2(
                    Mathf.Max(1f, key.Size.x * ratio.x),
                    Mathf.Max(1f, key.Size.y * ratio.y));
            }

            Vector2 visibleCanvas = viewportSize / Mathf.Max(0.01f, Scale);
            Vector2 topLeft = new Vector2(
                CanvasArea.x * 0.5f + (-viewportSize.x * 0.5f - pan.x) / Mathf.Max(0.01f, Scale),
                CanvasArea.y * 0.5f - (viewportSize.y * 0.5f - pan.y) / Mathf.Max(0.01f, Scale));
            minimapViewport.rectTransform.anchoredPosition = new Vector2(topLeft.x * ratio.x, -topLeft.y * ratio.y);
            minimapViewport.rectTransform.sizeDelta = new Vector2(
                Mathf.Clamp(visibleCanvas.x * ratio.x, 2f, area.x),
                Mathf.Clamp(visibleCanvas.y * ratio.y, 2f, area.y));
        }

        private void SyncSelection()
        {
            int drawn = 0;
            for (int i = 0; i < selection.Count; i++)
            {
                KeyViewerKeyConfig key = KeyViewer.GetKey(selection[i]);
                if (key == null)
                    continue;

                Image outline = OutlineAt(drawn++);
                if (!outline.enabled)
                    outline.enabled = true;
                outline.rectTransform.anchoredPosition =
                    new Vector2(key.Position.x * Scale, -key.Position.y * Scale);
                outline.rectTransform.sizeDelta = key.Size * Scale;
            }

            for (int i = drawn; i < outlines.Count; i++)
            {
                if (outlines[i].enabled)
                    outlines[i].enabled = false;
            }

            bool hasBounds = TryGetSelectionBounds(out Rect bounds);
            bool showGroupOutline = hasBounds && selection.Count > 1;
            if (groupOutline != null)
            {
                groupOutline.enabled = showGroupOutline;
                if (showGroupOutline)
                {
                    groupOutline.rectTransform.anchoredPosition =
                        new Vector2(bounds.xMin * Scale, -bounds.yMin * Scale);
                    groupOutline.rectTransform.sizeDelta = bounds.size * Scale;
                }
            }

            bool showGrips = hasBounds;
            for (int i = 0; i < grips.Count; i++)
            {
                if (grips[i].gameObject.activeSelf != showGrips)
                    grips[i].gameObject.SetActive(showGrips);
            }

            if (!showGrips)
                return;

            Vector2 position = new Vector2(bounds.xMin * Scale, -bounds.yMin * Scale);
            Vector2 size = bounds.size * Scale;
            for (int i = 0; i < grips.Count; i++)
            {
                Vector2 offset = GripOffsets[i];
                grips[i].anchoredPosition = new Vector2(
                    position.x + size.x * offset.x,
                    position.y - size.y * offset.y);
            }
        }

        private bool TryGetSelectionBounds(out Rect bounds)
        {
            float minimumX = float.MaxValue;
            float minimumY = float.MaxValue;
            float maximumX = float.MinValue;
            float maximumY = float.MinValue;
            for (int i = 0; i < selection.Count; i++)
            {
                KeyViewerKeyConfig key = KeyViewer.GetKey(selection[i]);
                if (key == null)
                    continue;
                minimumX = Mathf.Min(minimumX, key.Position.x);
                minimumY = Mathf.Min(minimumY, key.Position.y);
                maximumX = Mathf.Max(maximumX, key.Position.x + key.Size.x);
                maximumY = Mathf.Max(maximumY, key.Position.y + key.Size.y);
            }

            if (minimumX > maximumX || minimumY > maximumY)
            {
                bounds = new Rect();
                return false;
            }

            bounds = Rect.MinMaxRect(minimumX, minimumY, maximumX, maximumY);
            return true;
        }

        private void SyncMarquee()
        {
            bool active = mode == DragMode.Marquee;
            if (marquee.enabled != active)
                marquee.enabled = active;
            if (!active)
                return;

            Vector2 min = Vector2.Min(marqueeStart, marqueeEnd);
            Vector2 max = Vector2.Max(marqueeStart, marqueeEnd);
            marquee.rectTransform.anchoredPosition = new Vector2(min.x * Scale, -min.y * Scale);
            marquee.rectTransform.sizeDelta = (max - min) * Scale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            resizeHorizontal = 0;
            resizeVertical = 0;
            pressedEmptySpace = true;

            if (eventData.button == PointerEventData.InputButton.Middle)
                return;

            if (!TryGetBoardPoint(eventData, out Vector2 point))
                return;

            if (TryGrabHandle(point, out resizeHorizontal, out resizeVertical))
            {
                pressedEmptySpace = false;
                return;
            }

            int hit = HitTest(point);
            pressedEmptySpace = hit < 0;

            if (hit < 0)
            {
                if (!IsAdditive())
                    Select(-1);
                return;
            }

            if (!IsAdditive() && selection.Contains(hit))
                return;

            Select(hit, IsAdditive());
        }

        private static bool IsAdditive()
        {
            return HemiModifiers.Command || HemiModifiers.Shift;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle ||
                eventData.button == PointerEventData.InputButton.Right)
            {
                if (!TryGetViewportPoint(eventData, out dragStart))
                    return;

                panStart = pan;
                mode = DragMode.Pan;
                return;
            }

            if (pressedEmptySpace)
            {
                if (!TryGetBoardPoint(eventData, out marqueeStart))
                    return;

                marqueeEnd = marqueeStart;
                mode = DragMode.Marquee;
                return;
            }

            KeyViewerKeyConfig key = KeyViewer.GetKey(Selected);
            if (key == null || !TryGetBoardPoint(eventData, out dragStart))
                return;

            startPosition = key.Position;
            startSize = key.Size;

            dragOrigins.Clear();
            for (int i = 0; i < selection.Count; i++)
            {
                KeyViewerKeyConfig member = KeyViewer.GetKey(selection[i]);
                dragOrigins.Add(member == null ? Vector2.zero : member.Position);
            }

            bool resizing = resizeHorizontal != 0 || resizeVertical != 0;
            if (resizing && selection.Count > 1)
            {
                if (!BeginGroupResize())
                    return;
                mode = DragMode.GroupResize;
            }
            else
            {
                mode = resizing ? DragMode.Resize : DragMode.Move;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mode == DragMode.None)
                return;

            if (mode == DragMode.Pan)
            {
                if (TryGetViewportPoint(eventData, out Vector2 viewportPoint))
                    pan = panStart + (viewportPoint - dragStart);
                return;
            }

            if (!TryGetBoardPoint(eventData, out Vector2 point))
                return;

            if (mode == DragMode.Marquee)
            {
                marqueeEnd = point;
                return;
            }

            Vector2 delta = point - dragStart;
            if (mode == DragMode.Move)
            {
                MoveSelection(delta);
                return;
            }

            if (mode == DragMode.GroupResize)
            {
                ResizeSelection(delta, false);
                return;
            }

            ResizeAxis(resizeHorizontal, delta.x, startPosition.x, startSize.x, out float px, out float sx);
            ResizeAxis(resizeVertical, delta.y, startPosition.y, startSize.y, out float py, out float sy);
            KeyViewer.SetKeyBounds(Selected, new Vector2(px, py), new Vector2(sx, sy), false);
        }

        private static void ResizeAxis(int direction, float delta, float startPosition, float startSize,
            out float position, out float size)
        {
            position = startPosition;
            size = startSize;
            if (direction < 0)
            {
                float shift = Mathf.Min(delta, startSize - MinimumKeySize);
                position = startPosition + shift;
                size = startSize - shift;
            }
            else if (direction > 0)
            {
                size = Mathf.Max(MinimumKeySize, startSize + delta);
            }
        }

        private bool BeginGroupResize()
        {
            resizeIndices.Clear();
            resizeOrigins.Clear();
            resizeSizes.Clear();
            for (int i = 0; i < selection.Count; i++)
            {
                int index = selection[i];
                KeyViewerKeyConfig key = KeyViewer.GetKey(index);
                if (key == null)
                    continue;

                resizeIndices.Add(index);
                resizeOrigins.Add(key.Position);
                resizeSizes.Add(key.Size);
            }

            if (resizeIndices.Count < 2)
                return false;

            resizeGroupStart = KeyViewerGeometry.Bounds(resizeOrigins, resizeSizes);
            if (resizeGroupStart.width <= 0f || resizeGroupStart.height <= 0f)
                return false;

            resizeGesture = null;
            resizedDiffersFromStart = false;
            resizedPositions.Clear();
            resizedSizes.Clear();
            return true;
        }

        private bool ResizeSelection(Vector2 delta, bool save)
        {
            if (resizeIndices.Count != resizeOrigins.Count || resizeIndices.Count != resizeSizes.Count)
                return false;

            float step = Mathf.Max(1f, KeyViewer.GridSize);
            float deltaX = resizeHorizontal == 0 ? 0f : Mathf.Round(delta.x / step) * step;
            float deltaY = resizeVertical == 0 ? 0f : Mathf.Round(delta.y / step) * step;

            float left = resizeGroupStart.xMin;
            float right = resizeGroupStart.xMax;
            float top = resizeGroupStart.yMin;
            float bottom = resizeGroupStart.yMax;
            ScaleAxis(resizeHorizontal, deltaX, resizeGroupStart.width, true, ref left, ref right);
            ScaleAxis(resizeVertical, deltaY, resizeGroupStart.height, false, ref top, ref bottom);

            Rect targetBounds = Rect.MinMaxRect(left, top, right, bottom);
            KeyViewerGeometry.Transform(
                resizeOrigins,
                resizeSizes,
                resizeGroupStart,
                targetBounds,
                resizedPositions,
                resizedSizes);
            bool differsFromStart = false;
            for (int i = 0; i < resizeIndices.Count; i++)
            {
                if ((resizedPositions[i] - resizeOrigins[i]).sqrMagnitude > 0.000001f ||
                    (resizedSizes[i] - resizeSizes[i]).sqrMagnitude > 0.000001f)
                {
                    differsFromStart = true;
                }
            }

            resizedDiffersFromStart = differsFromStart;
            if (!differsFromStart)
            {
                resizedPositions.Clear();
                resizedPositions.AddRange(resizeOrigins);
                resizedSizes.Clear();
                resizedSizes.AddRange(resizeSizes);
            }
            if (resizeGesture == null && !differsFromStart)
                return false;
            if (resizeGesture == null)
            {
                resizeGesture = KeyViewerHistory.BeginGesture();
                if (!KeyViewerHistory.Record(resizeGesture, null))
                {
                    resizeGesture = null;
                    return false;
                }
            }

            if (!save)
            {
                return KeyViewer.SetKeyBoundsBatch(
                    resizeIndices,
                    resizedPositions,
                    resizedSizes,
                    false);
            }

            return FinishGroupResize();
        }

        private void ScaleAxis(int direction, float delta, float extent, bool horizontal, ref float low, ref float high)
        {
            if (direction == 0)
                return;

            if (direction < 0)
            {
                float requested = (high - (low + delta)) / extent;
                low = high - extent * KeyViewerGeometry.ClampAxisScale(
                    resizeOrigins, resizeSizes, high, requested,
                    MinimumKeySize, KeyViewer.MaximumBoardSize, horizontal);
                return;
            }

            float grow = (high + delta - low) / extent;
            high = low + extent * KeyViewerGeometry.ClampAxisScale(
                resizeOrigins, resizeSizes, low, grow,
                MinimumKeySize, KeyViewer.MaximumBoardSize, horizontal);
        }

        private bool FinishGroupResize()
        {
            if (resizeGesture == null)
                return false;

            bool changed = KeyViewer.SetKeyBoundsBatch(
                resizeIndices,
                resizedPositions,
                resizedSizes,
                resizedDiffersFromStart);
            if (resizedDiffersFromStart)
                KeyViewerHistory.CommitGesture(resizeGesture);
            else
                KeyViewerHistory.CancelGesture(resizeGesture);
            resizeGesture = null;
            return changed;
        }

        private void MoveSelection(Vector2 delta)
        {
            if (selection.Count != dragOrigins.Count)
                return;

            movePositions.Clear();
            for (int i = 0; i < selection.Count; i++)
                movePositions.Add(dragOrigins[i] + delta);

            KeyViewer.SetKeyPositions(selection, movePositions, false);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (mode == DragMode.None)
                return;

            if (mode == DragMode.Marquee)
            {
                CommitMarquee();
                mode = DragMode.None;
                return;
            }

            DragMode finishedMode = mode;
            bool moved = finishedMode != DragMode.Pan;
            mode = DragMode.None;

            if (finishedMode == DragMode.GroupResize)
            {
                if (TryGetBoardPoint(eventData, out Vector2 point))
                    ResizeSelection(point - dragStart, true);
                else
                    FinishGroupResize();

                resizeHorizontal = 0;
                resizeVertical = 0;
                return;
            }

            resizeHorizontal = 0;
            resizeVertical = 0;

            if (!moved)
                return;

            KeyViewer.CommitLayout();
            KeyViewerHistory.Break();
        }

        private void CommitMarquee()
        {
            Vector2 min = Vector2.Min(marqueeStart, marqueeEnd);
            Vector2 max = Vector2.Max(marqueeStart, marqueeEnd);

            marqueeHits.Clear();
            if (IsAdditive())
                marqueeHits.AddRange(selection);

            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                KeyViewerKeyConfig key = keys[i];
                if (key.Position.x > max.x || key.Position.x + key.Size.x < min.x ||
                    key.Position.y > max.y || key.Position.y + key.Size.y < min.y)
                {
                    continue;
                }
                if (!marqueeHits.Contains(i))
                    marqueeHits.Add(i);
            }

            SelectMany(marqueeHits);
        }

        public void OnScroll(PointerEventData eventData)
        {
            float delta = eventData.scrollDelta.y;
            if (Mathf.Abs(delta) < 0.01f)
                return;

            float previous = zoom;
            SetZoom(zoom * (delta > 0f ? 1.1f : 1f / 1.1f));
            if (Mathf.Approximately(previous, zoom) || !TryGetViewportPoint(eventData, out Vector2 point))
                return;

            float ratio = zoom / previous;
            pan = point - (point - pan) * ratio;
        }

        private void ClampPan()
        {
            Vector2 limit = (board.rect.size + viewport.rect.size) * 0.5f;
            pan = new Vector2(
                Mathf.Clamp(pan.x, -limit.x, limit.x),
                Mathf.Clamp(pan.y, -limit.y, limit.y));
        }

        private int HitTest(Vector2 point)
        {
            IReadOnlyList<KeyViewerKeyConfig> keys = KeyViewer.Config.Keys;
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                KeyViewerKeyConfig key = keys[i];
                if (point.x >= key.Position.x && point.x <= key.Position.x + key.Size.x &&
                    point.y >= key.Position.y && point.y <= key.Position.y + key.Size.y)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool TryGrabHandle(Vector2 point, out int horizontal, out int vertical)
        {
            horizontal = 0;
            vertical = 0;

            if (!TryGetSelectionBounds(out Rect bounds))
                return false;

            float radius = HandleGrabRadius / Mathf.Max(0.01f, Scale);

            for (int i = 0; i < GripCount; i++)
            {
                Vector2 offset = GripOffsets[i];
                Vector2 grip = new Vector2(
                    bounds.xMin + bounds.width * offset.x,
                    bounds.yMin + bounds.height * offset.y);

                if (Vector2.Distance(point, grip) > radius)
                    continue;

                horizontal = offset.x < 0.25f ? -1 : offset.x > 0.75f ? 1 : 0;
                vertical = offset.y < 0.25f ? -1 : offset.y > 0.75f ? 1 : 0;
                return horizontal != 0 || vertical != 0;
            }

            return false;
        }

        private bool TryGetViewportPoint(PointerEventData eventData, out Vector2 point)
        {
            point = Vector2.zero;
            return viewport != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewport, eventData.position, eventData.pressEventCamera, out point);
        }

        private bool TryGetBoardPoint(PointerEventData eventData, out Vector2 point)
        {
            point = Vector2.zero;
            if (board == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    board, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return false;
            }

            Vector2 size = board.rect.size;
            point = new Vector2(
                (local.x + size.x * 0.5f) / Scale,
                (size.y * 0.5f - local.y) / Scale);
            return true;
        }

        private sealed class KeyBox
        {
            private RectTransform rect;
            private Image face;
            private TextMeshProUGUI label;
            private int index;

            internal static KeyBox Create(RectTransform parent, int index)
            {
                HemiColors colors = HemiTheme.Colors;
                KeyViewerKeyConfig key = KeyViewer.GetKey(index);

                Image face = HemiKit.Panel("Key_" + index, parent, key == null ? colors.Card : key.BackgroundColor, 6f);
                face.raycastTarget = false;
                RectTransform rect = face.rectTransform;
                TopLeft(rect, new Vector2(0f, 1f));

                TextMeshProUGUI label = HemiKit.Text(
                    "Label", rect, key == null ? "" : key.ResolveLabel(), 13f, Color.white, true, TextAlignmentOptions.Center);
                HemiKit.Stretch(label.rectTransform, 2f, 2f, 2f, 2f);

                return new KeyBox { rect = rect, face = face, label = label, index = index };
            }

            internal void Sync(float scale, bool isSelected)
            {
                KeyViewerKeyConfig key = KeyViewer.GetKey(index);
                if (key == null || rect == null)
                    return;

                rect.anchoredPosition = new Vector2(key.Position.x * scale, -key.Position.y * scale);
                rect.sizeDelta = key.Size * scale;

                Color background = key.BackgroundColor;
                if (key.Hidden)
                    background.a *= 0.28f;
                face.color = isSelected
                    ? Color.Lerp(background, HemiTheme.Colors.Accent, 0.25f)
                    : background;
                Color labelColor = key.TextColor;
                if (key.Hidden)
                    labelColor.a *= 0.45f;
                label.color = labelColor;

                string text = key.ResolveLabel();
                if (key.Hidden)
                    text += " [hidden]";
                if (label.text != text)
                    label.text = text;

                label.fontSize = Mathf.Clamp(
                    (key.FontSize > 0.01f ? key.FontSize : KeyViewer.Config.FontSize) * scale, 6f, 40f);
            }

            internal void Destroy()
            {
                if (rect == null)
                    return;

                rect.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(rect.gameObject);
                rect = null;
            }
        }
    }
}

namespace HemiTweaks
{
    internal static class KeyViewerGeometry
    {
        internal static Rect Bounds(IList<Vector2> positions, IList<Vector2> sizes)
        {
            if (positions == null || sizes == null)
                return new Rect(0f, 0f, 1f, 1f);

            int count = Math.Min(positions.Count, sizes.Count);
            if (count == 0)
                return new Rect(0f, 0f, 1f, 1f);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                Vector2 position = positions[i];
                Vector2 size = sizes[i];
                minX = Math.Min(minX, position.x);
                minY = Math.Min(minY, position.y);
                maxX = Math.Max(maxX, position.x + size.x);
                maxY = Math.Max(maxY, position.y + size.y);
            }
            return new Rect(minX, minY, Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY));
        }

        internal static float ClampUniformScale(
            IList<Vector2> positions,
            IList<Vector2> sizes,
            Rect bounds,
            float requested,
            float minimumSize,
            float maximumCoordinate)
        {
            if (positions == null || sizes == null || float.IsNaN(requested) || float.IsInfinity(requested))
                return 1f;

            int count = Math.Min(positions.Count, sizes.Count);
            float minimum = 0f;
            float maximum = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Vector2 size = sizes[i];
                if (size.x > 0f)
                {
                    minimum = Math.Max(minimum, minimumSize / size.x);
                    maximum = Math.Min(maximum, maximumCoordinate / size.x);
                }
                if (size.y > 0f)
                {
                    minimum = Math.Max(minimum, minimumSize / size.y);
                    maximum = Math.Min(maximum, maximumCoordinate / size.y);
                }

                float deltaX = positions[i].x - bounds.xMin;
                float deltaY = positions[i].y - bounds.yMin;
                if (deltaX > 0f)
                    maximum = Math.Min(maximum, (maximumCoordinate - bounds.xMin) / deltaX);
                if (deltaY > 0f)
                    maximum = Math.Min(maximum, (maximumCoordinate - bounds.yMin) / deltaY);
            }

            if (maximum < minimum)
                return maximum;
            return Math.Min(Math.Max(requested, minimum), maximum);
        }

        internal static float ClampAxisScale(
            IList<Vector2> positions,
            IList<Vector2> sizes,
            float fixedAnchor,
            float requestedScale,
            float minimumSize,
            float maximumValue,
            bool horizontal)
        {
            if (positions == null || sizes == null ||
                float.IsNaN(requestedScale) || float.IsInfinity(requestedScale))
            {
                return 1f;
            }

            float lower = 0f;
            float upper = float.MaxValue;
            int count = Math.Min(positions.Count, sizes.Count);
            for (int i = 0; i < count; i++)
            {
                float position = horizontal ? positions[i].x : positions[i].y;
                float size = horizontal ? sizes[i].x : sizes[i].y;
                if (size > 0f)
                {
                    lower = Math.Max(lower, minimumSize / size);
                    upper = Math.Min(upper, maximumValue / size);
                }

                float delta = position - fixedAnchor;
                if (delta > 0f)
                {
                    lower = Math.Max(lower, -fixedAnchor / delta);
                    upper = Math.Min(upper, (maximumValue - fixedAnchor) / delta);
                }
                else if (delta < 0f)
                {
                    lower = Math.Max(lower, (maximumValue - fixedAnchor) / delta);
                    upper = Math.Min(upper, -fixedAnchor / delta);
                }
            }

            lower = Math.Max(0f, lower);
            if (upper < lower)
                return upper;
            return Math.Min(Math.Max(requestedScale, lower), upper);
        }

        internal static void Transform(
            IList<Vector2> positions,
            IList<Vector2> sizes,
            Rect sourceBounds,
            Rect targetBounds,
            List<Vector2> outputPositions,
            List<Vector2> outputSizes)
        {
            if (outputPositions == null)
                throw new ArgumentNullException(nameof(outputPositions));
            if (outputSizes == null)
                throw new ArgumentNullException(nameof(outputSizes));

            outputPositions.Clear();
            outputSizes.Clear();
            if (positions == null || sizes == null)
                return;

            int count = Math.Min(positions.Count, sizes.Count);
            float scaleX = sourceBounds.width > 0.000001f ? targetBounds.width / sourceBounds.width : 1f;
            float scaleY = sourceBounds.height > 0.000001f ? targetBounds.height / sourceBounds.height : 1f;
            for (int i = 0; i < count; i++)
            {
                Vector2 sourcePosition = positions[i];
                Vector2 sourceSize = sizes[i];
                outputPositions.Add(new Vector2(
                    targetBounds.xMin + (sourcePosition.x - sourceBounds.xMin) * scaleX,
                    targetBounds.yMin + (sourcePosition.y - sourceBounds.yMin) * scaleY));
                outputSizes.Add(new Vector2(sourceSize.x * scaleX, sourceSize.y * scaleY));
            }
        }
    }

    internal static class KeyViewerHistory
    {
        private const int Capacity = 64;

        private static readonly List<Snapshot> undo = new List<Snapshot>();
        private static readonly List<Snapshot> redo = new List<Snapshot>();

        private static string pendingTag;

        private static bool applying;
        private static int revision;

        internal sealed class Gesture
        {
            internal readonly List<Snapshot> undoBefore;
            internal readonly List<Snapshot> redoBefore;
            internal readonly string pendingTagBefore;
            internal int expectedRevision;
            internal bool active = true;

            internal Gesture(
                List<Snapshot> undo,
                List<Snapshot> redo,
                string pendingTag,
                int revision)
            {
                undoBefore = undo;
                redoBefore = redo;
                pendingTagBefore = pendingTag;
                expectedRevision = revision;
            }
        }

        internal static bool CanUndo => undo.Count > 0;

        internal static bool CanRedo => redo.Count > 0;

        internal static void Record(string tag)
        {
            if (applying)
                return;

            if (!string.IsNullOrEmpty(tag) && tag == pendingTag && undo.Count > 0)
            {
                if (redo.Count > 0)
                {
                    redo.Clear();
                    revision++;
                }
                return;
            }

            undo.Add(Snapshot.Capture());
            if (undo.Count > Capacity)
                undo.RemoveAt(0);

            pendingTag = tag;
            redo.Clear();
            revision++;
        }

        internal static Gesture BeginGesture()
        {
            return new Gesture(
                new List<Snapshot>(undo),
                new List<Snapshot>(redo),
                pendingTag,
                revision);
        }

        internal static bool Record(Gesture gesture, string tag)
        {
            if (!OwnsCurrentHistory(gesture))
                return false;
            Record(tag);
            gesture.expectedRevision = revision;
            return true;
        }

        internal static bool CancelGesture(Gesture gesture)
        {
            if (!OwnsCurrentHistory(gesture))
                return false;

            undo.Clear();
            undo.AddRange(gesture.undoBefore);
            redo.Clear();
            redo.AddRange(gesture.redoBefore);
            pendingTag = gesture.pendingTagBefore;
            gesture.active = false;
            revision++;
            return true;
        }

        internal static void CommitGesture(Gesture gesture)
        {
            if (!OwnsCurrentHistory(gesture))
                return;
            gesture.active = false;
            Break();
        }

        private static bool OwnsCurrentHistory(Gesture gesture)
        {
            return gesture != null && gesture.active && gesture.expectedRevision == revision;
        }

        internal static void Break()
        {
            if (pendingTag == null)
                return;
            pendingTag = null;
            revision++;
        }

        internal static bool Undo()
        {
            return Step(undo, redo);
        }

        internal static bool Redo()
        {
            return Step(redo, undo);
        }

        internal static void Clear()
        {
            bool changed = undo.Count > 0 || redo.Count > 0 || pendingTag != null;
            undo.Clear();
            redo.Clear();
            pendingTag = null;
            if (changed)
                revision++;
        }

        private static bool Step(List<Snapshot> from, List<Snapshot> to)
        {
            if (from.Count == 0)
                return false;

            Snapshot target = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            to.Add(Snapshot.Capture());
            if (to.Count > Capacity)
                to.RemoveAt(0);

            applying = true;
            try
            {
                target.Restore();
            }
            finally
            {
                applying = false;
            }

            pendingTag = null;
            revision++;
            return true;
        }

        internal readonly struct Snapshot
        {
            private readonly List<KeyViewerKeyConfig> keys;
            private readonly Vector2 board;

            private Snapshot(List<KeyViewerKeyConfig> keys, Vector2 board)
            {
                this.keys = keys;
                this.board = board;
            }

            private static List<KeyViewerKeyConfig> Clone(IReadOnlyList<KeyViewerKeyConfig> source)
            {
                List<KeyViewerKeyConfig> copy = new List<KeyViewerKeyConfig>(source.Count);
                for (int i = 0; i < source.Count; i++)
                {
                    KeyViewerKeyConfig clone = new KeyViewerKeyConfig();
                    clone.CopyFrom(source[i]);
                    copy.Add(clone);
                }
                return copy;
            }

            internal static Snapshot Capture()
            {
                return new Snapshot(Clone(KeyViewer.Config.Keys), KeyViewer.Config.BoardSize);
            }

            internal void Restore()
            {
                if (keys == null)
                    return;

                KeyViewer.ReplaceKeys(Clone(keys), board);
            }
        }
    }
}
