using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Evoker
{
    internal sealed partial class EvokerInstaller : IClassContentInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;
        private readonly GrantedSpellFeatureFactory _grantedSpellFeatures;
        private readonly ElementalBloodlineInstaller _elementalBloodlines;
        private readonly DraconicBloodlineInstaller _draconicBloodline;
        private readonly ShadowbornBloodlineInstaller _shadowbornBloodline;
        private static readonly int[] EvokerBonusFeatLevels = { 1, 6, 10, 16, 20 };

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
            _grantedSpellFeatures = new GrantedSpellFeatureFactory(blueprints, localization, icons);
            _elementalBloodlines = new ElementalBloodlineInstaller(
                blueprints,
                localization,
                _grantedSpellFeatures,
                this);
            _draconicBloodline = new DraconicBloodlineInstaller(blueprints, localization);
            _shadowbornBloodline = new ShadowbornBloodlineInstaller(
                blueprints,
                localization,
                logger,
                icons,
                _grantedSpellFeatures,
                this);
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
            var evocationUnleashed = EnsureEvocationUnleashedClassCardFeature(characterClass);
            EnsureElementalConversionClassCardFeature(characterClass);
            EnsureEvokerFamiliarClassCardFeature(characterClass);
            ConfigureEvokerBonusFeatProgression(characterClass);
            EnsureShadowbornBonusFeatCompatibilityStub(characterClass);
            ReplaceSorcererProficiencies(characterClass);
            EnsureEvokerBloodlineSelection(characterClass);
            EnsureEvocationSpellFocusRecommendation(characterClass);
            _blueprints.SetCharacterClassArchetypes(characterClass);
            var noMartialWeaponProficiency = EnsureNoMartialWeaponProficiencyFeature(characterClass);
            EnsureMartialWeaponProficiencyBlockedForEvoker(noMartialWeaponProficiency);
            _blueprints.AddFeatureToLevel(
                characterClass.Progression,
                1,
                EnsureSpellShapingFeature(characterClass));
            _blueprints.AddFeatureToLevel(
                characterClass.Progression,
                1,
                evocationUnleashed);
            _blueprints.AddFeatureToLevel(
                characterClass.Progression,
                1,
                noMartialWeaponProficiency);

            EnsureEvokerArcaneBloodline(characterClass);
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

        private BlueprintFeature EnsureNoMartialWeaponProficiencyFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.EvokerNoMartialWeaponProficiency);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_EvokerNoMartialWeaponProficiency",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.EvokerNoMartialWeaponProficiency),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.EvokerNoMartialWeaponProficiency, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            feature.ReapplyOnLevelUp = true;
            _blueprints.SetProgressionClasses(feature, characterClass);

            var martialWeaponProficiency = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.MartialWeaponProficiency,
                "Martial Weapon Proficiency");
            var bloodragerProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.BloodragerProficiencies,
                "Bloodrager Proficiencies");
            var removeMartialWeapons = new RemoveFeatureOnApply
            {
                name = "$RemoveFeatureOnApply$EvokerMartialWeapons"
            };
            var removeBloodragerProficiencies = new RemoveFeatureOnApply
            {
                name = "$RemoveFeatureOnApply$EvokerBloodragerProficiencies"
            };
            _blueprints.SetRemoveFeatureOnApplyFeature(removeMartialWeapons, martialWeaponProficiency);
            _blueprints.SetRemoveFeatureOnApplyFeature(removeBloodragerProficiencies, bloodragerProficiencies);
            _blueprints.SetComponents(feature, removeMartialWeapons, removeBloodragerProficiencies);
            return feature;
        }

        private void ConfigureEvokerBonusFeatProgression(BlueprintCharacterClass characterClass)
        {
            var evokerBonusFeat = EnsureEvokerBonusFeatSelection(characterClass);
            if (characterClass?.Progression == null)
            {
                return;
            }

            _blueprints.RemoveFeatureFromProgression(characterClass.Progression, GameBlueprintIds.Selections.SorcererBonusFeat);
            _blueprints.RemoveFeatureFromProgression(characterClass.Progression, GameBlueprintIds.Selections.SorcererFeatSelection);
            _blueprints.RemoveFeatureFromProgression(characterClass.Progression, evokerBonusFeat);
            foreach (var level in EvokerBonusFeatLevels)
            {
                _blueprints.AddFeatureToLevel(characterClass.Progression, level, evokerBonusFeat);
            }

            _blueprints.AddProgressionUiGroup(characterClass.Progression, evokerBonusFeat);
        }

        internal BlueprintFeatureSelection EnsureEvokerBonusFeatSelection(BlueprintCharacterClass characterClass)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.EvokerBonusFeat);
            if (selection == null)
            {
                var source = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBonusFeat,
                    "Sorcerer Bonus Feat");
                selection = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Selections.EvokerBonusFeat,
                    "WotrMod_EvokerBonusFeat");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.EvokerBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.EvokerBonusFeatName),
                _localization.Text(LocalizationIds.Mod.EvokerBonusFeatDescription));
            if (characterClass != null)
            {
                _blueprints.SetProgressionClassesShallow(selection, characterClass);
            }

            return selection;
        }

        private BlueprintFeatureSelection EnsureShadowbornBonusFeatCompatibilityStub(BlueprintCharacterClass characterClass)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.ShadowbornBonusFeat);
            if (selection == null)
            {
                var source = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBonusFeat,
                    "Sorcerer Bonus Feat");
                selection = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Selections.ShadowbornBonusFeat,
                    "WotrMod_ShadowbornBonusFeatCompatibilityStub");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.ShadowbornBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.ShadowbornBonusFeatName),
                _localization.Text(LocalizationIds.Mod.ShadowbornBonusFeatDescription));
            _blueprints.SetComponents(selection);
            _blueprints.SetFeatureSelectionFeatures(selection, Array.Empty<BlueprintFeature>());
            _blueprints.SetFeatureSelectionAllFeatures(selection, Array.Empty<BlueprintFeature>());
            selection.IsClassFeature = false;
            selection.HideInUI = true;
            selection.HideInCharacterSheetAndLevelUp = true;
            selection.HideNotAvailibleInUI = true;
            if (characterClass != null)
            {
                _blueprints.SetProgressionClassesShallow(selection, characterClass);
            }

            return selection;
        }

        private void ReplaceSorcererProficiencies(BlueprintCharacterClass characterClass)
        {
            var evokerProficiencies = EnsureEvokerProficiencies(characterClass);
            if (characterClass?.Progression == null)
            {
                return;
            }

            ReplaceProgressionFeature(
                characterClass.Progression,
                GameBlueprintIds.Features.SorcererProficiencies,
                evokerProficiencies);
        }

        private BlueprintFeature EnsureEvokerProficiencies(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.EvokerProficiencies);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.SorcererProficiencies,
                    "Sorcerer Proficiencies");
                feature = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Features.EvokerProficiencies,
                    "WotrMod_EvokerProficiencies");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.EvokerProficiencies, feature);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.EvokerProficienciesName),
                _localization.Text(LocalizationIds.Mod.EvokerProficienciesDescription));
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureEvocationUnleashedClassCardFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.EvocationUnleashedClassCard);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_EvocationUnleashedClassCard",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.EvocationUnleashedClassCard)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.EvocationUnleashedClassCard, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            feature.HideInUI = false;
            feature.HideInCharacterSheetAndLevelUp = false;
            feature.HideNotAvailibleInUI = false;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.EvocationUnleashedName),
                _localization.Text(LocalizationIds.Mod.EvocationUnleashedDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.EvocationUnleashedDescription));
            var icon = _icons.Load("Icons\\evocation_unleashed.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            _blueprints.SetComponents(feature);
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureElementalConversionClassCardFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.ElementalConversionClassCard);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_ElementalConversionClassCard",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.ElementalConversionClassCard)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ElementalConversionClassCard, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ElementalConversionName),
                _localization.Text(LocalizationIds.Mod.ElementalConversionDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.ElementalConversionDescription));
            var icon = _icons.Load("Icons\\elemental_conversion.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            _blueprints.SetComponents(feature);
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureEvokerFamiliarClassCardFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.EvokerFamiliarClassCard);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.DruidNatureBond,
                        "Druid Nature Bond"),
                    ModBlueprintIds.Features.EvokerFamiliarClassCard,
                    "WotrMod_EvokerFamiliarClassCard");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.EvokerFamiliarClassCard, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.EvokerFamiliarName),
                _localization.Text(LocalizationIds.Mod.EvokerFamiliarDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.EvokerFamiliarDescription));
            _blueprints.SetComponents(feature);
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private void EnsureMartialWeaponProficiencyBlockedForEvoker(BlueprintFeature noMartialWeaponProficiency)
        {
            var martialWeaponProficiency = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.MartialWeaponProficiency,
                "Martial Weapon Proficiency");
            var componentName = "$PrerequisiteNoFeature$EvokerNoMartialWeaponProficiency";
            if (_blueprints.GetComponents<BlueprintComponent>(martialWeaponProficiency)
                .Any(component => component?.name == componentName))
            {
                return;
            }

            var prerequisite = new PrerequisiteNoFeature
            {
                name = componentName,
                Group = Prerequisite.GroupType.All,
                HideInUI = true
            };
            _blueprints.SetPrerequisiteNoFeatureFeature(prerequisite, noMartialWeaponProficiency);
            _blueprints.AddComponent(martialWeaponProficiency, prerequisite);
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
                    var spell = EnsureEvokerSpellClone(definition);
                    return new KeyValuePair<BlueprintAbility, int>(spell, definition.SpellLevel);
                });

            _blueprints.SetSpellListSpells(
                spellList,
                spellsByLevel.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key.name));
        }

        private BlueprintFeatureSelection EnsureEvokerBloodlineSelection(BlueprintCharacterClass characterClass = null)
        {
            return _elementalBloodlines.EnsureSelection(characterClass);
        }

        internal BlueprintProgression EnsureEvokerArcaneBloodline(BlueprintCharacterClass characterClass)
        {
            return _elementalBloodlines.EnsureEvokerArcaneBloodline(characterClass);
        }

        internal BlueprintFeatureSelection EnsureArcanistNewArcanaSelection(BlueprintCharacterClass characterClass)
        {
            return _elementalBloodlines.EnsureArcanistNewArcanaSelection(characterClass);
        }

        internal void MoveProtectionFromEnergyToCommunal(
            BlueprintProgression progression,
            BlueprintCharacterClass characterClass)
        {
            _elementalBloodlines.MoveProtectionFromEnergyToCommunal(progression, characterClass);
        }

        internal BlueprintFeatureSelection EnsureDraconicEvokerBloodlineSelection(BlueprintCharacterClass characterClass = null)
        {
            return _draconicBloodline.EnsureSelection(characterClass);
        }

        private IEnumerable<GameAction> GetActions(BlueprintAbility ability)
        {
            return _blueprints.GetComponents<AbilityEffectRunAction>(ability)
                .SelectMany(runAction => runAction.Actions?.Actions ?? Array.Empty<GameAction>());
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

        internal BlueprintProgression EnsureShadowbornBloodline(BlueprintCharacterClass characterClass)
        {
            return _shadowbornBloodline.EnsureBloodline(characterClass);
        }

        internal BlueprintFeature EnsureShadowbornLivingGhostFeature(BlueprintCharacterClass characterClass)
        {
            return _shadowbornBloodline.EnsureLivingGhostFeature(characterClass);
        }

        internal void ReplaceDescriptor(BlueprintScriptableObject blueprint, SpellDescriptor remove, SpellDescriptor add)
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

        internal void SetIcon(BlueprintUnitFact fact, string iconPath)
        {
            var icon = _icons.Load(iconPath);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(fact, icon);
            }
        }

        internal static void ReplaceBuffReferences(
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

        internal static void ConfigureAddFeatureOnClassLevel(
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

        internal void BindAbilityRankConfigsToClass(BlueprintAbility ability, BlueprintCharacterClass characterClass)
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

        internal static void ReplaceProgressionFeature(
            BlueprintProgression progression,
            string oldFeatureGuid,
            BlueprintFeatureBase newFeature)
        {
            if (progression == null || string.IsNullOrWhiteSpace(oldFeatureGuid) || newFeature == null)
            {
                return;
            }

            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldFeatureGuid));
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature => feature != null && feature.AssetGuid == oldGuid ? newFeature : feature));
            }

            ReplaceProgressionUiFeature(progression, oldGuid, newFeature);
        }

        internal static void RemoveProgressionFeature(
            BlueprintProgression progression,
            string featureGuid)
        {
            if (progression == null || string.IsNullOrWhiteSpace(featureGuid))
            {
                return;
            }

            var guid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(featureGuid));
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Where(feature => feature == null || feature.AssetGuid != guid));
            }

            RemoveProgressionUiFeature(progression, guid);
        }

        internal static void RemoveProgressionFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase feature)
        {
            if (feature == null)
            {
                return;
            }

            RemoveProgressionFeature(progression, feature.AssetGuid.ToString());
        }

        internal static void ReplaceProgressionFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase oldFeature,
            BlueprintFeatureBase newFeature)
        {
            if (oldFeature == null || newFeature == null)
            {
                return;
            }

            var oldGuid = oldFeature.AssetGuid;
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature => feature != null && feature.AssetGuid == oldGuid ? newFeature : feature));
            }

            ReplaceProgressionUiFeature(progression, oldGuid, newFeature);
        }

        internal static void ReplaceProgressionUiFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase oldFeature,
            BlueprintFeatureBase newFeature)
        {
            if (oldFeature == null || newFeature == null)
            {
                return;
            }

            ReplaceProgressionUiFeature(progression, oldFeature.AssetGuid, newFeature);
        }

        private static void ReplaceProgressionUiFeature(
            BlueprintProgression progression,
            BlueprintGuid oldGuid,
            BlueprintFeatureBase newFeature)
        {
            if (progression == null || newFeature == null)
            {
                return;
            }

            var field = FindField(typeof(UIGroup), "m_Features");
            if (field == null)
            {
                return;
            }

            foreach (var group in progression.UIGroups ?? Array.Empty<UIGroup>())
            {
                var references = field.GetValue(group) as IEnumerable<BlueprintFeatureBaseReference>;
                if (references == null || !references.Any(reference => reference?.Get()?.AssetGuid == oldGuid))
                {
                    continue;
                }

                field.SetValue(
                    group,
                    references
                        .Select(reference => reference?.Get()?.AssetGuid == oldGuid
                            ? BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>(newFeature)
                            : reference)
                        .ToList());
            }
        }

        private static void RemoveProgressionUiFeature(
            BlueprintProgression progression,
            BlueprintGuid oldGuid)
        {
            if (progression == null)
            {
                return;
            }

            var field = FindField(typeof(UIGroup), "m_Features");
            if (field == null)
            {
                return;
            }

            foreach (var group in progression.UIGroups ?? Array.Empty<UIGroup>())
            {
                var references = field.GetValue(group) as IEnumerable<BlueprintFeatureBaseReference>;
                if (references == null || !references.Any(reference => reference?.Get()?.AssetGuid == oldGuid))
                {
                    continue;
                }

                field.SetValue(
                    group,
                    references
                        .Where(reference => reference?.Get()?.AssetGuid != oldGuid)
                        .ToList());
            }
        }

        internal static void ReplaceFeatureReferences(
            BlueprintScriptableObject blueprint,
            BlueprintGuid oldFeatureGuid,
            BlueprintFeature newFeature)
        {
            foreach (var component in blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                foreach (var field in GetInstanceFields(component.GetType()))
                {
                    if (field.FieldType == typeof(BlueprintFeatureReference))
                    {
                        var reference = (BlueprintFeatureReference)field.GetValue(component);
                        if (reference != null && reference.Get()?.AssetGuid == oldFeatureGuid)
                        {
                            field.SetValue(
                                component,
                                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(newFeature));
                        }
                    }
                    else if (field.FieldType == typeof(BlueprintUnitFactReference))
                    {
                        var reference = (BlueprintUnitFactReference)field.GetValue(component);
                        if (reference != null && reference.Get()?.AssetGuid == oldFeatureGuid)
                        {
                            field.SetValue(
                                component,
                                BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(newFeature));
                        }
                    }
                }
            }
        }

        internal static void ReplaceAbilityReferences(
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

        internal static string DeterministicGuid(string seed)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes("wotr_mod:" + seed));
                return new Guid(bytes).ToString("N");
            }
        }

    }
}
