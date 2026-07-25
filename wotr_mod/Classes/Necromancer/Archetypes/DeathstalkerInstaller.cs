using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using UnityModManagerNet;
using wotr_mod.Classes.Evoker;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Necromancer.Archetypes
{
    internal sealed class DeathstalkerInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

        public DeathstalkerInstaller(
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

        public BlueprintArchetype Ensure(
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook baseSpellbook,
            BlueprintSpellList spellList)
        {
            var archetype = _blueprints.Get<BlueprintArchetype>(ModBlueprintIds.Archetypes.Deathstalker);
            if (archetype == null)
            {
                archetype = new BlueprintArchetype
                {
                    name = "WotrMod_NecromancerDeathstalkerArchetype",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Archetypes.Deathstalker)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Archetypes.Deathstalker, archetype);
            }

            var deathstalkerSpellList = EnsureDeathstalkerSpellList(spellList);
            var deathstalkerSpellbook = EnsureDeathstalkerSpellbook(baseSpellbook, deathstalkerSpellList, characterClass);
            _blueprints.SetComponents(archetype);
            _blueprints.SetArchetypeDisplay(
                archetype,
                _localization.Text(LocalizationIds.Mod.DeathstalkerName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerDescription));
            _blueprints.SetArchetypeParentClass(archetype, characterClass);

            var baseAttackBonus = _blueprints.Require<BlueprintStatProgression>(
                GameBlueprintIds.StatProgressions.BaseAttackBonusHigh, "Deathstalker base attack bonus progression");
            var proficiencies = EnsureDeathstalkerProficiencies(characterClass);
            var bonusFeat = EnsureDeathstalkerBonusFeatSelection(archetype);
            var fighterTraining = EnsureDeathstalkerFighterTraining(characterClass, bonusFeat);
            var sneakAttack = EnsureDeathstalkerSneakAttack(characterClass);
            var trapfinding = EnsureDeathstalkerTrapfinding(characterClass);
            var masterStrike = EnsureDeathstalkerMasterStrike(characterClass);
            var finesseTraining = EnsureDeathstalkerFinesseTraining(characterClass);
            var finesseTrainingUpgrade = EnsureDeathstalkerFinesseTrainingUpgrade(finesseTraining);
            var wraithstep1 = EnsureDeathstalkerWraithstepTier(
                ModBlueprintIds.Features.DeathstalkerWraithstep1,
                LocalizationIds.Mod.DeathstalkerWraithstep1Name, LocalizationIds.Mod.DeathstalkerWraithstep1Description,
                characterClass);
            var wraithstep2 = EnsureDeathstalkerWraithstepTier(
                ModBlueprintIds.Features.DeathstalkerWraithstep2,
                LocalizationIds.Mod.DeathstalkerWraithstep2Name, LocalizationIds.Mod.DeathstalkerWraithstep2Description,
                characterClass);
            EnsureDonorComponent(wraithstep2, GameBlueprintIds.Features.DruidWoodlandStride, "Druid Woodland Stride");
            var wraithstep3 = EnsureDeathstalkerWraithstepTier(
                ModBlueprintIds.Features.DeathstalkerWraithstep3,
                LocalizationIds.Mod.DeathstalkerWraithstep3Name, LocalizationIds.Mod.DeathstalkerWraithstep3Description,
                characterClass);
            EnsureDonorComponent(wraithstep3, GameBlueprintIds.Features.VelociraptorAgileMovement, "Velociraptor Agile Movement");
            var wraithstep4 = EnsureDeathstalkerWraithstepTier(
                ModBlueprintIds.Features.DeathstalkerWraithstep4,
                LocalizationIds.Mod.DeathstalkerWraithstep4Name, LocalizationIds.Mod.DeathstalkerWraithstep4Description,
                characterClass);
            var dimensionDoor = EnsureDeathstalkerDimensionDoorAbility();
            EnsureFeatureGrantsFact(wraithstep4, dimensionDoor, "$AddFacts$DeathstalkerTrueWraithstepDimensionDoor");
            var necromancerBonusFeat = new NecromancerInstaller(_blueprints, _localization, _logger, _icons).EnsureNecromancerBonusFeatSelection();

            _blueprints.SetProgressionClasses(proficiencies, characterClass);
            _blueprints.SetProgressionClassesShallow(bonusFeat, characterClass);
            _blueprints.SetProgressionClasses(fighterTraining, characterClass);
            _blueprints.SetProgressionClasses(sneakAttack, characterClass);
            _blueprints.SetProgressionClasses(trapfinding, characterClass);

            var deathstalkerLevelEntries = new[]
            {
                CreateLevelEntry(1,  proficiencies, fighterTraining, bonusFeat, trapfinding),
                CreateLevelEntry(2,  bonusFeat, finesseTraining),
                CreateLevelEntry(3,  sneakAttack, wraithstep1),
                CreateLevelEntry(5,  finesseTrainingUpgrade),
                CreateLevelEntry(6,  bonusFeat, sneakAttack),
                CreateLevelEntry(7,  wraithstep2),
                CreateLevelEntry(8,  finesseTrainingUpgrade),
                CreateLevelEntry(9,  sneakAttack),
                CreateLevelEntry(10, bonusFeat),
                CreateLevelEntry(11, wraithstep3),
                CreateLevelEntry(12, sneakAttack),
                CreateLevelEntry(13, finesseTrainingUpgrade),
                CreateLevelEntry(14, bonusFeat),
                CreateLevelEntry(15, sneakAttack, wraithstep4),
                CreateLevelEntry(18, bonusFeat, sneakAttack),
                CreateLevelEntry(20, masterStrike)
            };

            _blueprints.SetArchetypeReplaceSpellbook(archetype, deathstalkerSpellbook);
            _blueprints.SetArchetypeStartingEquipment(
                archetype,
                true,
                _blueprints.GetCharacterClassStartingGold(characterClass),
                GetDeathstalkerStartingEquipment(characterClass));
            _blueprints.SetArchetypeFeatureChanges(
                archetype,
                deathstalkerLevelEntries,
                CreateDeathstalkerRemoveFeatureEntries(necromancerBonusFeat));
            _blueprints.SetArchetypeBaseAttackBonus(archetype, baseAttackBonus);
            AddDeathstalkerFeaturesToProgressionUi(
                characterClass.Progression,
                sneakAttack, trapfinding, masterStrike);
            _blueprints.AddProgressionUiGroup(characterClass.Progression, wraithstep1, wraithstep2, wraithstep3, wraithstep4);
            _blueprints.SetArchetypeBuildChanging(archetype, true);

            return archetype;
        }

        // Two-Weapon Fighting is a shared vanilla feature used by every class in the
        // game, and already carries vanilla recommendation components (e.g.
        // RecommendationWeaponSubcategoryFocus, which returns Bad for anyone who hasn't
        // already taken Weapon Focus in a matching weapon — true for a fresh Deathstalker).
        // The game's recommendation aggregator (LevelUpRecommendationEx) lets a single Bad
        // permanently veto any later Good from another component, so simply adding our
        // recommendation to the shared blueprint would get silently overridden. Instead,
        // clone the feature for Deathstalker's own bonus feat list, strip the vanilla
        // recommendation components from the clone (leaving prerequisites/effects intact),
        // and attach only our archetype-aware recommendation there.
        private BlueprintFeature EnsureDeathstalkerTwoWeaponFighting(
            BlueprintArchetype archetype,
            BlueprintFeatureSelection bonusFeat)
        {
            var source = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.TwoWeaponFighting,
                "Two-Weapon Fighting");
            var guid = EvokerInstaller.DeterministicGuid("WotrMod_DeathstalkerTwoWeaponFighting");
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(source, guid, "WotrMod_DeathstalkerTwoWeaponFighting");
                _blueprints.AddCachedBlueprint(guid, feature);
            }

            _blueprints.RemoveComponents<LevelUpRecommendationComponent>(feature);
            var recommendation = _blueprints.EnsureComponent(
                feature,
                () => new ArchetypeFeatureRecommendation
                {
                    name = "$ArchetypeFeatureRecommendation$DeathstalkerTwoWeaponFighting"
                });
            recommendation.AddArchetype(archetype);
            recommendation.AddSelection(bonusFeat);

            return feature;
        }

        private BlueprintItem[] GetDeathstalkerStartingEquipment(BlueprintCharacterClass characterClass)
        {
            var cureLightWoundsPotion = _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.PotionOfCureLightWounds,
                "Potion of Cure Light Wounds");
            var shortsword = _blueprints.Require<BlueprintItem>(
                GameBlueprintIds.Items.Shortsword,
                "Shortsword");

            var startingEquipment = _blueprints.GetCharacterClassStartingEquipment(characterClass)
                .Where(item => item != null)
                .Concat(new[]
                {
                    cureLightWoundsPotion,
                    cureLightWoundsPotion,
                    shortsword,
                    shortsword
                })
                .ToArray();
            return startingEquipment;
        }

        // ─── Remove feature entries ───────────────────────────────────────────

        private LevelEntry[] CreateDeathstalkerRemoveFeatureEntries(BlueprintFeatureBase necromancerBonusFeat)
        {
            var entries = new List<LevelEntry>();
            AddLevelEntryIfAny(entries, 1,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerProficiencies, "Necromancer Proficiencies"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlineArcana, "Master of Death"),
                necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 2,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBoneSpikeKnownSpell, "Bone Spike granted spell"),
                necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 3,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"));
            AddLevelEntryIfAny(entries, 4,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerCorpseExplosionKnownSpell, "Corpse Explosion granted spell"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerStygianPrecision, "Stygian Precision"));
            AddLevelEntryIfAny(entries, 6, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 7,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerEldritchHorrorKnownSpell, "Eldritch Horror granted spell"));
            AddLevelEntryIfAny(entries, 8,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerStygianPrecision, "Stygian Precision"));
            AddLevelEntryIfAny(entries, 9,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower9, "Grasp of the Dead"));
            AddLevelEntryIfAny(entries, 10, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 11,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerHarvestTheFallenKnownSpell, "Harvest the Fallen granted spell"));
            AddLevelEntryIfAny(entries, 12,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerStygianPrecision, "Stygian Precision"));
            AddLevelEntryIfAny(entries, 14, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 15,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower3, "Death's Gift"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower15, "Incorporeal Form"));
            AddLevelEntryIfAny(entries, 16,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerStygianPrecision, "Stygian Precision"));
            AddLevelEntryIfAny(entries, 18, necromancerBonusFeat);
            AddLevelEntryIfAny(entries, 19,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerHellOnEarthKnownSpell, "Hell on Earth granted spell"));
            AddLevelEntryIfAny(entries, 20,
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerBloodlinePower20, "One of Us"),
                GetFeatureIfAvailable(ModBlueprintIds.Features.NecromancerReapersJudgement, "Reaper's Judgement"));
            return entries.ToArray();
        }

        private BlueprintFeature GetFeatureIfAvailable(string guid, string displayName)
        {
            var feature = _blueprints.Get<BlueprintFeature>(guid);
            if (feature == null)
                _blueprints.Warning($"Skipping Deathstalker remove feature: {displayName} ({guid}) was not available.");
            return feature;
        }

        private static void AddLevelEntryIfAny(ICollection<LevelEntry> entries, int level, params BlueprintFeatureBase[] features)
        {
            var available = (features ?? Array.Empty<BlueprintFeatureBase>()).Where(f => f != null).ToArray();
            if (available.Length == 0) return;
            entries.Add(CreateLevelEntry(level, available));
        }

        // ─── Progression UI ───────────────────────────────────────────────────

        private void AddDeathstalkerFeaturesToProgressionUi(
            BlueprintProgression progression,
            BlueprintFeatureBase sneakAttack, BlueprintFeatureBase trapfinding, BlueprintFeatureBase masterStrike)
        {
            if (progression == null || sneakAttack == null) return;

            // Appends (rather than replacing) since the base Necromancer class and other
            // archetypes already populated progression.UIGroups on this shared progression.
            _blueprints.AddProgressionUiGroup(progression, sneakAttack, trapfinding, masterStrike);
        }

        // ─── Deathstalker-specific features ──────────────────────────────────

        private BlueprintFeature EnsureDeathstalkerSneakAttack(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DeathstalkerSneakAttack);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.RogueSneakAttack, "Rogue/Slayer Sneak Attack"),
                    ModBlueprintIds.Features.DeathstalkerSneakAttack,
                    "WotrMod_NecromancerDeathstalkerSneakAttack");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DeathstalkerSneakAttack, feature);
            }
            feature.IsClassFeature = true;
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureDeathstalkerTrapfinding(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DeathstalkerTrapfinding);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.RogueTrapfinding, "Rogue Trapfinding"),
                    ModBlueprintIds.Features.DeathstalkerTrapfinding,
                    "WotrMod_NecromancerDeathstalkerTrapfinding");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DeathstalkerTrapfinding, feature);
            }
            feature.IsClassFeature = true;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.DeathstalkerTrapfindingName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerTrapfindingDescription));
            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(feature))
            {
                _blueprints.ConfigureContextRankConfig(
                    rank,
                    AbilityRankType.Default,
                    ContextRankBaseValueType.ClassLevel,
                    ContextRankProgression.Div2,
                    characterClass: characterClass);
                _blueprints.SetContextRankMinimum(rank, 1);
            }
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintFeature EnsureDeathstalkerMasterStrike(BlueprintCharacterClass characterClass)
        {
            var buff = EnsureDeathstalkerMasterStrikeBuff(characterClass);
            var ability = EnsureDeathstalkerMasterStrikeToggleAbility(buff);
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DeathstalkerMasterStrike);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.RogueMasterStrike, "Rogue Master Strike"),
                    ModBlueprintIds.Features.DeathstalkerMasterStrike,
                    "WotrMod_NecromancerDeathstalkerMasterStrike");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DeathstalkerMasterStrike, feature);
            }
            feature.IsClassFeature = true;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.DeathstalkerMasterStrikeName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerMasterStrikeDescription));
            var components = _blueprints.GetComponents<BlueprintComponent>(feature)
                .Where(c => c.GetType().Name != "PrerequisiteFeature")
                .ToArray();
            _blueprints.SetComponents(feature, components);
            foreach (var addFacts in _blueprints.GetComponents<AddFacts>(feature))
            {
                _blueprints.SetAddFacts(addFacts, ability);
            }
            if (!_blueprints.GetComponents<AddFacts>(feature).Any())
            {
                var af = new AddFacts { name = "$AddFacts$DeathstalkerMasterStrike" };
                _blueprints.AddComponent(feature, af);
                _blueprints.SetAddFacts(af, ability);
            }
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private BlueprintActivatableAbility EnsureDeathstalkerMasterStrikeToggleAbility(BlueprintBuff buff)
        {
            var ability = _blueprints.Get<BlueprintActivatableAbility>(ModBlueprintIds.Abilities.DeathstalkerMasterStrikeToggle);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintActivatableAbility>(
                        GameBlueprintIds.Abilities.RogueMasterStrikeToggle, "Rogue Master Strike toggle ability"),
                    ModBlueprintIds.Abilities.DeathstalkerMasterStrikeToggle,
                    "WotrMod_NecromancerDeathstalkerMasterStrikeToggle");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.DeathstalkerMasterStrikeToggle, ability);
            }
            _blueprints.SetUnitFactDisplay(ability,
                _localization.Text(LocalizationIds.Mod.DeathstalkerMasterStrikeName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerMasterStrikeDescription));
            EvokerInstaller.ReplaceBuffReferences(ability, GameBlueprintIds.Buffs.RogueMasterStrikeBuff, buff);
            return ability;
        }

        private BlueprintBuff EnsureDeathstalkerMasterStrikeBuff(BlueprintCharacterClass characterClass)
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.DeathstalkerMasterStrikeBuff);
            if (buff == null)
            {
                buff = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintBuff>(GameBlueprintIds.Buffs.RogueMasterStrikeBuff, "Rogue Master Strike buff"),
                    ModBlueprintIds.Buffs.DeathstalkerMasterStrikeBuff,
                    "WotrMod_NecromancerDeathstalkerMasterStrikeBuff");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.DeathstalkerMasterStrikeBuff, buff);
            }
            _blueprints.SetUnitFactDisplay(buff,
                _localization.Text(LocalizationIds.Mod.DeathstalkerMasterStrikeName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerMasterStrikeDescription));
            foreach (var component in _blueprints.GetComponents<BlueprintComponent>(buff)
                         .Where(c => c.GetType().Name == "ContextCalculateAbilityParamsBasedOnClass"))
            {
                AccessTools.Field(component.GetType(), "m_CharacterClass")?.SetValue(
                    component,
                    BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
            }
            return buff;
        }

        private BlueprintFeatureSelection EnsureDeathstalkerFinesseTraining(BlueprintCharacterClass characterClass)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.DeathstalkerFinesseTraining);
            if (selection == null)
            {
                selection = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeatureSelection>(GameBlueprintIds.Features.RogueFinesseTrainingSelection, "Rogue Finesse Training"),
                    ModBlueprintIds.Selections.DeathstalkerFinesseTraining,
                    "WotrMod_NecromancerDeathstalkerFinesseTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.DeathstalkerFinesseTraining, selection);
            }
            selection.IsClassFeature = true;
            selection.Ranks = 4;
            _blueprints.SetUnitFactDisplay(selection,
                _localization.Text(LocalizationIds.Mod.DeathstalkerFinesseTrainingName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerFinesseTrainingDescription));
            if (characterClass != null) _blueprints.SetProgressionClassesShallow(selection, characterClass);
            return selection;
        }

        private BlueprintFeature EnsureDeathstalkerFinesseTrainingUpgrade(BlueprintFeatureSelection finesseTraining)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Selections.DeathstalkerFinesseTrainingUpgrade);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerDeathstalkerFinesseTrainingUpgrade",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Selections.DeathstalkerFinesseTrainingUpgrade),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.DeathstalkerFinesseTrainingUpgrade, feature);
            }
            var addFacts = new AddFacts { name = "$AddFacts$DeathstalkerFinesseTrainingUpgrade" };
            _blueprints.SetAddFacts(addFacts, finesseTraining);
            _blueprints.SetComponents(feature, addFacts);
            return feature;
        }

        private BlueprintFeature EnsureDeathstalkerWraithstepTier(
            string featureGuid,
            string nameKey,
            string descriptionKey,
            BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerDeathstalkerWraithstep" + featureGuid,
                    AssetGuid = BlueprintGuid.Parse(featureGuid),
                    IsClassFeature = true,
                    Ranks = 1
                };
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature, _localization.Text(nameKey), _localization.Text(descriptionKey));
            var icon = _icons.Load("Icons\\wraith_step.png");
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(feature, icon);
            }

            var speedBonus = new AddStatBonus
            {
                name = "$AddStatBonus$" + featureGuid,
                Stat = StatType.Speed,
                Value = 10,
                Descriptor = ModifierDescriptor.UntypedStackable
            };
            _blueprints.SetComponents(feature, speedBonus);
            if (characterClass != null) _blueprints.SetProgressionClasses(feature, characterClass);
            return feature;
        }

        private void EnsureDonorComponent(BlueprintFeature feature, string donorGuid, string donorName)
        {
            var donor = _blueprints.Require<BlueprintFeature>(donorGuid, donorName);
            var donorComponent = _blueprints.GetComponents<BlueprintComponent>(donor).First();
            var donorType = donorComponent.GetType();
            if (_blueprints.GetComponents<BlueprintComponent>(feature).Any(c => c.GetType() == donorType))
            {
                return;
            }

            _blueprints.AddComponent(feature, _blueprints.CloneComponent(donorComponent));
        }

        private BlueprintAbility EnsureDeathstalkerDimensionDoorAbility()
        {
            var ability = _blueprints.Get<BlueprintAbility>(ModBlueprintIds.Abilities.DeathstalkerDimensionDoor);
            if (ability == null)
            {
                ability = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintAbility>(GameBlueprintIds.Abilities.DarkLurkerDimensionDoor, "Dark Lurker Dimension Door"),
                    ModBlueprintIds.Abilities.DeathstalkerDimensionDoor,
                    "WotrMod_NecromancerDeathstalkerDimensionDoor");
                ability.OnEnable();
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Abilities.DeathstalkerDimensionDoor, ability);
            }
            _blueprints.SetAbilityDisplay(ability,
                _localization.Text(LocalizationIds.Mod.DeathstalkerDimensionDoorName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerDimensionDoorDescription));
            return ability;
        }

        private BlueprintFeature EnsureDeathstalkerFighterTraining(
            BlueprintCharacterClass characterClass, BlueprintFeatureSelection bonusFeatSelection)
        {
            var arcaneArmorProficiency = EnsureDeathstalkerArcaneArmorProficiency();
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DeathstalkerFighterTraining);
            if (feature == null)
            {
                feature = _blueprints.CloneBlueprint(
                    _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.MagusFighterTraining, "Magus Fighter Training"),
                    ModBlueprintIds.Features.DeathstalkerFighterTraining, "WotrMod_NecromancerDeathstalkerTraining");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DeathstalkerFighterTraining, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.DeathstalkerFighterTrainingName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerFighterTrainingDescription));
            _blueprints.ConfigureClassLevelsForPrerequisites(
                feature,
                _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Fighter, "Fighter class"),
                characterClass, bonusFeatSelection, 1.0, 0);
            _blueprints.AddClassLevelsForPrerequisites(
                feature,
                _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Rogue, "Rogue class"),
                characterClass, bonusFeatSelection, 1.0, 0,
                "$ClassLevelsForPrerequisites$DeathstalkerRogueLevels");
            EnsureFeatureGrantsFact(feature, arcaneArmorProficiency, "$AddFacts$DeathstalkerArcaneArmorProficiency");
            return feature;
        }

        private BlueprintFeature EnsureDeathstalkerArcaneArmorProficiency()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DeathstalkerArcaneArmorProficiency);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerDeathstalkerArcaneArmorProficiency",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.DeathstalkerArcaneArmorProficiency),
                    IsClassFeature = true,
                    Ranks = 1,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DeathstalkerArcaneArmorProficiency, feature);
            }

            feature.IsClassFeature = true;
            feature.Ranks = 1;
            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.DeathstalkerFighterTrainingName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerFighterTrainingDescription));

            var bloodragerProficiencies = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.BloodragerProficiencies, "Bloodrager Proficiencies");
            var sourceComponent = _blueprints.GetComponents<BlueprintComponent>(bloodragerProficiencies)
                .FirstOrDefault(candidate => candidate.GetType().Name == "ArcaneArmorProficiency");
            if (sourceComponent == null)
            {
                _logger.Error("Bloodrager Proficiencies has no ArcaneArmorProficiency component to clone.");
                _blueprints.SetComponents(feature);
                return feature;
            }

            var clonedComponent = _blueprints.CloneComponent(sourceComponent);
            clonedComponent.name = "$ArcaneArmorProficiency$DeathstalkerArmor";
            if (clonedComponent is ArcaneArmorProficiency armorProficiency)
            {
                armorProficiency.Armor = new[]
                {
                    ArmorProficiencyGroup.Light,
                    ArmorProficiencyGroup.Medium,
                    ArmorProficiencyGroup.Heavy
                };
            }
            _blueprints.SetComponents(feature, clonedComponent);
            return feature;
        }

        private void EnsureFeatureGrantsFact(BlueprintFeature feature, BlueprintUnitFact fact, string componentName)
        {
            var existing = _blueprints.GetComponents<AddFacts>(feature)
                .FirstOrDefault(component => component.name == componentName);
            if (existing != null)
            {
                _blueprints.SetAddFacts(existing, fact);
                return;
            }

            var addFacts = new AddFacts { name = componentName };
            _blueprints.SetAddFacts(addFacts, fact);
            _blueprints.AddComponent(feature, addFacts);
        }

        private BlueprintFeature EnsureDeathstalkerProficiencies(BlueprintCharacterClass characterClass)
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.DeathstalkerProficiencies);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_NecromancerDeathstalkerProficiencies",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.DeathstalkerProficiencies),
                    IsClassFeature = true, Ranks = 1
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.DeathstalkerProficiencies, feature);
            }
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            _blueprints.SetUnitFactDisplay(feature,
                _localization.Text(LocalizationIds.Mod.DeathstalkerProficienciesName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerProficienciesDescription));
            var addFacts = new AddFacts { name = "$AddFacts$DeathstalkerProficiencies" };
            _blueprints.SetAddFacts(addFacts,
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.ArmorProficiencyLight, "Light Armor Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.SimpleWeaponProficiency, "Simple Weapon Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.MartialWeaponProficiency, "Martial Weapon Proficiency"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.LightningReflexes, "Lightning Reflexes"),
                _blueprints.Require<BlueprintFeature>(GameBlueprintIds.Features.WeaponFinesse, "Weapon Finesse"));
            var athleticsClassSkill = new AddClassSkill
            {
                name = "$AddClassSkill$DeathstalkerAthletics",
                Skill = StatType.SkillAthletics
            };
            var mobilityClassSkill = new AddClassSkill
            {
                name = "$AddClassSkill$DeathstalkerMobility",
                Skill = StatType.SkillMobility
            };
            var thieveryClassSkill = new AddClassSkill
            {
                name = "$AddClassSkill$DeathstalkerThievery",
                Skill = StatType.SkillThievery
            };
            var stealthClassSkill = new AddClassSkill
            {
                name = "$AddClassSkill$DeathstalkerStealth",
                Skill = StatType.SkillStealth
            };
            var perceptionClassSkill = new AddClassSkill
            {
                name = "$AddClassSkill$DeathstalkerPerception",
                Skill = StatType.SkillPerception
            };
            _blueprints.SetComponents(feature, addFacts, athleticsClassSkill, mobilityClassSkill, thieveryClassSkill, stealthClassSkill, perceptionClassSkill);
            return feature;
        }

        private BlueprintFeatureSelection EnsureDeathstalkerBonusFeatSelection(BlueprintArchetype archetype)
        {
            var selection = _blueprints.Get<BlueprintFeatureSelection>(ModBlueprintIds.Selections.DeathstalkerBonusFeat);
            var necromancerBonusFeat = new NecromancerInstaller(_blueprints, _localization, _logger, _icons).EnsureNecromancerBonusFeatSelection();
            if (selection == null)
            {
                selection = _blueprints.CloneBlueprint(
                    necromancerBonusFeat,
                    ModBlueprintIds.Selections.DeathstalkerBonusFeat,
                    "WotrMod_NecromancerDeathstalkerBonusFeatSelection");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Selections.DeathstalkerBonusFeat, selection);
            }
            _blueprints.SetUnitFactDisplay(selection,
                _localization.Text(LocalizationIds.Mod.DeathstalkerBonusFeatName),
                _localization.Text(LocalizationIds.Mod.DeathstalkerBonusFeatDescription));
            var twoWeaponFighting = EnsureDeathstalkerTwoWeaponFighting(archetype, selection);
            var choices = _blueprints.GetFeatureSelectionAllFeatures(necromancerBonusFeat)
                .Concat(_blueprints.GetFeatureSelectionAllFeatures(
                    _blueprints.Require<BlueprintFeatureSelection>(GameBlueprintIds.Selections.FighterFeat, "Fighter Bonus Feat")))
                .Concat(_blueprints.GetFeatureSelectionAllFeatures(
                    _blueprints.Require<BlueprintFeatureSelection>(GameBlueprintIds.Selections.RogueTalent, "Rogue Talent")))
                .Select(f => f?.AssetGuid.ToString() == GameBlueprintIds.Features.TwoWeaponFighting ? twoWeaponFighting : f)
                .GroupBy(f => f.AssetGuid)
                .Select(g => g.First())
                .ToArray();
            _blueprints.SetFeatureSelectionAllFeatures(selection, choices);
            _blueprints.SetFeatureSelectionFeatures(selection, Array.Empty<BlueprintFeature>());
            selection.IsClassFeature = true;
            selection.Ranks = 1;
            return selection;
        }

        private BlueprintSpellList EnsureDeathstalkerSpellList(BlueprintSpellList baseSpellList)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(ModBlueprintIds.SpellLists.Deathstalker);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(
                    baseSpellList, ModBlueprintIds.SpellLists.Deathstalker, "WotrMod_NecromancerDeathstalkerSpellList");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.SpellLists.Deathstalker, spellList);
            }
            // Deathstalker uses necromancer spells starting at level 1
            var spellsByLevel = NecromancerSpellRegistry.GetAll()
                .Where(d => d.SpellLevel >= 1)
                .Select(d =>
                {
                    var spell = _blueprints.Require<BlueprintAbility>(d.SpellGuid, d.DisplayName);
                    return new KeyValuePair<BlueprintAbility, int>(spell, d.SpellLevel);
                });
            _blueprints.SetSpellListSpells(spellList, spellsByLevel.OrderBy(p => p.Value).ThenBy(p => p.Key.name));
            return spellList;
        }

        private BlueprintSpellbook EnsureDeathstalkerSpellbook(
            BlueprintSpellbook baseSpellbook, BlueprintSpellList spellList, BlueprintCharacterClass characterClass)
        {
            var spellbook = _blueprints.Get<BlueprintSpellbook>(ModBlueprintIds.Spellbooks.Deathstalker);
            if (spellbook == null)
            {
                spellbook = _blueprints.CloneBlueprint(
                    baseSpellbook, ModBlueprintIds.Spellbooks.Deathstalker, "WotrMod_NecromancerDeathstalkerSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Deathstalker, spellbook);
            }
            var inquisitorSpellbook = _blueprints.Require<BlueprintSpellbook>(GameBlueprintIds.Spellbooks.Inquisitor, "Inquisitor spellbook");
            _blueprints.CopySpellbookProgression(spellbook, inquisitorSpellbook);
            spellbook.CastingAttribute = baseSpellbook.CastingAttribute;
            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
            return spellbook;
        }

        private static LevelEntry CreateLevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry { Level = level };
            entry.SetFeatures(features);
            return entry;
        }
    }
}
