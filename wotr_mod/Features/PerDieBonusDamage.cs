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

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var context = evt.Reason.Context;
            var sourceAbility = context?.SourceAbility;
            if (sourceAbility == null || !IsEligibleSource(sourceAbility, context.SourceAbilityContext?.Ability?.Spellbook))
            {
                return;
            }

            var rank = Fact?.GetRank() ?? 0;
            if (rank <= 0)
            {
                return;
            }

            foreach (var baseDamage in evt.DamageBundle)
            {
                if (!MatchesDamage(baseDamage))
                {
                    continue;
                }

                baseDamage.AddModifier(baseDamage.Dice.ModifiedValue.Rolls * rank, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        private bool IsEligibleSource(BlueprintAbility sourceAbility, Spellbook spellbook)
        {
            if (IsAdditionalAbility(sourceAbility))
            {
                return true;
            }

            if (!IncludeClassSpellbookSpells ||
                !sourceAbility.IsSpell ||
                sourceAbility.School != SpellSchool.Evocation ||
                spellbook == null)
            {
                return false;
            }

            return IsClassSpellbook(spellbook);
        }

        private bool IsClassSpellbook(Spellbook spellbook)
        {
            foreach (var characterClass in Classes ?? new BlueprintCharacterClass[0])
            {
                if (Owner.GetSpellbook(characterClass) == GetClassSpellbook(spellbook))
                {
                    return true;
                }
            }

            return false;
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

        private Spellbook GetClassSpellbook(Spellbook spellbook)
        {
            var memorizedSource = spellbook?.Blueprint.GetComponent<GetKnownSpellsFromMemorizationSpellbook>()?.Spellbook;
            return memorizedSource != null ? Owner.GetSpellbook(memorizedSource) : spellbook;
        }
    }
}
