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
        private const float SpawnOffsetX = 2.5f;
        private const float SpawnOffsetZ = 1.5f;
        private const float SpawnOrientation = 180f;
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid PrologueLabyrinthGuid = BlueprintGuid.Parse(GameBlueprintIds.Areas.PrologueLabyrinth);

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public BillyPlacementPatch(BlueprintTool blueprints, UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Billy Shield Maze Placement";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            _blueprints.Require<BlueprintArea>(GameBlueprintIds.Areas.PrologueLabyrinth, "Shield Maze area");
            _blueprints.Require<BlueprintUnit>(ModBlueprintIds.Units.UndeadCiarCompanion, "Billy companion unit");
        }

        public void OnAreaLoaded()
        {
            try
            {
                if (!ShouldPlaceBilly())
                {
                    return;
                }

                var billyBlueprint = _blueprints.Require<BlueprintUnit>(
                    ModBlueprintIds.Units.UndeadCiarCompanion,
                    "Billy companion unit");
                var billy = Game.Instance.AddUnitToPersistentState(billyBlueprint);
                var position = GetSpawnPosition();

                billy.Position = position;
                billy.SpawnPosition = position;
                billy.Orientation = SpawnOrientation;
                billy.IsInGame = true;
                billy.CreateView();

                _logger.Log($"Placed Billy in the Shield Maze at {position}.");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to place Billy in the Shield Maze: {ex}");
            }
        }

        private static bool ShouldPlaceBilly()
        {
            if (!Game.HasInstance || Game.Instance.CurrentlyLoadedArea == null)
            {
                return false;
            }

            if (Game.Instance.CurrentlyLoadedArea.AssetGuid != PrologueLabyrinthGuid)
            {
                return false;
            }

            return !IsBillyInPlayerRoster() && !IsBillyInLoadedArea();
        }

        private static bool IsBillyInPlayerRoster()
        {
            var player = Game.Instance.Player;
            if (player == null)
            {
                return false;
            }

            return player.AllCharacters
                .Concat(player.PartyAndPets)
                .Where(unit => unit != null)
                .Distinct()
                .Any(IsBilly);
        }

        private static bool IsBillyInLoadedArea()
        {
            return Game.Instance.LoadedAreaState?.AllEntityData
                .OfType<UnitEntityData>()
                .Any(IsBilly) == true;
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit.Descriptor?.Blueprint != null && unit.Descriptor.Blueprint.AssetGuid == BillyGuid;
        }

        private static Vector3 GetSpawnPosition()
        {
            var player = Game.Instance.Player;
            if (player == null)
            {
                return Vector3.zero;
            }

            var center = player.GetPartyCenter();
            return new Vector3(center.x + SpawnOffsetX, center.y, center.z + SpawnOffsetZ);
        }
    }
}
