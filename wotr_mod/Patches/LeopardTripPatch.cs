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
    internal sealed class LeopardTripPatch : IGamePatch, IUnitLoadHandler
    {
        private static readonly BlueprintGuid LeopardUnitGuid = BlueprintGuid.Parse(GameBlueprintIds.Units.LeopardCompanion);

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public LeopardTripPatch(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Leopard Trip";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            var leopard = _blueprints.Require<BlueprintUnit>(
                GameBlueprintIds.Units.LeopardCompanion,
                "Leopard companion");
            var trippingBite = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.TrippingBite,
                "Tripping Bite");

            _blueprints.AddFactToUnitBlueprint(leopard, trippingBite);
            AddTripToLoadedLeopards(trippingBite);
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            try
            {
                var trippingBite = _blueprints.Get<BlueprintUnitFact>(GameBlueprintIds.Features.TrippingBite);
                if (trippingBite != null)
                {
                    AddTripToLoadedLeopard(unit, trippingBite);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to repair loaded leopard trip fact: {ex}");
            }
        }

        private void AddTripToLoadedLeopards(BlueprintUnitFact trippingBite)
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            foreach (var unit in Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.PartyAndPets).Where(u => u != null).Distinct())
            {
                AddTripToLoadedLeopard(unit, trippingBite);
            }
        }

        private static void AddTripToLoadedLeopard(UnitEntityData unit, BlueprintUnitFact trippingBite)
        {
            var descriptor = unit.Descriptor;
            if (descriptor?.Blueprint == null || descriptor.Blueprint.AssetGuid != LeopardUnitGuid)
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
