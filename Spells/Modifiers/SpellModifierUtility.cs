using System;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal static class SpellModifierUtility
    {
        public static void SetSchool(BlueprintAbility spell, SpellSchool school, BlueprintTool blueprints)
        {
            var oldComponent = spell?.GetComponent<SpellComponent>();
            if (oldComponent != null)
            {
                var newComponent = blueprints.CloneComponent(oldComponent);
                newComponent.School = school;
                blueprints.ReplaceComponent(spell, oldComponent, newComponent);
            }
        }

        public static void ReplaceDescriptor(BlueprintAbility spell, SpellDescriptor remove, SpellDescriptor add, BlueprintTool blueprints)
        {
            var oldComponent = spell?.GetComponent<SpellDescriptorComponent>();
            if (oldComponent == null)
            {
                return;
            }

            var newComponent = new SpellDescriptorComponent
            {
                Descriptor = oldComponent.Descriptor
            };
            newComponent.Descriptor &= ~remove;
            newComponent.Descriptor |= add;
            blueprints.ReplaceComponent(spell, oldComponent, newComponent);
        }

        public static int PatchRunActions(BlueprintAbility spell, Func<GameAction, int> patch)
        {
            var runAction = spell?.GetComponent<AbilityEffectRunAction>();
            return ActionListPatcher.Patch(runAction?.Actions, patch);
        }

        public static DamageTypeDescription EnergyDamage(DamageEnergyType energy)
        {
            return new DamageTypeDescription
            {
                Type = DamageType.Energy,
                Energy = energy
            };
        }

        public static DamageTypeDescription ForceDamage()
        {
            return new DamageTypeDescription
            {
                Type = DamageType.Force
            };
        }

        public static ContextDiceValue CopyDiceValue(ContextDiceValue source, DiceType diceType)
        {
            if (source == null)
            {
                return null;
            }

            return new ContextDiceValue
            {
                DiceType = diceType,
                DiceCountValue = source.DiceCountValue,
                BonusValue = source.BonusValue
            };
        }

        public static void SetPrivateField(object instance, string name, object value)
        {
            var field = FindField(instance.GetType(), name);
            field?.SetValue(instance, value);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(name, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }
    }
}
