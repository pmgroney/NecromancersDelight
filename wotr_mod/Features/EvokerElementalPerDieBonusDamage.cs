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
    public sealed class EvokerElementalPerDieBonusDamage :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClass[] Classes;
        public DamageEnergyType EnergyType;
        public bool CountAnyEnergyDamageWhileConversionBuffActive;
        public BlueprintBuff ConversionBuff;
        public BlueprintAbility[] AdditionalAbilities;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var context = evt.Reason.Context;
            var sourceAbility = context?.SourceAbility;
            if (sourceAbility == null)
            {
                return;
            }

            if (!IsAdditionalAbility(sourceAbility))
            {
                if (!sourceAbility.IsSpell)
                {
                    return;
                }

                if (sourceAbility.School != SpellSchool.Evocation)
                {
                    return;
                }

                var spellbook = context.SourceAbilityContext?.Ability?.Spellbook;
                if (spellbook == null || !IsClassSpellbook(spellbook))
                {
                    return;
                }
            }

            var rank = Fact?.GetRank() ?? 0;
            if (rank <= 0)
            {
                return;
            }

            foreach (var baseDamage in evt.DamageBundle)
            {
                var energy = baseDamage as EnergyDamage;
                if (energy == null || !MatchesDamage(energy))
                {
                    continue;
                }

                baseDamage.AddModifier(baseDamage.Dice.ModifiedValue.Rolls * rank, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
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

        private bool MatchesDamage(EnergyDamage damage)
        {
            if (damage.EnergyType == EnergyType)
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
