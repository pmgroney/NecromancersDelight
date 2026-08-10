using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class DeathlyFinesseAttackStatReplacement :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
        IRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,
        IInitiatorRulebookSubscriber
    {
        public StatType ReplacementStat = StatType.Charisma;
        public ModifierDescriptor Descriptor = ModifierDescriptor.UntypedStackable;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt == null || !IsProficient(evt.Initiator, evt.Weapon))
            {
                return;
            }

            var currentBonus = GetStatBonus(evt.Initiator, evt.AttackBonusStat);
            var replacementBonus = GetStatBonus(evt.Initiator, ReplacementStat);
            var bonus = replacementBonus - currentBonus;
            if (bonus > 0)
            {
                evt.AddModifier(bonus, Fact, Descriptor);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
        }

        private static bool IsProficient(UnitEntityData unit, ItemEntityWeapon weapon)
        {
            var category = weapon?.Blueprint?.Category;
            return category.HasValue &&
                   unit != null &&
                   unit.Proficiencies.Contains(category.Value);
        }

        private static int GetStatBonus(UnitEntityData unit, StatType stat)
        {
            return (unit?.Stats?.GetStat(stat) as ModifiableValueAttributeStat)?.Bonus ?? 0;
        }
    }
}
