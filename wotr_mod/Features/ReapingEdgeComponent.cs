using System;
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
using UnityModManagerNet;

namespace wotr_mod.Features
{
    public sealed class ReapingEdgeComponent :
        OneTimeMeleeDamageBonusComponent,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookHandler<RuleDealDamage>
    {
        public BlueprintBuff BrittleBoneBuff;
        public BlueprintBuff FatigueBuff;
        public BlueprintBuff ExhaustionBuff;
        public static UnityModManager.ModEntry.ModLogger Logger;
        
        public override void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (Used)
            {
                Logger?.Warning("!!!! ReapingEdge damage ignored: already used");
                return;
            }

            var attack = evt.ParentRule?.AttackRoll?.RuleAttackWithWeapon;
            if (attack?.Weapon == null || !IsMeleeWeapon(attack.Weapon))
            {
                Logger?.Warning("!!!! ReapingEdge damage ignored: invalid attack/weapon");
                return;
            }

            base.OnEventAboutToTrigger(evt);
        }

        protected override void ApplyDamageBonus(RuleCalculateDamage evt, ItemEntityWeapon weapon)
        {
            foreach (var damage in evt.DamageBundle)
            {
                if (!(damage is PhysicalDamage physical))
                {
                    continue;
                }

                physical.Enchantment = Math.Max(physical.Enchantment, 1);
                physical.EnchantmentTotal = Math.Max(physical.EnchantmentTotal, 1);

                if (GetClassLevel() >= 10)
                {
                    physical.IgnoreReduction = true;
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
        }
        
        public override void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            if (ConsumedRule != evt)
            {
                return;
            }

            base.OnEventDidTrigger(evt);
            ConsumedRule = null;
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
            if (evt.AttackRoll == null || !evt.AttackRoll.IsHit)
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
            var dice = Math.Max((GetClassLevel() + 1) / 2, 0);
            return IsScythe(weapon) ? dice * 2 : dice;
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
