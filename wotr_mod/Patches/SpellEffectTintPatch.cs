using HarmonyLib;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal static class SpellEffectTintPatch
    {
        private static readonly AccessTools.FieldRef<AreaEffectView, IFxHandle> AreaEffectSpawnedFx =
            AccessTools.FieldRefAccess<AreaEffectView, IFxHandle>("m_SpawnedFx");

        public static void ApplyTint(GameObject fx, Color tint)
        {
            if (fx == null)
            {
                return;
            }

            foreach (var particleSystem in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particleSystem.main;
                main.startColor = tint;
            }

            foreach (var renderer in fx.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    SetMaterialColor(material, "_Color", tint);
                    SetMaterialColor(material, "_TintColor", tint);
                    SetMaterialColor(material, "_BaseColor", tint);
                    SetMaterialColor(material, "_EmissionColor", tint);
                }
            }
        }

        private static void SetMaterialColor(Material material, string propertyName, Color tint)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, tint);
            }
        }

        [HarmonyPatch(typeof(ProjectileController), "CreateView")]
        private static class ProjectileControllerCreateViewPatch
        {
            private static void Postfix(Projectile projectile)
            {
                var blueprint = projectile?.Blueprint;
                if (blueprint == null ||
                    !SpellEffectTintRegistry.TryGetProjectileTint(blueprint.AssetGuid.ToString(), out var tint))
                {
                    return;
                }

                ApplyTint(projectile.View, tint);
            }
        }

        [HarmonyPatch(typeof(AreaEffectView), "SpawnFxs")]
        private static class AreaEffectViewSpawnFxsPatch
        {
            private static void Postfix(AreaEffectView __instance)
            {
                var data = __instance?.Data as AreaEffectEntityData;
                var blueprint = data?.Blueprint;
                if (blueprint == null ||
                    !SpellEffectTintRegistry.TryGetAreaEffectTint(blueprint.AssetGuid.ToString(), out var tint))
                {
                    return;
                }

                var fx = AreaEffectSpawnedFx(__instance);
                if (fx == null)
                {
                    return;
                }

                fx.RunAfterSpawn(spawned => ApplyTint(spawned, tint));
            }
        }
    }
}
