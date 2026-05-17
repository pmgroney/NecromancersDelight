using System;
using System.Linq;
using UnityModManagerNet;
using wotr_mod.Classes;
using wotr_mod.Content.Localization;
using wotr_mod.Infrastructure;
using wotr_mod.Items;
using wotr_mod.Spells;

namespace wotr_mod.Content
{
    internal sealed class ModContentPatch : IGamePatch, IAreaLoadHandler
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly LocalizationTool _localization;
        private readonly IContentModule[] _modules;

        public ModContentPatch(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            string modPath)
        {
            _logger = logger;
            _localization = localization;
            _modules = new IContentModule[]
            {
                new SpellInstaller(blueprints, localization, logger, modPath),
                new CustomItemInstaller(blueprints, localization, logger),
                new CharacterClassInstaller(blueprints, localization, logger, modPath),
                new BillyQuestStarter(blueprints, logger),
                new CompanionInstaller(blueprints, localization, modPath),
                new DefendersHeartAssaultTimerPatch(blueprints, logger)
            };
        }

        public string Name => "Mod Content";

        public void RegisterLocalization()
        {
            ModText.Register(_localization);
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

        public void OnAreaLoaded()
        {
            foreach (var module in _modules.OfType<IAreaLoadModule>())
            {
                try
                {
                    module.OnAreaLoaded();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed area load for {((IContentModule)module).Name}: {ex}");
                }
            }
        }
    }
}
