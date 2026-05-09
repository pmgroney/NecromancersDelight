using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
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
                    _blueprints.RemoveComponents<SpellListComponent>(spell);
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

            PatchBaseGameSpells();
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

            if (definition.NewSpellGuid == ModBlueprintIds.Spells.GlacialPrison)
            {
                ConfigureGlacialPrisonVisuals(spell);
                return;
            }

            if (definition.NewSpellGuid == ModBlueprintIds.Spells.CataclysmicStorm)
            {
                ConfigureCataclysmicStormVisuals(spell);
                return;
            }

            if (definition.NewSpellGuid == ModBlueprintIds.Spells.HeavensWrath)
            {
                ConfigureHeavensWrathVisuals(spell);
                return;
            }

            if (definition.NewSpellGuid == ModBlueprintIds.Spells.VitriolicApocalypse)
            {
                ConfigureVitriolicApocalypseVisuals(spell);
                return;
            }

            var rayVisuals = GetRayVisuals(definition.NewSpellGuid);
            if (rayVisuals.HasValue)
            {
                ConfigureProjectileVisuals(spell, rayVisuals.Value);
                return;
            }

            var hellfireRayVisuals = GetHellfireRayVisuals(definition.NewSpellGuid);
            if (hellfireRayVisuals.HasValue)
            {
                ConfigureProjectileVisuals(spell, hellfireRayVisuals.Value);
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

        private static void ConfigureGlacialPrisonVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Cold);
            SpellEffectTintRegistry.RegisterAreaEffectTint(
                ModBlueprintIds.AreaEffects.GlacialPrison,
                SpellEffectTheme.Cold);
        }

        private static void ConfigureCataclysmicStormVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Electric);
            SpellEffectTintRegistry.RegisterAreaEffectTint(
                ModBlueprintIds.AreaEffects.CataclysmicStorm,
                SpellEffectTheme.Electric);
        }

        private static void ConfigureHeavensWrathVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Electric);
            SpellEffectTintRegistry.RegisterAreaEffectTint(
                ModBlueprintIds.AreaEffects.HeavensWrath,
                SpellEffectTheme.Electric);
        }

        private void ConfigureVitriolicApocalypseVisuals(BlueprintAbility ability)
        {
            ConfigureProjectileVisuals(
                ability,
                new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.VitriolicApocalypse,
                    "WotrMod_VitriolicApocalypseProjectile",
                    SpellEffectTheme.Acid));

            var buff = _blueprints.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(
                ModBlueprintIds.Buffs.MolecularDissolution);
            if (buff == null)
            {
                return;
            }

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text("wotr_mod.buff.molecular_dissolution.name"),
                _localization.Text("wotr_mod.buff.molecular_dissolution.description"));

            var icon = _icons.Load("Icons\\molecular_dissolution.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(buff, icon);
            }
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

        private static ProjectileVisuals? GetRayVisuals(string spellGuid)
        {
            if (spellGuid == ModBlueprintIds.Spells.CausticBeam)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.CausticBeam,
                    "WotrMod_CausticBeamProjectile",
                    SpellEffectTheme.Acid);
            }

            if (spellGuid == ModBlueprintIds.Spells.EmperorsWrath)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.EmperorsWrath,
                    "WotrMod_EmperorsWrathProjectile",
                    SpellEffectTheme.Electric);
            }

            if (spellGuid == ModBlueprintIds.Spells.ForceRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.ForceRay,
                    "WotrMod_ForceRayProjectile",
                    SpellEffectTheme.Arcane);
            }

            if (spellGuid == ModBlueprintIds.Spells.FrostBlast)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.FrostBlast,
                    "WotrMod_FrostBlastProjectile",
                    SpellEffectTheme.Cold);
            }

            return null;
        }

        private static ProjectileVisuals? GetHellfireRayVisuals(string spellGuid)
        {
            if (spellGuid == ModBlueprintIds.Spells.AcidHellfireRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.AcidHellfireRay,
                    "WotrMod_AcidHellfireRayProjectile",
                    SpellEffectTheme.Acid);
            }

            if (spellGuid == ModBlueprintIds.Spells.ColdHellfireRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.ColdHellfireRay,
                    "WotrMod_ColdHellfireRayProjectile",
                    SpellEffectTheme.Cold);
            }

            if (spellGuid == ModBlueprintIds.Spells.ElectricHellfireRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.ElectricHellfireRay,
                    "WotrMod_ElectricHellfireRayProjectile",
                    SpellEffectTheme.Electric);
            }

            if (spellGuid == ModBlueprintIds.Spells.FireHellfireRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.FireHellfireRay,
                    "WotrMod_FireHellfireRayProjectile",
                    SpellEffectTheme.Fire);
            }

            if (spellGuid == ModBlueprintIds.Spells.ShadowHellfireRay)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.ShadowHellfireRay,
                    "WotrMod_ShadowHellfireRayProjectile",
                    SpellEffectTheme.Shadow);
            }

            return null;
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

            if (spellGuid == ModBlueprintIds.Spells.CausticOblivion)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.CausticOblivion,
                    "WotrMod_CausticOblivionProjectile",
                    SpellEffectTheme.Acid);
            }

            if (spellGuid == ModBlueprintIds.Spells.DissolutionWave)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.DissolutionWave,
                    "WotrMod_DissolutionWaveProjectile",
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

            if (spellGuid == ModBlueprintIds.Spells.VitriolicSphere)
            {
                return new ProjectileVisuals(
                    ModBlueprintIds.Projectiles.VitriolicSphere,
                    "WotrMod_VitriolicSphereProjectile",
                    SpellEffectTheme.Acid);
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
            return definition.NewSpellGuid == ModBlueprintIds.Spells.BoneSpike ||
                   definition.NewSpellGuid == ModBlueprintIds.Spells.AcidHellfireRay ||
                   definition.NewSpellGuid == ModBlueprintIds.Spells.ColdHellfireRay ||
                   definition.NewSpellGuid == ModBlueprintIds.Spells.ElectricHellfireRay ||
                   definition.NewSpellGuid == ModBlueprintIds.Spells.FireHellfireRay ||
                   definition.NewSpellGuid == ModBlueprintIds.Spells.ShadowHellfireRay;
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
            var tint = GetIconTint(definition.NewSpellGuid);
            if (icon != null)
            {
                return tint.HasValue
                    ? _icons.Tint(icon, definition.InternalName + "Icon", tint.Value.Color, tint.Value.Strength)
                    : icon;
            }

            if (!tint.HasValue)
            {
                return null;
            }

            var source = _blueprints.Require<BlueprintAbility>(
                definition.BaseSpellGuid,
                definition.InternalName + " icon donor");
            return _icons.Tint(source.Icon, definition.InternalName + "Icon", tint.Value.Color, tint.Value.Strength);
        }

        private static MissileIconTint? GetIconTint(string spellGuid)
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

            if (spellGuid == ModBlueprintIds.Spells.CausticBeam)
            {
                return new MissileIconTint(SpellEffectThemes.Acid, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.EmperorsWrath)
            {
                return new MissileIconTint(SpellEffectThemes.Electric, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.ForceRay)
            {
                return new MissileIconTint(SpellEffectThemes.Arcane, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.FrostBlast)
            {
                return new MissileIconTint(SpellEffectThemes.Cold, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.AcidHellfireRay)
            {
                return new MissileIconTint(SpellEffectThemes.Acid, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.ColdHellfireRay)
            {
                return new MissileIconTint(SpellEffectThemes.Cold, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.ElectricHellfireRay)
            {
                return new MissileIconTint(SpellEffectThemes.Electric, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.FireHellfireRay)
            {
                return new MissileIconTint(SpellEffectThemes.Fire, 0.72f);
            }

            if (spellGuid == ModBlueprintIds.Spells.ShadowHellfireRay)
            {
                return new MissileIconTint(SpellEffectThemes.Shadow, 0.72f);
            }

            return null;
        }

        private void PatchBaseGameSpells()
        {
            var fireStorm = _blueprints.Require<BlueprintAbility>(
                GameBlueprintIds.Spells.FireStorm,
                "Fire Storm");

            _blueprints.SetAbilityDisplay(
                fireStorm,
                _localization.Text("wotr_mod.spell.fire_storm.name"),
                _localization.Text("wotr_mod.spell.fire_storm.description"));
            _localization.Put("d763c71a-4ad6-456f-8e62-62e57e59b66b", "3 rounds");

            var patched = SpellModifierUtility.PatchRunActions(fireStorm, action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage?.DamageType.Type != DamageType.Energy ||
                    damage.DamageType.Energy != DamageEnergyType.Fire ||
                    damage.Value == null ||
                    damage.Value.DiceType != DiceType.D6)
                {
                    return 0;
                }

                damage.Value = SpellModifierUtility.CopyDiceValue(damage.Value, DiceType.D8);
                return 1;
            });

            if (patched == 0)
            {
                _logger.Warning("Fire Storm damage dice were not patched.");
            }

            PatchFireStormPersistence(fireStorm);
            PatchMeteorSwarm();
        }

        private void PatchMeteorSwarm()
        {
            var meteorSwarm = _blueprints.Require<BlueprintAbility>(
                GameBlueprintIds.Spells.MeteorSwarm,
                "Meteor Swarm");

            _blueprints.SetAbilityDisplay(
                meteorSwarm,
                _localization.Text("wotr_mod.spell.meteor_swarm.name"),
                _localization.Text("wotr_mod.spell.meteor_swarm.description"));

            var patchedDamage = SpellModifierUtility.PatchRunActions(meteorSwarm, action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage?.DamageType.Type != DamageType.Physical ||
                    damage.Value?.DiceType != DiceType.D6 ||
                    damage.Value.DiceCountValue?.ValueType != ContextValueType.Simple ||
                    damage.Value.DiceCountValue.Value != 8)
                {
                    return 0;
                }

                damage.Value = SpellModifierUtility.CopyDiceValue(damage.Value, DiceType.D12);
                return 1;
            });

            if (patchedDamage == 0)
            {
                _logger.Warning("Meteor Swarm impact damage dice were not patched.");
            }

            var patchedRadius = 0;
            foreach (var targetsAround in _blueprints.GetComponents<AbilityTargetsAround>(meteorSwarm))
            {
                SpellModifierUtility.SetPrivateField(targetsAround, "m_Radius", 45.Feet());
                patchedRadius++;
            }

            if (patchedRadius == 0)
            {
                _logger.Warning("Meteor Swarm radius was not patched.");
            }
        }

        private void PatchFireStormPersistence(BlueprintAbility fireStorm)
        {
            var runAction = fireStorm.GetComponent<AbilityEffectRunAction>();
            var actions = runAction?.Actions?.Actions;
            if (actions == null)
            {
                _logger.Warning("Fire Storm run action was not available for persistence patch.");
                return;
            }

            if (actions.Any(action => action?.name == "$ContextActionSpawnAreaEffect$FireStormPersistent"))
            {
                return;
            }

            var area = EnsureFireStormAreaEffect();
            if (area == null)
            {
                _logger.Warning("Fire Storm persistent area was not available.");
                return;
            }

            var spawn = new ContextActionSpawnAreaEffect
            {
                name = "$ContextActionSpawnAreaEffect$FireStormPersistent",
                DurationValue = Rounds(3),
                OnUnit = false
            };
            _blueprints.SetSpawnAreaEffect(spawn, area);

            runAction.Actions.Actions = actions.Concat(new GameAction[] { spawn }).ToArray();
        }

        private BlueprintAbilityAreaEffect EnsureFireStormAreaEffect()
        {
            var area = _blueprints.Get<BlueprintAbilityAreaEffect>(ModBlueprintIds.AreaEffects.FireStorm);
            if (area == null)
            {
                var sirocco = _blueprints.Require<BlueprintAbility>(
                    GameBlueprintIds.Spells.Sirocco,
                    "Sirocco area donor");
                var donorArea = FindSpawnAreaEffect(sirocco)?.AreaEffect;
                if (donorArea == null)
                {
                    return null;
                }

                area = _blueprints.CloneBlueprint(
                    donorArea,
                    ModBlueprintIds.AreaEffects.FireStorm,
                    "WotrMod_FireStormAreaEffect");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.AreaEffects.FireStorm, area);
            }

            area.Shape = AreaEffectShape.Cylinder;
            area.Size = 40.Feet();
            area.SpellResistance = true;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;

            _blueprints.SetComponents(
                area,
                new SpellDescriptorComponent
                {
                    name = "$SpellDescriptorComponent$FireStormArea",
                    Descriptor = SpellDescriptor.Fire
                },
                new AbilityAreaEffectRunAction
                {
                    name = "$AbilityAreaEffectRunAction$FireStorm",
                    UnitEnter = new ActionList { Actions = Array.Empty<GameAction>() },
                    UnitExit = new ActionList { Actions = Array.Empty<GameAction>() },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            EnemyOnly("$Conditional$FireStormEnemyRound", new GameAction[]
                            {
                                ReflexFireDamage("$ContextActionSavingThrow$FireStormRound")
                            })
                        }
                    }
                });

            var rank = _blueprints.EnsureComponent(
                area,
                () => new ContextRankConfig { name = "$ContextRankConfig$FireStormArea" });
            _blueprints.ConfigureContextRankConfig(rank);
            _blueprints.SetContextRankMaximum(rank, 20);

            return area;
        }

        private static Conditional EnemyOnly(string name, GameAction[] actions)
        {
            return new Conditional
            {
                name = name,
                ConditionsChecker = new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        new ContextConditionIsEnemy
                        {
                            name = "$ContextConditionIsEnemy$FireStorm"
                        }
                    }
                },
                IfTrue = new ActionList { Actions = actions },
                IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
            };
        }

        private static ContextActionSavingThrow ReflexFireDamage(string name)
        {
            return new ContextActionSavingThrow
            {
                name = name,
                Type = SavingThrowType.Reflex,
                Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionDealDamage
                        {
                            name = "$ContextActionDealDamage$FireStormRound",
                            DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Fire),
                            Value = new ContextDiceValue
                            {
                                DiceType = DiceType.D8,
                                DiceCountValue = new ContextValue
                                {
                                    ValueType = ContextValueType.Rank,
                                    ValueRank = Kingmaker.Enums.AbilityRankType.Default
                                },
                                BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 }
                            },
                            IsAoE = true,
                            HalfIfSaved = true,
                            AddAdditionalDamage = false,
                            AddFavoredEnemyDamage = false
                        }
                    }
                }
            };
        }

        private static ContextActionSpawnAreaEffect FindSpawnAreaEffect(BlueprintAbility spell)
        {
            ContextActionSpawnAreaEffect result = null;
            SpellModifierUtility.PatchRunActions(spell, action =>
            {
                if (result == null)
                {
                    result = action as ContextActionSpawnAreaEffect;
                }

                return 0;
            });

            return result;
        }

        private static ContextDurationValue Rounds(int rounds)
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = rounds }
            };
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
