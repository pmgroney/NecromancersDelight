using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class DamageTypeSpellModifier : ISpellModifier
    {
        private readonly SpellDescriptor _removeDescriptor;
        private readonly SpellDescriptor _addDescriptor;
        private readonly DamageEnergyType? _fromEnergy;
        private readonly DamageEnergyType? _toEnergy;
        private readonly bool _fromForce;
        private readonly bool _toForce;
        private readonly DiceType? _diceType;
        private readonly ScalingConfig _scaling;

        public struct ScalingConfig
        {
            public AbilityRankType RankType;
            public ContextRankProgression Progression;
            public int StartLevel;
            public int StepLevel;
            public string[] AdditionalClasses;
        }

        public DamageTypeSpellModifier(
            SpellDescriptor removeDescriptor,
            SpellDescriptor addDescriptor,
            DamageEnergyType? fromEnergy,
            DamageEnergyType? toEnergy,
            bool fromForce = false,
            bool toForce = false,
            DiceType? diceType = null,
            ScalingConfig? scaling = null)
        {
            _removeDescriptor = removeDescriptor;
            _addDescriptor = addDescriptor;
            _fromEnergy = fromEnergy;
            _toEnergy = toEnergy;
            _fromForce = fromForce;
            _toForce = toForce;
            _diceType = diceType;
            _scaling = scaling ?? default;
        }

        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.ReplaceDescriptor(spell, _removeDescriptor, _addDescriptor, context.Blueprints);
            SpellModifierUtility.PatchRunActions(spell, PatchDamage);

            if (_scaling.RankType != default || _scaling.Progression != default)
            {
                var rank = context.Blueprints.EnsureComponent(spell, () => new ContextRankConfig());
                var additionalClasses = _scaling.AdditionalClasses?
                    .Select(guid => context.Blueprints.Get<BlueprintCharacterClass>(guid))
                    .Where(c => c != null)
                    .ToArray();

                context.Blueprints.ConfigureContextRankConfig(
                    rank,
                    type: _scaling.RankType,
                    progression: _scaling.Progression,
                    startLevel: _scaling.StartLevel,
                    stepLevel: _scaling.StepLevel,
                    additionalClasses: additionalClasses);
            }
        }

        private int PatchDamage(Kingmaker.ElementsSystem.GameAction action)
        {
            var damage = action as ContextActionDealDamage;
            if (damage == null || !MatchesSource(damage))
            {
                return 0;
            }

            damage.DamageType = _toForce
                ? SpellModifierUtility.ForceDamage()
                : SpellModifierUtility.EnergyDamage(_toEnergy.Value);

            if (_diceType.HasValue && damage.Value != null)
            {
                damage.Value = SpellModifierUtility.CopyDiceValue(damage.Value, _diceType.Value);
            }

            return 1;
        }

        private bool MatchesSource(ContextActionDealDamage damage)
        {
            if (_fromForce)
            {
                return damage.DamageType.Type == DamageType.Force;
            }

            return _fromEnergy.HasValue &&
                   damage.DamageType.Type == DamageType.Energy &&
                   damage.DamageType.Energy == _fromEnergy.Value;
        }
    }
}
