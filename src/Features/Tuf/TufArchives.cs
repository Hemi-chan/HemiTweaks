using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;

namespace HemiTweaks.Tuf
{
    internal static class TufArchives
    {
        private const long MaxExtractedBytes = 1024L * 1024L * 1024L;

        private const int MaxEntries = 4000;
        private static readonly string[] ShellMetadataExtensions =
        {
            ".lnk", ".url", ".scf", ".library-ms", ".website"
        };

        private static readonly TimeSpan SevenZipTimeout = TimeSpan.FromMinutes(10);

        private static bool searchedForSevenZip;
        private static string sevenZipPath;

        internal static void Extract(string archive, string destination)
        {
            Extract(archive, destination, CancellationToken.None);
        }

        internal static void Extract(string archive, string destination, CancellationToken token)
        {
            Directory.CreateDirectory(destination);
            token.ThrowIfCancellationRequested();

            string tool = FindSevenZip();
            if (tool != null && ExtractWithSevenZip(tool, archive, destination, token))
                return;

            token.ThrowIfCancellationRequested();
            ExtractManaged(archive, destination, token);
        }

        private static string FindSevenZip()
        {
            if (searchedForSevenZip)
                return sevenZipPath;

            searchedForSevenZip = true;

            sevenZipPath = SevenZipRuntime.Executable;
            if (sevenZipPath != null)
                return sevenZipPath;

            foreach (string candidate in SevenZipCandidates())
            {
                try
                {
                    if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                    {
                        sevenZipPath = candidate;
                        MelonLogger.Msg("Using 7-Zip to unpack downloads: " + candidate);
                        return sevenZipPath;
                    }
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning("Could not check for 7-Zip at " + candidate + ": " + exception.Message);
                }
            }

            MelonLogger.Msg("7-Zip was not found; downloads will be unpacked with the built-in zip reader.");
            return null;
        }

        private static IEnumerable<string> SevenZipCandidates()
        {
            string[] programFiles =
            {
                Environment.GetEnvironmentVariable("ProgramW6432"),
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)")
            };

            foreach (string root in programFiles)
            {
                if (!string.IsNullOrEmpty(root))
                    yield return Path.Combine(root, "7-Zip", "7z.exe");
            }

            foreach (string name in new[] { "7z", "7zz", "7za" })
            {
                foreach (string found in OnPath(name))
                    yield return found;
            }

            yield return "/opt/homebrew/bin/7zz";
            yield return "/opt/homebrew/bin/7z";
            yield return "/usr/local/bin/7zz";
            yield return "/usr/local/bin/7z";
            yield return "/usr/bin/7z";
            yield return "/usr/bin/7za";
        }

        private static IEnumerable<string> OnPath(string name)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                yield break;

            string[] suffixes = IsWindows
                ? new[] { ".exe" }
                : new[] { "" };

