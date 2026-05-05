using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class SkillPointsPerCharacterLevel :
        UnitFactComponentDelegate,
        IUnitCalculateSkillPointsOnLevelupHandler,
        IUnitSubscriber
    {
        public int SkillPointsPerLevel = 1;

        public void HandleUnitCalculateSkillPointsOnLevelup(
            LevelUpState state,
            ref int extraSkillPoints)
        {
            if (state == null || SkillPointsPerLevel == 0)
            {
                return;
            }

            extraSkillPoints += SkillPointsPerLevel;
        }
    }
}
