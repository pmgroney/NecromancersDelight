using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Features;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class DraconicBloodlineInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly EvokerInstaller _evoker;

        public DraconicBloodlineInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            EvokerInstaller evoker)
        {
            _blueprints = blueprints;
            _localization = localization;
            _evoker = evoker;
        }

        internal BlueprintFeatureSelection EnsureSelection(BlueprintCharacterClass characterClass = null)
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
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.BlackDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBlackDragonBloodline,
                    "WotrMod_EvokerBloodline_BlackDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.BlueDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBlueDragonBloodline,
                    "WotrMod_EvokerBloodline_BlueDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.BrassDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBrassDragonBloodline,
                    "WotrMod_EvokerBloodline_BrassDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.BronzeDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerBronzeDragonBloodline,
                    "WotrMod_EvokerBloodline_BronzeDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.CopperDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerCopperDragonBloodline,
                    "WotrMod_EvokerBloodline_CopperDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.GoldDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerGoldDragonBloodline,
                    "WotrMod_EvokerBloodline_GoldDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.GreenDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerGreenDragonBloodline,
                    "WotrMod_EvokerBloodline_GreenDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.RedDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerRedDragonBloodline,
                    "WotrMod_EvokerBloodline_RedDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.SilverDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerSilverDragonBloodline,
                    "WotrMod_EvokerBloodline_SilverDragon",
                    characterClass),
                EnsureDragonBloodlineClone(GameBlueprintIds.Progressions.WhiteDragonBloodline,
                    ModBlueprintIds.Progressions.EvokerWhiteDragonBloodline,
                    "WotrMod_EvokerBloodline_WhiteDragon",
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

        private BlueprintProgression EnsureDragonBloodlineClone(
            string donorGuid,
            string newGuid,
            string internalName,
            BlueprintCharacterClass characterClass)
        {
            var donor = _blueprints.Require<BlueprintProgression>(donorGuid, internalName + " donor");
            var bloodline = _blueprints.Get<BlueprintProgression>(newGuid);
            if (bloodline == null)
            {
                bloodline = _blueprints.CloneBlueprint(donor, newGuid, internalName);
                _blueprints.AddCachedBlueprint(newGuid, bloodline);
            }

            _blueprints.CopyUnitFactDisplay(bloodline, donor);
            ConfigureBreathWeaponReplacement(bloodline, donor, internalName, characterClass);
            _evoker.MoveProtectionFromEnergyToCommunal(bloodline, characterClass);
            _blueprints.EnsureCustomClassOwnsProgressionFeatures(bloodline, internalName, characterClass);
            return bloodline;
        }

        private void ConfigureBreathWeaponReplacement(
            BlueprintProgression bloodline,
            BlueprintProgression donor,
            string internalName,
            BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return;
            }

            var sourceBaseFeature = FindBreathBaseFeature(donor);
            if (sourceBaseFeature == null)
            {
                throw new InvalidOperationException(internalName + " donor breath base feature was not found.");
            }

            var sourceFeature = FindGrantedFeature(sourceBaseFeature);
            if (sourceFeature == null)
            {
                throw new InvalidOperationException(internalName + " donor breath feature was not found.");
            }

            var sourceAbility = FindGrantedAbility(sourceFeature);
            if (sourceAbility == null)
            {
                throw new InvalidOperationException(internalName + " donor breath ability was not found.");
            }

            var ability = EnsureBreathAbilityClone(sourceAbility, internalName, characterClass);
            var feature = EnsureBreathFeatureClone(sourceFeature, sourceAbility, ability, internalName, characterClass);
            var baseFeature = EnsureBreathBaseFeatureClone(sourceBaseFeature, sourceFeature, feature, internalName);
            EvokerInstaller.ReplaceProgressionFeature(bloodline, sourceBaseFeature, baseFeature);
            EvokerInstaller.ReplaceProgressionUiFeature(bloodline, sourceBaseFeature, baseFeature);

            var sourceExtraUse = FindBreathExtraUseFeature(donor);
            if (sourceExtraUse != null)
            {
                var extraUse = EnsureBreathExtraUseFeatureClone(sourceExtraUse, internalName);
                EvokerInstaller.ReplaceProgressionFeature(bloodline, sourceExtraUse, extraUse);
                EvokerInstaller.ReplaceProgressionUiFeature(bloodline, sourceExtraUse, extraUse);
            }
        }

        private BlueprintAbility EnsureBreathAbilityClone(
            BlueprintAbility sourceAbility,
            string internalName,
            BlueprintCharacterClass characterClass)
        {
            var abilityGuid = EvokerInstaller.DeterministicGuid(internalName + ".BreathAbility");
            var ability = _blueprints.Get<BlueprintAbility>(abilityGuid);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(sourceAbility, abilityGuid, internalName + "_BreathAbility");
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }

            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(ability))
            {
                _blueprints.ConfigureContextRankConfig(
                    rank,
                    baseValueType: ContextRankBaseValueType.ClassLevel,
                    characterClass: characterClass);
                _blueprints.SetContextRankMinimum(rank, 1);
            }

            foreach (var action in GetActions(ability).OfType<ContextActionDealDamage>())
            {
                if (action.Value == null)
                {
                    continue;
                }

                action.Value.DiceType = DiceType.D8;
                action.Value.BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 };
            }

            _blueprints.SetUnitFactDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponDescription));
            return ability;
        }

        private BlueprintFeature EnsureBreathFeatureClone(
            BlueprintFeature sourceFeature,
            BlueprintAbility sourceAbility,
            BlueprintAbility ability,
            string internalName,
            BlueprintCharacterClass characterClass)
        {
            var featureGuid = EvokerInstaller.DeterministicGuid(internalName + ".BreathFeature");
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(sourceFeature, featureGuid, internalName + "_BreathFeature");
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            EvokerInstaller.ReplaceAbilityReferences(feature, sourceAbility.AssetGuid.ToString(), ability);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);

            var damage = _blueprints.EnsureComponent(
                feature,
                () => new ClassLevelBreathDamageScaling { name = "$ClassLevelBreathDamageScaling$" + internalName });
            damage.Ability = ability;
            damage.CharacterClass = characterClass;

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponDescription));
            return feature;
        }

        private BlueprintFeature EnsureBreathBaseFeatureClone(
            BlueprintFeature sourceBaseFeature,
            BlueprintFeature sourceFeature,
            BlueprintFeature feature,
            string internalName)
        {
            var baseFeatureGuid = EvokerInstaller.DeterministicGuid(internalName + ".BreathBaseFeature");
            var baseFeature = _blueprints.Get<BlueprintFeature>(baseFeatureGuid);
            if (baseFeature == null)
            {
                baseFeature = _blueprints.CloneBlueprint(sourceBaseFeature, baseFeatureGuid, internalName + "_BreathBaseFeature");
                _blueprints.AddCachedBlueprint(baseFeatureGuid, baseFeature);
            }

            EvokerInstaller.ReplaceFeatureReferences(baseFeature, sourceFeature.AssetGuid, feature);
            _blueprints.SetUnitFactDisplay(
                baseFeature,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponDescription));
            return baseFeature;
        }

        private BlueprintFeature EnsureBreathExtraUseFeatureClone(
            BlueprintFeature sourceFeature,
            string internalName)
        {
            var featureGuid = EvokerInstaller.DeterministicGuid(internalName + ".BreathExtraUseFeature");
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(sourceFeature, featureGuid, internalName + "_BreathExtraUseFeature");
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponName),
                _localization.Text(LocalizationIds.Mod.DraconicEvokerBreathWeaponDescription));
            return feature;
        }

        private static BlueprintFeature FindBreathBaseFeature(BlueprintProgression progression)
        {
            return (progression.LevelEntries ?? Array.Empty<LevelEntry>())
                .Where(entry => entry.Level == 9)
                .SelectMany(entry => entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .OfType<BlueprintFeature>()
                .FirstOrDefault(feature => (feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    .Any(component => component.GetType().Name == "AddFeatureIfHasFact"));
        }

        private static BlueprintFeature FindBreathExtraUseFeature(BlueprintProgression progression)
        {
            return (progression.LevelEntries ?? Array.Empty<LevelEntry>())
                .Where(entry => entry.Level >= 17)
                .SelectMany(entry => entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .OfType<BlueprintFeature>()
                .FirstOrDefault(feature => (feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    .Any(component => component.GetType().Name == "IncreaseResourceAmount"));
        }

        private static BlueprintFeature FindGrantedFeature(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component.GetType().Name != "AddFeatureIfHasFact")
                {
                    continue;
                }

                var field = FindField(component.GetType(), "m_Feature");
                var value = field?.GetValue(component);
                var reference = value as BlueprintFeatureReference;
                var factReference = value as BlueprintUnitFactReference;
                var grantedFeature = reference?.Get();
                if (grantedFeature == null)
                {
                    grantedFeature = factReference?.Get() as BlueprintFeature;
                }

                if (grantedFeature != null)
                {
                    return grantedFeature;
                }
            }

            return null;
        }

        private static BlueprintAbility FindGrantedAbility(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component.GetType().Name != "AddFacts")
                {
                    continue;
                }

                var field = FindField(component.GetType(), "m_Facts");
                var references = field?.GetValue(component) as BlueprintUnitFactReference[];
                var ability = references?
                    .Select(reference => reference?.Get())
                    .OfType<BlueprintAbility>()
                    .FirstOrDefault();
                if (ability != null)
                {
                    return ability;
                }
            }

            return null;
        }

        private IEnumerable<GameAction> GetActions(BlueprintAbility ability)
        {
            return _blueprints.GetComponents<AbilityEffectRunAction>(ability)
                .SelectMany(runAction => runAction.Actions?.Actions ?? Array.Empty<GameAction>());
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
    }
}
