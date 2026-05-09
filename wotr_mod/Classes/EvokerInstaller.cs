using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Classes
{
    internal sealed class EvokerInstaller : IClassContentInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

        public EvokerInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            SpellIconLoader icons)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _icons = icons;
        }

        public bool CanInstall(CharacterClassDefinition definition)
        {
            return definition.UseEvokerBloodlines;
        }

        public void RegisterLocalization()
        {
        }

        public void ConfigureSpellList(CharacterClassDefinition definition, BlueprintSpellList spellList)
        {
            ConfigureEvokerSpellList(spellList);
        }

        public BlueprintFeatureBase EnsureProgressionFeature(CharacterClassDefinition definition)
        {
            return EnsureEvokerBloodlineSelection();
        }

        public void ConfigureProgression(CharacterClassDefinition definition, BlueprintProgression progression)
        {
            if (definition.UseUndeadBloodline)
            {
                AddUndeadBloodline(progression);
            }
        }

        public void Install(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            EnsureEvokerBloodlineSelection(characterClass);
            EnsureEvocationSpellFocusRecommendation(characterClass);
            _blueprints.SetCharacterClassArchetypes(characterClass);
            _blueprints.AddFeatureToLevel(
                characterClass.Progression,
                1,
                EnsureSpellShapingFeature(characterClass));

            EnsureShadowbornBloodline(characterClass);
            new EvokerScalingInstaller(_blueprints, _localization, _logger, _icons).Install(characterClass);
            _blueprints.SetCharacterClassArchetypes(
                characterClass,
                EnsureArchetypes(definition, characterClass, spellbook, spellList));
        }

        private BlueprintFeature EnsureSpellShapingFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.EvokerSpellShaping);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_EvokerSpellShaping",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.EvokerSpellShaping),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.EvokerSpellShaping, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            _blueprints.SetProgressionClasses(feature, characterClass);
            _blueprints.SetComponents(
                feature,
                new EvokerSpellShaping
                {
                    name = "$EvokerSpellShaping$Evoker",
                    Classes = new[] { characterClass }
                });
            return feature;
        }

        private void EnsureEvocationSpellFocusRecommendation(BlueprintCharacterClass characterClass)
        {
            var spellFocus = _blueprints.Require<BlueprintParametrizedFeature>(
                GameBlueprintIds.Features.SpellFocus,
                "Spell Focus");
            var recommendation = _blueprints.GetComponents<SpellFocusSchoolRecommendation>(spellFocus)
                .FirstOrDefault();

            if (recommendation == null)
            {
                recommendation = new SpellFocusSchoolRecommendation
                {
                    name = "$SpellFocusSchoolRecommendation$ClassSchools"
                };
                _blueprints.AddComponent(spellFocus, recommendation);
            }

            recommendation.AddRecommendedClass(characterClass, SpellSchool.Evocation);
        }

        private void ConfigureEvokerSpellList(BlueprintSpellList spellList)
        {
            var spellsByLevel = EvokerSpellRegistry.GetAll()
                .Select(definition =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(definition.SpellGuid, definition.DisplayName);
                    return new KeyValuePair<BlueprintAbility, int>(spell, definition.SpellLevel);
                });

            _blueprints.SetSpellListSpells(
                spellList,
                spellsByLevel.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key.name));
        }

        private BlueprintFeatureSelection EnsureEvokerBloodlineSelection(BlueprintCharacterClass characterClass = null)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.EvokerBloodline);
            if (selection == null)
            {
                var donorSelection = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBloodline,
                    "Sorcerer bloodline selection");
                selection = _blueprints.CloneBlueprint(
                    donorSelection,
                    ModBlueprintIds.Selections.EvokerBloodline,
                    "WotrMod_EvokerBloodlineSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.EvokerBloodline, selection);
            }

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
                    LocalizationIds.Mod.EvokerAirName, LocalizationIds.Mod.EvokerAirDescription,
                    GameBlueprintIds.Features.BloodlineElementalAirArcana,
                    GameBlueprintIds.Abilities.BloodlineElementalAirArcanaAbility,
                    GameBlueprintIds.Buffs.BloodlineElementalAirArcanaBuff,
                    ModBlueprintIds.Features.EvokerAirArcana,
                    ModBlueprintIds.Abilities.EvokerAirArcana,
                    ModBlueprintIds.Buffs.EvokerAirArcana,
                    "WotrMod_EvokerAirArcanaFeature",
                    "WotrMod_EvokerAirArcanaAbility",
                    "WotrMod_EvokerAirArcanaBuff",
                    SpellEffectTheme.Electric),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalEarthBloodline,
                    ModBlueprintIds.Progressions.EvokerEarthBloodline, "WotrMod_EvokerBloodline_Earth",
                    LocalizationIds.Mod.EvokerEarthName, LocalizationIds.Mod.EvokerEarthDescription,
                    GameBlueprintIds.Features.BloodlineElementalEarthArcana,
                    GameBlueprintIds.Abilities.BloodlineElementalEarthArcanaAbility,
                    GameBlueprintIds.Buffs.BloodlineElementalEarthArcanaBuff,
                    ModBlueprintIds.Features.EvokerEarthArcana,
                    ModBlueprintIds.Abilities.EvokerEarthArcana,
                    ModBlueprintIds.Buffs.EvokerEarthArcana,
                    "WotrMod_EvokerEarthArcanaFeature",
                    "WotrMod_EvokerEarthArcanaAbility",
                    "WotrMod_EvokerEarthArcanaBuff",
                    SpellEffectTheme.Acid),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalFireBloodline,
                    ModBlueprintIds.Progressions.EvokerFireBloodline, "WotrMod_EvokerBloodline_Fire",
                    LocalizationIds.Mod.EvokerFireName, LocalizationIds.Mod.EvokerFireDescription,
                    GameBlueprintIds.Features.BloodlineElementalFireArcana,
                    GameBlueprintIds.Abilities.BloodlineElementalFireArcanaAbility,
                    GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff,
                    ModBlueprintIds.Features.EvokerFireArcana,
                    ModBlueprintIds.Abilities.EvokerFireArcana,
                    ModBlueprintIds.Buffs.EvokerFireArcana,
                    "WotrMod_EvokerFireArcanaFeature",
                    "WotrMod_EvokerFireArcanaAbility",
                    "WotrMod_EvokerFireArcanaBuff",
                    SpellEffectTheme.Fire),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalWaterBloodline,
                    ModBlueprintIds.Progressions.EvokerWaterBloodline, "WotrMod_EvokerBloodline_Water",
                    LocalizationIds.Mod.EvokerWaterName, LocalizationIds.Mod.EvokerWaterDescription,
                    GameBlueprintIds.Features.BloodlineElementalWaterArcana,
                    GameBlueprintIds.Abilities.BloodlineElementalWaterArcanaAbility,
                    GameBlueprintIds.Buffs.BloodlineElementalWaterArcanaBuff,
                    ModBlueprintIds.Features.EvokerWaterArcana,
                    ModBlueprintIds.Abilities.EvokerWaterArcana,
                    ModBlueprintIds.Buffs.EvokerWaterArcana,
                    "WotrMod_EvokerWaterArcanaFeature",
                    "WotrMod_EvokerWaterArcanaAbility",
                    "WotrMod_EvokerWaterArcanaBuff",
                    SpellEffectTheme.Cold)
            };

            _blueprints.SetFeatureSelectionFeatures(selection, bloodlines);
            _blueprints.SetFeatureSelectionAllFeatures(selection, bloodlines);

            if (characterClass != null)
            {
                foreach (var bloodline in bloodlines)
                {
                    _blueprints.SetProgressionClasses(bloodline, characterClass);
                }

                _blueprints.SetProgressionClasses(selection, characterClass);
            }

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
            if (existing != null)
            {
                return existing;
            }

            var donor = _blueprints.Require<BlueprintProgression>(donorGuid, internalName + " donor");
            var clone = _blueprints.CloneBlueprint(donor, newGuid, internalName);
            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _blueprints.AddCachedBlueprint(newGuid, clone);
            return clone;
        }

        private BlueprintProgression EnsureEvokerBloodline(
            string donorGuid,
            string newGuid,
            string internalName,
            string displayNameKey,
            string descriptionKey,
            string sourceArcanaFeatureGuid,
            string sourceArcanaAbilityGuid,
            string sourceArcanaBuffGuid,
            string arcanaFeatureGuid,
            string arcanaAbilityGuid,
            string arcanaBuffGuid,
            string arcanaFeatureName,
            string arcanaAbilityName,
            string arcanaBuffName,
            SpellEffectTheme theme)
        {
            var progression = EnsureEvokerBloodline(
                donorGuid,
                newGuid,
                internalName,
                displayNameKey,
                descriptionKey);
            var arcana = EnsureEvokerElementalArcanaFeature(
                sourceArcanaFeatureGuid,
                sourceArcanaAbilityGuid,
                sourceArcanaBuffGuid,
                arcanaFeatureGuid,
                arcanaAbilityGuid,
                arcanaBuffGuid,
                arcanaFeatureName,
                arcanaAbilityName,
                arcanaBuffName,
                theme);
            ReplaceProgressionFeature(progression, sourceArcanaFeatureGuid, arcana);
            return progression;
        }

        private BlueprintFeature EnsureEvokerElementalArcanaFeature(
            string sourceFeatureGuid,
            string sourceAbilityGuid,
            string sourceBuffGuid,
            string featureGuid,
            string abilityGuid,
            string buffGuid,
            string featureName,
            string abilityName,
            string buffName,
            SpellEffectTheme theme)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(sourceFeatureGuid, featureName + " donor");
                feature = _blueprints.CloneBlueprint(source, featureGuid, featureName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            var ability = EnsureEvokerElementalArcanaAbility(
                sourceAbilityGuid,
                sourceBuffGuid,
                abilityGuid,
                buffGuid,
                abilityName,
                buffName,
                theme);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            return feature;
        }

        private BlueprintActivatableAbility EnsureEvokerElementalArcanaAbility(
            string sourceAbilityGuid,
            string sourceBuffGuid,
            string abilityGuid,
            string buffGuid,
            string abilityName,
            string buffName,
            SpellEffectTheme theme)
        {
            var ability = _blueprints.Get<BlueprintActivatableAbility>(abilityGuid);
            if (ability == null)
            {
                var source = _blueprints.Require<BlueprintActivatableAbility>(sourceAbilityGuid, abilityName + " donor");
                ability = _blueprints.CloneBlueprint(source, abilityGuid, abilityName);
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }

            var buff = EnsureEvokerElementalArcanaBuff(sourceBuffGuid, buffGuid, buffName, theme);
            ReplaceBuffReferences(ability, sourceBuffGuid, buff);
            return ability;
        }

        private BlueprintBuff EnsureEvokerElementalArcanaBuff(
            string sourceBuffGuid,
            string buffGuid,
            string buffName,
            SpellEffectTheme theme)
        {
            var buff = _blueprints.Get<BlueprintBuff>(buffGuid);
            if (buff == null)
            {
                var source = _blueprints.Require<BlueprintBuff>(sourceBuffGuid, buffName + " donor");
                buff = _blueprints.CloneBlueprint(source, buffGuid, buffName);
                _blueprints.AddCachedBlueprint(buffGuid, buff);
            }

            var themeToggle = _blueprints.GetComponents<SpellEffectThemeToggleComponent>(buff).FirstOrDefault();
            if (themeToggle == null)
            {
                themeToggle = new SpellEffectThemeToggleComponent
                {
                    name = "$SpellEffectThemeToggleComponent$" + buffName
                };
                _blueprints.AddComponent(buff, themeToggle);
            }

            themeToggle.Theme = theme;
            return buff;
        }

        private BlueprintFeatureSelection EnsureDraconicEvokerBloodlineSelection(BlueprintCharacterClass characterClass = null)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.DraconicEvokerBloodline);
            if (selection == null)
            {
                var donorSelection = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBloodline,
                    "Sorcerer bloodline selection");
                selection = _blueprints.CloneBlueprint(
                    donorSelection,
                    ModBlueprintIds.Selections.DraconicEvokerBloodline,
                    "WotrMod_DraconicEvokerBloodlineSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.DraconicEvokerBloodline, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBloodlineName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBloodlineDescription));

            var bloodlines = new[]
            {
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.BlackDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBlackDragonBloodline,
                    "WotrMod_EvokerBloodline_BlackDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.BlueDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBlueDragonBloodline,
                    "WotrMod_EvokerBloodline_BlueDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.BrassDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBrassDragonBloodline,
                    "WotrMod_EvokerBloodline_BrassDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.BronzeDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBronzeDragonBloodline,
                    "WotrMod_EvokerBloodline_BronzeDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.CopperDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerCopperDragonBloodline,
                    "WotrMod_EvokerBloodline_CopperDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.GoldDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerGoldDragonBloodline,
                    "WotrMod_EvokerBloodline_GoldDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.GreenDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerGreenDragonBloodline,
                    "WotrMod_EvokerBloodline_GreenDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.RedDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerRedDragonBloodline,
                    "WotrMod_EvokerBloodline_RedDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.SilverDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerSilverDragonBloodline,
                    "WotrMod_EvokerBloodline_SilverDragon"),
                EnsureEvokerDragonBloodline(GameBlueprintIds.Progressions.WhiteDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerWhiteDragonBloodline,
                    "WotrMod_EvokerBloodline_WhiteDragon")
            };

            _blueprints.SetFeatureSelectionFeatures(selection, bloodlines);
            _blueprints.SetFeatureSelectionAllFeatures(selection, bloodlines);

            if (characterClass != null)
            {
                foreach (var bloodline in bloodlines)
                {
                    _blueprints.SetProgressionClasses(bloodline, characterClass);
                }

                _blueprints.SetProgressionClasses(selection, characterClass);
            }

            return selection;
        }

        private BlueprintProgression EnsureEvokerDragonBloodline(
            string donorGuid,
            string newGuid,
            string internalName)
        {
            var donor = _blueprints.Require<BlueprintProgression>(donorGuid, internalName + " donor");
            var bloodline = _blueprints.Get<BlueprintProgression>(newGuid);
            if (bloodline == null)
            {
                bloodline = _blueprints.CloneBlueprint(donor, newGuid, internalName);
                _blueprints.AddCachedBlueprint(newGuid, bloodline);
            }

            _blueprints.CopyUnitFactDisplay(bloodline, donor);
            return bloodline;
        }

        private void AddUndeadBloodline(BlueprintProgression progression)
        {
            var undeadBloodline = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.UndeadBloodline,
                "Undead bloodline progression");

            var firstLevelEntry = progression.LevelEntries.FirstOrDefault(e => e.Level == 1);
            if (firstLevelEntry != null)
            {
                var features = firstLevelEntry.Features.ToList();
                features.Add(undeadBloodline);
                firstLevelEntry.SetFeatures(features);
            }
        }

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

        private BlueprintProgression EnsureShadowbornBloodline(BlueprintCharacterClass characterClass)
        {
            var bloodline = _blueprints.Get<BlueprintProgression>(ModBlueprintIds.Progressions.ShadowbornBloodline);
            if (bloodline == null)
            {
                var donor = _blueprints.Require<BlueprintProgression>(
                    ModBlueprintIds.Progressions.EvokerFireBloodline,
                    "Evoker Fire bloodline");
                bloodline = _blueprints.CloneBlueprint(
                    donor,
                    ModBlueprintIds.Progressions.ShadowbornBloodline,
                    "WotrMod_ShadowbornBloodline");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Progressions.ShadowbornBloodline, bloodline);
            }

            var umbralRay = EnsureShadowbornDamageFeature(
                GameBlueprintIds.Features.BloodlineElementalFireElementalRayFeature,
                GameBlueprintIds.Abilities.BloodlineElementalFireElementalRayAbility,
                ModBlueprintIds.Features.ShadowbornUmbralRay,
                ModBlueprintIds.Abilities.ShadowbornUmbralRay,
                "WotrMod_ShadowbornUmbralRayFeature",
                "WotrMod_ShadowbornUmbralRayAbility",
                LocalizationIds.Mod.ShadowbornUmbralRayName,
                LocalizationIds.Mod.ShadowbornUmbralRayDescription,
                characterClass,
                "Icons\\umbral_ray.png");
            var umbralBlast = EnsureShadowbornDamageFeature(
                GameBlueprintIds.Features.BloodlineElementalFireElementalBlastFeature,
                GameBlueprintIds.Abilities.BloodlineElementalFireElementalBlastAbility,
                ModBlueprintIds.Features.ShadowbornUmbralBlast,
                ModBlueprintIds.Abilities.ShadowbornUmbralBlast,
                "WotrMod_ShadowbornUmbralBlastFeature",
                "WotrMod_ShadowbornUmbralBlastAbility",
                LocalizationIds.Mod.ShadowbornUmbralBlastName,
                LocalizationIds.Mod.ShadowbornUmbralBlastDescription,
                characterClass,
                "Icons\\umbral_blast.png");
            var resistance = EnsureShadowbornResistanceFeature(characterClass);
            var elementalBody = EnsureShadowbornElementalBodyFeature();
            var arcana = EnsureShadowbornArcanaFeature(characterClass);
            var shadowHands = EnsureShadowbornKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel1,
                GameBlueprintIds.Spells.BurningHands,
                ModBlueprintIds.Features.ShadowbornBurningHandsKnownSpell,
                ModBlueprintIds.Spells.ShadowbornBurningHands,
                "WotrMod_ShadowbornBurningHandsKnownSpell",
                "WotrMod_ShadowHandsSpell",
                LocalizationIds.Mod.ShadowbornBurningHandsName,
                LocalizationIds.Mod.ShadowbornBurningHandsDescription,
                1,
                "Icons\\shadow_hands.png");
            var shadowRay = EnsureShadowbornKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel2,
                GameBlueprintIds.Spells.ScorchingRay,
                ModBlueprintIds.Features.ShadowbornScorchingRayKnownSpell,
                ModBlueprintIds.Spells.ShadowbornScorchingRay,
                "WotrMod_ShadowbornScorchingRayKnownSpell",
                "WotrMod_ShadowRaySpell",
                LocalizationIds.Mod.ShadowbornScorchingRayName,
                LocalizationIds.Mod.ShadowbornScorchingRayDescription,
                2,
                "Icons\\shadow_ray.png");

            _blueprints.SetUnitFactDisplay(
                bloodline,
                _localization.Text(LocalizationIds.Mod.ShadowbornBloodlineName),
                _localization.Text(LocalizationIds.Mod.ShadowbornBloodlineDescription));
            bloodline.HideInUI = true;
            bloodline.HideInCharacterSheetAndLevelUp = true;
            bloodline.HideNotAvailibleInUI = true;
            SetIcon(bloodline, "Icons\\shadowborn_bloodline.png");
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireArcana,
                arcana);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalRayFeature,
                umbralRay);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBlastFeature,
                umbralBlast);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalSpellLevel9,
                elementalBody);
            RemoveProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBodyFeature);
            RemoveProgressionFeatureExceptLevel(
                bloodline,
                elementalBody,
                19);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireResistanceFeature,
                resistance);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel1,
                shadowHands);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel2,
                shadowRay);
            new LivingDarknessInstaller(_blueprints, _localization, _logger, _icons).Install(bloodline, characterClass);
            SetProgressionClassesForLevelEntryFeatures(bloodline, characterClass);

            return bloodline;
        }

        private BlueprintFeatureSelection EnsureShadowbornBonusFeatSelection(BlueprintCharacterClass characterClass)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.ShadowbornBonusFeat);
            if (selection == null)
            {
                var donor = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBonusFeat,
                    "Sorcerer Bonus Feat");
                selection = _blueprints.CloneBlueprint(
                    donor,
                    ModBlueprintIds.Selections.ShadowbornBonusFeat,
                    "WotrMod_ShadowbornBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.ShadowbornBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.ShadowbornBonusFeatName),
                _localization.Text(LocalizationIds.Mod.ShadowbornBonusFeatDescription));
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(selection, characterClass);
            }

            return selection;
        }

        private BlueprintFeature EnsureShadowbornLivingGhostFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.ShadowbornLivingGhost);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_ShadowbornLivingGhostFeature",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.ShadowbornLivingGhost),
                    IsClassFeature = true,
                    Ranks = 1,
                    ReapplyOnLevelUp = false
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ShadowbornLivingGhost, feature);
            }

            var addFacts = new AddFacts { name = "$AddFacts$ShadowbornLivingGhostFeature" };
            _blueprints.SetAddFacts(addFacts, EnsureShadowbornLivingGhostAbility());
            _blueprints.SetComponents(feature, addFacts);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostName),
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostDescription));
            SetIcon(feature, "Icons\\living_ghost.png");
            _blueprints.SetProgressionClasses(feature, characterClass);

            return feature;
        }

        private BlueprintActivatableAbility EnsureShadowbornLivingGhostAbility()
        {
            var ability = _blueprints.Get<BlueprintActivatableAbility>(ModBlueprintIds.Abilities.ShadowbornLivingGhost);
            if (ability == null)
            {
                var source = _blueprints.Require<BlueprintActivatableAbility>(
                    GameBlueprintIds.Abilities.BloodlineElementalFireArcanaAbility,
                    "Fire bloodline arcana ability donor");
                ability = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Abilities.ShadowbornLivingGhost,
                    "WotrMod_ShadowbornLivingGhostAbility");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.ShadowbornLivingGhost, ability);
            }

            var buff = EnsureShadowbornLivingGhostBuff();
            ReplaceBuffReferences(ability, GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff, buff);
            _blueprints.SetComponents(ability);
            _blueprints.SetUnitFactDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostName),
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostDescription));
            SetIcon(ability, "Icons\\living_ghost.png");

            return ability;
        }

        private BlueprintBuff EnsureShadowbornLivingGhostBuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ShadowbornLivingGhost);
            if (buff == null)
            {
                var source = _blueprints.Require<BlueprintBuff>(
                    GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff,
                    "Fire bloodline arcana buff donor");
                buff = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Buffs.ShadowbornLivingGhost,
                    "WotrMod_ShadowbornLivingGhostBuff");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ShadowbornLivingGhost, buff);
            }

            var incorporeal = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.Incorporeal,
                "Incorporeal creature feature");
            var addFacts = new AddFacts { name = "$AddFacts$ShadowbornLivingGhostBuff" };
            _blueprints.SetAddFacts(addFacts, incorporeal);
            _blueprints.SetComponents(buff, addFacts);
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostName),
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostDescription));
            SetIcon(buff, "Icons\\living_ghost.png");

            return buff;
        }

        private void SetProgressionClassesForLevelEntryFeatures(
            BlueprintProgression progression,
            BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return;
            }

            var seen = new HashSet<BlueprintGuid>();
            foreach (var feature in (progression.LevelEntries ?? Array.Empty<LevelEntry>())
                         .SelectMany(entry => entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                         .Where(feature => feature != null))
            {
                if (seen.Add(feature.AssetGuid))
                {
                    _blueprints.SetProgressionClasses(feature, characterClass);
                }
            }
        }

        private BlueprintFeature EnsureShadowbornElementalBodyFeature()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.ShadowbornElementalBody);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_ShadowbornUmbralBodyFeature",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.ShadowbornElementalBody),
                    Ranks = 1,
                    IsClassFeature = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ShadowbornElementalBody, feature);
            }

            feature.Ranks = 1;
            feature.IsClassFeature = true;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornElementalBodyName),
                _localization.Text(LocalizationIds.Mod.ShadowbornElementalBodyDescription));
            _blueprints.SetComponents(
                feature,
                new AddEnergyDamageImmunity
                {
                    name = "$AddEnergyDamageImmunity$ShadowbornNegativeEnergyHealing",
                    EnergyType = DamageEnergyType.NegativeEnergy,
                    HealOnDamage = true
                },
                new ShadowbornNegativeEnergyHealing
                {
                    name = "$ShadowbornNegativeEnergyHealing$UndeadDoubleHealing",
                    UndeadType = _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.UndeadType,
                        "Undead type"),
                    ResistanceFeaturesToRemove = new[]
                    {
                        _blueprints.Require<BlueprintFeature>(
                            ModBlueprintIds.Features.ShadowbornResistanceLevel1,
                            "Shadowborn negative energy resistance 10"),
                        _blueprints.Require<BlueprintFeature>(
                            ModBlueprintIds.Features.ShadowbornResistanceLevel2,
                            "Shadowborn negative energy resistance 20")
                    }
                });
            SetIcon(feature, "Icons\\umbral_body.png");

            return feature;
        }

        private BlueprintFeature EnsureShadowbornResistanceFeature(BlueprintCharacterClass characterClass)
        {
            var level1 = EnsureShadowbornResistanceLevelFeature(
                GameBlueprintIds.Features.BloodlineElementalFireResistanceLevel1,
                ModBlueprintIds.Features.ShadowbornResistanceLevel1,
                "WotrMod_ShadowbornResistance10");
            var level2 = EnsureShadowbornResistanceLevelFeature(
                GameBlueprintIds.Features.BloodlineElementalFireResistanceLevel2,
                ModBlueprintIds.Features.ShadowbornResistanceLevel2,
                "WotrMod_ShadowbornResistance20");

            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.ShadowbornResistance);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.BloodlineElementalFireResistanceFeature,
                    "Fire bloodline resistance donor");
                feature = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Features.ShadowbornResistance,
                    "WotrMod_ShadowbornResistance");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ShadowbornResistance, feature);
            }

            foreach (var component in _blueprints.GetComponents<BlueprintComponent>(feature)
                         .Where(component => component.GetType().Name == "AddFeatureOnClassLevel"))
            {
                ConfigureAddFeatureOnClassLevel(component, characterClass, level1, level2);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceName),
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceDescription));
            SetIcon(feature, "Icons\\shadow_resistance.png");
            _blueprints.SetProgressionClasses(feature, characterClass);

            return feature;
        }

        private BlueprintFeature EnsureShadowbornResistanceLevelFeature(
            string sourceFeatureGuid,
            string featureGuid,
            string featureName)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(sourceFeatureGuid, featureName + " donor");
                feature = _blueprints.CloneBlueprint(source, featureGuid, featureName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            foreach (var resistance in _blueprints.GetComponents<AddDamageResistanceEnergy>(feature))
            {
                resistance.Type = DamageEnergyType.NegativeEnergy;
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceName),
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceDescription));
            SetIcon(feature, "Icons\\shadow_resistance.png");

            return feature;
        }

        private BlueprintFeature EnsureShadowbornArcanaFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.ShadowbornArcana);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.BloodlineElementalFireArcana,
                    "Fire bloodline arcana donor");
                feature = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Features.ShadowbornArcana,
                    "WotrMod_ShadowbornArcanaFeature");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ShadowbornArcana, feature);
            }

            var ability = EnsureShadowbornArcanaAbility();
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaName),
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaDescription));
            SetIcon(feature, "Icons\\umbral_arcana.png");
            _blueprints.SetProgressionClasses(feature, characterClass);

            return feature;
        }

        private BlueprintActivatableAbility EnsureShadowbornArcanaAbility()
        {
            var ability = _blueprints.Get<BlueprintActivatableAbility>(ModBlueprintIds.Abilities.ShadowbornArcana);
            if (ability == null)
            {
                var source = _blueprints.Require<BlueprintActivatableAbility>(
                    GameBlueprintIds.Abilities.BloodlineElementalFireArcanaAbility,
                    "Fire bloodline arcana ability donor");
                ability = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Abilities.ShadowbornArcana,
                    "WotrMod_ShadowbornArcanaAbility");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.ShadowbornArcana, ability);
            }

            var buff = EnsureShadowbornArcanaBuff();
            ReplaceBuffReferences(ability, GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff, buff);
            _blueprints.SetUnitFactDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaName),
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaDescription));
            SetIcon(ability, "Icons\\umbral_arcana.png");

            return ability;
        }

        private BlueprintBuff EnsureShadowbornArcanaBuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ShadowbornArcana);
            if (buff == null)
            {
                var source = _blueprints.Require<BlueprintBuff>(
                    GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff,
                    "Fire bloodline arcana buff donor");
                buff = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Buffs.ShadowbornArcana,
                    "WotrMod_ShadowbornArcanaBuff");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ShadowbornArcana, buff);
            }

            foreach (var oldChangeElement in _blueprints.GetComponents<ChangeSpellElementalDamage>(buff))
            {
                var newChangeElement = _blueprints.CloneComponent(oldChangeElement);
                newChangeElement.Element = DamageEnergyType.NegativeEnergy;
                _blueprints.ReplaceComponent(buff, oldChangeElement, newChangeElement);
            }

            ReplaceDescriptor(buff, SpellDescriptor.Fire, SpellDescriptor.Death);
            var themeToggle = _blueprints.GetComponents<SpellEffectThemeToggleComponent>(buff).FirstOrDefault();
            if (themeToggle == null)
            {
                themeToggle = new SpellEffectThemeToggleComponent
                {
                    name = "$SpellEffectThemeToggleComponent$ShadowbornArcana"
                };
                _blueprints.AddComponent(buff, themeToggle);
            }

            themeToggle.Theme = SpellEffectTheme.Shadow;
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaName),
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaDescription));
            SetIcon(buff, "Icons\\umbral_arcana.png");

            return buff;
        }

        private BlueprintFeature EnsureShadowbornKnownSpellFeature(
            string sourceFeatureGuid,
            string sourceSpellGuid,
            string featureGuid,
            string spellGuid,
            string featureName,
            string spellName,
            string displayNameKey,
            string descriptionKey,
            int spellLevel,
            string iconPath)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(sourceFeatureGuid, featureName + " donor");
                feature = _blueprints.CloneBlueprint(source, featureGuid, featureName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            var spell = EnsureShadowbornSpell(sourceSpellGuid, spellGuid, spellName, displayNameKey, descriptionKey, iconPath);
            var addKnownSpell = new AddKnownSpell { name = "$AddKnownSpell$" + featureName };
            var evokerClass = _blueprints.Require<BlueprintCharacterClass>(ModBlueprintIds.Classes.Evoker, "Evoker class");
            _blueprints.SetAddKnownSpell(addKnownSpell, evokerClass, spell, spellLevel);
            _blueprints.SetComponents(feature, addKnownSpell);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SetIcon(feature, iconPath);

            return feature;
        }

        private BlueprintAbility EnsureShadowbornSpell(
            string sourceSpellGuid,
            string spellGuid,
            string spellName,
            string displayNameKey,
            string descriptionKey,
            string iconPath)
        {
            var spell = _blueprints.Get<BlueprintAbility>(spellGuid);
            if (spell == null)
            {
                var source = _blueprints.Require<BlueprintAbility>(sourceSpellGuid, spellName + " donor");
                spell = _blueprints.CloneBlueprint(source, spellGuid, spellName);
                _blueprints.AddCachedBlueprint(spellGuid, spell);
            }

            _blueprints.SetAbilityDisplay(
                spell,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SetIcon(spell, iconPath);
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, _blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Fire, SpellDescriptor.Death, _blueprints);
            PatchFireDamageToNegativeEnergy(spell);
            ConfigureShadowbornSpellVisuals(spellGuid, spell);

            return spell;
        }

        private BlueprintFeature EnsureShadowbornDamageFeature(
            string sourceFeatureGuid,
            string sourceAbilityGuid,
            string featureGuid,
            string abilityGuid,
            string featureName,
            string abilityName,
            string displayNameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass,
            string iconPath)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(sourceFeatureGuid, featureName + " donor");
                feature = _blueprints.CloneBlueprint(source, featureGuid, featureName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            var ability = EnsureShadowbornDamageAbility(
                sourceAbilityGuid,
                abilityGuid,
                abilityName,
                displayNameKey,
                descriptionKey,
                characterClass,
                iconPath);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            ReplaceAbilityReferences(feature, sourceAbilityGuid, ability);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SetIcon(feature, iconPath);

            return feature;
        }

        private BlueprintAbility EnsureShadowbornDamageAbility(
            string sourceAbilityGuid,
            string abilityGuid,
            string abilityName,
            string displayNameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass,
            string iconPath)
        {
            var ability = _blueprints.Get<BlueprintAbility>(abilityGuid);
            if (ability == null)
            {
                var source = _blueprints.Require<BlueprintAbility>(sourceAbilityGuid, abilityName + " donor");
                ability = _blueprints.CloneBlueprint(source, abilityGuid, abilityName);
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }

            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SetIcon(ability, iconPath);
            SpellModifierUtility.ReplaceDescriptor(ability, SpellDescriptor.Fire, SpellDescriptor.Death, _blueprints);
            BindAbilityRankConfigsToClass(ability, characterClass);
            PatchFireDamageToNegativeEnergy(ability);
            ConfigureShadowbornDamageVisuals(abilityGuid, ability);

            return ability;
        }

        private static readonly FieldInfo CasterAppearProjectileField =
            AccessTools.Field(typeof(BlueprintProjectile), "m_CasterAppearProjectile");

        private void ConfigureShadowbornDamageVisuals(string abilityGuid, BlueprintAbility ability)
        {
            if (abilityGuid != ModBlueprintIds.Abilities.ShadowbornUmbralRay &&
                abilityGuid != ModBlueprintIds.Abilities.ShadowbornUmbralBlast)
            {
                return;
            }

            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Shadow);

            var projectile = abilityGuid == ModBlueprintIds.Abilities.ShadowbornUmbralRay
                ? EnsureShadowbornUmbralRayProjectile()
                : EnsureShadowbornProjectile(
                    ability,
                    ModBlueprintIds.Projectiles.ShadowbornUmbralBlast,
                    "WotrMod_ShadowbornUmbralBlastProjectile");
            if (projectile == null) return;

            ApplyShadowProjectileVisuals(ability, projectile);
            if (abilityGuid == ModBlueprintIds.Abilities.ShadowbornUmbralRay)
            {
                RegisterCasterAppearTint(projectile);
            }

            ability.OnEnable();
        }

        private void ConfigureShadowbornSpellVisuals(string spellGuid, BlueprintAbility spell)
        {
            if (spellGuid != ModBlueprintIds.Spells.ShadowbornBurningHands &&
                spellGuid != ModBlueprintIds.Spells.ShadowbornScorchingRay)
            {
                return;
            }

            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                spell.AssetGuid.ToString(),
                SpellEffectTheme.Shadow);

            if (spellGuid != ModBlueprintIds.Spells.ShadowbornScorchingRay)
            {
                return;
            }

            var projectile = EnsureShadowbornProjectile(
                spell,
                ModBlueprintIds.Projectiles.ShadowbornScorchingRay,
                "WotrMod_ShadowbornScorchingRayProjectile");
            if (projectile == null) return;

            ApplyShadowProjectileVisuals(spell, projectile);
            spell.OnEnable();
        }

        private void ApplyShadowProjectileVisuals(BlueprintAbility ability, BlueprintProjectile projectile)
        {
            SpellEffectTintRegistry.RegisterProjectileTint(
                projectile.AssetGuid.ToString(),
                SpellEffectTheme.Shadow);

            foreach (var delivery in _blueprints.GetComponents<AbilityDeliverProjectile>(ability))
            {
                _blueprints.SetAbilityDeliverProjectiles(delivery, projectile);
            }
        }

        private static void RegisterCasterAppearTint(BlueprintProjectile projectile)
        {
            if (CasterAppearProjectileField == null)
            {
                return;
            }

            var reference = CasterAppearProjectileField.GetValue(projectile) as BlueprintProjectileReference;
            var casterAppear = reference?.Get() as BlueprintProjectile;
            if (casterAppear != null)
            {
                SpellEffectTintRegistry.RegisterProjectileTint(
                    casterAppear.AssetGuid.ToString(),
                    SpellEffectTheme.Shadow);
            }
        }

        private BlueprintProjectile EnsureShadowbornUmbralRayProjectile()
        {
            var projectile = _blueprints.Get<BlueprintProjectile>(ModBlueprintIds.Projectiles.ShadowbornUmbralRay);
            if (projectile != null)
            {
                return projectile;
            }

            var donor = _blueprints.Require<BlueprintProjectile>(
                GameBlueprintIds.Projectiles.Enervation,
                "Enervation projectile donor");
            projectile = _blueprints.CloneBlueprint(
                donor,
                ModBlueprintIds.Projectiles.ShadowbornUmbralRay,
                "WotrMod_ShadowbornUmbralRayProjectile");
            projectile.OnEnable();
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Projectiles.ShadowbornUmbralRay, projectile);

            return projectile;
        }

        private BlueprintProjectile EnsureShadowbornProjectile(
            BlueprintAbility ability,
            string projectileGuid,
            string projectileName)
        {
            var projectile = _blueprints.Get<BlueprintProjectile>(projectileGuid);
            if (projectile != null)
            {
                return projectile;
            }

            var delivery = _blueprints.GetComponents<AbilityDeliverProjectile>(ability).FirstOrDefault();
            var projectileRefs = delivery != null
                ? BlueprintFields.AbilityDeliverProjectileProjectiles.GetValue(delivery) as BlueprintProjectileReference[]
                : null;
            var donor = projectileRefs?.FirstOrDefault()?.Get() as BlueprintProjectile;
            if (donor == null)
            {
                return null;
            }

            projectile = _blueprints.CloneBlueprint(donor, projectileGuid, projectileName);
            projectile.OnEnable();
            _blueprints.AddCachedBlueprint(projectileGuid, projectile);

            return projectile;
        }

        private static void PatchFireDamageToNegativeEnergy(BlueprintAbility ability)
        {
            SpellModifierUtility.PatchRunActions(ability, action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage == null ||
                    damage.DamageType.Type != Kingmaker.RuleSystem.Rules.Damage.DamageType.Energy ||
                    damage.DamageType.Energy != DamageEnergyType.Fire)
                {
                    return 0;
                }

                damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.NegativeEnergy);
                return 1;
            });
        }

        private void ReplaceDescriptor(BlueprintScriptableObject blueprint, SpellDescriptor remove, SpellDescriptor add)
        {
            foreach (var oldDescriptor in _blueprints.GetComponents<SpellDescriptorComponent>(blueprint))
            {
                var newDescriptor = new SpellDescriptorComponent
                {
                    Descriptor = oldDescriptor.Descriptor
                };
                newDescriptor.Descriptor &= ~remove;
                newDescriptor.Descriptor |= add;
                _blueprints.ReplaceComponent(blueprint, oldDescriptor, newDescriptor);
            }
        }

        private void SetIcon(BlueprintUnitFact fact, string iconPath)
        {
            var icon = _icons.Load(iconPath);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(fact, icon);
            }
        }

        private static void ReplaceBuffReferences(
            BlueprintScriptableObject blueprint,
            string oldBuffGuid,
            BlueprintBuff newBuff)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldBuffGuid));
            foreach (var component in blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                ReplaceBuffReferencesInObject(component, oldGuid, newBuff);
            }

            ReplaceBuffReferencesInObject(blueprint, oldGuid, newBuff);
        }

        private static void ReplaceBuffReferencesInObject(object instance, BlueprintGuid oldGuid, BlueprintBuff newBuff)
        {
            foreach (var field in GetInstanceFields(instance.GetType()))
            {
                if (field.FieldType == typeof(BlueprintBuffReference))
                {
                    var reference = (BlueprintBuffReference)field.GetValue(instance);
                    if (reference != null && reference.Get()?.AssetGuid == oldGuid)
                    {
                        field.SetValue(
                            instance,
                            BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(newBuff));
                    }
                }
                else if (field.FieldType == typeof(BlueprintBuffReference[]))
                {
                    var references = (BlueprintBuffReference[])field.GetValue(instance);
                    if (references == null || !references.Any(reference => reference != null && reference.Get()?.AssetGuid == oldGuid))
                    {
                        continue;
                    }

                    field.SetValue(
                        instance,
                        references
                            .Select(reference => reference != null && reference.Get()?.AssetGuid == oldGuid
                                ? BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(newBuff)
                                : reference)
                            .ToArray());
                }
            }
        }

        private static void ConfigureAddFeatureOnClassLevel(
            BlueprintComponent component,
            BlueprintCharacterClass characterClass,
            BlueprintFeature level1Feature,
            BlueprintFeature level2Feature)
        {
            var featureField = FindField(component.GetType(), "m_Feature");
            var beforeThisLevelField = FindField(component.GetType(), "BeforeThisLevel");
            var feature = beforeThisLevelField?.GetValue(component) is bool beforeThisLevel && beforeThisLevel
                ? level1Feature
                : level2Feature;

            featureField?.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));

            FindField(component.GetType(), "m_Class")?.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
            FindField(component.GetType(), "m_AdditionalClasses")?.SetValue(
                component,
                Array.Empty<BlueprintCharacterClassReference>());
            FindField(component.GetType(), "m_Archetypes")?.SetValue(
                component,
                Array.Empty<BlueprintArchetypeReference>());
        }

        private void BindAbilityRankConfigsToClass(BlueprintAbility ability, BlueprintCharacterClass characterClass)
        {
            foreach (var oldConfig in _blueprints.GetComponents<ContextRankConfig>(ability))
            {
                var newConfig = _blueprints.CloneComponent(oldConfig);
                var reference = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass);
                if (BlueprintFields.ContextRankConfigClass.FieldType.IsArray)
                {
                    BlueprintFields.ContextRankConfigClass.SetValue(newConfig, new[] { reference });
                }
                else
                {
                    BlueprintFields.ContextRankConfigClass.SetValue(newConfig, reference);
                }

                BlueprintFields.ContextRankConfigBaseValueType?.SetValue(
                    newConfig,
                    ContextRankBaseValueType.ClassLevel);
                BlueprintFields.ContextRankConfigArchetype?.SetValue(newConfig, null);
                BlueprintFields.ContextRankConfigAdditionalArchetypes?.SetValue(
                    newConfig,
                    Array.Empty<BlueprintArchetypeReference>());
                _blueprints.ReplaceComponent(ability, oldConfig, newConfig);
            }
        }

        private static void ReplaceProgressionFeature(
            BlueprintProgression progression,
            string oldFeatureGuid,
            BlueprintFeatureBase newFeature)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldFeatureGuid));
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature => feature != null && feature.AssetGuid == oldGuid ? newFeature : feature));
            }
        }

        private static void RemoveProgressionFeature(
            BlueprintProgression progression,
            string featureGuid)
        {
            var guid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(featureGuid));
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Where(feature => feature == null || feature.AssetGuid != guid));
            }
        }

        private static void RemoveProgressionFeatureExceptLevel(
            BlueprintProgression progression,
            BlueprintFeatureBase featureToRemove,
            int levelToKeep)
        {
            if (featureToRemove == null)
            {
                return;
            }

            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                if (entry.Level == levelToKeep)
                {
                    continue;
                }

                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Where(feature => feature == null || feature.AssetGuid != featureToRemove.AssetGuid));
            }
        }

        private static void ReplaceAbilityReferences(
            BlueprintScriptableObject blueprint,
            string oldAbilityGuid,
            BlueprintAbility newAbility)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldAbilityGuid));
            foreach (var component in blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                foreach (var field in GetInstanceFields(component.GetType()))
                {
                    if (field.FieldType == typeof(BlueprintAbilityReference))
                    {
                        var reference = (BlueprintAbilityReference)field.GetValue(component);
                        if (ReferencesAbility(reference, oldGuid))
                        {
                            field.SetValue(
                                component,
                                BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(newAbility));
                        }
                    }
                    else if (field.FieldType == typeof(BlueprintAbilityReference[]))
                    {
                        var references = (BlueprintAbilityReference[])field.GetValue(component);
                        if (references == null || !references.Any(reference => ReferencesAbility(reference, oldGuid)))
                        {
                            continue;
                        }

                        field.SetValue(
                            component,
                            references
                                .Select(reference => ReferencesAbility(reference, oldGuid)
                                    ? BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(newAbility)
                                    : reference)
                                .ToArray());
                    }
                }
            }
        }

        private static bool ReferencesAbility(BlueprintAbilityReference reference, BlueprintGuid guid)
        {
            return reference != null && reference.Get()?.AssetGuid == guid;
        }

        private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                foreach (var field in current.GetFields(flags))
                {
                    yield return field;
                }
            }
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features.Where(feature => feature != null));
            return entry;
        }
    }
}
