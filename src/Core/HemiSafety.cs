using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HemiTweaks
{
    internal static class HemiAssetSafety
    {
        internal const long MaximumFontBytes = 64L * 1024 * 1024;
        internal const int MaximumFontCacheEntries = 128;

        internal static bool IsLocalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            string value = path.Trim();
            if ((value.Length >= 2 && (value[0] == '/' || value[0] == '\\')
                    && (value[1] == '/' || value[1] == '\\'))
                || value.StartsWith("\\??\\", StringComparison.Ordinal)
                || value.StartsWith("\\Device\\", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("\\GLOBAL??\\", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("\\DosDevices\\", StringComparison.OrdinalIgnoreCase))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    return false;
                if (value[i] == ':' && !(i == 1 && char.IsLetter(value[0])
                    && value.Length > 2 && (value[2] == '\\' || value[2] == '/')
                    && Path.DirectorySeparatorChar == '\\'))
                    return false;
            }
            return true;
        }

        internal static bool IsSafeRelativePath(string path)
        {
            if (!IsLocalPath(path) || Path.IsPathRooted(path)
                || path[0] == '/' || path[0] == '\\' || path.IndexOf(':') >= 0)
                return false;
            foreach (string segment in path.Replace('\\', '/').Split('/'))
            {
                if (segment == ".." || segment == ".")
                    return false;
            }
            return true;
        }

        internal static bool IsWithin(string path, string directory)
        {
            if (!IsLocalPath(path) || !IsLocalPath(directory))
                return false;
            try
            {
                string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (Directory.Exists(directory)
                    && (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    return false;
                string full = Path.GetFullPath(path);
                StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                    ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!full.StartsWith(root, comparison))
                    return false;
                string current = full;
                while (current.Length >= root.Length)
                {
                    if ((File.Exists(current) || Directory.Exists(current))
                        && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        return false;
                    current = Path.GetDirectoryName(current);
                    if (string.IsNullOrEmpty(current))
                        break;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static string ImportedLocalPath(string path, string sourceDirectory)
        {
            if (!IsLocalPath(path))
                return "";
            try
            {
                string full = Path.IsPathRooted(path) ? Path.GetFullPath(path)
                    : IsSafeRelativePath(path) ? Path.GetFullPath(Path.Combine(sourceDirectory, path)) : null;
                return full != null && IsWithin(full, sourceDirectory) ? full : "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal static bool IsFontPath(string path)
        {
            if (!IsLocalPath(path))
                return false;
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".ttf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".otf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ttc", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class HemiFileNames
    {
        private const string InvalidCharacters = @"/\:*?""<>|";
        private static readonly char[] TrailingCharacters = { '.', ' ' };

        internal static bool IsInvalid(char character)
        {
            return character < ' ' || character == (char)0x7F || InvalidCharacters.IndexOf(character) >= 0;
        }

        internal static string Sanitize(string name, string fallback, char replacement = '_', int maximumLength = 0)
        {
            string source = (name ?? "").Trim();
            char[] characters = null;
            for (int i = 0; i < source.Length; i++)
            {
                if (!IsInvalid(source[i]))
                    continue;
                if (characters == null)
                    characters = source.ToCharArray();
                characters[i] = replacement;
            }

            string cleaned = (characters == null ? source : new string(characters).Trim()).TrimEnd(TrailingCharacters);

            if (maximumLength > 0 && cleaned.Length > maximumLength)
                cleaned = cleaned.Substring(0, maximumLength).Trim().TrimEnd(TrailingCharacters);

            if (IsReserved(cleaned))
                cleaned = "_" + cleaned;
            if (maximumLength > 0 && cleaned.Length > maximumLength)
                cleaned = cleaned.Substring(0, maximumLength);
            return cleaned.Length == 0 ? fallback : cleaned;
        }

        internal static bool IsReserved(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            int dot = name.IndexOf('.');
            string stem = (dot < 0 ? name : name.Substring(0, dot)).TrimEnd(' ', '.').ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL"
                || stem == "CONIN$" || stem == "CONOUT$")
                return true;
            return stem.Length == 4 && (stem.StartsWith("COM", System.StringComparison.Ordinal)
                || stem.StartsWith("LPT", System.StringComparison.Ordinal))
                && ((stem[3] >= '1' && stem[3] <= '9') || stem[3] == '\u00b9' || stem[3] == '\u00b2' || stem[3] == '\u00b3');
        }
    }

    internal static class HemiHex
    {
        private const string Digits = "0123456789abcdef";

        internal static string Encode(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0)
                return string.Empty;

            char[] text = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                text[i * 2] = Digits[bytes[i] >> 4];
                text[i * 2 + 1] = Digits[bytes[i] & 15];
            }
            return new string(text);
        }

        internal static string Sha256(byte[] bytes)
        {
            using (SHA256 hash = SHA256.Create())
                return Encode(hash.ComputeHash(bytes));
        }
    }

    internal static class HemiNumberSafety
    {
        internal static float FiniteOr(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }

        internal static float Clamp(float value, float minimum, float maximum, float fallback)
        {
            return Math.Max(minimum, Math.Min(maximum, FiniteOr(value, fallback)));
        }
    }

    internal static class HemiTextSafety
    {
        private const int MaximumNormalizationPasses = 8;

        private static readonly Regex BreakTags = new Regex(@"<(?:br|cr|nbsp)\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Markup = new Regex(
            @"</?(?:color|material|quad|size|font|font-weight|b|i|u|s|sup|sub|align|alpha|cspace|indent|line-height|line-indent|" +
            @"a|action|link|lowercase|uppercase|allcaps|smallcaps|margin|margin-left|margin-right|mark|mspace|noparse|nobr|page|pos|space|" +
            @"sprite|style|voffset|width|gradient|rotate|scale|br|cr|nbsp|zwsp|zwj|shy)(?:=[^<>]*|\s[^<>]*)?>|<#[0-9a-f]{3,8}>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Whitespace = new Regex(@"[\s\p{Cc}]+|\\[rntv]", RegexOptions.Compiled);
        private static readonly Regex RemainingUnicodeEscapes = new Regex(@"\\(?=u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8})", RegexOptions.Compiled);

        internal static string Literal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            StringBuilder literal = null;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool control = char.IsControl(character);
                bool unicodeEscape = character == '\\' && index + 1 < value.Length
                    && (value[index + 1] == 'u' || value[index + 1] == 'U');
                if (control || unicodeEscape)
                {
                    if (literal == null)
                    {
                        literal = new StringBuilder(value.Length);
                        literal.Append(value, 0, index);
                    }
                    literal.Append(control ? ' ' : '＼');
                }
                else if (literal != null)
                {
                    literal.Append(character);
                }
            }
            return literal == null ? value : literal.ToString();
        }

        internal static string PlainSingleLine(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            for (int pass = 0; pass < MaximumNormalizationPasses; pass++)
            {
                string next = DecodeUnicodeEscapes(value);
                next = BreakTags.Replace(next, " ");
                next = Markup.Replace(next, "");
                if (string.Equals(next, value, StringComparison.Ordinal))
                    break;
                value = next;
            }

            value = value.Replace('<', '‹').Replace('>', '›');
            value = RemainingUnicodeEscapes.Replace(value, "＼");
            return Whitespace.Replace(value, " ").Trim();
        }

        private static string DecodeUnicodeEscapes(string value)
        {
            StringBuilder decoded = null;
            int start = 0;
            for (int index = 0; index < value.Length; index++)
            {
                if (!TryReadUnicodeEscape(value, index, out int codePoint, out int length))
                    continue;

                if (decoded == null)
                    decoded = new StringBuilder(value.Length);
                decoded.Append(value, start, index - start);
                if (codePoint <= char.MaxValue)
                    decoded.Append((char)codePoint);
                else
                    decoded.Append(char.ConvertFromUtf32(codePoint));
                index += length - 1;
                start = index + 1;
            }

            if (decoded == null)
                return value;
            decoded.Append(value, start, value.Length - start);
            return decoded.ToString();
        }

        private static bool TryReadUnicodeEscape(string value, int index, out int codePoint, out int length)
        {
            codePoint = 0;
            length = 0;
            if (value[index] != '\\' || index + 1 >= value.Length)
                return false;

            int digits = value[index + 1] == 'u' ? 4 : value[index + 1] == 'U' ? 8 : 0;
            if (digits == 0 || value.Length - index < digits + 2)
                return false;

            uint parsed = 0;
            for (int digit = 0; digit < digits; digit++)
            {
                char character = value[index + digit + 2];
                int hex = character >= '0' && character <= '9' ? character - '0'
                    : character >= 'a' && character <= 'f' ? character - 'a' + 10
                    : character >= 'A' && character <= 'F' ? character - 'A' + 10 : -1;
                if (hex < 0)
                    return false;
                parsed = (parsed << 4) | (uint)hex;
            }

            if (parsed > 0x10FFFF)
                return false;
            codePoint = (int)parsed;
            length = digits + 2;
            return true;
        }
    }

    internal static class HemiStreamSafety
    {
        internal static byte[] ReadFile(string path, long maximumBytes)
        {
            using (Stream input = OpenRead(path, maximumBytes))
            {
                long size = input.Length;
                if (size < 0)
                    size = 0;
                if (size > maximumBytes)
                    size = maximumBytes;
                if (size > int.MaxValue)
                    size = int.MaxValue;
                using (MemoryStream output = new MemoryStream((int)size))
                {
                    input.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        internal static Stream OpenRead(string path, long maximumBytes)
        {
            FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                if (input.Length > maximumBytes)
                    throw new InvalidDataException("The file exceeds its size limit.");
                return new LimitedReadStream(input, maximumBytes);
            }
            catch
            {
                input.Dispose();
                throw;
            }
        }

        internal static byte[] DecodeBase64(string encoded, long maximumBytes)
        {
            if (encoded == null || maximumBytes < 0)
                throw new InvalidDataException("Invalid asset byte budget.");
            long symbols = 0;
            int padding = 0;
            for (int i = 0; i < encoded.Length; i++)
            {
                char value = encoded[i];
                if (char.IsWhiteSpace(value))
                    continue;
                symbols++;
                if (value == '=') padding++;
                else if (padding != 0) throw new FormatException("Invalid base64 padding.");
            }
            if (symbols % 4 != 0 || padding > 2)
                throw new FormatException("Invalid base64 length.");
            long decodedBytes = symbols / 4 * 3 - padding;
            if (decodedBytes < 0 || decodedBytes > maximumBytes)
                throw new InvalidDataException("The embedded asset exceeds its size limit.");
            return Convert.FromBase64String(encoded);
        }

        private sealed class LimitedReadStream : Stream
        {
            private readonly Stream inner;
            private readonly long maximumBytes;
            private long consumed;

            internal LimitedReadStream(Stream input, long limit)
            {
                inner = input;
                maximumBytes = limit;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bounded = (int)Math.Min(count, maximumBytes - consumed + 1);
                int read = inner.Read(buffer, offset, bounded);
                if (read > maximumBytes - consumed)
                    throw new InvalidDataException("The file exceeds its size limit.");
                consumed += read;
                return read;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) inner.Dispose();
                base.Dispose(disposing);
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => inner.Length;
            public override long Position { get => consumed; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }

    internal static class HemiDownloadSafety
    {
        internal static async Task<int> ReadAsync(
            Stream source,
            byte[] buffer,
            TimeSpan idleTimeout,
            CancellationToken token,
            Action abortResponse)
        {
            token.ThrowIfCancellationRequested();
            Task<int> pending = source.ReadAsync(buffer, 0, buffer.Length, token);
            using (CancellationTokenSource timer = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task elapsed = Task.Delay(idleTimeout, timer.Token);
                if (await Task.WhenAny(pending, elapsed).ConfigureAwait(false) == pending)
                {
                    timer.Cancel();
                    int read = await pending.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    return read;
                }

                Exception closeError = null;
                try
                {
                    source.Dispose();
                }
                catch (Exception exception)
                {
                    closeError = exception;
                }

                try
                {
                    abortResponse?.Invoke();
                }
                catch (Exception exception)
                {
                    closeError = exception;
                }

                ObserveFault(pending);
                token.ThrowIfCancellationRequested();
                throw new TimeoutException("The download stopped sending data.", closeError);
            }
        }

        private static void ObserveFault(Task task)
        {
            task.ContinueWith(completed =>
            {
                Exception ignored = completed.Exception;
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    internal static class HemiImageSafety
    {
        internal const long MaximumFileBytes = 64L * 1024 * 1024;
        internal const int MaximumDimension = 8192;
        internal const long MaximumPixels = 16L * 1024 * 1024;
        internal const int MaximumCacheEntries = 128;
        internal const long MaximumCachePixels = 64L * 1024 * 1024;
        internal const int MaximumFailedPaths = 128;

        private const uint HeaderChunk = 0x49484452;
        private const uint DataChunk = 0x49444154;
        private const uint EndChunk = 0x49454E44;

        internal static bool IsWithinDimensions(int width, int height)
        {
            return width > 0 && height > 0 && width <= MaximumDimension && height <= MaximumDimension
                && (long)width * height <= MaximumPixels;
        }

        internal static bool CanCache(int count, long pixels, int width, int height)
        {
            return count >= 0 && count < MaximumCacheEntries && pixels >= 0 && pixels <= MaximumCachePixels
                && IsWithinDimensions(width, height) && (long)width * height <= MaximumCachePixels - pixels;
        }

        internal static bool TryGetDimensions(byte[] data, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (data == null || data.Length < 2 || data.LongLength > MaximumFileBytes)
                return false;

            if (data.Length >= 8 && data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71
                && data[4] == 13 && data[5] == 10 && data[6] == 26 && data[7] == 10)
                return TryReadPng(data, out width, out height);
            return data[0] == 255 && data[1] == 216 && TryReadJpeg(data, out width, out height);
        }

        private static bool TryReadPng(byte[] data, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (data.Length < 33 || ReadUInt32(data, 8) != 13 || ReadUInt32(data, 12) != HeaderChunk)
                return false;
            uint pngWidth = ReadUInt32(data, 16);
            uint pngHeight = ReadUInt32(data, 20);
            if (pngWidth > MaximumDimension || pngHeight > MaximumDimension)
                return false;
            width = (int)pngWidth;
            height = (int)pngHeight;
            if (!IsWithinDimensions(width, height) || data[26] != 0 || data[27] != 0 || data[28] > 1)
                return false;

            bool hasPixels = false;
            int offset = 33;
            while (offset <= data.Length - 12)
            {
                uint length = ReadUInt32(data, offset);
                if (length > data.Length - offset - 12)
                    return false;
                uint type = ReadUInt32(data, offset + 4);
                if (type == HeaderChunk)
                    return false;
                if (type == DataChunk)
                    hasPixels = true;
                if (type == EndChunk)
                    return length == 0 && hasPixels;
                offset += (int)length + 12;
            }
            return false;
        }

        private static bool TryReadJpeg(byte[] data, out int width, out int height)
        {
            width = 0;
            height = 0;
            int offset = 2;
            bool hasFrame = false;
            bool hasScan = false;
            bool inScan = false;
            while (offset < data.Length)
            {
                if (inScan)
                {
                    while (offset < data.Length && data[offset] != 255)
                        offset++;
                }
                if (offset >= data.Length || data[offset] != 255)
                    return false;
                while (offset < data.Length && data[offset] == 255)
                    offset++;
                if (offset >= data.Length)
                    return false;
                int marker = data[offset++];
                if (marker == 0 || (marker >= 208 && marker <= 215))
                {
                    if (!inScan)
                        return false;
                    continue;
                }
                inScan = false;
                if (marker == 217)
                    return hasFrame && hasScan;
                if (marker == 216 || marker == 220)
                    return false;
                if (marker == 1)
                    continue;
                if (offset > data.Length - 2)
                    return false;
                int length = ReadUInt16(data, offset);
                if (length < 2 || length > data.Length - offset)
                    return false;

                bool isFrame = marker >= 192 && marker <= 207 && marker != 196 && marker != 200 && marker != 204;
                if (isFrame)
                {
                    if (hasFrame || length < 11 || length != 8 + 3 * data[offset + 7])
                        return false;
                    height = ReadUInt16(data, offset + 3);
                    width = ReadUInt16(data, offset + 5);
                    if (!IsWithinDimensions(width, height))
                        return false;
                    hasFrame = true;
                }
                else if (marker == 218)
                {
                    if (!hasFrame || length < 8 || length != 6 + 2 * data[offset + 2])
                        return false;
                    hasScan = true;
                    inScan = true;
                }
                offset += length;
            }
            return false;
        }

        private static int ReadUInt16(byte[] data, int offset)
        {
            return (data[offset] << 8) | data[offset + 1];
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16)
                | ((uint)data[offset + 2] << 8) | data[offset + 3];
        }
    }

    internal static class HemiUpdateSafety
    {
        internal static Uri ValidateUrl(string value, string repository, bool firstHop, bool authorize)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps
                || !uri.IsDefaultPort || uri.UserInfo.Length != 0)
                throw new InvalidDataException("The update URL must be a direct HTTPS address.");

            string apiRoot = "/repos/" + repository + "/releases";
            bool api = string.Equals(uri.Host, "api.github.com", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(uri.AbsolutePath, apiRoot, StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.StartsWith(apiRoot + "/assets/", StringComparison.OrdinalIgnoreCase));
            bool download = string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.StartsWith("/" + repository + "/releases/download/", StringComparison.OrdinalIgnoreCase);
            if (firstHop)
            {
                if (authorize ? !api : !download)
                    throw new InvalidDataException("The update URL does not belong to this repository.");
            }
            else if (!api && !download
                && !string.Equals(uri.Host, "release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Host, "objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Host, "objects-origin.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Host, "github-releases.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The update redirect is not a GitHub release host.");
            }
            return uri;
        }

        internal static bool HasDigest(string digest)
        {
            if (digest == null || digest.Length != 71 || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                return false;
            for (int i = 7; i < digest.Length; i++)
            {
                char value = digest[i];
                if (!((value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F')))
                    return false;
            }
            return true;
        }

        internal static void ValidatePayload(byte[] bytes, long size, string digest)
        {
            if (bytes == null || bytes.LongLength != size || !HasDigest(digest))
                throw new InvalidDataException("The update size or digest metadata is missing or invalid.");
            if (!string.Equals(HemiHex.Sha256(bytes), digest.Substring(7), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The downloaded update does not match its release digest.");
        }

        internal static void ValidateAssembly(string path, string expectedName)
        {
            try
            {
                AssemblyName name = AssemblyName.GetAssemblyName(path);
                if (!string.Equals(name.Name, expectedName, StringComparison.Ordinal))
                    throw new InvalidDataException("The update has the wrong managed assembly identity.");
            }
            catch (BadImageFormatException exception)
            {
                throw new InvalidDataException("The update is not a managed assembly.", exception);
            }
        }
    }

    internal static class HemiAtomicFile
    {
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        internal static void WriteAllText(string path, string contents)
        {
            WriteAllText(path, contents, Utf8WithoutBom);
        }

        internal static void WriteAllText(string path, string contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            Save(path, stream =>
            {
                StreamWriter writer = new StreamWriter(stream, encoding);
                writer.Write(contents);
                writer.Flush();
            });
        }

        internal static void WriteAllBytes(string path, byte[] contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            Save(path, stream => stream.Write(contents, 0, contents.Length));
        }

        internal static void Copy(string sourcePath, string destinationPath)
        {
            if (sourcePath == null)
                throw new ArgumentNullException(nameof(sourcePath));
            if (destinationPath == null)
                throw new ArgumentNullException(nameof(destinationPath));

            string source = Path.GetFullPath(sourcePath);
            Save(destinationPath, output =>
            {
                using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buffer = new byte[65536];
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        output.Write(buffer, 0, read);
                }
            });
        }

        private static void Save(string path, Action<FileStream> write)
        {
            string destination = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);

            string temporary = TemporaryPath(destination);
            try
            {
                using (FileStream stream = new FileStream(
                    temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    write(stream);
                    stream.Flush(true);
                }

                Replace(temporary, destination);
            }
            finally
            {
                TryDelete(temporary);
            }
        }

        private static string TemporaryPath(string destination)
        {
            string directory = Path.GetDirectoryName(destination);
            string name = "." + Path.GetFileName(destination) + "."
                + Guid.NewGuid().ToString("N") + ".tmp";
            return Path.Combine(directory, name);
        }

        private static void Replace(string temporary, string destination)
        {
            if (!File.Exists(destination))
            {
                File.Move(temporary, destination);
                return;
            }

            File.Replace(temporary, destination, null);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
