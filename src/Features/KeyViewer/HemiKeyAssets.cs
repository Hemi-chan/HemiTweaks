using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HemiTweaks.Interface;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json.Linq;

namespace HemiTweaks
{
    internal static class HemiKeyProfileAssets
    {
        private const string Prefix = "htprofile-asset:";
        private sealed class CachedCopy
        {
            internal long Length;
            internal DateTime Stamp;
            internal string Destination;
        }

        private static readonly Dictionary<string, CachedCopy> Copies = new Dictionary<string, CachedCopy>(StringComparer.Ordinal);

        internal static string Folder(string profilePath)
        {
            string name = Path.GetFileName(profilePath);
            if (Path.DirectorySeparatorChar == '\\')
                name = name.ToUpperInvariant();
            return Path.Combine(Path.GetDirectoryName(profilePath), "Assets", HemiHex.Sha256(Encoding.UTF8.GetBytes(name)).Substring(0, 16));
        }

        internal static JObject Portable(JObject profile)
        {
            JObject result = (JObject)profile.DeepClone();
            List<HemiProfileAsset> assets = new List<HemiProfileAsset>();
            HemiProfileTransfer.AssetCollector collector = new HemiProfileTransfer.AssetCollector(assets, null);
            HemiProfilePolicy.VisitKeys(result, (path, relative) => collector.Collect(path) ?? "");
            RemoveAssets(result);
            result["assets"] = JArray.FromObject(assets);
            return result;
        }

        internal static JObject Read(JObject profile, string destinationPath, bool external)
        {
            JObject result = (JObject)profile.DeepClone();
            List<HemiProfileAsset> assets = null;
            foreach (JProperty property in result.Properties())
            {
                if (!string.Equals(property.Name, "assets", StringComparison.OrdinalIgnoreCase)
                    || property.Value.Type == JTokenType.Null)
                    continue;
                if (assets != null)
                    throw new InvalidDataException("Duplicate key-profile asset blocks.");
                assets = property.Value.ToObject<List<HemiProfileAsset>>();
            }
            string folder = Folder(destinationPath);
            Dictionary<string, string> restored = assets == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : HemiProfileTransfer.RestoreAssets(assets, folder, false);
            int removed = 0;
            HemiProfilePolicy.VisitKeys(result, (path, relative) =>
            {
                if (path.StartsWith(Prefix, StringComparison.Ordinal)
                    && restored.TryGetValue(path.Substring(Prefix.Length), out string materialized))
                    return materialized;
                if (!external && HemiAssetSafety.IsWithin(path, folder))
                    return path;
                removed++;
                return "";
            });
            RemoveAssets(result);
            if (removed > 0)
                MelonLogger.Warning("Key profile ignored " + removed + " external file reference(s); choose those files locally to use them.");
            return result;
        }

