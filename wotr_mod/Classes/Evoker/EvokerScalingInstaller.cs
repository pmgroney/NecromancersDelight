using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class EvokerScalingInstaller
    {
        private static readonly int[] ElementScalingLevels = { 1, 5, 9, 13, 17 };
        private static readonly int[] ElementalCapstoneScalingLevels = { 1, 5, 9, 13, 17, 20 };
        private static readonly int[] ArcaneScalingLevels = { 1, 4, 8, 12, 16, 20 };

        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

        public EvokerScalingInstaller(
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

        public void Install(BlueprintCharacterClass characterClass)
        {
            ApplyArcane(
                ModBlueprintIds.Progressions.EvokerArcaneBloodline,
                ModBlueprintIds.Features.EvokerScalingArcane,
                "WotrMod_EvokerScaling_Arcane",
                LocalizationIds.Mod.EvokerScalingArcaneName,
                LocalizationIds.Mod.EvokerScalingArcaneDescription,
                "Icons\\arcane_supremacy.png",
                characterClass);
            ApplyElement(
                ModBlueprintIds.Progressions.EvokerAirBloodline,
                ModBlueprintIds.Features.EvokerScalingAir,
                "WotrMod_EvokerScaling_Air",
                LocalizationIds.Mod.EvokerScalingAirName,
                LocalizationIds.Mod.EvokerScalingAirDescription,
                "Icons\\tempest_surge.png",
                DamageEnergyType.Electricity,
                conversionBuffGuid: null,
                additionalAbilityGuids: new[] { ModBlueprintIds.Abilities.EvokerAirElementalRay },
                characterClass: characterClass,
                hasLevel20DamageCapstone: true);
            ApplyElement(
                ModBlueprintIds.Progressions.EvokerEarthBloodline,
                ModBlueprintIds.Features.EvokerScalingEarth,
                "WotrMod_EvokerScaling_Earth",
                LocalizationIds.Mod.EvokerScalingEarthName,
                LocalizationIds.Mod.EvokerScalingEarthDescription,
                "Icons\\corrosive_mastery.png",
                DamageEnergyType.Acid,
                conversionBuffGuid: null,
                additionalAbilityGuids: new[] { ModBlueprintIds.Abilities.EvokerEarthElementalRay },
                characterClass: characterClass,
                hasLevel20DamageCapstone: true);
            ApplyElement(
                ModBlueprintIds.Progressions.EvokerFireBloodline,
                ModBlueprintIds.Features.EvokerScalingFire,
                "WotrMod_EvokerScaling_Fire",
                LocalizationIds.Mod.EvokerScalingFireName,
                LocalizationIds.Mod.EvokerScalingFireDescription,
                "Icons\\infernal_potency.png",
                DamageEnergyType.Fire,
                conversionBuffGuid: null,
                additionalAbilityGuids: new[] { ModBlueprintIds.Abilities.EvokerFireElementalRay },
                characterClass: characterClass,
                hasLevel20DamageCapstone: true);
            ApplyElement(
                ModBlueprintIds.Progressions.EvokerWaterBloodline,
                ModBlueprintIds.Features.EvokerScalingWater,
                "WotrMod_EvokerScaling_Water",
                LocalizationIds.Mod.EvokerScalingWaterName,
                LocalizationIds.Mod.EvokerScalingWaterDescription,
                "Icons\\glacial_dominion.png",
                DamageEnergyType.Cold,
                conversionBuffGuid: null,
                additionalAbilityGuids: new[] { ModBlueprintIds.Abilities.EvokerWaterElementalRay },
                characterClass: characterClass,
                hasLevel20DamageCapstone: true);
            ApplyElement(
                ModBlueprintIds.Progressions.ShadowbornBloodline,
                ModBlueprintIds.Features.ShadowbornScaling,
                "WotrMod_EvokerScaling_Shadowborn",
                LocalizationIds.Mod.ShadowbornScalingName,
                LocalizationIds.Mod.ShadowbornScalingDescription,
                "Icons\\umbral_potency.png",
                DamageEnergyType.NegativeEnergy,
                conversionBuffGuid: ModBlueprintIds.Buffs.ShadowbornArcana,
                additionalAbilityGuids: new[] { ModBlueprintIds.Abilities.ShadowbornUmbralRay },
                characterClass: characterClass,
                bindProgressionToClass: false);
        }

        private void ApplyElement(
            string progressionGuid,
            string featureGuid,
            string internalName,
            string nameKey,
            string descriptionKey,
            string iconPath,
            DamageEnergyType energyType,
            string conversionBuffGuid,
            string[] additionalAbilityGuids,
            BlueprintCharacterClass characterClass,
            bool bindProgressionToClass = true,
            bool hasLevel20DamageCapstone = false)
        {
            var progression = _blueprints.Get<BlueprintProgression>(progressionGuid);
            if (progression == null)
            {
                _logger.Warning($"Could not find Evoker scaling progression {progressionGuid} for {internalName}.");
                return;
            }

            var feature = EnsureElementFeature(
                featureGuid,
                internalName,
                nameKey,
                descriptionKey,
                iconPath,
                energyType,
                conversionBuffGuid,
                additionalAbilityGuids,
                hasLevel20DamageCapstone,
                characterClass);
            AddScalingFeature(
                progression,
                feature,
                hasLevel20DamageCapstone ? ElementalCapstoneScalingLevels : ElementScalingLevels,
                characterClass,
                bindProgressionToClass);
        }

        private void ApplyElement(
            string progressionGuid,
            string featureGuid,
            string internalName,
            string nameKey,
            string descriptionKey,
            string iconPath,
            DamageEnergyType energyType,
            BlueprintCharacterClass characterClass)
        {
            ApplyElement(
                progressionGuid,
                featureGuid,
                internalName,
                nameKey,
                descriptionKey,
                iconPath,
                energyType,
                conversionBuffGuid: null,
                additionalAbilityGuids: null,
                characterClass: characterClass);
        }

        private void ApplyArcane(
            string progressionGuid,
            string featureGuid,
            string internalName,
            string nameKey,
            string descriptionKey,
            string iconPath,
            BlueprintCharacterClass characterClass)
        {
            var progression = _blueprints.Get<BlueprintProgression>(progressionGuid);
            if (progression == null)
            {
                _logger.Warning($"Could not find Evoker scaling progression {progressionGuid} for {internalName}.");
                return;
            }

            var feature = EnsureArcaneFeature(
                featureGuid,
                internalName,
                nameKey,
                descriptionKey,
                iconPath,
                characterClass);
            AddScalingFeature(progression, feature, ArcaneScalingLevels, characterClass);
        }

        private BlueprintFeature EnsureElementFeature(
            string featureGuid,
            string internalName,
            string nameKey,
            string descriptionKey,
            string iconPath,
            DamageEnergyType energyType,
            string conversionBuffGuid,
            string[] additionalAbilityGuids,
            bool hasLevel20DamageCapstone,
            BlueprintCharacterClass characterClass)
        {
            var scalingLevels = hasLevel20DamageCapstone ? ElementalCapstoneScalingLevels : ElementScalingLevels;
            var feature = EnsureScalingFeature(
                featureGuid,
                internalName,
                nameKey,
                descriptionKey,
                iconPath,
                scalingLevels.Length,
                characterClass);
            var conversionBuff = string.IsNullOrWhiteSpace(conversionBuffGuid)
                ? null
                : _blueprints.Get<BlueprintBuff>(conversionBuffGuid);
            var additionalAbilities = (additionalAbilityGuids ?? Array.Empty<string>())
                .Select(guid => _blueprints.Get<BlueprintAbility>(guid))
                .Where(ability => ability != null)
                .ToArray();
            _blueprints.SetComponents(
                feature,
                new PerDieBonusDamage
                {
                    name = "$PerDieBonusDamage$" + internalName,
                    Classes = new[] { characterClass },
                    IncludeClassSpellbookSpells = true,
                    MatchEnergyDamage = true,
                    EnergyType = energyType,
                    CountAnyEnergyDamageWhileConversionBuffActive = conversionBuff != null,
                    ConversionBuff = conversionBuff,
                    AdditionalAbilities = additionalAbilities,
                    CapstoneRank = hasLevel20DamageCapstone ? scalingLevels.Length : 0,
                    CapstoneBonusDamagePerDie = hasLevel20DamageCapstone ? 1 : 0
                });
            return feature;
        }

        private BlueprintFeature EnsureArcaneFeature(
            string featureGuid,
            string internalName,
            string nameKey,
            string descriptionKey,
            string iconPath,
            BlueprintCharacterClass characterClass)
        {
            var feature = EnsureScalingFeature(
                featureGuid,
                internalName,
                nameKey,
                descriptionKey,
                iconPath,
                ArcaneScalingLevels.Length,
                characterClass);
            _blueprints.SetComponents(
                feature,
                new EvokerArcaneDcScaling
                {
                    name = "$EvokerArcaneDcScaling$" + internalName,
                    Classes = new[] { characterClass }
                },
                new PerDieBonusDamage
                {
                    name = "$PerDieBonusDamage$" + internalName,
                    Classes = new[] { characterClass },
                    MatchForceDamage = true,
                    AdditionalAbilities = new[]
                    {
                        _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Spells.ForceRay),
                        _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.EvokerForceRay)
                    }.Where(ability => ability != null).ToArray()
                });
            return feature;
        }

        private BlueprintFeature EnsureScalingFeature(
            string featureGuid,
            string internalName,
            string nameKey,
            string descriptionKey,
            string iconPath,
            int ranks,
            BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = internalName,
                    AssetGuid = BlueprintGuid.Parse(featureGuid)
                };
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = ranks;
            feature.HideInUI = false;
            feature.HideInCharacterSheetAndLevelUp = false;
            feature.HideNotAvailibleInUI = false;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(nameKey),
                _localization.Text(descriptionKey));

            var icon = GetScalingIcon(featureGuid, internalName, iconPath);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private UnityEngine.Sprite GetScalingIcon(string featureGuid, string internalName, string iconPath)
        {
            return _icons.Load(iconPath);
        }

        private void AddScalingFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase feature,
            IEnumerable<int> levels,
            BlueprintCharacterClass characterClass,
            bool bindProgressionToClass = true)
        {
            RemoveFeatureFromProgression(progression, feature);
            foreach (var level in levels)
            {
                AddFeatureToLevel(progression, level, feature);
            }

            if (characterClass != null && bindProgressionToClass)
            {
                _blueprints.SetProgressionClassesShallow(progression, characterClass);
            }
        }

        private static void RemoveFeatureFromProgression(
            BlueprintProgression progression,
            BlueprintFeatureBase feature)
        {
            if (progression?.LevelEntries == null || feature == null)
            {
                return;
            }

            var guid = feature.AssetGuid;
            foreach (var entry in progression.LevelEntries)
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Where(existing => existing == null || existing.AssetGuid != guid));
            }

            progression.LevelEntries = progression.LevelEntries
                .Where(entry => (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>()).Any())
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        private static void AddFeatureToLevel(
            BlueprintProgression progression,
            int level,
            BlueprintFeatureBase feature)
        {
            progression.LevelEntries = progression.LevelEntries ?? Array.Empty<LevelEntry>();
            var entry = progression.LevelEntries.FirstOrDefault(levelEntry => levelEntry.Level == level);
            if (entry == null)
            {
                entry = new LevelEntry { Level = level };
                entry.SetFeatures(new[] { feature });
                progression.LevelEntries = progression.LevelEntries.Concat(new[] { entry }).OrderBy(levelEntry => levelEntry.Level).ToArray();
                return;
            }

            var features = (entry.Features ?? new List<BlueprintFeatureBase>()).ToList();
            if (features.All(existing => existing == null || existing.AssetGuid != feature.AssetGuid))
            {
                features.Add(feature);
                entry.SetFeatures(features);
            }
        }
    }
}
