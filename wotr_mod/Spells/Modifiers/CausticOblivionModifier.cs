using System;
using Kingmaker;
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
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class CausticOblivionModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Cold, SpellDescriptor.Acid, context.Blueprints);
            spell.Range = AbilityRange.Long;

            ConfigureDamageAndRiders(
                context,
                spell,
                EnsureBurningBuff(context),
                EnsureBlindedBuff(context));
        }

        private static void ConfigureDamageAndRiders(
            SpellModifierContext context,
            BlueprintAbility spell,
            BlueprintBuff burningBuff,
            BlueprintBuff blindedBuff)
        {
            var runAction = spell.GetComponent<AbilityEffectRunAction>();
            if (runAction?.Actions == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no run action found.");
                return;
            }

            runAction.SavingThrowType = SavingThrowType.Fortitude;
            runAction.Actions.Actions = new GameAction[]
            {
                new CausticOblivionDamageAction
                {
                    name = "$CausticOblivionDamageAction$Initial",
                    DiceCount = 20,
                    DiceType = DiceType.D10,
                    HalfIfSaved = true
                },
                ApplyBuff(context, burningBuff, 3, "$ContextActionApplyBuff$CausticOblivionBurning"),
                FailedSaveBlindness(context, blindedBuff)
            };
        }

        private static ContextActionConditionalSaved FailedSaveBlindness(
            SpellModifierContext context,
            BlueprintBuff blindedBuff)
        {
            return new ContextActionConditionalSaved
            {
                name = "$ContextActionConditionalSaved$CausticOblivionBlindness",
                Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                Failed = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        ApplyBuff(context, blindedBuff, 3, "$ContextActionApplyBuff$CausticOblivionBlindness")
                    }
                }
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

        private static BlueprintBuff EnsureBurningBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.CausticOblivionBurning);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_CausticOblivionBurningBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.CausticOblivionBurning)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.CausticOblivionBurning, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddFactContextActions
                {
                    name = "$AddFactContextActions$CausticOblivionBurning",
                    Activated = new ActionList { Actions = Array.Empty<GameAction>() },
                    Deactivated = new ActionList { Actions = Array.Empty<GameAction>() },
                    Dispose = new ActionList { Actions = Array.Empty<GameAction>() },
                    NewRound = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new CausticOblivionDamageAction
                            {
                                name = "$CausticOblivionDamageAction$Burning",
                                DiceCount = 8,
                                DiceType = DiceType.D6
                            }
                        }
                    }
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$CausticOblivionBurning",
                    Descriptor = SpellDescriptor.Acid
                });

            return buff;
        }

        private static BlueprintBuff EnsureBlindedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.CausticOblivionBlinded);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_CausticOblivionBlindedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.CausticOblivionBlinded)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.CausticOblivionBlinded, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$CausticOblivionBlindness",
                    Condition = UnitCondition.Blindness
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$CausticOblivionBlindness",
                    Descriptor = SpellDescriptor.Acid
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

    public sealed class CausticOblivionDamageAction : ContextAction
    {
        public int DiceCount;
        public DiceType DiceType;
        public bool HalfIfSaved;

        public override string GetCaption()
        {
            return "Caustic Oblivion acid damage";
        }

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target.Unit;
            if (caster == null || target == null)
            {
                return;
            }

            var saved = HalfIfSaved && Context?.SavingThrow?.IsPassed == true;
            var damage = new EnergyDamage(
                new DiceFormula(Math.Max(1, DiceCount), DiceType),
                DamageEnergyType.Acid)
            {
                IgnoreImmunities = true
            };

            var rule = new RuleDealDamage(caster, target, damage)
            {
                Reason = Context,
                Half = saved,
                HalfBecauseSavingThrow = saved,
                SourceAbility = Context.SourceAbility
            };
            Rulebook.Trigger(rule);
        }
    }
}
