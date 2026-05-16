using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class FrozenLanceModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);

            var staggered = context.Blueprints.Require<BlueprintBuff>(
                GameBlueprintIds.Buffs.SnowballStaggered,
                "Snowball staggered buff");
            var immobilized = EnsureImmobilizedBuff(context);

            SpellModifierUtility.PatchRunActions(spell, action =>
            {
                var changed = 0;
                var damage = action as ContextActionDealDamage;
                if (damage != null && damage.DamageType.Type == DamageType.Energy)
                {
                    damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Cold);
                    damage.Value = new ContextDiceValue
                    {
                        DiceType = DiceType.D8,
                        DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 8 },
                        BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                    };
                    damage.AddAdditionalDamage = false;
                    damage.AddFavoredEnemyDamage = false;
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
                            new FrozenLanceFailureAction
                            {
                                name = "$FrozenLanceFailureAction$FrozenLance",
                                StaggeredBuff = staggered,
                                ImmobilizedBuff = immobilized,
                                Rounds = 1
                            }
                        }
                    };
                    changed++;
                }

                return changed;
            });
        }

        private static BlueprintBuff EnsureImmobilizedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.FrozenLanceImmobilized);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_FrozenLanceImmobilizedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.FrozenLanceImmobilized)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.FrozenLanceImmobilized, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$FrozenLanceImmobilized",
                    Condition = UnitCondition.CantMove
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$FrozenLanceImmobilized",
                    Descriptor = SpellDescriptor.Cold
                });

            return buff;
        }
    }

    public sealed class FrozenLanceFailureAction : ContextAction
    {
        public BlueprintBuff StaggeredBuff;
        public BlueprintBuff ImmobilizedBuff;
        public int Rounds = 1;

        public override string GetCaption()
        {
            return "Frozen Lance failure rider";
        }

        public override void RunAction()
        {
            var target = Target.Unit;
            if (target == null)
            {
                return;
            }

            var buff = target.State.HasCondition(UnitCondition.Slowed) ||
                       target.State.HasCondition(UnitCondition.Staggered)
                ? ImmobilizedBuff
                : StaggeredBuff;
            if (buff == null)
            {
                return;
            }

            target.Buffs.AddBuff(buff, Context, TimeSpan.FromSeconds(Math.Max(1, Rounds) * 6));
        }
    }
}
