using System;
using System.Linq;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class EvokerForceSpellConversion :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var sourceAbility = evt.Reason.Context?.SourceAbility;
            if (sourceAbility == null || !sourceAbility.IsSpell)
            {
                return;
            }

            var energyDamages = evt.DamageBundle
                .OfType<EnergyDamage>()
                .ToArray();
            foreach (var energyDamage in energyDamages)
            {
                var forceDamage = new ForceDamage(energyDamage.Dice, energyDamage.Bonus)
                {
                    Reality = energyDamage.Reality,
                    Precision = energyDamage.Precision,
                    CriticalModifier = energyDamage.CriticalModifier,
                    AdditionalCriticalMultiplier = energyDamage.AdditionalCriticalMultiplier,
                    CriticalApplied = energyDamage.CriticalApplied,
                    TacticalCriticalModifier = energyDamage.TacticalCriticalModifier,
                    Sneak = energyDamage.Sneak,
                    Half = energyDamage.Half,
                    IgnoreImmunities = energyDamage.IgnoreImmunities,
                    PreRolledValue = energyDamage.PreRolledValue,
                    DamageIncreaseReason = energyDamage.DamageIncreaseReason,
                    IgnoreReduction = energyDamage.IgnoreReduction,
                    IgnoreModifiers = energyDamage.IgnoreModifiers,
                    AlignmentsMask = energyDamage.AlignmentsMask,
                    CausedByCheckFail = energyDamage.CausedByCheckFail,
                    AlreadyHalved = energyDamage.AlreadyHalved,
                    SourceFact = energyDamage.SourceFact,
                    Durability = energyDamage.Durability,
                    Vulnerability = energyDamage.Vulnerability,
                    BonusPercent = energyDamage.BonusPercent
                };

                evt.Remove(damage => ReferenceEquals(damage, energyDamage));
                evt.AddUnsafe(forceDamage);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }
    }
}
