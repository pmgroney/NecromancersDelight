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
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public LeopardTripPatch(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
        }

        public string Name => "Leopard Trip";

        public void RegisterLocalization()
        {
            _localization.Put(
                LocalizationIds.Game.AnimalCompanionLeopardDescription,
                "{g|Encyclopedia:Size}Size{/g}: Small\n{g|Encyclopedia:Speed}Speed{/g}: 50 ft.\n{g|Encyclopedia:Armor_Class}AC{/g}: +4 natural armor\n{g|Encyclopedia:Attack}Attacks{/g}: bite ({g|Encyclopedia:Dice}1d4{/g} plus trip), 2 claws (1d2)\n{g|Encyclopedia:Ability_Scores}Ability scores{/g}: {g|Encyclopedia:Strength}Str{/g} 12, {g|Encyclopedia:Dexterity}Dex{/g} 21, {g|Encyclopedia:Constitution}Con{/g} 13, {g|Encyclopedia:Intelligence}Int{/g} 2, {g|Encyclopedia:Wisdom}Wis{/g} 12, {g|Encyclopedia:Charisma}Cha{/g} 6\nSpecial qualities: {g|Encyclopedia:Scent}scent{/g}.\nAt 4th level, a leopard's size becomes Medium and it gains Str +4, Dex -2, Con +2, and the {g|FeaturePounce}pounce{/g} ability.\nSneak Attack: A leopard deals an additional 1d6 precision {g|Encyclopedia:Damage}damage{/g} to {g|Encyclopedia:Flat_Footed}flat-footed{/g} or {g|Encyclopedia:Flanking}flanked{/g} targets (2d6 at 4th level).");
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
