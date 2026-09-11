using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityFileDialog;
using UnityEngine;

namespace HemiTweaks
{
    [Serializable]
    internal sealed class KeyViewerProfileFile
    {
        public string format = KeyViewerProfiles.ProfileFormat;
        public int formatVersion = KeyViewerProfiles.CurrentFormatVersion;
        public string name;
        public string createdUtc;
        public string updatedUtc;
        public bool enabled;

        public int designWidth;
        public int designHeight;

        public KeyViewerProfileKeyData[] keys;
        public int fontSize;
        public float pressedScale;
        public float animationSpeed;
        public string easing;
        public bool showCounters;
        public float noteFadeNear;
        public float noteFadeFar;
        public KeyViewerAdvancedConfigData advanced;
        public List<Interface.HemiProfileAsset> assets = null;

        public float boardWidth;
        public float boardHeight;
        public int boardAnchor;
        public float boardOffsetX;
        public float boardOffsetY;
    }

    [Serializable]
    internal sealed class KeyViewerProfileKeyData
    {
        public string kind;

        public string stat;

        public string key;
        public float x;
        public float y;
        public float width;
        public float height;
        public string backgroundColor;
        public string borderColor;
        public float borderWidth;
        public float cornerRadius;
        public string textColor;
        public bool rainingEffect;
        public float rainHeight;
        public float rainSpeed;
        public string rainColor;
        public float rainOpacity;
        public float rainCornerRadius;
        public bool rainReverse;
        public float rainMinimumLength;
        public string rainBorderColor;
        public float rainBorderWidth;

        public string activeBackgroundColor;
        public string activeBorderColor;
        public string activeTextColor;
        public string displayText;
        public float fontSize;
        public bool idleTransparent;
        public bool activeTransparent;
        public string idleImage;
        public string activeImage;
        public string imageFit;
        public bool shadowEnabled;
        public string shadowColor;
        public string activeShadowColor;
        public float shadowOffsetX;
        public float shadowOffsetY;
        public float shadowBlur;

        public bool rainGradient;
        public string rainColorBottom;
        public float rainOpacityBottom;
        public float rainBorderOpacity;
        public string rainBorderSide;
        public float noteWidth;
        public string noteAlignment;
        public float noteOffsetX;
        public float noteOffsetY;
        public bool glowEnabled;
        public float glowSize;
        public float glowOpacity;
        public string glowColor;

        public bool counterEnabled;
        public string counterPlacement;
        public string counterAlign;
        public string counterAlignMode;
        public float counterGap;
        public float counterFontSize;
        public string counterIdleColor;
        public string counterActiveColor;
        public bool counterAnimationEnabled;
        public float counterAnimationScale;
        public float counterAnimationSeconds;
        public int count;
        public KeyViewerAdvancedKeyData advanced;
    }

    internal static class KeyViewerProfiles
    {
        public const string ProfileFormat = "HemiTweaks.KeyViewerProfile";
        public const int CurrentFormatVersion = 3;

        private const long MaximumProfileBytes = Interface.HemiProfileTransfer.MaximumDocumentBytes;
        private const int MaximumProfileNameLength = 64;
        public const int MaximumKeys = 512;

        private static readonly List<ProfileRecord> Profiles = new List<ProfileRecord>();
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static MelonPreferences_Category preferencesCategory;
        private static MelonPreferences_Entry<bool> defaultProfileCreatedEntry;
        private static MelonPreferences_Entry<string> activeProfileNameEntry;
        private static string activeProfilePath;
        private static int selectedIndex = -1;
        private static bool initialized;
        private static bool writingActiveProfile;
        private static string statusMessage;

        public static int Count => Profiles.Count;
        public static bool HasSelection => selectedIndex >= 0 && selectedIndex < Profiles.Count;
        public static string SelectedName => HasSelection ? Profiles[selectedIndex].Name : Interface.HemiLang.Get("KVP_NO_PROFILES");
        public static int SelectedDisplayIndex => HasSelection ? selectedIndex + 1 : 0;
        public static bool SelectedIsActive => HasSelection && PathsEqual(Profiles[selectedIndex].Path, activeProfilePath);
        public static string ProfilesDirectory =>
            Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, "KeyViewerProfiles");
        public static string StatusMessage => statusMessage ?? Interface.HemiLang.Get("KVP_STATUS_NONE_YET");
        public static bool StatusIsError { get; private set; }

        public static void Initialize(MelonPreferences_Category category)
        {
            preferencesCategory = category;
            defaultProfileCreatedEntry = category.CreateEntry(
                "KeyViewerDefaultProfileCreated",
                false,
                "KeyViewer Default Profile Created",
                "Tracks the one-time creation of the built-in Default profile.");
            activeProfileNameEntry = category.CreateEntry(
                "KeyViewerActiveProfile",
                "",
                "KeyViewer Active Profile",
                "Profile that receives automatic KeyViewer setting saves.");

            RepairProfilesMissingKeyData();
            Refresh(null);
            bool createdDefaultNow = EnsureDefaultProfileOnce();
            string preferredActiveName = activeProfileNameEntry.Value;
            int preferredIndex = FindByName(preferredActiveName);

            if (preferredIndex < 0 && createdDefaultNow)
                preferredIndex = FindByName("Default");

            if (preferredIndex >= 0)
            {
                selectedIndex = preferredIndex;
                SetActiveProfile(Profiles[preferredIndex], false);
            }
            else
            {
                activeProfilePath = null;
                if (!string.IsNullOrEmpty(activeProfileNameEntry.Value))
                {
                    activeProfileNameEntry.Value = "";
                    SaveProfilePreferences();
                }
            }

            initialized = true;
            if (Profiles.Count > 0)
                SetStatus(Interface.HemiLang.Get(
                    Profiles.Count == 1 ? "KVP_STATUS_ONE_AVAILABLE" : "KVP_STATUS_MANY_AVAILABLE",
                    Profiles.Count), false);
        }

        public static void SelectPrevious()
        {
            if (Profiles.Count == 0)
                return;

            selectedIndex = (selectedIndex - 1 + Profiles.Count) % Profiles.Count;
            SetStatus(Interface.HemiLang.Get("KVP_STATUS_SELECTED", SelectedName), false);
        }

        public static void SelectNext()
        {
            if (Profiles.Count == 0)
                return;

            selectedIndex = (selectedIndex + 1) % Profiles.Count;
            SetStatus(Interface.HemiLang.Get("KVP_STATUS_SELECTED", SelectedName), false);
        }

        public static bool Create(string requestedName)
        {
            string name = NormalizeProfileName(requestedName);
            if (string.IsNullOrEmpty(name))
                return Fail(Interface.HemiLang.Get("KVP_ERROR_NAME_REQUIRED"));

            if (FindByName(name) >= 0)
                return Fail(Interface.HemiLang.Get("KVP_ERROR_NAME_TAKEN"));

            try
            {
                string path = GetInternalPath(name);
                KeyViewerProfileFile profile = KeyViewer.CreateProfileSnapshot(name);
                WriteProfile(path, profile, name, null);
                Refresh(path);
                if (!HasSelection || !PathsEqual(Profiles[selectedIndex].Path, path))
                    return Fail(Interface.HemiLang.Get("KVP_ERROR_CREATED_NOT_LOADED"));

                SetActiveProfile(Profiles[selectedIndex], true);
                return Succeed(Interface.HemiLang.Get("KVP_STATUS_CREATED", name));
            }
            catch (Exception ex)
            {
                return Fail(Interface.HemiLang.Get("KVP_ERROR_CREATE", ex.Message));
            }
        }

        public static bool SaveSelected()
        {
            if (!HasSelection)
                return Fail(Interface.HemiLang.Get("KVP_ERROR_SELECT_TO_SAVE"));

            ProfileRecord record = Profiles[selectedIndex];
            try
            {
                string createdUtc = null;
                if (TryReadProfile(record.Path, out KeyViewerProfileFile existing, out _))
                    createdUtc = existing.createdUtc;

                KeyViewerProfileFile profile = KeyViewer.CreateProfileSnapshot(record.Name);
                WriteProfile(record.Path, profile, record.Name, createdUtc);
                Refresh(record.Path);
                return Succeed(Interface.HemiLang.Get("KVP_STATUS_SAVED", record.Name));
            }
            catch (Exception ex)
            {
                return Fail(Interface.HemiLang.Get("KVP_ERROR_SAVE", ex.Message));
            }
        }

        public static bool LoadSelected()
        {
            KeyViewerHistory.Clear();
            if (!HasSelection)
                return Fail(Interface.HemiLang.Get("KVP_ERROR_SELECT_TO_LOAD"));

            ProfileRecord record = Profiles[selectedIndex];
            if (!TryReadProfile(record.Path, out KeyViewerProfileFile profile, out string error))
                return Fail(error);

            if (!KeyViewer.ApplyProfile(profile, out error))
                return Fail(error);

            SetActiveProfile(record, true);
            return Succeed(Interface.HemiLang.Get("KVP_STATUS_LOADED", record.Name));
        }

