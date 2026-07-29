using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CustomHeritagePatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly HeritageFactory _heritageFactory;

        public CustomHeritagePatch(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _heritageFactory = new HeritageFactory(blueprints, localization);
        }

        public string Name => "Custom Heritages";

        public void RegisterLocalization()
        {
        }

        public void Apply()
        {
            var gnomeHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.GnomeHeritage,
                "Gnome heritage selection");
            var halflingHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.HalflingHeritage,
                "Halfling heritage selection");
            var pyromaniacGnome = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.PyromaniacGnome,
                "Pyromaniac gnome heritage");
            var slowSpeedGnome = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.SlowSpeedGnome,
                "Gnome slow speed feature");
            var slowSpeedHalfling = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.SlowSpeedHalfling,
                "Halfling slow speed feature");
            var halfElfHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.HalfElfHeritage,
                "Half-elf heritage selection");
            var halfOrcHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.HalfOrcHeritage,
                "Half-orc heritage selection");
            var humanRace = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Human,
                "Human race");
            var gnomeRace = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Gnome,
                "Gnome race");
            var halflingRace = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Halfling,
                "Halfling race");
            var humanHeritageSelection = _heritageFactory.EnsureSelection(
                ModBlueprintIds.Selections.HumanHeritage,
                "WotrMod_HumanHeritageSelection",
                LocalizationIds.Mod.HumanHeritageName,
                LocalizationIds.Mod.HumanHeritageDescription);

            var removeGraveltoeSlowSpeed = CreateRemoveFeature(
                slowSpeedGnome,
                "$RemoveFeatureOnApply$GraveltoeGnomeSlowSpeed");
            var graveltoeGnome = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.GraveltoeGnomeHeritage,
                "WotrMod_GraveltoeGnomeHeritage",
                LocalizationIds.Mod.GraveltoeGnomeHeritageName,
                LocalizationIds.Mod.GraveltoeGnomeHeritageDescription,
                removeGraveltoeSlowSpeed,
                _heritageFactory.CreateStatBonus("GraveltoeGnome", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("GraveltoeGnome", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("GraveltoeGnome", StatType.Strength, 3));

            var removeShadowSlowSpeed = CreateRemoveFeature(
                slowSpeedGnome,
                "$RemoveFeatureOnApply$ShadowGnomeSlowSpeed");
            var shadowGnome = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.ShadowGnomeHeritage,
                "WotrMod_ShadowGnomeHeritage",
                LocalizationIds.Mod.ShadowGnomeHeritageName,
                LocalizationIds.Mod.ShadowGnomeHeritageDescription,
                removeShadowSlowSpeed,
                _heritageFactory.CreateStatBonus("ShadowGnome", StatType.Charisma, 1),
                _heritageFactory.CreateStatBonus("ShadowGnome", StatType.Dexterity, 3));

            var removeGlintpebbleSlowSpeed = CreateRemoveFeature(
                slowSpeedGnome,
                "$RemoveFeatureOnApply$GlintpebbleGnomeSlowSpeed");
            var glintpebbleGnome = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.GlintpebbleGnomeHeritage,
                "WotrMod_GlintpebbleGnomeHeritage",
                LocalizationIds.Mod.GlintpebbleGnomeHeritageName,
                LocalizationIds.Mod.GlintpebbleGnomeHeritageDescription,
                removeGlintpebbleSlowSpeed,
                _heritageFactory.CreateStatBonus("GlintpebbleGnome", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("GlintpebbleGnome", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("GlintpebbleGnome", StatType.Wisdom, 3));

            var addPyromaniac = new AddFeatureOnApply { name = "$AddFeatureOnApply$CleverPyromaniacGnome" };
            _blueprints.SetAddFeatureOnApplyFeature(addPyromaniac, pyromaniacGnome);
            var removeCleverPyromaniacSlowSpeed = CreateRemoveFeature(
                slowSpeedGnome,
                "$RemoveFeatureOnApply$CleverPyromaniacGnomeSlowSpeed");
            var cleverPyromaniac = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.CleverPyromaniacGnome,
                "CleverPyromaniacGnome",
                LocalizationIds.Mod.CleverPyromaniacName,
                LocalizationIds.Mod.CleverPyromaniacDescription,
                addPyromaniac,
                removeCleverPyromaniacSlowSpeed,
                _heritageFactory.CreateStatBonus("CleverPyromaniacGnome", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("CleverPyromaniacGnome", StatType.Constitution, 2),
                _heritageFactory.CreateStatBonus("CleverPyromaniacGnome", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("CleverPyromaniacGnome", StatType.Intelligence, 3));

            var hearthsongHalfling = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.HearthsongHalflingHeritage,
                "WotrMod_HearthsongHalflingHeritage",
                LocalizationIds.Mod.HearthsongHalflingHeritageName,
                LocalizationIds.Mod.HearthsongHalflingHeritageDescription,
                CreateRemoveFeature(slowSpeedHalfling, "$RemoveFeatureOnApply$HearthsongHalflingSlowSpeed"),
                _heritageFactory.CreateStatBonus("HearthsongHalfling", StatType.Dexterity, 1),
                _heritageFactory.CreateStatBonus("HearthsongHalfling", StatType.Charisma, 1),
                _heritageFactory.CreateStatBonus("HearthsongHalfling", StatType.Constitution, 2));

            var stonebackHalfling = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.StonebackHalflingHeritage,
                "WotrMod_StonebackHalflingHeritage",
                LocalizationIds.Mod.StonebackHalflingHeritageName,
                LocalizationIds.Mod.StonebackHalflingHeritageDescription,
                CreateRemoveFeature(slowSpeedHalfling, "$RemoveFeatureOnApply$StonebackHalflingSlowSpeed"),
                _heritageFactory.CreateStatBonus("StonebackHalfling", StatType.Dexterity, 1),
                _heritageFactory.CreateStatBonus("StonebackHalfling", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("StonebackHalfling", StatType.Strength, 3),
                _heritageFactory.CreateStatBonus("StonebackHalfling", StatType.Constitution, 2));

            var starwatchHalfling = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.StarwatchHalflingHeritage,
                "WotrMod_StarwatchHalflingHeritage",
                LocalizationIds.Mod.StarwatchHalflingHeritageName,
                LocalizationIds.Mod.StarwatchHalflingHeritageDescription,
                CreateRemoveFeature(slowSpeedHalfling, "$RemoveFeatureOnApply$StarwatchHalflingSlowSpeed"),
                _heritageFactory.CreateStatBonus("StarwatchHalfling", StatType.Dexterity, 1),
                _heritageFactory.CreateStatBonus("StarwatchHalfling", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("StarwatchHalfling", StatType.Wisdom, 3),
                _heritageFactory.CreateStatBonus("StarwatchHalfling", StatType.Constitution, 2));

            var lorefinderHalfling = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.LorefinderHalflingHeritage,
                "WotrMod_LorefinderHalflingHeritage",
                LocalizationIds.Mod.LorefinderHalflingHeritageName,
                LocalizationIds.Mod.LorefinderHalflingHeritageDescription,
                CreateRemoveFeature(slowSpeedHalfling, "$RemoveFeatureOnApply$LorefinderHalflingSlowSpeed"),
                _heritageFactory.CreateStatBonus("LorefinderHalfling", StatType.Dexterity, 1),
                _heritageFactory.CreateStatBonus("LorefinderHalfling", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("LorefinderHalfling", StatType.Intelligence, 3),
                _heritageFactory.CreateStatBonus("LorefinderHalfling", StatType.Constitution, 2));

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
                _heritageFactory.CreateStatBonus("DescendantOfKings", StatType.Dexterity, 2),
                _heritageFactory.CreateSelectedRaceStatBonus("DescendantOfKings", 2));

            var orcLordsBlood = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.OrcLordsBloodHeritage,
                "WotrMod_OrcLordsBloodHeritage",
                LocalizationIds.Mod.OrcLordsBloodHeritageName,
                LocalizationIds.Mod.OrcLordsBloodHeritageDescription,
                _heritageFactory.CreateStatBonus("OrcLordsBlood", StatType.Constitution, 2),
                _heritageFactory.CreateStatBonus("OrcLordsBlood", StatType.Dexterity, 2),
                _heritageFactory.CreateSelectedRaceStatBonus("OrcLordsBlood", 2));

            var trueHighElf = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.TrueHighElfHeritage,
                "WotrMod_TrueHighElfHeritage",
                LocalizationIds.Mod.TrueHighElfHeritageName,
                LocalizationIds.Mod.TrueHighElfHeritageDescription,
                _heritageFactory.CreateStatBonus("TrueHighElf", StatType.Constitution, 2),
                _heritageFactory.CreateStatBonus("TrueHighElf", StatType.Dexterity, 2),
                _heritageFactory.CreateSelectedRaceStatBonus("TrueHighElf", 2));

            RemoveStrengthPenalty(
                gnomeRace,
                graveltoeGnome,
                shadowGnome,
                glintpebbleGnome,
                cleverPyromaniac);
            RemoveStrengthPenalty(
                halflingRace,
                hearthsongHalfling,
                stonebackHalfling,
                starwatchHalfling,
                lorefinderHalfling);

            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, graveltoeGnome);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, shadowGnome);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, glintpebbleGnome);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, cleverPyromaniac);
            _blueprints.AddFeatureToSelection(halflingHeritageSelection, hearthsongHalfling);
            _blueprints.AddFeatureToSelection(halflingHeritageSelection, stonebackHalfling);
            _blueprints.AddFeatureToSelection(halflingHeritageSelection, starwatchHalfling);
            _blueprints.AddFeatureToSelection(halflingHeritageSelection, lorefinderHalfling);
            _blueprints.AddFeatureToSelection(halfElfHeritageSelection, trueHighElf);
            _blueprints.AddFeatureToSelection(halfOrcHeritageSelection, orcLordsBlood);
            _blueprints.AddFeatureToSelection(humanHeritageSelection, normalHuman);
            _blueprints.AddFeatureToSelection(humanHeritageSelection, descendantOfKings);
            _blueprints.AddFeatureToRace(humanRace, humanHeritageSelection);
        }

        private RemoveFeatureOnApply CreateRemoveFeature(BlueprintFeature feature, string componentName)
        {
            var removeFeature = new RemoveFeatureOnApply { name = componentName };
            _blueprints.SetRemoveFeatureOnApplyFeature(removeFeature, feature);
            return removeFeature;
        }

        private void RemoveStrengthPenalty(BlueprintRace race, params BlueprintFeature[] heritages)
        {
            var penaltyExemptions = (heritages ?? new BlueprintFeature[0])
                .Where(heritage => heritage != null)
                .ToArray();

            foreach (var statPenalty in _blueprints.GetComponents<AddStatBonusIfHasFact>(race)
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

            foreach (var recalculate in _blueprints.GetComponents<RecalculateOnFactsChange>(race))
            {
                foreach (var heritage in penaltyExemptions)
                {
                    _blueprints.AddCheckedFact(recalculate, heritage);
                }
            }
        }

    }
}
