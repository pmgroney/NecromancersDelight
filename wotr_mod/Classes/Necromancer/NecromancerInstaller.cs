using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using UnityModManagerNet;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Necromancer
{
    internal sealed class NecromancerInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

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
        }

        public void Install(
            CharacterClassDefinition definition,
            BlueprintCharacterClass sorcererClass,
            BlueprintSpellbook sorcererSpellbook,
            BlueprintProgression sorcererProgression,
            BlueprintSpellList wizardList)
        {
            var necromancerClass = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);
            
            // Bloodline
            EnsureNecromancerBloodline();

            // Spells
            var spellList = EnsureSpellList(definition, wizardList);
            var spellbook = EnsureSpellbook(definition, sorcererSpellbook, spellList);

            // Progression
            var progression = EnsureProgression(definition, sorcererProgression, null);
            
            // Class
            necromancerClass = EnsureClass(definition, sorcererClass, spellbook, progression);

            // Archetypes
            // To be called by the coordinator or here
        }

        private BlueprintCharacterClass EnsureClass(
            CharacterClassDefinition definition,
            BlueprintCharacterClass donor,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            var characterClass = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);
            if (characterClass == null)
            {
                characterClass = _blueprints.CloneBlueprint(donor, definition.ClassGuid, definition.InternalName);
                _blueprints.AddCachedBlueprint(definition.ClassGuid, characterClass);
            }

            ConfigureClass(characterClass, definition, spellbook, progression);
            return characterClass;
        }

        private void ConfigureClass(
            BlueprintCharacterClass characterClass,
            CharacterClassDefinition definition,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            _blueprints.SetUnitFactDisplay(
                characterClass,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            characterClass.Spellbook = spellbook;
            characterClass.Progression = progression;

            if (definition.Chassis != null)
            {
                ConfigureClassChassis(definition, characterClass);
            }

            if (definition.Presentation != null)
            {
                ConfigureClassPresentation(definition, characterClass);
            }
        }

        private void ConfigureClassChassis(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            if (definition.Chassis.HitDie.HasValue)
            {
                characterClass.HitDie = definition.Chassis.HitDie.Value;
            }

            if (!string.IsNullOrEmpty(definition.Chassis.BaseAttackBonusGuid))
            {
                characterClass.BaseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                    definition.Chassis.BaseAttackBonusGuid,
                    $"{definition.InternalName} BAB");
            }
        }

        private void ConfigureClassPresentation(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            characterClass.Difficulty = definition.Presentation.Difficulty;
            if (definition.Presentation.RecommendedAttributes != null)
            {
                characterClass.RecommendedAttributes = definition.Presentation.RecommendedAttributes;
            }

            if (definition.Presentation.NotRecommendedAttributes != null)
            {
                characterClass.NotRecommendedAttributes = definition.Presentation.NotRecommendedAttributes;
            }
        }

        private BlueprintProgression EnsureProgression(
            CharacterClassDefinition definition,
            BlueprintProgression donor,
            BlueprintFeatureBase bloodlineFeature)
        {
            var progression = _blueprints.Get<BlueprintProgression>(definition.ProgressionGuid);
            if (progression == null)
            {
                progression = _blueprints.CloneBlueprint(donor, definition.ProgressionGuid, definition.InternalName + "Progression");
                _blueprints.AddCachedBlueprint(definition.ProgressionGuid, progression);
            }

            _blueprints.SetUnitFactDisplay(
                progression,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            var features = CopyFeatures(definition, progression.LevelEntries.SelectMany(le => le.Features), bloodlineFeature);
            progression.LevelEntries = new[]
            {
                CreateLevelEntry(1, features.ToArray())
            };

            AddNecromancerFeaturesToProgression(progression);

            return progression;
        }

        private List<BlueprintFeatureBase> CopyFeatures(
            CharacterClassDefinition definition,
            IEnumerable<BlueprintFeatureBase> features,
            BlueprintFeatureBase bloodlineFeature)
        {
            var result = new List<BlueprintFeatureBase>();
            foreach (var feature in features)
            {
                if (feature is BlueprintFeatureSelection selection && selection.AssetGuid == GameBlueprintIds.Selections.SorcererBloodlineSelection)
                {
                    if (definition.RemoveSorcererBloodline)
                    {
                        if (bloodlineFeature != null)
                        {
                            result.Add(bloodlineFeature);
                        }
                        continue;
                    }
                }
                result.Add(feature);
            }
            return result;
        }

        private void AddNecromancerFeaturesToProgression(BlueprintProgression progression)
        {
            var proficiencies = EnsureNecromancerProficiencies();
            var bonusFeat = EnsureNecromancerBonusFeatSelection();

            AddFeaturesToLevel(progression, 1, proficiencies, bonusFeat);
            AddFeaturesToLevel(progression, 7, bonusFeat);
            AddFeaturesToLevel(progression, 13, bonusFeat);
            AddFeaturesToLevel(progression, 19, bonusFeat);
        }

        private void AddFeaturesToLevel(BlueprintProgression progression, int level, params BlueprintFeatureBase[] featuresToAdd)
        {
            var entry = progression.LevelEntries.FirstOrDefault(le => le.Level == level);
            if (entry == null)
            {
                entry = CreateLevelEntry(level, featuresToAdd);
                var entries = progression.LevelEntries.ToList();
                entries.Add(entry);
                progression.LevelEntries = entries.OrderBy(le => le.Level).ToArray();
            }
            else
            {
                var features = entry.Features.ToList();
                features.AddRange(featuresToAdd);
                entry.Features = features;
            }
        }

        private BlueprintFeature EnsureNecromancerProficiencies()
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerProficiencies);
            if (existing != null) return existing;

            var wizardProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.WizardProficiencies,
                "Wizard proficiencies");

            var clone = _blueprints.CloneBlueprint(
                wizardProficiencies,
                ModBlueprintIds.Features.NecromancerProficiencies,
                "WotrMod_NecromancerProficiencies");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.NecromancerProficienciesName),
                _localization.Text(LocalizationIds.Mod.NecromancerProficienciesDescription));

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerProficiencies, clone);
            return clone;
        }

        private BlueprintFeatureSelection EnsureNecromancerBonusFeatSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.NecromancerBonusFeat);
            if (existing != null) return existing;

            var wizardFeat = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.WizardFeatSelection,
                "Wizard bonus feat selection");

            var clone = _blueprints.CloneBlueprint(
                wizardFeat,
                ModBlueprintIds.Selections.NecromancerBonusFeat,
                "WotrMod_NecromancerBonusFeatSelection");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.NecromancerBonusFeatName),
                _localization.Text(LocalizationIds.Mod.NecromancerBonusFeatDescription));

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.NecromancerBonusFeat, clone);
            return clone;
        }

        private BlueprintSpellList EnsureSpellList(CharacterClassDefinition definition, BlueprintSpellList donor)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(definition.SpellListGuid);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(donor, definition.SpellListGuid, definition.InternalName + "SpellList");
                _blueprints.AddCachedBlueprint(definition.SpellListGuid, spellList);
            }

            ConfigureNecromancerSpellList(spellList);
            return spellList;
        }

        private void ConfigureNecromancerSpellList(BlueprintSpellList spellList, int minimumSpellLevel = 0)
        {
            _blueprints.ClearSpellList(spellList);
            foreach (var spellDef in NecromancerSpellRegistry.GetAll())
            {
                if (spellDef.SpellLevel < minimumSpellLevel) continue;

                var spell = _blueprints.Get<BlueprintAbility>(spellDef.NewSpellGuid);
                if (spell != null)
                {
                    _blueprints.AddSpellToList(spellList, spell, spellDef.SpellLevel);
                    ApplySelectionRecommendation(spell, spellDef);
                }
            }
        }

        private void ApplySelectionRecommendation(BlueprintScriptableObject blueprint, NecromancerSpellDefinition definition)
        {
            if (definition.IsRecommended)
            {
                _blueprints.AddSelectionRecommendation(blueprint);
            }
        }

        private BlueprintSpellbook EnsureSpellbook(CharacterClassDefinition definition, BlueprintSpellbook donor, BlueprintSpellList spellList)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(definition.SpellbookGuid);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(donor, definition.SpellbookGuid, definition.InternalName + "Spellbook");
                _blueprints.AddCachedBlueprint(definition.SpellbookGuid, spellbook);
            }

            spellbook.SpellList = spellList;
            spellbook.CastingStat = definition.CastingStat;
            _blueprints.SetUnitFactDisplay(
                spellbook,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));

            return spellbook;
        }

        private BlueprintProgression EnsureNecromancerBloodline()
        {
            var existing = _blueprints.Get<BlueprintProgression>(ModBlueprintIds.Progressions.NecromancerBloodline);
            var necromancerClass = _blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Necromancer);

            var clone = existing ?? _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintProgression>(
                        GameBlueprintIds.Progressions.UndeadBloodline,
                        "Undead bloodline donor"),
                    ModBlueprintIds.Progressions.NecromancerBloodline,
                    "WotrMod_NecromancerBloodline");
            
            _blueprints.SetComponents(clone);

            _blueprints.SetUnitFactDisplay(
                clone,
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
                "WotrMod_NecromancerBloodlinePower20",
                "One of Us",
                LocalizationIds.Mod.NecromancerBloodlinePower20Name,
                LocalizationIds.Mod.NecromancerBloodlinePower20Description,
                necromancerClass);

            var boneArmor = EnsureBoneArmorFeature(necromancerClass);
            var boneSpike = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel1,
                ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell,
                "WotrMod_NecromancerKnownSpell_BoneSpike",
                "Bone Spike donor",
                ModBlueprintIds.Spells.BoneSpike,
                "Bone Spike",
                LocalizationIds.Mod.SpellBoneSpikeName,
                LocalizationIds.Mod.SpellBoneSpikeDescription,
                1,
                necromancerClass);
            var corpseExplosion = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel2,
                ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell,
                "WotrMod_NecromancerKnownSpell_CorpseExplosion",
                "Corpse Explosion donor",
                ModBlueprintIds.Spells.CorpseExplosion,
                "Corpse Explosion",
                LocalizationIds.Mod.SpellCorpseExplosionName,
                LocalizationIds.Mod.SpellCorpseExplosionDescription,
                2,
                necromancerClass);
            var eldritchHorror = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel3,
                ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell,
                "WotrMod_NecromancerKnownSpell_EldritchHorror",
                "Eldritch Horror donor",
                ModBlueprintIds.Spells.EldritchHorror,
                "Eldritch Horror",
                LocalizationIds.Mod.SpellEldritchHorrorName,
                LocalizationIds.Mod.SpellEldritchHorrorDescription,
                3,
                necromancerClass);
            var hellOnEarth = EnsureKnownSpellFeature(
                GameBlueprintIds.Features.BloodlineUndeadSpellLevel9,
                ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell,
                "WotrMod_NecromancerKnownSpell_HellOnEarth",
                "Hell on Earth donor",
                ModBlueprintIds.Spells.HellOnEarth,
                "Hell on Earth",
                LocalizationIds.Mod.SpellHellOnEarthName,
                LocalizationIds.Mod.SpellHellOnEarthDescription,
                9,
                necromancerClass);

            clone.LevelEntries = new[]
            {
                CreateLevelEntry(1, arcana, power1, boneArmor),
                CreateLevelEntry(2, boneSpike),
                CreateLevelEntry(3, power3),
                CreateLevelEntry(4, corpseExplosion),
                CreateLevelEntry(5, boneArmor),
                CreateLevelEntry(7, eldritchHorror),
                CreateLevelEntry(9, power3, power9, boneArmor),
                CreateLevelEntry(13, boneArmor),
                CreateLevelEntry(15, power15),
                CreateLevelEntry(17, boneArmor),
                CreateLevelEntry(19, hellOnEarth),
                CreateLevelEntry(20, power20)
            };

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Progressions.NecromancerBloodline, clone);
            return clone;
        }

        private BlueprintFeature EnsureMasterOfDeathFeature(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlineArcana);
            if (existing != null) return existing;

            var donor = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.BloodlineUndeadArcana,
                "Undead arcana donor");

            var clone = _blueprints.CloneBlueprint(
                donor,
                ModBlueprintIds.Features.NecromancerBloodlineArcana,
                "WotrMod_NecromancerBloodlineArcana");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaName),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaDescription));

            _blueprints.SetComponents(clone, new MasterOfDeathArcanaComponent());

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBloodlineArcana, clone);
            return clone;
        }

        private BlueprintFeature EnsureWitheringRayFeature(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerWitheringRayFeature);
            if (existing != null) return existing;

            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadGraveTouchResource,
                ModBlueprintIds.AbilityResources.NecromancerWitheringRayResource,
                "WotrMod_NecromancerWitheringRayResource");

            var ability = EnsureWitheringRayAbility(characterClass, resource);

            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadGraveTouchFeature,
                ModBlueprintIds.Features.NecromancerWitheringRayFeature,
                "WotrMod_NecromancerWitheringRayFeature",
                "Grave Touch",
                LocalizationIds.Mod.NecromancerWitheringRayName,
                LocalizationIds.Mod.NecromancerWitheringRayDescription,
                characterClass);

            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);
            PatchWitheringRayDamage(ability);

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerWitheringRayFeature, feature);
            return feature;
        }

        private BlueprintAbility EnsureWitheringRayAbility(BlueprintCharacterClass characterClass, BlueprintAbilityResource resource)
        {
            var existing = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.NecromancerWitheringRay);
            if (existing != null) return existing;

            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadGraveTouchAbility,
                ModBlueprintIds.Abilities.NecromancerWitheringRay,
                "WotrMod_NecromancerWitheringRayAbility",
                "Grave Touch",
                LocalizationIds.Mod.NecromancerWitheringRayName,
                LocalizationIds.Mod.NecromancerWitheringRayDescription,
                characterClass,
                resource);

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.NecromancerWitheringRay, ability);
            return ability;
        }

        private void PatchWitheringRayDamage(BlueprintAbility ability)
        {
            _blueprints.EditComponent<AbilityEffectRunAction>(ability, comp =>
            {
                var damage = comp.Actions.Actions.OfType<ContextActionDealDamage>().FirstOrDefault();
                if (damage != null)
                {
                    damage.Value.DiceCountValue = CreateRankValue();
                    damage.Value.DiceType = Kingmaker.RuleSystem.DiceType.D6;
                    damage.Value.BonusValue = 0;
                }
            });
        }

        private BlueprintFeature EnsureDeathsGiftFeature(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerDeathsGiftFeature);
            if (existing != null) return existing;

            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadDeathsGiftFeature,
                ModBlueprintIds.Features.NecromancerDeathsGiftFeature,
                "WotrMod_NecromancerDeathsGiftFeature",
                "Death's Gift",
                LocalizationIds.Mod.NecromancerDeathsGiftName,
                LocalizationIds.Mod.NecromancerDeathsGiftDescription,
                characterClass);

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerDeathsGiftFeature, feature);
            return feature;
        }

        private BlueprintFeature EnsureGraspOfTheDeadFeature(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerGraspOfTheDeadFeature);
            if (existing != null) return existing;

            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadGraspOfTheDeadResource,
                ModBlueprintIds.AbilityResources.NecromancerGraspOfTheDeadResource,
                "WotrMod_NecromancerGraspOfTheDeadResource");

            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadGraspOfTheDeadAbility,
                ModBlueprintIds.Abilities.NecromancerGraspOfTheDead,
                "WotrMod_NecromancerGraspOfTheDeadAbility",
                "Grasp of the Dead",
                LocalizationIds.Mod.NecromancerGraspOfTheDeadName,
                LocalizationIds.Mod.NecromancerGraspOfTheDeadDescription,
                characterClass,
                resource);

            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadGraspOfTheDeadFeature,
                ModBlueprintIds.Features.NecromancerGraspOfTheDeadFeature,
                "WotrMod_NecromancerGraspOfTheDeadFeature",
                "Grasp of the Dead",
                LocalizationIds.Mod.NecromancerGraspOfTheDeadName,
                LocalizationIds.Mod.NecromancerGraspOfTheDeadDescription,
                characterClass);

            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerGraspOfTheDeadFeature, feature);
            return feature;
        }

        private BlueprintFeature EnsureIncorporealFormFeature(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerIncorporealFormFeature);
            if (existing != null) return existing;

            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadIncorporealFormResource,
                ModBlueprintIds.AbilityResources.NecromancerIncorporealFormResource,
                "WotrMod_NecromancerIncorporealFormResource");

            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadIncorporealFormAbility,
                ModBlueprintIds.Abilities.NecromancerIncorporealForm,
                "WotrMod_NecromancerIncorporealFormAbility",
                "Incorporeal Form",
                LocalizationIds.Mod.NecromancerIncorporealFormName,
                LocalizationIds.Mod.NecromancerIncorporealFormDescription,
                characterClass,
                resource);

            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadIncorporealFormFeature,
                ModBlueprintIds.Features.NecromancerIncorporealFormFeature,
                "WotrMod_NecromancerIncorporealFormFeature",
                "Incorporeal Form",
                LocalizationIds.Mod.NecromancerIncorporealFormName,
                LocalizationIds.Mod.NecromancerIncorporealFormDescription,
                characterClass);

            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerIncorporealFormFeature, feature);
            return feature;
        }

        private BlueprintFeature EnsureBoneArmorFeature(BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBoneArmor);
            if (existing != null) return existing;

            var feature = new BlueprintFeature
            {
                name = "WotrMod_NecromancerBoneArmor",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.NecromancerBoneArmor)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBoneArmor, feature);

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorName),
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorDescription));

            var icon = _icons.Load("Icons/bone_armor.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            _blueprints.SetComponents(feature);
            _blueprints.AddContextRankConfig(feature, characterClass);

            return feature;
        }

        private BlueprintFeature EnsureKnownSpellFeature(
            string donorGuid,
            string featureGuid,
            string internalName,
            string donorName,
            string spellGuid,
            string spellName,
            string displayNameKey,
            string descriptionKey,
            int spellLevel,
            BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (existing != null) return existing;

            var donor = _blueprints.Require<BlueprintFeature>(donorGuid, donorName);
            var spell = _blueprints.Require<BlueprintAbility>(spellGuid, spellName);

            var clone = _blueprints.CloneBlueprint(donor, featureGuid, internalName);
            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));

            _blueprints.EditComponent<AddKnownSpell>(clone, comp =>
            {
                comp.Spell = spell;
                comp.CharacterClass = characterClass;
                comp.SpellLevel = spellLevel;
            });

            _blueprints.AddCachedBlueprint(featureGuid, clone);
            return clone;
        }

        private BlueprintAbilityResource EnsureAbilityResource(string donorGuid, string resourceGuid, string internalName)
        {
            var existing = _blueprints.Get<BlueprintAbilityResource>(resourceGuid);
            if (existing != null) return existing;

            var donor = _blueprints.Require<BlueprintAbilityResource>(donorGuid, internalName + " donor");
            var clone = _blueprints.CloneBlueprint(donor, resourceGuid, internalName);
            _blueprints.AddCachedBlueprint(resourceGuid, clone);
            return clone;
        }

        private BlueprintAbility EnsureNecromancerClassLevelAbility(
            string donorGuid,
            string abilityGuid,
            string internalName,
            string donorName,
            string displayNameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass,
            BlueprintAbilityResource resource)
        {
            var donor = _blueprints.Require<BlueprintAbility>(donorGuid, donorName);
            var clone = _blueprints.CloneBlueprint(donor, abilityGuid, internalName);

            _blueprints.SetAbilityDisplay(
                clone,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));

            PatchAbilityResource(clone, resource);
            PatchAbilityClassLevelRanks(clone, characterClass);

            return clone;
        }

        private BlueprintFeature EnsureNecromancerBloodlineFeature(
            string donorGuid,
            string featureGuid,
            string internalName,
            string donorName,
            string displayNameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass)
        {
            var existing = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (existing != null) return existing;

            var donor = _blueprints.Require<BlueprintFeature>(donorGuid, donorName);
            var clone = _blueprints.CloneBlueprint(donor, featureGuid, internalName);

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));

            _blueprints.AddContextRankConfig(clone, characterClass);

            return clone;
        }

        private void PatchFeatureAbilityAndResource(
            BlueprintFeature feature,
            BlueprintAbility ability,
            BlueprintAbilityResource resource,
            BlueprintCharacterClass characterClass)
        {
            _blueprints.EditComponent<AddFacts>(feature, comp =>
            {
                comp.Facts = new BlueprintUnitFactReference[] { ability.ToReference<BlueprintUnitFactReference>() };
            });

            _blueprints.EditComponent<AddAbilityResources>(feature, comp =>
            {
                comp.Resource = resource.ToReference<BlueprintAbilityResourceReference>();
            });

            _blueprints.AddContextRankConfig(feature, characterClass);
        }

        private void PatchAbilityResource(BlueprintAbility ability, BlueprintAbilityResource resource)
        {
            _blueprints.EditComponent<AbilityResourceLogic>(ability, comp =>
            {
                comp.RequiredResource = resource.ToReference<BlueprintAbilityResourceReference>();
            });
        }

        private void PatchAbilityClassLevelRanks(BlueprintAbility ability, BlueprintCharacterClass characterClass)
        {
            _blueprints.AddContextRankConfig(ability, characterClass);
        }

        private ContextValue CreateRankValue()
        {
            return new ContextValue
            {
                ValueType = ContextValueType.Rank,
                ValueRank = AbilityRankType.Default
            };
        }

        private LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            return new LevelEntry
            {
                Level = level,
                Features = features.ToList()
            };
        }
    }
}
