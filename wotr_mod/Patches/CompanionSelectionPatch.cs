using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CompanionSelectionPatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly CompanionSelectionTarget[] _targets;

        public CompanionSelectionPatch(
            BlueprintTool blueprints,
            LocalizationTool localization,
            params CompanionSelectionTarget[] targets)
        {
            _blueprints = blueprints;
            _localization = localization;
            _targets = targets ?? new CompanionSelectionTarget[0];
        }

        public string Name => "Companion Selection";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            if (_targets.Length == 0)
            {
                return;
            }

            var oldSylvanCompanionSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SylvanCompanion,
                "Sylvan companion selection");
            var companionSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SylvanAnimalCompanion,
                "Sylvan animal companion selection");
            var modCompanionSelection = EnsureCompanionPetSelection(companionSelection);
            var companionProgression = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.SylvanAnimalCompanion,
                "Sylvan animal companion progression");

            foreach (var target in _targets)
            {
                var characterClass = _blueprints.Require<BlueprintCharacterClass>(target.ClassGuid, target.Name + " class");
                var progression = _blueprints.Require<BlueprintProgression>(target.ProgressionGuid, target.Name + " progression");

                RemoveFeatureFromLevel(progression, 1, oldSylvanCompanionSelection);
                RemoveFeatureFromLevel(progression, 1, companionSelection);
                _blueprints.AddFeatureToLevel(progression, 1, modCompanionSelection);
                _blueprints.AddProgressionUiGroup(progression, modCompanionSelection);
                _blueprints.SetProgressionClasses(modCompanionSelection, characterClass);
                _blueprints.AddScalingClass(companionProgression, characterClass);
            }
        }

        private BlueprintFeatureSelection EnsureCompanionPetSelection(BlueprintFeatureSelection source)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.CompanionPet);
            if (selection == null)
            {
                selection = _blueprints.CloneBlueprint(
                    source,
                    ModBlueprintIds.Selections.CompanionPet,
                    "WotrMod_CompanionPetSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.CompanionPet, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.CompanionPetName),
                _localization.Text(LocalizationIds.Mod.CompanionPetDescription));
            _blueprints.SetFeatureSelectionFeatures(selection, Array.Empty<BlueprintFeature>());
            _blueprints.SetFeatureSelectionAllFeatures(selection, _blueprints.GetFeatureSelectionAllFeatures(source));
            _blueprints.SetComponents(
                selection,
                _blueprints.GetComponents<BlueprintComponent>(source)
                    .Select(component => _blueprints.CloneComponent(component))
                    .ToArray());

            return selection;
        }

        private static void RemoveFeatureFromLevel(BlueprintProgression progression, int level, BlueprintFeatureBase feature)
        {
            var entry = progression.LevelEntries?.FirstOrDefault(item => item.Level == level);
            if (entry == null || feature == null)
            {
                return;
            }

            var features = entry.Features
                .Where(existing => existing == null || existing.AssetGuid != feature.AssetGuid)
                .ToArray();
            if (features.Length != entry.Features.Count)
            {
                entry.SetFeatures(features);
            }
        }

        internal sealed class CompanionSelectionTarget
        {
            public readonly string Name;
            public readonly string ClassGuid;
            public readonly string ProgressionGuid;

            public CompanionSelectionTarget(string name, string classGuid, string progressionGuid)
            {
                Name = name;
                ClassGuid = classGuid;
                ProgressionGuid = progressionGuid;
            }
        }
    }
}
