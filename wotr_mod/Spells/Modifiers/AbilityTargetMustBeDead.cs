using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class AbilityTargetMustBeDead : BlueprintComponent, IAbilityTargetRestriction
    {
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
        {
            var unit = target.Unit;
            return unit?.Descriptor?.State != null && unit.Descriptor.State.IsDead;
        }

        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
        {
            return "Target must be dead.";
        }
    }
}
