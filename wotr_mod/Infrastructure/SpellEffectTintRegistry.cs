using System.Collections.Generic;
using UnityEngine;

namespace wotr_mod.Infrastructure
{
    internal static class SpellEffectTintRegistry
    {
        private static readonly Dictionary<string, Color> ProjectileTints = new Dictionary<string, Color>();
        private static readonly Dictionary<string, Color> AreaEffectTints = new Dictionary<string, Color>();

        public static void RegisterProjectileTint(string projectileGuid, Color tint)
        {
            Register(ProjectileTints, projectileGuid, tint);
        }

        public static void RegisterProjectileTint(string projectileGuid, SpellEffectTheme theme)
        {
            Register(ProjectileTints, projectileGuid, SpellEffectThemes.ColorFor(theme));
        }

        public static void RegisterAreaEffectTint(string areaEffectGuid, Color tint)
        {
            Register(AreaEffectTints, areaEffectGuid, tint);
        }

        public static void RegisterAreaEffectTint(string areaEffectGuid, SpellEffectTheme theme)
        {
            Register(AreaEffectTints, areaEffectGuid, SpellEffectThemes.ColorFor(theme));
        }

        public static bool TryGetProjectileTint(string projectileGuid, out Color tint)
        {
            return TryGet(ProjectileTints, projectileGuid, out tint);
        }

        public static bool TryGetAreaEffectTint(string areaEffectGuid, out Color tint)
        {
            return TryGet(AreaEffectTints, areaEffectGuid, out tint);
        }

        private static void Register(Dictionary<string, Color> registry, string guid, Color tint)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            registry[BlueprintTool.NormalizeGuid(guid)] = tint;
        }

        private static bool TryGet(Dictionary<string, Color> registry, string guid, out Color tint)
        {
            tint = default(Color);
            return !string.IsNullOrEmpty(guid) &&
                   registry.TryGetValue(BlueprintTool.NormalizeGuid(guid), out tint);
        }
    }
}
