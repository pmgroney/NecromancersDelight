using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Loot;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.View.MapObjects;
using UnityModManagerNet;
using wotr_mod.Content;
using wotr_mod.Infrastructure;

namespace wotr_mod.Items
{
    internal sealed class CustomItemInstaller : IContentModule, IAreaLoadModule
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public CustomItemInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
        }

        public string Name => "Custom Items";

        public void RegisterLocalization()
        {
        }

        public void Install()
        {
            foreach (var definition in CustomItemRegistry.GetAll())
            {
                var item = EnsureItem(definition);
                ApplyPlacements(definition, item);
            }
        }

        private BlueprintItem EnsureItem(CustomItemDefinition definition)
        {
            var existing = _blueprints.Get<BlueprintItem>(definition.ItemGuid);
            var item = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintItem>(definition.SourceItemGuid, definition.InternalName + " donor item"),
                definition.ItemGuid,
                definition.InternalName);

            _blueprints.SetItemDisplay(
                item,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.ItemGuid, item);
            }

            return item;
        }

        private void ApplyPlacements(CustomItemDefinition definition, BlueprintItem item)
        {
            foreach (var placement in definition.Placements)
            {
                switch (placement.Kind)
                {
                    case ItemPlacementKind.ChestLoot:
                        AddToChestLoot(placement, item);
                        break;
                    case ItemPlacementKind.UnitLoot:
                        AddToUnitLoot(definition, placement, item);
                        break;
                    case ItemPlacementKind.MapObjectLoot:
                        // Applied in OnAreaLoaded — requires a loaded area state.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void OnAreaLoaded()
        {
            DumpMapObjectsWithLoot();

            foreach (var definition in CustomItemRegistry.GetAll())
            {
                var item = _blueprints.Get<BlueprintItem>(definition.ItemGuid);
                if (item == null)
                {
                    continue;
                }

                foreach (var placement in definition.Placements)
                {
                    if (placement.Kind == ItemPlacementKind.MapObjectLoot)
                    {
                        AddToMapObjectLoot(placement, item);
                    }
                }
            }
        }

        private void DumpMapObjectsWithLoot()
        {
            try
            {
                var mapObjects = Game.Instance?.State?.LoadedAreaState?.AllEntityData
                    ?.OfType<MapObjectEntityData>()
                    ?.Where(obj => obj.Parts.Get<InteractionLootPart>() != null)
                    ?.ToList();

                if (mapObjects == null || mapObjects.Count == 0)
                {
                    _logger.Log("[ItemDebug] No map objects with loot found in current area.");
                    return;
                }

                _logger.Log($"[ItemDebug] Map objects with loot in current area ({mapObjects.Count}):");
                foreach (var obj in mapObjects)
                {
                    _logger.Log($"[ItemDebug]   UniqueId={obj.UniqueId}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"[ItemDebug] Error dumping map objects: {ex.Message}");
            }
        }

        private void AddToChestLoot(ItemPlacementDefinition placement, BlueprintItem item)
        {
            var loot = _blueprints.Require<BlueprintLoot>(placement.TargetGuid, placement.TargetName);
            if (_blueprints.AddItemToLoot(loot, item, placement.Count, placement.Identify))
            {
                _logger.Log($"Added {item.name} to chest loot {placement.TargetName}.");
            }
        }

        private void AddToMapObjectLoot(ItemPlacementDefinition placement, BlueprintItem item)
        {
            var mapObjects = Game.Instance.State.LoadedAreaState.AllEntityData
                .OfType<MapObjectEntityData>();

            var mapObject = mapObjects.FirstOrDefault(x =>
                string.Equals(x.UniqueId, placement.TargetGuid, StringComparison.OrdinalIgnoreCase));

            if (mapObject == null)
            {
                _logger.Log($"Map object loot target not found: {placement.TargetName} / {placement.TargetGuid}");
                return;
            }

            var lootPart = mapObject.Parts.Get<InteractionLootPart>();
            if (lootPart == null)
            {
                _logger.Log($"Map object has no InteractionLootPart: {placement.TargetName} / {placement.TargetGuid}");
                return;
            }

            var lootEntry = new LootEntry
            {
                Item = item.ToReference<BlueprintItemReference>(),
                Count = placement.Count,
                Identify = placement.Identify
            };

            lootPart.AddItems(new[] { lootEntry });

            _logger.Log($"Added {item.name} to map object loot {placement.TargetName}.");
        }
        
        private void AddToUnitLoot(
            CustomItemDefinition definition,
            ItemPlacementDefinition placement,
            BlueprintItem item)
        {
            var unit = _blueprints.Require<BlueprintUnit>(placement.TargetGuid, placement.TargetName);
            var loot = EnsureUnitLoot(definition, placement, item);
            if (_blueprints.AddLootToUnit(unit, loot, "$AddLoot$" + definition.InternalName))
            {
                _logger.Log($"Added {item.name} to unit loot for {placement.TargetName}.");
            }
        }

        private BlueprintUnitLoot EnsureUnitLoot(
            CustomItemDefinition definition,
            ItemPlacementDefinition placement,
            BlueprintItem item)
        {
            var existing = _blueprints.Get<BlueprintUnitLoot>(placement.UnitLootGuid);
            if (existing != null)
            {
                _blueprints.SetComponents(
                    existing,
                    _blueprints.CreateFixedLootItem(item, placement.Count, placement.Identify, definition.InternalName));
                return existing;
            }

            var loot = new BlueprintUnitLoot
            {
                name = definition.InternalName + "_UnitLoot",
                AssetGuid = BlueprintGuid.Parse(placement.UnitLootGuid)
            };
            _blueprints.SetComponents(
                loot,
                _blueprints.CreateFixedLootItem(item, placement.Count, placement.Identify, definition.InternalName));
            _blueprints.AddCachedBlueprint(placement.UnitLootGuid, loot);
            return loot;
        }
    }
}
