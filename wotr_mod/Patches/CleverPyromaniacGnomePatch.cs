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

        public CleverPyromaniacGnomePatch(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
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

            var cleverPyromaniac = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.CleverPyromaniacGnome);
            if (cleverPyromaniac == null)
            {
                cleverPyromaniac = CreateFeature(pyromaniac);
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.CleverPyromaniacGnome, cleverPyromaniac);
            }

            _blueprints.AddFeatureToSelection(heritageSelection, cleverPyromaniac);
        }

        private BlueprintFeature CreateFeature(BlueprintFeature pyromaniac)
        {
            var feature = new BlueprintFeature
            {
                name = "CleverPyromaniacGnome",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.CleverPyromaniacGnome),
                Groups = new[] { FeatureGroup.Racial },
                Ranks = 1,
                ReapplyOnLevelUp = false,
                IsClassFeature = true
            };

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.CleverPyromaniacName),
                _localization.Text(LocalizationIds.Mod.CleverPyromaniacDescription));

            var addPyromaniac = new AddFeatureOnApply { name = "$AddFeatureOnApply$CleverPyromaniacGnome" };
            _blueprints.SetAddFeatureOnApplyFeature(addPyromaniac, pyromaniac);

            _blueprints.SetComponents(
                feature,
                addPyromaniac,
                CreateStatBonus(StatType.Charisma, -2),
                CreateStatBonus(StatType.Intelligence, 2));

            return feature;
        }

        private static AddStatBonus CreateStatBonus(StatType stat, int value)
        {
            return new AddStatBonus
            {
                name = $"$AddStatBonus$CleverPyromaniacGnome${stat}",
                Descriptor = ModifierDescriptor.Racial,
                Stat = stat,
                Value = value,
                ScaleByBasicAttackBonus = false
            };
        }
    }
}
