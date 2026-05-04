using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Features
{
    public sealed class ReapingEdgeComponent :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        IRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookHandler<RuleDealDamage>,
        IRulebookHandler<RuleDealDamage>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintCharacterClass CharacterClass;
        public BlueprintBuff BrittleBoneBuff;
        public BlueprintBuff FatigueBuff;
        public BlueprintBuff ExhaustionBuff;
        private bool _used;
        public static UnityModManager.ModEntry.ModLogger Logger;
        
        
        
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (_used)
            {
                return;
            }
            
            var attack = evt.ParentRule?.AttackRoll?.RuleAttackWithWeapon;
            if (attack == null || attack.Weapon == null || !IsMeleeWeapon(attack.Weapon))
            {
                return;
            }

            var weapon = attack.Weapon;
            if (weapon == null)
            {
                return;
            }

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
                _used = true;
                return;
            }

            evt.ParentRule?.Add(extraDamage);
            _used = true;
        }
        
        private static bool IsMeleeWeapon(ItemEntityWeapon weapon)
        {
            return weapon.Blueprint.IsMelee;
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
 
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
            Logger.Warning("!!!ReapingEdge RuleDealDamage fired");

            if (evt == null)
            {
                Logger.Warning("[ReapingEdge] RuleDealDamage evt is null");
                return;
            }

            var weapon = GetWeapon(evt);
            if (weapon == null)
            {
                Logger.Warning("[ReapingEdge] Weapon is null");
                return;
            }

            if (weapon.Blueprint == null)
            {
                Logger.Warning("[ReapingEdge] Weapon blueprint is null");
                return;
            }

            if (!IsMeleeWeapon(weapon))
            {
                Logger.Warning("[ReapingEdge] Weapon is not melee");
                return;
            }

            if (evt.AttackRoll == null)
            {
                Logger.Warning("[ReapingEdge] AttackRoll is null");
                return;
            }

            if (!evt.AttackRoll.IsHit)
            {
                Logger.Warning("[ReapingEdge] Attack was not a hit");
                return;
            }

            Logger.Warning("[ReapingEdge] Valid melee damage hit, spending");
            // level 20 explosion logic can go below this later
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

        private int RollWeaponBaseDamage(ItemEntityWeapon weapon)
        {
            return Rulebook.Trigger(new RuleRollDice(Owner, weapon.DamageDice)).Result;
        }

        private static ItemEntityWeapon GetWeapon(RuleDealDamage evt)
        {
            return evt?.AttackRoll?.RuleAttackWithWeapon?.Weapon;
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
