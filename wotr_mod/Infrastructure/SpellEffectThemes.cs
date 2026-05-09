using UnityEngine;

namespace wotr_mod.Infrastructure
{
    internal static class SpellEffectThemes
    {
        public static readonly Color Electric = new Color(0.78f, 0.94f, 1.00f);
        public static readonly Color Shadow   = new Color(0.55f, 0.00f, 0.90f);
        public static readonly Color Acid     = new Color(0.40f, 1.00f, 0.05f);
        public static readonly Color Necro    = new Color(0.03f, 0.28f, 0.08f);
        public static readonly Color Cold     = new Color(0.05f, 0.35f, 1.00f);
        public static readonly Color Fire     = new Color(1.00f, 0.25f, 0.05f);
        public static readonly Color Arcane   = new Color(0.62f, 0.34f, 1.00f);

        public static Color ColorFor(SpellEffectTheme theme)
        {
            switch (theme)
            {
                case SpellEffectTheme.Electric: return Electric;
                case SpellEffectTheme.Shadow:   return Shadow;
                case SpellEffectTheme.Acid:     return Acid;
                case SpellEffectTheme.Necro:    return Necro;
                case SpellEffectTheme.Cold:     return Cold;
                case SpellEffectTheme.Fire:     return Fire;
                case SpellEffectTheme.Arcane:   return Arcane;
                default:                        return Color.white;
            }
        }
    }
}
