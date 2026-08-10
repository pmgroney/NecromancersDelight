using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using wotr_mod.Infrastructure;

namespace wotr_mod.Features
{
    public sealed class MasterOfDeathArcanaClassSpells :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        private static readonly BlueprintGuid WitheringRayGuid = BlueprintGuid.Parse(ModBlueprintIds.Abilities.WitheringRay);

        public BlueprintCharacterClass[] Classes;
        public BlueprintBuff ConversionBuff;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var context = evt.Reason.Context;
            var sourceAbility = context?.SourceAbility;
            if (sourceAbility == null)
            {
                return;
            }

            // Withering Ray is a supernatural bloodline power, not a spellbook spell, so it
            // can't be matched by the IsSpell/spellbook checks below. Match it directly instead.
            if (sourceAbility.AssetGuid == WitheringRayGuid)
            {
                foreach (var characterClass in Classes ?? new BlueprintCharacterClass[0])
                {
                    if (characterClass == null || Owner.Progression.GetClassLevel(characterClass) <= 0)
                    {
                        continue;
                    }

                    ApplyBonus(evt, characterClass);
                    return;
                }

                return;
            }

            var isEligibleSpell = sourceAbility.IsSpell &&
                                  (sourceAbility.School == SpellSchool.Necromancy ||
                                   IsSpellConvertedByMaleficConversion(evt));
            if (!isEligibleSpell)
            {
                return;
            }

