using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.HitSystem;
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

        private static readonly List<Color?> PendingAbilityFxTints = new List<Color?>();
        private static readonly List<Color?> PendingProjectileCastFxTints = new List<Color?>();
        private static readonly List<Color?> PendingProjectileHitTints = new List<Color?>();

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

        private static bool TryGetRuntimeTint(UnitEntityData caster, bool sourceIsSpell, out Color tint)
        {
            tint = default(Color);
            if (!sourceIsSpell ||
                !SpellEffectRuntimeTintRegistry.TryGetActiveTheme(caster, out var theme))
            {
                return false;
            }

            tint = SpellEffectThemes.ColorFor(theme);
            return true;
        }

        private static bool TryGetProjectileTint(Projectile projectile, out Color tint)
        {
            if (TryGetRuntimeTint(projectile?.Launcher?.Unit, IsSpellSource(projectile), out tint))
            {
                return true;
            }

            var blueprint = projectile?.Blueprint;
            return blueprint != null &&
                   SpellEffectTintRegistry.TryGetProjectileTint(blueprint.AssetGuid.ToString(), out tint);
        }

        private static bool TryGetAbilitySpawnFxTint(AbilityExecutionContext context, out Color tint)
        {
            if (TryGetRuntimeTint(context?.MaybeCaster, IsSpellSource(context), out tint))
            {
                return true;
            }

            var guid = context?.Ability?.Blueprint?.AssetGuid.ToString();
            return guid != null &&
                   SpellEffectTintRegistry.TryGetAbilitySpawnFxTint(guid, out tint);
        }

        private static bool IsSpellSource(AbilityExecutionContext context)
        {
            return context != null &&
                   (context.AbilityBlueprint?.IsSpell == true ||
                    context.SourceAbility?.IsSpell == true ||
                    IsSpellAbility(context.Ability) ||
                    IsSpellAbility(context.SourceAbilityContext?.Ability));
        }

        private static bool IsSpellSource(Projectile projectile)
        {
            var reason = projectile?.SavedContext?.CurrentEvent?.Reason;
            if (reason == null)
            {
                return true;
            }

            if (reason.Ability != null)
            {
                return IsSpellAbility(reason.Ability) ||
                       (reason.Context != null && IsSpellSource(reason.Context));
            }

            return IsSpellSource(reason.Context);
        }

        private static bool IsSpellSource(MechanicsContext context)
        {
            if (context == null)
            {
                return true;
            }

            return context.SourceAbility?.IsSpell == true ||
                   context.SourceAbilityContext?.AbilityBlueprint?.IsSpell == true ||
                   IsSpellAbility(context.SourceAbilityContext?.Ability);
        }

        private static bool IsSpellAbility(AbilityData ability)
        {
            return ability?.Blueprint?.IsSpell == true || ability?.Spellbook != null;
        }

        private static void PushPendingTint(List<Color?> stack, Color? tint)
        {
            stack.Add(tint);
        }

        private static Color? PopPendingTint(List<Color?> stack)
        {
            if (stack.Count == 0)
            {
                return null;
            }

            var tint = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return tint;
        }

        private static Color? PeekPendingTint(List<Color?> stack)
        {
            return stack.Count == 0 ? null : stack[stack.Count - 1];
        }

        private static void ApplyPendingTint(IFxHandle handle, Color? tint)
        {
            if (handle == null || tint == null)
            {
                return;
            }

            var tintValue = tint.Value;
            handle.RunAfterSpawn(obj => ApplyTint(obj, tintValue));
        }

        [HarmonyPatch(typeof(ProjectileController), "CreateView")]
        private static class ProjectileControllerCreateViewPatch
        {
            private static void Prefix(Projectile projectile)
            {
                Color tint;
                if (TryGetProjectileTint(projectile, out tint))
                {
                    PushPendingTint(PendingProjectileCastFxTints, tint);
                    return;
                }

                PushPendingTint(PendingProjectileCastFxTints, null);
            }

            private static void Postfix(Projectile projectile)
            {
                var tint = PopPendingTint(PendingProjectileCastFxTints);
                if (tint == null)
                {
                    return;
                }

                ApplyTint(projectile?.View, tint.Value);
            }
        }

        [HarmonyPatch(typeof(AreaEffectView), "SpawnFxs")]
        private static class AreaEffectViewSpawnFxsPatch
        {
            private static void Postfix(AreaEffectView __instance)
            {
                var data = __instance?.Data as AreaEffectEntityData;
                var blueprint = data?.Blueprint;
                var context = data?.Context ?? __instance?.Context;
                if (!TryGetRuntimeTint(context?.MaybeCaster, IsSpellSource(context), out var tint) &&
                    (blueprint == null ||
                     !SpellEffectTintRegistry.TryGetAreaEffectTint(blueprint.AssetGuid.ToString(), out tint)))
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

        [HarmonyPatch(typeof(HitPlayer), "PlayProjectileHit")]
        private static class HitPlayerPlayProjectileHitPatch
        {
            private static void Prefix(Projectile projectile)
            {
                if (TryGetProjectileTint(projectile, out var tint))
                {
                    PushPendingTint(PendingProjectileHitTints, tint);
                    return;
                }

                PushPendingTint(PendingProjectileHitTints, null);
            }

            private static void Postfix()
            {
                PopPendingTint(PendingProjectileHitTints);
            }
        }

        [HarmonyPatch(typeof(HitPlayer), "PlayHit")]
        private static class HitPlayerPlayHitPatch
        {
            private static void Postfix(IFxHandle __result)
            {
                ApplyPendingTint(__result, PeekPendingTint(PendingProjectileHitTints));
            }
        }

        [HarmonyPatch(typeof(AbilitySpawnFx), "Spawn")]
        private static class AbilitySpawnFxSpawnPatch
        {
            private static void Prefix(AbilityExecutionContext context)
            {
                if (TryGetAbilitySpawnFxTint(context, out var tint))
                {
                    PushPendingTint(PendingAbilityFxTints, tint);
                    return;
                }

                PushPendingTint(PendingAbilityFxTints, null);
            }

            private static void Postfix()
            {
                PopPendingTint(PendingAbilityFxTints);
            }
        }

        [HarmonyPatch(typeof(FxHelper), "SpawnFxOnPoint")]
        private static class FxHelperSpawnFxOnPointPatch
        {
            private static void Postfix(IFxHandle __result)
            {
                var tint = PeekPendingTint(PendingAbilityFxTints) ??
                           PeekPendingTint(PendingProjectileCastFxTints) ??
                           PeekPendingTint(PendingProjectileHitTints);
                ApplyPendingTint(__result, tint);
            }
        }

        [HarmonyPatch(typeof(FxHelper), "SpawnFxOnUnit")]
        private static class FxHelperSpawnFxOnUnitPatch
        {
            private static void Postfix(IFxHandle __result)
            {
                var tint = PeekPendingTint(PendingAbilityFxTints) ??
                           PeekPendingTint(PendingProjectileHitTints);
                ApplyPendingTint(__result, tint);
            }
        }
    }
}
