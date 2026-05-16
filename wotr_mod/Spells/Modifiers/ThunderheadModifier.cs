using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class ThunderheadModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);

            var spawn = FindSpawnAreaEffect(spell);
            if (spawn?.AreaEffect == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no spawn area effect found.");
                return;
            }

            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.Thunderhead);
            if (area == null)
            {
                area = context.Blueprints.CloneBlueprint(
                    spawn.AreaEffect,
                    ModBlueprintIds.AreaEffects.Thunderhead,
                    "WotrMod_ThunderheadAreaEffect");
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.Thunderhead, area);
            }

            context.Blueprints.SetSpawnAreaEffect(spawn, area);
            spawn.OnUnit = false;
            spawn.DurationValue = CasterLevelRounds();

            var rank = context.Blueprints.EnsureComponent(spell, () => new ContextRankConfig { name = "$ContextRankConfig$Thunderhead" });
            context.Blueprints.ConfigureContextRankConfig(rank);

            ConfigureArea(context, area, EnsureDazeBuff(context));
        }

        private static void ConfigureArea(SpellModifierContext context, BlueprintAbilityAreaEffect area, BlueprintBuff dazeBuff)
        {
            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 20.Feet();
            area.SpellResistance = true;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;

            var enterActions = EnemyOnly(new GameAction[]
            {
                ElectricityDamage("$ContextActionDealDamage$ThunderheadEnter"),
                FortitudeRider(context, dazeBuff, "$ContextActionSavingThrow$ThunderheadEnter")
            });
            var roundActions = EnemyOnly(new GameAction[]
            {
                ElectricityDamage("$ContextActionDealDamage$ThunderheadRound"),
                FortitudeRider(context, dazeBuff, "$ContextActionSavingThrow$ThunderheadRound"),
                new ThunderheadArcAction
                {
                    name = "$ThunderheadArcAction$ThunderheadRound",
                    RadiusFeet = 20,
                    DiceCount = 1
                }
            });

            context.Blueprints.SetComponents(
                area,
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$ThunderheadArea",
                    Descriptor = SpellDescriptor.Electricity
                },
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$Thunderhead",
                    UnitEnter = new ActionList { Actions = new GameAction[] { enterActions } },
                    UnitExit = new ActionList { Actions = Array.Empty<GameAction>() },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList { Actions = new GameAction[] { roundActions } }
                });
        }

        private static Conditional EnemyOnly(GameAction[] actions)
        {
            return new Conditional
            {
                name = "$Conditional$ThunderheadEnemy",
                ConditionsChecker = new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        new ContextConditionIsEnemy
                        {
                            name = "$ContextConditionIsEnemy$Thunderhead"
                        }
                    }
                },
                IfTrue = new ActionList { Actions = actions },
                IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
            };
        }

        private static ContextActionDealDamage ElectricityDamage(string name)
        {
            return new ContextActionDealDamage
            {
                name = name,
                DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Electricity),
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.D6,
                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 4 },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                },
                IsAoE = true,
                HalfIfSaved = false
            };
        }

        private static ContextActionSavingThrow FortitudeRider(
            SpellModifierContext context,
            BlueprintBuff dazeBuff,
            string name)
        {
            var applyBuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$ThunderheadDaze",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(1),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyBuff, dazeBuff);

            return new ContextActionSavingThrow
            {
                name = name,
                Type = SavingThrowType.Fortitude,
                Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionConditionalSaved
                        {
                            name = "$ContextActionConditionalSaved$ThunderheadDaze",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList { Actions = new GameAction[] { applyBuff } }
                        }
                    }
                }
            };
        }

        private static BlueprintBuff EnsureDazeBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ThunderheadDaze);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_ThunderheadDazeBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.ThunderheadDaze)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ThunderheadDaze, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$ThunderheadDazed",
                    Condition = UnitCondition.Dazed
                },
                new AddCondition
                {
                    name = "$AddCondition$ThunderheadDazzled",
                    Condition = UnitCondition.Dazzled
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$ThunderheadDaze",
                    Descriptor = SpellDescriptor.Electricity
                });

            return buff;
        }

        private static ContextActionSpawnAreaEffect FindSpawnAreaEffect(BlueprintAbility spell)
        {
            ContextActionSpawnAreaEffect result = null;
            SpellModifierUtility.PatchRunActions(spell, action =>
            {
                if (result == null)
                {
                    result = action as ContextActionSpawnAreaEffect;
                }

                return 0;
            });

            return result;
        }

        private static ContextDurationValue CasterLevelRounds()
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue
                {
                    ValueType = ContextValueType.Rank,
                    ValueRank = Kingmaker.Enums.AbilityRankType.Default
                }
            };
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

    public sealed class ThunderheadArcAction : ContextAction
    {
        public int RadiusFeet = 20;
        public int DiceCount = 1;

        public override string GetCaption()
        {
            return "Thunderhead random arc";
        }

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var source = Target.Unit;
            if (caster == null || source == null)
            {
                return;
            }

            var targets = Game.Instance?.State?.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Where(unit => IsValidArcTarget(caster, source, unit, RadiusFeet))
                .ToArray();
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            var target = targets[UnityEngine.Random.Range(0, targets.Length)];
            var damage = new EnergyDamage(new DiceFormula(Math.Max(1, DiceCount), DiceType.D6), DamageEnergyType.Electricity);
            var rule = new RuleDealDamage(caster, target, damage)
            {
                Reason = Context
            };
            Rulebook.Trigger(rule);
        }

        private static bool IsValidArcTarget(UnitEntityData caster, UnitEntityData source, UnitEntityData target, int radiusFeet)
        {
            if (target == null || target == source || !caster.IsEnemy(target))
            {
                return false;
            }

            if (target.State.IsDead || target.State.IsFinallyDead)
            {
                return false;
            }

            return source.DistanceTo(target) <= radiusFeet.Feet().Meters;
        }
    }
}
