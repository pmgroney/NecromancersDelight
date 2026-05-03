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
                        "They lack form. I'll correct that.",
                        "Noise without discipline.",
                        "Focus. They won't."
                    }
                },
                {
                    BillyReactivityCategory.DemonBoss,
                    new[]
                    {
                        "Better. This might teach me something.",
                        "Finally, resistance.",
                        "Let's see if you're worth the effort."
                    }
                },
                {
                    BillyReactivityCategory.Crusaders,
                    new[]
                    {
                        "Order... attempting to assert itself.",
                        "They try. That matters.",
                        "Discipline without consistency. Still better than chaos."
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
                        "Unstable path.",
                        "We'll see if it holds."
                    }
                },
                {
                    BillyReactivityCategory.EvilChoice,
                    new[]
                    {
                        "Efficient. Not optimal.",
                        "Power without control degrades quickly.",
                        "Short-term gain. Long-term cost."
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
                        "Unfocused. That's the problem.",
                        "Undeath without purpose is just decay.",
                        "They stopped improving."
                    }
                },
                {
                    BillyReactivityCategory.PowerfulUndead,
                    new[]
                    {
                        "Closer... but still flawed.",
                        "Control without discipline.",
                        "They chose power. Not mastery."
                    }
                },
                {
                    BillyReactivityCategory.Pharasma,
                    new[]
                    {
                        "Yes, I know. I'm on the list.",
                        "I'll resolve it. Eventually.",
                        "One problem at a time."
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
                        "Power... requires control.",
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
                        "I'll compensate."
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
                        "I dislike relying on luck."
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
                        "Time is no longer a constraint."
                    }
                },
                {
                    BillyReactivityCategory.Death,
                    new[]
                    {
                        "Get up. Preferably the correct way.",
                        "Death is not the end. Usually.",
                        "I'll handle this."
                    }
                },
                {
                    BillyReactivityCategory.Identity,
                    new[]
                    {
                        "I am not a mistake. I am... unresolved.",
                        "The body failed. The discipline didn't.",
                        "I continue. That's enough."
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
