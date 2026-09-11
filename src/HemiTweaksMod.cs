using System;
using HemiTweaks.Interface;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(HemiTweaks.HemiTweaksMod), HemiTweaks.BuildInfo.Name, HemiTweaks.BuildInfo.Version, HemiTweaks.BuildInfo.Author, HemiTweaks.BuildInfo.DownloadLink)]

namespace HemiTweaks
{
    internal static class BuildInfo
    {
        public const string Name = "HemiTweaks";
        public const string Author = "Hemi";
        public const string Version = "1.0.0 - Beta 21";
        public const string DownloadLink = null;
    }

    public sealed class HemiTweaksMod : MelonMod
    {
        internal const float DefaultCanvasWidth = 1920f;
        internal const float DefaultCanvasHeight = 1080f;

        internal static void ApplyDefaultScaling(CanvasScaler scaler)
        {
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DefaultCanvasWidth, DefaultCanvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> enableChangeBuildNameEntry;
        private static MelonPreferences_Entry<bool> nonScrollEntry;
        private static MelonPreferences_Entry<bool> noCheckpointEntry;
        private static MelonPreferences_Entry<string> languageEntry;
        private static MelonPreferences_Entry<bool> effectRemoverEntry;
        private static MelonPreferences_Entry<bool> effectRemoverBlockSaveEntry;
        private static MelonPreferences_Entry<bool> hideUiEntry;
        private static MelonPreferences_Entry<bool> hideLevelTitleEntry;
        private static MelonPreferences_Entry<bool> hideAutoplayEntry;
        private static MelonPreferences_Entry<bool> hideNoFailEntry;
        private static MelonPreferences_Entry<bool> hideResultsEntry;
        private static MelonPreferences_Entry<bool> hideErrorMeterEntry;
        private static MelonPreferences_Entry<bool> tileInfoEntry;
        private static MelonPreferences_Entry<bool> tileInfoAngleEntry;
        private static MelonPreferences_Entry<bool> tileInfoBeatsEntry;
        private static MelonPreferences_Entry<bool> tileInfoCountEntry;
        private static MelonPreferences_Entry<bool> tileInfoSecondsEntry;
        private static MelonPreferences_Entry<bool> tileInfoExcludeLastEntry;
        private static MelonPreferences_Entry<float> tileInfoScaleEntry;
        private static MelonPreferences_Entry<float> tileInfoShadowXEntry;
        private static MelonPreferences_Entry<float> tileInfoShadowYEntry;
        private static MelonPreferences_Entry<bool> tufEntry;
        private static MelonPreferences_Entry<string> tufFolderEntry;
        private static MelonPreferences_Entry<int> tufKeyEntry;
        private static MelonPreferences_Entry<string> tileInfoShadowColorEntry;
        private static MelonPreferences_Entry<string> buildNameEntry;
        private static MelonPreferences_Entry<bool> unlockLimitsEntry;
        private static MelonPreferences_Entry<bool> enablePlanetColorChangerEntry;
        private static MelonPreferences_Entry<bool> enableTileCornerCurvatureEntry;
        private static MelonPreferences_Entry<float> tileCornerCurvatureEntry;
        private static MelonPreferences_Entry<string> redPlanetColorEntry;
        private static MelonPreferences_Entry<string> bluePlanetColorEntry;
        private static MelonPreferences_Entry<string> redRingColorEntry;
        private static MelonPreferences_Entry<string> blueRingColorEntry;
        private static MelonPreferences_Entry<string> redTailColorEntry;
        private static MelonPreferences_Entry<string> blueTailColorEntry;
        private static MelonPreferences_Entry<bool> enablePlanetOverlayEntry;
        private static MelonPreferences_Entry<string> redOverlayPathEntry;
        private static MelonPreferences_Entry<string> blueOverlayPathEntry;
        private static MelonPreferences_Entry<float> redOverlayScaleEntry;
        private static MelonPreferences_Entry<float> blueOverlayScaleEntry;
        private static MelonPreferences_Entry<int> toggleKeyEntry;
        private static MelonPreferences_Entry<bool> darkModeEntry;
        private static MelonPreferences_Entry<bool> transitionAnimationsEntry;
        private static MelonPreferences_Entry<bool> smoothScrollingEntry;
        private static MelonPreferences_Entry<float> uiFontSizeEntry;

        private static HemiRoot interfaceRoot;
        private static bool waitingForHotkey;
        private static int blockGameInputUntilFrame = -1;

        internal static event Action AppearanceChanged;

        public static bool EnableChangeBuildName => enableChangeBuildNameEntry == null || enableChangeBuildNameEntry.Value;
        public static bool NonScrollEnabled => nonScrollEntry != null && nonScrollEntry.Value;
        public static bool NoCheckpointEnabled => noCheckpointEntry != null && noCheckpointEntry.Value;
        public static string LanguageCode
        {
            get
            {
                string stored = languageEntry == null ? "" : languageEntry.Value ?? "";
                if (!string.IsNullOrWhiteSpace(stored))
                    return stored;

                try
                {
                    return RDString.language == SystemLanguage.Korean ? "KR" : Interface.HemiLang.FallbackCode;
                }
                catch
                {
                    return Interface.HemiLang.FallbackCode;
                }
            }
        }

        internal static string LanguageSetting => languageEntry == null ? "" : languageEntry.Value ?? "";

        public static bool EffectRemoverEnabled => effectRemoverEntry != null && effectRemoverEntry.Value;

        public static bool EffectRemoverBlockSave => effectRemoverBlockSaveEntry == null || effectRemoverBlockSaveEntry.Value;

        internal static MelonPreferences_Entry<bool> EffectRemoverBlockSaveEntry => effectRemoverBlockSaveEntry;

        internal static MelonPreferences_Category Category => category;
        public static bool HideUiEnabled => hideUiEntry != null && hideUiEntry.Value;
        public static bool HideLevelTitle => hideLevelTitleEntry != null && hideLevelTitleEntry.Value;
        public static bool HideAutoplay => hideAutoplayEntry != null && hideAutoplayEntry.Value;
        public static bool HideNoFail => hideNoFailEntry != null && hideNoFailEntry.Value;
        public static bool HideResults => hideResultsEntry != null && hideResultsEntry.Value;
        public static bool HideErrorMeter => hideErrorMeterEntry != null && hideErrorMeterEntry.Value;
        public static bool TileInfoEnabled => tileInfoEntry != null && tileInfoEntry.Value;
        public static bool TileInfoAngle => tileInfoAngleEntry != null && tileInfoAngleEntry.Value;
        public static bool TileInfoBeats => tileInfoBeatsEntry != null && tileInfoBeatsEntry.Value;
        public static bool TileInfoCount => tileInfoCountEntry != null && tileInfoCountEntry.Value;
        public static bool TileInfoSeconds => tileInfoSecondsEntry != null && tileInfoSecondsEntry.Value;
        public static bool TileInfoExcludeLast => tileInfoExcludeLastEntry != null && tileInfoExcludeLastEntry.Value;
        public static float TileInfoScale => tileInfoScaleEntry == null ? 0.5f : Mathf.Clamp(tileInfoScaleEntry.Value, 0.15f, 2f);
        public static float TileInfoShadowX => tileInfoShadowXEntry == null ? 0.35f : tileInfoShadowXEntry.Value;
        public static float TileInfoShadowY => tileInfoShadowYEntry == null ? -0.35f : tileInfoShadowYEntry.Value;
        public static Color TileInfoShadowColor => ReadColor(tileInfoShadowColorEntry, new Color(0f, 0f, 0f, 0.5f));
        public static bool TufEnabled => tufEntry != null && tufEntry.Value;
        public static string TufFolder => tufFolderEntry == null ? "" : tufFolderEntry.Value ?? "";
        internal static KeyCode TufKey => tufKeyEntry == null ? KeyCode.F9 : (KeyCode)tufKeyEntry.Value;
        public static string BuildName => buildNameEntry == null ? BuildInfo.Name : buildNameEntry.Value;
        public static bool UnlockLimitsEnabled => unlockLimitsEntry != null && unlockLimitsEntry.Value;
        public static bool EnablePlanetColorChanger => enablePlanetColorChangerEntry != null && enablePlanetColorChangerEntry.Value;
        public static bool EnableTileCornerCurvature => enableTileCornerCurvatureEntry != null && enableTileCornerCurvatureEntry.Value;
        public static float TileCornerCurvatureAmount => tileCornerCurvatureEntry == null ? 0.5f : Mathf.Clamp01(tileCornerCurvatureEntry.Value);
        public static Color RedPlanetColor => ReadColor(redPlanetColorEntry, Color.red);
        public static Color BluePlanetColor => ReadColor(bluePlanetColorEntry, Color.blue);
        public static Color RedRingColor => ReadColor(redRingColorEntry, Color.red);
        public static Color BlueRingColor => ReadColor(blueRingColorEntry, Color.blue);
        public static Color RedTailColor => ReadColor(redTailColorEntry, Color.red);
        public static Color BlueTailColor => ReadColor(blueTailColorEntry, Color.blue);
        public static bool PlanetOverlayEnabled => enablePlanetOverlayEntry != null && enablePlanetOverlayEntry.Value;
        public static string RedOverlayPath => redOverlayPathEntry == null ? "red.png" : redOverlayPathEntry.Value;
        public static string BlueOverlayPath => blueOverlayPathEntry == null ? "blue.png" : blueOverlayPathEntry.Value;
        public static float RedOverlayScale => redOverlayScaleEntry == null ? 0.05f : Mathf.Clamp(redOverlayScaleEntry.Value, 0.0001f, 5f);
        public static float BlueOverlayScale => blueOverlayScaleEntry == null ? 0.05f : Mathf.Clamp(blueOverlayScaleEntry.Value, 0.0001f, 5f);
        public static bool ShouldBlockGameInput =>
            IsInterfaceOpen ||
            Time.frameCount <= blockGameInputUntilFrame;

        internal static bool IsInterfaceOpen => interfaceRoot != null && interfaceRoot.IsVisible;

        internal static KeyCode ToggleSettingsKey => toggleKeyEntry == null ? KeyCode.F8 : (KeyCode)toggleKeyEntry.Value;
        internal static bool IsDarkMode => darkModeEntry == null || darkModeEntry.Value;
        internal static bool TransitionAnimationsEnabled => transitionAnimationsEntry == null || transitionAnimationsEntry.Value;
        internal static bool SmoothScrollingEnabled => smoothScrollingEntry == null || smoothScrollingEntry.Value;

        internal static float UIFontSize => uiFontSizeEntry == null ? 16f : Mathf.Clamp(uiFontSizeEntry.Value, 12f, 100f);
        internal static bool IsWaitingForHotkey => waitingForHotkey;

        public override void OnInitializeMelon()
        {
            category = MelonPreferences.CreateCategory(BuildInfo.Name, BuildInfo.Name);
            enableChangeBuildNameEntry = category.CreateEntry("EnableChangeBuildName", true, "Enable ChangeBuildName", "Changes the build text shown by the game.");
            nonScrollEntry = category.CreateEntry("EnableNonScroll", false, "Enable NonScroll", "Stops the mouse wheel zooming the camera while a level is being played.");
            noCheckpointEntry = category.CreateEntry("EnableNoCheckpoint", false, "Enable NoCheckpoint", "Stops checkpoints taking effect, so a failed run restarts from the beginning.");
            languageEntry = category.CreateEntry("Language", "", "Language", "Language file for the mod's interface. Empty follows the game.");
            effectRemoverEntry = category.CreateEntry("EnableEffectRemover", false, "Enable EffectRemover", "Hides a level's visual effects. The level file itself is never changed.");
            effectRemoverBlockSaveEntry = category.CreateEntry("EffectRemoverBlockSave", true, "Effect Remover: Block Editor Save", "Refuses to save in the editor while effects are hidden, so they cannot be written away by accident.");
            EffectRemover.RegisterOptions(category);
            hideUiEntry = category.CreateEntry("EnableHideUi", false, "Enable HideUI", "Hides parts of the game HUD.");
            hideLevelTitleEntry = category.CreateEntry("HideLevelTitle", true, "Hide Level Title", "Hides the song name and composer.");
            hideAutoplayEntry = category.CreateEntry("HideAutoplay", true, "Hide Autoplay", "Hides the autoplay text and the editor Otto button.");
            hideNoFailEntry = category.CreateEntry("HideNoFail", true, "Hide No-Fail Icon", "Hides the no-fail modifier icon.");
            hideResultsEntry = category.CreateEntry("HideResults", true, "Hide Results", "Hides the congratulations and detailed result texts.");
            hideErrorMeterEntry = category.CreateEntry("HideErrorMeter", true, "Hide Error Meter", "Hides the hit error meter.");
            tileInfoEntry = category.CreateEntry("EnableTileInfo", false, "Enable ShowTileInfo", "Shows the selected tiles angle, beats, count and seconds in the editor.");
            tileInfoAngleEntry = category.CreateEntry("TileInfoAngle", true, "Tile Info Angle", "Shows the selection angle.");
            tileInfoBeatsEntry = category.CreateEntry("TileInfoBeats", true, "Tile Info Beats", "Shows the selection length in beats.");
            tileInfoCountEntry = category.CreateEntry("TileInfoCount", true, "Tile Info Count", "Shows how many tiles are selected.");
            tileInfoSecondsEntry = category.CreateEntry("TileInfoSeconds", true, "Tile Info Seconds", "Shows the selection length in seconds.");
            tileInfoExcludeLastEntry = category.CreateEntry("TileInfoExcludeLast", false, "Tile Info Exclude Last", "Leaves the last selected tile out of the timing figures.");
            tileInfoScaleEntry = category.CreateEntry("TileInfoScale", 0.5f, "Tile Info Text Size", "Size of the tile info text, relative to the tile number it replaces.");
            tileInfoShadowXEntry = category.CreateEntry("TileInfoShadowX", 0.35f, "Tile Info Shadow X", "Shadow offset, as a fraction of the text size.");
            tileInfoShadowYEntry = category.CreateEntry("TileInfoShadowY", -0.35f, "Tile Info Shadow Y", "Shadow offset, as a fraction of the text size.");
            tileInfoShadowColorEntry = CreateColorEntry("TileInfoShadowColor", new Color(0f, 0f, 0f, 0.5f), "Tile Info Shadow Colour");
            tufEntry = category.CreateEntry("EnableTuf", false, "Enable TUF", "Browse and download levels from The Universal Forums.");
            tufFolderEntry = category.CreateEntry("TufFolder", "", "TUF Folder", "Where downloaded levels are kept. Empty uses the default.");
            tufKeyEntry = category.CreateEntry("TufKey", (int)KeyCode.F9, "TUF Key", "Key that opens the TUF level browser.");
            buildNameEntry = category.CreateEntry("BuildName", BuildInfo.Name, "Build Name", "Text to show in the build name field.");
            unlockLimitsEntry = category.CreateEntry("UnlockLimits", false, "Unlock Numeric Limits", "Removes editor value, speed, settings, and event restrictions.");
            enablePlanetColorChangerEntry = category.CreateEntry("EnablePlanetColorChanger", false, "Enable Planet Color Changer", "Overrides planet, ring, tail, and overlay visuals.");
            enableTileCornerCurvatureEntry = category.CreateEntry("EnableTileCornerCurvature", false, "Enable Tile Corner Curvature", "Rounds floor corners across a wider range of tile angles.");
            tileCornerCurvatureEntry = category.CreateEntry("TileCornerCurvature", 0.5f, "Tile Corner Curvature", "Controls the strength of rounded floor corners.");
            redPlanetColorEntry = CreateColorEntry("RedPlanetColor", Color.red, "Red Planet Color");
            bluePlanetColorEntry = CreateColorEntry("BluePlanetColor", Color.blue, "Blue Planet Color");
            redRingColorEntry = CreateColorEntry("RedRingColor", Color.red, "Red Ring Color");
            blueRingColorEntry = CreateColorEntry("BlueRingColor", Color.blue, "Blue Ring Color");
            redTailColorEntry = CreateColorEntry("RedTailColor", Color.red, "Red Tail Color");
            blueTailColorEntry = CreateColorEntry("BlueTailColor", Color.blue, "Blue Tail Color");
            enablePlanetOverlayEntry = category.CreateEntry("EnablePlanetOverlay", false, "Enable Planet Overlay", "Shows custom images over the red and blue planets.");
            redOverlayPathEntry = category.CreateEntry("RedOverlayPath", "red.png", "Red Overlay Path", "Absolute path or a path relative to the current level folder.");
            blueOverlayPathEntry = category.CreateEntry("BlueOverlayPath", "blue.png", "Blue Overlay Path", "Absolute path or a path relative to the current level folder.");
            redOverlayScaleEntry = category.CreateEntry("RedOverlayScale", 0.05f, "Red Overlay Scale", "Scale of the red planet overlay image.");
            blueOverlayScaleEntry = category.CreateEntry("BlueOverlayScale", 0.05f, "Blue Overlay Scale", "Scale of the blue planet overlay image.");
            toggleKeyEntry = category.CreateEntry("ToggleSettingsKey", (int)KeyCode.F8, "Toggle Settings Key", "Key used to open and close the HemiTweaks interface.");
            darkModeEntry = category.CreateEntry("DarkMode", true, "Dark Mode", "Uses the dark settings UI theme.");
            transitionAnimationsEntry = category.CreateEntry("TransitionAnimations", true, "Transition Animations", "Animates tab and settings page transitions.");
            smoothScrollingEntry = category.CreateEntry("SmoothScrolling", true, "Smooth Scrolling", "Smoothly eases scroll movement.");
            uiFontSizeEntry = category.CreateEntry("UIFontSize", 16f, "UI Font Size", "Base font size used by the HemiTweaks interface.");

            PlanetColorChanger.Initialize(category);
            UnlockLimits.RefreshRuntimeState();
            TileCornerCurvature.Initialize(EnableTileCornerCurvature, TileCornerCurvatureAmount);
            KeyViewer.Initialize(category);
            StateOverlay.Initialize(category);
            ComboOverlay.Initialize(category);
            ProgressBarOverlay.Initialize(category);
            OverloadBarOverlay.Initialize(category);
            HemiUpdater.Initialize(category);
            AutoOffset.Initialize(category);

            HemiProfiles.Applied += OnProfileApplied;

            interfaceRoot = HemiRoot.Create();
            HemiUpdatePopup.Ensure();
            HemiAutoOffsetPopup.Ensure();
            MelonLogger.Msg("Loaded. Press " + ToggleSettingsKey + " to open the interface.");
            HintIfHotkeyMayBeSwallowedByTheSystem(ToggleSettingsKey, "ToggleSettingsKey");
            if (TufEnabled)
                HintIfHotkeyMayBeSwallowedByTheSystem(TufKey, "TufKey");
        }

        private static void HintIfHotkeyMayBeSwallowedByTheSystem(KeyCode key, string entryName)
        {
            if (Application.platform != RuntimePlatform.OSXPlayer || !IsFunctionKey(key))
                return;

            MelonLogger.Msg(
                "If " + key + " does nothing, macOS is consuming it as a media key: press fn+" + key + ", turn on" +
                " System Settings > Keyboard > \"Use F1, F2, etc. keys as standard function keys\", or set [" +
                BuildInfo.Name + "] " + entryName + " in UserData/MelonPreferences.cfg to another KeyCode.");
        }

        private static bool IsFunctionKey(KeyCode key)
        {
            return key >= KeyCode.F1 && key <= KeyCode.F12;
        }

        private static void OnProfileApplied()
        {
            KeyViewer.ReloadFromPreferences();
            StateOverlay.UpdateLifecycle();
            ComboOverlay.AdoptNewDefaults();
            ComboOverlay.UpdateLifecycle();
            ProgressBarOverlay.UpdateLifecycle();
            OverloadBarOverlay.UpdateLifecycle();
            TileCornerCurvature.SetEnabled(EnableTileCornerCurvature);
            TileCornerCurvature.SetAmount(TileCornerCurvatureAmount);
            UnlockLimits.RefreshRuntimeState();
            ChangeBuildName.Refresh();
            PlanetColorChanger.InvalidateOverlayCache();
            PlanetColorChanger.SetEnabled(EnablePlanetColorChanger);
            AutoOffset.ReloadFromPreferences();
            AppearanceChanged?.Invoke();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            Tuf.TufOfficial.NotifySceneChanged();

            PlanetColorChanger.NotifySceneChanged();
            AutoOffset.NotifySceneChanged();

            RunCounters.NotifySceneChanged(sceneName);
        }

        public override void OnUpdate()
        {
            preferenceWriter.Tick();
            HemiUpdater.Tick();
            AutoOffset.Tick();
            StateGroupStore.TickSave();
            KeyViewer.TickSave();
            KeyViewer.UpdateLifecycle();
            StateOverlay.UpdateLifecycle();
            ComboOverlay.UpdateLifecycle();
            ProgressBarOverlay.UpdateLifecycle();
            OverloadBarOverlay.UpdateLifecycle();
            TileCornerCurvature.UpdateLifecycle();
            HideUi.UpdateLifecycle();

            KeyCode toggleKey = ToggleSettingsKey;
            bool togglePressed = toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey) && interfaceRoot != null;

            if (IsInterfaceOpen)
            {
                if (togglePressed && interfaceRoot.TryCloseWithHotkey())
                {
                    BlockGameInputThisFrame();
                    return;
                }

                MarkGameInputBlocked();
                return;
            }

            if (togglePressed)
            {
                interfaceRoot.Toggle();
                BlockGameInputThisFrame();
                return;
            }

            KeyCode tufKey = TufKey;
            if (TufEnabled && tufKey != KeyCode.None && Input.GetKeyDown(tufKey) && interfaceRoot != null)
            {
                interfaceRoot.OpenAt(Interface.HemiScreen.Tuf);
                BlockGameInputThisFrame();
                return;
            }

            BlockGameInputForOpenWindow();
        }

