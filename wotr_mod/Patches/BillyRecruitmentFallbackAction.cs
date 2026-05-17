using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Parts;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class BillyRecruitmentLogAction : GameAction
    {
        public string Stage;

        public override void RunAction()
        {
            BillyRecruitmentDiagnostics.Log(Stage ?? name ?? "unknown");
        }

        public override string GetCaption()
        {
            return "Log Billy recruitment state";
        }
    }

    internal sealed class BillyRecruitmentCleanupAction : GameAction
    {
        public string Stage;

        public override void RunAction()
        {
            BillyRecruitmentDiagnostics.RemoveStaleCompanionStandIns(Stage ?? name ?? "recruitment cleanup");
        }

        public override string GetCaption()
        {
            return "Remove stale Billy companion stand-ins";
        }
    }

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

            BillyRecruitmentDiagnostics.Log("fallback start");
            if (player.Party.Any(IsBilly))
            {
                Main.Log("Billy recruitment fallback skipped: Billy is already in Player.Party.");
                return;
            }

            var billy = player.AllCharacters.FirstOrDefault(IsBilly);
            if (billy == null)
            {
                Main.Warning("Billy recruitment fallback skipped: no Billy unit exists in Player.AllCharacters.");
                return;
            }

            try
            {
                Main.Log("Billy recruitment fallback attaching: " + BillyRecruitmentDiagnostics.DescribeUnit(billy));
                player.AttachPartyMember(billy);
                player.FixPartyAfterChange(false);
                BillyRecruitmentDiagnostics.Log("fallback after attach");
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

    internal static class BillyRecruitmentDiagnostics
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid BillyStandInGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.BillyShieldMazeStandIn);

        public static void Log(string stage)
        {
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                if (player == null)
                {
                    Main.Warning($"Billy recruitment diagnostic [{stage}]: player is unavailable.");
                    return;
                }

                Main.Log(
                    $"Billy recruitment diagnostic [{stage}]: area={game.CurrentlyLoadedArea?.AssetGuid}, " +
                    $"party={Count(player.Party)}, partyAndPets={Count(player.PartyAndPets)}, " +
                    $"active={Count(player.ActiveCompanions)}, remote={Count(player.RemoteCompanions)}, " +
                    $"allCharacters={Count(player.AllCharacters)}.");
                Main.Log(DescribeRelevantBillyUnits("Party", player.Party));
                Main.Log(DescribeRelevantBillyUnits("PartyAndPets", player.PartyAndPets));
                Main.Log(DescribeRelevantBillyUnits("ActiveCompanions", player.ActiveCompanions));
                Main.Log(DescribeRelevantBillyUnits("RemoteCompanions", player.RemoteCompanions));
                Main.Log(DescribeRelevantBillyUnits("AllCharacters", player.AllCharacters));
                Main.Log(DescribeRelevantBillyUnits(
                    "LoadedArea",
                    game.LoadedAreaState?.AllEntityData.OfType<UnitEntityData>()));
                Main.Log(DescribeRelevantBillyUnits(
                    "CrossScene",
                    game.State?.PlayerState?.CrossSceneState?.AllEntityData.OfType<UnitEntityData>()));
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy recruitment diagnostic [{stage}] failed: {ex}");
            }
        }

        public static int RemoveStaleCompanionStandIns(string stage)
        {
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                if (player == null)
                {
                    Main.Warning($"Billy recruitment cleanup [{stage}] skipped: player is unavailable.");
                    return 0;
                }

                var removed = 0;
                removed += RemoveStaleCompanionStandIns(game.LoadedAreaState?.AllEntityData.OfType<UnitEntityData>());
                removed += RemoveStaleCompanionStandIns(
                    game.State?.PlayerState?.CrossSceneState?.AllEntityData.OfType<UnitEntityData>());

                if (removed > 0)
                {
                    player.InvalidateCharacterLists();
                    Main.Log($"Billy recruitment cleanup [{stage}] removed {removed} stale companion-blueprint stand-in unit(s).");
                }
                else
                {
                    Main.Log($"Billy recruitment cleanup [{stage}] found no stale companion-blueprint stand-ins.");
                }

                return removed;
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy recruitment cleanup [{stage}] failed: {ex}");
                return 0;
            }
        }

        public static string DescribeUnit(UnitEntityData unit)
        {
            if (unit == null)
            {
                return "<null>";
            }

            var state = unit.Get<UnitPartCompanion>()?.State.ToString() ?? "<no companion part>";
            var blueprint = unit.Descriptor?.Blueprint?.AssetGuid.ToString() ?? "<no blueprint>";
            return $"kind={DescribeKind(unit)}, blueprint={blueprint}, companionState={state}, isInGame={unit.IsInGame}, " +
                   $"isPlayerFaction={unit.IsPlayerFaction}, hasView={unit.View != null}, " +
                   $"position=({unit.Position.x:0.###}, {unit.Position.y:0.###}, {unit.Position.z:0.###}), " +
                   $"orientation={unit.Orientation:0.###}";
        }

        private static string DescribeRelevantBillyUnits(string label, IEnumerable<UnitEntityData> units)
        {
            var billyUnits = units?
                .Where(IsBillyOrStandIn)
                .Select(DescribeUnit)
                .ToArray() ?? Array.Empty<string>();

            return billyUnits.Length == 0
                ? $"Billy recruitment diagnostic {label}: none."
                : $"Billy recruitment diagnostic {label}: {string.Join(" | ", billyUnits)}";
        }

        private static int RemoveStaleCompanionStandIns(IEnumerable<UnitEntityData> units)
        {
            var staleUnits = units?
                .Where(IsStaleCompanionStandIn)
                .ToArray() ?? Array.Empty<UnitEntityData>();
            var removed = 0;
            foreach (var unit in staleUnits)
            {
                Main.Log("Billy recruitment cleanup removing stale unit: " + DescribeUnit(unit));
                unit.HoldingState?.RemoveEntityData(unit);
                removed++;
            }

            return removed;
        }

        private static int Count(ICollection<UnitEntityData> units)
        {
            return units?.Count ?? 0;
        }

        private static int Count(IEnumerable<UnitEntityData> units)
        {
            return units?.Count() ?? 0;
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint?.AssetGuid == BillyGuid;
        }

        private static bool IsBillyStandIn(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint != null
                   && unit.Descriptor.Blueprint.AssetGuid == BillyStandInGuid;
        }

        private static bool IsBillyOrStandIn(UnitEntityData unit)
        {
            return IsBilly(unit) || IsBillyStandIn(unit);
        }

        private static bool IsStaleCompanionStandIn(UnitEntityData unit)
        {
            return IsBilly(unit) && !HasRosterCompanionState(unit);
        }

        private static bool HasRosterCompanionState(UnitEntityData unit)
        {
            var state = unit?.Get<UnitPartCompanion>()?.State;
            return state == CompanionState.Remote
                   || state == CompanionState.InParty
                   || state == CompanionState.InPartyDetached
                   || state == CompanionState.ExCompanion;
        }

        private static string DescribeKind(UnitEntityData unit)
        {
            if (IsBilly(unit))
            {
                return "companion";
            }

            return IsBillyStandIn(unit) ? "stand-in" : "other";
        }
    }
}
