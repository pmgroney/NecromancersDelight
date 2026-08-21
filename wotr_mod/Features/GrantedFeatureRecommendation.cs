using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class GrantedFeatureRecommendation : LevelUpRecommendationComponent
    {
        public BlueprintGuid[] NotRecommendedClassGuids;
        public BlueprintGuid[] NotRecommendedArchetypeGuids;
        public BlueprintGuid[] ExemptArchetypeGuids;

        public void AddNotRecommendedClass(BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return;
            }

            NotRecommendedClassGuids = AddGuid(NotRecommendedClassGuids, characterClass.AssetGuid);
        }

        public void AddNotRecommendedArchetype(string archetypeGuid)
        {
            NotRecommendedArchetypeGuids = AddGuid(NotRecommendedArchetypeGuids, archetypeGuid);
        }

        public void AddExemptArchetype(string archetypeGuid)
        {
            ExemptArchetypeGuids = AddGuid(ExemptArchetypeGuids, archetypeGuid);
        }

        public override RecommendationPriority GetPriority(LevelUpState levelUpState)
        {
            if (!IsNotRecommendedClass(levelUpState?.SelectedClass) && !IsNotRecommendedArchetypeActive(levelUpState))
            {
                return RecommendationPriority.Same;
            }

            return IsExemptArchetypeActive(levelUpState) ? RecommendationPriority.Same : RecommendationPriority.Bad;
        }

        private bool IsNotRecommendedClass(BlueprintCharacterClass characterClass)
        {
            return characterClass != null &&
                   NotRecommendedClassGuids != null &&
                   NotRecommendedClassGuids.Any(guid => guid == characterClass.AssetGuid);
        }

        private bool IsNotRecommendedArchetypeActive(LevelUpState levelUpState)
        {
            return IsMatchingArchetypeActive(levelUpState, NotRecommendedArchetypeGuids);
        }

        private bool IsExemptArchetypeActive(LevelUpState levelUpState)
        {
            return IsMatchingArchetypeActive(levelUpState, ExemptArchetypeGuids);
        }

        private bool IsMatchingArchetypeActive(LevelUpState levelUpState, BlueprintGuid[] archetypeGuids)
        {
            var selectedArchetype = Game.Instance?.LevelUpController?.TryGetSeletedArchetype();
            if (IsMatch(selectedArchetype, archetypeGuids))
            {
                return true;
            }

            var selectedClass = levelUpState?.SelectedClass;
            var classData = selectedClass == null
                ? null
                : levelUpState.Unit?.Progression.GetClassData(selectedClass);
            return classData?.Archetypes != null &&
                   classData.Archetypes.Any(archetype => IsMatch(archetype, archetypeGuids));
        }

        private static bool IsMatch(BlueprintArchetype archetype, BlueprintGuid[] archetypeGuids)
        {
            return archetype != null &&
                   archetypeGuids != null &&
                   archetypeGuids.Any(guid => guid == archetype.AssetGuid);
        }

        private static BlueprintGuid[] AddGuid(BlueprintGuid[] guids, string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return guids ?? new BlueprintGuid[0];
            }

            return AddGuid(guids, BlueprintGuid.Parse(guid));
        }

        private static BlueprintGuid[] AddGuid(BlueprintGuid[] guids, BlueprintGuid guid)
        {
            if (guids != null && guids.Any(existing => existing == guid))
            {
                return guids;
            }

            return (guids ?? new BlueprintGuid[0])
                .Concat(new[] { guid })
                .ToArray();
        }
    }
}
