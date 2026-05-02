using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class MasterOfDeathArcanaClassSpells :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClass[] Classes;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var context = evt.Reason.Context;
            if (context?.SourceAbility == null || !context.SourceAbility.IsSpell)
            {
                return;
            }

            if (context.SourceAbility.School != SpellSchool.Necromancy)
            {
                return;
            }

            var spellbook = context.SourceAbilityContext?.Ability?.Spellbook;
            if (spellbook == null)
            {
                return;
            }

            foreach (var characterClass in Classes ?? new BlueprintCharacterClass[0])
            {
                if (Owner.GetSpellbook(characterClass) != GetClassSpellbook(spellbook))
                {
                    continue;
                }

                foreach (var baseDamage in evt.DamageBundle)
                {
                    baseDamage.AddModifier(baseDamage.Dice.ModifiedValue.Rolls, Fact);
                }

                return;
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        private Spellbook GetClassSpellbook(Spellbook spellbook)
        {
            var memorizedSource = spellbook?.Blueprint.GetComponent<GetKnownSpellsFromMemorizationSpellbook>()?.Spellbook;
            return memorizedSource != null ? Owner.GetSpellbook(memorizedSource) : spellbook;
        }
    }

    public sealed class GetKnownSpellsFromMemorizationSpellbook : BlueprintComponent
    {
        public BlueprintSpellbook Spellbook;
    }
}
