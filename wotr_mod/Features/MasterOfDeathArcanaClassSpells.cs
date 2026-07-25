using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
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

            if (!sourceAbility.IsSpell || sourceAbility.School != SpellSchool.Necromancy)
            {
                return;
            }

            var spellbook = context.SourceAbilityContext?.Ability?.Spellbook;
            if (spellbook == null)
            {
                return;
            }

            foreach (var characterClass in Classes ?? new BlueprintCharacterClass[0])
            {
                if (Owner.GetSpellbook(characterClass) != GetClassSpellbook(spellbook))
                {
                    continue;
                }

                ApplyBonus(evt, characterClass);
                ApplyEnergyResistancePenetration(evt, characterClass);
                return;
            }
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

            foreach (var damage in evt.DamageBundle)
            {
                if (!(damage is EnergyDamage energyDamage)
                    || (energyDamage.EnergyType != DamageEnergyType.Unholy
                        && energyDamage.EnergyType != DamageEnergyType.NegativeEnergy))
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

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }

        private Spellbook GetClassSpellbook(Spellbook spellbook)
        {
            var memorizedSource = spellbook?.Blueprint.GetComponent<GetKnownSpellsFromMemorizationSpellbook>()?.Spellbook;
            return memorizedSource != null ? Owner.GetSpellbook(memorizedSource) : spellbook;
        }

        private int GetBonusPerDie(BlueprintCharacterClass characterClass)
        {
            var classLevel = characterClass == null ? 0 : Owner.Progression.GetClassLevel(characterClass);
            if (classLevel <= 0)
            {
                return 0;
            }

            var bonus = 1 + classLevel / 4;
            return bonus > 6 ? 6 : bonus;
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
