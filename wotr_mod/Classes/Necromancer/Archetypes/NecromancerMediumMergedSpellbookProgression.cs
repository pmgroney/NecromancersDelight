using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Necromancer.Archetypes
{
    internal static class NecromancerMediumMergedSpellbookProgression
    {
        private const int MaxSpellLevel = 10;

        public static void Apply(BlueprintTool blueprints, BlueprintSpellbook spellbook)
        {
            if (blueprints == null || spellbook == null)
            {
                return;
            }

            var spellsPerDay = EnsureExtendedTable(
                blueprints,
                GameBlueprintIds.SpellTables.InquisitorSpellsPerDay,
                ModBlueprintIds.SpellTables.NecromancerMediumMergedSpellsPerDay,
                "WotrMod_NecromancerMediumMergedSpellsPerDay");
            var spellsKnown = EnsureExtendedTable(
                blueprints,
                GameBlueprintIds.SpellTables.InquisitorSpellsKnown,
                ModBlueprintIds.SpellTables.NecromancerMediumMergedSpellsKnown,
                "WotrMod_NecromancerMediumMergedSpellsKnown");

            BlueprintFields.SpellbookSpellsPerDay.SetValue(
                spellbook,
                BlueprintReferenceBase.CreateTyped<BlueprintSpellsTableReference>(spellsPerDay));
            BlueprintFields.SpellbookSpellsKnown.SetValue(
                spellbook,
                BlueprintReferenceBase.CreateTyped<BlueprintSpellsTableReference>(spellsKnown));
        }

        private static BlueprintSpellsTable EnsureExtendedTable(
            BlueprintTool blueprints,
            string sourceGuid,
            string tableGuid,
            string internalName)
        {
            var table = blueprints.Get<BlueprintSpellsTable>(tableGuid);
            if (table == null)
            {
                table = blueprints.CloneBlueprint(
                    blueprints.Require<BlueprintSpellsTable>(sourceGuid, internalName + " donor"),
                    tableGuid,
                    internalName);
                blueprints.AddCachedBlueprint(tableGuid, table);
            }

            ExtendMergedProgression(table);
            return table;
        }

        private static void ExtendMergedProgression(BlueprintSpellsTable table)
        {
            if (table.Levels == null)
            {
                return;
            }

            for (var casterLevel = 0; casterLevel < table.Levels.Length; casterLevel++)
            {
                if (casterLevel < 22)
                {
                    continue;
                }

                if (table.Levels[casterLevel] == null)
                {
                    table.Levels[casterLevel] = new SpellsLevelEntry();
                }

                var source = table.Levels[casterLevel]?.Count ?? Array.Empty<int>();
                var targetSpellLevel = HighestSpellLevelForCasterLevel(casterLevel);
                var requiredLength = Math.Max(source.Length, targetSpellLevel + 1);
                var count = new int[Math.Min(requiredLength, MaxSpellLevel + 1)];
                Array.Copy(source, count, Math.Min(source.Length, count.Length));
                for (var spellLevel = 7; spellLevel < count.Length; spellLevel++)
                {
                    count[spellLevel] = SlotsForMergedSpellLevel(casterLevel, spellLevel);
                }

                table.Levels[casterLevel].Count = count;
            }
        }

        private static int HighestSpellLevelForCasterLevel(int casterLevel)
        {
            if (casterLevel >= 28)
            {
                return 10;
            }

            if (casterLevel >= 26)
            {
                return 9;
            }

            if (casterLevel >= 24)
            {
                return 8;
            }

            if (casterLevel >= 22)
            {
                return 7;
            }

            return 6;
        }

        private static int SlotsForMergedSpellLevel(int casterLevel, int spellLevel)
        {
            var unlockCasterLevel = 8 + (spellLevel * 2);
            if (casterLevel < unlockCasterLevel)
            {
                return 0;
            }

            var slots = casterLevel - unlockCasterLevel + 1;
            return Math.Min(5, slots);
        }
    }
}
