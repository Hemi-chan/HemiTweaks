using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HemiTweaks
{
    internal enum HemiUpdateState
    {
        Idle,
        Checking,
        UpToDate,
        Available,
        CheckFailed,
        Downloading,
        Installed,
        InstallFailed
    }

    internal readonly struct HemiModVersion : IComparable<HemiModVersion>
    {
        private static readonly Regex BasePattern = new Regex(@"(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled);
        private static readonly Regex BetaPattern = new Regex(@"[Bb]eta[\s_.-]*(\d+)", RegexOptions.Compiled);

        internal readonly int Major;
        internal readonly int Minor;
        internal readonly int Patch;

        internal readonly int Beta;

        private HemiModVersion(int major, int minor, int patch, int beta)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Beta = beta;
        }

        internal bool IsBeta => Beta != int.MaxValue;

        internal string Display => IsBeta
            ? Major + "." + Minor + "." + Patch + " - Beta " + Beta
            : Major + "." + Minor + "." + Patch;

        internal static bool TryParse(string text, HemiModVersion baseFallback, out HemiModVersion version)
        {
            version = default;
            if (string.IsNullOrEmpty(text))
                return false;

            Match baseMatch = BasePattern.Match(text);
            Match betaMatch = BetaPattern.Match(text);
            if (!baseMatch.Success && !betaMatch.Success)
                return false;

            int major = baseFallback.Major, minor = baseFallback.Minor, patch = baseFallback.Patch;
            if (baseMatch.Success)
            {
                major = int.Parse(baseMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                minor = int.Parse(baseMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                patch = int.Parse(baseMatch.Groups[3].Value, CultureInfo.InvariantCulture);
            }

            int beta = betaMatch.Success
                ? int.Parse(betaMatch.Groups[1].Value, CultureInfo.InvariantCulture)
                : int.MaxValue;
            version = new HemiModVersion(major, minor, patch, beta);
            return true;
        }

        internal static HemiModVersion Current
        {
            get
            {
                HemiModVersion one = new HemiModVersion(1, 0, 0, int.MaxValue);
                return TryParse(BuildInfo.Version, one, out HemiModVersion current) ? current : one;
            }
        }

        public int CompareTo(HemiModVersion other)
        {
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Patch != other.Patch) return Patch.CompareTo(other.Patch);
            return Beta.CompareTo(other.Beta);
        }
    }

    internal static class HemiUpdater
    {
        internal const string Repository = "Hemi-chan/HemiTweaks";
        private const string ReleasesApi = "https://api.github.com/repos/" + Repository + "/releases?per_page=10";
        private const string AssetName = "HemiTweaks.dll";
        private const long MaxMetadataBytes = 4L * 1024 * 1024;
        private const long MaxAssetBytes = 64L * 1024 * 1024;
        private const int TimeoutSeconds = 30;
        private const int MaxRedirects = 5;

        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<bool> checkOnStartupEntry;
        private static MelonPreferences_Entry<string> tokenEntry;
        private static HttpClient http;

        private struct Pending
        {
            internal HemiUpdateState State;
            internal string Version, Error, AssetUrl, ApiAssetUrl, Digest;
            internal long Size;
        }

        private static readonly object gate = new object();
        private static bool hasPending;
        private static Pending pending;

        private static bool startupChecked;
        private static bool busy;
        private static string assetUrl;
        private static string apiAssetUrl;
        private static string assetDigest;
        private static long assetSize;

        internal static HemiUpdateState State { get; private set; } = HemiUpdateState.Idle;
        internal static string CurrentVersion => BuildInfo.Version;
        internal static string LatestVersion { get; private set; } = "";
        internal static string Error { get; private set; } = "";
        internal static bool UpdateAvailable => State == HemiUpdateState.Available;
        internal static bool CheckOnStartup => checkOnStartupEntry != null && checkOnStartupEntry.Value;
        internal static string Token => tokenEntry == null ? "" : (tokenEntry.Value ?? "");

        internal static void Initialize(MelonPreferences_Category preferencesCategory)
        {
            category = preferencesCategory;
            checkOnStartupEntry = category.CreateEntry("UpdateCheckOnStartup", true, "Check For Updates On Startup",
                "Looks at the mod's GitHub releases once when the game starts.");
            tokenEntry = category.CreateEntry("UpdateGitHubToken", "", "GitHub Token For Updates",
                "Only needed while the repository is private. Stored as written.", true);
            CleanUpOldBinary();
        }

        internal static void SetCheckOnStartup(bool value) => Store(checkOnStartupEntry, value);

        internal static void SetToken(string value) => Store(tokenEntry, (value ?? "").Trim());

        private static void Store<T>(MelonPreferences_Entry<T> entry, T value)
        {
            if (entry == null || EqualityComparer<T>.Default.Equals(entry.Value, value))
                return;
            entry.Value = value;
            category?.SaveToFile(false);
        }

        internal static void Tick()
        {
            if (!startupChecked)
            {
                startupChecked = true;
                if (CheckOnStartup)
                    Check();
            }

            Pending snapshot;
            lock (gate)
            {
                if (!hasPending)
                    return;
                hasPending = false;
                snapshot = pending;
            }

            if (snapshot.Version != null)
                LatestVersion = snapshot.Version;
            Error = snapshot.Error ?? "";
            if (snapshot.AssetUrl != null)
            {
                assetUrl = snapshot.AssetUrl;
                apiAssetUrl = snapshot.ApiAssetUrl;
                assetDigest = snapshot.Digest;
                assetSize = snapshot.Size;
            }
            busy = snapshot.State == HemiUpdateState.Checking || snapshot.State == HemiUpdateState.Downloading;
            State = snapshot.State;

            if (snapshot.State == HemiUpdateState.Installed)
                RelaunchAndQuit();
        }

        internal static void Check()
        {
            if (busy)
                return;
            busy = true;
            State = HemiUpdateState.Checking;
            Error = "";
            Task.Run(CheckAsync);
        }

        internal static void Install()
        {
            if (busy || State != HemiUpdateState.Available || string.IsNullOrEmpty(assetUrl))
                return;
            busy = true;
            State = HemiUpdateState.Downloading;
            Error = "";
            string url = assetUrl, api = apiAssetUrl, digest = assetDigest;
            long size = assetSize;
            Task.Run(() => InstallAsync(url, api, digest, size));
        }

        internal static void Shutdown()
        {
            http?.Dispose();
            http = null;
        }

        private static async Task CheckAsync()
        {
            try
            {
                string body = await GetString(ReleasesApi, true).ConfigureAwait(false);
                JArray releases = JArray.Parse(body);

                HemiModVersion current = HemiModVersion.Current;
                bool found = false;
                HemiModVersion best = default;
                string bestAsset = null, bestApiAsset = null, bestDigest = null;
                long bestSize = 0;

                foreach (JToken token in releases)
                {
                    if (!(token is JObject release) || release.Value<bool?>("draft") == true)
                        continue;

                    if (!HemiModVersion.TryParse(release.Value<string>("tag_name"), current, out HemiModVersion version)
                        && !HemiModVersion.TryParse(release.Value<string>("name"), current, out version))
                        continue;

                    if (found && version.CompareTo(best) <= 0)
                        continue;

                    string download = null, api = null, digest = null;
                    long size = 0;
                    if (release["assets"] is JArray assets)
                    {
                        foreach (JToken entry in assets)
                        {
                            if (entry is JObject asset
                                && string.Equals(asset.Value<string>("name"), AssetName, StringComparison.OrdinalIgnoreCase))
                            {
                                download = asset.Value<string>("browser_download_url");
                                api = asset.Value<string>("url");
                                digest = asset.Value<string>("digest");
                                size = asset.Value<long?>("size") ?? 0;
                                break;
                            }
                        }
                    }

                    found = true;
                    best = version;
                    bestAsset = download;
                    bestApiAsset = api;
                    bestDigest = digest;
                    bestSize = size;
                }

                if (!found)
                {
                    Park(HemiUpdateState.CheckFailed, null, Interface.HemiLang.Get("UPD_NO_RELEASE"));
                    return;
                }

                if (best.CompareTo(current) <= 0)
                {
                    Park(HemiUpdateState.UpToDate, best.Display, null);
                    return;
                }

                if (string.IsNullOrEmpty(bestAsset))
                {
                    Park(HemiUpdateState.CheckFailed, best.Display, Interface.HemiLang.Get("UPD_NO_ASSET"));
                    return;
                }

                if (!HemiUpdateSafety.HasDigest(bestDigest) || bestSize < 1024 || bestSize > MaxAssetBytes)
                    throw new InvalidDataException("The release has no usable SHA-256 and size metadata.");
                HemiUpdateSafety.ValidateUrl(bestAsset, Repository, true, false);
                if (!string.IsNullOrEmpty(bestApiAsset))
                    HemiUpdateSafety.ValidateUrl(bestApiAsset, Repository, true, true);

                MelonLogger.Msg("Update available: " + CurrentVersion + " -> " + best.Display);
                Park(HemiUpdateState.Available, best.Display, null, bestAsset, bestApiAsset, bestDigest, bestSize);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Update check failed: " + exception.Message);
                Park(HemiUpdateState.CheckFailed, null, Describe(exception));
            }
        }

        private static async Task InstallAsync(string downloadUrl, string apiUrl, string digest, long size)
        {
            try
            {
                bool useApi = !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(apiUrl);
                byte[] bytes = await GetBytes(useApi ? apiUrl : downloadUrl, useApi).ConfigureAwait(false);
                HemiUpdateSafety.ValidatePayload(bytes, size, digest);

                if (bytes.Length < 1024 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z')
                    throw new InvalidDataException("The downloaded file is not a .NET assembly.");

                string installed = Swap(bytes);
                MelonLogger.Msg("Installed " + LatestVersion + " to " + installed + "; restarting through Steam.");
                Park(HemiUpdateState.Installed, null, null);
            }
            catch (Exception exception)
            {
                MelonLogger.Error("Update install failed: " + exception.Message);
                Park(HemiUpdateState.InstallFailed, null, Describe(exception));
            }
        }

        private static string Swap(byte[] bytes)
        {
            string current = CurrentBinaryPath();
            string staged = current + ".update";
            string retired = current + ".old";

            HemiAtomicFile.WriteAllBytes(staged, bytes);
            HemiUpdateSafety.ValidateAssembly(staged, Path.GetFileNameWithoutExtension(AssetName));
            try
            {
                if (File.Exists(retired))
                    File.Delete(retired);
            }
            catch (IOException)
            {
                retired = current + "." + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + ".old";
            }

            if (File.Exists(current))
                File.Move(current, retired);
            try
            {
                File.Move(staged, current);
            }
            catch
            {
                if (!File.Exists(current) && File.Exists(retired))
                    File.Move(retired, current);
                throw;
            }
            return current;
        }

        private static string CurrentBinaryPath()
        {
            string location = null;
            try
            {
                location = typeof(HemiUpdater).Assembly.Location;
            }
            catch (Exception)
            {
            }
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
                return location;
            return Path.Combine(MelonEnvironment.ModsDirectory, AssetName);
        }

        private static void CleanUpOldBinary()
        {
            try
            {
                string current = CurrentBinaryPath();
                string directory = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return;
                foreach (string file in Directory.GetFiles(directory, Path.GetFileName(current) + "*.old"))
                {
                    try { File.Delete(file); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                string staged = current + ".update";
                if (File.Exists(staged))
                    File.Delete(staged);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not tidy old mod binaries: " + exception.Message);
            }
        }

        private static void RelaunchAndQuit()
        {
            try
            {
                ProcessStartInfo info;
                int processId;
                using (Process current = Process.GetCurrentProcess())
                    processId = current.Id;
                RuntimePlatform platform = Application.platform;
                if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
                {
                    info = new ProcessStartInfo("powershell.exe",
                        "-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -Command \""
                        + HemiRelaunch.WindowsScript(processId) + "\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                }
                else
                {
                    bool mac = platform == RuntimePlatform.OSXPlayer || platform == RuntimePlatform.OSXEditor;
                    info = new ProcessStartInfo("/bin/sh", "-c \"" + HemiRelaunch.PosixScript(processId, mac) + "\"")
                    {
                        UseShellExecute = false
                    };
                }
                using (Process helper = Process.Start(info))
                {
                    if (helper == null)
                        throw new InvalidOperationException("The restart helper could not be started.");
                }
            }
            catch (Exception exception)
            {
                Error = exception.Message;
                MelonLogger.Warning("Update installed, but automatic restart could not be scheduled ("
                    + exception.Message + "). Restart the game manually.");
                return;
            }

            Application.Quit();
        }

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
                catch (Exception)
                {
                }

                http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
                {
                    Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
                };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("HemiTweaks/" + BuildInfo.Version + " (+ADOFAI mod updater)");
                return http;
            }
        }

        private static async Task<string> GetString(string url, bool authorize)
        {
            byte[] bytes = await Request(url, authorize, "application/vnd.github+json", MaxMetadataBytes).ConfigureAwait(false);
            return Encoding.UTF8.GetString(bytes);
        }

        private static Task<byte[]> GetBytes(string url, bool authorize)
        {
            return Request(url, authorize, "application/octet-stream", MaxAssetBytes);
        }

        private static async Task<byte[]> Request(string url, bool authorize, string accept, long limit)
        {
            string token = Token;
            for (int hop = 0; hop <= MaxRedirects; hop++)
            {
                Uri validated = HemiUpdateSafety.ValidateUrl(url, Repository, hop == 0, authorize);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, validated))
                {
                    request.Headers.Accept.ParseAdd(accept);
                    if (string.Equals(validated.Host, "api.github.com", StringComparison.OrdinalIgnoreCase))
                        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
                    if (authorize && hop == 0 && !string.IsNullOrEmpty(token))
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    using (HttpResponseMessage response = await Client
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        int status = (int)response.StatusCode;
                        if (status >= 300 && status < 400 && response.Headers.Location != null)
                        {
                            url = response.Headers.Location.IsAbsoluteUri
                                ? response.Headers.Location.ToString()
                                : new Uri(new Uri(url), response.Headers.Location).ToString();
                            continue;
                        }

                        if (status == 404)
                        {
                            bool sent = authorize && hop == 0 && !string.IsNullOrEmpty(token);
                            MelonLogger.Warning("GitHub answered 404 for " + validated.GetLeftPart(UriPartial.Path)
                                + (sent ? " with a " + DescribeToken(token) + " token" : " without a token")
                                + ScopesNote(response));
                            throw new HttpRequestException(Interface.HemiLang.Get(sent ? "UPD_NO_ACCESS" : "UPD_NOT_FOUND"));
                        }
                        if (status == 401 || status == 403)
                        {
                            MelonLogger.Warning("GitHub answered " + status + " for " + validated.GetLeftPart(UriPartial.Path) + ScopesNote(response));
                            throw new HttpRequestException(Interface.HemiLang.Get("UPD_FORBIDDEN"));
                        }
                        response.EnsureSuccessStatusCode();

                        if (response.Content.Headers.ContentLength > limit)
                            throw new InvalidDataException("The reply is larger than " + limit + " bytes.");

                        using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (MemoryStream output = new MemoryStream())
                        {
                            byte[] buffer = new byte[65536];
                            while (true)
                            {
                                int read = await HemiDownloadSafety.ReadAsync(stream, buffer, TimeSpan.FromSeconds(20),
                                    System.Threading.CancellationToken.None, response.Dispose).ConfigureAwait(false);
                                if (read == 0)
                                    return output.ToArray();
                                if (output.Length + read > limit)
                                    throw new InvalidDataException("The reply is larger than " + limit + " bytes.");
                                output.Write(buffer, 0, read);
                            }
                        }
                    }
                }
            }
            throw new HttpRequestException("Too many redirects.");
        }

        private static string DescribeToken(string token)
        {
            if (token.StartsWith("github_pat_", StringComparison.Ordinal))
                return "fine-grained";
            if (token.StartsWith("ghp_", StringComparison.Ordinal))
                return "classic";
            if (token.StartsWith("gho_", StringComparison.Ordinal))
                return "OAuth";
            return "unrecognised (" + token.Length + " chars)";
        }

        private static string ScopesNote(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-OAuth-Scopes", out IEnumerable<string> scopes))
                return " (token scopes: " + string.Join(",", scopes) + ")";
            return "";
        }

        private static void Park(HemiUpdateState state, string version, string error, string asset = null, string apiAsset = null,
            string digest = null, long size = 0)
        {
            lock (gate)
            {
                hasPending = true;
                pending = new Pending
                {
                    State = state, Version = version, Error = error, AssetUrl = asset,
                    ApiAssetUrl = apiAsset, Digest = digest, Size = size
                };
            }
        }

        private static string Describe(Exception exception)
        {
            if (exception is AggregateException aggregate && aggregate.InnerException != null)
                exception = aggregate.InnerException;
            if (exception is TaskCanceledException || exception is TimeoutException)
                return Interface.HemiLang.Get("UPD_TIMEOUT");
            if (exception is InvalidDataException)
                return Interface.HemiLang.Get("UPD_UNSAFE_UPDATE");
            return exception.Message;
        }
    }
}
