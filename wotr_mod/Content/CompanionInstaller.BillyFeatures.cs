using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using wotr_mod.Features;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content
{
    internal sealed partial class CompanionInstaller
    {
        private BlueprintFeature EnsureBillyPositiveEnergyImmunity()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.BillyPositiveEnergyImmunity);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_BillyPositiveEnergyImmunity",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.BillyPositiveEnergyImmunity),
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true,
                    Ranks = 1,
                    IsClassFeature = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.BillyPositiveEnergyImmunity, feature);
            }

            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            feature.Ranks = 1;
            feature.IsClassFeature = true;
            _blueprints.SetComponents(
                feature,
                new AddEnergyDamageImmunity
                {
                    name = "$AddEnergyDamageImmunity$BillyPositiveEnergy",
                    EnergyType = DamageEnergyType.PositiveEnergy,
                    HealOnDamage = false
                });
            return feature;
        }

        private BlueprintFeature EnsureBillyFeatureList()
        {
            var featureList = GetOrClone<BlueprintFeature>(
                GameBlueprintIds.Units.CiarFeatureList,
                ModBlueprintIds.Features.BillyFeatureList,
                "WotrMod_BillyFeatureList",
                "Ciar feature list");

            var addClassLevels = featureList.ComponentsArray.OfType<AddClassLevels>().FirstOrDefault();
            if (addClassLevels == null)
            {
                throw new InvalidOperationException("Billy feature list does not have an AddClassLevels component.");
            }

            ConfigureBillyClassLevels(addClassLevels);
            ConfigureBillyClassSkills(featureList);
            ConfigureBillyLevelGrantedFeatures(featureList);
            featureList.HideInUI = true;
            featureList.HideInCharacterSheetAndLevelUp = true;
            return featureList;
        }

        private void ConfigureBillyLevelGrantedFeatures(BlueprintFeature featureList)
        {
            var characterClass = _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Cleric, "Cleric class");
            var archetype = _blueprints.Require<BlueprintArchetype>(
                GameBlueprintIds.Archetypes.PriestOfBalance,
                "Priest of Balance archetype");
            var zenArchery = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.ZenArcherZenArcheryFeature,
                "Zen Archery");
            var pointBlankMaster = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.PointBlankMasterLongbow,
                "Point Blank Master - Longbow");

            ConfigureGrantedFeatureRecommendation(pointBlankMaster, archetype);

            var components = (featureList.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(component => !IsBillyLevelGrantedFeature(component))
                .Concat(new BlueprintComponent[]
                {
                    CreateBillyLevelGrantedFeature(
                        "$AddFeatureOnClassLevel$BillyZenArcheryLevel4",
                        characterClass,
                        zenArchery,
                        4),
                    CreateBillyLevelGrantedFeature(
                        "$AddFeatureOnClassLevel$BillyPointBlankMasterLevel8",
                        characterClass,
                        pointBlankMaster,
                        8)
                })
                .ToArray();

            _blueprints.SetComponents(featureList, components);
        }

        private void ConfigureGrantedFeatureRecommendation(
            BlueprintFeature feature,
            BlueprintArchetype notRecommendedArchetype)
        {
            _blueprints.RemoveComponents<RecommendationRequiresSpellbook>(feature);
            var recommendation = _blueprints.EnsureComponent(
                feature,
                () => new GrantedFeatureRecommendation
                {
                    name = "$GrantedFeatureRecommendation$BillyPointBlankMaster"
                });
            recommendation.AddNotRecommendedArchetype(notRecommendedArchetype.AssetGuid.ToString());
        }

        private static bool IsBillyLevelGrantedFeature(BlueprintComponent component)
        {
            return component?.GetType().Name == "AddFeatureOnClassLevel" &&
                   (component.name == "$AddFeatureOnClassLevel$BillyZenArcheryLevel4" ||
                    component.name == "$AddFeatureOnClassLevel$BillyPointBlankMasterLevel8");
        }

        private static AddFeatureOnClassLevel CreateBillyLevelGrantedFeature(
            string componentName,
            BlueprintCharacterClass characterClass,
            BlueprintFeature feature,
            int level)
        {
            var component = new AddFeatureOnClassLevel
            {
                name = componentName,
                Level = level,
                BeforeThisLevel = false
            };
            SetField(
                component,
                "m_Class",
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
            SetField(
                component,
                "m_Feature",
                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));
            SetField(component, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>());
            SetField(component, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            return component;
        }

        private void ConfigureBillyClassSkills(BlueprintFeature featureList)
        {
            var components = (featureList.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(component => !(component is AddClassSkill) && !(component is SkillPointsPerCharacterLevel))
                .Concat(new BlueprintComponent[]
                {
                    CreateClassSkill(StatType.SkillPerception, "$AddClassSkill$BillyPerception"),
                    CreateClassSkill(StatType.SkillThievery, "$AddClassSkill$BillyThievery"),
                    CreateClassSkill(StatType.SkillLoreNature, "$AddClassSkill$BillyLoreNature"),
                    CreateBillyEcclesitheurgeSkillPointCorrection()
                })
                .ToArray();

            _blueprints.SetComponents(featureList, components);
        }

        private static SkillPointsPerCharacterLevel CreateBillyEcclesitheurgeSkillPointCorrection()
        {
            return new SkillPointsPerCharacterLevel
            {
                name = "$SkillPointsPerCharacterLevel$BillyEcclesitheurgeCorrection",
                SkillPointsPerLevel = -1
            };
        }

        private static AddClassSkill CreateClassSkill(StatType skill, string name)
        {
            return new AddClassSkill
            {
                name = name,
                Skill = skill
            };
        }

        private void ConfigureBillyClassLevels(AddClassLevels addClassLevels)
        {
            var cleric = _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Cleric, "Cleric class");
            var priestOfBalance = _blueprints.Require<BlueprintArchetype>(
                GameBlueprintIds.Archetypes.PriestOfBalance,
                "Priest of Balance archetype");

            SetField(
                addClassLevels,
                "m_CharacterClass",
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(cleric));
            SetField(
                addClassLevels,
                "m_Archetypes",
                new[]
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintArchetypeReference>(priestOfBalance)
                });
            addClassLevels.Levels = 1;
            addClassLevels.RaceStat = StatType.Wisdom;
            addClassLevels.LevelsStat = StatType.Wisdom;
            addClassLevels.Skills = new[]
            {
                StatType.SkillLoreReligion,
                StatType.SkillPerception,
                StatType.SkillThievery,
                StatType.SkillLoreNature
            };
            var protectionFromChaos = _blueprints.Require<BlueprintAbility>(
                GameBlueprintIds.Spells.ProtectionFromChaos,
                "Protection from Chaos spell");
            var cureLightWounds = _blueprints.Require<BlueprintAbility>(
                GameBlueprintIds.Spells.CureLightWounds,
                "Cure Light Wounds spell");
            SetField(addClassLevels, "m_SelectSpells", Array.Empty<BlueprintAbilityReference>());
            SetField(
                addClassLevels,
                "m_MemorizeSpells",
                new[]
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(protectionFromChaos),
                    BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(cureLightWounds),
                    BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(cureLightWounds),
                    BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(cureLightWounds)
                });
            addClassLevels.Selections = new[]
            {
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.Deity,
                    GameBlueprintIds.Features.Irori,
                    "Deity selection",
                    "Irori"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.Domain,
                    GameBlueprintIds.Features.HealingDomainProgression,
                    "Domain selection",
                    "Healing domain"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.SecondaryDomain,
                    GameBlueprintIds.Features.LawDomainProgressionSecondary,
                    "Secondary domain selection",
                    "Law domain"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.BasicFeat,
                    new[]
                    {
                        GameBlueprintIds.Features.PointBlankShot,
                        GameBlueprintIds.Features.PreciseShot
                    },
                    "Basic feat selection",
                    "starting feats")
            };
            addClassLevels.DoNotApplyAutomatically = false;
        }

        private SelectionEntry CreateSelectionEntry(
            string selectionGuid,
            string featureGuid,
            string selectionName,
            string featureName)
        {
            return CreateSelectionEntry(selectionGuid, new[] { featureGuid }, selectionName, featureName);
        }

        private SelectionEntry CreateSelectionEntry(
            string selectionGuid,
            IEnumerable<string> featureGuids,
            string selectionName,
            string featureName)
        {
            var selection = _blueprints.Require<BlueprintFeatureSelection>(selectionGuid, selectionName);
            var features = featureGuids
                .Select(guid => _blueprints.Require<BlueprintFeature>(guid, featureName))
                .ToArray();

            var entry = new SelectionEntry
            {
                IsParametrizedFeature = false,
                IsFeatureSelectMythicSpellbook = false,
                ParamSpellSchool = SpellSchool.None,
                ParamWeaponCategory = WeaponCategory.UnarmedStrike,
                Stat = StatType.Unknown
            };
            SetField(entry, "m_Selection", BlueprintReferenceBase.CreateTyped<BlueprintFeatureSelectionReference>(selection));
            SetField(entry, "m_Features", features.Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>).ToArray());
            SetField(entry, "m_ParametrizedFeature", null);
            SetField(entry, "m_ParamObject", null);
            SetField(entry, "m_FeatureSelectMythicSpellbook", null);
            SetField(entry, "m_Spellbook", null);
            return entry;
        }
    }
}
