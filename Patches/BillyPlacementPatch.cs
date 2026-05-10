using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
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
        private const float DefendersHeartSpawnOffsetX = 2f;
        private const float DefendersHeartSpawnOffsetZ = -1.5f;
        private const float DefendersHeartSpawnOrientation = 180f;
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

                if (ShouldPlaceBillyInShieldMaze())
                {
                    PlaceBilly(CreateBillyAreaUnit(), GetSpawnPosition(
                        ShieldMazeSpawnOffsetX,
                        ShieldMazeSpawnOffsetZ),
                        ShieldMazeSpawnOrientation);
                    return;
                }

                if (ShouldPlaceBillyInDefendersHeart())
                {
                    PlaceBilly(CreateBillyAreaUnit(), GetSpawnPosition(
                        DefendersHeartSpawnOffsetX,
                        DefendersHeartSpawnOffsetZ),
                        DefendersHeartSpawnOrientation);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to place Billy: {ex}");
            }
        }

        private static bool ShouldPlaceBillyInShieldMaze()
        {
            if (!Game.HasInstance || Game.Instance.CurrentlyLoadedArea == null)
            {
                return false;
            }

            if (Game.Instance.CurrentlyLoadedArea.AssetGuid != PrologueLabyrinthGuid)
            {
                return false;
            }

            return !IsBillyInPlayerRoster() && !IsBillyAreaStandInPresent();
        }

        private static bool ShouldPlaceBillyInDefendersHeart()
        {
            if (!Game.HasInstance || Game.Instance.CurrentlyLoadedArea == null)
            {
                return false;
            }

            if (Game.Instance.CurrentlyLoadedArea.AssetGuid != DefendersHeartGuid)
            {
                return false;
            }

            return IsBillyInPlayerRoster() && !IsBillyAreaStandInPresent();
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
                billy.CreateView();
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
                .Any(IsBilly);
        }

        private static bool IsBillyAreaStandInPresent()
        {
            var player = Game.Instance.Player;
            var inLoadedArea = Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Any(unit => IsBilly(unit) && !IsPlayerRosterUnit(player, unit)) == true;

            var inCrossSceneState = Game.Instance.State?.PlayerState?.CrossSceneState?.AllEntityData
                .OfType<UnitEntityData>()
                .Any(unit => IsBilly(unit) && !IsPlayerRosterUnit(player, unit)) == true;

            return inLoadedArea || inCrossSceneState;
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
                || player.AllCharacters.Contains(unit);
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
    }
}
