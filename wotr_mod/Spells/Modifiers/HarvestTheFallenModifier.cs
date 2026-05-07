using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class HarvestTheFallenModifier : ISpellModifier
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
            spell.NotOffensive = true;

            var temporaryHitPointsBuff = EnsureTemporaryHitPointsBuff(context);

            var rank = new ContextRankConfig { name = "$ContextRankConfig$HarvestTheFallenTemporaryHitPoints" };
            context.Blueprints.ConfigureContextRankConfig(
                rank,
                progression: ContextRankProgression.DoublePlusBonusValue);

            context.Blueprints.SetComponents(
                spell,
                new SpellComponent { name = "$SpellComponent$HarvestTheFallen", School = SpellSchool.Necromancy },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$HarvestTheFallen",
                    Descriptor = SpellDescriptor.Death
                },
                rank,
                new AbilityEffectRunAction
                {
                    name = "$AbilityEffectRunAction$HarvestTheFallen",
                    SavingThrowType = SavingThrowType.Unknown,
                    Actions = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new HarvestTheFallenAction
                            {
                                name = "$HarvestTheFallenAction$Harvest",
                                RadiusFeet = 30,
                                MaxStacks = 5,
                                TemporaryHitPointsBuff = temporaryHitPointsBuff
                            }
                        }
                    }
                });
        }

        private static BlueprintBuff EnsureTemporaryHitPointsBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.HarvestTheFallenTemporaryHitPoints);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_HarvestTheFallenTemporaryHitPointsBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.HarvestTheFallenTemporaryHitPoints)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.HarvestTheFallenTemporaryHitPoints, buff);
            }

            buff.Stacking = StackingType.Stack;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new TemporaryHitPointsFromAbilityValue
                {
                    name = "$TemporaryHitPointsFromAbilityValue$HarvestTheFallen",
                    Descriptor = ModifierDescriptor.UntypedStackable,
                    Value = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        ValueRank = AbilityRankType.Default
                    },
                    RemoveWhenHitPointsEnd = true
                });
            return buff;
        }

    }

    public sealed class HarvestTheFallenAction : ContextAction
    {
        public int RadiusFeet;
        public int MaxStacks;
        public BlueprintBuff TemporaryHitPointsBuff;

        public override string GetCaption()
        {
            return "Harvest the Fallen";
        }

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            if (caster == null || TemporaryHitPointsBuff == null)
            {
                return;
            }

            var casterLevel = Math.Max(1, Context.Params?.CasterLevel ?? 1);
            var availableStacks = Math.Max(0, MaxStacks - CountExistingStacks(caster));
            if (availableStacks <= 0)
            {
                return;
            }

            var stacks = CountHarvestTargets(caster, RadiusFeet);
            stacks = Math.Min(stacks, availableStacks);
            for (var i = 0; i < stacks; i++)
            {
                HealCaster(caster, casterLevel, Context);
                caster.Buffs.AddBuff(
                    TemporaryHitPointsBuff,
                    Context,
                    TimeSpan.FromMinutes(10 * casterLevel));
            }
        }

        private int CountExistingStacks(UnitEntityData caster)
        {
            var count = 0;
            foreach (var buff in caster.Buffs)
            {
                if (buff?.Blueprint == TemporaryHitPointsBuff)
                {
                    count++;
                }
            }

            return count;
        }

        private static void HealCaster(UnitEntityData caster, int casterLevel, MechanicsContext context)
        {
            var heal = new RuleHealDamage(caster, caster, new DiceFormula(1, DiceType.D8), casterLevel)
            {
                Reason = context
            };
            Rulebook.Trigger(heal);
        }

        private static int CountHarvestTargets(UnitEntityData caster, int radiusFeet)
        {
            var areaState = Game.Instance?.State?.LoadedAreaState;
            if (areaState == null)
            {
                return 0;
            }

            var radiusMeters = radiusFeet.Feet().Meters;
            return areaState.AllEntityData
                .OfType<UnitEntityData>()
                .Count(unit => IsHarvestTarget(caster, unit, radiusMeters));
        }

        private static bool IsHarvestTarget(UnitEntityData caster, UnitEntityData unit, float radiusMeters)
        {
            if (unit == null || unit == caster || !caster.IsEnemy(unit))
            {
                return false;
            }

            if (caster.DistanceTo(unit) > radiusMeters)
            {
                return false;
            }

            if (unit.State.IsDead || unit.State.IsFinallyDead)
            {
                return true;
            }

            return unit.MaxHP > 0 && unit.HPLeft * 4 <= unit.MaxHP;
        }
    }
}
