using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace wotr_mod.Features
{
    public sealed class PerDieBonusDamage :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClass[] Classes;
        public bool IncludeClassSpellbookSpells;
        public bool MatchEnergyDamage;
        public DamageEnergyType EnergyType;
        public bool MatchForceDamage;
        public bool CountAnyEnergyDamageWhileConversionBuffActive;
        public BlueprintBuff ConversionBuff;
        public BlueprintAbility[] AdditionalAbilities;
        public int CapstoneRank;
        public int CapstoneBonusDamagePerDie;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var context = evt.Reason.Context;
            var sourceAbility = context?.SourceAbility;
            if (sourceAbility == null || !IsEligibleSource(sourceAbility))
            {
                return;
            }

            var rank = Fact?.GetRank() ?? 0;
            if (rank <= 0)
            {
                return;
            }

            var bonusPerDie = rank;
            if (CapstoneRank > 0 && rank >= CapstoneRank)
            {
                bonusPerDie += CapstoneBonusDamagePerDie;
            }

            foreach (var baseDamage in evt.DamageBundle)
            {
                if (!MatchesDamage(baseDamage))
                {
                    continue;
                }

                baseDamage.AddModifier(baseDamage.Dice.ModifiedValue.Rolls * bonusPerDie, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        private bool IsEligibleSource(BlueprintAbility sourceAbility)
        {
            if (IsAdditionalAbility(sourceAbility))
            {
                return true;
            }

            if (!IncludeClassSpellbookSpells ||
                !sourceAbility.IsSpell ||
                sourceAbility.School != SpellSchool.Evocation)
            {
                return false;
            }

            return true;
        }

        private bool MatchesDamage(BaseDamage damage)
        {
            var energy = damage as EnergyDamage;
            if (energy != null)
            {
                return MatchesEnergyDamage(energy);
            }

            return MatchForceDamage && damage is ForceDamage;
        }

        private bool MatchesEnergyDamage(EnergyDamage damage)
        {
            if (MatchEnergyDamage && damage.EnergyType == EnergyType)
            {
                return true;
            }

            return CountAnyEnergyDamageWhileConversionBuffActive &&
                   ConversionBuff != null &&
                   Owner.HasFact(ConversionBuff);
        }

        private bool IsAdditionalAbility(BlueprintAbility ability)
        {
            foreach (var additionalAbility in AdditionalAbilities ?? new BlueprintAbility[0])
            {
                if (additionalAbility != null && additionalAbility.AssetGuid == ability.AssetGuid)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
