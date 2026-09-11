using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace HemiTweaks.Tuf
{
    internal enum TufSort
    {
        Recent,
        Difficulty,
        Clears,
        Likes
    }

    internal enum TufPackSort
    {
        Recent,
        Name,
        Levels
    }

    internal enum TufListState
    {
        Idle,
        Loading,
        Ready,
        Empty,
        Error
    }

    internal sealed class TufDifficultyFilter
    {
        internal static readonly IReadOnlyList<string> RankedNames = BuildRankedNames();

        internal static readonly IReadOnlyList<string> QuantumNames = new[]
        {
            "Qq",
            "GQ0 (G1~G4)", "GQ1 (G5~G8)", "GQ2 (G9~G12)", "GQ3 (G13~G16)", "GQ4 (G17~G20)",
            "UQ0 (U1~U4)", "UQ1 (U5~U8)", "UQ2 (U9~U12)", "UQ3 (U13~U16)", "UQ4 (U17~U20)"
        };

        internal static readonly IReadOnlyList<string> SpecialNames = new[]
        {
            "Unranked", "Censored", "Impossible"
        };

        private readonly List<string> selected;

        internal TufDifficultyFilter(int minIndex, int maxIndex, IEnumerable<string> selectedNames = null)
        {
            Normalize(RankedNames.Count - 1, ref minIndex, ref maxIndex);

            MinIndex = minIndex;
            MaxIndex = maxIndex;

            HashSet<string> requested = new HashSet<string>(
                selectedNames ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            selected = SpecialNames.Concat(QuantumNames).Where(requested.Contains).ToList();
        }

        internal static TufDifficultyFilter All => new TufDifficultyFilter(0, RankedNames.Count - 1);

        internal int MinIndex { get; }

        internal int MaxIndex { get; }

        internal string MinName => RankedNames[MinIndex];

        internal string MaxName => RankedNames[MaxIndex];

        internal IReadOnlyList<string> SelectedNames => selected;

        internal bool IsSelected(string name)
        {
            return selected.Contains(name);
        }

        internal TufDifficultyFilter WithRange(int minIndex, int maxIndex)
        {
            Normalize(RankedNames.Count - 1, ref minIndex, ref maxIndex);
            if (minIndex == MinIndex && maxIndex == MaxIndex)
                return this;

            return new TufDifficultyFilter(minIndex, maxIndex, selected);
        }

        internal TufDifficultyFilter Toggle(string name)
        {
            if (!SpecialNames.Contains(name) && !QuantumNames.Contains(name))
                return this;

            HashSet<string> next = new HashSet<string>(selected, StringComparer.Ordinal);
            if (!next.Add(name))
                next.Remove(name);
            return new TufDifficultyFilter(MinIndex, MaxIndex, next);
        }

        internal int QuantumMinIndex => QuantumEdge(0, 1, 0);

        internal int QuantumMaxIndex => QuantumEdge(QuantumNames.Count - 1, -1, QuantumNames.Count - 1);

        internal TufDifficultyFilter WithQuantumRange(int minIndex, int maxIndex)
        {
            Normalize(QuantumNames.Count - 1, ref minIndex, ref maxIndex);

            List<string> next = selected.Where(SpecialNames.Contains).ToList();
            for (int i = minIndex; i <= maxIndex; i++)
                next.Add(QuantumNames[i]);
            return new TufDifficultyFilter(MinIndex, MaxIndex, next);
        }

        internal TufDifficultyFilter WithoutQuantum()
        {
            return new TufDifficultyFilter(MinIndex, MaxIndex, selected.Where(SpecialNames.Contains));
        }

        private int QuantumEdge(int from, int step, int fallback)
        {
            for (int i = from; i >= 0 && i < QuantumNames.Count; i += step)
            {
                if (selected.Contains(QuantumNames[i]))
                    return i;
            }
            return fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static void Normalize(int last, ref int minIndex, ref int maxIndex)
        {
            minIndex = Clamp(minIndex, 0, last);
            maxIndex = Clamp(maxIndex, 0, last);
            if (minIndex > maxIndex)
            {
                int swap = minIndex;
                minIndex = maxIndex;
                maxIndex = swap;
            }
        }

        internal static readonly string[] RankedColors =
        {
            "0099FF", "00A2FF", "00A2FF", "00B2FF", "00BBFF", "00C3FF", "00CCFF", "00DDFF", "00E5FF", "00EEFF",
            "00FFFF", "00FFE8", "00FFD0", "00FFB8", "00FFAA", "00FF88", "00FF70", "00FF48", "00FF30", "44FF15",

            "F2A700", "F09E08", "EE9510", "ED8C18", "EB8420", "EA7B28", "E87230", "E66938", "E56040", "E35848",
            "E14F4F", "E04657", "DE3D5F", "DC3467", "DB2C6F", "D92377", "D71A7F", "D61187", "D4088F", "D20097",

            "7B4FB2", "744AA8", "6E469F", "674295", "613E8C", "5A3A83", "543679", "4D3170", "472D67", "40295D",
            "3A2554", "33214A", "34214C", "261838", "20142E", "191025", "130C1C", "0C0812", "060409", "000000"
        };

        internal static readonly string[] QuantumColors =
        {
            "FFFFFF",
            "F1A105", "EB7B29", "E3554A", "C0345E", "D61088",
            "7149A4", "3F2067", "2F2B36", "7E0000", "FFFEFE"
        };

        private static IReadOnlyList<string> BuildRankedNames()
        {
            List<string> values = new List<string>(60);
            foreach (string band in new[] { "P", "G", "U" })
            {
                for (int i = 1; i <= 20; i++)
                    values.Add(band + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return values;
        }
    }

    internal sealed class TufLevel
    {
        internal int Id;
        internal string Song = "";
        internal string Artist = "";
        internal string Creator = "";
        internal string DifficultyName = "";
        internal string DifficultyColor = "";
        internal int Clears;
        internal int Likes;
        internal Uri Download;

        internal string Suffix = "";
    }

    internal sealed class TufPage
    {
        internal readonly List<TufLevel> Levels = new List<TufLevel>();
        internal bool HasMore;
    }

    internal sealed class TufPack
    {
        internal string Name = "";
        internal string Owner = "";
        internal int LevelCount;
        internal int Likes;
    }

    internal sealed class TufPackPage
    {
        internal readonly List<TufPack> Packs = new List<TufPack>();
        internal bool HasMore;
    }

    internal static class TufLibrary
    {
        private static readonly HashSet<int> installed = new HashSet<int>();
        private static string scannedRoot;
        private static DateTime scannedAt = DateTime.MinValue;

        private static readonly TimeSpan RescanAfter = TimeSpan.FromSeconds(5);

        private static bool warnedAboutRoot;

        internal static string Root
        {
            get
            {
                string configured = HemiTweaksMod.TufFolder;
                string candidate = string.IsNullOrWhiteSpace(configured) ? DefaultRoot : configured;
                if (TryNormaliseRoot(candidate, out string normalised, out string error))
                    return normalised;

                if (!warnedAboutRoot)
                {
                    warnedAboutRoot = true;
                    MelonLogger.Warning("The TUF folder setting is unsafe, so the default is being used instead: " + error);
                }
                if (TryNormaliseRoot(DefaultRoot, out normalised, out error))
                    return normalised;
                throw new InvalidDataException(error);
            }
        }

        internal static string DefaultRoot =>
            Path.Combine(MelonEnvironment.UserDataDirectory, BuildInfo.Name, "TUF");

        internal static bool EnsureRoot(out string error)
        {
            error = null;
            try
            {
                Directory.CreateDirectory(Root);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        internal static bool ValidateRoot(string candidate, out string normalised, out string error)
        {
            normalised = "";
            error = null;

            if (string.IsNullOrWhiteSpace(candidate))
                return true;

            if (!TryNormaliseRoot(candidate, out string full, out error))
                return false;

            string probe = Path.Combine(full, ".hemitweaks-write-test-" + Guid.NewGuid().ToString("N"));
            bool created = false;
            try
            {
                Directory.CreateDirectory(full);
                using (FileStream file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    created = true;
            }
            catch (Exception exception)
            {
                error = Interface.HemiLang.Get("TUFB_FOLDER_NOT_WRITABLE", exception.Message);
                return false;
            }
            finally
            {
                if (created)
                {
                    try
                    {
                        File.Delete(probe);
                    }
                    catch (Exception exception)
                    {
                        MelonLogger.Warning("Could not remove the write test file: " + exception.Message);
                    }
                }
            }

            normalised = full;
            return true;
        }

        private static bool TryNormaliseRoot(string candidate, out string normalised, out string error)
        {
            normalised = null;
            error = Interface.HemiLang.Get("TUFB_FOLDER_NOT_FULL_PATH");
            if (!HemiAssetSafety.IsLocalPath(candidate))
                return false;

            try
            {
                string trimmed = candidate.Trim();
                if (!Path.IsPathRooted(trimmed))
                    return false;
                if (Path.DirectorySeparatorChar == '\\'
                    && (trimmed.Length < 3 || !char.IsLetter(trimmed[0]) || trimmed[1] != ':'
                        || (trimmed[2] != '\\' && trimmed[2] != '/')))
                    return false;

                string full = Path.GetFullPath(trimmed);
                string diskRoot = Path.GetPathRoot(full);
                full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(full, diskRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), PathComparison))
                {
                    error = Interface.HemiLang.Get("TUFB_FOLDER_IS_DISK_ROOT");
                    return false;
                }

                Stack<string> ancestors = new Stack<string>();
                string current = full;
                while (!string.IsNullOrEmpty(current))
                {
                    ancestors.Push(current);
                    current = Path.GetDirectoryName(current);
                }
                while (ancestors.Count > 0)
                {
                    current = ancestors.Pop();
                    if ((Directory.Exists(current) || File.Exists(current))
                        && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    {
                        error = Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY");
                        return false;
                    }
                }
                normalised = full;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = Interface.HemiLang.Get("TUFB_FOLDER_UNUSABLE", exception.Message);
                return false;
            }
        }

        private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        internal static HashSet<int> InstalledIds()
        {
            Rescan(false);
            lock (installed)
            {
                return new HashSet<int>(installed);
            }
        }

        private static string FolderOf(string root, int levelId)
        {
            try
            {
                string folder = Path.Combine(root, levelId.ToString(CultureInfo.InvariantCulture));
                return Directory.Exists(folder) ? folder : null;
            }
            catch
            {
                return null;
            }
        }

        internal static string FolderOf(int levelId)
        {
            try
            {
                return FolderOf(Root, levelId);
            }
            catch
            {
                return null;
            }
        }

        internal static void Rescan(bool force)
        {
            string root = Root;
            if (!force
                && string.Equals(scannedRoot, root, StringComparison.OrdinalIgnoreCase)
                && DateTime.UtcNow - scannedAt < RescanAfter)
            {
                return;
            }

            scannedRoot = root;
            scannedAt = DateTime.UtcNow;

            lock (installed)
            {
                installed.Clear();
                try
                {
                    if (Directory.Exists(root))
                        RefreshInstalled(root);
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Could not read the TUF folder: " + exception.Message);
                }
            }
        }

        private static string[] RefreshInstalled(string root)
        {
            string[] directories = Directory.GetDirectories(root);
            installed.Clear();
            foreach (string folder in directories)
            {
                int id = IdFromFolderName(Path.GetFileName(folder));
                if (id > 0)
                    installed.Add(id);
            }
            return directories;
        }

        internal static int IdFromFolderName(string name)
        {
            return int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out int id) ? id : 0;
        }

        internal static string FolderNameFor(TufLevel level)
        {
            return (level == null ? 0 : level.Id).ToString(CultureInfo.InvariantCulture);
        }

        private const string ScratchFolderName = ".hemitweaks-incoming";

        private static readonly Dictionary<string, string> reservations = new Dictionary<string, string>(
            Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        internal static string Reserve(string root, string folderName)
        {
            if (!TryNormaliseRoot(root, out string normalised, out string error))
                throw new InvalidDataException(error);
            EnsureFolderName(folderName);
            string scratch = Path.Combine(normalised, ScratchFolderName);
            if (!HemiAssetSafety.IsWithin(scratch, normalised))
                throw new InvalidDataException(Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY"));
            Directory.CreateDirectory(scratch);

            string staging = Path.Combine(scratch, folderName + "-" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(staging) || File.Exists(staging))
                throw new IOException("The TUF staging path already exists.");
            Directory.CreateDirectory(staging);
            lock (reservations)
            {
                reservations.Add(staging, normalised);
            }
            return staging;
        }

        internal static string Commit(string staging, string folderName)
        {
            EnsureFolderName(folderName);
            string final;
            lock (reservations)
            {
                if (staging == null || !reservations.TryGetValue(staging, out string root)
                    || !OwnsStaging(staging, root, out string normalised))
                    throw new InvalidDataException(Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY"));

                final = Path.Combine(normalised, folderName);
                if (!HemiAssetSafety.IsWithin(final, normalised))
                    throw new InvalidDataException(Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY"));
                if (Directory.Exists(final) || File.Exists(final))
                    throw new IOException("The TUF destination already exists.");
                Directory.Move(staging, final);
                reservations.Remove(staging);
            }

            lock (installed)
            {
                scannedAt = DateTime.MinValue;
            }
            return final;
        }

        internal static void Discard(string staging)
        {
            lock (reservations)
            {
                if (staging == null || !reservations.TryGetValue(staging, out string root))
                    return;
                if (OwnsStaging(staging, root, out string _))
                    DeleteTree(staging);
                else
                    MelonLogger.Warning("Refusing to discard an unsafe TUF staging path.");
                reservations.Remove(staging);
            }
        }

        private static bool OwnsStaging(string staging, string root, out string normalised) =>
            TryNormaliseRoot(root, out normalised, out string _)
            && HemiAssetSafety.IsWithin(staging, Path.Combine(normalised, ScratchFolderName));

        private static void EnsureFolderName(string folderName)
        {
            int id = IdFromFolderName(folderName);
            if (id <= 0 || folderName != id.ToString(CultureInfo.InvariantCulture))
                throw new InvalidDataException(Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY"));
        }

        private static void DeleteTree(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    DeleteOwnedDirectory(path);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not remove " + path + ": " + exception.Message);
            }
        }

        private static void DeleteOwnedDirectory(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY"));
            foreach (string child in Directory.GetFileSystemEntries(path))
            {
                FileAttributes attributes = File.GetAttributes(child);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException(Interface.HemiLang.Get("TUFB_FOLDER_ESCAPES_LIBRARY"));
                if ((attributes & FileAttributes.Directory) != 0)
                    DeleteOwnedDirectory(child);
                else
                    File.Delete(child);
            }
            Directory.Delete(path, false);
        }

        private const string DetailsFileName = ".hemitweaks-tuf.json";

        private sealed class LevelDetails
        {
            public int Id;
            public string Song;
            public string Artist;
            public string Creator;
            public string DifficultyName;
            public string DifficultyColor;
            public int Clears;
            public int Likes;
            public string Suffix;
        }

        private static readonly List<TufLevel> installedLevels = new List<TufLevel>();
        private static DateTime installedListedAt = DateTime.MinValue;
        private static string installedListedRoot;

        internal static void FillMissingDetails(IEnumerable<TufLevel> found)
        {
            if (found == null)
                return;

            string root = null;

            foreach (TufLevel level in found)
            {
                if (level == null || level.Id <= 0)
                    continue;

                if (root == null)
                {
                    try
                    {
                        root = Root;
                    }
                    catch
                    {
                        return;
                    }
                }

                string folder = FolderOf(root, level.Id);
                if (folder == null || File.Exists(Path.Combine(folder, DetailsFileName)))
                    continue;

                WriteDetails(folder, level);
            }
        }

        internal static void WriteDetails(string folder, TufLevel level)
        {
            if (string.IsNullOrEmpty(folder) || level == null)
                return;

            try
            {
                LevelDetails details = new LevelDetails
                {
                    Id = level.Id,
                    Song = level.Song,
                    Artist = level.Artist,
                    Creator = level.Creator,
                    DifficultyName = level.DifficultyName,
                    DifficultyColor = level.DifficultyColor,
                    Clears = level.Clears,
                    Likes = level.Likes,
                    Suffix = level.Suffix
                };

                HemiAtomicFile.WriteAllText(
                    Path.Combine(folder, DetailsFileName),
                    JsonConvert.SerializeObject(details, Formatting.Indented));

                installedListedAt = DateTime.MinValue;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not record what level was downloaded: " + exception.Message);
            }
        }

        internal static List<TufLevel> InstalledLevels()
        {
            string root = Root;

            if (string.Equals(installedListedRoot, root, StringComparison.OrdinalIgnoreCase)
                && DateTime.UtcNow - installedListedAt < RescanAfter)
            {
                return installedLevels;
            }

            installedListedRoot = root;
            installedListedAt = DateTime.UtcNow;
            installedLevels.Clear();

            try
            {
                if (!Directory.Exists(root))
                    return installedLevels;

                string[] directories;

                lock (installed)
                {
                    directories = RefreshInstalled(root);
                    scannedRoot = root;
                    scannedAt = installedListedAt;
                }

                DateTime[] written = new DateTime[directories.Length];
                for (int i = 0; i < directories.Length; i++)
                    written[i] = Directory.GetLastWriteTimeUtc(directories[i]);

                Array.Sort(written, directories, ReverseChronological.Instance);

                foreach (string folder in directories)
                {
                    int id = IdFromFolderName(Path.GetFileName(folder));
                    if (id > 0)
                        installedLevels.Add(ReadDetails(folder, id));
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not list the installed levels: " + exception.Message);
            }

            return installedLevels;
        }

        private sealed class ReverseChronological : IComparer<DateTime>
        {
            internal static readonly ReverseChronological Instance = new ReverseChronological();

            public int Compare(DateTime left, DateTime right)
            {
                return right.CompareTo(left);
            }
        }

        private static TufLevel ReadDetails(string folder, int id)
        {
            try
            {
                string path = Path.Combine(folder, DetailsFileName);
                if (File.Exists(path))
                {
                    LevelDetails details = JsonConvert.DeserializeObject<LevelDetails>(File.ReadAllText(path));
                    if (details != null)
                    {
                        return new TufLevel
                        {
                            Id = id,
                            Song = details.Song ?? "",
                            Artist = details.Artist ?? "",
                            Creator = details.Creator ?? "",
                            DifficultyName = details.DifficultyName ?? "",
                            DifficultyColor = details.DifficultyColor ?? "",
                            Clears = details.Clears,
                            Likes = details.Likes,
                            Suffix = details.Suffix ?? ""
                        };
                    }
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not read the note beside level " + id + ": " + exception.Message);
            }

            return new TufLevel
            {
                Id = id,
                Song = Interface.HemiLang.Get("TUFB_UNTITLED_LEVEL", id.ToString(CultureInfo.InvariantCulture))
            };
        }
    }

    internal enum TufJobState
    {
        Downloading,
        Extracting,
        Done,
        Failed
    }

    internal sealed class TufJob
    {
        internal volatile TufJobState State = TufJobState.Downloading;
        internal volatile string Error;

        internal volatile string Folder;

        internal volatile float Progress = -1f;
    }

    internal static class TufDownloads
    {
        private static readonly string[] AllowedHosts =
        {
            "tuforums.com",
            "api.tuforums.com",
            "cdn.tuforums.com",
            "files.tuforums.com"
        };

        private const long MaxArchiveBytes = 512L * 1024L * 1024L;
        private static readonly TimeSpan BodyIdleTimeout = TimeSpan.FromSeconds(20);

        private static readonly object gate = new object();
        private static readonly Dictionary<int, TufJob> jobs = new Dictionary<int, TufJob>();
        private static CancellationTokenSource running;
        private static TufJob runningJob;
        private static HttpClient http;

        internal static bool IsAllowed(Uri uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
                return false;
            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return false;

            string host = uri.Host;
            for (int i = 0; i < AllowedHosts.Length; i++)
            {
                if (string.Equals(host, AllowedHosts[i], StringComparison.OrdinalIgnoreCase))
                    return true;

                if (host.EndsWith("." + AllowedHosts[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static TufJob Find(int levelId)
        {
            lock (gate)
            {
                return jobs.TryGetValue(levelId, out TufJob job) ? job : null;
            }
        }

        internal static bool IsBusy
        {
            get
            {
                lock (gate)
                {
                    return running != null;
                }
            }
        }

        internal static TufJob Start(TufLevel level)
        {
            if (level == null || level.Download == null || !IsAllowed(level.Download))
                return null;

            lock (gate)
            {
                if (jobs.TryGetValue(level.Id, out TufJob existing)
                    && existing.State != TufJobState.Failed
                    && existing.State != TufJobState.Done)
                {
                    return existing;
                }

                if (running != null)
                    return null;

                TufJob job = new TufJob();
                jobs[level.Id] = job;
                string root;
                try
                {
                    root = TufLibrary.Root;
                }
                catch (Exception exception)
                {
                    job.Error = Describe(exception);
                    job.State = TufJobState.Failed;
                    MelonLogger.Warning("Could not start the TUF download: " + exception.Message);
                    return job;
                }
                CancellationTokenSource owner = new CancellationTokenSource();
                running = owner;
                runningJob = job;

                Uri link = level.Download;
                string name = TufLibrary.FolderNameFor(level);
                TufLevel details = level;
                Task.Run(() => Run(job, link, name, root, details, owner));
                return job;
            }
        }

        internal static void CancelAll()
        {
            lock (gate)
            {
                running?.Cancel();
            }
        }

        internal static void Cancel(TufJob job)
        {
            lock (gate)
            {
                if (ReferenceEquals(runningJob, job))
                    running?.Cancel();
            }
        }

        private static async Task Run(
            TufJob job,
            Uri link,
            string folderName,
            string root,
            TufLevel level,
            CancellationTokenSource owner)
        {
            string archive = null;
            CancellationToken token = owner.Token;
            TufJobState finished = TufJobState.Failed;
            string error = null;
            try
            {
                token.ThrowIfCancellationRequested();
                string candidate = Path.Combine(Path.GetTempPath(), "hemitweaks-tuf-" + Guid.NewGuid().ToString("N") + ".zip");
                using (FileStream file = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    archive = candidate;
                    await Download(link, file, job, token).ConfigureAwait(false);
                }

                token.ThrowIfCancellationRequested();
                job.State = TufJobState.Extracting;

                string staging = TufLibrary.Reserve(root, folderName);
                try
                {
                    TufArchives.Extract(archive, staging, token);
                    lock (gate)
                    {
                        token.ThrowIfCancellationRequested();
                        job.Folder = TufLibrary.Commit(staging, folderName);
                    }

                    TufLibrary.WriteDetails(job.Folder, level);
                }
                catch
                {
                    TufLibrary.Discard(staging);
                    throw;
                }

                finished = TufJobState.Done;
            }
            catch (OperationCanceledException)
            {
                error = Interface.HemiLang.Get("TUFB_DOWNLOAD_CANCELLED");
            }
            catch (Exception exception)
            {
                error = Describe(exception);
                MelonLogger.Warning("TUF download failed: " + exception);
            }
            finally
            {
                TryDelete(archive);
                lock (gate)
                {
                    if (ReferenceEquals(running, owner))
                    {
                        running = null;
                        runningJob = null;
                    }
                    job.Error = error;
                    job.State = finished;
                    owner.Dispose();
                }
            }
        }

        private static async Task Download(Uri link, Stream file, TufJob job, CancellationToken token)
        {
            if (http == null)
            {
                http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
                {
                    Timeout = TimeSpan.FromMinutes(10)
                };
                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "HemiTweaks/" + BuildInfo.Version + " (+ADOFAI mod)");
            }

            using (HttpResponseMessage response = await http
                .GetAsync(link, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false))
            {
                int status = (int)response.StatusCode;

                int hops = 0;
                Uri current = link;
                HttpResponseMessage message = response;
                while (status >= 300 && status < 400 && hops < 4)
                {
                    Uri next = message.Headers.Location;
                    if (next == null)
                        break;
                    if (!next.IsAbsoluteUri)
                        next = new Uri(current, next);
                    if (!IsAllowed(next))
                        throw new InvalidDataException(Interface.HemiLang.Get("TUFB_DOWNLOAD_REDIRECTED_OFF"));

                    current = next;
                    message.Dispose();
                    message = await http.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, token)
                        .ConfigureAwait(false);
                    status = (int)message.StatusCode;
                    hops++;
                }

                using (message)
                {
                    message.EnsureSuccessStatusCode();

                    long? length = message.Content.Headers.ContentLength;
                    if (length > MaxArchiveBytes)
                        throw new InvalidDataException(Interface.HemiLang.Get("TUFB_DOWNLOAD_TOO_LARGE"));

                    using (Stream source = await message.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        byte[] buffer = new byte[65536];
                        long total = 0;
                        while (true)
                        {
                            int read = await HemiDownloadSafety.ReadAsync(source, buffer, BodyIdleTimeout,
                                token, message.Dispose).ConfigureAwait(false);
                            if (read == 0)
                                break;

                            if (read > MaxArchiveBytes - total)
                                throw new InvalidDataException(Interface.HemiLang.Get("TUFB_DOWNLOAD_TOO_LARGE"));

                            token.ThrowIfCancellationRequested();
                            file.Write(buffer, 0, read);
                            total += read;
                            job.Progress = length > 0 ? (float)((double)total / length.Value) : -1f;
                        }
                    }
                }
            }
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static string Describe(Exception exception)
        {
            if (exception is InvalidDataException)
                return exception.Message;
            if (exception is IOException)
                return Interface.HemiLang.Get("TUFB_DOWNLOAD_WRITE_FAILED");
            if (exception is UnauthorizedAccessException)
                return Interface.HemiLang.Get("TUFB_DOWNLOAD_NO_PERMISSION");
            return Interface.HemiLang.Get("TUFB_DOWNLOAD_FAILED");
        }

        internal static void Shutdown()
        {
            CancelAll();
            http?.Dispose();
            http = null;
        }
    }
}
