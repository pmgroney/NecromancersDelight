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
                
            });

        public static IReadOnlyList<CustomItemDefinition> GetAll()
        {
            return AllDefinitions;
        }
    }
}
