using System.Collections.Generic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content.Localization
{
    internal static class ModText
    {
        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            { LocalizationIds.Mod.EvokerName, "Evoker" },
            { LocalizationIds.Mod.EvokerDescription, "An Evoker is a living conduit of raw, untamed power, channeling destructive arcane forces into devastating spells." },
            { LocalizationIds.Mod.ShadowbornName, "Shadowborn" },
            { LocalizationIds.Mod.ShadowbornDescription, "A Shadowborn follows a dark reflection of the Evoker's Pyromancer bloodline, replacing the usual bloodline choice with negative-energy powers shaped from living shadow." },
            { LocalizationIds.Mod.ShadowbornBloodlineName, "Shadowborn" },
            { LocalizationIds.Mod.ShadowbornBloodlineDescription, "Your bloodline burns with lightless power, shaping the Pyromancer's destructive gifts into negative energy." },
            { LocalizationIds.Mod.ShadowbornArcanaName, "Umbral Arcana" },
            { LocalizationIds.Mod.ShadowbornArcanaDescription, "Your damaging spells can be infused with living shadow, changing elemental damage into negative energy." },
            { LocalizationIds.Mod.ShadowbornBurningHandsName, "Shadow Hands" },
            { LocalizationIds.Mod.ShadowbornBurningHandsDescription, "A cone of grasping shadow deals negative energy damage to creatures in its path." },
            { LocalizationIds.Mod.ShadowbornScorchingRayName, "Shadow Ray" },
            { LocalizationIds.Mod.ShadowbornScorchingRayDescription, "A ray of concentrated shadow deals negative energy damage with a ranged touch attack." },
            { LocalizationIds.Mod.ShadowbornUmbralRayName, "Umbral Ray" },
            { LocalizationIds.Mod.ShadowbornUmbralRayDescription, "You can unleash a ray of living shadow as a standard action, targeting any foe within 30 feet as a ranged touch attack. This ray deals 1d6 negative energy damage plus 1d6 for every two Evoker levels beyond 1st. You can use this ability a number of times per day equal to 3 + your Charisma modifier." },
            { LocalizationIds.Mod.ShadowbornUmbralBlastName, "Umbral Blast" },
            { LocalizationIds.Mod.ShadowbornUmbralBlastDescription, "At 9th level, you can release a 20-foot burst of living shadow. This blast deals 1d6 negative energy damage per Evoker level. A successful Reflex save halves the damage." },
            { LocalizationIds.Mod.ShadowbornResistanceName, "Umbral Resistance" },
            { LocalizationIds.Mod.ShadowbornResistanceDescription, "At 3rd level, your shadowed blood grants negative energy resistance 10. At 9th level, this resistance increases to 20." },
            { LocalizationIds.Mod.NecromancerName, "Necromancer" },
            { LocalizationIds.Mod.NecromancerDescription, "A spontaneous arcane caster who rejects traditional bloodlines and draws power from death itself." },
            { LocalizationIds.Mod.SepulchritName, "Sepulchrit" },
            { LocalizationIds.Mod.SepulchritDescription, "A Sepulchrit approaches necromancy through study, formulae, and the patient cataloging of death. This archetype uses Intelligence instead of Charisma for necromancer spellcasting." },
            { LocalizationIds.Mod.GravebladeName, "Graveblade" },
            { LocalizationIds.Mod.GravebladeDescription, "A Graveblade drags necromancy onto the front line, feeding on the nearness of death to sharpen steel, harden sinew, and strike with the sure, practiced accuracy of a dedicated warrior. Graveblades use full base attack bonus progression." },
            { LocalizationIds.Mod.GravebladeProficienciesName, "Graveblade Proficiencies" },
            { LocalizationIds.Mod.GravebladeProficienciesDescription, "Graveblades are proficient with light armor, medium armor, heavy armor, and all martial weapons." },
            { LocalizationIds.Mod.GravebladeBonusFeatName, "Graveblade Bonus Feat" },
            { LocalizationIds.Mod.GravebladeBonusFeatDescription, "At 2nd, 6th, 10th, and 16th level, a graveblade can draw from the necromancer bonus feat list or from the martial techniques normally reserved for fighters." },
            { LocalizationIds.Mod.GravebladeFighterTrainingName, "Graveblade Training" },
            { LocalizationIds.Mod.GravebladeFighterTrainingDescription, "A graveblade's necromantic combat drills count their graveblade levels as fighter levels when qualifying for feats chosen through Graveblade Bonus Feat." },
            { LocalizationIds.Mod.GravebladeArmorMasteryName, "Graveforged Armor" },
            { LocalizationIds.Mod.GravebladeArmorMasteryDescription, "At 19th level, a graveblade's armor becomes a reliquary of battle-scarred bone and cold iron. While wearing armor, the graveblade gains DR 10/-." },
            { LocalizationIds.Mod.GravebladeReapingEdgeName, "The Reaping Edge" },
            { LocalizationIds.Mod.GravebladeReapingEdgeDescription, "As a swift action, you can turn your weapon into a conduit for the Weight of the Grave for 1 minute. You can use this ability a number of times per day equal to your Necromancer level x2 + your Charisma modifier.\n\nAt 1st level, your weapon deals an extra 1d6 negative energy damage and is treated as magical for overcoming damage reduction. This damage increases by 1d6 at every odd Necromancer level, and each mythic level counts as two levels for this damage scaling. The extra damage is doubled while wielding a scythe.\n\nAt 5th level, enemies hit by The Reaping Edge take a -2 penalty to AC for 1 round.\n\nAt 10th level, your weapon is treated as evil for overcoming damage reduction.\n\nAt 15th level, critical hits fatigue the target for 1 minute with no save. If the target is already fatigued, it becomes exhausted instead.\n\nAt 20th level, if you kill an enemy with a weapon attack while The Reaping Edge is active, bone shards deal your weapon's base damage to adjacent enemies." },
            { LocalizationIds.Mod.GravebladeReapingEdgeBuffName, "The Reaping Edge" },
            { LocalizationIds.Mod.GravebladeReapingEdgeBuffDescription, "Your weapon carries the Weight of the Grave." },
            { LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneName, "Brittle Bone" },
            { LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneDescription, "Armor and flesh turn brittle, causing a -2 penalty to AC." },
            { LocalizationIds.Mod.GravebladeReapingEdgeFatigueName, "Lingering Rot" },
            { LocalizationIds.Mod.GravebladeReapingEdgeFatigueDescription, "The target is fatigued by necrotic rot." },
            { LocalizationIds.Mod.GravebladeReapingEdgeExhaustionName, "Lingering Rot" },
            { LocalizationIds.Mod.GravebladeReapingEdgeExhaustionDescription, "The target is exhausted by necrotic rot." },
            { LocalizationIds.Mod.NecromancerProficienciesName, "Necromancer Proficiencies" },
            { LocalizationIds.Mod.NecromancerProficienciesDescription, "Necromancers are proficient with all simple weapons and the scythe." },
            { LocalizationIds.Mod.NecromancerBonusFeatName, "Necromancer Bonus Feat" },
            { LocalizationIds.Mod.NecromancerBonusFeatDescription, "At 6th, 10th, and 16th level, a necromancer gains a bonus feat in addition to those gained from normal advancement. These bonus feats must be selected from those listed as combat feats, metamagic feats, or Spell Focus." },
            { LocalizationIds.Mod.EvokerBloodlineName, "Evoker Bloodline" },
            { LocalizationIds.Mod.EvokerBloodlineDescription, "Select an evoker bloodline. Evokers channel their power through a narrow set of destructive traditions." },
            { LocalizationIds.Mod.NecromancerBloodlineName, "Necromancer" },
            { LocalizationIds.Mod.NecromancerBloodlineDescription, "Necromancers draw power from death and undeath." },
            { LocalizationIds.Mod.NecromancerBloodlineArcanaName, "Master of Death" },
            { LocalizationIds.Mod.NecromancerBloodlineArcanaDescription, "Your natural affinity to death grants a +1 damage bonus per die for necromancy spells cast from your Necromancer spellbook." },
            { LocalizationIds.Mod.NecromancerBloodlinePower1Name, "Withering Ray" },
            { LocalizationIds.Mod.NecromancerBloodlinePower1Description, "At 1st level, you can unleash a withering ray as a standard action, targeting any foe within 30 feet as a ranged touch attack. This ray deals 1d6 unholy damage, plus 1d6 for every two Necromancer levels beyond 1st. You can use this ability a number of times per day equal to 3 + your Charisma modifier." },
            { LocalizationIds.Mod.NecromancerBloodlinePower3Name, "Death's Gift" },
            { LocalizationIds.Mod.NecromancerBloodlinePower3Description, "At 3rd level, you gain DR 5/magic and cold resistance 5. These bonuses improve to 10 at 9th level and 20 at 15th level." },
            { LocalizationIds.Mod.NecromancerBloodlinePower9Name, "Grasp of the Dead" },
            { LocalizationIds.Mod.NecromancerBloodlinePower9Description, "At 9th level, you can cause a swarm of skeletal arms to burst from the ground in a 20-foot burst. This deals 1d6 points of slashing damage per caster level." },
            { LocalizationIds.Mod.NecromancerBloodlinePower15Name, "Incorporeal Form" },
            { LocalizationIds.Mod.NecromancerBloodlinePower15Description, "At 15th level, you can become incorporeal for a number of rounds equal to your caster level." },
            { LocalizationIds.Mod.NecromancerBloodlinePower20Name, "One of Us" },
            { LocalizationIds.Mod.NecromancerBloodlinePower20Description, "At 20th level, you become one of the undead. You gain immunity to cold, nonlethal damage, paralysis, and sleep." },
            { LocalizationIds.Mod.NecromancerBoneArmorName, "Bone Armor" },
            { LocalizationIds.Mod.NecromancerBoneArmorDescription, "A silent ward of bone encases you, granting a +1 natural armor bonus to AC. This bonus increases by +1 at 5th level and every four levels thereafter, to a maximum of +5 at 17th level." },
            { LocalizationIds.Mod.NecromancerMasterOfDeathClassCardDescription, "Necromancers turn the cold grammar of death into a weapon. Their necromancy spells strike with crueler force, drawing extra power from every die of damage when cast through the Necromancer spellbook." },
            { LocalizationIds.Mod.NecromancerBoneArmorClassCardDescription, "A Necromancer is never truly unguarded. Pale plates, splinters, and spectral ribs gather around them as they grow in power, forming a grim ward that hardens their defenses without slowing their spellcasting." },
            { LocalizationIds.Mod.EvokerArcaneName, "Arcanist" },
            { LocalizationIds.Mod.EvokerArcaneDescription, "Your blood hums with raw arcane resonance, shaping pure magical force into devastating expressions of power." },
            { LocalizationIds.Mod.EvokerAirName, "Stormcaller" },
            { LocalizationIds.Mod.EvokerAirDescription, "Lightning dances at your fingertips and thunder rolls in your wake." },
            { LocalizationIds.Mod.EvokerEarthName, "Vitriomancer" },
            { LocalizationIds.Mod.EvokerEarthDescription, "Your power manifests as virulent acid and caustic corruption." },
            { LocalizationIds.Mod.EvokerFireName, "Pyromancer" },
            { LocalizationIds.Mod.EvokerFireDescription, "Flame is your birthright, and your magic burns with unrestrained intensity." },
            { LocalizationIds.Mod.EvokerWaterName, "Glacialist" },
            { LocalizationIds.Mod.EvokerWaterDescription, "Frost answers your call, locking enemies in killing cold." }
        };

        public static void Register(LocalizationTool localization)
        {
            foreach (var pair in Strings)
            {
                localization.Put(pair.Key, pair.Value);
            }
        }
    }
}
