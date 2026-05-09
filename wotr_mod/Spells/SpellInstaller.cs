using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using UnityEngine;
using UnityModManagerNet;
using wotr_mod.Content;
using wotr_mod.Infrastructure;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Spells
{
    internal sealed class SpellInstaller : IContentModule
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly string _modPath;
        private readonly SpellIconLoader _icons;

        public SpellInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            string modPath)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _modPath = modPath;
            _icons = new SpellIconLoader(modPath);
        }

        public string Name => "Spells";

        public void RegisterLocalization()
        {
            var path = Path.Combine(_modPath ?? string.Empty, "Data", "Localization", "Spells.resx");
            var loaded = _localization.PutResx(path);
            if (loaded == 0)
            {
                _logger.Warning($"No spell localization loaded from {path}.");
            }
        }

        public void Install()
        {
            var wizardList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Wizard,
                "Wizard spell list");
            var clericList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Cleric,
                "Cleric/Oracle spell list");

            foreach (var definition in SpellRegistry.GetAll())
            {
                var spell = EnsureSpell(definition);
                if (IsGrantedOnlySpell(definition))
                {
                    _blueprints.RemoveSpellFromList(wizardList, spell);
                    _blueprints.RemoveSpellFromList(clericList, spell);
                    continue;
                }

                _blueprints.AddSpellToList(wizardList, spell, definition.SpellLevel);
                if (IsDivineListSpell(definition))
                {
                    _blueprints.AddSpellToList(clericList, spell, definition.SpellLevel);
                }
            }
        }

        internal IReadOnlyDictionary<string, BlueprintAbility> EnsureAll()
        {
            return SpellRegistry.GetAll().ToDictionary(def => def.NewSpellGuid, EnsureSpell);
        }

        private BlueprintAbility EnsureSpell(SpellDefinition definition)
        {
            var existing = _blueprints.Get<BlueprintAbility>(definition.NewSpellGuid);
            if (existing != null)
            {
                ApplyMetadata(existing, definition);
                definition.Modifier?.Apply(new SpellModifierContext(existing, definition, _blueprints, _logger));
                ConfigureSpellVisuals(existing, definition);
                return existing;
            }

            var baseSpell = _blueprints.Require<BlueprintAbility>(
                definition.BaseSpellGuid,
                definition.InternalName + " donor spell");

            var clone = _blueprints.CloneBlueprint(baseSpell, definition.NewSpellGuid, definition.InternalName);
            ApplyMetadata(clone, definition);
            definition.Modifier?.Apply(new SpellModifierContext(clone, definition, _blueprints, _logger));
            clone.OnEnable();
            _blueprints.AddCachedBlueprint(definition.NewSpellGuid, clone);
            ConfigureSpellVisuals(clone, definition);

            return clone;
        }

        private static readonly FieldInfo CasterAppearProjectileField =
            AccessTools.Field(typeof(BlueprintProjectile), "m_CasterAppearProjectile");

        private void ConfigureSpellVisuals(BlueprintAbility spell, SpellDefinition definition)
        {
            if (definition.NewSpellGuid == ModBlueprintIds.Spells.VitriolicBlast)
            {
                ConfigureVitriolicBurstVisuals(spell);
                return;
            }

            if (definition.NewSpellGuid == ModBlueprintIds.Spells.EldritchHorror)
            {
                ConfigureEldritchHorrorVisuals(spell);
                return;
            }

            if (definition.NewSpellGuid == ModBlueprintIds.Spells.Thunderhead)
            {
                ConfigureThunderheadVisuals(spell);
                return;
            }

            var necroProjectileVisuals = GetNecroProjectileVisuals(definition.NewSpellGuid);
            if (necroProjectileVisuals.HasValue)
            {
                ConfigureProjectileVisuals(spell, necroProjectileVisuals.Value);
                return;
            }

            var missileVisuals = GetMissileVisuals(definition.NewSpellGuid);
            if (missileVisuals.HasValue)
            {
                ConfigureProjectileVisuals(spell, missileVisuals.Value);
            }
        }

        private void ConfigureVitriolicBurstVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Acid);

            var projectile = EnsureVitriolicBlastProjectile(ability);
            if (projectile == null) return;

            SpellEffectTintRegistry.RegisterProjectileTint(
                projectile.AssetGuid.ToString(),
                SpellEffectTheme.Acid);
            RegisterProjectileCasterAppearTint(projectile, SpellEffectTheme.Acid);

            foreach (var delivery in _blueprints.GetComponents<AbilityDeliverProjectile>(ability))
            {
                _blueprints.SetAbilityDeliverProjectiles(delivery, projectile);
            }

            ability.OnEnable();
        }

        private BlueprintProjectile EnsureVitriolicBlastProjectile(BlueprintAbility ability)
        {
            var existing = _blueprints.Get<BlueprintProjectile>(ModBlueprintIds.Projectiles.VitriolicBlast);
            if (existing != null) return existing;

            var delivery = _blueprints.GetComponents<AbilityDeliverProjectile>(ability).FirstOrDefault();
            var projectileRefs = delivery != null
                ? BlueprintFields.AbilityDeliverProjectileProjectiles.GetValue(delivery) as BlueprintProjectileReference[]
                : null;
            var donor = projectileRefs?.FirstOrDefault()?.Get() as BlueprintProjectile;
            if (donor == null) return null;

            var projectile = _blueprints.CloneBlueprint(
                donor,
                ModBlueprintIds.Projectiles.VitriolicBlast,
                "WotrMod_VitriolicBlastProjectile");
            projectile.OnEnable();
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Projectiles.VitriolicBlast, projectile);
            return projectile;
        }

        private void ConfigureProjectileVisuals(BlueprintAbility ability, ProjectileVisuals visuals)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                visuals.Theme);

            var projectile = EnsureProjectile(ability, visuals.ProjectileGuid, visuals.ProjectileName);
            if (projectile == null) return;

            SpellEffectTintRegistry.RegisterProjectileTint(
                projectile.AssetGuid.ToString(),
                visuals.Theme);

            foreach (var delivery in _blueprints.GetComponents<AbilityDeliverProjectile>(ability))
            {
                _blueprints.SetAbilityDeliverProjectiles(delivery, projectile);
            }

            ability.OnEnable();
        }

        private static void ConfigureEldritchHorrorVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Necro);
            SpellEffectTintRegistry.RegisterAreaEffectTint(
                ModBlueprintIds.AreaEffects.EldritchHorror,
                SpellEffectTheme.Necro);
        }

        private static void ConfigureThunderheadVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Electric);
            SpellEffectTintRegistry.RegisterAreaEffectTint(
                ModBlueprintIds.AreaEffects.Thunderhead,
                SpellEffectTheme.Electric);
        }

        private BlueprintProjectile EnsureProjectile(BlueprintAbility ability, string projectileGuid, string projectileName)
        {
            var existing = _blueprints.Get<BlueprintProjectile>(projectileGuid);
            if (existing != null) return existing;

            var donor = GetFirstProjectile(ability);
            if (donor == null) return null;

            var projectile = _blueprints.CloneBlueprint(donor, projectileGuid, projectileName);
            projectile.OnEnable();
            _blueprints.AddCachedBlueprint(projectileGuid, projectile);
            return projectile;
        }

        private static BlueprintProjectile GetFirstProjectile(BlueprintAbility ability)
        {
            var delivery = ability?.GetComponent<AbilityDeliverProjectile>();
            var projectileRefs = delivery != null
                ? BlueprintFields.AbilityDeliverProjectileProjectiles.GetValue(delivery) as BlueprintProjectileReference[]
                : null;
            return projectileRefs?.FirstOrDefault()?.Get() as BlueprintProjectile;
        }

        private static ProjectileVisuals? GetNecroProjectileVisuals(string spellGuid)
        {
            if (spellGuid == ModBlueprintIds.Spells.BoneSpike)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.BoneSpike,
                    "WotrMod_BoneSpikeProjectile",
                    SpellEffectTheme.Necro);
            }

            if (spellGuid == ModBlueprintIds.Spells.DeathRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.DeathRay,
                    "WotrMod_DeathRayProjectile",
                    SpellEffectTheme.Necro);
            }

            return null;
        }

        private static ProjectileVisuals? GetMissileVisuals(string spellGuid)
        {
            if (spellGuid == ModBlueprintIds.Spells.AcidMissile)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.AcidMissile,
                    "WotrMod_AcidMissileProjectile",
                    SpellEffectTheme.Acid);
            }

            if (spellGuid == ModBlueprintIds.Spells.CorrosiveCascade)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.CorrosiveCascade,
                    "WotrMod_CorrosiveCascadeProjectile",
                    SpellEffectTheme.Acid);
            }

            if (spellGuid == ModBlueprintIds.Spells.ElectricMissile)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.ElectricMissile,
                    "WotrMod_ElectricMissileProjectile",
                    SpellEffectTheme.Electric);
            }

            if (spellGuid == ModBlueprintIds.Spells.FireMissile)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.FireMissile,
                    "WotrMod_FireMissileProjectile",
                    SpellEffectTheme.Fire);
            }

            if (spellGuid == ModBlueprintIds.Spells.FrozenLance)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.FrozenLance,
                    "WotrMod_FrozenLanceProjectile",
                    SpellEffectTheme.Cold);
            }

            if (spellGuid == ModBlueprintIds.Spells.IceMissile)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.IceMissile,
                    "WotrMod_IceMissileProjectile",
                    SpellEffectTheme.Cold);
            }

            return null;
        }

        private static void RegisterProjectileCasterAppearTint(BlueprintProjectile projectile, SpellEffectTheme theme)
        {
            if (CasterAppearProjectileField == null) return;
            var reference = CasterAppearProjectileField.GetValue(projectile) as BlueprintProjectileReference;
            var casterAppear = reference?.Get() as BlueprintProjectile;
            if (casterAppear != null)
            {
                SpellEffectTintRegistry.RegisterProjectileTint(
                    casterAppear.AssetGuid.ToString(),
                    theme);
            }
        }

        private static bool IsGrantedOnlySpell(SpellDefinition definition)
        {
            return definition.NewSpellGuid == ModBlueprintIds.Spells.BoneSpike;
        }

        private static bool IsDivineListSpell(SpellDefinition definition)
        {
            return definition.School == SpellSchool.Necromancy;
        }

        private void ApplyMetadata(BlueprintAbility spell, SpellDefinition definition)
        {
            _blueprints.RemoveComponents<LevelUpRecommendationComponent>(spell);

            _blueprints.SetAbilityDisplay(
                spell,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            var spellComponent = _blueprints.GetComponents<SpellComponent>(spell).FirstOrDefault();
            if (spellComponent != null)
            {
                spellComponent.School = definition.School;
            }
            else
            {
                _logger.Warning($"{definition.InternalName} has no SpellComponent.");
            }

            var icon = GetIcon(definition);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(spell, icon);
            }
        }

        private Sprite GetIcon(SpellDefinition definition)
        {
            var icon = _icons.Load(definition.IconPath);
            if (icon != null)
            {
                return icon;
            }

            var tint = GetMissileIconTint(definition.NewSpellGuid);
            if (!tint.HasValue)
            {
                return null;
            }

            var source = _blueprints.Require<BlueprintAbility>(
                definition.BaseSpellGuid,
                definition.InternalName + " icon donor");
            return _icons.Tint(source.Icon, definition.InternalName + "Icon", tint.Value.Color, tint.Value.Strength);
        }

        private static MissileIconTint? GetMissileIconTint(string spellGuid)
        {
            if (spellGuid == ModBlueprintIds.Spells.FireMissile)
            {
                return new MissileIconTint(new Color(1f, 0.16f, 0.08f, 1f), 0.78f);
            }

            if (spellGuid == ModBlueprintIds.Spells.AcidMissile)
            {
                return new MissileIconTint(new Color(0.18f, 0.95f, 0.18f, 1f), 0.76f);
            }

            if (spellGuid == ModBlueprintIds.Spells.ElectricMissile)
            {
                return new MissileIconTint(new Color(0.25f, 0.7f, 1f, 1f), 0.74f);
            }

            if (spellGuid == ModBlueprintIds.Spells.IceMissile)
            {
                return new MissileIconTint(new Color(0.65f, 0.95f, 1f, 1f), 0.74f);
            }

            return null;
        }

        private readonly struct MissileIconTint
        {
            public MissileIconTint(Color color, float strength)
            {
                Color = color;
                Strength = strength;
            }

            public Color Color { get; }
            public float Strength { get; }
        }

        private readonly struct ProjectileVisuals
        {
            public ProjectileVisuals(string projectileGuid, string projectileName, SpellEffectTheme theme)
            {
                ProjectileGuid = projectileGuid;
                ProjectileName = projectileName;
                Theme = theme;
            }

            public string ProjectileGuid { get; }
            public string ProjectileName { get; }
            public SpellEffectTheme Theme { get; }
        }
    }
}
