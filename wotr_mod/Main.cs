using System;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod
{
    public static class Main
    {
        private static UnityModManager.ModEntry.ModLogger _logger;
        private static PatchRegistry _registry;
        private static bool _applied;

        internal static string ModPath { get; private set; }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _logger = modEntry.Logger;
            ModPath = modEntry.Path;
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
    }
}
