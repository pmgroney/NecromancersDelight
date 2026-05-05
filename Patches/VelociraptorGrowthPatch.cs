using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.FactLogic;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class VelociraptorGrowthPatch : IGamePatch, IUnitLoadHandler
    {
        private const int GrowthLevel = 4;
        private static readonly BlueprintGuid VelociraptorUnitGuid = BlueprintGuid.Parse(GameBlueprintIds.Units.VelociraptorCompanion);

        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public VelociraptorGrowthPatch(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
        }

        public string Name => "Velociraptor Growth";

        public void RegisterLocalization()
        {
            _localization.Put(
                LocalizationIds.Game.AnimalCompanionVelociraptorDescription,
                "{g|Encyclopedia:Size}Size{/g}: Small\n{g|Encyclopedia:Speed}Speed{/g}: 60 ft.\n{g|Encyclopedia:Armor_Class}AC{/g}: +3 natural armor\n{g|Encyclopedia:Attack}Attacks{/g}: bite ({g|Encyclopedia:Dice}1d4{/g}), 2 talons (1d6)\n{g|Encyclopedia:Ability_Scores}Ability scores{/g}: {g|Encyclopedia:Strength}Str{/g} 11, {g|Encyclopedia:Dexterity}Dex{/g} 17, {g|Encyclopedia:Constitution}Con{/g} 17, {g|Encyclopedia:Intelligence}Int{/g} 2, {g|Encyclopedia:Wisdom}Wis{/g} 12, {g|Encyclopedia:Charisma}Cha{/g} 14\nSpecial qualities: {g|Encyclopedia:Scent}scent{/g}.\nAt 4th level, a velociraptor's size becomes Medium and it gains Str +4, Dex -2, Con +2, +2 to its natural armor {g|Encyclopedia:Bonus}bonus{/g} to AC, and the {g|FeaturePounce}pounce{/g} ability.\nAgile Movement: A velociraptor can move through {g|Encyclopedia:Threatened_Area}threatened areas{/g} without provoking {g|Encyclopedia:Attack_Of_Opportunity}attacks of opportunity{/g}.");
        }

        public void Apply()
        {
            var companionFeature = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.VelociraptorCompanion,
                "Velociraptor companion feature");
            var upgradeFeature = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.VelociraptorUpgrade,
                "Velociraptor upgrade feature");

            var addPet = _blueprints.GetComponents<AddPet>(companionFeature).FirstOrDefault();
            if (addPet == null)
            {
                throw new InvalidOperationException("Velociraptor companion feature has no AddPet component.");
            }

            addPet.UpgradeLevel = GrowthLevel;
            AddGrowthToLoadedVelociraptors(upgradeFeature);
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            try
            {
                var upgradeFeature = _blueprints.Get<BlueprintFeature>(GameBlueprintIds.Features.VelociraptorUpgrade);
                if (upgradeFeature != null)
                {
                    AddGrowthToLoadedVelociraptor(unit, upgradeFeature);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to repair loaded velociraptor growth feature: {ex}");
            }
        }

        private void AddGrowthToLoadedVelociraptors(BlueprintFeature upgradeFeature)
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            foreach (var unit in Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.PartyAndPets).Where(u => u != null).Distinct())
            {
                AddGrowthToLoadedVelociraptor(unit, upgradeFeature);
            }
        }

        private static void AddGrowthToLoadedVelociraptor(UnitEntityData unit, BlueprintFeature upgradeFeature)
        {
            var descriptor = unit.Descriptor;
            if (descriptor?.Blueprint == null || descriptor.Blueprint.AssetGuid != VelociraptorUnitGuid)
            {
                return;
            }

            if (descriptor.Progression.CharacterLevel < GrowthLevel || descriptor.Facts.Get(upgradeFeature) != null)
            {
                return;
            }

            descriptor.Facts.Add(upgradeFeature.CreateFact(null, descriptor, null));
        }
    }
}
