using System;
using System.Linq;
using Kingmaker;
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
        private RuleDealDamage _consumedDamageRule;
        private bool _consumedDamageTargetWasAlive;
        private bool _resolvingBoneShards;
        
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

            if (GetClassLevel() < 20)
            {
                base.OnEventDidTrigger(evt);
                ConsumedRule = null;
            }
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
            if (!IsConsumedMeleeDamageRule(evt))
            {
                return;
            }

            _consumedDamageRule = evt;
            _consumedDamageTargetWasAlive = !IsDead(evt.Target);
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt == null || evt != _consumedDamageRule)
            {
                return;
            }

            try
            {
                ApplyBoneShards(evt);
            }
            finally
            {
                RemoveFact();
                ConsumedRule = null;
                _consumedDamageRule = null;
                _consumedDamageTargetWasAlive = false;
            }
        }

        private void ApplyBoneShards(RuleDealDamage evt)
        {
            if (GetClassLevel() < 20 ||
                _resolvingBoneShards ||
                !_consumedDamageTargetWasAlive ||
                evt.Result <= 0 ||
                !IsDead(evt.Target))
            {
                return;
            }

            var caster = Owner;
            var source = evt.Target;

            var attack = evt.AttackRoll?.RuleAttackWithWeapon;
            var damageType = attack?.Weapon?.Blueprint?.DamageType;
            var form = damageType?.Type == DamageType.Physical
                ? damageType.Physical.Form
                : PhysicalDamageForm.Bludgeoning;
            foreach (var target in GetAdjacentEnemies(caster, source))
            {
                DealBoneShardDamage(caster, target, evt.Result, form);
            }
        }

        private void DealBoneShardDamage(
            UnitEntityData caster,
            UnitEntityData target,
            int damageAmount,
            PhysicalDamageForm form)
        {
            var damage = new PhysicalDamage(
                new ModifiableDiceFormula(new DiceFormula(0, DiceType.Zero)),
                damageAmount,
                form);

            _resolvingBoneShards = true;
            try
            {
                Rulebook.Trigger(new RuleDealDamage(caster, target, damage));
            }
            finally
            {
                _resolvingBoneShards = false;
            }
        }

        private static UnitEntityData[] GetAdjacentEnemies(UnitEntityData caster, UnitEntityData source)
        {
            var areaState = Game.Instance?.State?.LoadedAreaState;
            if (areaState == null)
            {
                return Array.Empty<UnitEntityData>();
            }

            var radiusMeters = 5.Feet().Meters;
            return areaState.AllEntityData
                .OfType<UnitEntityData>()
                .Where(unit => IsBoneShardTarget(caster, source, unit, radiusMeters))
                .ToArray();
        }

        private static bool IsBoneShardTarget(
            UnitEntityData caster,
            UnitEntityData source,
            UnitEntityData target,
            float radiusMeters)
        {
            return target != null &&
                   target != caster &&
                   target != source &&
                   !IsDead(target) &&
                   caster.IsEnemy(target) &&
                   source.DistanceTo(target) <= radiusMeters;
        }

        private bool IsConsumedMeleeDamageRule(RuleDealDamage evt)
        {
            if (_resolvingBoneShards || ConsumedRule == null || evt == null)
            {
                return false;
            }

            if (evt.Calculate != null && evt.Calculate != ConsumedRule)
            {
                return false;
            }

            var attack = evt.AttackRoll?.RuleAttackWithWeapon;
            return attack?.Weapon != null && IsMeleeWeapon(attack.Weapon);
        }

        private static bool IsDead(UnitEntityData unit)
        {
            return unit == null ||
                   unit.State.IsDead ||
                   unit.State.IsFinallyDead;
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
