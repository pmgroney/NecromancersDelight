using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class BillyRecruitmentFallbackAction : GameAction
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);

        public override void RunAction()
        {
            var player = Game.Instance?.Player;
            if (player == null)
            {
                Main.Warning("Billy recruitment fallback failed: player is unavailable.");
                return;
            }

            if (player.Party.Any(IsBilly))
            {
                return;
            }

            var billy = player.AllCharacters.FirstOrDefault(IsBilly);
            if (billy == null)
            {
                return;
            }

            try
            {
                player.AttachPartyMember(billy);
                player.FixPartyAfterChange(false);
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy recruitment fallback failed: {ex}");
            }
        }

        public override string GetCaption()
        {
            return "Attach Billy if Recruit left him in roster only";
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint?.AssetGuid == BillyGuid;
        }
    }
}
