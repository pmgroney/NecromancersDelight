using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using Kingmaker.Sound;
using Kingmaker.Visual.Sound;
using UnityEngine;
using wotr_mod.Infrastructure;
using wotr_mod.Patches;

namespace wotr_mod.Content
{
    internal sealed partial class CompanionInstaller
    {
        private void LoadBillyVoiceBank()
        {
            try
            {
                var audioPath = Path.Combine(_modPath, "Audio");
                AkSoundEngine.AddBasePath(audioPath);
                var result = AkSoundEngine.LoadBank("CMP_Billy_GVR_ENG", out _);
                if (result == AKRESULT.AK_Success || result == AKRESULT.AK_BankAlreadyLoaded)
                    Main.Log($"Billy voice bank loaded ({result}).");
                else
                    Main.Warning($"Billy voice bank load returned {result} — audio will be silent.");

                LoadVoicePackage("Wrath_Main_VO_Dialogues_PartyBanter");
                LoadVoicePackage("Wrath_DLC6_VO_Dialogues");
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy voice bank load failed: {ex.Message}");
            }
        }

        private static void LoadVoicePackage(string packageName)
        {
            try
            {
                SoundPackagesManager.LoadPackage(packageName);
                Main.Log($"Voice package load requested: {packageName}.");
            }
            catch (Exception ex)
            {
                Main.Warning($"Voice package load failed for {packageName}: {ex.Message}");
            }
        }

        private void EnsureBillyBanterReplacements(BlueprintUnit billy)
        {
            if (billy == null)
            {
                return;
            }

            var root = _blueprints.Require<Kingmaker.Blueprints.Root.BlueprintRoot>(
                GameBlueprintIds.Root.BlueprintRoot,
                "BlueprintRoot");
            if (root.Camping == null)
            {
                _blueprints.Warning("Billy banters skipped: BlueprintRoot.Camping was not available.");
                return;
            }

            var replacements = new List<BlueprintBarkBanter>();
            foreach (var entry in CanonicalBillyBanterReplacements)
            {
                var replacement = EnsureBillyBanterReplacement(entry, billy);
                if (replacement != null)
                {
                    replacements.Add(replacement);
                }
            }

            var originalRefs = (BlueprintBarkBanterReference[])GetField(root.Camping, "m_AllBanters")
                               ?? Array.Empty<BlueprintBarkBanterReference>();

            var merged = originalRefs.ToList();

            foreach (var replacement in replacements)
            {
                if (merged.Any(reference => reference?.Get()?.AssetGuid == replacement.AssetGuid))
                {
                    continue;
                }

                merged.Add(BlueprintReferenceBase.CreateTyped<BlueprintBarkBanterReference>(replacement));
            }

            SetField(root.Camping, "m_AllBanters", merged.ToArray());
            Main.Log($"Installed {replacements.Count} Billy banter replacements.");
        }

        private BlueprintBarkBanter EnsureBillyBanterReplacement(BillyBanterReplacement entry, BlueprintUnit billy)
        {
            var source = _blueprints.Require<BlueprintBarkBanter>(entry.SourceGuid, entry.SourceName);
            var companion = _blueprints.Require<BlueprintUnit>(entry.CompanionGuid, entry.CompanionName);

            var runtimeLines = entry.Sequence
                .Select(line => line.ToRuntimeLine(GetSequenceText(source, line, entry)))
                .ToArray();
            source.Comment = $"Billy banter replacement for {entry.SourceName}.";
            ConfigureBillyBanterConditions(source, billy, companion, null);
            source.Conditions.Unique = false;
            SetField(source, "m_Weight", GetBillyBanterWeight(entry));
            ConfigureBanterForRuntimeSequence(
                source,
                source,
                entry,
                billy,
                companion,
                line => _localization.Text(line.LocalizationKey));
            BillyBanterRuntimePatch.RegisterSequence(
                source,
                billy,
                companion,
                runtimeLines);

            return source;
        }

