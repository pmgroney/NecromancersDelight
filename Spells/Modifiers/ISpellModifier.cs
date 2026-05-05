namespace wotr_mod.Spells.Modifiers
{
    internal interface ISpellModifier
    {
        void Apply(SpellModifierContext context);
    }
}
