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
    public sealed class ScytheWeaponFocusRecommendation : ParametrizedLevelUpRecommendationComponent
    {
        public BlueprintCharacterClass[] RecommendedClasses;
        public BlueprintArchetype[] RecommendedArchetypes;
        public BlueprintFeatureSelection[] RecommendedSelections;

        public override RecommendationPriority GetPriority(FeatureParam param, LevelUpState levelUpState)
        {
            if (param == null || !param.WeaponCategory.HasValue || !IsRecommendedCharacter(levelUpState))
            {
                return RecommendationPriority.Same;
            }

            return param.WeaponCategory.Value == WeaponCategory.Scythe
                ? RecommendationPriority.Good
                : RecommendationPriority.Same;
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

        public void AddRecommendedClass(BlueprintCharacterClass characterClass)
        {
            if (characterClass == null || HasClass(characterClass))
            {
                return;
            }

            RecommendedClasses = (RecommendedClasses ?? new BlueprintCharacterClass[0])
                .Concat(new[] { characterClass })
                .ToArray();
        }

        private bool IsRecommendedCharacter(LevelUpState levelUpState)
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
            if (HasClass(selectedClass))
            {
                return true;
            }

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

        private bool HasClass(BlueprintCharacterClass characterClass)
        {
            return RecommendedClasses != null &&
                   characterClass != null &&
                   RecommendedClasses.Any(candidate =>
                       candidate != null && candidate.AssetGuid == characterClass.AssetGuid);
        }
    }
}