        private static float GetBillyBanterWeight(BillyBanterReplacement entry)
        {
            return string.Equals(entry.CompanionName, "Ulbrig", StringComparison.Ordinal) ? 3f : 1f;
        }

        private LocalizedString GetSequenceText(
            BlueprintBarkBanter banter,
            BillyBanterSequenceLine line,
            BillyBanterReplacement entry)
        {
            if (line.Kind == BillyBanterLineKind.Billy)
            {
                return _localization.Text(line.LocalizationKey);
            }

            var text = GetSourceText(banter, line.SourceRole);
            if (text == null)
            {
                _blueprints.Warning($"Billy banter {entry.LineId} could not find vanilla {line.SourceRole} text.");
            }

            return text;
        }

        private static void ConfigureBanterForRuntimeSequence(
            BlueprintBarkBanter banter,
            BlueprintBarkBanter source,
            BillyBanterReplacement entry,
            BlueprintUnit billy,
            BlueprintUnit companion,
            Func<BillyBanterSequenceLine, LocalizedString> localize)
        {
            var firstLine = entry.Sequence.FirstOrDefault();
            var secondLine = entry.Sequence.Skip(1).FirstOrDefault();
            var rootSpeaker = firstLine?.Kind == BillyBanterLineKind.Billy ? billy : companion;

            SetField(
                banter,
                "m_Unit",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(rootSpeaker));

            var firstText = firstLine?.Kind == BillyBanterLineKind.Billy
                ? localize(firstLine)
                : firstLine == null
                    ? null
                    : GetSourceText(source, firstLine.SourceRole);

            if (firstText != null)
            {
                banter.FirstPhrase = new[] { firstText };
            }

            if (secondLine?.Kind == BillyBanterLineKind.Billy)
            {
                EnsureBillyResponse(banter, billy, localize(secondLine));
            }
            else
            {
                EnsureKeptResponseSpeaker(banter, companion);
            }
        }

        private static LocalizedString GetSourceText(BlueprintBarkBanter banter, BanterSourceRole role)
        {
            if (role == BanterSourceRole.FirstPhrase)
            {
                return (banter.FirstPhrase ?? Array.Empty<LocalizedString>()).FirstOrDefault();
            }

            return (banter.Responses ?? Array.Empty<BlueprintBarkBanter.BanterResponseEntry>())
                .FirstOrDefault(response => response?.Response != null)
                ?.Response;
        }

        private static void ConfigureBillyBanterConditions(
            BlueprintBarkBanter banter,
            BlueprintUnit billy,
            BlueprintUnit companion,
            BlueprintUnit requiredVanillaSpeaker)
        {
            banter.Conditions = banter.Conditions ?? new BanterConditions();
            var extraConditions = banter.Conditions.ExtraConditions ?? new ConditionsChecker
            {
                Operation = Operation.And,
                Conditions = Array.Empty<Condition>()
            };
            var existing = (extraConditions.Conditions ?? Array.Empty<Condition>())
                .Where(condition => condition == null ||
                                    condition.name == null ||
                                    !condition.name.StartsWith("$CompanionInParty$WotrMod_Banter_", StringComparison.Ordinal))
                .ToArray();
            extraConditions.Operation = Operation.And;
            var requiredCompanions = new[] { billy, companion, requiredVanillaSpeaker }
                .Where(unit => unit != null)
                .GroupBy(unit => unit.AssetGuid)
                .Select(group => group.First());
            extraConditions.Conditions = existing
                .Concat(requiredCompanions.Select(unit => CreateCompanionInPartyCondition(unit, unit.name)))
                .ToArray();
            banter.Conditions.ExtraConditions = extraConditions;
        }