        public override void OnDeinitializeMelon()
        {
            HemiProfiles.Applied -= OnProfileApplied;
            StateGroupStore.FlushSave();
            KeyViewer.FlushSave();
            preferenceWriter.Flush();
            WritePreferences();
            KeyViewer.Shutdown();
            StateOverlay.Shutdown();
            ComboOverlay.Shutdown();
            ProgressBarOverlay.Shutdown();
            OverloadBarOverlay.Shutdown();
            Tuf.TufApi.Shutdown();
            Tuf.TufDownloads.Shutdown();
            HemiUpdater.Shutdown();
            HemiUpdatePopup.Shutdown();
            AutoOffset.Shutdown();
            HemiAutoOffsetPopup.Shutdown();
            HideUi.Shutdown();
            HemiTextMaterial.ClearPlain();
            PlanetColorChanger.Shutdown();
            TileCornerCurvature.Shutdown();
            UnlockLimits.Shutdown();

            if (interfaceRoot != null)
            {
                UnityEngine.Object.Destroy(interfaceRoot.gameObject);
                interfaceRoot = null;
            }
        }

        internal static void CancelAllCaptures()
        {
            waitingForHotkey = false;
            KeyViewer.StopRegistration();
            waitingForTufKey = false;
        }

        internal static void SetTileInfo(bool value)
        {
            if (!SetBooleanPreference(tileInfoEntry, value))
                return;

            if (!value)
                TileInfo.Release();
        }

