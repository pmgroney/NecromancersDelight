using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class EvocationUnleashedDamageBonus :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var sourceAbility = evt.Reason.Context?.SourceAbility;
            if (sourceAbility == null ||
                !sourceAbility.IsSpell ||
                sourceAbility.School != SpellSchool.Evocation)
            {
                return;
            }

            var casterUnit = Owner;
            if (casterUnit != null && casterUnit != evt.Target && !casterUnit.IsEnemy(evt.Target))
            {
                evt.Remove(_ => true);
                return;
            }

            var charismaBonus = Owner?.Stats?.Charisma?.Bonus ?? 0;
            if (charismaBonus <= 0)
            {
                return;
            }

            foreach (var baseDamage in evt.DamageBundle)
            {
                baseDamage.AddModifier(charismaBonus, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }
    }
}