            foreach (var characterClass in Classes ?? new BlueprintCharacterClass[0])
            {
                if (characterClass == null || Owner.Progression.GetClassLevel(characterClass) <= 0)
                {
                    continue;
                }

                ApplyBonus(evt, characterClass);
                ApplyEnergyResistancePenetration(evt, characterClass);
                return;
            }
        }

        private bool IsSpellConvertedByMaleficConversion(RuleCalculateDamage evt)
        {
            if (ConversionBuff == null || !Owner.HasFact(ConversionBuff))
            {
                return false;
            }

            return evt.DamageBundle
                .OfType<EnergyDamage>()
                .Any(damage => damage.EnergyType == DamageEnergyType.Unholy ||
                               IsElementalEnergy(damage.EnergyType));
        }

        private static bool IsElementalEnergy(DamageEnergyType energyType)
        {
            return energyType == DamageEnergyType.Acid ||
                   energyType == DamageEnergyType.Cold ||
                   energyType == DamageEnergyType.Electricity ||
                   energyType == DamageEnergyType.Fire;
        }

        private void ApplyBonus(RuleCalculateDamage evt, BlueprintCharacterClass characterClass)
        {
            var bonusPerDie = GetBonusPerDie(characterClass);
            if (bonusPerDie <= 0)
            {
                return;
            }

            foreach (var baseDamage in evt.DamageBundle)
            {
                baseDamage.AddModifier(baseDamage.Dice.ModifiedValue.Rolls * bonusPerDie, Fact);
            }
        }

        private void ApplyEnergyResistancePenetration(
            RuleCalculateDamage evt,
            BlueprintCharacterClass characterClass)
        {
            var classLevel = characterClass == null ? 0 : Owner.Progression.GetClassLevel(characterClass);
            if (classLevel < 4)
            {
                return;
            }

            var maleficConversionActive = ConversionBuff != null && Owner.HasFact(ConversionBuff);
            foreach (var damage in evt.DamageBundle)
            {
                if (!(damage is EnergyDamage energyDamage)
                    || !IsMasterOfDeathEnergyDamage(energyDamage.EnergyType, maleficConversionActive))
                {
                    continue;
                }

                if (classLevel >= 12)
                {
                    damage.IgnoreReduction = true;
                    continue;
                }

                var penetration = classLevel >= 8 ? 10 : 5;
                damage.ReductionPenalty.Add(new Modifier(penetration, Fact));
            }
        }

        private static bool IsMasterOfDeathEnergyDamage(
            DamageEnergyType energyType,
            bool maleficConversionActive)
        {
            return energyType == DamageEnergyType.Unholy ||
                   energyType == DamageEnergyType.NegativeEnergy ||
                   (maleficConversionActive && IsElementalEnergy(energyType));
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        private int GetBonusPerDie(BlueprintCharacterClass characterClass)
        {
            var classLevel = characterClass == null ? 0 : Owner.Progression.GetClassLevel(characterClass);
            return GetDamageScaling(classLevel);
        }

        internal static int GetDamageScaling(int classLevel)
        {
            if (classLevel <= 0)
            {
                return 0;
            }

            var bonus = 1 + classLevel / 4;
            return bonus > 6 ? 6 : bonus;
        }
    }

    public sealed class MasterOfDeathUndeadSummonTrait :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSummonUnit>,
        IRulebookHandler<RuleSummonUnit>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintFeature UndeadType;
        public BlueprintBuff SummonBuff;

        public void OnEventAboutToTrigger(RuleSummonUnit evt)
        {
        }

        public void OnEventDidTrigger(RuleSummonUnit evt)
        {
            var summoned = evt?.SummonedUnit;
            if (SummonBuff == null || !IsUndead(summoned))
            {
                return;
            }

            if (evt.Context != null)
            {
                summoned.Buffs.AddBuff(SummonBuff, evt.Context, null);
                return;
            }

            summoned.Buffs.AddBuff(SummonBuff, Owner, null, null);
        }

        private bool IsUndead(UnitEntityData unit)
        {
            return UndeadType != null &&
                   unit?.Descriptor?.Progression?.Features?.HasFact(UndeadType) == true;
        }
    }

    public sealed class MasterOfDeathUndeadSummonBuff : UnitFactComponentDelegate
    {
        public BlueprintCharacterClass CharacterClass;
        public ModifierDescriptor Descriptor = ModifierDescriptor.UntypedStackable;

        protected override void OnActivate()
        {
            ApplyBonuses();
        }

        protected override void OnTurnOn()
        {
            ApplyBonuses();
        }

        protected override void OnDeactivate()
        {
            RemoveBonuses();
        }

        protected override void OnTurnOff()
        {
            RemoveBonuses();
        }

        private void ApplyBonuses()
        {
            RemoveBonuses();

            var classLevel = GetCasterClassLevel();
            if (classLevel <= 0)
            {
                return;
            }

            var combatBonus = MasterOfDeathArcanaClassSpells.GetDamageScaling(classLevel);
            AddStatBonus(StatType.HitPoints, classLevel * 3);
            AddStatBonus(StatType.AdditionalAttackBonus, combatBonus);
            AddStatBonus(StatType.AdditionalDamage, combatBonus);
        }

        private int GetCasterClassLevel()
        {
            var caster = Fact?.MaybeContext?.MaybeCaster;
            return CharacterClass == null || caster == null
                ? 0
                : caster.Progression.GetClassLevel(CharacterClass);
        }

        private void AddStatBonus(StatType stat, int value)
        {
            if (Owner?.Descriptor?.Stats == null)
            {
                return;
            }

            Owner.Descriptor.Stats.GetStat(stat)?.AddModifierUnique(value, Runtime, Descriptor);
        }

        private void RemoveBonuses()
        {
            if (Owner?.Descriptor?.Stats == null)
            {
                return;
            }

            foreach (var stat in Owner.Descriptor.Stats.AllStats)
            {
                stat?.RemoveModifiersFrom(Runtime);
            }
        }
    }

    public sealed class GetKnownSpellsFromMemorizationSpellbook : BlueprintComponent
    {
        public BlueprintSpellbook Spellbook;
    }

    public sealed class WitheringRayCastingStatDamageBonus :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        private static readonly BlueprintGuid WitheringRayGuid =
            BlueprintGuid.Parse(ModBlueprintIds.Abilities.WitheringRay);

        public BlueprintCharacterClass CharacterClass;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (evt.Reason.Context?.SourceAbility?.AssetGuid != WitheringRayGuid || CharacterClass == null)
            {
                return;
            }

            var castingAttribute = Owner.GetSpellbook(CharacterClass)?.Blueprint.CastingAttribute
                ?? Kingmaker.EntitySystem.Stats.StatType.Charisma;
            var castingStatBonus = Owner.Stats.GetAttribute(castingAttribute)?.Bonus ?? 0;
            if (castingStatBonus <= 0)
            {
                return;
            }

            foreach (var damage in evt.DamageBundle)
            {
                damage.AddModifier(castingStatBonus, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }
    }
}
