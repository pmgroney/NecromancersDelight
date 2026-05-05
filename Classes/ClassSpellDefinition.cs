using System;
using Kingmaker.Blueprints;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal sealed class ClassSpellDefinition
    {
        public ClassSpellDefinition(
            string displayName,
            string spellGuid,
            int spellLevel,
            SelectionRecommendation? recommendation = null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(spellGuid))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(spellGuid));
            }

            if (spellLevel < 0 || spellLevel > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(spellLevel), "Spell level must be between 0 and 9.");
            }

            BlueprintGuid.Parse(spellGuid);

            DisplayName = displayName;
            SpellGuid = spellGuid;
            SpellLevel = spellLevel;
            Recommendation = recommendation;
        }

        public string DisplayName { get; }
        public string SpellGuid { get; }
        public int SpellLevel { get; }
        public SelectionRecommendation? Recommendation { get; }
    }
}
