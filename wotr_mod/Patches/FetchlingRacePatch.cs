using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class FetchlingRacePatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly HeritageFactory _heritageFactory;

        public FetchlingRacePatch(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _heritageFactory = new HeritageFactory(blueprints, localization);
        }

        public string Name => "Fetchling Race";

        public void RegisterLocalization() { }

        public void Apply()
        {
            var fetchling = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Fetchling,
                "Fetchling race");
            var human = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Human,
                "Human race");

            fetchling.IsClassFeature = true;
            ConfigurePlayableRaceIdentity(fetchling, human);

            var heritageSelection = _heritageFactory.EnsureSelection(
                ModBlueprintIds.Selections.FetchlingHeritage,
                "WotrMod_FetchlingHeritageSelection",
                LocalizationIds.Mod.FetchlingHeritageName,
                LocalizationIds.Mod.FetchlingHeritageDescription);

            var defaultFetchling = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.DefaultFetchlingHeritage,
                "WotrMod_DefaultFetchlingHeritage",
                LocalizationIds.Mod.DefaultFetchlingHeritageName,
                LocalizationIds.Mod.DefaultFetchlingHeritageDescription,
                _heritageFactory.CreateStatBonus("DefaultFetchling", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("DefaultFetchling", StatType.Charisma, 3),
                _heritageFactory.CreateStatBonus("DefaultFetchling", StatType.Constitution, 2));

            var shadowWarrior = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.ShadowWarriorFetchlingHeritage,
                "WotrMod_ShadowWarriorFetchlingHeritage",
                LocalizationIds.Mod.ShadowWarriorFetchlingHeritageName,
                LocalizationIds.Mod.ShadowWarriorFetchlingHeritageDescription,
                _heritageFactory.CreateStatBonus("ShadowWarriorFetchling", StatType.Strength, 3),
                _heritageFactory.CreateStatBonus("ShadowWarriorFetchling", StatType.Charisma, 3),
                _heritageFactory.CreateStatBonus("ShadowWarriorFetchling", StatType.Constitution, 2));

            var gloomscribe = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.GloomscribeFetchlingHeritage,
                "WotrMod_GloomscribeFetchlingHeritage",
                LocalizationIds.Mod.GloomscribeFetchlingHeritageName,
                LocalizationIds.Mod.GloomscribeFetchlingHeritageDescription,
                _heritageFactory.CreateStatBonus("GloomscribeFetchling", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("GloomscribeFetchling", StatType.Intelligence, 3),
                _heritageFactory.CreateStatBonus("GloomscribeFetchling", StatType.Constitution, 2));

            var veilseer = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.VeilseerFetchlingHeritage,
                "WotrMod_VeilseerFetchlingHeritage",
                LocalizationIds.Mod.VeilseerFetchlingHeritageName,
                LocalizationIds.Mod.VeilseerFetchlingHeritageDescription,
                _heritageFactory.CreateStatBonus("VeilseerFetchling", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("VeilseerFetchling", StatType.Wisdom, 3),
                _heritageFactory.CreateStatBonus("VeilseerFetchling", StatType.Constitution, 2));

            var nightwarden = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.NightwardenFetchlingHeritage,
                "WotrMod_NightwardenFetchlingHeritage",
                LocalizationIds.Mod.NightwardenFetchlingHeritageName,
                LocalizationIds.Mod.NightwardenFetchlingHeritageDescription,
                _heritageFactory.CreateStatBonus("NightwardenFetchling", StatType.Strength, 3),
                _heritageFactory.CreateStatBonus("NightwardenFetchling", StatType.Dexterity, 3),
                _heritageFactory.CreateStatBonus("NightwardenFetchling", StatType.Constitution, 2));

            _blueprints.AddFeatureToSelection(heritageSelection, defaultFetchling);
            _blueprints.AddFeatureToSelection(heritageSelection, shadowWarrior);
            _blueprints.AddFeatureToSelection(heritageSelection, gloomscribe);
            _blueprints.AddFeatureToSelection(heritageSelection, veilseer);
            _blueprints.AddFeatureToSelection(heritageSelection, nightwarden);

            var bonusFeat = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.BasicFeat,
                "Basic feat selection");
            var shadowBlending = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.FetchlingShadowBlending,
                "Fetchling shadow blending");
            var shadowyResistance = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.FetchlingShadowyResistance,
                "Fetchling shadowy resistance");
            _blueprints.SetRaceFeatures(
                fetchling,
                new BlueprintFeatureBase[] { shadowBlending, shadowyResistance, heritageSelection, bonusFeat });

            _blueprints.AddRaceToRoot(fetchling, insertAt: 3);
        }

        private static void ConfigurePlayableRaceIdentity(BlueprintRace fetchling, BlueprintRace human)
        {
            fetchling.RaceId = Race.Catfolk;
            fetchling.Size = human.Size;
        }
    }
}
