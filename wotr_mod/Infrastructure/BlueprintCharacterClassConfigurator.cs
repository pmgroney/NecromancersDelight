using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;

namespace wotr_mod.Infrastructure
{
    internal sealed class BlueprintCharacterClassConfigurator
    {
        private readonly BlueprintTool _blueprints;

        public BlueprintCharacterClassConfigurator(BlueprintTool blueprints)
        {
            _blueprints = blueprints;
        }

        public void SetCharacterClassDisplay(
            BlueprintCharacterClass characterClass,
            LocalizedString name,
            LocalizedString description)
        {
            BlueprintFields.CharacterClassLocalizedName.SetValue(characterClass, name);
            BlueprintFields.CharacterClassLocalizedDescription.SetValue(characterClass, description);
            BlueprintFields.CharacterClassLocalizedDescriptionShort.SetValue(characterClass, description);
        }

        public void SetCharacterClassProgression(
            BlueprintCharacterClass characterClass,
            BlueprintProgression progression)
        {
            BlueprintFields.CharacterClassProgression.SetValue(
                characterClass,
                BlueprintReferenceBase.CreateTyped<BlueprintProgressionReference>(progression));
        }

        public void SetCharacterClassSpellbook(
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook)
        {
            BlueprintFields.CharacterClassSpellbook.SetValue(
                characterClass,
                BlueprintReferenceBase.CreateTyped<BlueprintSpellbookReference>(spellbook));
        }

        public void SetCharacterClassHitDie(
            BlueprintCharacterClass characterClass,
            Kingmaker.RuleSystem.DiceType hitDie)
        {
            BlueprintFields.CharacterClassHitDie.SetValue(characterClass, hitDie);
        }

        public void SetCharacterClassSkillPoints(BlueprintCharacterClass characterClass, int skillPoints)
        {
            characterClass.SkillPoints = skillPoints;
        }

        public void SetCharacterClassBaseAttackBonus(
            BlueprintCharacterClass characterClass,
            BlueprintStatProgression progression)
        {
            BlueprintFields.CharacterClassBaseAttackBonus.SetValue(
                characterClass,
                progression == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintStatProgressionReference>(progression));
        }

        public void SetCharacterClassArchetypes(
            BlueprintCharacterClass characterClass,
            params BlueprintArchetype[] archetypes)
        {
            foreach (var archetype in (archetypes ?? Array.Empty<BlueprintArchetype>()).Where(archetype => archetype != null))
            {
                SetArchetypeParentClass(archetype, characterClass);
            }

            var references = (archetypes ?? Enumerable.Empty<BlueprintArchetype>())
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintArchetypeReference>)
                .ToArray();
            BlueprintFields.CharacterClassArchetypes.SetValue(characterClass, references);

            if (string.Equals(characterClass?.AssetGuid.ToString(), ModBlueprintIds.Classes.Necromancer, StringComparison.OrdinalIgnoreCase))
            {
                _blueprints.Log("Necromancer archetypes assigned: " + DescribeArchetypes(archetypes));
            }
        }

        public void SetCharacterClassAppearanceFromClass(
            BlueprintCharacterClass target,
            BlueprintCharacterClass source)
        {
            CopyClassAppearanceField(BlueprintFields.CharacterClassPrimaryColor, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassSecondaryColor, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassEquipmentEntities, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassMaleEquipmentEntities, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassFemaleEquipmentEntities, target, source);
        }

        public void SetArchetypeDisplay(
            BlueprintArchetype archetype,
            LocalizedString name,
            LocalizedString description)
        {
            BlueprintFields.ArchetypeLocalizedName.SetValue(archetype, name);
            BlueprintFields.ArchetypeLocalizedDescription.SetValue(archetype, description);
            BlueprintFields.ArchetypeLocalizedDescriptionShort.SetValue(archetype, description);
        }

        public void SetArchetypeReplaceSpellbook(BlueprintArchetype archetype, BlueprintSpellbook spellbook)
        {
            BlueprintFields.ArchetypeReplaceSpellbook.SetValue(
                archetype,
                spellbook == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintSpellbookReference>(spellbook));
        }

        public void SetArchetypeFeatureChanges(
            BlueprintArchetype archetype,
            IEnumerable<LevelEntry> addFeatures,
            IEnumerable<LevelEntry> removeFeatures)
        {
            var addEntries = (addFeatures ?? Enumerable.Empty<LevelEntry>()).ToArray();
            var removeEntries = (removeFeatures ?? Enumerable.Empty<LevelEntry>()).ToArray();
            BlueprintFields.ArchetypeAddFeatures.SetValue(archetype, addEntries);
            BlueprintFields.ArchetypeRemoveFeatures.SetValue(archetype, removeEntries);

            if (string.Equals(archetype?.AssetGuid.ToString(), ModBlueprintIds.Archetypes.Graveblade, StringComparison.OrdinalIgnoreCase))
            {
                _blueprints.Log("Graveblade AddFeatures: " + DescribeLevelEntries(addEntries));
                _blueprints.Log("Graveblade RemoveFeatures: " + DescribeLevelEntries(removeEntries));
            }
        }

        public void SetArchetypeBuildChanging(BlueprintArchetype archetype, bool buildChanging)
        {
            BlueprintFields.ArchetypeBuildChanging?.SetValue(archetype, buildChanging);
        }

        public void SetArchetypeParentClass(
            BlueprintArchetype archetype,
            BlueprintCharacterClass characterClass)
        {
            BlueprintFields.ArchetypeParentClass?.SetValue(archetype, characterClass);
        }

