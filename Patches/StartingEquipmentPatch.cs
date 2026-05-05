using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal class StartingEquipmentPatch : IGamePatch, IAreaLoadHandler, IUnitLoadHandler
    {
        private static readonly FieldInfo AddItemToPlayerItemToGiveField =
            typeof(AddItemToPlayer).GetField("m_ItemToGive", BindingFlags.Instance | BindingFlags.NonPublic);

        private static StartingEquipmentPatch _current;
        private static bool _grantInProgress;

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly StartingEquipmentRule[] _rules;

        public StartingEquipmentPatch(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger,
            params StartingEquipmentRule[] rules)
        {
            _blueprints = blueprints;
            _logger = logger;
            _rules = rules ?? new StartingEquipmentRule[0];
            _current = this;
        }

        public virtual string Name => "Starting Equipment";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            foreach (var rule in _rules)
            {
                _blueprints.Require<BlueprintArchetype>(rule.ArchetypeGuid, $"{rule.Name} archetype");

                foreach (var featureGuid in rule.FeatureFallbackGuids)
                {
                    _blueprints.Require<BlueprintFeature>(featureGuid, $"{rule.Name} fallback feature");
                }

                foreach (var item in rule.Items)
                {
                    _blueprints.Require<BlueprintItem>(item.ItemGuid, item.Description);
                }
            }
        }

        public void OnAreaLoaded()
        {
            TryApply(Game.Instance?.Player?.MainCharacter, "area load", true);
        }

        public void OnUnitLoaded(UnitEntityData unit)
        {
            if (unit?.IsMainCharacter == true)
            {
                TryApply(unit, "unit load", false);
            }
        }

        private void TryApply(UnitEntityData mainCharacter, string source, bool logSkip)
        {
            if (_grantInProgress)
            {
                return;
            }

            foreach (var rule in _rules)
            {
                TryApply(rule, mainCharacter, source, logSkip);
            }
        }

        private void TryApply(StartingEquipmentRule rule, UnitEntityData mainCharacter, string source, bool logSkip)
        {
            try
            {
                if (!ShouldApply(rule, mainCharacter, out var reason))
                {
                    if (logSkip)
                    {
                        _logger.Log($"Skipped {rule.Name} starting equipment from {source}: {reason}.");
                    }

                    return;
                }

                var missingItems = rule.Items
                    .Where(item => !HasItem(mainCharacter, item.ItemGuid))
                    .ToArray();
                foreach (var item in missingItems)
                {
                    var blueprint = _blueprints.Require<BlueprintItem>(item.ItemGuid, item.Description);
                    AddItemWithGameAction(blueprint, item);
                    _logger.Log($"Added {rule.Name} starting equipment item {item.Description} with AddItemToPlayer from {source}.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to grant {rule.Name} starting equipment from {source}: {ex}");
            }
        }

        private bool ShouldApply(StartingEquipmentRule rule, UnitEntityData mainCharacter, out string reason)
        {
            if (mainCharacter == null)
            {
                reason = "main character is unavailable";
                return false;
            }

            if (!mainCharacter.IsMainCharacter)
            {
                reason = $"{mainCharacter.CharacterName} is not the main character";
                return false;
            }

            if (mainCharacter.Descriptor?.Progression == null)
            {
                reason = $"{mainCharacter.CharacterName} has no progression data";
                return false;
            }

            if (Game.Instance?.Player?.Inventory == null)
            {
                reason = "player inventory is unavailable";
                return false;
            }

            if (!MatchesRule(rule, mainCharacter, out var matchReason))
            {
                reason = matchReason;
                return false;
            }

            if (rule.Items.All(item => HasItem(mainCharacter, item.ItemGuid)))
            {
                reason = "all configured items are already present";
                return false;
            }

            reason = "eligible";
            return true;
        }

        private bool MatchesRule(StartingEquipmentRule rule, UnitEntityData mainCharacter, out string reason)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(rule.ArchetypeGuid);
            if (archetype != null && mainCharacter.Descriptor.Progression.IsArchetype(archetype))
            {
                reason = $"{rule.Name} archetype is present";
                return true;
            }

            foreach (var featureGuid in rule.FeatureFallbackGuids)
            {
                var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
                if (feature != null && mainCharacter.Descriptor.Progression.Features.HasFact(feature))
                {
                    reason = $"{rule.Name} fallback feature is present";
                    return true;
                }
            }

            reason = $"{rule.Name} archetype/fallback feature not detected";
            return false;
        }

        private static bool HasItem(UnitEntityData mainCharacter, string itemGuid)
        {
            var guid = BlueprintGuid.Parse(itemGuid);
            var inventoryHasItem = Game.Instance.Player.Inventory.Items
                .Any(item => item?.Blueprint != null && item.Blueprint.AssetGuid == guid);
            var equippedHasItem = mainCharacter.Body?.CurrentEquipmentSlots
                .Any(slot => slot?.MaybeItem?.Blueprint != null && slot.MaybeItem.Blueprint.AssetGuid == guid) == true;

            return inventoryHasItem || equippedHasItem;
        }

        private static void AddItemWithGameAction(BlueprintItem blueprint, StartingEquipmentItem item)
        {
            if (AddItemToPlayerItemToGiveField == null)
            {
                throw new MissingFieldException(typeof(AddItemToPlayer).FullName, "m_ItemToGive");
            }

            var action = new AddItemToPlayer
            {
                name = "$AddItemToPlayer$StartingEquipment",
                Silent = item.Silent,
                Quantity = item.Quantity,
                Identify = true,
                Equip = item.Equip,
                EquipOn = item.Equip ? new PlayerCharacter { name = "$PlayerCharacter$StartingEquipment" } : null,
                PreferredWeaponSet = item.PreferredWeaponSet,
                ErrorIfDidNotEquip = false
            };

            AddItemToPlayerItemToGiveField.SetValue(
                action,
                BlueprintReferenceBase.CreateTyped<BlueprintItemReference>(blueprint));

            _grantInProgress = true;
            try
            {
                action.RunAction();
            }
            finally
            {
                _grantInProgress = false;
            }
        }

        private static void HandleAddItemToPlayer(AddItemToPlayer action)
        {
            if (_grantInProgress || _current == null)
            {
                return;
            }

            var item = action?.ItemToGive;
            var source = item == null
                ? "AddItemToPlayer"
                : $"AddItemToPlayer {item.name}";
            var logSkip = _current._rules.Any(rule => rule.TriggerItemGuids.Any(guid => ItemMatchesGuid(item, guid)));
            _current.TryApply(Game.Instance?.Player?.MainCharacter, source, logSkip);
        }

        private static bool ItemMatchesGuid(BlueprintItem item, string itemGuid)
        {
            return item != null && item.AssetGuid == BlueprintGuid.Parse(itemGuid);
        }

        [HarmonyPatch(typeof(AddItemToPlayer), nameof(AddItemToPlayer.RunAction))]
        private static class AddItemToPlayerRunActionPatch
        {
            private static void Postfix(AddItemToPlayer __instance)
            {
                HandleAddItemToPlayer(__instance);
            }
        }

        [HarmonyPatch]
        private static class UnitDescriptorAddStartingInventoryPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(UnitDescriptor).GetMethod(
                    nameof(UnitDescriptor.AddStartingInventory),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);
            }

            private static void Postfix(UnitDescriptor __instance)
            {
                var unit = __instance?.Unit;
                if (unit?.IsMainCharacter == true)
                {
                    _current?.TryApply(unit, "AddStartingInventory", true);
                }
            }
        }
    }

    internal sealed class StartingEquipmentRule
    {
        public StartingEquipmentRule(
            string name,
            string archetypeGuid,
            StartingEquipmentItem[] items,
            string[] featureFallbackGuids,
            string[] triggerItemGuids)
        {
            Name = name;
            ArchetypeGuid = archetypeGuid;
            Items = items ?? new StartingEquipmentItem[0];
            FeatureFallbackGuids = featureFallbackGuids ?? new string[0];
            TriggerItemGuids = triggerItemGuids ?? new string[0];
        }

        public string Name { get; }
        public string ArchetypeGuid { get; }
        public StartingEquipmentItem[] Items { get; }
        public string[] FeatureFallbackGuids { get; }
        public string[] TriggerItemGuids { get; }
    }

    internal sealed class StartingEquipmentItem
    {
        public StartingEquipmentItem(
            string itemGuid,
            string description,
            bool equip,
            bool silent = false,
            int quantity = 1,
            int preferredWeaponSet = 0)
        {
            ItemGuid = itemGuid;
            Description = description;
            Equip = equip;
            Silent = silent;
            Quantity = quantity;
            PreferredWeaponSet = preferredWeaponSet;
        }

        public string ItemGuid { get; }
        public string Description { get; }
        public bool Equip { get; }
        public bool Silent { get; }
        public int Quantity { get; }
        public int PreferredWeaponSet { get; }
    }
}
