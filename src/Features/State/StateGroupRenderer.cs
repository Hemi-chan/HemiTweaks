using System;
using System.Collections.Generic;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HemiTweaks
{
    internal sealed class StateGroupOverlayBehaviour : MonoBehaviour
    {
        private readonly List<GroupView> views = new List<GroupView>();
        private Canvas canvas;
        private RectTransform canvasRect;
        private int appliedRevision = -1;
        private int appliedFontRevision = -1;
        private bool wasVisible;
        private bool entrancePending;
        private bool runCompleted;

        private bool hasAlwaysVisible;

        private static StateGroupOverlayBehaviour current;

        private static scrController startedController;

        private void Awake()
        {
            current = this;
            BuildCanvas();
        }

        private void Update()
        {
            if (canvas == null)
                return;

            StateValues.Poll();

            if (appliedRevision != StateGroupStore.Revision || appliedFontRevision != StateOverlay.FontRevision)
            {
                appliedRevision = StateGroupStore.Revision;
                appliedFontRevision = StateOverlay.FontRevision;
                Rebuild();
            }

            bool playing = IsPlaying();
            bool waitingForStart = IsWaitingForStart();
            scrController controller = ADOBase.controller;
            bool motionAllowed = StateOverlay.Enabled && playing && !waitingForStart
                && !HemiTweaksMod.IsInterfaceOpen && controller != null && controller.gameworld;
            bool animate = motionAllowed && (IsMotionState(controller) || runCompleted);
            if (!motionAllowed)
                entrancePending = false;
            else if (animate && entrancePending)
            {
                entrancePending = false;
                for (int i = 0; i < views.Count; i++)
                    views[i].BeginEntrance();
            }
            SetWaitingForStart(!HemiTweaksMod.IsInterfaceOpen && (waitingForStart || entrancePending));
            bool visible = StateOverlay.Enabled && (playing || hasAlwaysVisible);
            if (visible != wasVisible)
            {
                wasVisible = visible;
                canvas.enabled = visible;
            }
            if (!visible)
            {
                ResetMotions();
                return;
            }

            for (int i = 0; i < views.Count; i++)
                views[i].Tick(playing, canvasRect, !animate);
        }

        private void OnDestroy()
        {
            StateImageCache.Clear();
            if (current == this)
                current = null;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            ResetMotions();
            if (canvas != null)
                canvas.enabled = false;
            wasVisible = false;
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            runCompleted = false;
            ResetMotions();
        }

        internal static void NotifyRunStarted()
        {
            scrController controller = ADOBase.controller;
            if (controller != null && controller.gameworld)
                startedController = controller;
            StateGroupOverlayBehaviour live = current;
            if (live == null)
                return;
            live.runCompleted = false;
            live.ResetMotions();
            live.entrancePending = StateOverlay.Enabled && controller != null
                && controller.gameworld && !controller.paused;
        }

        internal static void NotifyRunPrepared(scrController controller)
        {
            if (controller == null || controller != ADOBase.controller)
                return;
            startedController = null;
            StateGroupOverlayBehaviour live = current;
            if (live == null)
                return;
            live.runCompleted = false;
            live.ResetMotions();
            live.SetWaitingForStart(IsWaitingForStart() && !HemiTweaksMod.IsInterfaceOpen);
        }

        private static bool IsWaitingForStart()
        {
            scrController controller = ADOBase.controller;
            return controller != null && controller.gameworld && ADOBase.editor == null
                && controller != startedController;
        }

        private void SetWaitingForStart(bool waiting)
        {
            for (int i = 0; i < views.Count; i++)
                views[i].SetWaitingForStart(waiting);
        }

        internal static void NotifyRunEnded()
        {
            StateGroupOverlayBehaviour live = current;
            if (live == null)
                return;
            live.ResetMotions();
        }

        internal static void NotifyRunCompleted()
        {
            StateGroupOverlayBehaviour live = current;
            if (live == null || live.runCompleted)
                return;
            live.runCompleted = true;
            live.entrancePending = false;
            scrController controller = ADOBase.controller;
            if (!StateOverlay.Enabled || !IsPlaying() || IsWaitingForStart()
                || HemiTweaksMod.IsInterfaceOpen || controller == null || !controller.gameworld)
            {
                live.ResetMotions();
                return;
            }
            for (int i = 0; i < live.views.Count; i++)
                live.views[i].BeginExit();
        }

        private void ResetMotions()
        {
            entrancePending = false;
            for (int i = 0; i < views.Count; i++)
                views[i].ResetMotion();
        }

        private static bool IsMotionState(scrController controller)
        {
            return controller != null && controller.gameworld && !controller.paused
                && (controller.state == States.Start || controller.state == States.Countdown
                    || controller.state == States.Checkpoint || controller.state == States.PlayerControl);
        }

        private static bool IsPlaying()
        {
            return StateValues.IsPlaying;
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("StateGroupCanvas");
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue - 64;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            HemiTweaksMod.ApplyDefaultScaling(scaler);

            canvasRect = canvasObject.GetComponent<RectTransform>();
            canvas.enabled = false;
        }

        private void Rebuild()
        {
            for (int i = 0; i < views.Count; i++)
                views[i].Destroy();
            views.Clear();
            hasAlwaysVisible = false;

            IReadOnlyList<StateGroup> groups = StateGroupStore.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                StateGroup group = groups[i];
                if (group == null || !group.Enabled || group.Stats == null || group.Stats.Count == 0)
                    continue;

                for (int j = 0; j < group.Stats.Count; j++)
                {
                    StateStat stat = group.Stats[j];
                    if (stat != null && stat.Enabled && StateGroupStore.IsAlwaysVisible(stat))
                        hasAlwaysVisible = true;
                }

                GroupView view = GroupView.Create(group, canvasRect);
                if (view != null)
                    views.Add(view);
            }
        }

        private sealed class GroupView
        {
            private GameObject root;
            private StateGroup group;
            private RectTransform rect;
            private HorizontalOrVerticalLayoutGroup layout;
            private readonly List<StatView> stats = new List<StatView>();

            internal readonly StateMotionPlayer Motion = new StateMotionPlayer();

            private readonly HemiTextMaterialSlot materialSlot = new HemiTextMaterialSlot();

            internal static GroupView Create(StateGroup group, RectTransform parent)
            {
                RectTransform rect = StateGroupVisual.CreateRoot(group, parent, true);
                GameObject root = rect.gameObject;

                GroupView view = new GroupView
                {
                    root = root,
                    group = group,
                    rect = rect,
                    layout = root.GetComponent<HorizontalOrVerticalLayoutGroup>()
                };

                for (int i = 0; i < group.Stats.Count; i++)
                {
                    StateStat stat = group.Stats[i];
                    if (stat == null || !stat.Enabled)
                        continue;
                    StatView statView = StatView.Create(stat, group, root.transform);
                    if (statView != null)
                        view.stats.Add(statView);
                }
                return view;
            }

            internal void BeginEntrance()
            {
                ResetMotion();
                Motion.BeginEntrance(group.Entrance);
            }

            internal void ResetMotion()
            {
                Motion.Rest();
                ApplyLayout();
            }

            internal void BeginExit()
            {
                if (group.Exit == null || !group.Exit.Enabled)
                    ResetMotion();
                else
                    Motion.BeginExit(group.Exit);
            }

            internal void SetWaitingForStart(bool waiting)
            {
                bool show = !waiting || group.Entrance == null || !group.Entrance.Enabled;
                if (root != null && root.activeSelf != show)
                    root.SetActive(show);
            }

            internal void Tick(bool playing, RectTransform canvas, bool motionBlocked)
            {
                for (int i = 0; i < stats.Count; i++)
                    stats[i].Tick(group, materialSlot, playing);
                if (motionBlocked)
                    ResetMotion();
                else
                {
                    ApplyLayout();
                    Vector2 before = Motion.Displacement;
                    Motion.Tick(rect, canvas);
                    if (Motion.Displacement != before)
                        ApplyLayout();
                }
            }

            private void ApplyLayout()
            {
                if (rect == null || group == null)
                    return;

                Vector2 anchor = StateGroupLayout.AnchorVector(group.Anchor);
                if (rect.anchorMin != anchor)
                {
                    rect.anchorMin = anchor;
                    rect.anchorMax = anchor;
                    rect.pivot = anchor;
                }

                Vector2 position = StateGroupLayout.AnchoredPositionFor(group) + Motion.Displacement;
                if (rect.anchoredPosition != position)
                    rect.anchoredPosition = position;

                if (layout == null)
                    return;

                if (!Mathf.Approximately(layout.spacing, group.Spacing))
                    layout.spacing = group.Spacing;

                if (layout is VerticalLayoutGroup)
                {
                    TextAnchor alignment = StateGroupLayout.ChildAlignment(group.Anchor);
                    if (layout.childAlignment != alignment)
                        layout.childAlignment = alignment;
                }
            }

            internal void Destroy()
            {
                if (root != null)
                    UnityEngine.Object.Destroy(root);
                materialSlot.Destroy();
                root = null;
                rect = null;
                layout = null;
                group = null;
                stats.Clear();
            }
        }

        private sealed class StatView
        {
            private StateStat stat;
            private GameObject root;
            private TextMeshProUGUI label;
            private StateStatText driver;

            internal static StatView Create(StateStat stat, StateGroup group, Transform parent)
            {
                if (stat.Kind == StateStatKind.Image)
                {
                    Image image = StateGroupVisual.CreateImage(stat, parent);
                    return image == null
                        ? null
                        : new StatView { stat = stat, root = image.gameObject };
                }

                TextMeshProUGUI label = StateGroupVisual.CreateText(stat, group, parent);
                return new StatView
                {
                    stat = stat,
                    root = label.gameObject,
                    label = label,
                    driver = new StateStatText(stat, label)
                };
            }

            internal void Tick(StateGroup group, HemiTextMaterialSlot slot, bool playing)
            {
                bool show = playing || StateGroupStore.IsAlwaysVisible(stat);
                if (root != null && root.activeSelf != show)
                    root.SetActive(show);
                if (!show)
                    return;

                StateGroupVisual.ApplyTextStyle(stat, group, label);
                StateGroupVisual.ApplyMaterial(group, slot, label);
                driver?.Tick();
            }
        }
    }

    internal sealed class StateStatText
    {
        private readonly StateStat stat;
        private readonly TextMeshProUGUI label;
        private string last;
        private float nextUpdate;

        internal StateStatText(StateStat stat, TextMeshProUGUI label)
        {
            this.stat = stat;
            this.label = label;
        }

        internal void Tick()
        {
            if (label == null || stat == null)
                return;

            float interval = stat.RefreshInterval;
            if (interval > 0.0001f)
            {
                float now = Time.unscaledTime;
                if (now < nextUpdate)
                    return;
                nextUpdate = now + interval;
            }

            string text = StateGroupVisual.ComposeText(stat);
            if (text == last)
                return;

            last = text;
            label.text = text;
        }
    }

    internal static class StateGroupVisual
    {
        internal static RectTransform CreateRoot(StateGroup group, Transform parent, bool positioned)
        {
            GameObject root = new GameObject("Group_" + (group.Name ?? "group"));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.AddComponent<RectTransform>();
            if (positioned)
            {
                Vector2 anchor = StateGroupLayout.AnchorVector(group.Anchor);
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = anchor;
                rect.anchoredPosition = StateGroupLayout.AnchoredPositionFor(group);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            if (group.Horizontal)
            {
                HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = group.Spacing;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
            else
            {
                VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
                layout.spacing = group.Spacing;
                layout.childAlignment = StateGroupLayout.ChildAlignment(group.Anchor);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        internal static TextMeshProUGUI CreateText(StateStat stat, StateGroup group, Transform parent)
        {
            GameObject obj = new GameObject("Stat_" + stat.Kind);
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
            label.fontSize = group.FontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = StateGroupStore.ParseColor(stat.ColorHex, group.ResolveColor());
            label.alignment = TextAlignmentFor(group.Anchor);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.enableAutoSizing = false;
            label.richText = true;

            TMP_FontAsset fontAsset = StateOverlay.GetFontAsset();
            if (fontAsset != null)
                label.font = fontAsset;

            ContentSizeFitter fitter = obj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label.text = ComposeText(stat);
            return label;
        }

        internal static Image CreateImage(StateStat stat, Transform parent)
        {
            GameObject obj = new GameObject("Stat_Image");
            obj.transform.SetParent(parent, false);

            Image image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            Sprite sprite = StateImageCache.Load(stat.ImagePath);
            if (sprite == null)
            {
                UnityEngine.Object.Destroy(obj);
                return null;
            }

            image.sprite = sprite;
            float height = Mathf.Clamp(stat.ImageHeight, 4f, 1024f);
            float width = sprite.rect.height > 0f ? height * (sprite.rect.width / sprite.rect.height) : height;

            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.preferredWidth = width;
            return image;
        }

        internal static void ApplyTextStyle(StateStat stat, StateGroup group, TextMeshProUGUI label)
        {
            if (label == null || stat == null || group == null)
                return;

            if (!Mathf.Approximately(label.fontSize, group.FontSize))
                label.fontSize = group.FontSize;

            Color color = StateGroupStore.ParseColor(stat.ColorHex, group.ResolveColor());
            if (label.color != color)
                label.color = color;

            TextAlignmentOptions alignment = TextAlignmentFor(group.Anchor);
            if (label.alignment != alignment)
                label.alignment = alignment;
        }

        internal static void ApplyMaterial(StateGroup group, HemiTextMaterialSlot slot, TextMeshProUGUI label)
        {
            if (label == null || group == null || slot == null)
                return;

            Material material = slot.Resolve(
                label.font, group.ResolveShadowColor(), group.ShadowX, group.ShadowY, out bool rewritten);
            if (material == null)
                return;

            bool assigned = label.fontSharedMaterial != material;
            if (assigned)
                label.fontSharedMaterial = material;

            if (assigned || rewritten)
                label.UpdateMeshPadding();
        }

        internal static string ComposeText(StateStat stat)
        {
            return (stat.Label ?? "")
                + stat.ResolveSeparator()
                + StateValues.Format(stat)
                + (stat.Suffix ?? "");
        }

        internal static TextAlignmentOptions TextAlignmentFor(StateAnchor anchor)
        {
            switch (anchor)
            {
                case StateAnchor.TopRight:
                case StateAnchor.MiddleRight:
                case StateAnchor.BottomRight:
                    return TextAlignmentOptions.Right;
                case StateAnchor.TopCenter:
                case StateAnchor.MiddleCenter:
                case StateAnchor.BottomCenter:
                    return TextAlignmentOptions.Center;
                default:
                    return TextAlignmentOptions.Left;
            }
        }
    }

    internal static class StateImageCache
    {
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static long cachedPixels;
        private static bool budgetWarning;

        internal static Sprite Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (sprites.TryGetValue(path, out Sprite cached))
                return cached;
            if (failed.Contains(path))
                return null;
            if (failed.Count >= HemiImageSafety.MaximumFailedPaths
                || !HemiImageSafety.CanCache(sprites.Count, cachedPixels, 1, 1))
            {
                WarnBudget();
                return null;
            }

            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                texture = HemiImageDecoder.LoadFile(path, "State image", HemiImageSafety.MaximumCachePixels - cachedPixels);
                if (texture == null)
                {
                    failed.Add(path);
                    return null;
                }
                if (!HemiImageSafety.CanCache(sprites.Count, cachedPixels, texture.width, texture.height))
                {
                    failed.Add(path);
                    WarnBudget();
                    return null;
                }

                UnityEngine.Object.DontDestroyOnLoad(texture);

                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                sprite.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(sprite);

                sprites[path] = sprite;
                cachedPixels += (long)texture.width * texture.height;
                texture = null;
                Sprite result = sprite;
                sprite = null;
                return result;
            }
            catch (Exception exception)
            {
                failed.Add(path);
                MelonLogger.Warning("State image failed to load (" + path + "): " + exception.Message);
                return null;
            }
            finally
            {
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
        }

        internal static void Invalidate(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            failed.Remove(path);
            budgetWarning = false;
            if (sprites.TryGetValue(path, out Sprite sprite))
            {
                sprites.Remove(path);
                if (sprite != null && sprite.texture != null)
                    cachedPixels -= (long)sprite.texture.width * sprite.texture.height;
                DestroySprite(sprite);
            }
        }

        internal static void Clear()
        {
            foreach (Sprite sprite in sprites.Values)
                DestroySprite(sprite);
            sprites.Clear();
            failed.Clear();
            cachedPixels = 0;
            budgetWarning = false;
        }

        private static void WarnBudget()
        {
            if (budgetWarning)
                return;
            budgetWarning = true;
            MelonLogger.Warning("State image cache reached its image or failed-path limit; existing images were retained.");
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
                return;
            Texture texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null)
                UnityEngine.Object.Destroy(texture);
        }
    }
}
