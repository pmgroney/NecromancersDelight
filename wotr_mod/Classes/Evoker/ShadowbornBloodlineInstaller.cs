using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class ShadowbornBloodlineInstaller
    {
        private const float ShadowbornUmbralRayProjectileSpeed = 18f;

        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;
        private readonly GrantedSpellFeatureFactory _grantedSpellFeatures;
        private readonly EvokerInstaller _evoker;

        public ShadowbornBloodlineInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            SpellIconLoader icons,
            GrantedSpellFeatureFactory grantedSpellFeatures,
            EvokerInstaller evoker)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _icons = icons;
            _grantedSpellFeatures = grantedSpellFeatures;
            _evoker = evoker;
        }

        internal BlueprintProgression EnsureBloodline(BlueprintCharacterClass characterClass)
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
            var shadowHellfireRay = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                ModBlueprintIds.Features.ShadowbornHellfireRayKnownSpell,
                "WotrMod_ShadowbornHellfireRayKnownSpell",
                ModBlueprintIds.Spells.ShadowHellfireRay,
                "wotr_mod.spell.shadow_hellfire_ray.name",
                "wotr_mod.spell.shadow_hellfire_ray.description",
                6,
                null,
                characterClass);

            _blueprints.SetUnitFactDisplay(
                bloodline,
                _localization.Text(LocalizationIds.Mod.ShadowbornBloodlineName),
                _localization.Text(LocalizationIds.Mod.ShadowbornBloodlineDescription));
            bloodline.HideInUI = true;
            bloodline.HideInCharacterSheetAndLevelUp = true;
            bloodline.HideNotAvailibleInUI = true;
            _evoker.SetIcon(bloodline, "Icons\\shadowborn_bloodline.png");
            ReplaceFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireArcana,
                arcana);
            EvokerInstaller.ReplaceProgressionUiFeature(
                bloodline,
                _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.BloodlineElementalFireArcana,
                    "Fire bloodline arcana"),
                arcana);
            ReplaceFireProgressionFeature(
                bloodline,
                ModBlueprintIds.Features.EvokerFireArcana,
                arcana);
            EvokerInstaller.ReplaceProgressionUiFeature(
                bloodline,
                _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.EvokerFireArcana),
                arcana);
            ReplaceFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalRayFeature,
                umbralRay);
            ReplaceFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBlastFeature,
                umbralBlast);
            MoveFireProgressionFeatureToLevel(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalSpellLevel9,
                elementalBody,
                14);
            RemoveFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBodyFeature);
            ReplaceFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireResistanceFeature,
                resistance);
            ReplaceFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel1,
                shadowHands);
            ReplaceFireProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel2,
                shadowRay);
            new LivingDarknessInstaller(_blueprints, _localization, _logger, _icons).Install(bloodline, characterClass);
            MoveFireProgressionFeatureToLevel(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalSpellLevel6,
                shadowHellfireRay,
                12);
            _blueprints.RemoveFeatureFromProgression(bloodline, GameBlueprintIds.Selections.SorcererFeatSelection);
            _evoker.MoveProtectionFromEnergyToCommunal(bloodline, characterClass);
            EvokerInstaller.RemoveProgressionFeature(
                bloodline,
                FireBloodlineOwnedFeatureGuid(GameBlueprintIds.Features.BloodlineElementalSpellLevel3));
            RemoveFireProgressionFeature(bloodline, GameBlueprintIds.Features.BloodlineElementalClassSkill);
            RemoveFireProgressionFeature(bloodline, GameBlueprintIds.Features.BloodlineElementalSpellLevel8);
            _blueprints.EnsureCustomClassOwnsProgressionFeatures(
                bloodline,
                "WotrMod_ShadowbornBloodline",
                characterClass);
            RemoveShadowbornOwnedProgressionFeature(bloodline, GameBlueprintIds.Features.BloodlineElementalClassSkill);
            RemoveShadowbornOwnedProgressionFeature(bloodline, GameBlueprintIds.Features.BloodlineElementalSpellLevel8);
            RemoveGrantedSpellFromProgression(bloodline, GameBlueprintIds.Spells.SummonMonsterVIII);

            if (characterClass?.Progression != null)
            {
                _blueprints.AddProgressionUiGroup(characterClass.Progression, shadowHands, shadowRay, shadowHellfireRay);
            }

            return bloodline;
        }

        private void RemoveGrantedSpellFromProgression(BlueprintProgression progression, string spellGuid)
        {
            var grantedSpellGuid = BlueprintGuid.Parse(spellGuid);
            var grants = (progression.LevelEntries ?? Array.Empty<LevelEntry>())
                .SelectMany(entry => entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .Where(feature => GrantsKnownSpell(feature, grantedSpellGuid))
                .GroupBy(feature => feature.AssetGuid)
                .Select(group => group.First())
                .ToArray();

            foreach (var feature in grants)
            {
                _blueprints.RemoveFeatureFromProgression(progression, feature);
            }
        }

        private void RemoveShadowbornOwnedProgressionFeature(
            BlueprintProgression progression,
            string sourceFeatureGuid)
        {
            var ownedFeatureGuid = EvokerInstaller.DeterministicGuid(
                "WotrMod_ShadowbornBloodline.OwnedFeature." + BlueprintTool.NormalizeGuid(sourceFeatureGuid));
            _blueprints.RemoveFeatureFromProgression(progression, ownedFeatureGuid);
        }

        private bool GrantsKnownSpell(BlueprintFeatureBase feature, BlueprintGuid spellGuid)
        {
            if (feature == null)
            {
                return false;
            }

            return _blueprints.GetComponents<AddKnownSpell>(feature)
                .Any(component =>
                    BlueprintFields.AddKnownSpellSpell.GetValue(component) is BlueprintAbilityReference spell
                    && spell.Guid == spellGuid);
        }

        private static void ReplaceFireProgressionFeature(
            BlueprintProgression bloodline,
            string sourceFeatureGuid,
            BlueprintFeatureBase replacement)
        {
            EvokerInstaller.ReplaceProgressionFeature(bloodline, sourceFeatureGuid, replacement);
            EvokerInstaller.ReplaceProgressionFeature(bloodline, FireBloodlineOwnedFeatureGuid(sourceFeatureGuid), replacement);
            foreach (var ownedFeatureGuid in FireBloodlineReplacementFeatureGuids(sourceFeatureGuid))
            {
                EvokerInstaller.ReplaceProgressionFeature(bloodline, ownedFeatureGuid, replacement);
            }
        }

        private void RemoveFireProgressionFeature(
            BlueprintProgression bloodline,
            string sourceFeatureGuid)
        {
            EvokerInstaller.RemoveProgressionFeature(bloodline, sourceFeatureGuid);
            EvokerInstaller.RemoveProgressionFeature(bloodline, FireBloodlineOwnedFeatureGuid(sourceFeatureGuid));
            foreach (var ownedFeatureGuid in FireBloodlineReplacementFeatureGuids(sourceFeatureGuid))
            {
                EvokerInstaller.RemoveProgressionFeature(bloodline, ownedFeatureGuid);
            }
        }

        private void MoveFireProgressionFeatureToLevel(
            BlueprintProgression bloodline,
            string sourceFeatureGuid,
            BlueprintFeatureBase replacement,
            int level)
        {
            if (replacement == null)
            {
                return;
            }

            RemoveFireProgressionFeature(bloodline, sourceFeatureGuid);
            EvokerInstaller.RemoveProgressionFeature(bloodline, replacement);
            _blueprints.AddFeatureToLevel(bloodline, level, replacement);
        }

        internal static string FireBloodlineOwnedFeatureGuid(string sourceFeatureGuid)
        {
            return EvokerInstaller.DeterministicGuid(
                "WotrMod_EvokerBloodline_Fire.OwnedFeature." + BlueprintTool.NormalizeGuid(sourceFeatureGuid));
        }

        private static string[] FireBloodlineReplacementFeatureGuids(string sourceFeatureGuid)
        {
            if (BlueprintTool.NormalizeGuid(sourceFeatureGuid) == GameBlueprintIds.Features.BloodlineElementalFireElementalRayFeature)
            {
                return new[] { ModBlueprintIds.Features.EvokerFireElementalRay };
            }

            return new string[0];
        }

        internal BlueprintFeature EnsureLivingGhostFeature(BlueprintCharacterClass characterClass)
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
            _evoker.SetIcon(feature, "Icons\\living_ghost.png");
            _blueprints.SetProgressionClasses(feature, characterClass);

            return feature;
        }

        internal BlueprintFeature EnsureShadowMasteryFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.ShadowbornShadowMastery);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_ShadowbornShadowMasteryFeature",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.ShadowbornShadowMastery),
                    IsClassFeature = true,
                    Ranks = 1,
                    ReapplyOnLevelUp = false
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.ShadowbornShadowMastery, feature);
            }

            _blueprints.SetComponents(
                feature,
                new AscendantElement
                {
                    name = "$AscendantElement$ShadowbornShadowMastery",
                    Element = DamageEnergyType.NegativeEnergy
                });
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornShadowMasteryName),
                _localization.Text(LocalizationIds.Mod.ShadowbornShadowMasteryDescription));
            _evoker.SetIcon(feature, "Icons\\shadow_mastery.png");
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
            EvokerInstaller.ReplaceBuffReferences(ability, GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff, buff);
            _blueprints.SetComponents(ability);
            _blueprints.SetUnitFactDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostName),
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostDescription));
            _evoker.SetIcon(ability, "Icons\\living_ghost.png");

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
            var blurBuff = _blueprints.Require<BlueprintBuff>(
                GameBlueprintIds.Buffs.Blur,
                "Blur buff");
            buff.FxOnStart = blurBuff.FxOnStart;
            buff.FxOnRemove = blurBuff.FxOnRemove;
            buff.ResourceAssetIds = blurBuff.ResourceAssetIds;
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostName),
                _localization.Text(LocalizationIds.Mod.ShadowbornLivingGhostDescription));
            _evoker.SetIcon(buff, "Icons\\living_ghost.png");

            return buff;
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
            _evoker.SetIcon(feature, "Icons\\umbral_body.png");

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
                EvokerInstaller.ConfigureAddFeatureOnClassLevel(component, characterClass, level1, level2);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceName),
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceDescription));
            _evoker.SetIcon(feature, "Icons\\shadow_resistance.png");
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
            _evoker.SetIcon(feature, "Icons\\shadow_resistance.png");

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
            _evoker.SetIcon(feature, "Icons\\umbral_arcana.png");
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
            EvokerInstaller.ReplaceBuffReferences(ability, GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff, buff);
            _blueprints.SetUnitFactDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaName),
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaDescription));
            _evoker.SetIcon(ability, "Icons\\umbral_arcana.png");

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

            _evoker.ReplaceDescriptor(buff, SpellDescriptor.Fire, SpellDescriptor.Death);
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
            _evoker.SetIcon(buff, "Icons\\umbral_arcana.png");

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
            _evoker.SetIcon(feature, iconPath);

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
            var source = _blueprints.Require<BlueprintAbility>(sourceSpellGuid, spellName + " donor");
            if (spell == null)
            {
                spell = _blueprints.CloneBlueprint(source, spellGuid, spellName);
                _blueprints.AddCachedBlueprint(spellGuid, spell);
            }

            _blueprints.SetAbilityDisplay(
                spell,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _evoker.SetIcon(spell, iconPath);
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, _blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Fire, SpellDescriptor.Death, _blueprints);
            PatchFireDamageToNegativeEnergy(spell);
            _evoker.ClearDamageScalingCaps(spell);
            ConfigureShadowbornSpellVisuals(spellGuid, spell, source);
            _evoker.RestoreRankDrivenProjectileDelivery(spell, source);

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

            var abilityResource = _blueprints.GetComponents<AbilityResourceLogic>(ability)
                .Select(logic => logic.RequiredResource)
                .FirstOrDefault(resource => resource != null);
            if (abilityResource != null)
            {
                foreach (var addResources in _blueprints.GetComponents<AddAbilityResources>(feature))
                {
                    _blueprints.SetAddAbilityResourcesResource(addResources, abilityResource);
                    if (abilityGuid == ModBlueprintIds.Abilities.ShadowbornUmbralRay)
                    {
                        addResources.RestoreOnLevelUp = true;
                    }
                }
            }

            EvokerInstaller.ReplaceAbilityReferences(feature, sourceAbilityGuid, ability);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _evoker.SetIcon(feature, iconPath);

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
            _evoker.SetIcon(ability, iconPath);
            SpellModifierUtility.ReplaceDescriptor(ability, SpellDescriptor.Fire, SpellDescriptor.Death, _blueprints);
            _evoker.BindAbilityRankConfigsToClass(ability, characterClass);
            PatchFireDamageToNegativeEnergy(ability);
            if (abilityGuid == ModBlueprintIds.Abilities.ShadowbornUmbralRay)
            {
                _evoker.ConfigureRankedD6Damage(ability, characterClass);
                var resource = EnsureShadowbornUmbralRayResource(characterClass);
                foreach (var resourceLogic in _blueprints.GetComponents<AbilityResourceLogic>(ability))
                {
                    _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
                }
            }

            ConfigureShadowbornDamageVisuals(abilityGuid, ability);

            return ability;
        }

        private BlueprintAbilityResource EnsureShadowbornUmbralRayResource(BlueprintCharacterClass characterClass)
        {
            var resourceGuid = EvokerInstaller.DeterministicGuid("WotrMod_ShadowbornUmbralRayAbility.Resource");
            var resource = _blueprints.Get<BlueprintAbilityResource>(resourceGuid);
            if (resource == null)
            {
                var donor = _blueprints.Require<BlueprintAbilityResource>(
                    GameBlueprintIds.AbilityResources.BloodlineElementalElementalRayResource,
                    "Elemental ray resource donor");
                resource = _blueprints.CloneBlueprint(donor, resourceGuid, "WotrMod_ShadowbornUmbralRayResource");
                _blueprints.AddCachedBlueprint(resourceGuid, resource);
            }

            _blueprints.ConfigureAbilityResourceMaxAmount(resource, 0, StatType.Charisma, characterClass, 1);
            return resource;
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

        private void ConfigureShadowbornSpellVisuals(
            string spellGuid,
            BlueprintAbility spell,
            BlueprintAbility source)
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

            ApplyShadowProjectileVisuals(spell, projectile, source);
            spell.OnEnable();
        }

        private void ApplyShadowProjectileVisuals(
            BlueprintAbility ability,
            BlueprintProjectile projectile,
            BlueprintAbility source = null)
        {
            SpellEffectTintRegistry.RegisterProjectileTint(
                projectile.AssetGuid.ToString(),
                SpellEffectTheme.Shadow);

            var sourceSlotCount = source == null
                ? 0
                : _blueprints.GetComponents<AbilityDeliverProjectile>(source)
                    .Select(_blueprints.GetAbilityDeliverProjectileSlotCount)
                    .DefaultIfEmpty(0)
                    .Max();

            foreach (var delivery in _blueprints.GetComponents<AbilityDeliverProjectile>(ability))
            {
                _blueprints.SetAbilityDeliverProjectilesRepeated(
                    delivery,
                    projectile,
                    Math.Max(sourceSlotCount, _blueprints.GetAbilityDeliverProjectileSlotCount(delivery)));
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
                ConfigureShadowbornUmbralRayProjectile(projectile);
                return projectile;
            }

            var rayOfFrostDonor = _blueprints.Require<BlueprintProjectile>(
                GameBlueprintIds.Projectiles.RayOfFrost,
                "Ray of Frost projectile donor");
            projectile = _blueprints.CloneBlueprint(
                rayOfFrostDonor,
                ModBlueprintIds.Projectiles.ShadowbornUmbralRay,
                "WotrMod_ShadowbornUmbralRayProjectile");
            ConfigureShadowbornUmbralRayProjectile(projectile);
            projectile.OnEnable();
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Projectiles.ShadowbornUmbralRay, projectile);

            return projectile;
        }

        private void ConfigureShadowbornUmbralRayProjectile(BlueprintProjectile projectile)
        {
            var enervationDonor = _blueprints.Require<BlueprintProjectile>(
                GameBlueprintIds.Projectiles.Enervation,
                "Enervation projectile donor");

            projectile.Speed = ShadowbornUmbralRayProjectileSpeed;
            projectile.MinTime = enervationDonor.MinTime;
            projectile.CastFx = enervationDonor.CastFx;
            projectile.CastEffectDuration = enervationDonor.CastEffectDuration;
            projectile.LifetimeParticlesAfterHit = enervationDonor.LifetimeParticlesAfterHit;
            projectile.ProjectileHit = enervationDonor.ProjectileHit;
            projectile.DamageHit = enervationDonor.DamageHit;
            projectile.MissMinRadius = enervationDonor.MissMinRadius;
            projectile.MissMaxRadius = enervationDonor.MissMaxRadius;
            projectile.MissRaycastDistance = enervationDonor.MissRaycastDistance;
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

    }
}