        internal static void SetTuf(bool value)
        {
            if (!SetBooleanPreference(tufEntry, value))
                return;

            if (!value && Interface.HemiRoot.Instance != null && Interface.HemiRoot.Instance.Screen == Interface.HemiScreen.Tuf)
                Interface.HemiRoot.Instance.Go(Interface.HemiScreen.Main);
            else
                Interface.HemiRoot.Instance?.Refresh();
        }

        internal static bool SetTufFolder(string value, out string error)
        {
            error = null;
            if (tufFolderEntry == null)
                return false;

            if (!Tuf.TufLibrary.ValidateRoot(value, out string normalised, out error))
                return false;

            tufFolderEntry.Value = normalised;
            SavePreferences();
            return true;
        }

        internal static void SetTufKey(KeyCode key)
        {
            if (tufKeyEntry == null)
                return;

            tufKeyEntry.Value = (int)key;
            SavePreferences();
        }

        internal static void SetTileInfoScale(float value) => SetFloatPreference(tileInfoScaleEntry, Mathf.Clamp(value, 0.15f, 2f), 0f);

        internal static void SetTileInfoShadow(float x, float y)
        {
            if (tileInfoShadowXEntry == null || tileInfoShadowYEntry == null)
                return;

            tileInfoShadowXEntry.Value = x;
            tileInfoShadowYEntry.Value = y;
            SavePreferences();
        }

