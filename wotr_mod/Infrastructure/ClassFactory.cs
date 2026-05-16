using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal sealed class ClassFactory
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;

        public ClassFactory(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
        }

        public BlueprintCharacterClass EnsureClass(
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

        public void ConfigureClass(
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

        public void ConfigureClassChassis(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            if (definition.Chassis == null)
            {
                return;
            }

            if (definition.Chassis.HitDie.HasValue)
            {
                _blueprints.SetCharacterClassHitDie(characterClass, definition.Chassis.HitDie.Value);
            }

            if (definition.Chassis.SkillPoints.HasValue)
            {
                _blueprints.SetCharacterClassSkillPoints(characterClass, definition.Chassis.SkillPoints.Value);
            }

            if (!string.IsNullOrEmpty(definition.Chassis.BaseAttackBonusGuid))
            {
                var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                    definition.Chassis.BaseAttackBonusGuid,
                    definition.InternalName + " base attack bonus progression");
                _blueprints.SetCharacterClassBaseAttackBonus(characterClass, baseAttackBonus);
            }
        }

        public void ConfigureClassPresentation(
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
                .Select(guid => GetPresentationFeature(
                    guid,
                    $"{definition.InternalName} signature ability",
                    requireReferencedFeatures))
                .Where(feature => feature != null)
                .ToArray();
            _blueprints.SetCharacterClassSignatureAbilities(characterClass, signatureAbilities);

            var defaultBuild = string.IsNullOrWhiteSpace(presentation.DefaultBuildGuid)
                ? null
                : GetPresentationFeature(
                    presentation.DefaultBuildGuid,
                    $"{definition.InternalName} default build",
                    requireReferencedFeatures);
            _blueprints.SetCharacterClassDefaultBuild(characterClass, defaultBuild);
        }

        public BlueprintSpellList EnsureSpellList(
            CharacterClassDefinition definition,
            BlueprintSpellList donor,
            Action<BlueprintSpellList> configureAction)
        {
            return EnsureSpellList(
                donor,
                definition.SpellListGuid,
                definition.InternalName + "_SpellList",
                configureAction);
        }

        public BlueprintSpellList EnsureSpellList(
            BlueprintSpellList donor,
            string spellListGuid,
            string internalName,
            Action<BlueprintSpellList> configureAction)
        {
            var existing = _blueprints.Get<BlueprintSpellList>(spellListGuid);
            var spellList = existing ?? _blueprints.CloneBlueprint(
                donor,
                spellListGuid,
                internalName);

            configureAction?.Invoke(spellList);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(spellListGuid, spellList);
            }

            return spellList;
        }

        public BlueprintSpellbook EnsureSpellbook(
            BlueprintSpellbook donor,
            string spellbookGuid,
            string internalName,
            Action<BlueprintSpellbook> configureAction)
        {
            var existing = _blueprints.Get<BlueprintSpellbook>(spellbookGuid);
            var spellbook = existing ?? _blueprints.CloneBlueprint(donor, spellbookGuid, internalName);

            configureAction?.Invoke(spellbook);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(spellbookGuid, spellbook);
            }

            return spellbook;
        }

        public BlueprintArchetype EnsureArchetype(string archetypeGuid, string internalName)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(archetypeGuid);
            if (archetype != null)
            {
                return archetype;
            }

            archetype = new BlueprintArchetype
            {
                name = internalName,
                AssetGuid = BlueprintGuid.Parse(archetypeGuid)
            };
            _blueprints.AddCachedBlueprint(archetypeGuid, archetype);
            return archetype;
        }

        public void ConfigureArchetypeIdentity(
            BlueprintArchetype archetype,
            BlueprintCharacterClass characterClass,
            string displayNameKey,
            string descriptionKey,
            BlueprintSpellbook replacementSpellbook)
        {
            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, replacementSpellbook);
        }

        public void ConfigureSpellList(BlueprintSpellList spellList, IEnumerable<ClassSpellDefinition> spellDefinitions)
        {
            var spellsByLevel = spellDefinitions
                .Select(definition =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(definition.SpellGuid, definition.DisplayName);
                    ApplySelectionRecommendation(spell, definition);
                    return new KeyValuePair<BlueprintAbility, int>(spell, definition.SpellLevel);
                });

            _blueprints.SetSpellListSpells(
                spellList,
                spellsByLevel.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key.name));
        }

        public BlueprintAbilityResource EnsureAbilityResource(
            string donorGuid,
            string resourceGuid,
            string internalName)
        {
            var resource = _blueprints.Get<BlueprintAbilityResource>(resourceGuid);
            if (resource != null)
            {
                return resource;
            }

            resource = _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintAbilityResource>(donorGuid, internalName + " donor"),
                resourceGuid,
                internalName);
            _blueprints.AddCachedBlueprint(resourceGuid, resource);
            return resource;
        }

        public void BindFeatureAbilityAndResource(
            BlueprintFeature feature,
            BlueprintAbility ability,
            BlueprintAbilityResource resource,
            BlueprintCharacterClass characterClass)
        {
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            if (!_blueprints.GetComponents<AddFacts>(feature).Any())
            {
                var addFacts = new AddFacts { name = "$AddFacts$" + feature.name };
                _blueprints.AddComponent(feature, addFacts);
                _blueprints.SetAddFacts(addFacts, ability);
            }

            BindFeatureResource(feature, resource);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
        }

        public void BindFeatureResource(BlueprintFeature feature, BlueprintAbilityResource resource)
        {
            foreach (var addResources in _blueprints.GetComponents<AddAbilityResources>(feature))
            {
                _blueprints.SetAddAbilityResourcesResource(addResources, resource);
            }

            if (!_blueprints.GetComponents<AddAbilityResources>(feature).Any())
            {
                var addResources = new AddAbilityResources
                {
                    name = "$AddAbilityResources$" + feature.name,
                    RestoreAmount = true
                };
                _blueprints.AddComponent(feature, addResources);
                _blueprints.SetAddAbilityResourcesResource(addResources, resource);
            }
        }

        public void BindAbilityResource(BlueprintAbility ability, BlueprintAbilityResource resource)
        {
            foreach (var resourceLogic in _blueprints.GetComponents<AbilityResourceLogic>(ability))
            {
                _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
            }
        }

        public void BindAbilityRanksToClass(BlueprintAbility ability, BlueprintCharacterClass characterClass)
        {
            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(ability))
            {
                _blueprints.ConfigureContextRankConfig(
                    rank,
                    AbilityRankType.Default,
                    ContextRankBaseValueType.ClassLevel,
                    ContextRankProgression.AsIs,
                    characterClass: characterClass);
                _blueprints.SetContextRankMinimum(rank, 1);
            }
        }

        public static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }

        private void ApplySelectionRecommendation(BlueprintScriptableObject blueprint, ClassSpellDefinition definition)
        {
            if (!definition.Recommendation.HasValue)
            {
                return;
            }

            _blueprints.AddSelectionRecommendation(
                blueprint,
                definition.Recommendation.Value,
                $"$PureRecommendation${definition.DisplayName}");
        }

        private BlueprintFeature GetPresentationFeature(
            string guid,
            string name,
            bool reportMissing)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null && reportMissing)
            {
                _blueprints.ReportError($"{name} ({guid}) was not available.");
            }

            return feature;
        }
    }
}
