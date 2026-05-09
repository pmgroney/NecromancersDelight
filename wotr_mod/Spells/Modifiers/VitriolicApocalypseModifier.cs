using System;
using System.Linq;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class VitriolicApocalypseModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Fire, SpellDescriptor.Acid | SpellDescriptor.Evil, context.Blueprints);
            spell.Range = AbilityRange.Custom;
            spell.CustomRange = 120.Feet();
            spell.SpellResistance = true;

            var rank = context.Blueprints.EnsureComponent(
                spell,
                () => new ContextRankConfig { name = "$ContextRankConfig$VitriolicApocalypse" });
            context.Blueprints.ConfigureContextRankConfig(rank);

            foreach (var targetAround in context.Blueprints.GetComponents<AbilityTargetsAround>(spell))
            {
                SpellModifierUtility.SetPrivateField(targetAround, "m_Radius", 40.Feet());
            }

            var aoeRadius = context.Blueprints.GetComponents<AbilityAoERadius>(spell).FirstOrDefault();
            if (aoeRadius != null)
            {
                SpellModifierUtility.SetPrivateField(aoeRadius, "m_Radius", 40.Feet());
            }

            ConfigureDamage(context, spell, EnsureMolecularDissolutionBuff(context));
        }

        private static void ConfigureDamage(
            SpellModifierContext context,
            BlueprintAbility spell,
            BlueprintBuff molecularDissolution)
        {
            var runAction = spell.GetComponent<AbilityEffectRunAction>();
            var actions = runAction?.Actions?.Actions;
            if (actions == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no run action found.");
                return;
            }

            var changed = false;
            var patched = actions.SelectMany(action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage == null ||
                    damage.DamageType.Type != DamageType.Energy ||
                    damage.DamageType.Energy != DamageEnergyType.Fire)
                {
                    return new[] { action };
                }

                changed = true;
                return new GameAction[]
                {
                    CreateDamageAction(damage, "$ContextActionDealDamage$VitriolicApocalypseAcid", DamageEnergyType.Acid),
                    CreateDamageAction(damage, "$ContextActionDealDamage$VitriolicApocalypseUnholy", DamageEnergyType.Unholy)
                };
            }).ToArray();

            if (changed)
            {
                runAction.Actions.Actions = patched;
            }

            AddMolecularDissolutionAction(context, runAction, molecularDissolution);
        }

        private static void AddMolecularDissolutionAction(
            SpellModifierContext context,
            AbilityEffectRunAction runAction,
            BlueprintBuff molecularDissolution)
        {
            var actions = runAction.Actions.Actions;
            if (actions.Any(action => action?.name == "$ContextActionConditionalSaved$VitriolicApocalypseMolecularDissolution"))
            {
                return;
            }

            var applyBuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$VitriolicApocalypseMolecularDissolution",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(3),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyBuff, molecularDissolution);

            runAction.Actions.Actions = actions
                .Concat(new GameAction[]
                {
                    new ContextActionConditionalSaved
                    {
                        name = "$ContextActionConditionalSaved$VitriolicApocalypseMolecularDissolution",
                        Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                        Failed = new ActionList { Actions = new GameAction[] { applyBuff } }
                    }
                })
                .ToArray();
        }

        private static ContextActionDealDamage CreateDamageAction(
            ContextActionDealDamage source,
            string name,
            DamageEnergyType energyType)
        {
            return new ContextActionDealDamage
            {
                name = name,
                DamageType = SpellModifierUtility.EnergyDamage(energyType),
                Drain = source.Drain,
                AbilityType = source.AbilityType,
                EnergyDrainType = source.EnergyDrainType,
                Duration = source.Duration,
                ReadPreRolledFromSharedValue = source.ReadPreRolledFromSharedValue,
                PreRolledSharedValue = source.PreRolledSharedValue,
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
                Half = source.Half,
                DisableSneakDamage = source.DisableSneakDamage,
                AlreadyHalved = source.AlreadyHalved,
                IsAoE = true,
                HalfIfSaved = true,
                IgnoreCritical = source.IgnoreCritical,
                IgnoreUnitModifiers = source.IgnoreUnitModifiers,
                DisableKineticCache = source.DisableKineticCache,
                AddAdditionalDamage = false,
                AddFavoredEnemyDamage = false,
                UseWeaponDamageModifiers = source.UseWeaponDamageModifiers,
                UseMinHPAfterDamage = source.UseMinHPAfterDamage,
                MinHPAfterDamage = source.MinHPAfterDamage,
                WriteResultToSharedValue = source.WriteResultToSharedValue,
                WriteRawResultToSharedValue = source.WriteRawResultToSharedValue,
                ResultSharedValue = source.ResultSharedValue,
                WriteCriticalToSharedValue = source.WriteCriticalToSharedValue,
                CriticalSharedValue = source.CriticalSharedValue,
                SetFactAsReason = source.SetFactAsReason
            };
        }

        private static BlueprintBuff EnsureMolecularDissolutionBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.MolecularDissolution);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_MolecularDissolutionBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.MolecularDissolution)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.MolecularDissolution, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddFactContextActions
                {
                    name = "$AddFactContextActions$MolecularDissolution",
                    Activated = new ActionList { Actions = Array.Empty<GameAction>() },
                    Deactivated = new ActionList { Actions = Array.Empty<GameAction>() },
                    Dispose = new ActionList { Actions = Array.Empty<GameAction>() },
                    NewRound = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new ContextActionDealDamage
                            {
                                name = "$ContextActionDealDamage$MolecularDissolution",
                                DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Acid),
                                Value = new ContextDiceValue
                                {
                                    DiceType = DiceType.D8,
                                    DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 8 },
                                    BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                                },
                                IsAoE = true,
                                HalfIfSaved = false,
                                AddAdditionalDamage = false,
                                AddFavoredEnemyDamage = false
                            }
                        }
                    }
                },
                new AddStatBonus
                {
                    name = "$AddStatBonus$MolecularDissolutionAC",
                    Stat = StatType.AC,
                    Value = -6,
                    Descriptor = ModifierDescriptor.Penalty
                },
                new BuffMovementSpeed
                {
                    name = "$BuffMovementSpeed$MolecularDissolution",
                    Descriptor = ModifierDescriptor.Penalty,
                    Value = 0,
                    CappedOnMultiplier = true,
                    MultiplierCap = 0.5f
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$MolecularDissolution",
                    Descriptor = SpellDescriptor.Acid | SpellDescriptor.Evil
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
