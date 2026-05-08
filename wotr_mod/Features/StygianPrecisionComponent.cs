using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class StygianPrecisionComponent :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
        IRulebookHandler<RuleCalculateWeaponStats>,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        IRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookHandler<RuleAttackRoll>,
        IRulebookHandler<RuleAttackRoll>,
        IInitiatorRulebookSubscriber
    {
        public int CriticalEdgeBonus;
        public bool AutoConfirmCriticalHits;

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt == null || !IsScythe(evt.Weapon))
            {
                return;
            }

            var bonus = CriticalEdgeBonus > 0
                ? CriticalEdgeBonus
                : (Fact?.GetRank() ?? 0);
            if (bonus > 0)
            {
                evt.CriticalEdgeBonus += bonus;
            }
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (AutoConfirmCriticalHits && evt != null && IsScythe(evt.Weapon))
            {
                evt.AutoCriticalConfirmation = true;
            }
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (AutoConfirmCriticalHits && evt != null && IsScythe(evt.Weapon))
            {
                evt.AutoCriticalConfirmation = true;
            }
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
        }

        private static bool IsScythe(ItemEntityWeapon weapon)
        {
            return weapon != null &&
                   weapon.Blueprint != null &&
                   weapon.Blueprint.Category == WeaponCategory.Scythe;
        }
    }
}
