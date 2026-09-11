using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace HemiTweaks
{
    public static class PlanetColorChanger
    {
        private const string OverlayObjectName = "HemiTweaks_PlanetColorOverlay";

        private sealed class LoadedOverlay
        {
            public Texture2D Texture;
            public Sprite Sprite;
            public long Pixels;
        }

        private static readonly Dictionary<PlanetRenderer, SpriteRenderer> OverlayMap =
            new Dictionary<PlanetRenderer, SpriteRenderer>();

        private static readonly Dictionary<string, LoadedOverlay> LoadedOverlays =
            new Dictionary<string, LoadedOverlay>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> FailedOverlays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static long loadedPixels;
        private static bool imageBudgetWarning;

        private static bool editorIsPlaying;
        private static bool originalColorsCaptured;
        private static bool suppressApply;
        private static PlanetColor originalRedColor;
        private static PlanetColor originalBlueColor;

        private static MelonPreferences_Entry<string> backupRedEntry;
        private static MelonPreferences_Entry<string> backupBlueEntry;

        public static void Initialize(MelonPreferences_Category category)
        {
            if (category == null)
                return;

            backupRedEntry = category.CreateEntry(
                "PlanetColorBackupRed", "", "Planet Colour Backup (Red)",
                "The player's own red planet colour, held while the changer is active.");
            backupBlueEntry = category.CreateEntry(
                "PlanetColorBackupBlue", "", "Planet Colour Backup (Blue)",
                "The player's own blue planet colour, held while the changer is active.");
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                CaptureOriginalColors();
                ApplyColors();
                return;
            }

            RestoreOriginalColors();
        }

        public static void ApplyColors()
        {
            if (suppressApply || !HemiTweaksMod.EnablePlanetColorChanger)
                return;

            CaptureOriginalColors();

            Color redPlanetColor = HemiTweaksMod.RedPlanetColor;
            Color bluePlanetColor = HemiTweaksMod.BluePlanetColor;
            PlanetColor redPersistenceColor = new PlanetColor(redPlanetColor);
            PlanetColor bluePersistenceColor = new PlanetColor(bluePlanetColor);
            Persistence.SetPlayerColor(redPersistenceColor, true);
            Persistence.SetPlayerColor(bluePersistenceColor, false);

            scrPlayerManager manager = ADOBase.playerManager;
            if (manager != null)
            {
                Color redRingColor = HemiTweaksMod.RedRingColor;
                Color blueRingColor = HemiTweaksMod.BlueRingColor;
                Color redTailColor = HemiTweaksMod.RedTailColor;
                Color blueTailColor = HemiTweaksMod.BlueTailColor;

                scrPlayer[] players = manager.players;
                for (int i = 0; i < players.Length; i++)
                {
                    scrPlayer player = players[i];
                    if (player == null || player.planetarySystem == null)
                        continue;

                    ApplyColorToPlanet(player.planetarySystem.planetRed?.planetRenderer, true, redPlanetColor, redRingColor, redTailColor);
                    ApplyColorToPlanet(player.planetarySystem.planetBlue?.planetRenderer, false, bluePlanetColor, blueRingColor, blueTailColor);
                }
            }

            if (!HemiTweaksMod.PlanetOverlayEnabled)
                DisableAllOverlays();

            TryUpdateLogoColors();
        }

        public static void InvalidateOverlayCache()
        {
            HashSet<Sprite> owned = new HashSet<Sprite>();
            foreach (LoadedOverlay loaded in LoadedOverlays.Values)
            {
                if (loaded.Sprite != null)
                    owned.Add(loaded.Sprite);
            }
            foreach (SpriteRenderer overlay in OverlayMap.Values)
            {
                if (overlay != null)
                    overlay.sprite = null;
            }
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer != null && renderer.sprite != null && owned.Contains(renderer.sprite))
                    renderer.sprite = null;
            }
            foreach (LoadedOverlay loaded in LoadedOverlays.Values)
            {
                if (loaded.Sprite != null)
                    UnityEngine.Object.Destroy(loaded.Sprite);
                if (loaded.Texture != null)
                    UnityEngine.Object.Destroy(loaded.Texture);
            }

            LoadedOverlays.Clear();
            FailedOverlays.Clear();
            loadedPixels = 0;
            imageBudgetWarning = false;
        }

        public static void Shutdown()
        {
            if (HemiTweaksMod.EnablePlanetColorChanger)
                RestoreOriginalColors();
            else
                DisableAllOverlays();

            InvalidateOverlayCache();
            OverlayMap.Clear();
        }

        private static void CaptureOriginalColors()
        {
            if (originalColorsCaptured)
                return;

            if (TryReadBackup(backupRedEntry, out PlanetColor storedRed) &&
                TryReadBackup(backupBlueEntry, out PlanetColor storedBlue))
            {
                originalRedColor = storedRed;
                originalBlueColor = storedBlue;
                originalColorsCaptured = true;
                MelonLogger.Msg("Recovered the planet colours saved before the changer was last active.");
                return;
            }

            originalRedColor = Persistence.GetPlayerColor(true);
            originalBlueColor = Persistence.GetPlayerColor(false);
            originalColorsCaptured = true;
            WriteBackup();
        }

        private static void SetBackup(string red, string blue)
        {
            if (backupRedEntry == null || backupBlueEntry == null)
                return;

            backupRedEntry.Value = red;
            backupBlueEntry.Value = blue;
            backupRedEntry.Category?.SaveToFile(false);
        }

        private static void WriteBackup() => SetBackup(Serialize(originalRedColor), Serialize(originalBlueColor));

        private static void ClearBackup() => SetBackup("", "");

        private static string Serialize(PlanetColor color)
        {
            string custom = color.customColor.HasValue
                ? "#" + ColorUtility.ToHtmlStringRGBA(color.customColor.Value)
                : "";
            return ((int)color.preset).ToString(CultureInfo.InvariantCulture) + "|" + custom;
        }

        private static bool TryReadBackup(MelonPreferences_Entry<string> entry, out PlanetColor color)
        {
            color = default;
            string value = entry?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] parts = value.Split('|');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int preset) ||
                !Enum.IsDefined(typeof(PlanetColorPreset), preset))
            {
                return false;
            }

            Color? custom = null;
            if (!string.IsNullOrEmpty(parts[1]))
            {
                if (!ColorUtility.TryParseHtmlString(parts[1], out Color parsed))
                    return false;
                custom = parsed;
            }

            color = new PlanetColor { preset = (PlanetColorPreset)preset, customColor = custom };
            return true;
        }

        private static void RestoreOriginalColors()
        {
            DisableAllOverlays();

            if (!originalColorsCaptured)
                return;

            suppressApply = true;
            try
            {
                Persistence.SetPlayerColor(originalRedColor, true);
                Persistence.SetPlayerColor(originalBlueColor, false);

                scrPlayerManager manager = ADOBase.playerManager;
                if (manager != null)
                {
                    scrPlayer[] players = manager.players;
                    for (int i = 0; i < players.Length; i++)
                    {
                        scrPlayer player = players[i];
                        if (player?.planetarySystem != null)
                            player.planetarySystem.LoadPlanetColors(player.playerID);
                    }
                }
            }
            finally
            {
                suppressApply = false;
            }

            originalColorsCaptured = false;
            editorIsPlaying = false;
            ClearBackup();
            TryUpdateLogoColors();
        }

        private static void ApplyColorToPlanet(PlanetRenderer planet, bool isRed, Color planetColor, Color ringColor, Color tailColor)
        {
            if (planet == null)
                return;

            planet.EnableCustomColor();
            planet.SetPlanetColor(planetColor);
            planet.ringComp.color = ringColor.WithAlpha(ringColor.a * 0.4f);
            planet.SetTailColor(tailColor);

            ApplyOverlayToPlanet(planet, isRed);
        }

        private static void ApplyOverlayToPlanet(PlanetRenderer planet, bool isRed)
        {
            SpriteRenderer overlay = GetOrCreateOverlay(planet);

            if (!HemiTweaksMod.PlanetOverlayEnabled || (ADOBase.isLevelEditor && !editorIsPlaying))
            {
                overlay.sprite = null;
                overlay.gameObject.SetActive(false);
                return;
            }

            string path = isRed ? HemiTweaksMod.RedOverlayPath : HemiTweaksMod.BlueOverlayPath;
            float scale = isRed ? HemiTweaksMod.RedOverlayScale : HemiTweaksMod.BlueOverlayScale;
            Sprite sprite = LoadOverlaySprite(path);

            if (sprite == null)
            {
                overlay.sprite = null;
                overlay.gameObject.SetActive(false);
                return;
            }

            overlay.gameObject.SetActive(true);
            overlay.sprite = sprite;
            overlay.transform.localScale = Vector3.one * scale;
            overlay.sortingLayerID = planet.sprite.meshRenderer.sortingLayerID;
            overlay.sortingOrder = planet.sprite.meshRenderer.sortingOrder + 10;
            overlay.color = Color.white;
        }

        internal static void NotifySceneChanged()
        {
            PruneOverlayMap(false);
            SweepUnusedOverlays();
            FailedOverlays.Clear();
            imageBudgetWarning = false;
        }

        private static void PruneOverlayMap(bool disableLive)
        {
            List<PlanetRenderer> stale = null;
            foreach (KeyValuePair<PlanetRenderer, SpriteRenderer> pair in OverlayMap)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    if (disableLive)
                    {
                        pair.Value.gameObject.SetActive(false);
                        pair.Value.sprite = null;
                    }

                    continue;
                }

                if (stale == null)
                    stale = new List<PlanetRenderer>();
                stale.Add(pair.Key);
            }

            if (stale == null)
                return;

            for (int i = 0; i < stale.Count; i++)
                OverlayMap.Remove(stale[i]);
        }

        private static void SweepUnusedOverlays()
        {
            if (LoadedOverlays.Count == 0)
                return;

            HashSet<Sprite> used = new HashSet<Sprite>();
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer != null && renderer.sprite != null)
                    used.Add(renderer.sprite);
            }

            List<string> unused = new List<string>();
            foreach (KeyValuePair<string, LoadedOverlay> pair in LoadedOverlays)
            {
                if (pair.Value.Sprite != null && used.Contains(pair.Value.Sprite))
                    continue;
                if (pair.Value.Sprite != null)
                    UnityEngine.Object.Destroy(pair.Value.Sprite);
                if (pair.Value.Texture != null)
                    UnityEngine.Object.Destroy(pair.Value.Texture);
                loadedPixels -= pair.Value.Pixels;
                unused.Add(pair.Key);
            }
            for (int i = 0; i < unused.Count; i++)
                LoadedOverlays.Remove(unused[i]);
        }

        private static SpriteRenderer GetOrCreateOverlay(PlanetRenderer planet)
        {
            if (OverlayMap.TryGetValue(planet, out SpriteRenderer overlay) && overlay != null)
                return overlay;

            Transform existing = planet.transform.Find(OverlayObjectName);
            if (existing != null)
            {
                overlay = existing.gameObject.GetOrAddComponent<SpriteRenderer>();
                OverlayMap[planet] = overlay;
                return overlay;
            }

            GameObject overlayObject = new GameObject(OverlayObjectName);
            overlayObject.transform.SetParent(planet.transform, false);
            overlayObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            overlayObject.transform.localRotation = Quaternion.identity;
            overlayObject.transform.localScale = Vector3.one;

            overlay = overlayObject.AddComponent<SpriteRenderer>();
            OverlayMap[planet] = overlay;
            return overlay;
        }

        private static void DisableAllOverlays()
        {
            foreach (PlanetRenderer planet in UnityEngine.Object.FindObjectsByType<PlanetRenderer>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Transform overlay = planet.transform.Find(OverlayObjectName);
                if (overlay != null)
                    overlay.gameObject.SetActive(false);
            }

            PruneOverlayMap(true);
        }

        private static Sprite LoadOverlaySprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            if (FailedOverlays.Contains(path))
                return null;

            string fullPath = path;
            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                if (!HemiAssetSafety.IsLocalPath(path))
                {
                    if (FailedOverlays.Count < HemiImageSafety.MaximumFailedPaths && FailedOverlays.Add(path))
                        MelonLogger.Warning("Planet overlay images must use local file paths.");
                    return null;
                }
                fullPath = GetOverlayFullPath(path);
                if (LoadedOverlays.TryGetValue(fullPath, out LoadedOverlay cached))
                    return cached.Sprite;
                if (FailedOverlays.Contains(fullPath))
                    return null;
                if (FailedOverlays.Count >= HemiImageSafety.MaximumFailedPaths
                    || !HemiImageSafety.CanCache(LoadedOverlays.Count, loadedPixels, 1, 1))
                {
                    WarnImageBudget();
                    return null;
                }

                texture = HemiImageDecoder.LoadFile(fullPath, "Planet overlay image", HemiImageSafety.MaximumCachePixels - loadedPixels);
                if (texture == null)
                {
                    FailedOverlays.Add(fullPath);
                    return null;
                }
                if (!HemiImageSafety.CanCache(LoadedOverlays.Count, loadedPixels, texture.width, texture.height))
                {
                    FailedOverlays.Add(fullPath);
                    WarnImageBudget();
                    return null;
                }

                texture.name = "HemiTweaks Planet Overlay";
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                if (sprite == null)
                    throw new InvalidOperationException("Unity could not create the overlay sprite.");

                long pixels = (long)texture.width * texture.height;
                LoadedOverlays[fullPath] = new LoadedOverlay
                {
                    Texture = texture,
                    Sprite = sprite,
                    Pixels = pixels
                };
                loadedPixels += pixels;
                texture = null;
                Sprite result = sprite;
                sprite = null;
                return result;
            }
            catch (Exception exception)
            {
                if (FailedOverlays.Count < HemiImageSafety.MaximumFailedPaths && FailedOverlays.Add(fullPath))
                    MelonLogger.Warning("Planet overlay image failed to load (" + fullPath + "): " + exception.Message);
                return null;
            }
            finally
            {
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
        }

        private static void WarnImageBudget()
        {
            if (imageBudgetWarning)
                return;
            imageBudgetWarning = true;
            MelonLogger.Warning("Planet overlay cache reached its image or failed-path limit; existing images were retained.");
        }

        private static string GetOverlayFullPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            if (ADOBase.customLevel != null && !string.IsNullOrEmpty(ADOBase.customLevel.levelPath))
            {
                string levelFolder = Path.GetDirectoryName(ADOBase.customLevel.levelPath);
                if (!string.IsNullOrEmpty(levelFolder))
                    return Path.Combine(levelFolder, path);
            }

            string modDirectory = Path.GetDirectoryName(typeof(PlanetColorChanger).Assembly.Location);
            return Path.Combine(modDirectory ?? ".", path);
        }

        private static void TryUpdateLogoColors()
        {
            try
            {
                if (scrLogoText.instance != null)
                    scrLogoText.instance.UpdateColors();
            }
            catch (Exception)
            {
            }
        }

        [HarmonyPatch(typeof(PlanetarySystem), nameof(PlanetarySystem.LoadPlanetColors))]
        private static class PlanetarySystemLoadPlanetColorsPatch
        {
            private static void Postfix()
            {
                ApplyColors();
            }
        }

        [HarmonyPatch(typeof(scrPlayer), "Start")]
        private static class ScrPlayerStartPatch
        {
            private static void Postfix()
            {
                ApplyColors();
            }
        }

        [HarmonyPatch(typeof(scnGame), nameof(scnGame.ResetScene))]
        private static class ScnGameResetScenePatch
        {
            private static void Postfix()
            {
                if (ADOBase.isLevelEditor)
                    editorIsPlaying = false;

                ApplyColors();
            }
        }

        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.Play))]
        private static class ScnEditorPlayPatch
        {
            private static void Postfix()
            {
                editorIsPlaying = true;
                ApplyColors();
            }
        }
    }

    internal static class TileCornerCurvature
    {
        private const float FiveDegrees = 0.08726646f;
        private const float StraightAngleEpsilon = 0.0001f;
        private const float DefaultClockwiseArcThreshold = 2.0942953f;
        private const float DefaultCounterclockwiseArcThreshold = 4.3634233f;
        private const float ConstantMatchEpsilon = 0.00001f;
        private const float RefreshDelay = 0.08f;

        private static bool runtimeEnabled;
        private static float runtimeAmount = 0.5f;
        private static bool refreshPending;
        private static float refreshAt;

        internal static bool IsActive => runtimeEnabled && runtimeAmount > 0.0001f;

        internal static void Initialize(bool enabled, float amount)
        {
            runtimeEnabled = enabled;
            runtimeAmount = Mathf.Clamp01(amount);
            if (runtimeEnabled)
                RequestRefresh(true);
        }

        internal static void SetEnabled(bool enabled)
        {
            if (runtimeEnabled == enabled)
                return;

            runtimeEnabled = enabled;
            RequestRefresh(true);
        }

        internal static void SetAmount(float amount)
        {
            float clamped = Mathf.Clamp01(amount);
            if (Mathf.Approximately(runtimeAmount, clamped))
                return;

            runtimeAmount = clamped;
            if (runtimeEnabled)
                RequestRefresh(false);
        }

        internal static void UpdateLifecycle()
        {
            if (!refreshPending || Time.realtimeSinceStartup < refreshAt)
                return;

            RefreshFloorMeshes();
        }

        internal static void Shutdown()
        {
            if (!runtimeEnabled)
                return;

            runtimeEnabled = false;
            refreshPending = false;
            RefreshFloorMeshes();
        }

        internal static float GetAdjustedShortAngle(float angleA, float angleB)
        {
            float fullTurn = Mathf.PI * 2f;
            float clockwise = PositiveModulo(angleB - angleA, fullTurn);
            float counterclockwise = PositiveModulo(angleA - angleB, fullTurn);
            float shortest = Mathf.Min(clockwise, counterclockwise);

            if (!IsActive ||
                shortest < StraightAngleEpsilon ||
                Mathf.Abs(shortest - Mathf.PI) < StraightAngleEpsilon)
            {
                return shortest;
            }

            float fullyRoundedAngle = Mathf.Min(shortest, FiveDegrees);
            return Mathf.Lerp(shortest, fullyRoundedAngle, runtimeAmount);
        }

        internal static float GetClockwiseArcThreshold()
        {
            return IsActive ? Mathf.PI - StraightAngleEpsilon : DefaultClockwiseArcThreshold;
        }

        internal static float GetCounterclockwiseArcThreshold()
        {
            return IsActive ? Mathf.PI + StraightAngleEpsilon : DefaultCounterclockwiseArcThreshold;
        }

        private static float PositiveModulo(float value, float modulus)
        {
            return (value % modulus + modulus) % modulus;
        }

        private static void RequestRefresh(bool immediate)
        {
            refreshPending = true;
            refreshAt = immediate ? Time.realtimeSinceStartup : Time.realtimeSinceStartup + RefreshDelay;
        }

        private static List<Mesh> TakeCachedMeshes()
        {
            List<Mesh> taken = new List<Mesh>();
            try
            {
                if (FloorMesh.cache == null)
                    return taken;

                foreach (KeyValuePair<string, FloorMesh.MeshCache> entry in FloorMesh.cache)
                {
                    if (entry.Value != null && entry.Value.mesh != null)
                        taken.Add(entry.Value.mesh);
                }
                FloorMesh.cache.Clear();
            }
            catch (System.Exception exception)
            {
                MelonLogger.Warning("Failed to take the tile geometry cache: " + exception.Message);
            }
            return taken;
        }

        private static void DestroyReplacedMeshes(List<Mesh> meshes)
        {
            if (meshes == null || meshes.Count == 0)
                return;

            try
            {
                HashSet<int> live = new HashSet<int>();
                FloorMesh[] all = UnityEngine.Object.FindObjectsByType<FloorMesh>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null)
                        continue;

                    MeshFilter filter = all[i].GetComponent<MeshFilter>();
                    Mesh mesh = filter == null ? null : filter.sharedMesh;
                    if (mesh != null)
                        live.Add(mesh.GetInstanceID());
                }

                for (int i = 0; i < meshes.Count; i++)
                {
                    Mesh mesh = meshes[i];
                    if (mesh != null && !live.Contains(mesh.GetInstanceID()))
                        UnityEngine.Object.Destroy(mesh);
                }
            }
            catch (System.Exception exception)
            {
                MelonLogger.Warning("Failed to release replaced tile meshes: " + exception.Message);
            }
        }

        private static void RefreshFloorMeshes()
        {
            refreshPending = false;

            try
            {
                List<Mesh> stale = TakeCachedMeshes();

                FloorMesh[] floorMeshes = UnityEngine.Object.FindObjectsByType<FloorMesh>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

                foreach (FloorMesh floorMesh in floorMeshes)
                {
                    if (floorMesh != null)
                        floorMesh.meshChanged = true;
                }

                FloorMesh.UpdateAllRequired();

                foreach (FloorMesh floorMesh in floorMeshes)
                {
                    if (floorMesh != null && floorMesh.polygonCollider != null)
                        floorMesh.GenerateCollider();
                }

                DestroyReplacedMeshes(stale);
            }
            catch (System.Exception exception)
            {
                MelonLogger.Warning("Failed to refresh tile corner curvature: " + exception.Message);
            }
        }

        [HarmonyPatch(typeof(FloorMesh), "SmallestAngleBetweenTwoAngles")]
        private static class SmallestAnglePatch
        {
            private static bool Prefix(float angleA, float angleB, ref float __result)
            {
                if (!IsActive)
                    return true;

                __result = GetAdjustedShortAngle(angleA, angleB);
                return false;
            }
        }

        [HarmonyPatch(typeof(FloorMesh), "GetPositions")]
        private static class ArcRangePatch
        {
            private static readonly MethodInfo ClockwiseThresholdMethod = AccessTools.Method(
                typeof(TileCornerCurvature),
                nameof(GetClockwiseArcThreshold));

            private static readonly MethodInfo CounterclockwiseThresholdMethod = AccessTools.Method(
                typeof(TileCornerCurvature),
                nameof(GetCounterclockwiseArcThreshold));

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int replacements = 0;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value)
                    {
                        if (Mathf.Abs(value - DefaultClockwiseArcThreshold) < ConstantMatchEpsilon)
                        {
                            instruction.opcode = OpCodes.Call;
                            instruction.operand = ClockwiseThresholdMethod;
                            replacements++;
                        }
                        else if (Mathf.Abs(value - DefaultCounterclockwiseArcThreshold) < ConstantMatchEpsilon)
                        {
                            instruction.opcode = OpCodes.Call;
                            instruction.operand = CounterclockwiseThresholdMethod;
                            replacements++;
                        }
                    }

                    yield return instruction;
                }

                if (replacements != 2)
                    MelonLogger.Warning("Tile Corner Curvature found " + replacements + " of 2 FloorMesh arc thresholds.");
            }
        }
    }
}
