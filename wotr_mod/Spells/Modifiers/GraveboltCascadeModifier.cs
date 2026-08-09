using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
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
    internal sealed class GraveboltCascadeModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, context.Blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Force, SpellDescriptor.Death, context.Blueprints);

            spell.SpellResistance = true;
            spell.CanTargetEnemies = true;
            spell.CanTargetFriends = false;
            spell.CanTargetSelf = false;

            ConfigureProjectileCount(context);
            ConfigureEffect(context, EnsureSickenedBuff(context));
        }

        private static void ConfigureProjectileCount(SpellModifierContext context)
        {
            context.Blueprints.RemoveComponents<ContextRankConfig>(context.Ability);

            var projectileCount = new ContextRankConfig { name = "$ContextRankConfig$GraveboltCascadeProjectiles" };
            context.Blueprints.ConfigureContextRankConfig(
                projectileCount,
                type: AbilityRankType.ProjectilesCount);
            context.Blueprints.SetContextRankMinimum(projectileCount, 3);
            context.Blueprints.SetContextRankMaximum(projectileCount, 3);
            context.Blueprints.AddComponent(context.Ability, projectileCount);

            foreach (var delivery in context.Blueprints.GetComponents<AbilityDeliverProjectile>(context.Ability))
            {
                delivery.UseMaxProjectilesCount = true;
                delivery.MaxProjectilesCountRank = AbilityRankType.ProjectilesCount;
                delivery.NeedAttackRoll = false;
            }
        }

        private static void ConfigureEffect(SpellModifierContext context, BlueprintBuff sickenedBuff)
        {
            var runAction = context.Blueprints.GetComponents<AbilityEffectRunAction>(context.Ability).FirstOrDefault();
            if (runAction == null)
            {
                runAction = new AbilityEffectRunAction { name = "$AbilityEffectRunAction$GraveboltCascade" };
                context.Blueprints.AddComponent(context.Ability, runAction);
            }

            runAction.SavingThrowType = SavingThrowType.Fortitude;
            runAction.Actions = new ActionList
            {
                Actions = new GameAction[]
                {
                    new GraveboltCascadeAction
                    {
                        name = "$GraveboltCascadeAction$DamageOrHeal",
                        UndeadType = context.Blueprints.Require<BlueprintFeature>(
                            GameBlueprintIds.Features.UndeadType,
                            "Undead type")
                    },
                    FortitudeSickened(context, sickenedBuff)
                }
            };
        }

        private static ContextActionSavingThrow FortitudeSickened(
            SpellModifierContext context,
            BlueprintBuff sickenedBuff)
        {
            var applyBuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$GraveboltCascadeSickened",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(1),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyBuff, sickenedBuff);

            return new ContextActionSavingThrow
            {
                name = "$ContextActionSavingThrow$GraveboltCascadeSickened",
                Type = SavingThrowType.Fortitude,
                Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionConditionalSaved
                        {
                            name = "$ContextActionConditionalSaved$GraveboltCascadeSickened",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList { Actions = new GameAction[] { applyBuff } }
                        }
                    }
                }
            };
        }

        private static BlueprintBuff EnsureSickenedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.GraveboltCascadeSickened);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_GraveboltCascadeSickenedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.GraveboltCascadeSickened)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.GraveboltCascadeSickened, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$GraveboltCascadeSickened",
                    Condition = UnitCondition.Sickened
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$GraveboltCascadeSickened",
                    Descriptor = SpellDescriptor.Sickened
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

    public sealed class GraveboltCascadeAction : ContextAction
    {
        public BlueprintFeature UndeadType;

        public override string GetCaption()
        {
            return "Gravebolt Cascade damage or undead healing";
        }

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target.Unit;
            if (caster == null || target == null)
            {
                return;
            }

            var casterLevel = Math.Max(1, Context.Params?.CasterLevel ?? 1);
            if (IsUndead(target))
            {
                HealTarget(caster, target, casterLevel);
                return;
            }

            DealUnholyDamage(caster, target, casterLevel);
        }

        private bool IsUndead(UnitEntityData unit)
        {
            return UndeadType != null &&
                   unit?.Descriptor?.Progression?.Features?.HasFact(UndeadType) == true;
        }

        private void DealUnholyDamage(UnitEntityData caster, UnitEntityData target, int casterLevel)
        {
            var damage = new EnergyDamage(new DiceFormula(1, DiceType.D8), casterLevel, DamageEnergyType.Unholy);
            var rule = new RuleDealDamage(caster, target, damage)
            {
                Reason = Context,
                SourceAbility = Context.SourceAbility
            };
            Rulebook.Trigger(rule);
        }

        private void HealTarget(UnitEntityData caster, UnitEntityData target, int casterLevel)
        {
            var heal = new RuleHealDamage(caster, target, new DiceFormula(1, DiceType.D8), casterLevel)
            {
                Reason = Context
            };
            Rulebook.Trigger(heal);
        }
    }
}