        private static Kingmaker.Designers.EventConditionActionSystem.Conditions.CompanionInParty CreateCompanionInPartyCondition(
            BlueprintUnit companion,
            string label,
            bool matchWhenActive = true,
            bool matchWhenDetached = true,
            bool matchWhenRemote = true,
            bool matchWhenDead = false,
            bool matchWhenEx = false)
        {
            var condition = new Kingmaker.Designers.EventConditionActionSystem.Conditions.CompanionInParty
            {
                name = $"$CompanionInParty$WotrMod_Banter_{label}",
                MatchWhenActive = matchWhenActive,
                MatchWhenDetached = matchWhenDetached,
                MatchWhenRemote = matchWhenRemote,
                MatchWhenDead = matchWhenDead,
                MatchWhenEx = matchWhenEx
            };
            SetField(
                condition,
                "m_companion",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(companion));
            return condition;
        }

        private static void EnsureKeptResponseSpeaker(BlueprintBarkBanter banter, BlueprintUnit companion)
        {
            foreach (var response in banter.Responses ?? Array.Empty<BlueprintBarkBanter.BanterResponseEntry>())
            {
                SetField(
                    response,
                    "m_Unit",
                    BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(companion));
            }
        }

        private static void EnsureBillyResponse(BlueprintBarkBanter banter, BlueprintUnit billy, LocalizedString text)
        {
            var responses = banter.Responses ?? Array.Empty<BlueprintBarkBanter.BanterResponseEntry>();
            var response = responses.FirstOrDefault();
            if (response == null)
            {
                response = new BlueprintBarkBanter.BanterResponseEntry
                {
                    ResponseCondition = new ConditionsChecker()
                };
                responses = new[] { response };
                banter.Responses = responses;
            }

            SetField(
                response,
                "m_Unit",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(billy));
            response.Response = text;
        }

        private void RegisterBillyBanterLocalization()
        {
            foreach (var line in CanonicalBillyBanterReplacements.SelectMany(entry => entry.BillyLines))
            {
                _localization.Put(line.LocalizationKey, line.Text);
                if (!TextOnlyBillyBanterLineIds.Contains(line.LineId))
                {
                    _localization.PutSoundEvent(line.LocalizationKey, line.AkEvent);
                }
            }
        }

        private void RegisterBillySceneInterjectionLocalization()
        {
            foreach (var line in BillySceneInterjections.SelectMany(entry => entry.Lines))
            {
                _localization.Put(line.LocalizationKey, line.Text);
                _localization.PutSoundEvent(line.LocalizationKey, line.AkEvent);
            }
        }

        private void EnsureBillySceneInterjections(BlueprintUnit billy)
        {
            if (billy == null)
            {
                return;
            }

            var installed = 0;
            foreach (var entry in BillySceneInterjections)
            {
                var anchor = _blueprints.Get<BlueprintCue>(entry.AnchorCueGuid);
                if (anchor == null)
                {
                    _blueprints.Warning($"Billy scene interjection skipped {entry.SceneId}: anchor cue {entry.AnchorCueGuid} was not found.");
                    continue;
                }

                if (entry.Lines.Length == 0)
                {
                    continue;
                }

                if (anchor.Answers != null && anchor.Answers.Count > 0)
                {
                    if (entry.PredecessorAnswerGuids.Length == 0 || string.IsNullOrWhiteSpace(entry.AnchorCloneGuid))
                    {
                        _blueprints.Warning($"Billy scene interjection skipped {entry.SceneId}: anchor cue uses answers and no predecessor answer was configured.");
                        continue;
                    }

                    EnsureBillySceneInterjectionAfterAnsweredCue(entry, anchor, billy);
                    installed++;
                    continue;
                }

                var originalContinue = CopyCueSelectionWithoutBillySceneInterjections(anchor.Continue);
                var billyCues = entry.Lines
                    .Select((line, index) => EnsureBillySceneInterjectionCue(entry.AnchorCueGuid, billy, line, index == 0))
                    .ToArray();

                for (var i = 0; i < billyCues.Length; i++)
                {
                    billyCues[i].Continue = i + 1 < billyCues.Length
                        ? CreateCueSelection(billyCues[i + 1])
                        : CopyCueSelection(originalContinue);
                }

                anchor.Continue = CreateCueSelectionWithFallback(billyCues[0], originalContinue);
                installed++;
            }

            Main.Log($"Installed {installed} Billy scene interjections.");
        }

