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
                            GameBlueprintIds.Loot.CultistsLairLuxeryCasket,
                            "Cultists' Lair luxury casket",
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
                            GameBlueprintIds.Loot.KenabresBurningCrusaderCorpseWithScroll,
                            "Market Square crusader corpse with scroll",
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