            foreach (string directory in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                foreach (string suffix in suffixes)
                {
                    string candidate = null;
                    try
                    {
                        candidate = Path.Combine(directory.Trim(), name + suffix);
                    }
                    catch
                    {
                    }

                    if (candidate != null)
                        yield return candidate;
                }
            }
        }

        private static bool IsWindows => Path.DirectorySeparatorChar == '\\';

        private static bool ExtractWithSevenZip(
            string tool,
            string archive,
            string destination,
            CancellationToken token)
        {
            if (tool.IndexOf('"') >= 0 || archive.IndexOf('"') >= 0 || destination.IndexOf('"') >= 0)
            {
                MelonLogger.Warning("7-Zip paths containing a quote are not supported; using the built-in reader.");
                return false;
            }

            try
            {
                SevenZipResult listing = RunSevenZip(
                    tool, "l -slt -ba -- \"" + archive + "\"", token);
                if (!UsableResult(listing, "list"))
                    return false;
                ValidateSevenZipListing(listing.Output, destination);

                SevenZipResult extraction = RunSevenZip(
                    tool,
                    "x -y -bso0 -bsp0 -o\"" + destination + "\" -- \"" + archive + "\"",
                    token);
                if (!UsableResult(extraction, "unpack"))
                {
                    if (!ResetDestination(destination))
                        throw new IOException("The failed 7-Zip staging folder could not be reset.");
                    return false;
                }

                CheckUnpackedSize(destination);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not run 7-Zip: " + exception.Message);
                if (!ResetDestination(destination))
                {
                    throw new IOException(
                        "The failed 7-Zip staging folder could not be reset.", exception);
                }
                return false;
            }
        }

        private sealed class SevenZipResult
        {
            internal int ExitCode;
            internal bool TimedOut;
            internal string Output = "";
            internal string Error = "";
        }

        private static SevenZipResult RunSevenZip(
            string tool,
            string arguments,
            CancellationToken token)
        {
            ProcessStartInfo start = new ProcessStartInfo(tool)
            {
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(start))
            {
                if (process == null)
                    return null;

                Task<string> error = process.StandardError.ReadToEndAsync();
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Stopwatch timer = Stopwatch.StartNew();

                while (!process.WaitForExit(250))
                {
                    if (token.IsCancellationRequested)
                    {
                        TryKill(process);
                        process.WaitForExit(5000);
                        throw new OperationCanceledException(token);
                    }

                    if (timer.Elapsed >= SevenZipTimeout)
                    {
                        TryKill(process);
                        process.WaitForExit(5000);
                        return new SevenZipResult { TimedOut = true };
                    }
                }

                return new SevenZipResult
                {
                    ExitCode = process.ExitCode,
                    Output = output.GetAwaiter().GetResult(),
                    Error = error.GetAwaiter().GetResult()
                };
            }
        }

        private static bool UsableResult(SevenZipResult result, string operation)
        {
            if (result == null)
                return false;
            if (result.TimedOut)
            {
                MelonLogger.Warning("7-Zip took too long while trying to " + operation
                    + " an archive; falling back to the built-in reader.");
                return false;
            }
            if (result.ExitCode == 0)
                return true;

            MelonLogger.Warning("7-Zip could not " + operation + " that archive (exit "
                + result.ExitCode.ToString(CultureInfo.InvariantCulture) + "): "
                + result.Error.Trim());
            return false;
        }

        private static void ValidateSevenZipListing(string listing, string destination)
        {
            int count = 0;
            long total = 0;
            using (StringReader reader = new StringReader(listing ?? ""))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (Field(line, "Path = ", out string entry))
                    {
                        count++;
                        if (count > MaxEntries)
                            throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_MANY_FILES"));
                        EnsureSafeEntryPath(entry, destination);
                    }
                    else if (Field(line, "Size = ", out string sizeText))
                    {
                        if (!long.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out long size)
                            || size > MaxExtractedBytes - total)
                            throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_LARGE"));
                        total += size;
                    }
                    else if (Field(line, "Symbolic Link = ", out string link)
                        || Field(line, "Hard Link = ", out link))
                    {
                        if (!string.IsNullOrWhiteSpace(link))
                            throw Escapes();
                    }
                    else if (Field(line, "Attributes = ", out string attributes) && IsLinkAttribute(attributes))
                        throw Escapes();
                    else if (Field(line, "Mode = ", out string mode) && IsLinkMode(mode))
                        throw Escapes();
                }
            }

            if (count == 0)
                throw new InvalidDataException("7-Zip returned an empty archive listing.");
        }

        private static bool Field(string line, string prefix, out string value)
        {
            value = line.StartsWith(prefix, StringComparison.Ordinal) ? line.Substring(prefix.Length) : null;
            return value != null;
        }

        private static InvalidDataException Escapes()
            => new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_ESCAPES_FOLDER"));

        private static InvalidDataException EscapesFrom(Exception inner)
            => new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_ESCAPES_FOLDER"), inner);

        private static bool IsLinkAttribute(string attributes)
        {
            foreach (string value in (attributes ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (value.StartsWith("l", StringComparison.OrdinalIgnoreCase)
                    || value.IndexOf("reparse", StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("l", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool IsLinkMode(string mode)
        {
            if (IsLinkAttribute(mode))
                return true;
            string value = (mode ?? "").Trim();
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int hexadecimal)
                    && (hexadecimal & 0xF000) == 0xA000;
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                && (number & 0xF000) == 0xA000)
                return true;
            try
            {
                return value.Length > 0 && (Convert.ToInt32(value, 8) & 0xF000) == 0xA000;
            }
            catch (Exception exception) when (exception is FormatException || exception is OverflowException || exception is ArgumentException)
            {
                return false;
            }
        }

        private static string EnsureSafeEntryPath(string entry, string destination)
        {
            if (string.IsNullOrEmpty(entry))
                throw Escapes();

            string portable = entry.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (portable[0] == Path.DirectorySeparatorChar || portable.IndexOf(':') >= 0)
                throw Escapes();

            foreach (string segment in portable.Split(Path.DirectorySeparatorChar))
            {
                if (segment == ".")
                    continue;
                if (segment == ".."
                    || segment.EndsWith(".", StringComparison.Ordinal)
                    || segment.EndsWith(" ", StringComparison.Ordinal)
                    || HemiFileNames.IsReserved(segment))
                    throw Escapes();
                foreach (char character in segment)
                {
                    if (char.IsControl(character) || HemiFileNames.IsInvalid(character))
                        throw Escapes();
                }
            }

            string fileName = portable.Substring(portable.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            if (IsShellMetadata(fileName))
                throw new InvalidDataException("The archive contains unsupported shell metadata.");

            string root = RootWithSeparator(destination);
            string target;
            try
            {
                target = Path.GetFullPath(Path.Combine(destination, portable));
            }
            catch (Exception exception)
            {
                throw EscapesFrom(exception);
            }

            if (!target.StartsWith(root, PathComparison) || !HemiAssetSafety.IsWithin(target, destination))
                throw Escapes();
            return target;
        }

        private static bool IsShellMetadata(string name)
        {
            if (string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "autorun.inf", StringComparison.OrdinalIgnoreCase))
                return true;
            string extension = Path.GetExtension(name);
            foreach (string blocked in ShellMetadataExtensions)
            {
                if (string.Equals(extension, blocked, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void TryKill(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not stop 7-Zip: " + exception.Message);
            }
        }

        private static void CheckUnpackedSize(string destination)
        {
            long total = 0;
            int count = 0;
            string root = RootWithSeparator(destination);
            Stack<string> pending = new Stack<string>();
            pending.Push(Path.GetFullPath(destination));

            while (pending.Count > 0)
            {
                foreach (string path in Directory.GetFileSystemEntries(pending.Pop()))
                {
                    string full = Path.GetFullPath(path);
                    if (!full.StartsWith(root, PathComparison))
                        throw Escapes();

                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        throw Escapes();

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        pending.Push(path);
                        continue;
                    }

                    count++;
                    if (count > MaxEntries)
                        throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_MANY_FILES"));

                    total += new FileInfo(path).Length;
                    if (total > MaxExtractedBytes)
                        throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_LARGE"));
                }
            }
        }

        private static StringComparison PathComparison =>
            IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        private static string RootWithSeparator(string destination)
        {
            string root = Path.GetFullPath(destination);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                root += Path.DirectorySeparatorChar;
            return root;
        }

        private static bool ResetDestination(string destination)
        {
            try
            {
                if (Directory.Exists(destination))
                    Directory.Delete(destination, true);
                Directory.CreateDirectory(destination);
                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not reset the failed 7-Zip staging folder: "
                    + exception.Message);
                return false;
            }
        }

        private static void ExtractManaged(string archive, string destination, CancellationToken token)
        {
            long written = 0;
            HashSet<string> createdFiles = new HashSet<string>(
                IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            using (FileStream file = File.OpenRead(archive))
            using (ZipArchive zip = new ZipArchive(file, ZipArchiveMode.Read, false, EntryNameBytes))
            {
                if (zip.Entries.Count > MaxEntries)
                    throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_MANY_FILES"));

                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    string target = EnsureSafeEntryPath(DecodeEntryName(entry.FullName), destination);
                    int attributes = entry.ExternalAttributes;
                    if (((attributes >> 16) & 0xF000) == 0xA000
                        || (attributes & (int)FileAttributes.ReparsePoint) != 0)
                        throw Escapes();

                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    if (entry.Length < 0 || entry.Length > MaxExtractedBytes - written)
                        throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_LARGE"));

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (Stream source = entry.Open())
                    using (FileStream output = new FileStream(target, createdFiles.Contains(target) ? FileMode.Create : FileMode.CreateNew,
                        FileAccess.Write, FileShare.None))
                    {
                        written += CopyManagedEntry(source, output, Math.Min(entry.Length, MaxExtractedBytes - written), token);
                        createdFiles.Add(target);
                    }
                }
            }
        }

        private static long CopyManagedEntry(Stream source, Stream output, long maximumBytes, CancellationToken token)
        {
            byte[] buffer = new byte[65536];
            long written = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, maximumBytes - written + 1));
                if (read == 0)
                    return written;
                if (read > maximumBytes - written)
                    throw new InvalidDataException(Interface.HemiLang.Get("TUFB_ARCHIVE_TOO_LARGE"));
                token.ThrowIfCancellationRequested();
                output.Write(buffer, 0, read);
                written += read;
            }
        }

        private static Encoding entryNameBytes;
        private static bool triedEntryNameBytes;
        private static Encoding legacyEntryNames;
        private static bool triedLegacyEntryNames;

        private static Encoding EntryNameBytes => Lookup(28591, ref entryNameBytes, ref triedEntryNameBytes);

        private static Encoding LegacyEntryNames =>
            Lookup(LegacyCodePage(), ref legacyEntryNames, ref triedLegacyEntryNames);

        private static int LegacyCodePage()
        {
            try
            {
                int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
                return codePage == 65001 ? 0 : codePage;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not read the system codepage: " + exception.Message);
                return 0;
            }
        }

        private static Encoding Lookup(int codePage, ref Encoding cached, ref bool tried)
        {
            if (tried)
                return cached;

            tried = true;
            if (codePage <= 0)
                return null;

            try
            {
                cached = Encoding.GetEncoding(codePage);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Codepage " + codePage.ToString(CultureInfo.InvariantCulture)
                    + " is not available here, so archive names written in it cannot be read: "
                    + exception.Message);
            }

            return cached;
        }

        private static string DecodeEntryName(string name)
        {
            Encoding bytes = EntryNameBytes;
            if (bytes == null || string.IsNullOrEmpty(name))
                return name;

            bool preserved = true;
            bool plainAscii = true;
            foreach (char character in name)
            {
                if (character > 0xFF)
                {
                    preserved = false;
                    break;
                }
                if (character > 0x7F)
                    plainAscii = false;
            }

            if (!preserved || plainAscii)
                return name;

            byte[] raw = bytes.GetBytes(name);

            try
            {
                return new UTF8Encoding(false, true).GetString(raw);
            }
            catch (DecoderFallbackException)
            {
            }

            Encoding legacy = LegacyEntryNames;
            return legacy == null ? name : legacy.GetString(raw);
        }
    }
}
