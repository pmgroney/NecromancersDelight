using Kingmaker.RuleSystem;

namespace wotr_mod.Classes
{
    internal sealed class ClassChassisDefinition
    {
        public ClassChassisDefinition(DiceType? hitDie = null, string baseAttackBonusGuid = null)
        {
            HitDie = hitDie;
            BaseAttackBonusGuid = baseAttackBonusGuid;
        }

        public DiceType? HitDie { get; }
        public string BaseAttackBonusGuid { get; }
    }
}
