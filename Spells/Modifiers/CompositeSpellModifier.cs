namespace wotr_mod.Spells.Modifiers
{
    internal sealed class CompositeSpellModifier : ISpellModifier
    {
        private readonly ISpellModifier[] _modifiers;

        public CompositeSpellModifier(params ISpellModifier[] modifiers)
        {
            _modifiers = modifiers;
        }

        public void Apply(SpellModifierContext context)
        {
            foreach (var modifier in _modifiers)
            {
                modifier?.Apply(context);
            }
        }
    }
}
