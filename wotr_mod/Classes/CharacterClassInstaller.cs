using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.Designers.Mechanics.Buffs;
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
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityModManagerNet;
using wotr_mod.Content;
using wotr_mod.Content.Localization;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Classes
{
    internal sealed class CharacterClassInstaller : IContentModule
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;
        private readonly List<IClassContentInstaller> _contentInstallers;
        private readonly ClassSpellbookInstaller _spellbookInstaller;
        private readonly ClassProgressionInstaller _progressionInstaller;

        public CharacterClassInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            string modPath)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _icons = new SpellIconLoader(modPath);
            _spellbookInstaller = new ClassSpellbookInstaller(blueprints);
            _progressionInstaller = new ClassProgressionInstaller(blueprints);

            _contentInstallers = new List<IClassContentInstaller>
            {
                new EvokerInstaller(blueprints, localization, logger, _icons)
            };
        }

        public string Name => "Character Classes";

        public void RegisterLocalization()
        {
            ModText.Register(_localization);
            foreach (var installer in _contentInstallers)
            {
                installer.RegisterLocalization();
            }
        }

        public void Install()
        {
            var sorcererClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Sorcerer,
                "Sorcerer class");
            var wizardClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Wizard,
                "Wizard class");
            var sorcererSpellbook = _blueprints.Require<BlueprintSpellbook>(
                GameBlueprintIds.Spellbooks.Sorcerer,
                "Sorcerer spellbook");
            var sorcererProgression = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.Sorcerer,
                "Sorcerer progression");
            var wizardList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Wizard,
                "Wizard spell list");

            BlueprintFeatureSelection evokerBloodlineSelection = null;

            foreach (var definition in CharacterClassRegistry.GetActive())
            {
                try
                {
                    if (definition.UseEvokerBloodlines && evokerBloodlineSelection == null)
                    {
                        evokerBloodlineSelection = EnsureEvokerBloodlineSelection();
                    }

                    var spellList = EnsureSpellList(definition, wizardList);
                    var spellbook = _spellbookInstaller.EnsureSpellbook(definition, sorcererSpellbook, spellList);
                    var characterClass = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);
                    BlueprintFeatureBase bloodlineFeature = null;
                    if (definition.UseEvokerBloodlines)
                    {
                        bloodlineFeature = evokerBloodlineSelection;
                    }

                    var progression = _progressionInstaller.EnsureProgression(
                        definition,
                        sorcererProgression,
                        bloodlineFeature);
                    characterClass = EnsureClass(definition, sorcererClass, spellbook, progression);
                    if (definition.UseNecromancerBloodline)
                    {
                        _blueprints.SetCharacterClassAppearanceFromClass(characterClass, wizardClass);
                    }

                    if (definition.UseUndeadBloodline)
                    {
                        AddUndeadBloodline(progression);
                    }

                    _blueprints.SetCharacterClassHidden(characterClass, false);
                    _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
                    try
                    {
                        _blueprints.SetProgressionClasses(characterClass.Progression, characterClass);
                    }
                    catch (Exception ex)
                    {
                        _blueprints.ReportError($"ERROR during deep-registration of {definition.InternalName}: {ex}");
                    }

                    if (definition.UseNecromancerBloodline)
                    {
                        InstallNecromancerContent(definition, characterClass, spellbook, spellList);
                    }

                    foreach (var installer in _contentInstallers)
                    {
                        if (installer.CanInstall(definition))
                        {
                            try
                            {
                                installer.Install(definition, characterClass, spellbook, spellList);
                            }
                            catch (Exception ex)
                            {
                                _blueprints.ReportError($"ERROR installing class content for {definition.InternalName}: {ex}");
                            }
                        }
                    }

                    ConfigureClassPresentation(definition, characterClass, requireReferencedFeatures: true);

                    try
                    {
                        _blueprints.AddCharacterClassToRoot(characterClass);
                    }
                    catch (Exception ex)
                    {
                        _blueprints.ReportError($"ERROR adding {definition.InternalName} to root: {ex}");
                    }

                    _blueprints.ReportCharacterClassRegistrationErrors(characterClass, definition.InternalName);
                }
                catch (Exception ex)
                {
                    _blueprints.ReportError($"ERROR installing {definition.InternalName}: {ex}");
                    throw;
                }
            }
        }

        private void InstallNecromancerContent(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            ConfigureNecromancerSpellList(spellList);
            EnsureNecromancerBloodline();
            RegisterNecromancerFeatures(characterClass);

            if (characterClass.Progression != null)
            {
                AddNecromancerFeaturesToProgression(characterClass.Progression);
            }

            _blueprints.SetCharacterClassArchetypes(characterClass);
            _blueprints.SetCharacterClassArchetypes(
                characterClass,
                EnsureArchetypes(definition, characterClass, spellbook, spellList));
        }

        private BlueprintSpellList EnsureSpellList(CharacterClassDefinition definition, BlueprintSpellList donor)
        {
            var existing = _blueprints.Get<BlueprintSpellList>(definition.SpellListGuid);
            var clone = existing ?? _blueprints.CloneBlueprint(donor, definition.SpellListGuid, definition.InternalName + "_SpellList");

            if (definition.UseEvokerBloodlines)
            {
                ConfigureEvokerSpellList(clone);
            }
            else
            {
                ConfigureNecromancerSpellList(clone);
            }

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.SpellListGuid, clone);
            }

            return clone;
        }

        private void ConfigureEvokerSpellList(BlueprintSpellList spellList)
        {
            var spellsByLevel = EvokerSpellRegistry.GetAll()
                .Select(definition =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(definition.SpellGuid, definition.DisplayName);
                    return new KeyValuePair<BlueprintAbility, int>(spell, definition.SpellLevel);
                });

            _blueprints.SetSpellListSpells(
                spellList,
                spellsByLevel.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key.name));
        }

        private void ConfigureNecromancerSpellList(BlueprintSpellList spellList, int minimumSpellLevel = 0)
        {
            var spellsByLevel = NecromancerSpellRegistry.GetAll()
                .Where(definition => definition.SpellLevel >= minimumSpellLevel)
                .Select(definition =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(definition.SpellGuid, definition.DisplayName);
                    ApplySelectionRecommendation(spell, definition);
                    return new KeyValuePair<BlueprintAbility, int>(spell, definition.SpellLevel);
                });

            _blueprints.SetSpellListSpells(
                spellList,
                spellsByLevel.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key.name));
        }

        private void ApplySelectionRecommendation(BlueprintScriptableObject blueprint, ClassSpellDefinition definition)
        {
            if (!definition.Recommendation.HasValue)
            {
                return;
            }

            _blueprints.AddSelectionRecommendation(
                blueprint,
                definition.Recommendation.Value,
                $"$PureRecommendation${definition.DisplayName}");
        }

        private BlueprintArchetype[] EnsureArchetypes(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            if (!definition.UseNecromancerBloodline)
            {
                return Array.Empty<BlueprintArchetype>();
            }

            return new[]
            {
                EnsureSepulchritArchetype(characterClass, spellbook, spellList),
                EnsureGravebladeArchetype(characterClass, spellbook, spellList)
            };
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
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
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
            _blueprints.SetArchetypeParentClass(archetype, characterClass);
            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusHigh,
                "Graveblade base attack bonus progression");
            var proficiencies = EnsureGravebladeProficiencies(characterClass);
            var reapingEdge = EnsureGravebladeReapingEdge(characterClass);
            var bonusFeat = EnsureGravebladeBonusFeatSelection();
            var fighterTraining = EnsureGravebladeFighterTraining(characterClass, bonusFeat);
            var armorTraining = EnsureGravebladeArmorTraining();
            var armorMastery = EnsureGravebladeArmorMastery();
            var twoHandedWeaponTraining = EnsureGravebladeTwoHandedWeaponTraining(characterClass);
            var overhandChop = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.TwoHandedFighterOverhandChop,
                "Two-Handed Fighter Overhand Chop");
            var backswing = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.TwoHandedFighterBackswing,
                "Two-Handed Fighter Backswing");
            var piledriver = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.TwoHandedFighterPiledriver,
                "Two-Handed Fighter Piledriver");
            var greaterPowerAttack = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.TwoHandedFighterGreaterPowerAttack,
                "Two-Handed Fighter Greater Power Attack");
            var weaponMastery = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.TwoHandedFighterDevastatingBlow,
                "Two-Handed Fighter weapon mastery feature");
            var firstLevelFighterBonusFeat = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.FighterFeat,
                "Fighter Bonus Feat");
            var necromancerBonusFeat = EnsureNecromancerBonusFeatSelection();
            
            // Register all archetype-specific features with the class
            _blueprints.SetProgressionClasses(proficiencies, characterClass);
            _blueprints.SetProgressionClasses(reapingEdge, characterClass);
            _blueprints.SetProgressionClasses(bonusFeat, characterClass);
            _blueprints.SetProgressionClasses(fighterTraining, characterClass);
            _blueprints.SetProgressionClasses(armorTraining, characterClass);
            _blueprints.SetProgressionClasses(armorMastery, characterClass);
            _blueprints.SetProgressionClasses(twoHandedWeaponTraining, characterClass);

            var gravebladeLevelEntries = new[]
            {
                CreateLevelEntry(1, proficiencies, fighterTraining, reapingEdge, firstLevelFighterBonusFeat),
                CreateLevelEntry(2, bonusFeat),
                CreateLevelEntry(3, armorTraining, reapingEdge, overhandChop),
                CreateLevelEntry(5, reapingEdge, twoHandedWeaponTraining),
                CreateLevelEntry(6, bonusFeat),
                CreateLevelEntry(7, armorTraining, reapingEdge, backswing),
                CreateLevelEntry(9, reapingEdge, twoHandedWeaponTraining),
                CreateLevelEntry(10, bonusFeat),
                CreateLevelEntry(11, armorTraining, reapingEdge, piledriver),
                CreateLevelEntry(13, reapingEdge, twoHandedWeaponTraining),
                CreateLevelEntry(15, armorTraining, reapingEdge, greaterPowerAttack),
                CreateLevelEntry(16, bonusFeat),
                CreateLevelEntry(17, reapingEdge, twoHandedWeaponTraining),
                CreateLevelEntry(19, armorMastery, reapingEdge, weaponMastery)
            };
            _blueprints.SetArchetypeReplaceSpellbook(archetype, gravebladeSpellbook);
            _blueprints.SetArchetypeStartingEquipmentFromClass(
                archetype,
                characterClass,
                _blueprints.Require<BlueprintItem>(
                    GameBlueprintIds.Items.MasterworkScythe,
                    "Masterwork scythe"));
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                gravebladeLevelEntries,
                CreateGravebladeRemoveFeatureEntries(necromancerBonusFeat));
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            _blueprints.SetArchetypeSignatureAbilities(archetype, reapingEdge);
            AddGravebladeFeaturesToProgressionUi(
                characterClass.Progression,
                reapingEdge,
                armorTraining,
                armorMastery,
                twoHandedWeaponTraining,
                overhandChop,
                backswing,
                piledriver,
                greaterPowerAttack,
                weaponMastery);
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        private LevelEntry[] CreateGravebladeRemoveFeatureEntries(BlueprintFeatureBase necromancerBonusFeat)
        {
            var entries = new List<LevelEntry>();
            AddLevelEntryIfAny(
                entries,
                1,
                GetFeatureIfAvailable(GameBlueprintIds.Features.SorcererCantrips, "Sorcerer Cantrips"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerProficiencies, "Necromancer Proficiencies"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"),
                necromancerBonusFeat);
            AddLevelEntryIfAny(
                entries,
                2,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell, "Bone Spike granted spell"));
            AddLevelEntryIfAny(
                entries,
                3,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"));
            AddLevelEntryIfAny(
                entries,
                4,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell, "Corpse Explosion granted spell"));
            AddLevelEntryIfAny(entries, 6, necromancerBonusFeat);
            AddLevelEntryIfAny(
                entries,
                7,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell, "Eldritch Horror granted spell"));
            AddLevelEntryIfAny(
                entries,
                9,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower9, "Grasp of the Dead"));
            AddLevelEntryIfAny(entries, 10, necromancerBonusFeat);
            AddLevelEntryIfAny(
                entries,
                15,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower15, "Incorporeal Form"));
            AddLevelEntryIfAny(entries, 16, necromancerBonusFeat);
            AddLevelEntryIfAny(
                entries,
                19,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell, "Hell on Earth granted spell"));
            AddLevelEntryIfAny(
                entries,
                20,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower20, "One of Us"));

            return entries.ToArray();
        }

        private BlueprintFeature GetFeatureIfAvailable(string guid, string displayName)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null)
            {
                _blueprints.Warning($"Skipping Graveblade remove feature: {displayName} ({guid}) was not available.");
            }

            return feature;
        }

        private static void AddLevelEntryIfAny(
            ICollection<LevelEntry> entries,
            int level,
            params BlueprintFeatureBase[] features)
        {
            var availableFeatures = (features ?? Array.Empty<BlueprintFeatureBase>())
                .Where(feature => feature != null)
                .ToArray();
            if (availableFeatures.Length == 0)
            {
                return;
            }

            entries.Add(CreateLevelEntry(level, availableFeatures));
        }

        private void AddGravebladeFeaturesToProgressionUi(
            BlueprintProgression progression,
            BlueprintFeatureBase reapingEdge,
            BlueprintFeatureBase armorTraining,
            BlueprintFeatureBase armorMastery,
            BlueprintFeatureBase twoHandedWeaponTraining,
            BlueprintFeatureBase overhandChop,
            BlueprintFeatureBase backswing,
            BlueprintFeatureBase piledriver,
            BlueprintFeatureBase greaterPowerAttack,
            BlueprintFeatureBase weaponMastery)
        {
            if (progression == null || reapingEdge == null || armorTraining == null || armorMastery == null)
            {
                return;
            }

            var features = GetNecromancerFeatures();
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

            _blueprints.SetProgressionUiGroups(
                progression,
                new[] { boneArmor },
                new[] { deathsGift },
                new[] { necromancerBonusFeat },
                new[] { armorTraining, armorMastery },
                new[] { twoHandedWeaponTraining, overhandChop, backswing, piledriver, greaterPowerAttack, weaponMastery },
                new[] { reapingEdge },
                new[] { witheringRay, graspOfTheDead, incorporealForm, oneOfUs },
                new[] { boneSpike, corpseExplosion, eldritchHorror, hellOnEarth });
        }

        private BlueprintFeature EnsureGravebladeTwoHandedWeaponTraining(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeTwoHandedWeaponTraining);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.TwoHandedFighterWeaponTraining,
                        "Two-Handed Fighter Weapon Training"),
                    ModBlueprintIds.Features.GravebladeTwoHandedWeaponTraining,
                    "WotrMod_NecromancerGravebladeTwoHandedWeaponTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeTwoHandedWeaponTraining, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 10;

            var components = _blueprints.GetComponents<BlueprintComponent>(feature)
                .Where(component => component.GetType().Name != "PrerequisiteArchetypeLevel")
                .ToArray();
            _blueprints.SetComponents(feature, components);

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureGravebladeArmorMastery()
        {
            var buff = EnsureGravebladeArmorMasteryBuff();
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeArmorMastery);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.FighterArmorMastery,
                        "Fighter Armor Mastery"),
                    ModBlueprintIds.Features.GravebladeArmorMastery,
                    "WotrMod_NecromancerGravebladeArmorMastery");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeArmorMastery, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.GravebladeArmorMasteryName),
                _localization.Text(LocalizationIds.Mod.GravebladeArmorMasteryDescription));

            foreach (var component in _blueprints.GetComponents<BuffOnArmor>(feature))
            {
                SetBuffOnArmorBuff(component, buff);
            }

            return feature;
        }

        private static void SetBuffOnArmorBuff(BuffOnArmor component, BlueprintBuff buff)
        {
            var field = typeof(BuffOnArmor).GetField("m_Buff", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(component, buff.ToReference<BlueprintBuffReference>());
        }

        private BlueprintBuff EnsureGravebladeArmorMasteryBuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.GravebladeArmorMastery);
            if (buff == null)
            {
                buff = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintBuff>(
                        GameBlueprintIds.Buffs.FighterArmorMastery,
                        "Fighter Armor Mastery buff"),
                    ModBlueprintIds.Buffs.GravebladeArmorMastery,
                    "WotrMod_NecromancerGravebladeArmorMasteryBuff");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.GravebladeArmorMastery, buff);
            }

            foreach (var resistance in _blueprints.GetComponents<AddDamageResistancePhysical>(buff))
            {
                resistance.Value = new ContextValue
                {
                    ValueType = ContextValueType.Simple,
                    Value = 10
                };
            }

            return buff;
        }

        private BlueprintFeature EnsureGravebladeArmorTraining()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.GravebladeArmorTraining);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.FighterArmorTraining,
                        "Fighter Armor Training"),
                    ModBlueprintIds.Features.GravebladeArmorTraining,
                    "WotrMod_NecromancerGravebladeArmorTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.GravebladeArmorTraining, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 5;

            return feature;
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

        private BlueprintFeature EnsureGravebladeProficiencies(BlueprintCharacterClass characterClass)
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
            feature.Ranks = 10;
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
            PatchFeatureResource(feature, resource);

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

            resource.LocalizedName = _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName);
            resource.LocalizedDescription = _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription);
            _blueprints.ConfigureAbilityResourceMaxAmount(resource, 0, StatType.Charisma, characterClass, 2);

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

            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Personal;
            ability.ActionType = UnitCommand.CommandType.Swift;
            ability.CanTargetSelf = true;
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = false;
            ability.CanTargetPoint = false;
            ability.NotOffensive = true;
            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeName),
                _localization.Text(LocalizationIds.Mod.GravebladeReapingEdgeDescription));
            var icon = _icons.Load("Icons\\reaping_edge.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(ability, icon);
            }

            var resourceLogic = new AbilityResourceLogic
            {
                name = "$AbilityResourceLogic$GravebladeReapingEdge",
                Amount = 1
            };
            _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
            _blueprints.SetAbilityResourceLogicSpendResource(resourceLogic, true);

            var applyBuff = new ContextActionApplyBuff
            {
                name = "$ContextActionApplyBuff$GravebladeReapingEdge",
                UseDurationSeconds = true,
                DurationSeconds = 60f,
                AsChild = false,
                IgnoreParentContext = true,
                IsNotDispelable = true
            };
            _blueprints.SetApplyBuffActionBuff(applyBuff, buff);

            var runAction = new AbilityEffectRunAction
            {
                name = "$AbilityEffectRunAction$GravebladeReapingEdge",
                Actions = new ActionList { Actions = new GameAction[] { applyBuff } }
            };
            _blueprints.SetComponents(ability, resourceLogic, runAction);
            ability.OnEnable();

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

            ReapingEdgeComponent.Logger = _logger;
            
            var removeBuff = new ContextActionRemoveBuff
            {
                name = "$ContextActionRemoveBuff$GravebladeReapingEdge",
                ToCaster = false,
                RemoveRank = false,
                OnlyFromCaster = false
            };

            var removeBuffField = AccessTools.Field(typeof(ContextActionRemoveBuff), "m_Buff");

            removeBuffField.SetValue(
                removeBuff,
                buff.ToReference<BlueprintBuffReference>());

            var assigned = removeBuffField.GetValue(removeBuff) != null;
            //_logger.Warning($"!!!![ReapingEdge] RemoveBuff m_Buff assigned: {assigned}");
            
            var removeOnHit = new AddInitiatorAttackWithWeaponTrigger
            {
                name = "$AddInitiatorAttackWithWeaponTrigger$GravebladeReapingEdge",
                TriggerBeforeAttack = false,
                OnlyHit = true,
                CheckWeaponRangeType = true,
                RangeType = WeaponRangeType.Melee,
                ActionsOnInitiator = true,
                Action = new ActionList
                {
                    Actions = new GameAction[] { removeBuff }
                }
            };

            _blueprints.SetComponents(
                buff,
                new ReapingEdgeComponent
                {
                    name = "$ReapingEdgeComponent$Graveblade",
                    CharacterClass = characterClass,
                    BrittleBoneBuff = brittleBoneBuff,
                    FatigueBuff = fatigueBuff,
                    ExhaustionBuff = exhaustionBuff
                },
                removeOnHit);

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
                var necromancerBonusFeat = EnsureNecromancerBonusFeatSelection();
                selection = _blueprints.CloneBlueprint(
                    necromancerBonusFeat,
                    ModBlueprintIds.Selections.GravebladeBonusFeat,
                    "WotrMod_NecromancerGravebladeBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.GravebladeBonusFeat, selection);
            }

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatName),
                _localization.Text(LocalizationIds.Mod.GravebladeBonusFeatDescription));

            var necromancerFeatSelection = EnsureNecromancerBonusFeatSelection();
            var choices = _blueprints.GetFeatureSelectionAllFeatures(necromancerFeatSelection)
                .Concat(_blueprints.GetFeatureSelectionAllFeatures(
                    _blueprints.Require<BlueprintFeatureSelection>(
                        GameBlueprintIds.Selections.FighterFeat,
                        "Fighter Bonus Feat")))
                .GroupBy(feature => feature.AssetGuid)
                .Select(group => group.First())
                .ToArray();
            _blueprints.SetFeatureSelectionAllFeatures(selection, choices);
            _blueprints.SetFeatureSelectionFeatures(selection, Array.Empty<BlueprintFeature>());

            return selection;
        }

        private BlueprintSpellList EnsureGravebladeSpellList(BlueprintSpellList baseSpellList)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(ModBlueprintIds.SpellLists.Graveblade);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(
                    baseSpellList,
                    ModBlueprintIds.SpellLists.Graveblade,
                    "WotrMod_NecromancerGravebladeSpellList");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.SpellLists.Graveblade, spellList);
            }

            ConfigureNecromancerSpellList(spellList, minimumSpellLevel: 1);
            return spellList;
        }

        private BlueprintSpellbook EnsureGravebladeSpellbook(
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList,
            BlueprintCharacterClass characterClass)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(ModBlueprintIds.Spellbooks.Graveblade);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(
                    baseSpellbook,
                    ModBlueprintIds.Spellbooks.Graveblade,
                    "WotrMod_NecromancerGravebladeSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Graveblade, spellbook);
            }

            var rangerSpellbook = _blueprints.Require<BlueprintSpellbook>(
                GameBlueprintIds.Spellbooks.Ranger,
                "Ranger spellbook");
            _blueprints.CopySpellbookProgression(spellbook, rangerSpellbook);
            spellbook.CastingAttribute = baseSpellbook.CastingAttribute;
            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
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
                spellbook = _blueprints.CloneBlueprint(
                    baseSpellbook,
                    ModBlueprintIds.Spellbooks.Sepulchrit,
                    "WotrMod_NecromancerSepulchritSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Sepulchrit, spellbook);
            }

            spellbook.CastingAttribute = StatType.Intelligence;
            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
            return spellbook;
        }

        private BlueprintSpellbook EnsureSpellbook(
            CharacterClassDefinition definition,
            BlueprintSpellbook donor,
            BlueprintSpellList spellList)
        {
            var existing = _blueprints.Get<BlueprintSpellbook>(definition.SpellbookGuid);
            if (existing != null)
            {
                return existing;
            }

            var clone = _blueprints.CloneBlueprint(donor, definition.SpellbookGuid, definition.InternalName + "_Spellbook");
            clone.CastingAttribute = definition.CastingStat;
            _blueprints.SetSpellbookSpellList(clone, spellList);
            _blueprints.AddCachedBlueprint(definition.SpellbookGuid, clone);
            return clone;
        }

        private BlueprintProgression EnsureProgression(
            CharacterClassDefinition definition,
            BlueprintProgression donor,
            BlueprintFeatureBase bloodlineFeature)
        {
            var existing = _blueprints.Get<BlueprintProgression>(definition.ProgressionGuid);
            var progression = existing ?? _blueprints.CloneBlueprint(
                donor,
                definition.ProgressionGuid,
                definition.InternalName + "_Progression");

            _blueprints.ClearUnitFactDisplay(progression);

            progression.LevelEntries = donor.LevelEntries?
                .Select(entry =>
                {
                    var newEntry = new LevelEntry { Level = entry.Level };
                    newEntry.SetFeatures(CopyFeatures(definition, entry.Features, bloodlineFeature));
                    return newEntry;
                })
                .ToArray();

            if (definition.UseNecromancerBloodline)
            {
                AddNecromancerFeaturesToProgression(progression);
            }

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(definition.ProgressionGuid, progression);
            }

            return progression;
        }

        private List<BlueprintFeatureBase> CopyFeatures(
            CharacterClassDefinition definition,
            IEnumerable<BlueprintFeatureBase> features,
            BlueprintFeatureBase bloodlineFeature)
        {
            var result = new List<BlueprintFeatureBase>();

            foreach (var feature in features ?? Enumerable.Empty<BlueprintFeatureBase>())
            {
                if (feature != null &&
                    definition.RemoveSorcererBloodline &&
                    feature.AssetGuid == BlueprintGuid.Parse(GameBlueprintIds.Selections.SorcererBloodline))
                {
                    if (bloodlineFeature != null)
                    {
                        result.Add(bloodlineFeature);
                    }

                    continue;
                }

                if (feature != null &&
                    definition.InternalName == "WotrMod_NecromancerClass" &&
                    feature.AssetGuid == BlueprintGuid.Parse(GameBlueprintIds.Features.SorcererProficiencies))
                {
                    // Skip Sorcerer Proficiencies for Necromancer, we add our own later in AddNecromancerFeaturesToProgression
                    continue;
                }

                if (feature != null &&
                    definition.InternalName == "WotrMod_NecromancerClass" &&
                    feature.AssetGuid == BlueprintGuid.Parse(GameBlueprintIds.Selections.SorcererBonusFeat))
                {
                    // Skip Sorcerer's level 1 bloodline bonus feat. Necromancer adds its own chain at 6, 10, and 16.
                    continue;
                }

                if (feature != null &&
                    definition.InternalName == "WotrMod_NecromancerClass" &&
                    feature.AssetGuid == BlueprintGuid.Parse(GameBlueprintIds.Selections.SorcererFeatSelection))
                {
                    // Skip Sorcerer Feat Selection for Necromancer at levels 7, 13, 19
                    continue;
                }

                result.Add(feature);
            }

            return result;
        }

        private BlueprintFeatureSelection EnsureNecromancerBonusFeatSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.NecromancerBonusFeat);
            var selection = existing;
            if (selection == null)
            {
                var sorcererBonusFeat = _blueprints.Require<BlueprintFeatureSelection>(
                    GameBlueprintIds.Selections.SorcererBonusFeat,
                    "Sorcerer Bonus Feat");

                selection = _blueprints.CloneBlueprint(
                    sorcererBonusFeat,
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

        private BlueprintFeature EnsureNecromancerProficiencies()
        {
            var existing = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerProficiencies);
            if (existing != null)
            {
                return existing;
            }

            var sorcererProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.SorcererProficiencies,
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

            AddFeaturesToLevel(progression, 1, necromancerProficiencies, masterOfDeath, witheringRay, boneArmor, necromancerBonusFeat);
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

        private void RegisterNecromancerFeatures(BlueprintCharacterClass characterClass)
        {
            foreach (var feature in GetNecromancerFeatures())
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }
        }

        private BlueprintFeature[] GetNecromancerFeatures()
        {
            return new[]
            {
                EnsureNecromancerProficiencies(),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBloodlineArcana,
                    "Necromancer Arcana"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBloodlinePower1,
                    "Grave Touch"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBloodlinePower3,
                    "Death's Gift"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBloodlinePower9,
                    "Grasp of the Dead"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBloodlinePower15,
                    "Incorporeal Form"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBloodlinePower20,
                    "One of Us"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBoneArmor,
                    "Bone Armor"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell,
                    "Bone Spike"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell,
                    "Corpse Explosion"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell,
                    "Eldritch Horror"),
                _blueprints.Require<BlueprintFeature>(
                    ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell,
                    "Hell on Earth"),
                EnsureNecromancerBonusFeatSelection()
            };
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
                entry.SetFeatures(featuresToAdd);
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

        private BlueprintCharacterClass EnsureClass(
            CharacterClassDefinition definition,
            BlueprintCharacterClass donor,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            var existing = _blueprints.Get<BlueprintCharacterClass>(definition.ClassGuid);
            if (existing != null)
            {
                ConfigureClass(existing, definition, spellbook, progression);
                return existing;
            }

            var clone = _blueprints.CloneBlueprint(donor, definition.ClassGuid, definition.InternalName);
            ConfigureClass(clone, definition, spellbook, progression);

            _blueprints.AddCachedBlueprint(definition.ClassGuid, clone);
            return clone;
        }

        private void ConfigureClass(
            BlueprintCharacterClass characterClass,
            CharacterClassDefinition definition,
            BlueprintSpellbook spellbook,
            BlueprintProgression progression)
        {
            _blueprints.SetCharacterClassSpellbook(characterClass, spellbook);
            _blueprints.SetCharacterClassProgression(characterClass, progression);
            _blueprints.SetCharacterClassDisplay(
                characterClass,
                _localization.Text(definition.DisplayNameKey),
                _localization.Text(definition.DescriptionKey));
            ConfigureClassChassis(definition, characterClass);
            ConfigureClassPresentation(definition, characterClass);
        }

        private void ConfigureClassChassis(CharacterClassDefinition definition, BlueprintCharacterClass characterClass)
        {
            if (definition.Chassis == null)
            {
                return;
            }

            if (definition.Chassis.HitDie.HasValue)
            {
                _blueprints.SetCharacterClassHitDie(characterClass, definition.Chassis.HitDie.Value);
            }

            if (!string.IsNullOrEmpty(definition.Chassis.BaseAttackBonusGuid))
            {
                var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                    definition.Chassis.BaseAttackBonusGuid,
                    definition.InternalName + " base attack bonus progression");
                _blueprints.SetCharacterClassBaseAttackBonus(characterClass, baseAttackBonus);
            }
        }

        private void ConfigureClassPresentation(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            bool requireReferencedFeatures = false)
        {
            var presentation = definition.Presentation;
            if (presentation == null)
            {
                _blueprints.SetCharacterClassSignatureAbilities(characterClass);
                _blueprints.SetCharacterClassDefaultBuild(characterClass, null);
                return;
            }

            _blueprints.SetCharacterClassDifficulty(characterClass, presentation.Difficulty);
            _blueprints.SetCharacterClassAttributeRecommendations(
                characterClass,
                presentation.RecommendedAttributes,
                presentation.NotRecommendedAttributes);

            var signatureAbilities = presentation.SignatureAbilityGuids
                .Where(guid => !string.IsNullOrWhiteSpace(guid))
                .Select(guid => GetPresentationFeature(
                    guid,
                    $"{definition.InternalName} signature ability",
                    requireReferencedFeatures))
                .Where(feature => feature != null)
                .ToArray();
            _blueprints.SetCharacterClassSignatureAbilities(characterClass, signatureAbilities);

            var defaultBuild = string.IsNullOrWhiteSpace(presentation.DefaultBuildGuid)
                ? null
                : GetPresentationFeature(
                    presentation.DefaultBuildGuid,
                    $"{definition.InternalName} default build",
                    requireReferencedFeatures);
            _blueprints.SetCharacterClassDefaultBuild(characterClass, defaultBuild);
        }

        private BlueprintFeature GetPresentationFeature(
            string guid,
            string name,
            bool reportMissing)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null && reportMissing)
            {
                _blueprints.ReportError($"{name} ({guid}) was not available.");
            }

            return feature;
        }

        private BlueprintFeatureSelection EnsureEvokerBloodlineSelection()
        {
            var existing = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.EvokerBloodline);
            if (existing != null)
            {
                return existing;
            }

            var donorSelection = _blueprints.Require<BlueprintFeatureSelection>(
                GameBlueprintIds.Selections.SorcererBloodline,
                "Sorcerer bloodline selection");
            var selection = _blueprints.CloneBlueprint(
                donorSelection,
                ModBlueprintIds.Selections.EvokerBloodline,
                "WotrMod_EvokerBloodlineSelection");

            _blueprints.SetUnitFactDisplay(
                selection,
                _localization.Text(LocalizationIds.Mod.EvokerBloodlineName),
                _localization.Text(LocalizationIds.Mod.EvokerBloodlineDescription));

            var bloodlines = new[]
            {
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ArcaneBloodline, ModBlueprintIds.Progressions.EvokerArcaneBloodline, "WotrMod_EvokerBloodline_Arcane", LocalizationIds.Mod.EvokerArcaneName, LocalizationIds.Mod.EvokerArcaneDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalAirBloodline, ModBlueprintIds.Progressions.EvokerAirBloodline, "WotrMod_EvokerBloodline_Air", LocalizationIds.Mod.EvokerAirName, LocalizationIds.Mod.EvokerAirDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalEarthBloodline, ModBlueprintIds.Progressions.EvokerEarthBloodline, "WotrMod_EvokerBloodline_Earth", LocalizationIds.Mod.EvokerEarthName, LocalizationIds.Mod.EvokerEarthDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalFireBloodline, ModBlueprintIds.Progressions.EvokerFireBloodline, "WotrMod_EvokerBloodline_Fire", LocalizationIds.Mod.EvokerFireName, LocalizationIds.Mod.EvokerFireDescription),
                EnsureEvokerBloodline(GameBlueprintIds.Progressions.ElementalWaterBloodline, ModBlueprintIds.Progressions.EvokerWaterBloodline, "WotrMod_EvokerBloodline_Water", LocalizationIds.Mod.EvokerWaterName, LocalizationIds.Mod.EvokerWaterDescription)
            };

            foreach (var bloodline in bloodlines)
            {
                var evokerClass = _blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Evoker);
                if (evokerClass != null)
                {
                    _blueprints.SetProgressionClasses(bloodline, evokerClass);
                }
            }

            _blueprints.SetFeatureSelectionFeatures(selection, bloodlines);
            _blueprints.SetFeatureSelectionAllFeatures(selection, bloodlines);

            var evokerClassForSelection = _blueprints.Get<BlueprintCharacterClass>(ModBlueprintIds.Classes.Evoker);
            if (evokerClassForSelection != null)
            {
                _blueprints.SetProgressionClasses(selection, evokerClassForSelection);
            }

            _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.EvokerBloodline, selection);
            return selection;
        }

        private BlueprintProgression EnsureEvokerBloodline(
            string donorGuid,
            string newGuid,
            string internalName,
            string displayNameKey,
            string descriptionKey)
        {
            var existing = _blueprints.Get<BlueprintProgression>(newGuid);
            if (existing != null)
            {
                return existing;
            }

            var donor = _blueprints.Require<BlueprintProgression>(donorGuid, internalName + " donor");
            var clone = _blueprints.CloneBlueprint(donor, newGuid, internalName);
            _blueprints.SetUnitFactDisplay(
                clone,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            _blueprints.AddCachedBlueprint(newGuid, clone);
            return clone;
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

            // Define custom features
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

            var visibleFeatures = new BlueprintFeatureBase[]
            {
                arcana,
                power1,
                power3,
                boneSpike,
                corpseExplosion,
                eldritchHorror,
                power9,
                power15,
                hellOnEarth,
                power20
            };

            // Map features to progression
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
                CreateLevelEntry(15, power3, power15),
                CreateLevelEntry(17, boneArmor),
                CreateLevelEntry(19, hellOnEarth),
                CreateLevelEntry(20, power20)
            };
            _blueprints.SetProgressionUiDeterminators(clone, visibleFeatures);
            _blueprints.SetProgressionUiGroups(clone, new[] { visibleFeatures });

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Progressions.NecromancerBloodline, clone);
            }

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
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(donorGuid, donorName),
                    featureGuid,
                    internalName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureDeathsGiftFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadDeathsGift,
                ModBlueprintIds.Features.NecromancerBloodlinePower3,
                "WotrMod_NecromancerBloodlinePower3",
                "Death's Gift",
                LocalizationIds.Mod.NecromancerBloodlinePower3Name,
                LocalizationIds.Mod.NecromancerBloodlinePower3Description,
                characterClass);

            feature.Ranks = 3;
            feature.ReapplyOnLevelUp = true;

            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(feature))
            {
                _blueprints.ConfigureFeatureRankCustomProgression(rank, feature, 5, 10, 20);
            }

            foreach (var resistance in _blueprints.GetComponents<AddDamageResistanceEnergy>(feature))
            {
                resistance.Value = new ContextValue
                {
                    ValueType = ContextValueType.Rank,
                    ValueRank = AbilityRankType.Default
                };
            }

            return feature;
        }

        private BlueprintFeature EnsureMasterOfDeathFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlineArcana);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.RedDragonBloodlineArcana,
                        "Red Dragon Arcana donor"),
                    ModBlueprintIds.Features.NecromancerBloodlineArcana,
                    "WotrMod_NecromancerMasterOfDeath");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBloodlineArcana, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaName),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlineArcanaDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerMasterOfDeathClassCardDescription));
            var icon = _icons.Load("Icons\\necromancer.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            var component = new MasterOfDeathArcanaClassSpells
            {
                name = "$MasterOfDeathArcanaClassSpells$Necromancer",
                Classes = characterClass == null ? Array.Empty<BlueprintCharacterClass>() : new[] { characterClass }
            };
            _blueprints.SetComponents(feature, component);

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintFeature EnsureWitheringRayFeature(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.NecromancerBloodlinePower1);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(
                        GameBlueprintIds.Features.BloodlineElementalEarthElementalRayFeature,
                        "Earth elemental ray feature donor"),
                    ModBlueprintIds.Features.NecromancerBloodlinePower1,
                    "WotrMod_NecromancerWitheringRayFeature");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBloodlinePower1, feature);
            }

            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineElementalElementalRayResource,
                ModBlueprintIds.AbilityResources.WitheringRay,
                "WotrMod_NecromancerWitheringRayResource");
            var ability = EnsureWitheringRayAbility(characterClass, resource);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            if (!_blueprints.GetComponents<AddFacts>(feature).Any())
            {
                var addFacts = new AddFacts { name = "$AddFacts$NecromancerWitheringRay" };
                _blueprints.AddComponent(feature, addFacts);
                _blueprints.SetAddFacts(addFacts, ability);
            }

            PatchFeatureResource(feature, resource);
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlinePower1Name),
                _localization.Text(LocalizationIds.Mod.NecromancerBloodlinePower1Description));

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private BlueprintAbility EnsureWitheringRayAbility(BlueprintCharacterClass characterClass, BlueprintAbilityResource resource)
        {
            var ability = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.WitheringRay);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintAbility>(
                        GameBlueprintIds.Abilities.BloodlineElementalEarthElementalRayAbility,
                        "Earth elemental ray ability donor"),
                    ModBlueprintIds.Abilities.WitheringRay,
                    "WotrMod_NecromancerWitheringRayAbility");
                ability.OnEnable();
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.WitheringRay, ability);
            }

            _blueprints.SetAbilityDisplay(
                ability,
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
                ContextRankProgression.StartPlusDivStep,
                1,
                2,
                characterClass);
            _blueprints.SetContextRankMinimum(rank, 1);

            return ability;
        }

        private BlueprintFeature EnsureGraspOfTheDeadFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadGraspOfTheDead,
                ModBlueprintIds.Features.NecromancerBloodlinePower9,
                "WotrMod_NecromancerBloodlinePower9",
                "Grasp of the Dead",
                LocalizationIds.Mod.NecromancerBloodlinePower9Name,
                LocalizationIds.Mod.NecromancerBloodlinePower9Description,
                characterClass);
            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadGraspOfTheDeadResource,
                ModBlueprintIds.AbilityResources.GraspOfTheDead,
                "WotrMod_NecromancerGraspOfTheDeadResource");
            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadGraspOfTheDeadAbility,
                ModBlueprintIds.Abilities.GraspOfTheDead,
                "WotrMod_NecromancerGraspOfTheDeadAbility",
                "Grasp of the Dead ability",
                LocalizationIds.Mod.NecromancerBloodlinePower9Name,
                LocalizationIds.Mod.NecromancerBloodlinePower9Description,
                characterClass,
                resource);

            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureIncorporealFormFeature(BlueprintCharacterClass characterClass)
        {
            var feature = EnsureNecromancerBloodlineFeature(
                GameBlueprintIds.Features.BloodlineUndeadIncorporealForm,
                ModBlueprintIds.Features.NecromancerBloodlinePower15,
                "WotrMod_NecromancerBloodlinePower15",
                "Incorporeal Form",
                LocalizationIds.Mod.NecromancerBloodlinePower15Name,
                LocalizationIds.Mod.NecromancerBloodlinePower15Description,
                characterClass);
            var resource = EnsureAbilityResource(
                GameBlueprintIds.AbilityResources.BloodlineUndeadIncorporealFormResource,
                ModBlueprintIds.AbilityResources.IncorporealForm,
                "WotrMod_NecromancerIncorporealFormResource");
            var ability = EnsureNecromancerClassLevelAbility(
                GameBlueprintIds.Abilities.BloodlineUndeadIncorporealFormAbility,
                ModBlueprintIds.Abilities.IncorporealForm,
                "WotrMod_NecromancerIncorporealFormAbility",
                "Incorporeal Form ability",
                LocalizationIds.Mod.NecromancerBloodlinePower15Name,
                LocalizationIds.Mod.NecromancerBloodlinePower15Description,
                characterClass,
                resource);

            PatchFeatureAbilityAndResource(feature, ability, resource, characterClass);
            return feature;
        }

        private BlueprintAbilityResource EnsureAbilityResource(string donorGuid, string resourceGuid, string internalName)
        {
            var resource = _blueprints.Get<BlueprintAbilityResource>(resourceGuid);
            if (resource != null)
            {
                return resource;
            }

            resource = _blueprints.CloneBlueprint(
                _blueprints.Require<BlueprintAbilityResource>(donorGuid, internalName + " donor"),
                resourceGuid,
                internalName);
            _blueprints.AddCachedBlueprint(resourceGuid, resource);
            return resource;
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
            var ability = _blueprints.Get<BlueprintAbility>(abilityGuid);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintAbility>(donorGuid, donorName),
                    abilityGuid,
                    internalName);
                ability.OnEnable();
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }

            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            SpellModifierUtility.SetSchool(ability, SpellSchool.Necromancy, _blueprints);
            PatchAbilityResource(ability, resource);
            PatchAbilityClassLevelRanks(ability, characterClass);

            return ability;
        }

        private void PatchFeatureAbilityAndResource(
            BlueprintFeature feature,
            BlueprintAbility ability,
            BlueprintAbilityResource resource,
            BlueprintCharacterClass characterClass)
        {
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }

            if (!_blueprints.GetComponents<AddFacts>(feature).Any())
            {
                var addFacts = new AddFacts { name = "$AddFacts$" + feature.name };
                _blueprints.AddComponent(feature, addFacts);
                _blueprints.SetAddFacts(addFacts, ability);
            }

            PatchFeatureResource(feature, resource);
            _blueprints.BindAbilityComponentsToClass(feature, characterClass);
        }

        private void PatchFeatureResource(BlueprintFeature feature, BlueprintAbilityResource resource)
        {
            foreach (var addResources in _blueprints.GetComponents<AddAbilityResources>(feature))
            {
                _blueprints.SetAddAbilityResourcesResource(addResources, resource);
            }

            if (!_blueprints.GetComponents<AddAbilityResources>(feature).Any())
            {
                var addResources = new AddAbilityResources
                {
                    name = "$AddAbilityResources$" + feature.name,
                    RestoreAmount = true
                };
                _blueprints.AddComponent(feature, addResources);
                _blueprints.SetAddAbilityResourcesResource(addResources, resource);
            }
        }

        private void PatchAbilityResource(BlueprintAbility ability, BlueprintAbilityResource resource)
        {
            foreach (var resourceLogic in _blueprints.GetComponents<AbilityResourceLogic>(ability))
            {
                _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
            }
        }

        private void PatchAbilityClassLevelRanks(BlueprintAbility ability, BlueprintCharacterClass characterClass)
        {
            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(ability))
            {
                _blueprints.ConfigureContextRankConfig(
                    rank,
                    AbilityRankType.Default,
                    ContextRankBaseValueType.ClassLevel,
                    ContextRankProgression.AsIs,
                    characterClass: characterClass);
                _blueprints.SetContextRankMinimum(rank, 1);
            }
        }

        private static void PatchWitheringRayDamage(BlueprintAbility ability)
        {
            SpellModifierUtility.PatchRunActions(ability, action =>
            {
                var damage = action as ContextActionDealDamage;
                if (damage == null)
                {
                    return 0;
                }

                damage.DamageType = SpellModifierUtility.EnergyDamage(DamageEnergyType.Unholy);
                damage.Value = new ContextDiceValue
                {
                    DiceType = DiceType.D6,
                    DiceCountValue = CreateRankValue(),
                    BonusValue = CreateRankValue()
                };
                return 1;
            });
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
                    IsClassFeature = true,
                    Ranks = 5,
                    ReapplyOnLevelUp = false
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.NecromancerBoneArmor, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 5;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorName),
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.NecromancerBoneArmorClassCardDescription));
            var icon = _icons.Load("Icons\\bone_armor.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }
            _blueprints.SetComponents(
                feature,
                new AddStatBonus
                {
                    name = "$AddStatBonus$NecromancerBoneArmor",
                    Stat = StatType.AC,
                    Descriptor = ModifierDescriptor.NaturalArmor,
                    Value = 1
                });

            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

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
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(donorGuid, donorName),
                    featureGuid,
                    internalName);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            var spell = _blueprints.Require<BlueprintAbility>(spellGuid, spellName);
            var addKnownSpell = new AddKnownSpell { name = $"$AddKnownSpell${internalName}" };
            _blueprints.SetAddKnownSpell(addKnownSpell, characterClass, spell, spellLevel);
            _blueprints.SetComponents(feature, addKnownSpell);
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(displayNameKey),
                _localization.Text(descriptionKey));
            if (spell.Icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, spell.Icon);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            if (characterClass != null)
            {
                _blueprints.SetProgressionClasses(feature, characterClass);
            }

            return feature;
        }

        private static ContextValue CreateRankValue()
        {
            return new ContextValue
            {
                ValueType = ContextValueType.Rank,
                ValueRank = AbilityRankType.Default
            };
        }
        
        private static SkillPointsPerCharacterLevel CreateSkillPointBonus(string className)
        {
            return new SkillPointsPerCharacterLevel
            {
                name = className,
                SkillPointsPerLevel = 3
            };
        }
        
        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }

        private void AddUndeadBloodline(BlueprintProgression progression)
        {
            var undeadBloodline = _blueprints.Require<BlueprintProgression>(
                GameBlueprintIds.Progressions.UndeadBloodline,
                "Undead bloodline progression");

            var firstLevelEntry = progression.LevelEntries.FirstOrDefault(e => e.Level == 1);
            if (firstLevelEntry != null)
            {
                var features = firstLevelEntry.Features.ToList();
                features.Add(undeadBloodline);
                firstLevelEntry.SetFeatures(features);
            }
        }
    }
}
