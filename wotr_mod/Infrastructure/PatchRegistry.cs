using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using UnityModManagerNet;
using wotr_mod.Content;
using wotr_mod.Patches;

namespace wotr_mod.Infrastructure
{
    internal sealed class PatchRegistry
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly IGamePatch[] _patches;

        private PatchRegistry(UnityModManager.ModEntry.ModLogger logger, IGamePatch[] patches)
        {
            _logger = logger;
            _patches = patches;
        }

        public static PatchRegistry Create(UnityModManager.ModEntry.ModLogger logger, string modPath)
        {
            var blueprints = new BlueprintTool(logger);
            var localization = new LocalizationTool();

            return new PatchRegistry(
                logger,
                new IGamePatch[]
                {
                    new ModContentPatch(blueprints, localization, logger, modPath),
                    new CompanionSelectionPatch(
                        blueprints,
                        localization,
                        new CompanionSelectionPatch.CompanionSelectionTarget(
                            "Evoker",
                            ModBlueprintIds.Classes.Evoker,
                            ModBlueprintIds.Progressions.Evoker),
                        new CompanionSelectionPatch.CompanionSelectionTarget(
                            "Necromancer",
                            ModBlueprintIds.Classes.Necromancer,
                            ModBlueprintIds.Progressions.Necromancer)),
                    new BillyPlacementPatch(blueprints, logger),
                    new GravebladeStartingEquipmentPatch(blueprints, logger),
                    new CustomHeritagePatch(blueprints, localization),
                    new CleverPyromaniacGnomePatch(blueprints, localization),
                    new LeopardTripPatch(blueprints, localization, logger),
                    new VelociraptorGrowthPatch(blueprints, localization, logger)
                });
        }

        private static string _logFile = null;

        public static void FallbackError(string message)
        {
            if (_logFile != null)
            {
                try
                {
                    System.IO.File.AppendAllText(_logFile, $"[ERROR] {message}{Environment.NewLine}");
                }
                catch { }
            }
        }

        public void RegisterLocalization()
        {
            foreach (var patch in _patches)
            {
                patch.RegisterLocalization();
            }
        }

        public void ApplyAll()
        {
            foreach (var patch in _patches)
            {
                try
                {
                    patch.Apply();
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to apply {patch.Name}: {ex}");
                    throw;
                }
            }

            _logger.Log("Applied mod patches: " + string.Join(", ", _patches.Select(p => p.Name).ToArray()));
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            foreach (var handler in _patches.OfType<IUnitLoadHandler>())
            {
                handler.OnUnitLoaded(unit);
            }
        }

        public void OnAreaLoaded()
        {
            foreach (var handler in _patches.OfType<IAreaLoadHandler>())
            {
                handler.OnAreaLoaded();
            }
        }
    }
}