        internal static void SetTileInfoShadowColor(Color value)
        {
            if (tileInfoShadowColorEntry == null)
                return;

            tileInfoShadowColorEntry.Value = "#" + ColorUtility.ToHtmlStringRGBA(value);
            SavePreferences();
        }

        internal static void SetTileInfoOption(MelonPreferences_Entry<bool> entry, bool value) => SetBooleanPreference(entry, value);

        internal static MelonPreferences_Entry<bool> TileInfoAngleEntry => tileInfoAngleEntry;
        internal static MelonPreferences_Entry<bool> TileInfoBeatsEntry => tileInfoBeatsEntry;
        internal static MelonPreferences_Entry<bool> TileInfoCountEntry => tileInfoCountEntry;
        internal static MelonPreferences_Entry<bool> TileInfoSecondsEntry => tileInfoSecondsEntry;
        internal static MelonPreferences_Entry<bool> TileInfoExcludeLastEntry => tileInfoExcludeLastEntry;

        internal static void SetHideUi(bool value)
        {
            if (!SetBooleanPreference(hideUiEntry, value))
                return;

            HideUi.RefreshRuntimeState();
        }

        internal static void SetHideUiOption(MelonPreferences_Entry<bool> entry, bool value)
        {
            if (!SetBooleanPreference(entry, value))
                return;

            HideUi.RefreshRuntimeState();
        }

