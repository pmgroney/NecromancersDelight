using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using wotr_mod.Features;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CustomHeritagePatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly HeritageFactory _heritageFactory;

        public CustomHeritagePatch(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
            _heritageFactory = new HeritageFactory(blueprints, localization);
        }

        public string Name => "Custom Heritages";

        public void RegisterLocalization()
        {
            _localization.Put(LocalizationIds.Mod.UmbralDhampirHeritageName, "Umbral Dhampir");
            _localization.Put(
                LocalizationIds.Mod.UmbralDhampirHeritageDescription,
                "These dhampirs carry an unusually deep shadow in their blood. They gain a +3 racial bonus to Charisma, a +3 racial bonus to Dexterity, and a +2 racial bonus to Constitution.");

            _localization.Put(LocalizationIds.Mod.CryptguardDhampirHeritageName, "Cryptguard");
            _localization.Put(
                LocalizationIds.Mod.CryptguardDhampirHeritageDescription,
                "These dhampirs are born to be guardians of the dead, possessing unnatural strength. They gain a +3 racial bonus to Strength, a +2 racial bonus to Dexterity, and a +3 racial bonus to Charisma.");

            _localization.Put(LocalizationIds.Mod.GraveltoeGnomeHeritageName, "Graveltoe Gnome");
            _localization.Put(
                LocalizationIds.Mod.GraveltoeGnomeHeritageDescription,
                "These gnomes are bright-eyed, quick, and have the strength of the earth. They gain a +3 racial bonus to Charisma, a +2 racial bonus to Dexterity, and a +3 racial bonus to Strength.");

            _localization.Put(LocalizationIds.Mod.ShadowGnomeHeritageName, "Shadow Gnome");
            _localization.Put(
                LocalizationIds.Mod.ShadowGnomeHeritageDescription,
                "These gnomes move like living silhouettes, charming and elusive. They gain a +3 racial bonus to Charisma and a +2 racial bonus to Dexterity.");

            _localization.Put(LocalizationIds.Mod.HumanHeritageName, "Human Heritage");
            _localization.Put(
                LocalizationIds.Mod.HumanHeritageDescription,
                "The circumstances of a human's birth and ancestry can shape their natural gifts.");

            _localization.Put(LocalizationIds.Mod.NormalHumanHeritageName, "Normal");
            _localization.Put(
                LocalizationIds.Mod.NormalHumanHeritageDescription,
                "These humans follow the standard human heritage.");

            _localization.Put(LocalizationIds.Mod.DescendantOfKingsHeritageName, "Descendant of Kings");
            _localization.Put(
                LocalizationIds.Mod.DescendantOfKingsHeritageDescription,
                "These humans descend from old lines of rulers and conquerors. They gain a +2 racial bonus to Constitution, and their selectable human ability score bonus increases from +2 to +4.");

            _localization.Put(LocalizationIds.Mod.OrcLordsBloodHeritageName, "Orc Lords Blood");
            _localization.Put(
                LocalizationIds.Mod.OrcLordsBloodHeritageDescription,
                "These half-orcs carry the blood of ancient warlords. They gain a +2 racial bonus to Constitution, and their selectable half-orc ability score bonus increases from +2 to +4.");

            _localization.Put(LocalizationIds.Mod.TrueHighElfHeritageName, "True High Elf");
            _localization.Put(
                LocalizationIds.Mod.TrueHighElfHeritageDescription,
                "These half-elves are touched by an old and potent elven lineage. They gain a +2 racial bonus to Constitution, and their selectable half-elf ability score bonus increases from +2 to +4.");
        }

        public void Apply()
        {
            var dhampirHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.DhampirHeritage,
                "Dhampir heritage selection");
            var gnomeHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.GnomeHeritage,
                "Gnome heritage selection");
            var halfElfHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.HalfElfHeritage,
                "Half-elf heritage selection");
            var halfOrcHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.HalfOrcHeritage,
                "Half-orc heritage selection");
            var humanRace = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Human,
                "Human race");
            var humanHeritageSelection = _heritageFactory.EnsureSelection(
                ModBlueprintIds.Selections.HumanHeritage,
                "WotrMod_HumanHeritageSelection",
                LocalizationIds.Mod.HumanHeritageName,
                LocalizationIds.Mod.HumanHeritageDescription);

            var umbralDhampir = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.UmbralDhampirHeritage,
                "WotrMod_UmbralDhampirHeritage",
                LocalizationIds.Mod.UmbralDhampirHeritageName,
                LocalizationIds.Mod.UmbralDhampirHeritageDescription,
                _heritageFactory.CreateStatBonus("UmbralDhampir", StatType.Charisma, 3),
                _heritageFactory.CreateStatBonus("UmbralDhampir", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("UmbralDhampir", StatType.Constitution, 2));

            var cryptguardDhampir = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.CryptguardDhampirHeritage,
                "WotrMod_CryptguardDhampirHeritage",
                LocalizationIds.Mod.CryptguardDhampirHeritageName,
                LocalizationIds.Mod.CryptguardDhampirHeritageDescription,
                _heritageFactory.CreateStatBonus("CryptguardDhampir", StatType.Strength, 3),
                _heritageFactory.CreateStatBonus("CryptguardDhampir", StatType.Dexterity, 2),
                _heritageFactory.CreateStatBonus("CryptguardDhampir", StatType.Charisma, 3));

            var graveltoeGnome = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.GraveltoeGnomeHeritage,
                "WotrMod_GraveltoeGnomeHeritage",
                LocalizationIds.Mod.GraveltoeGnomeHeritageName,
                LocalizationIds.Mod.GraveltoeGnomeHeritageDescription,
                _heritageFactory.CreateStatBonus("GraveltoeGnome", StatType.Charisma, 1),
                _heritageFactory.CreateStatBonus("GraveltoeGnome", StatType.Strength, 3));

            var shadowGnome = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.ShadowGnomeHeritage,
                "WotrMod_ShadowGnomeHeritage",
                LocalizationIds.Mod.ShadowGnomeHeritageName,
                LocalizationIds.Mod.ShadowGnomeHeritageDescription,
                _heritageFactory.CreateStatBonus("ShadowGnome", StatType.Charisma, 1),
                _heritageFactory.CreateStatBonus("ShadowGnome", StatType.Dexterity, 2));

                var normalHuman = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.NormalHumanHeritage,
                "WotrMod_NormalHumanHeritage",
                LocalizationIds.Mod.NormalHumanHeritageName,
                LocalizationIds.Mod.NormalHumanHeritageDescription);

            var descendantOfKings = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.DescendantOfKingsHeritage,
                "WotrMod_DescendantOfKingsHeritage",
                LocalizationIds.Mod.DescendantOfKingsHeritageName,
                LocalizationIds.Mod.DescendantOfKingsHeritageDescription,
                _heritageFactory.CreateStatBonus("DescendantOfKings", StatType.Constitution, 2),
                _heritageFactory.CreateSelectedRaceStatBonus("DescendantOfKings", 2));

            var orcLordsBlood = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.OrcLordsBloodHeritage,
                "WotrMod_OrcLordsBloodHeritage",
                LocalizationIds.Mod.OrcLordsBloodHeritageName,
                LocalizationIds.Mod.OrcLordsBloodHeritageDescription,
                _heritageFactory.CreateStatBonus("OrcLordsBlood", StatType.Constitution, 2),
                _heritageFactory.CreateSelectedRaceStatBonus("OrcLordsBlood", 2));

            var trueHighElf = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.TrueHighElfHeritage,
                "WotrMod_TrueHighElfHeritage",
                LocalizationIds.Mod.TrueHighElfHeritageName,
                LocalizationIds.Mod.TrueHighElfHeritageDescription,
                _heritageFactory.CreateStatBonus("TrueHighElf", StatType.Constitution, 2),
                _heritageFactory.CreateSelectedRaceStatBonus("TrueHighElf", 2));

            RemoveGnomeStrengthPenalty(graveltoeGnome, shadowGnome);

            _blueprints.AddFeatureToSelection(dhampirHeritageSelection, umbralDhampir);
            _blueprints.AddFeatureToSelection(dhampirHeritageSelection, cryptguardDhampir);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, graveltoeGnome);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, shadowGnome);
            _blueprints.AddFeatureToSelection(halfElfHeritageSelection, trueHighElf);
            _blueprints.AddFeatureToSelection(halfOrcHeritageSelection, orcLordsBlood);
            _blueprints.AddFeatureToSelection(humanHeritageSelection, normalHuman);
            _blueprints.AddFeatureToSelection(humanHeritageSelection, descendantOfKings);
            _blueprints.AddFeatureToRace(humanRace, humanHeritageSelection);
        }

        private void RemoveGnomeStrengthPenalty(params BlueprintFeature[] heritages)
        {
            var gnomeRace = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Gnome,
                "Gnome race");
            var penaltyExemptions = (heritages ?? new BlueprintFeature[0])
                .Where(heritage => heritage != null)
                .ToArray();

            foreach (var statPenalty in _blueprints.GetComponents<AddStatBonusIfHasFact>(gnomeRace)
                         .Where(component =>
                             component.Stat == StatType.Strength &&
                             component.Descriptor == ModifierDescriptor.Racial &&
                             component.InvertCondition))
            {
                foreach (var heritage in penaltyExemptions)
                {
                    _blueprints.AddCheckedFact(statPenalty, heritage);
                }
            }

            foreach (var recalculate in _blueprints.GetComponents<RecalculateOnFactsChange>(gnomeRace))
            {
                foreach (var heritage in penaltyExemptions)
                {
                    _blueprints.AddCheckedFact(recalculate, heritage);
                }
            }
        }

    }
}
