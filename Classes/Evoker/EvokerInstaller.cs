using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityEngine;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Classes.Evoker;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class EvokerInstaller : IClassContentInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

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

        public void Install(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            _blueprints.SetCharacterClassArchetypes(characterClass);

            _blueprints.SetCharacterClassArchetypes(
                characterClass,
                EnsureArchetypes(definition, characterClass, spellbook, spellList));
            new EvokerScalingInstaller(_blueprints, _localization, _logger, _icons).Install(characterClass);
        }

        private void ConfigureEvokerSpellList(BlueprintSpellList spellList)
        {
            var spellsByLevel = EvokerSpellRegistry.GetAll()
                .GroupBy(s => s.SpellLevel);

            foreach (var levelGroup in spellsByLevel)
            {
                foreach (var spellDef in levelGroup)
                {
                    var spell = _blueprints.Get<BlueprintAbility>(spellDef.SpellGuid);
                    if (spell == null)
                    {
                        _logger.Warning($"Could not find spell {spellDef.DisplayName} ({spellDef.SpellGuid}) for Evoker spell list.");
                        continue;
                    }

                    _blueprints.AddSpellToList(spellList, spell, levelGroup.Key);
                }
            }
        }

        private BlueprintArchetype[] EnsureArchetypes(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            return new[]
            {
                EnsureShadowbornArchetype(characterClass)
            };
        }

        private BlueprintArchetype EnsureShadowbornArchetype(BlueprintCharacterClass characterClass)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Shadowborn);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_EvokerShadowbornArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Shadowborn)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Shadowborn, archetype);
            }

            var evokerBloodlineSelection = _blueprints.Require<BlueprintFeatureSelection>(
                ModBlueprintIds.Selections.EvokerBloodline,
                "Evoker bloodline selection");
            var shadowbornBloodline = EnsureShadowbornBloodline(characterClass);

            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.ShadowbornName),
                _localization.Text(LocalizationIds.Mod.ShadowbornDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, null);
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                new[] { CreateLevelEntry(1, shadowbornBloodline) },
                new[] { CreateLevelEntry(1, evokerBloodlineSelection) });
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private BlueprintProgression EnsureShadowbornBloodline(BlueprintCharacterClass characterClass)
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
                GameBlueprintIds.Features.BloodlineElementalEarthElementalRayFeature);
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
                GameBlueprintIds.Features.BloodlineElementalFireElementalBlastFeature);
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

            _blueprints.SetUnitFactDisplay(
                bloodline,
                _localization.Text(LocalizationIds.Mod.ShadowbornBloodlineName),
                _localization.Text(LocalizationIds.Mod.ShadowbornBloodlineDescription));
            SetIcon(bloodline, "Icons\\shadowborn_bloodline.png");
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireArcana,
                arcana);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalRayFeature,
                umbralRay);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBlastFeature,
                umbralBlast);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBodyFeature,
                elementalBody);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireResistanceFeature,
                resistance);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel1,
                shadowHands);
            ReplaceProgressionFeature(
                bloodline,
                GameBlueprintIds.Features.BloodlineElementalFireSpellLevel2,
                shadowRay);
            _blueprints.SetProgressionClasses(bloodline, characterClass);

            return bloodline;
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
            SetShadowTintedIcon(
                feature,
                GameBlueprintIds.Features.BloodlineElementalFireElementalBodyFeature,
                "ShadowbornElementalBody");

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
                ConfigureAddFeatureOnClassLevel(component, characterClass, level1, level2);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceName),
                _localization.Text(LocalizationIds.Mod.ShadowbornResistanceDescription));
            SetShadowTintedIcon(
                feature,
                GameBlueprintIds.Features.BloodlineElementalFireResistanceFeature,
                "ShadowbornResistance");
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
            SetShadowTintedIcon(feature, sourceFeatureGuid, featureName);

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
            SetShadowTintedIcon(
                feature,
                GameBlueprintIds.Features.BloodlineElementalFireArcana,
                "ShadowbornArcanaFeature");
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
            ReplaceBuffReferences(ability, GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff, buff);
            _blueprints.SetUnitFactDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaName),
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaDescription));
            SetShadowTintedIcon(
                ability,
                GameBlueprintIds.Abilities.BloodlineElementalFireArcanaAbility,
                "ShadowbornArcanaAbility");

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

            ReplaceDescriptor(buff, SpellDescriptor.Fire, SpellDescriptor.Death);
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaName),
                _localization.Text(LocalizationIds.Mod.ShadowbornArcanaDescription));
            SetShadowTintedIcon(
                buff,
                GameBlueprintIds.Buffs.BloodlineElementalFireArcanaBuff,
                "ShadowbornArcanaBuff");

            return buff;
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
            SetIcon(feature, iconPath);

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
            if (spell == null)
            {
                var source = _blueprints.Require<BlueprintAbility>(sourceSpellGuid, spellName + " donor");
                spell = _blueprints.CloneBlueprint(source, spellGuid, spellName);
                _blueprints.AddCachedBlueprint(spellGuid, spell);
            }

            _blueprints.SetAbilityDisplay(
                spell,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SetIcon(spell, iconPath);
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, _blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Fire, SpellDescriptor.Death, _blueprints);
            PatchFireDamageToNegativeEnergy(spell);

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
            string iconSourceFeatureGuid)
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
                iconSourceFeatureGuid);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            ReplaceAbilityReferences(feature, sourceAbilityGuid, ability);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SetShadowTintedIcon(feature, iconSourceFeatureGuid, featureName);

            return feature;
        }

        private BlueprintAbility EnsureShadowbornDamageAbility(
            string sourceAbilityGuid,
            string abilityGuid,
            string abilityName,
            string displayNameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass,
            string iconSourceFeatureGuid)
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
            SetShadowTintedIcon(ability, iconSourceFeatureGuid, abilityName);
            SpellModifierUtility.ReplaceDescriptor(ability, SpellDescriptor.Fire, SpellDescriptor.Death, _blueprints);
            BindAbilityRankConfigsToClass(ability, characterClass);
            PatchFireDamageToNegativeEnergy(ability);

            return ability;
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

        private void ReplaceDescriptor(BlueprintScriptableObject blueprint, SpellDescriptor remove, SpellDescriptor add)
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

        private void SetIcon(BlueprintUnitFact fact, string iconPath)
        {
            var icon = _icons.Load(iconPath);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(fact, icon);
            }
        }

        private void SetShadowTintedIcon(BlueprintUnitFact fact, string sourceFeatureGuid, string cacheKey)
        {
            var source = _blueprints.Require<BlueprintUnitFact>(sourceFeatureGuid, cacheKey + " icon source");
            var tint = GetShadowIconTint(cacheKey);
            var icon = _icons.Tint(source.Icon, cacheKey, tint.Color, tint.Strength);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(fact, icon);
            }
        }

        private static ShadowIconTint GetShadowIconTint(string cacheKey)
        {
            if (cacheKey != null &&
                (cacheKey.Contains("UmbralBlast") ||
                 cacheKey.Contains("ShadowbornArcana") ||
                 cacheKey.Contains("ShadowbornResistance") ||
                 cacheKey.Contains("ShadowbornElementalBody")))
            {
                return new ShadowIconTint(new Color(0.32f, 0.06f, 0.78f, 1f), 0.86f);
            }

            return new ShadowIconTint(new Color(0.45f, 0.2f, 0.85f, 1f), 0.65f);
        }

        private readonly struct ShadowIconTint
        {
            public ShadowIconTint(Color color, float strength)
            {
                Color = color;
                Strength = strength;
            }

            public Color Color { get; }
            public float Strength { get; }
        }

        private static void ReplaceBuffReferences(
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

        private static void ConfigureAddFeatureOnClassLevel(
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

        private void BindAbilityRankConfigsToClass(BlueprintAbility ability, BlueprintCharacterClass characterClass)
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

        private static void ReplaceProgressionFeature(
            BlueprintProgression progression,
            string oldFeatureGuid,
            BlueprintFeatureBase newFeature)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldFeatureGuid));
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature => feature != null && feature.AssetGuid == oldGuid ? newFeature : feature));
            }
        }

        private static void ReplaceAbilityReferences(
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

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features.Where(feature => feature != null));
            return entry;
        }
    }
}
