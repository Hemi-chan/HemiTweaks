using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HemiTweaks.Interface
{
    internal sealed class HemiLanguage
    {
        internal string Code = "";

        internal string Name = "";

        internal readonly Dictionary<string, string> Strings =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal static class HemiLang
    {
        private const string ResourcePrefix = "HemiTweaks.Resources.Lang.";
        private const string FolderName = "Lang";

        private const string NativeNameKey = "0NATIVELANG";

        internal const string FallbackCode = "EN";

        private static readonly Dictionary<string, HemiLanguage> languages =
            new Dictionary<string, HemiLanguage>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<string> order = new List<string>();

        private static HemiLanguage current;
        private static HemiLanguage fallback;
        private static bool loaded;

        internal static IReadOnlyList<string> Codes
        {
            get
            {
                Load();
                return order;
            }
        }


        internal static string NameOf(string code)
        {
            Load();
            return languages.TryGetValue(code ?? "", out HemiLanguage language) ? language.Name : code ?? "";
        }

        internal static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            Load();

            if (current != null && current.Strings.TryGetValue(key, out string text))
                return text;

            if (fallback != null && fallback.Strings.TryGetValue(key, out string english))
                return english;

            return key;
        }

        internal static string Get(string key, params object[] arguments)
        {
            string text = Get(key);
            if (arguments == null || arguments.Length == 0)
                return text;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, text, arguments);
            }
            catch (FormatException)
            {
                MelonLogger.Warning("The string '" + key + "' does not match the values given to it.");
                return text;
            }
        }

        internal static void Reload()
        {
            loaded = false;
            languages.Clear();
            order.Clear();
            current = null;
            fallback = null;
            Load();
        }

        internal static void Use(string code)
        {
            Load();
            if (!string.IsNullOrEmpty(code) && languages.TryGetValue(code, out HemiLanguage language))
                current = language;
        }

        private static void Load()
        {
            if (loaded)
                return;

            loaded = true;

            LoadEmbedded();
            LoadFolder();

            languages.TryGetValue(FallbackCode, out fallback);

            order.Clear();
            if (languages.ContainsKey(FallbackCode))
                order.Add(FallbackCode);

            List<string> rest = new List<string>();
            foreach (KeyValuePair<string, HemiLanguage> pair in languages)
            {
                if (!string.Equals(pair.Key, FallbackCode, StringComparison.OrdinalIgnoreCase))
                    rest.Add(pair.Value.Code);
            }
            rest.Sort(StringComparer.OrdinalIgnoreCase);
            order.AddRange(rest);

            Use(HemiTweaksMod.LanguageCode);
            if (current == null)
                current = fallback;
        }

        private static void LoadEmbedded()
        {
            Assembly assembly = typeof(HemiLang).Assembly;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!TryEmbeddedLanguage(name, out _, out string code))
                    continue;

                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(name))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Merge(code, reader.ReadToEnd());
                    }
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Could not read the built-in language " + code + ": " + exception.Message);
                }
            }
        }

        private static void LoadFolder()
        {
            string folder = Folder;

            try
            {
                Directory.CreateDirectory(folder);
                ExtractMissingDefaults(folder);

                foreach (string path in Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        Merge(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
                    }
                    catch (Exception exception)
                    {
                        MelonLogger.Warning("Could not read the language file "
                            + Path.GetFileName(path) + ": " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not read the language folder: " + exception.Message);
            }
        }

        private static void ExtractMissingDefaults(string folder)
        {
            Assembly assembly = typeof(HemiLang).Assembly;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!TryEmbeddedLanguage(name, out string fileName, out _))
                    continue;

                string path = Path.Combine(folder, fileName);

                try
                {
                    if (File.Exists(path))
                        continue;

                    using (Stream stream = assembly.GetManifestResourceStream(name))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        HemiAtomicFile.WriteAllText(path, reader.ReadToEnd());
                    }

                    MelonLogger.Msg("Wrote the language file " + fileName + " to " + folder);
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Could not write " + fileName + ": " + exception.Message);
                }
            }
        }

        private static bool TryEmbeddedLanguage(string name, out string fileName, out string code)
        {
            fileName = name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(ResourcePrefix.Length) : null;
            code = fileName?.Substring(0, fileName.Length - ".json".Length);
            return fileName != null;
        }

        internal static string Folder =>
            Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, FolderName);

        private static void Merge(string code, string json)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(json))
                return;

            JObject root = JsonConvert.DeserializeObject<JObject>(json);
            if (root == null)
                return;

            if (!languages.TryGetValue(code, out HemiLanguage language))
            {
                language = new HemiLanguage { Code = code.ToUpperInvariant() };
                languages[language.Code] = language;
            }

            foreach (KeyValuePair<string, JToken> pair in root)
            {
                if (pair.Value != null && pair.Value.Type == JTokenType.String)
                    language.Strings[pair.Key] = pair.Value.Value<string>();
            }

            if (language.Strings.TryGetValue(NativeNameKey, out string native) && !string.IsNullOrWhiteSpace(native))
                language.Name = native;

            if (string.IsNullOrEmpty(language.Name))
                language.Name = language.Code;
        }
    }
}
