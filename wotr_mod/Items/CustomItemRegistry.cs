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
                    "WotrMod_ScythePlus1",
                    ModBlueprintIds.Items.ScythePlus1,
                    GameBlueprintIds.Items.ScythePlus1,
                    LocalizationIds.Mod.ScythePlus1Name,
                    LocalizationIds.Mod.ScythePlus1Description,
                    placements: new[]
                    {
                        ItemPlacementDefinition.OnUnit(
                            GameBlueprintIds.Units.ShieldMazeShittyCultist,
                            "Shield Maze Cultist",
                            GameBlueprintIds.Loot.ShieldMazeShittyCultistScytheLoot,
                            count: 1,
                            identify: true),
                        ItemPlacementDefinition.InMapObjectLoot(
                            "1d94f397-8b18-412c-a90c-43e414f71f0a",
                            "Shield Maze Visible Shelf",
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