        internal static JObject Local(JObject profile, string destinationPath)
        {
            JObject result = (JObject)profile.DeepClone();
            string folder = Folder(destinationPath);
            long total = 0;
            int count = 0;
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HemiProfilePolicy.VisitKeys(result, (path, relative) =>
            {
                if (!HemiAssetSafety.IsLocalPath(path) || !Path.IsPathRooted(path))
                    throw new IOException("A key-profile asset path is not a local absolute path.");
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg"
                    && extension != ".ttf" && extension != ".otf" && extension != ".ttc")
                    throw new IOException("A key-profile asset has an unsupported extension.");
                try
                {
                    string full = Path.GetFullPath(path);
                    string cacheKey = folder + "\n" + full;
                    FileInfo source = new FileInfo(full);
                    if (Copies.TryGetValue(cacheKey, out CachedCopy cached)
                        && HemiAssetSafety.IsWithin(cached.Destination, folder) && File.Exists(cached.Destination)
                        && (!source.Exists || (cached.Length == source.Length && cached.Stamp == source.LastWriteTimeUtc)))
                        return cached.Destination;
                    if (!source.Exists)
                        throw new FileNotFoundException("A selected key-profile asset no longer exists.", full);
                    if (used.Add(full))
                    {
                        total += source.Length;
                        count++;
                    }
                    if (source.Length > HemiProfileTransfer.MaximumAssetBytes
                        || total > HemiProfileTransfer.MaximumTotalAssetBytes || count > HemiProfileTransfer.MaximumAssets)
                        throw new InvalidDataException("Key profile assets exceed their byte or item budget.");
                    if (HemiAssetSafety.IsWithin(full, folder))
                        return full;

                    byte[] bytes = HemiStreamSafety.ReadFile(full, HemiProfileTransfer.MaximumAssetBytes);
                    Directory.CreateDirectory(folder);
                    string destination = Path.Combine(folder, "asset-" + HemiHex.Sha256(bytes).Substring(0, 32) + extension);
                    if (!HemiAssetSafety.IsWithin(destination, folder))
                        throw new InvalidDataException("Unsafe key profile asset directory.");
                    if (!File.Exists(destination))
                    {
                        long stored = 0;
                        foreach (string existing in Directory.GetFiles(folder))
                            stored += new FileInfo(existing).Length;
                        if (bytes.LongLength > HemiProfileTransfer.MaximumTotalAssetBytes - stored)
                            throw new InvalidDataException("The key profile asset directory is full.");
                        HemiAtomicFile.WriteAllBytes(destination, bytes);
                    }
                    if (Copies.Count >= HemiProfileTransfer.MaximumAssets)
                        Copies.Clear();
                    Copies[cacheKey] = new CachedCopy { Length = source.Length, Stamp = source.LastWriteTimeUtc, Destination = destination };
                    return destination;
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Key profile asset was not copied: " + exception.Message);
                    throw new IOException("The existing key profile was preserved because an asset could not be copied.", exception);
                }
            });
            RemoveAssets(result);
            return result;
        }