        internal static MelonPreferences_Entry<bool> HideLevelTitleEntry => hideLevelTitleEntry;
        internal static MelonPreferences_Entry<bool> HideAutoplayEntry => hideAutoplayEntry;
        internal static MelonPreferences_Entry<bool> HideNoFailEntry => hideNoFailEntry;
        internal static MelonPreferences_Entry<bool> HideResultsEntry => hideResultsEntry;
        internal static MelonPreferences_Entry<bool> HideErrorMeterEntry => hideErrorMeterEntry;

        internal static void SetNonScroll(bool value) => SetBooleanPreference(nonScrollEntry, value);

        internal static void SetNoCheckpoint(bool value) => SetBooleanPreference(noCheckpointEntry, value);

        internal static void SetLanguage(string code)
        {
            if (languageEntry == null)
                return;

            languageEntry.Value = code ?? "";
            SavePreferences();

            Interface.HemiLang.Use(LanguageCode);

            Interface.HemiRoot.Instance?.RefreshShell();
            AppearanceChanged?.Invoke();
        }

        internal static void SetEffectRemover(bool value)
        {
            if (!SetBooleanPreference(effectRemoverEntry, value))
                return;

            EffectRemover.RefreshEditorButtons();
        }

        internal static void SetEffectRemoverOption(MelonPreferences_Entry<bool> entry, bool value)
        {
            if (!SetBooleanPreference(entry, value))
                return;

            EffectRemover.RefreshEditorButtons();
        }

