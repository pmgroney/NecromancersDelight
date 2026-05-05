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
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public VelociraptorGrowthPatch(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Velociraptor Growth";

        public void RegisterLocalization()
        {
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
