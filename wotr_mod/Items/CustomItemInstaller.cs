using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.View.MapObjects;
using UnityModManagerNet;
using wotr_mod.Content;
using wotr_mod.Infrastructure;

namespace wotr_mod.Items
{
    internal sealed class CustomItemInstaller : IContentModule, IAreaLoadModule
    {
        private static readonly BlueprintGuid PrologueLabyrinthGuid = BlueprintGuid.Parse(GameBlueprintIds.Areas.PrologueLabyrinth);
        private static readonly string[] ShieldMazeWeaponRackIds =
        {
            "71c5f42a-f490-4d9d-a3ff-1cf0702b1caf",
            "55508648-91b4-47c0-9245-f625cb333473",
            "bcfce8e8-f634-446f-9a7f-9974d5c51c01"
        };

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
            EnsureShieldMazeRuntimeLootSeededFlag();

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

            if (item is BlueprintItemArmor armor
                && string.Equals(definition.ItemGuid, ModBlueprintIds.Items.ArchersTunic, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureArchersTunicItem(armor);
            }

            if (string.Equals(definition.ItemGuid, ModBlueprintIds.Items.BillyPilgrimageRecord, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureBillyPilgrimageRecord(item);
            }

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.ItemGuid, item);
            }

            return item;
        }

        private void ConfigureBillyPilgrimageRecord(BlueprintItem item)
        {
            _blueprints.SetComponents(item);
        }

        private void EnsureSupportBlueprints()
        {
            EnsureArchersTunicBowTrainingFeature();
            EnsureArchersTunicEnchantment();
            EnsureNeophytesLongbowOfDisciplineEnchantment();
        }

