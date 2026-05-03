using System;
using System.Linq;
using UnityModManagerNet;
using wotr_mod.Classes;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Content
{
    internal sealed class ModContentPatch : IGamePatch
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly IContentModule[] _modules;

        public ModContentPatch(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            string modPath)
        {
            _logger = logger;
            _modules = new IContentModule[]
            {
                new SpellInstaller(blueprints, localization, logger, modPath),
                new CharacterClassInstaller(blueprints, localization, logger, modPath),
                new CompanionInstaller(blueprints, localization, modPath)
            };
        }

        public string Name => "Mod Content";

        public void RegisterLocalization()
        {
            foreach (var module in _modules)
            {
                module.RegisterLocalization();
            }
        }

        public void Apply()
        {
            foreach (var module in _modules)
            {
                try
                {
                    module.Install();
                    _logger.Log($"Installed {module.Name}.");
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to install {module.Name}: {ex}");
                }
            }

            _logger.Log("Content modules: " + string.Join(", ", _modules.Select(m => m.Name).ToArray()));
        }
    }
}
