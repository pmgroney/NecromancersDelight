using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.FactLogic;
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
        private const string ShadowDemonHouseChestUniqueId = "15fcbf37-2f36-42e4-a595-261694b5ac9c";
        private static readonly BlueprintGuid KenabresBurningGuid = BlueprintGuid.Parse(GameBlueprintIds.Areas.KenabresBurning);
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

                if (string.Equals(
                        definition.ItemGuid,
                        ModBlueprintIds.Items.ApprenticeEvokersStaff,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ConfigureApprenticeEvokersStaffItem(weapon);
                }
            }

            if (item is BlueprintItemArmor armor
                && string.Equals(definition.ItemGuid, ModBlueprintIds.Items.ArchersTunic, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureArchersTunicItem(armor);
            }

            if (item is BlueprintItemArmor cutpurseVest
                && string.Equals(definition.ItemGuid, ModBlueprintIds.Items.CutpurseVest, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureCutpurseVest(cutpurseVest);
            }

            if (item is BlueprintItemArmor battleMageVest
                && string.Equals(definition.ItemGuid, ModBlueprintIds.Items.BattleMageVest, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureBattleMageVest(battleMageVest);
            }

            if (item is BlueprintItemArmor acolyteArmor
                && string.Equals(definition.ItemGuid, ModBlueprintIds.Items.IroriAcolytesArmor, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureBillyArmorItem(
                    acolyteArmor,
                    GameBlueprintIds.Enchantments.ArmorEnhancementBonus2,
                    ModBlueprintIds.Enchantments.IroriAcolytesArmor,
                    "Irori Acolyte's Armor");
            }

            if (item is BlueprintItemArmor adeptArmor
                && string.Equals(definition.ItemGuid, ModBlueprintIds.Items.IroriAdeptsArmor, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureBillyArmorItem(
                    adeptArmor,
                    GameBlueprintIds.Enchantments.ArmorEnhancementBonus3,
                    ModBlueprintIds.Enchantments.IroriAdeptsArmor,
                    "Irori Adept's Armor");
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
            EnsureApprenticeEvokersStaffFeature();
            EnsureApprenticeEvokersStaffEnchantment();
            EnsureArchersTunicBowTrainingFeature();
            EnsureArchersTunicEnchantment();
            EnsureBattleMageVestFeature();
            EnsureBattleMageVestEnchantment();
            EnsureBillyArmorFeature(
                ModBlueprintIds.Features.IroriAcolytesArmor,
                "WotrMod_IroriAcolytesArmor_Feature",
                2,
                GameBlueprintIds.Features.PositiveChannelingBonus1Die,
                GameBlueprintIds.Features.NegativeChannelingBonus1Die);
            EnsureBillyArmorEnchantment(
                ModBlueprintIds.Enchantments.IroriAcolytesArmor,
                ModBlueprintIds.Features.IroriAcolytesArmor,
                "WotrMod_IroriAcolytesArmor_Enchantment");
            EnsureBillyArmorFeature(
                ModBlueprintIds.Features.IroriAdeptsArmor,
                "WotrMod_IroriAdeptsArmor_Feature",
                3,
                GameBlueprintIds.Features.PositiveChannelingBonus2Dice,
                GameBlueprintIds.Features.NegativeChannelingBonus2Dice);
            EnsureBillyArmorEnchantment(
                ModBlueprintIds.Enchantments.IroriAdeptsArmor,
                ModBlueprintIds.Features.IroriAdeptsArmor,
                "WotrMod_IroriAdeptsArmor_Enchantment");
            EnsureNeophytesLongbowOfDisciplineEnchantment();
            EnsureDisciplineForceDamageEnchantment(
                ModBlueprintIds.Enchantments.AcolytesLongbowOfDisciplineForceDamage,
                "WotrMod_AcolytesLongbowOfDiscipline_ForceDamage",
                DiceType.D8);
            EnsureDisciplineForceDamageEnchantment(
                ModBlueprintIds.Enchantments.AdeptsLongbowOfDisciplineForceDamage,
                "WotrMod_AdeptsLongbowOfDiscipline_ForceDamage",
                DiceType.D10);
        }

        private void EnsureApprenticeEvokersStaffFeature()
        {
            var existing = _blueprints.Get<BlueprintFeature>(
                ModBlueprintIds.Features.ApprenticeEvokersStaff);
            var feature = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.Ashmaker,
                    "Ashmaker donor feature"),
                ModBlueprintIds.Features.ApprenticeEvokersStaff,
                "WotrMod_ApprenticeEvokersStaff_Feature");

            _blueprints.SetComponents(
                feature,
                new AddStatBonus
                {
                    name = "$AddStatBonus$WotrMod_ApprenticeEvokersStaff",
                    Descriptor = ModifierDescriptor.UntypedStackable,
                    Stat = StatType.AdditionalAttackBonus,
                    Value = 1
                },
                new IncreaseSpellSchoolCasterLevel
                {
                    name = "$IncreaseSpellSchoolCasterLevel$WotrMod_ApprenticeEvokersStaff",
                    School = SpellSchool.Evocation,
                    BonusLevel = 1,
                    Descriptor = ModifierDescriptor.UntypedStackable
                });

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(
                    ModBlueprintIds.Features.ApprenticeEvokersStaff,
                    feature);
            }
        }

        private void EnsureApprenticeEvokersStaffEnchantment()
        {
            var existing = _blueprints.Get<BlueprintWeaponEnchantment>(
                ModBlueprintIds.Enchantments.ApprenticeEvokersStaff);
            var enchantment = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintWeaponEnchantment>(
                    GameBlueprintIds.Enchantments.Ashmaker,
                    "Ashmaker donor enchantment"),
                ModBlueprintIds.Enchantments.ApprenticeEvokersStaff,
                "WotrMod_ApprenticeEvokersStaff_Enchantment");
            var feature = _blueprints.Require<BlueprintFeature>(
                ModBlueprintIds.Features.ApprenticeEvokersStaff,
                "Apprentice Evoker's Staff feature");
            var addFeature = new AddUnitFeatureEquipment
            {
                name = "$AddUnitFeatureEquipment$WotrMod_ApprenticeEvokersStaff"
            };
            _blueprints.SetAddUnitFeatureEquipmentFeature(addFeature, feature);
            _blueprints.SetComponents(enchantment, addFeature);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(
                    ModBlueprintIds.Enchantments.ApprenticeEvokersStaff,
                    enchantment);
            }
        }

        private void ConfigureApprenticeEvokersStaffItem(BlueprintItemWeapon weapon)
        {
            var enhancement = _blueprints.Require<BlueprintWeaponEnchantment>(
                GameBlueprintIds.Enchantments.WeaponEnhancementBonus1,
                "+1 weapon enhancement");
            var enchantment = _blueprints.Require<BlueprintWeaponEnchantment>(
                ModBlueprintIds.Enchantments.ApprenticeEvokersStaff,
                "Apprentice Evoker's Staff enchantment");

            _blueprints.SetComponents(weapon);
            _blueprints.SetWeaponEnchantments(weapon, enhancement, enchantment);
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

        private void ConfigureCutpurseVest(BlueprintItemArmor armor)
        {
            var enhancement = _blueprints.Require<BlueprintArmorEnchantment>(
                GameBlueprintIds.Enchantments.ArmorEnhancementBonus1,
                "+1 armor enhancement");
            var dexterity = _blueprints.Require<BlueprintEquipmentEnchantment>(
                GameBlueprintIds.Enchantments.Dexterity1,
                "+1 Dexterity equipment enchantment");

            // StuddedStandartPlus1 uses the original StuddedBandits visual.
            armor.ForcedRampColorPresetIndex = 0;
            _blueprints.SetItemCost(armor, 4175);
            _blueprints.SetArmorEnchantments(armor, enhancement, dexterity);
        }

        private void ConfigureBattleMageVest(BlueprintItemArmor armor)
        {
            var enhancement = _blueprints.Require<BlueprintArmorEnchantment>(
                GameBlueprintIds.Enchantments.ArmorEnhancementBonus2,
                "+2 armor enhancement");
            var charisma = _blueprints.Require<BlueprintEquipmentEnchantment>(
                GameBlueprintIds.Enchantments.Charisma2,
                "+2 Charisma equipment enchantment");
            var slashingResistance = _blueprints.Require<BlueprintArmorEnchantment>(
                ModBlueprintIds.Enchantments.BattleMageVest,
                "Battle Mage Vest slashing resistance enchantment");

            armor.ForcedRampColorPresetIndex = 2;
            _blueprints.SetComponents(armor);
            _blueprints.SetArmorEnchantments(armor, enhancement, charisma, slashingResistance);
        }

        private void EnsureBattleMageVestFeature()
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.BattleMageVest);
            var feature = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.UnbendingArmor,
                    "Unbending Armor feature donor"),
                ModBlueprintIds.Features.BattleMageVest,
                "WotrMod_BattleMageVest_Feature");

            _blueprints.SetComponents(
                feature,
                new AddDamageResistancePhysical
                {
                    name = "$AddDamageResistancePhysical$WotrMod_BattleMageVest_Slashing",
                    Value = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 5
                    },
                    BypassedByForm = true,
                    Form = PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing
                });

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.BattleMageVest, feature);
            }
        }

        private void EnsureBattleMageVestEnchantment()
        {
            var existing = _blueprints.Get<BlueprintArmorEnchantment>(ModBlueprintIds.Enchantments.BattleMageVest);
            var enchantment = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintArmorEnchantment>(
                    GameBlueprintIds.Enchantments.UnbendingArmorEnchantment,
                    "Unbending Armor enchantment donor"),
                ModBlueprintIds.Enchantments.BattleMageVest,
                "WotrMod_BattleMageVest_Enchantment");
            var addFeature = new AddUnitFeatureEquipment
            {
                name = "$AddUnitFeatureEquipment$WotrMod_BattleMageVest"
            };
            _blueprints.SetAddUnitFeatureEquipmentFeature(
                addFeature,
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.BattleMageVest,
                    "Battle Mage Vest feature"));
            _blueprints.SetComponents(enchantment, addFeature);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Enchantments.BattleMageVest, enchantment);
            }
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

        private void EnsureBillyArmorFeature(
            string featureGuid,
            string internalName,
            int bowBonus,
            string positiveChannelFeatureGuid,
            string negativeChannelFeatureGuid)
        {
            var existing = _blueprints.Get<BlueprintFeature>(featureGuid);
            var feature = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintFeature>(
                    GameBlueprintIds.Features.RobeOfConsciousnessFeature,
                    "Robe of Consciousness donor feature"),
                featureGuid,
                internalName);
            var components = new List<BlueprintComponent>
            {
                new WeaponGroupAttackBonus
                {
                    name = "$WeaponGroupAttackBonus$" + internalName,
                    WeaponGroup = WeaponFighterGroup.Bows,
                    AttackBonus = bowBonus,
                    Descriptor = ModifierDescriptor.None,
                    multiplyByContext = false,
                    contextMultiplier = new ContextValue()
                },
                new WeaponGroupDamageBonus
                {
                    name = "$WeaponGroupDamageBonus$" + internalName,
                    WeaponGroup = WeaponFighterGroup.Bows,
                    DamageBonus = bowBonus,
                    Descriptor = ModifierDescriptor.None,
                    AdditionalValue = new ContextValue()
                }
            };
            var addChannelFeatures = new AddFacts
            {
                name = "$AddFacts$" + internalName + "_Channel"
            };
            _blueprints.SetAddFacts(
                addChannelFeatures,
                _blueprints.Require<BlueprintFeature>(
                    positiveChannelFeatureGuid,
                    internalName + " positive channel bonus"),
                _blueprints.Require<BlueprintFeature>(
                    negativeChannelFeatureGuid,
                    internalName + " negative channel bonus"));
            components.Add(addChannelFeatures);
            _blueprints.SetComponents(feature, components.ToArray());

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }
        }

        private void EnsureBillyArmorEnchantment(
            string enchantmentGuid,
            string featureGuid,
            string internalName)
        {
            var existing = _blueprints.Get<BlueprintArmorEnchantment>(enchantmentGuid);
            var enchantment = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintArmorEnchantment>(
                    GameBlueprintIds.Enchantments.RobeOfConsciousnessEnchantment,
                    "Robe of Consciousness donor enchantment"),
                enchantmentGuid,
                internalName);
            var feature = _blueprints.Require<BlueprintFeature>(
                featureGuid,
                internalName + " feature");
            var addFeature = new AddUnitFeatureEquipment
            {
                name = "$AddUnitFeatureEquipment$" + internalName
            };
            _blueprints.SetAddUnitFeatureEquipmentFeature(addFeature, feature);
            _blueprints.SetComponents(enchantment, addFeature);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(enchantmentGuid, enchantment);
            }
        }

        private void ConfigureBillyArmorItem(
            BlueprintItemArmor armor,
            string enhancementGuid,
            string enchantmentGuid,
            string itemName)
        {
            var enhancement = _blueprints.Require<BlueprintArmorEnchantment>(
                enhancementGuid,
                itemName + " enhancement");
            var enchantment = _blueprints.Require<BlueprintArmorEnchantment>(
                enchantmentGuid,
                itemName + " enchantment");

            _blueprints.SetComponents(armor);
            _blueprints.SetArmorEnchantments(armor, enhancement, enchantment);
        }

        private void EnsureNeophytesLongbowOfDisciplineEnchantment()
        {
            EnsureDisciplineForceDamageEnchantment(
                ModBlueprintIds.Enchantments.NeophytesLongbowOfDisciplineForceDamage,
                "WotrMod_NeophytesLongbowOfDiscipline_ForceDamage",
                DiceType.D6);
        }

        private void EnsureDisciplineForceDamageEnchantment(
            string enchantmentGuid,
            string internalName,
            DiceType diceType)
        {
            var existing = _blueprints.Get<BlueprintWeaponEnchantment>(enchantmentGuid);
            var enchantment = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintWeaponEnchantment>(
                    GameBlueprintIds.Enchantments.LongswordOfRightEnchantment,
                    "Longsword of Right donor enchantment"),
                enchantmentGuid,
                internalName);

            ConfigureDisciplineForceDamage(enchantment, diceType, internalName);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(enchantmentGuid, enchantment);
            }
        }

        private BlueprintWeaponEnchantment ConfigureDisciplineForceDamage(
            BlueprintWeaponEnchantment enchantment,
            DiceType diceType,
            string internalName)
        {
            var component = _blueprints.EnsureComponent(
                enchantment,
                () => new WeaponConditionalDamageDice
                {
                    name = "$WeaponConditionalDamageDice$" + internalName
                });

            component.Damage = new DamageDescription
            {
                Dice = new DiceFormula(1, diceType),
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
                        name = "$ContextConditionAlignment$" + internalName + "_Chaotic",
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
            AddShadowDemonHouseLockpicks();

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

        private void AddShadowDemonHouseLockpicks()
        {
            if (!IsKenabresBurningLoaded())
            {
                return;
            }

            var lockpick = _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.ConsumableLockpickPlus5,
                "Lockpick +5");
            EnsureMapObjectLootItemCount(
                ShadowDemonHouseChestUniqueId,
                "Market Square Shadow Demon house book chest",
                lockpick,
                count: 6,
                identify: true);
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

        private static bool IsKenabresBurningLoaded()
        {
            return Game.HasInstance
                && Game.Instance.CurrentlyLoadedArea != null
                && Game.Instance.CurrentlyLoadedArea.AssetGuid == KenabresBurningGuid;
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

        private void EnsureMapObjectLootItemCount(
            string mapObjectUniqueId,
            string targetName,
            BlueprintItem item,
            int count,
            bool identify)
        {
            var mapObject = Game.Instance?.State?.LoadedAreaState?.AllEntityData
                ?.OfType<MapObjectEntityData>()
                ?.FirstOrDefault(entity => string.Equals(
                    entity.UniqueId,
                    mapObjectUniqueId,
                    StringComparison.OrdinalIgnoreCase));

            if (mapObject == null)
            {
                _logger.Log($"Map object loot target not found: {targetName} / {mapObjectUniqueId}");
                return;
            }

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
                if (existingItem.Count < count)
                {
                    existingItem.SetCount(count);
                    _logger.Log($"Updated {item.name} count in map object loot {targetName} to {count}.");
                }

                if (identify)
                {
                    existingItem.Identify();
                }

                return;
            }

            lootPart.Loot.Add(item, count, identify, createdItem =>
            {
                createdItem.SetCount(count);
                if (identify)
                {
                    createdItem.Identify();
                }
            });

            _logger.Log($"Added {count} {item.name} to map object loot {targetName}.");
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