        private void EnsureBillySceneInterjectionAfterAnsweredCue(
            BillySceneInterjection entry,
            BlueprintCue anchor,
            BlueprintUnit billy)
        {
            var originalAnswers = (anchor.Answers ?? new List<BlueprintAnswerBaseReference>()).ToList();
            var billyCues = entry.Lines
                .Select(line => EnsureBillySceneInterjectionCue(entry.AnchorCueGuid, billy, line, false))
                .ToArray();

            for (var i = 0; i < billyCues.Length; i++)
            {
                billyCues[i].Continue = i + 1 < billyCues.Length
                    ? CreateCueSelection(billyCues[i + 1])
                    : CreateEmptyCueSelection();
                billyCues[i].Answers = i + 1 == billyCues.Length
                    ? originalAnswers.ToList()
                    : new List<BlueprintAnswerBaseReference>();
            }

            var gatedAnchor = GetOrClone<BlueprintCue>(
                entry.AnchorCueGuid,
                entry.AnchorCloneGuid,
                "WotrMod_BillySceneInterjectionAnchor_" + entry.SceneId,
                "scene interjection anchor cue");
            gatedAnchor.Answers = new List<BlueprintAnswerBaseReference>();
            gatedAnchor.Continue = CreateCueSelection(billyCues[0]);
            gatedAnchor.Conditions = CreateConditionsWithBillyInParty(anchor.Conditions, billy);

            foreach (var answerGuid in entry.PredecessorAnswerGuids)
            {
                var answer = _blueprints.Get<BlueprintAnswer>(answerGuid);
                if (answer == null)
                {
                    _blueprints.Warning($"Billy scene interjection skipped answer path {entry.SceneId}: predecessor answer {answerGuid} was not found.");
                    continue;
                }

                var originalNextCue = CopyCueSelectionWithoutBillySceneInterjections(answer.NextCue);
                answer.NextCue = CreateCueSelectionWithFallback(gatedAnchor, originalNextCue);
            }
        }

