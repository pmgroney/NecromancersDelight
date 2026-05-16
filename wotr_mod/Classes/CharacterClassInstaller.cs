using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using UnityModManagerNet;
using wotr_mod.Classes.Evoker;
using wotr_mod.Classes.Necromancer;
using wotr_mod.Content;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes
{
    internal sealed class CharacterClassInstaller : IContentModule
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;
        private readonly List<IClassContentInstaller> _contentInstallers;
        private readonly ClassFactory _classFactory;
        private readonly ClassSpellbookInstaller _spellbookInstaller;
        private readonly ClassProgressionInstaller _progressionInstaller;

        public CharacterClassInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            string modPath)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _icons = new SpellIconLoader(modPath);
            _classFactory = new ClassFactory(blueprints, localization);
            _spellbookInstaller = new ClassSpellbookInstaller(blueprints);
            _progressionInstaller = new ClassProgressionInstaller(blueprints);

            _contentInstallers = new List<IClassContentInstaller>
            {
                new EvokerInstaller(blueprints, localization, logger, _icons),
                new NecromancerInstaller(blueprints, localization, logger, _icons)
            };
        }

        public string Name => "Character Classes";

        public void RegisterLocalization()
        {
            foreach (var installer in _contentInstallers)
            {
                installer.RegisterLocalization();
            }
        }

        public void Install()
        {
            var sorcererClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Sorcerer,
                "Sorcerer class");
            var sorcererSpellbook = _blueprints.Require<BlueprintSpellbook>(
                GameBlueprintIds.Spellbooks.Sorcerer,
                "Sorcerer spellbook");
            var sorcererProgression = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.Sorcerer,
                "Sorcerer progression");
            var wizardList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Wizard,
                "Wizard spell list");

            foreach (var definition in CharacterClassRegistry.GetActive())
            {
                try
                {
                    var spellList = EnsureSpellList(definition, wizardList);
                    var spellbook = _spellbookInstaller.EnsureSpellbook(definition, sorcererSpellbook, spellList);
                    var progressionFeature = EnsureProgressionFeature(definition);

                    var progression = _progressionInstaller.EnsureProgression(
                        definition,
                        sorcererProgression,
                        progressionFeature);
                    var characterClass = _classFactory.EnsureClass(definition, sorcererClass, spellbook, progression);
                    ConfigureProgression(definition, progression);

                    _blueprints.SetCharacterClassHidden(characterClass, false);
                    _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);

                    foreach (var installer in _contentInstallers)
                    {
                        if (installer.CanInstall(definition))
                        {
                            try
                            {
                                installer.Install(definition, characterClass, spellbook, spellList);
                            }
                            catch (Exception ex)
                            {
                                _blueprints.ReportError(
                                    $"ERROR installing class content for {definition.InternalName}: {ex}");
                            }
                        }
                    }

                    try
                    {
                        _blueprints.EnsureCustomClassOwnsProgressionFeatures(
                            characterClass.Progression,
                            definition.InternalName,
                            characterClass);
                    }
                    catch (Exception ex)
                    {
                        _blueprints.ReportError($"ERROR claiming progression ownership for {definition.InternalName}: {ex}");
                    }

                    _classFactory.ConfigureClassPresentation(
                        definition,
                        characterClass,
                        requireReferencedFeatures: true);

                    try
                    {
                        _blueprints.AddCharacterClassToRoot(characterClass);
                    }
                    catch (Exception ex)
                    {
                        _blueprints.ReportError($"ERROR adding {definition.InternalName} to root: {ex}");
                    }

                    _blueprints.ReportCharacterClassRegistrationErrors(characterClass, definition.InternalName);
                }
                catch (Exception ex)
                {
                    _blueprints.ReportError($"ERROR installing {definition.InternalName}: {ex}");
                    throw;
                }
            }
        }

        private BlueprintSpellList EnsureSpellList(CharacterClassDefinition definition, BlueprintSpellList donor)
        {
            return _classFactory.EnsureSpellList(
                definition,
                donor,
                spellList =>
                {
                    var installer = _contentInstallers.FirstOrDefault(i => i.CanInstall(definition));
                    installer?.ConfigureSpellList(definition, spellList);
                });
        }

        private BlueprintFeatureBase EnsureProgressionFeature(CharacterClassDefinition definition)
        {
            return _contentInstallers
                .FirstOrDefault(i => i.CanInstall(definition))
                ?.EnsureProgressionFeature(definition);
        }

        private void ConfigureProgression(CharacterClassDefinition definition, BlueprintProgression progression)
        {
            foreach (var installer in _contentInstallers)
            {
                if (installer.CanInstall(definition))
                {
                    installer.ConfigureProgression(definition, progression);
                }
            }
        }
    }
}
