using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.PubSubSystem;
using UnityModManagerNet;

namespace wotr_mod.Features
{
    public abstract class OneTimeMeleeDamageBonusComponent : 
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>
    {
        public BlueprintCharacterClass CharacterClass;
        protected bool Used;
        protected RuleCalculateDamage ConsumedRule;
        public static UnityModManager.ModEntry.ModLogger BaseLogger;

        protected override void OnTurnOn()
        {
            Used = false;
            ConsumedRule = null;
        }

        public virtual void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (Used)
            {
                return;
            }

            var attack = evt.ParentRule?.AttackRoll?.RuleAttackWithWeapon;
            if (attack == null || attack.Weapon == null || !IsMeleeWeapon(attack.Weapon))
            {
                return;
            }

            ApplyDamageBonus(evt, attack.Weapon);
            
            Used = true;
            ConsumedRule = evt;
        }

        public virtual void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            if (ConsumedRule != evt)
            {
                return;
            }

            RemoveFact();
            ConsumedRule = null;
        }

        protected abstract void ApplyDamageBonus(RuleCalculateDamage evt, ItemEntityWeapon weapon);

        protected virtual void RemoveFact()
        {
            if (Fact is Buff buffFact)
            {
                EventBus.RaiseEvent<IUnitBuffHandler>(h => h.HandleBuffDidRemoved(buffFact));
            }

            if (Fact is IFactContextOwner fact)
            {
                fact.RunActionInContext(
                    new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new ContextActionRemoveSelf { name = "$OneTimeMeleeDamageBonus$RemoveSelf" }
                        }
                    },
                    Owner);
            }
        }

        protected static bool IsMeleeWeapon(ItemEntityWeapon weapon)
        {
            return weapon.Blueprint.IsMelee;
        }

        protected int GetClassLevel()
        {
            return CharacterClass == null
                ? Owner.Progression.CharacterLevel
                : Owner.Progression.GetClassLevel(CharacterClass);
        }
    }
}
