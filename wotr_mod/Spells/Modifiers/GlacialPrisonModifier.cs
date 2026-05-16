using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
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
    internal sealed class GlacialPrisonModifier : ISpellModifier
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

            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.GlacialPrison);
            if (area == null)
            {
                area = context.Blueprints.CloneBlueprint(
                    spawn.AreaEffect,
                    ModBlueprintIds.AreaEffects.GlacialPrison,
                    "WotrMod_GlacialPrisonAreaEffect");
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.GlacialPrison, area);
            }

            context.Blueprints.SetSpawnAreaEffect(spawn, area);
            spawn.OnUnit = false;
            spawn.DurationValue = CasterLevelRounds();

            var rank = context.Blueprints.EnsureComponent(spell, () => new ContextRankConfig { name = "$ContextRankConfig$GlacialPrison" });
            context.Blueprints.ConfigureContextRankConfig(rank);

            var aoeRadius = context.Blueprints.GetComponents<AbilityAoERadius>(spell).FirstOrDefault();
            if (aoeRadius != null)
            {
                SpellModifierUtility.SetPrivateField(aoeRadius, "m_Radius", 40.Feet());
            }

            ConfigureArea(
                context,
                area,
                EnsureConditionBuff(context, ModBlueprintIds.Buffs.GlacialPrisonDifficultTerrain, "DifficultTerrain", UnitCondition.DifficultTerrain),
                EnsureConditionBuff(context, ModBlueprintIds.Buffs.GlacialPrisonEntangled, "Entangled", UnitCondition.Entangled),
                EnsureConditionBuff(context, ModBlueprintIds.Buffs.GlacialPrisonParalyzed, "Paralyzed", UnitCondition.Paralyzed));
        }

        private static void ConfigureArea(
            SpellModifierContext context,
            BlueprintAbilityAreaEffect area,
            BlueprintBuff difficultTerrainBuff,
            BlueprintBuff entangledBuff,
            BlueprintBuff paralyzedBuff)
        {
            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 40.Feet();
            area.SpellResistance = true;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;

            context.Blueprints.SetComponents(
                area,
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$GlacialPrisonArea",
                    Descriptor = SpellDescriptor.Cold
                },
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$GlacialPrison",
                    UnitEnter = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            EnemyOnly(new GameAction[]
                            {
                                ApplyLinkedBuff(context, difficultTerrainBuff, "$ContextActionApplyBuff$GlacialPrisonDifficultTerrain"),
                                ColdDamage("$ContextActionDealDamage$GlacialPrisonEnter"),
                                ReflexThenFortitude(context, entangledBuff, paralyzedBuff, "$ContextActionSavingThrow$GlacialPrisonEnter")
                            })
                        }
                    },
                    UnitExit = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            RemoveBuff(context, difficultTerrainBuff, "$ContextActionRemoveBuff$GlacialPrisonDifficultTerrain"),
                            RemoveBuff(context, entangledBuff, "$ContextActionRemoveBuff$GlacialPrisonEntangled"),
                            RemoveBuff(context, paralyzedBuff, "$ContextActionRemoveBuff$GlacialPrisonParalyzed")
                        }
                    },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            EnemyOnly(new GameAction[]
                            {
                                ColdDamage("$ContextActionDealDamage$GlacialPrisonRound"),
                                ReflexThenFortitude(context, entangledBuff, paralyzedBuff, "$ContextActionSavingThrow$GlacialPrisonRound")
                            })
                        }
                    }
                });
        }

        private static Conditional EnemyOnly(GameAction[] actions)
        {
            return new Conditional
            {
                name = "$Conditional$GlacialPrisonEnemy",
                ConditionsChecker = new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        new ContextConditionIsEnemy
                        {
                            name = "$ContextConditionIsEnemy$GlacialPrison"
                        }
                    }
                },
                IfTrue = new ActionList { Actions = actions },
                IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
            };
        }

        private static ContextActionDealDamage ColdDamage(string name)
        {
            return new ContextActionDealDamage
            {
                name = name,
                DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Cold),
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

        private static ContextActionSavingThrow ReflexThenFortitude(
            SpellModifierContext context,
            BlueprintBuff entangledBuff,
            BlueprintBuff paralyzedBuff,
            string name)
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
                            name = "$ContextActionConditionalSaved$GlacialPrisonEntangle",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList
                            {
                                Actions = new GameAction[]
                                {
                                    ApplyLinkedBuff(context, entangledBuff, "$ContextActionApplyBuff$GlacialPrisonEntangled"),
                                    FortitudeParalysis(context, paralyzedBuff)
                                }
                            }
                        }
                    }
                }
            };
        }

        private static ContextActionSavingThrow FortitudeParalysis(
            SpellModifierContext context,
            BlueprintBuff paralyzedBuff)
        {
            return new ContextActionSavingThrow
            {
                name = "$ContextActionSavingThrow$GlacialPrisonParalyzed",
                Type = SavingThrowType.Fortitude,
                Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionConditionalSaved
                        {
                            name = "$ContextActionConditionalSaved$GlacialPrisonParalyzed",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList
                            {
                                Actions = new GameAction[]
                                {
                                    ApplyLinkedBuff(context, paralyzedBuff, "$ContextActionApplyBuff$GlacialPrisonParalyzed")
                                }
                            }
                        }
                    }
                }
            };
        }

        private static ContextActionApplyBuff ApplyLinkedBuff(
            SpellModifierContext context,
            BlueprintBuff buff,
            string name)
        {
            var action = new ContextActionApplyBuff
            {
                name = name,
                Permanent = true,
                UseDurationSeconds = false,
                DurationValue = Rounds(0),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = true,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(action, buff);
            return action;
        }

        private static ContextActionRemoveBuff RemoveBuff(
            SpellModifierContext context,
            BlueprintBuff buff,
            string name)
        {
            var action = new ContextActionRemoveBuff
            {
                name = name,
                RemoveRank = false,
                ToCaster = false,
                OnlyFromCaster = false
            };
            context.Blueprints.SetRemoveBuffActionBuff(action, buff);
            return action;
        }

        private static BlueprintBuff EnsureConditionBuff(
            SpellModifierContext context,
            string guid,
            string suffix,
            UnitCondition condition)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(guid);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_GlacialPrison" + suffix + "Buff",
                    AssetGuid = BlueprintGuid.Parse(guid)
                };
                context.Blueprints.AddCachedBlueprint(guid, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$GlacialPrison" + suffix,
                    Condition = condition
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$GlacialPrison" + suffix,
                    Descriptor = SpellDescriptor.Cold
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
}
