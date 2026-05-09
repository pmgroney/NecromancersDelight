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
            new Dictionary<BillyReactivityCategory, string[]>
            {
                {
                    BillyReactivityCategory.GeneralDemonEncounter,
                    new[]
                    {
                        "Chaotic. Predictable.",
                        "They lack form. And hygiene.",
                        "Noise without discipline.",
                        "Focus. They certainly won't."
                    }
                },
                {
                    BillyReactivityCategory.DemonBoss,
                    new[]
                    {
                        "Better. Maybe this one survives two minutes.",
                        "Finally, resistance.",
                        "Let's see if you're actually memorable."
                    }
                },
                {
                    BillyReactivityCategory.Crusaders,
                    new[]
                    {
                        "Order... trying its best.",
                        "They try. That's adorable.",
                        "Discipline without consistency. Still better than demons."
                    }
                },
                {
                    BillyReactivityCategory.LawfulChoice,
                    new[]
                    {
                        "Structure holds.",
                        "Correct decision.",
                        "Progress through order."
                    }
                },
                {
                    BillyReactivityCategory.ChaoticChoice,
                    new[]
                    {
                        "I would not have chosen that.",
                        "Bold. In the worst way.",
                        "We'll see how badly this goes."
                    }
                },
                {
                    BillyReactivityCategory.EvilChoice,
                    new[]
                    {
                        "Efficient. Concerning, but efficient.",
                        "Power without control always rots eventually.",
                        "Short-term gain. Future catastrophe."
                    }
                },
                {
                    BillyReactivityCategory.GoodChoice,
                    new[]
                    {
                        "Mercy requires discipline.",
                        "Restraint is harder than violence.",
                        "Acceptable."
                    }
                },
                {
                    BillyReactivityCategory.UndeadEncounter,
                    new[]
                    {
                        "Unfocused. Embarrassing.",
                        "Undeath without purpose is just flailing.",
                        "They stopped improving."
                    }
                },
                {
                    BillyReactivityCategory.PowerfulUndead,
                    new[]
                    {
                        "Closer... but still sloppy.",
                        "Control without discipline.",
                        "Power first. Wisdom never."
                    }
                },
                {
                    BillyReactivityCategory.Pharasma,
                    new[]
                    {
                        "Yes, yes, I'm aware she's unhappy.",
                        "I'll sort it out eventually.",
                        "One existential crisis at a time."
                    }
                },
                {
                    BillyReactivityCategory.Irori,
                    new[]
                    {
                        "Stillness. Even now.",
                        "The path continues.",
                        "Form persists beyond flesh."
                    }
                },
                {
                    BillyReactivityCategory.MythicPower,
                    new[]
                    {
                        "This changes the equation.",
                        "Power... usually makes people stupid.",
                        "We refine further."
                    }
                },
                {
                    BillyReactivityCategory.MythicAngel,
                    new[]
                    {
                        "Structured. Purposeful.",
                        "This aligns... mostly."
                    }
                },
                {
                    BillyReactivityCategory.MythicDemon,
                    new[]
                    {
                        "Power without restraint.",
                        "This feels like a terrible idea."
                    }
                },
                {
                    BillyReactivityCategory.MythicAeon,
                    new[]
                    {
                        "Correction. I respect that.",
                        "Balance enforced."
                    }
                },
                {
                    BillyReactivityCategory.MythicLich,
                    new[]
                    {
                        "I see the appeal.",
                        "...and the flaw."
                    }
                },
                {
                    BillyReactivityCategory.MythicTrickster,
                    new[]
                    {
                        "Unpredictable.",
                        "I miss reliable physics."
                    }
                },
                {
                    BillyReactivityCategory.MythicAzata,
                    new[]
                    {
                        "Freedom without structure.",
                        "Effective. Inconsistently."
                    }
                },
                {
                    BillyReactivityCategory.MythicGoldDragon,
                    new[]
                    {
                        "Discipline with compassion.",
                        "Rare."
                    }
                },
                {
                    BillyReactivityCategory.MythicLegend,
                    new[]
                    {
                        "No shortcuts. Good.",
                        "Pure refinement."
                    }
                },
                {
                    BillyReactivityCategory.MythicSwarm,
                    new[]
                    {
                        "No self. No mastery.",
                        "This is failure."
                    }
                },
                {
                    BillyReactivityCategory.Drezen,
                    new[]
                    {
                        "A foundation. Build it correctly.",
                        "Structures matter. So do habits."
                    }
                },
                {
                    BillyReactivityCategory.WorldMap,
                    new[]
                    {
                        "Distance is irrelevant.",
                        "We proceed.",
                        "Good. More walking. My favorite eternal activity."
                    }
                },
                {
                    BillyReactivityCategory.Death,
                    new[]
                    {
                        "Get up. Preferably less dramatically.",
                        "Death is rarely the end around here.",
                        "I'll handle this. Again."
                    }
                },
                {
                    BillyReactivityCategory.Identity,
                    new[]
                    {
                        "I am not a mistake. Probably.",
                        "The body failed. The discipline stayed stubborn.",
                        "I continue. Spite helps."
                    }
                }
            };

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
