using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Class.LevelUp;
using wotr_mod.Infrastructure;

namespace wotr_mod.Features
{
    public sealed class GrantedSpellRecommendation : LevelUpRecommendationComponent
    {
        public BlueprintGuid[] NotRecommendedClassGuids;
        public BlueprintGuid[] ExemptArchetypeGuids;

        public void AddNotRecommendedClass(BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return;
            }

            NotRecommendedClassGuids = AddGuid(NotRecommendedClassGuids, characterClass.AssetGuid);
        }

        public void AddNotRecommendedClass(string classGuid)
        {
            NotRecommendedClassGuids = AddGuid(NotRecommendedClassGuids, classGuid);
        }

        public void AddExemptArchetype(string archetypeGuid)
        {
            ExemptArchetypeGuids = AddGuid(ExemptArchetypeGuids, archetypeGuid);
        }

        public override RecommendationPriority GetPriority(LevelUpState levelUpState)
        {
            if (!IsNotRecommendedClass(levelUpState?.SelectedClass))
            {
                return RecommendationPriority.Same;
            }

            return IsExemptArchetypeActive(levelUpState) ? RecommendationPriority.Same : RecommendationPriority.Bad;
        }

        private bool IsNotRecommendedClass(BlueprintCharacterClass characterClass)
        {
            if (NotRecommendedClassGuids == null || NotRecommendedClassGuids.Length == 0)
            {
                return true;
            }

            return characterClass != null &&
                   NotRecommendedClassGuids.Any(guid => guid == characterClass.AssetGuid);
        }

        private bool IsExemptArchetypeActive(LevelUpState levelUpState)
        {
            var selectedArchetype = Game.Instance?.LevelUpController?.TryGetSeletedArchetype();
            if (IsExempt(selectedArchetype))
            {
                return true;
            }

            var selectedClass = levelUpState?.SelectedClass;
            var classData = selectedClass == null
                ? null
                : levelUpState.Unit?.Progression.GetClassData(selectedClass);
            return classData?.Archetypes != null && classData.Archetypes.Any(IsExempt);
        }

        private bool IsExempt(BlueprintArchetype archetype)
        {
            return archetype != null &&
                   ExemptArchetypeGuids != null &&
                   ExemptArchetypeGuids.Any(guid => guid == archetype.AssetGuid);
        }

        private static BlueprintGuid[] AddGuid(BlueprintGuid[] guids, string guid)
        {
            return AddGuid(guids, BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(guid)));
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
