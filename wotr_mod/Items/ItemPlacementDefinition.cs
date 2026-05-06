using System;
using Kingmaker.Blueprints;

namespace wotr_mod.Items
{
    internal enum ItemPlacementKind
    {
        ChestLoot,
        UnitLoot,
        MapObjectLoot
    }

    internal sealed class ItemPlacementDefinition
    {
        private ItemPlacementDefinition(
            ItemPlacementKind kind,
            string targetGuid,
            string targetName,
            int count,
            bool identify,
            string unitLootGuid)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");
            }

            Kind = kind;
            TargetGuid = RequireGuid(targetGuid, nameof(targetGuid));
            TargetName = RequireText(targetName, nameof(targetName));
            Count = count;
            Identify = identify;
            UnitLootGuid = string.IsNullOrWhiteSpace(unitLootGuid)
                ? null
                : RequireGuid(unitLootGuid, nameof(unitLootGuid));

        }
        
        public static ItemPlacementDefinition InMapObjectLoot(
            string mapObjectUniqueId,
            string mapObjectName,
            int count = 1,
            bool identify = true)
        {
            return new ItemPlacementDefinition(
                ItemPlacementKind.MapObjectLoot,
                mapObjectUniqueId,
                mapObjectName,
                count,
                identify,
                unitLootGuid: null);
        }

        public ItemPlacementKind Kind { get; }
        public string TargetGuid { get; }
        public string TargetName { get; }
        public int Count { get; }
        public bool Identify { get; }
        public string UnitLootGuid { get; }

        public static ItemPlacementDefinition InChestLoot(
            string lootGuid,
            string lootName,
            int count = 1,
            bool identify = true)
        {
            return new ItemPlacementDefinition(
                ItemPlacementKind.ChestLoot,
                lootGuid,
                lootName,
                count,
                identify,
                unitLootGuid: null);
        }

        public static ItemPlacementDefinition OnUnit(
            string unitGuid,
            string unitName,
            int count = 1,
            bool identify = true)
        {
            return new ItemPlacementDefinition(
                ItemPlacementKind.UnitLoot,
                unitGuid,
                unitName,
                count,
                identify,
                unitLootGuid: null);
        }

        public static ItemPlacementDefinition OnUnit(
            string unitGuid,
            string unitName,
            string unitLootGuid,
            int count = 1,
            bool identify = true)
        {
            return new ItemPlacementDefinition(
                ItemPlacementKind.UnitLoot,
                unitGuid,
                unitName,
                count,
                identify,
                unitLootGuid);
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or empty.", parameterName);
            }

            return value;
        }

        private static string RequireGuid(string value, string parameterName)
        {
            RequireText(value, parameterName);
            BlueprintGuid.Parse(value);
            return value;
        }
    }
}
