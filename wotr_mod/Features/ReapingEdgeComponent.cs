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

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var weapon = GetWeapon(evt.ParentRule);
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
            if (bonusDice <= 0)
            {
                return;
            }

            var extraDamage = new EnergyDamage(
                new DiceFormula(bonusDice, DiceType.D6),
                DamageEnergyType.NegativeEnergy);
            var bundle = evt.DamageBundle as DamageBundle;
            if (bundle != null)
            {
                bundle.Add(extraDamage);
                return;
            }

            evt.ParentRule?.Add(extraDamage);
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
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
            if (GetClassLevel() < 20 || evt.Target == null || !evt.Target.State.IsDead)
            {
                return;
            }

            var weapon = GetWeapon(evt);
            if (weapon == null)
            {
                return;
            }

            var burstDamage = RollWeaponBaseDamage(weapon);
            if (burstDamage <= 0)
            {
                return;
            }

            foreach (var target in GameHelper.GetTargetsAround(evt.Target.Position, new Feet(5), true, false))
            {
                if (target == null || target == evt.Target || target.State.IsDead || !Owner.IsEnemy(target))
                {
                    continue;
                }

                GameHelper.DealDirectDamage(Owner, target, burstDamage);
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
