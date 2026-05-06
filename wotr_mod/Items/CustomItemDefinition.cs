using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;

namespace wotr_mod.Items
{
    internal sealed class CustomItemDefinition
    {
        public CustomItemDefinition(
            string internalName,
            string itemGuid,
            string sourceItemGuid,
            string displayNameKey,
            string descriptionKey,
            IEnumerable<string> enchantmentGuids = null,
            IEnumerable<ItemPlacementDefinition> placements = null)
        {
            InternalName = RequireText(internalName, nameof(internalName));
            ItemGuid = RequireGuid(itemGuid, nameof(itemGuid));
            SourceItemGuid = RequireGuid(sourceItemGuid, nameof(sourceItemGuid));
            DisplayNameKey = RequireText(displayNameKey, nameof(displayNameKey));
            DescriptionKey = RequireText(descriptionKey, nameof(descriptionKey));
            EnchantmentGuids = (enchantmentGuids ?? Enumerable.Empty<string>())
                .Select(guid => RequireGuid(guid, nameof(enchantmentGuids)))
                .ToArray();
            Placements = (placements ?? Enumerable.Empty<ItemPlacementDefinition>()).ToArray();
        }

        public string InternalName { get; }
        public string ItemGuid { get; }
        public string SourceItemGuid { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public IReadOnlyList<string> EnchantmentGuids { get; }
        public IReadOnlyList<ItemPlacementDefinition> Placements { get; }

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
