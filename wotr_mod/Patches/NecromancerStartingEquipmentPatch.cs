using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class NecromancerStartingEquipmentPatch : IGamePatch, IAreaLoadHandler, IUnitLoadHandler
    {
        private static readonly BlueprintGuid ScytheGuid =
            BlueprintGuid.Parse(GameBlueprintIds.Items.Scythe);

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public NecromancerStartingEquipmentPatch(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Necromancer Starting Equipment";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            _blueprints.Require<BlueprintCharacterClass>(
                ModBlueprintIds.Classes.Necromancer,
                "Necromancer class");
            _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.Scythe,
                "Scythe");
        }

        public void OnAreaLoaded()
        {
            TryApply(Game.Instance?.Player?.MainCharacter, "area load");
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            if (unit?.IsMainCharacter == true)
            {
                TryApply(unit, "unit load");
            }
        }

        private void TryApply(UnitEntityData mainCharacter, string source)
        {
            try
            {
                if (!ShouldApply(mainCharacter, out var reason))
                {
                    return;
                }

                var scythe = _blueprints.Require<BlueprintItem>(
                    GameBlueprintIds.Items.Scythe,
                    "Scythe");
                ItemEntity item = null;
                mainCharacter.Inventory.Add(scythe, 1, true, createdItem =>
                {
                    item = createdItem;
                    item?.Identify();
                });
                TryEquip(mainCharacter, item);
                _logger.Log($"Added Necromancer starting equipment item Scythe from {source}.");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to grant Necromancer starting equipment from {source}: {ex}");
            }
        }

        private bool ShouldApply(UnitEntityData mainCharacter, out string reason)
        {
            reason = "eligible";

            if (mainCharacter?.Descriptor?.Progression == null)
            {
                reason = "main character progression is unavailable";
                return false;
            }

            var necromancerClass = _blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Necromancer);
            if (necromancerClass == null || mainCharacter.Descriptor.Progression.GetClassLevel(necromancerClass) <= 0)
            {
                reason = "main character is not a Necromancer";
                return false;
            }

            if (HasScythe(mainCharacter))
            {
                reason = "scythe already present";
                return false;
            }

            return true;
        }

        private static bool HasScythe(UnitEntityData mainCharacter)
        {
            var personalInventoryHasItem = mainCharacter.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == ScytheGuid) == true;
            var partyInventoryHasItem = Game.Instance?.Player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == ScytheGuid) == true;
            var equippedHasItem = mainCharacter.Body?.CurrentEquipmentSlots
                ?.Any(slot => slot?.MaybeItem?.Blueprint?.AssetGuid == ScytheGuid) == true;

            return personalInventoryHasItem || partyInventoryHasItem || equippedHasItem;
        }

        private static void TryEquip(UnitEntityData mainCharacter, ItemEntity item)
        {
            if (item == null)
            {
                return;
            }

            foreach (var slot in mainCharacter.Descriptor.Body.EquipmentSlots)
            {
                if (slot.HasItem || !slot.CanInsertItem(item))
                {
                    continue;
                }

                var pairedHand = (slot as HandSlot)?.PairSlot;
                if (pairedHand?.MaybeWeapon?.Blueprint?.IsTwoHanded == true)
                {
                    continue;
                }

                slot.InsertItem(item);
                return;
            }
        }
    }
}