        public static void AutoSaveActive()
        {
            if (!initialized || writingActiveProfile || string.IsNullOrEmpty(activeProfilePath))
                return;

            if (!File.Exists(activeProfilePath))
            {
                ClearActiveProfile();
                return;
            }

            writingActiveProfile = true;
            try
            {
                string name = NormalizeProfileName(activeProfileNameEntry.Value);
                if (string.IsNullOrEmpty(name))
                    name = NormalizeProfileName(Path.GetFileNameWithoutExtension(activeProfilePath));

                if (!TryGetCachedCreatedUtc(activeProfilePath, out string createdUtc))
                {
                    createdUtc = null;
                    if (TryReadProfile(activeProfilePath, out KeyViewerProfileFile existing, out _))
                        createdUtc = existing.createdUtc;
                }

                KeyViewerProfileFile profile = KeyViewer.CreateProfileSnapshot(name);
                WriteProfile(activeProfilePath, profile, name, createdUtc);
            }
            catch (Exception ex)
            {
                Fail(Interface.HemiLang.Get("KVP_ERROR_AUTOSAVE", ex.Message));
            }
            finally
            {
                writingActiveProfile = false;
            }
        }

        public static bool ImportFromFile()
        {
            try
            {
                string sourcePath = FileBrowser.PickFile(
                    Persistence.GetLastUsedFolder(),
                    Interface.HemiLang.Get("KVP_FILE_FILTER_JSON"),
                    new[] { "json" },
                    Interface.HemiLang.Get("KVP_DIALOG_IMPORT"));
                if (string.IsNullOrEmpty(sourcePath))
                    return false;

                Persistence.UpdateLastUsedFolder(sourcePath);
                if (!TryReadProfile(sourcePath, out KeyViewerProfileFile profile, out string error, true))
                    return Fail(error);

                string fallbackName = Path.GetFileNameWithoutExtension(sourcePath);
                string baseName = NormalizeProfileName(string.IsNullOrWhiteSpace(profile.name) ? fallbackName : profile.name);
                if (string.IsNullOrEmpty(baseName))
                    baseName = "Imported Profile";

                string uniqueName = GetUniqueName(baseName);
                string destinationPath = GetInternalPath(uniqueName);
                profile = HemiKeyProfileAssets.Read(JObject.FromObject(profile), destinationPath, true)
                    .ToObject<KeyViewerProfileFile>();
                WriteProfile(destinationPath, profile, uniqueName, profile.createdUtc);
                Refresh(destinationPath);
                if (!HasSelection || !PathsEqual(Profiles[selectedIndex].Path, destinationPath))
                    return Fail(Interface.HemiLang.Get("KVP_ERROR_IMPORTED_NOT_LOADED"));

                return Succeed(Interface.HemiLang.Get("KVP_STATUS_IMPORTED", uniqueName));
            }
            catch (Exception ex)
            {
                return Fail(Interface.HemiLang.Get("KVP_ERROR_IMPORT", ex.Message));
            }
        }

        public static bool ExportSelected()
        {
            if (!HasSelection)
                return Fail(Interface.HemiLang.Get("KVP_ERROR_SELECT_TO_EXPORT"));

            ProfileRecord record = Profiles[selectedIndex];
            if (!TryReadProfile(record.Path, out KeyViewerProfileFile profile, out string error))
                return Fail(error);

            try
            {
                string destinationPath = FileBrowser.SaveFile(
                    Persistence.GetLastUsedFolder(),
                    record.Name + ".json",
                    Interface.HemiLang.Get("KVP_FILE_FILTER_JSON"),
                    new[] { "json" },
                    Interface.HemiLang.Get("KVP_DIALOG_EXPORT"));
                if (string.IsNullOrEmpty(destinationPath))
                    return false;

                if (!string.Equals(Path.GetExtension(destinationPath), ".json", StringComparison.OrdinalIgnoreCase))
                    destinationPath += ".json";

                Persistence.UpdateLastUsedFolder(destinationPath);
                JObject portable = HemiKeyProfileAssets.Portable(JObject.FromObject(profile));
                string json = JsonConvert.SerializeObject(portable, Formatting.Indented);
                if (Utf8WithoutBom.GetByteCount(json) > MaximumProfileBytes)
                    throw new InvalidDataException("The exported key profile exceeds its size limit.");
                HemiAtomicFile.WriteAllText(destinationPath, json, Utf8WithoutBom);
                return Succeed(Interface.HemiLang.Get("KVP_STATUS_EXPORTED", record.Name));
            }
            catch (Exception ex)
            {
                return Fail(Interface.HemiLang.Get("KVP_ERROR_EXPORT", ex.Message));
            }
        }

        public static bool DeleteSelected()
        {
            if (!HasSelection)
                return Fail(Interface.HemiLang.Get("KVP_ERROR_SELECT_TO_DELETE"));

            ProfileRecord record = Profiles[selectedIndex];
            try
            {
                bool deletingActive = PathsEqual(record.Path, activeProfilePath);
                File.Delete(record.Path);
                if (deletingActive)
                    ClearActiveProfile();
                Refresh(null);
                return Succeed(Interface.HemiLang.Get("KVP_STATUS_DELETED", record.Name));
            }
            catch (Exception ex)
            {
                return Fail(Interface.HemiLang.Get("KVP_ERROR_DELETE", ex.Message));
            }
        }

