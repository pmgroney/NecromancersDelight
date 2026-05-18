using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityModManagerNet;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Classes.Necromancer
{
    internal sealed partial class NecromancerInstaller : IClassContentInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;
        private readonly GrantedSpellFeatureFactory _grantedSpellFeatures;

        public NecromancerInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            SpellIconLoader icons)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _icons = icons;
            _grantedSpellFeatures = new GrantedSpellFeatureFactory(blueprints, localization, icons);
        }

        public bool CanInstall(CharacterClassDefinition definition) => definition.UseNecromancerBloodline;

        public void RegisterLocalization() { }

        public void ConfigureSpellList(CharacterClassDefinition definition, BlueprintSpellList spellList)
        {
            ConfigureNecromancerSpellList(spellList);
        }

        public BlueprintFeatureBase EnsureProgressionFeature(CharacterClassDefinition definition)
        {
            return null;
        }

        public void ConfigureProgression(CharacterClassDefinition definition, BlueprintProgression progression)
        {
        }

        public void Install(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            var wizardClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Wizard,
                "Wizard class");
            _blueprints.SetCharacterClassAppearanceFromClass(characterClass, wizardClass);

            EnsureNecromancerBloodline();
            RegisterNecromancerFeatures(characterClass);
            EnsureNecromancySpellFocusRecommendation(characterClass);

            if (characterClass.Progression != null)
            {
                AddNecromancerFeaturesToProgression(characterClass.Progression);
            }

            _blueprints.SetCharacterClassArchetypes(characterClass);
            _blueprints.SetCharacterClassArchetypes(
                characterClass,
                EnsureArchetypes(characterClass, spellbook, spellList));
        }

        private void ConfigureNecromancerSpellList(BlueprintSpellList spellList, int minimumSpellLevel = 0)
        {
            var spellsByLevel = NecromancerSpellRegistry.GetAll()
                .Where(d => d.SpellLevel >= minimumSpellLevel)
                .Select(d =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(d.SpellGuid, d.DisplayName);
                    ApplySelectionRecommendation(spell, d);
                    return new KeyValuePair<BlueprintAbility, int>(spell, d.SpellLevel);
                });

            _blueprints.SetSpellListSpells(
                spellList,
                spellsByLevel.OrderBy(p => p.Value).ThenBy(p => p.Key.name));
        }

        private void ApplySelectionRecommendation(BlueprintScriptableObject blueprint, ClassSpellDefinition definition)
        {
            if (!definition.Recommendation.HasValue) return;
            _blueprints.AddSelectionRecommendation(
                blueprint,
                definition.Recommendation.Value,
                $"$PureRecommendation${definition.DisplayName}");
        }

        private void EnsureNecromancySpellFocusRecommendation(BlueprintCharacterClass characterClass)
        {
            var spellFocus = _blueprints.Require<BlueprintParametrizedFeature>(
                GameBlueprintIds.Features.SpellFocus,
                "Spell Focus");
            var recommendation = _blueprints.GetComponents<SpellFocusSchoolRecommendation>(spellFocus)
                .FirstOrDefault();

            if (recommendation == null)
            {
                recommendation = new SpellFocusSchoolRecommendation
                {
                    name = "$SpellFocusSchoolRecommendation$ClassSchools"
                };
                _blueprints.AddComponent(spellFocus, recommendation);
            }

            recommendation.AddRecommendedClass(characterClass, SpellSchool.Necromancy);
        }

        private BlueprintProgression EnsureNecromancerBloodline()
        {
            var existing = _blueprints.Get<BlueprintProgression>(ModBlueprintIds.Progressions.NecromancerBloodline);
            var necromancerClass = _blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Necromancer);

            var clone = existing ?? _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintProgression>(
                    GameBlueprintIds.Progressions.UndeadBloodline, "Undead bloodline donor"),
                ModBlueprintIds.Progressions.NecromancerBloodline,
                "WotrMod_NecromancerBloodline");
            _blueprints.SetComponents(clone);

            _blueprints.SetUnitFactDisplay(clone,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineName),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineDescription));
            clone.HideInUI = true;
            clone.HideInCharacterSheetAndLevelUp = true;
            clone.HideNotAvailibleInUI = true;

            var arcana = EnsureMasterOfDeathFeature(necromancerClass);
            var power1 = EnsureWitheringRayFeature(necromancerClass);
            var power3 = EnsureDeathsGiftFeature(necromancerClass);
            var power9 = EnsureGraspOfTheDeadFeature(necromancerClass);
            var power15 = EnsureIncorporealFormFeature(necromancerClass);
            var power20 = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadOneOfUs,
                ModBlueprintIds.Features.NecromancerBloodlinePower20,
                "WotrMod_NecromancerBloodlinePower20", "One of Us",
                LocalizationIds.Mod.NecromancerBloodlinePower20Name,
                LocalizationIds.Mod.NecromancerBloodlinePower20Description,
                necromancerClass);
            var oneOfUsIcon = _icons.Load("Icons\\one_of_us.png");
            if (oneOfUsIcon != null) _blueprints.SetUnitFactIcon(power20, oneOfUsIcon);
            var boneArmor = EnsureBoneArmorFeature(necromancerClass);
            var boneSpike = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel1,
                ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell,
                "WotrMod_NecromancerKnownSpell_BoneSpike", "Bone Spike donor",
                ModBlueprintIds.Spells.BoneSpike, "Bone Spike",
                LocalizationIds.Mod.SpellBoneSpikeName, LocalizationIds.Mod.SpellBoneSpikeDescription,
                1, necromancerClass);
            var corpseExplosion = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel2,
                ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell,
                "WotrMod_NecromancerKnownSpell_CorpseExplosion", "Corpse Explosion donor",
                ModBlueprintIds.Spells.CorpseExplosion, "Corpse Explosion",
                LocalizationIds.Mod.SpellCorpseExplosionName, LocalizationIds.Mod.SpellCorpseExplosionDescription,
                2, necromancerClass);
            var eldritchHorror = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel3,
                ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell,
                "WotrMod_NecromancerKnownSpell_EldritchHorror", "Eldritch Horror donor",
                ModBlueprintIds.Spells.EldritchHorror, "Eldritch Horror",
                LocalizationIds.Mod.SpellEldritchHorrorName, LocalizationIds.Mod.SpellEldritchHorrorDescription,
                3, necromancerClass);
            var hellOnEarth = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel9,
                ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell,
                "WotrMod_NecromancerKnownSpell_HellOnEarth", "Hell on Earth donor",
                ModBlueprintIds.Spells.HellOnEarth, "Hell on Earth",
                LocalizationIds.Mod.SpellHellOnEarthName, LocalizationIds.Mod.SpellHellOnEarthDescription,
                9, necromancerClass);
            var harvestTheFallen = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel9,
                ModBlueprintIds.Features.NecromancerHarvestTheFallenKnownSpell,
                "WotrMod_NecromancerKnownSpell_HarvestTheFallen", "Harvest the Fallen donor",
                ModBlueprintIds.Spells.HarvestTheFallen, "Harvest the Fallen",
                LocalizationIds.Mod.SpellHarvestTheFallenName, LocalizationIds.Mod.SpellHarvestTheFallenDescription,
                5, necromancerClass);
            var stygianPrecision = EnsureStygianPrecisionFeature(necromancerClass);
            var reapersJudgement = EnsureReapersJudgementFeature(necromancerClass);

            var visibleFeatures = new BlueprintFeatureBase[]
            {
                arcana, power1, power3, boneSpike, corpseExplosion,
                eldritchHorror, power9, harvestTheFallen, power15, hellOnEarth, power20, stygianPrecision, reapersJudgement
            };
            clone.LevelEntries = new[]
            {
                _blueprints.CreateLevelEntry(1, arcana, power1, boneArmor),
                _blueprints.CreateLevelEntry(2, boneSpike),
                _blueprints.CreateLevelEntry(3, power3),
                _blueprints.CreateLevelEntry(4, corpseExplosion, stygianPrecision),
                _blueprints.CreateLevelEntry(5, boneArmor),
                _blueprints.CreateLevelEntry(7, eldritchHorror),
                _blueprints.CreateLevelEntry(8, stygianPrecision),
                _blueprints.CreateLevelEntry(9, power3, power9, boneArmor),
                _blueprints.CreateLevelEntry(11, harvestTheFallen),
                _blueprints.CreateLevelEntry(12, stygianPrecision),
                _blueprints.CreateLevelEntry(13, boneArmor),
                _blueprints.CreateLevelEntry(15, power3, power15),
                _blueprints.CreateLevelEntry(16, stygianPrecision),
                _blueprints.CreateLevelEntry(17, boneArmor),
                _blueprints.CreateLevelEntry(19, hellOnEarth),
                _blueprints.CreateLevelEntry(20, power20, reapersJudgement)
            };
            _blueprints.SetProgressionUiDeterminators(clone, visibleFeatures);
            _blueprints.SetProgressionUiGroups(clone, new[] { visibleFeatures });
            _blueprints.EnsureCustomClassOwnsProgressionFeatures(
                clone,
                "WotrMod_NecromancerBloodline",
                necromancerClass);

            if (existing == null)
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Progressions.NecromancerBloodline, clone);

            return clone;
        }

        internal BlueprintFeature[] GetNecromancerFeatures()
        {
            return new[]
            {
                EnsureNecromancerProficiencies(),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Necromancer Arcana"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower1, "Grave Touch"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower9, "Grasp of the Dead"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower15, "Incorporeal Form"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower20, "One of Us"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBoneArmor, "Bone Armor"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell, "Bone Spike"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell, "Corpse Explosion"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell, "Eldritch Horror"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerHarvestTheFallenKnownSpell, "Harvest the Fallen"),
                _blueprints.Require<BlueprintFeature>(ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell, "Hell on Earth"),
                EnsureNecromancerBonusFeatSelection(),
                EnsureStygianPrecisionFeature(_blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Necromancer)),
                EnsureReapersJudgementFeature(_blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Necromancer))
            };
        }

        internal void RegisterNecromancerFeatures(BlueprintCharacterClass characterClass)
        {
            foreach (var feature in GetNecromancerFeatures())
            {
                if (feature is BlueprintFeatureSelection selection)
                {
                    _blueprints.SetProgressionClassesShallow(selection, characterClass);
                    continue;
                }

                _blueprints.SetProgressionClasses(feature, characterClass);
            }
        }

        internal void AddNecromancerFeaturesToProgression(BlueprintProgression progression)
        {
            var features = GetNecromancerFeatures();
            var necromancerProficiencies = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerProficiencies, "Necromancer Proficiencies");
            var masterOfDeath = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death");
            var witheringRay = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBloodlinePower1, "Withering Ray");
            var deathsGift = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift");
            var graspOfTheDead = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBloodlinePower9, "Grasp of the Dead");
            var incorporealForm = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBloodlinePower15, "Incorporeal Form");
            var oneOfUs = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBloodlinePower20, "One of Us");
            var boneArmor = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBoneArmor, "Bone Armor");
            var boneSpike = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell, "Bone Spike");
            var corpseExplosion = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell, "Corpse Explosion");
            var eldritchHorror = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell, "Eldritch Horror");
            var harvestTheFallen = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerHarvestTheFallenKnownSpell, "Harvest the Fallen");
            var hellOnEarth = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell, "Hell on Earth");
            var necromancerBonusFeat = FindNecromancerFeature<BlueprintFeatureSelection>(
                features, ModBlueprintIds.Selections.NecromancerBonusFeat, "Necromancer Bonus Feat");
            var stygianPrecision = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerStygianPrecision, "Stygian Precision");
            var reapersJudgement = FindNecromancerFeature<BlueprintFeature>(
                features, ModBlueprintIds.Features.NecromancerReapersJudgement, "Reaper's Judgement");

            _blueprints.RemoveFeaturesFromProgression(
                progression,
                GameBlueprintIds.Features.SorcererProficiencies,
                GameBlueprintIds.Selections.SorcererBonusFeat,
                GameBlueprintIds.Selections.SorcererFeatSelection,
                ModBlueprintIds.Features.NecromancerProficiencies,
                ModBlueprintIds.Selections.NecromancerBonusFeat);

            _blueprints.AddFeaturesToLevel(progression, 1,  necromancerProficiencies, masterOfDeath, witheringRay, boneArmor, necromancerBonusFeat);
            _blueprints.AddFeaturesToLevel(progression, 2,  boneSpike);
            _blueprints.AddFeaturesToLevel(progression, 3,  deathsGift);
            _blueprints.AddFeaturesToLevel(progression, 4,  corpseExplosion, stygianPrecision);
            _blueprints.AddFeaturesToLevel(progression, 5,  boneArmor);
            _blueprints.AddFeaturesToLevel(progression, 6,  necromancerBonusFeat);
            _blueprints.AddFeaturesToLevel(progression, 7,  eldritchHorror);
            _blueprints.AddFeaturesToLevel(progression, 8,  stygianPrecision);
            _blueprints.AddFeaturesToLevel(progression, 9,  deathsGift, graspOfTheDead, boneArmor);
            _blueprints.AddFeaturesToLevel(progression, 10, necromancerBonusFeat);
            _blueprints.AddFeaturesToLevel(progression, 11, harvestTheFallen);
            _blueprints.AddFeaturesToLevel(progression, 12, stygianPrecision);
            _blueprints.AddFeaturesToLevel(progression, 13, boneArmor);
            _blueprints.AddFeaturesToLevel(progression, 15, deathsGift, incorporealForm);
            _blueprints.AddFeaturesToLevel(progression, 16, necromancerBonusFeat, stygianPrecision);
            _blueprints.AddFeaturesToLevel(progression, 17, boneArmor);
            _blueprints.AddFeaturesToLevel(progression, 19, hellOnEarth);
            _blueprints.AddFeaturesToLevel(progression, 20, oneOfUs, reapersJudgement);

            _blueprints.SetProgressionUiDeterminators(progression, new List<BlueprintFeatureBase> { masterOfDeath });
            _blueprints.SetProgressionUiGroups(
                progression,
                new[] { boneArmor },
                new[] { deathsGift },
                new[] { necromancerBonusFeat },
                new[] { stygianPrecision, reapersJudgement },
                new[] { witheringRay, graspOfTheDead, incorporealForm, oneOfUs },
                new[] { boneSpike, corpseExplosion, eldritchHorror, harvestTheFallen, hellOnEarth });
        }

        internal static T FindNecromancerFeature<T>(
            IEnumerable<BlueprintFeature> features,
            string guid,
            string displayName) where T : BlueprintFeature
        {
            var targetGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(guid));
            var feature = (features ?? Enumerable.Empty<BlueprintFeature>())
                .OfType<T>()
                .FirstOrDefault(candidate => candidate.AssetGuid == targetGuid);
            if (feature == null)
            {
                throw new InvalidOperationException(
                    $"Necromancer feature set did not contain {displayName} ({guid}).");
            }

            return feature;
        }

        internal BlueprintFeature EnsureStygianPrecisionFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerStygianPrecision);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerStygianPrecision",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.NecromancerStygianPrecision)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerStygianPrecision, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 4;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerStygianPrecisionName),
                _localization.Text(LocalizationIds.Mod.NecromancerStygianPrecisionDescription));
            var icon = _icons.Load("Icons\\stygian_precision.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            _blueprints.SetComponents(feature, new StygianPrecisionComponent
            {
                name = "$StygianPrecisionComponent$Necromancer"
            });
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        internal BlueprintFeature EnsureReapersJudgementFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerReapersJudgement);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerReapersJudgement",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.NecromancerReapersJudgement)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerReapersJudgement, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerReapersJudgementName),
                _localization.Text(LocalizationIds.Mod.NecromancerReapersJudgementDescription));
            var icon = _icons.Load("Icons\\reapers_judgement.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            _blueprints.SetComponents(feature, new StygianPrecisionComponent
            {
                name = "$StygianPrecisionComponent$ReapersJudgement",
                CriticalEdgeBonus = 1,
                AutoConfirmCriticalHits = true
            });
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        internal BlueprintFeatureSelection EnsureNecromancerBonusFeatSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.NecromancerBonusFeat);
            var selection = existing;
            if (selection == null)
            {
                var donor = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBonusFeat, "Sorcerer Bonus Feat");
                selection = _blueprints.CloneBlueprint(
                    donor,
                    ModBlueprintIds.Selections.NecromancerBonusFeat,
                    "WotrMod_NecromancerBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.NecromancerBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(selection,
                _localization.Text(LocalizationIds.Mod.NecromancerBonusFeatName),
                _localization.Text(LocalizationIds.Mod.NecromancerBonusFeatDescription));
            selection.IsClassFeature = true;
            selection.Ranks = 1;
            return selection;
        }

        private BlueprintFeature EnsureNecromancerProficiencies()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerProficiencies);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.SorcererProficiencies, "Sorcerer Proficiencies"),
                    ModBlueprintIds.Features.NecromancerProficiencies,
                    "NecromancerProficiencies");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerProficiencies, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerProficienciesName),
                _localization.Text(LocalizationIds.Mod.NecromancerProficienciesDescription));
            var addFacts = _blueprints.EnsureComponent(feature, () => new AddFacts());
            _blueprints.SetAddFacts(addFacts,
                _blueprints.Require<BlueprintFeature>("e70ecf1ed95ca2f40b754f1adb22bbdd", "Simple Weapon Proficiency"),
                _blueprints.Require<BlueprintFeature>("96c174b0ebca7b246b82d4bc4aac4574", "Scythe Proficiency"));
            return feature;
        }

        private BlueprintFeature EnsureNecromancerBloodlineFeature(
            string donorGuid, string featureGuid, string internalName, string donorName,
            string displayNameKey, string descriptionKey, BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(donorGuid, donorName),
                    featureGuid, internalName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(displayNameKey), _localization.Text(descriptionKey));
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureDeathsGiftFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadDeathsGift,
                ModBlueprintIds.Features.NecromancerBloodlinePower3,
                "WotrMod_NecromancerBloodlinePower3", "Death's Gift",
                LocalizationIds.Mod.NecromancerBloodlinePower3Name,
                LocalizationIds.Mod.NecromancerBloodlinePower3Description, characterClass);
            feature.Ranks = 3;
            feature.ReapplyOnLevelUp = true;
            var icon = _icons.Load("Icons\\deaths_gift.png");
            if (icon != null) _blueprints.SetUnitFactIcon(feature, icon);
            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(feature))
                _blueprints.ConfigureFeatureRankCustomProgression(rank, feature, 5, 10, 20);
            foreach (var resistance in _blueprints.GetComponents<AddDamageResistanceEnergy>(feature))
                resistance.Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default };
            return feature;
        }

        private BlueprintFeature EnsureMasterOfDeathFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlineArcana);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.RedDragonBloodlineArcana, "Red Dragon Arcana donor"),
                    ModBlueprintIds.Features.NecromancerBloodlineArcana, "WotrMod_NecromancerMasterOfDeath");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBloodlineArcana, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaName),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaDescription));
            _blueprints.SetUnitFactShortDescription(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerMasterOfDeathClassCardDescription));
            var icon = _icons.Load("Icons\\necromancer.png");
            if (icon != null) _blueprints.SetUnitFactIcon(feature, icon);
            _blueprints.SetComponents(feature, new MasterOfDeathArcanaClassSpells
            {
                name = "$MasterOfDeathArcanaClassSpells$Necromancer",
                Classes = characterClass == null ? Array.Empty<BlueprintCharacterClass>() : new[] { characterClass }
            });
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureWitheringRayFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower1);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.BloodlineElementalEarthElementalRayFeature, "Earth elemental ray feature donor"),
                    ModBlueprintIds.Features.NecromancerBloodlinePower1, "WotrMod_NecromancerWitheringRayFeature");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBloodlinePower1, feature);
            }
            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineElementalElementalRayResource,
                ModBlueprintIds.AbilityResources.WitheringRay, "WotrMod_NecromancerWitheringRayResource");
            var ability = EnsureWitheringRayAbility(characterClass, resource);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
                _blueprints.SetAddFacts(addFacts, ability);
            if (!_blueprints.GetComponents<AddFacts>(feature).Any())
            {
                var af = new AddFacts { name = "$AddFacts$NecromancerWitheringRay" };
                _blueprints.AddComponent(feature, af);
                _blueprints.SetAddFacts(af, ability);
            }
            PatchFeatureResource(feature, resource);
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlinePower1Name),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlinePower1Description));
            _blueprints.SetUnitFactShortDescription(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerWitheringRayClassCardDescription));
            if (ability.Icon != null) _blueprints.SetUnitFactIcon(feature, ability.Icon);
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintAbility EnsureWitheringRayAbility(BlueprintCharacterClass characterClass, BlueprintAbilityResource resource)
        {
            var ability = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.WitheringRay);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintAbility>(GameBlueprintIds.Abilities.BloodlineElementalEarthElementalRayAbility, "Earth elemental ray ability donor"),
                    ModBlueprintIds.Abilities.WitheringRay, "WotrMod_NecromancerWitheringRayAbility");
                ability.OnEnable();
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.WitheringRay, ability);
            }
            _blueprints.SetAbilityDisplay(ability,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlinePower1Name),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlinePower1Description));
            SpellModifierUtility.SetSchool(ability, SpellSchool.Necromancy, _blueprints);
            SpellModifierUtility.ReplaceDescriptor(ability, SpellDescriptor.Acid, SpellDescriptor.Death, _blueprints);
            PatchWitheringRayDamage(ability);
            PatchAbilityResource(ability, resource);
            var rank = _blueprints.EnsureComponent(ability, () => new ContextRankConfig { name = "$ContextRankConfig$NecromancerWitheringRay" });
            _blueprints.ConfigureContextRankConfig(
                rank,
                AbilityRankType.Default,
                ContextRankBaseValueType.ClassLevel,
                ContextRankProgression.OnePlusDivStep,
                startLevel: 1,
                stepLevel: 2,
                characterClass: characterClass);
            _blueprints.SetContextRankMinimum(rank, 1);
            ConfigureWitheringRayVisuals(ability);
            return ability;
        }

        private void ConfigureWitheringRayVisuals(BlueprintAbility ability)
        {
            SpellEffectTintRegistry.RegisterAbilitySpawnFxTint(
                ability.AssetGuid.ToString(),
                SpellEffectTheme.Necro);

            var projectile = EnsureWitheringRayProjectile(ability);
            if (projectile == null) return;

            SpellEffectTintRegistry.RegisterProjectileTint(
                projectile.AssetGuid.ToString(),
                SpellEffectTheme.Necro);

            foreach (var delivery in _blueprints.GetComponents<AbilityDeliverProjectile>(ability))
            {
                _blueprints.SetAbilityDeliverProjectiles(delivery, projectile);
            }

            ability.OnEnable();
        }

        private BlueprintProjectile EnsureWitheringRayProjectile(BlueprintAbility ability)
        {
            var existing = _blueprints.Get<BlueprintProjectile>(ModBlueprintIds.Projectiles.WitheringRay);
            if (existing != null) return existing;

            var delivery = _blueprints.GetComponents<AbilityDeliverProjectile>(ability).FirstOrDefault();
            var projectileRefs = delivery != null
                ? BlueprintFields.AbilityDeliverProjectileProjectiles.GetValue(delivery) as BlueprintProjectileReference[]
                : null;
            var donor = projectileRefs?.FirstOrDefault()?.Get() as BlueprintProjectile;
            if (donor == null) return null;

            var projectile = _blueprints.CloneBlueprint(
                donor,
                ModBlueprintIds.Projectiles.WitheringRay,
                "WotrMod_WitheringRayProjectile");
            projectile.OnEnable();
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Projectiles.WitheringRay, projectile);
            return projectile;
        }

        private BlueprintFeature EnsureGraspOfTheDeadFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadGraspOfTheDead,
                ModBlueprintIds.Features.NecromancerBloodlinePower9,
                "WotrMod_NecromancerBloodlinePower9", "Grasp of the Dead",
                LocalizationIds.Mod.NecromancerBloodlinePower9Name,
                LocalizationIds.Mod.NecromancerBloodlinePower9Description, characterClass);
            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadGraspOfTheDeadResource,
                ModBlueprintIds.AbilityResources.GraspOfTheDead, "WotrMod_NecromancerGraspOfTheDeadResource");
            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadGraspOfTheDeadAbility,
                ModBlueprintIds.Abilities.GraspOfTheDead, "WotrMod_NecromancerGraspOfTheDeadAbility",
                "Grasp of the Dead ability",
                LocalizationIds.Mod.NecromancerBloodlinePower9Name,
                LocalizationIds.Mod.NecromancerBloodlinePower9Description, characterClass, resource);
            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureIncorporealFormFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadIncorporealForm,
                ModBlueprintIds.Features.NecromancerBloodlinePower15,
                "WotrMod_NecromancerBloodlinePower15", "Incorporeal Form",
                LocalizationIds.Mod.NecromancerBloodlinePower15Name,
                LocalizationIds.Mod.NecromancerBloodlinePower15Description, characterClass);
            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadIncorporealFormResource,
                ModBlueprintIds.AbilityResources.IncorporealForm, "WotrMod_NecromancerIncorporealFormResource");
            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadIncorporealFormAbility,
                ModBlueprintIds.Abilities.IncorporealForm, "WotrMod_NecromancerIncorporealFormAbility",
                "Incorporeal Form ability",
                LocalizationIds.Mod.NecromancerBloodlinePower15Name,
                LocalizationIds.Mod.NecromancerBloodlinePower15Description, characterClass, resource);
            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureBoneArmorFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBoneArmor);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerBoneArmor",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.NecromancerBoneArmor),
                    IsClassFeature = true, Ranks = 5, ReapplyOnLevelUp = false
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBoneArmor, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 5;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorName),
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorDescription));
            _blueprints.SetUnitFactShortDescription(feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorClassCardDescription));
            var icon = _icons.Load("Icons\\bone_armor.png");
            if (icon != null) _blueprints.SetUnitFactIcon(feature, icon);
            _blueprints.SetComponents(feature, new AddStatBonus
            {
                name = "$AddStatBonus$NecromancerBoneArmor",
                Stat = StatType.AC, Descriptor = ModifierDescriptor.NaturalArmor, Value = 1
            });
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureKnownSpellFeature(
            string donorGuid, string featureGuid, string internalName, string donorName,
            string spellGuid, string spellName, string displayNameKey, string descriptionKey,
            int spellLevel, BlueprintCharacterClass characterClass)
        {
            return _grantedSpellFeatures.Ensure(
                donorGuid,
                featureGuid,
                internalName,
                donorName,
                spellGuid,
                spellName,
                displayNameKey,
                descriptionKey,
                spellLevel,
                characterClass,
                configureAsClassFeature: true,
                componentName: "$AddKnownSpell" + internalName);
        }

        private BlueprintAbilityResource EnsureAbilityResource(string donorGuid, string resourceGuid, string internalName)
        {
            var resource = _blueprints.Get<BlueprintAbilityResource>(resourceGuid);
            if (resource != null) return resource;
            resource = _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintAbilityResource>(donorGuid, internalName + " donor"),
                resourceGuid, internalName);
            _blueprints.AddCachedBlueprint(resourceGuid, resource);
            return resource;
        }

        private BlueprintAbility EnsureNecromancerClassLevelAbility(
            string donorGuid, string abilityGuid, string internalName, string donorName,
            string displayNameKey, string descriptionKey,
            BlueprintCharacterClass characterClass, BlueprintAbilityResource resource)
        {
            var ability = _blueprints.Get<BlueprintAbility>(abilityGuid);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintAbility>(donorGuid, donorName), abilityGuid, internalName);
                ability.OnEnable();
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }
            _blueprints.SetAbilityDisplay(ability, _localization.Text(displayNameKey), _localization.Text(descriptionKey));
            SpellModifierUtility.SetSchool(ability, SpellSchool.Necromancy, _blueprints);
            PatchAbilityResource(ability, resource);
            PatchAbilityClassLevelRanks(ability, characterClass);
            return ability;
        }

        private void PatchFeatureAbilityAndResource(
            BlueprintFeature feature, BlueprintAbility ability,
            BlueprintAbilityResource resource, BlueprintCharacterClass characterClass)
        {
            foreach (var af in _blueprints.GetComponents<AddFacts>(feature))
                _blueprints.SetAddFacts(af, ability);
            if (!_blueprints.GetComponents<AddFacts>(feature).Any())
            {
                var af = new AddFacts { name = "$AddFacts$" + feature.name };
                _blueprints.AddComponent(feature, af);
                _blueprints.SetAddFacts(af, ability);
            }
            PatchFeatureResource(feature, resource);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
        }

        internal void PatchFeatureResource(BlueprintFeature feature, BlueprintAbilityResource resource)
        {
            foreach (var ar in _blueprints.GetComponents<AddAbilityResources>(feature))
                _blueprints.SetAddAbilityResourcesResource(ar, resource);
            if (!_blueprints.GetComponents<AddAbilityResources>(feature).Any())
            {
                var ar = new AddAbilityResources { name = "$AddAbilityResources$" + feature.name, RestoreAmount = true };
                _blueprints.AddComponent(feature, ar);
                _blueprints.SetAddAbilityResourcesResource(ar, resource);
            }
        }

        private void PatchAbilityResource(BlueprintAbility ability, BlueprintAbilityResource resource)
        {
            foreach (var rl in _blueprints.GetComponents<AbilityResourceLogic>(ability))
                _blueprints.SetAbilityResourceLogicResource(rl, resource);
        }

        private void PatchAbilityClassLevelRanks(BlueprintAbility ability, BlueprintCharacterClass characterClass)
        {
            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(ability))
            {
                _blueprints.ConfigureContextRankConfig(rank, AbilityRankType.Default, ContextRankBaseValueType.ClassLevel, ContextRankProgression.AsIs, characterClass: characterClass);
                _blueprints.SetContextRankMinimum(rank, 1);
            }
        }

        private static void PatchWitheringRayDamage(BlueprintAbility ability)
        {
            SpellModifierUtility.PatchRunActions(ability, action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage == null) return 0;

                damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Unholy);
                damage.Value = SpellModifierUtility.RankedD6DiceOnly(AbilityRankType.Default);
                return 1;
            });
        }

    }
}
