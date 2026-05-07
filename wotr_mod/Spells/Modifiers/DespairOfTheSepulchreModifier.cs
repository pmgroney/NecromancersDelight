using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class DespairOfTheSepulchreModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, context.Blueprints);

            spell.Range = AbilityRange.Personal;
            spell.CanTargetPoint = false;
            spell.CanTargetEnemies = false;
            spell.CanTargetFriends = true;
            spell.CanTargetSelf = true;
            spell.SpellResistance = false;
            spell.NotOffensive = false;

            var debuff = EnsureDebuff(context);
            var aura = EnsureAuraBuff(context, EnsureAreaEffect(context, debuff));

            var applyAura = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$DespairOfTheSepulchreAura",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = CasterLevelRounds(),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyAura, aura);

            var rank = new ContextRankConfig { name = "$ContextRankConfig$DespairOfTheSepulchre" };
            context.Blueprints.ConfigureContextRankConfig(rank);

            context.Blueprints.SetComponents(
                spell,
                new SpellComponent { name = "$SpellComponent$DespairOfTheSepulchre", School = SpellSchool.Necromancy },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$DespairOfTheSepulchre",
                    Descriptor = SpellDescriptor.Death | SpellDescriptor.Fear
                },
                rank,
                new AbilityEffectRunAction
                {
                    name = "$AbilityEffectRunAction$DespairOfTheSepulchre",
                    SavingThrowType = SavingThrowType.Unknown,
                    Actions = new ActionList { Actions = new GameAction[] { applyAura } }
                });
        }

        private static BlueprintBuff EnsureAuraBuff(SpellModifierContext context, BlueprintAbilityAreaEffect area)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.DespairOfTheSepulchreAura);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_DespairOfTheSepulchreAuraBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.DespairOfTheSepulchreAura)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.DespairOfTheSepulchreAura, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);

            var addArea = new AddAreaEffect { name = "$AddAreaEffect$DespairOfTheSepulchre" };
            context.Blueprints.SetAddAreaEffect(addArea, area);
            context.Blueprints.SetComponents(buff, addArea);
            return buff;
        }

        private static BlueprintAbilityAreaEffect EnsureAreaEffect(SpellModifierContext context, BlueprintBuff debuff)
        {
            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.DespairOfTheSepulchre);
            if (area == null)
            {
                area = new BlueprintAbilityAreaEffect
                {
                    name = "WotrMod_DespairOfTheSepulchreAreaEffect",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.AreaEffects.DespairOfTheSepulchre)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.DespairOfTheSepulchre, area);
            }

            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 20.Feet();
            area.SpellResistance = false;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;

            var applyDebuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$DespairOfTheSepulchreDebuff",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = AreaLinkedDuration(),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = true,
                SameDuration = true,
                NotLinkToAreaEffect = false,
                IgnoreParentContext = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyDebuff, debuff);

            var removeDebuff = new ContextActionRemoveBuff
            {
                name = "$ContextActionRemoveBuff$DespairOfTheSepulchreDebuff",
                RemoveRank = false,
                ToCaster = false,
                OnlyFromCaster = true
            };
            context.Blueprints.SetRemoveBuffActionBuff(removeDebuff, debuff);

            var enemyOnly = new Conditional
            {
                name = "$Conditional$DespairOfTheSepulchreEnemy",
                ConditionsChecker = new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        new ContextConditionIsEnemy
                        {
                            name = "$ContextConditionIsEnemy$DespairOfTheSepulchre"
                        }
                    }
                },
                IfTrue = new ActionList { Actions = new GameAction[] { applyDebuff } },
                IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
            };

            context.Blueprints.SetComponents(
                area,
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$DespairOfTheSepulchre",
                    UnitEnter = new ActionList { Actions = new GameAction[] { enemyOnly } },
                    UnitExit = new ActionList { Actions = new GameAction[] { removeDebuff } },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList { Actions = Array.Empty<GameAction>() }
                });

            return area;
        }

        private static BlueprintBuff EnsureDebuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.DespairOfTheSepulchreDebuff);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_DespairOfTheSepulchreDebuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.DespairOfTheSepulchreDebuff)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.DespairOfTheSepulchreDebuff, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$DespairOfTheSepulchreFatigued",
                    Condition = UnitCondition.Fatigued
                },
                new AddStatBonus
                {
                    name = "$AddStatBonus$DespairOfTheSepulchreAttackPenalty",
                    Stat = StatType.AdditionalAttackBonus,
                    Value = -4,
                    Descriptor = ModifierDescriptor.Penalty
                },
                new DespairOfTheSepulchreSavingThrowPenalty
                {
                    name = "$DespairOfTheSepulchreSavingThrowPenalty$Death"
                });

            return buff;
        }

        private static ContextDurationValue CasterLevelRounds()
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue
                {
                    ValueType = ContextValueType.Rank,
                    ValueRank = AbilityRankType.Default
                }
            };
        }

        private static ContextDurationValue AreaLinkedDuration()
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 1 }
            };
        }
    }

    public sealed class DespairOfTheSepulchreSavingThrowPenalty :
        UnitFactComponentDelegate,
        ITargetRulebookHandler<RuleSavingThrow>,
        IRulebookHandler<RuleSavingThrow>,
        ITargetRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var ability = evt?.Reason?.Context?.SourceAbility;
            if (ability == null || !ability.SpellDescriptor.HasFlag(SpellDescriptor.Death))
            {
                return;
            }

            evt.AddModifier(-4, Fact, ModifierDescriptor.Penalty);
        }

        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
        }
    }
}
