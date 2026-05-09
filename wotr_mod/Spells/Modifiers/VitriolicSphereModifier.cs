using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class VitriolicSphereModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Evocation, context.Blueprints);
            spell.Range = AbilityRange.Custom;
            spell.CustomRange = 90.Feet();

            var rank = context.Blueprints.EnsureComponent(
                spell,
                () => new ContextRankConfig { name = "$ContextRankConfig$VitriolicSphere" });
            context.Blueprints.ConfigureContextRankConfig(rank);
            context.Blueprints.SetContextRankMaximum(rank, 15);

            foreach (var targetAround in context.Blueprints.GetComponents<AbilityTargetsAround>(spell))
            {
                SpellModifierUtility.SetPrivateField(targetAround, "m_Radius", 30.Feet());
            }

            ConfigureDamageAndRider(context, spell, EnsureNauseatedBuff(context));
        }

        private static void ConfigureDamageAndRider(
            SpellModifierContext context,
            BlueprintAbility spell,
            BlueprintBuff nauseatedBuff)
        {
            var runAction = spell.GetComponent<AbilityEffectRunAction>();
            var actions = runAction?.Actions?.Actions;
            if (actions == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no run action found.");
                return;
            }

            var changed = false;
            var patched = actions.Select(action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage == null || damage.DamageType.Type != DamageType.Energy)
                {
                    return action;
                }

                changed = true;
                return CreateDamageAction(damage);
            }).ToArray();

            if (!patched.Any(action => action?.name == "$ContextActionConditionalSaved$VitriolicSphereNausea"))
            {
                var damageIndex = Array.FindIndex(patched, action => action is VitriolicSphereDamageAction);
                var insertIndex = damageIndex < 0 ? patched.Length : damageIndex + 1;
                patched = patched
                    .Take(insertIndex)
                    .Concat(new GameAction[] { FailedSaveNausea(context, nauseatedBuff) })
                    .Concat(patched.Skip(insertIndex))
                    .ToArray();
                changed = true;
            }

            if (changed)
            {
                runAction.Actions.Actions = patched;
            }
        }

        private static VitriolicSphereDamageAction CreateDamageAction(ContextActionDealDamage source)
        {
            return new VitriolicSphereDamageAction
            {
                name = "$VitriolicSphereDamageAction$VitriolicSphere",
                DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Acid),
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
                SetFactAsReason = source.SetFactAsReason,
                ResistanceBypass = 10
            };
        }

        private static ContextActionConditionalSaved FailedSaveNausea(
            SpellModifierContext context,
            BlueprintBuff nauseatedBuff)
        {
            var applyBuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$VitriolicSphereNausea",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = Rounds(1),
                IsFromSpell = true,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            context.Blueprints.SetApplyBuffActionBuff(applyBuff, nauseatedBuff);

            return new ContextActionConditionalSaved
            {
                name = "$ContextActionConditionalSaved$VitriolicSphereNausea",
                Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                Failed = new ActionList { Actions = new GameAction[] { applyBuff } }
            };
        }

        private static BlueprintBuff EnsureNauseatedBuff(SpellModifierContext context)
        {
            var buff = context.Blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.VitriolicSphereNauseated);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_VitriolicSphereNauseatedBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.VitriolicSphereNauseated)
                };
                context.Blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.VitriolicSphereNauseated, buff);
            }

            buff.Stacking = StackingType.Replace;
            context.Blueprints.CopyUnitFactDisplay(buff, context.Ability);
            context.Blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$VitriolicSphereNauseated",
                    Condition = UnitCondition.Nauseated
                },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$VitriolicSphereNauseated",
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

    public sealed class VitriolicSphereDamageAction : ContextActionDealDamage
    {
        public int ResistanceBypass = 10;

        public override string GetCaption()
        {
            return "Vitriolic Sphere acid damage";
        }

        public override void RunAction()
        {
            var target = Target.Unit;
            var damageReduction = target?.Get<UnitPartDamageReduction>();
            if (damageReduction == null || ResistanceBypass <= 0)
            {
                base.RunAction();
                return;
            }

            damageReduction.AddPenaltyEntry(ResistanceBypass, null);
            try
            {
                base.RunAction();
            }
            finally
            {
                damageReduction.RemovePenaltyEntry(null);
            }
        }
    }
}
