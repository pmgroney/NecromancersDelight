using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
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
            if (signatureAbilityGuids != null)
            {
                foreach (var guid in signatureAbilityGuids.Where(guid => !string.IsNullOrWhiteSpace(guid)))
                {
                    BlueprintGuid.Parse(guid);
                }
            }

            if (!string.IsNullOrWhiteSpace(defaultBuildGuid))
            {
                BlueprintGuid.Parse(defaultBuildGuid);
            }

            Difficulty = difficulty;
            RecommendedAttributes = Array.AsReadOnly((recommendedAttributes ?? Array.Empty<StatType>()).ToArray());
            NotRecommendedAttributes = Array.AsReadOnly((notRecommendedAttributes ?? Array.Empty<StatType>()).ToArray());
            SignatureAbilityGuids = Array.AsReadOnly((signatureAbilityGuids ?? Array.Empty<string>()).ToArray());
            DefaultBuildGuid = defaultBuildGuid;
        }

        public int Difficulty { get; }
        public IReadOnlyList<StatType> RecommendedAttributes { get; }
        public IReadOnlyList<StatType> NotRecommendedAttributes { get; }
        public IReadOnlyList<string> SignatureAbilityGuids { get; }
        public string DefaultBuildGuid { get; }
    }
}
