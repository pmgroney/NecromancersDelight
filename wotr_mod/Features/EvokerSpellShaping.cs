using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class EvokerSpellShaping :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClass[] Classes;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (evt.Target == null || Owner == null || !Owner.IsAlly(evt.Target))
            {
                return;
            }

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

            evt.Remove(_ => true);
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

        private Spellbook GetClassSpellbook(Spellbook spellbook)
        {
            var memorizedSource = spellbook?.Blueprint.GetComponent<GetKnownSpellsFromMemorizationSpellbook>()?.Spellbook;
            return memorizedSource != null ? Owner.GetSpellbook(memorizedSource) : spellbook;
        }
    }
}
