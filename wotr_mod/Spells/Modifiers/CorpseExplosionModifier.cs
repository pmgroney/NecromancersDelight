using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class CorpseExplosionModifier : ISpellModifier
    {
        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, context.Blueprints);
            SpellModifierUtility.ReplaceDescriptor(spell, SpellDescriptor.Fire, SpellDescriptor.Death, context.Blueprints);

            spell.CanTargetPoint = false;
            spell.CanTargetEnemies = true;
            spell.CanTargetFriends = true;
            spell.CanTargetSelf = false;
            spell.Range = AbilityRange.Medium;

            context.Blueprints.EnsureComponent(spell, () => new AbilityCanTargetDead());
            context.Blueprints.EnsureComponent(spell, () => new AbilityTargetMustBeDead());

            var targetsAround = context.Blueprints.GetComponents<AbilityTargetsAround>(spell);
            foreach (var targetAround in targetsAround)
            {
                SpellModifierUtility.SetPrivateField(targetAround, "m_Radius", 15.Feet());
                SpellModifierUtility.SetPrivateField(targetAround, "m_IncludeDead", false);
            }

            SpellModifierUtility.PatchRunActions(spell, action =>
            {
                var damage = action as Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage;
                if (damage == null ||
                    damage.DamageType.Type != Kingmaker.RuleSystem.Rules.Damage.DamageType.Energy ||
                    damage.DamageType.Energy != DamageEnergyType.Fire)
                {
                    return 0;
                }

                damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Unholy);
                if (damage.Value != null)
                {
                    damage.Value = SpellModifierUtility.CopyDiceValue(damage.Value, DiceType.D8);
                }

                return 1;
            });
        }
    }
}