        internal static void SetEffectRemoverCameraZoom(float value)
        {
            MelonPreferences_Entry<float> entry = EffectRemover.CameraZoomEntry;
            if (entry == null || Mathf.Approximately(entry.Value, value))
                return;

            entry.Value = Mathf.Clamp(value, 100f, 1000f);
            SavePreferences();
        }

        internal static void SetEnableChangeBuildName(bool value)
        {
            if (!SetBooleanPreference(enableChangeBuildNameEntry, value))
                return;

            ChangeBuildName.Refresh();
        }

        internal static void SetBuildName(string value)
        {
            if (buildNameEntry == null)
                return;

            buildNameEntry.Value = value ?? "";
            SavePreferences();
            ChangeBuildName.Refresh();
        }

        internal static void SetUnlockLimits(bool value)
        {
            if (!SetBooleanPreference(unlockLimitsEntry, value))
                return;

            UnlockLimits.RefreshRuntimeState();
        }

        internal static void SetEnablePlanetColorChanger(bool value)
        {
            if (!SetBooleanPreference(enablePlanetColorChangerEntry, value))
                return;

            PlanetColorChanger.SetEnabled(value);
        }

        internal static void SetEnableTileCornerCurvature(bool value)
        {
            if (!SetBooleanPreference(enableTileCornerCurvatureEntry, value))
                return;

            TileCornerCurvature.SetEnabled(value);
        }

