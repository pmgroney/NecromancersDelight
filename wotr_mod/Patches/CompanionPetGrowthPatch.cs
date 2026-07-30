using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.FactLogic;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CompanionPetGrowthPatch : IGamePatch, IUnitLoadHandler
    {
        private const int GrowthLevel = 4;

        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly Dictionary<BlueprintGuid, BlueprintFeature> _growthFeaturesByPet =
            new Dictionary<BlueprintGuid, BlueprintFeature>();

        public CompanionPetGrowthPatch(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
        }

        public string Name => "Companion Pet Growth";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            if (Main.Settings == null || !Main.Settings.FasterPetGrowth)
            {
                return;
            }

            var companionSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SylvanAnimalCompanion,
                "Sylvan animal companion selection");

            foreach (var companionFeature in _blueprints.GetFeatureSelectionAllFeatures(companionSelection)
                         .Where(feature => feature != null))
            {
                ConfigureGrowth(companionFeature);
            }

            AddGrowthToLoadedPets();
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            if (Main.Settings == null || !Main.Settings.FasterPetGrowth)
            {
                return;
            }

            try
            {
                AddGrowthToLoadedPet(unit);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to repair loaded companion pet growth feature: {ex}");
            }
        }

        private void ConfigureGrowth(BlueprintFeature companionFeature)
        {
            var addPet = _blueprints.GetComponents<AddPet>(companionFeature).FirstOrDefault();
            if (addPet == null || addPet.UpgradeLevel != 7)
            {
                return;
            }

            var pet = addPet.Pet;
            var upgradeFeature = addPet.UpgradeFeature;
            if (pet == null || upgradeFeature == null)
            {
                _logger.Warning($"Skipping faster growth for {companionFeature.name}: pet or upgrade feature is missing.");
                return;
            }

            UpdateGrowthDescription(companionFeature);
            addPet.UpgradeLevel = GrowthLevel;
            _growthFeaturesByPet[pet.AssetGuid] = upgradeFeature;
        }

        private void UpdateGrowthDescription(BlueprintFeature companionFeature)
        {
            var localizedDescription =
                BlueprintFields.UnitFactDescription.GetValue(companionFeature) as LocalizedString;
            var description = companionFeature.Description;
            if (localizedDescription == null ||
                string.IsNullOrEmpty(localizedDescription.Key) ||
                string.IsNullOrEmpty(description))
            {
                _logger.Warning($"Skipping faster-growth description for {companionFeature.name}: description is missing.");
                return;
            }

            var updatedDescription = description.Replace("7th level", "4th level");
            if (updatedDescription == description)
            {
                _logger.Warning($"Skipping faster-growth description for {companionFeature.name}: no level-7 text was found.");
                return;
            }

            _localization.Put(localizedDescription.Key, updatedDescription);
        }

        private void AddGrowthToLoadedPets()
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            foreach (var unit in Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.PartyAndPets).Where(u => u != null).Distinct())
            {
                AddGrowthToLoadedPet(unit);
            }
        }

        private void AddGrowthToLoadedPet(UnitEntityData unit)
        {
            var descriptor = unit.Descriptor;
            if (descriptor?.Blueprint == null ||
                !_growthFeaturesByPet.TryGetValue(descriptor.Blueprint.AssetGuid, out var upgradeFeature))
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
