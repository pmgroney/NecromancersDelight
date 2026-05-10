using Kingmaker.Blueprints;
using Kingmaker.RuleSystem;

namespace wotr_mod.Classes
{
    internal sealed class ClassChassisDefinition
    {
        public ClassChassisDefinition(DiceType? hitDie = null, string baseAttackBonusGuid = null, int? skillPoints = null)
        {
            if (!string.IsNullOrWhiteSpace(baseAttackBonusGuid))
            {
                BlueprintGuid.Parse(baseAttackBonusGuid);
            }

            HitDie = hitDie;
            BaseAttackBonusGuid = baseAttackBonusGuid;
            SkillPoints = skillPoints;
        }

        public DiceType? HitDie { get; }
        public string BaseAttackBonusGuid { get; }
        public int? SkillPoints { get; }
    }
}
