using Kingmaker.EntitySystem.Stats;

namespace wotr_mod.Classes
{
    internal sealed class CharacterClassDefinition
    {
        public CharacterClassDefinition(
            string internalName,
            string classGuid,
            string progressionGuid,
            string spellbookGuid,
            string spellListGuid,
            string displayNameKey,
            string descriptionKey,
            StatType castingStat,
            bool useEvokerBloodlines,
            bool removeSorcererBloodline,
            bool useUndeadBloodline = false,
            bool useNecromancerBloodline = false,
            ClassChassisDefinition chassis = null,
            CharacterClassPresentationDefinition presentation = null)
        {
            InternalName = internalName;
            ClassGuid = classGuid;
            ProgressionGuid = progressionGuid;
            SpellbookGuid = spellbookGuid;
            SpellListGuid = spellListGuid;
            DisplayNameKey = displayNameKey;
            DescriptionKey = descriptionKey;
            CastingStat = castingStat;
            UseEvokerBloodlines = useEvokerBloodlines;
            RemoveSorcererBloodline = removeSorcererBloodline;
            UseUndeadBloodline = useUndeadBloodline;
            UseNecromancerBloodline = useNecromancerBloodline;
            Chassis = chassis;
            Presentation = presentation;
        }

        public string InternalName { get; }
        public string ClassGuid { get; }
        public string ProgressionGuid { get; }
        public string SpellbookGuid { get; }
        public string SpellListGuid { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public StatType CastingStat { get; }
        public bool UseEvokerBloodlines { get; }
        public bool RemoveSorcererBloodline { get; }
        public bool UseUndeadBloodline { get; }
        public bool UseNecromancerBloodline { get; }
        public ClassChassisDefinition Chassis { get; }
        public CharacterClassPresentationDefinition Presentation { get; }
    }
}
