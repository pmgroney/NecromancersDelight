using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal static class BillyBanterRuntimePatch
    {
        private static readonly Dictionary<BlueprintGuid, SequenceRegistration> SequenceTails =
            new Dictionary<BlueprintGuid, SequenceRegistration>();

        public static void RegisterSequence(
            BlueprintBarkBanter banter,
            BlueprintUnit billy,
            BlueprintUnit companion,
            SequenceLine[] lines)
        {
            if (banter == null || billy == null || companion == null || lines == null || lines.Length == 0)
            {
                return;
            }

            var tail = lines.Skip(2).ToArray();
            if (tail.Length == 0)
            {
                return;
            }

            SequenceTails[banter.AssetGuid] = new SequenceRegistration(billy.AssetGuid, companion.AssetGuid, tail);
        }

        public sealed class SequenceLine
        {
            public SequenceLine(bool isBilly, string sourceRole, LocalizedString text)
            {
                IsBilly = isBilly;
                SourceRole = sourceRole;
                Text = text;
            }

            public bool IsBilly { get; }
            public string SourceRole { get; }
            public LocalizedString Text { get; }
        }

        [HarmonyPatch(typeof(BlueprintBarkBanter), nameof(BlueprintBarkBanter.CreatePlayer))]
        private static class CreatePlayerPatch
        {
            [HarmonyPostfix]
            private static void Postfix(BlueprintBarkBanter __instance, BarkBanterPlayer __result)
            {
                try
                {
                    if (__instance == null ||
                        !SequenceTails.TryGetValue(__instance.AssetGuid, out var sequence))
                    {
                        return;
                    }

                    var entries = GetEntries(__result);
                    if (entries == null)
                    {
                        Main.Warning($"Billy banter sequence skipped for {__instance.name}: player entries not found.");
                        return;
                    }

                    var playerUnits = GetPlayerUnits();
                    var billy = FindUnit(sequence.BillyGuid, playerUnits);
                    var companion = FindUnit(sequence.CompanionGuid, playerUnits);
                    if (billy == null || companion == null)
                    {
                        Main.Warning(
                            $"Billy banter sequence skipped for {__instance.name}: Billy found={billy != null}, companion found={companion != null}.");
                        return;
                    }

                    while (entries.Count > 2)
                    {
                        entries.RemoveAt(entries.Count - 1);
                    }

                    foreach (var line in sequence.Lines)
                    {
                        if (line?.Text == null)
                        {
                            Main.Warning($"Billy banter sequence for {__instance.name} skipped a null {line?.SourceRole ?? "line"} text.");
                            continue;
                        }

                        entries.Add(CreateEntry(line.IsBilly ? billy : companion, line.Text));
                    }

                    Main.Log($"Billy banter tail appended for {__instance.name}; entries={entries.Count}.");
                }
                catch (Exception ex)
                {
                    Main.Warning($"Billy banter runtime patch failed for {__instance?.name ?? "<null>"}: {ex}");
                }
            }
        }

        private static IList GetEntries(BarkBanterPlayer player)
        {
            if (player == null)
            {
                return null;
            }

            return AccessTools.Field(typeof(BarkBanterPlayer), "m_Entries")?.GetValue(player) as IList;
        }

        private static object CreateEntry(EntityDataBase speaker, LocalizedString text)
        {
            var entryType = AccessTools.Inner(typeof(BarkBanterPlayer), "Entry");
            return Activator.CreateInstance(entryType, speaker, text);
        }

        private static IEnumerable<UnitEntityData> GetPlayerUnits()
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return Enumerable.Empty<UnitEntityData>();
            }

            var player = Game.Instance.Player;
            return player.PartyAndPets
                .Concat(player.ActiveCompanions)
                .Concat(player.RemoteCompanions)
                .Concat(player.AllCharacters)
                .Where(unit => unit != null)
                .Distinct();
        }

        private static EntityDataBase FindUnit(BlueprintGuid blueprintGuid, IEnumerable<UnitEntityData> units)
        {
            return units.FirstOrDefault(unit => unit?.Descriptor?.Blueprint?.AssetGuid == blueprintGuid);
        }

        private sealed class SequenceRegistration
        {
            public SequenceRegistration(BlueprintGuid billyGuid, BlueprintGuid companionGuid, SequenceLine[] lines)
            {
                BillyGuid = billyGuid;
                CompanionGuid = companionGuid;
                Lines = lines;
            }

            public BlueprintGuid BillyGuid { get; }
            public BlueprintGuid CompanionGuid { get; }
            public SequenceLine[] Lines { get; }
        }
    }
}
