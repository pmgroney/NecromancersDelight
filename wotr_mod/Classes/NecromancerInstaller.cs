using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Designers.Mechanics.Buffs;
using wotr_mod.Features;
using UnityModManagerNet;
using wotr_mod.Content.Localization;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes
{
    internal sealed class NecromancerInstaller : IClassContentInstaller
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

        public bool CanInstall(CharacterClassDefinition definition)
        {
            return definition.UseNecromancerBloodline;
        }

        public void RegisterLocalization()
        {
            // Implementation moved here if specific
        }

        public void Install(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            ConfigureNecromancerSpellList(spellList);
            EnsureNecromancerBloodline();
            RegisterNecromancerFeatures(characterClass);

            _blueprints.SetCharacterClassArchetypes(
                characterClass,
                EnsureArchetypes(definition, characterClass, spellbook, spellList));

            if (characterClass.Progression != null)
            {
                AddNecromancerFeaturesToProgression(characterClass.Progression);
            }
        }

        private void AddNecromancerFeaturesToProgression(BlueprintProgression progression)
        {
            var features = GetNecromancerFeatures();
            var necromancerProficiencies = features[0];
            var masterOfDeath = features[1];
            var witheringRay = features[2];
            var deathsGift = features[3];
            var graspOfTheDead = features[4];
            var incorporealForm = features[5];
            var oneOfUs = features[6];
            var boneArmor = features[7];
            var boneSpike = features[8];
            var corpseExplosion = features[9];
            var eldritchHorror = features[10];
            var hellOnEarth = features[11];
            var necromancerBonusFeat = features[12];

            AddFeaturesToLevel(progression, 1, necromancerProficiencies, masterOfDeath, witheringRay, boneArmor);
            AddFeaturesToLevel(progression, 2, boneSpike);
            AddFeaturesToLevel(progression, 3, deathsGift);
            AddFeaturesToLevel(progression, 4, corpseExplosion);
            AddFeaturesToLevel(progression, 5, boneArmor);
            AddFeaturesToLevel(progression, 6, necromancerBonusFeat);
            AddFeaturesToLevel(progression, 7, eldritchHorror);
            AddFeaturesToLevel(progression, 9, deathsGift, graspOfTheDead, boneArmor);
            AddFeaturesToLevel(progression, 10, necromancerBonusFeat);
            AddFeaturesToLevel(progression, 13, boneArmor);
            AddFeaturesToLevel(progression, 15, deathsGift, incorporealForm);
            AddFeaturesToLevel(progression, 16, necromancerBonusFeat);
            AddFeaturesToLevel(progression, 17, boneArmor);
            AddFeaturesToLevel(progression, 19, hellOnEarth);
            AddFeaturesToLevel(progression, 20, oneOfUs);

            var classCards = new List<BlueprintFeatureBase> { masterOfDeath };
            _blueprints.SetProgressionUiDeterminators(progression, classCards);
            _blueprints.SetProgressionUiGroups(
                progression,
                new[] { boneArmor },
                new[] { deathsGift },
                new[] { necromancerBonusFeat },
                new[] { witheringRay, graspOfTheDead, incorporealForm, oneOfUs },
                new[] { boneSpike, corpseExplosion, eldritchHorror, hellOnEarth });
        }

        private static void AddFeaturesToLevel(
            BlueprintProgression progression,
            int level,
            params BlueprintFeatureBase[] featuresToAdd)
        {
            progression.LevelEntries = progression.LevelEntries ?? Array.Empty<LevelEntry>();

            var entry = progression.LevelEntries.FirstOrDefault(e => e.Level == level);
            if (entry == null)
            {
                entry = new LevelEntry { Level = level };
                entry.SetFeatures(featuresToAdd.Where(feature => feature != null));
                progression.LevelEntries = progression.LevelEntries.Concat(new[] { entry }).OrderBy(e => e.Level).ToArray();
                return;
            }

            var features = entry.Features.ToList();
            foreach (var feature in featuresToAdd.Where(feature => feature != null))
            {
                if (!features.Any(existing => existing != null && existing.AssetGuid == feature.AssetGuid))
                {
                    features.Add(feature);
                }
            }

            entry.SetFeatures(features);
        }

        private void RegisterNecromancerFeatures(BlueprintCharacterClass characterClass)
        {
            foreach (var feature in GetNecromancerFeatures())
            {
                if (feature == null)
                {
                    continue;
                }

                _blueprints.SetProgressionClasses(feature, characterClass);
            }
        }

        private BlueprintFeature[] GetNecromancerFeatures()
        {
            return new[]
            {
                EnsureNecromancerProficiencies(),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBloodlineArcana,
                    "Necromancer Arcana"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBloodlinePower1,
                    "Withering Ray"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBloodlinePower3,
                    "Death's Gift"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBloodlinePower9,
                    "Grasp of the Dead"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBloodlinePower15,
                    "Incorporeal Form"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBloodlinePower20,
                    "One of Us"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBoneArmor,
                    "Bone Armor"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell,
                    "Bone Spike"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell,
                    "Corpse Explosion"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell,
                    "Eldritch Horror"),
                GetNecromancerFeature(
                    ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell,
                    "Hell on Earth"),
                EnsureNecromancerBonusFeatSelection()
            };
        }

        private BlueprintFeature GetNecromancerFeature(string guid, string name)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null)
            {
                _blueprints.ReportError($"WotrMod_NecromancerClass missing expected feature: {name} ({guid}).");
            }

            return feature;
        }

        private BlueprintFeature EnsureNecromancerProficiencies()
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerProficiencies);
            if (existing != null)
            {
                return existing;
            }

            var sorcererProficiencies = _blueprints.Require<BlueprintFeature>(
                "25c97697236ccf2479d0c6a4185eae7f",
                "Sorcerer Proficiencies");
            var scytheProficiency = _blueprints.Require<BlueprintFeature>(
                "96c174b0ebca7b246b82d4bc4aac4574",
                "Scythe Proficiency");
            var simpleWeaponProficiency = _blueprints.Require<BlueprintFeature>(
                "e70ecf1ed95ca2f40b754f1adb22bbdd",
                "Simple Weapon Proficiency");

            var clone = _blueprints.CloneBlueprint(
                sorcererProficiencies,
                ModBlueprintIds.Features.NecromancerProficiencies,
                "NecromancerProficiencies");

            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(LocalizationIds.Mod.NecromancerProficienciesName),
                _localization.Text(LocalizationIds.Mod.NecromancerProficienciesDescription));

            var addFacts = _blueprints.EnsureComponent<AddFacts>(clone, () => new AddFacts());
            _blueprints.SetAddFacts(addFacts, simpleWeaponProficiency, scytheProficiency);

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerProficiencies, clone);

            return clone;
        }

        private void ConfigureNecromancerSpellList(BlueprintSpellList spellList, int minimumSpellLevel = 0)
        {
            var spellsByLevel = NecromancerSpellRegistry.GetAll()
                .Where(s => s.SpellLevel >= minimumSpellLevel)
                .GroupBy(s => s.SpellLevel);

            foreach (var levelGroup in spellsByLevel)
            {
                foreach (var spellDef in levelGroup)
                {
                    var spell = _blueprints.Get<BlueprintAbility>(spellDef.SpellGuid);
                    if (spell == null)
                    {
                        _logger.Warning($"Could not find spell {spellDef.DisplayName} ({spellDef.SpellGuid}) for Necromancer spell list.");
                        continue;
                    }

                    _blueprints.AddSpellToList(spellList, spell, levelGroup.Key);
                    ApplySelectionRecommendation(spell, spellDef);
                }
            }
        }

        private void ApplySelectionRecommendation(BlueprintScriptableObject blueprint, ClassSpellDefinition definition)
        {
            if (definition.Recommendation == null)
            {
                return;
            }

            _blueprints.AddSelectionRecommendation(blueprint, definition.Recommendation.Value, "$NecromancerSelectionRecommendation$" + blueprint.name);
        }

        private BlueprintArchetype[] EnsureArchetypes(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            return new[]
            {
                EnsureSepulchritArchetype(characterClass, spellbook, spellList),
                EnsureGravebladeArchetype(characterClass, spellbook, spellList)
            };
        }

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features.Where(feature => feature != null));
            return entry;
        }

        private BlueprintArchetype EnsureSepulchritArchetype(
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Sepulchrit);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_NecromancerSepulchritArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Sepulchrit)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Sepulchrit, archetype);
            }

            var sepulchritSpellbook = EnsureSepulchritSpellbook(baseSpellbook, spellList, characterClass);
            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.SepulchritName),
                _localization.Text(LocalizationIds.Mod.SepulchritDescription));
            _blueprints.SetArchetypeReplaceSpellbook(archetype, sepulchritSpellbook);
            _blueprints.SetArchetypeFeatureChanges(archetype, Array.Empty<LevelEntry>(), Array.Empty<LevelEntry>());
            _blueprints.SetArchetypeBuildChanging(archetype, true);
            _blueprints.SetArchetypeAttributeRecommendations(
                archetype,
                new[] { StatType.Intelligence, StatType.Dexterity, StatType.Constitution },
                new[] { StatType.Strength, StatType.Charisma });

            return archetype;
        }

        private BlueprintArchetype EnsureGravebladeArchetype(
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Graveblade);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_NecromancerGravebladeArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Graveblade)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Graveblade, archetype);
            }

            var gravebladeSpellList = EnsureGravebladeSpellList(spellList);
            var gravebladeSpellbook = EnsureGravebladeSpellbook(baseSpellbook, gravebladeSpellList, characterClass);
            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.GravebladeName),
                _localization.Text(LocalizationIds.Mod.GravebladeDescription));
            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusHigh,
                "Graveblade base attack bonus progression");
            var proficiencies = EnsureGravebladeProficiencies();
            var reapingEdge = EnsureGravebladeReapingEdge(characterClass);
            var bonusFeat = EnsureGravebladeBonusFeatSelection();
            var fighterTraining = EnsureGravebladeFighterTraining(characterClass, bonusFeat);
            _blueprints.SetArchetypeReplaceSpellbook(archetype, gravebladeSpellbook);
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                new[]
                {
                    CreateLevelEntry(1, proficiencies, fighterTraining, reapingEdge),
                    CreateLevelEntry(6, bonusFeat),
                    CreateLevelEntry(10, bonusFeat),
                    CreateLevelEntry(16, bonusFeat)
                },
                new[]
                {
                    CreateLevelEntry(
                        1,
                        GetNecromancerFeature(
                            ModBlueprintIds.Features.NecromancerBloodlineArcana,
                            "Master of Death"),
                        GetNecromancerFeature(
                            ModBlueprintIds.Features.NecromancerBloodlinePower1,
                            "Withering Ray")),
                    CreateLevelEntry(
                        2,
                        GetNecromancerFeature(
                            ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell,
                            "Bone Spike granted spell")),
                    CreateLevelEntry(
                        4,
                        GetNecromancerFeature(
                            ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell,
                            "Corpse Explosion granted spell")),
                    CreateLevelEntry(6, EnsureNecromancerBonusFeatSelection()),
                    CreateLevelEntry(
                        7,
                        GetNecromancerFeature(
                            ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell,
                            "Eldritch Horror granted spell")),
                    CreateLevelEntry(10, EnsureNecromancerBonusFeatSelection()),
                    CreateLevelEntry(16, EnsureNecromancerBonusFeatSelection()),
                    CreateLevelEntry(
                        19,
                        GetNecromancerFeature(
                            ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell,
                            "Hell on Earth granted spell"))
                });
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            _blueprints.SetArchetypeSignatureAbilities(archetype, reapingEdge);
            AddGravebladeFeaturesToProgressionUi(characterClass.Progression, reapingEdge);
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private void AddGravebladeFeaturesToProgressionUi(
            BlueprintProgression progression,
            BlueprintFeatureBase reapingEdge)
        {
            if (progression == null || reapingEdge == null)
            {
                return;
            }

            var features = GetNecromancerFeatures();
            var witheringRay = (BlueprintFeatureBase)features[2];
            var deathsGift = (BlueprintFeatureBase)features[3];
            var graspOfTheDead = (BlueprintFeatureBase)features[4];
            var incorporealForm = (BlueprintFeatureBase)features[5];
            var oneOfUs = (BlueprintFeatureBase)features[6];
            var boneArmor = (BlueprintFeatureBase)features[7];
            var boneSpike = (BlueprintFeatureBase)features[8];
            var corpseExplosion = (BlueprintFeatureBase)features[9];
            var eldritchHorror = (BlueprintFeatureBase)features[10];
            var hellOnEarth = (BlueprintFeatureBase)features[11];
            var necromancerBonusFeat = (BlueprintFeatureBase)features[12];

            _blueprints.SetProgressionUiGroups(
                progression,
                new[] { boneArmor },
                new[] { deathsGift },
                new[] { necromancerBonusFeat },
                new[] { witheringRay, reapingEdge, graspOfTheDead, incorporealForm, oneOfUs },
                new[] { boneSpike, corpseExplosion, eldritchHorror, hellOnEarth });
        }

        private BlueprintFeature EnsureGravebladeFighterTraining(
            BlueprintCharacterClass characterClass,
            BlueprintFeatureSelection bonusFeatSelection)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeFighterTraining);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.MagusFighterTraining,
                        "Magus Fighter Training"),
                    ModBlueprintIds.Features.GravebladeFighterTraining,
                    "WotrMod_NecromancerGravebladeTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeFighterTraining, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingName),
                _localization.Text(LocalizationIds.Mod.GravebladeFighterTrainingDescription));
            _blueprints.ConfigureClassLevelsForPrerequisites(
                feature,
                _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Fighter, "Fighter class"),
                characterClass,
                bonusFeatSelection,
                1.0,
                0);

            return feature;
        }

        private BlueprintFeature EnsureGravebladeProficiencies()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeProficiencies);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerGravebladeProficiencies",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.GravebladeProficiencies),
                    IsClassFeature = true,
                    Ranks = 1
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeProficiencies, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.GravebladeProficienciesName),
                _localization.Text(LocalizationIds.Mod.GravebladeProficienciesDescription));

            var addFacts = new AddFacts { name = "$AddFacts$GravebladeProficiencies" };
            _blueprints.SetAddFacts(
                addFacts,
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyLight, "Light Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyMedium, "Medium Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyHeavy, "Heavy Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.MartialWeaponProficiency, "Martial Weapon Proficiency"));
            _blueprints.SetComponents(feature, addFacts);

            return feature;
        }

        private BlueprintFeature EnsureGravebladeReapingEdge(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeReapingEdge);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeFeature",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.GravebladeReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeReapingEdge, feature);
            }

            var brittleBoneBuff = EnsureReapingEdgeBrittleBoneBuff();
            var fatigueBuff = EnsureReapingEdgeConditionBuff(
                ModBlueprintIds.Buffs.ReapingEdgeFatigue,
                "WotrMod_NecromancerGravebladeReapingEdgeFatigueBuff",
                UnitCondition.Fatigued,
                LocalizationIds.Mod.GravebladeReapingEdgeFatigueName,
                LocalizationIds.Mod.GravebladeReapingEdgeFatigueDescription);
            var exhaustionBuff = EnsureReapingEdgeConditionBuff(
                ModBlueprintIds.Buffs.ReapingEdgeExhaustion,
                "WotrMod_NecromancerGravebladeReapingEdgeExhaustionBuff",
                UnitCondition.Exhausted,
                LocalizationIds.Mod.GravebladeReapingEdgeExhaustionName,
                LocalizationIds.Mod.GravebladeReapingEdgeExhaustionDescription);
            var buff = EnsureReapingEdgeBuff(characterClass, brittleBoneBuff, fatigueBuff, exhaustionBuff);
            var resource = EnsureReapingEdgeResource(characterClass);
            var ability = EnsureReapingEdgeAbility(resource, buff);

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            var addFacts = new AddFacts { name = "$AddFacts$GravebladeReapingEdge" };
            _blueprints.SetAddFacts(addFacts, ability);
            _blueprints.SetComponents(feature, addFacts);
            
            // Note: PatchFeatureResource was in CharacterClassInstaller, I should copy it or inline it
            var addResource = new AddAbilityResources { name = "$AddAbilityResources$GravebladeReapingEdge" };
            _blueprints.SetAddAbilityResourcesResource(addResource, resource);
            _blueprints.AddComponent(feature, addResource);

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintAbilityResource EnsureReapingEdgeResource(BlueprintCharacterClass characterClass)
        {
            var resource = _blueprints.Get<BlueprintAbilityResource>(ModBlueprintIds.AbilityResources.ReapingEdge);
            if (resource == null)
            {
                resource = new BlueprintAbilityResource
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeResource",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.AbilityResources.ReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.AbilityResources.ReapingEdge, resource);
            }

            _blueprints.ConfigureAbilityResourceMaxAmount(resource, 0, StatType.Charisma, characterClass);
            return resource;
        }

        private BlueprintAbility EnsureReapingEdgeAbility(BlueprintAbilityResource resource, BlueprintBuff buff)
        {
            var ability = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.ReapingEdge);
            if (ability == null)
            {
                ability = new BlueprintAbility
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeAbility",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Abilities.ReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.ReapingEdge, ability);
            }

            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(ability, icon);
            }

            var applyBuff = new ContextActionApplyBuff { name = "$ContextActionApplyBuff$GravebladeReapingEdge" };
            _blueprints.SetApplyBuffActionBuff(applyBuff, buff);
            applyBuff.Permanent = true;

            var runAction = new AbilityExecuteActionOnCast { name = "$AbilityExecuteActionOnCast$GravebladeReapingEdge" };
            runAction.Actions = new ActionList { Actions = new GameAction[] { applyBuff } };

            var resourceLogic = new AbilityResourceLogic { name = "$AbilityResourceLogic$GravebladeReapingEdge" };
            _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
            _blueprints.SetAbilityResourceLogicSpendResource(resourceLogic, true);

            _blueprints.SetComponents(ability, runAction, resourceLogic);
            ability.ActionType = UnitCommand.CommandType.Free;
            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Personal;
            ability.CanTargetSelf = true;

            return ability;
        }

        private BlueprintBuff EnsureReapingEdgeBuff(
            BlueprintCharacterClass characterClass,
            BlueprintBuff brittleBoneBuff,
            BlueprintBuff fatigueBuff,
            BlueprintBuff exhaustionBuff)
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ReapingEdge);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_NecromancerGravebladeReapingEdgeBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.ReapingEdge)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ReapingEdge, buff);
            }

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBuffName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBuffDescription));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(buff, icon);
            }

            _blueprints.SetComponents(
                buff,
                new ReapingEdgeComponent
                {
                    name = "$ReapingEdgeComponent$Graveblade",
                    CharacterClass = characterClass,
                    BrittleBoneBuff = brittleBoneBuff,
                    FatigueBuff = fatigueBuff,
                    ExhaustionBuff = exhaustionBuff
                });

            return buff;
        }

        private BlueprintBuff EnsureReapingEdgeBrittleBoneBuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.ReapingEdgeBrittleBone);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_NecromancerGravebladeBrittleBoneBuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.ReapingEdgeBrittleBone)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.ReapingEdgeBrittleBone, buff);
            }

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeBrittleBoneDescription));
            _blueprints.SetComponents(
                buff,
                new AddStatBonus
                {
                    name = "$AddStatBonus$GravebladeBrittleBone",
                    Stat = StatType.AC,
                    Value = -2,
                    Descriptor = ModifierDescriptor.Penalty
                });

            return buff;
        }

        private BlueprintBuff EnsureReapingEdgeConditionBuff(
            string buffGuid,
            string internalName,
            UnitCondition condition,
            string displayNameKey,
            string descriptionKey)
        {
            var buff = _blueprints.Get<BlueprintBuff>(buffGuid);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = internalName,
                    AssetGuid = BlueprintGuid.Parse(buffGuid)
                };
                _blueprints.AddCachedBlueprint(buffGuid, buff);
            }

            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$" + internalName,
                    Condition = condition
                });

            return buff;
        }

        private BlueprintFeatureSelection EnsureGravebladeBonusFeatSelection()
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.GravebladeBonusFeat);
            if (selection == null)
            {
                selection = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeatureSelection>(GameBlueprintIds.Selections.FighterFeat, "Fighter bonus feat selection"),
                    ModBlueprintIds.Selections.GravebladeBonusFeat,
                    "WotrMod_GravebladeBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.GravebladeBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatName),
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatDescription));

            return selection;
        }

        private BlueprintSpellList EnsureGravebladeSpellList(BlueprintSpellList baseSpellList)
        {
            var list = _blueprints.Get<BlueprintSpellList>(ModBlueprintIds.SpellLists.Graveblade);
            if (list == null)
            {
                list = _blueprints.CloneBlueprint(baseSpellList, ModBlueprintIds.SpellLists.Graveblade, "WotrMod_GravebladeSpellList");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.SpellLists.Graveblade, list);
            }

            ConfigureNecromancerSpellList(list, minimumSpellLevel: 1);
            return list;
        }

        private BlueprintSpellbook EnsureGravebladeSpellbook(
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList,
            BlueprintCharacterClass characterClass)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(ModBlueprintIds.Spellbooks.Graveblade);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(baseSpellbook, ModBlueprintIds.Spellbooks.Graveblade, "WotrMod_GravebladeSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Graveblade, spellbook);
            }

            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
            spellbook.CastingAttribute = StatType.Intelligence;

            return spellbook;
        }

        private BlueprintSpellbook EnsureSepulchritSpellbook(
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList,
            BlueprintCharacterClass characterClass)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(ModBlueprintIds.Spellbooks.Sepulchrit);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(baseSpellbook, ModBlueprintIds.Spellbooks.Sepulchrit, "WotrMod_SepulchritSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Sepulchrit, spellbook);
            }

            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
            spellbook.CastingAttribute = StatType.Intelligence;

            return spellbook;
        }

        private BlueprintFeatureSelection EnsureNecromancerBonusFeatSelection()
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.NecromancerBonusFeat);
            if (selection == null)
            {
                selection = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeatureSelection>(GameBlueprintIds.Selections.SorcererBonusFeat, "Sorcerer bonus feat selection"),
                    ModBlueprintIds.Selections.NecromancerBonusFeat,
                    "WotrMod_NecromancerBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.NecromancerBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.NecromancerBonusFeatName),
                _localization.Text(LocalizationIds.Mod.NecromancerBonusFeatDescription));

            return selection;
        }

        private void EnsureNecromancerBloodline()
        {
            var bloodline = _blueprints.Get<BlueprintProgression>(ModBlueprintIds.Progressions.NecromancerBloodline);
            if (bloodline != null)
            {
                ReportMissingNecromancerFeatures("existing bloodline");
                return;
            }

            var sorcererBloodlineSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SorcererBloodline,
                "Sorcerer bloodline selection");
            var undeadBloodline = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.UndeadBloodline,
                "Undead bloodline");

            bloodline = _blueprints.CloneBlueprint(
                undeadBloodline,
                ModBlueprintIds.Progressions.NecromancerBloodline,
                "NecromancerBloodline");

            _blueprints.SetUnitFactDisplay(
                bloodline,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineName),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineDescription));

            var arcana = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlineArcana);
            if (arcana == null)
            {
                arcana = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.RedDragonBloodlineArcana, "Red Dragon Arcana"),
                    ModBlueprintIds.Features.NecromancerBloodlineArcana,
                    "NecromancerBloodlineArcana");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBloodlineArcana, arcana);
            }

            _blueprints.SetUnitFactDisplay(
                arcana,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaName),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaDescription));

            var features = bloodline.LevelEntries.SelectMany(le => le.Features).ToList();
            var entry1 = bloodline.LevelEntries.First(le => le.Level == 1);
            var entry1Features = entry1.Features.ToList();
            entry1Features.Add(arcana);
            entry1.SetFeatures(entry1Features);

            _blueprints.AddFeatureToSelection(sorcererBloodlineSelection, bloodline);
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Progressions.NecromancerBloodline, bloodline);
            ReportMissingNecromancerFeatures("new bloodline");
        }

        private void ReportMissingNecromancerFeatures(string phase)
        {
            var expected = new[]
            {
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Necromancer Arcana"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBloodlinePower1, "Withering Ray"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBloodlinePower9, "Grasp of the Dead"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBloodlinePower15, "Incorporeal Form"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBloodlinePower20, "One of Us"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBoneArmor, "Bone Armor"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell, "Bone Spike known spell"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell, "Corpse Explosion known spell"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell, "Eldritch Horror known spell"),
                new KeyValuePair<string, string>(ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell, "Hell on Earth known spell")
            };

            foreach (var item in expected)
            {
                if (_blueprints.Get<BlueprintFeature>(item.Key) == null)
                {
                    _blueprints.ReportError(
                        $"WotrMod_NecromancerClass {phase} is missing {item.Value} ({item.Key}).");
                }
            }
        }
    }
}
