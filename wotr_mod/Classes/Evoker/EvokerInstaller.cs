using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using UnityModManagerNet;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class EvokerInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public EvokerInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
        }

        public void Install(
            CharacterClassDefinition definition,
            BlueprintCharacterClass sorcererClass,
            BlueprintSpellbook sorcererSpellbook,
            BlueprintProgression sorcererProgression,
            BlueprintSpellList wizardList)
        {
            var evokerBloodlineSelection = EnsureEvokerBloodlineSelection();
            
            var spellList = EnsureSpellList(definition, wizardList);
            var spellbook = EnsureSpellbook(definition, sorcererSpellbook, spellList);
            var progression = EnsureProgression(definition, sorcererProgression, evokerBloodlineSelection);
            
            EnsureClass(definition, sorcererClass, spellbook, progression);
        }

        private BlueprintCharacterClass EnsureClass(
            CharacterClassDefinition definition,
            BlueprintCharacterClass donor,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            var characterClass = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);
            if (characterClass == null)
            {
                characterClass = _blueprints.CloneBlueprint(donor, definition.ClassGuid, definition.InternalName);
                _blueprints.AddCachedBlueprint(definition.ClassGuid, characterClass);
            }

            ConfigureClass(characterClass, definition, spellbook, progression);
            return characterClass;
        }

        private void ConfigureClass(
            BlueprintCharacterClass characterClass,
            CharacterClassDefinition definition,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            _blueprints.SetUnitFactDisplay(
                characterClass,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            characterClass.Spellbook = spellbook;
            characterClass.Progression = progression;

            if (definition.Chassis != null)
            {
                ConfigureClassChassis(definition, characterClass);
            }

            if (definition.Presentation != null)
            {
                ConfigureClassPresentation(definition, characterClass);
            }
        }

        private void ConfigureClassChassis(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            if (definition.Chassis.HitDie.HasValue)
            {
                characterClass.HitDie = definition.Chassis.HitDie.Value;
            }

            if (!string.IsNullOrEmpty(definition.Chassis.BaseAttackBonusGuid))
            {
                characterClass.BaseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                    definition.Chassis.BaseAttackBonusGuid,
                    $"{definition.InternalName} BAB");
            }
        }

        private void ConfigureClassPresentation(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            characterClass.Difficulty = definition.Presentation.Difficulty;
            if (definition.Presentation.RecommendedAttributes != null)
            {
                characterClass.RecommendedAttributes = definition.Presentation.RecommendedAttributes;
            }

            if (definition.Presentation.NotRecommendedAttributes != null)
            {
                characterClass.NotRecommendedAttributes = definition.Presentation.NotRecommendedAttributes;
            }
        }

        private BlueprintProgression EnsureProgression(
            CharacterClassDefinition definition,
            BlueprintProgression donor,
            BlueprintFeatureBase bloodlineFeature)
        {
            var progression = _blueprints.Get<BlueprintProgression>(definition.ProgressionGuid);
            if (progression == null)
            {
                progression = _blueprints.CloneBlueprint(donor, definition.ProgressionGuid, definition.InternalName + "Progression");
                _blueprints.AddCachedBlueprint(definition.ProgressionGuid, progression);
            }

            _blueprints.SetUnitFactDisplay(
                progression,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            var features = CopyFeatures(definition, progression.LevelEntries.SelectMany(le => le.Features), bloodlineFeature);
            progression.LevelEntries = new[]
            {
                CreateLevelEntry(1, features.ToArray())
            };

            return progression;
        }

        private List<BlueprintFeatureBase> CopyFeatures(
            CharacterClassDefinition definition,
            IEnumerable<BlueprintFeatureBase> features,
            BlueprintFeatureBase bloodlineFeature)
        {
            var result = new List<BlueprintFeatureBase>();
            foreach (var feature in features)
            {
                if (feature is BlueprintFeatureSelection selection && selection.AssetGuid == GameBlueprintIds.Selections.SorcererBloodlineSelection)
                {
                    if (definition.RemoveSorcererBloodline)
                    {
                        if (bloodlineFeature != null)
                        {
                            result.Add(bloodlineFeature);
                        }
                        continue;
                    }
                }
                result.Add(feature);
            }
            return result;
        }

        private BlueprintSpellList EnsureSpellList(CharacterClassDefinition definition, BlueprintSpellList donor)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(definition.SpellListGuid);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(donor, definition.SpellListGuid, definition.InternalName + "SpellList");
                _blueprints.AddCachedBlueprint(definition.SpellListGuid, spellList);
            }

            ConfigureEvokerSpellList(spellList);
            return spellList;
        }

        private void ConfigureEvokerSpellList(BlueprintSpellList spellList)
        {
            _blueprints.ClearSpellList(spellList);
            foreach (var spellDef in EvokerSpellRegistry.GetAll())
            {
                var spell = _blueprints.Get<BlueprintAbility>(spellDef.NewSpellGuid);
                if (spell != null)
                {
                    _blueprints.AddSpellToList(spellList, spell, spellDef.SpellLevel);
                }
            }
        }

        private BlueprintSpellbook EnsureSpellbook(CharacterClassDefinition definition, BlueprintSpellbook donor, BlueprintSpellList spellList)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(definition.SpellbookGuid);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(donor, definition.SpellbookGuid, definition.InternalName + "Spellbook");
                _blueprints.AddCachedBlueprint(definition.SpellbookGuid, spellbook);
            }

            spellbook.SpellList = spellList;
            spellbook.CastingStat = definition.CastingStat;
            _blueprints.SetUnitFactDisplay(
                spellbook,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            return spellbook;
        }

        private BlueprintFeatureSelection EnsureEvokerBloodlineSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.EvokerBloodline);
            if (existing != null) return existing;

            var selection = new BlueprintFeatureSelection
            {
                name = "WotrMod_EvokerBloodlineSelection",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Selections.EvokerBloodline)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.EvokerBloodline, selection);

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.EvokerBloodlineSelectionName),
                _localization.Text(LocalizationIds.Mod.EvokerBloodlineSelectionDescription));

            selection.m_AllFeatures = new[]
            {
                EnsureEvokerBloodline(
                    GameBlueprintIds.Progressions.BlueDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBlueDragonBloodline,
                    "WotrMod_EvokerBlueDragonBloodline",
                    LocalizationIds.Mod.EvokerBlueDragonBloodlineName,
                    LocalizationIds.Mod.EvokerBlueDragonBloodlineDescription).ToReference<BlueprintFeatureReference>(),
                EnsureEvokerBloodline(
                    GameBlueprintIds.Progressions.BrassDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBrassDragonBloodline,
                    "WotrMod_EvokerBrassDragonBloodline",
                    LocalizationIds.Mod.EvokerBrassDragonBloodlineName,
                    LocalizationIds.Mod.EvokerBrassDragonBloodlineDescription).ToReference<BlueprintFeatureReference>(),
                EnsureEvokerBloodline(
                    GameBlueprintIds.Progressions.BronzeDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBronzeDragonBloodline,
                    "WotrMod_EvokerBronzeDragonBloodline",
                    LocalizationIds.Mod.EvokerBronzeDragonBloodlineName,
                    LocalizationIds.Mod.EvokerBronzeDragonBloodlineDescription).ToReference<BlueprintFeatureReference>(),
                EnsureEvokerBloodline(
                    GameBlueprintIds.Progressions.CopperDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerCopperDragonBloodline,
                    "WotrMod_EvokerCopperDragonBloodline",
                    LocalizationIds.Mod.EvokerCopperDragonBloodlineName,
                    LocalizationIds.Mod.EvokerCopperDragonBloodlineDescription).ToReference<BlueprintFeatureReference>(),
                EnsureEvokerBloodline(
                    GameBlueprintIds.Progressions.GoldDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerGoldDragonBloodline,
                    "WotrMod_EvokerGoldDragonBloodline",
                    LocalizationIds.Mod.EvokerGoldDragonBloodlineName,
                    LocalizationIds.Mod.EvokerGoldDragonBloodlineDescription).ToReference<BlueprintFeatureReference>(),
                EnsureEvokerBloodline(
                    GameBlueprintIds.Progressions.SilverDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerSilverDragonBloodline,
                    "WotrMod_EvokerSilverDragonBloodline",
                    LocalizationIds.Mod.EvokerSilverDragonBloodlineName,
                    LocalizationIds.Mod.EvokerSilverDragonBloodlineDescription).ToReference<BlueprintFeatureReference>()
            };

            return selection;
        }

        private BlueprintProgression EnsureEvokerBloodline(
            string donorGuid,
            string newGuid,
            string internalName,
            string displayNameKey,
            string descriptionKey)
        {
            var existing = _blueprints.Get<BlueprintProgression>(newGuid);
            if (existing != null) return existing;

            var donor = _blueprints.Require<BlueprintProgression>(donorGuid, internalName + " donor");
            var clone = _blueprints.CloneBlueprint(donor, newGuid, internalName);
            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _blueprints.AddCachedBlueprint(newGuid, clone);
            return clone;
        }

        private LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            return new LevelEntry
            {
                Level = level,
                Features = features.ToList()
            };
        }
    }
}
