using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class LivingDarknessNegativeEnergySavePenalty :
        UnitFactComponentDelegate,
        ITargetRulebookHandler<RuleSavingThrow>,
        IRulebookHandler<RuleSavingThrow>,
        ITargetRulebookSubscriber
    {
        public int Penalty;

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var ability = evt?.Reason?.Context?.SourceAbility;
            if (ability == null)
            {
                return;
            }

            if (ability.School != SpellSchool.Necromancy &&
                !ability.SpellDescriptor.HasFlag(SpellDescriptor.Death))
            {
                return;
            }

            evt.AddModifier(Penalty, Fact, ModifierDescriptor.Penalty);
        }

        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
        }
    }

    // Adds flat bonus damage to negative energy damage as an approximation of resistance penetration.
    // The flat bonus effectively overcomes resistance up to the penetration value in most cases.
    public sealed class LivingDarknessResistancePenetration :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public int Penetration;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            foreach (var baseDamage in evt.DamageBundle)
            {
                var energy = baseDamage as EnergyDamage;
                if (energy == null || energy.EnergyType != DamageEnergyType.NegativeEnergy)
                {
                    continue;
                }

                baseDamage.AddModifier(Penetration, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }
    }
}
