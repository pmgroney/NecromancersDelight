using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal sealed partial class EvokerInstaller
    {
        private BlueprintArchetype[] EnsureArchetypes(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            return new[]
            {
                EnsureShadowbornArchetype(characterClass),
                EnsureDraconicEvokerArchetype(characterClass)
            };
        }

        private BlueprintArchetype EnsureDraconicEvokerArchetype(BlueprintCharacterClass characterClass)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.DraconicEvoker);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_DraconicEvokerArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.DraconicEvoker)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.DraconicEvoker, archetype);
            }

            var evokerBloodlineSelection = _blueprints.Require<BlueprintFeatureSelection>(
                ModBlueprintIds.Selections.EvokerBloodline,
                "Evoker bloodline selection");
            var draconicBloodlineSelection = EnsureDraconicEvokerBloodlineSelection(characterClass);
            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusMedium,
                "Draconic Evoker base attack bonus progression");
            var weaponFocusClaw = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.WeaponFocusClaw,
                "Weapon Focus (Claw)");
            var lightArmorProficiency = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.ArmorProficiencyLight,
                "Light Armor Proficiency");
            var arcaneArmorProficiency = EnsureDraconicEvokerArcaneArmorProficiency(characterClass);

            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, null);
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                new[]
                {
                    CreateLevelEntry(
                        1,
                        draconicBloodlineSelection,
                        weaponFocusClaw,
                        lightArmorProficiency,
                        arcaneArmorProficiency)
                },
                new[] { CreateLevelEntry(1, evokerBloodlineSelection) });
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private BlueprintFeature EnsureDraconicEvokerArcaneArmorProficiency(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DraconicEvokerArcaneArmorProficiency);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_DraconicEvokerArcaneArmorProficiency",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.DraconicEvokerArcaneArmorProficiency),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DraconicEvokerArcaneArmorProficiency, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerDescription));

            var bloodragerProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.BloodragerProficiencies,
                "Bloodrager Proficiencies");
            var sourceComponent = _blueprints.GetComponents<BlueprintComponent>(bloodragerProficiencies)
                .FirstOrDefault(candidate => candidate.GetType().Name == "ArcaneArmorProficiency");
            if (sourceComponent == null)
            {
                _logger.Error("Bloodrager Proficiencies has no ArcaneArmorProficiency component to clone.");
                _blueprints.SetComponents(feature);
                return feature;
            }

            var clonedComponent = _blueprints.CloneComponent(sourceComponent);
            clonedComponent.name = "$ArcaneArmorProficiency$DraconicEvokerLightArmor";
            if (clonedComponent is ArcaneArmorProficiency armorProficiency)
            {
                armorProficiency.Armor = new[] { ArmorProficiencyGroup.Light };
            }

            _blueprints.SetComponents(feature, clonedComponent);
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintArchetype EnsureShadowbornArchetype(BlueprintCharacterClass characterClass)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Shadowborn);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_EvokerShadowbornArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Shadowborn)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Shadowborn, archetype);
            }

            var evokerBloodlineSelection = _blueprints.Require<BlueprintFeatureSelection>(
                ModBlueprintIds.Selections.EvokerBloodline,
                "Evoker bloodline selection");
            var sorcererFeatSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SorcererFeatSelection,
                "Sorcerer feat selection");
            var sorcererBonusFeat = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SorcererBonusFeat,
                "Sorcerer bonus feat");
            var shadowbornBloodline = EnsureShadowbornBloodline(characterClass);
            var shadowbornBonusFeat = EnsureShadowbornBonusFeatSelection(characterClass);
            var shadowbornLivingGhost = EnsureShadowbornLivingGhostFeature(characterClass);

            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.ShadowbornName),
                _localization.Text(LocalizationIds.Mod.ShadowbornDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, null);
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                CreateShadowbornArchetypeFeatureEntries(
                    shadowbornBloodline,
                    shadowbornBonusFeat,
                    shadowbornLivingGhost),
                CreateShadowbornArchetypeRemoveFeatureEntries(evokerBloodlineSelection, sorcererBonusFeat, sorcererFeatSelection));
            if (characterClass.Progression != null)
            {
                _blueprints.AddProgressionUiGroup(characterClass.Progression, shadowbornBonusFeat);
                _blueprints.AddProgressionUiGroup(characterClass.Progression, shadowbornLivingGhost);
            }

            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private static LevelEntry[] CreateShadowbornArchetypeRemoveFeatureEntries(
            BlueprintFeatureBase evokerBloodlineSelection,
            BlueprintFeatureBase sorcererBonusFeat,
            BlueprintFeatureBase sorcererFeatSelection)
        {
            return new[]
            {
                CreateLevelEntry(1, evokerBloodlineSelection, sorcererBonusFeat),
                CreateLevelEntry(7, sorcererFeatSelection),
                CreateLevelEntry(13, sorcererFeatSelection),
                CreateLevelEntry(19, sorcererFeatSelection)
            };
        }

        private static LevelEntry[] CreateShadowbornArchetypeFeatureEntries(
            BlueprintProgression shadowbornBloodline,
            BlueprintFeatureSelection shadowbornBonusFeat,
            BlueprintFeature shadowbornLivingGhost)
        {
            var entries = (shadowbornBloodline.LevelEntries ?? Array.Empty<LevelEntry>())
                .Select(entry =>
                {
                    var features = (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                        .Where(feature => feature != null)
                        .ToArray();
                    return features.Length == 0 ? null : CreateLevelEntry(entry.Level, features);
                })
                .Where(entry => entry != null)
                .ToList();

            AddFeatureToLevel(entries, 1, shadowbornBonusFeat);
            AddFeatureToLevel(entries, 6, shadowbornBonusFeat);
            AddFeatureToLevel(entries, 10, shadowbornBonusFeat);
            AddFeatureToLevel(entries, 16, shadowbornBonusFeat);
            AddFeatureToLevel(entries, 20, shadowbornBonusFeat);
            AddFeatureToLevel(entries, 20, shadowbornLivingGhost);
            return entries
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        private static void AddFeatureToLevel(
            ICollection<LevelEntry> entries,
            int level,
            BlueprintFeatureBase feature)
        {
            if (feature == null)
            {
                return;
            }

            var entry = entries.FirstOrDefault(levelEntry => levelEntry.Level == level);
            if (entry == null)
            {
                entries.Add(CreateLevelEntry(level, feature));
                return;
            }

            var features = (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>()).ToList();
            if (features
                .All(existing => existing == null || existing.AssetGuid != feature.AssetGuid))
            {
                features.Add(feature);
                entry.SetFeatures(features);
            }
        }
    }
}
