using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
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

            spellbook.SpellList = spellList;
            spellbook.CastingStat = StatType.Intelligence;
            spellbook.CharacterClass = characterClass;
            _blueprints.SetUnitFactDisplay(
                spellbook,
                _localization.Text(LocalizationIds.Mod.SepulchritName),
                _localization.Text(LocalizationIds.Mod.SepulchritDescription));

            return spellbook;
        }
    }
}
