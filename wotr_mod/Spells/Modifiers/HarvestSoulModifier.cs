using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class HarvestSoulModifier : ISpellModifier
    {
        private static readonly IReadOnlyList<VariantDefinition> Variants = new[]
        {
            new VariantDefinition(1, ModBlueprintIds.Spells.HarvestSoulRestoreLevel1),
            new VariantDefinition(2, ModBlueprintIds.Spells.HarvestSoulRestoreLevel2),
            new VariantDefinition(3, ModBlueprintIds.Spells.HarvestSoulRestoreLevel3),
            new VariantDefinition(4, ModBlueprintIds.Spells.HarvestSoulRestoreLevel4),
            new VariantDefinition(5, ModBlueprintIds.Spells.HarvestSoulRestoreLevel5),
            new VariantDefinition(6, ModBlueprintIds.Spells.HarvestSoulRestoreLevel6),
            new VariantDefinition(7, ModBlueprintIds.Spells.HarvestSoulRestoreLevel7)
        };

        public void Apply(SpellModifierContext context)
        {
            var variants = Variants
                .Select(variant => EnsureVariant(context, variant))
                .ToArray();

            ConfigureRoot(context, variants);
        }

        private static void ConfigureRoot(SpellModifierContext context, BlueprintAbility[] variants)
        {
            var abilityVariants = new AbilityVariants { name = "$AbilityVariants$HarvestSoul" };
            context.Blueprints.SetAbilityVariants(abilityVariants, variants);

            context.Blueprints.SetComponents(
                context.Ability,
                new SpellComponent { name = "$SpellComponent$HarvestSoul", School = SpellSchool.Necromancy },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$HarvestSoul",
                    Descriptor = SpellDescriptor.Death
                },
                abilityVariants);
        }

        private static BlueprintAbility EnsureVariant(
            SpellModifierContext context,
            VariantDefinition definition)
        {
            var variant = context.Blueprints.Get<BlueprintAbility>(definition.Guid);
            var created = false;
            if (variant == null)
            {
                variant = context.Blueprints.CloneBlueprint(
                    context.Ability,
                    definition.Guid,
                    $"WotrMod_HarvestSoul_RestoreLevel{definition.RestoreLevel}");
                created = true;
            }

            ConfigureVariant(context, variant, definition.RestoreLevel);

            if (created)
            {
                variant.OnEnable();
                context.Blueprints.AddCachedBlueprint(definition.Guid, variant);
            }

            return variant;
        }

        private static void ConfigureVariant(
            SpellModifierContext context,
            BlueprintAbility variant,
            int restoreLevel)
        {
            context.Blueprints.SetAbilityDisplay(
                variant,
                new LocalizedString { Key = $"wotr_mod.spell.harvest_soul.restore_level_{restoreLevel}.name" },
                new LocalizedString { Key = "wotr_mod.spell.harvest_soul.description" });

            if (context.Ability.Icon != null)
            {
                context.Blueprints.SetUnitFactIcon(variant, context.Ability.Icon);
            }

            SpellModifierUtility.SetSchool(variant, SpellSchool.Necromancy, context.Blueprints);
            variant.Range = AbilityRange.Close;
            variant.CanTargetPoint = false;
            variant.CanTargetEnemies = true;
            variant.CanTargetFriends = false;
            variant.CanTargetSelf = false;
            variant.SpellResistance = true;
            variant.NotOffensive = false;

            context.Blueprints.SetComponents(
                variant,
                new SpellComponent { name = "$SpellComponent$HarvestSoulLevel" + restoreLevel, School = SpellSchool.Necromancy },
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$HarvestSoulLevel" + restoreLevel,
                    Descriptor = SpellDescriptor.Death
                },
                new ContextRankConfig
                {
                    name = "$ContextRankConfig$HarvestSoulDamageLevel" + restoreLevel
                },
                new AbilityEffectRunAction
                {
                    name = "$AbilityEffectRunAction$HarvestSoulLevel" + restoreLevel,
                    SavingThrowType = SavingThrowType.Fortitude,
                    Actions = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new HarvestSoulAction
                            {
                                name = "$HarvestSoulAction$Level" + restoreLevel,
                                RestoreSpellLevel = restoreLevel,
                                MaxDamageDice = 25
                            }
                        }
                    }
                });

            var rank = context.Blueprints.GetComponents<ContextRankConfig>(variant).FirstOrDefault();
            if (rank != null)
            {
                context.Blueprints.ConfigureContextRankConfig(rank);
                context.Blueprints.SetContextRankMaximum(rank, 25);
            }
        }

        private readonly struct VariantDefinition
        {
            public VariantDefinition(int restoreLevel, string guid)
            {
                RestoreLevel = restoreLevel;
                Guid = guid;
            }

            public int RestoreLevel { get; }
            public string Guid { get; }
        }
    }

    public sealed class HarvestSoulAction : ContextAction
    {
        public int RestoreSpellLevel;
        public int MaxDamageDice;

        public override string GetCaption()
        {
            return "Harvest Soul";
        }

        public override void RunAction()
        {
            var caster = Context?.MaybeCaster;
            var target = Target.Unit;
            if (caster == null || target == null)
            {
                return;
            }

            var wasAlive = !IsDead(target);
            DealDamage(caster, target);

            if (wasAlive && IsDead(target))
            {
                RestoreSpellSlot(caster, RestoreSpellLevel);
            }
        }

        private void DealDamage(UnitEntityData caster, UnitEntityData target)
        {
            var casterLevel = Math.Max(1, Context.Params?.CasterLevel ?? 1);
            var dice = Math.Max(1, Math.Min(MaxDamageDice, casterLevel));
            var saved = Context?.SavingThrow?.IsPassed == true;
            var damage = new EnergyDamage(
                new DiceFormula(dice, DiceType.D6),
                DamageEnergyType.NegativeEnergy);

            var rule = new RuleDealDamage(caster, target, damage)
            {
                Reason = Context,
                Half = saved,
                HalfBecauseSavingThrow = saved,
                SourceAbility = Context.SourceAbility
            };
            Rulebook.Trigger(rule);
        }

        private bool RestoreSpellSlot(UnitEntityData caster, int spellLevel)
        {
            var spellbook = Context?.SourceAbilityContext?.Ability?.Spellbook;
            if (TryRestoreSpellSlot(spellbook, spellLevel))
            {
                return true;
            }

            foreach (var candidate in caster.Descriptor?.Spellbooks ?? Enumerable.Empty<Spellbook>())
            {
                if (candidate == spellbook)
                {
                    continue;
                }

                if (TryRestoreSpellSlot(candidate, spellLevel))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryRestoreSpellSlot(Spellbook spellbook, int spellLevel)
        {
            if (spellbook == null || spellLevel < 1 || spellLevel > spellbook.MaxSpellLevel)
            {
                return false;
            }

            if (spellbook.Blueprint?.Spontaneous == true)
            {
                var before = spellbook.GetSpontaneousSlots(spellLevel);
                spellbook.RestoreSpontaneousSlots(spellLevel, 1);
                return spellbook.GetSpontaneousSlots(spellLevel) > before;
            }

            var spentSlot = spellbook.GetMemorizedSpellSlots(spellLevel)
                .FirstOrDefault(slot => slot != null && slot.SpellShell != null && !slot.Available);
            if (spentSlot == null)
            {
                return false;
            }

            spentSlot.Available = true;
            return true;
        }

        private static bool IsDead(UnitEntityData unit)
        {
            return unit == null ||
                   unit.State.IsDead ||
                   unit.State.IsFinallyDead ||
                   unit.HPLeft <= 0;
        }
    }
}
