using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace wotr_mod.Features
{
    public sealed class ClassLevelBreathDamageScaling :
        UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>,
        IRulebookHandler<RuleCalculateDamage>,
        IInitiatorRulebookSubscriber
    {
        public BlueprintAbility Ability;
        public BlueprintCharacterClass CharacterClass;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (Ability == null ||
                evt.Reason.Context?.SourceAbility == null ||
                evt.Reason.Context.SourceAbility.AssetGuid != Ability.AssetGuid)
            {
                return;
            }

            var classLevel = CharacterClass == null
                ? Owner.Progression.CharacterLevel
                : Owner.Progression.GetClassLevel(CharacterClass);
            if (classLevel < 9)
            {
                return;
            }

            var diceType = DiceType.D8;
            var bonusPerDie = 1;
            if (classLevel >= 20)
            {
                diceType = DiceType.D12;
                bonusPerDie = 3;
            }
            else if (classLevel >= 17)
            {
                diceType = DiceType.D10;
                bonusPerDie = 2;
            }

            foreach (var damage in evt.DamageBundle)
            {
                var energyDamage = damage as EnergyDamage;
                if (energyDamage == null)
                {
                    continue;
                }

                var rolls = energyDamage.Dice.ModifiedValue.Rolls;
                energyDamage.Dice.Modify(new DiceFormula(rolls, diceType), Fact);
                energyDamage.AddModifier(rolls * bonusPerDie, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
        }
    }
}
