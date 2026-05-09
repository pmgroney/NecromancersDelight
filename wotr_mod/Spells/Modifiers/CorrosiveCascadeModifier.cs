using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class CorrosiveCascadeModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);

            foreach (var rank in context.Blueprints.GetComponents<ContextRankConfig>(spell))
            {
                context.Blueprints.SetContextRankMaximum(rank, 12);
            }

            foreach (var delivery in context.Blueprints.GetComponents<AbilityDeliverProjectile>(spell))
            {
                context.Blueprints.SetAbilityDeliverProjectileLength(delivery, 60.Feet());
            }

            var corrosion = EnsureCorrosionBuff(context);
            var armorDebuff = EnsureArmorDebuff(context);
            SpellModifierUtility.PatchRunActions(spell, action =>
            {
                var changed = 0;
                var damage = action as ContextActionDealDamage;
                if (damage != null && damage.DamageType.Type == DamageType.Energy)
                {
                    damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Acid);
                    damage.Value = new ContextDiceValue
                    {
                        DiceType = DiceType.D6,
                        DiceCountValue = new ContextValue
                        {
                            ValueType = ContextValueType.Rank,
                            ValueRank = Kingmaker.Enums.AbilityRankType.Default
                        },
                        BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                    };
                    damage.IsAoE = true;
                    damage.HalfIfSaved = true;
                    changed++;
                }

                var saved = action as ContextActionConditionalSaved;
                if (saved != null)
                {
                    saved.Succeed = new ActionList { Actions = Array.Empty<GameAction>() };
                    saved.Failed = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            ApplyBuff(context, armorDebuff, 1, "$ContextActionApplyBuff$CorrosiveCascadeArmorDebuff")
                        }
                    };
                    changed++;
                }

                return changed;
            });

            AddCorrosionAction(context, spell, corrosion);
        }

        private static void AddCorrosionAction(
            SpellModifierContext context,
            Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility spell,
            BlueprintBuff corrosion)
        {
            var runAction = spell.GetComponent<AbilityEffectRunAction>();
            var actions = runAction?.Actions?.Actions;
            if (actions == null ||
                actions.Any(action => action?.name == "$ContextActionApplyBuff$CorrosiveCascadeCorrosion"))
            {
                return;
            }

            var corrosionAction = ApplyBuff(
                context,
                corrosion,
                2,
                "$ContextActionApplyBuff$CorrosiveCascadeCorrosion");
            var insertIndex = Array.FindIndex(actions, action => action is ContextActionConditionalSaved);
            if (insertIndex < 0)
            {
                insertIndex = actions.Length;
            }

            runAction.Actions.Actions = actions
                .Take(insertIndex)
                .Concat(new GameAction[] { corrosionAction })
                .Concat(actions.Skip(insertIndex))
                .ToArray();
        }

        private static ContextActionApplyBuff ApplyBuff(
            SpellModifierContext context,
            BlueprintBuff buff,
            int rounds,
            string name)
        {
            var action = new ContextActionApplyBuff
            {
                name = name,
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(rounds),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(action, buff);
            return action;
        }

        private static BlueprintBuff EnsureCorrosionBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.CorrosiveCascadeCorrosion);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_CorrosiveCascadeCorrosionBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.CorrosiveCascadeCorrosion)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.CorrosiveCascadeCorrosion, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddFactContextActions
                {
                    name = "$AddFactContextActions$CorrosiveCascadeCorrosion",
                    Activated = new ActionList { Actions = Array.Empty<GameAction>() },
                    Deactivated = new ActionList { Actions = Array.Empty<GameAction>() },
                    Dispose = new ActionList { Actions = Array.Empty<GameAction>() },
                    NewRound = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new ContextActionDealDamage
                            {
                                name = "$ContextActionDealDamage$CorrosiveCascadeCorrosion",
                                DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Acid),
                                Value = new ContextDiceValue
                                {
                                    DiceType = DiceType.D6,
                                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 2 },
                                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                                }
                            }
                        }
                    }
                });

            return buff;
        }

        private static BlueprintBuff EnsureArmorDebuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.CorrosiveCascadeArmorDebuff);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_CorrosiveCascadeArmorDebuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.CorrosiveCascadeArmorDebuff)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.CorrosiveCascadeArmorDebuff, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddStatBonus
                {
                    name = "$AddStatBonus$CorrosiveCascadeArmor",
                    Stat = StatType.AC,
                    Value = -2,
                    Descriptor = ModifierDescriptor.Penalty
                });

            return buff;
        }

        private static ContextDurationValue Rounds(int rounds)
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = rounds }
            };
        }
    }
}
