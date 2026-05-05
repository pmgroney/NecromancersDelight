using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using wotr_mod.Features;
using wotr_mod.Infrastructure;

namespace wotr_mod.Infrastructure
{
    internal sealed class HeritageFactory
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;

        public HeritageFactory(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
        }

        public BlueprintFeatureSelection EnsureSelection(
            string guid,
            string internalName,
            string displayNameKey,
            string descriptionKey)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(guid);
            if (selection == null)
            {
                selection = new BlueprintFeatureSelection
                {
                    name = internalName,
                    AssetGuid = BlueprintGuid.Parse(guid),
                    Groups = new[] { FeatureGroup.Racial },
                    Ranks = 1,
                    IsClassFeature = true
                };
                _blueprints.AddCachedBlueprint(guid, selection);
            }

            selection.Groups = new[] { FeatureGroup.Racial };
            selection.Ranks = 1;
            selection.IsClassFeature = true;
            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));

            return selection;
        }

        public BlueprintFeature EnsureHeritage(
            string guid,
            string internalName,
            string displayNameKey,
            string descriptionKey,
            params BlueprintComponent[] components)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = internalName,
                    AssetGuid = BlueprintGuid.Parse(guid),
                    Groups = new[] { FeatureGroup.Racial },
                    Ranks = 1,
                    ReapplyOnLevelUp = false,
                    IsClassFeature = true
                };
                _blueprints.AddCachedBlueprint(guid, feature);
            }

            feature.Groups = new[] { FeatureGroup.Racial };
            feature.Ranks = 1;
            feature.ReapplyOnLevelUp = false;
            feature.IsClassFeature = true;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _blueprints.SetComponents(feature, components);

            return feature;
        }

        public AddStatBonus CreateStatBonus(string heritageName, StatType stat, int value)
        {
            return new AddStatBonus
            {
                name = $"$AddStatBonus${heritageName}${stat}",
                Descriptor = ModifierDescriptor.Racial,
                Stat = stat,
                Value = value,
                ScaleByBasicAttackBonus = false
            };
        }

        public SelectedRaceStatBonus CreateSelectedRaceStatBonus(string heritageName, int value)
        {
            return new SelectedRaceStatBonus
            {
                name = $"$SelectedRaceStatBonus${heritageName}",
                Descriptor = ModifierDescriptor.Racial,
                Value = value
            };
        }
    }
}
