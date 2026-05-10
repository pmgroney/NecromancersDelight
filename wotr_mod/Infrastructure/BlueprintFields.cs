using System.Reflection;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;

namespace wotr_mod.Infrastructure
{
    internal static class BlueprintFields
    {
        public static readonly FieldInfo ProgressionClasses =
            typeof(BlueprintProgression).GetField("m_Classes", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ProgressionUIDeterminatorsGroup =
            typeof(BlueprintProgression).GetField("m_UIDeterminatorsGroup", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ProgressionUIGroups =
            typeof(BlueprintProgression).GetField("UIGroups", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo UIGroupFeatures =
            typeof(UIGroup).GetField("m_Features", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo BlueprintComponents =
            typeof(BlueprintScriptableObject).GetField("m_Components", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(BlueprintScriptableObject).GetField("Components", BindingFlags.Instance | BindingFlags.NonPublic);

        static BlueprintFields()
        {
            // Logging for missing fields to help debugging
            if (ProgressionClasses == null) PatchRegistry.FallbackError("WARNING: BlueprintProgression.m_Classes not found");
            if (ProgressionUIDeterminatorsGroup == null) PatchRegistry.FallbackError("WARNING: BlueprintProgression.m_UIDeterminatorsGroup not found");
            if (ProgressionUIGroups == null) PatchRegistry.FallbackError("WARNING: BlueprintProgression.UIGroups not found");
            if (UIGroupFeatures == null) PatchRegistry.FallbackError("WARNING: UIGroup.m_Features not found");
            if (BlueprintComponents == null) PatchRegistry.FallbackError("WARNING: BlueprintScriptableObject.m_Components (or Components) not found");
            if (CharacterClassHiddenFields.Length == 0) PatchRegistry.FallbackError("WARNING: BlueprintCharacterClass hidden fields not found");
        }

        public static readonly FieldInfo UnitFactDisplayName =
            typeof(BlueprintUnitFact).GetField("m_DisplayName", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo UnitFactDescription =
            typeof(BlueprintUnitFact).GetField("m_Description", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo UnitFactDescriptionShort =
            typeof(BlueprintUnitFact).GetField("m_DescriptionShort", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ItemDisplayName =
            typeof(BlueprintItem).GetField("m_DisplayNameText", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ItemDescription =
            typeof(BlueprintItem).GetField("m_DescriptionText", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ItemWeaponEnchantments =
            typeof(BlueprintItemWeapon).GetField("m_Enchantments", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ItemArmorEnchantments =
            typeof(BlueprintItemArmor).GetField("m_Enchantments", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityDisplayName = UnitFactDisplayName;

        public static readonly FieldInfo AbilityDescription = UnitFactDescription;

        public static readonly FieldInfo CharacterClassLocalizedName =
            typeof(BlueprintCharacterClass).GetField("LocalizedName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassLocalizedDescription =
            typeof(BlueprintCharacterClass).GetField("LocalizedDescription", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassLocalizedDescriptionShort =
            typeof(BlueprintCharacterClass).GetField("LocalizedDescriptionShort", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassProgression =
            typeof(BlueprintCharacterClass).GetField("m_Progression", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassSpellbook =
            typeof(BlueprintCharacterClass).GetField("m_Spellbook", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassHitDie =
            typeof(BlueprintCharacterClass).GetField("HitDie", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassBaseAttackBonus =
            typeof(BlueprintCharacterClass).GetField("m_BaseAttackBonus", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassArchetypes =
            typeof(BlueprintCharacterClass).GetField("m_Archetypes", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassSignatureAbilities =
            typeof(BlueprintCharacterClass).GetField("m_SignatureAbilities", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeSignatureAbilities =
            typeof(BlueprintArchetype).GetField("m_SignatureAbilities", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassDifficulty =
            typeof(BlueprintCharacterClass).GetField("m_Difficulty", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassRecommendedAttributes =
            typeof(BlueprintCharacterClass).GetField("RecommendedAttributes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassNotRecommendedAttributes =
            typeof(BlueprintCharacterClass).GetField("NotRecommendedAttributes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassDefaultBuild =
            typeof(BlueprintCharacterClass).GetField("m_DefaultBuild", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassPrimaryColor =
            typeof(BlueprintCharacterClass).GetField("PrimaryColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassSecondaryColor =
            typeof(BlueprintCharacterClass).GetField("SecondaryColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassEquipmentEntities =
            typeof(BlueprintCharacterClass).GetField("m_EquipmentEntities", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassMaleEquipmentEntities =
            typeof(BlueprintCharacterClass).GetField("MaleEquipmentEntities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassFemaleEquipmentEntities =
            typeof(BlueprintCharacterClass).GetField("FemaleEquipmentEntities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassStartingGold =
            typeof(BlueprintCharacterClass).GetField("StartingGold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassStartingItems =
            typeof(BlueprintCharacterClass).GetField("m_StartingItems", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo CharacterClassHiddenInCharacterCreation =
            typeof(BlueprintCharacterClass).GetField("m_HiddenInCharacterCreation", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(BlueprintCharacterClass).GetField("m_Hidden", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(BlueprintCharacterClass).GetField("HideInUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo[] CharacterClassHiddenFields = new[]
        {
            CharacterClassHiddenInCharacterCreation,
            typeof(BlueprintCharacterClass).GetField("HideInUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            typeof(BlueprintCharacterClass).GetField("HideIfRestricted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        }
            .Where(field => field != null)
            .GroupBy(field => field.Name)
            .Select(group => group.First())
            .ToArray();

        public static readonly FieldInfo ArchetypeLocalizedName =
            typeof(BlueprintArchetype).GetField("LocalizedName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeLocalizedDescription =
            typeof(BlueprintArchetype).GetField("LocalizedDescription", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeLocalizedDescriptionShort =
            typeof(BlueprintArchetype).GetField("LocalizedDescriptionShort", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeReplaceSpellbook =
            typeof(BlueprintArchetype).GetField("m_ReplaceSpellbook", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeAddFeatures =
            typeof(BlueprintArchetype).GetField("AddFeatures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeRemoveFeatures =
            typeof(BlueprintArchetype).GetField("RemoveFeatures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeBuildChanging =
            typeof(BlueprintArchetype).GetField("BuildChanging", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeParentClass =
            typeof(BlueprintArchetype).GetField("m_ParentClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeOverrideAttributeRecommendations =
            typeof(BlueprintArchetype).GetField("OverrideAttributeRecommendations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeRecommendedAttributes =
            typeof(BlueprintArchetype).GetField("RecommendedAttributes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeNotRecommendedAttributes =
            typeof(BlueprintArchetype).GetField("NotRecommendedAttributes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeBaseAttackBonus =
            typeof(BlueprintArchetype).GetField("m_BaseAttackBonus", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeReplaceStartingEquipment =
            typeof(BlueprintArchetype).GetField("ReplaceStartingEquipment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeStartingGold =
            typeof(BlueprintArchetype).GetField("StartingGold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ArchetypeStartingItems =
            typeof(BlueprintArchetype).GetField("m_StartingItems", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpellbookSpellList =
            typeof(BlueprintSpellbook).GetField("m_SpellList", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpellbookCharacterClass =
            typeof(BlueprintSpellbook).GetField("m_CharacterClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpellbookSpellsPerDay =
            typeof(BlueprintSpellbook).GetField("m_SpellsPerDay", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpellbookSpellsKnown =
            typeof(BlueprintSpellbook).GetField("m_SpellsKnown", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpellbookSpellSlots =
            typeof(BlueprintSpellbook).GetField("m_SpellSlots", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpellLevelListSpells =
            typeof(SpellLevelList).GetField("m_Spells", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo FeatureSelectionFeatures =
            typeof(BlueprintFeatureSelection).GetField("m_Features", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo FeatureSelectionAllFeatures =
            typeof(BlueprintFeatureSelection).GetField("m_AllFeatures", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ParametrizedFeatureSpellList =
            typeof(BlueprintParametrizedFeature).GetField("m_SpellList", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ParametrizedFeatureSpellcasterClass =
            typeof(BlueprintParametrizedFeature).GetField("m_SpellcasterClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LearnSpellParametrizedSpellList =
            typeof(LearnSpellParametrized).GetField("m_SpellList", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LearnSpellParametrizedSpellcasterClass =
            typeof(LearnSpellParametrized).GetField("m_SpellcasterClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo RaceFeatures =
            typeof(BlueprintRace).GetField("m_Features", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddFeatureOnApplyFeature =
            typeof(AddFeatureOnApply).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo RemoveFeatureOnApplyFeature =
            typeof(RemoveFeatureOnApply).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddFactsFacts =
            typeof(AddFacts).GetField("m_Facts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo PrerequisiteNoFeatureFeature =
            typeof(PrerequisiteNoFeature).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddStartingEquipmentBasicItems =
            typeof(AddStartingEquipment).GetField("m_BasicItems", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddStartingEquipmentRestrictedByClass =
            typeof(AddStartingEquipment).GetField("m_RestrictedByClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddUnitFeatureEquipmentFeature =
            typeof(AddUnitFeatureEquipment).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LootEntryItem =
            typeof(LootEntry).GetField("m_Item", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LootItemType =
            typeof(LootItem).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LootItemItem =
            typeof(LootItem).GetField("m_Item", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LootItemLoot =
            typeof(LootItem).GetField("m_Loot", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LootItemsPackFixedItem =
            typeof(LootItemsPackFixed).GetField("m_Item", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo LootItemsPackFixedCount =
            typeof(LootItemsPackFixed).GetField("m_Count", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddLootLoot =
            typeof(AddLoot).GetField("m_Loot", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddStatBonusIfHasFactCheckedFacts =
            typeof(AddStatBonusIfHasFact).GetField("m_CheckedFacts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo RecalculateOnFactsChangeCheckedFacts =
            typeof(RecalculateOnFactsChange).GetField("m_CheckedFacts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddKnownSpellCharacterClass =
            typeof(AddKnownSpell).GetField("m_CharacterClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddKnownSpellSpell =
            typeof(AddKnownSpell).GetField("m_Spell", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddKnownSpellArchetype =
            typeof(AddKnownSpell).GetField("m_Archetype", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo RecommendationNoFeatFromGroupFeatures =
            typeof(RecommendationNoFeatFromGroup).GetField("m_Features", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo RecommendationNoFeatFromGroupExcludedFeatures =
            typeof(RecommendationNoFeatFromGroup).GetField("m_FeaturesExlude", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddAbilityResourcesResource =
            typeof(AddAbilityResources).GetField("m_Resource", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityResourceMaxAmount =
            typeof(BlueprintAbilityResource).GetField("m_MaxAmount", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityResourceLogicRequiredResource =
            typeof(AbilityResourceLogic).GetField("m_RequiredResource", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityResourceLogicIsSpendResource =
            typeof(AbilityResourceLogic).GetField("m_IsSpendResource", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityDeliverProjectileProjectiles =
            typeof(AbilityDeliverProjectile).GetField("m_Projectiles", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityDeliverProjectileLength =
            typeof(AbilityDeliverProjectile).GetField("m_Length", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityDeliverProjectileUseMaxProjectilesCount =
            typeof(AbilityDeliverProjectile).GetField("UseMaxProjectilesCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextActionApplyBuffBuff =
            typeof(ContextActionApplyBuff).GetField("m_Buff", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextActionRemoveBuffBuff =
            typeof(ContextActionRemoveBuff).GetField("m_Buff", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AddAreaEffectArea =
            typeof(AddAreaEffect).GetField("m_AreaEffect", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo BuffOnArmorBuff =
            typeof(BuffOnArmor).GetField("m_Buff", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo AbilityCasterHasNoFactsFacts =
            typeof(AbilityCasterHasNoFacts).GetField("m_Facts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo BindAbilitiesToClassCharacterClass =
            typeof(BindAbilitiesToClass).GetField("m_CharacterClass", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo BindAbilitiesToClassAdditionalClasses =
            typeof(BindAbilitiesToClass).GetField("m_AdditionalClasses", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo BindAbilitiesToClassArchetypes =
            typeof(BindAbilitiesToClass).GetField("m_Archetypes", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ReplaceCasterLevelOfAbilityClass =
            typeof(ReplaceCasterLevelOfAbility).GetField("m_Class", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ReplaceCasterLevelOfAbilityAdditionalClasses =
            typeof(ReplaceCasterLevelOfAbility).GetField("m_AdditionalClasses", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ReplaceCasterLevelOfAbilityArchetypes =
            typeof(ReplaceCasterLevelOfAbility).GetField("m_Archetypes", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigUseMax =
            typeof(ContextRankConfig).GetField("m_UseMax", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigMax =
            typeof(ContextRankConfig).GetField("m_Max", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo UnitAddFacts =
            typeof(BlueprintUnit).GetField("m_AddFacts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo UnitFactIcon =
            typeof(BlueprintUnitFact).GetField("m_Icon", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo SpawnAreaEffectArea =
            typeof(ContextActionSpawnAreaEffect).GetField("m_AreaEffect", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigBaseValueType =
            typeof(ContextRankConfig).GetField("m_BaseValueType", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigType =
            typeof(ContextRankConfig).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigProgression =
            typeof(ContextRankConfig).GetField("m_Progression", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigFeature =
            typeof(ContextRankConfig).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigCustomProgression =
            typeof(ContextRankConfig).GetField("m_CustomProgression", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigStartLevel =
            typeof(ContextRankConfig).GetField("m_StartLevel", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigStepLevel =
            typeof(ContextRankConfig).GetField("m_StepLevel", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigClass =
            typeof(ContextRankConfig).GetField("m_Class", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigAdditionalArchetypes =
            typeof(ContextRankConfig).GetField("m_AdditionalArchetypes", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigArchetype =
            typeof(ContextRankConfig).GetField("Archetype", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigUseMin =
            typeof(ContextRankConfig).GetField("m_UseMin", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly FieldInfo ContextRankConfigMin =
            typeof(ContextRankConfig).GetField("m_Min", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
