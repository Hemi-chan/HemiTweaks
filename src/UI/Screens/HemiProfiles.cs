using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HemiTweaks.Interface
{
    internal static class HemiProfiles
    {
        private const string ActiveMarker = "active.txt";
        private const string DefaultName = "Default";

        private sealed class ProfilePreferences
        {
            public int FormatVersion = HemiProfileTransfer.PreferencesFormatVersion;
            public Dictionary<string, JToken> Entries =
                new Dictionary<string, JToken>(StringComparer.Ordinal);
        }

        internal static event Action Applied;

        private static bool migrated;
        private static bool scrubbed;

        private static string RootDirectory
        {
            get
            {
                string path = Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, "Profiles");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        private static string AssetRoot => Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, "ProfileAssets");

        internal static string FileOf(string name)
        {
            string root = RootDirectory;
            string safe = Sanitize(name);
            if (HemiFileNames.IsReserved(name) && name.IndexOf('/') < 0 && name.IndexOf('\\') < 0
                && name.IndexOf(':') < 0)
            {
                string legacy = Path.Combine(root, name + "." + HemiProfileTransfer.Extension);
                if (File.Exists(legacy))
                    return legacy;
            }
            return Path.Combine(root, safe + "." + HemiProfileTransfer.Extension);
        }

        internal static string AssetFolderOf(string name)
        {
            return Path.Combine(AssetRoot, Sanitize(name));
        }

        internal static string Active
        {
            get
            {
                List<string> names = List();
                try
                {
                    string marker = Path.Combine(RootDirectory, ActiveMarker);
                    if (File.Exists(marker))
                    {
                        string name = File.ReadAllText(marker).Trim();
                        if (!string.IsNullOrEmpty(name) && Exists(name))
                            return name;
                    }
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Could not read the active profile: " + exception.Message);
                }
                return names.Count > 0 ? names[0] : DefaultName;
            }
        }

        internal static List<string> List()
        {
            MigrateLegacyFolders();
            ScrubStoredProfiles();

            List<string> names = new List<string>();
            try
            {
                foreach (string file in Directory.GetFiles(RootDirectory, "*." + HemiProfileTransfer.Extension))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrEmpty(name))
                        names.Add(name);
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not list profiles: " + exception.Message);
            }

            names.Sort((a, b) =>
            {
                bool aDefault = string.Equals(a, DefaultName, StringComparison.OrdinalIgnoreCase);
                bool bDefault = string.Equals(b, DefaultName, StringComparison.OrdinalIgnoreCase);
                if (aDefault != bDefault)
                    return aDefault ? -1 : 1;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            if (names.Count == 0)
            {
                try
                {
                    SaveSnapshot(DefaultName);
                    SetActive(DefaultName);
                    names.Add(DefaultName);
                }
                catch (Exception exception)
                {
                    MelonLogger.Error("Could not create the Default profile: " + exception.Message);
                }
            }
            return names;
        }

        internal static bool Exists(string name)
        {
            return !string.IsNullOrEmpty(name) && File.Exists(FileOf(name));
        }

        internal static string Create(string requestedName)
        {
            string name = UniqueName(requestedName);
            try
            {
                FlushPendingWrites();
                SaveSnapshot(Active);

                ResetLiveToDefaults();
                SaveSnapshot(name);
                SetActive(name);
                Applied?.Invoke();
                MelonLogger.Msg("Created profile '" + name + "' from defaults.");
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not create profile '" + name + "': " + exception.Message);
            }
            return name;
        }

        private static void ResetLiveToDefaults()
        {
            MelonPreferences_Category category = RequireCategory();

            foreach (MelonPreferences_Entry entry in category.Entries)
            {
                if (HemiProfilePolicy.IsPortable(entry.Identifier))
                    entry.ResetToDefault();
            }
            category.SaveToFile(false);

            string livePath = StateGroupStore.FilePath;
            if (File.Exists(livePath))
                File.Delete(livePath);
            StateGroupStore.Reload();
        }

        internal static void Save(string name)
        {
            FlushPendingWrites();

            if (string.IsNullOrEmpty(name))
            {
                HemiTweaksMod.Category?.SaveToFile(false);
                return;
            }

            try
            {
                SaveSnapshot(name);
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not save profile '" + name + "': " + exception.Message);
            }
        }

        internal static void Apply(string name)
        {
            try
            {
                FlushPendingWrites();

                string previous = Active;
                if (string.Equals(previous, name, StringComparison.OrdinalIgnoreCase))
                {
                    SaveSnapshot(previous);
                    return;
                }

                if (!Exists(name))
                    return;

                SaveSnapshot(previous);
                Load(name);
                MelonLogger.Msg("Applied profile '" + name + "'.");
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not apply profile '" + name + "': " + exception.Message);
            }
        }

        internal static void Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            if (string.Equals(Active, name, StringComparison.OrdinalIgnoreCase) || List().Count <= 1)
                return;

            try
            {
                string file = FileOf(name);
                if (File.Exists(file))
                    File.Delete(file);
                HemiProfileTransfer.TryDeleteAssetFolder(AssetFolderOf(name));
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not delete profile '" + name + "': " + exception.Message);
            }
        }

        internal static string Rename(string name, string requested)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(requested))
                return null;

            string wanted = HemiFileNames.Sanitize(requested, "Profile", maximumLength: 100);
            if (string.Equals(wanted, name, StringComparison.Ordinal))
                return null;

            bool caseOnly = string.Equals(wanted, name, StringComparison.OrdinalIgnoreCase);
            string target = caseOnly ? wanted : Unique(wanted);

            try
            {
                bool wasActive = string.Equals(Active, name, StringComparison.OrdinalIgnoreCase);
                FlushPendingWrites();
                if (wasActive)
                    SaveSnapshot(name);

                string oldFile = FileOf(name);
                string newFile = FileOf(target);
                if (!File.Exists(oldFile))
                    return null;

                HemiProfileDocument document = HemiProfileTransfer.Read(oldFile);
                document.Name = target;
                HemiProfileTransfer.Write(document, newFile);
                if (caseOnly)
                {
                    string staging = newFile + ".renaming";
                    File.Move(newFile, staging);
                    File.Move(staging, newFile);
                }
                else
                {
                    File.Delete(oldFile);
                }

                HemiProfileTransfer.TryDeleteAssetFolder(AssetFolderOf(name));

                if (wasActive)
                    Load(target);
                else if (string.Equals(Active, name, StringComparison.OrdinalIgnoreCase))
                    SetActive(target);

                MelonLogger.Msg("Renamed profile '" + name + "' to '" + target + "'.");
                return target;
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not rename profile '" + name + "': " + exception.Message);
                return null;
            }
        }

        internal static void SnapshotActive()
        {
            FlushPendingWrites();
            SaveSnapshot(Active);
        }

        internal static string UniqueName(string requested)
        {
            return Unique(string.IsNullOrWhiteSpace(requested) ? "Profile" : requested);
        }

        private static void SetActive(string name)
        {
            HemiAtomicFile.WriteAllText(Path.Combine(RootDirectory, ActiveMarker), name ?? DefaultName);
        }

        private static void FlushPendingWrites()
        {
            HemiTweaksMod.FlushPreferences();
            StateGroupStore.FlushSave();
            KeyViewer.FlushSave();
        }

        private static void SaveSnapshot(string name)
        {
            HemiProfileTransfer.Write(HemiProfileTransfer.Capture(name), FileOf(name));
        }

        private static void ScrubStoredProfiles()
        {
            if (scrubbed)
                return;
            scrubbed = true;
            string marker = Path.Combine(RootDirectory, ".portable-settings-v1");
            if (File.Exists(marker))
                return;
            bool complete = true;
            foreach (string path in Directory.GetFiles(RootDirectory, "*." + HemiProfileTransfer.Extension))
            {
                try
                {
                    if (!HemiAssetSafety.IsWithin(path, RootDirectory))
                        throw new InvalidDataException("Unsafe stored profile path.");
                    HemiProfileDocument document = HemiProfileTransfer.Read(path);
                    if (document.NeedsSettingsRewrite)
                        HemiProfileTransfer.Write(document, path);
                }
                catch (Exception exception)
                {
                    complete = false;
                    MelonLogger.Warning("Stored profile settings could not be sanitized: " + exception.Message);
                }
            }
            if (complete)
                HemiAtomicFile.WriteAllText(marker, "1");
        }

        private static void Load(string name)
        {
            HemiProfileDocument document = HemiProfileTransfer.Read(FileOf(name));
            HemiProfileTransfer.Materialize(document, AssetFolderOf(name));
            LoadPreferences(document.Preferences);
            RestoreStateGroups(document.StateGroups);
            SetActive(name);
            StateGroupStore.Reload();
            Applied?.Invoke();
        }

        private static void LoadPreferences(JObject block)
        {
            ProfilePreferences preferences = block.ToObject<ProfilePreferences>();
            if (preferences == null || preferences.Entries == null)
                throw new InvalidDataException("The profile preference snapshot is empty.");
            if (preferences.FormatVersion <= 0 || preferences.FormatVersion > HemiProfileTransfer.PreferencesFormatVersion)
            {
                throw new InvalidDataException("Unsupported profile preference version: "
                    + preferences.FormatVersion + ".");
            }

            MelonPreferences_Category category = RequireCategory();

            Dictionary<MelonPreferences_Entry, object> converted =
                new Dictionary<MelonPreferences_Entry, object>();
            foreach (MelonPreferences_Entry entry in category.Entries)
            {
                if (!HemiProfilePolicy.IsPortable(entry.Identifier))
                    continue;
                if (!preferences.Entries.TryGetValue(entry.Identifier, out JToken token))
                    continue;

                try
                {
                    Type type = entry.GetReflectedType();
                    if (token.Type == JTokenType.Null && type.IsValueType)
                    {
                        throw new InvalidDataException(
                            "A value type cannot be restored from null.");
                    }

                    converted[entry] = token.Type == JTokenType.Null
                        ? null
                        : token.ToObject(type);
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException(
                        "Profile preference '" + entry.Identifier + "' is invalid.", exception);
                }
            }

            foreach (MelonPreferences_Entry entry in category.Entries)
            {
                if (HemiProfilePolicy.IsPortable(entry.Identifier))
                    entry.ResetToDefault();
            }
            foreach (KeyValuePair<MelonPreferences_Entry, object> pair in converted)
                pair.Key.BoxedValue = pair.Value;

            category.SaveToFile(false);
        }

        private static void RestoreStateGroups(JObject groups)
        {
            string livePath = StateGroupStore.FilePath;
            if (groups != null)
            {
                HemiAtomicFile.WriteAllText(livePath, JsonConvert.SerializeObject(groups, Formatting.Indented));
            }
            else if (File.Exists(livePath))
            {
                File.Delete(livePath);
            }
        }

        private static void MigrateLegacyFolders()
        {
            if (migrated)
                return;
            migrated = true;

            string activeName = null;
            try
            {
                string marker = Path.Combine(RootDirectory, ActiveMarker);
                if (File.Exists(marker))
                    activeName = File.ReadAllText(marker).Trim();
            }
            catch (Exception)
            {
            }

            bool reloadActive = false;
            try
            {
                foreach (string directory in Directory.GetDirectories(RootDirectory))
                {
                    string name = Path.GetFileName(directory);
                    try
                    {
                        string preferencesPath = Path.Combine(directory, "Preferences.json");
                        string migratedMarker = Path.Combine(directory, ".hemitweaks-migrated");
                        if (File.Exists(migratedMarker)
                            || !HemiAssetSafety.IsWithin(preferencesPath, directory))
                            continue;
                        if (!File.Exists(preferencesPath))
                        {
                            MelonLogger.Warning("Profile folder '" + name + "' has no preference snapshot and was left alone.");
                            continue;
                        }

                        string target = FileOf(name);
                        if (!File.Exists(target))
                        {
                            HemiProfileDocument document = new HemiProfileDocument
                            {
                                Name = name,
                                SavedAt = DateTime.UtcNow.ToString("o"),
                                Preferences = HemiProfileTransfer.ParseObject(
                                    System.Text.Encoding.UTF8.GetString(HemiStreamSafety.ReadFile(preferencesPath, 1024L * 1024)))
                            };
                            string stateGroupsPath = Path.Combine(directory, "StateGroups.json");
                            if (File.Exists(stateGroupsPath) && HemiAssetSafety.IsWithin(stateGroupsPath, directory))
                                document.StateGroups = HemiProfileTransfer.ParseObject(
                                    System.Text.Encoding.UTF8.GetString(HemiStreamSafety.ReadFile(stateGroupsPath, 1024L * 1024)));
                            HemiProfileTransfer.EmbedAssets(document, directory);
                            HemiProfileTransfer.Write(document, target);
                        }

                        HemiAtomicFile.WriteAllText(migratedMarker, "1");
                        if (string.Equals(name, activeName, StringComparison.OrdinalIgnoreCase))
                            reloadActive = true;
                        MelonLogger.Msg("Migrated profile folder '" + name + "' into " + Path.GetFileName(target) + ".");
                    }
                    catch (Exception exception)
                    {
                        MelonLogger.Warning("Could not migrate profile folder '" + name + "': " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not look for old profile folders: " + exception.Message);
            }

            if (reloadActive && Exists(activeName))
            {
                try
                {
                    Load(activeName);
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Could not reload the migrated profile '" + activeName + "': " + exception.Message);
                }
            }
        }

        private static string Unique(string requested)
        {
            string baseName = HemiFileNames.Sanitize(requested, "Profile", maximumLength: 100);
            string name = baseName;
            int suffix = 2;
            while (Exists(name))
                name = baseName + " " + suffix++;
            return name;
        }

        private static string Sanitize(string name)
        {
            return HemiFileNames.Sanitize(name, "Profile");
        }

        internal static MelonPreferences_Category RequireCategory() => HemiTweaksMod.Category
            ?? throw new InvalidOperationException("HemiTweaks preferences are not initialized.");
    }

    internal static class HemiProfilePolicy
    {
        private static readonly HashSet<string> Portable = new HashSet<string>(StringComparer.Ordinal)
        {
            "AutoOffsetDecimal",
            "AutoOffsetDecimalOffsets",
            "AutoOffsetPopup",
            "AutoOffsetPopupScale",
            "AutoOffsetPopupX",
            "AutoOffsetPopupY",
            "AutoOffsetRecord",
            "BlueOverlayPath",
            "BlueOverlayScale",
            "BluePlanetColor",
            "BlueRingColor",
            "BlueTailColor",
            "BuildName",
            "ComboAnchor",
            "ComboColor",
            "ComboDefaults",
            "ComboFadeEnabled",
            "ComboFadeHold",
            "ComboFadeIn",
            "ComboFadeInEase",
            "ComboFadeOut",
            "ComboFadeOutEase",
            "ComboFontSize",
            "ComboKind",
            "ComboLabelColor",
            "ComboOffsetX",
            "ComboOffsetY",
            "ComboPulseAmount",
            "ComboPulseDuration",
            "ComboPulseGrowEase",
            "ComboPulseReturnEase",
            "ComboShadowColor",
            "ComboShadowX",
            "ComboShadowY",
            "DarkMode",
            "EffectRemover",
            "EffectRemoverAdvancedFilter",
            "EffectRemoverAllDecorations",
            "EffectRemoverBackground",
            "EffectRemoverCamera",
            "EffectRemoverCameraZoom",
            "EffectRemoverCameraZoomValue",
            "EffectRemoverCheckpoint",
            "EffectRemoverDecorations",
            "EffectRemoverDefaultTrackAnimation",
            "EffectRemoverDefaultTrackColour",
            "EffectRemoverFilter",
            "EffectRemoverFrameRate",
            "EffectRemoverHide",
            "EffectRemoverHitSound",
            "EffectRemoverHoldSound",
            "EffectRemoverParticles",
            "EffectRemoverPlanetOrbit",
            "EffectRemoverPlanetRadius",
            "EffectRemoverPlanetScale",
            "EffectRemoverRepeatEvents",
            "EffectRemoverScripting",
            "EffectRemoverTrackAnimate",
            "EffectRemoverTrackColor",
            "EffectRemoverTrackMove",
            "EffectRemoverTrackOpacity",
            "EffectRemoverTrackPosition",
            "EnableAutoOffset",
            "EnableChangeBuildName",
            "EnableCombo",
            "EnableEffectRemover",
            "EnableHideUi",
            "EnableKeyViewer",
            "EnableNoCheckpoint",
            "EnableNonScroll",
            "EnableOverloadBar",
            "EnablePlanetColorChanger",
            "EnablePlanetOverlay",
            "EnableProgressBar",
            "EnableState",
            "EnableTileCornerCurvature",
            "EnableTileInfo",
            "EnableTuf",
            "HideAutoplay",
            "HideErrorMeter",
            "HideLevelTitle",
            "HideNoFail",
            "HideResults",
            "KeyViewerAnimationSpeed",
            "KeyViewerBoardAnchor",
            "KeyViewerBoardHeight",
            "KeyViewerBoardOffsetX",
            "KeyViewerBoardOffsetY",
            "KeyViewerBoardWidth",
            "KeyViewerEasing",
            "KeyViewerFontSize",
            "KeyViewerLayout",
            "KeyViewerNoteFadeFar",
            "KeyViewerNoteFadeNear",
            "KeyViewerPressedScale",
            "KeyViewerShowCounters",
            "KeyViewerShowNotes",
            "OverloadBarAnchor",
            "OverloadBarColor",
            "OverloadBarFill",
            "OverloadBarLength",
            "OverloadBarOffsetX",
            "OverloadBarOffsetY",
            "OverloadBarOrientation",
            "OverloadBarThickness",
            "OverloadBarTrack",
            "OverloadBarTrackColor",
            "ProgressBarAnchor",
            "ProgressBarColor",
            "ProgressBarHeight",
            "ProgressBarOffsetX",
            "ProgressBarOffsetY",
            "ProgressBarOutline",
            "ProgressBarOutlineColor",
            "ProgressBarOutlineSize",
            "ProgressBarSource",
            "ProgressBarTrack",
            "ProgressBarTrackColor",
            "ProgressBarWidth",
            "RedOverlayPath",
            "RedOverlayScale",
            "RedPlanetColor",
            "RedRingColor",
            "RedTailColor",
            "SmoothScrolling",
            "StateFontFilePath",
            "StateFontSize",
            "StateFontSource",
            "StateSystemFontName",
            "TileCornerCurvature",
            "TileInfoAngle",
            "TileInfoBeats",
            "TileInfoCount",
            "TileInfoExcludeLast",
            "TileInfoScale",
            "TileInfoSeconds",
            "TileInfoShadowColor",
            "TileInfoShadowX",
            "TileInfoShadowY",
            "ToggleSettingsKey",
            "TransitionAnimations",
            "TufKey",
            "UIFontSize",
            "UnlockLimits",
        };

        internal static bool IsPortable(string identifier) => identifier != null && Portable.Contains(identifier);

        internal static int ScrubSettings(JObject preferences)
        {
            int changed = 0;
            foreach (JToken entriesToken in Children(preferences, "Entries"))
            {
                JObject entries = RequireObject(entriesToken, "Preferences.Entries");
                foreach (JProperty property in new List<JProperty>(entries.Properties()))
                {
                    if (IsPortable(property.Name))
                        continue;
                    property.Remove();
                    changed++;
                }
            }
            return changed;
        }

        internal static int VisitAssets(HemiProfileDocument document, Func<string, bool, string> map)
        {
            int changed = 0;
            foreach (JToken token in Children(document.Preferences, "Entries"))
            {
                JObject entries = RequireObject(token, "Preferences.Entries");
                changed += MapFields(entries, map, false, "StateFontFilePath");
                changed += MapFields(entries, map, true, "RedOverlayPath", "BlueOverlayPath");
                foreach (JToken layout in Children(entries, "KeyViewerLayout"))
                {
                    if (layout.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)layout))
                        continue;
                    JObject keys;
                    try { keys = HemiProfileTransfer.ParseObject((string)layout); }
                    catch (JsonException) { continue; }
                    catch (InvalidDataException) { continue; }
                    int count = VisitKeys(keys, map);
                    if (count == 0)
                        continue;
                    ((JValue)layout).Value = JsonConvert.SerializeObject(keys, Formatting.None);
                    changed += count;
                }
            }

            foreach (JToken groups in Children(document.StateGroups, "Groups"))
            {
                JArray groupArray = RequireArray(groups, "StateGroups.Groups");
                foreach (JToken group in groupArray)
                {
                    JObject groupObject = RequireObject(group, "StateGroups.Groups[]");
                    foreach (JToken stats in Children(groupObject, "Stats"))
                    {
                        JArray statArray = RequireArray(stats, "StateGroups.Groups.Stats");
                        foreach (JToken stat in statArray)
                            changed += MapFields(RequireObject(stat, "StateGroups.Groups.Stats[]"), map, false, "ImagePath");
                    }
                }
            }
            return changed;
        }

        internal static int ScrubUnembeddedAssets(HemiProfileDocument document)
        {
            return VisitAssets(document,
                (text, relative) => HemiProfileTransfer.IsCarriedReference(text, relative) ? null : "");
        }

        internal static int VisitKeys(JObject document, Func<string, bool, string> map)
        {
            int changed = 0;
            foreach (JToken keys in Children(document, "keys"))
            {
                JArray array = RequireArray(keys, "keys");
                foreach (JToken token in array)
                {
                    JObject key = RequireObject(token, "keys[]");
                    changed += MapFields(key, map, false, "idleImage", "activeImage");
                    foreach (JToken advanced in Children(key, "advanced"))
                        changed += MapFields(RequireObject(advanced, "keys[].advanced"), map, false,
                            "fontFilePath", "counterFontFilePath");
                }
            }
            return changed;
        }

        private static JArray RequireArray(JToken token, string field)
        {
            if (token is JArray array)
                return array;
            throw new InvalidDataException("Profile field '" + field + "' must be an ordinary JSON array.");
        }

        private static JObject RequireObject(JToken token, string field)
        {
            if (token is JObject value)
                return value;
            throw new InvalidDataException("Profile field '" + field + "' must be an ordinary JSON object.");
        }

        private static IEnumerable<JToken> Children(JObject parent, string name)
        {
            if (parent == null)
                yield break;
            foreach (JProperty property in parent.Properties())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return property.Value;
            }
        }

        private static int MapFields(JObject parent, Func<string, bool, string> map, bool relative, params string[] names)
        {
            int changed = 0;
            foreach (string name in names)
            {
                foreach (JToken value in Children(parent, name))
                {
                    if (value.Type == JTokenType.Null)
                        continue;
                    if (value.Type != JTokenType.String)
                        throw new InvalidDataException("Profile asset field '" + name + "' must be a string.");
                    string text = (string)value;
                    if (string.IsNullOrEmpty(text))
                        continue;
                    string mapped = map(text, relative);
                    if (mapped == null || string.Equals(mapped, text, StringComparison.Ordinal))
                        continue;
                    ((JValue)value).Value = mapped;
                    changed++;
                }
            }
            return changed;
        }
    }

    internal sealed class HemiProfileDocument
    {
        public string Format = HemiProfileTransfer.Format;
        public int FormatVersion = HemiProfileTransfer.FormatVersion;
        public string Name;
        public string ModVersion = BuildInfo.Version;
        public string SavedAt;
        public JObject Preferences;
        public JObject StateGroups;
        public List<HemiProfileAsset> Assets = new List<HemiProfileAsset>();
        [JsonIgnore] internal bool NeedsSettingsRewrite;
    }

    internal sealed class HemiProfileAsset
    {
        public string Id;
        public string FileName;
        public long Size;
        public string Sha256;
        public string Base64;
    }

    internal static class HemiProfileTransfer
    {
        internal const string Extension = "htprofile";
        internal const string Format = "HemiTweaks.Profile";
        internal const int FormatVersion = 1;

        internal const int PreferencesFormatVersion = 1;

        internal const string TokenPrefix = "htprofile-asset:";

        internal const long MaximumAssetBytes = 64L * 1024 * 1024;
        internal const long MaximumTotalAssetBytes = 256L * 1024 * 1024;
        internal const long MaximumDocumentBytes = 512L * 1024 * 1024;
        internal const int MaximumAssets = 512;

        private static readonly HashSet<string> AssetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".ttf", ".otf", ".ttc"
        };

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
            MaxDepth = 64
        };

        internal static string LastStatus { get; private set; } = "";

        internal static HemiProfileDocument Capture(string name)
        {
            MelonPreferences_Category category = HemiProfiles.RequireCategory();

            JObject entries = new JObject();
            foreach (MelonPreferences_Entry entry in category.Entries)
            {
                if (!HemiProfilePolicy.IsPortable(entry.Identifier))
                    continue;
                object value = entry.BoxedValue;
                entries[entry.Identifier] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
            }

            HemiProfileDocument document = new HemiProfileDocument
            {
                Name = name,
                SavedAt = DateTime.UtcNow.ToString("o"),
                Preferences = new JObject
                {
                    ["FormatVersion"] = PreferencesFormatVersion,
                    ["Entries"] = entries
                }
            };

            string live = StateGroupStore.FilePath;
            if (File.Exists(live))
                document.StateGroups = ParseObject(File.ReadAllText(live));

            EmbedAssets(document);
            return document;
        }

        internal static bool IsCarriedReference(string text, bool relative)
        {
            return text.StartsWith(TokenPrefix, StringComparison.Ordinal)
                || (relative && HemiAssetSafety.IsSafeRelativePath(text));
        }

        internal static int EmbedAssets(HemiProfileDocument document, string sourceDirectory = null)
        {
            if (document.Assets == null)
                document.Assets = new List<HemiProfileAsset>();

            HemiProfilePolicy.ScrubSettings(document.Preferences);
            AssetCollector collector = new AssetCollector(document.Assets, sourceDirectory);
            int count = HemiProfilePolicy.VisitAssets(document, (text, relative) =>
            {
                string collected = collector.Collect(text);
                return collected ?? (IsCarriedReference(text, relative) ? null : "");
            });
            if (collector.Skipped > 0)
                MelonLogger.Warning("Profile '" + document.Name + "' left out " + collector.Skipped + " file(s) too large to embed.");
            return count;
        }

        internal static void Write(HemiProfileDocument document, string path)
        {
            HemiProfilePolicy.ScrubSettings(document.Preferences);
            HemiProfilePolicy.ScrubUnembeddedAssets(document);
            HemiAtomicFile.WriteAllText(path, JsonConvert.SerializeObject(document, Formatting.Indented, Settings));
        }

        internal static HemiProfileDocument Read(string path)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists)
                throw new FileNotFoundException("The profile file does not exist.", path);
            if (info.Length > MaximumDocumentBytes)
                throw new InvalidDataException("The file is larger than " + MaximumDocumentBytes + " bytes.");

            HemiProfileDocument document;
            try
            {
                using (Stream stream = HemiStreamSafety.OpenRead(path, MaximumDocumentBytes))
                using (StreamReader text = new StreamReader(stream))
                using (JsonTextReader reader = new JsonTextReader(text))
                    document = JsonSerializer.Create(Settings).Deserialize<HemiProfileDocument>(reader);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The file is not valid JSON.", exception);
            }

            if (document == null || !string.Equals(document.Format, Format, StringComparison.Ordinal))
                throw new InvalidDataException("The file is not a HemiTweaks profile.");
            if (document.FormatVersion <= 0 || document.FormatVersion > FormatVersion)
                throw new InvalidDataException("Unsupported profile file version: " + document.FormatVersion + ".");
            if (document.Preferences == null)
                throw new InvalidDataException("The profile holds no preferences.");
            if (document.Assets == null)
                document.Assets = new List<HemiProfileAsset>();
            if (document.Assets.Count > MaximumAssets)
                throw new InvalidDataException("The profile contains too many assets.");
            int scrubbed = HemiProfilePolicy.ScrubSettings(document.Preferences);
            scrubbed += HemiProfilePolicy.ScrubUnembeddedAssets(document);
            document.NeedsSettingsRewrite = scrubbed > 0;
            return document;
        }

        internal static int Materialize(HemiProfileDocument document, string assetFolder)
        {
            Dictionary<string, string> restored = RestoreAssets(document.Assets, assetFolder);

            int unresolved = 0;
            string Resolve(string text, bool relative)
            {
                if (!text.StartsWith(TokenPrefix, StringComparison.Ordinal))
                {
                    if (relative && HemiAssetSafety.IsSafeRelativePath(text))
                        return null;
                    unresolved++;
                    return "";
                }
                if (restored.TryGetValue(text.Substring(TokenPrefix.Length), out string path))
                    return path;
                unresolved++;
                return "";
            }

            HemiProfilePolicy.ScrubSettings(document.Preferences);
            HemiProfilePolicy.VisitAssets(document, Resolve);
            if (unresolved > 0)
                MelonLogger.Warning("Profile '" + document.Name + "': " + unresolved + " file reference(s) had no usable file and were cleared.");
            return restored.Count;
        }

        internal static bool Export()
        {
            string name = HemiProfiles.Active;
            try
            {
                HemiProfiles.SnapshotActive();
                HemiProfileDocument document = Read(HemiProfiles.FileOf(name));

                string target = HemiFilePicker.Save(
                    name + "." + Extension,
                    HemiLang.Get("UI_PROFILE_FILE_FILTER"),
                    new[] { Extension },
                    HemiLang.Get("UI_EXPORT_PROFILE_TITLE"));
                if (string.IsNullOrEmpty(target))
                {
                    SetStatus(HemiLang.Get("UI_PROFILE_TRANSFER_CANCELLED"));
                    return false;
                }
                if (!target.EndsWith("." + Extension, StringComparison.OrdinalIgnoreCase))
                    target += "." + Extension;

                Write(document, target);
                MelonLogger.Msg("Exported profile '" + name + "' to " + target
                    + " (" + document.Assets.Count + " embedded files).");
                SetStatus(HemiLang.Get("UI_PROFILE_EXPORTED", name, document.Assets.Count));
                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not export profile '" + name + "': " + exception.Message);
                SetStatus(HemiLang.Get("UI_PROFILE_EXPORT_FAILED"));
                return false;
            }
        }

        internal static bool Import()
        {
            string source = HemiFilePicker.Pick(
                HemiLang.Get("UI_PROFILE_FILE_FILTER"),
                new[] { Extension },
                HemiLang.Get("UI_IMPORT_PROFILE_TITLE"));
            if (string.IsNullOrEmpty(source))
            {
                SetStatus(HemiLang.Get("UI_PROFILE_TRANSFER_CANCELLED"));
                return false;
            }

            string name = null;
            try
            {
                HemiProfileDocument document;
                try
                {
                    document = Read(source);
                }
                catch (InvalidDataException exception)
                {
                    MelonLogger.Warning("Not a HemiTweaks profile (" + source + "): " + exception.Message);
                    SetStatus(HemiLang.Get("UI_PROFILE_NOT_A_PROFILE"));
                    return false;
                }

                string requested = string.IsNullOrWhiteSpace(document.Name)
                    ? Path.GetFileNameWithoutExtension(source)
                    : document.Name;
                name = HemiProfiles.UniqueName(requested);
                document.Name = name;
                Write(document, HemiProfiles.FileOf(name));

                HemiProfiles.Apply(name);
                if (!string.Equals(HemiProfiles.Active, name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("the profile was written but could not be applied; it is kept as '" + name + "'");

                MelonLogger.Msg("Imported profile '" + name + "' from " + source
                    + " (" + document.Assets.Count + " embedded files).");
                SetStatus(HemiLang.Get("UI_PROFILE_IMPORTED", name, document.Assets.Count));
                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Could not import profile" + (name == null ? "" : " '" + name + "'")
                    + " from " + source + ": " + exception.Message);
                SetStatus(HemiLang.Get("UI_PROFILE_IMPORT_FAILED"));
                return false;
            }
        }

        private static void SetStatus(string text)
        {
            LastStatus = text ?? "";
        }

        internal static JObject ParseObject(string text)
        {
            return JsonConvert.DeserializeObject<JObject>(text, Settings)
                ?? throw new InvalidDataException("The snapshot is empty.");
        }

        internal sealed class AssetCollector
        {
            private readonly List<HemiProfileAsset> assets;
            private readonly Dictionary<string, HemiProfileAsset> byPath = new Dictionary<string, HemiProfileAsset>(StringComparer.OrdinalIgnoreCase);
            private readonly string sourceDirectory;
            private long total;

            internal int Skipped { get; private set; }

            internal AssetCollector(List<HemiProfileAsset> assets, string sourceDirectory)
            {
                this.assets = assets;
                this.sourceDirectory = sourceDirectory;
                foreach (HemiProfileAsset asset in assets)
                {
                    if (asset != null && asset.Base64 != null)
                        total += asset.Size;
                }
            }

            internal string Collect(string text)
            {
                if (text.Length > 2048 || text.StartsWith(TokenPrefix, StringComparison.Ordinal))
                    return null;

                string full;
                try
                {
                    if (!HemiAssetSafety.IsLocalPath(text) || !AssetExtensions.Contains(Path.GetExtension(text)))
                        return null;
                    if (sourceDirectory != null)
                    {
                        full = HemiAssetSafety.ImportedLocalPath(text, sourceDirectory);
                        if (string.IsNullOrEmpty(full))
                            return null;
                    }
                    else
                    {
                        if (!Path.IsPathRooted(text))
                            return null;
                        full = Path.GetFullPath(text);
                    }
                }
                catch (Exception)
                {
                    return null;
                }

                if (!File.Exists(full))
                    return null;

                if (byPath.TryGetValue(full, out HemiProfileAsset existing))
                    return TokenPrefix + existing.Id;

                long size = new FileInfo(full).Length;
                if (size > MaximumAssetBytes || total + size > MaximumTotalAssetBytes || assets.Count >= MaximumAssets)
                {
                    MelonLogger.Warning("Profile left out " + full + ": too large to embed (" + size + " bytes).");
                    Skipped++;
                    return null;
                }

                byte[] bytes = HemiStreamSafety.ReadFile(full, Math.Min(MaximumAssetBytes, MaximumTotalAssetBytes - total));
                HemiProfileAsset asset = new HemiProfileAsset
                {
                    Id = "a" + (assets.Count + 1),
                    FileName = StripHashSuffix(Path.GetFileNameWithoutExtension(full)) + Path.GetExtension(full),
                    Size = bytes.LongLength,
                    Sha256 = HemiHex.Sha256(bytes),
                    Base64 = Convert.ToBase64String(bytes)
                };
                assets.Add(asset);
                byPath[full] = asset;
                total += bytes.LongLength;
                return TokenPrefix + asset.Id;
            }
        }

        internal static Dictionary<string, string> RestoreAssets(List<HemiProfileAsset> assets, string folder, bool pruneUnused = true)
        {
            if (assets != null && assets.Count > MaximumAssets)
                throw new InvalidDataException("The profile contains too many assets.");
            Dictionary<string, string> paths = new Dictionary<string, string>(StringComparer.Ordinal);
            HashSet<string> wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (assets != null && assets.Count > 0)
            {
                Directory.CreateDirectory(folder);
                string root = Path.GetFullPath(folder);
                long total = 0;

                foreach (HemiProfileAsset asset in assets)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.Id) || string.IsNullOrEmpty(asset.Base64))
                        continue;
                    if (paths.ContainsKey(asset.Id))
                        continue;

                    try
                    {
                        string fileName = HemiFileNames.Sanitize(Path.GetFileName(asset.FileName ?? ""), "asset", maximumLength: 120);
                        string extension = Path.GetExtension(fileName);
                        if (!AssetExtensions.Contains(extension))
                        {
                            MelonLogger.Warning("Profile asset " + asset.Id + " skipped: unsupported type '" + extension + "'.");
                            continue;
                        }

                        byte[] bytes = HemiStreamSafety.DecodeBase64(asset.Base64,
                            Math.Min(MaximumAssetBytes, MaximumTotalAssetBytes - total));
                        if (bytes.LongLength > MaximumAssetBytes || total + bytes.LongLength > MaximumTotalAssetBytes)
                        {
                            MelonLogger.Warning("Profile asset " + asset.Id + " skipped: too large (" + bytes.LongLength + " bytes).");
                            continue;
                        }

                        string hash = HemiHex.Sha256(bytes);
                        if (!string.IsNullOrEmpty(asset.Sha256)
                            && !string.Equals(asset.Sha256, hash, StringComparison.OrdinalIgnoreCase))
                        {
                            MelonLogger.Warning("Profile asset " + asset.Id + " skipped: contents do not match their checksum.");
                            continue;
                        }

                        string candidate = StripHashSuffix(Path.GetFileNameWithoutExtension(fileName))
                            + "-" + hash.Substring(0, 8) + extension;
                        string path = Path.GetFullPath(Path.Combine(root, candidate));
                        if (!HemiAssetSafety.IsWithin(path, root))
                            continue;

                        FileInfo existing = new FileInfo(path);
                        if (!existing.Exists || existing.Length != bytes.LongLength)
                            HemiAtomicFile.WriteAllBytes(path, bytes);

                        wanted.Add(candidate);
                        paths[asset.Id] = path;
                        total += bytes.LongLength;
                    }
                    catch (Exception exception)
                    {
                        MelonLogger.Warning("Profile asset " + asset.Id + " skipped: " + exception.Message);
                    }
                }
            }

            if (pruneUnused)
                SweepUnused(folder, wanted);
            return paths;
        }

        private static void SweepUnused(string folder, HashSet<string> wanted)
        {
            if (!Directory.Exists(folder))
                return;

            try
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (wanted.Contains(Path.GetFileName(file)))
                        continue;
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                if (wanted.Count == 0 && Directory.GetFileSystemEntries(folder).Length == 0)
                    Directory.Delete(folder);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not tidy profile assets in " + folder + ": " + exception.Message);
            }
        }

        internal static void TryDeleteAssetFolder(string folder)
        {
            SweepUnused(folder, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static string StripHashSuffix(string stem)
        {
            return HashSuffix.Replace(stem ?? "", "");
        }

        private static readonly Regex HashSuffix = new Regex("(-[0-9a-fA-F]{8})+$", RegexOptions.Compiled);
    }
}
