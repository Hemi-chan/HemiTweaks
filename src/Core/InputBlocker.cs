using System;
using System.Collections.Generic;
using ADOFAI.Editor;
using HarmonyLib;

namespace HemiTweaks
{
    internal static class InputBlocker
    {
        public static bool BlockBool(ref bool result)
        {
            if (!HemiTweaksMod.ShouldBlockGameInput)
                return true;

            result = false;
            return false;
        }

        public static bool BlockInt(ref int result)
        {
            if (!HemiTweaksMod.ShouldBlockGameInput)
                return true;

            result = 0;
            return false;
        }

        public static bool BlockMainKeys(ref List<AnyKeyCode> result)
        {
            if (!HemiTweaksMod.ShouldBlockGameInput)
                return true;

            result = new List<AnyKeyCode>();
            return false;
        }
    }

    [HarmonyPatch(typeof(RDInput), "GetState")]
    internal static class RDInputGetStatePatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInput), "GetMain", new Type[] { typeof(ButtonState) })]
    internal static class RDInputGetMainPatch
    {
        private static bool Prefix(ref int __result)
        {
            return InputBlocker.BlockInt(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInput), "GetStateKeys", new Type[] { typeof(ButtonState) })]
    internal static class RDInputGetStateKeysPatch
    {
        private static bool Prefix(ref List<AnyKeyCode> __result)
        {
            return InputBlocker.BlockMainKeys(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInput), "get_holdingControl")]
    internal static class RDInputHoldingControlPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInput), "get_holdingShift")]
    internal static class RDInputHoldingShiftPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInput), "get_holdingAlt")]
    internal static class RDInputHoldingAltPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInputType_Keyboard), "Main")]
    internal static class RDInputTypeKeyboardMainPatch
    {
        private static bool Prefix(ref int __result)
        {
            return InputBlocker.BlockInt(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInputType_Keyboard), "MainIgnoreActive")]
    internal static class RDInputTypeKeyboardMainIgnoreActivePatch
    {
        private static bool Prefix(ref int __result)
        {
            return InputBlocker.BlockInt(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInputType_AsyncKeyboard), "Main")]
    internal static class RDInputTypeAsyncKeyboardMainPatch
    {
        private static bool Prefix(ref int __result)
        {
            return InputBlocker.BlockInt(ref __result);
        }
    }

    [HarmonyPatch(typeof(RDInputType_Mouse), "Main")]
    internal static class RDInputTypeMouseMainPatch
    {
        private static bool Prefix(ref int __result)
        {
            return InputBlocker.BlockInt(ref __result);
        }
    }

    [HarmonyPatch(typeof(scrController), "ProcessKeyInputs")]
    internal static class ScrControllerProcessKeyInputsPatch
    {
        private static bool Prefix()
        {
            return !HemiTweaksMod.ShouldBlockGameInput;
        }
    }

    [HarmonyPatch(typeof(scrPlayer), "ValidInputWasTriggered")]
    internal static class ScrPlayerValidInputWasTriggeredPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(scrPlayer), "CountValidKeysPressed")]
    internal static class ScrPlayerCountValidKeysPressedPatch
    {
        private static bool Prefix(ref int __result)
        {
            return InputBlocker.BlockInt(ref __result);
        }
    }

    [HarmonyPatch(typeof(EditorKeybind), "IsPressed")]
    internal static class EditorKeybindIsPressedPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(EditorKeybind), "IsHeld")]
    internal static class EditorKeybindIsHeldPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }

    [HarmonyPatch(typeof(EditorKeybind), "IsReleased")]
    internal static class EditorKeybindIsReleasedPatch
    {
        private static bool Prefix(ref bool __result)
        {
            return InputBlocker.BlockBool(ref __result);
        }
    }
}
