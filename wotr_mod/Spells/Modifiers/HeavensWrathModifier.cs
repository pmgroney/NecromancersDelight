using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
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
    internal sealed class HeavensWrathModifier : ISpellModifier
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

            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.HeavensWrath);
            if (area == null)
            {
                area = context.Blueprints.CloneBlueprint(
                    spawn.AreaEffect,
                    ModBlueprintIds.AreaEffects.HeavensWrath,
                    "WotrMod_HeavensWrathAreaEffect");
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.HeavensWrath, area);
            }

            context.Blueprints.SetSpawnAreaEffect(spawn, area);
            spawn.OnUnit = false;
            spawn.DurationValue = CasterLevelRounds();

            var spellRank = context.Blueprints.EnsureComponent(spell, () => new ContextRankConfig { name = "$ContextRankConfig$HeavensWrathSpell" });
            context.Blueprints.ConfigureContextRankConfig(spellRank);

            var areaRank = context.Blueprints.EnsureComponent(area, () => new ContextRankConfig { name = "$ContextRankConfig$HeavensWrathArea" });
            context.Blueprints.ConfigureContextRankConfig(areaRank);

            var aoeRadius = context.Blueprints.GetComponents<AbilityAoERadius>(spell).FirstOrDefault();
            if (aoeRadius != null)
            {
                SpellModifierUtility.SetPrivateField(aoeRadius, "m_Radius", 40.Feet());
            }

            ConfigureArea(context, area, EnsureStunnedBuff(context), EnsureMetalArmorPenaltyBuff(context));
        }

        private static void ConfigureArea(
            SpellModifierContext context,
            BlueprintAbilityAreaEffect area,
            BlueprintBuff stunnedBuff,
            BlueprintBuff metalArmorPenaltyBuff)
        {
            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 40.Feet();
            area.SpellResistance = true;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;
            area.Fx = new PrefabLink { AssetId = GameBlueprintIds.FxAssets.CloudThunderstormBlastArea };

            var enterActions = EnemyOnly("$Conditional$HeavensWrathEnemyEnter",
                StrikeActions(context, stunnedBuff, metalArmorPenaltyBuff, "Enter"));
            var roundActions = EnemyOnly("$Conditional$HeavensWrathEnemyRound",
                StrikeActions(context, stunnedBuff, metalArmorPenaltyBuff, "Round"));

            context.Blueprints.SetComponents(
                area,
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$HeavensWrathArea",
                    Descriptor = SpellDescriptor.Electricity
                },
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$HeavensWrath",
                    UnitEnter = new ActionList { Actions = new GameAction[] { enterActions } },
                    UnitExit = new ActionList { Actions = Array.Empty<GameAction>() },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList { Actions = new GameAction[] { roundActions } }
                });
        }

        private static GameAction[] StrikeActions(
            SpellModifierContext context,
            BlueprintBuff stunnedBuff,
            BlueprintBuff metalArmorPenaltyBuff,
            string suffix)
        {
            var applyPenalty = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$HeavensWrathMetalArmorPenalty" + suffix,
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(1),
                IsFromSpell = true,
                IsNotDispelable = true,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyPenalty, metalArmorPenaltyBuff);

            var removePenalty = new ContextActionRemoveBuff
            {
                name = "$ContextActionRemoveBuff$HeavensWrathMetalArmorPenalty" + suffix,
                RemoveRank = false,
                ToCaster = false,
                OnlyFromCaster = true
            };
            context.Blueprints.SetRemoveBuffActionBuff(removePenalty, metalArmorPenaltyBuff);

            return new GameAction[]
            {
                ElectricityDamage("$ContextActionDealDamage$HeavensWrath" + suffix),
                applyPenalty,
                FortitudeRider(context, stunnedBuff, "$ContextActionSavingThrow$HeavensWrath" + suffix),
                removePenalty
            };
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
                            name = "$ContextConditionIsEnemy$HeavensWrath"
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
                    DiceType = DiceType.D12,
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

        private static ContextActionSavingThrow FortitudeRider(
            SpellModifierContext context,
            BlueprintBuff stunnedBuff,
            string name)
        {
            var applyStun = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$HeavensWrathStunned",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(1),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyStun, stunnedBuff);

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
                            name = "$ContextActionConditionalSaved$HeavensWrath",
                            Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                            Failed = new ActionList
                            {
                                Actions = new GameAction[]
                                {
                                    applyStun,
                                    new HeavensWrathJumpAction
                                    {
                                        name = "$HeavensWrathJumpAction$FailedSave",
                                        RadiusFeet = 20,
                                        DiceCount = 6
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static BlueprintBuff EnsureStunnedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.HeavensWrathStunned);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_HeavensWrathStunnedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.HeavensWrathStunned)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.HeavensWrathStunned, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$HeavensWrathStunned",
                    Condition = UnitCondition.Stunned
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$HeavensWrathStunned",
                    Descriptor = SpellDescriptor.Electricity
                });

            return buff;
        }

        private static BlueprintBuff EnsureMetalArmorPenaltyBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.HeavensWrathMetalArmorPenalty);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_HeavensWrathMetalArmorPenaltyBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.HeavensWrathMetalArmorPenalty)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.HeavensWrathMetalArmorPenalty, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new HeavensWrathMetalArmorSavePenalty
                {
                    name = "$HeavensWrathMetalArmorSavePenalty$Fortitude",
                    Penalty = -4
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

    public sealed class HeavensWrathJumpAction : ContextAction
    {
        public int RadiusFeet = 20;
        public int DiceCount = 6;

        public override string GetCaption()
        {
            return "Heaven's Wrath lightning jump";
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
                .Where(unit => IsValidJumpTarget(caster, source, unit, RadiusFeet))
                .ToArray();
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                var damage = new EnergyDamage(
                    new DiceFormula(Math.Max(1, DiceCount), DiceType.D12),
                    DamageEnergyType.Electricity);
                var rule = new RuleDealDamage(caster, target, damage)
                {
                    Reason = Context
                };
                Rulebook.Trigger(rule);
            }
        }

        private static bool IsValidJumpTarget(UnitEntityData caster, UnitEntityData source, UnitEntityData target, int radiusFeet)
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

    public sealed class HeavensWrathMetalArmorSavePenalty :
        UnitFactComponentDelegate,
        ITargetRulebookHandler<RuleSavingThrow>,
        IRulebookHandler<RuleSavingThrow>,
        ITargetRulebookSubscriber
    {
        public int Penalty = -4;

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            if (evt == null || evt.Type != SavingThrowType.Fortitude)
            {
                return;
            }

            if (!IsWearingArmor(Owner))
            {
                return;
            }

            evt.AddModifier(Penalty, Fact, ModifierDescriptor.Penalty);
        }

        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
        }

        private static bool IsWearingArmor(UnitEntityData unit)
        {
            var armor = unit?.Body?.Armor?.MaybeArmor;
            var group = armor?.Blueprint?.Type?.ProficiencyGroup ?? ArmorProficiencyGroup.None;
            return group == ArmorProficiencyGroup.Light ||
                   group == ArmorProficiencyGroup.Medium ||
                   group == ArmorProficiencyGroup.Heavy ||
                   group == ArmorProficiencyGroup.LightBarding ||
                   group == ArmorProficiencyGroup.MediumBarding ||
                   group == ArmorProficiencyGroup.HeavyBarding;
        }
    }
}
