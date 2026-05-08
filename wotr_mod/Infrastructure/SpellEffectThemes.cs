using UnityEngine;

namespace wotr_mod.Infrastructure
{
    internal static class SpellEffectThemes
    {
        public static readonly Color Electric = new Color(0.30f, 0.85f, 1.00f);
        public static readonly Color Shadow   = new Color(0.55f, 0.00f, 0.90f);
        public static readonly Color Acid     = new Color(0.40f, 1.00f, 0.05f);
        public static readonly Color Necro    = new Color(0.35f, 1.00f, 0.30f);

        public static Color ColorFor(SpellEffectTheme theme)
        {
            switch (theme)
            {
                case SpellEffectTheme.Electric: return Electric;
                case SpellEffectTheme.Shadow:   return Shadow;
                case SpellEffectTheme.Acid:     return Acid;
                case SpellEffectTheme.Necro:    return Necro;
                default:                        return Color.white;
            }
        }
    }
}
