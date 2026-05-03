using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace wotr_mod.Features
{
    public sealed class SelectedRaceStatBonus :
        UnitFactComponentDelegate,
        IUnitLevelUpHandler,
        IUnitSubscriber
    {
        public ModifierDescriptor Descriptor = ModifierDescriptor.Racial;
        public int Value = 2;

        public void HandleUnitBeforeLevelUp(UnitEntityData unit)
        {
        }

        public void HandleUnitAfterLevelUp(UnitEntityData unit, LevelUpController controller)
        {
            if (Value == 0 ||
                unit == null ||
                unit != Owner ||
                controller == null ||
                controller.State == null ||
                !controller.State.SelectedRaceStat.HasValue)
            {
                return;
            }

            Apply(controller.State.SelectedRaceStat.Value);
        }

        protected override void OnTurnOff()
        {
            RemoveModifiers();
        }

        private void Apply(StatType stat)
        {
            RemoveModifiers();
            Owner.Descriptor.Stats.GetStat(stat)?.AddModifierUnique(Value, Runtime, Descriptor);
        }

        private void RemoveModifiers()
        {
            if (Owner == null || Owner.Descriptor == null || Owner.Descriptor.Stats == null)
            {
                return;
            }

            foreach (var stat in Owner.Descriptor.Stats.AllStats)
            {
                stat?.RemoveModifiersFrom(Runtime);
            }
        }
    }
}
