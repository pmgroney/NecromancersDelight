using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CleverPyromaniacGnomePatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly HeritageFactory _heritageFactory;

        public CleverPyromaniacGnomePatch(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
            _heritageFactory = new HeritageFactory(blueprints, localization);
        }

        public string Name => "Clever Pyromaniac Gnome";

        public void RegisterLocalization()
        {
            _localization.Put(LocalizationIds.Mod.CleverPyromaniacName, "Clever Pyromaniac");
            _localization.Put(
                LocalizationIds.Mod.CleverPyromaniacDescription,
                "These gnomes are as dangerously fascinated by fire as pyromaniacs, but their experiments sharpen the mind instead of charm. They gain the Pyromaniac heritage benefits and replace the gnome racial +2 Charisma bonus with a +2 Intelligence bonus.");
        }

        public void Apply()
        {
            var heritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.GnomeHeritage,
                "Gnome heritage selection");
            var pyromaniac = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.PyromaniacGnome,
                "Pyromaniac gnome heritage");

            var cleverPyromaniac = _heritageFactory.EnsureHeritage(
                ModBlueprintIds.Features.CleverPyromaniacGnome,
                "CleverPyromaniacGnome",
                LocalizationIds.Mod.CleverPyromaniacName,
                LocalizationIds.Mod.CleverPyromaniacDescription);

            var addPyromaniac = new AddFeatureOnApply { name = "$AddFeatureOnApply$CleverPyromaniacGnome" };
            _blueprints.SetAddFeatureOnApplyFeature(addPyromaniac, pyromaniac);

            _blueprints.SetComponents(
                cleverPyromaniac,
                addPyromaniac,
                _heritageFactory.CreateStatBonus("CleverPyromaniacGnome", StatType.Charisma, -2),
                _heritageFactory.CreateStatBonus("CleverPyromaniacGnome", StatType.Intelligence, 3));

            _blueprints.AddFeatureToSelection(heritageSelection, cleverPyromaniac);
        }

    }
}
