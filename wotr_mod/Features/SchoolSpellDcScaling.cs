using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class SchoolSpellDcScaling :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
        IRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClass[] Classes;
        public SpellSchool School;

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            var spell = evt?.Spell;
            if (spell == null || !spell.IsSpell || spell.School != School)
            {
                return;
            }

            var spellbook = evt.Spellbook;
            if (spellbook == null || !IsClassSpellbook(spellbook))
            {
                return;
            }

            var bonus = (Fact?.GetRank() ?? 0) * 2;
            if (bonus > 0)
            {
                evt.AddBonusDC(bonus);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt)
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

        private Spellbook GetClassSpellbook(Spellbook spellbook)
        {
            var memorizedSource = spellbook?.Blueprint.GetComponent<GetKnownSpellsFromMemorizationSpellbook>()?.Spellbook;
            return memorizedSource != null ? Owner.GetSpellbook(memorizedSource) : spellbook;
        }
    }
}
