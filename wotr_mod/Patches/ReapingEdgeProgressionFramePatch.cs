using System;
using HarmonyLib;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.CharacterInfo.Sections.Progression.ChupaChupses;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.ChupaChupses;
using UnityEngine;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    [HarmonyPatch]
    internal static class ReapingEdgeProgressionFramePatch
    {
        private static readonly Func<FeatureProgressionChupaChupsView, FeatureProgressionChupaChupsVM> ViewModelGetter =
            AccessTools.MethodDelegate<Func<FeatureProgressionChupaChupsView, FeatureProgressionChupaChupsVM>>(
                AccessTools.PropertyGetter(typeof(FeatureProgressionChupaChupsView), "ViewModel"));

        private static readonly AccessTools.FieldRef<FeatureProgressionChupaChupsView, GameObject> SquareBorder =
            AccessTools.FieldRefAccess<FeatureProgressionChupaChupsView, GameObject>("m_SquareBorder");

        private static readonly AccessTools.FieldRef<FeatureProgressionChupaChupsView, GameObject> RoundBorder =
            AccessTools.FieldRefAccess<FeatureProgressionChupaChupsView, GameObject>("m_RoundBorder");

        private static readonly AccessTools.FieldRef<FeatureProgressionChupaChupsView, GameObject> SquareBorderGot =
            AccessTools.FieldRefAccess<FeatureProgressionChupaChupsView, GameObject>("m_SquareBorderGot");

        private static readonly AccessTools.FieldRef<FeatureProgressionChupaChupsView, GameObject> RoundBorderGot =
            AccessTools.FieldRefAccess<FeatureProgressionChupaChupsView, GameObject>("m_RoundBorderGot");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FeatureProgressionChupaChupsView), "SetFrame")]
        private static void SetFramePostfix(FeatureProgressionChupaChupsView __instance)
        {
            UseRoundFrame(__instance, SquareBorder(__instance), RoundBorder(__instance));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FeatureProgressionChupaChupsView), "SetAccessMode")]
        private static void SetAccessModePostfix(FeatureProgressionChupaChupsView __instance)
        {
            UseRoundFrame(__instance, SquareBorderGot(__instance), RoundBorderGot(__instance));
        }

        private static void UseRoundFrame(
            FeatureProgressionChupaChupsView view,
            GameObject squareFrame,
            GameObject roundFrame)
        {
            if (!IsReapingEdge(view) || squareFrame == null || roundFrame == null)
            {
                return;
            }

            var wasVisible = squareFrame.activeSelf || roundFrame.activeSelf;
            squareFrame.SetActive(false);
            roundFrame.SetActive(wasVisible);
        }

        private static bool IsReapingEdge(FeatureProgressionChupaChupsView view)
        {
            var feature = ViewModelGetter(view)?.Feature?.Feature;
            return string.Equals(
                feature?.AssetGuid.ToString(),
                ModBlueprintIds.Features.GravebladeReapingEdge,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
