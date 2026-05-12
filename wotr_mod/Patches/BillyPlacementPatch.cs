using System;
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
        private const float ShieldMazeSpawnOffsetX = 64f;
        private const float ShieldMazeSpawnOffsetZ = -40f;
        private const float ShieldMazeSpawnOrientation = 5f;
        private const float DefendersHeartSpawnX = -82f;
        private const float DefendersHeartSpawnY = 40f;
        private const float DefendersHeartSpawnZ = -7f;
        private const float DefendersHeartSpawnOrientation = 0f;
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
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
            _canPlaceBilly = _blueprints.Get<BlueprintUnit>(ModBlueprintIds.Units.UndeadCiarCompanion) != null;
            _logger.Log($"Billy placement apply: canPlaceBilly={_canPlaceBilly}.");
            if (!_canPlaceBilly)
            {
                _logger.Warning("Billy companion unit was not available; skipping Billy placement.");
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

                if (ShouldPlaceBillyInShieldMaze(out var shieldMazeReason))
                {
                    var position = GetSpawnPosition(
                        ShieldMazeSpawnOffsetX,
                        ShieldMazeSpawnOffsetZ);
                    var billy = CreateBillyAreaUnit();
                    PlaceBilly(billy, position, ShieldMazeSpawnOrientation);
                    _logger.Log($"Billy placement spawned Shield Maze stand-in: position={position}, orientation={ShieldMazeSpawnOrientation}, hasView={billy?.View != null}.");
                    return;
                }

                if (ShouldPlaceBillyInDefendersHeart(out var defendersHeartReason))
                {
                    var position = GetDefendersHeartSpawnPosition();
                    var billy = FindBillyRosterUnit(null);
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

        private static bool ShouldPlaceBillyInShieldMaze(out string reason)
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
            var roster = IsBillyInPlayerRoster();
            var standIn = IsBillyAreaStandInPresent(includeCrossScene: true);
            reason = DescribePlacementGate(player, roster, standIn);
            return !roster && !standIn;
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
            var standIn = IsBillyAreaStandInPresent(includeCrossScene: false);
            reason = DescribePlacementGate(player, roster, standIn);
            return roster && !standIn;
        }

        private UnitEntityData CreateBillyAreaUnit()
        {
            var billyBlueprint = _blueprints.Require<BlueprintUnit>(
                ModBlueprintIds.Units.UndeadCiarCompanion,
                "Billy companion unit");
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

        private static bool IsBillyAreaStandInPresent(bool includeCrossScene)
        {
            var player = Game.Instance.Player;
            var inLoadedArea = Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Any(unit => IsBilly(unit) && !IsPlayerRosterUnit(player, unit)) == true;

            if (inLoadedArea || !includeCrossScene)
            {
                return inLoadedArea;
            }

            return Game.Instance.State?.PlayerState?.CrossSceneState?.AllEntityData
                .OfType<UnitEntityData>()
                .Any(unit => IsBilly(unit) && !IsPlayerRosterUnit(player, unit)) == true;
        }

        private static string DescribePlacementGate(Player player, bool roster, bool standIn)
        {
            return $"roster={roster}, standIn={standIn}, partyAndPetsBilly={CountBilly(player?.PartyAndPets)}, activeBilly={CountBilly(player?.ActiveCompanions)}, remoteBilly={CountBilly(player?.RemoteCompanions)}, allCharactersBilly={CountBilly(player?.AllCharacters)}, loadedAreaBilly={CountLoadedAreaBilly(player, false)}, loadedAreaNonRosterBilly={CountLoadedAreaBilly(player, true)}, crossSceneNonRosterBilly={CountCrossSceneBilly(player, true)}.";
        }

        private static int CountLoadedAreaBilly(Player player, bool nonRosterOnly)
        {
            return Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Count(unit => IsBilly(unit) && (!nonRosterOnly || !IsPlayerRosterUnit(player, unit))) ?? 0;
        }

        private static int CountBilly(System.Collections.Generic.IEnumerable<UnitEntityData> units)
        {
            return units?.Count(IsBilly) ?? 0;
        }

        private static int CountCrossSceneBilly(Player player, bool nonRosterOnly)
        {
            return Game.Instance.State?.PlayerState?.CrossSceneState?.AllEntityData
                .OfType<UnitEntityData>()
                .Count(unit => IsBilly(unit) && (!nonRosterOnly || !IsPlayerRosterUnit(player, unit))) ?? 0;
        }

        private static UnitEntityData FindBillyRosterUnit(UnitEntityData standIn)
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
                .Where(unit => unit != null && !ReferenceEquals(unit, standIn))
                .Distinct()
                .FirstOrDefault(IsBilly);
        }

        private static bool IsPlayerRosterUnit(Player player, UnitEntityData unit)
        {
            if (player == null || unit == null)
            {
                return false;
            }

            return player.PartyAndPets.Contains(unit)
                || player.ActiveCompanions.Contains(unit)
                || player.RemoteCompanions.Contains(unit)
                || player.AllCharacters.Contains(unit) && HasRosterCompanionState(unit);
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

        private static Vector3 GetSpawnPosition(float offsetX, float offsetZ)
        {
            var player = Game.Instance.Player;
            if (player == null)
            {
                return Vector3.zero;
            }

            var center = player.GetPartyCenter();
            return new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
        }

        private static Vector3 GetDefendersHeartSpawnPosition()
        {
            return new Vector3(
                DefendersHeartSpawnX,
                DefendersHeartSpawnY,
                DefendersHeartSpawnZ);
        }
    }
}
