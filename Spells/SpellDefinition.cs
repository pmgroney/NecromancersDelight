using Kingmaker.Blueprints.Classes.Spells;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Spells
{
    internal sealed class SpellDefinition
    {
        public SpellDefinition(
            string baseSpellGuid,
            string newSpellGuid,
            string internalName,
            int spellLevel,
            SpellSchool school,
            string displayNameKey,
            string descriptionKey,
            string iconPath,
            ISpellModifier modifier)
        {
            BaseSpellGuid = baseSpellGuid;
            NewSpellGuid = newSpellGuid;
            InternalName = internalName;
            SpellLevel = spellLevel;
            School = school;
            DisplayNameKey = displayNameKey;
            DescriptionKey = descriptionKey;
            IconPath = iconPath;
            Modifier = modifier;
        }

        public string BaseSpellGuid { get; }
        public string NewSpellGuid { get; }
        public string InternalName { get; }
        public int SpellLevel { get; }
        public SpellSchool School { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public string IconPath { get; }
        public ISpellModifier Modifier { get; }
        public bool IsNecromancy => School == SpellSchool.Necromancy;
    }
}
