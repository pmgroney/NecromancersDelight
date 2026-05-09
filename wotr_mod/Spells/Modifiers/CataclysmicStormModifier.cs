using System;
using System.Linq;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class CataclysmicStormModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);
            spell.Range = AbilityRange.Long;

            var spawn = FindSpawnAreaEffect(spell);
            if (spawn?.AreaEffect == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no spawn area effect found.");
                return;
            }

            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.CataclysmicStorm);
            if (area == null)
            {
                area = context.Blueprints.CloneBlueprint(
                    spawn.AreaEffect,
                    ModBlueprintIds.AreaEffects.CataclysmicStorm,
                    "WotrMod_CataclysmicStormAreaEffect");
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.CataclysmicStorm, area);
            }

            context.Blueprints.SetSpawnAreaEffect(spawn, area);
            spawn.OnUnit = false;
            spawn.DurationValue = CasterLevelRounds();

            var spellRank = context.Blueprints.EnsureComponent(spell, () => new ContextRankConfig { name = "$ContextRankConfig$CataclysmicStormSpell" });
            context.Blueprints.ConfigureContextRankConfig(spellRank);

            var areaRank = context.Blueprints.EnsureComponent(area, () => new ContextRankConfig { name = "$ContextRankConfig$CataclysmicStormArea" });
            context.Blueprints.ConfigureContextRankConfig(areaRank);

            var aoeRadius = context.Blueprints.GetComponents<AbilityAoERadius>(spell).FirstOrDefault();
            if (aoeRadius != null)
            {
                SpellModifierUtility.SetPrivateField(aoeRadius, "m_Radius", 40.Feet());
            }

            ConfigureArea(context, area);
        }

        private static void ConfigureArea(SpellModifierContext context, BlueprintAbilityAreaEffect area)
        {
            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 40.Feet();
            area.SpellResistance = true;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;

            var enterActions = EnemyOnly("$Conditional$CataclysmicStormEnemyEnter", new GameAction[]
            {
                ElectricityDamage("$ContextActionDealDamage$CataclysmicStormEnter"),
                ReflexProne("$ContextActionSavingThrow$CataclysmicStormProneEnter")
            });
            var roundActions = EnemyOnly("$Conditional$CataclysmicStormEnemyRound", new GameAction[]
            {
                ElectricityDamage("$ContextActionDealDamage$CataclysmicStormRound"),
                ReflexProne("$ContextActionSavingThrow$CataclysmicStormProneRound")
            });

            context.Blueprints.SetComponents(
                area,
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$CataclysmicStormArea",
                    Descriptor = SpellDescriptor.Electricity
                },
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$CataclysmicStorm",
                    UnitEnter = new ActionList { Actions = new GameAction[] { enterActions } },
                    UnitExit = new ActionList { Actions = Array.Empty<GameAction>() },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList { Actions = new GameAction[] { roundActions } }
                });
        }

        private static Conditional EnemyOnly(string name, GameAction[] actions)
        {
            return new Conditional
            {
                name = name,
                ConditionsChecker = new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        new ContextConditionIsEnemy
                        {
                            name = "$ContextConditionIsEnemy$CataclysmicStorm"
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
                    DiceType = DiceType.D8,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        ValueRank = Kingmaker.Enums.AbilityRankType.Default
                    },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                },
                IsAoE = true,
                HalfIfSaved = false,
                AddAdditionalDamage = false,
                AddFavoredEnemyDamage = false
            };
        }

        private static ContextActionSavingThrow ReflexProne(string name)
        {
            return new ContextActionSavingThrow
            {
                name = name,
                Type = SavingThrowType.Reflex,
                Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionConditionalSaved
                        {
                            name = "$ContextActionConditionalSaved$CataclysmicStormProne",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList
                            {
                                Actions = new GameAction[]
                                {
                                    new ContextActionKnockdownTarget
                                    {
                                        name = "$ContextActionKnockdownTarget$CataclysmicStorm"
                                    }
                                }
                            }
                        }
                    }
                }
            };
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
    }
}
