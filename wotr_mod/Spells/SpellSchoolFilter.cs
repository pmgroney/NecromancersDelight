using Kingmaker.Blueprints.Classes.Spells;

namespace wotr_mod.Spells
{
    internal static class SpellSchoolFilter
    {
        public static bool IsEvokerSchool(SpellSchool school)
        {
            return school == SpellSchool.Evocation || school == SpellSchool.Conjuration;
        }

        public static bool IsNecromancerSchool(SpellSchool school)
        {
            return school == SpellSchool.Necromancy;
        }
    }
}
