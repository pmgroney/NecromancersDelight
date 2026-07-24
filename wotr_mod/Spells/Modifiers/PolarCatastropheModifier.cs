using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
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
    internal sealed class PolarCatastropheModifier : ISpellModifier
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

            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.PolarCatastrophe);
            if (area == null)
            {
                area = context.Blueprints.CloneBlueprint(
                    spawn.AreaEffect,
                    ModBlueprintIds.AreaEffects.PolarCatastrophe,
                    "WotrMod_PolarCatastropheAreaEffect");
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.PolarCatastrophe, area);
            }

            context.Blueprints.SetSpawnAreaEffect(spawn, area);
            spawn.OnUnit = false;
            spawn.DurationValue = CasterLevelRounds();

            var spellRank = context.Blueprints.EnsureComponent(spell, () => new ContextRankConfig { name = "$ContextRankConfig$PolarCatastropheSpell" });
            context.Blueprints.ConfigureContextRankConfig(spellRank);

            var areaRank = context.Blueprints.EnsureComponent(area, () => new ContextRankConfig { name = "$ContextRankConfig$PolarCatastropheArea" });
            context.Blueprints.ConfigureContextRankConfig(areaRank);

            var aoeRadius = context.Blueprints.GetComponents<AbilityAoERadius>(spell).FirstOrDefault();
            if (aoeRadius != null)
            {
                SpellModifierUtility.SetPrivateField(aoeRadius, "m_Radius", 40.Feet());
            }

            ConfigureArea(context, area, EnsureExhaustedBuff(context));
        }

        private static void ConfigureArea(SpellModifierContext context, BlueprintAbilityAreaEffect area, BlueprintBuff exhaustedBuff)
        {
            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 40.Feet();
            area.SpellResistance = true;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;
            area.Fx = new PrefabLink { AssetId = GameBlueprintIds.FxAssets.IceStormArea };

            var enterActions = EnemyOnly("$Conditional$PolarCatastropheEnemyEnter", new GameAction[]
            {
                ColdDamage("$ContextActionDealDamage$PolarCatastropheEnter"),
                FortitudeExhausted(context, exhaustedBuff, "$ContextActionSavingThrow$PolarCatastropheEnter")
            });
            var roundActions = EnemyOnly("$Conditional$PolarCatastropheEnemyRound", new GameAction[]
            {
                ColdDamage("$ContextActionDealDamage$PolarCatastropheRound"),
                FortitudeExhausted(context, exhaustedBuff, "$ContextActionSavingThrow$PolarCatastropheRound")
            });

            context.Blueprints.SetComponents(
                area,
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$PolarCatastropheArea",
                    Descriptor = SpellDescriptor.Cold
                },
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$PolarCatastrophe",
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
                            name = "$ContextConditionIsEnemy$PolarCatastrophe"
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
                        ValueRank = AbilityRankType.Default
                    },
                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                },
                IsAoE = true,
                HalfIfSaved = false,
                AddAdditionalDamage = false,
                AddFavoredEnemyDamage = false
            };
        }

        private static ContextActionSavingThrow FortitudeExhausted(
            SpellModifierContext context,
            BlueprintBuff exhaustedBuff,
            string name)
        {
            var applyExhausted = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$PolarCatastropheExhausted",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(1),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyExhausted, exhaustedBuff);

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
                            name = "$ContextActionConditionalSaved$PolarCatastropheExhausted",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList { Actions = new GameAction[] { applyExhausted } }
                        }
                    }
                }
            };
        }

        private static BlueprintBuff EnsureExhaustedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.PolarCatastropheExhausted);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_PolarCatastropheExhaustedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.PolarCatastropheExhausted)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.PolarCatastropheExhausted, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$PolarCatastropheExhausted",
                    Condition = UnitCondition.Exhausted
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$PolarCatastropheExhausted",
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
                    ValueRank = AbilityRankType.Default
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
