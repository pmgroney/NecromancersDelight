using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Evoker.Archetypes
{
    internal sealed class ArcanistEvokerInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly EvokerInstaller _evoker;

        public ArcanistEvokerInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            EvokerInstaller evoker)
        {
            _blueprints = blueprints;
            _localization = localization;
            _evoker = evoker;
        }

        public BlueprintArchetype Ensure(BlueprintCharacterClass characterClass)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.ArcanistEvoker);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_ArcanistEvokerArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.ArcanistEvoker)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.ArcanistEvoker, archetype);
            }

            var evokerBloodlineSelection = _blueprints.Require<BlueprintFeatureSelection>(
                ModBlueprintIds.Selections.EvokerBloodline,
                "Evoker bloodline selection");
            var arcaneBloodline = _evoker.EnsureEvokerArcaneBloodline(characterClass);
            var arcanistNewArcana = _evoker.EnsureArcanistNewArcanaSelection(characterClass);
            var spellPenetration = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.SpellPenetration,
                "Spell Penetration");
            var combatCasting = _evoker.EnsureEvokerCombatCastingFeature(characterClass);

            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.ArcanistEvokerName),
                _localization.Text(LocalizationIds.Mod.ArcanistEvokerDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, null);
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                CreateFeatureEntries(arcaneBloodline, arcanistNewArcana, spellPenetration),
                CreateRemoveFeatureEntries(evokerBloodlineSelection, combatCasting));
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private static LevelEntry[] CreateFeatureEntries(
            BlueprintProgression arcaneBloodline,
            BlueprintFeatureSelection arcanistNewArcana,
            BlueprintFeature spellPenetration)
        {
            var entries = (arcaneBloodline.LevelEntries ?? Array.Empty<LevelEntry>())
                .Select(entry =>
                {
                    var features = (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                        .Where(feature => feature != null)
                        .ToArray();
                    return features.Length == 0 ? null : CreateLevelEntry(entry.Level, features);
                })
                .Where(entry => entry != null)
                .ToList();

            AddFeatureToLevel(entries, 2, spellPenetration);
            AddFeatureToLevel(entries, 4, arcanistNewArcana);
            return entries
                .OrderBy(entry => entry.Level)
                .ToArray();
        }

        private static LevelEntry[] CreateRemoveFeatureEntries(
            BlueprintFeatureBase evokerBloodlineSelection,
            BlueprintFeature combatCasting)
        {
            return new[]
            {
                CreateLevelEntry(1, evokerBloodlineSelection),
                CreateLevelEntry(2, combatCasting)
            };
        }

        private static void AddFeatureToLevel(
            ICollection<LevelEntry> entries,
            int level,
            BlueprintFeatureBase feature)
        {
            if (feature == null)
            {
                return;
            }

            var entry = entries.FirstOrDefault(levelEntry => levelEntry.Level == level);
            if (entry == null)
            {
                entries.Add(CreateLevelEntry(level, feature));
                return;
            }

            var features = (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>()).ToList();
            if (features.All(existing => existing == null || existing.AssetGuid != feature.AssetGuid))
            {
                features.Add(feature);
                entry.SetFeatures(features);
            }
        }

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }
    }
}
