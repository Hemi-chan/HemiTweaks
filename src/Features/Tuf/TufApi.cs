using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HemiTweaks.Tuf
{
    internal static class TufApi
    {
        private const string Origin = "https://api.tuforums.com/";

        internal const int PageSize = 50;

        private const float MinimumGapSeconds = 0.35f;

        private const int MaxResponseBytes = 2 * 1024 * 1024;

        private const int TimeoutSeconds = 20;

        private const int CacheEntries = 64;

        private const int MaxQueryLength = 120;

        private static readonly object gate = new object();
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly List<string> cacheOrder = new List<string>();

        private static HttpClient http;
        private static CancellationTokenSource inFlight;
        private static DateTime lastRequestUtc = DateTime.MinValue;

        private static TufPage pendingLevels;
        private static TufPackPage pendingPacks;
        private static string pendingError;
        private static long pendingToken;
        private static long requestToken;

        internal static bool IsBusy { get; private set; }


        private static HttpClient Client
        {
            get
            {
                if (http != null)
                    return http;

                try
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                }
                catch
                {
                }

                http = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false
                })
                {
                    BaseAddress = new Uri(Origin),
                    Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
                };

                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "HemiTweaks/" + BuildInfo.Version + " (+ADOFAI mod)");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                return http;
            }
        }

        internal static long RequestLevels(string query, TufSort sort, bool ascending, int offset, TufDifficultyFilter filter)
        {
            string path = BuildLevelPath(query, sort, ascending, offset, filter);
            return Begin(path, true);
        }

        internal static long RequestPacks(string query, TufPackSort sort, bool ascending, int offset)
        {
            string path = BuildPackPath(query, sort, ascending, offset);
            return Begin(path, false);
        }

        private static long Begin(string path, bool levels)
        {
            long token = Interlocked.Increment(ref requestToken);

            lock (gate)
            {
                pendingLevels = null;
                pendingPacks = null;
                pendingError = null;

                if (cache.TryGetValue(path, out string cached))
                {
                    Deliver(cached, levels, token);
                    return token;
                }

                inFlight?.Cancel();
                inFlight?.Dispose();
                inFlight = new CancellationTokenSource();
                IsBusy = true;
            }

            CancellationToken cancellation = inFlight.Token;
            Task.Run(() => Fetch(path, levels, token, cancellation));
            return token;
        }

        private static async Task Fetch(string path, bool levels, long token, CancellationToken cancellation)
        {
            try
            {
                await Throttle(cancellation).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();

                string body = await Get(path, cancellation).ConfigureAwait(false);

                lock (gate)
                {
                    Remember(path, body);
                    if (token == Interlocked.Read(ref requestToken))
                        Deliver(body, levels, token);
                    IsBusy = false;
                }
            }
            catch (OperationCanceledException)
            {
                lock (gate)
                {
                    if (token == Interlocked.Read(ref requestToken))
                        IsBusy = false;
                }
            }
            catch (Exception exception)
            {
                lock (gate)
                {
                    if (token == Interlocked.Read(ref requestToken))
                    {
                        pendingError = Describe(exception);
                        pendingToken = token;
                        IsBusy = false;
                    }
                }
            }
        }

        private static async Task Throttle(CancellationToken cancellation)
        {
            TimeSpan wait;
            lock (gate)
            {
                TimeSpan since = DateTime.UtcNow - lastRequestUtc;
                wait = TimeSpan.FromSeconds(MinimumGapSeconds) - since;
                if (wait <= TimeSpan.Zero)
                    lastRequestUtc = DateTime.UtcNow;
            }

            if (wait <= TimeSpan.Zero)
                return;

            await Task.Delay(wait, cancellation).ConfigureAwait(false);
            lock (gate)
            {
                lastRequestUtc = DateTime.UtcNow;
            }
        }

        private static async Task<string> Get(string path, CancellationToken cancellation)
        {
            MelonLogger.Msg("TUF request: " + path);

            using (HttpResponseMessage response = await Client
                .GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellation)
                .ConfigureAwait(false))
            {
                int status = (int)response.StatusCode;
                MelonLogger.Msg("TUF answered " + status.ToString(CultureInfo.InvariantCulture) + " for " + path);

                if (status >= 300 && status < 400)
                    throw new HttpRequestException(Interface.HemiLang.Get("TUFB_API_REDIRECTED"));

                if (status == 429)
                    throw new HttpRequestException(Interface.HemiLang.Get("TUFB_API_RATE_LIMITED"));

                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength > MaxResponseBytes)
                    throw new InvalidDataException(Interface.HemiLang.Get("TUFB_API_REPLY_TOO_LARGE"));

                using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return await ReadBounded(stream, cancellation).ConfigureAwait(false);
                }
            }
        }

        private static async Task<string> ReadBounded(Stream stream, CancellationToken cancellation)
        {
            using (MemoryStream output = new MemoryStream())
            {
                byte[] buffer = new byte[32768];
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false);
                    if (read == 0)
                        return Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);

                    if (output.Length + read > MaxResponseBytes)
                        throw new InvalidDataException(Interface.HemiLang.Get("TUFB_API_REPLY_TOO_LARGE"));
                    output.Write(buffer, 0, read);
                }
            }
        }

        private static void Deliver(string body, bool levels, long token)
        {
            try
            {
                if (levels)
                    pendingLevels = ParseLevels(body);
                else
                    pendingPacks = ParsePacks(body);
                pendingToken = token;
            }
            catch (Exception exception)
            {
                pendingError = Describe(exception);
                pendingToken = token;
            }
        }

        private static bool TryTake<T>(ref T pending, out T page, out string error, out long token)
            where T : class
        {
            lock (gate)
            {
                page = pending;
                error = pendingError;
                token = pendingToken;
                if (page == null && error == null)
                    return false;

                pending = null;
                pendingError = null;
                return true;
            }
        }

        internal static bool TryTakeLevels(out TufPage page, out string error, out long token)
            => TryTake(ref pendingLevels, out page, out error, out token);

        internal static bool TryTakePacks(out TufPackPage page, out string error, out long token)
            => TryTake(ref pendingPacks, out page, out error, out token);

        private static void Remember(string path, string body)
        {
            if (cache.ContainsKey(path))
                return;

            cache[path] = body;
            cacheOrder.Add(path);
            while (cacheOrder.Count > CacheEntries)
            {
                cache.Remove(cacheOrder[0]);
                cacheOrder.RemoveAt(0);
            }
        }

        internal static void ClearCache()
        {
            lock (gate)
            {
                cache.Clear();
                cacheOrder.Clear();
            }
        }

        internal static void Shutdown()
        {
            lock (gate)
            {
                inFlight?.Cancel();
                inFlight?.Dispose();
                inFlight = null;
                http?.Dispose();
                http = null;
                cache.Clear();
                cacheOrder.Clear();
            }
        }

        internal static string BuildLevelPath(string query, TufSort sort, bool ascending, int offset, TufDifficultyFilter filter)
        {
            filter = filter ?? TufDifficultyFilter.All;

            string order;
            switch (sort)
            {
                case TufSort.Difficulty: order = "DIFF"; break;
                case TufSort.Clears: order = "CLEARS"; break;
                case TufSort.Likes: order = "LIKES"; break;
                default: order = "RECENT"; break;
            }

            StringBuilder path = new StringBuilder("v2/database/levels?limit=")
                .Append(PageSize)
                .Append("&offset=").Append(Math.Max(0, offset))
                .Append("&query=").Append(Uri.EscapeDataString(Cap(query, "", MaxQueryLength)))
                .Append("&pguRange=").Append(Uri.EscapeDataString(filter.MinName))
                .Append(',').Append(Uri.EscapeDataString(filter.MaxName))
                .Append("&sort=").Append(order).Append('_').Append(ascending ? "ASC" : "DESC")
                .Append("&deletedFilter=hide");

            if (filter.SelectedNames.Count > 0)
            {
                path.Append("&specialDifficulties=")
                    .Append(Uri.EscapeDataString(string.Join(",", filter.SelectedNames)));
            }

            return path.ToString();
        }

        internal static string BuildPackPath(string query, TufPackSort sort, bool ascending, int offset)
        {
            string order;
            switch (sort)
            {
                case TufPackSort.Name: order = "NAME"; break;
                case TufPackSort.Levels: order = "LEVELS"; break;
                default: order = "RECENT"; break;
            }

            return "v2/database/levels/packs?limit=" + PageSize
                + "&offset=" + Math.Max(0, offset)
                + "&query=" + Uri.EscapeDataString(Cap(query, "", MaxQueryLength))
                + "&sort=" + order
                + "&order=" + (ascending ? "ASC" : "DESC");
        }

        private static TufPage ParseLevels(string body)
        {
            JObject root = JObject.Parse(body);
            TufPage page = new TufPage { HasMore = root.Value<bool?>("hasMore") == true };

            if (!(root["results"] is JArray results))
                return page;

            int limit = Math.Min(results.Count, PageSize);
            for (int i = 0; i < limit; i++)
            {
                TufLevel level = ReadLevel(results[i]);
                if (level != null)
                    page.Levels.Add(level);
            }
            return page;
        }

        private static TufLevel ReadLevel(JToken token)
        {
            int id = token.Value<int?>("id") ?? 0;
            if (id <= 0)
                return null;

            JToken difficulty = token["difficulty"];
            Uri.TryCreate(token.Value<string>("dlLink"), UriKind.Absolute, out Uri download);

            return new TufLevel
            {
                Id = id,
                Song = Cap(token.Value<string>("song"), "Unknown song", 120),
                Artist = Cap(token.Value<string>("artist"), "Unknown artist", 120),
                Creator = Cap(ReadCreator(token), "Unknown creator", 160),
                DifficultyName = Cap(difficulty?.Value<string>("name"), "Unranked", 40),
                DifficultyColor = Cap(difficulty?.Value<string>("color"), "", 16),
                Clears = Math.Max(0, token.Value<int?>("clears") ?? 0),
                Likes = Math.Max(0, token.Value<int?>("likes") ?? 0),
                Download = TufDownloads.IsAllowed(download) ? download : null,
                Suffix = Cap(token.Value<string>("suffix"), "", 24)
            };
        }

        private static string ReadCreator(JToken token)
        {
            string direct = token.Value<string>("creator");
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            if (token["levelCredits"] is JArray credits)
            {
                List<string> names = new List<string>();
                foreach (JToken credit in credits)
                {
                    string name = credit.Value<string>("creatorName")
                        ?? credit["creator"]?.Value<string>("name");
                    if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                        names.Add(name);
                    if (names.Count >= 3)
                        break;
                }
                if (names.Count > 0)
                    return string.Join(" & ", names);
            }

            return token.Value<string>("charter");
        }

        private static TufPackPage ParsePacks(string body)
        {
            JObject root = JObject.Parse(body);
            TufPackPage page = new TufPackPage { HasMore = root.Value<bool?>("hasMore") == true };

            JArray results = root["results"] as JArray ?? root["packs"] as JArray;
            if (results == null)
                return page;

            int limit = Math.Min(results.Count, PageSize);
            for (int i = 0; i < limit; i++)
            {
                JToken token = results[i];
                string id = token.Value<string>("id") ?? token.Value<int?>("id")?.ToString();
                if (string.IsNullOrEmpty(id))
                    continue;

                page.Packs.Add(new TufPack
                {
                    Name = Cap(token.Value<string>("name"), Interface.HemiLang.Get("TUFB_UNTITLED_PACK"), 120),
                    Owner = Cap(token.Value<string>("ownerName") ?? token["owner"]?.Value<string>("name"), "", 80),
                    LevelCount = Math.Max(0, token.Value<int?>("levelCount") ?? token.Value<int?>("levels") ?? 0),
                    Likes = Math.Max(0, token.Value<int?>("likes") ?? 0)
                });
            }
            return page;
        }

        private static string Cap(string value, string fallback, int length)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            value = value.Trim();
            return value.Length > length ? value.Substring(0, length) : value;
        }

        private static string Describe(Exception exception)
        {
            if (exception is InvalidDataException)
                return exception.Message;

            if (exception is HttpRequestException)
            {
                return string.IsNullOrEmpty(exception.Message)
                    ? Interface.HemiLang.Get("TUF_ERROR_UNREACHABLE")
                    : exception.Message;
            }

            if (exception is TaskCanceledException)
                return Interface.HemiLang.Get("TUFB_API_TIMEOUT");

            MelonLogger.Warning("TUF request failed: " + exception);
            return Interface.HemiLang.Get("TUF_ERROR_UNREACHABLE");
        }
    }

    internal static class TufOfficial
    {
        private static bool jumping;

        internal static string WorldKeyOf(TufLevel level)
        {
            if (level == null || string.IsNullOrEmpty(level.Suffix))
                return null;

            string code = level.Suffix.Trim().Trim('(', ')').Trim();
            if (code.Length == 0)
                return null;

            int dash = code.IndexOf('-');
            if (dash <= 0)
                return null;

            string world = code.Substring(0, dash);
            try
            {
                return ADOBase.worldData != null && ADOBase.worldData.ContainsKey(world) ? world : null;
            }
            catch
            {
                return null;
            }
        }

        internal static string BlockedReason(string world)
        {
            try
            {
                if (jumping)
                    return Interface.HemiLang.Get("TUFB_OFFICIAL_ALREADY_LOADING");

                if (ADOBase.controller == null)
                    return Interface.HemiLang.Get("TUFB_NOT_AVAILABLE_HERE");

                if (GCS.FOOL_JOKER)
                    return Interface.HemiLang.Get("TUFB_OFFICIAL_APRIL_FOOLS");

                if (ADOBase.isLevelEditor)
                    return Interface.HemiLang.Get("TUFB_OFFICIAL_LEAVE_EDITOR");

                if (!ADOBase.worldData.TryGetValue(world, out WorldData data))
                    return Interface.HemiLang.Get("TUFB_OFFICIAL_UNKNOWN_LEVEL");
                if (data.doNotBuild || data.notRealWorld)
                    return Interface.HemiLang.Get("TUFB_OFFICIAL_NOT_PLAYABLE");

                string dlc = DlcProblem(world);
                if (dlc != null)
                    return dlc;

                if (!IsUnlocked(world))
                    return Interface.HemiLang.Get("TUFB_OFFICIAL_LOCKED");

                return null;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("TUF official check failed: " + exception.Message);
                return Interface.HemiLang.Get("TUFB_NOT_AVAILABLE_HERE");
            }
        }

        private static bool IsUnlocked(string world)
        {
            int stage = Persistence.GetOverallProgressStage();

            if (stage < 3 && world == "6")
                return false;

            bool lateMain = world == "7" || world == "8" || world == "9"
                || world == "10" || world == "11" || world == "12" || world == "B";
            if (stage < 5 && lateMain)
                return false;

            return true;
        }

        private static string DlcProblem(string world)
        {
            try
            {
                if (DLCManager.DLCManagers == null)
                    return null;

                foreach (DLCManager dlc in DLCManager.DLCManagers)
                {
                    if (dlc == null || !dlc.IsDLCLevel(world))
                        continue;
                    if (!dlc.own)
                        return Interface.HemiLang.Get("TUFB_DLC_NOT_OWNED");
                    if (!dlc.installed)
                        return Interface.HemiLang.Get("TUFB_DLC_NOT_INSTALLED");
                    if (!dlc.upToDate)
                        return Interface.HemiLang.Get("TUFB_DLC_OUT_OF_DATE");
                }
            }
            catch
            {
            }
            return null;
        }

        internal static bool Open(string world, out string reason)
        {
            reason = BlockedReason(world);
            if (reason != null)
                return false;

            try
            {
                GCS.customLevelId = null;
                GCS.loadCustomFromBundle = false;
                GCS.useNoFail = false;
                GCS.useUnlockKeyLimiter = false;
                GCS.checkpointNum = 0;

                jumping = true;
                ADOBase.controller.EnterWorld(world);
                return true;
            }
            catch (Exception exception)
            {
                jumping = false;
                MelonLogger.Warning("Could not open official level: " + exception);
                reason = Interface.HemiLang.Get("TUFB_OPEN_FAILED");
                return false;
            }
        }

        internal static void NotifySceneChanged()
        {
            jumping = false;
        }
    }

    internal static class TufLauncher
    {
        internal static List<string> ChartsIn(string folder)
        {
            List<string> charts = new List<string>();
            try
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    return charts;

                charts.AddRange(folder.GetFilesWithExtension(GCS.levelTextExtensions[0], SearchOption.AllDirectories));
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not read the level folder: " + exception.Message);
                return charts;
            }

            charts.Sort(CompareCharts);
            return charts;
        }

        private static int CompareCharts(string left, string right)
        {
            bool leftMain = IsMain(left);
            bool rightMain = IsMain(right);
            if (leftMain != rightMain)
                return leftMain ? -1 : 1;

            return string.Compare(
                Path.GetFileName(left), Path.GetFileName(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMain(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            return string.Equals(name, "main", StringComparison.OrdinalIgnoreCase);
        }

        internal static string BlockedReason()
        {
            try
            {
                if (ADOBase.editor != null)
                {
                    return ADOBase.editor.isLoading
                        ? Interface.HemiLang.Get("TUFB_EDITOR_LOADING")
                        : null;
                }

                if (ADOBase.controller == null)
                    return Interface.HemiLang.Get("TUFB_NOT_AVAILABLE_HERE");

                return null;
            }
            catch
            {
                return Interface.HemiLang.Get("TUFB_NOT_AVAILABLE_HERE");
            }
        }

        internal static bool Open(string chartPath, out string reason)
        {
            reason = BlockedReason();
            if (reason != null)
                return false;

            if (string.IsNullOrEmpty(chartPath) || !File.Exists(chartPath))
            {
                reason = Interface.HemiLang.Get("TUFB_CHART_MISSING");
                return false;
            }

            try
            {
                scnEditor editor = ADOBase.editor;
                return editor != null
                    ? OpenInEditor(editor, chartPath)
                    : OpenByLoadingEditor(chartPath);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not open the level in the editor: " + exception);
                reason = Interface.HemiLang.Get("TUFB_OPEN_FAILED");
                return false;
            }
        }

        private static bool OpenInEditor(scnEditor editor, string chartPath)
        {
            MelonLogger.Msg("Opening in the editor already running: " + chartPath);
            editor.CheckUnsavedChanges(delegate
            {
                editor.OpenLevel(chartPath);
            });
            return true;
        }

        private static bool OpenByLoadingEditor(string chartPath)
        {
            GCS.customLevelPaths = null;
            GCS.customLevelIndex = 0;
            GCS.useNoFail = false;
            GCS.useUnlockKeyLimiter = false;
            GCS.practiceMode = false;
            GCS.checkpointNum = 0;

            MelonLogger.Msg("Loading the editor for: " + chartPath);
            scnEditor.levelToOpenOnLoad = chartPath;
            ADOBase.controller.GoToLevelEditor();
            return true;
        }
    }

    internal static class SevenZipRuntime
    {
        private const string ResourcePrefix = "HemiTweaks.Resources.SevenZip.";

        private static bool unpacked;
        private static string executable;

        internal static string Executable
        {
            get
            {
                if (unpacked)
                    return executable;

                unpacked = true;
                executable = Unpack();
                return executable;
            }
        }

        private static string Unpack()
        {
            string platform = PlatformFolder();
            if (platform == null)
            {
                MelonLogger.Msg("No 7-Zip is bundled for this platform.");
                return null;
            }

            try
            {
                string directory = Path.Combine(
                    MelonEnvironment.UserDataDirectory, BuildInfo.Name, "Cache", "7zip", platform);
                Directory.CreateDirectory(directory);

                Assembly assembly = typeof(SevenZipRuntime).Assembly;
                string[] names = FileNames(platform);
                string first = null;

                foreach (string name in names)
                {
                    string path = Path.Combine(directory, name);
                    if (!WriteIfMissing(assembly, platform, name, path))
                        return null;

                    if (first == null)
                        first = path;
                }

                MakeExecutable(first);
                MelonLogger.Msg("Bundled 7-Zip ready: " + first);
                return first;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not unpack the bundled 7-Zip: " + exception.Message);
                return null;
            }
        }

        private static bool WriteIfMissing(Assembly assembly, string platform, string name, string path)
        {
            string resource = ResourcePrefix + platform + "." + name + ".gz";

            using (Stream source = assembly.GetManifestResourceStream(resource))
            {
                if (source == null)
                {
                    MelonLogger.Warning("The bundled 7-Zip is missing a file: " + resource);
                    return false;
                }

                byte[] contents;
                using (GZipStream unzip = new GZipStream(source, CompressionMode.Decompress))
                using (MemoryStream memory = new MemoryStream())
                {
                    unzip.CopyTo(memory);
                    contents = memory.ToArray();
                }

                if (File.Exists(path) && new FileInfo(path).Length == contents.Length)
                    return true;

                File.WriteAllBytes(path, contents);
                return true;
            }
        }

        private static string PlatformFolder()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "win64";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "osx";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "linux64";
                default:
                    return null;
            }
        }

        private static string[] FileNames(string platform)
        {
            return platform == "win64"
                ? new[] { "7z.exe", "7z.dll" }
                : new[] { "7zz" };
        }

        private static void MakeExecutable(string path)
        {
            if (Path.DirectorySeparatorChar == '\\')
                return;

            try
            {
                ProcessStartInfo start = new ProcessStartInfo("/bin/chmod")
                {
                    Arguments = "+x \"" + path + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(start))
                {
                    if (process == null || !process.WaitForExit(10000) || process.ExitCode != 0)
                        MelonLogger.Warning("Could not mark the bundled 7-Zip executable: " + path);
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not run chmod on the bundled 7-Zip: " + exception.Message);
            }
        }
    }
}
