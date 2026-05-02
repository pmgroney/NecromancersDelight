using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Features;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Necromancer.Archetypes
{
    internal sealed class GravebladeInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;

        public GravebladeInstaller(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
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
            
            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusHigh,
                "Graveblade base attack bonus progression");
            
            var proficiencies = EnsureGravebladeProficiencies();
            var reapingEdge = EnsureGravebladeReapingEdge(characterClass);
            var bonusFeat = EnsureGravebladeBonusFeatSelection();
            var fighterTraining = EnsureGravebladeFighterTraining(characterClass, bonusFeat);

            _blueprints.SetArchetypeReplaceSpellbook(archetype, gravebladeSpellbook);
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            
            var addedFeatures = new List<LevelEntry>
            {
                CreateLevelEntry(1, proficiencies, reapingEdge),
                CreateLevelEntry(2, bonusFeat),
                CreateLevelEntry(4, bonusFeat),
                CreateLevelEntry(5, fighterTraining)
            };
            for (int i = 6; i <= 20; i += 2)
            {
                addedFeatures.Add(CreateLevelEntry(i, bonusFeat));
            }

            _blueprints.SetArchetypeFeatureChanges(archetype, addedFeatures.ToArray(), Array.Empty<LevelEntry>());
            _blueprints.SetArchetypeBuildChanging(archetype, true);
            _blueprints.SetArchetypeAttributeRecommendations(
                archetype,
                new[] { StatType.Strength, StatType.Intelligence, StatType.Constitution },
                new[] { StatType.Charisma });

            return archetype;
        }

        private BlueprintSpellList EnsureGravebladeSpellList(BlueprintSpellList baseSpellList)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(ModBlueprintIds.SpellLists.Graveblade);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(baseSpellList, ModBlueprintIds.SpellLists.Graveblade, "WotrMod_GravebladeSpellList");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.SpellLists.Graveblade, spellList);
            }

            _blueprints.ClearSpellList(spellList);
            var wizardList = _blueprints.Require<BlueprintSpellList>(GameBlueprintIds.SpellLists.Wizard, "Wizard spell list");
            
            for (int level = 1; level <= 6; level++)
            {
                var spells = wizardList.SpellsByLevel[level].Spells;
                foreach (var spell in spells)
                {
                    var component = spell.GetComponent<SpellComponent>();
                    if (component != null && (component.School == SpellSchool.Necromancy || component.School == SpellSchool.Transmutation))
                    {
                        _blueprints.AddSpellToList(spellList, spell, level);
                    }
                }
            }

            return spellList;
        }

        private BlueprintSpellbook EnsureGravebladeSpellbook(
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList,
            BlueprintCharacterClass characterClass)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(ModBlueprintIds.Spellbooks.Graveblade);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(baseSpellbook, ModBlueprintIds.Spellbooks.Graveblade, "WotrMod_GravebladeSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Graveblade, spellbook);
            }

            spellbook.SpellList = spellList;
            spellbook.CastingStat = StatType.Intelligence;
            spellbook.CharacterClass = characterClass;
            _blueprints.SetUnitFactDisplay(
                spellbook,
                _localization.Text(LocalizationIds.Mod.GravebladeName),
                _localization.Text(LocalizationIds.Mod.GravebladeDescription));

            return spellbook;
        }

        private BlueprintFeature EnsureGravebladeProficiencies()
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeProficiencies);
            if (existing != null) return existing;

            var magusProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.MagusProficiencies,
                "Magus proficiencies");

            var clone = _blueprints.CloneBlueprint(
                magusProficiencies,
                ModBlueprintIds.Features.GravebladeProficiencies,
                "WotrMod_GravebladeProficiencies");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.GravebladeProficienciesName),
                _localization.Text(LocalizationIds.Mod.GravebladeProficienciesDescription));

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeProficiencies, clone);
            return clone;
        }

        private BlueprintFeature EnsureGravebladeReapingEdge(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeReapingEdgeFeature);
            if (existing != null) return existing;

            var resource = EnsureReapingEdgeResource(characterClass);
            var brittleBoneBuff = EnsureReapingEdgeBrittleBoneBuff();
            var fatigueBuff = EnsureReapingEdgeConditionBuff(
                ModBlueprintIds.Buffs.GravebladeReapingEdgeFatigue,
                "WotrMod_GravebladeReapingEdgeFatigueBuff",
                UnitCondition.Fatigued,
                LocalizationIds.Mod.GravebladeReapingEdgeFatigueName,
                LocalizationIds.Mod.GravebladeReapingEdgeFatigueDescription);
            var exhaustionBuff = EnsureReapingEdgeConditionBuff(
                ModBlueprintIds.Buffs.GravebladeReapingEdgeExhaustion,
                "WotrMod_GravebladeReapingEdgeExhaustionBuff",
                UnitCondition.Exhausted,
                LocalizationIds.Mod.GravebladeReapingEdgeExhaustionName,
                LocalizationIds.Mod.GravebladeReapingEdgeExhaustionDescription);

            var buff = EnsureReapingEdgeBuff(characterClass, brittleBoneBuff, fatigueBuff, exhaustionBuff);
            var ability = EnsureReapingEdgeAbility(resource, buff);

            var feature = new BlueprintFeature
            {
                name = "WotrMod_GravebladeReapingEdgeFeature",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.GravebladeReapingEdgeFeature)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeReapingEdgeFeature, feature);

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));

            _blueprints.SetComponents(feature,
                new AddAbilityResources { Resource = resource.ToReference<BlueprintAbilityResourceReference>() },
                new AddFacts { Facts = new BlueprintUnitFactReference[] { ability.ToReference<BlueprintUnitFactReference>() } });

            return feature;
        }

        private BlueprintAbilityResource EnsureReapingEdgeResource(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintAbilityResource>(ModBlueprintIds.AbilityResources.GravebladeReapingEdge);
            if (existing != null) return existing;

            var resource = new BlueprintAbilityResource
            {
                name = "WotrMod_GravebladeReapingEdgeResource",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.AbilityResources.GravebladeReapingEdge)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.AbilityResources.GravebladeReapingEdge, resource);

            resource.m_MaxAmount = new BlueprintAbilityResource.Amount
            {
                BaseValue = 0,
                IncreasedByLevel = true,
                m_Class = new[] { characterClass.ToReference<BlueprintCharacterClassReference>() },
                m_Archetypes = Array.Empty<BlueprintArchetypeReference>(),
                LevelIncrease = 1
            };

            return resource;
        }

        private BlueprintAbility EnsureReapingEdgeAbility(BlueprintAbilityResource resource, BlueprintBuff buff)
        {
            var existing = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.GravebladeReapingEdge);
            if (existing != null) return existing;

            var ability = new BlueprintAbility
            {
                name = "WotrMod_GravebladeReapingEdgeAbility",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Abilities.GravebladeReapingEdge)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.GravebladeReapingEdge, ability);

            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));

            _blueprints.SetAbilityType(ability, AbilityType.Supernatural);
            _blueprints.SetAbilityRange(ability, AbilityRange.Personal);
            _blueprints.SetAbilityActionType(ability, UnitCommand.CommandType.Swift);

            _blueprints.SetComponents(ability,
                new AbilityResourceLogic
                {
                    m_RequiredResource = resource.ToReference<BlueprintAbilityResourceReference>(),
                    RequiredAmount = 1
                },
                new AbilityEffectRunAction
                {
                    Actions = new ActionList
                    {
                        Actions = new[]
                        {
                            new ContextActionApplyBuff
                            {
                                m_Buff = buff.ToReference<BlueprintBuffReference>(),
                                DurationValue = new ContextDurationValue
                                {
                                    Rate = DurationRate.Rounds,
                                    DiceType = DiceType.Zero,
                                    DiceCountValue = 0,
                                    BonusValue = new ContextValue
                                    {
                                        ValueType = ContextValueType.Simple,
                                        Value = 1
                                    }
                                }
                            }
                        }
                    }
                });

            return ability;
        }

        private BlueprintBuff EnsureReapingEdgeBuff(
            BlueprintCharacterClass characterClass,
            BlueprintBuff brittleBoneBuff,
            BlueprintBuff fatigueBuff,
            BlueprintBuff exhaustionBuff)
        {
            var existing = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.GravebladeReapingEdge);
            if (existing != null) return existing;

            var buff = new BlueprintBuff
            {
                name = "WotrMod_GravebladeReapingEdgeBuff",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.GravebladeReapingEdge)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.GravebladeReapingEdge, buff);

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));

            _blueprints.SetComponents(buff,
                new ReapingEdgeComponent
                {
                    BrittleBoneBuff = brittleBoneBuff.ToReference<BlueprintBuffReference>(),
                    FatigueBuff = fatigueBuff.ToReference<BlueprintBuffReference>(),
                    ExhaustionBuff = exhaustionBuff.ToReference<BlueprintBuffReference>()
                },
                new ContextRankConfig
                {
                    m_Type = AbilityRankType.Default,
                    m_BaseValueType = ContextRankBaseValueType.ClassLevel,
                    m_Class = new[] { characterClass.ToReference<BlueprintCharacterClassReference>() },
                    m_Progression = ContextRankProgression.AsIs
                });

            return buff;
        }

        private BlueprintBuff EnsureReapingEdgeBrittleBoneBuff()
        {
            var existing = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.GravebladeReapingEdgeBrittleBone);
            if (existing != null) return existing;

            var buff = new BlueprintBuff
            {
                name = "WotrMod_GravebladeReapingEdgeBrittleBoneBuff",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.GravebladeReapingEdgeBrittleBone)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.GravebladeReapingEdgeBrittleBone, buff);

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneDescription));

            _blueprints.SetComponents(buff,
                new AddDamageTypeSpecialThreshold
                {
                    DamageType = DamageType.Physical,
                    Threshold = 5
                });

            return buff;
        }

        private BlueprintBuff EnsureReapingEdgeConditionBuff(
            string buffGuid,
            string internalName,
            UnitCondition condition,
            string displayNameKey,
            string descriptionKey)
        {
            var existing = _blueprints.Get<BlueprintBuff>(buffGuid);
            if (existing != null) return existing;

            var buff = new BlueprintBuff
            {
                name = internalName,
                AssetGuid = BlueprintGuid.Parse(buffGuid)
            };
            _blueprints.AddCachedBlueprint(buffGuid, buff);

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));

            _blueprints.SetComponents(buff, new AddCondition { Condition = condition });

            return buff;
        }

        private BlueprintFeatureSelection EnsureGravebladeBonusFeatSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.GravebladeBonusFeat);
            if (existing != null) return existing;

            var combatFeat = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.CombatFeatSelection,
                "Combat feat selection");

            var clone = _blueprints.CloneBlueprint(
                combatFeat,
                ModBlueprintIds.Selections.GravebladeBonusFeat,
                "WotrMod_GravebladeBonusFeatSelection");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatName),
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatDescription));

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.GravebladeBonusFeat, clone);
            return clone;
        }

        private BlueprintFeature EnsureGravebladeFighterTraining(
            BlueprintCharacterClass characterClass,
            BlueprintFeatureSelection bonusFeatSelection)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeFighterTraining);
            if (existing != null) return existing;

            var fighterTraining = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.FighterTraining,
                "Fighter training");

            var clone = _blueprints.CloneBlueprint(
                fighterTraining,
                ModBlueprintIds.Features.GravebladeFighterTraining,
                "WotrMod_GravebladeFighterTraining");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingName),
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingDescription));

            _blueprints.EditComponent<ClassLevelsForPrerequisites>(clone, comp =>
            {
                comp.m_FakeClass = characterClass.ToReference<BlueprintCharacterClassReference>();
                comp.Modifier = 0.5f;
            });

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeFighterTraining, clone);
            return clone;
        }

        private LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            return new LevelEntry
            {
                Level = level,
                Features = features.ToList()
            };
        }
    }
}
