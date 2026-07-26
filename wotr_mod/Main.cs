using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using UnityEngine;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod
{
    public static class Main
    {
        private static UnityModManager.ModEntry.ModLogger _logger;
        private static PatchRegistry _registry;
        private static bool _applied;
        private const float TooltipSliderWidth = 180f;
        private const float TooltipSliderIndent = 12f;
        private static readonly Color OptionsAccentColor = new Color(0.58f, 0.78f, 1f);

        internal static string ModPath { get; private set; }
        internal static NecromancersDelightSettings Settings { get; private set; }

        internal static void Log(string message)
        {
            _logger?.Log(message);
        }

        internal static void Warning(string message)
        {
            _logger?.Warning(message);
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _logger = modEntry.Logger;
            ModPath = modEntry.Path;
            Settings = UnityModManager.ModSettings.Load<NecromancersDelightSettings>(modEntry)
                ?? new NecromancersDelightSettings();
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            _registry = PatchRegistry.Create(_logger, ModPath);

            try
            {
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                TryApplyPatch();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load mod patches: {ex}");
                return false;
            }

            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Space(16f);

            GUILayout.BeginVertical(GUILayout.Width(320f));
            Settings.EnableAchievementsWhileModded = GUILayout.Toggle(
                Settings.EnableAchievementsWhileModded,
                "Re-enable achievements while modded");

            Settings.MakeWoljifBaseRogue = GUILayout.Toggle(
                Settings.MakeWoljifBaseRogue,
                "Make Woljif a base Rogue (requires restart)");

            GUILayout.Space(14f);
            GUILayout.Label("Mouseover tooltip icon size");
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(TooltipSliderIndent);
            GUILayout.BeginVertical(GUILayout.Width(TooltipSliderWidth));
            Settings.TooltipIconMagnificationMode = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(
                    Settings.TooltipIconMagnificationMode,
                    0,
                    2,
                    GUILayout.Width(TooltipSliderWidth)));
            Settings.TooltipIconMagnificationMode = Mathf.Clamp(Settings.TooltipIconMagnificationMode, 0, 2);
            DrawTooltipMagnificationScale(Settings.TooltipIconMagnificationMode);
            GUILayout.Label(
                TooltipIconMagnificationLabel(Settings.TooltipIconMagnificationMode),
                SelectedTooltipValueStyle());
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawTooltipMagnificationScale(int selectedMode)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(TooltipSliderWidth));
            DrawTooltipScaleMarker("Off", selectedMode == 0, TextAnchor.MiddleLeft);
            DrawTooltipScaleMarker("1.5x", selectedMode == 1, TextAnchor.MiddleCenter);
            DrawTooltipScaleMarker("2x", selectedMode == 2, TextAnchor.MiddleRight);
            GUILayout.EndHorizontal();
        }

        private static void DrawTooltipScaleMarker(string label, bool selected, TextAnchor alignment)
        {
            GUILayout.Label(
                label,
                selected ? SelectedTooltipMarkerStyle(alignment) : TooltipMarkerStyle(alignment),
                GUILayout.Width(60f));
        }

        private static GUIStyle TooltipMarkerStyle(TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment
            };
        }

        private static GUIStyle SelectedTooltipMarkerStyle(TextAnchor alignment)
        {
            var style = TooltipMarkerStyle(alignment);
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = OptionsAccentColor;
            return style;
        }

        private static GUIStyle SelectedTooltipValueStyle()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = OptionsAccentColor;
            return style;
        }

        private static string TooltipIconMagnificationLabel(int mode)
        {
            switch (mode)
            {
                case 0:
                    return "Disabled";
                case 2:
                    return "2x";
                default:
                    return "1.5x";
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        internal static void TryApplyPatch()
        {
            if (_applied || _registry == null)
            {
                return;
            }

            try
            {
                _registry.RegisterLocalization();
                _registry.ApplyAll();
                _applied = true;
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warning($"Mod patch is waiting for blueprints: {ex.Message}");
            }
        }

        internal static void RegisterLocalization()
        {
            _registry?.RegisterLocalization();
        }

        internal static void OnUnitLoaded(UnitEntityData unit)
        {
            if (!_applied || _registry == null)
            {
                return;
            }

            _registry.OnUnitLoaded(unit);
        }

        internal static void OnAreaLoaded()
        {
            if (!_applied || _registry == null)
            {
                return;
            }

            _registry.OnAreaLoaded();
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        private static class BlueprintsCacheInitPatch
        {
            private static void Postfix()
            {
                TryApplyPatch();
            }
        }

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.Init))]
        private static class LocalizationManagerInitPatch
        {
            private static void Postfix()
            {
                RegisterLocalization();
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "Initialize")]
        private static class UnitEntityDataInitializePatch
        {
            private static void Postfix(UnitEntityData __instance)
            {
                OnUnitLoaded(__instance);
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "OnPostLoad")]
        private static class UnitEntityDataPostLoadPatch
        {
            private static void Postfix(UnitEntityData __instance)
            {
                OnUnitLoaded(__instance);
            }
        }

        [HarmonyPatch(typeof(Game), "AreaLoadingComplete")]
        private static class GameAreaLoadingCompletePatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                __result = NotifyAfterAreaLoadingComplete(__result);
            }
        }

        private static IEnumerator NotifyAfterAreaLoadingComplete(IEnumerator areaLoading)
        {
            while (areaLoading.MoveNext())
            {
                yield return areaLoading.Current;
            }

            OnAreaLoaded();
        }
    }
}
