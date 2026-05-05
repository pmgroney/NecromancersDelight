using System;
using Kingmaker.EntitySystem.Stats;

namespace wotr_mod.Classes
{
    internal sealed class CharacterClassPresentationDefinition
    {
        public CharacterClassPresentationDefinition(
            int difficulty,
            StatType[] recommendedAttributes,
            StatType[] notRecommendedAttributes,
            string[] signatureAbilityGuids = null,
            string defaultBuildGuid = null)
        {
            Difficulty = difficulty;
            RecommendedAttributes = recommendedAttributes ?? Array.Empty<StatType>();
            NotRecommendedAttributes = notRecommendedAttributes ?? Array.Empty<StatType>();
            SignatureAbilityGuids = signatureAbilityGuids ?? Array.Empty<string>();
            DefaultBuildGuid = defaultBuildGuid;
        }

        public int Difficulty { get; }
        public StatType[] RecommendedAttributes { get; }
        public StatType[] NotRecommendedAttributes { get; }
        public string[] SignatureAbilityGuids { get; }
        public string DefaultBuildGuid { get; }
    }
}
