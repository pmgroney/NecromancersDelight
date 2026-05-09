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
    internal sealed class DissolutionWaveModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);

            foreach (var delivery in context.Blueprints.GetComponents<AbilityDeliverProjectile>(spell))
            {
                context.Blueprints.SetAbilityDeliverProjectileLength(delivery, 40.Feet());
            }

            foreach (var rank in context.Blueprints.GetComponents<ContextRankConfig>(spell))
            {
                context.Blueprints.SetContextRankMaximum(rank, 20);
            }

            ConfigureDamageAndRider(context, spell, EnsureCorrodedBuff(context));
        }

        private static void ConfigureDamageAndRider(
            SpellModifierContext context,
            BlueprintAbility spell,
            BlueprintBuff corrodedBuff)
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

                damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Acid);
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

            if (!actions.Any(action => action?.name == "$ContextActionConditionalSaved$DissolutionWaveCorroded"))
            {
                runAction.Actions.Actions = actions
                    .Concat(new GameAction[] { FailedSaveCorrosion(context, corrodedBuff) })
                    .ToArray();
                changed = true;
            }

            if (!changed)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no damage action patched.");
            }
        }

        private static ContextActionConditionalSaved FailedSaveCorrosion(
            SpellModifierContext context,
            BlueprintBuff corrodedBuff)
        {
            var applyCorroded = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$DissolutionWaveCorroded",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(3),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyCorroded, corrodedBuff);

            return new ContextActionConditionalSaved
            {
                name = "$ContextActionConditionalSaved$DissolutionWaveCorroded",
                Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                Failed = new ActionList { Actions = new GameAction[] { applyCorroded } }
            };
        }

        private static BlueprintBuff EnsureCorrodedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.DissolutionWaveCorroded);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_DissolutionWaveCorrodedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.DissolutionWaveCorroded)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.DissolutionWaveCorroded, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddStatBonus
                {
                    name = "$AddStatBonus$DissolutionWaveCorrodedNaturalArmor",
                    Stat = StatType.AC,
                    Value = -4,
                    Descriptor = ModifierDescriptor.NaturalArmor
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
