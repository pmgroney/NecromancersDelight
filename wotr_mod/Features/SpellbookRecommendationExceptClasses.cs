using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class SpellbookRecommendationExceptClasses : LevelUpRecommendationComponent
    {
        public BlueprintCharacterClass[] NotRecommendedClasses;
        public bool RecommendSpellbookClasses = true;

        public override RecommendationPriority GetPriority(LevelUpState levelUpState)
        {
            var selectedClass = levelUpState?.SelectedClass;
            if (selectedClass == null)
            {
                return RecommendationPriority.Same;
            }

            return IsNotRecommendedClass(selectedClass)
                ? RecommendationPriority.Bad
                : RecommendSpellbookClasses && selectedClass.Spellbook != null
                    ? RecommendationPriority.Good
                    : RecommendationPriority.Same;
        }

        private bool IsNotRecommendedClass(BlueprintCharacterClass characterClass)
        {
            return characterClass != null &&
                   NotRecommendedClasses != null &&
                   NotRecommendedClasses.Any(c => c != null && c.AssetGuid == characterClass.AssetGuid);
        }
    }
}
