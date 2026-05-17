using System;
using System.Globalization;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class BillyPlacementPatch : IGamePatch, IAreaLoadHandler
    {
        private const float ShieldMazeSpawnX = 133.47f;
        private const float ShieldMazeSpawnY = 40f;
        private const float ShieldMazeSpawnZ = 133.75f;
        private const float ShieldMazeSpawnOrientation = 5f;
        private const float DefendersHeartSpawnX = -82f;
        private const float DefendersHeartSpawnY = 40f;
        private const float DefendersHeartSpawnZ = -7f;
        private const float DefendersHeartSpawnOrientation = 0f;
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid BillyStandInGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.BillyShieldMazeStandIn);
        private static readonly BlueprintGuid PrologueLabyrinthGuid = BlueprintGuid.Parse(GameBlueprintIds.Areas.PrologueLabyrinth);
        private static readonly BlueprintGuid DefendersHeartGuid = BlueprintGuid.Parse(GameBlueprintIds.Areas.DefendersHeart);

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private bool _canPlaceBilly;

        public BillyPlacementPatch(BlueprintTool blueprints, UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Billy Placement";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            _blueprints.Require<BlueprintArea>(GameBlueprintIds.Areas.PrologueLabyrinth, "Shield Maze area");
            _blueprints.Require<BlueprintArea>(GameBlueprintIds.Areas.DefendersHeart, "Defender's Heart area");
            _canPlaceBilly = _blueprints.Get<BlueprintUnit>(ModBlueprintIds.Units.UndeadCiarCompanion) != null
                             && _blueprints.Get<BlueprintUnit>(ModBlueprintIds.Units.BillyShieldMazeStandIn) != null;
            _logger.Log($"Billy placement apply: canPlaceBilly={_canPlaceBilly}.");
            if (!_canPlaceBilly)
            {
                _logger.Warning("Billy companion or Shield Maze stand-in unit was not available; skipping Billy placement.");
            }
        }

        public void OnAreaLoaded()
        {
            try
            {
                if (!_canPlaceBilly)
                {
                    return;
                }

                if (!Game.HasInstance || Game.Instance.CurrentlyLoadedArea == null)
                {
                    _logger.Log("Billy placement skipped: game instance or loaded area is unavailable.");
                    return;
                }

                var areaGuid = Game.Instance.CurrentlyLoadedArea.AssetGuid;
                _logger.Log($"Billy placement area loaded: area={areaGuid}.");

                if (TryPlaceBillyInShieldMaze(out var shieldMazeReason))
                {
                    return;
                }

                if (ShouldPlaceBillyInDefendersHeart(out var defendersHeartReason))
                {
                    var position = GetDefendersHeartSpawnPosition();
                    var billy = FindBillyRosterUnit();
                    PlaceBilly(billy, position, DefendersHeartSpawnOrientation);
                    Game.Instance.Player?.InvalidateCharacterLists();
                    _logger.Log($"Billy placement positioned Defender's Heart roster Billy: position={position}, orientation={DefendersHeartSpawnOrientation}, hasView={billy?.View != null}, isInGame={billy?.IsInGame == true}.");
                    return;
                }

                if (areaGuid == PrologueLabyrinthGuid)
                {
                    _logger.Log("Billy placement skipped Shield Maze: " + shieldMazeReason);
                }
                else if (areaGuid == DefendersHeartGuid)
                {
                    _logger.Log("Billy placement skipped Defender's Heart: " + defendersHeartReason);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to place Billy: {ex}");
            }
        }

        private bool TryPlaceBillyInShieldMaze(out string reason)
        {
            if (!Game.HasInstance || Game.Instance.CurrentlyLoadedArea == null)
            {
                reason = "game instance or loaded area is unavailable.";
                return false;
            }

            if (Game.Instance.CurrentlyLoadedArea.AssetGuid != PrologueLabyrinthGuid)
            {
                reason = "loaded area is not Shield Maze.";
                return false;
            }

            var player = Game.Instance.Player;
            BillyRecruitmentDiagnostics.RemoveStaleCompanionStandIns("Shield Maze placement");
            var roster = IsBillyInPlayerRoster();
            if (roster)
            {
                reason = DescribePlacementGate(player, roster, IsBillyStandInPresent(includeCrossScene: true));
                return false;
            }

            var existingStandIn = FindBillyStandIn(includeCrossScene: true);
            var position = GetShieldMazeSpawnPosition();
            if (existingStandIn != null)
            {
                PlaceBilly(existingStandIn, position, ShieldMazeSpawnOrientation);
                reason = DescribePlacementGate(player, false, true);
                _logger.Log(
                    $"Billy placement positioned existing Shield Maze stand-in: position={FormatVector3(position)}, orientation={ShieldMazeSpawnOrientation}, hasView={existingStandIn.View != null}, isInGame={existingStandIn.IsInGame}.");
                return true;
            }

            var billy = CreateBillyAreaUnit();
            PlaceBilly(billy, position, ShieldMazeSpawnOrientation);
            reason = DescribePlacementGate(player, false, true);
            _logger.Log(
                $"Billy placement spawned Shield Maze stand-in: position={FormatVector3(position)}, orientation={ShieldMazeSpawnOrientation}, hasView={billy?.View != null}, isInGame={billy?.IsInGame == true}.");
            return true;
        }

        private static bool ShouldPlaceBillyInDefendersHeart(out string reason)
        {
            if (!Game.HasInstance || Game.Instance.CurrentlyLoadedArea == null)
            {
                reason = "game instance or loaded area is unavailable.";
                return false;
            }

            if (Game.Instance.CurrentlyLoadedArea.AssetGuid != DefendersHeartGuid)
            {
                reason = "loaded area is not Defender's Heart.";
                return false;
            }

            var player = Game.Instance.Player;
            var roster = IsBillyInPlayerRoster();
            var standIn = IsBillyStandInPresent(includeCrossScene: false);
            reason = DescribePlacementGate(player, roster, standIn);
            return roster && !standIn;
        }

        private UnitEntityData CreateBillyAreaUnit()
        {
            var billyBlueprint = _blueprints.Require<BlueprintUnit>(
                ModBlueprintIds.Units.BillyShieldMazeStandIn,
                "Billy Shield Maze stand-in unit");
            return Game.Instance.AddUnitToPersistentState(billyBlueprint);
        }

        private static void PlaceBilly(UnitEntityData billy, Vector3 position, float orientation)
        {
            if (billy == null)
            {
                return;
            }

            billy.Position = position;
            billy.SpawnPosition = position;
            billy.Orientation = orientation;
            billy.IsInGame = true;
            if (billy.View == null)
            {
                var view = billy.CreateView();
                if (view != null)
                {
                    view.Blueprint = billy.Blueprint;
                    billy.AttachToViewOnLoad(view);
                }
            }
        }

        private static bool IsBillyInPlayerRoster()
        {
            var player = Game.Instance.Player;
            if (player == null)
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

        private static bool IsBillyStandInPresent(bool includeCrossScene)
        {
            return FindBillyStandIn(includeCrossScene) != null;
        }

        private static UnitEntityData FindBillyStandIn(bool includeCrossScene)
        {
            var inLoadedArea = Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .FirstOrDefault(IsBillyStandIn);

            if (inLoadedArea != null || !includeCrossScene)
            {
                return inLoadedArea;
            }

            return Game.Instance.State?.PlayerState?.CrossSceneState?.AllEntityData
                .OfType<UnitEntityData>()
                .FirstOrDefault(IsBillyStandIn);
        }

        private static string DescribePlacementGate(Player player, bool roster, bool standIn)
        {
            return $"roster={roster}, standIn={standIn}, partyAndPetsBilly={CountBilly(player?.PartyAndPets)}, activeBilly={CountBilly(player?.ActiveCompanions)}, remoteBilly={CountBilly(player?.RemoteCompanions)}, allCharactersBilly={CountBilly(player?.AllCharacters)}, allCharactersStandIn={CountBillyStandIn(player?.AllCharacters)}, loadedAreaBilly={CountLoadedAreaBilly()}, loadedAreaStandIn={CountLoadedAreaBillyStandIn()}, crossSceneStandIn={CountCrossSceneBillyStandIn()}.";
        }

        private static int CountLoadedAreaBilly()
        {
            return Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Count(IsBilly) ?? 0;
        }

        private static int CountBilly(System.Collections.Generic.IEnumerable<UnitEntityData> units)
        {
            return units?.Count(IsBilly) ?? 0;
        }

        private static int CountBillyStandIn(System.Collections.Generic.IEnumerable<UnitEntityData> units)
        {
            return units?.Count(IsBillyStandIn) ?? 0;
        }

        private static int CountLoadedAreaBillyStandIn()
        {
            return Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Count(IsBillyStandIn) ?? 0;
        }

        private static int CountCrossSceneBillyStandIn()
        {
            return Game.Instance.State?.PlayerState?.CrossSceneState?.AllEntityData
                .OfType<UnitEntityData>()
                .Count(IsBillyStandIn) ?? 0;
        }

        private static UnitEntityData FindBillyRosterUnit()
        {
            var player = Game.Instance.Player;
            if (player == null)
            {
                return null;
            }

            return player.RemoteCompanions
                .Concat(player.ActiveCompanions)
                .Concat(player.PartyAndPets)
                .Concat(player.AllCharacters)
                .Where(unit => unit != null)
                .Distinct()
                .FirstOrDefault(IsBilly);
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
            return unit?.Descriptor?.Blueprint != null && unit.Descriptor.Blueprint.AssetGuid == BillyGuid;
        }

        private static bool IsBillyStandIn(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint != null && unit.Descriptor.Blueprint.AssetGuid == BillyStandInGuid;
        }

        private static Vector3 GetShieldMazeSpawnPosition()
        {
            return new Vector3(
                ShieldMazeSpawnX,
                ShieldMazeSpawnY,
                ShieldMazeSpawnZ);
        }

        private static Vector3 GetDefendersHeartSpawnPosition()
        {
            return new Vector3(
                DefendersHeartSpawnX,
                DefendersHeartSpawnY,
                DefendersHeartSpawnZ);
        }

        private static string FormatVector3(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.###}, {1:0.###}, {2:0.###})",
                value.x,
                value.y,
                value.z);
        }

    }
}
