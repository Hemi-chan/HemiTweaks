using System;
using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks
{
    internal static class NoCheckpoint
    {
        internal static bool ShouldSuppress()
        {
            return HemiTweaksMod.NoCheckpointEnabled;
        }
    }

    [HarmonyPatch(typeof(ffxCheckpoint), nameof(ffxCheckpoint.StartEffect))]
    internal static class NoCheckpointStartEffectPatch
    {
        private static bool Prefix()
        {
            return !NoCheckpoint.ShouldSuppress();
        }
    }

    internal static class NonScroll
    {
        internal static bool ShouldSuppress()
        {
            if (!HemiTweaksMod.NonScrollEnabled)
                return false;

            try
            {
                scnEditor editor = ADOBase.editor;
                return editor != null && editor.playMode;
            }
            catch
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(RDInput), nameof(RDInput.mouseScrollDelta), MethodType.Getter)]
    internal static class NonScrollWheelPatch
    {
        private static bool Prefix(ref Vector2 __result)
        {
            if (!NonScroll.ShouldSuppress())
                return true;

            __result = Vector2.zero;
            return false;
        }
    }

    internal sealed class ChangeBuildNameOriginal : MonoBehaviour
    {
        internal string Text;
    }

    public static class ChangeBuildName
    {

        [HarmonyPatch(typeof(scrEnableIfBeta), "Awake")]
        private static class ScrEnableIfBetaAwakePatch
        {
            private static void Postfix(scrEnableIfBeta __instance)
            {
                Apply(__instance);
            }
        }

        public static void Refresh()
        {
            foreach (scrEnableIfBeta beta in UnityEngine.Object.FindObjectsByType<scrEnableIfBeta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(beta);
        }

        private static void Apply(scrEnableIfBeta instance)
        {
            if (!instance.setBuildText)
                return;

            TMP_Text text = instance.GetComponent<TMP_Text>();

            if (text == null)
                return;

            ChangeBuildNameOriginal original = text.GetComponent<ChangeBuildNameOriginal>();
            if (original == null)
            {
                original = text.gameObject.AddComponent<ChangeBuildNameOriginal>();
                original.Text = text.text;
            }

            if (!HemiTweaksMod.EnableChangeBuildName)
            {
                text.text = original.Text;
                return;
            }

            string buildName = HemiTweaksMod.BuildName?.Trim();

            text.text = string.IsNullOrEmpty(buildName)
                ? original.Text
                : buildName;
        }
    }

    internal static class HideUi
    {
        private static bool capturedOriginals;
        private static bool originalDontShowTitles;
        private static bool originalNoAutoHud;

        private static int appliedScene = -1;

        internal static bool Active => HemiTweaksMod.HideUiEnabled;

        internal static void UpdateLifecycle()
        {
            int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if (scene == appliedScene)
                return;

            if (ADOBase.uiController == null && ADOBase.editor == null)
                return;

            appliedScene = scene;
            RefreshRuntimeState();
        }

        internal static bool HideLevelTitle => Active && HemiTweaksMod.HideLevelTitle;
        internal static bool HideAutoplay => Active && HemiTweaksMod.HideAutoplay;
        internal static bool HideNoFail => Active && HemiTweaksMod.HideNoFail;
        internal static bool HideResults => Active && HemiTweaksMod.HideResults;
        internal static bool HideErrorMeter => Active && HemiTweaksMod.HideErrorMeter;

        internal static void RefreshRuntimeState()
        {
            try
            {
                CaptureOriginals();

                if (capturedOriginals)
                {
                    GCS.d_dontShowTitles = HideLevelTitle || originalDontShowTitles;

                    RDC.noAutoHud = HideAutoplay || originalNoAutoHud;
                }

                ApplyEditorAutoplay();

                scrController controller = ADOBase.controller;
                if (controller != null)
                    controller.UpdateErrorMeterVisibility();

                ApplyNoFail();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("Hide UI could not be applied: " + exception.Message);
            }
        }

        private static void CaptureOriginals()
        {
            if (capturedOriginals)
                return;

            try
            {
                originalDontShowTitles = GCS.d_dontShowTitles;
                originalNoAutoHud = RDC.noAutoHud;
                capturedOriginals = true;
            }
            catch
            {
            }
        }

        private static void ApplyEditorAutoplay()
        {
            scnEditor editor = ADOBase.editor;
            if (editor == null)
                return;

            bool hide = HideAutoplay;

            if (editor.autoImage != null)
                editor.autoImage.enabled = !hide;
            if (editor.buttonAuto != null)
                editor.buttonAuto.enabled = !hide;
        }

        private static void ApplyNoFail()
        {
            scrUIController ui = ADOBase.uiController;
            if (ui == null || ui.noFailImage == null)
                return;

            ui.noFailImage.enabled = !HideNoFail;
        }

        internal static void ApplyResults(scrController controller)
        {
            if (controller == null || !HideResults)
                return;

            Hide(controller.txtCongrats);
            Hide(controller.txtAllStrictClear);
            Hide(controller.txtAprilCongrats);

            if (controller.detailedResults != null)
                controller.detailedResults.gameObject.SetActive(false);
        }

        private static void Hide(Text text)
        {
            if (text != null)
                text.gameObject.SetActive(false);
        }

        internal static void Shutdown()
        {
            if (!capturedOriginals)
                return;

            try
            {
                GCS.d_dontShowTitles = originalDontShowTitles;
                RDC.noAutoHud = originalNoAutoHud;
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(scrUIController), "Update")]
    internal static class HideUiNoFailPatch
    {
        private static void Postfix(scrUIController __instance)
        {
            if (!HideUi.HideNoFail || __instance == null || __instance.noFailImage == null)
                return;
            if (__instance.noFailImage.enabled)
                __instance.noFailImage.enabled = false;
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.OnLandOnPortal))]
    internal static class HideUiResultsPatch
    {
        private static void Postfix(scrController __instance)
        {
            HideUi.ApplyResults(__instance);
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.UpdateErrorMeterVisibility))]
    internal static class HideUiErrorMeterPatch
    {
        private static void Postfix(scrController __instance)
        {
            if (!HideUi.HideErrorMeter || __instance == null || __instance.errorMeter == null)
                return;
            __instance.errorMeter.gameObject.SetActive(false);
        }
    }

    public static class UnlockLimits
    {
        private static readonly FieldInfo PracticeSpeedPercentField =
            AccessTools.Field(typeof(PracticeTimeline), "speedPercent");
        private static readonly MethodInfo PracticeUpdateSpeedMethod =
            AccessTools.Method(typeof(PracticeTimeline), "UpdateSpeed");
        private static readonly FieldInfo CategoryTabsField =
            AccessTools.Field(typeof(scnEditor), "categoryTabs");

        private const int MinimumPlaySpeed = 1;
        private const int MaximumPlaySpeed = 1000;

        public static void RefreshRuntimeState()
        {
            bool enabled = HemiTweaksMod.UnlockLimitsEnabled;

            foreach (PauseSettingButton setting in UnityEngine.Object.FindObjectsByType<PauseSettingButton>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                ApplyRange(setting);
            }

            if (!enabled)
            {
                ClampPlaySpeed();

                foreach (PracticeTimeline timeline in UnityEngine.Object.FindObjectsByType<PracticeTimeline>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    PracticeUpdateSpeedMethod?.Invoke(timeline, null);
                }
            }

            foreach (scnEditor editor in UnityEngine.Object.FindObjectsByType<scnEditor>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (editor.levelEventsPanel == null)
                    continue;

                editor.ShowEventsPage(editor.currentPage);
            }
        }

        public static void Shutdown()
        {
            try
            {
                ClampPlaySpeed();
            }
            catch (Exception exception)
            {
                MelonLoader.MelonLogger.Warning("Could not restore the editor play speed: " + exception.Message);
            }
        }

        private static void ClampPlaySpeed()
        {
            int current = Persistence.shortcutPlaySpeed;
            int clamped = Mathf.Clamp(current, MinimumPlaySpeed, MaximumPlaySpeed);
            if (current == clamped)
                return;

            Persistence.shortcutPlaySpeed = clamped;
            MelonLoader.MelonLogger.Msg(
                "Editor play speed was outside the game's range (" + current + "%) and is back at " + clamped + "%.");
        }

        private static void ApplyRange(PauseSettingButton setting)
        {
            setting.hasRange = !HemiTweaksMod.UnlockLimitsEnabled && setting.minInt != setting.maxInt;
        }

        [HarmonyPatch(typeof(ADOFAI.PropertyInfo), nameof(ADOFAI.PropertyInfo.Validate), typeof(float))]
        private static class PropertyInfoValidateFloatPatch
        {
            private static bool Prefix(float value, ref float __result)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled)
                    return true;

                __result = value;
                return false;
            }
        }

        [HarmonyPatch(typeof(ADOFAI.PropertyInfo), nameof(ADOFAI.PropertyInfo.Validate), typeof(int))]
        private static class PropertyInfoValidateIntPatch
        {
            private static bool Prefix(int value, ref int __result)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled)
                    return true;

                __result = value;
                return false;
            }
        }

        [HarmonyPatch(
            typeof(ADOFAI.PropertyInfo),
            nameof(ADOFAI.PropertyInfo.Validate),
            typeof(Vector2),
            typeof(bool))]
        private static class PropertyInfoValidateVector2Patch
        {
            private static bool Prefix(
                ADOFAI.PropertyInfo __instance,
                Vector2 value,
                bool forceAllowEmpty,
                ref Vector2 __result)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled)
                    return true;

                Vector2 fallback = __instance.value_default is Vector2 vector
                    ? vector
                    : Vector2.zero;
                float x = value.x;
                float y = value.y;

                if (!__instance.vector2_allowEmpty && !forceAllowEmpty)
                {
                    if (float.IsNaN(x))
                        x = fallback.x;
                    if (float.IsNaN(y))
                        y = fallback.y;
                }

                __result = new Vector2(x, y);
                return false;
            }
        }

        [HarmonyPatch(
            typeof(ADOFAI.PropertyInfo),
            nameof(ADOFAI.PropertyInfo.Validate),
            typeof(Tuple<float, float>))]
        private static class PropertyInfoValidateFloatPairPatch
        {
            private static bool Prefix(
                ADOFAI.PropertyInfo __instance,
                Tuple<float, float> value,
                ref Tuple<float, float> __result)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled || value == null)
                    return true;

                Tuple<float, float> fallback =
                    __instance.value_default as Tuple<float, float> ??
                    new Tuple<float, float>(0f, 0f);
                float first = float.IsNaN(value.Item1) ? fallback.Item1 : value.Item1;
                float second = float.IsNaN(value.Item2) ? fallback.Item2 : value.Item2;

                __result = new Tuple<float, float>(first, second);
                return false;
            }
        }

        [HarmonyPatch(typeof(EditorSpeedIndicator), "ShiftSpeed")]
        private static class EditorSpeedIndicatorShiftSpeedPatch
        {
            private static bool Prefix(EditorSpeedIndicator __instance, int direction)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled)
                    return true;

                int step =
                    Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                        ? 1
                        : 10;
                long requested = (long)Persistence.shortcutPlaySpeed + (long)step * direction;
                int speed = requested > int.MaxValue
                    ? int.MaxValue
                    : requested < int.MinValue
                        ? int.MinValue
                        : (int)requested;

                Persistence.shortcutPlaySpeed = speed;
                __instance.percent.text = Persistence.shortcutPlaySpeed + "%";
                return false;
            }
        }

        [HarmonyPatch(typeof(Persistence), nameof(Persistence.shortcutPlaySpeed), MethodType.Setter)]
        private static class PersistenceShortcutPlaySpeedSetterPatch
        {
            private static bool Prefix(int value)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled)
                    return true;

                Persistence.generalPrefs.SetInt("shortcutPlaySpeed", value);
                return false;
            }
        }

        [HarmonyPatch(
            typeof(SettingsMenu),
            nameof(SettingsMenu.UpdateSetting),
            typeof(PauseSettingButton),
            typeof(SettingsMenu.Interaction))]
        private static class SettingsMenuUpdateSettingPatch
        {
            private static void Prefix(PauseSettingButton setting)
            {
                if (setting == null)
                    return;

                ApplyRange(setting);
            }
        }

        [HarmonyPatch(typeof(PracticeTimeline), "UpdateSpeed")]
        private static class PracticeTimelineUpdateSpeedPatch
        {
            private static bool Prefix(PracticeTimeline __instance)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled || PracticeSpeedPercentField == null)
                    return true;

                int speed = (int)PracticeSpeedPercentField.GetValue(__instance);
                __instance.speedText.text = speed + "%";
                return false;
            }
        }

        [HarmonyPatch(typeof(LevelEventButton), nameof(LevelEventButton.enableButton), MethodType.Setter)]
        private static class LevelEventButtonEnablePatch
        {
            private static void Prefix(ref bool value)
            {
                if (HemiTweaksMod.UnlockLimitsEnabled)
                    value = true;
            }
        }

        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.UpdateCategoryVisibility))]
        private static class EditorCategoryVisibilityPatch
        {
            private static void Postfix(scnEditor __instance)
            {
                if (!HemiTweaksMod.UnlockLimitsEnabled ||
                    CategoryTabsField?.GetValue(__instance) is not List<CategoryTab> tabs)
                {
                    return;
                }

                foreach (CategoryTab tab in tabs)
                {
                    if (tab != null)
                        tab.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.AddEventAtSelected))]
        private static class EditorAddEventPatch
        {
            private static void Prefix(scnEditor __instance, out bool __state)
            {
                __state = UnlockPathEditing(__instance);
            }

            private static void Postfix(scnEditor __instance, bool __state)
            {
                RestorePathEditing(__instance, __state);
            }

            private static Exception Finalizer(
                Exception __exception,
                scnEditor __instance,
                bool __state)
            {
                RestorePathEditing(__instance, __state);
                return __exception;
            }
        }

        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemoveEventAtSelected))]
        private static class EditorRemoveEventPatch
        {
            private static void Prefix(scnEditor __instance, out bool __state)
            {
                __state = UnlockPathEditing(__instance);
            }

            private static void Postfix(scnEditor __instance, bool __state)
            {
                RestorePathEditing(__instance, __state);
            }

            private static Exception Finalizer(
                Exception __exception,
                scnEditor __instance,
                bool __state)
            {
                RestorePathEditing(__instance, __state);
                return __exception;
            }
        }

        private static bool UnlockPathEditing(scnEditor editor)
        {
            if (!HemiTweaksMod.UnlockLimitsEnabled || !editor.lockPathEditing)
                return false;

            editor.lockPathEditing = false;
            return true;
        }

        private static void RestorePathEditing(scnEditor editor, bool unlocked)
        {
            if (unlocked)
                editor.lockPathEditing = true;
        }
    }
}
