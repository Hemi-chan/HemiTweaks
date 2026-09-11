using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HemiTweaks
{
    internal static class RunCounters
    {
        private const string UnsavedKey = "unsaved";

        private static string loadedKey = "";

        private static string loadedScene = "";

        private static int attempts;
        private static int totalAttempts;
        private static int editorCheckpoints;

        private static int checkpointBase;

        internal static int Attempts => InLevel ? attempts : 0;

        internal static int TotalAttempts => InLevel ? totalAttempts : 0;

        internal static int Checkpoints => Math.Max(0, GameCheckpoints() - checkpointBase) + editorCheckpoints;

        private static bool InLevel
        {
            get
            {
                try { return loadedKey.Length != 0 && ADOBase.controller != null; }
                catch { return false; }
            }
        }

        internal static void NotifySceneChanged(string sceneName)
        {
            if (loadedKey.Length != 0 && string.Equals(sceneName, loadedScene, StringComparison.Ordinal))
                return;

            Unload();
        }

        internal static void NotifyRunStarted() => Guard(StartOfficialRun);

        private static void Guard(Action step)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Attempt counting failed: " + exception.Message);
            }
        }

        private static void StartOfficialRun()
        {
            if (ADOBase.isScnGame || ADOBase.isLevelEditor)
                return;

            scrController controller = ADOBase.controller;
            if (controller == null || !controller.gameworld)
                return;

            Adopt();
            EndRun();
        }

        private static void AdoptOfficial()
        {
            if (ADOBase.isScnGame || ADOBase.isLevelEditor)
                return;

            Adopt();
        }

        private static void UnloadInEditor()
        {
            if (ADOBase.isLevelEditor)
                Unload();
        }

        private static void Unload()
        {
            loadedKey = "";
            loadedScene = "";
            attempts = 0;
            totalAttempts = 0;
        }

        private static void Adopt()
        {
            string key = Identity();
            if (string.Equals(key, loadedKey, StringComparison.Ordinal))
                return;

            string previous = loadedKey;
            loadedKey = key;
            loadedScene = ActiveScene();

            if (string.Equals(previous, UnsavedKey, StringComparison.Ordinal) && Persisted(key))
            {
                totalAttempts = RunTotals.Move(previous, key);
                return;
            }

            attempts = 0;
            editorCheckpoints = 0;
            checkpointBase = Math.Max(0, GameCheckpoints());
            if (!Persisted(key))
                RunTotals.Forget(key);
            totalAttempts = TotalFor(key);
        }

        private static void EndRun()
        {
            attempts++;
            totalAttempts = Persisted(loadedKey) && GameCountsLevel()
                ? GameTotal()
                : RunTotals.Increment(loadedKey, Persisted(loadedKey));
        }

        private static void RefreshGameTotal()
        {
            if (loadedKey.Length == 0 || !Persisted(loadedKey) || !GameCountsLevel())
                return;

            totalAttempts = GameTotal();
        }

        private static int TotalFor(string key)
        {
            if (!Persisted(key))
                return RunTotals.Read(key, false);

            return GameCountsLevel() ? GameTotal() : RunTotals.Read(key, true);
        }

        private static bool Persisted(string key)
        {
            return !string.Equals(key, UnsavedKey, StringComparison.Ordinal);
        }

        private static string ActiveScene()
        {
            try
            {
                return SceneManager.GetActiveScene().name ?? "";
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Active scene could not be read for attempt counting: " + exception.Message);
                return "";
            }
        }

        private static string Identity()
        {
            try
            {
                scnGame level = ADOBase.customLevel;
                if (level != null && !string.IsNullOrEmpty(level.levelPath))
                    return "path:" + level.levelPath;

                if (ADOBase.isOfficialLevel)
                {
                    string name = ADOBase.controller?.levelName;
                    if (!string.IsNullOrEmpty(name))
                        return "level:" + name;
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Level identity could not be read for attempt counting: " + exception.Message);
            }

            return UnsavedKey;
        }

        private static bool GameCountsLevel()
        {
            try
            {
                if (ADOBase.isCLSLevel)
                    return true;

                scrController controller = ADOBase.controller;
                return controller != null && controller.isbosslevel && ADOBase.isOfficialLevel;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Level kind could not be read for attempt counting: " + exception.Message);
                return false;
            }
        }

        private static int GameTotal()
        {
            try
            {
                if (ADOBase.isCLSLevel)
                {
                    string hash = ADOBase.customLevel?.levelData?.Hash;
                    return string.IsNullOrEmpty(hash) ? 0 : Math.Max(0, Persistence.GetCustomWorldAttempts(hash));
                }

                return Math.Max(0, Persistence.GetWorldAttempts(scrController.currentWorld));
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("The game's attempt count could not be read: " + exception.Message);
                return 0;
            }
        }

        private static int GameCheckpoints()
        {
            try { return scrController.checkpointsUsed; }
            catch { return 0; }
        }

        [HarmonyPatch(typeof(scnGame), nameof(scnGame.FinishCustomLevelLoading))]
        private static class ScnGameFinishCustomLevelLoadingPatch
        {
            private static void Prefix() => Guard(Adopt);

            private static void Postfix() => Guard(EndRun);
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.WaitForStartCo))]
        private static class ScrControllerWaitForStartCoPatch
        {
            private static void Postfix() => Guard(AdoptOfficial);
        }

        [HarmonyPatch(typeof(scnGame), nameof(scnGame.LoadLevel))]
        private static class ScnGameLoadLevelPatch
        {
            private static void Prefix() => Guard(UnloadInEditor);

            private static void Postfix(bool __result)
            {
                if (__result)
                    Guard(Adopt);
            }
        }

        [HarmonyPatch(typeof(Persistence), nameof(Persistence.IncrementWorldAttempts))]
        private static class PersistenceIncrementWorldAttemptsPatch
        {
            private static void Postfix() => Guard(RefreshGameTotal);
        }

        [HarmonyPatch(typeof(Persistence), nameof(Persistence.IncrementCustomWorldAttempts))]
        private static class PersistenceIncrementCustomWorldAttemptsPatch
        {
            private static void Postfix() => Guard(RefreshGameTotal);
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.FailAction))]
        private static class ScrControllerFailActionPatch
        {
            private static void Postfix()
            {
                try
                {
                    if (!ADOBase.isLevelEditor || GCS.practiceMode)
                        return;

                    scnEditor editor = ADOBase.editor;
                    if (editor == null || GCS.checkpointNum <= 0 || GCS.checkpointNum != editor.selectedFloorCached)
                        return;

                    editorCheckpoints++;
                }
                catch
                {
                }
            }
        }

        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SwitchToEditMode))]
        private static class ScnEditorSwitchToEditModePatch
        {
            private static void Postfix()
            {
                editorCheckpoints = 0;
                checkpointBase = 0;
            }
        }
    }

    internal static class RunTotals
    {

        private const string KeyPrefix = "rt.";

        private const int DigestLength = 16;

        private static readonly byte[] SealSecret = Encoding.ASCII.GetBytes("HemiTweaks.RunTotals.2026-09-07");

        private static readonly Dictionary<string, int> known = new Dictionary<string, int>(StringComparer.Ordinal);

        internal static int Read(string identity, bool persistent)
        {
            int value;
            if (known.TryGetValue(identity, out value))
                return value;

            value = persistent ? Load(identity) : 0;
            known[identity] = value;
            return value;
        }

        internal static int Increment(string identity, bool persistent)
        {
            int value = Read(identity, persistent) + 1;
            known[identity] = value;
            if (persistent)
                Store(identity, value);
            return value;
        }

        internal static void Forget(string identity)
        {
            known.Remove(identity);
        }

        internal static int Move(string from, string to)
        {
            int carried = Read(from, false);
            known.Remove(from);

            int existing = Read(to, true);
            if (carried <= existing)
                return existing;

            known[to] = carried;
            Store(to, carried);
            return carried;
        }

        private static int Load(string identity)
        {
            string key = KeyFor(identity);
            string stored;
            try
            {
                stored = PlayerPrefs.GetString(key, "");
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Run total could not be read from the preference store: " + exception.Message);
                return 0;
            }

            if (string.IsNullOrEmpty(stored))
                return 0;

            int value;
            if (TryUnseal(key, stored, out value))
                return value;

            MelonLogger.Warning("Stored run total failed its check and starts again from zero: " + key);
            return 0;
        }

        private static void Store(string identity, int value)
        {
            string key = KeyFor(identity);
            try
            {
                PlayerPrefs.SetString(key, Seal(key, value));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Run total could not be written to the preference store: " + exception.Message);
            }
        }

        private static string KeyFor(string identity)
        {
            using (SHA256 hash = SHA256.Create())
                return KeyPrefix + HemiHex.Encode(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Substring(0, DigestLength);
        }

        private static string Seal(string key, int value)
        {
            return value.ToString(CultureInfo.InvariantCulture) + "." + Code(key, value);
        }

        private static bool TryUnseal(string key, string stored, out int value)
        {
            value = 0;
            int dot = stored.IndexOf('.');
            if (dot <= 0 || dot == stored.Length - 1)
                return false;

            int count;
            if (!int.TryParse(stored.Substring(0, dot), NumberStyles.None, CultureInfo.InvariantCulture, out count))
                return false;

            if (!string.Equals(stored.Substring(dot + 1), Code(key, count), StringComparison.Ordinal))
                return false;

            value = count;
            return true;
        }

        private static string Code(string key, int value)
        {
            byte[] message = Encoding.UTF8.GetBytes(key + "\n" + value.ToString(CultureInfo.InvariantCulture));
            using (HMACSHA256 mac = new HMACSHA256(SealSecret))
                return HemiHex.Encode(mac.ComputeHash(message)).Substring(0, DigestLength);
        }
    }
}
