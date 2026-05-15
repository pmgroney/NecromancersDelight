using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class BillyRecruitedCondition : Condition
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid ShieldMazeGuid = BlueprintGuid.Parse(GameBlueprintIds.Areas.PrologueLabyrinth);

        protected override bool CheckCondition()
        {
            var game = Game.Instance;
            var player = game?.Player;
            if (player == null)
            {
                return false;
            }

            if (IsBillyRecruitedFlagUnlocked(player))
            {
                return true;
            }

            if (game.CurrentlyLoadedArea?.AssetGuid == ShieldMazeGuid)
            {
                return false;
            }

            return player.PartyAndPets
                       .Concat(player.ActiveCompanions)
                       .Concat(player.RemoteCompanions)
                       .Where(unit => unit != null)
                       .Distinct()
                       .Any(IsBilly)
                   || player.AllCharacters
                       .Where(unit => unit != null)
                       .Distinct()
                       .Any(unit => IsBilly(unit) && HasRosterCompanionState(unit));
        }

        protected override string GetConditionCaption()
        {
            return "Billy is recruited";
        }

        private static bool IsBillyRecruitedFlagUnlocked(Player player)
        {
            var flag = ResourcesLibrary.TryGetBlueprint<BlueprintUnlockableFlag>(
                ModBlueprintIds.Flags.BillyRecruited);
            return flag != null && player.UnlockableFlags.IsUnlocked(flag);
        }

        private static bool HasRosterCompanionState(UnitEntityData unit)
        {
            var state = unit?.Get<UnitPartCompanion>()?.State;
            return state == CompanionState.Remote
                   || state == CompanionState.InParty
                   || state == CompanionState.InPartyDetached
                   || state == CompanionState.ExCompanion;
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint?.AssetGuid == BillyGuid;
        }
    }
}
