using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    // Recommends a plain (non-parametrized) feature only for characters on one of the
    // given archetypes; neutral for everyone else. Mirrors
    // ScytheWeaponFocusRecommendation's archetype-detection logic, for features that
    // don't take a FeatureParam (e.g. Two-Weapon Fighting, vs. Weapon Focus's weapon category).
    public sealed class ArchetypeFeatureRecommendation : LevelUpRecommendationComponent
    {
        public BlueprintArchetype[] RecommendedArchetypes;
        public BlueprintFeatureSelection[] RecommendedSelections;

        public override RecommendationPriority GetPriority(LevelUpState levelUpState)
        {
            return IsRecommendedArchetype(levelUpState) ? RecommendationPriority.Good : RecommendationPriority.Same;
        }

        public void AddArchetype(BlueprintArchetype archetype)
        {
            if (archetype == null || HasArchetype(archetype))
            {
                return;
            }

            RecommendedArchetypes = (RecommendedArchetypes ?? new BlueprintArchetype[0])
                .Concat(new[] { archetype })
                .ToArray();
        }

        public void AddSelection(BlueprintFeatureSelection selection)
        {
            if (selection == null || HasSelection(selection))
            {
                return;
            }

            RecommendedSelections = (RecommendedSelections ?? new BlueprintFeatureSelection[0])
                .Concat(new[] { selection })
                .ToArray();
        }

        private bool IsRecommendedArchetype(LevelUpState levelUpState)
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
            return RecommendedSelections != null &&
                   selection != null &&
                   RecommendedSelections.Any(candidate =>
                       candidate != null && candidate.AssetGuid == selection.AssetGuid);
        }

        private bool HasArchetype(BlueprintArchetype archetype)
        {
            return RecommendedArchetypes != null &&
                   archetype != null &&
                   RecommendedArchetypes.Any(candidate =>
                       candidate != null && candidate.AssetGuid == archetype.AssetGuid);
        }
    }
}
