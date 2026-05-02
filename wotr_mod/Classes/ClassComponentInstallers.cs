using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal sealed class ClassSpellbookInstaller
    {
        private readonly BlueprintTool _blueprints;

        public ClassSpellbookInstaller(BlueprintTool blueprints)
        {
            _blueprints = blueprints;
        }

        public BlueprintSpellbook EnsureSpellbook(
            CharacterClassDefinition definition,
            BlueprintSpellbook donor,
            BlueprintSpellList spellList)
        {
            var existing = _blueprints.Get<BlueprintSpellbook>(definition.SpellbookGuid);
            if (existing != null)
            {
                return existing;
            }

            var clone = _blueprints.CloneBlueprint(donor, definition.SpellbookGuid, definition.InternalName + "_Spellbook");
            _blueprints.SetSpellbookSpellList(clone, spellList);
            clone.CastingAttribute = definition.CastingStat;
            clone.AllSpellsKnown = true;
            clone.IsArcane = true;
            _blueprints.AddCachedBlueprint(definition.SpellbookGuid, clone);

            return clone;
        }
    }

    internal sealed class ClassProgressionInstaller
    {
        private readonly BlueprintTool _blueprints;

        public ClassProgressionInstaller(BlueprintTool blueprints)
        {
            _blueprints = blueprints;
        }

        public BlueprintProgression EnsureProgression(
            CharacterClassDefinition definition,
            BlueprintProgression donor,
            BlueprintFeatureBase bloodlineFeature)
        {
            var existing = _blueprints.Get<BlueprintProgression>(definition.ProgressionGuid);
            var clone = existing ?? _blueprints.CloneBlueprint(donor, definition.ProgressionGuid, definition.InternalName + "_Progression");

            clone.LevelEntries = (donor.LevelEntries ?? Array.Empty<LevelEntry>())
                .Select(entry =>
                {
                    var copy = new LevelEntry { Level = entry.Level };
                    copy.SetFeatures(CopyFeatures(definition, entry.Features, bloodlineFeature));
                    return copy;
                })
                .ToArray();
            clone.UIGroups = Array.Empty<UIGroup>();

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.ProgressionGuid, clone);
            }

            return clone;
        }

        private List<BlueprintFeatureBase> CopyFeatures(
            CharacterClassDefinition definition,
            IEnumerable<BlueprintFeatureBase> features,
            BlueprintFeatureBase bloodlineFeature)
        {
            var result = new List<BlueprintFeatureBase>();
            foreach (var feature in features ?? Enumerable.Empty<BlueprintFeatureBase>())
            {
                if (feature != null &&
                    definition.RemoveSorcererBloodline &&
                    feature.AssetGuid == BlueprintGuid.Parse(GameBlueprintIds.Selections.SorcererBloodline))
                {
                    if (bloodlineFeature != null)
                    {
                        result.Add(bloodlineFeature);
                    }

                    continue;
                }

                result.Add(feature);
            }

            return result;
        }
    }
}