        private void EnsureArchersTunicBowTrainingFeature()
        {
            var existing = _blueprints.Get<BlueprintFeature>(
                ModBlueprintIds.Features.ArchersTunicBowTraining);
            var feature = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.RobeOfConsciousnessFeature,
                    "Robe of Consciousness donor feature"),
                ModBlueprintIds.Features.ArchersTunicBowTraining,
                "WotrMod_ArchersTunic_BowTraining");

            ConfigureArchersTunicBowTrainingFeature(feature);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(
                    ModBlueprintIds.Features.ArchersTunicBowTraining,
                    feature);
            }
        }

        private void EnsureArchersTunicEnchantment()
        {
            var existing = _blueprints.Get<BlueprintArmorEnchantment>(
                ModBlueprintIds.Enchantments.ArchersTunic);
            var enchantment = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintArmorEnchantment>(
                    GameBlueprintIds.Enchantments.RobeOfConsciousnessEnchantment,
                    "Robe of Consciousness donor enchantment"),
                ModBlueprintIds.Enchantments.ArchersTunic,
                "WotrMod_ArchersTunic_Enchantment");

            ConfigureArchersTunicEnchantment(enchantment);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(
                    ModBlueprintIds.Enchantments.ArchersTunic,
                    enchantment);
            }
        }

        private void ConfigureArchersTunicItem(BlueprintItemArmor armor)
        {
            var enhancement = _blueprints.Require<BlueprintArmorEnchantment>(
                GameBlueprintIds.Enchantments.ArmorEnhancementBonus1,
                "+1 armor enhancement");
            var enchantment = _blueprints.Require<BlueprintArmorEnchantment>(
                ModBlueprintIds.Enchantments.ArchersTunic,
                "Irori Neophyte's Armor enchantment");

            _blueprints.SetComponents(armor);
            _blueprints.SetArmorEnchantments(armor, enhancement, enchantment);
        }

        private void ConfigureArchersTunicEnchantment(BlueprintArmorEnchantment enchantment)
        {
            var feature = _blueprints.Require<BlueprintFeature>(
                ModBlueprintIds.Features.ArchersTunicBowTraining,
                "Irori Neophyte's Armor bow training feature");
            var addFeature = new AddUnitFeatureEquipment
            {
                name = "$AddUnitFeatureEquipment$WotrMod_ArchersTunic_Bows"
            };
            _blueprints.SetAddUnitFeatureEquipmentFeature(addFeature, feature);

            _blueprints.SetComponents(
                enchantment,
                addFeature);
        }

        private void ConfigureArchersTunicBowTrainingFeature(BlueprintFeature feature)
        {
            _blueprints.SetComponents(
                feature,
                new WeaponGroupAttackBonus
                {
                    name = "$WeaponGroupAttackBonus$WotrMod_ArchersTunic_Bows",
                    WeaponGroup = WeaponFighterGroup.Bows,
                    AttackBonus = 1,
                    Descriptor = ModifierDescriptor.None,
                    multiplyByContext = false,
                    contextMultiplier = new ContextValue()
                },
                new WeaponGroupDamageBonus
                {
                    name = "$WeaponGroupDamageBonus$WotrMod_ArchersTunic_Bows",
                    WeaponGroup = WeaponFighterGroup.Bows,
                    DamageBonus = 1,
                    Descriptor = ModifierDescriptor.None,
                    AdditionalValue = new ContextValue()
                });
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

        private BlueprintUnlockableFlag EnsureShieldMazeRuntimeLootSeededFlag()
        {
            var flag = _blueprints.Get<BlueprintUnlockableFlag>(ModBlueprintIds.Flags.ShieldMazeRuntimeLootSeeded);
            if (flag != null)
            {
                return flag;
            }

            flag = new BlueprintUnlockableFlag
            {
                name = "WotrMod_ShieldMazeRuntimeLootSeeded",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Flags.ShieldMazeRuntimeLootSeeded)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Flags.ShieldMazeRuntimeLootSeeded, flag);
            return flag;
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
            var isShieldMazeLoaded = IsShieldMazeLoaded();
            var shieldMazeRuntimeLootSeeded = isShieldMazeLoaded && IsShieldMazeRuntimeLootSeeded();
            var shieldMazeRuntimeLootTargetsReady = isShieldMazeLoaded && AreShieldMazeRuntimeLootTargetsReady();
            var shouldSeedShieldMazeRuntimeLoot = isShieldMazeLoaded
                                                  && !shieldMazeRuntimeLootSeeded
                                                  && shieldMazeRuntimeLootTargetsReady;
            if (shouldSeedShieldMazeRuntimeLoot)
            {
                AddShieldMazeFixedLoot();
            }
            else if (isShieldMazeLoaded)
            {
                _logger.Log(
                    shieldMazeRuntimeLootSeeded
                        ? "Skipped Shield Maze runtime loot seeding: already seeded in this playthrough."
                        : "Skipped Shield Maze runtime loot seeding: loot targets are not ready yet.");
            }

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
                            if (IsShieldMazeRuntimeLootPlacement(placement)
                                && !shouldSeedShieldMazeRuntimeLoot)
                            {
                                break;
                            }

                            AddToMapObjectLoot(placement, item);
                            break;
                    }
                }
            }

            if (shouldSeedShieldMazeRuntimeLoot)
            {
                MarkShieldMazeRuntimeLootSeeded();
            }
        }

        private void AddShieldMazeFixedLoot()
        {
            if (!IsShieldMazeLoaded())
            {
                return;
            }

            AddToMapObjectLoot(
                ItemPlacementDefinition.InMapObjectLoot(
                    "71c5f42a-f490-4d9d-a3ff-1cf0702b1caf",
                    "Weapon Rack",
                    count: 1,
                    identify: true),
                _blueprints.Require<BlueprintItem>(
                    GameBlueprintIds.Items.ColdIronMasterworkRapier,
                    "Cold-iron masterwork rapier"));

            AddToMapObjectLoot(
                ItemPlacementDefinition.InMapObjectLoot(
                    "55508648-91b4-47c0-9245-f625cb333473",
                    "Weapon Rack",
                    count: 1,
                    identify: true),
                _blueprints.Require<BlueprintItem>(
                    GameBlueprintIds.Items.MasterworkGreatsword,
                    "Masterwork greatsword"));

            AddShieldMazeInflictPotionBatches();
        }

        private void AddShieldMazeInflictPotionBatches()
        {
            var lightPotion = _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.PotionOfInflictLightWounds,
                "Potion of Inflict Light Wounds");
            var moderatePotion = _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.PotionOfInflictModerateWounds,
                "Potion of Inflict Moderate Wounds");

            var containers = Game.Instance.State.LoadedAreaState.AllEntityData
                .OfType<MapObjectEntityData>()
                .Where(mapObject => !string.IsNullOrWhiteSpace(mapObject.UniqueId))
                .Where(mapObject => !ShieldMazeWeaponRackIds.Contains(mapObject.UniqueId, StringComparer.OrdinalIgnoreCase))
                .Where(mapObject => mapObject.Parts.Get<InteractionLootPart>() != null)
                .Take(5)
                .ToArray();

            if (containers.Length < 5)
            {
                _logger.Warning($"Found only {containers.Length} Shield Maze loot containers for Inflict Wounds potions.");
            }

            for (var i = 0; i < containers.Length; i++)
            {
                var targetName = "Shield Maze potion stash " + (i + 1);
                switch (i)
                {
                    case 0:
                        AddToMapObjectLoot(containers[i], targetName, lightPotion, 4, identify: true);
                        break;
                    case 1:
                        AddToMapObjectLoot(containers[i], targetName, lightPotion, 3, identify: true);
                        break;
                    case 2:
                        AddToMapObjectLoot(containers[i], targetName, lightPotion, 2, identify: true);
                        AddToMapObjectLoot(containers[i], targetName, moderatePotion, 1, identify: true);
                        break;
                    case 3:
                        AddToMapObjectLoot(containers[i], targetName, moderatePotion, 2, identify: true);
                        break;
                    case 4:
                        AddToMapObjectLoot(containers[i], targetName, lightPotion, 2, identify: true);
                        AddToMapObjectLoot(containers[i], targetName, moderatePotion, 2, identify: true);
                        break;
                }
            }
        }

        private static bool IsShieldMazeLoaded()
        {
            return Game.HasInstance
                && Game.Instance.CurrentlyLoadedArea != null
                && Game.Instance.CurrentlyLoadedArea.AssetGuid == PrologueLabyrinthGuid;
        }

        private static bool AreShieldMazeRuntimeLootTargetsReady()
        {
            var mapObjects = Game.Instance?.State?.LoadedAreaState?.AllEntityData
                ?.OfType<MapObjectEntityData>();
            if (mapObjects == null)
            {
                return false;
            }

            var loadedIds = mapObjects
                .Where(mapObject => !string.IsNullOrWhiteSpace(mapObject.UniqueId))
                .Select(mapObject => mapObject.UniqueId)
                .ToArray();
            return ShieldMazeWeaponRackIds.All(requiredId =>
                loadedIds.Contains(requiredId, StringComparer.OrdinalIgnoreCase));
        }

        private static bool IsShieldMazeRuntimeLootPlacement(ItemPlacementDefinition placement)
        {
            return placement.Kind == ItemPlacementKind.MapObjectLoot
                && ShieldMazeWeaponRackIds.Contains(placement.TargetGuid, StringComparer.OrdinalIgnoreCase);
        }

        private bool IsShieldMazeRuntimeLootSeeded()
        {
            var flag = EnsureShieldMazeRuntimeLootSeededFlag();
            return Game.Instance?.Player?.UnlockableFlags?.IsUnlocked(flag) == true;
        }

        private void MarkShieldMazeRuntimeLootSeeded()
        {
            var player = Game.Instance?.Player;
            if (player == null)
            {
                return;
            }

            var flag = EnsureShieldMazeRuntimeLootSeededFlag();
            if (!player.UnlockableFlags.IsUnlocked(flag))
            {
                player.UnlockableFlags.Unlock(flag);
                _logger.Log("Marked Shield Maze runtime loot as seeded.");
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

            AddToMapObjectLoot(mapObject, placement.TargetName, item, placement.Count, placement.Identify);
        }

        private void AddToMapObjectLoot(
            MapObjectEntityData mapObject,
            string targetName,
            BlueprintItem item,
            int count,
            bool identify)
        {
            var lootPart = mapObject.Parts.Get<InteractionLootPart>();
            if (lootPart == null)
            {
                _logger.Log($"Map object has no InteractionLootPart: {targetName} / {mapObject.UniqueId}");
                return;
            }

            if (lootPart.Loot == null)
            {
                lootPart.Loot = new ItemsCollection(mapObject);
            }

            var existingItem = lootPart.Loot.Items.FirstOrDefault(existing => existing?.Blueprint?.AssetGuid == item.AssetGuid);
            if (existingItem != null)
            {
                if (identify)
                {
                    existingItem.Identify();
                }

                _logger.Log($"Map object loot already contains {item.name}: {targetName}.");
                return;
            }

            lootPart.Loot.Add(item, count, identify, createdItem =>
            {
                if (identify)
                {
                    createdItem.Identify();
                }
            });

            _logger.Log($"Added {item.name} to map object loot {targetName}.");
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
