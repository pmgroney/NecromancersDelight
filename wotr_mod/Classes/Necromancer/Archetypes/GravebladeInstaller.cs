using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Necromancer.Archetypes
{
    internal sealed class GravebladeInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

        public GravebladeInstaller(
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

        public BlueprintArchetype Ensure(
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Graveblade);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_NecromancerGravebladeArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Graveblade)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Graveblade, archetype);
            }

            var gravebladeSpellList = EnsureGravebladeSpellList(spellList);
            var gravebladeSpellbook = EnsureGravebladeSpellbook(baseSpellbook, gravebladeSpellList, characterClass);
            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.GravebladeName),
                _localization.Text(LocalizationIds.Mod.GravebladeDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);

            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusHigh, "Graveblade base attack bonus progression");
            var proficiencies = EnsureGravebladeProficiencies(characterClass);
            var reapingEdgeTiers = EnsureGravebladeReapingEdge(characterClass);
            var reapingEdge = reapingEdgeTiers[0];
            var bonusFeat = EnsureGravebladeBonusFeatSelection();
            EnsureWeaponFocusRecommendation(archetype, bonusFeat);
            var fighterTraining = EnsureGravebladeFighterTraining(characterClass, bonusFeat);
            var armorTraining = EnsureGravebladeArmorTraining();
            var armorMastery = EnsureGravebladeArmorMastery();
            var overhandChop = _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.TwoHandedFighterOverhandChop, "Two-Handed Fighter Overhand Chop");
            var backswing = _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.TwoHandedFighterBackswing, "Two-Handed Fighter Backswing");
            var piledriver = _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.TwoHandedFighterPiledriver, "Two-Handed Fighter Piledriver");
            var greaterPowerAttack = _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.TwoHandedFighterGreaterPowerAttack, "Two-Handed Fighter Greater Power Attack");
            var weaponMastery = _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.TwoHandedFighterDevastatingBlow, "Two-Handed Fighter weapon mastery feature");
            var necromancerBonusFeat = new NecromancerInstaller(_blueprints, _localization, _logger, _icons).EnsureNecromancerBonusFeatSelection();

            _blueprints.SetProgressionClasses(proficiencies, characterClass);
            foreach (var reapingEdgeTier in reapingEdgeTiers)
            {
                _blueprints.SetProgressionClasses(reapingEdgeTier, characterClass);
            }
            _blueprints.SetProgressionClassesShallow(bonusFeat, characterClass);
            _blueprints.SetProgressionClasses(fighterTraining, characterClass);
            _blueprints.SetProgressionClasses(armorTraining, characterClass);
            _blueprints.SetProgressionClasses(armorMastery, characterClass);

            var gravebladeLevelEntries = new[]
            {
                CreateLevelEntry(1,  proficiencies, fighterTraining, reapingEdge, bonusFeat),
                CreateLevelEntry(3,  armorTraining, overhandChop),
                CreateLevelEntry(5,  reapingEdgeTiers[1]),
                CreateLevelEntry(6,  bonusFeat),
                CreateLevelEntry(7,  armorTraining, backswing),
                CreateLevelEntry(10, bonusFeat, reapingEdgeTiers[2]),
                CreateLevelEntry(11, armorTraining, piledriver),
                CreateLevelEntry(14, bonusFeat),
                CreateLevelEntry(15, armorTraining, greaterPowerAttack, reapingEdgeTiers[3]),
                CreateLevelEntry(18, bonusFeat),
                CreateLevelEntry(19, armorMastery, weaponMastery),
                CreateLevelEntry(20, reapingEdgeTiers[4])
            };

            _blueprints.SetArchetypeReplaceSpellbook(archetype, gravebladeSpellbook);
            _blueprints.SetArchetypeStartingEquipment(
                archetype,
                true,
                _blueprints.GetCharacterClassStartingGold(characterClass),
                GetGravebladeStartingEquipment(characterClass));
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                gravebladeLevelEntries,
                CreateGravebladeRemoveFeatureEntries(necromancerBonusFeat));
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            _blueprints.SetArchetypeSignatureAbilities(archetype, reapingEdge);
            AddGravebladeFeaturesToProgressionUi(
                characterClass.Progression,
                reapingEdgeTiers, armorTraining, armorMastery,
                overhandChop, backswing, piledriver, greaterPowerAttack, weaponMastery);
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private void EnsureWeaponFocusRecommendation(
            BlueprintArchetype archetype,
            BlueprintFeatureSelection bonusFeat)
        {
            var weaponFocus = _blueprints.Require<BlueprintParametrizedFeature>(
                GameBlueprintIds.Features.WeaponFocus,
                "Weapon Focus");
            var recommendation = _blueprints.EnsureComponent(
                weaponFocus,
                () => new GravebladeWeaponFocusRecommendation
                {
                    name = "$GravebladeWeaponFocusRecommendation$Scythe"
                });
            recommendation.AddGravebladeArchetype(archetype);
            recommendation.AddGravebladeSelection(bonusFeat);
        }

        private BlueprintItem[] GetGravebladeStartingEquipment(BlueprintCharacterClass characterClass)
        {
            var cureLightWoundsPotion = _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.PotionOfCureLightWounds,
                "Potion of Cure Light Wounds");

            var startingEquipment = _blueprints.GetCharacterClassStartingEquipment(characterClass)
                .Where(item => item != null)
                .Concat(new[]
                {
                    cureLightWoundsPotion,
                    cureLightWoundsPotion
                })
                .ToArray();
            return startingEquipment;
        }

        // ─── Remove feature entries ───────────────────────────────────────────

        private LevelEntry[] CreateGravebladeRemoveFeatureEntries(BlueprintFeatureBase necromancerBonusFeat)
        {
            var entries = new List<LevelEntry>();
            AddLevelEntryIfAny(entries, 1,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerProficiencies, "Necromancer Proficiencies"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"),
                necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 2,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell, "Bone Spike granted spell"));
            AddLevelEntryIfAny(entries, 3,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"));
            AddLevelEntryIfAny(entries, 4,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell, "Corpse Explosion granted spell"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"));
            AddLevelEntryIfAny(entries, 6, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 7,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell, "Eldritch Horror granted spell"));
            AddLevelEntryIfAny(entries, 8,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"));
            AddLevelEntryIfAny(entries, 9,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower9, "Grasp of the Dead"));
            AddLevelEntryIfAny(entries, 10, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 11,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerHarvestTheFallenKnownSpell, "Harvest the Fallen granted spell"));
            AddLevelEntryIfAny(entries, 12,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"));
            AddLevelEntryIfAny(entries, 14,
                necromancerBonusFeat,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerHarvestSoulKnownSpell, "Harvest Soul granted spell"));
            AddLevelEntryIfAny(entries, 15,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower15, "Incorporeal Form"));
            AddLevelEntryIfAny(entries, 16,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerDeathClutchKnownSpell, "Death Clutch granted spell"));
            AddLevelEntryIfAny(entries, 18, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 19,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell, "Hell on Earth granted spell"));
            AddLevelEntryIfAny(entries, 20,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower20, "One of Us"));
            return entries.ToArray();
        }

        private BlueprintFeature GetFeatureIfAvailable(string guid, string displayName)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null)
                _blueprints.Warning($"Skipping Graveblade remove feature: {displayName} ({guid}) was not available.");
            return feature;
        }

        private static void AddLevelEntryIfAny(ICollection<LevelEntry> entries, int level, params BlueprintFeatureBase[] features)
        {
            var available = (features ?? Array.Empty<BlueprintFeatureBase>()).Where(f => f != null).ToArray();
            if (available.Length == 0) return;
            entries.Add(CreateLevelEntry(level, available));
        }

        // ─── Progression UI ───────────────────────────────────────────────────

        private void AddGravebladeFeaturesToProgressionUi(
            BlueprintProgression progression,
            BlueprintFeatureBase[] reapingEdgeTiers, BlueprintFeatureBase armorTraining,
            BlueprintFeatureBase armorMastery,
            BlueprintFeatureBase overhandChop, BlueprintFeatureBase backswing,
            BlueprintFeatureBase piledriver, BlueprintFeatureBase greaterPowerAttack,
            BlueprintFeatureBase weaponMastery)
        {
            if (progression == null || reapingEdgeTiers == null || reapingEdgeTiers.Length == 0 || armorTraining == null || armorMastery == null) return;

            // Appends (rather than replacing) since the base Necromancer class and other
            // archetypes already populated progression.UIGroups on this shared progression.
            _blueprints.AddProgressionUiGroup(progression, armorTraining, armorMastery);
            _blueprints.AddProgressionUiGroup(progression, overhandChop, backswing, piledriver, greaterPowerAttack, weaponMastery);
            _blueprints.AddProgressionUiGroup(progression, reapingEdgeTiers);
        }

        // ─── Graveblade-specific features ────────────────────────────────────

        private BlueprintFeature EnsureGravebladeArmorMastery()
        {
            var buff = EnsureGravebladeArmorMasteryBuff();
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeArmorMastery);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.FighterArmorMastery, "Fighter Armor Mastery"),
                    ModBlueprintIds.Features.GravebladeArmorMastery, "WotrMod_NecromancerGravebladeArmorMastery");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeArmorMastery, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.GravebladeArmorMasteryName),
                _localization.Text(LocalizationIds.Mod.GravebladeArmorMasteryDescription));
            foreach (var component in _blueprints.GetComponents<BuffOnArmor>(feature))
                _blueprints.SetBuffOnArmorBuff(component, buff);
            return feature;
        }

        private BlueprintBuff EnsureGravebladeArmorMasteryBuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.GravebladeArmorMastery);
            if (buff == null)
            {
                buff = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintBuff>(GameBlueprintIds.Buffs.FighterArmorMastery, "Fighter Armor Mastery buff"),
                    ModBlueprintIds.Buffs.GravebladeArmorMastery, "WotrMod_NecromancerGravebladeArmorMasteryBuff");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.GravebladeArmorMastery, buff);
            }
            foreach (var resistance in _blueprints.GetComponents<AddDamageResistancePhysical>(buff))
                resistance.Value = new ContextValue { ValueType = ContextValueType.Simple, Value = 10 };
            return buff;
        }

        private BlueprintFeature EnsureGravebladeArmorTraining()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeArmorTraining);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.FighterArmorTraining, "Fighter Armor Training"),
                    ModBlueprintIds.Features.GravebladeArmorTraining, "WotrMod_NecromancerGravebladeArmorTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeArmorTraining, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 5;
            return feature;
        }

        private BlueprintFeature EnsureGravebladeFighterTraining(
            BlueprintCharacterClass characterClass, BlueprintFeatureSelection bonusFeatSelection)
        {
            var arcaneArmorProficiency = EnsureGravebladeArcaneArmorProficiency();
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeFighterTraining);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.MagusFighterTraining, "Magus Fighter Training"),
                    ModBlueprintIds.Features.GravebladeFighterTraining, "WotrMod_NecromancerGravebladeTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeFighterTraining, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingName),
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingDescription));
            _blueprints.ConfigureClassLevelsForPrerequisites(
                feature,
                _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Fighter, "Fighter class"),
                characterClass, bonusFeatSelection, 1.0, 0);
            EnsureFeatureGrantsFact(feature, arcaneArmorProficiency, "$AddFacts$GravebladeArcaneArmorProficiency");
            return feature;
        }

        private BlueprintFeature EnsureGravebladeArcaneArmorProficiency()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeArcaneArmorProficiency);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerGravebladeArcaneArmorProficiency",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.GravebladeArcaneArmorProficiency),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeArcaneArmorProficiency, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingName),
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingDescription));

            var bloodragerProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.BloodragerProficiencies, "Bloodrager Proficiencies");
            var sourceComponent = _blueprints.GetComponents<BlueprintComponent>(bloodragerProficiencies)
                .FirstOrDefault(candidate => candidate.GetType().Name == "ArcaneArmorProficiency");
            if (sourceComponent == null)
            {
                _logger.Error("Bloodrager Proficiencies has no ArcaneArmorProficiency component to clone.");
                _blueprints.SetComponents(feature);
                return feature;
            }

            var clonedComponent = _blueprints.CloneComponent(sourceComponent);
            clonedComponent.name = "$ArcaneArmorProficiency$GravebladeArmor";
            if (clonedComponent is ArcaneArmorProficiency armorProficiency)
            {
                armorProficiency.Armor = new[]
                {
                    ArmorProficiencyGroup.Light,
                    ArmorProficiencyGroup.Medium,
                    ArmorProficiencyGroup.Heavy
                };
            }
            _blueprints.SetComponents(feature, clonedComponent);
            return feature;
        }

        private void EnsureFeatureGrantsFact(BlueprintFeature feature, BlueprintUnitFact fact, string componentName)
        {
            var existing = _blueprints.GetComponents<AddFacts>(feature)
                .FirstOrDefault(component => component.name == componentName);
            if (existing != null)
            {
                _blueprints.SetAddFacts(existing, fact);
                return;
            }

            var addFacts = new AddFacts { name = componentName };
            _blueprints.SetAddFacts(addFacts, fact);
            _blueprints.AddComponent(feature, addFacts);
        }

        private BlueprintFeature EnsureGravebladeProficiencies(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeProficiencies);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerGravebladeProficiencies",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.GravebladeProficiencies),
                    IsClassFeature = true, Ranks = 1
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeProficiencies, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.GravebladeProficienciesName),
                _localization.Text(LocalizationIds.Mod.GravebladeProficienciesDescription));
            var addFacts = new AddFacts { name = "$AddFacts$GravebladeProficiencies" };
            _blueprints.SetAddFacts(addFacts,
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyLight, "Light Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyMedium, "Medium Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyHeavy, "Heavy Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.SimpleWeaponProficiency, "Simple Weapon Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.MartialWeaponProficiency, "Martial Weapon Proficiency"));
            var athleticsClassSkill = new AddClassSkill
            {
                name = "$AddClassSkill$GravebladeAthletics",
                Skill = StatType.SkillAthletics
            };
            _blueprints.SetComponents(feature, addFacts, athleticsClassSkill);
            return feature;
        }

        private BlueprintFeature[] EnsureGravebladeReapingEdge(BlueprintCharacterClass characterClass)
        {
            var brittleBoneBuff = EnsureReapingEdgeBrittleBoneBuff();
            var fatigueBuff = EnsureReapingEdgeConditionBuff(
                ModBlueprintIds.Buffs.ReapingEdgeFatigue, "WotrMod_NecromancerGravebladeReapingEdgeFatigueBuff",
                UnitCondition.Fatigued,
                LocalizationIds.Mod.GravebladeReapingEdgeFatigueName,
                LocalizationIds.Mod.GravebladeReapingEdgeFatigueDescription);
            var exhaustionBuff = EnsureReapingEdgeConditionBuff(
                ModBlueprintIds.Buffs.ReapingEdgeExhaustion, "WotrMod_NecromancerGravebladeReapingEdgeExhaustionBuff",
                UnitCondition.Exhausted,
                LocalizationIds.Mod.GravebladeReapingEdgeExhaustionName,
                LocalizationIds.Mod.GravebladeReapingEdgeExhaustionDescription);
            var buff = EnsureReapingEdgeBuff(characterClass, brittleBoneBuff, fatigueBuff, exhaustionBuff);
            var resource = EnsureReapingEdgeResource(characterClass);
            var ability = EnsureReapingEdgeAbility(resource, buff);
            var baseFeature = EnsureReapingEdgeTierFeature(
                ModBlueprintIds.Features.GravebladeReapingEdge,
                "WotrMod_NecromancerGravebladeReapingEdgeBaseFeature",
                LocalizationIds.Mod.GravebladeReapingEdgeBaseName,
                LocalizationIds.Mod.GravebladeReapingEdgeBaseDescription,
                characterClass,
                ability,
                resource);
            var brittleBoneFeature = EnsureReapingEdgeTierFeature(
                ModBlueprintIds.Features.GravebladeReapingEdgeBrittleBone,
                "WotrMod_NecromancerGravebladeReapingEdgeBrittleBoneFeature",
                LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneFeatureName,
                LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneFeatureDescription,
                characterClass);
            var evilFeature = EnsureReapingEdgeTierFeature(
                ModBlueprintIds.Features.GravebladeReapingEdgeEvil,
                "WotrMod_NecromancerGravebladeReapingEdgeEvilFeature",
                LocalizationIds.Mod.GravebladeReapingEdgeEvilName,
                LocalizationIds.Mod.GravebladeReapingEdgeEvilDescription,
                characterClass);
            var lingeringRotFeature = EnsureReapingEdgeTierFeature(
                ModBlueprintIds.Features.GravebladeReapingEdgeLingeringRot,
                "WotrMod_NecromancerGravebladeReapingEdgeLingeringRotFeature",
                LocalizationIds.Mod.GravebladeReapingEdgeLingeringRotFeatureName,
                LocalizationIds.Mod.GravebladeReapingEdgeLingeringRotFeatureDescription,
                characterClass);
            var boneShardsFeature = EnsureReapingEdgeTierFeature(
                ModBlueprintIds.Features.GravebladeReapingEdgeBoneShards,
                "WotrMod_NecromancerGravebladeReapingEdgeBoneShardsFeature",
                LocalizationIds.Mod.GravebladeReapingEdgeBoneShardsName,
                LocalizationIds.Mod.GravebladeReapingEdgeBoneShardsDescription,
                characterClass);

            return new[]
            {
                baseFeature, brittleBoneFeature, evilFeature, lingeringRotFeature, boneShardsFeature
            };
        }

        private BlueprintFeature EnsureReapingEdgeTierFeature(
            string featureGuid,
            string internalName,
            string displayNameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass,
            BlueprintAbility ability = null,
            BlueprintAbilityResource resource = null)
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
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null) _blueprints.SetUnitFactIcon(feature, icon);

            if (ability != null)
            {
                var addFacts = new AddFacts { name = "$AddFacts$" + internalName };
                _blueprints.SetAddFacts(addFacts, ability);
                _blueprints.SetComponents(feature, addFacts);
                if (resource != null)
                {
                    var necroInstaller = new NecromancerInstaller(_blueprints, _localization, _logger, _icons);
                    necroInstaller.PatchFeatureResource(feature, resource);
                }
            }
            else
            {
                _blueprints.SetComponents(feature);
            }

            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintAbilityResource EnsureReapingEdgeResource(BlueprintCharacterClass characterClass)
        {
            var resource = _blueprints.Get<BlueprintAbilityResource>(ModBlueprintIds.AbilityResources.ReapingEdge);
            if (resource == null)
            {
                resource = new BlueprintAbilityResource
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeResource",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.AbilityResources.ReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.AbilityResources.ReapingEdge, resource);
            }
            resource.LocalizedName = _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName);
            resource.LocalizedDescription = _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription);
            _blueprints.ConfigureAbilityResourceMaxAmount(resource, 0, StatType.Charisma, characterClass, 1);
            return resource;
        }

        private BlueprintAbility EnsureReapingEdgeAbility(BlueprintAbilityResource resource, BlueprintBuff buff)
        {
            var ability = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.ReapingEdge);
            if (ability == null)
            {
                ability = new BlueprintAbility
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeAbility",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Abilities.ReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.ReapingEdge, ability);
            }
            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Personal;
            ability.ActionType = UnitCommand.CommandType.Swift;
            ability.CanTargetSelf = true;
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = false;
            ability.CanTargetPoint = false;
            ability.NotOffensive = true;
            _blueprints.SetAbilityDisplay(ability,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null) _blueprints.SetUnitFactIcon(ability, icon);

            var resourceLogic = new AbilityResourceLogic { name = "$AbilityResourceLogic$GravebladeReapingEdge", Amount = 1 };
            _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
            _blueprints.SetAbilityResourceLogicSpendResource(resourceLogic, true);

            var applyBuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$GravebladeReapingEdge",
                Permanent = true, UseDurationSeconds = false, AsChild = true,
                IgnoreParentContext = false, IsNotDispelable = false, ToCaster = false, SameDuration = false
            };
            _blueprints.SetApplyBuffActionBuff(applyBuff, buff);

            var runAction = new AbilityEffectRunAction
            {
                name = "$AbilityEffectRunAction$GravebladeReapingEdge",
                Actions = new ActionList { Actions = new GameAction[] { applyBuff } }
            };

            var hasNoBuff = new AbilityCasterHasNoFacts { name = "$AbilityCasterHasNoFacts$GravebladeReapingEdge" };
            _blueprints.SetAbilityCasterHasNoFacts(hasNoBuff, buff);

            _blueprints.SetComponents(ability, resourceLogic, runAction, hasNoBuff);
            return ability;
        }

        private BlueprintBuff EnsureReapingEdgeBuff(
            BlueprintCharacterClass characterClass,
            BlueprintBuff brittleBoneBuff, BlueprintBuff fatigueBuff, BlueprintBuff exhaustionBuff)
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ReapingEdge);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.ReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ReapingEdge, buff);
            }
            buff.IsClassFeature = true;
            buff.Stacking = StackingType.Replace;
            buff.Ranks = 0;
            _blueprints.SetUnitFactDisplay(buff,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBuffName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBuffDescription));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null) _blueprints.SetUnitFactIcon(buff, icon);

            ReapingEdgeComponent.Logger = _logger;
            _blueprints.SetComponents(buff, new ReapingEdgeComponent
            {
                name = "$ReapingEdgeComponent$Graveblade",
                CharacterClass = characterClass,
                BrittleBoneBuff = brittleBoneBuff,
                FatigueBuff = fatigueBuff,
                ExhaustionBuff = exhaustionBuff
            });
            return buff;
        }

        private BlueprintBuff EnsureReapingEdgeBrittleBoneBuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ReapingEdgeBrittleBone);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_NecromancerGravebladeBrittleBoneBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.ReapingEdgeBrittleBone)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ReapingEdgeBrittleBone, buff);
            }
            _blueprints.SetUnitFactDisplay(buff,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneDescription));
            _blueprints.SetComponents(buff, new AddStatBonus
            {
                name = "$AddStatBonus$GravebladeBrittleBone",
                Stat = StatType.AC, Value = -2, Descriptor = ModifierDescriptor.Penalty
            });
            return buff;
        }

        private BlueprintBuff EnsureReapingEdgeConditionBuff(
            string buffGuid, string internalName, UnitCondition condition,
            string displayNameKey, string descriptionKey)
        {
            var buff = _blueprints.Get<BlueprintBuff>(buffGuid);
            if (buff == null)
            {
                buff = new BlueprintBuff { name = internalName, AssetGuid = BlueprintGuid.Parse(buffGuid) };
                _blueprints.AddCachedBlueprint(buffGuid, buff);
            }
            _blueprints.SetUnitFactDisplay(buff, _localization.Text(displayNameKey), _localization.Text(descriptionKey));
            _blueprints.SetComponents(buff, new AddCondition { name = "$AddCondition$" + internalName, Condition = condition });
            return buff;
        }

        private BlueprintFeatureSelection EnsureGravebladeBonusFeatSelection()
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.GravebladeBonusFeat);
            var necromancerBonusFeat = new NecromancerInstaller(_blueprints, _localization, _logger, _icons).EnsureNecromancerBonusFeatSelection();
            if (selection == null)
            {
                selection = _blueprints.CloneBlueprint(
                    necromancerBonusFeat,
                    ModBlueprintIds.Selections.GravebladeBonusFeat,
                    "WotrMod_NecromancerGravebladeBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.GravebladeBonusFeat, selection);
            }
            _blueprints.SetUnitFactDisplay(selection,
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatName),
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatDescription));
            var choices = _blueprints.GetFeatureSelectionAllFeatures(necromancerBonusFeat)
                .Concat(_blueprints.GetFeatureSelectionAllFeatures(
                    _blueprints.Require<BlueprintFeatureSelection>(GameBlueprintIds.Selections.FighterFeat, "Fighter Bonus Feat")))
                .GroupBy(f => f.AssetGuid)
                .Select(g => g.First())
                .ToArray();
            _blueprints.SetFeatureSelectionAllFeatures(selection, choices);
            _blueprints.SetFeatureSelectionFeatures(selection, Array.Empty<BlueprintFeature>());
            selection.IsClassFeature = true;
            selection.Ranks = 1;
            return selection;
        }

        private BlueprintSpellList EnsureGravebladeSpellList(BlueprintSpellList baseSpellList)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(ModBlueprintIds.SpellLists.Graveblade);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(
                    baseSpellList, ModBlueprintIds.SpellLists.Graveblade, "WotrMod_NecromancerGravebladeSpellList");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.SpellLists.Graveblade, spellList);
            }
            // Graveblade uses necromancer spells starting at level 1
            var spellsByLevel = NecromancerSpellRegistry.GetAll()
                .Where(d => d.SpellLevel >= 1)
                .Select(d =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(d.SpellGuid, d.DisplayName);
                    return new KeyValuePair<BlueprintAbility, int>(spell, d.SpellLevel);
                });
            _blueprints.SetSpellListSpells(spellList, spellsByLevel.OrderBy(p => p.Value).ThenBy(p => p.Key.name));
            return spellList;
        }

        private BlueprintSpellbook EnsureGravebladeSpellbook(
            BlueprintSpellbook baseSpellbook, BlueprintSpellList spellList, BlueprintCharacterClass characterClass)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(ModBlueprintIds.Spellbooks.Graveblade);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(
                    baseSpellbook, ModBlueprintIds.Spellbooks.Graveblade, "WotrMod_NecromancerGravebladeSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Graveblade, spellbook);
            }
            var inquisitorSpellbook = _blueprints.Require<BlueprintSpellbook>(
                GameBlueprintIds.Spellbooks.Inquisitor,
                "Inquisitor spellbook");
            _blueprints.CopySpellbookProgression(spellbook, inquisitorSpellbook);
            NecromancerMediumMergedSpellbookProgression.Apply(_blueprints, spellbook);
            spellbook.CastingAttribute = baseSpellbook.CastingAttribute;
            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
            return spellbook;
        }

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }
    }
}
