using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class GravebladeWeaponFocusRecommendation : ParametrizedLevelUpRecommendationComponent
    {
        public BlueprintArchetype[] GravebladeArchetypes;
        public BlueprintFeatureSelection[] GravebladeSelections;

        public override RecommendationPriority GetPriority(FeatureParam param, LevelUpState levelUpState)
        {
            if (param == null || !param.WeaponCategory.HasValue || !IsGraveblade(levelUpState))
            {
                return RecommendationPriority.Same;
            }

            return param.WeaponCategory.Value == WeaponCategory.Scythe
                ? RecommendationPriority.Good
                : RecommendationPriority.Bad;
        }

        public void AddGravebladeArchetype(BlueprintArchetype archetype)
        {
            if (archetype == null || HasArchetype(archetype))
            {
                return;
            }

            GravebladeArchetypes = (GravebladeArchetypes ?? new BlueprintArchetype[0])
                .Concat(new[] { archetype })
                .ToArray();
        }

        public void AddGravebladeSelection(BlueprintFeatureSelection selection)
        {
            if (selection == null || HasSelection(selection))
            {
                return;
            }

            GravebladeSelections = (GravebladeSelections ?? new BlueprintFeatureSelection[0])
                .Concat(new[] { selection })
                .ToArray();
        }

        private bool IsGraveblade(LevelUpState levelUpState)
        {
            if (levelUpState?.Selections != null &&
                levelUpState.Selections.Any(selectionState =>
                    HasSelection(selectionState?.Selection as BlueprintScriptableObject)))
            {
                return true;
            }

            var selectedArchetype = Game.Instance?.LevelUpController?.TryGetSeletedArchetype();
            if (HasArchetype(selectedArchetype))
            {
                return true;
            }

            var selectedClass = levelUpState?.SelectedClass;
            var classData = selectedClass == null
                ? null
                : levelUpState.Unit?.Progression.GetClassData(selectedClass);
            return classData?.Archetypes != null &&
                   classData.Archetypes.Any(HasArchetype);
        }

        private bool HasSelection(BlueprintScriptableObject selection)
        {
            return GravebladeSelections != null &&
                   selection != null &&
                   GravebladeSelections.Any(candidate =>
                       candidate != null && candidate.AssetGuid == selection.AssetGuid);
        }

        private bool HasArchetype(BlueprintArchetype archetype)
        {
            return GravebladeArchetypes != null &&
                   archetype != null &&
                   GravebladeArchetypes.Any(candidate =>
                       candidate != null && candidate.AssetGuid == archetype.AssetGuid);
        }
    }
}
