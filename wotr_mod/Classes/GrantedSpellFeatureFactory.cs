using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes
{
    internal sealed class GrantedSpellFeatureFactory
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly SpellIconLoader _icons;

        public GrantedSpellFeatureFactory(
            BlueprintTool blueprints,
            LocalizationTool localization,
            SpellIconLoader icons)
        {
            _blueprints = blueprints;
            _localization = localization;
            _icons = icons;
        }

        public BlueprintFeature Ensure(
            string donorFeatureGuid,
            string featureGuid,
            string internalName,
            string donorName,
            string spellGuid,
            string spellName,
            string displayNameKey,
            string descriptionKey,
            int spellLevel,
            BlueprintCharacterClass characterClass,
            string iconPath = null,
            bool configureAsClassFeature = false,
            string componentName = null)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(donorFeatureGuid, donorName),
                    featureGuid,
                    internalName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            var spell = _blueprints.Require<BlueprintAbility>(spellGuid, spellName);
            var addKnownSpell = new AddKnownSpell { name = componentName ?? "$AddKnownSpell$" + internalName };
            _blueprints.SetAddKnownSpell(addKnownSpell, characterClass, spell, spellLevel);
            _blueprints.SetComponents(feature, addKnownSpell);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            ApplyIcon(feature, spell, iconPath);

            if (configureAsClassFeature)
            {
                feature.IsClassFeature = true;
                feature.Ranks = 1;
            }

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private void ApplyIcon(BlueprintFeature feature, BlueprintAbility spell, string iconPath)
        {
            if (!string.IsNullOrEmpty(iconPath))
            {
                var icon = _icons?.Load(iconPath);
                if (icon != null)
                {
                    _blueprints.SetUnitFactIcon(feature, icon);
                }

                return;
            }

            if (spell.Icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, spell.Icon);
            }
        }
    }
}
