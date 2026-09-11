using DG.Tweening;
using DG.Tweening.Core.Easing;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace HemiTweaks
{
    internal sealed class StateMotionPlayer
    {
        private enum Phase
        {
            Resting,
            Entering,
            Exiting,
            Gone
        }

        private const float Overshoot = 1.70158f;
        private const float Period = 0f;

        private const float Margin = 8f;

        private Phase phase;
        private float startTime;
        private float seconds;
        private Ease ease;
        private StateSlideEdge edge;
        private Vector2 from;
        private Vector2 to;
        private bool travelMeasured;

        private Vector2 resumeFrom;
        private bool hasResumePoint;

        internal Vector2 Displacement { get; private set; }

        internal void BeginEntrance(StateMotion motion)
        {
            bool inFlight = phase == Phase.Entering || phase == Phase.Exiting;
            Vector2 live = Displacement;

            Rest();
            if (motion == null || !motion.Enabled)
                return;
            Start(Phase.Entering, motion);

            if (inFlight && live != Vector2.zero)
            {
                resumeFrom = live;
                hasResumePoint = true;

                Displacement = live;
            }
        }

        internal void BeginExit(StateMotion motion)
        {
            if (motion == null || !motion.Enabled || phase == Phase.Exiting || phase == Phase.Gone)
                return;
            Start(Phase.Exiting, motion);
        }

        internal void Rest()
        {
            phase = Phase.Resting;
            Displacement = Vector2.zero;
            travelMeasured = false;
            hasResumePoint = false;
        }

        private void Start(Phase next, StateMotion motion)
        {
            phase = next;
            seconds = Mathf.Clamp(motion.Seconds, StateMotion.MinimumSeconds, StateMotion.MaximumSeconds);
            ease = motion.ResolveEase();
            edge = motion.Edge;
            travelMeasured = false;
        }

        internal void Tick(RectTransform rect, RectTransform canvas)
        {
            if (phase == Phase.Resting || phase == Phase.Gone)
                return;

            if (!travelMeasured)
            {
                if (rect == null || canvas == null)
                {
                    Rest();
                    return;
                }

                Vector2 offscreen = OffscreenDisplacement(rect, canvas);
                if (phase == Phase.Entering)
                {
                    from = hasResumePoint ? resumeFrom : offscreen;
                    to = Vector2.zero;
                    hasResumePoint = false;
                }
                else
                {
                    from = Displacement;
                    to = offscreen;
                }
                travelMeasured = true;
                startTime = Time.unscaledTime;
                Displacement = from;
                return;
            }

            float elapsed = Time.unscaledTime - startTime;
            if (elapsed >= seconds)
            {
                Displacement = to;
                phase = phase == Phase.Entering ? Phase.Resting : Phase.Gone;
                return;
            }

            float eased = EaseManager.Evaluate(ease, null, elapsed, seconds, Overshoot, Period);
            Displacement = from + (to - from) * eased;
        }

        private Vector2 OffscreenDisplacement(RectTransform rect, RectTransform canvas)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = canvas.InverseTransformPoint(corners[i]);
                xMin = Mathf.Min(xMin, local.x);
                xMax = Mathf.Max(xMax, local.x);
                yMin = Mathf.Min(yMin, local.y);
                yMax = Mathf.Max(yMax, local.y);
            }

            xMin -= Displacement.x;
            xMax -= Displacement.x;
            yMin -= Displacement.y;
            yMax -= Displacement.y;

            Rect screen = canvas.rect;
            switch (edge)
            {
                case StateSlideEdge.Right:
                    return new Vector2(Mathf.Max(0f, screen.xMax - xMin) + Margin, 0f);
                case StateSlideEdge.Top:
                    return new Vector2(0f, Mathf.Max(0f, screen.yMax - yMin) + Margin);
                case StateSlideEdge.Bottom:
                    return new Vector2(0f, -(Mathf.Max(0f, yMax - screen.yMin) + Margin));
                default:
                    return new Vector2(-(Mathf.Max(0f, xMax - screen.xMin) + Margin), 0f);
            }
        }
    }

    internal static class StateMotionPatches
    {
        [HarmonyPatch(typeof(scrController), nameof(scrController.Awake_Rewind))]
        private static class AwakeRewindPatch
        {
            private static void Postfix(scrController __instance)
            {
                StateGroupOverlayBehaviour.NotifyRunPrepared(__instance);
            }
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.FailAction))]
        private static class FailActionPatch
        {
            private static void Postfix(scrController __instance)
            {
                if (__instance != null && __instance == ADOBase.controller)
                    StateGroupOverlayBehaviour.NotifyRunCompleted();
            }
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.Fail2Action))]
        private static class Fail2ActionPatch
        {
            private static void Prefix(scrController __instance, out bool __state)
            {
                __state = __instance != null && __instance == ADOBase.controller
                    && __instance.state == States.Fail
                    && (__instance.gameworld || (__instance.currFloor != null && __instance.currFloor.freeroam));
            }

            private static void Postfix(scrController __instance, bool __state)
            {
                if (__state && __instance == ADOBase.controller)
                    StateGroupOverlayBehaviour.NotifyRunCompleted();
            }
        }

        [HarmonyPatch(typeof(scrController), nameof(scrController.OnLandOnPortal))]
        private static class OnLandOnPortalPatch
        {
            private static void Prefix(scrController __instance, float ___winTime, out bool __state)
            {
                __state = __instance != null && __instance == ADOBase.controller && ___winTime == 0f;
            }

            private static void Postfix(scrController __instance, bool __state)
            {
                if (__state && __instance == ADOBase.controller)
                    StateGroupOverlayBehaviour.NotifyRunCompleted();
            }
        }

        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SwitchToEditMode), new[] { typeof(bool) })]
        private static class SwitchToEditModePatch
        {
            private static void Postfix(scnEditor __instance)
            {
                if (__instance != null && __instance == ADOBase.editor && __instance.inStrictlyEditingMode)
                    StateGroupOverlayBehaviour.NotifyRunEnded();
            }
        }
    }
}
