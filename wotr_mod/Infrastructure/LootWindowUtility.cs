using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Items;
using Kingmaker.View;
using Kingmaker.View.MapObjects;

namespace wotr_mod.Infrastructure
{
    internal static class LootWindowUtility
    {
        public static void LogOpenedContainers(EntityViewBase[] objects, string contextLabel)
        {
            var areaPart = Game.Instance?.CurrentlyLoadedAreaPart;
            Main.Log(
                $"{contextLabel} LootVM opened: area={Game.Instance?.CurrentlyLoadedArea?.AssetGuid}, "
                + $"areaPart={areaPart?.AssetGuid}, areaPartName={areaPart?.name ?? "<none>"}, objects={objects?.Length ?? 0}.");

            if (objects == null)
            {
                return;
            }

            foreach (var entityViewBase in objects)
            {
                Main.Log($"{contextLabel} LootVM object: {DescribeLootView(entityViewBase)}");
            }
        }

        public static bool HasUniqueId(EntityViewBase entityViewBase, string uniqueId)
        {
            return string.Equals(
                entityViewBase?.Data?.UniqueId,
                uniqueId,
                StringComparison.OrdinalIgnoreCase);
        }

        public static InteractionLootPart GetLootPart(EntityViewBase entityViewBase)
        {
            return entityViewBase?.Data?.Get<InteractionLootPart>();
        }

        public static string DescribeLootView(EntityViewBase entityViewBase)
        {
            var data = entityViewBase?.Data;
            var lootPart = GetLootPart(entityViewBase);
            return $"uniqueId={data?.UniqueId ?? "<none>"}, entity={data}, view={entityViewBase}, "
                + $"hasLootPart={lootPart != null}, {DescribeItemsCollection(lootPart?.Loot)}";
        }

        private static string DescribeItemsCollection(ItemsCollection collection)
        {
            if (collection == null)
            {
                return "loot=<none>";
            }

            var items = collection.Items
                .Select(DescribeItem)
                .ToArray();

            return $"lootItems=[{string.Join(", ", items)}]";
        }

        private static string DescribeItem(ItemEntity item)
        {
            return item?.Blueprint == null
                ? "<null>"
                : $"{item.Blueprint.name}:{item.Blueprint.AssetGuid}x{item.Count}";
        }
    }
}