        private static void RemoveAssets(JObject profile)
        {
            List<JProperty> remove = new List<JProperty>();
            foreach (JProperty property in profile.Properties())
            {
                if (string.Equals(property.Name, "assets", StringComparison.OrdinalIgnoreCase))
                    remove.Add(property);
            }
            foreach (JProperty property in remove)
                property.Remove();
        }
    }

    internal static class HemiDmNoteAssets
    {
        private const string ImagePrefix = "dmnote-local-image://";
        private const int MaximumEncodedCharacters = 64 * 1024 * 1024;

        internal static void Prepare(JObject root, string sourcePath)
        {
            JArray images = root["embeddedLocalImages"] as JArray;
            JArray fonts = root["embeddedLocalFonts"] as JArray;
            if ((long)(images?.Count ?? 0) + (fonts?.Count ?? 0) > HemiProfileTransfer.MaximumAssets)
                throw new InvalidDataException("The DM Note preset contains too many embedded assets.");

            string name = HemiFileNames.Sanitize(Path.GetFileNameWithoutExtension(sourcePath), "DmNote", maximumLength: 48);
            string folder = Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, "KeyViewerAssets", name);
            long total = 0;
            Dictionary<string, string> restoredImages = Restore(images, "imageId", folder, "image", ref total);
            Dictionary<string, string> restoredFonts = Restore(fonts, "fontId", folder, "font", ref total);
            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
            int rejected = 0;

            foreach (string key in new[] { "keyPositions", "positions", "statPositions", "graphPositions", "knobPositions" })
            {
                if (!(root[key] is JObject table))
                    continue;
                foreach (JProperty tab in table.Properties())
                {
                    if (!(tab.Value is JArray elements))
                        continue;
                    foreach (JToken token in elements)
                    {
                        if (!(token is JObject raw))
                            continue;
                        JObject position = raw["position"] as JObject ?? raw;
                        foreach (string field in new[] { "activeImage", "inactiveImage" })
                        {
                            string original = position[field]?.ToString() ?? "";
                            string resolved = ResolveImage(original, restoredImages, sourceDirectory);
                            if (original.Length > 0 && resolved.Length == 0)
                                rejected++;
                            position[field] = resolved;
                        }
                    }
                }
            }

            if ((root["fontSettings"] as JObject)?["customFonts"] is JArray customFonts)
            {
                foreach (JToken token in customFonts)
                {
                    if (!(token is JObject font))
                        continue;
                    string id = font["id"]?.ToString() ?? "";
                    string original = font["localPath"]?.ToString() ?? "";
                    if (restoredFonts.TryGetValue(id, out string restored))
                    {
                        font["localPath"] = restored;
                        font["enabled"] = true;
                    }
                    else
                    {
                        string resolved = ResolveStoreAsset(original, sourceDirectory, "fonts");
                        if (!HemiAssetSafety.IsFontPath(resolved))
                            resolved = "";
                        font["localPath"] = resolved;
                        if (original.Length > 0 && resolved.Length == 0)
                            rejected++;
                    }
                }
            }
            if (rejected > 0)
                MelonLogger.Warning("DM Note ignored " + rejected + " external file reference(s); embed assets or use the selected store's images/fonts folders.");
        }

        private static string ResolveImage(string path, Dictionary<string, string> images, string sourceDirectory)
        {
            if (path.StartsWith(ImagePrefix, StringComparison.OrdinalIgnoreCase))
                return images.TryGetValue(path.Substring(ImagePrefix.Length), out string restored) ? restored : "";
            string resolved = ResolveStoreAsset(path, sourceDirectory, "images");
            string extension = Path.GetExtension(resolved).ToLowerInvariant();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" ? resolved : "";
        }

        private static string ResolveStoreAsset(string path, string sourceDirectory, string kind)
        {
            if (!HemiAssetSafety.IsLocalPath(path))
                return "";
            try
            {
                string allowed = Path.Combine(sourceDirectory, kind);
                string candidate = Path.IsPathRooted(path) ? path
                    : HemiAssetSafety.IsSafeRelativePath(path) ? Path.Combine(sourceDirectory, path) : "";
                if (HemiAssetSafety.IsWithin(candidate, allowed))
                    return Path.GetFullPath(candidate);
                return HemiAssetSafety.ImportedLocalPath(path, allowed);
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static Dictionary<string, string> Restore(JArray entries, string idField, string folder, string kind, ref long total)
        {
            Dictionary<string, string> restored = new Dictionary<string, string>(StringComparer.Ordinal);
            if (entries == null)
                return restored;
            foreach (JToken token in entries)
            {
                if (!(token is JObject entry))
                    continue;
                string id = entry[idField]?.ToString() ?? "";
                string encoded = entry["dataBase64"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(id) || encoded.Length == 0 || encoded.Length > MaximumEncodedCharacters
                    || restored.ContainsKey(id))
                    continue;
                string extension = (entry["extension"]?.ToString() ?? (kind == "font" ? "ttf" : "png"))
                    .Trim().TrimStart('.').ToLowerInvariant();
                bool allowed = kind == "font" ? extension == "ttf" || extension == "otf" || extension == "ttc"
                    : extension == "png" || extension == "jpg" || extension == "jpeg";
                if (!allowed)
                    continue;
                try
                {
                    byte[] bytes = HemiStreamSafety.DecodeBase64(encoded,
                        Math.Min(HemiProfileTransfer.MaximumAssetBytes, HemiProfileTransfer.MaximumTotalAssetBytes - total));
                    if (bytes.Length == 0)
                        continue;
                    string hash = HemiHex.Sha256(bytes);
                    string safeId = HemiFileNames.Sanitize(id, kind, maximumLength: 40);
                    string path = Path.Combine(folder, kind + "-" + safeId + "-" + hash.Substring(0, 16) + "." + extension);
                    if (!HemiAssetSafety.IsWithin(path, folder))
                        throw new InvalidDataException("Unsafe DM Note asset folder.");
                    Directory.CreateDirectory(folder);
                    if (!File.Exists(path))
                        HemiAtomicFile.WriteAllBytes(path, bytes);
                    restored[id] = path;
                    total += bytes.LongLength;
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("DM Note skipped an invalid embedded asset: " + exception.Message);
                }
            }
            return restored;
        }
    }
}
