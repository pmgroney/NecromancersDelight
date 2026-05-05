using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityModManagerNet;

namespace wotr_mod.Features
{
    public sealed class ReapingEdgeComponent :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookHandler<RuleDealDamage>
    {
        public BlueprintCharacterClass CharacterClass;
        public BlueprintBuff BrittleBoneBuff;
        public BlueprintBuff FatigueBuff;
        public BlueprintBuff ExhaustionBuff;
        private bool _used;
        private RuleCalculateDamage _consumedRule;
        public static UnityModManager.ModEntry.ModLogger Logger;
        
        protected override void OnTurnOn()
        {
            _used = false;
            _consumedRule = null;
        }
        
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (_used)
            {
                Logger?.Warning("!!!! ReapingEdge damage ignored: already used");
                return;
            }

            var attack = evt.ParentRule?.AttackRoll?.RuleAttackWithWeapon;
            if (attack == null || attack.Weapon == null || !IsMeleeWeapon(attack.Weapon))
            {
                Logger?.Warning("!!!! ReapingEdge damage ignored: invalid attack/weapon");
                return;
            }

            var weapon = attack.Weapon;

            foreach (var damage in evt.DamageBundle)
            {
                var physical = damage as PhysicalDamage;
                if (physical == null)
                {
                    continue;
                }

                physical.Enchantment = Math.Max(physical.Enchantment, 1);
                physical.EnchantmentTotal = Math.Max(physical.EnchantmentTotal, 1);

                if (GetClassLevel() >= 10)
                {
                    physical.AlignmentsMask |= DamageAlignment.Evil;
                }
            }

            var bonusDice = GetBonusDice(weapon);
            var extraDamage = new EnergyDamage(
                new DiceFormula(bonusDice, DiceType.D6),
                DamageEnergyType.NegativeEnergy);

            if (evt.DamageBundle is DamageBundle bundle)
            {
                bundle.Add(extraDamage);
            }
            else
            {
                evt.ParentRule?.Add(extraDamage);
            }

            _used = true;
            _consumedRule = evt;
        }
        
        
        
        private static bool IsMeleeWeapon(ItemEntityWeapon weapon)
        {
            return weapon.Blueprint.IsMelee;
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            if (_consumedRule != evt)
            {
                return;
            }

            if (Fact is Buff buffFact)
            {
                EventBus.RaiseEvent<IUnitBuffHandler>(
                    h => h.HandleBuffDidRemoved(buffFact));
            }

            if (Fact is IFactContextOwner fact)
            {
                fact.RunActionInContext(
                    new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new ContextActionRemoveSelf
                            {
                                name = "$ContextActionRemoveSelf$GravebladeReapingEdge"
                            }
                        }
                    },
                    Owner);
            }
            else
            {
                Logger?.Warning("!!!! ReapingEdge fact was not IFactContextOwner; could not remove self");
            }

            _consumedRule = null;
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Weapon == null || !IsMeleeWeapon(evt.Weapon))
            {
                return;
            }

            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit)
            {
                return;
            }

            ApplyHitEffects(evt);
        }

        private void ApplyHitEffects(RuleAttackWithWeapon evt)
        {
            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit || evt.Target == null)
            {
                return;
            }

            var level = GetClassLevel();
            if (level >= 5)
            {
                ApplyTimedBuff(evt.Target, BrittleBoneBuff, 6f);
            }

            if (level < 15 || !evt.AttackRoll.IsCriticalConfirmed)
            {
                return;
            }

            var rotBuff = evt.Target.State.HasCondition(UnitCondition.Fatigued)
                ? ExhaustionBuff
                : FatigueBuff;
            ApplyTimedBuff(evt.Target, rotBuff, 60f);
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt == null)
            {
                return;
            }
        }

        private int GetBonusDice(ItemEntityWeapon weapon)
        {
            var level = GetScalingLevel();
            var dice = level <= 0 ? 0 : (level + 1) / 2;
            return IsScythe(weapon) ? dice * 2 : dice;
        }

        private int GetScalingLevel()
        {
            return GetClassLevel() + Owner.Progression.MythicLevel * 2;
        }

        private int GetClassLevel()
        {
            return CharacterClass == null
                ? Owner.Progression.CharacterLevel
                : Owner.Progression.GetClassLevel(CharacterClass);
        }

        private static bool IsScythe(ItemEntityWeapon weapon)
        {
            return weapon.Blueprint.Category == WeaponCategory.Scythe;
        }

        private static void ApplyTimedBuff(UnitEntityData target, BlueprintBuff buff, float seconds)
        {
            if (buff != null)
            {
                target.AddBuffDuration(buff, seconds);
            }
        }
    }
}
