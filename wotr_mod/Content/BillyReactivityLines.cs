using System;
using System.Collections.Generic;
using System.Linq;

namespace wotr_mod.Content
{
    public enum BillyReactivityCategory
    {
        GeneralDemonEncounter,
        DemonBoss,
        Crusaders,
        LawfulChoice,
        ChaoticChoice,
        EvilChoice,
        GoodChoice,
        UndeadEncounter,
        PowerfulUndead,
        Pharasma,
        Irori,
        MythicPower,
        MythicAngel,
        MythicDemon,
        MythicAeon,
        MythicLich,
        MythicTrickster,
        MythicAzata,
        MythicGoldDragon,
        MythicLegend,
        MythicSwarm,
        Drezen,
        WorldMap,
        Death,
        Identity
    }

    internal static class BillyReactivityLines
    {
        private static readonly Dictionary<BillyReactivityCategory, string[]> Lines =
            new Dictionary<BillyReactivityCategory, string[]>();

        public static IReadOnlyList<string> Get(BillyReactivityCategory category)
        {
            return Lines.TryGetValue(category, out var lines)
                ? lines
                : Array.Empty<string>();
        }

        public static IEnumerable<string> All()
        {
            return Lines.Values.SelectMany(lines => lines);
        }
    }
}
