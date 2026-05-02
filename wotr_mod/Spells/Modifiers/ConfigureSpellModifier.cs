using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class ConfigureSpellModifier : ISpellModifier
    {
        private readonly SpellSchool _school;
        private readonly AbilityRange? _range;

        public ConfigureSpellModifier(SpellSchool school, AbilityRange? range = null)
        {
            _school = school;
            _range = range;
        }

        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, _school, context.Blueprints);
            if (_range.HasValue)
            {
                spell.Range = _range.Value;
            }
        }
    }
}
