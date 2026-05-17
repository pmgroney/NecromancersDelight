using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Features;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class ElementalBloodlineInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly GrantedSpellFeatureFactory _grantedSpellFeatures;
        private readonly EvokerInstaller _evoker;

        public ElementalBloodlineInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            GrantedSpellFeatureFactory grantedSpellFeatures,
            EvokerInstaller evoker)
        {
            _blueprints = blueprints;
            _localization = localization;
            _grantedSpellFeatures = grantedSpellFeatures;
            _evoker = evoker;
        }

        internal BlueprintFeatureSelection EnsureSelection(BlueprintCharacterClass characterClass = null)
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
                    SpellEffectTheme.Electric,
                    characterClass),
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
                    SpellEffectTheme.Acid,
                    characterClass),
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
                    SpellEffectTheme.Fire,
                    characterClass),
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
                    SpellEffectTheme.Cold,
                    characterClass)
            };

            _blueprints.SetFeatureSelectionFeatures(selection, bloodlines);
            _blueprints.SetFeatureSelectionAllFeatures(selection, bloodlines);

            if (characterClass != null)
            {
                _blueprints.SetProgressionClassesShallow(selection, characterClass);
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

        internal BlueprintProgression EnsureEvokerArcaneBloodline(BlueprintCharacterClass characterClass)
        {
            var progression = EnsureEvokerBloodline(
                GameBlueprintIds.Progressions.ArcaneBloodline,
                ModBlueprintIds.Progressions.EvokerArcaneBloodline,
                "WotrMod_EvokerBloodline_Arcane",
                LocalizationIds.Mod.EvokerArcaneName,
                LocalizationIds.Mod.EvokerArcaneDescription);
            var forceArcana = EnsureEvokerForceArcanaFeature(characterClass);
            EvokerInstaller.ReplaceProgressionFeature(
                progression,
                GameBlueprintIds.Features.BloodlineArcaneArcaneBondFeature,
                forceArcana);
            EvokerInstaller.ReplaceProgressionFeature(
                progression,
                GameBlueprintIds.Features.BloodlineArcaneNewArcanaSelection,
                EnsureArcanistNewArcanaSelection(characterClass));
            _blueprints.RemoveFeatureFromProgression(
                progression,
                GameBlueprintIds.Features.BloodlineArcaneSchoolPowerSelection);
            _blueprints.RemoveFeatureFromProgression(
                progression,
                GameBlueprintIds.Selections.SorcererFeatSelection);
            _blueprints.MoveFeatureToLevel(
                progression,
                GameBlueprintIds.Features.BloodlineArcaneSpellLevel1,
                FindProgressionFeature(progression, GameBlueprintIds.Features.BloodlineArcaneSpellLevel1),
                2);
            if (characterClass != null)
            {
                _blueprints.EnsureCustomClassOwnsProgressionFeatures(
                    progression,
                    "WotrMod_EvokerBloodline_Arcane",
                    characterClass);
            }

            return progression;
        }

        internal BlueprintFeatureSelection EnsureArcanistNewArcanaSelection(BlueprintCharacterClass characterClass)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.ArcanistEvokerNewArcana);
            if (selection == null)
            {
                var source = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Features.BloodlineArcaneNewArcanaSelection,
                    "Arcane bloodline New Arcana selection");
                selection = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Selections.ArcanistEvokerNewArcana,
                    "WotrMod_ArcanistEvokerNewArcanaSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.ArcanistEvokerNewArcana, selection);
            }

            var feature = EnsureArcanistNewArcanaFeature(characterClass);
            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.ArcanistEvokerNewArcanaName),
                _localization.Text(LocalizationIds.Mod.ArcanistEvokerNewArcanaDescription));
            _blueprints.SetComponents(selection);
            _blueprints.SetFeatureSelectionFeatures(selection, new BlueprintFeature[] { feature });
            _blueprints.SetFeatureSelectionAllFeatures(selection, new BlueprintFeature[] { feature });
            if (characterClass != null)
            {
                _blueprints.SetProgressionClassesShallow(selection, characterClass);
            }

            return selection;
        }

        private BlueprintParametrizedFeature EnsureArcanistNewArcanaFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintParametrizedFeature>(ModBlueprintIds.Features.ArcanistEvokerNewArcana);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintParametrizedFeature>(
                    GameBlueprintIds.Features.BloodlineArcaneNewArcanaFeature,
                    "Arcane bloodline New Arcana feature");
                feature = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Features.ArcanistEvokerNewArcana,
                    "WotrMod_ArcanistEvokerNewArcanaFeature");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ArcanistEvokerNewArcana, feature);
            }

            var wizardSpellList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Wizard,
                "Wizard spell list");
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ArcanistEvokerNewArcanaName),
                _localization.Text(LocalizationIds.Mod.ArcanistEvokerNewArcanaDescription));
            var learnSpell = new LearnSpellParametrized
            {
                name = "$LearnSpellParametrized$ArcanistEvokerNewArcana",
                SpecificSpellLevel = false,
                SpellLevelPenalty = 0,
                SpellLevel = 0
            };
            _blueprints.SetLearnSpellParametrizedSource(feature, learnSpell, characterClass, wizardSpellList);
            _blueprints.SetComponents(feature, learnSpell);
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureEvokerForceArcanaFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureEvokerElementalArcanaFeature(
                GameBlueprintIds.Features.BloodlineElementalFireArcana,
                GameBlueprintIds.Abilities.BloodlineElementalFireArcanaAbility,
                GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff,
                ModBlueprintIds.Features.EvokerForceArcana,
                ModBlueprintIds.Abilities.EvokerForceArcana,
                ModBlueprintIds.Buffs.EvokerForceArcana,
                "WotrMod_EvokerForceArcanaFeature",
                "WotrMod_EvokerForceArcanaAbility",
                "WotrMod_EvokerForceArcanaBuff",
                SpellEffectTheme.Force);

            ConfigureEvokerForceArcanaDisplay(feature);
            _blueprints.SetProgressionClasses(feature, characterClass);

            var ability = _blueprints.Require<BlueprintActivatableAbility>(
                ModBlueprintIds.Abilities.EvokerForceArcana,
                "Evoker force arcana ability");
            ConfigureEvokerForceArcanaDisplay(ability);

            var buff = _blueprints.Require<BlueprintBuff>(
                ModBlueprintIds.Buffs.EvokerForceArcana,
                "Evoker force arcana buff");
            ConfigureEvokerForceArcanaBuff(buff);

            return feature;
        }

        private void ConfigureEvokerForceArcanaBuff(BlueprintBuff buff)
        {
            var components = _blueprints
                .GetComponents<BlueprintComponent>(buff)
                .Where(component => !(component is ChangeSpellElementalDamage))
                .ToList();
            if (!components.OfType<EvokerForceSpellConversion>().Any())
            {
                components.Add(new EvokerForceSpellConversion
                {
                    name = "$EvokerForceSpellConversion$EvokerForceArcana"
                });
            }

            _blueprints.SetComponents(buff, components.ToArray());
            _evoker.ReplaceDescriptor(buff, SpellDescriptor.Fire, SpellDescriptor.Force);

            var themeToggle = _blueprints.GetComponents<SpellEffectThemeToggleComponent>(buff).FirstOrDefault();
            if (themeToggle == null)
            {
                themeToggle = new SpellEffectThemeToggleComponent
                {
                    name = "$SpellEffectThemeToggleComponent$EvokerForceArcana"
                };
                _blueprints.AddComponent(buff, themeToggle);
            }

            themeToggle.Theme = SpellEffectTheme.Force;
            ConfigureEvokerForceArcanaDisplay(buff);
        }

        private void ConfigureEvokerForceArcanaDisplay(BlueprintUnitFact fact)
        {
            _blueprints.SetUnitFactDisplay(
                fact,
                _localization.Text(LocalizationIds.Mod.EvokerForceArcanaName),
                _localization.Text(LocalizationIds.Mod.EvokerForceArcanaDescription));
            _evoker.SetIcon(fact, "Icons\\force_arcana.png");
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
            SpellEffectTheme theme,
            BlueprintCharacterClass characterClass)
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
            EvokerInstaller.ReplaceProgressionFeature(progression, sourceArcanaFeatureGuid, arcana);
            ConfigureElementalRay(progression, theme, characterClass);
            var hellfireRayKnownSpell = EnsureElementalHellfireRayKnownSpell(theme, characterClass);
            if (hellfireRayKnownSpell != null)
            {
                _blueprints.MoveFeatureToLevel(
                    progression,
                    GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                    hellfireRayKnownSpell,
                    12);
            }

            _blueprints.RemoveFeatureFromProgression(progression, GameBlueprintIds.Selections.SorcererFeatSelection);
            MoveProtectionFromEnergyToCommunal(progression, characterClass);
            AddElementalBodySpellUiGroup(progression);
            _blueprints.EnsureCustomClassOwnsProgressionFeatures(progression, internalName, characterClass);
            return progression;
        }

        private void ConfigureElementalRay(
            BlueprintProgression progression,
            SpellEffectTheme theme,
            BlueprintCharacterClass characterClass)
        {
            switch (theme)
            {
                case SpellEffectTheme.Electric:
                    ReplaceElementalRay(
                        progression,
                        GameBlueprintIds.Features.BloodlineElementalAirElementalRayFeature,
                        GameBlueprintIds.Abilities.BloodlineElementalAirElementalRayAbility,
                        ModBlueprintIds.Features.EvokerAirElementalRay,
                        ModBlueprintIds.Abilities.EvokerAirElementalRay,
                        "WotrMod_EvokerAirElementalRayFeature",
                        "WotrMod_EvokerAirElementalRayAbility",
                        ModBlueprintIds.Features.LegacyEvokerAirElementalRayOwned,
                        characterClass);
                    return;
                case SpellEffectTheme.Acid:
                    ReplaceElementalRay(
                        progression,
                        GameBlueprintIds.Features.BloodlineElementalEarthElementalRayFeature,
                        GameBlueprintIds.Abilities.BloodlineElementalEarthElementalRayAbility,
                        ModBlueprintIds.Features.EvokerEarthElementalRay,
                        ModBlueprintIds.Abilities.EvokerEarthElementalRay,
                        "WotrMod_EvokerEarthElementalRayFeature",
                        "WotrMod_EvokerEarthElementalRayAbility",
                        ModBlueprintIds.Features.LegacyEvokerEarthElementalRayOwned,
                        characterClass);
                    return;
                case SpellEffectTheme.Fire:
                    ReplaceElementalRay(
                        progression,
                        GameBlueprintIds.Features.BloodlineElementalFireElementalRayFeature,
                        GameBlueprintIds.Abilities.BloodlineElementalFireElementalRayAbility,
                        ModBlueprintIds.Features.EvokerFireElementalRay,
                        ModBlueprintIds.Abilities.EvokerFireElementalRay,
                        "WotrMod_EvokerFireElementalRayFeature",
                        "WotrMod_EvokerFireElementalRayAbility",
                        ModBlueprintIds.Features.LegacyEvokerFireElementalRayOwned,
                        characterClass);
                    return;
                case SpellEffectTheme.Cold:
                    ReplaceElementalRay(
                        progression,
                        GameBlueprintIds.Features.BloodlineElementalWaterElementalRayFeature,
                        GameBlueprintIds.Abilities.BloodlineElementalWaterElementalRayAbility,
                        ModBlueprintIds.Features.EvokerWaterElementalRay,
                        ModBlueprintIds.Abilities.EvokerWaterElementalRay,
                        "WotrMod_EvokerWaterElementalRayFeature",
                        "WotrMod_EvokerWaterElementalRayAbility",
                        ModBlueprintIds.Features.LegacyEvokerWaterElementalRayOwned,
                        characterClass);
                    return;
                default:
                    throw new InvalidOperationException($"Unsupported elemental bloodline theme {theme}.");
            }
        }

        private void ReplaceElementalRay(
            BlueprintProgression progression,
            string sourceFeatureGuid,
            string sourceAbilityGuid,
            string featureGuid,
            string abilityGuid,
            string featureName,
            string abilityName,
            string legacyFeatureGuid,
            BlueprintCharacterClass characterClass)
        {
            var feature = EnsureElementalRayFeature(
                sourceFeatureGuid,
                sourceAbilityGuid,
                featureGuid,
                abilityGuid,
                featureName,
                abilityName,
                characterClass);
            EnsureLegacyElementalRayFeature(
                sourceFeatureGuid,
                sourceAbilityGuid,
                legacyFeatureGuid,
                featureName + "_LegacyOwned",
                _blueprints.Require<BlueprintAbility>(abilityGuid, abilityName));
            EvokerInstaller.ReplaceProgressionFeature(progression, sourceFeatureGuid, feature);
            EvokerInstaller.ReplaceProgressionFeature(progression, legacyFeatureGuid, feature);
        }

        private BlueprintFeature EnsureElementalRayFeature(
            string sourceFeatureGuid,
            string sourceAbilityGuid,
            string featureGuid,
            string abilityGuid,
            string featureName,
            string abilityName,
            BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(sourceFeatureGuid, featureName + " donor"),
                    featureGuid,
                    featureName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            var ability = EnsureElementalRayAbility(sourceAbilityGuid, abilityGuid, abilityName, characterClass);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            EvokerInstaller.ReplaceAbilityReferences(feature, sourceAbilityGuid, ability);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.EvokerElementalRayName),
                _localization.Text(LocalizationIds.Mod.EvokerElementalRayDescription));
            if (ability.Icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, ability.Icon);
            }

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureLegacyElementalRayFeature(
            string sourceFeatureGuid,
            string sourceAbilityGuid,
            string legacyFeatureGuid,
            string legacyFeatureName,
            BlueprintAbility currentAbility)
        {
            var feature = _blueprints.Get<BlueprintFeature>(legacyFeatureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(sourceFeatureGuid, legacyFeatureName + " donor"),
                    legacyFeatureGuid,
                    legacyFeatureName);
                _blueprints.AddCachedBlueprint(legacyFeatureGuid, feature);
            }

            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, currentAbility);
            }

            EvokerInstaller.ReplaceAbilityReferences(feature, sourceAbilityGuid, currentAbility);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.EvokerElementalRayName),
                _localization.Text(LocalizationIds.Mod.EvokerElementalRayDescription));
            if (currentAbility.Icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, currentAbility.Icon);
            }

            return feature;
        }

        private BlueprintAbility EnsureElementalRayAbility(
            string sourceAbilityGuid,
            string abilityGuid,
            string abilityName,
            BlueprintCharacterClass characterClass)
        {
            var ability = _blueprints.Get<BlueprintAbility>(abilityGuid);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintAbility>(sourceAbilityGuid, abilityName + " donor"),
                    abilityGuid,
                    abilityName);
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }

            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.EvokerElementalRayName),
                _localization.Text(LocalizationIds.Mod.EvokerElementalRayDescription));
            _evoker.ConfigureElementalRayDamage(ability, characterClass);
            return ability;
        }

        internal void MoveProtectionFromEnergyToCommunal(
            BlueprintProgression progression,
            BlueprintCharacterClass characterClass)
        {
            var protectionFromEnergyCommunal = EnsureProtectionFromEnergyCommunalKnownSpell(characterClass);
            _blueprints.MoveFeatureToLevel(
                progression,
                GameBlueprintIds.Features.BloodlineElementalSpellLevel3,
                protectionFromEnergyCommunal,
                8);
        }

        private BlueprintFeature EnsureProtectionFromEnergyCommunalKnownSpell(BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return null;
            }

            var feature = _blueprints.Get<BlueprintFeature>(
                ModBlueprintIds.Features.EvokerProtectionFromEnergyCommunalKnownSpell);
            if (feature == null)
            {
                var source = _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.BloodlineElementalSpellLevel3,
                    "Protection from Energy bloodline spell donor");
                feature = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Features.EvokerProtectionFromEnergyCommunalKnownSpell,
                    "WotrMod_EvokerProtectionFromEnergyCommunalKnownSpell");
                _blueprints.AddCachedBlueprint(
                    ModBlueprintIds.Features.EvokerProtectionFromEnergyCommunalKnownSpell,
                    feature);
            }

            var spell = _blueprints.Require<BlueprintAbility>(
                GameBlueprintIds.Spells.ProtectionFromEnergyCommunal,
                "Protection from Energy Communal spell");
            var addKnownSpell = new AddKnownSpell { name = "$AddKnownSpell$EvokerProtectionFromEnergyCommunal" };
            _blueprints.SetAddKnownSpell(addKnownSpell, characterClass, spell, 4);
            _blueprints.SetComponents(feature, addKnownSpell);
            _blueprints.CopyUnitFactDisplay(feature, spell);
            return feature;
        }

        private BlueprintFeature EnsureElementalHellfireRayKnownSpell(
            SpellEffectTheme theme,
            BlueprintCharacterClass characterClass)
        {
            switch (theme)
            {
                case SpellEffectTheme.Electric:
                    return EnsureKnownSpellFeature(
                        GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                        ModBlueprintIds.Features.EvokerAirHellfireRayKnownSpell,
                        "WotrMod_EvokerAirHellfireRayKnownSpell",
                        ModBlueprintIds.Spells.ElectricHellfireRay,
                        "wotr_mod.spell.electric_hellfire_ray.name",
                        "wotr_mod.spell.electric_hellfire_ray.description",
                        6,
                        null,
                        characterClass);
                case SpellEffectTheme.Acid:
                    return EnsureKnownSpellFeature(
                        GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                        ModBlueprintIds.Features.EvokerEarthHellfireRayKnownSpell,
                        "WotrMod_EvokerEarthHellfireRayKnownSpell",
                        ModBlueprintIds.Spells.AcidHellfireRay,
                        "wotr_mod.spell.acid_hellfire_ray.name",
                        "wotr_mod.spell.acid_hellfire_ray.description",
                        6,
                        null,
                        characterClass);
                case SpellEffectTheme.Fire:
                    return EnsureKnownSpellFeature(
                        GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                        ModBlueprintIds.Features.EvokerFireHellfireRayKnownSpell,
                        "WotrMod_EvokerFireHellfireRayKnownSpell",
                        ModBlueprintIds.Spells.FireHellfireRay,
                        "wotr_mod.spell.fire_hellfire_ray.name",
                        "wotr_mod.spell.fire_hellfire_ray.description",
                        6,
                        null,
                        characterClass);
                case SpellEffectTheme.Cold:
                    return EnsureKnownSpellFeature(
                        GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                        ModBlueprintIds.Features.EvokerWaterHellfireRayKnownSpell,
                        "WotrMod_EvokerWaterHellfireRayKnownSpell",
                        ModBlueprintIds.Spells.ColdHellfireRay,
                        "wotr_mod.spell.cold_hellfire_ray.name",
                        "wotr_mod.spell.cold_hellfire_ray.description",
                        6,
                        null,
                        characterClass);
                default:
                    throw new InvalidOperationException($"Unsupported elemental bloodline theme {theme}.");
            }
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
            EvokerInstaller.ReplaceBuffReferences(ability, sourceBuffGuid, buff);
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

        private BlueprintFeature EnsureKnownSpellFeature(
            string sourceFeatureGuid,
            string featureGuid,
            string featureName,
            string spellGuid,
            string displayNameKey,
            string descriptionKey,
            int spellLevel,
            string iconPath,
            BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return null;
            }

            return _grantedSpellFeatures.Ensure(
                sourceFeatureGuid,
                featureGuid,
                featureName,
                featureName + " donor",
                spellGuid,
                featureName + " spell",
                displayNameKey,
                descriptionKey,
                spellLevel,
                characterClass,
                iconPath);
        }

        private void AddElementalBodySpellUiGroup(BlueprintProgression progression)
        {
            _blueprints.AddProgressionUiGroup(
                progression,
                FindProgressionFeature(progression, GameBlueprintIds.Features.BloodlineElementalSpellLevel4),
                FindProgressionFeature(progression, GameBlueprintIds.Features.BloodlineElementalSpellLevel5),
                FindProgressionFeature(progression, GameBlueprintIds.Features.BloodlineElementalSpellLevel6),
                FindProgressionFeature(progression, GameBlueprintIds.Features.BloodlineElementalSpellLevel7),
                FindProgressionFeature(progression, GameBlueprintIds.Features.BloodlineElementalSpellLevel9));
        }

        private static BlueprintFeatureBase FindProgressionFeature(
            BlueprintProgression progression,
            string featureGuid)
        {
            var guid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(featureGuid));
            return (progression.LevelEntries ?? Array.Empty<LevelEntry>())
                .SelectMany(entry => entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .FirstOrDefault(feature => feature != null && feature.AssetGuid == guid);
        }
    }
}
