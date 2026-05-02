using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CompanionSelectionPatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly CompanionSelectionTarget[] _targets;

        public CompanionSelectionPatch(BlueprintTool blueprints, params CompanionSelectionTarget[] targets)
        {
            _blueprints = blueprints;
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

            var companionSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SylvanCompanion,
                "Sylvan companion selection");
            var companionProgression = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.SylvanAnimalCompanion,
                "Sylvan animal companion progression");

            foreach (var target in _targets)
            {
                var characterClass = _blueprints.Require<BlueprintCharacterClass>(target.ClassGuid, target.Name + " class");
                var progression = _blueprints.Require<BlueprintProgression>(target.ProgressionGuid, target.Name + " progression");

                _blueprints.AddFeatureToLevel(progression, 1, companionSelection);
                _blueprints.AddScalingClass(companionProgression, characterClass);
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
