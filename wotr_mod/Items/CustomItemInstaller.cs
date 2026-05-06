using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Conditions;
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
            EnsureSupportBlueprints();

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

            if (item is BlueprintItemEquipment equipment)
            {
                equipment.DC = 0;
            }

            if (item is BlueprintItemWeapon weapon)
            {
                foreach (var enchantmentGuid in definition.EnchantmentGuids)
                {
                    var enchantment = _blueprints.Require<BlueprintWeaponEnchantment>(
                        enchantmentGuid,
                        definition.InternalName + " enchantment");
                    _blueprints.AddWeaponEnchantment(weapon, enchantment);
                }
            }

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.ItemGuid, item);
            }

            return item;
        }

        private void EnsureSupportBlueprints()
        {
            EnsureNeophytesLongbowOfDisciplineEnchantment();
        }

        private void EnsureNeophytesLongbowOfDisciplineEnchantment()
        {
            var existing = _blueprints.Get<BlueprintWeaponEnchantment>(
                ModBlueprintIds.Enchantments.NeophytesLongbowOfDisciplineForceDamage);
            var enchantment = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintWeaponEnchantment>(
                    GameBlueprintIds.Enchantments.LongswordOfRightEnchantment,
                    "Longsword of Right donor enchantment"),
                ModBlueprintIds.Enchantments.NeophytesLongbowOfDisciplineForceDamage,
                "WotrMod_NeophytesLongbowOfDiscipline_ForceDamage");

            ConfigureDisciplineForceDamage(enchantment);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(
                    ModBlueprintIds.Enchantments.NeophytesLongbowOfDisciplineForceDamage,
                    enchantment);
            }
        }

        private BlueprintWeaponEnchantment ConfigureDisciplineForceDamage(BlueprintWeaponEnchantment enchantment)
        {
            var component = _blueprints.EnsureComponent(
                enchantment,
                () => new WeaponConditionalDamageDice
                {
                    name = "$WeaponConditionalDamageDice$WotrMod_NeophytesLongbowOfDiscipline_ForceDamage"
                });

            component.Damage = new DamageDescription
            {
                Dice = new DiceFormula(1, DiceType.D6),
                Bonus = 0,
                TypeDescription = new DamageTypeDescription
                {
                    Type = DamageType.Force,
                    Common = new DamageTypeDescription.CommomData(),
                    Physical = new DamageTypeDescription.PhysicalData()
                },
                IgnoreReduction = false,
                IgnoreImmunities = false
            };
            component.CheckWielder = false;
            component.IsBane = false;
            component.Conditions = new ConditionsChecker
            {
                Operation = Operation.And,
                Conditions = new Condition[]
                {
                    new ContextConditionAlignment
                    {
                        name = "$ContextConditionAlignment$WotrMod_NeophytesLongbowOfDiscipline_Chaotic",
                        CheckCaster = false,
                        Alignment = AlignmentComponent.Chaotic
                    }
                }
            };

            return enchantment;
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
                        // Applied in OnAreaLoaded — unit runtime inventory needs a loaded area state.
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
            foreach (var definition in CustomItemRegistry.GetAll())
            {
                var item = _blueprints.Get<BlueprintItem>(definition.ItemGuid);
                if (item == null)
                {
                    continue;
                }

                foreach (var placement in definition.Placements)
                {
                    switch (placement.Kind)
                    {
                        case ItemPlacementKind.UnitLoot:
                            AddToLoadedUnitInventory(placement, item);
                            break;
                        case ItemPlacementKind.MapObjectLoot:
                            AddToMapObjectLoot(placement, item);
                            break;
                    }
                }
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

            if (lootPart.Loot == null)
            {
                lootPart.Loot = new ItemsCollection(mapObject);
            }

            if (lootPart.Loot.Items.Any(existing => existing?.Blueprint?.AssetGuid == item.AssetGuid))
            {
                _logger.Log($"Map object loot already contains {item.name}: {placement.TargetName}.");
                return;
            }

            lootPart.Loot.Add(item, placement.Count, placement.Identify, null);

            _logger.Log($"Added {item.name} to map object loot {placement.TargetName}.");
        }

        private void AddToLoadedUnitInventory(ItemPlacementDefinition placement, BlueprintItem item)
        {
            var unitGuid = BlueprintGuid.Parse(placement.TargetGuid);
            var unit = Game.Instance?.State?.LoadedAreaState?.AllEntityData
                ?.OfType<UnitEntityData>()
                ?.FirstOrDefault(entity => entity?.Blueprint?.AssetGuid == unitGuid);

            if (unit == null)
            {
                _logger.Log($"Loaded unit target not found: {placement.TargetName} / {placement.TargetGuid}");
                return;
            }

            if (unit.Inventory.Items.Any(existing => existing?.Blueprint?.AssetGuid == item.AssetGuid))
            {
                _logger.Log($"Loaded unit inventory already contains {item.name}: {placement.TargetName}.");
                return;
            }

            unit.Inventory.Add(item, placement.Count, placement.Identify, createdItem =>
            {
                if (placement.Identify)
                {
                    createdItem.Identify();
                }
            });

            _logger.Log($"Added {item.name} to loaded unit inventory for {placement.TargetName}.");
        }
    }
}
