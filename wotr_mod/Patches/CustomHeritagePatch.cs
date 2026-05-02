using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class CustomHeritagePatch : IGamePatch
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;

        public CustomHeritagePatch(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
        }

        public string Name => "Custom Heritages";

        public void RegisterLocalization()
        {
            _localization.Put(LocalizationIds.Mod.UmbralDhampirHeritageName, "Umbral Dhampir");
            _localization.Put(
                LocalizationIds.Mod.UmbralDhampirHeritageDescription,
                "These dhampirs carry an unusually deep shadow in their blood. They gain a +4 racial bonus to Charisma, a +2 racial bonus to Dexterity, a -2 penalty to Intelligence, and 1 additional skill point at each level.");

            _localization.Put(LocalizationIds.Mod.GraveguardDhampirHeritageName, "Graveguard");
            _localization.Put(
                LocalizationIds.Mod.GraveguardDhampirHeritageDescription,
                "These dhampirs are born to be guardians of the dead, possessing unnatural strength. They gain a +4 racial bonus to Strength, a +2 racial bonus to Charisma, a -2 penalty to Intelligence, and 1 additional skill point at each level.");

            _localization.Put(LocalizationIds.Mod.UmbralGnomeHeritageName, "Umbral Gnome");
            _localization.Put(
                LocalizationIds.Mod.UmbralGnomeHeritageDescription,
                "These gnomes are bright-eyed, quick, and touched by old shadow. They gain a +4 racial bonus to Charisma, a +2 racial bonus to Dexterity, a -2 penalty to Intelligence, and 1 additional skill point at each level.");

            _localization.Put(LocalizationIds.Mod.ShadowGnomeHeritageName, "Shadow Gnome");
            _localization.Put(
                LocalizationIds.Mod.ShadowGnomeHeritageDescription,
                "These gnomes move like living silhouettes, charming and elusive but physically slight. They gain a +4 racial bonus to Charisma, a +2 racial bonus to Dexterity, a -2 penalty to Strength, and 1 additional skill point at each level.");
        }

        public void Apply()
        {
            var dhampirHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.DhampirHeritage,
                "Dhampir heritage selection");
            var gnomeHeritageSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.GnomeHeritage,
                "Gnome heritage selection");

            var umbralDhampir = EnsureHeritage(
                ModBlueprintIds.Features.UmbralDhampirHeritage,
                "WotrMod_UmbralDhampirHeritage",
                LocalizationIds.Mod.UmbralDhampirHeritageName,
                LocalizationIds.Mod.UmbralDhampirHeritageDescription,
                CreateStatBonus("UmbralDhampir", StatType.Charisma, 4),
                CreateStatBonus("UmbralDhampir", StatType.Dexterity, 2),
                CreateStatBonus("UmbralDhampir", StatType.Intelligence, -2),
                CreateSkillPointBonus("UmbralDhampir"));

            var graveguardDhampir = EnsureHeritage(
                ModBlueprintIds.Features.GraveguardDhampirHeritage,
                "WotrMod_GraveguardDhampirHeritage",
                LocalizationIds.Mod.GraveguardDhampirHeritageName,
                LocalizationIds.Mod.GraveguardDhampirHeritageDescription,
                CreateStatBonus("GraveguardDhampir", StatType.Strength, 4),
                CreateStatBonus("GraveguardDhampir", StatType.Charisma, 2),
                CreateStatBonus("GraveguardDhampir", StatType.Intelligence, -2),
                CreateSkillPointBonus("GraveguardDhampir"));

            var umbralGnome = EnsureHeritage(
                ModBlueprintIds.Features.UmbralGnomeHeritage,
                "WotrMod_UmbralGnomeHeritage",
                LocalizationIds.Mod.UmbralGnomeHeritageName,
                LocalizationIds.Mod.UmbralGnomeHeritageDescription,
                CreateStatBonus("UmbralGnome", StatType.Charisma, 2),
                CreateStatBonus("UmbralGnome", StatType.Dexterity, 2),
                CreateStatBonus("UmbralGnome", StatType.Intelligence, -2),
                CreateStatBonus("UmbralGnome", StatType.Constitution, -2),
                CreateStatBonus("UmbralGnome", StatType.Strength, 2),
                CreateSkillPointBonus("UmbralGnome"));

            var shadowGnome = EnsureHeritage(
                ModBlueprintIds.Features.ShadowGnomeHeritage,
                "WotrMod_ShadowGnomeHeritage",
                LocalizationIds.Mod.ShadowGnomeHeritageName,
                LocalizationIds.Mod.ShadowGnomeHeritageDescription,
                CreateStatBonus("ShadowGnome", StatType.Charisma, 2),
                CreateStatBonus("ShadowGnome", StatType.Dexterity, 2),
                CreateStatBonus("ShadowGnome", StatType.Constitution, -2),
                CreateSkillPointBonus("ShadowGnome"));

            _blueprints.AddFeatureToSelection(dhampirHeritageSelection, umbralDhampir);
            _blueprints.AddFeatureToSelection(dhampirHeritageSelection, graveguardDhampir);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, umbralGnome);
            _blueprints.AddFeatureToSelection(gnomeHeritageSelection, shadowGnome);
        }

        private BlueprintFeature EnsureHeritage(
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

        private static AddStatBonus CreateStatBonus(string heritageName, StatType stat, int value)
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

        private static AddSkillPointPerCharacterLevel CreateSkillPointBonus(string heritageName)
        {
            return new AddSkillPointPerCharacterLevel
            {
                name = $"$AddSkillPointPerCharacterLevel${heritageName}"
            };
        }
    }
}
