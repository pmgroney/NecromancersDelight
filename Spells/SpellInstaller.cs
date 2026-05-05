using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Abilities.Blueprints;
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

            foreach (var definition in SpellRegistry.GetAll())
            {
                var spell = EnsureSpell(definition);
                if (IsGrantedOnlySpell(definition))
                {
                    _blueprints.RemoveSpellFromList(wizardList, spell);
                    continue;
                }

                _blueprints.AddSpellToList(wizardList, spell, definition.SpellLevel);
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

            return clone;
        }

        private static bool IsGrantedOnlySpell(SpellDefinition definition)
        {
            return definition.NewSpellGuid == ModBlueprintIds.Spells.BoneSpike;
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
    }
}
