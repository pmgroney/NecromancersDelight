using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class AbsoluteZeroModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);

            foreach (var delivery in context.Blueprints.GetComponents<AbilityDeliverProjectile>(spell))
            {
                context.Blueprints.SetAbilityDeliverProjectileLength(delivery, 30.Feet());
            }

            foreach (var rank in context.Blueprints.GetComponents<ContextRankConfig>(spell))
            {
                context.Blueprints.SetContextRankMaximum(rank, 15);
            }

            ConfigureDamageAndControl(
                context,
                spell,
                EnsurePetrifiedBuff(context),
                EnsureSlowedBuff(context));
        }

        private static void ConfigureDamageAndControl(
            SpellModifierContext context,
            BlueprintAbility spell,
            BlueprintBuff petrifiedBuff,
            BlueprintBuff slowedBuff)
        {
            var runAction = spell.GetComponent<AbilityEffectRunAction>();
            var actions = runAction?.Actions?.Actions;
            if (actions == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no run action found.");
                return;
            }

            runAction.SavingThrowType = SavingThrowType.Fortitude;

            var changed = false;
            foreach (var damage in actions.OfType<ContextActionDealDamage>())
            {
                if (damage.DamageType.Type != DamageType.Energy)
                {
                    continue;
                }

                damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Cold);
                damage.Value = new ContextDiceValue
                {
                    DiceType = DiceType.D8,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        ValueRank = Kingmaker.Enums.AbilityRankType.Default
                    },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                };
                damage.IsAoE = true;
                damage.HalfIfSaved = true;
                damage.AddAdditionalDamage = false;
                damage.AddFavoredEnemyDamage = false;
                changed = true;
            }

            if (!actions.Any(action => action?.name == "$ContextActionConditionalSaved$AbsoluteZeroControl"))
            {
                runAction.Actions.Actions = actions
                    .Concat(new GameAction[] { SaveControl(context, petrifiedBuff, slowedBuff) })
                    .ToArray();
                changed = true;
            }

            if (!changed)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no damage action patched.");
            }
        }

        private static ContextActionConditionalSaved SaveControl(
            SpellModifierContext context,
            BlueprintBuff petrifiedBuff,
            BlueprintBuff slowedBuff)
        {
            var applyPetrified = ApplyBuff(context, petrifiedBuff, 1, "$ContextActionApplyBuff$AbsoluteZeroPetrified");
            var applySlowed = ApplyBuff(context, slowedBuff, 3, "$ContextActionApplyBuff$AbsoluteZeroSlowed");

            return new ContextActionConditionalSaved
            {
                name = "$ContextActionConditionalSaved$AbsoluteZeroControl",
                Succeed = new ActionList { Actions = new GameAction[] { applySlowed } },
                Failed = new ActionList { Actions = new GameAction[] { applyPetrified } }
            };
        }

        private static ContextActionApplyBuff ApplyBuff(
            SpellModifierContext context,
            BlueprintBuff buff,
            int rounds,
            string name)
        {
            var applyBuff = new ContextActionApplyBuff
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
            context.Blueprints.SetApplyBuffActionBuff(applyBuff, buff);
            return applyBuff;
        }

        private static BlueprintBuff EnsurePetrifiedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.AbsoluteZeroPetrified);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_AbsoluteZeroPetrifiedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.AbsoluteZeroPetrified)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.AbsoluteZeroPetrified, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$AbsoluteZeroPetrified",
                    Condition = UnitCondition.Petrified
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$AbsoluteZeroPetrified",
                    Descriptor = SpellDescriptor.Cold
                });

            return buff;
        }

        private static BlueprintBuff EnsureSlowedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.AbsoluteZeroSlowed);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_AbsoluteZeroSlowedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.AbsoluteZeroSlowed)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.AbsoluteZeroSlowed, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$AbsoluteZeroSlowed",
                    Condition = UnitCondition.Slowed
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$AbsoluteZeroSlowed",
                    Descriptor = SpellDescriptor.Cold
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
