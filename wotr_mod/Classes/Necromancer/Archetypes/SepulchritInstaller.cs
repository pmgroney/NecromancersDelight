using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Necromancer.Archetypes
{
    internal sealed class SepulchritInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;

        public SepulchritInstaller(BlueprintTool blueprints, LocalizationTool localization)
        {
            _blueprints = blueprints;
            _localization = localization;
        }

        public BlueprintArchetype Ensure(
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

            var sepulchritSpellList = EnsureSepulchritSpellList(spellList);
            var sepulchritSpellbook = EnsureSepulchritSpellbook(baseSpellbook, sepulchritSpellList, characterClass);
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

        private BlueprintSpellList EnsureSepulchritSpellList(BlueprintSpellList necromancerSpellList)
        {
            var spellList = _blueprints.Get<BlueprintSpellList>(ModBlueprintIds.SpellLists.Sepulchrit);
            if (spellList == null)
            {
                spellList = _blueprints.CloneBlueprint(
                    necromancerSpellList,
                    ModBlueprintIds.SpellLists.Sepulchrit,
                    "WotrMod_NecromancerSepulchritSpellList");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.SpellLists.Sepulchrit, spellList);
            }

            var wizardSpellList = _blueprints.Require<BlueprintSpellList>(
                GameBlueprintIds.SpellLists.Wizard, "Wizard spell list");
            _blueprints.SetSpellListSpells(
                spellList,
                MergeSpellLists(necromancerSpellList, wizardSpellList));
            return spellList;
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
                    baseSpellbook, ModBlueprintIds.Spellbooks.Sepulchrit, "WotrMod_NecromancerSepulchritSpellbook");
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Spellbooks.Sepulchrit, spellbook);
            }

            var wizardSpellbook = _blueprints.Require<BlueprintSpellbook>(
                GameBlueprintIds.Spellbooks.Wizard, "Wizard spellbook");
            _blueprints.CopySpellbookProgression(spellbook, wizardSpellbook);
            spellbook.CastingAttribute = StatType.Intelligence;
            spellbook.CanCopyScrolls = true;
            spellbook.IsArcane = true;
            _blueprints.SetSpellbookSpellList(spellbook, spellList);
            _blueprints.SetSpellbookCharacterClass(spellbook, characterClass);
            return spellbook;
        }

        private static IEnumerable<KeyValuePair<BlueprintAbility, int>> MergeSpellLists(
            params BlueprintSpellList[] spellLists)
        {
            var spells = new Dictionary<BlueprintGuid, KeyValuePair<BlueprintAbility, int>>();
            foreach (var spellList in spellLists ?? Array.Empty<BlueprintSpellList>())
            {
                foreach (var levelList in spellList?.SpellsByLevel ?? Array.Empty<SpellLevelList>())
                {
                    foreach (var spell in levelList.Spells ?? Enumerable.Empty<BlueprintAbility>())
                    {
                        if (spell == null || spells.ContainsKey(spell.AssetGuid))
                        {
                            continue;
                        }

                        spells.Add(spell.AssetGuid, new KeyValuePair<BlueprintAbility, int>(spell, levelList.SpellLevel));
                    }
                }
            }

            return spells.Values
                .OrderBy(pair => pair.Value)
                .ThenBy(pair => pair.Key.name);
        }
    }
}
