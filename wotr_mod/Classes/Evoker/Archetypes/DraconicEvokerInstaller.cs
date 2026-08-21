using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Evoker.Archetypes
{
    internal sealed class DraconicEvokerInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly EvokerInstaller _evoker;

        public DraconicEvokerInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            EvokerInstaller evoker)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _evoker = evoker;
        }

        public BlueprintArchetype Ensure(BlueprintCharacterClass characterClass)
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
            var draconicBloodlineSelection = _evoker.EnsureDraconicEvokerBloodlineSelection(characterClass);
            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusMedium,
                "Draconic Evoker base attack bonus progression");
            var weaponFocusClaw = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.WeaponFocusClaw,
                "Weapon Focus (Claw)");
            var mediumArmorProficiency = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.ArmorProficiencyMedium,
                "Medium Armor Proficiency");
            var heavyArmorProficiency = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.ArmorProficiencyHeavy,
                "Heavy Armor Proficiency");
            var lightArcaneArmorProficiency = EnsureArcaneArmorProficiency(
                ModBlueprintIds.Features.DraconicEvokerArcaneArmorProficiency,
                "WotrMod_DraconicEvokerLightArcaneArmorProficiency",
                "$ArcaneArmorProficiency$DraconicEvokerLightArmor",
                ArmorProficiencyGroup.Light,
                characterClass);
            var mediumArcaneArmorProficiency = EnsureArcaneArmorProficiency(
                ModBlueprintIds.Features.DraconicEvokerMediumArcaneArmorProficiency,
                "WotrMod_DraconicEvokerMediumArcaneArmorProficiency",
                "$ArcaneArmorProficiency$DraconicEvokerMediumArmor",
                ArmorProficiencyGroup.Medium,
                characterClass);
            var heavyArcaneArmorProficiency = EnsureArcaneArmorProficiency(
                ModBlueprintIds.Features.DraconicEvokerHeavyArcaneArmorProficiency,
                "WotrMod_DraconicEvokerHeavyArcaneArmorProficiency",
                "$ArcaneArmorProficiency$DraconicEvokerHeavyArmor",
                ArmorProficiencyGroup.Heavy,
                characterClass);

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
                        lightArcaneArmorProficiency),
                    CreateLevelEntry(
                        4,
                        mediumArmorProficiency,
                        mediumArcaneArmorProficiency),
                    CreateLevelEntry(
                        9,
                        heavyArmorProficiency,
                        heavyArcaneArmorProficiency)
                },
                new[]
                {
                    CreateLevelEntry(1, evokerBloodlineSelection)
                });
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private BlueprintFeature EnsureArcaneArmorProficiency(
            string featureGuid,
            string internalName,
            string componentName,
            ArmorProficiencyGroup armorGroup,
            BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = internalName,
                    AssetGuid = BlueprintGuid.Parse(featureGuid),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            feature.name = internalName;
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
            clonedComponent.name = componentName;
            if (clonedComponent is ArcaneArmorProficiency armorProficiency)
            {
                armorProficiency.Armor = new[] { armorGroup };
            }

            _blueprints.SetComponents(feature, clonedComponent);
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }
    }
}
