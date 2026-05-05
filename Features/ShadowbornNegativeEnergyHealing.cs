using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class ShadowbornNegativeEnergyHealing :
        UnitFactComponentDelegate,
        ITargetRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        ITargetRulebookSubscriber
    {
        public BlueprintFeature UndeadType;
        public BlueprintFeature[] ResistanceFeaturesToRemove;

        protected override void OnTurnOn()
        {
            RemoveResistanceFeatures();
        }

        protected override void OnActivate()
        {
            RemoveResistanceFeatures();
        }

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (UndeadType == null || !Owner.HasFact(UndeadType))
            {
                return;
            }

            foreach (var damage in evt.DamageBundle)
            {
                var energyDamage = damage as EnergyDamage;
                if (energyDamage == null || energyDamage.EnergyType != DamageEnergyType.NegativeEnergy)
                {
                    continue;
                }

                energyDamage.BonusPercent += 100;
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        private void RemoveResistanceFeatures()
        {
            foreach (var feature in ResistanceFeaturesToRemove ?? new BlueprintFeature[0])
            {
                if (feature != null && Owner.HasFact(feature))
                {
                    Owner.Progression.Features.RemoveFact(feature);
                }
            }
        }
    }
}
