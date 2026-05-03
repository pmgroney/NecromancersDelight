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
        private const float SpawnOffsetX = 64f;
        private const float SpawnOffsetZ = -40f;
        private const float SpawnOrientation = 5f;
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

                var billy = FindBillyRosterOrphan() ?? CreateBillyAreaUnit();
                var position = GetSpawnPosition();

                billy.Position = position;
                billy.SpawnPosition = position;
                billy.Orientation = SpawnOrientation;
                billy.IsInGame = true;
                if (billy.View == null)
                {
                    billy.CreateView();
                }

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

        private UnitEntityData CreateBillyAreaUnit()
        {
            var billyBlueprint = _blueprints.Require<BlueprintUnit>(
                ModBlueprintIds.Units.UndeadCiarCompanion,
                "Billy companion unit");
            var billy = Game.Instance.CreateUnitVacuum(billyBlueprint);
            Game.Instance.LoadedAreaState.AddEntityData(billy);
            return billy;
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

        private static UnitEntityData FindBillyRosterOrphan()
        {
            var player = Game.Instance.Player;
            if (player == null)
            {
                return null;
            }

            return player.AllCharacters
                .Where(IsBilly)
                .FirstOrDefault(unit => !player.PartyAndPets.Contains(unit)
                    && !player.ActiveCompanions.Contains(unit)
                    && !player.RemoteCompanions.Contains(unit));
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