        internal static void SetTileCornerCurvatureAmount(float value)
        {
            if (tileCornerCurvatureEntry == null)
                return;

            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(tileCornerCurvatureEntry.Value, next))
                return;

            tileCornerCurvatureEntry.Value = next;
            SavePreferences();
            TileCornerCurvature.SetAmount(next);
        }

        internal static Color GetPlanetColor(PlanetColorTarget target)
        {
            switch (target)
            {
                case PlanetColorTarget.RedPlanet:
                case PlanetColorTarget.RedRing:
                case PlanetColorTarget.RedTail:
                    return ReadColor(GetColorEntry(target), Color.red);
                case PlanetColorTarget.BluePlanet:
                case PlanetColorTarget.BlueRing:
                case PlanetColorTarget.BlueTail:
                    return ReadColor(GetColorEntry(target), Color.blue);
                default:
                    return Color.white;
            }
        }

        internal static void SetPlanetColor(PlanetColorTarget target, Color value)
        {
            MelonPreferences_Entry<string> entry = GetColorEntry(target);
            if (!SetStringPreference(entry, "#" + ColorUtility.ToHtmlStringRGBA(value), StringComparison.OrdinalIgnoreCase))
                return;

            PlanetColorChanger.ApplyColors();
        }

        internal static void SetPlanetOverlayEnabled(bool value)
        {
            if (!SetBooleanPreference(enablePlanetOverlayEntry, value))
                return;

            PlanetColorChanger.ApplyColors();
        }

        internal static void SetPlanetOverlayPath(bool red, string value)
        {
            MelonPreferences_Entry<string> entry = red ? redOverlayPathEntry : blueOverlayPathEntry;
            if (!SetStringPreference(entry, value ?? "", StringComparison.Ordinal))
                return;

            PlanetColorChanger.InvalidateOverlayCache();
            PlanetColorChanger.ApplyColors();
        }

        internal static void SetPlanetOverlayScale(bool red, float value)
        {
            MelonPreferences_Entry<float> entry = red ? redOverlayScaleEntry : blueOverlayScaleEntry;
            if (!SetFloatPreference(entry, Mathf.Clamp(value, 0.0001f, 5f), 0.0001f))
                return;

            PlanetColorChanger.ApplyColors();
        }

        internal static void SetToggleSettingsKey(KeyCode key)
        {
            if (!IsAssignableHotkey(key) || toggleKeyEntry == null || toggleKeyEntry.Value == (int)key)
                return;

            toggleKeyEntry.Value = (int)key;
            SavePreferences();
            AppearanceChanged?.Invoke();
        }

        internal static void SetDarkMode(bool value)
        {
            if (!SetBooleanPreference(darkModeEntry, value))
                return;

            AppearanceChanged?.Invoke();
        }

        internal static void SetTransitionAnimations(bool value) => SetBooleanPreference(transitionAnimationsEntry, value);

        internal static void SetSmoothScrolling(bool value) => SetBooleanPreference(smoothScrollingEntry, value);

        internal static void SetUIFontSize(float value)
        {
            if (!SetFloatPreference(uiFontSizeEntry, Mathf.Clamp(Mathf.Round(value), 12f, 100f), 0.01f))
                return;

            AppearanceChanged?.Invoke();
        }

        internal static void StartHotkeyCapture() => SetHotkeyCapture(true);

        internal static void CancelHotkeyCapture()
        {
            if (waitingForHotkey)
                SetHotkeyCapture(false);
        }

