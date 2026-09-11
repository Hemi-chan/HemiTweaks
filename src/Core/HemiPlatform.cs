using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using MelonLoader;
using UnityEngine;

namespace HemiTweaks
{
    internal static class HemiModifiers
    {
        internal static bool Command =>
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        internal static bool Shift =>
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        internal static readonly KeyCode[] AllKeys = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));
    }

    internal static class HemiShell
    {
        internal static bool OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                MelonLogger.Warning("Cannot open a folder: no path was given.");
                return false;
            }

            try
            {
                string full = Path.GetFullPath(path);
                if (!Directory.Exists(full))
                {
                    MelonLogger.Warning("Cannot open a folder that is not there: " + full);
                    return false;
                }

                full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                string url = ToFolderUrl(full);
                MelonLogger.Msg("Opening folder: " + url);
                Application.OpenURL(url);
                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Could not open " + path + ": " + exception.Message);
                return false;
            }
        }

        private static string ToFolderUrl(string folder)
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.WindowsEditor)
            {
                return "file:///" + folder.Replace(Path.DirectorySeparatorChar, '/');
            }

            return new Uri(folder).AbsoluteUri;
        }
    }

    internal static class HemiRelaunch
    {
        internal const int DelaySeconds = 5;
        private const string SteamRunUrl = "steam://rungameid/977950";

        internal static string WindowsScript(int processId)
        {
            string id = ProcessId(processId);
            return "Wait-Process -Id " + id + " -ErrorAction SilentlyContinue; "
                + "Start-Sleep -Seconds " + DelaySeconds.ToString(CultureInfo.InvariantCulture) + "; "
                + "Start-Process '" + SteamRunUrl + "'";
        }

        internal static string PosixScript(int processId, bool macOS)
        {
            return "while kill -0 " + ProcessId(processId) + " 2>/dev/null; do sleep 1; done; "
                + "sleep " + DelaySeconds.ToString(CultureInfo.InvariantCulture) + "; "
                + (macOS ? "open" : "xdg-open") + " '" + SteamRunUrl + "'";
        }

        private static string ProcessId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal static class HemiOverlayHost
    {
        internal static void Ensure<T>(ref GameObject host, string name) where T : Component
        {
            if (host == null)
            {
                host = new GameObject(name);
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<T>();
            }

            if (!host.activeSelf)
                host.SetActive(true);
        }
    }

    internal sealed class HemiSaveDebounce
    {
        private readonly Action save;
        private readonly float delay;
        private bool dirty;
        private float due;

        internal HemiSaveDebounce(Action save, float delaySeconds = 0.4f)
        {
            this.save = save;
            delay = Mathf.Max(0f, delaySeconds);
        }

        internal void Request()
        {
            dirty = true;
            due = Time.unscaledTime + delay;
        }

        internal void Tick()
        {
            if (dirty && Time.unscaledTime >= due)
                Flush();
        }

        internal void Flush()
        {
            if (!dirty)
                return;

            dirty = false;
            try
            {
                save();
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Error("A deferred save failed: " + exception);
            }
        }
    }

    internal static class HemiEases
    {
        internal static readonly Ease[] Values =
        {
            Ease.Linear,
            Ease.InSine, Ease.OutSine, Ease.InOutSine,
            Ease.InQuad, Ease.OutQuad, Ease.InOutQuad,
            Ease.InCubic, Ease.OutCubic, Ease.InOutCubic,
            Ease.InQuart, Ease.OutQuart, Ease.InOutQuart,
            Ease.InQuint, Ease.OutQuint, Ease.InOutQuint,
            Ease.InExpo, Ease.OutExpo, Ease.InOutExpo,
            Ease.InCirc, Ease.OutCirc, Ease.InOutCirc,
            Ease.InElastic, Ease.OutElastic, Ease.InOutElastic,
            Ease.InBack, Ease.OutBack, Ease.InOutBack,
            Ease.InBounce, Ease.OutBounce, Ease.InOutBounce
        };

        internal static readonly string[] Names = Array.ConvertAll(Values, value => value.ToString());
        private static readonly Dictionary<string, Ease> ByName = BuildLookup();

        private static Dictionary<string, Ease> BuildLookup()
        {
            Dictionary<string, Ease> lookup = new Dictionary<string, Ease>(StringComparer.Ordinal);
            for (int i = 0; i < Values.Length; i++)
                lookup.Add(Names[i], Values[i]);
            return lookup;
        }

        internal static int IndexOf(string name) => Array.IndexOf(Names, name);

        internal static Ease Parse(string name, Ease fallback = Ease.OutCubic)
        {
            return name != null && ByName.TryGetValue(name, out Ease value) ? value : fallback;
        }

        internal static float Evaluate(Ease ease, float progress)
        {
            if (float.IsNaN(progress) || progress <= 0f)
                return 0f;
            if (progress >= 1f)
                return 1f;
            return EaseManager.Evaluate(ease, null, progress, 1f, 1.70158f, 0f);
        }
    }
}
