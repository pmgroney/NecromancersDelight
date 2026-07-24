using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Evoker.Archetypes
{
    internal sealed class ShadowbornInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly EvokerInstaller _evoker;

        public ShadowbornInstaller(
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
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Shadowborn);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_EvokerShadowbornArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Shadowborn)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Shadowborn, archetype);
            }

            var evokerBloodlineSelection = _blueprints.Require<BlueprintFeatureSelection>(
                ModBlueprintIds.Selections.EvokerBloodline,
                "Evoker bloodline selection");
            var shadowbornBloodline = _evoker.EnsureShadowbornBloodline(characterClass);
            var shadowbornLivingGhost = _evoker.EnsureShadowbornLivingGhostFeature(characterClass);
            var shadowbornShadowMastery = _evoker.EnsureShadowbornShadowMasteryFeature(characterClass);

            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.ShadowbornName),
                _localization.Text(LocalizationIds.Mod.ShadowbornDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, null);
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                CreateFeatureEntries(
                    shadowbornBloodline,
                    shadowbornLivingGhost,
                    shadowbornShadowMastery),
                CreateRemoveFeatureEntries(evokerBloodlineSelection));
            if (characterClass.Progression != null)
            {
                _blueprints.AddProgressionUiGroup(characterClass.Progression, shadowbornLivingGhost);
                _blueprints.AddProgressionUiGroup(characterClass.Progression, shadowbornShadowMastery);
            }

            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private static LevelEntry[] CreateRemoveFeatureEntries(
            BlueprintFeatureBase evokerBloodlineSelection)
        {
            return new[]
            {
                CreateLevelEntry(1, evokerBloodlineSelection)
            };
        }

        private static LevelEntry[] CreateFeatureEntries(
            BlueprintProgression shadowbornBloodline,
            BlueprintFeature shadowbornLivingGhost,
            BlueprintFeature shadowbornShadowMastery)
        {
            var entries = (shadowbornBloodline.LevelEntries ?? Array.Empty<LevelEntry>())
                .Select(entry =>
                {
                    var features = (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                        .Where(feature => feature != null)
                        .ToArray();
                    return features.Length == 0 ? null : CreateLevelEntry(entry.Level, features);
                })
                .Where(entry => entry != null)
                .ToList();

            AddFeatureToLevel(entries, 6, shadowbornShadowMastery);
            AddFeatureToLevel(entries, 20, shadowbornLivingGhost);
            return entries
                .OrderBy(entry => entry.Level)
                .ToArray();
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
