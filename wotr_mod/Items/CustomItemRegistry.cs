using System;
using System.Collections.Generic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Items
{
    internal static class CustomItemRegistry
    {
        private static readonly IReadOnlyList<CustomItemDefinition> AllDefinitions =
            Array.AsReadOnly(new[]
            {
                new CustomItemDefinition(
                    "WotrMod_ApprenticeEvokersStaff",
                    ModBlueprintIds.Items.ApprenticeEvokersStaff,
                    GameBlueprintIds.Items.Ashmaker,
                    LocalizationIds.Mod.ApprenticeEvokersStaffName,
                    LocalizationIds.Mod.ApprenticeEvokersStaffDescription,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.EstrodTowerBasementBox,
                            "Tower of Estrod basement box",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_NeophytesLongbowOfDiscipline",
                    ModBlueprintIds.Items.NeophytesLongbowOfDiscipline,
                    GameBlueprintIds.Items.CompositeLongbowPlus1,
                    LocalizationIds.Mod.NeophytesLongbowOfDisciplineName,
                    LocalizationIds.Mod.NeophytesLongbowOfDisciplineDescription,
                    enchantmentGuids: new[]
                    {
                        ModBlueprintIds.Enchantments.NeophytesLongbowOfDisciplineForceDamage
                    },
                    placements: new[]
                    {
                        ItemPlacementDefinition.OnUnit(
                            GameBlueprintIds.Units.Hosilla,
                            "Hosilla",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_AcolytesLongbowOfDiscipline",
                    ModBlueprintIds.Items.AcolytesLongbowOfDiscipline,
                    GameBlueprintIds.Items.CompositeLongbowPlus2,
                    LocalizationIds.Mod.AcolytesLongbowOfDisciplineName,
                    LocalizationIds.Mod.AcolytesLongbowOfDisciplineDescription,
                    enchantmentGuids: new[]
                    {
                        ModBlueprintIds.Enchantments.AcolytesLongbowOfDisciplineForceDamage
                    },
                    placements: new[]
                    {
                        ItemPlacementDefinition.OnUnit(
                            GameBlueprintIds.Units.ReliableRedoubtGargoyleMiniboss,
                            "Reliable Redoubt gargoyle miniboss",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_AdeptsLongbowOfDiscipline",
                    ModBlueprintIds.Items.AdeptsLongbowOfDiscipline,
                    GameBlueprintIds.Items.CompositeLongbowPlus3,
                    LocalizationIds.Mod.AdeptsLongbowOfDisciplineName,
                    LocalizationIds.Mod.AdeptsLongbowOfDisciplineDescription,
                    enchantmentGuids: new[]
                    {
                        ModBlueprintIds.Enchantments.AdeptsLongbowOfDisciplineForceDamage
                    },
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.MidnightFaneReserveBalorCorpse,
                            "Midnight Fane reserve balor corpse",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_ArchersTunic",
                    ModBlueprintIds.Items.ArchersTunic,
                    GameBlueprintIds.Items.BreastplatePlus1,
                    LocalizationIds.Mod.ArchersTunicName,
                    LocalizationIds.Mod.ArchersTunicDescription,
                    enchantmentGuids: new[]
                    {
                        ModBlueprintIds.Enchantments.ArchersTunic
                    },
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.EstrodTowerBasementBox,
                            "Tower of Estrod basement box",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_IroriAcolytesArmor",
                    ModBlueprintIds.Items.IroriAcolytesArmor,
                    GameBlueprintIds.Items.BreastplatePlus2,
                    LocalizationIds.Mod.IroriAcolytesArmorName,
                    LocalizationIds.Mod.IroriAcolytesArmorDescription,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.LostChapelLibraryCupboard,
                            "Lost Chapel library cupboard",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_IroriAdeptsArmor",
                    ModBlueprintIds.Items.IroriAdeptsArmor,
                    GameBlueprintIds.Items.BreastplatePlus3,
                    LocalizationIds.Mod.IroriAdeptsArmorName,
                    LocalizationIds.Mod.IroriAdeptsArmorDescription,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.IvorySanctumXantirNotes,
                            "Ivory Sanctum Xanthir notes",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_BillyPilgrimageRecord",
                    ModBlueprintIds.Items.BillyPilgrimageRecord,
                    GameBlueprintIds.Items.ZachariusNecromancy,
                    LocalizationIds.Mod.BillyPilgrimageRecordName,
                    LocalizationIds.Mod.BillyPilgrimageRecordDescription,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.KenabresBurningChestNearBridge,
                            "Market Square chest near bridge",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_BattleMageVest",
                    ModBlueprintIds.Items.BattleMageVest,
                    GameBlueprintIds.Items.StuddedHolyAgainstBlindnessPlus3,
                    LocalizationIds.Mod.BattleMageVestName,
                    LocalizationIds.Mod.BattleMageVestDescription,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.DrezenCitadelLevel2GoodLootChest,
                            "Drezen Citadel level 2 good loot chest",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_CutpurseVest",
                    ModBlueprintIds.Items.CutpurseVest,
                    GameBlueprintIds.Items.StuddedLeatherPlus1,
                    LocalizationIds.Mod.CutpurseVestName,
                    LocalizationIds.Mod.CutpurseVestDescription,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InChestLoot(
                            GameBlueprintIds.Loot.KenabresBurningHouseWithDemonsChest,
                            "Market Square House with Demons chest",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_ScythePlus1",
                    ModBlueprintIds.Items.ScythePlus1,
                    GameBlueprintIds.Items.ScythePlus1,
                    LocalizationIds.Mod.ScythePlus1Name,
                    LocalizationIds.Mod.ScythePlus1Description,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InMapObjectLoot(
                            "bcfce8e8-f634-446f-9a7f-9974d5c51c01",
                            "Shield Maze Weapon Rack",
                            count: 1,
                            identify: true)
                    }),

                new CustomItemDefinition(
                    "WotrMod_ShortswordPlus1",
                    ModBlueprintIds.Items.ShortswordPlus1,
                    GameBlueprintIds.Items.ShortswordPlus1,
                    LocalizationIds.Mod.ShortswordPlus1Name,
                    LocalizationIds.Mod.ShortswordPlus1Description,
                    placements: new[]
                    {
                        ItemPlacementDefinition.InMapObjectLoot(
                            "bcfce8e8-f634-446f-9a7f-9974d5c51c01",
                            "Shield Maze Weapon Rack",
                            count: 1,
                            identify: true)
                    }),

            });

        public static IReadOnlyList<CustomItemDefinition> GetAll()
        {
            return AllDefinitions;
        }
    }
}
