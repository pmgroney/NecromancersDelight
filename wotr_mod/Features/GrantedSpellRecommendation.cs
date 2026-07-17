using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class GrantedSpellRecommendation : LevelUpRecommendationComponent
    {
        public BlueprintGuid[] ExemptArchetypeGuids;

        public override RecommendationPriority GetPriority(LevelUpState levelUpState)
        {
            return IsExemptArchetypeActive(levelUpState) ? RecommendationPriority.Same : RecommendationPriority.Bad;
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
    }
}
