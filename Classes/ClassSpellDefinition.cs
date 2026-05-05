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