        private BlueprintCue EnsureBillySceneInterjectionCue(
            string anchorCueGuid,
            BlueprintUnit billy,
            BillySceneInterjectionLine line,
            bool gateOnBillyInParty)
        {
            var cue = GetOrClone<BlueprintCue>(
                anchorCueGuid,
                line.CueGuid,
                "WotrMod_BillySceneInterjection_" + line.LineId,
                "scene interjection anchor cue");
            cue.Text = _localization.Text(line.LocalizationKey);
            cue.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(cue.Speaker, billy);
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Answers = new List<BlueprintAnswerBaseReference>();
            cue.Continue = CreateEmptyCueSelection();
            cue.ShowOnce = false;
            cue.ShowOnceCurrentDialog = false;
            cue.Conditions = gateOnBillyInParty
                ? new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        CreateCompanionInPartyCondition(
                            billy,
                            "SceneInterjectionBilly",
                            matchWhenRemote: false)
                    }
                }
                : new ConditionsChecker();
            return cue;
        }

        private ConditionsChecker CreateConditionsWithBillyInParty(ConditionsChecker source, BlueprintUnit billy)
        {
            var conditions = new List<Condition>();
            if (source?.Conditions != null)
            {
                conditions.AddRange(source.Conditions);
            }

            conditions.Add(
                CreateCompanionInPartyCondition(
                    billy,
                    "SceneInterjectionBilly",
                    matchWhenRemote: false));

            return new ConditionsChecker
            {
                Operation = Operation.And,
                Conditions = conditions.ToArray()
            };
        }

        private static CueSelection CopyCueSelectionWithoutBillySceneInterjections(CueSelection selection)
        {
            var refs = selection?.Cues ?? new List<BlueprintCueBaseReference>();
            return new CueSelection
            {
                Cues = refs
                    .Where(reference => !IsBillySceneInterjectionCue(reference?.Get()))
                    .ToList(),
                Strategy = selection?.Strategy ?? Strategy.First
            };
        }

        private static CueSelection CopyCueSelection(CueSelection selection)
        {
            return new CueSelection
            {
                Cues = (selection?.Cues ?? new List<BlueprintCueBaseReference>()).ToList(),
                Strategy = selection?.Strategy ?? Strategy.First
            };
        }

        private static CueSelection CreateCueSelectionWithFallback(BlueprintCue firstCue, CueSelection fallback)
        {
            var refs = new List<BlueprintCueBaseReference>();
            if (firstCue != null)
            {
                refs.Add(BlueprintReferenceBase.CreateTyped<BlueprintCueBaseReference>(firstCue));
            }

            refs.AddRange(fallback?.Cues ?? new List<BlueprintCueBaseReference>());
            return new CueSelection
            {
                Cues = refs,
                Strategy = fallback?.Strategy ?? Strategy.First
            };
        }

        private static bool IsBillySceneInterjectionCue(BlueprintCueBase cue)
        {
            return cue != null
                && cue.name != null
                && (cue.name.StartsWith("WotrMod_BillySceneInterjection_", StringComparison.Ordinal)
                    || cue.name.StartsWith("WotrMod_BillySceneInterjectionAnchor_", StringComparison.Ordinal));
        }

        private BlueprintUnitAsksList EnsureBillyBarks()
        {
            RegisterBillyBarkLines();
            var barks = GetOrClone<BlueprintUnitAsksList>(
                GameBlueprintIds.UnitAsks.ZombieCiarBarks,
                ModBlueprintIds.UnitAsks.BillyBarks,
                "WotrMod_BillyBarks",
                "Zombie Ciar barks");
            var component = new UnitAsksComponent
            {
                name = "$UnitBarksComponent$Billy",
                SoundBanks = new[] { "CMP_Billy_GVR_ENG" },
                PreviewSound = string.Empty
            };

            component.Aggro = CreateBark(component, BillyCombatStartLines, cooldown: 30f, interruptOthers: true, chance: 0.8f);
            component.Pain = CreateBark(component, BillyTakingDamageLines, cooldown: 2f);
            component.Fatigue = CreateBark(component, BillyIdleBanterLines, cooldown: 240f, chance: 0.05f);
            component.Death = CreateBark(component, BillyLowHealthLines, cooldown: 0f, interruptOthers: true);
            component.Unconscious = CreateBark(component, BillyLowHealthLines, cooldown: 0f, interruptOthers: true);
            component.LowHealth = CreateBark(component, BillyLowHealthLines, cooldown: 10f);
            component.CriticalHit = CreateBark(component, BillyOnHitLines, cooldown: 0f, chance: 0.7f);
            component.Order = CreateEmptyBark(component);
            component.OrderMove = CreateBark(component, BillyMovementLines, cooldown: 45f, chance: 0.1f);
            component.Selected = CreateBark(
                component,
                BillyIdleBanterLines.Concat(BillyPartyBanterLines).Concat(BillyIroriFlavorLines),
                cooldown: 0f,
                chance: 1.0f,
                audioOnly: true);
            component.RefuseEquip = CreateBark(component, BillyPartyBanterLines, cooldown: 0f, interruptOthers: true);
            component.RefuseCast = CreateBark(component, BillyBuffingLines, cooldown: 0f, interruptOthers: true);
            component.CheckSuccess = CreateBark(component, BillyOnHitLines, cooldown: 0f);
            component.CheckFail = CreateBark(component, BillyCheckFailLines, cooldown: 0f);
            component.RefuseUnequip = CreateEmptyBark(component);
            component.Discovery = CreateBark(component, BillyMovementLines.Concat(BillyIroriFlavorLines), cooldown: 0f);
            component.Stealth = CreateBark(component, BillyStealthLines, cooldown: 0f);
            component.StormRain = CreateEmptyBark(component);
            component.StormSnow = CreateEmptyBark(component);
            component.AnimationBarks = Array.Empty<UnitAsksComponent.AnimationBark>();

            _blueprints.SetComponents(barks, component);

            // Diagnostic: confirm AkEvent strings are assigned
            var aggroEntry = component.Aggro?.Entries?.FirstOrDefault();
            Main.Log($"Billy bark diagnostic — Aggro[0].AkEvent='{aggroEntry?.AkEvent ?? "<null>"}'");
            var painEntry = component.Pain?.Entries?.FirstOrDefault();
            Main.Log($"Billy bark diagnostic — Pain[0].AkEvent='{painEntry?.AkEvent ?? "<null>"}'");

            return barks;
        }

        private void RegisterBillyBarkLines()
        {
            foreach (var pair in BillyBarkLocalizationKeys)
            {
                _localization.Put(pair.Value, pair.Key);
            }
        }

        private UnitAsksComponent.Bark CreateBark(
            UnitAsksComponent owner,
            IEnumerable<string> lines,
            float cooldown,
            bool interruptOthers = false,
            float delayMin = 0f,
            float delayMax = 0f,
            float chance = 1f,
            bool showOnScreen = true,
            bool audioOnly = false)
        {
            return new UnitAsksComponent.Bark
            {
                Entries = lines.Select(audioOnly
                    ? (Func<string, UnitAsksComponent.BarkEntry>)CreateAudioOnlyBarkEntry
                    : CreateBarkEntry).ToArray(),
                Cooldown = cooldown,
                InterruptOthers = interruptOthers,
                DelayMin = delayMin,
                DelayMax = delayMax,
                Chance = chance,
                ShowOnScreen = showOnScreen,
                Owner = owner
            };
        }

        private UnitAsksComponent.BarkEntry CreateAudioOnlyBarkEntry(string line)
        {
            var entry = new UnitAsksComponent.BarkEntry
            {
                Text = null,
                AkEvent = BillyBarkAkEvents.TryGetValue(line, out var akEvent) ? akEvent : string.Empty,
                RandomWeight = 1f,
                ExcludeTime = 2
            };
            SetField(entry, "m_RequiredFlags", Array.Empty<BlueprintUnlockableFlagReference>());
            SetField(entry, "m_ExcludedFlags", Array.Empty<BlueprintUnlockableFlagReference>());
            SetField(entry, "m_RequiredEtudes", Array.Empty<BlueprintEtudeReference>());
            SetField(entry, "m_ExcludedEtudes", Array.Empty<BlueprintEtudeReference>());
            return entry;
        }

        private static UnitAsksComponent.Bark CreateEmptyBark(UnitAsksComponent owner)
        {
            return new UnitAsksComponent.Bark
            {
                Entries = Array.Empty<UnitAsksComponent.BarkEntry>(),
                Cooldown = 0f,
                InterruptOthers = false,
                DelayMin = 0f,
                DelayMax = 0f,
                Chance = 1f,
                ShowOnScreen = false,
                Owner = owner
            };
        }

        private UnitAsksComponent.AnimationBark CreateAnimationBark(
            UnitAsksComponent owner,
            MappedAnimationEventType animationEvent,
            IEnumerable<string> lines,
            float cooldown,
            bool interruptOthers = false,
            float delayMin = 0f,
            float delayMax = 0f,
            float chance = 1f)
        {
            return new UnitAsksComponent.AnimationBark
            {
                AnimationEvent = animationEvent,
                Entries = lines.Select(CreateBarkEntry).ToArray(),
                Cooldown = cooldown,
                InterruptOthers = interruptOthers,
                DelayMin = delayMin,
                DelayMax = delayMax,
                Chance = chance,
                ShowOnScreen = true,
                Owner = owner
            };
        }

        private UnitAsksComponent.BarkEntry CreateBarkEntry(string line)
        {
            var entry = new UnitAsksComponent.BarkEntry
            {
                Text = CreateSharedString(_localization.Text(BillyBarkLocalizationKeys[line])),
                AkEvent = BillyBarkAkEvents.TryGetValue(line, out var akEvent) ? akEvent : string.Empty,
                RandomWeight = 1f,
                ExcludeTime = 2
            };
            SetField(entry, "m_RequiredFlags", Array.Empty<BlueprintUnlockableFlagReference>());
            SetField(entry, "m_ExcludedFlags", Array.Empty<BlueprintUnlockableFlagReference>());
            SetField(entry, "m_RequiredEtudes", Array.Empty<BlueprintEtudeReference>());
            SetField(entry, "m_ExcludedEtudes", Array.Empty<BlueprintEtudeReference>());
            return entry;
        }

        private static SharedStringAsset CreateSharedString(LocalizedString text)
        {
            var asset = ScriptableObject.CreateInstance<SharedStringAsset>();
            asset.String = text;
            return asset;
        }

        private static Dictionary<string, string> BuildBillyBarkLocalizationKeys()
        {
            var keys = new Dictionary<string, string>();
            foreach (var line in GetAllBillyBarkLines())
            {
                if (!keys.ContainsKey(line))
                {
                    keys[line] = $"wotr_mod.companion.billy.bark.{keys.Count:D2}";
                }
            }

            return keys;
        }

        private static IEnumerable<string> GetAllBillyBarkLines()
        {
            return BillyIdleBanterLines
                .Concat(BillyMovementLines)
                .Concat(BillyCombatStartLines)
                .Concat(BillyRangedAttackLines)
                .Concat(BillyOnHitLines)
                .Concat(BillyOnKillLines)
                .Concat(BillyTakingDamageLines)
                .Concat(BillyLowHealthLines)
                .Concat(BillyBuffingLines)
                .Concat(BillyPartyBanterLines)
                .Concat(BillyIroriFlavorLines)
                .Concat(BillyCheckFailLines);
        }

        private static Dictionary<string, string> BuildBillyBarkAkEvents()
        {
            var events = new Dictionary<string, string>();

            // Per-line events — each line maps to Play_CMP_Billy_{Category}_{Index:D2}
            // First-wins for lines shared across categories
            AddLineEvents(events, BillyIdleBanterLines,   "Play_CMP_Billy_IdleBanter");
            AddLineEvents(events, BillyMovementLines,     "Play_CMP_Billy_Movement");
            AddLineEvents(events, BillyCombatStartLines,  "Play_CMP_Billy_CombatStart");
            AddLineEvents(events, BillyRangedAttackLines, "Play_CMP_Billy_RangedAttack");
            AddLineEvents(events, BillyOnHitLines,        "Play_CMP_Billy_OnHit");
            AddLineEvents(events, BillyOnKillLines,       "Play_CMP_Billy_OnKill");
            AddLineEvents(events, BillyTakingDamageLines, "Play_CMP_Billy_TakingDamage");
            AddLineEvents(events, BillyLowHealthLines,    "Play_CMP_Billy_LowHealth");
            AddLineEvents(events, BillyBuffingLines,      "Play_CMP_Billy_Buffing");
            AddLineEvents(events, BillyPartyBanterLines,  "Play_CMP_Billy_PartyBanter");
            AddLineEvents(events, BillyIroriFlavorLines,  "Play_CMP_Billy_IroriFlavor");

            // Inline slot groups have dedicated recordings — override any prior assignment
            SetLineEvents(events, BillyCheckFailLines, "Play_CMP_Billy_CheckFail");
            SetLineEvents(events, BillyStealthLines,   "Play_CMP_Billy_Stealth");

            return events;
        }

        private static void AddLineEvents(Dictionary<string, string> dict, string[] lines, string prefix)
        {
            for (var i = 0; i < lines.Length; i++)
                if (!dict.ContainsKey(lines[i]))
                    dict[lines[i]] = $"{prefix}_{i:D2}";
        }

        private static void SetLineEvents(Dictionary<string, string> dict, string[] lines, string prefix)
        {
            for (var i = 0; i < lines.Length; i++)
                dict[lines[i]] = $"{prefix}_{i:D2}";
        }

    }
}
