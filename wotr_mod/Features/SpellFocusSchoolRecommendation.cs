using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class SpellFocusSchoolRecommendation : ParametrizedLevelUpRecommendationComponent
    {
        public BlueprintCharacterClass[] EvocationClasses;
        public BlueprintCharacterClass[] NecromancyClasses;

        public override RecommendationPriority GetPriority(FeatureParam param, LevelUpState levelUpState)
        {
            var selectedClass = levelUpState?.SelectedClass;
            if (selectedClass == null)
            {
                return RecommendationPriority.Same;
            }

            if (HasClass(EvocationClasses, selectedClass))
            {
                return param.SpellSchool == SpellSchool.Evocation
                    ? RecommendationPriority.Good
                    : RecommendationPriority.Same;
            }

            if (HasClass(NecromancyClasses, selectedClass))
            {
                return param.SpellSchool == SpellSchool.Necromancy
                    ? RecommendationPriority.Good
                    : RecommendationPriority.Same;
            }

            return selectedClass.Spellbook != null
                ? RecommendationPriority.Good
                : RecommendationPriority.Same;
        }

        public void AddRecommendedClass(BlueprintCharacterClass characterClass, SpellSchool school)
        {
            if (school == SpellSchool.Evocation)
            {
                EvocationClasses = AddUnique(EvocationClasses, characterClass);
            }
            else if (school == SpellSchool.Necromancy)
            {
                NecromancyClasses = AddUnique(NecromancyClasses, characterClass);
            }
        }

        private static bool HasClass(BlueprintCharacterClass[] classes, BlueprintCharacterClass characterClass)
        {
            return classes != null &&
                   characterClass != null &&
                   classes.Any(c => c != null && c.AssetGuid == characterClass.AssetGuid);
        }

        private static BlueprintCharacterClass[] AddUnique(
            BlueprintCharacterClass[] classes,
            BlueprintCharacterClass characterClass)
        {
            if (characterClass == null || HasClass(classes, characterClass))
            {
                return classes ?? new BlueprintCharacterClass[0];
            }

            return (classes ?? new BlueprintCharacterClass[0])
                .Concat(new[] { characterClass })
                .ToArray();
        }
    }
}
