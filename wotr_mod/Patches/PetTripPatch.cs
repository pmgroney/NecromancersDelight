using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class PetTripPatch : IGamePatch, IUnitLoadHandler
    {
        private static readonly BlueprintGuid[] TripCompanionUnitGuids =
        {
            BlueprintGuid.Parse(GameBlueprintIds.Units.LeopardCompanion),
            BlueprintGuid.Parse(GameBlueprintIds.Units.VelociraptorCompanion)
        };

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public PetTripPatch(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Pet Trip";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            if (Main.Settings == null || !Main.Settings.FasterPetGrowth)
            {
                return;
            }

            var trippingBite = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.TrippingBite,
                "Tripping Bite");

            AddTripToCompanionBlueprint(GameBlueprintIds.Units.LeopardCompanion, "Leopard companion", trippingBite);
            AddTripToCompanionBlueprint(GameBlueprintIds.Units.VelociraptorCompanion, "Velociraptor companion", trippingBite);
            AddTripToLoadedCompanions(trippingBite);
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            if (Main.Settings == null || !Main.Settings.FasterPetGrowth)
            {
                return;
            }

            try
            {
                var trippingBite = _blueprints.Get<BlueprintUnitFact>(GameBlueprintIds.Features.TrippingBite);
                if (trippingBite != null)
                {
                    AddTripToLoadedCompanion(unit, trippingBite);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to repair loaded companion trip fact: {ex}");
            }
        }

        private void AddTripToCompanionBlueprint(string unitGuid, string unitName, BlueprintUnitFact trippingBite)
        {
            var unit = _blueprints.Require<BlueprintUnit>(unitGuid, unitName);
            _blueprints.AddFactToUnitBlueprint(unit, trippingBite);
        }

        private void AddTripToLoadedCompanions(BlueprintUnitFact trippingBite)
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            foreach (var unit in Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.PartyAndPets).Where(u => u != null).Distinct())
            {
                AddTripToLoadedCompanion(unit, trippingBite);
            }
        }

        private static void AddTripToLoadedCompanion(UnitEntityData unit, BlueprintUnitFact trippingBite)
        {
            var descriptor = unit.Descriptor;
            if (descriptor?.Blueprint == null || !TripCompanionUnitGuids.Contains(descriptor.Blueprint.AssetGuid))
            {
                return;
            }

            if (descriptor.Facts.Get(trippingBite) != null)
            {
                return;
            }

            descriptor.Facts.Add(trippingBite.CreateFact(null, descriptor, null));
        }
    }
}
