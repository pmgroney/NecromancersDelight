using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using UnityModManagerNet;
using wotr_mod.Classes.Necromancer;
using wotr_mod.Content;
using wotr_mod.Content.Localization;
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
            ModText.Register(_localization);
            foreach (var installer in _contentInstallers)
            {
                installer.RegisterLocalization();
            }
        }

        public void Install()
        {
            var sorcererClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Sorcerer, "Sorcerer class");
            var wizardClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Wizard, "Wizard class");
            var sorcererSpellbook = _blueprints.Require<BlueprintSpellbook>(
                GameBlueprintIds.Spellbooks.Sorcerer, "Sorcerer spellbook");
            var sorcererProgression = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.Sorcerer, "Sorcerer progression");
            var wizardList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Wizard, "Wizard spell list");

            BlueprintFeatureSelection evokerBloodlineSelection = null;

            foreach (var definition in CharacterClassRegistry.GetActive())
            {
                try
                {
                    if (definition.UseEvokerBloodlines && evokerBloodlineSelection == null)
                    {
                        evokerBloodlineSelection = EnsureEvokerBloodlineSelection();
                    }

                    var spellList = EnsureSpellList(definition, wizardList);
                    var spellbook = _spellbookInstaller.EnsureSpellbook(definition, sorcererSpellbook, spellList);
                    var characterClass = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);

                    BlueprintFeatureBase bloodlineFeature = definition.UseEvokerBloodlines
                        ? evokerBloodlineSelection
                        : null;

                    var progression = _progressionInstaller.EnsureProgression(
                        definition, sorcererProgression, bloodlineFeature);
                    characterClass = EnsureClass(definition, sorcererClass, spellbook, progression);

                    if (definition.UseNecromancerBloodline)
                    {
                        _blueprints.SetCharacterClassAppearanceFromClass(characterClass, wizardClass);
                    }

                    if (definition.UseUndeadBloodline)
                    {
                        AddUndeadBloodline(progression);
                    }

                    _blueprints.SetCharacterClassHidden(characterClass, false);
                    _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);

                    try
                    {
                        _blueprints.SetProgressionClasses(characterClass.Progression, characterClass);
                    }
                    catch (Exception ex)
                    {
                        _blueprints.ReportError(
                            $"ERROR during deep-registration of {definition.InternalName}: {ex}");
                    }

                    foreach (var installer in _contentInstallers)
                    {
                        if (!installer.CanInstall(definition)) continue;
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

                    ConfigureClassPresentation(definition, characterClass, requireReferencedFeatures: true);

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

        // ─── Spell list ───────────────────────────────────────────────────────

        private BlueprintSpellList EnsureSpellList(CharacterClassDefinition definition, BlueprintSpellList donor)
        {
            var existing = _blueprints.Get<BlueprintSpellList>(definition.SpellListGuid);
            var spellList = existing ?? _blueprints.CloneBlueprint(
                donor, definition.SpellListGuid, definition.InternalName + "_SpellList");

            // Delegate spell population to whichever content installer owns this class.
            var installer = _contentInstallers.FirstOrDefault(i => i.CanInstall(definition));
            installer?.ConfigureSpellList(definition, spellList);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.SpellListGuid, spellList);
            }

            return spellList;
        }

        // ─── Class / progression ──────────────────────────────────────────────

        private BlueprintCharacterClass EnsureClass(
            CharacterClassDefinition definition,
            BlueprintCharacterClass donor,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            var existing = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);
            if (existing != null)
            {
                ConfigureClass(existing, definition, spellbook, progression);
                return existing;
            }

            var clone = _blueprints.CloneBlueprint(donor, definition.ClassGuid, definition.InternalName);
            ConfigureClass(clone, definition, spellbook, progression);
            _blueprints.AddCachedBlueprint(definition.ClassGuid, clone);
            return clone;
        }

        private void ConfigureClass(
            BlueprintCharacterClass characterClass,
            CharacterClassDefinition definition,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            _blueprints.SetCharacterClassSpellbook(characterClass, spellbook);
            _blueprints.SetCharacterClassProgression(characterClass, progression);
            _blueprints.SetCharacterClassDisplay(
                characterClass,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));
            ConfigureClassChassis(definition, characterClass);
            ConfigureClassPresentation(definition, characterClass);
        }

        private void ConfigureClassChassis(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            if (definition.Chassis == null) return;

            if (definition.Chassis.HitDie.HasValue)
            {
                _blueprints.SetCharacterClassHitDie(characterClass, definition.Chassis.HitDie.Value);
            }

            if (!string.IsNullOrEmpty(definition.Chassis.BaseAttackBonusGuid))
            {
                var bab = _blueprints.Require<BlueprintStatProgression>(
                    definition.Chassis.BaseAttackBonusGuid,
                    definition.InternalName + " base attack bonus progression");
                _blueprints.SetCharacterClassBaseAttackBonus(characterClass, bab);
            }
        }

        private void ConfigureClassPresentation(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            bool requireReferencedFeatures = false)
        {
            var presentation = definition.Presentation;
            if (presentation == null)
            {
                _blueprints.SetCharacterClassSignatureAbilities(characterClass);
                _blueprints.SetCharacterClassDefaultBuild(characterClass, null);
                return;
            }

            _blueprints.SetCharacterClassDifficulty(characterClass, presentation.Difficulty);
            _blueprints.SetCharacterClassAttributeRecommendations(
                characterClass,
                presentation.RecommendedAttributes,
                presentation.NotRecommendedAttributes);

            var signatureAbilities = presentation.SignatureAbilityGuids
                .Where(guid => !string.IsNullOrWhiteSpace(guid))
                .Select(guid => GetPresentationFeature(guid,
                    $"{definition.InternalName} signature ability", requireReferencedFeatures))
                .Where(f => f != null)
                .ToArray();
            _blueprints.SetCharacterClassSignatureAbilities(characterClass, signatureAbilities);

            var defaultBuild = string.IsNullOrWhiteSpace(presentation.DefaultBuildGuid)
                ? null
                : GetPresentationFeature(presentation.DefaultBuildGuid,
                    $"{definition.InternalName} default build", requireReferencedFeatures);
            _blueprints.SetCharacterClassDefaultBuild(characterClass, defaultBuild);
        }

        private BlueprintFeature GetPresentationFeature(string guid, string name, bool reportMissing)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null && reportMissing)
            {
                _blueprints.ReportError($"{name} ({guid}) was not available.");
            }
            return feature;
        }

        // ─── Evoker bloodline selection ────────────────────────────────────────

        private BlueprintFeatureSelection EnsureEvokerBloodlineSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.EvokerBloodline);
            if (existing != null) return existing;

            var donorSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SorcererBloodline, "Sorcerer bloodline selection");
            var selection = _blueprints.CloneBlueprint(
                donorSelection, ModBlueprintIds.Selections.EvokerBloodline, "WotrMod_EvokerBloodlineSelection");

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.EvokerBloodlineName),
                _localization.Text(LocalizationIds.Mod.EvokerBloodlineDescription));

            var bloodlines = new[]
            {
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ArcaneBloodline,
                    ModBlueprintIds.Progressions.EvokerArcaneBloodline, "WotrMod_EvokerBloodline_Arcane",
                    LocalizationIds.Mod.EvokerArcaneName, LocalizationIds.Mod.EvokerArcaneDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalAirBloodline,
                    ModBlueprintIds.Progressions.EvokerAirBloodline, "WotrMod_EvokerBloodline_Air",
                    LocalizationIds.Mod.EvokerAirName, LocalizationIds.Mod.EvokerAirDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalEarthBloodline,
                    ModBlueprintIds.Progressions.EvokerEarthBloodline, "WotrMod_EvokerBloodline_Earth",
                    LocalizationIds.Mod.EvokerEarthName, LocalizationIds.Mod.EvokerEarthDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalFireBloodline,
                    ModBlueprintIds.Progressions.EvokerFireBloodline, "WotrMod_EvokerBloodline_Fire",
                    LocalizationIds.Mod.EvokerFireName, LocalizationIds.Mod.EvokerFireDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalWaterBloodline,
                    ModBlueprintIds.Progressions.EvokerWaterBloodline, "WotrMod_EvokerBloodline_Water",
                    LocalizationIds.Mod.EvokerWaterName, LocalizationIds.Mod.EvokerWaterDescription)
            };

            var evokerClass = _blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Evoker);
            if (evokerClass != null)
            {
                foreach (var bloodline in bloodlines)
                {
                    _blueprints.SetProgressionClasses(bloodline, evokerClass);
                }
                _blueprints.SetProgressionClasses(selection, evokerClass);
            }

            _blueprints.SetFeatureSelectionFeatures(selection, bloodlines);
            _blueprints.SetFeatureSelectionAllFeatures(selection, bloodlines);
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.EvokerBloodline, selection);
            return selection;
        }

        private BlueprintProgression EnsureEvokerBloodline(
            string donorGuid, string newGuid, string internalName,
            string displayNameKey, string descriptionKey)
        {
            var existing = _blueprints.Get<BlueprintProgression>(newGuid);
            if (existing != null) return existing;

            var donor = _blueprints.Require<BlueprintProgression>(donorGuid, internalName + " donor");
            var clone = _blueprints.CloneBlueprint(donor, newGuid, internalName);
            _blueprints.SetUnitFactDisplay(
                clone, _localization.Text(displayNameKey), _localization.Text(descriptionKey));
            _blueprints.AddCachedBlueprint(newGuid, clone);
            return clone;
        }

        // ─── Undead bloodline ─────────────────────────────────────────────────

        private void AddUndeadBloodline(BlueprintProgression progression)
        {
            var undeadBloodline = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.UndeadBloodline, "Undead bloodline progression");

            var firstLevelEntry = progression.LevelEntries.FirstOrDefault(e => e.Level == 1);
            if (firstLevelEntry != null)
            {
                var features = firstLevelEntry.Features.ToList();
                features.Add(undeadBloodline);
                firstLevelEntry.SetFeatures(features);
            }
        }

        // ─── Shared static helpers ────────────────────────────────────────────

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }

        private static SkillPointsPerCharacterLevel CreateSkillPointBonus(string className)
        {
            return new SkillPointsPerCharacterLevel
            {
                name = className,
                SkillPointsPerLevel = 3
            };
        }
    }
}