        public void SetArchetypeBaseAttackBonus(
            BlueprintArchetype archetype,
            BlueprintStatProgression progression)
        {
            BlueprintFields.ArchetypeBaseAttackBonus.SetValue(
                archetype,
                progression == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintStatProgressionReference>(progression));
        }

        public void SetArchetypeStartingEquipment(
            BlueprintArchetype archetype,
            bool replaceStartingEquipment,
            int startingGold,
            params BlueprintItem[] items)
        {
            BlueprintFields.ArchetypeReplaceStartingEquipment?.SetValue(archetype, replaceStartingEquipment);
            BlueprintFields.ArchetypeStartingGold?.SetValue(archetype, startingGold);
            BlueprintFields.ArchetypeStartingItems?.SetValue(
                archetype,
                (items ?? Array.Empty<BlueprintItem>())
                    .Where(item => item != null)
                    .Select(BlueprintReferenceBase.CreateTyped<BlueprintItemReference>)
                    .ToArray());
        }

        public void SetCharacterClassStartingEquipment(
            BlueprintCharacterClass characterClass,
            int startingGold,
            params BlueprintItem[] items)
        {
            BlueprintFields.CharacterClassStartingGold?.SetValue(characterClass, startingGold);
            BlueprintFields.CharacterClassStartingItems?.SetValue(
                characterClass,
                (items ?? Array.Empty<BlueprintItem>())
                    .Where(item => item != null)
                    .Select(BlueprintReferenceBase.CreateTyped<BlueprintItemReference>)
                    .ToArray());
        }

        public int GetCharacterClassStartingGold(BlueprintCharacterClass characterClass)
        {
            return BlueprintFields.CharacterClassStartingGold?.GetValue(characterClass) is int gold
                ? gold
                : 0;
        }

        public BlueprintItem[] GetCharacterClassStartingEquipment(BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return Array.Empty<BlueprintItem>();
            }

            return characterClass.StartingItems
                .Where(item => item != null)
                .ToArray();
        }

        public void SetArchetypeAttributeRecommendations(
            BlueprintArchetype archetype,
            IEnumerable<StatType> recommendedAttributes,
            IEnumerable<StatType> notRecommendedAttributes)
        {
            BlueprintFields.ArchetypeOverrideAttributeRecommendations?.SetValue(archetype, true);
            BlueprintFields.ArchetypeRecommendedAttributes?.SetValue(
                archetype,
                (recommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
            BlueprintFields.ArchetypeNotRecommendedAttributes?.SetValue(
                archetype,
                (notRecommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
        }

        public void SetCharacterClassSignatureAbilities(
            BlueprintCharacterClass characterClass,
            params BlueprintFeature[] features)
        {
            var references = (features ?? Enumerable.Empty<BlueprintFeature>())
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>)
                .ToArray();
            BlueprintFields.CharacterClassSignatureAbilities.SetValue(characterClass, references);
        }

        public void SetArchetypeSignatureAbilities(
            BlueprintArchetype archetype,
            params BlueprintFeature[] features)
        {
            var references = (features ?? Enumerable.Empty<BlueprintFeature>())
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>)
                .ToArray();
            BlueprintFields.ArchetypeSignatureAbilities.SetValue(archetype, references);
        }

        public void SetCharacterClassDifficulty(BlueprintCharacterClass characterClass, int difficulty)
        {
            BlueprintFields.CharacterClassDifficulty?.SetValue(characterClass, difficulty);
        }

        public void SetCharacterClassAttributeRecommendations(
            BlueprintCharacterClass characterClass,
            IEnumerable<StatType> recommendedAttributes,
            IEnumerable<StatType> notRecommendedAttributes)
        {
            BlueprintFields.CharacterClassRecommendedAttributes?.SetValue(
                characterClass,
                (recommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
            BlueprintFields.CharacterClassNotRecommendedAttributes?.SetValue(
                characterClass,
                (notRecommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
        }

        public void SetCharacterClassDefaultBuild(
            BlueprintCharacterClass characterClass,
            BlueprintFeature defaultBuild)
        {
            BlueprintFields.CharacterClassDefaultBuild?.SetValue(
                characterClass,
                defaultBuild == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(defaultBuild));
        }

        private static void CopyClassAppearanceField(
            FieldInfo field,
            BlueprintCharacterClass target,
            BlueprintCharacterClass source)
        {
            var value = field?.GetValue(source);
            if (value is Array array)
            {
                value = array.Clone();
            }

            field?.SetValue(target, value);
        }

        private static string DescribeArchetypes(IEnumerable<BlueprintArchetype> archetypes)
        {
            var values = (archetypes ?? Enumerable.Empty<BlueprintArchetype>())
                .Select(archetype => archetype == null
                    ? "<null>"
                    : $"{archetype.name}({archetype.AssetGuid})")
                .ToArray();
            return values.Length == 0 ? "<none>" : string.Join(", ", values);
        }

        private static string DescribeLevelEntries(IEnumerable<LevelEntry> entries)
        {
            var values = (entries ?? Enumerable.Empty<LevelEntry>())
                .Select(entry => entry == null
                    ? "<null>"
                    : $"L{entry.Level}=[{DescribeFeatures(entry.Features)}]")
                .ToArray();
            return values.Length == 0 ? "<none>" : string.Join("; ", values);
        }

        private static string DescribeFeatures(IEnumerable<BlueprintFeatureBase> features)
        {
            var values = (features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .Select(feature => feature == null
                    ? "<null>"
                    : $"{feature.name}({feature.AssetGuid})")
                .ToArray();
            return values.Length == 0 ? "<none>" : string.Join(", ", values);
        }
    }
}