        private static void Refresh(string preferredPath)
        {
            Profiles.Clear();
            selectedIndex = -1;

            try
            {
                Directory.CreateDirectory(ProfilesDirectory);
                string[] paths = Directory.GetFiles(ProfilesDirectory, "*.json", SearchOption.TopDirectoryOnly);
                Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (!TryReadProfile(path, out KeyViewerProfileFile profile, out string error))
                    {
                        MelonLogger.Warning("Ignoring invalid KeyViewer profile '" + Path.GetFileName(path) + "': " + error);
                        continue;
                    }

                    string name = NormalizeProfileName(profile.name);
                    if (string.IsNullOrEmpty(name))
                        name = NormalizeProfileName(Path.GetFileNameWithoutExtension(path));

                    Profiles.Add(new ProfileRecord(name, path));
                }

                Profiles.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
                if (Profiles.Count == 0)
                    return;

                selectedIndex = 0;
                if (!string.IsNullOrEmpty(preferredPath))
                {
                    string normalizedPreferred = Path.GetFullPath(preferredPath);
                    for (int i = 0; i < Profiles.Count; i++)
                    {
                        if (!string.Equals(Path.GetFullPath(Profiles[i].Path), normalizedPreferred, StringComparison.OrdinalIgnoreCase))
                            continue;

                        selectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Fail(Interface.HemiLang.Get("KVP_ERROR_FOLDER_READ", ex.Message));
            }
        }

        private static bool EnsureDefaultProfileOnce()
        {
            if (defaultProfileCreatedEntry.Value)
                return false;

            try
            {
                string defaultPath = GetInternalPath("Default");
                if (!TryReadProfile(defaultPath, out _, out _))
                {
                    KeyViewerProfileFile profile = KeyViewer.CreateProfileSnapshot("Default");
                    WriteProfile(defaultPath, profile, "Default", null);
                }

                defaultProfileCreatedEntry.Value = true;
                if (string.IsNullOrEmpty(activeProfileNameEntry.Value))
                    activeProfileNameEntry.Value = "Default";

                SaveProfilePreferences();
                Refresh(defaultPath);
                return true;
            }
            catch (Exception ex)
            {
                Fail(Interface.HemiLang.Get("KVP_ERROR_DEFAULT_CREATE", ex.Message));
                return false;
            }
        }

        private static void RepairProfilesMissingKeyData()
        {
            try
            {
                Directory.CreateDirectory(ProfilesDirectory);
                string[] paths = Directory.GetFiles(ProfilesDirectory, "*.json", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    FileInfo file = new FileInfo(path);
                    if (!file.Exists || file.Length <= 0 || file.Length > MaximumProfileBytes)
                        continue;

                    KeyViewerProfileFile broken = JsonConvert.DeserializeObject<KeyViewerProfileFile>(
                        File.ReadAllText(path, Encoding.UTF8));
                    if (broken == null ||
                        !string.Equals(broken.format, ProfileFormat, StringComparison.Ordinal) ||
                        broken.formatVersion != CurrentFormatVersion ||
                        broken.keys != null)
                    {
                        continue;
                    }

                    string name = NormalizeProfileName(broken.name);
                    if (string.IsNullOrEmpty(name))
                        name = NormalizeProfileName(Path.GetFileNameWithoutExtension(path));
                    if (string.IsNullOrEmpty(name))
                        continue;

                    KeyViewerProfileFile repaired = KeyViewer.CreateProfileSnapshot(name);
                    repaired.enabled = broken.enabled;
                    repaired.fontSize = broken.fontSize;
                    repaired.pressedScale = broken.pressedScale;
                    repaired.animationSpeed = broken.animationSpeed;
                    repaired.easing = broken.easing;
                    WriteProfile(path, repaired, name, broken.createdUtc);
                    MelonLogger.Msg("Repaired KeyViewer profile '" + name + "' that was missing key data.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not repair incomplete KeyViewer profiles: " + ex.Message);
            }
        }

        private static void SetActiveProfile(ProfileRecord record, bool savePreferences)
        {
            activeProfilePath = record?.Path;
            activeProfileNameEntry.Value = record?.Name ?? "";
            if (savePreferences)
                SaveProfilePreferences();
        }

        private static void ClearActiveProfile()
        {
            activeProfilePath = null;
            if (activeProfileNameEntry != null)
                activeProfileNameEntry.Value = "";
            SaveProfilePreferences();
        }

        private static void SaveProfilePreferences()
        {
            preferencesCategory?.SaveToFile(false);
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return false;

            try
            {
                return string.Equals(
                    Path.GetFullPath(left),
                    Path.GetFullPath(right),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool TryReadProfile(string path, out KeyViewerProfileFile profile, out string error, bool forImport = false)
        {
            profile = null;
            error = null;

            try
            {
                FileInfo file = new FileInfo(path);
                if (!file.Exists)
                {
                    error = Interface.HemiLang.Get("KVP_ERROR_FILE_MISSING");
                    return false;
                }

                if (file.Length <= 0 || file.Length > MaximumProfileBytes)
                {
                    error = Interface.HemiLang.Get("KVP_ERROR_FILE_SIZE");
                    return false;
                }

                JObject document;
                using (Stream input = HemiStreamSafety.OpenRead(path, MaximumProfileBytes))
                using (StreamReader text = new StreamReader(input, Encoding.UTF8))
                using (JsonTextReader reader = new JsonTextReader(text) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                    document = JObject.Load(reader);
                StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                    ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                bool stored = string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)),
                    Path.GetFullPath(ProfilesDirectory), comparison);
                if (stored)
                {
                    if (!HemiAssetSafety.IsWithin(path, ProfilesDirectory))
                        throw new InvalidDataException("Unsafe key-profile storage path.");
                    JObject mapped = HemiKeyProfileAssets.Read(document, path, false);
                    if (!JToken.DeepEquals(document, mapped))
                    {
                        string backup = Path.Combine(ProfilesDirectory, "Legacy", Path.GetFileName(path));
                        if (!File.Exists(backup))
                            HemiAtomicFile.Copy(path, backup);
                    }
                    document = mapped;
                    if (forImport)
                        document = HemiKeyProfileAssets.Portable(document);
                }
                profile = document.ToObject<KeyViewerProfileFile>();
                return ValidateProfile(profile, out error);
            }
            catch (Exception ex)
            {
                error = Interface.HemiLang.Get("KVP_ERROR_READ", ex.Message);
                return false;
            }
        }

        private static bool ValidateProfile(KeyViewerProfileFile profile, out string error)
        {
            if (profile == null || !string.Equals(profile.format, ProfileFormat, StringComparison.Ordinal))
            {
                error = Interface.HemiLang.Get("KVP_ERROR_NOT_PROFILE");
                return false;
            }

            if (profile.formatVersion != CurrentFormatVersion)
            {
                error = Interface.HemiLang.Get("KVP_ERROR_VERSION");
                return false;
            }

            if (profile.keys == null || profile.keys.Length > MaximumKeys)
            {
                error = Interface.HemiLang.Get("KVP_ERROR_KEY_LIST");
                return false;
            }

            if (!KeyViewer.ValidateProfile(profile, out error))
                return false;

            error = null;
            return true;
        }

        private static void WriteProfile(string path, KeyViewerProfileFile profile, string name, string createdUtc)
        {
            Directory.CreateDirectory(ProfilesDirectory);

            string now = DateTime.UtcNow.ToString("O");
            profile.format = ProfileFormat;
            profile.formatVersion = CurrentFormatVersion;
            profile.name = name;
            profile.createdUtc = string.IsNullOrWhiteSpace(createdUtc) ? now : createdUtc;
            profile.updatedUtc = now;

            JObject local = HemiKeyProfileAssets.Local(JObject.FromObject(profile), path);
            string json = JsonConvert.SerializeObject(local, Formatting.Indented);
            if (Utf8WithoutBom.GetByteCount(json) > MaximumProfileBytes)
                throw new InvalidDataException("The key profile exceeds its size limit.");
            HemiAtomicFile.WriteAllText(path, json, Utf8WithoutBom);

            RememberCreatedUtc(path, profile.createdUtc);
        }

        private static string createdUtcPath;
        private static DateTime createdUtcStamp;
        private static long createdUtcLength;
        private static string createdUtcValue;

        private static bool TryGetCachedCreatedUtc(string path, out string createdUtc)
        {
            createdUtc = null;
            if (string.IsNullOrEmpty(createdUtcPath) || !string.Equals(createdUtcPath, path, StringComparison.Ordinal))
                return false;

            try
            {
                FileInfo file = new FileInfo(path);
                if (!file.Exists || file.LastWriteTimeUtc != createdUtcStamp || file.Length != createdUtcLength)
                    return false;
            }
            catch
            {
                return false;
            }

            createdUtc = createdUtcValue;
            return true;
        }

        private static void RememberCreatedUtc(string path, string createdUtc)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                if (!file.Exists)
                {
                    createdUtcPath = null;
                    return;
                }

                createdUtcPath = path;
                createdUtcStamp = file.LastWriteTimeUtc;
                createdUtcLength = file.Length;
                createdUtcValue = createdUtc;
            }
            catch
            {
                createdUtcPath = null;
            }
        }

        private static string GetInternalPath(string name)
        {
            return Path.Combine(ProfilesDirectory, name + ".json");
        }

        private static int FindByName(string name)
        {
            for (int i = 0; i < Profiles.Count; i++)
            {
                if (string.Equals(Profiles[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string GetUniqueName(string requestedName)
        {
            if (FindByName(requestedName) < 0 && !File.Exists(GetInternalPath(requestedName)))
                return requestedName;

            for (int suffix = 2; suffix < 10000; suffix++)
            {
                string suffixText = " (" + suffix + ")";
                int baseLength = Mathf.Max(1, MaximumProfileNameLength - suffixText.Length);
                string baseName = requestedName.Length > baseLength ? requestedName.Substring(0, baseLength) : requestedName;
                string candidate = baseName + suffixText;
                if (FindByName(candidate) < 0 && !File.Exists(GetInternalPath(candidate)))
                    return candidate;
            }

            return "Imported Profile " + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        }

        private static string NormalizeProfileName(string requestedName)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
                return "";

            string name = requestedName.Trim();
            if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 5);

            return HemiFileNames.Sanitize(name, "", '_', MaximumProfileNameLength).TrimStart('.');
        }

        private static bool Succeed(string message)
        {
            SetStatus(message, false);
            return true;
        }

        private static bool Fail(string message)
        {
            SetStatus(message, true);
            return false;
        }

        private static void SetStatus(string message, bool isError)
        {
            statusMessage = message ?? "";
            StatusIsError = isError;
        }

        private sealed class ProfileRecord
        {
            public ProfileRecord(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public string Name { get; }
            public string Path { get; }
        }
    }

    internal static class KeyViewerDmNote
    {
        internal static readonly string[] FileExtensions = { "json" };

        internal struct Result
        {
            public bool Success;
            public string Message;
        }

        internal struct TabInfo
        {
            public string Id;
            public string Name;
            public int Keys;
        }

        internal sealed class Preset
        {
            internal JObject Root;
            internal JObject KeyPositions;
            internal List<TabInfo> Tabs = new List<TabInfo>();

            internal string PreferredTab;
        }

        internal static bool TryRead(string path, out Preset preset, out string error)
        {
            preset = null;
            error = null;

            if (!HemiAssetSafety.IsLocalPath(path) || !File.Exists(path))
            {
                error = Interface.HemiLang.Get("KVP_DM_ERROR_FILE_MISSING");
                return false;
            }

            JObject root;
            try
            {
                using (Stream input = HemiStreamSafety.OpenRead(path, Interface.HemiProfileTransfer.MaximumDocumentBytes))
                using (StreamReader text = new StreamReader(input))
                using (JsonTextReader reader = new JsonTextReader(text) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                    root = JObject.Load(reader);
            }
            catch (Exception exception)
            {
                error = Interface.HemiLang.Get("KVP_DM_ERROR_PARSE", exception.Message);
                return false;
            }

            JObject keyPositions = root["keyPositions"] as JObject ?? root["positions"] as JObject;
            JObject statPositions = root["statPositions"] as JObject;
            JObject graphPositions = root["graphPositions"] as JObject;
            JObject knobPositions = root["knobPositions"] as JObject;
            if (keyPositions == null && statPositions == null && graphPositions == null && knobPositions == null)
            {
                error = Interface.HemiLang.Get("KVP_DM_ERROR_NOT_PRESET");
                return false;
            }

            try { HemiDmNoteAssets.Prepare(root, path); }
            catch (Exception exception)
            {
                error = Interface.HemiLang.Get("KVP_DM_ERROR_PARSE", exception.Message);
                return false;
            }

            preset = new Preset
            {
                Root = root,
                KeyPositions = keyPositions ?? new JObject()
            };

            List<string> tabIds = new List<string>();
            AddTabIds(tabIds, keyPositions);
            AddTabIds(tabIds, statPositions);
            AddTabIds(tabIds, graphPositions);
            AddTabIds(tabIds, knobPositions);
            for (int i = 0; i < tabIds.Count; i++)
            {
                string id = tabIds[i];
                int count = CountMappedKeys(root, keyPositions, id)
                    + CountElements(statPositions, id)
                    + CountElements(graphPositions, id)
                    + CountElements(knobPositions, id);
                if (count == 0)
                    continue;

                preset.Tabs.Add(new TabInfo
                {
                    Id = id,
                    Name = TabName(root, id),
                    Keys = count
                });
            }

            if (preset.Tabs.Count == 0)
            {
                preset = null;
                error = Interface.HemiLang.Get("KVP_DM_ERROR_NO_KEYS");
                return false;
            }

            string selected = root["selectedKeyType"]?.ToString();
            preset.PreferredTab = !string.IsNullOrWhiteSpace(selected) && HasTab(preset, selected)
                ? selected
                : preset.Tabs[0].Id;

            return true;
        }

        private static void AddTabIds(List<string> ids, JObject table)
        {
            if (table == null)
                return;
            foreach (JProperty property in table.Properties())
            {
                if (property.Value is not JArray array || array.Count == 0 || ids.Contains(property.Name))
                    continue;
                ids.Add(property.Name);
            }
        }

        private static bool HasTab(Preset preset, string id)
        {
            for (int i = 0; i < preset.Tabs.Count; i++)
            {
                if (string.Equals(preset.Tabs[i].Id, id, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static string TabName(JObject root, string id)
        {
            if (root["customTabs"] is JArray custom)
            {
                foreach (JToken entry in custom)
                {
                    if (entry is JObject o && string.Equals(o["id"]?.ToString(), id, StringComparison.Ordinal))
                    {
                        string name = o["name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                            return name;
                    }
                }
            }

            return id;
        }

        internal static Result Apply(Preset preset, string tab)
        {
            if (preset == null)
                return Fail(Interface.HemiLang.Get("KVP_DM_ERROR_NOTHING"));

            JObject root = preset.Root;
            JArray elements = preset.KeyPositions[tab] as JArray;
            JArray statElements = (root["statPositions"] as JObject)?[tab] as JArray;
            JArray graphElements = (root["graphPositions"] as JObject)?[tab] as JArray;
            JArray knobElements = (root["knobPositions"] as JObject)?[tab] as JArray;
            if ((elements == null || elements.Count == 0)
                && (statElements == null || statElements.Count == 0)
                && (graphElements == null || graphElements.Count == 0)
                && (knobElements == null || knobElements.Count == 0))
                return Fail(Interface.HemiLang.Get("KVP_DM_ERROR_TAB_EMPTY"));

            JArray names = (root["keys"] as JObject)?[tab] as JArray;
            JObject noteSettings = ResolveNoteSettings(root, tab);

            List<KeyViewerKeyConfig> keys = new List<KeyViewerKeyConfig>();
            List<int> order = new List<int>();
            int skipped = 0;

            for (int i = 0; i < (elements?.Count ?? 0); i++)
            {
                if (elements[i] is not JObject element)
                {
                    skipped++;
                    continue;
                }

                JObject geometry = element["position"] as JObject ?? element;
                JToken slot = names != null && i < names.Count ? names[i] : null;
                if (!TryResolveSlot(slot, out KeyCode code, out List<KeyCode> additional, out KeyViewerKeyMatch match))
                {
                    skipped++;
                    continue;
                }

                KeyViewerKeyConfig key = ReadKey(code, geometry, noteSettings);
                key.KeyMatch = match;
                key.AdditionalKeys.AddRange(additional);
                keys.Add(key);
                order.Add((int)Number(geometry, "zIndex", 0f));
            }

            AddUtilityElements(keys, order, statElements, KeyViewerElementKind.Stat, noteSettings);
            AddUtilityElements(keys, order, graphElements, KeyViewerElementKind.Graph, noteSettings);
            AddUtilityElements(keys, order, knobElements, KeyViewerElementKind.Knob, noteSettings);

            SortByDepth(keys, order);
            ApplyFontPaths(keys, root);

            if (keys.Count == 0)
                return Fail(Interface.HemiLang.Get("KVP_DM_ERROR_NO_MAPPED"));

            if (keys.Count > KeyViewerProfiles.MaximumKeys)
            {
                skipped += keys.Count - KeyViewerProfiles.MaximumKeys;
                keys.RemoveRange(KeyViewerProfiles.MaximumKeys, keys.Count - KeyViewerProfiles.MaximumKeys);
            }

            Vector2 board = Rebase(keys);
            KeyViewerAdvancedConfigData advanced = ReadAdvancedConfig(root, tab, noteSettings);
            KeyViewer.ApplyImportedLayout(keys, board, ReadFade(noteSettings), advanced);

            string message = Interface.HemiLang.Get("KVP_DM_STATUS_IMPORTED", keys.Count);
            if (skipped > 0)
                message += " " + Interface.HemiLang.Get("KVP_DM_STATUS_SKIPPED", skipped);

            MelonLogger.Msg(message);
            return new Result { Success = true, Message = message };
        }

        private static JObject ResolveNoteSettings(JObject root, string tab)
        {
            JObject merged = root["noteSettings"] is JObject global
                ? (JObject)global.DeepClone()
                : new JObject();
            if ((root["tabNoteOverrides"] as JObject)?[tab] is JObject tabOverride)
                merged.Merge(tabOverride, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            return merged;
        }

        private static bool TryResolveSlot(
            JToken slot,
            out KeyCode primary,
            out List<KeyCode> additional,
            out KeyViewerKeyMatch match)
        {
            primary = KeyCode.None;
            additional = new List<KeyCode>();
            match = KeyViewerKeyMatch.Any;

            if (slot is JObject multi && multi["keys"] is JArray members)
            {
                match = string.Equals(multi["match"]?.ToString(), "all", StringComparison.OrdinalIgnoreCase)
                    ? KeyViewerKeyMatch.All
                    : KeyViewerKeyMatch.Any;
                for (int i = 0; i < members.Count; i++)
                {
                    KeyCode code = ResolveKeyCode(members[i]?.ToString());
                    if (code == KeyCode.None || code == primary || additional.Contains(code))
                        continue;
                    if (primary == KeyCode.None) primary = code; else additional.Add(code);
                }
                return primary != KeyCode.None;
            }

            primary = ResolveKeyCode(slot?.ToString());
            return primary != KeyCode.None;
        }

        private static void AddUtilityElements(
            List<KeyViewerKeyConfig> keys,
            List<int> order,
            JArray elements,
            KeyViewerElementKind kind,
            JObject noteSettings)
        {
            if (elements == null)
                return;
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] is not JObject raw)
                    continue;
                JObject p = raw["position"] as JObject ?? raw;
                KeyViewerKeyConfig item = ReadKey(KeyCode.None, p, noteSettings);
                item.Kind = kind;
                item.Stat = ReadStat(Text(raw, "statType", Text(p, "statType", "kps")));
                item.RainingEffect = false;
                item.CounterEnabled = false;
                if (kind == KeyViewerElementKind.Graph)
                {
                    item.GraphType = string.Equals(
                        Text(raw, "graphType", Text(p, "graphType", "line")),
                        "bar",
                        StringComparison.OrdinalIgnoreCase)
                        ? KeyViewerGraphType.Bar
                        : KeyViewerGraphType.Line;
                    item.GraphSpeedSeconds = Mathf.Clamp(
                        Number(raw, "graphSpeed", Number(p, "graphSpeed", 1000f)) / 1000f, 0.5f, 5f);
                    item.GraphColor = ParseColor(Text(raw, "graphColor", Text(p, "graphColor", "#24BBB4")), 1f);
                    item.GraphShowAverage = Flag(raw, "showAvgLine", Flag(p, "showAvgLine", false));
                    item.GraphAnimationEnabled = Flag(raw, "graphAnimationEnabled", Flag(p, "graphAnimationEnabled", true));
                }
                else if (kind == KeyViewerElementKind.Knob)
                {
                    item.KnobAxisId = Text(raw, "axisId", Text(p, "axisId", ""));
                    item.KnobSensitivity = Mathf.Clamp(
                        Number(raw, "sensitivity", Number(p, "sensitivity", 1f)), 0.01f, 100f);
                    item.KnobReverse = Flag(raw, "reverse", Flag(p, "reverse", false));
                }
                keys.Add(item);
                order.Add((int)Number(raw, "zIndex", Number(p, "zIndex", 0f)));
            }
        }

        private static KeyViewerStat ReadStat(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "kpsavg": return KeyViewerStat.KpsAverage;
                case "kpsmax": return KeyViewerStat.KpsMaximum;
                case "total": return KeyViewerStat.Total;
                default: return KeyViewerStat.Kps;
            }
        }

        private static KeyViewerAdvancedConfigData ReadAdvancedConfig(JObject root, string tab, JObject noteSettings)
        {
            JObject grid = root["gridSettings"] as JObject;
            JToken css = root["customCss"] ?? root["customCSS"];
            KeyViewerAdvancedConfigData data = new KeyViewerAdvancedConfigData
            {
                noteEffectMode = root["noteEffect"] == null
                    ? 0
                    : Flag(root, "noteEffect", false) ? 2 : 1,
                keyCounterMode = root["keyCounterEnabled"] == null
                    ? 0
                    : Flag(root, "keyCounterEnabled", false) ? 2 : 1,
                noteFrameLimit = Mathf.RoundToInt(Mathf.Clamp(Number(noteSettings, "frameLimit", 0f), 0f, 240f)),
                delayedNoteEnabled = Flag(noteSettings, "delayedNoteEnabled", false),
                shortNoteThresholdMs = Mathf.Clamp(Number(noteSettings, "shortNoteThresholdMs", 50f), 0f, 2000f),
                keyDisplayDelayMs = Mathf.Clamp(Number(noteSettings, "keyDisplayDelayMs", 0f), 0f, 30000f),
                noteFadeTop = Mathf.Clamp(Number(noteSettings, "fadeTopPx", 0f), 0f, KeyViewer.MaximumNoteFade),
                noteFadeBottom = Mathf.Clamp(Number(noteSettings, "fadeBottomPx", 0f), 0f, KeyViewer.MaximumNoteFade),
                reverseNoteFadeTop = Mathf.Clamp(Number(noteSettings, "reverseFadeTopPx", 0f), 0f, KeyViewer.MaximumNoteFade),
                reverseNoteFadeBottom = Mathf.Clamp(Number(noteSettings, "reverseFadeBottomPx", 0f), 0f, KeyViewer.MaximumNoteFade),
                boardBackgroundColor = root["backgroundColor"]?.ToString(),
                gridAlignmentGuides = Flag(grid, "alignmentGuides", true),
                gridSpacingGuides = Flag(grid, "spacingGuides", true),
                gridSizeMatchGuides = Flag(grid, "sizeMatchGuides", true),
                gridMinimap = Flag(grid, "minimapEnabled", true),
                gridSnapSize = Mathf.Clamp(Number(grid, "gridSnapSize", 5f), 1f, 10f),
                gridOverlayPadding = Mathf.Clamp(Number(grid, "overlayPadding", 30f), 0f, 30f),
                useCustomCss = root["useCustomCss"] != null
                    ? Flag(root, "useCustomCss", false)
                    : Flag(root, "useCustomCSS", false),
                customCss = css is JObject cssObject
                    ? Text(cssObject, "content", "")
                    : css?.ToString() ?? ""
            };
            if ((root["tabCssOverrides"] as JObject)?[tab] is JObject tabCss && Flag(tabCss, "enabled", false))
            {
                string tabContent = Text(tabCss, "content", "");
                if (!string.IsNullOrWhiteSpace(tabContent))
                    data.customCss = (data.customCss ?? "") + "\n" + tabContent;
            }
            if ((root["fontSettings"] as JObject)?["customFonts"] is JArray fonts)
            {
                foreach (JToken token in fonts)
                {
                    if (token is not JObject font || !Flag(font, "enabled", true))
                        continue;
                    if (!string.Equals(Text(font, "type", ""), "web", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string fontCss = Text(font, "cssContent", "");
                    if (!string.IsNullOrWhiteSpace(fontCss))
                        data.customCss = (data.customCss ?? "") + "\n" + fontCss;
                }
            }
            if ((root["layerGroups"] as JObject)?[tab] is JArray groups)
            {
                List<KeyViewerLayerGroup> result = new List<KeyViewerLayerGroup>();
                foreach (JToken token in groups)
                {
                    if (token is not JObject group)
                        continue;
                    result.Add(new KeyViewerLayerGroup
                    {
                        Id = Text(group, "id", ""),
                        Name = Text(group, "name", "")
                    });
                }
                data.layerGroups = result.ToArray();
            }
            return data;
        }

        private static void SortByDepth(List<KeyViewerKeyConfig> keys, List<int> depths)
        {
            int[] index = new int[keys.Count];
            for (int i = 0; i < index.Length; i++)
                index[i] = i;

            Array.Sort(index, (a, b) =>
            {
                int byDepth = depths[a].CompareTo(depths[b]);
                return byDepth != 0 ? byDepth : a.CompareTo(b);
            });

            List<KeyViewerKeyConfig> sorted = new List<KeyViewerKeyConfig>(keys.Count);
            for (int i = 0; i < index.Length; i++)
                sorted.Add(keys[index[i]]);

            keys.Clear();
            keys.AddRange(sorted);
        }

        private static Result Fail(string message)
        {
            MelonLogger.Warning("DM Note import: " + message);
            return new Result { Success = false, Message = message };
        }

        private static int CountElements(JObject table, string tab)
        {
            return table?[tab] is JArray array ? array.Count : 0;
        }

        private static int CountMappedKeys(JObject root, JObject positions, string tab)
        {
            if (positions?[tab] is not JArray elements ||
                (root["keys"] as JObject)?[tab] is not JArray slots)
                return 0;

            int count = 0;
            int limit = Math.Min(elements.Count, slots.Count);
            for (int i = 0; i < limit; i++)
            {
                if (elements[i] is JObject &&
                    TryResolveSlot(slots[i], out _, out _, out _))
                    count++;
            }
            return count;
        }

        private static Vector2 Rebase(List<KeyViewerKeyConfig> keys)
        {
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

                float noteWidth = key.NoteWidth > 0.5f ? key.NoteWidth : key.Size.x;
                float noteLeft = key.ResolveNoteX();
                minX = Mathf.Min(minX, noteLeft);
                maxX = Mathf.Max(maxX, noteLeft + noteWidth);
            }

            Vector2 offset = new Vector2(minX, minY);
            for (int i = 0; i < keys.Count; i++)
                keys[i].Position -= offset;

            return new Vector2(
                Mathf.Max(KeyViewer.MinimumBoardSize, maxX - minX),
                Mathf.Max(KeyViewer.MinimumBoardSize, maxY - minY));
        }

        private static Vector2 ReadFade(JObject noteSettings)
        {
            if (noteSettings == null)
                return Vector2.zero;

            bool reverse = Flag(noteSettings, "reverse", false);
            float near = reverse
                ? Number(noteSettings, "reverseFadeTopPx", 0f)
                : Number(noteSettings, "fadeBottomPx", 0f);
            float far = reverse
                ? Number(noteSettings, "reverseFadeBottomPx", 0f)
                : Number(noteSettings, "fadeTopPx", 0f);

            return new Vector2(
                Mathf.Clamp(near, 0f, KeyViewer.MaximumNoteFade),
                Mathf.Clamp(far, 0f, KeyViewer.MaximumNoteFade));
        }

        private static KeyViewerKeyConfig ReadKey(KeyCode code, JObject p, JObject noteSettings)
        {
            Color background = ParseColor(Text(p, "backgroundColor", "rgba(46, 46, 47, 0.9)"), 0.9f);
            Color activeBackground = ParseColor(Text(p, "activeBackgroundColor", ColorText(background)), background.a);
            Color border = ParseColor(Text(p, "borderColor", "rgba(113, 113, 113, 0.9)"), 0.9f);
            Color activeBorder = ParseColor(Text(p, "activeBorderColor", ColorText(border)), border.a);
            Color text = ParseColor(Text(p, "fontColor", "rgba(121, 121, 121, 0.9)"), 1f);
            Color activeText = ParseColor(Text(p, "activeFontColor", ColorText(text)), text.a);

            KeyViewerGradient backgroundGradient = ReadGradient(p["backgroundGradient"]);
            KeyViewerGradient activeBackgroundGradient = ReadGradient(p["activeBackgroundGradient"]);
            if (p["activeBackgroundColor"] == null && activeBackgroundGradient == null)
                activeBackgroundGradient = KeyViewerGradient.Clone(backgroundGradient);
            KeyViewerGradient borderGradient = ReadGradient(p["borderGradient"]);
            KeyViewerGradient activeBorderGradient = ReadGradient(p["activeBorderGradient"]);
            if (p["activeBorderColor"] == null && activeBorderGradient == null)
                activeBorderGradient = KeyViewerGradient.Clone(borderGradient);

            ReadNoteColors(p, false, out Color noteTop, out Color noteBottom);

            JObject counter = p["counter"] as JObject;
            JObject fill = counter?["fill"] as JObject;
            JObject animation = counter?["animation"] as JObject;

            KeyViewerKeyConfig key = new KeyViewerKeyConfig
            {
                Kind = KeyViewerElementKind.Key,
                Key = code,
                KeyMatch = KeyViewerKeyMatch.Any,
                Position = new Vector2(Number(p, "dx", 0f), Number(p, "dy", 0f)),
                Size = new Vector2(
                    Mathf.Max(1f, Number(p, "width", 60f)),
                    Mathf.Max(1f, Number(p, "height", 60f))),

                BackgroundColor = background,
                ActiveBackgroundColor = activeBackground,
                BorderColor = border,
                ActiveBorderColor = activeBorder,
                TextColor = text,
                ActiveTextColor = activeText,
                BorderWidth = Mathf.Clamp(Number(p, "borderWidth", 3f), 0f, 20f),
                CornerRadius = Mathf.Clamp(Number(p, "borderRadius", 10f), 0f, 100f),
                DisplayText = p["displayText"]?.ToString() ?? "",
                FontSize = Mathf.Clamp(Number(p, "fontSize", 0f), 0f, 200f),
                IdleTransparent = Flag(p, "idleTransparent", false),
                ActiveTransparent = Flag(p, "activeTransparent", false),
                IdleImagePath = p["inactiveImage"]?.ToString() ?? "",
                ActiveImagePath = p["activeImage"]?.ToString() ?? "",
                ImageFit = ReadImageFit(p),
                IdleImageFit = ReadImageFit(p, "idleImageFit"),
                ActiveImageFit = ReadImageFit(p, "activeImageFit"),

                ShadowColor = new Color(0f, 0f, 0f, 0.55f),
                ActiveShadowColor = new Color(0f, 0f, 0f, 0.55f),
                ShadowOffset = new Vector2(0f, 3f),
                ShadowBlur = 8f,
                ActiveShadowOffset = new Vector2(0f, 3f),
                ActiveShadowBlur = 8f,

                ElementId = Text(p, "id", ""),
                Hidden = Flag(p, "hidden", false),
                LayerName = Text(p, "layerName", ""),
                GroupId = Text(p, "groupId", ""),
                CssClass = Text(p, "className", ""),
                UseInlineStyles = Flag(p, "useInlineStyles", false),
                FontFamily = Text(p, "fontFamily", ""),
                FontWeight = Mathf.RoundToInt(Mathf.Clamp(Number(p, "fontWeight", 400f), 100f, 900f)),
                FontItalic = Flag(p, "fontItalic", false),
                FontUnderline = Flag(p, "fontUnderline", false),
                FontStrikethrough = Flag(p, "fontStrikethrough", false),
                BackgroundGradient = backgroundGradient,
                ActiveBackgroundGradient = activeBackgroundGradient,
                BorderGradient = borderGradient,
                ActiveBorderGradient = activeBorderGradient,

                RainingEffect = Flag(p, "noteEffectEnabled", true),
                RainColor = noteTop,
                RainColorBottom = noteBottom,
                RainGradient = IsGradient(p["noteColor"]),
                RainOpacity = noteTop.a,
                RainOpacityBottom = noteBottom.a,
                RainCornerRadius = Mathf.Clamp(Number(p, "noteBorderRadius", 0f), 0f, 100f),
                RainBorderWidth = Mathf.Clamp(Number(p, "noteBorderWidth", 0f), 0f, 20f),
                RainBorderColor = ParseColor(Text(p, "noteBorderColor", "#FFFFFF"), 1f),
                RainBorderOpacity = Mathf.Clamp01(Number(p, "noteBorderOpacity", 100f) / 100f),
                RainBorderSide = ReadBorderSide(Text(p, "noteBorderSide", "all")),
                NoteWidth = Mathf.Max(0f, Number(p, "noteWidth", 0f)),
                NoteAlignment = ReadAlignment(Text(p, "noteAlignment", "center")),
                NoteOffset = new Vector2(
                    Mathf.Clamp(Number(p, "noteOffsetX", 0f), -1000f, 1000f),
                    Mathf.Clamp(Number(p, "noteOffsetY", 0f), -1000f, 1000f)),
                GlowEnabled = Flag(p, "noteGlowEnabled", false),
                GlowSize = Mathf.Clamp(Number(p, "noteGlowSize", 20f), 0f, KeyViewer.MaximumGlowSize),
                GlowGradient = p["noteGlowColor"] == null
                    ? IsGradient(p["noteColor"])
                    : IsGradient(p["noteGlowColor"]),
                NoteAutoYCorrection = Flag(p, "noteAutoYCorrection", true),

                Count = Mathf.Max(0, (int)Number(p, "count", 0f))
            };

            ReadNoteColors(p, true, out Color importedGlowTop, out Color importedGlowBottom);
            key.GlowColor = Opaque(importedGlowTop);
            key.GlowColorBottom = Opaque(importedGlowBottom);
            key.GlowOpacity = importedGlowTop.a;
            key.GlowOpacityBottom = importedGlowBottom.a;
            ReadShadow(p["shadow"] as JObject, false, key);
            ReadShadow(p["activeShadow"] as JObject, true, key);

            key.RainColor = Opaque(key.RainColor);
            key.RainColorBottom = Opaque(key.RainColorBottom);
            key.GlowColor = Opaque(key.GlowColor);
            key.GlowColorBottom = Opaque(key.GlowColorBottom);

            ReadTrack(key, noteSettings);
            ReadCounter(key, counter, fill, animation, text, activeText);
            return key;
        }

        private static void ReadTrack(KeyViewerKeyConfig key, JObject noteSettings)
        {
            key.RainHeight = Mathf.Clamp(
                Number(noteSettings, "trackHeight", KeyViewer.RainingTrackHeight),
                KeyViewer.MinimumRainingTrackHeight,
                KeyViewer.MaximumRainingTrackHeight);
            key.RainSpeed = Mathf.Clamp(
                Number(noteSettings, "speed", KeyViewer.RainingSpeed),
                KeyViewer.MinimumRainingSpeed,
                KeyViewer.MaximumRainingSpeed);
            key.RainReverse = Flag(noteSettings, "reverse", false);
            key.RainMinimumLength = Mathf.Clamp(
                Number(noteSettings, "shortNoteMinLengthPx", 30f),
                KeyViewer.MinimumRainNoteLength,
                KeyViewer.MaximumRainNoteLength);
        }

        private static void ReadCounter(
            KeyViewerKeyConfig key,
            JObject counter,
            JObject fill,
            JObject animation,
            Color idleText,
            Color activeText)
        {
            key.CounterEnabled = counter != null && Flag(counter, "enabled", true);
            key.CounterPlacement = string.Equals(Text(counter, "placement", "inside"), "outside", StringComparison.OrdinalIgnoreCase)
                ? KeyViewerCounterPlacement.Outside
                : KeyViewerCounterPlacement.Inside;
            key.CounterAlign = ReadCounterAlign(Text(counter, "align", "bottom"));
            key.CounterAlignMode = string.Equals(Text(counter, "alignMode", "center"), "between", StringComparison.OrdinalIgnoreCase)
                ? KeyViewerCounterAlignMode.Between
                : KeyViewerCounterAlignMode.Center;
            key.CounterGap = Mathf.Clamp(Number(counter, "gap", 4f), 0f, 200f);
            key.CounterFontSize = Mathf.Clamp(Number(counter, "fontSize", 14f), 6f, 100f);
            key.CounterIdleColor = fill != null
                ? ParseColor(Text(fill, "idle", ColorText(idleText)), idleText.a)
                : idleText;
            key.CounterActiveColor = fill != null
                ? ParseColor(Text(fill, "active", ColorText(activeText)), activeText.a)
                : activeText;
            JObject stroke = counter?["stroke"] as JObject;
            key.CounterIdleStrokeColor = stroke != null
                ? ParseColor(Text(stroke, "idle", "transparent"), 0f)
                : Color.clear;
            key.CounterActiveStrokeColor = stroke != null
                ? ParseColor(Text(stroke, "active", "transparent"), 0f)
                : Color.clear;
            key.CounterIdleGradient = ReadGradient(counter?["fillIdleGradient"]);
            key.CounterActiveGradient = ReadGradient(counter?["fillActiveGradient"]);
            key.CounterFontFamily = Text(counter, "fontFamily", "");
            key.CounterFontWeight = Mathf.RoundToInt(Mathf.Clamp(Number(counter, "fontWeight", 400f), 100f, 900f));
            key.CounterFontItalic = Flag(counter, "fontItalic", false);
            key.CounterFontUnderline = Flag(counter, "fontUnderline", false);
            key.CounterFontStrikethrough = Flag(counter, "fontStrikethrough", false);

            key.CounterAnimationEnabled = animation != null && Flag(animation, "enabled", true);
            key.CounterAnimationScale = Mathf.Clamp(Number(animation, "scale", 1.25f), 1f, 3f);
            key.CounterAnimationSeconds = Mathf.Clamp(Number(animation, "durationMs", 180f) / 1000f, 0.02f, 2f);
            key.CounterAnimationBezier = ReadBezier(animation?["bezier"] as JArray);
        }

        private static void ReadShadow(JObject source, bool active, KeyViewerKeyConfig key)
        {
            if (source == null)
                return;
            bool enabled = Flag(source, "enabled", false);
            Color color = ParseColor(Text(source, "color", "rgba(0,0,0,0.55)"), 0.55f);
            Vector2 offset = new Vector2(
                Mathf.Clamp(Number(source, "offsetX", 0f), -100f, 100f),
                Mathf.Clamp(Number(source, "offsetY", 3f), -100f, 100f));
            float blur = Mathf.Clamp(Number(source, "blur", 8f), 0f, 100f);
            if (active)
            {
                key.ActiveShadowEnabled = enabled;
                key.ActiveShadowColor = color;
                key.ActiveShadowOffset = offset;
                key.ActiveShadowBlur = blur;
            }
            else
            {
                key.ShadowEnabled = enabled;
                key.ShadowColor = color;
                key.ShadowOffset = offset;
                key.ShadowBlur = blur;
            }
        }

        private static Vector4 ReadBezier(JArray values)
        {
            if (values == null || values.Count < 4)
                return new Vector4(0.34f, 1.56f, 0.64f, 1f);
            return new Vector4(
                TokenNumber(values[0], 0.34f),
                TokenNumber(values[1], 1.56f),
                TokenNumber(values[2], 0.64f),
                TokenNumber(values[3], 1f));
        }

        private static KeyViewerGradient ReadGradient(JToken token)
        {
            if (token is not JObject source || source["stops"] is not JArray stops || stops.Count < 2)
                return null;
            KeyViewerGradient gradient = new KeyViewerGradient
            {
                Angle = Number(source, "angle", 90f)
            };
            for (int i = 0; i < stops.Count && i < 8; i++)
            {
                if (stops[i] is not JObject stop)
                    continue;
                gradient.Stops.Add(new KeyViewerGradientStop(
                    ParseColor(Text(stop, "color", "#FFFFFF"), 1f),
                    Mathf.Clamp01(Number(stop, "pos", i / Mathf.Max(1f, stops.Count - 1f)))));
            }
            gradient.Normalize();
            return gradient.Stops.Count >= 2 ? gradient : null;
        }

        private static void ReadNoteColors(JObject p, bool glow, out Color top, out Color bottom)
        {
            string opacityKey = glow ? "noteGlowOpacity" : "noteOpacity";
            float baseOpacity = Number(p, opacityKey, glow ? 70f : 80f);
            float topOpacity = Mathf.Clamp01(Number(p, glow ? "noteGlowOpacityTop" : "noteOpacityTop", baseOpacity) / 100f);
            float bottomOpacity = Mathf.Clamp01(Number(p, glow ? "noteGlowOpacityBottom" : "noteOpacityBottom", baseOpacity) / 100f);

            JToken color = p[glow ? "noteGlowColor" : "noteColor"];
            if (glow && (color == null || color.Type == JTokenType.Null))
                color = p["noteColor"];

            if (color is JObject gradient &&
                string.Equals(gradient["type"]?.ToString(), "gradient", StringComparison.OrdinalIgnoreCase))
            {
                top = ParseColor(gradient["top"]?.ToString(), topOpacity);
                bottom = ParseColor(gradient["bottom"]?.ToString(), bottomOpacity);
                top.a = topOpacity;
                bottom.a = bottomOpacity;
                return;
            }

            string solid = color == null || color.Type == JTokenType.Null ? "#FFFFFF" : color.ToString();
            top = ParseColor(solid, topOpacity);
            bottom = ParseColor(solid, bottomOpacity);
            top.a = topOpacity;
            bottom.a = bottomOpacity;
        }

        private static bool IsGradient(JToken color)
        {
            return color is JObject obj &&
                string.Equals(obj["type"]?.ToString(), "gradient", StringComparison.OrdinalIgnoreCase);
        }

        private static Color Opaque(Color color)
        {
            return new Color(color.r, color.g, color.b, 1f);
        }

        private static string ColorText(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        private static KeyViewerImageFit ReadImageFit(JObject p)
        {
            string fit = p["idleImageFit"]?.ToString();
            if (string.IsNullOrEmpty(fit))
                fit = p["activeImageFit"]?.ToString();
            if (string.IsNullOrEmpty(fit))
                fit = p["imageFit"]?.ToString();

            return ParseImageFit(fit);
        }

        private static KeyViewerImageFit ReadImageFit(JObject p, string field)
        {
            string fit = p?[field]?.ToString();
            return string.IsNullOrWhiteSpace(fit) ? ReadImageFit(p) : ParseImageFit(fit);
        }

        private static KeyViewerImageFit ParseImageFit(string fit)
        {
            switch ((fit ?? "").ToLowerInvariant())
            {
                case "cover": return KeyViewerImageFit.Cover;
                case "fill": return KeyViewerImageFit.Fill;
                case "none": return KeyViewerImageFit.None;
                default: return KeyViewerImageFit.Contain;
            }
        }

        private static void ApplyFontPaths(List<KeyViewerKeyConfig> elements, JObject root)
        {
            if ((root["fontSettings"] as JObject)?["customFonts"] is not JArray fonts)
                return;
            Dictionary<string, string> local = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (JToken token in fonts)
            {
                if (token is not JObject font || !Flag(font, "enabled", true))
                    continue;
                if (!string.Equals(Text(font, "type", ""), "local", StringComparison.OrdinalIgnoreCase))
                    continue;
                string name = Text(font, "name", "");
                string path = Text(font, "localPath", "");
                if (!string.IsNullOrWhiteSpace(name) && HemiAssetSafety.IsFontPath(path) && File.Exists(path))
                    local[name] = path;
            }
            for (int i = 0; i < elements.Count; i++)
            {
                KeyViewerKeyConfig element = elements[i];
                if (!string.IsNullOrWhiteSpace(element.FontFamily)
                    && local.TryGetValue(element.FontFamily, out string fontPath))
                    element.FontFilePath = fontPath;
                if (!string.IsNullOrWhiteSpace(element.CounterFontFamily)
                    && local.TryGetValue(element.CounterFontFamily, out string counterPath))
                    element.CounterFontFilePath = counterPath;
            }
        }

        private static KeyViewerNoteBorderSide ReadBorderSide(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "vertical": return KeyViewerNoteBorderSide.Vertical;
                case "horizontal": return KeyViewerNoteBorderSide.Horizontal;
                default: return KeyViewerNoteBorderSide.All;
            }
        }

        private static KeyViewerNoteAlignment ReadAlignment(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "left": return KeyViewerNoteAlignment.Left;
                case "right": return KeyViewerNoteAlignment.Right;
                default: return KeyViewerNoteAlignment.Center;
            }
        }

        private static KeyViewerCounterAlign ReadCounterAlign(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "top": return KeyViewerCounterAlign.Top;
                case "left": return KeyViewerCounterAlign.Left;
                case "right": return KeyViewerCounterAlign.Right;
                default: return KeyViewerCounterAlign.Bottom;
            }
        }

        private static string Text(JObject source, string key, string fallback)
        {
            JToken token = source?[key];
            return token == null || token.Type == JTokenType.Null ? fallback : token.ToString();
        }

        private static float Number(JObject source, string key, float fallback) => TokenNumber(source?[key], fallback);

        private static float TokenNumber(JToken token, float fallback)
        {
            if (token == null || token.Type == JTokenType.Null)
                return fallback;
            try { return token.ToObject<float>(); }
            catch { return fallback; }
        }

        private static bool Flag(JObject source, string key, bool fallback)
        {
            JToken token = source?[key];
            if (token == null || token.Type == JTokenType.Null)
                return fallback;

            try
            {
                return token.ToObject<bool>();
            }
            catch
            {
                return fallback;
            }
        }

        internal static Color ParseColor(string value, float fallbackAlpha)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Color(1f, 1f, 1f, fallbackAlpha);

            string text = value.Trim();
            try
            {
                if (string.Equals(text, "transparent", StringComparison.OrdinalIgnoreCase))
                    return new Color(0f, 0f, 0f, 0f);

                if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                    return ParseRgb(text, fallbackAlpha);

                string hex = text.TrimStart('#');
                if (hex.Length == 3 || hex.Length == 4)
                {
                    float r = Convert.ToInt32(new string(hex[0], 2), 16) / 255f;
                    float g = Convert.ToInt32(new string(hex[1], 2), 16) / 255f;
                    float b = Convert.ToInt32(new string(hex[2], 2), 16) / 255f;
                    float a = hex.Length == 4 ? Convert.ToInt32(new string(hex[3], 2), 16) / 255f : fallbackAlpha;
                    return new Color(r, g, b, a);
                }

                if (hex.Length == 6 || hex.Length == 8)
                {
                    float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                    float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                    float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                    float a = hex.Length == 8 ? Convert.ToInt32(hex.Substring(6, 2), 16) / 255f : fallbackAlpha;
                    return new Color(r, g, b, a);
                }
            }
            catch
            {
            }

            return new Color(1f, 1f, 1f, fallbackAlpha);
        }

        private static Color ParseRgb(string text, float fallbackAlpha)
        {
            int open = text.IndexOf('(');
            int close = text.IndexOf(')');
            if (open < 0 || close <= open)
                return new Color(1f, 1f, 1f, fallbackAlpha);

            string[] parts = text.Substring(open + 1, close - open - 1)
                .Split(new[] { ',', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return new Color(1f, 1f, 1f, fallbackAlpha);

            return new Color(
                Channel(parts[0], 255f),
                Channel(parts[1], 255f),
                Channel(parts[2], 255f),
                parts.Length >= 4 ? Channel(parts[3], 1f) : fallbackAlpha);
        }

        private static float Channel(string value, float divisor)
        {
            string text = value.Trim();
            if (text.EndsWith("%", StringComparison.Ordinal) &&
                float.TryParse(text.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
            {
                return Mathf.Clamp01(percent / 100f);
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
                ? Mathf.Clamp01(number / divisor)
                : 1f;
        }

        internal static KeyCode ResolveKeyCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return KeyCode.None;

            string trimmed = name.Trim();

            if (trimmed.Length > 1 &&
                int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int virtualKey))
            {
                return FromWindowsVirtualKey(virtualKey);
            }

            string normalized = trimmed.Replace(" ", "").Replace("_", "").Replace("-", "");
            if (normalized.Length == 0)
                return KeyCode.None;

            if (normalized.Length == 1)
            {
                KeyCode direct = FromCharacter(normalized[0]);
                if (direct != KeyCode.None)
                    return direct;
            }

            if (normalized.StartsWith("Key", StringComparison.OrdinalIgnoreCase) && normalized.Length == 4)
                normalized = normalized.Substring(3);
            else if (normalized.StartsWith("Digit", StringComparison.OrdinalIgnoreCase) && normalized.Length == 6)
                normalized = normalized.Substring(5);
            else if (normalized.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) && normalized.Length > 6)
            {
                KeyCode numpad = FromNumpad(normalized.Substring(6).ToUpperInvariant());
                if (numpad != KeyCode.None)
                    return numpad;
            }

            KeyCode alias = FromAlias(normalized.ToUpperInvariant());
            if (alias != KeyCode.None)
                return alias;

            if (normalized.Length == 1)
            {
                KeyCode single = FromCharacter(normalized[0]);
                if (single != KeyCode.None)
                    return single;
            }

            if (!char.IsDigit(normalized[0]) &&
                Enum.TryParse(normalized, true, out KeyCode parsed) &&
                Enum.IsDefined(typeof(KeyCode), parsed) &&
                !KeyViewer.IsMouseKey(parsed))
            {
                return parsed;
            }

            return KeyCode.None;
        }

        private static KeyCode FromWindowsVirtualKey(int key)
        {
            if (key >= 0x30 && key <= 0x39)
                return (KeyCode)((int)KeyCode.Alpha0 + (key - 0x30));
            if (key >= 0x41 && key <= 0x5A)
                return (KeyCode)((int)KeyCode.A + (key - 0x41));
            if (key >= 0x60 && key <= 0x69)
                return (KeyCode)((int)KeyCode.Keypad0 + (key - 0x60));
            if (key >= 0x70 && key <= 0x7E)
                return (KeyCode)((int)KeyCode.F1 + (key - 0x70));

            switch (key)
            {
                case 0x08: return KeyCode.Backspace;
                case 0x09: return KeyCode.Tab;
                case 0x0D: return KeyCode.Return;
                case 0x10:
                case 0xA0: return KeyCode.LeftShift;
                case 0x11:
                case 0xA2: return KeyCode.LeftControl;
                case 0x12:
                case 0xA4: return KeyCode.LeftAlt;
                case 0x13: return KeyCode.Pause;
                case 0x14: return KeyCode.CapsLock;
                case 0x15:
                case 0xA5: return KeyCode.RightAlt;
                case 0x19:
                case 0xA3: return KeyCode.RightControl;
                case 0x1B: return KeyCode.Escape;
                case 0x20: return KeyCode.Space;
                case 0x21: return KeyCode.PageUp;
                case 0x22: return KeyCode.PageDown;
                case 0x23: return KeyCode.End;
                case 0x24: return KeyCode.Home;
                case 0x25: return KeyCode.LeftArrow;
                case 0x26: return KeyCode.UpArrow;
                case 0x27: return KeyCode.RightArrow;
                case 0x28: return KeyCode.DownArrow;
                case 0x2C: return KeyCode.Print;
                case 0x2D: return KeyCode.Insert;
                case 0x2E: return KeyCode.Delete;
                case 0x5B: return KeyCode.LeftWindows;
                case 0x5C: return KeyCode.RightWindows;
                case 0x5D: return KeyCode.Menu;
                case 0x6A: return KeyCode.KeypadMultiply;
                case 0x6B: return KeyCode.KeypadPlus;
                case 0x6D: return KeyCode.KeypadMinus;
                case 0x6E: return KeyCode.KeypadPeriod;
                case 0x6F: return KeyCode.KeypadDivide;
                case 0x90: return KeyCode.Numlock;
                case 0x91: return KeyCode.ScrollLock;
                case 0xA1: return KeyCode.RightShift;
                case 0xBA: return KeyCode.Semicolon;
                case 0xBB: return KeyCode.Equals;
                case 0xBC: return KeyCode.Comma;
                case 0xBD: return KeyCode.Minus;
                case 0xBE: return KeyCode.Period;
                case 0xBF: return KeyCode.Slash;
                case 0xC0: return KeyCode.BackQuote;
                case 0xDB: return KeyCode.LeftBracket;
                case 0xDC: return KeyCode.Backslash;
                case 0xDD: return KeyCode.RightBracket;
                case 0xDE: return KeyCode.Quote;
                default: return KeyCode.None;
            }
        }

        private static KeyCode FromCharacter(char value)
        {
            char upper = char.ToUpperInvariant(value);
            if (upper >= 'A' && upper <= 'Z')
                return (KeyCode)((int)KeyCode.A + (upper - 'A'));
            if (upper >= '0' && upper <= '9')
                return (KeyCode)((int)KeyCode.Alpha0 + (upper - '0'));

            switch (value)
            {
                case '.': return KeyCode.Period;
                case ',': return KeyCode.Comma;
                case '/': return KeyCode.Slash;
                case '\\': return KeyCode.Backslash;
                case ';': return KeyCode.Semicolon;
                case '\'': return KeyCode.Quote;
                case '[': return KeyCode.LeftBracket;
                case ']': return KeyCode.RightBracket;
                case '-': return KeyCode.Minus;
                case '=': return KeyCode.Equals;
                case '`': return KeyCode.BackQuote;
                default: return KeyCode.None;
            }
        }

        private static KeyCode FromNumpad(string suffix)
        {
            switch (suffix)
            {
                case "ENTER":
                case "RETURN": return KeyCode.KeypadEnter;
                case "PLUS":
                case "ADD": return KeyCode.KeypadPlus;
                case "MINUS":
                case "SUBTRACT": return KeyCode.KeypadMinus;
                case "MULTIPLY":
                case "STAR":
                case "ASTERISK": return KeyCode.KeypadMultiply;
                case "DIVIDE":
                case "SLASH": return KeyCode.KeypadDivide;
                case "DECIMAL":
                case "PERIOD":
                case "DOT":
                case "DELETE":
                case "DEL": return KeyCode.KeypadPeriod;
                case "EQUAL":
                case "EQUALS": return KeyCode.KeypadEquals;
                default:
                    return suffix.Length == 1 && suffix[0] >= '0' && suffix[0] <= '9'
                        ? (KeyCode)((int)KeyCode.Keypad0 + (suffix[0] - '0'))
                        : KeyCode.None;
            }
        }

        private static KeyCode FromAlias(string upper)
        {
            switch (upper)
            {
                case "DOT":
                case "PERIOD": return KeyCode.Period;
                case "COMMA": return KeyCode.Comma;
                case "SLASH":
                case "FORWARDSLASH": return KeyCode.Slash;
                case "BACKSLASH": return KeyCode.Backslash;
                case "SEMICOLON": return KeyCode.Semicolon;
                case "QUOTE":
                case "APOSTROPHE": return KeyCode.Quote;
                case "BACKQUOTE":
                case "BACKTICK":
                case "GRAVE":
                case "SECTION": return KeyCode.BackQuote;
                case "DECIMAL": return KeyCode.KeypadPeriod;
                case "APPS": return KeyCode.Menu;
                case "MINUS": return KeyCode.Minus;
                case "PLUS": return KeyCode.Plus;
                case "EQUAL":
                case "EQUALS": return KeyCode.Equals;
                case "SQUAREBRACKETOPEN":
                case "OPENBRACKET":
                case "LEFTBRACKET":
                case "LBRACKET": return KeyCode.LeftBracket;
                case "SQUAREBRACKETCLOSE":
                case "CLOSEBRACKET":
                case "RIGHTBRACKET":
                case "RBRACKET": return KeyCode.RightBracket;
                case "ENTER": return KeyCode.Return;
                case "ESC": return KeyCode.Escape;
                case "SPACE":
                case "SPACEBAR": return KeyCode.Space;
                case "CAPSLOCK": return KeyCode.CapsLock;
                case "INS": return KeyCode.Insert;
                case "DEL": return KeyCode.Delete;
                case "PRINTSCREEN":
                case "PRTSC":
                case "PRTSCR":
                case "SYSREQ": return KeyCode.Print;
                case "CONTEXTMENU": return KeyCode.Menu;
                case "LSHIFT":
                case "LEFTSHIFT":
                case "SHIFTLEFT": return KeyCode.LeftShift;
                case "RSHIFT":
                case "RIGHTSHIFT":
                case "SHIFTRIGHT": return KeyCode.RightShift;
                case "LCTRL":
                case "LCONTROL":
                case "LEFTCTRL":
                case "LEFTCONTROL":
                case "CONTROLLEFT":
                case "CTRL":
                case "CONTROL": return KeyCode.LeftControl;
                case "RCTRL":
                case "RCONTROL":
                case "RIGHTCTRL":
                case "RIGHTCONTROL":
                case "CONTROLRIGHT":
                case "HANJA": return KeyCode.RightControl;
                case "LALT":
                case "LEFTALT":
                case "ALTLEFT":
                case "ALT": return KeyCode.LeftAlt;
                case "RALT":
                case "RIGHTALT":
                case "ALTRIGHT":
                case "ALTGR":
                case "HANGUL": return KeyCode.RightAlt;
                case "UP":
                case "UPARROW":
                case "ARROWUP": return KeyCode.UpArrow;
                case "DOWN":
                case "DOWNARROW":
                case "ARROWDOWN": return KeyCode.DownArrow;
                case "LEFT":
                case "LEFTARROW":
                case "ARROWLEFT": return KeyCode.LeftArrow;
                case "RIGHT":
                case "RIGHTARROW":
                case "ARROWRIGHT": return KeyCode.RightArrow;
                default: return KeyCode.None;
            }
        }
    }
}
