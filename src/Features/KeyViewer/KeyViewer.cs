using System;
using System.Collections.Generic;
using System.Globalization;
using MelonLoader;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks
{
    internal enum KeyViewerEasing
    {
        Linear,
        CircIn,
        CircOut,
        CircInOut,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce
    }

    internal static class KeyViewerEasings
    {
        private static readonly DG.Tweening.Ease[] StoredValues =
        {
            DG.Tweening.Ease.Linear,
            DG.Tweening.Ease.InCirc,
            DG.Tweening.Ease.OutCirc,
            DG.Tweening.Ease.InOutCirc,
            DG.Tweening.Ease.InSine,
            DG.Tweening.Ease.OutSine,
            DG.Tweening.Ease.InOutSine,
            DG.Tweening.Ease.InQuad,
            DG.Tweening.Ease.OutQuad,
            DG.Tweening.Ease.InOutQuad,
            DG.Tweening.Ease.InCubic,
            DG.Tweening.Ease.OutCubic,
            DG.Tweening.Ease.InOutCubic,
            DG.Tweening.Ease.InQuart,
            DG.Tweening.Ease.OutQuart,
            DG.Tweening.Ease.InOutQuart,
            DG.Tweening.Ease.InQuint,
            DG.Tweening.Ease.OutQuint,
            DG.Tweening.Ease.InOutQuint,
            DG.Tweening.Ease.InExpo,
            DG.Tweening.Ease.OutExpo,
            DG.Tweening.Ease.InOutExpo,
            DG.Tweening.Ease.InElastic,
            DG.Tweening.Ease.OutElastic,
            DG.Tweening.Ease.InOutElastic,
            DG.Tweening.Ease.InBack,
            DG.Tweening.Ease.OutBack,
            DG.Tweening.Ease.InOutBack,
            DG.Tweening.Ease.InBounce,
            DG.Tweening.Ease.OutBounce,
            DG.Tweening.Ease.InOutBounce
        };

        internal static DG.Tweening.Ease Resolve(KeyViewerEasing value)
        {
            int index = (int)value;
            return index >= 0 && index < StoredValues.Length
                ? StoredValues[index]
                : DG.Tweening.Ease.OutCirc;
        }

        internal static KeyViewerEasing NormalizeStored(int value)
        {
            if (value < 0)
                return KeyViewerEasing.Linear;
            if (value >= StoredValues.Length)
                return KeyViewerEasing.CircInOut;
            return (KeyViewerEasing)value;
        }

        internal static int CatalogIndex(KeyViewerEasing value)
        {
            DG.Tweening.Ease resolved = Resolve(value);
            for (int i = 0; i < HemiEases.Values.Length; i++)
            {
                if (HemiEases.Values[i] == resolved)
                    return i;
            }
            return 0;
        }

        internal static KeyViewerEasing FromCatalogIndex(int index)
        {
            if (index < 0 || index >= HemiEases.Values.Length)
                return KeyViewerEasing.CircOut;

            DG.Tweening.Ease resolved = HemiEases.Values[index];
            for (int i = 0; i < StoredValues.Length; i++)
            {
                if (StoredValues[i] == resolved)
                    return (KeyViewerEasing)i;
            }
            return KeyViewerEasing.CircOut;
        }
    }

    internal enum KeyViewerImageFit
    {
        Cover,
        Contain,
        Fill,
        None
    }

    internal enum KeyViewerNoteAlignment
    {
        Left,
        Center,
        Right
    }

    internal enum KeyViewerNoteBorderSide
    {
        All,
        Vertical,
        Horizontal
    }

    internal enum KeyViewerCounterPlacement
    {
        Inside,
        Outside
    }

    internal enum KeyViewerCounterAlign
    {
        Top,
        Bottom,
        Left,
        Right
    }

    internal enum KeyViewerCounterAlignMode
    {
        Center,
        Between
    }

    internal enum KeyViewerElementKind
    {
        Key,

        Stat,

        Graph,

        Knob
    }

    internal enum KeyViewerStat
    {
        Kps,

        KpsAverage,

        KpsMaximum,

        Total
    }

    internal enum KeyViewerGraphType
    {
        Line,
        Bar
    }

    internal enum KeyViewerKeyMatch
    {
        Any,
        All
    }

    internal sealed class KeyViewerKeyConfig
    {
        public KeyViewerElementKind Kind;
        public KeyViewerStat Stat;
        public KeyCode Key;

        public readonly List<KeyCode> AdditionalKeys = new List<KeyCode>();
        public KeyViewerKeyMatch KeyMatch;
        public Vector2 Position;
        public Vector2 Size;

        public Color BackgroundColor;
        public Color ActiveBackgroundColor;
        public Color BorderColor;
        public Color ActiveBorderColor;
        public float BorderWidth;
        public float CornerRadius;
        public Color TextColor;
        public Color ActiveTextColor;

        public string DisplayText;

        public float FontSize;

        public bool IdleTransparent;
        public bool ActiveTransparent;

        public string IdleImagePath;
        public string ActiveImagePath;
        public KeyViewerImageFit ImageFit;
        public KeyViewerImageFit IdleImageFit;
        public KeyViewerImageFit ActiveImageFit;

        public bool ShadowEnabled;
        public Color ShadowColor;
        public Color ActiveShadowColor;
        public Vector2 ShadowOffset;
        public float ShadowBlur;
        public bool ActiveShadowEnabled;
        public Vector2 ActiveShadowOffset;
        public float ActiveShadowBlur;

        public string ElementId;
        public bool Hidden;
        public string LayerName;
        public string GroupId;
        public string CssClass;
        public bool UseInlineStyles;
        public string FontFamily;
        public string FontFilePath;
        public int FontWeight;
        public bool FontItalic;
        public bool FontUnderline;
        public bool FontStrikethrough;
        public KeyViewerGradient BackgroundGradient;
        public KeyViewerGradient ActiveBackgroundGradient;
        public KeyViewerGradient BorderGradient;
        public KeyViewerGradient ActiveBorderGradient;

        public bool RainingEffect;
        public float RainHeight;
        public float RainSpeed;
        public Color RainColor;

        public bool RainGradient;
        public Color RainColorBottom;

        public float RainOpacity;
        public float RainOpacityBottom;
        public float RainCornerRadius;
        public bool RainReverse;
        public float RainMinimumLength;
        public Color RainBorderColor;
        public float RainBorderWidth;
        public float RainBorderOpacity;
        public KeyViewerNoteBorderSide RainBorderSide;

        public float NoteWidth;
        public KeyViewerNoteAlignment NoteAlignment;
        public Vector2 NoteOffset;

        public bool GlowEnabled;
        public float GlowSize;
        public float GlowOpacity;
        public Color GlowColor;
        public bool GlowGradient;
        public Color GlowColorBottom;
        public float GlowOpacityBottom;
        public bool NoteAutoYCorrection;

        public bool CounterEnabled;
        public KeyViewerCounterPlacement CounterPlacement;
        public KeyViewerCounterAlign CounterAlign;
        public KeyViewerCounterAlignMode CounterAlignMode;
        public float CounterGap;
        public float CounterFontSize;
        public Color CounterIdleColor;
        public Color CounterActiveColor;
        public KeyViewerGradient CounterIdleGradient;
        public KeyViewerGradient CounterActiveGradient;
        public Color CounterIdleStrokeColor;
        public Color CounterActiveStrokeColor;
        public string CounterFontFamily;
        public string CounterFontFilePath;
        public int CounterFontWeight;
        public bool CounterFontItalic;
        public bool CounterFontUnderline;
        public bool CounterFontStrikethrough;
        public bool CounterAnimationEnabled;
        public float CounterAnimationScale;
        public float CounterAnimationSeconds;
        public Vector4 CounterAnimationBezier;

        public KeyViewerGraphType GraphType;
        public float GraphSpeedSeconds;
        public Color GraphColor;
        public bool GraphShowAverage;
        public bool GraphAnimationEnabled;
        public string KnobAxisId;
        public float KnobSensitivity;
        public bool KnobReverse;

        public int Count;

        public void CopyFrom(KeyViewerKeyConfig other)
        {
            Kind = other.Kind;
            Stat = other.Stat;
            Key = other.Key;
            AdditionalKeys.Clear();
            AdditionalKeys.AddRange(other.AdditionalKeys);
            KeyMatch = other.KeyMatch;
            Position = other.Position;
            Size = other.Size;

            BackgroundColor = other.BackgroundColor;
            ActiveBackgroundColor = other.ActiveBackgroundColor;
            BorderColor = other.BorderColor;
            ActiveBorderColor = other.ActiveBorderColor;
            BorderWidth = other.BorderWidth;
            CornerRadius = other.CornerRadius;
            TextColor = other.TextColor;
            ActiveTextColor = other.ActiveTextColor;
            DisplayText = other.DisplayText;
            FontSize = other.FontSize;
            IdleTransparent = other.IdleTransparent;
            ActiveTransparent = other.ActiveTransparent;
            IdleImagePath = other.IdleImagePath;
            ActiveImagePath = other.ActiveImagePath;
            ImageFit = other.ImageFit;
            IdleImageFit = other.IdleImageFit;
            ActiveImageFit = other.ActiveImageFit;
            ShadowEnabled = other.ShadowEnabled;
            ShadowColor = other.ShadowColor;
            ActiveShadowColor = other.ActiveShadowColor;
            ShadowOffset = other.ShadowOffset;
            ShadowBlur = other.ShadowBlur;
            ActiveShadowEnabled = other.ActiveShadowEnabled;
            ActiveShadowOffset = other.ActiveShadowOffset;
            ActiveShadowBlur = other.ActiveShadowBlur;
            ElementId = other.ElementId;
            Hidden = other.Hidden;
            LayerName = other.LayerName;
            GroupId = other.GroupId;
            CssClass = other.CssClass;
            UseInlineStyles = other.UseInlineStyles;
            FontFamily = other.FontFamily;
            FontFilePath = other.FontFilePath;
            FontWeight = other.FontWeight;
            FontItalic = other.FontItalic;
            FontUnderline = other.FontUnderline;
            FontStrikethrough = other.FontStrikethrough;
            BackgroundGradient = KeyViewerGradient.Clone(other.BackgroundGradient);
            ActiveBackgroundGradient = KeyViewerGradient.Clone(other.ActiveBackgroundGradient);
            BorderGradient = KeyViewerGradient.Clone(other.BorderGradient);
            ActiveBorderGradient = KeyViewerGradient.Clone(other.ActiveBorderGradient);

            RainingEffect = other.RainingEffect;
            RainHeight = other.RainHeight;
            RainSpeed = other.RainSpeed;
            RainColor = other.RainColor;
            RainGradient = other.RainGradient;
            RainColorBottom = other.RainColorBottom;
            RainOpacity = other.RainOpacity;
            RainOpacityBottom = other.RainOpacityBottom;
            RainCornerRadius = other.RainCornerRadius;
            RainReverse = other.RainReverse;
            RainMinimumLength = other.RainMinimumLength;
            RainBorderColor = other.RainBorderColor;
            RainBorderWidth = other.RainBorderWidth;
            RainBorderOpacity = other.RainBorderOpacity;
            RainBorderSide = other.RainBorderSide;
            NoteWidth = other.NoteWidth;
            NoteAlignment = other.NoteAlignment;
            NoteOffset = other.NoteOffset;
            GlowEnabled = other.GlowEnabled;
            GlowSize = other.GlowSize;
            GlowOpacity = other.GlowOpacity;
            GlowColor = other.GlowColor;
            GlowGradient = other.GlowGradient;
            GlowColorBottom = other.GlowColorBottom;
            GlowOpacityBottom = other.GlowOpacityBottom;
            NoteAutoYCorrection = other.NoteAutoYCorrection;

            CounterEnabled = other.CounterEnabled;
            CounterPlacement = other.CounterPlacement;
            CounterAlign = other.CounterAlign;
            CounterAlignMode = other.CounterAlignMode;
            CounterGap = other.CounterGap;
            CounterFontSize = other.CounterFontSize;
            CounterIdleColor = other.CounterIdleColor;
            CounterActiveColor = other.CounterActiveColor;
            CounterIdleGradient = KeyViewerGradient.Clone(other.CounterIdleGradient);
            CounterActiveGradient = KeyViewerGradient.Clone(other.CounterActiveGradient);
            CounterIdleStrokeColor = other.CounterIdleStrokeColor;
            CounterActiveStrokeColor = other.CounterActiveStrokeColor;
            CounterFontFamily = other.CounterFontFamily;
            CounterFontFilePath = other.CounterFontFilePath;
            CounterFontWeight = other.CounterFontWeight;
            CounterFontItalic = other.CounterFontItalic;
            CounterFontUnderline = other.CounterFontUnderline;
            CounterFontStrikethrough = other.CounterFontStrikethrough;
            CounterAnimationEnabled = other.CounterAnimationEnabled;
            CounterAnimationScale = other.CounterAnimationScale;
            CounterAnimationSeconds = other.CounterAnimationSeconds;
            CounterAnimationBezier = other.CounterAnimationBezier;
            GraphType = other.GraphType;
            GraphSpeedSeconds = other.GraphSpeedSeconds;
            GraphColor = other.GraphColor;
            GraphShowAverage = other.GraphShowAverage;
            GraphAnimationEnabled = other.GraphAnimationEnabled;
            KnobAxisId = other.KnobAxisId;
            KnobSensitivity = other.KnobSensitivity;
            KnobReverse = other.KnobReverse;
            Count = other.Count;
        }

        public float ResolveNoteWidth()
        {
            return NoteWidth > 0.01f ? NoteWidth : Size.x;
        }

        public float ResolveNoteX()
        {
            float width = ResolveNoteWidth();
            switch (NoteAlignment)
            {
                case KeyViewerNoteAlignment.Left:
                    return Position.x + NoteOffset.x;
                case KeyViewerNoteAlignment.Right:
                    return Position.x + Size.x - width + NoteOffset.x;
                default:
                    return Position.x + (Size.x - width) * 0.5f + NoteOffset.x;
            }
        }

        public string ResolveLabel()
        {
            if (!string.IsNullOrEmpty(DisplayText))
                return DisplayText;

            return Kind == KeyViewerElementKind.Stat || Kind == KeyViewerElementKind.Graph
                ? StatName(Stat)
                : Kind == KeyViewerElementKind.Knob
                    ? (string.IsNullOrWhiteSpace(LayerName) ? "Knob" : LayerName)
                : ResolveKeyLabel();
        }

        private string ResolveKeyLabel()
        {
            if (AdditionalKeys.Count == 0)
                return KeyViewerKeyNames.GetDisplayName(Key);
            string separator = KeyMatch == KeyViewerKeyMatch.All ? " + " : " / ";
            string result = KeyViewerKeyNames.GetDisplayName(Key);
            for (int i = 0; i < AdditionalKeys.Count; i++)
                result += separator + KeyViewerKeyNames.GetDisplayName(AdditionalKeys[i]);
            return result;
        }

        internal static string StatName(KeyViewerStat stat)
        {
            switch (stat)
            {
                case KeyViewerStat.Total:
                    return Interface.HemiLang.Get("KV_STAT_TOTAL");
                case KeyViewerStat.KpsAverage:
                    return "AVG";
                case KeyViewerStat.KpsMaximum:
                    return "MAX";
                default:
                    return Interface.HemiLang.Get("KV_STAT_KPS");
            }
        }
    }

    internal sealed class KeyViewerConfig
    {
        public readonly List<KeyViewerKeyConfig> Keys = new List<KeyViewerKeyConfig>();
        public int FontSize;
        public float PressedScale;
        public float AnimationSpeed;
        public KeyViewerEasing Easing;

        public Vector2 BoardSize;
        public StateAnchor BoardAnchor;
        public Vector2 BoardOffset;

        public bool ShowCounters;

        public bool ShowNotes = true;

        public float NoteFadeNear;
        public float NoteFadeFar;

        public int NoteFrameLimit;
        public bool DelayedNoteEnabled;
        public float ShortNoteThresholdMs;
        public float KeyDisplayDelayMs;
        public float NoteFadeTop;
        public float NoteFadeBottom;
        public float ReverseNoteFadeTop;
        public float ReverseNoteFadeBottom;
        public Color BoardBackgroundColor;
        public bool GridAlignmentGuides;
        public bool GridSpacingGuides;
        public bool GridSizeMatchGuides;
        public bool GridMinimap;
        public float GridSnapSize;
        public float GridOverlayPadding;
        public bool UseCustomCss;
        public string CustomCss;
        public readonly List<KeyViewerLayerGroup> LayerGroups = new List<KeyViewerLayerGroup>();

        public void CopyFrom(KeyViewerConfig other)
        {
            Keys.Clear();
            for (int i = 0; i < other.Keys.Count; i++)
            {
                KeyViewerKeyConfig key = new KeyViewerKeyConfig();
                key.CopyFrom(other.Keys[i]);
                Keys.Add(key);
            }

            FontSize = other.FontSize;
            PressedScale = other.PressedScale;
            AnimationSpeed = other.AnimationSpeed;
            Easing = other.Easing;
            BoardSize = other.BoardSize;
            BoardAnchor = other.BoardAnchor;
            BoardOffset = other.BoardOffset;
            ShowCounters = other.ShowCounters;
            ShowNotes = other.ShowNotes;
            NoteFadeNear = other.NoteFadeNear;
            NoteFadeFar = other.NoteFadeFar;
            NoteFrameLimit = other.NoteFrameLimit;
            DelayedNoteEnabled = other.DelayedNoteEnabled;
            ShortNoteThresholdMs = other.ShortNoteThresholdMs;
            KeyDisplayDelayMs = other.KeyDisplayDelayMs;
            NoteFadeTop = other.NoteFadeTop;
            NoteFadeBottom = other.NoteFadeBottom;
            ReverseNoteFadeTop = other.ReverseNoteFadeTop;
            ReverseNoteFadeBottom = other.ReverseNoteFadeBottom;
            BoardBackgroundColor = other.BoardBackgroundColor;
            GridAlignmentGuides = other.GridAlignmentGuides;
            GridSpacingGuides = other.GridSpacingGuides;
            GridSizeMatchGuides = other.GridSizeMatchGuides;
            GridMinimap = other.GridMinimap;
            GridSnapSize = other.GridSnapSize;
            GridOverlayPadding = other.GridOverlayPadding;
            UseCustomCss = other.UseCustomCss;
            CustomCss = other.CustomCss;
            LayerGroups.Clear();
            for (int i = 0; i < other.LayerGroups.Count; i++)
                LayerGroups.Add(new KeyViewerLayerGroup(other.LayerGroups[i]));
        }
    }

    internal static class KeyViewer
    {
        public static float GridSize => Mathf.Clamp(ViewerConfig?.GridSnapSize ?? 5f, 1f, 10f);
        public const float RainingSpeed = 400f;
        public const float RainingTrackHeight = 400f;
        public const float MinimumRainingSpeed = 70f;
        public const float MaximumRainingSpeed = 9999f;
        public const float MinimumRainingTrackHeight = 20f;
        public const float MaximumRainingTrackHeight = 2000f;
        public const float MinimumRainNoteLength = 1f;
        public const float MaximumRainNoteLength = 9999f;
        public const float MaximumGlowSize = 60f;
        public const float MaximumNoteFade = 400f;
        public const float MinimumBoardSize = 60f;
        public const float MaximumBoardSize = 4000f;
        private const float DefaultBoardWidth = 620f;
        private const float DefaultBoardHeight = 150f;

        internal const float MinimumKeySize = 16f;

        private static readonly KeyCode[] DefaultKeys =
        {
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.Semicolon
        };

        private static readonly KeyViewerConfig ViewerConfig = new KeyViewerConfig();

        private static int commitBatchDepth;
        private static bool commitBatchPending;

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> enabledEntry;
        private static MelonPreferences_Entry<string> layoutEntry;
        private static MelonPreferences_Entry<int> fontSizeEntry;
        private static MelonPreferences_Entry<float> pressedScaleEntry;
        private static MelonPreferences_Entry<float> animationSpeedEntry;
        private static MelonPreferences_Entry<int> easingEntry;
        private static MelonPreferences_Entry<float> boardWidthEntry;
        private static MelonPreferences_Entry<float> boardHeightEntry;
        private static MelonPreferences_Entry<int> boardAnchorEntry;
        private static MelonPreferences_Entry<float> boardOffsetXEntry;
        private static MelonPreferences_Entry<float> boardOffsetYEntry;
        private static MelonPreferences_Entry<bool> showCountersEntry;
        private static MelonPreferences_Entry<bool> showNotesEntry;
        private static MelonPreferences_Entry<float> noteFadeNearEntry;
        private static MelonPreferences_Entry<float> noteFadeFarEntry;
        private static GameObject behaviourObject;
        private static int keyCollectionVersion;
        private static int remappingKeyIndex = -1;

        public static bool IsRegistering { get; private set; }
        public static bool IsAddingKey => IsRegistering && remappingKeyIndex < 0;
        public static int RemappingKeyIndex => IsRegistering ? remappingKeyIndex : -1;
        public static bool Enabled => enabledEntry != null && enabledEntry.Value;
        public static int KeyCollectionVersion => keyCollectionVersion;
        public static KeyViewerConfig Config => ViewerConfig;

        public static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            enabledEntry = category.CreateEntry(
                "EnableKeyViewer",
                false,
                "Enable KeyViewer",
                "Displays configured keyboard keys with their live pressed state.");
            layoutEntry = category.CreateEntry(
                "KeyViewerLayout",
                "",
                "KeyViewer Layout",
                "JSON data for the grid-based KeyViewer layout.");
            fontSizeEntry = category.CreateEntry("KeyViewerFontSize", 22, "KeyViewer Font Size", "Maximum TextMeshPro font size.");
            pressedScaleEntry = category.CreateEntry("KeyViewerPressedScale", 0.90f, "KeyViewer Pressed Scale", "Scale applied while a key is pressed.");
            animationSpeedEntry = category.CreateEntry("KeyViewerAnimationSpeed", 14f, "KeyViewer Animation Speed", "Pressed animation response speed.");
            easingEntry = category.CreateEntry("KeyViewerEasing", (int)KeyViewerEasing.CircOut, "KeyViewer Easing", "Pressed animation easing mode.");
            boardWidthEntry = category.CreateEntry("KeyViewerBoardWidth", 0f, "Overlay Width", "Width of the Key Viewer's own area, in pixels. 0 migrates from the old screen-sized layout.");
            boardHeightEntry = category.CreateEntry("KeyViewerBoardHeight", 0f, "Overlay Height", "Height of the Key Viewer's own area, in pixels.");
            boardAnchorEntry = category.CreateEntry("KeyViewerBoardAnchor", (int)StateAnchor.TopLeft, "Overlay Anchor", "Which corner of the screen the overlay area is pinned to.");
            boardOffsetXEntry = category.CreateEntry("KeyViewerBoardOffsetX", 0f, "Overlay Offset X", "Distance in from the anchored edge; negative pushes past it.");
            boardOffsetYEntry = category.CreateEntry("KeyViewerBoardOffsetY", 0f, "Overlay Offset Y", "Distance in from the anchored edge; negative pushes past it.");
            showCountersEntry = category.CreateEntry("KeyViewerShowCounters", false, "Show Key Counters", "Master switch for the per-key press counters.");
            showNotesEntry = category.CreateEntry("KeyViewerShowNotes", true, "Show Key Notes", "Master switch for the per-key note trails.");
            noteFadeNearEntry = category.CreateEntry("KeyViewerNoteFadeNear", 0f, "Note Fade (Key End)", "Pixels of fade at the end of the note track next to the key.");
            noteFadeFarEntry = category.CreateEntry("KeyViewerNoteFadeFar", 0f, "Note Fade (Far End)", "Pixels of fade at the far end of the note track.");

            LoadConfig();
            KeyViewerProfiles.Initialize(category);

            if (Enabled)
                EnsureBehaviourObject();
        }

        public static void UpdateLifecycle()
        {
            if (Enabled)
                EnsureBehaviourObject();
            else if (behaviourObject != null && behaviourObject.activeSelf)
                behaviourObject.SetActive(false);
        }

        public static void ReloadFromPreferences()
        {
            KeyViewerHistory.Clear();
            if (category == null)
                return;

            LoadConfig();

            KeyViewerImageCache.Clear();

            KeyViewerTypography.Clear();

            StopRegistration();
            keyCollectionVersion++;
            UpdateLifecycle();
        }

        internal static void RefreshFonts()
        {
            KeyViewerTypography.Clear();
            keyCollectionVersion++;
        }

        public static void Shutdown()
        {
            StopRegistration();
            FlushSave();
            if (behaviourObject != null)
                UnityEngine.Object.Destroy(behaviourObject);

            behaviourObject = null;
            KeyViewerImageCache.Clear();
            KeyViewerTypography.Clear();
        }

        public static void SetEnabled(bool value)
        {
            if (enabledEntry == null || enabledEntry.Value == value)
                return;

            enabledEntry.Value = value;
            StopRegistration();
            UpdateLifecycle();
            Save();
        }

        public static void BeginAddingKey()
        {
            IsRegistering = true;
            remappingKeyIndex = -1;
            ReleaseFocus();
        }

        public static void BeginRemappingKey(int keyIndex)
        {
            if (keyIndex < 0 || keyIndex >= ViewerConfig.Keys.Count)
                return;

            IsRegistering = true;
            remappingKeyIndex = keyIndex;
            ReleaseFocus();
        }

        private static void ReleaseFocus()
        {
            try
            {
                UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
                if (events != null)
                    events.SetSelectedGameObject(null);
            }
            catch
            {
            }
        }

        public static void StopRegistration()
        {
            IsRegistering = false;
            remappingKeyIndex = -1;
        }

        public static bool CaptureRegistrationInput()
        {
            if (!IsRegistering)
                return false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopRegistration();
                return true;
            }

            foreach (KeyCode key in HemiModifiers.AllKeys)
            {
                if (key == KeyCode.None || IsMouseKey(key) || !Input.GetKeyDown(key))
                    continue;

                if (remappingKeyIndex >= 0 && remappingKeyIndex < ViewerConfig.Keys.Count)
                {
                    ViewerConfig.Keys[remappingKeyIndex].Key = key;
                }
                else
                {
                    AddKey(key);
                }

                StopRegistration();
                WriteLayout();
                Save();
                break;
            }

            return true;
        }

        public static KeyViewerKeyConfig GetKey(int keyIndex)
        {
            return keyIndex >= 0 && keyIndex < ViewerConfig.Keys.Count
                ? ViewerConfig.Keys[keyIndex]
                : null;
        }

        private static readonly List<KeyViewerKeyConfig> clipboard = new List<KeyViewerKeyConfig>();


        internal static void CopyKeys(IReadOnlyList<int> keyIndices)
        {
            if (keyIndices == null || keyIndices.Count == 0)
                return;

            clipboard.Clear();
            for (int i = 0; i < keyIndices.Count; i++)
            {
                KeyViewerKeyConfig source = GetKey(keyIndices[i]);
                if (source == null)
                    continue;

                KeyViewerKeyConfig copy = new KeyViewerKeyConfig();
                copy.CopyFrom(source);
                clipboard.Add(copy);
            }
        }

        internal static List<int> PasteKeys()
        {
            List<int> added = new List<int>();
            if (clipboard.Count == 0)
                return added;

            KeyViewerHistory.Record(null);

            for (int i = 0; i < clipboard.Count; i++)
            {
                if (ViewerConfig.Keys.Count >= KeyViewerProfiles.MaximumKeys)
                    break;

                KeyViewerKeyConfig source = clipboard[i];
                KeyViewerKeyConfig copy = new KeyViewerKeyConfig();
                copy.CopyFrom(source);

                copy.Count = 0;
                copy.Position = ClampPosition(
                    SnapVector(source.Position + new Vector2(source.Size.x + PasteGap, 0f)));

                ViewerConfig.Keys.Add(copy);
                added.Add(ViewerConfig.Keys.Count - 1);
            }

            if (added.Count == 0)
                return added;

            keyCollectionVersion++;
            CommitLayout();
            return added;
        }

        private const float PasteGap = 8f;

        public static void RemoveKeys(IList<int> keyIndices)
        {
            KeyViewerHistory.Record(null);
            if (keyIndices == null || keyIndices.Count == 0)
                return;

            List<int> sorted = new List<int>();
            for (int i = 0; i < keyIndices.Count; i++)
            {
                int index = keyIndices[i];
                if (index >= 0 && index < ViewerConfig.Keys.Count && !sorted.Contains(index))
                    sorted.Add(index);
            }

            sorted.Sort();
            for (int i = sorted.Count - 1; i >= 0; i--)
                ViewerConfig.Keys.RemoveAt(sorted[i]);

            if (sorted.Count == 0)
                return;

            StopRegistration();
            keyCollectionVersion++;
            WriteLayout();
            Save();
        }

        public static bool ReorderKeys(IList<int> keyIndices, int delta)
        {
            List<KeyViewerKeyConfig> keys = ViewerConfig.Keys;
            if (keyIndices == null || keyIndices.Count == 0 || delta == 0 || keys.Count < 2)
                return false;

            KeyViewerHistory.Record(null);
            int step = delta > 0 ? 1 : -1;

            Dictionary<int, int> current = new Dictionary<int, int>();
            List<int> order = new List<int>();
            for (int i = 0; i < keyIndices.Count; i++)
            {
                int index = keyIndices[i];
                if (index < 0 || index >= keys.Count || current.ContainsKey(index))
                    continue;
                current[index] = index;
                order.Add(index);
            }
            if (order.Count == 0)
                return false;

            order.Sort();
            if (step > 0)
                order.Reverse();

            HashSet<int> occupied = new HashSet<int>(order);
            bool moved = false;

            for (int i = 0; i < order.Count; i++)
            {
                int from = current[order[i]];
                int to = from + step;
                if (to < 0 || to >= keys.Count || occupied.Contains(to))
                    continue;

                KeyViewerKeyConfig swap = keys[from];
                keys[from] = keys[to];
                keys[to] = swap;

                occupied.Remove(from);
                occupied.Add(to);
                current[order[i]] = to;
                moved = true;
            }

            if (!moved)
                return false;

            for (int i = 0; i < keyIndices.Count; i++)
            {
                if (current.TryGetValue(keyIndices[i], out int next))
                    keyIndices[i] = next;
            }

            keyCollectionVersion++;
            WriteLayout();
            Save();
            return true;
        }

        public static void SetKeyPosition(int keyIndex, Vector2 position, bool save)
        {
            KeyViewerHistory.Record("SetKeyPosition");
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null)
                return;

            config.Position = ClampPosition(SnapVector(position));
            if (save)
                CommitLayout();
        }

        public static void SetKeyPositions(IList<int> keyIndices, IList<Vector2> positions, bool save)
        {
            KeyViewerHistory.Record("SetKeyPosition");
            if (keyIndices == null || positions == null)
                return;

            int count = Mathf.Min(keyIndices.Count, positions.Count);
            for (int i = 0; i < count; i++)
            {
                KeyViewerKeyConfig config = GetKey(keyIndices[i]);
                if (config != null)
                    config.Position = ClampPosition(SnapVector(positions[i]));
            }

            if (save)
                CommitLayout();
        }

        public static void SetKeySize(int keyIndex, Vector2 size, bool save)
        {
            KeyViewerHistory.Record("SetKeySize");
            ApplyKeyBounds(keyIndex, null, size, save);
        }

        public static void SetKeyBounds(int keyIndex, Vector2 position, Vector2 size, bool save)
        {
            KeyViewerHistory.Record("SetKeyBounds");
            ApplyKeyBounds(keyIndex, position, size, save);
        }

        private static void ApplyKeyBounds(int keyIndex, Vector2? position, Vector2 size, bool save)
        {
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null)
                return;

            config.Size = new Vector2(
                Snap(Mathf.Clamp(size.x, MinimumKeySize, MaximumBoardSize)),
                Snap(Mathf.Clamp(size.y, MinimumKeySize, MaximumBoardSize)));
            config.Position = ClampPosition(position.HasValue ? SnapVector(position.Value) : config.Position);
            config.BorderWidth = Mathf.Clamp(config.BorderWidth, 0f, GetMaximumBorderWidth(config));
            config.CornerRadius = Mathf.Clamp(config.CornerRadius, 0f, GetMaximumCornerRadius(config));
            if (save)
                CommitLayout();
        }

        internal static bool SetKeyBoundsBatch(
            IList<int> keyIndices,
            IList<Vector2> positions,
            IList<Vector2> sizes,
            bool save)
        {
            bool changed = false;
            if (keyIndices != null && positions != null && sizes != null)
            {
                int count = Math.Min(keyIndices.Count, Math.Min(positions.Count, sizes.Count));
                for (int i = 0; i < count; i++)
                {
                    KeyViewerKeyConfig config = GetKey(keyIndices[i]);
                    if (config == null)
                        continue;

                    Vector2 nextSize = new Vector2(
                        ClampFinite(sizes[i].x, MinimumKeySize, MaximumBoardSize, config.Size.x),
                        ClampFinite(sizes[i].y, MinimumKeySize, MaximumBoardSize, config.Size.y));
                    Vector2 nextPosition = new Vector2(
                        ClampFinite(positions[i].x, 0f, MaximumBoardSize, config.Position.x),
                        ClampFinite(positions[i].y, 0f, MaximumBoardSize, config.Position.y));
                    if (config.Size.Equals(nextSize) && config.Position.Equals(nextPosition))
                        continue;

                    config.Size = nextSize;
                    config.Position = nextPosition;
                    changed = true;
                }
            }

            if (save)
                CommitLayout();
            return changed;
        }

        public static void EditKey(int keyIndex, Action<KeyViewerKeyConfig> apply)
        {
            KeyViewerHistory.Record("edit");
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null || apply == null)
                return;

            apply(config);
            ClampKeyStyle(config);
            CommitLayout();
        }

        private static readonly Action<int, Action<int>> InvokeKeyAction = (index, action) => action(index);

        private static void ForEachKey(IList<int> keyIndices, Action<int> apply) => ForEachKey(keyIndices, apply, InvokeKeyAction);

        private static void ForEachKey<TValue>(IList<int> keyIndices, TValue value, Action<int, TValue> apply)
        {
            if (keyIndices == null)
                return;

            commitBatchDepth++;
            try
            {
                for (int i = 0; i < keyIndices.Count; i++)
                    apply(keyIndices[i], value);
            }
            finally
            {
                commitBatchDepth--;
            }

            if (commitBatchDepth == 0 && commitBatchPending)
            {
                commitBatchPending = false;
                CommitLayout();
            }
        }

        private static readonly Action<int, Action<KeyViewerKeyConfig>> ApplyEditKey = EditKey;

        public static void EditKeys(IList<int> keyIndices, Action<KeyViewerKeyConfig> apply) => ForEachKey(keyIndices, apply, ApplyEditKey);

        public static void SetKeyPosition(IList<int> keyIndices, Vector2 position, bool save) => ForEachKey(keyIndices, i => SetKeyPosition(i, position, save));

        public static void SetKeySize(IList<int> keyIndices, Vector2 size, bool save) => ForEachKey(keyIndices, i => SetKeySize(i, size, save));

        private static readonly Action<int, Color> ApplyKeyBackgroundColor = SetKeyBackgroundColor;

        public static void SetKeyBackgroundColor(IList<int> keyIndices, Color color) => ForEachKey(keyIndices, color, ApplyKeyBackgroundColor);

        private static readonly Action<int, Color> ApplyKeyBorderColor = SetKeyBorderColor;

        public static void SetKeyBorderColor(IList<int> keyIndices, Color color) => ForEachKey(keyIndices, color, ApplyKeyBorderColor);

        private static readonly Action<int, Color> ApplyKeyTextColor = SetKeyTextColor;

        public static void SetKeyTextColor(IList<int> keyIndices, Color color) => ForEachKey(keyIndices, color, ApplyKeyTextColor);

        private static readonly Action<int, float> ApplyKeyBorderWidth = SetKeyBorderWidth;

        public static void SetKeyBorderWidth(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyBorderWidth);

        private static readonly Action<int, float> ApplyKeyCornerRadius = SetKeyCornerRadius;

        public static void SetKeyCornerRadius(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyCornerRadius);

        private static readonly Action<int, bool> ApplyKeyRainingEffect = SetKeyRainingEffect;

        public static void SetKeyRainingEffect(IList<int> keyIndices, bool value) => ForEachKey(keyIndices, value, ApplyKeyRainingEffect);

        private static readonly Action<int, float> ApplyKeyRainHeight = SetKeyRainHeight;

        public static void SetKeyRainHeight(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyRainHeight);

        private static readonly Action<int, float> ApplyKeyRainSpeed = SetKeyRainSpeed;

        public static void SetKeyRainSpeed(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyRainSpeed);

        private static readonly Action<int, Color> ApplyKeyRainColor = SetKeyRainColor;

        public static void SetKeyRainColor(IList<int> keyIndices, Color color) => ForEachKey(keyIndices, color, ApplyKeyRainColor);

        private static readonly Action<int, float> ApplyKeyRainOpacity = SetKeyRainOpacity;

        public static void SetKeyRainOpacity(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyRainOpacity);

        private static readonly Action<int, float> ApplyKeyRainCornerRadius = SetKeyRainCornerRadius;

        public static void SetKeyRainCornerRadius(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyRainCornerRadius);

        private static readonly Action<int, bool> ApplyKeyRainReverse = SetKeyRainReverse;

        public static void SetKeyRainReverse(IList<int> keyIndices, bool value) => ForEachKey(keyIndices, value, ApplyKeyRainReverse);

        private static readonly Action<int, float> ApplyKeyRainMinimumLength = SetKeyRainMinimumLength;

        public static void SetKeyRainMinimumLength(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyRainMinimumLength);

        private static readonly Action<int, Color> ApplyKeyRainBorderColor = SetKeyRainBorderColor;

        public static void SetKeyRainBorderColor(IList<int> keyIndices, Color color) => ForEachKey(keyIndices, color, ApplyKeyRainBorderColor);

        private static readonly Action<int, float> ApplyKeyRainBorderWidth = SetKeyRainBorderWidth;

        public static void SetKeyRainBorderWidth(IList<int> keyIndices, float value) => ForEachKey(keyIndices, value, ApplyKeyRainBorderWidth);

        public static void ResetCounters()
        {
            for (int i = 0; i < ViewerConfig.Keys.Count; i++)
                ViewerConfig.Keys[i].Count = 0;

            countersVersion++;
            pressTimes.Clear();
            sampledKpsSum = 0f;
            sampledKpsCount = 0;
            maximumKps = 0f;
            nextStatisticsSample = 0f;
            CommitLayout();
        }

        public static int CountersVersion => countersVersion;

        private static int countersVersion;

        internal static void NotifyCounted()
        {
            countWriter.Request();

            float now = Time.unscaledTime;
            pressTimes.Enqueue(now);
            while (pressTimes.Count > 0 && now - pressTimes.Peek() > KpsWindowSeconds)
                pressTimes.Dequeue();
        }

        private const float KpsWindowSeconds = 1f;

        private static readonly Queue<float> pressTimes = new Queue<float>();
        private static float sampledKpsSum;
        private static int sampledKpsCount;
        private static float maximumKps;
        private static float nextStatisticsSample;

        internal static void UpdateStatistics(float now)
        {
            if (now + 0.0001f < nextStatisticsSample)
                return;
            nextStatisticsSample = now + 0.05f;
            float value = Kps;
            maximumKps = Mathf.Max(maximumKps, value);
            if (value > 0f)
            {
                sampledKpsSum += value;
                sampledKpsCount++;
            }
        }

        internal static float AverageKps => sampledKpsCount > 0 ? sampledKpsSum / sampledKpsCount : 0f;
        internal static float MaximumKps => maximumKps;

        public static float Kps
        {
            get
            {
                float now = Time.unscaledTime;
                while (pressTimes.Count > 0 && now - pressTimes.Peek() > KpsWindowSeconds)
                    pressTimes.Dequeue();
                return pressTimes.Count / KpsWindowSeconds;
            }
        }

        public static int TotalPresses
        {
            get
            {
                List<KeyViewerKeyConfig> keys = ViewerConfig.Keys;
                int total = 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys[i].Kind == KeyViewerElementKind.Key)
                        total += keys[i].Count;
                }
                return total;
            }
        }

        internal static int StatDisplayNumber(KeyViewerStat stat)
        {
            return stat == KeyViewerStat.Total ? TotalPresses : Mathf.RoundToInt(StatNumber(stat));
        }

        internal static float StatNumber(KeyViewerStat stat)
        {
            switch (stat)
            {
                case KeyViewerStat.Total: return TotalPresses;
                case KeyViewerStat.KpsAverage: return AverageKps;
                case KeyViewerStat.KpsMaximum: return MaximumKps;
                default: return Kps;
            }
        }

        private static readonly HemiSaveDebounce countWriter = new HemiSaveDebounce(WriteCounts, 3f);

        private static void WriteCounts()
        {
            if (layoutEntry == null)
                return;

            WriteLayout();
            WriteSettingsAndProfile();
        }

        private static void AdoptBoard(bool migratedLayout)
        {
            if (ViewerConfig.BoardSize.x >= 1f && ViewerConfig.BoardSize.y >= 1f)
            {
                ClampBoard();
                return;
            }

            if (migratedLayout)
            {
                ViewerConfig.BoardSize = new Vector2(
                    layoutDesignWidth > 0 ? layoutDesignWidth : Mathf.Max(1, Screen.width),
                    layoutDesignHeight > 0 ? layoutDesignHeight : Mathf.Max(1, Screen.height));
                ViewerConfig.BoardAnchor = StateAnchor.TopLeft;
                ViewerConfig.BoardOffset = Vector2.zero;
            }
            else
            {
                ViewerConfig.BoardSize = new Vector2(DefaultBoardWidth, DefaultBoardHeight);
                ViewerConfig.BoardAnchor = StateAnchor.BottomCenter;
                ViewerConfig.BoardOffset = new Vector2(0f, 70f);
            }

            ClampBoard();
            WriteBoardPreferences();
        }

        private static void ClampBoard()
        {
            ViewerConfig.BoardSize = new Vector2(
                Mathf.Clamp(ViewerConfig.BoardSize.x, MinimumBoardSize, MaximumBoardSize),
                Mathf.Clamp(ViewerConfig.BoardSize.y, MinimumBoardSize, MaximumBoardSize));
            ViewerConfig.BoardOffset = new Vector2(
                ClampFinite(ViewerConfig.BoardOffset.x, -4000f, 4000f, 0f),
                ClampFinite(ViewerConfig.BoardOffset.y, -4000f, 4000f, 0f));
        }

        private static void WriteBoardPreferences()
        {
            if (boardWidthEntry == null)
                return;

            boardWidthEntry.Value = ViewerConfig.BoardSize.x;
            boardHeightEntry.Value = ViewerConfig.BoardSize.y;
            boardAnchorEntry.Value = (int)ViewerConfig.BoardAnchor;
            boardOffsetXEntry.Value = ViewerConfig.BoardOffset.x;
            boardOffsetYEntry.Value = ViewerConfig.BoardOffset.y;
        }

        internal static Vector2 ResolveBoardOrigin(Rect bounds)
        {
            Vector2 board = bounds.size;
            if (board.x < 1f || board.y < 1f)
                return Vector2.zero;

            Vector2 anchor = StateGroupLayout.AnchorVector(ViewerConfig.BoardAnchor);
            Vector2 offset = ViewerConfig.BoardOffset;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            float left;
            if (anchor.x <= 0.001f)
                left = offset.x;
            else if (anchor.x >= 0.999f)
                left = screenWidth - board.x - offset.x;
            else
                left = (screenWidth - board.x) * 0.5f + offset.x;

            float top;
            if (anchor.y >= 0.999f)
                top = offset.y;
            else if (anchor.y <= 0.001f)
                top = screenHeight - board.y - offset.y;
            else
                top = (screenHeight - board.y) * 0.5f - offset.y;

            return new Vector2(left - bounds.xMin, top - bounds.yMin);
        }

        public static void SetBoardAnchor(StateAnchor anchor)
        {
            if (ViewerConfig.BoardAnchor == anchor)
                return;

            ViewerConfig.BoardAnchor = anchor;
            WriteBoardPreferences();
            Save();
        }

        public static void SetBoardOffset(Vector2 value)
        {
            Vector2 next = new Vector2(
                ClampFinite(value.x, -4000f, 4000f, 0f),
                ClampFinite(value.y, -4000f, 4000f, 0f));
            if ((ViewerConfig.BoardOffset - next).sqrMagnitude < 0.0001f)
                return;

            ViewerConfig.BoardOffset = next;
            WriteBoardPreferences();
            Save();
        }

        internal static void ApplyImportedLayout(
            List<KeyViewerKeyConfig> keys,
            Vector2 board,
            Vector2 fade,
            KeyViewerAdvancedConfigData advanced = null)
        {
            KeyViewerHistory.Clear();
            if (keys == null || keys.Count == 0)
                return;

            ViewerConfig.BoardSize = board;
            ClampBoard();

            ViewerConfig.Keys.Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                ClampKey(keys[i]);
                ViewerConfig.Keys.Add(keys[i]);
            }

            ViewerConfig.NoteFadeNear = Mathf.Clamp(fade.x, 0f, MaximumNoteFade);
            ViewerConfig.NoteFadeFar = Mathf.Clamp(fade.y, 0f, MaximumNoteFade);
            KeyViewerAdvancedPersistence.Apply(ViewerConfig, advanced);

            StopRegistration();
            keyCollectionVersion++;
            WriteBoardPreferences();
            WriteGlobalPreferences();
            CommitLayout();
        }

        public static void SetShowCounters(bool value)
        {
            if (showCountersEntry == null || ViewerConfig.ShowCounters == value)
                return;

            ViewerConfig.ShowCounters = value;
            showCountersEntry.Value = value;
            Save();
        }

        public static void SetShowNotes(bool value)
        {
            if (showNotesEntry == null || ViewerConfig.ShowNotes == value)
                return;

            ViewerConfig.ShowNotes = value;
            showNotesEntry.Value = value;
            Save();
        }

        public static void SetKeyBackgroundColor(int keyIndex, Color color)
        {
            KeyViewerHistory.Record("SetKeyBackgroundColor");
            SetKeyColor(keyIndex, color, (key, next) => key.BackgroundColor = next);
        }

        public static void SetKeyBorderColor(int keyIndex, Color color)
        {
            KeyViewerHistory.Record("SetKeyBorderColor");
            SetKeyColor(keyIndex, color, (key, next) => key.BorderColor = next);
        }

        public static void SetKeyTextColor(int keyIndex, Color color)
        {
            KeyViewerHistory.Record("SetKeyTextColor");
            SetKeyColor(keyIndex, color, (key, next) => key.TextColor = next);
        }

        public static void SetKeyBorderWidth(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyBorderWidth");
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null)
                return;

            float next = Mathf.Clamp(value, 0f, GetMaximumBorderWidth(config));
            if (Mathf.Approximately(config.BorderWidth, next))
                return;

            config.BorderWidth = next;
            CommitLayout();
        }

        public static void SetKeyCornerRadius(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyCornerRadius");
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null)
                return;

            float next = Mathf.Clamp(value, 0f, GetMaximumCornerRadius(config));
            if (Mathf.Approximately(config.CornerRadius, next))
                return;

            config.CornerRadius = next;
            CommitLayout();
        }

        public static void SetKeyRainingEffect(int keyIndex, bool value)
        {
            KeyViewerHistory.Record("SetKeyRainingEffect");
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null || config.RainingEffect == value)
                return;

            config.RainingEffect = value;
            CommitLayout();
        }

        public static void SetKeyRainHeight(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyRainHeight");
            SetKeyRainFloat(keyIndex, value, MinimumRainingTrackHeight, MaximumRainingTrackHeight, key => key.RainHeight, (key, next) => key.RainHeight = next);
        }

        public static void SetKeyRainSpeed(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyRainSpeed");
            SetKeyRainFloat(keyIndex, value, MinimumRainingSpeed, MaximumRainingSpeed, key => key.RainSpeed, (key, next) => key.RainSpeed = next);
        }

        public static void SetKeyRainColor(int keyIndex, Color color)
        {
            KeyViewerHistory.Record("SetKeyRainColor");
            SetKeyColor(keyIndex, color, (key, next) => key.RainColor = next);
        }

        public static void SetKeyRainOpacity(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyRainOpacity");
            SetKeyRainFloat(keyIndex, value, 0f, 1f, key => key.RainOpacity, (key, next) => key.RainOpacity = next);
        }

        public static void SetKeyRainCornerRadius(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyRainCornerRadius");
            SetKeyRainFloat(keyIndex, value, 0f, 100f, key => key.RainCornerRadius, (key, next) => key.RainCornerRadius = next);
        }

        public static void SetKeyRainReverse(int keyIndex, bool value)
        {
            KeyViewerHistory.Record("SetKeyRainReverse");
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null || config.RainReverse == value)
                return;

            config.RainReverse = value;
            CommitLayout();
        }

        public static void SetKeyRainMinimumLength(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyRainMinimumLength");
            SetKeyRainFloat(keyIndex, value, MinimumRainNoteLength, MaximumRainNoteLength, key => key.RainMinimumLength, (key, next) => key.RainMinimumLength = next);
        }

        public static void SetKeyRainBorderColor(int keyIndex, Color color)
        {
            KeyViewerHistory.Record("SetKeyRainBorderColor");
            SetKeyColor(keyIndex, color, (key, next) => key.RainBorderColor = next);
        }

        public static void SetKeyRainBorderWidth(int keyIndex, float value)
        {
            KeyViewerHistory.Record("SetKeyRainBorderWidth");
            SetKeyRainFloat(keyIndex, value, 0f, 20f, key => key.RainBorderWidth, (key, next) => key.RainBorderWidth = next);
        }

        public static void ResetLayout()
        {
            KeyViewerHistory.Record(null);
            for (int i = 0; i < ViewerConfig.Keys.Count; i++)
                ViewerConfig.Keys[i].Position = GetDefaultPosition(i, ViewerConfig.Keys[i].Size);

            CommitLayout();
        }

        public static void CommitLayout()
        {
            if (commitBatchDepth > 0)
            {
                commitBatchPending = true;
                return;
            }

            FitBoardToContent();
            WriteLayout();
            Save();
        }

        public static void SetFontSize(int value)
        {
            int next = Mathf.Clamp(value, 8, 100);
            if (ViewerConfig.FontSize == next)
                return;

            ViewerConfig.FontSize = next;
            fontSizeEntry.Value = next;
            Save();
        }

        public static void SetPressedScale(float value)
        {
            SetFloatValue(pressedScaleEntry, value, 0.5f, 1f, next => ViewerConfig.PressedScale = next);
        }

        public static void SetAnimationSpeed(float value)
        {
            SetFloatValue(animationSpeedEntry, value, 1f, 30f, next => ViewerConfig.AnimationSpeed = next);
        }

        public static void SetEasing(KeyViewerEasing easing)
        {
            KeyViewerEasing next = KeyViewerEasings.NormalizeStored((int)easing);
            if (ViewerConfig.Easing == next)
                return;

            ViewerConfig.Easing = next;
            easingEntry.Value = (int)next;
            Save();
        }

        public static KeyViewerProfileFile CreateProfileSnapshot(string name)
        {
            KeyViewerProfileKeyData[] keys = new KeyViewerProfileKeyData[ViewerConfig.Keys.Count];
            for (int i = 0; i < ViewerConfig.Keys.Count; i++)
                keys[i] = CreateProfileKey(ViewerConfig.Keys[i]);

            return new KeyViewerProfileFile
            {
                name = name,
                enabled = Enabled,
                designWidth = Mathf.Max(1, Screen.width),
                designHeight = Mathf.Max(1, Screen.height),
                keys = keys,
                fontSize = ViewerConfig.FontSize,
                pressedScale = ViewerConfig.PressedScale,
                animationSpeed = ViewerConfig.AnimationSpeed,
                easing = ViewerConfig.Easing.ToString(),
                showCounters = ViewerConfig.ShowCounters,
                noteFadeNear = ViewerConfig.NoteFadeNear,
                noteFadeFar = ViewerConfig.NoteFadeFar,
                advanced = KeyViewerAdvancedPersistence.Capture(ViewerConfig),
                boardWidth = ViewerConfig.BoardSize.x,
                boardHeight = ViewerConfig.BoardSize.y,
                boardAnchor = (int)ViewerConfig.BoardAnchor,
                boardOffsetX = ViewerConfig.BoardOffset.x,
                boardOffsetY = ViewerConfig.BoardOffset.y
            };
        }

        public static bool ApplyProfile(KeyViewerProfileFile profile, out string error)
        {
            if (!TryStageProfile(profile, out KeyViewerConfig staged, out error))
                return false;

            ViewerConfig.CopyFrom(staged);
            enabledEntry.Value = profile.enabled;
            ClampBoard();
            WriteBoardPreferences();
            WriteGlobalPreferences();
            WriteLayout();
            StopRegistration();
            keyCollectionVersion++;
            UpdateLifecycle();
            Save(false);
            return true;
        }

        public static bool ValidateProfile(KeyViewerProfileFile profile, out string error)
        {
            return TryStageProfile(profile, out _, out error);
        }

        private static void AddKey(KeyCode key)
        {
            KeyViewerHistory.Record(null);
            if (ViewerConfig.Keys.Count >= KeyViewerProfiles.MaximumKeys)
                return;

            KeyViewerKeyConfig config = CreateDefaultKey(key, ViewerConfig.Keys.Count);
            ViewerConfig.Keys.Add(config);
            keyCollectionVersion++;
        }

        public static int AddStat(KeyViewerStat stat)
        {
            return AddUtilityElement(KeyViewerElementKind.Stat, stat);
        }

        public static int AddGraph(KeyViewerStat stat)
        {
            return AddUtilityElement(KeyViewerElementKind.Graph, stat);
        }

        public static int AddKnob()
        {
            return AddUtilityElement(KeyViewerElementKind.Knob, KeyViewerStat.Kps);
        }

        private static int AddUtilityElement(KeyViewerElementKind kind, KeyViewerStat stat)
        {
            if (ViewerConfig.Keys.Count >= KeyViewerProfiles.MaximumKeys)
                return -1;

            KeyViewerHistory.Record(null);
            StopRegistration();

            KeyViewerKeyConfig config = CreateDefaultKey(KeyCode.None, ViewerConfig.Keys.Count);
            config.Kind = kind;
            config.Stat = stat;

            config.CounterEnabled = false;
            config.RainingEffect = false;
            config.GlowEnabled = false;

            if (kind == KeyViewerElementKind.Stat)
                config.Size = new Vector2(Mathf.Max(config.Size.x, 96f), config.Size.y);
            else if (kind == KeyViewerElementKind.Graph)
                config.Size = new Vector2(180f, 90f);
            else
                config.Size = new Vector2(80f, 80f);
            config.Position = ClampPosition(config.Position);

            ViewerConfig.Keys.Add(config);
            keyCollectionVersion++;
            WriteLayout();
            Save();
            return ViewerConfig.Keys.Count - 1;
        }

        internal static void ReplaceKeys(List<KeyViewerKeyConfig> keys, Vector2 board)
        {
            if (keys == null)
                return;

            List<KeyViewerKeyConfig> live = ViewerConfig.Keys;
            for (int i = 0; i < keys.Count && i < live.Count; i++)
                keys[i].Count = live[i].Count;

            live.Clear();
            live.AddRange(keys);

            if (board.x >= 1f && board.y >= 1f)
                ViewerConfig.BoardSize = board;

            for (int i = 0; i < live.Count; i++)
            {
                ClampKeyStyle(live[i]);
                live[i].Position = ClampPosition(live[i].Position);
            }

            StopRegistration();
            keyCollectionVersion++;
            WriteLayout();
            Save();
        }

        public static void SetStat(IList<int> keyIndices, KeyViewerStat stat)
        {
            ForEachKey(keyIndices, i =>
            {
                KeyViewerKeyConfig config = GetKey(i);
                if (config == null
                    || (config.Kind != KeyViewerElementKind.Stat && config.Kind != KeyViewerElementKind.Graph)
                    || config.Stat == stat)
                    return;

                config.Stat = stat;
                CommitLayout();
            });
        }

        private static void SetKeyColor(int keyIndex, Color color, Action<KeyViewerKeyConfig, Color> apply)
        {
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null)
                return;

            apply(config, color);
            CommitLayout();
        }

        private static void SetKeyRainFloat(
            int keyIndex,
            float value,
            float minimum,
            float maximum,
            Func<KeyViewerKeyConfig, float> read,
            Action<KeyViewerKeyConfig, float> apply)
        {
            KeyViewerKeyConfig config = GetKey(keyIndex);
            if (config == null)
                return;

            float next = Mathf.Clamp(value, minimum, maximum);
            if (Mathf.Approximately(read(config), next))
                return;

            apply(config, next);
            CommitLayout();
        }

        private static void LoadConfig()
        {
            ViewerConfig.Keys.Clear();
            layoutAdvanced = null;
            layoutKeysDropped = 0;
            string stored = layoutEntry.Value;
            loadedFromLayout = TryLoadLayout(stored, ViewerConfig.Keys);
            if (!loadedFromLayout)
            {
                for (int i = 0; i < DefaultKeys.Length; i++)
                    ViewerConfig.Keys.Add(CreateDefaultKey(DefaultKeys[i], i));
            }

            ViewerConfig.FontSize = Mathf.Clamp(fontSizeEntry.Value, 8, 100);
            ViewerConfig.PressedScale = Mathf.Clamp(pressedScaleEntry.Value, 0.5f, 1f);
            ViewerConfig.AnimationSpeed = Mathf.Clamp(animationSpeedEntry.Value, 1f, 30f);
            ViewerConfig.Easing = KeyViewerEasings.NormalizeStored(easingEntry.Value);
            ViewerConfig.ShowCounters = showCountersEntry.Value;
            ViewerConfig.ShowNotes = showNotesEntry.Value;
            ViewerConfig.NoteFadeNear = Mathf.Clamp(noteFadeNearEntry.Value, 0f, MaximumNoteFade);
            ViewerConfig.NoteFadeFar = Mathf.Clamp(noteFadeFarEntry.Value, 0f, MaximumNoteFade);
            ViewerConfig.BoardSize = new Vector2(boardWidthEntry.Value, boardHeightEntry.Value);
            ViewerConfig.BoardAnchor = (StateAnchor)Mathf.Clamp(boardAnchorEntry.Value, 0, 8);
            ViewerConfig.BoardOffset = new Vector2(boardOffsetXEntry.Value, boardOffsetYEntry.Value);
            KeyViewerAdvancedPersistence.Apply(ViewerConfig, layoutAdvanced);
            AdoptBoard(loadedFromLayout);

            if ((loadedFromLayout && layoutKeysDropped == 0) || string.IsNullOrWhiteSpace(stored))
                WriteLayout();
            else
                MelonLogger.Warning("Key Viewer kept the stored layout on disk unchanged so nothing is lost;"
                    + " it will be rewritten by the next layout edit.");
            WriteGlobalPreferences();
            Save(false);
        }

        private static bool TryLoadLayout(string json, List<KeyViewerKeyConfig> destination)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                KeyViewerLayoutData data = JsonConvert.DeserializeObject<KeyViewerLayoutData>(json);
                if (data == null || data.keys == null || data.keys.Length > KeyViewerProfiles.MaximumKeys)
                    return false;

                for (int i = 0; i < data.keys.Length; i++)
                {
                    if (!TryCreateKey(data.keys[i], out KeyViewerKeyConfig key, out string error))
                    {
                        layoutKeysDropped++;
                        MelonLogger.Warning("Key Viewer skipped stored element " + (i + 1) + " of "
                            + data.keys.Length + ": " + error);
                        continue;
                    }

                    destination.Add(key);
                }

                if (destination.Count == 0 && data.keys.Length > 0)
                {
                    MelonLogger.Warning("Key Viewer could not read any of the " + data.keys.Length
                        + " stored elements; using the default layout for this session.");
                    return false;
                }

                layoutDesignWidth = data.designWidth;
                layoutDesignHeight = data.designHeight;
                layoutAdvanced = data.advanced;
                return true;
            }
            catch (Exception exception)
            {
                destination.Clear();
                layoutAdvanced = null;
                MelonLogger.Warning("Key Viewer could not read the stored layout: " + exception.Message);
                return false;
            }
        }

        private static bool loadedFromLayout;

        private static int layoutKeysDropped;

        private static int layoutDesignWidth;
        private static int layoutDesignHeight;
        private static KeyViewerAdvancedConfigData layoutAdvanced;

        private static void WriteLayout()
        {
            KeyViewerProfileKeyData[] keys = new KeyViewerProfileKeyData[ViewerConfig.Keys.Count];
            for (int i = 0; i < ViewerConfig.Keys.Count; i++)
            {
                ClampKeyStyle(ViewerConfig.Keys[i]);
                keys[i] = CreateProfileKey(ViewerConfig.Keys[i]);
            }

            layoutEntry.Value = JsonConvert.SerializeObject(
                new KeyViewerLayoutData
                {
                    keys = keys,
                    advanced = KeyViewerAdvancedPersistence.Capture(ViewerConfig),
                    designWidth = Mathf.Max(1, Screen.width),
                    designHeight = Mathf.Max(1, Screen.height)
                },
                Formatting.None);
        }

        private static void WriteGlobalPreferences()
        {
            fontSizeEntry.Value = ViewerConfig.FontSize;
            pressedScaleEntry.Value = ViewerConfig.PressedScale;
            animationSpeedEntry.Value = ViewerConfig.AnimationSpeed;
            easingEntry.Value = (int)ViewerConfig.Easing;
            showCountersEntry.Value = ViewerConfig.ShowCounters;
            showNotesEntry.Value = ViewerConfig.ShowNotes;
            noteFadeNearEntry.Value = ViewerConfig.NoteFadeNear;
            noteFadeFarEntry.Value = ViewerConfig.NoteFadeFar;
        }

        private static KeyViewerProfileKeyData CreateProfileKey(KeyViewerKeyConfig key)
        {
            return new KeyViewerProfileKeyData
            {
                kind = key.Kind.ToString(),
                stat = key.Stat.ToString(),
                key = key.Key.ToString(),
                x = key.Position.x,
                y = key.Position.y,
                width = key.Size.x,
                height = key.Size.y,
                backgroundColor = SerializeColor(key.BackgroundColor),
                borderColor = SerializeColor(key.BorderColor),
                borderWidth = key.BorderWidth,
                cornerRadius = key.CornerRadius,
                textColor = SerializeColor(key.TextColor),
                rainingEffect = key.RainingEffect,
                rainHeight = key.RainHeight,
                rainSpeed = key.RainSpeed,
                rainColor = SerializeColor(key.RainColor),
                rainOpacity = key.RainOpacity,
                rainCornerRadius = key.RainCornerRadius,
                rainReverse = key.RainReverse,
                rainMinimumLength = key.RainMinimumLength,
                rainBorderColor = SerializeColor(key.RainBorderColor),
                rainBorderWidth = key.RainBorderWidth,

                activeBackgroundColor = SerializeColor(key.ActiveBackgroundColor),
                activeBorderColor = SerializeColor(key.ActiveBorderColor),
                activeTextColor = SerializeColor(key.ActiveTextColor),
                displayText = key.DisplayText ?? "",
                fontSize = key.FontSize,
                idleTransparent = key.IdleTransparent,
                activeTransparent = key.ActiveTransparent,
                idleImage = key.IdleImagePath ?? "",
                activeImage = key.ActiveImagePath ?? "",
                imageFit = key.ImageFit.ToString(),
                shadowEnabled = key.ShadowEnabled,
                shadowColor = SerializeColor(key.ShadowColor),
                activeShadowColor = SerializeColor(key.ActiveShadowColor),
                shadowOffsetX = key.ShadowOffset.x,
                shadowOffsetY = key.ShadowOffset.y,
                shadowBlur = key.ShadowBlur,

                rainGradient = key.RainGradient,
                rainColorBottom = SerializeColor(key.RainColorBottom),
                rainOpacityBottom = key.RainOpacityBottom,
                rainBorderOpacity = key.RainBorderOpacity,
                rainBorderSide = key.RainBorderSide.ToString(),
                noteWidth = key.NoteWidth,
                noteAlignment = key.NoteAlignment.ToString(),
                noteOffsetX = key.NoteOffset.x,
                noteOffsetY = key.NoteOffset.y,
                glowEnabled = key.GlowEnabled,
                glowSize = key.GlowSize,
                glowOpacity = key.GlowOpacity,
                glowColor = SerializeColor(key.GlowColor),

                counterEnabled = key.CounterEnabled,
                counterPlacement = key.CounterPlacement.ToString(),
                counterAlign = key.CounterAlign.ToString(),
                counterAlignMode = key.CounterAlignMode.ToString(),
                counterGap = key.CounterGap,
                counterFontSize = key.CounterFontSize,
                counterIdleColor = SerializeColor(key.CounterIdleColor),
                counterActiveColor = SerializeColor(key.CounterActiveColor),
                counterAnimationEnabled = key.CounterAnimationEnabled,
                counterAnimationScale = key.CounterAnimationScale,
                counterAnimationSeconds = key.CounterAnimationSeconds,
                count = key.Count,
                advanced = KeyViewerAdvancedPersistence.Capture(key)
            };
        }

        private static bool TryStageProfile(
            KeyViewerProfileFile profile,
            out KeyViewerConfig staged,
            out string error)
        {
            staged = null;
            error = null;
            if (profile == null || profile.keys == null || profile.keys.Length > KeyViewerProfiles.MaximumKeys)
            {
                error = Interface.HemiLang.Get("KV_ERR_KEY_LIST");
                return false;
            }

            KeyViewerConfig result = new KeyViewerConfig
            {
                FontSize = Mathf.Clamp(profile.fontSize, 8, 100),
                PressedScale = ClampFinite(profile.pressedScale, 0.5f, 1f, 0.9f),
                AnimationSpeed = ClampFinite(profile.animationSpeed, 1f, 30f, 14f),
                ShowCounters = profile.showCounters,
                NoteFadeNear = ClampFinite(profile.noteFadeNear, 0f, MaximumNoteFade, 0f),
                NoteFadeFar = ClampFinite(profile.noteFadeFar, 0f, MaximumNoteFade, 0f),
                BoardSize = profile.boardWidth >= MinimumBoardSize && profile.boardHeight >= MinimumBoardSize
                    ? new Vector2(
                        Mathf.Clamp(profile.boardWidth, MinimumBoardSize, MaximumBoardSize),
                        Mathf.Clamp(profile.boardHeight, MinimumBoardSize, MaximumBoardSize))
                    : new Vector2(
                        profile.designWidth > 0 ? profile.designWidth : Mathf.Max(1, Screen.width),
                        profile.designHeight > 0 ? profile.designHeight : Mathf.Max(1, Screen.height)),
                BoardAnchor = Enum.IsDefined(typeof(StateAnchor), profile.boardAnchor)
                    ? (StateAnchor)profile.boardAnchor
                    : StateAnchor.TopLeft,
                BoardOffset = new Vector2(
                    ClampFinite(profile.boardOffsetX, -4000f, 4000f, 0f),
                    ClampFinite(profile.boardOffsetY, -4000f, 4000f, 0f))
            };
            KeyViewerAdvancedPersistence.Apply(result, profile.advanced);

            if (!Enum.TryParse(profile.easing, true, out KeyViewerEasing easing) ||
                !Enum.IsDefined(typeof(KeyViewerEasing), easing))
            {
                error = Interface.HemiLang.Get("KV_ERR_EASING");
                return false;
            }

            result.Easing = easing;
            for (int i = 0; i < profile.keys.Length; i++)
            {
                if (!TryCreateKey(profile.keys[i], out KeyViewerKeyConfig key, out error))
                    return false;

                result.Keys.Add(key);
            }

            staged = result;
            return true;
        }

        private static bool TryCreateKey(
            KeyViewerProfileKeyData data,
            out KeyViewerKeyConfig key,
            out string error)
        {
            key = null;
            error = null;
            if (data == null)
            {
                error = Interface.HemiLang.Get("KV_ERR_KEY_MAPPING");
                return false;
            }

            bool isStat = string.Equals(data.kind, "Stat", StringComparison.OrdinalIgnoreCase);
            bool isGraph = string.Equals(data.kind, "Graph", StringComparison.OrdinalIgnoreCase);
            bool isKnob = string.Equals(data.kind, "Knob", StringComparison.OrdinalIgnoreCase);

            if (!Enum.TryParse(data.key, true, out KeyCode keyCode) ||
                !Enum.IsDefined(typeof(KeyCode), keyCode) ||
                (!isStat && !isGraph && !isKnob && keyCode == KeyCode.None) ||
                IsMouseKey(keyCode))
            {
                error = Interface.HemiLang.Get("KV_ERR_KEY_MAPPING");
                return false;
            }

            if (!IsFinite(data.x) || !IsFinite(data.y) ||
                !IsFinite(data.width) || !IsFinite(data.height) ||
                !IsFinite(data.borderWidth) || !IsFinite(data.cornerRadius) ||
                !TryParseColor(data.backgroundColor, out Color background) ||
                !TryParseColor(data.borderColor, out Color border) ||
                !TryParseColor(data.textColor, out Color text))
            {
                error = Interface.HemiLang.Get("KV_ERR_KEY_STYLE");
                return false;
            }

            bool hasRainSettings = IsFinite(data.rainHeight) && data.rainHeight > 0f &&
                IsFinite(data.rainSpeed) && data.rainSpeed > 0f;
            Color rainColor = GetDefaultRainColor();
            Color rainBorderColor = Color.white;
            if (!string.IsNullOrWhiteSpace(data.rainColor) && !TryParseColor(data.rainColor, out rainColor))
            {
                error = Interface.HemiLang.Get("KV_ERR_RAIN_COLOR");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(data.rainBorderColor) && !TryParseColor(data.rainBorderColor, out rainBorderColor))
            {
                error = Interface.HemiLang.Get("KV_ERR_RAIN_BORDER_COLOR");
                return false;
            }

            key = new KeyViewerKeyConfig
            {
                Key = keyCode,
                Kind = isStat
                    ? KeyViewerElementKind.Stat
                    : isGraph
                        ? KeyViewerElementKind.Graph
                        : isKnob ? KeyViewerElementKind.Knob : KeyViewerElementKind.Key,
                Stat = string.Equals(data.stat, "Total", StringComparison.OrdinalIgnoreCase)
                    ? KeyViewerStat.Total
                    : string.Equals(data.stat, "KpsAverage", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(data.stat, "KpsAvg", StringComparison.OrdinalIgnoreCase)
                        ? KeyViewerStat.KpsAverage
                        : string.Equals(data.stat, "KpsMaximum", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(data.stat, "KpsMax", StringComparison.OrdinalIgnoreCase)
                            ? KeyViewerStat.KpsMaximum
                            : KeyViewerStat.Kps,
                Position = new Vector2(data.x, data.y),
                Size = new Vector2(data.width, data.height),
                BackgroundColor = background,
                BorderColor = border,
                BorderWidth = data.borderWidth,
                CornerRadius = data.cornerRadius,
                TextColor = text,
                RainingEffect = data.rainingEffect,
                RainHeight = hasRainSettings ? data.rainHeight : RainingTrackHeight,
                RainSpeed = hasRainSettings ? data.rainSpeed : RainingSpeed,
                RainColor = rainColor,
                RainOpacity = hasRainSettings ? data.rainOpacity : 0.8f,
                RainCornerRadius = hasRainSettings ? data.rainCornerRadius : 2f,
                RainReverse = hasRainSettings && data.rainReverse,
                RainMinimumLength = hasRainSettings ? data.rainMinimumLength : 30f,
                RainBorderColor = rainBorderColor,
                RainBorderWidth = hasRainSettings ? data.rainBorderWidth : 0f,

                ActiveBackgroundColor = ReadColor(data.activeBackgroundColor, Brighten(background, 0.34f)),
                ActiveBorderColor = ReadColor(data.activeBorderColor, Brighten(border, 0.32f)),
                ActiveTextColor = ReadColor(data.activeTextColor, Brighten(text, 0.45f)),
                DisplayText = data.displayText ?? "",
                FontSize = Mathf.Max(0f, data.fontSize),
                IdleTransparent = data.idleTransparent,
                ActiveTransparent = data.activeTransparent,
                IdleImagePath = data.idleImage ?? "",
                ActiveImagePath = data.activeImage ?? "",
                ImageFit = ReadEnum(data.imageFit, KeyViewerImageFit.Contain),
                ShadowEnabled = data.shadowEnabled,
                ShadowColor = ReadColor(data.shadowColor, new Color(0f, 0f, 0f, 0.55f)),
                ActiveShadowColor = ReadColor(data.activeShadowColor, new Color(0f, 0f, 0f, 0.55f)),
                ShadowOffset = new Vector2(data.shadowOffsetX, data.shadowOffsetY),
                ShadowBlur = ClampFinite(data.shadowBlur, 0f, 64f, 8f),

                RainGradient = data.rainGradient,
                RainColorBottom = ReadColor(data.rainColorBottom, rainColor),
                RainOpacityBottom = data.rainOpacityBottom > 0.0001f ? data.rainOpacityBottom : (hasRainSettings ? data.rainOpacity : 0.8f),
                RainBorderOpacity = data.rainBorderOpacity > 0.0001f ? data.rainBorderOpacity : 1f,
                RainBorderSide = ReadEnum(data.rainBorderSide, KeyViewerNoteBorderSide.All),
                NoteWidth = Mathf.Max(0f, data.noteWidth),
                NoteAlignment = ReadEnum(data.noteAlignment, KeyViewerNoteAlignment.Center),
                NoteOffset = new Vector2(data.noteOffsetX, data.noteOffsetY),
                GlowEnabled = data.glowEnabled,
                GlowSize = ClampFinite(data.glowSize, 0f, MaximumGlowSize, 20f),
                GlowOpacity = data.glowOpacity > 0.0001f ? Mathf.Clamp01(data.glowOpacity) : 0.7f,
                GlowColor = ReadColor(data.glowColor, rainColor),

                CounterEnabled = data.counterEnabled,
                CounterPlacement = ReadEnum(data.counterPlacement, KeyViewerCounterPlacement.Inside),
                CounterAlign = ReadEnum(data.counterAlign, KeyViewerCounterAlign.Bottom),
                CounterAlignMode = ReadEnum(data.counterAlignMode, KeyViewerCounterAlignMode.Center),
                CounterGap = ClampFinite(data.counterGap, 0f, 200f, 4f),
                CounterFontSize = data.counterFontSize > 0.0001f ? data.counterFontSize : 14f,
                CounterIdleColor = ReadColor(data.counterIdleColor, new Color(1f, 1f, 1f, 0.72f)),
                CounterActiveColor = ReadColor(data.counterActiveColor, Color.white),
                CounterAnimationEnabled = data.counterAnimationEnabled,
                CounterAnimationScale = data.counterAnimationScale > 0.0001f ? data.counterAnimationScale : 1.25f,
                CounterAnimationSeconds = data.counterAnimationSeconds > 0.0001f ? data.counterAnimationSeconds : 0.18f,
                Count = Mathf.Max(0, data.count)
            };
            KeyViewerAdvancedPersistence.Apply(key, data.advanced);
            ClampKeyGeometry(key, false);
            ClampKeyStyle(key);
            return true;
        }

        private static Color ReadColor(string serialized, Color fallback)
        {
            return TryParseColor(serialized, out Color parsed) ? parsed : fallback;
        }

        private static T ReadEnum<T>(string serialized, T fallback) where T : struct
        {
            return !string.IsNullOrWhiteSpace(serialized) &&
                Enum.TryParse(serialized, true, out T parsed) &&
                Enum.IsDefined(typeof(T), parsed)
                ? parsed
                : fallback;
        }

        private static Color Brighten(Color color, float amount)
        {
            Color result = Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
            result.a = color.a;
            return result;
        }

        private static KeyViewerKeyConfig CreateDefaultKey(KeyCode key, int index)
        {
            Vector2 size = new Vector2(60f, 60f);

            KeyViewerKeyConfig config = new KeyViewerKeyConfig
            {
                Key = key,
                Position = GetDefaultPosition(index, size),
                Size = size,

                BackgroundColor = new Color32(0x00, 0x00, 0x00, 0x80),
                ActiveBackgroundColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                BorderColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                ActiveBorderColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                BorderWidth = 3f,
                CornerRadius = 10f,
                TextColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                ActiveTextColor = new Color32(0x00, 0x00, 0x00, 0xFF),
                DisplayText = "",
                FontSize = 0f,
                IdleTransparent = false,
                ActiveTransparent = false,
                IdleImagePath = "",
                ActiveImagePath = "",
                ImageFit = KeyViewerImageFit.Contain,

                ShadowEnabled = false,
                ShadowColor = new Color32(0x00, 0x00, 0x00, 0x8C),
                ActiveShadowColor = new Color32(0x00, 0x00, 0x00, 0x8C),
                ShadowOffset = new Vector2(0f, 3f),
                ShadowBlur = 8f,

                RainingEffect = true,
                RainHeight = 400f,
                RainSpeed = 400f,
                RainColor = new Color32(0x6B, 0x66, 0xFF, 0xFF),
                RainGradient = true,
                RainColorBottom = new Color32(0x94, 0x90, 0xFF, 0xFF),

                RainOpacity = 0f,
                RainOpacityBottom = 1f,
                RainCornerRadius = 10f,
                RainReverse = false,
                RainMinimumLength = 30f,
                RainBorderColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                RainBorderWidth = 0f,
                RainBorderOpacity = 1f,
                RainBorderSide = KeyViewerNoteBorderSide.All,
                NoteWidth = 0f,
                NoteAlignment = KeyViewerNoteAlignment.Center,
                NoteOffset = Vector2.zero,

                GlowEnabled = true,
                GlowSize = 20f,
                GlowOpacity = 0.7f,
                GlowColor = new Color32(0x2F, 0x42, 0xFF, 0xFF),

                CounterEnabled = true,
                CounterPlacement = KeyViewerCounterPlacement.Inside,
                CounterAlign = KeyViewerCounterAlign.Bottom,
                CounterAlignMode = KeyViewerCounterAlignMode.Center,
                CounterGap = 5f,
                CounterFontSize = 20f,
                CounterIdleColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                CounterActiveColor = new Color32(0x00, 0x00, 0x00, 0xFF),
                CounterAnimationEnabled = false,
                CounterAnimationScale = 1.25f,
                CounterAnimationSeconds = 0.18f
            };
            KeyViewerAdvancedPersistence.ApplyDefaults(config);
            return config;
        }

        private static Vector2 GetDefaultPosition(int index, Vector2 size)
        {
            const int columns = 10;

            float strideX = Snap(size.x) + GridSize;
            float strideY = Snap(size.y) + GridSize;

            int column = index % columns;
            int row = index / columns;

            return ClampPosition(new Vector2(column * strideX, row * strideY));
        }

        private static void ClampKey(KeyViewerKeyConfig key)
        {
            ClampKeyStyle(key);
            ClampKeyGeometry(key, true);
        }

        private static void ClampKeyGeometry(KeyViewerKeyConfig key, bool snap)
        {
            Vector2 size = new Vector2(
                ClampFinite(key.Size.x, MinimumKeySize, MaximumBoardSize, MinimumKeySize),
                ClampFinite(key.Size.y, MinimumKeySize, MaximumBoardSize, MinimumKeySize));
            Vector2 position = new Vector2(
                ClampFinite(key.Position.x, 0f, MaximumBoardSize, 0f),
                ClampFinite(key.Position.y, 0f, MaximumBoardSize, 0f));
            if (snap)
            {
                size = new Vector2(
                    Mathf.Clamp(Snap(size.x), MinimumKeySize, MaximumBoardSize),
                    Mathf.Clamp(Snap(size.y), MinimumKeySize, MaximumBoardSize));
                position = ClampPosition(SnapVector(position));
            }
            key.Size = size;
            key.Position = position;
        }

        private static void ClampKeyStyle(KeyViewerKeyConfig key)
        {
            key.BorderWidth = ClampFinite(key.BorderWidth, 0f, 20f, 0f);
            key.CornerRadius = ClampFinite(key.CornerRadius, 0f, 100f, 0f);
            key.RainHeight = ClampFinite(key.RainHeight, MinimumRainingTrackHeight, MaximumRainingTrackHeight, RainingTrackHeight);
            key.RainSpeed = ClampFinite(key.RainSpeed, MinimumRainingSpeed, MaximumRainingSpeed, RainingSpeed);
            key.RainOpacity = ClampFinite(key.RainOpacity, 0f, 1f, 0.8f);
            key.RainCornerRadius = ClampFinite(key.RainCornerRadius, 0f, 100f, 2f);
            key.RainMinimumLength = ClampFinite(key.RainMinimumLength, MinimumRainNoteLength, MaximumRainNoteLength, 30f);
            key.RainBorderWidth = ClampFinite(key.RainBorderWidth, 0f, 20f, 0f);
            key.RainOpacityBottom = ClampFinite(key.RainOpacityBottom, 0f, 1f, key.RainOpacity);
            key.RainBorderOpacity = ClampFinite(key.RainBorderOpacity, 0f, 1f, 1f);
            key.NoteWidth = ClampFinite(key.NoteWidth, 0f, 2000f, 0f);
            key.NoteOffset = new Vector2(
                ClampFinite(key.NoteOffset.x, -1000f, 1000f, 0f),
                ClampFinite(key.NoteOffset.y, -1000f, 1000f, 0f));
            key.GlowSize = ClampFinite(key.GlowSize, 0f, MaximumGlowSize, 20f);
            key.GlowOpacity = ClampFinite(key.GlowOpacity, 0f, 1f, 0.7f);
            key.FontSize = ClampFinite(key.FontSize, 0f, 200f, 0f);
            key.ShadowBlur = ClampFinite(key.ShadowBlur, 0f, 64f, 8f);
            key.ShadowOffset = new Vector2(
                ClampFinite(key.ShadowOffset.x, -64f, 64f, 0f),
                ClampFinite(key.ShadowOffset.y, -64f, 64f, 3f));
            key.CounterGap = ClampFinite(key.CounterGap, 0f, 200f, 4f);
            key.CounterFontSize = ClampFinite(key.CounterFontSize, 6f, 100f, 14f);
            key.CounterAnimationScale = ClampFinite(key.CounterAnimationScale, 1f, 3f, 1.25f);
            key.CounterAnimationSeconds = ClampFinite(key.CounterAnimationSeconds, 0.02f, 2f, 0.18f);
            key.Count = Mathf.Max(0, key.Count);
            if (key.DisplayText == null)
                key.DisplayText = "";
            if (key.IdleImagePath == null)
                key.IdleImagePath = "";
            if (key.ActiveImagePath == null)
                key.ActiveImagePath = "";
        }

        private static Color GetDefaultRainColor()
        {
            return new Color(47f / 255f, 66f / 255f, 1f, 1f);
        }

        private static float GetMaximumBorderWidth(KeyViewerKeyConfig key)
        {
            return Mathf.Max(0f, Mathf.Min(20f, Mathf.Min(key.Size.x, key.Size.y) * 0.5f));
        }

        private static float GetMaximumCornerRadius(KeyViewerKeyConfig key)
        {
            return Mathf.Max(0f, Mathf.Min(100f, Mathf.Min(key.Size.x, key.Size.y) * 0.5f));
        }

        private static Vector2 ClampPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, 0f, MaximumBoardSize),
                Mathf.Clamp(position.y, 0f, MaximumBoardSize));
        }

        public static Rect ContentBounds()
        {
            IReadOnlyList<KeyViewerKeyConfig> keys = ViewerConfig.Keys;
            if (keys.Count == 0)
                return new Rect(0f, 0f, MinimumBoardSize, MinimumBoardSize);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < keys.Count; i++)
            {
                KeyViewerKeyConfig key = keys[i];
                if (key == null || key.Hidden)
                    continue;

                minX = Mathf.Min(minX, key.Position.x);
                maxX = Mathf.Max(maxX, key.Position.x + key.Size.x);

                float top = key.Position.y;
                float bottom = key.Position.y + key.Size.y;

                if (ViewerConfig.ShowNotes && key.RainingEffect)
                {
                    float reach = Mathf.Max(0f, key.RainHeight);
                    if (key.RainReverse)
                        bottom += reach;
                    else
                        top -= reach;
                }

                minY = Mathf.Min(minY, top);
                maxY = Mathf.Max(maxY, bottom);
            }

            if (minX > maxX || minY > maxY)
                return new Rect(0f, 0f, MinimumBoardSize, MinimumBoardSize);

            float padding = Mathf.Clamp(ViewerConfig.GridOverlayPadding, 0f, 30f);
            return new Rect(
                minX - padding,
                minY - padding,
                Mathf.Max(MinimumBoardSize, maxX - minX + padding * 2f),
                Mathf.Max(MinimumBoardSize, maxY - minY + padding * 2f));
        }

        private static void FitBoardToContent()
        {
            Vector2 size = ContentBounds().size;
            if ((ViewerConfig.BoardSize - size).sqrMagnitude > 0.0001f)
                ViewerConfig.BoardSize = size;

            if (boardWidthEntry != null)
                boardWidthEntry.Value = ViewerConfig.BoardSize.x;
            if (boardHeightEntry != null)
                boardHeightEntry.Value = ViewerConfig.BoardSize.y;
        }

        private static Vector2 SnapVector(Vector2 value)
        {
            return new Vector2(Snap(value.x), Snap(value.y));
        }

        private static float Snap(float value)
        {
            return Mathf.Round(value / GridSize) * GridSize;
        }

        private static bool TryParseColor(string serialized, out Color color)
        {
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                string normalized = serialized.StartsWith("#", StringComparison.Ordinal)
                    ? serialized
                    : "#" + serialized;
                if (ColorUtility.TryParseHtmlString(normalized, out color))
                    return true;
            }

            color = Color.clear;
            return false;
        }

        private static string SerializeColor(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float ClampFinite(float value, float minimum, float maximum, float fallback)
        {
            return IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : fallback;
        }

        private static void SetFloatValue(
            MelonPreferences_Entry<float> entry,
            float value,
            float minimum,
            float maximum,
            Action<float> apply)
        {
            float next = Mathf.Clamp(value, minimum, maximum);
            if (Mathf.Approximately(entry.Value, next))
                return;

            entry.Value = next;
            apply(next);
            Save();
        }

        private static void EnsureBehaviourObject()
        {
            HemiOverlayHost.Ensure<KeyViewerBehaviour>(ref behaviourObject, "HemiTweaks_KeyViewer");
        }

        internal static bool IsMouseKey(KeyCode key)
        {
            return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
        }

        private static void Save(bool autoSaveProfile = true)
        {
            if (autoSaveProfile)
                writer.Request();
            else
                writeSettingsOnly.Request();
        }

        public static void TickSave()
        {
            countWriter.Tick();
            writeSettingsOnly.Tick();
            writer.Tick();
        }

        public static void FlushSave()
        {
            countWriter.Flush();
            writeSettingsOnly.Flush();
            writer.Flush();
        }

        private static readonly HemiSaveDebounce writeSettingsOnly = new HemiSaveDebounce(WriteSettingsFile);
        private static readonly HemiSaveDebounce writer = new HemiSaveDebounce(WriteSettingsAndProfile);

        private static void WriteSettingsFile()
        {
            category?.SaveToFile(false);
        }

        private static void WriteSettingsAndProfile()
        {
            category?.SaveToFile(false);
            KeyViewerProfiles.AutoSaveActive();
        }

        [Serializable]
        private sealed class KeyViewerLayoutData
        {
            public KeyViewerProfileKeyData[] keys;
            public KeyViewerAdvancedConfigData advanced;

            public int designWidth;
            public int designHeight;
        }
    }

    [DefaultExecutionOrder(10000)]
    internal sealed class KeyViewerBehaviour : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform boardBackgroundRect;
        private Image boardBackground;
        private RectTransform rainContainer;
        private RectTransform keyContainer;
        private readonly List<KeyViewerWidget> widgets = new List<KeyViewerWidget>();
        private readonly KeyViewerSpriteFactory sprites = new KeyViewerSpriteFactory();
        private int observedCollectionVersion = -1;
        private int observedCountersVersion = -1;

        private void Awake()
        {
            BuildCanvas();
        }

        private void OnDisable()
        {
            ForceReleaseAll();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                ForceReleaseAll();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ForceReleaseAll();
        }

        private void Update()
        {
            if (!KeyViewer.Enabled)
                return;

            KeyViewerConfig config = KeyViewer.Config;
            float now = Time.unscaledTime;
            float deltaTime = Time.unscaledDeltaTime;
            KeyViewer.UpdateStatistics(now);
            if (observedCollectionVersion != KeyViewer.KeyCollectionVersion)
            {
                ForceReleaseAll();
                observedCollectionVersion = KeyViewer.KeyCollectionVersion;
            }

            bool countersReset = observedCountersVersion != KeyViewer.CountersVersion;
            observedCountersVersion = KeyViewer.CountersVersion;

            Rect contentBounds = KeyViewer.ContentBounds();
            Vector2 origin = KeyViewer.ResolveBoardOrigin(contentBounds);
            boardBackgroundRect.anchoredPosition = new Vector2(
                origin.x + contentBounds.xMin,
                -(origin.y + contentBounds.yMin));
            boardBackgroundRect.sizeDelta = contentBounds.size;
            boardBackground.color = config.BoardBackgroundColor;
            boardBackground.enabled = config.BoardBackgroundColor.a > 0.001f;

            SyncWidgets(config.Keys.Count);
            for (int i = 0; i < config.Keys.Count; i++)
            {
                KeyViewerKeyConfig key = config.Keys[i];
                KeyViewerWidget widget = widgets[i];
                if (widget.gameObject.activeSelf == key.Hidden)
                    widget.gameObject.SetActive(!key.Hidden);
                if (key.Hidden)
                    continue;
                float halfWidth = key.Size.x * 0.5f;
                float halfHeight = key.Size.y * 0.5f;

                widget.SetPosition(new Vector2(
                    origin.x + key.Position.x + halfWidth,
                    -(origin.y + key.Position.y) - halfHeight));
                ResolveKeyState(key, out bool held, out bool pressed, out bool released);
                widget.SetVisual(
                    key,
                    config,
                    origin,
                    contentBounds,
                    sprites,
                    held,
                    pressed,
                    released,
                    countersReset,
                    now,
                    deltaTime);
            }
        }

        private static void ResolveKeyState(KeyViewerKeyConfig key, out bool held, out bool pressed, out bool released)
        {
            held = false;
            pressed = false;
            released = false;
            if (key.Kind != KeyViewerElementKind.Key)
                return;

            bool all = key.KeyMatch == KeyViewerKeyMatch.All;
            held = Input.GetKey(key.Key);
            for (int i = 0; i < key.AdditionalKeys.Count; i++)
            {
                bool member = Input.GetKey(key.AdditionalKeys[i]);
                held = all ? held & member : held | member;
            }

            if (held)
            {
                pressed = Input.GetKeyDown(key.Key);
                for (int i = 0; !pressed && i < key.AdditionalKeys.Count; i++)
                    pressed = Input.GetKeyDown(key.AdditionalKeys[i]);
                return;
            }

            released = Input.GetKeyUp(key.Key);
            for (int i = 0; !released && i < key.AdditionalKeys.Count; i++)
                released = Input.GetKeyUp(key.AdditionalKeys[i]);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                if (widgets[i] != null)
                    Destroy(widgets[i].gameObject);
            }

            widgets.Clear();
            sprites.Dispose();
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("KeyViewerCanvas");
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            GameObject boardObject = new GameObject("KeyViewerBoardBackground");
            boardObject.transform.SetParent(canvas.transform, false);
            boardBackgroundRect = boardObject.AddComponent<RectTransform>();
            boardBackgroundRect.anchorMin = new Vector2(0f, 1f);
            boardBackgroundRect.anchorMax = new Vector2(0f, 1f);
            boardBackgroundRect.pivot = new Vector2(0f, 1f);
            boardBackground = boardObject.AddComponent<Image>();
            boardBackground.raycastTarget = false;

            rainContainer = CreateTopLeftContainer("KeyViewerRainingEffects", canvas.transform);
            keyContainer = CreateTopLeftContainer("KeyViewerKeys", canvas.transform);
        }

        private static RectTransform CreateTopLeftContainer(string name, Transform parent)
        {
            GameObject containerObject = new GameObject(name);
            containerObject.transform.SetParent(parent, false);
            RectTransform rect = containerObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void SyncWidgets(int count)
        {
            while (widgets.Count < count)
                widgets.Add(CreateWidget(keyContainer, rainContainer));

            while (widgets.Count > count)
            {
                int last = widgets.Count - 1;
                Destroy(widgets[last].gameObject);
                widgets.RemoveAt(last);
            }

            for (int i = 0; i < widgets.Count; i++)
                widgets[i].ApplyDrawOrder(i);
        }

        private static KeyViewerWidget CreateWidget(RectTransform parent, RectTransform rainRoot)
        {
            RectTransform rainParent = CreateTopLeftContainer("KeyViewerRain", rainRoot);

            GameObject rootObject = new GameObject("KeyViewerKey");
            rootObject.transform.SetParent(parent, false);
            RectTransform rootRect = rootObject.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            Image shadow = CreateImage("Shadow", rootObject.transform);

            Image border = CreateImage("Border", rootObject.transform);
            Stretch(border.rectTransform);
            KeyViewerLinearGradient borderGradient = border.gameObject.AddComponent<KeyViewerLinearGradient>();

            Image background = CreateImage("Background", rootObject.transform);
            RectTransform backgroundRect = background.rectTransform;
            Stretch(backgroundRect);
            KeyViewerLinearGradient backgroundGradient = background.gameObject.AddComponent<KeyViewerLinearGradient>();

            GameObject graphObject = new GameObject("Graph");
            graphObject.transform.SetParent(backgroundRect, false);
            RectTransform graphRect = graphObject.AddComponent<RectTransform>();
            Stretch(graphRect);
            graphRect.offsetMin = new Vector2(6f, 6f);
            graphRect.offsetMax = new Vector2(-6f, -6f);
            KeyViewerGraphGraphic graph = graphObject.AddComponent<KeyViewerGraphGraphic>();
            graph.raycastTarget = false;

            GameObject imageHost = new GameObject("Images");
            imageHost.transform.SetParent(backgroundRect, false);
            RectTransform imageRect = imageHost.AddComponent<RectTransform>();
            Stretch(imageRect);
            imageHost.AddComponent<RectMask2D>();

            Image keyImage = CreateImage("KeyImage", imageRect);

            Image knobIndicator = CreateImage("KnobIndicator", backgroundRect);
            RectTransform knobIndicatorRect = knobIndicator.rectTransform;
            knobIndicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobIndicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobIndicatorRect.pivot = new Vector2(0.5f, 0f);
            knobIndicatorRect.anchoredPosition = Vector2.zero;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(backgroundRect, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(4f, 3f);
            labelRect.offsetMax = new Vector2(-4f, -3f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            HemiTextMaterial.ApplyPlain(label);

            GameObject counterObject = new GameObject("Counter");
            counterObject.transform.SetParent(rootObject.transform, false);
            RectTransform counterRect = counterObject.AddComponent<RectTransform>();

            TextMeshProUGUI counter = counterObject.AddComponent<TextMeshProUGUI>();
            counter.alignment = TextAlignmentOptions.Center;
            counter.textWrappingMode = TextWrappingModes.NoWrap;
            counter.overflowMode = TextOverflowModes.Overflow;
            counter.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                counter.font = TMP_Settings.defaultFontAsset;
            HemiTextMaterial.ApplyPlain(counter);
            KeyViewerLinearGradient counterGradient = counterObject.AddComponent<KeyViewerLinearGradient>();

            KeyViewerWidget widget = rootObject.AddComponent<KeyViewerWidget>();
            widget.Initialize(
                rootRect, shadow, border, borderGradient, backgroundRect, background, backgroundGradient,
                graph, imageRect, keyImage, knobIndicator, label, counterRect, counter, counterGradient, rainParent);
            return widget;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            Image image = obj.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ForceReleaseAll()
        {
            for (int i = 0; i < widgets.Count; i++)
                widgets[i].ForceRelease();
        }
    }

    internal sealed class KeyViewerWidget : MonoBehaviour
    {
        private const int MaximumRainSegments = 64;

        private RectTransform rectTransform;
        private Image shadow;
        private Image border;
        private KeyViewerLinearGradient borderGradient;
        private RectTransform backgroundRect;
        private Image background;
        private KeyViewerLinearGradient backgroundGradient;
        private KeyViewerGraphGraphic graph;
        private RectTransform imageRect;
        private Image keyImage;
        private Image knobIndicator;
        private TextMeshProUGUI label;
        private RectTransform counterRect;
        private TextMeshProUGUI counter;
        private KeyViewerLinearGradient counterGradient;
        private RectTransform rainParent;
        private readonly List<RainSegment> rainSegments = new List<RainSegment>();
        private readonly Queue<RainSegment> rainPool = new Queue<RainSegment>();
        private float currentScale = 1f;
        private float appliedScale = float.NaN;
        private float appliedCounterScale = float.NaN;
        private float scaleStart = 1f;
        private float scaleTarget = 1f;
        private float scaleElapsed;
        private float scaleSpeed;
        private KeyViewerEasing scaleEasing = KeyViewerEasing.CircOut;
        private bool scaleInitialized;
        private bool scalePressed;
        private bool wasPressed;
        private int shownCount = -1;
        private int shownStat = int.MinValue;
        private string shownStatText;
        private float counterPulse;
        private bool rawWasPressed;
        private bool displayedPressed;
        private readonly Queue<DelayedTransition> delayedTransitions = new Queue<DelayedTransition>();
        private float nextGraphSample;
        private KeyViewerStat observedGraphStat = (KeyViewerStat)(-1);
        private float knobAngle;
        private float nextNoteVisualTime;

        public void Initialize(
            RectTransform rect,
            Image shadowImage,
            Image borderImage,
            KeyViewerLinearGradient borderGradientEffect,
            RectTransform backgroundTransform,
            Image backgroundImage,
            KeyViewerLinearGradient backgroundGradientEffect,
            KeyViewerGraphGraphic graphGraphic,
            RectTransform imageTransform,
            Image imageComponent,
            Image knobIndicatorImage,
            TextMeshProUGUI text,
            RectTransform counterTransform,
            TextMeshProUGUI counterText,
            KeyViewerLinearGradient counterGradientEffect,
            RectTransform rainContainer)
        {
            rectTransform = rect;
            shadow = shadowImage;
            border = borderImage;
            borderGradient = borderGradientEffect;
            backgroundRect = backgroundTransform;
            background = backgroundImage;
            backgroundGradient = backgroundGradientEffect;
            graph = graphGraphic;
            imageRect = imageTransform;
            keyImage = imageComponent;
            knobIndicator = knobIndicatorImage;
            label = text;
            counterRect = counterTransform;
            counter = counterText;
            counterGradient = counterGradientEffect;
            rainParent = rainContainer;
        }

        public void SetPosition(Vector2 position)
        {
            rectTransform.anchoredPosition = position;
        }

        public void ApplyDrawOrder(int order)
        {
            if (transform.GetSiblingIndex() != order)
                transform.SetSiblingIndex(order);

            if (rainParent != null && rainParent.GetSiblingIndex() != order)
                rainParent.SetSiblingIndex(order);
        }

        public void SetVisual(
            KeyViewerKeyConfig key,
            KeyViewerConfig global,
            Vector2 origin,
            Rect contentBounds,
            KeyViewerSpriteFactory sprites,
            bool isPressed,
            bool pressedThisFrame,
            bool releasedThisFrame,
            bool countersReset,
            float now,
            float deltaTime)
        {
            ResolveDisplayedInput(global, isPressed, pressedThisFrame, releasedThisFrame, now,
                out isPressed, out pressedThisFrame, out releasedThisFrame);
            rectTransform.sizeDelta = key.Size;

            int outerRadius = Mathf.RoundToInt(key.CornerRadius);
            int innerRadius = Mathf.RoundToInt(Mathf.Max(0f, key.CornerRadius - key.BorderWidth));
            Sprite backgroundSprite = sprites.Rounded(innerRadius);
            bool transparent = isPressed ? key.ActiveTransparent : key.IdleTransparent;

            border.enabled = !transparent && key.BorderWidth > 0.01f;
            KeyViewerGradient resolvedBorderGradient = isPressed ? key.ActiveBorderGradient : key.BorderGradient;
            border.color = resolvedBorderGradient == null
                ? (isPressed ? key.ActiveBorderColor : key.BorderColor)
                : Color.white;
            borderGradient.Apply(resolvedBorderGradient);
            float inset = Mathf.Clamp(key.BorderWidth, 0f, Mathf.Min(key.Size.x, key.Size.y) * 0.5f);

            border.sprite = sprites.Ring(outerRadius, Mathf.Max(1, Mathf.CeilToInt(inset) + 1));

            backgroundRect.offsetMin = new Vector2(inset, inset);
            backgroundRect.offsetMax = new Vector2(-inset, -inset);
            background.sprite = backgroundSprite;
            background.enabled = !transparent;
            KeyViewerGradient resolvedBackgroundGradient = isPressed
                ? key.ActiveBackgroundGradient
                : key.BackgroundGradient;
            background.color = resolvedBackgroundGradient == null
                ? (isPressed ? key.ActiveBackgroundColor : key.BackgroundColor)
                : Color.white;
            backgroundGradient.Apply(resolvedBackgroundGradient);

            UpdateShadow(key, sprites, isPressed, transparent, outerRadius);
            UpdateImage(key, isPressed);
            UpdateGraph(key, now, countersReset);
            UpdateKnob(key, sprites, deltaTime);

            TMP_FontAsset resolvedFont = KeyViewerTypography.Resolve(key.FontFamily, key.FontFilePath);
            if (resolvedFont != null && label.font != resolvedFont)
            {
                label.font = resolvedFont;
                HemiTextMaterial.ApplyPlain(label);
            }
            KeyViewerTypography.ApplyStyle(label, key.FontWeight, key.FontItalic, key.FontUnderline, key.FontStrikethrough);

            float fontSize = key.FontSize > 0.01f ? key.FontSize : global.FontSize;

            string text;
            if (key.Kind == KeyViewerElementKind.Stat)
            {
                int statNumber = KeyViewer.StatDisplayNumber(key.Stat);
                if (statNumber != shownStat || shownStatText == null)
                {
                    shownStat = statNumber;
                    shownStatText = statNumber.ToString(CultureInfo.InvariantCulture);
                }
                text = shownStatText;
            }
            else
            {
                text = key.Kind == KeyViewerElementKind.Graph ? "" : key.ResolveLabel();
            }
            if (label.text != text)
                label.text = text;
            label.fontSizeMin = Mathf.Max(6f, fontSize * 0.42f);
            label.fontSizeMax = fontSize;
            label.enableVertexGradient = false;
            label.color = isPressed ? key.ActiveTextColor : key.TextColor;

            UpdateCounter(key, global, isPressed, pressedThisFrame, countersReset, deltaTime);

            UpdateScale(global, isPressed, deltaTime);
            UpdateRainingEffect(
                key, global, origin, contentBounds, sprites, isPressed, pressedThisFrame, releasedThisFrame, now);
        }

        private void UpdateScale(KeyViewerConfig global, bool isPressed, float deltaTime)
        {
            float pressedScale = IsFiniteScale(global.PressedScale)
                ? Mathf.Clamp(global.PressedScale, 0.5f, 1f)
                : 0.9f;
            float target = isPressed ? pressedScale : 1f;
            float speed = IsFiniteScale(global.AnimationSpeed) ? Mathf.Max(0f, global.AnimationSpeed) : 0f;
            KeyViewerEasing easing = global.Easing;

            if (!scaleInitialized || scalePressed != isPressed || scaleTarget != target ||
                scaleSpeed != speed || scaleEasing != easing)
            {
                scaleInitialized = true;
                scalePressed = isPressed;
                scaleStart = IsFiniteScale(currentScale) && currentScale >= 0f ? currentScale : 1f;
                scaleTarget = target;
                scaleSpeed = speed;
                scaleEasing = easing;
                scaleElapsed = 0f;
            }

            if (scaleSpeed <= 0f)
            {
                currentScale = scaleTarget;
            }
            else
            {
                float step = IsFiniteScale(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;
                scaleElapsed += step;
                float progress = 1f - Mathf.Exp(-scaleSpeed * scaleElapsed);
                if (!IsFiniteScale(progress) || progress >= 0.999f)
                    progress = 1f;

                float eased = HemiEases.Evaluate(KeyViewerEasings.Resolve(scaleEasing), progress);
                currentScale = Mathf.LerpUnclamped(scaleStart, scaleTarget, eased);
                if (!IsFiniteScale(currentScale))
                    currentScale = scaleTarget;
                else
                {
                    float span = 1f - pressedScale;
                    float minimum = Mathf.Max(0.05f, pressedScale - span);
                    float maximum = 1f + span;
                    currentScale = Mathf.Clamp(currentScale, minimum, maximum);
                }
            }

            if (appliedScale != currentScale)
            {
                appliedScale = currentScale;
                rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
        }

        private static bool IsFiniteScale(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void ResolveDisplayedInput(
            KeyViewerConfig global,
            bool rawPressed,
            bool rawDown,
            bool rawUp,
            float now,
            out bool pressed,
            out bool pressedThisFrame,
            out bool releasedThisFrame)
        {
            float delay = Mathf.Max(0f, global.KeyDisplayDelayMs) / 1000f;
            if (delay <= 0.0001f)
            {
                delayedTransitions.Clear();
                pressed = rawPressed;
                pressedThisFrame = rawDown || (rawPressed && !displayedPressed);
                releasedThisFrame = rawUp || (!rawPressed && displayedPressed);
                rawWasPressed = rawPressed;
                displayedPressed = rawPressed;
                return;
            }

            if (rawPressed != rawWasPressed)
            {
                delayedTransitions.Enqueue(new DelayedTransition(now + delay, rawPressed));
                rawWasPressed = rawPressed;
            }

            pressedThisFrame = false;
            releasedThisFrame = false;
            while (delayedTransitions.Count > 0 && delayedTransitions.Peek().DueTime <= now)
            {
                bool next = delayedTransitions.Dequeue().Pressed;
                if (next == displayedPressed)
                    continue;
                displayedPressed = next;
                if (next) pressedThisFrame = true; else releasedThisFrame = true;
            }
            pressed = displayedPressed;
        }

        private void UpdateGraph(KeyViewerKeyConfig key, float now, bool statisticsReset)
        {
            bool visible = key.Kind == KeyViewerElementKind.Graph;
            if (graph.gameObject.activeSelf != visible)
                graph.gameObject.SetActive(visible);
            if (!visible)
                return;

            graph.Configure(
                key.GraphType,
                key.GraphColor,
                key.GraphShowAverage,
                key.GraphAnimationEnabled);
            if (statisticsReset || observedGraphStat != key.Stat)
            {
                observedGraphStat = key.Stat;
                nextGraphSample = 0f;
                graph.ResetHistory();
            }
            if (now + 0.0001f < nextGraphSample)
                return;
            nextGraphSample = now + 0.1f;
            int sampleCount = Mathf.CeilToInt(Mathf.Clamp(key.GraphSpeedSeconds, 0.5f, 5f) / 0.1f);
            graph.Push(KeyViewer.StatNumber(key.Stat), sampleCount);
        }

        private void UpdateKnob(KeyViewerKeyConfig key, KeyViewerSpriteFactory sprites, float deltaTime)
        {
            bool visible = key.Kind == KeyViewerElementKind.Knob;
            if (knobIndicator.gameObject.activeSelf != visible)
                knobIndicator.gameObject.SetActive(visible);
            if (!visible)
                return;

            float axis = 0f;
            string axisName = key.KnobAxisId ?? "";
            if (!axisName.StartsWith("HIDA:", StringComparison.OrdinalIgnoreCase) && axisName.Length > 0)
            {
                try { axis = Input.GetAxisRaw(axisName); }
                catch { axis = 0f; }
            }

            float direction = key.KnobReverse ? -1f : 1f;
            knobAngle += axis * key.KnobSensitivity * direction * 360f * Mathf.Max(0f, deltaTime);
            RectTransform indicator = knobIndicator.rectTransform;
            indicator.sizeDelta = new Vector2(Mathf.Max(2f, key.Size.x * 0.06f), Mathf.Max(8f, key.Size.y * 0.38f));
            indicator.localRotation = Quaternion.Euler(0f, 0f, -knobAngle);
            knobIndicator.sprite = sprites.Rounded(Mathf.RoundToInt(indicator.sizeDelta.x * 0.5f));
            knobIndicator.color = key.TextColor;
        }

        private void UpdateShadow(
            KeyViewerKeyConfig key, KeyViewerSpriteFactory sprites, bool isPressed, bool suppressed, int radius)
        {
            bool enabled = isPressed ? key.ActiveShadowEnabled : key.ShadowEnabled;
            float blurValue = isPressed ? key.ActiveShadowBlur : key.ShadowBlur;
            Vector2 offset = isPressed ? key.ActiveShadowOffset : key.ShadowOffset;
            if (!enabled || suppressed || blurValue < 0.5f)
            {
                shadow.enabled = false;
                return;
            }

            int blur = Mathf.Max(1, Mathf.RoundToInt(blurValue));
            shadow.enabled = true;
            shadow.sprite = sprites.Soft(radius, blur);
            shadow.color = isPressed ? key.ActiveShadowColor : key.ShadowColor;

            RectTransform rect = shadow.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-blur + offset.x, -blur - offset.y);
            rect.offsetMax = new Vector2(blur + offset.x, blur - offset.y);
        }

        private void UpdateImage(KeyViewerKeyConfig key, bool isPressed)
        {
            string path = isPressed && !string.IsNullOrEmpty(key.ActiveImagePath)
                ? key.ActiveImagePath
                : key.IdleImagePath;

            Sprite sprite = KeyViewerImageCache.Load(path);
            if (sprite == null)
            {
                keyImage.enabled = false;
                return;
            }

            keyImage.enabled = true;
            keyImage.sprite = sprite;
            keyImage.type = Image.Type.Simple;
            keyImage.color = Color.white;

            RectTransform rect = keyImage.rectTransform;
            Vector2 box = imageRect.rect.size;
            float nativeWidth = Mathf.Max(1f, sprite.rect.width);
            float nativeHeight = Mathf.Max(1f, sprite.rect.height);

            KeyViewerImageFit imageFit = isPressed ? key.ActiveImageFit : key.IdleImageFit;
            if (imageFit == KeyViewerImageFit.Fill)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            float scale;
            switch (imageFit)
            {
                case KeyViewerImageFit.Cover:
                    scale = Mathf.Max(box.x / nativeWidth, box.y / nativeHeight);
                    break;
                case KeyViewerImageFit.None:
                    scale = 1f;
                    break;
                default:
                    scale = Mathf.Min(box.x / nativeWidth, box.y / nativeHeight);
                    break;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(nativeWidth * scale, nativeHeight * scale);
        }

        private void UpdateCounter(
            KeyViewerKeyConfig key,
            KeyViewerConfig global,
            bool isPressed,
            bool pressedThisFrame,
            bool countersReset,
            float deltaTime)
        {
            bool visible = global.ShowCounters && key.CounterEnabled && key.Kind == KeyViewerElementKind.Key;
            if (counter.enabled != visible)
                counter.enabled = visible;

            if (pressedThisFrame)
            {
                key.Count++;
                KeyViewer.NotifyCounted();
                if (key.CounterAnimationEnabled)
                    counterPulse = 1f;
            }

            if (countersReset)
                shownCount = -1;

            if (!visible)
                return;

            TMP_FontAsset resolvedFont = KeyViewerTypography.Resolve(key.CounterFontFamily, key.CounterFontFilePath);
            if (resolvedFont != null && counter.font != resolvedFont)
            {
                counter.font = resolvedFont;
                HemiTextMaterial.ApplyPlain(counter);
            }
            KeyViewerTypography.ApplyStyle(
                counter,
                key.CounterFontWeight,
                key.CounterFontItalic,
                key.CounterFontUnderline,
                key.CounterFontStrikethrough);

            if (shownCount != key.Count)
            {
                shownCount = key.Count;
                counter.text = key.Count.ToString(CultureInfo.InvariantCulture);
            }

            counter.fontSize = key.CounterFontSize;
            KeyViewerGradient fillGradient = isPressed ? key.CounterActiveGradient : key.CounterIdleGradient;
            counter.enableVertexGradient = false;
            counter.color = fillGradient == null
                ? (isPressed ? key.CounterActiveColor : key.CounterIdleColor)
                : Color.white;
            counterGradient.Apply(fillGradient);
            Color stroke = isPressed ? key.CounterActiveStrokeColor : key.CounterIdleStrokeColor;
            counter.outlineColor = stroke;
            counter.outlineWidth = stroke.a > 0.001f ? 0.18f : 0f;

            LayoutCounter(key);

            if (counterPulse > 0f)
            {
                float step = deltaTime / Mathf.Max(0.02f, key.CounterAnimationSeconds);
                counterPulse = Mathf.Max(0f, counterPulse - step);
            }

            float easedPulse = KeyViewerCubicBezier.Evaluate(key.CounterAnimationBezier, counterPulse);
            float pulse = Mathf.Lerp(1f, key.CounterAnimationScale, easedPulse);
            if (appliedCounterScale != pulse)
            {
                appliedCounterScale = pulse;
                counterRect.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        private void LayoutCounter(KeyViewerKeyConfig key)
        {
            float height = key.CounterFontSize * 1.4f;
            float width = Mathf.Max(key.Size.x, height * 3f);
            bool vertical = key.CounterAlign == KeyViewerCounterAlign.Top || key.CounterAlign == KeyViewerCounterAlign.Bottom;
            bool outside = key.CounterPlacement == KeyViewerCounterPlacement.Outside;

            counterRect.sizeDelta = vertical ? new Vector2(width, height) : new Vector2(height * 3f, key.Size.y);
            counterRect.pivot = new Vector2(0.5f, 0.5f);

            float halfWidth = key.Size.x * 0.5f;
            float halfHeight = key.Size.y * 0.5f;
            Vector2 position;

            switch (key.CounterAlign)
            {
                case KeyViewerCounterAlign.Top:
                    position = new Vector2(0f, outside
                        ? halfHeight + height * 0.5f + key.CounterGap
                        : halfHeight - height * 0.5f - key.CounterGap);
                    break;
                case KeyViewerCounterAlign.Left:
                    position = new Vector2(outside
                        ? -halfWidth - counterRect.sizeDelta.x * 0.5f - key.CounterGap
                        : -halfWidth + counterRect.sizeDelta.x * 0.5f + key.CounterGap, 0f);
                    break;
                case KeyViewerCounterAlign.Right:
                    position = new Vector2(outside
                        ? halfWidth + counterRect.sizeDelta.x * 0.5f + key.CounterGap
                        : halfWidth - counterRect.sizeDelta.x * 0.5f - key.CounterGap, 0f);
                    break;
                default:
                    position = new Vector2(0f, outside
                        ? -halfHeight - height * 0.5f - key.CounterGap
                        : -halfHeight + height * 0.5f + key.CounterGap);
                    break;
            }

            counterRect.anchorMin = new Vector2(0.5f, 0.5f);
            counterRect.anchorMax = new Vector2(0.5f, 0.5f);
            counterRect.anchoredPosition = position;

            if (key.CounterAlignMode != KeyViewerCounterAlignMode.Between || outside)
            {
                label.rectTransform.offsetMin = new Vector2(4f, 3f);
                label.rectTransform.offsetMax = new Vector2(-4f, -3f);
                return;
            }

            float reserve = (vertical ? height : counterRect.sizeDelta.x) + key.CounterGap;
            switch (key.CounterAlign)
            {
                case KeyViewerCounterAlign.Top:
                    label.rectTransform.offsetMin = new Vector2(4f, 3f);
                    label.rectTransform.offsetMax = new Vector2(-4f, -3f - reserve);
                    break;
                case KeyViewerCounterAlign.Left:
                    label.rectTransform.offsetMin = new Vector2(4f + reserve, 3f);
                    label.rectTransform.offsetMax = new Vector2(-4f, -3f);
                    break;
                case KeyViewerCounterAlign.Right:
                    label.rectTransform.offsetMin = new Vector2(4f, 3f);
                    label.rectTransform.offsetMax = new Vector2(-4f - reserve, -3f);
                    break;
                default:
                    label.rectTransform.offsetMin = new Vector2(4f, 3f + reserve);
                    label.rectTransform.offsetMax = new Vector2(-4f, -3f);
                    break;
            }
        }

        public void ForceRelease()
        {
            scaleInitialized = false;
            appliedScale = float.NaN;
            appliedCounterScale = float.NaN;
            wasPressed = false;
            rawWasPressed = false;
            displayedPressed = false;
            delayedTransitions.Clear();
            observedGraphStat = (KeyViewerStat)(-1);
            nextGraphSample = 0f;
            if (graph != null)
                graph.ResetHistory();
            knobAngle = 0f;
            ClearRainSegments();
        }

        private void OnDestroy()
        {
            DestroyRainObjects();

            if (rainParent != null)
                Destroy(rainParent.gameObject);
            rainParent = null;
        }

        private void UpdateRainingEffect(
            KeyViewerKeyConfig key,
            KeyViewerConfig global,
            Vector2 origin,
            Rect contentBounds,
            KeyViewerSpriteFactory sprites,
            bool pressed,
            bool pressedThisFrame,
            bool releasedThisFrame,
            float now)
        {
            if (!global.ShowNotes || !key.RainingEffect)
            {
                wasPressed = pressed;
                ClearRainSegments();
                return;
            }

            int rainOuterRadius = Mathf.RoundToInt(key.RainCornerRadius);
            int rainInnerRadius = Mathf.RoundToInt(Mathf.Max(0f, key.RainCornerRadius - key.RainBorderWidth));
            Sprite borderSprite = sprites.Rounded(rainOuterRadius);
            Sprite fillSprite = sprites.Rounded(rainInnerRadius);
            Sprite glowSprite = key.GlowEnabled && key.GlowSize > 0.5f
                ? sprites.Soft(rainOuterRadius, Mathf.Max(1, Mathf.RoundToInt(key.GlowSize)))
                : null;

            if (pressedThisFrame || (pressed && !wasPressed))
                CreateRainSegment(key, global, now);
            if (releasedThisFrame || (!pressed && wasPressed))
                FinalizeLatestRainSegment(key, global, now);

            wasPressed = pressed;
            if (global.NoteFrameLimit > 0 && now + 0.0001f < nextNoteVisualTime)
                return;
            nextNoteVisualTime = global.NoteFrameLimit > 0 ? now + 1f / global.NoteFrameLimit : now;
            for (int i = rainSegments.Count - 1; i >= 0; i--)
            {
                RainSegment segment = rainSegments[i];
                if (now < segment.StartTime)
                    continue;
                float length;
                float travel = 0f;
                if (!segment.Released)
                {
                    length = Mathf.Min(
                        Mathf.Max(1f, Mathf.Max(0f, now - segment.StartTime) * key.RainSpeed),
                        key.RainHeight);
                }
                else if (now < segment.EndTime)
                {
                    length = Mathf.Min(
                        segment.FinalLength,
                        Mathf.Max(1f, Mathf.Max(0f, now - segment.StartTime) * key.RainSpeed));
                }
                else
                {
                    length = segment.FinalLength;
                    travel = Mathf.Max(0f, now - segment.EndTime) * key.RainSpeed;
                }

                float visibleTop;
                float visibleBottom;
                if (key.RainReverse)
                {
                    float keyBottom = origin.y + key.Position.y + key.Size.y;
                    float trackBottom = key.NoteAutoYCorrection
                        ? origin.y + contentBounds.yMax
                        : Mathf.Min(Screen.height, keyBottom + key.RainHeight);
                    float top = keyBottom + travel;
                    float bottom = top + length;
                    visibleTop = Mathf.Max(keyBottom, top);
                    visibleBottom = Mathf.Min(trackBottom, bottom);
                }
                else
                {
                    float keyTop = origin.y + key.Position.y;
                    float trackTop = key.NoteAutoYCorrection
                        ? origin.y + contentBounds.yMin
                        : Mathf.Max(0f, keyTop - key.RainHeight);
                    float bottom = keyTop - travel;
                    float top = bottom - length;
                    visibleTop = Mathf.Max(trackTop, top);
                    visibleBottom = Mathf.Min(keyTop, bottom);
                }

                float visibleHeight = visibleBottom - visibleTop;

                if (visibleHeight <= 0.1f)
                {
                    if (segment.EndTime >= 0f)
                        RecycleRainSegmentAt(i);
                    continue;
                }

                ApplyRainSegmentVisual(
                    segment,
                    key,
                    global,
                    origin,
                    contentBounds,
                    borderSprite,
                    fillSprite,
                    glowSprite,
                    visibleTop,
                    visibleHeight);
            }
        }

        private void CreateRainSegment(KeyViewerKeyConfig key, KeyViewerConfig global, float now)
        {
            if (rainSegments.Count >= MaximumRainSegments)
                RecycleRainSegmentAt(0);

            RainSegment segment = rainPool.Count > 0 ? rainPool.Dequeue() : CreateRainSegmentObject();
            segment.Root.SetActive(true);
            float delaySeconds = 0f;
            if (global.DelayedNoteEnabled)
            {
                float minLengthSeconds = Mathf.Min(key.RainMinimumLength, key.RainHeight)
                    / Mathf.Max(1f, key.RainSpeed);
                float thresholdSeconds = Mathf.Max(0f, global.ShortNoteThresholdMs) / 1000f;
                if (thresholdSeconds > minLengthSeconds)
                    delaySeconds = (thresholdSeconds - minLengthSeconds) * 0.5f;
            }
            segment.PressTime = now;
            segment.StartTime = now + delaySeconds;
            segment.EndTime = -1f;
            segment.FinalLength = 0f;
            segment.Released = false;
            rainSegments.Add(segment);
        }

        private void FinalizeLatestRainSegment(KeyViewerKeyConfig key, KeyViewerConfig global, float now)
        {
            for (int i = rainSegments.Count - 1; i >= 0; i--)
            {
                RainSegment segment = rainSegments[i];
                if (segment.Released)
                    continue;

                float holdSeconds = Mathf.Max(0f, now - segment.PressTime);
                float visualSeconds = global.DelayedNoteEnabled
                    ? ResolveDelayedNoteLength(
                        holdSeconds,
                        Mathf.Min(key.RainMinimumLength, key.RainHeight) / Mathf.Max(1f, key.RainSpeed),
                        Mathf.Max(0f, global.ShortNoteThresholdMs) / 1000f)
                    : holdSeconds;
                segment.FinalLength = Mathf.Clamp(
                    visualSeconds * key.RainSpeed,
                    key.RainMinimumLength,
                    key.RainHeight);
                segment.EndTime = Mathf.Max(now, segment.StartTime + visualSeconds);
                segment.Released = true;
                return;
            }
        }

        private static float ResolveDelayedNoteLength(float hold, float minimum, float threshold)
        {
            if (threshold <= minimum)
                return Mathf.Max(minimum, hold);
            float displayDelay = (threshold - minimum) * 0.5f;
            float constantEnd = minimum + displayDelay;
            if (hold <= constantEnd)
                return minimum;
            if (hold >= threshold)
                return hold;
            float progress = Mathf.Clamp01((hold - constantEnd) / Mathf.Max(0.0001f, threshold - constantEnd));
            float smoothstep = progress * progress * (3f - 2f * progress);
            return hold - displayDelay + displayDelay * smoothstep;
        }

        private void ClearRainSegments()
        {
            for (int i = rainSegments.Count - 1; i >= 0; i--)
                RecycleRainSegmentAt(i);
        }

        private RainSegment CreateRainSegmentObject()
        {
            GameObject rainObject = new GameObject("RainingNote");
            rainObject.transform.SetParent(rainParent, false);
            RectTransform rainRect = rainObject.AddComponent<RectTransform>();
            rainRect.anchorMin = new Vector2(0f, 1f);
            rainRect.anchorMax = new Vector2(0f, 1f);
            rainRect.pivot = new Vector2(0f, 1f);

            GameObject glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(rainObject.transform, false);
            RectTransform glowRect = glowObject.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            Image glowImage = glowObject.AddComponent<Image>();
            glowImage.type = Image.Type.Sliced;
            glowImage.raycastTarget = false;

            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(rainObject.transform, false);
            RectTransform bodyRect = bodyObject.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            Image borderImage = bodyObject.AddComponent<Image>();
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(bodyObject.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.type = Image.Type.Sliced;
            fillImage.raycastTarget = false;

            return new RainSegment
            {
                Root = rainObject,
                Rect = rainRect,
                GlowRect = glowRect,
                Glow = glowImage,
                Border = borderImage,
                FillRect = fillRect,
                Fill = fillImage,
                FillGradient = fillObject.AddComponent<KeyViewerVerticalGradient>(),
                GlowGradient = glowObject.AddComponent<KeyViewerVerticalGradient>()
            };
        }

        private static void ApplyRainSegmentVisual(
            RainSegment segment,
            KeyViewerKeyConfig key,
            KeyViewerConfig global,
            Vector2 origin,
            Rect contentBounds,
            Sprite borderSprite,
            Sprite fillSprite,
            Sprite glowSprite,
            float visibleTop,
            float visibleHeight)
        {
            float width = key.ResolveNoteWidth();
            segment.Rect.anchoredPosition = new Vector2(
                origin.x + key.ResolveNoteX(), -visibleTop + key.NoteOffset.y);
            segment.Rect.sizeDelta = new Vector2(width, visibleHeight);

            float borderWidth = Mathf.Clamp(
                key.RainBorderWidth,
                0f,
                Mathf.Min(width, visibleHeight) * 0.5f);
            bool hasBorder = borderWidth > 0.01f;

            ResolveTrackFade(
                key, global, origin, contentBounds, visibleTop, visibleHeight, out float fadeTop, out float fadeBottom);

            Color top = key.RainColor;
            Color bottom = key.RainGradient ? key.RainColorBottom : key.RainColor;
            top.a = Mathf.Clamp01(top.a * key.RainOpacity * fadeTop);
            bottom.a = Mathf.Clamp01(bottom.a * (key.RainGradient ? key.RainOpacityBottom : key.RainOpacity) * fadeBottom);

            segment.Fill.sprite = hasBorder ? fillSprite : borderSprite;
            segment.Fill.color = Color.white;
            segment.FillGradient.Apply(top, bottom);

            Color borderColor = key.RainBorderColor;
            float borderAlpha = borderColor.a * key.RainBorderOpacity;
            segment.Border.enabled = hasBorder;
            segment.Border.sprite = borderSprite;
            segment.Border.color = new Color(borderColor.r, borderColor.g, borderColor.b,
                Mathf.Clamp01(borderAlpha * Mathf.Max(fadeTop, fadeBottom)));

            float insetX = key.RainBorderSide == KeyViewerNoteBorderSide.Horizontal ? 0f : borderWidth;
            float insetY = key.RainBorderSide == KeyViewerNoteBorderSide.Vertical ? 0f : borderWidth;
            segment.FillRect.offsetMin = new Vector2(insetX, insetY);
            segment.FillRect.offsetMax = new Vector2(-insetX, -insetY);

            if (glowSprite == null)
            {
                segment.Glow.enabled = false;
                return;
            }

            float glow = Mathf.Max(1f, key.GlowSize);
            segment.Glow.enabled = true;
            segment.Glow.sprite = glowSprite;
            segment.Glow.color = Color.white;
            segment.GlowRect.offsetMin = new Vector2(-glow, -glow);
            segment.GlowRect.offsetMax = new Vector2(glow, glow);

            Color glowTop = key.GlowColor;
            Color glowBottom = key.GlowGradient ? key.GlowColorBottom : key.GlowColor;
            glowTop.a = Mathf.Clamp01(glowTop.a * key.GlowOpacity * fadeTop);
            glowBottom.a = Mathf.Clamp01(glowBottom.a * (key.GlowGradient ? key.GlowOpacityBottom : key.GlowOpacity) * fadeBottom);
            segment.GlowGradient.Apply(glowTop, glowBottom);
        }

        private static void ResolveTrackFade(
            KeyViewerKeyConfig key,
            KeyViewerConfig global,
            Vector2 origin,
            Rect contentBounds,
            float visibleTop,
            float visibleHeight,
            out float topFade,
            out float bottomFade)
        {
            topFade = 1f;
            bottomFade = 1f;
            float nearFade = key.RainReverse ? global.ReverseNoteFadeTop : global.NoteFadeBottom;
            float farFade = key.RainReverse ? global.ReverseNoteFadeBottom : global.NoteFadeTop;
            if (nearFade < 0.5f && farFade < 0.5f)
                return;

            float keyTop = origin.y + key.Position.y;
            float keyEdge = key.RainReverse ? keyTop + key.Size.y : keyTop;
            float farEdge = key.NoteAutoYCorrection
                ? origin.y + (key.RainReverse ? contentBounds.yMax : contentBounds.yMin)
                : key.RainReverse ? keyEdge + key.RainHeight : keyEdge - key.RainHeight;

            float top = visibleTop;
            float bottom = visibleTop + visibleHeight;
            topFade = FadeAt(top, keyEdge, farEdge, nearFade, farFade);
            bottomFade = FadeAt(bottom, keyEdge, farEdge, nearFade, farFade);
        }

        private static float FadeAt(float y, float keyEdge, float farEdge, float nearFade, float farFade)
        {
            float near = Mathf.Abs(y - keyEdge);
            float far = Mathf.Abs(y - farEdge);
            float value = 1f;

            if (nearFade > 0.5f)
                value = Mathf.Min(value, Mathf.Clamp01(near / nearFade));
            if (farFade > 0.5f)
                value = Mathf.Min(value, Mathf.Clamp01(far / farFade));

            return value;
        }

        private void RecycleRainSegmentAt(int index)
        {
            RainSegment segment = rainSegments[index];
            rainSegments.RemoveAt(index);
            if (segment == null || segment.Root == null)
                return;

            segment.Root.SetActive(false);
            rainPool.Enqueue(segment);
        }

        private void DestroyRainObjects()
        {
            for (int i = 0; i < rainSegments.Count; i++)
            {
                if (rainSegments[i] != null && rainSegments[i].Root != null)
                    Destroy(rainSegments[i].Root);
            }

            while (rainPool.Count > 0)
            {
                RainSegment segment = rainPool.Dequeue();
                if (segment != null && segment.Root != null)
                    Destroy(segment.Root);
            }

            rainSegments.Clear();
        }

        private readonly struct DelayedTransition
        {
            public readonly float DueTime;
            public readonly bool Pressed;

            public DelayedTransition(float dueTime, bool pressed)
            {
                DueTime = dueTime;
                Pressed = pressed;
            }
        }

        private sealed class RainSegment
        {
            public GameObject Root;
            public RectTransform Rect;
            public RectTransform GlowRect;
            public Image Glow;
            public KeyViewerVerticalGradient GlowGradient;
            public Image Border;
            public RectTransform FillRect;
            public Image Fill;
            public KeyViewerVerticalGradient FillGradient;
            public float StartTime;
            public float PressTime;
            public float EndTime;
            public float FinalLength;
            public bool Released;
        }
    }

    internal static class KeyViewerKeyNames
    {
        private static readonly Dictionary<KeyCode, string> Names = new Dictionary<KeyCode, string>
        {
            { KeyCode.CapsLock, "Caps" },
            { KeyCode.Return, "Enter" },
            { KeyCode.KeypadEnter, "Num Enter" },
            { KeyCode.Escape, "Esc" },
            { KeyCode.Space, "Space" },
            { KeyCode.LeftShift, "L Shift" },
            { KeyCode.RightShift, "R Shift" },
            { KeyCode.LeftControl, "L Ctrl" },
            { KeyCode.RightControl, "R Ctrl" },
            { KeyCode.LeftAlt, "L Alt" },
            { KeyCode.RightAlt, "R Alt" },
            { KeyCode.LeftCommand, "L Cmd" },
            { KeyCode.RightCommand, "R Cmd" },
            { KeyCode.LeftWindows, "L Win" },
            { KeyCode.RightWindows, "R Win" },
            { KeyCode.UpArrow, "Up" },
            { KeyCode.DownArrow, "Down" },
            { KeyCode.LeftArrow, "Left" },
            { KeyCode.RightArrow, "Right" },
            { KeyCode.Semicolon, ";" },
            { KeyCode.Comma, "," },
            { KeyCode.Period, "." },
            { KeyCode.Slash, "/" },
            { KeyCode.Backslash, "\\" },
            { KeyCode.LeftBracket, "[" },
            { KeyCode.RightBracket, "]" },
            { KeyCode.Quote, "'" },
            { KeyCode.BackQuote, "`" },
            { KeyCode.Minus, "-" },
            { KeyCode.Equals, "=" }
        };

        private static readonly Dictionary<KeyCode, string> Derived = new Dictionary<KeyCode, string>();

        public static string GetDisplayName(KeyCode key)
        {
            if (Names.TryGetValue(key, out string displayName))
                return displayName;

            if (Derived.TryGetValue(key, out string cached))
                return cached;

            string name = key.ToString();
            if (name.StartsWith("Alpha", StringComparison.Ordinal) && name.Length == 6)
                name = name.Substring(5, 1);
            else if (name.StartsWith("Keypad", StringComparison.Ordinal) && name.Length == 7)
                name = "Num " + name.Substring(6, 1);

            Derived[key] = name;
            return name;
        }
    }
}
