using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.HitSystem;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using UnityEngine.Rendering;
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
        private static readonly Dictionary<int, ParticleSystem.MinMaxGradient> OriginalParticleColors =
            new Dictionary<int, ParticleSystem.MinMaxGradient>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxGradient> OriginalParticleColorOverLifetimeColors =
            new Dictionary<int, ParticleSystem.MinMaxGradient>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxGradient> OriginalParticleColorBySpeedColors =
            new Dictionary<int, ParticleSystem.MinMaxGradient>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxGradient> OriginalParticleTrailLifetimeColors =
            new Dictionary<int, ParticleSystem.MinMaxGradient>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxGradient> OriginalParticleTrailColors =
            new Dictionary<int, ParticleSystem.MinMaxGradient>();
        private static readonly Dictionary<int, Gradient> OriginalLineRendererGradients =
            new Dictionary<int, Gradient>();
        private static readonly Dictionary<int, Gradient> OriginalTrailRendererGradients =
            new Dictionary<int, Gradient>();
        private static readonly Dictionary<int, Gradient> OriginalRayViewGradients =
            new Dictionary<int, Gradient>();
        private static readonly Dictionary<int, Color> OriginalLightColors =
            new Dictionary<int, Color>();
        private static readonly Dictionary<int, Dictionary<string, Color>> OriginalMaterialColors =
            new Dictionary<int, Dictionary<string, Color>>();
        private static readonly FieldInfo RayViewColorGradientOriginField =
            AccessTools.Field(typeof(RayView), "m_ColorGradientOrigin");

        public static void ApplyTint(GameObject fx, Color tint)
        {
            if (fx == null)
            {
                return;
            }

            foreach (var particleSystem in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particleSystem.main;
                RememberParticleColor(particleSystem, main);
                main.startColor = TintMinMaxGradient(main.startColor, tint);

                var colorOverLifetime = particleSystem.colorOverLifetime;
                if (colorOverLifetime.enabled)
                {
                    RememberParticleColorOverLifetimeColor(particleSystem, colorOverLifetime);
                    colorOverLifetime.color = TintMinMaxGradient(colorOverLifetime.color, tint);
                }

                var colorBySpeed = particleSystem.colorBySpeed;
                if (colorBySpeed.enabled)
                {
                    RememberParticleColorBySpeedColor(particleSystem, colorBySpeed);
                    colorBySpeed.color = TintMinMaxGradient(colorBySpeed.color, tint);
                }

                var trails = particleSystem.trails;
                if (trails.enabled)
                {
                    RememberParticleTrailColors(particleSystem, trails);
                    trails.colorOverLifetime = TintMinMaxGradient(trails.colorOverLifetime, tint);
                    trails.colorOverTrail = TintMinMaxGradient(trails.colorOverTrail, tint);
                }
            }

            ApplyRayRendererTint(fx, tint);
            ApplyMaterialTint(fx, tint);
        }

        private static void SetMaterialColors(Material material, Color tint)
        {
            SetMaterialColor(material, "_Color", tint);
            SetMaterialColor(material, "_TintColor", tint);
            SetMaterialColor(material, "_BaseColor", tint);
            SetMaterialColor(material, "_EmissionColor", tint);

            var shader = material.shader;
            if (shader == null)
            {
                return;
            }

            for (var index = 0; index < shader.GetPropertyCount(); index++)
            {
                var propertyName = shader.GetPropertyName(index);
                var propertyType = shader.GetPropertyType(index);
                if (propertyType == ShaderPropertyType.Color ||
                    propertyType == ShaderPropertyType.Vector && IsColorLikePropertyName(propertyName))
                {
                    SetMaterialColor(material, propertyName, tint);
                }
            }
        }

        private static void SetMaterialColor(Material material, string propertyName, Color tint)
        {
            if (material.HasProperty(propertyName))
            {
                var original = material.GetColor(propertyName);
                RememberMaterialColor(material, propertyName);
                material.SetColor(propertyName, TintColor(original, tint));
            }
        }

        private static void RestoreTint(GameObject fx)
        {
            if (fx == null)
            {
                return;
            }

            foreach (var particleSystem in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!OriginalParticleColors.TryGetValue(particleSystem.GetInstanceID(), out var color))
                {
                    continue;
                }

                var main = particleSystem.main;
                main.startColor = color;

                var colorOverLifetime = particleSystem.colorOverLifetime;
                if (OriginalParticleColorOverLifetimeColors.TryGetValue(
                        particleSystem.GetInstanceID(),
                        out var colorOverLifetimeColor))
                {
                    colorOverLifetime.color = colorOverLifetimeColor;
                }

                var colorBySpeed = particleSystem.colorBySpeed;
                if (OriginalParticleColorBySpeedColors.TryGetValue(
                        particleSystem.GetInstanceID(),
                        out var colorBySpeedColor))
                {
                    colorBySpeed.color = colorBySpeedColor;
                }

                var trails = particleSystem.trails;
                if (OriginalParticleTrailLifetimeColors.TryGetValue(
                        particleSystem.GetInstanceID(),
                        out var trailLifetimeColor))
                {
                    trails.colorOverLifetime = trailLifetimeColor;
                }

                if (OriginalParticleTrailColors.TryGetValue(
                        particleSystem.GetInstanceID(),
                        out var trailColor))
                {
                    trails.colorOverTrail = trailColor;
                }
            }

            RestoreRayRendererTint(fx);
            RestoreMaterialTint(fx);
        }

        private static void ApplyRayRendererTint(GameObject fx, Color tint)
        {
            foreach (var lineRenderer in fx.GetComponentsInChildren<LineRenderer>(true))
            {
                RememberLineRendererGradient(lineRenderer);
                lineRenderer.colorGradient = TintGradient(GetOriginalLineRendererGradient(lineRenderer), tint);
            }

            foreach (var trailRenderer in fx.GetComponentsInChildren<TrailRenderer>(true))
            {
                RememberTrailRendererGradient(trailRenderer);
                trailRenderer.colorGradient = TintGradient(GetOriginalTrailRendererGradient(trailRenderer), tint);
            }

            foreach (var light in fx.GetComponentsInChildren<Light>(true))
            {
                RememberLightColor(light);
                light.color = TintColor(light.color, tint);
            }

            foreach (var rayView in fx.GetComponentsInChildren<RayView>(true))
            {
                if (TryRememberRayViewGradient(rayView, out var gradient))
                {
                    RayViewColorGradientOriginField.SetValue(rayView, TintGradient(gradient, tint));
                }
            }
        }

        private static void RestoreRayRendererTint(GameObject fx)
        {
            foreach (var lineRenderer in fx.GetComponentsInChildren<LineRenderer>(true))
            {
                if (OriginalLineRendererGradients.TryGetValue(lineRenderer.GetInstanceID(), out var gradient))
                {
                    lineRenderer.colorGradient = CloneGradient(gradient);
                }
            }

            foreach (var trailRenderer in fx.GetComponentsInChildren<TrailRenderer>(true))
            {
                if (OriginalTrailRendererGradients.TryGetValue(trailRenderer.GetInstanceID(), out var gradient))
                {
                    trailRenderer.colorGradient = CloneGradient(gradient);
                }
            }

            foreach (var light in fx.GetComponentsInChildren<Light>(true))
            {
                if (OriginalLightColors.TryGetValue(light.GetInstanceID(), out var color))
                {
                    light.color = color;
                }
            }

            foreach (var rayView in fx.GetComponentsInChildren<RayView>(true))
            {
                if (RayViewColorGradientOriginField == null ||
                    !OriginalRayViewGradients.TryGetValue(rayView.GetInstanceID(), out var gradient))
                {
                    continue;
                }

                RayViewColorGradientOriginField.SetValue(rayView, CloneGradient(gradient));
            }
        }

        private static void ApplyMaterialTint(GameObject fx, Color tint)
        {
            foreach (var renderer in fx.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    SetMaterialColors(material, tint);
                }
            }

            foreach (var particleRenderer in fx.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (particleRenderer.trailMaterial != null)
                {
                    SetMaterialColors(particleRenderer.trailMaterial, tint);
                }
            }
        }

        private static void RestoreMaterialTint(GameObject fx)
        {
            foreach (var renderer in fx.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    RestoreMaterialColors(material);
                }
            }

            foreach (var particleRenderer in fx.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                RestoreMaterialColors(particleRenderer.trailMaterial);
            }
        }

        private static Gradient GetOriginalLineRendererGradient(LineRenderer lineRenderer)
        {
            return OriginalLineRendererGradients.TryGetValue(lineRenderer.GetInstanceID(), out var gradient)
                ? gradient
                : lineRenderer.colorGradient;
        }

        private static void RememberLineRendererGradient(LineRenderer lineRenderer)
        {
            var key = lineRenderer.GetInstanceID();
            if (!OriginalLineRendererGradients.ContainsKey(key))
            {
                OriginalLineRendererGradients[key] = CloneGradient(lineRenderer.colorGradient);
            }
        }

        private static Gradient GetOriginalTrailRendererGradient(TrailRenderer trailRenderer)
        {
            return OriginalTrailRendererGradients.TryGetValue(trailRenderer.GetInstanceID(), out var gradient)
                ? gradient
                : trailRenderer.colorGradient;
        }

        private static void RememberTrailRendererGradient(TrailRenderer trailRenderer)
        {
            var key = trailRenderer.GetInstanceID();
            if (!OriginalTrailRendererGradients.ContainsKey(key))
            {
                OriginalTrailRendererGradients[key] = CloneGradient(trailRenderer.colorGradient);
            }
        }

        private static void RememberLightColor(Light light)
        {
            var key = light.GetInstanceID();
            if (!OriginalLightColors.ContainsKey(key))
            {
                OriginalLightColors[key] = light.color;
            }
        }

        private static bool TryRememberRayViewGradient(RayView rayView, out Gradient gradient)
        {
            gradient = null;
            if (RayViewColorGradientOriginField == null)
            {
                return false;
            }

            var key = rayView.GetInstanceID();
            if (!OriginalRayViewGradients.TryGetValue(key, out gradient))
            {
                gradient = RayViewColorGradientOriginField.GetValue(rayView) as Gradient;
                if (gradient == null)
                {
                    return false;
                }

                OriginalRayViewGradients[key] = CloneGradient(gradient);
            }

            return true;
        }

        private static Gradient TintGradient(Gradient source, Color tint)
        {
            var gradient = CloneGradient(source);
            var colorKeys = gradient.colorKeys;
            for (var index = 0; index < colorKeys.Length; index++)
            {
                colorKeys[index].color = TintColor(colorKeys[index].color, tint);
            }

            gradient.colorKeys = colorKeys;
            return gradient;
        }

        private static ParticleSystem.MinMaxGradient TintMinMaxGradient(
            ParticleSystem.MinMaxGradient source,
            Color tint)
        {
            var result = source;
            var mode = source.mode;
            switch (mode)
            {
                case ParticleSystemGradientMode.Color:
                    result.color = TintColor(source.color, tint);
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    result.colorMin = TintColor(source.colorMin, tint);
                    result.colorMax = TintColor(source.colorMax, tint);
                    break;
                case ParticleSystemGradientMode.Gradient:
                    result.gradient = TintGradient(source.gradient, tint);
                    break;
                case ParticleSystemGradientMode.TwoGradients:
                    result.gradientMin = TintGradient(source.gradientMin, tint);
                    result.gradientMax = TintGradient(source.gradientMax, tint);
                    break;
                case ParticleSystemGradientMode.RandomColor:
                    result.gradient = TintGradient(source.gradient, tint);
                    break;
            }

            result.mode = mode;
            return result;
        }

        private static Color TintColor(Color source, Color tint)
        {
            var intensity = Mathf.Max(source.r, source.g, source.b);
            if (intensity <= 0f)
            {
                intensity = 1f;
            }

            return new Color(
                tint.r * intensity,
                tint.g * intensity,
                tint.b * intensity,
                source.a);
        }

        private static Gradient CloneGradient(Gradient source)
        {
            var gradient = new Gradient();
            if (source == null)
            {
                return gradient;
            }

            gradient.mode = source.mode;
            gradient.SetKeys(source.colorKeys, source.alphaKeys);
            return gradient;
        }

        private static void RememberParticleColor(ParticleSystem particleSystem, ParticleSystem.MainModule main)
        {
            var key = particleSystem.GetInstanceID();
            if (!OriginalParticleColors.ContainsKey(key))
            {
                OriginalParticleColors[key] = main.startColor;
            }
        }

        private static void RememberParticleColorOverLifetimeColor(
            ParticleSystem particleSystem,
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime)
        {
            var key = particleSystem.GetInstanceID();
            if (!OriginalParticleColorOverLifetimeColors.ContainsKey(key))
            {
                OriginalParticleColorOverLifetimeColors[key] = colorOverLifetime.color;
            }
        }

        private static void RememberParticleColorBySpeedColor(
            ParticleSystem particleSystem,
            ParticleSystem.ColorBySpeedModule colorBySpeed)
        {
            var key = particleSystem.GetInstanceID();
            if (!OriginalParticleColorBySpeedColors.ContainsKey(key))
            {
                OriginalParticleColorBySpeedColors[key] = colorBySpeed.color;
            }
        }

        private static void RememberParticleTrailColors(
            ParticleSystem particleSystem,
            ParticleSystem.TrailModule trails)
        {
            var key = particleSystem.GetInstanceID();
            if (!OriginalParticleTrailLifetimeColors.ContainsKey(key))
            {
                OriginalParticleTrailLifetimeColors[key] = trails.colorOverLifetime;
            }

            if (!OriginalParticleTrailColors.ContainsKey(key))
            {
                OriginalParticleTrailColors[key] = trails.colorOverTrail;
            }
        }

        private static void RememberMaterialColor(Material material, string propertyName)
        {
            var key = material.GetInstanceID();
            if (!OriginalMaterialColors.TryGetValue(key, out var colors))
            {
                colors = new Dictionary<string, Color>();
                OriginalMaterialColors[key] = colors;
            }

            if (!colors.ContainsKey(propertyName))
            {
                colors[propertyName] = material.GetColor(propertyName);
            }
        }

        private static void RestoreMaterialColors(Material material)
        {
            if (material == null ||
                !OriginalMaterialColors.TryGetValue(material.GetInstanceID(), out var colors))
            {
                return;
            }

            foreach (var entry in colors)
            {
                if (material.HasProperty(entry.Key))
                {
                    material.SetColor(entry.Key, entry.Value);
                }
            }
        }

        private static bool IsColorLikePropertyName(string propertyName)
        {
            return propertyName != null &&
                   (propertyName.IndexOf("Color", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    propertyName.IndexOf("Tint", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    propertyName.IndexOf("Emission", System.StringComparison.OrdinalIgnoreCase) >= 0);
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
            if (handle == null)
            {
                return;
            }

            handle.RunAfterSpawn(obj =>
            {
                RestoreTint(obj);
                if (tint.HasValue)
                {
                    ApplyTint(obj, tint.Value);
                }
            });
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
                RestoreTint(projectile?.View);
                if (tint.HasValue)
                {
                    ApplyTint(projectile?.View, tint.Value);
                }
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