        private static void SetHotkeyCapture(bool value)
        {
            waitingForHotkey = value;
            BlockGameInputThisFrame();
            AppearanceChanged?.Invoke();
        }

        private static bool waitingForTufKey;

        internal static bool IsCapturingTufKey => waitingForTufKey;

        internal static void BeginTufKeyCapture(bool value)
        {
            waitingForTufKey = value;
        }

        internal static void CaptureTufKey()
        {
            if (!PumpCapture(ref waitingForTufKey, out KeyCode key))
                return;

            SetTufKey(key);
            Interface.HemiRoot.Instance?.Refresh();
        }

        internal static void CaptureHotkeyForInterface()
        {
            if (!PumpCapture(ref waitingForHotkey, out KeyCode key))
                return;

            SetToggleSettingsKey(key);
        }

        private static bool PumpCapture(ref bool waiting, out KeyCode captured)
        {
            captured = KeyCode.None;
            if (!waiting)
                return false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                waiting = false;
                AppearanceChanged?.Invoke();
                return false;
            }

            foreach (KeyCode key in HemiModifiers.AllKeys)
            {
                if (!IsAssignableHotkey(key) || !Input.GetKeyDown(key))
                    continue;

                waiting = false;
                captured = key;
                return true;
            }

            return false;
        }

        private static bool IsAssignableHotkey(KeyCode key)
        {
            return key != KeyCode.None && key != KeyCode.Escape && key < KeyCode.Mouse0 &&
                   !IsLockOrModifierKey(key);
        }

        private static bool IsLockOrModifierKey(KeyCode key)
        {
            return key >= KeyCode.Numlock && key <= KeyCode.AltGr;
        }

        private static void BlockGameInputForOpenWindow()
        {
            if (!ShouldBlockGameInput)
                return;

            blockGameInputUntilFrame = Mathf.Max(blockGameInputUntilFrame, Time.frameCount);
        }

        private static void BlockGameInputThisFrame()
        {
            MarkGameInputBlocked();
            Input.ResetInputAxes();
        }

        private static void MarkGameInputBlocked()
        {
            blockGameInputUntilFrame = Mathf.Max(blockGameInputUntilFrame, Time.frameCount + 1);
        }

        private static bool SetBooleanPreference(MelonPreferences_Entry<bool> entry, bool value)
        {
            if (entry == null || entry.Value == value)
                return false;

            entry.Value = value;
            SavePreferences();
            return true;
        }

        private static bool SetFloatPreference(MelonPreferences_Entry<float> entry, float value, float epsilon)
        {
            if (entry == null || Mathf.Abs(entry.Value - value) < epsilon)
                return false;

            entry.Value = value;
            SavePreferences();
            return true;
        }

        private static bool SetStringPreference(MelonPreferences_Entry<string> entry, string value, StringComparison comparison)
        {
            if (entry == null || string.Equals(entry.Value, value, comparison))
                return false;

            entry.Value = value;
            SavePreferences();
            return true;
        }

        private static void SavePreferences()
        {
            preferenceWriter.Request();
        }

        private static readonly HemiSaveDebounce preferenceWriter = new HemiSaveDebounce(WritePreferences);

        private static void WritePreferences()
        {
            category?.SaveToFile(false);
        }

        internal static void FlushPreferences()
        {
            preferenceWriter.Flush();
        }

        private static MelonPreferences_Entry<string> CreateColorEntry(string key, Color defaultColor, string displayName)
        {
            return category.CreateEntry(
                key,
                "#" + ColorUtility.ToHtmlStringRGBA(defaultColor),
                displayName,
                "RGBA color in hexadecimal format.");
        }

        private static Color ReadColor(MelonPreferences_Entry<string> entry, Color fallback)
        {
            if (entry != null && ColorUtility.TryParseHtmlString(entry.Value, out Color color))
                return color;
            return fallback;
        }

        private static MelonPreferences_Entry<string> GetColorEntry(PlanetColorTarget target)
        {
            switch (target)
            {
                case PlanetColorTarget.RedPlanet:
                    return redPlanetColorEntry;
                case PlanetColorTarget.BluePlanet:
                    return bluePlanetColorEntry;
                case PlanetColorTarget.RedRing:
                    return redRingColorEntry;
                case PlanetColorTarget.BlueRing:
                    return blueRingColorEntry;
                case PlanetColorTarget.RedTail:
                    return redTailColorEntry;
                case PlanetColorTarget.BlueTail:
                    return blueTailColorEntry;
                default:
                    return null;
            }
        }
    }

    public enum PlanetColorTarget
    {
        RedPlanet,
        BluePlanet,
        RedRing,
        BlueRing,
        RedTail,
        BlueTail
    }
}
