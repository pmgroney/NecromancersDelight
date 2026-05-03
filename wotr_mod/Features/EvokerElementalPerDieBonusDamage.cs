using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
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

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var context = evt.Reason.Context;
            if (context?.SourceAbility == null || !context.SourceAbility.IsSpell)
            {
                return;
            }

            if (context.SourceAbility.School != SpellSchool.Evocation)
            {
                return;
            }

            var spellbook = context.SourceAbilityContext?.Ability?.Spellbook;
            if (spellbook == null || !IsClassSpellbook(spellbook))
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

        private Spellbook GetClassSpellbook(Spellbook spellbook)
        {
            var memorizedSource = spellbook?.Blueprint.GetComponent<GetKnownSpellsFromMemorizationSpellbook>()?.Spellbook;
            return memorizedSource != null ? Owner.GetSpellbook(memorizedSource) : spellbook;
        }
    }
}
