using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content
{
    internal sealed class BillyQuestStarter : IContentModule, IAreaLoadModule, IItemsCollectionHandler
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid BillyStandInGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.BillyShieldMazeStandIn);
        private static readonly BlueprintGuid BowGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.NeophytesLongbowOfDiscipline);
        private static readonly BlueprintGuid PilgrimageRecordGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.BillyPilgrimageRecord);
        private static readonly BlueprintGuid ArchersTunicGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.ArchersTunic);
        private static readonly BlueprintGuid AcolyteBowGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.AcolytesLongbowOfDiscipline);
        private static readonly BlueprintGuid AcolyteArmorGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.IroriAcolytesArmor);
        private static readonly BlueprintGuid AdeptArmorGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.IroriAdeptsArmor);
        private static readonly BlueprintGuid AdeptBowGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.AdeptsLongbowOfDiscipline);
        private static readonly QuestExperienceReward Act1JalmerayLeadReward =
            new QuestExperienceReward(EncounterType.ChallengeMinor, 3);
        private static readonly QuestExperienceReward Act1TransferRecordReward =
            new QuestExperienceReward(EncounterType.ChallengeMinor, 4);
        private static readonly QuestExperienceReward Act1TrailColdReward =
            new QuestExperienceReward(EncounterType.QuestNormal, 4);
        private static readonly QuestExperienceReward Act2LeperSmileReward =
            new QuestExperienceReward(EncounterType.QuestNormal, 6);
        private static readonly QuestExperienceReward Act2LostChapelReward =
            new QuestExperienceReward(EncounterType.QuestNormal, 8);
        private static readonly QuestExperienceReward Act3IvorySanctumReward =
            new QuestExperienceReward(EncounterType.QuestNormal, 12);
        private static readonly QuestExperienceReward Act3MidnightFaneReward =
            new QuestExperienceReward(EncounterType.QuestNormal, 14);
        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        private IDisposable _subscription;

        public BillyQuestStarter(BlueprintTool blueprints, UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Billy Quest Starter";

        public void RegisterLocalization()
        {
        }

        public void Install()
        {
            EnsureStartedFlag();
            EnsureBillyConditionQuest();
            if (_subscription == null)
            {
                _subscription = EventBus.Subscribe(this);
            }
        }

        public void OnAreaLoaded()
        {
            TryStartBowQuest();
            TryAdvancePilgrimageRecordClue();
            TryAdvanceArchersTunicReward();
            TryAdvanceAcolyteBowReward();
            TryAdvanceAcolyteArmorReward();
            TryAdvanceAdeptArmorReward();
            TryAdvanceAdeptBowReward();
        }

        public void HandleItemsAdded(ItemsCollection collection, ItemEntity item, int count)
        {
            if (collection != Game.Instance?.Player?.Inventory || item?.Blueprint == null)
            {
                return;
            }

            if (item.Blueprint.AssetGuid == BowGuid)
            {
                TryStartBowQuest();
            }

            if (item.Blueprint.AssetGuid == PilgrimageRecordGuid)
            {
                TryAdvancePilgrimageRecordClue(startDialog: true);
            }

            if (item.Blueprint.AssetGuid == ArchersTunicGuid)
            {
                TryAdvanceArchersTunicReward(startDialog: true);
            }

            if (item.Blueprint.AssetGuid == AcolyteBowGuid)
            {
                TryAdvanceAcolyteBowReward(startDialog: true);
            }

            if (item.Blueprint.AssetGuid == AcolyteArmorGuid)
            {
                TryAdvanceAcolyteArmorReward(startDialog: true);
            }

            if (item.Blueprint.AssetGuid == AdeptArmorGuid)
            {
                TryAdvanceAdeptArmorReward(startDialog: true);
            }

            if (item.Blueprint.AssetGuid == AdeptBowGuid)
            {
                TryAdvanceAdeptBowReward(startDialog: true);
            }
        }

        public void HandleItemsRemoved(ItemsCollection collection, ItemEntity item, int count)
        {
        }

        private BlueprintUnlockableFlag EnsureStartedFlag()
        {
            var flag = _blueprints.Get<BlueprintUnlockableFlag>(ModBlueprintIds.Flags.BillyBowQuestStarted);
            if (flag != null)
            {
                return flag;
            }

            flag = new BlueprintUnlockableFlag
            {
                name = "WotrMod_BillyBowQuestStarted",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Flags.BillyBowQuestStarted)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Flags.BillyBowQuestStarted, flag);
            return flag;
        }

        private bool TryAdvancePilgrimageRecordClue(bool startDialog = false)
        {
            try
            {
                var player = Game.Instance?.Player;
                if (player?.QuestBook == null || !PlayerHasPilgrimageRecord())
                {
                    return false;
                }

                var jalmerayObjective = EnsureBillyConditionAct1JalmerayLeadObjective();
                var transferObjective = EnsureBillyConditionTransferRecordObjective();
                if (player.QuestBook.GetObjectiveState(transferObjective) != QuestObjectiveState.None)
                {
                    return false;
                }

                var jalmerayState = player.QuestBook.GetObjectiveState(jalmerayObjective);
                if (jalmerayState == QuestObjectiveState.None)
                {
                    return false;
                }

                if (jalmerayState == QuestObjectiveState.Started)
                {
                    player.QuestBook.CompleteObjective(jalmerayObjective);
                }

                StartQuestObjective(player, transferObjective);
                _logger.Log("Advanced Billy condition quest to the Act 1 transfer record clue.");
                if (startDialog)
                {
                    TryStartBillyQuestDialog(
                        ModBlueprintIds.Dialogs.BillyAct1RecordDialog,
                        "Billy Act 1 transfer record dialog");
                }

                TryAdvanceArchersTunicReward();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Billy Act 1 transfer record trigger failed: {ex}");
                return false;
            }
        }

        private bool TryAdvanceArchersTunicReward(bool startDialog = false)
        {
            try
            {
                var player = Game.Instance?.Player;
                if (player?.QuestBook == null || !PlayerHasArchersTunic())
                {
                    return false;
                }

                var transferObjective = EnsureBillyConditionTransferRecordObjective();
                var trailColdObjective = EnsureBillyConditionAct1TrailColdObjective();
                if (player.QuestBook.GetObjectiveState(trailColdObjective) != QuestObjectiveState.None)
                {
                    return false;
                }

                var transferState = player.QuestBook.GetObjectiveState(transferObjective);
                if (transferState == QuestObjectiveState.None)
                {
                    return false;
                }

                if (transferState == QuestObjectiveState.Started)
                {
                    player.QuestBook.CompleteObjective(transferObjective);
                }

                StartQuestObjective(player, trailColdObjective);
                _logger.Log("Advanced Billy condition quest to the Act 1 trail-cold objective.");
                if (startDialog)
                {
                    TryStartBillyQuestDialog(
                        ModBlueprintIds.Dialogs.BillyAct1TunicDialog,
                        "Billy Act 1 Irori Neophyte's Armor dialog");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Billy Act 1 Irori Neophyte's Armor trigger failed: {ex}");
                return false;
            }
        }

        private bool TryAdvanceAcolyteBowReward(bool startDialog = false)
        {
            return TryAdvanceEquipmentReward(
                AcolyteBowGuid,
                EnsureBillyConditionAct1TrailColdObjective,
                EnsureBillyConditionAct2LostChapelObjective,
                ModBlueprintIds.Items.NeophytesLongbowOfDiscipline,
                ModBlueprintIds.Dialogs.BillyAct2BowDialog,
                "Billy Act 2 Leper's Smile bow reward",
                startDialog);
        }

        private bool TryAdvanceAcolyteArmorReward(bool startDialog = false)
        {
            return TryAdvanceEquipmentReward(
                AcolyteArmorGuid,
                EnsureBillyConditionAct2LostChapelObjective,
                EnsureBillyConditionAct3IvorySanctumObjective,
                ModBlueprintIds.Items.ArchersTunic,
                ModBlueprintIds.Dialogs.BillyAct2ArmorDialog,
                "Billy Act 2 Lost Chapel armor reward",
                startDialog);
        }

        private bool TryAdvanceAdeptArmorReward(bool startDialog = false)
        {
            return TryAdvanceEquipmentReward(
                AdeptArmorGuid,
                EnsureBillyConditionAct3IvorySanctumObjective,
                EnsureBillyConditionAct3MidnightFaneObjective,
                ModBlueprintIds.Items.IroriAcolytesArmor,
                ModBlueprintIds.Dialogs.BillyAct3ArmorDialog,
                "Billy Act 3 Ivory Sanctum armor reward",
                startDialog);
        }

        private bool TryAdvanceAdeptBowReward(bool startDialog = false)
        {
            return TryAdvanceEquipmentReward(
                AdeptBowGuid,
                EnsureBillyConditionAct3MidnightFaneObjective,
                EnsureBillyConditionAct4AbyssObjective,
                ModBlueprintIds.Items.AcolytesLongbowOfDiscipline,
                ModBlueprintIds.Dialogs.BillyAct3BowDialog,
                "Billy Act 3 Midnight Fane bow reward",
                startDialog);
        }

        private bool TryAdvanceEquipmentReward(
            BlueprintGuid rewardItemGuid,
            Func<BlueprintQuestObjective> currentObjectiveFactory,
            Func<BlueprintQuestObjective> nextObjectiveFactory,
            string previousItemGuid,
            string dialogGuid,
            string logName,
            bool startDialog)
        {
            try
            {
                var player = Game.Instance?.Player;
                if (player?.QuestBook == null || !PlayerHasItem(rewardItemGuid))
                {
                    return false;
                }

                var currentObjective = currentObjectiveFactory();
                var nextObjective = nextObjectiveFactory();
                if (player.QuestBook.GetObjectiveState(nextObjective) != QuestObjectiveState.None)
                {
                    return false;
                }

                var currentState = player.QuestBook.GetObjectiveState(currentObjective);
                if (currentState == QuestObjectiveState.None)
                {
                    return false;
                }

                var previousItem = _blueprints.Require<BlueprintItem>(
                    previousItemGuid,
                    logName + " prior item");
                if (currentState == QuestObjectiveState.Started)
                {
                    player.QuestBook.CompleteObjective(currentObjective);
                }

                player.Inventory.Remove(previousItem, 1, allowRemoveEquipped: true);
                StartQuestObjective(player, nextObjective);
                _logger.Log("Advanced " + logName + " and replaced the prior equipment tier.");
                if (startDialog)
                {
                    TryStartBillyQuestDialog(dialogGuid, logName + " dialog");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(logName + " trigger failed: " + ex);
                return false;
            }
        }

        private void TryStartBowQuest()
        {
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                if (player?.Inventory == null || !PlayerHasBow() || game.IsModeActive(GameModeType.Dialog))
                {
                    return;
                }

                var flag = EnsureStartedFlag();
                var objective = EnsureBillyConditionQuestObjective();
                if (player.UnlockableFlags.IsUnlocked(flag))
                {
                    StartQuestObjective(player, objective);
                    return;
                }

                var billy = FindBilly();
                var initiator = player.MainCharacter.Value ?? player.GetMainPartyUnit();
                var dialog = _blueprints.Get<BlueprintDialog>(ModBlueprintIds.Dialogs.BillyBowQuestDialog);
                if (billy == null || initiator == null || dialog == null || objective == null)
                {
                    return;
                }

                StartQuestObjective(player, objective);
                player.UnlockableFlags.Unlock(flag);
                game.DialogController.StartDialogWithUnit(dialog, billy, initiator);
                _logger.Log("Started Billy bow quest dialog.");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Billy bow quest dialog failed to start: {ex}");
            }
        }

        private void TryStartBillyQuestDialog(string dialogGuid, string logName)
        {
            var game = Game.Instance;
            var player = game?.Player;
            if (player == null || game.IsModeActive(GameModeType.Dialog))
            {
                return;
            }

            var billy = FindBilly();
            var initiator = player.MainCharacter.Value ?? player.GetMainPartyUnit();
            var dialog = _blueprints.Get<BlueprintDialog>(dialogGuid);
            if (billy == null || initiator == null || dialog == null)
            {
                return;
            }

            game.DialogController.StartDialogWithUnit(dialog, billy, initiator);
            _logger.Log("Started " + logName + ".");
        }

        private BlueprintQuest EnsureBillyConditionQuest()
        {
            var quest = _blueprints.Get<BlueprintQuest>(ModBlueprintIds.Quests.BillyCondition);
            if (quest == null)
            {
                quest = new BlueprintQuest
                {
                    name = "WotrMod_BillyConditionQuest",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Quests.BillyCondition)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Quests.BillyCondition, quest);
            }

            var objective = EnsureBillyConditionQuestObjective();
            var jalmerayObjective = EnsureBillyConditionAct1JalmerayLeadObjective();
            var transferObjective = EnsureBillyConditionTransferRecordObjective();
            var trailColdObjective = EnsureBillyConditionAct1TrailColdObjective();
            var act2LostChapelObjective = EnsureBillyConditionAct2LostChapelObjective();
            var act3IvorySanctumObjective = EnsureBillyConditionAct3IvorySanctumObjective();
            var act3MidnightFaneObjective = EnsureBillyConditionAct3MidnightFaneObjective();
            var act4AbyssObjective = EnsureBillyConditionAct4AbyssObjective();
            quest.Title = CreateText(LocalizationIds.Mod.BillyConditionQuestTitle);
            quest.Description = CreateText(LocalizationIds.Mod.BillyConditionQuestDescription);
            quest.CompletionText = CreateText(LocalizationIds.Mod.BillyConditionQuestCompletion);
            SetField(quest, "m_Group", QuestGroupId.CompanionQuests);
            SetField(quest, "m_DescriptionPriority", 0);
            SetField(quest, "m_Type", QuestType.Normal);
            SetField(quest, "m_LastChapter", 5);
            SetField(
                quest,
                "m_Objectives",
                new List<BlueprintQuestObjectiveReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(objective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(jalmerayObjective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(transferObjective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(trailColdObjective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(act2LostChapelObjective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(act3IvorySanctumObjective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(act3MidnightFaneObjective),
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(act4AbyssObjective)
                });

            return quest;
        }

        private BlueprintQuestObjective EnsureBillyConditionQuestObjective()
        {
            var objective = _blueprints.Get<BlueprintQuestObjective>(ModBlueprintIds.QuestObjectives.BillyConditionInvestigate);
            if (objective == null)
            {
                objective = new BlueprintQuestObjective
                {
                    name = "WotrMod_BillyConditionInvestigateObjective",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.QuestObjectives.BillyConditionInvestigate)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.QuestObjectives.BillyConditionInvestigate, objective);
            }

            var quest = _blueprints.Get<BlueprintQuest>(ModBlueprintIds.Quests.BillyCondition);
            objective.Title = CreateText(LocalizationIds.Mod.BillyConditionInvestigateTitle);
            objective.Description = CreateText(LocalizationIds.Mod.BillyConditionInvestigateDescription);
            objective.Locations = objective.Locations ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintGlobalMapPoint.Reference>();
            objective.MultiEntranceEntries = objective.MultiEntranceEntries ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintMultiEntranceEntry.Reference>();
            objective.AutoFailDays = 0;
            objective.IsFakeFail = false;
            objective.StartOnKingdomTime = false;
            SetField(objective, "m_Addendums", new List<BlueprintQuestObjectiveReference>());
            SetField(objective, "m_Areas", new List<BlueprintAreaReference>());
            SetField(objective, "m_FinishParent", false);
            SetField(objective, "m_Hidden", false);
            SetField(
                objective,
                "m_NextObjectives",
                new List<BlueprintQuestObjectiveReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(
                        EnsureBillyConditionAct1JalmerayLeadObjective())
                });
            SetField(
                objective,
                "m_Quest",
                quest == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintQuestReference>(quest));
            SetField(objective, "m_Type", BlueprintQuestObjective.Type.Objective);
            QuestRewardInstaller.SetExperienceReward(
                _blueprints,
                objective,
                "WotrMod_BillyConditionInvestigateReward",
                Act1JalmerayLeadReward);

            return objective;
        }

        private BlueprintQuestObjective EnsureBillyConditionAct1JalmerayLeadObjective()
        {
            var objective = _blueprints.Get<BlueprintQuestObjective>(ModBlueprintIds.QuestObjectives.BillyConditionAct1JalmerayLead);
            if (objective == null)
            {
                objective = new BlueprintQuestObjective
                {
                    name = "WotrMod_BillyConditionAct1JalmerayLeadObjective",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.QuestObjectives.BillyConditionAct1JalmerayLead)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.QuestObjectives.BillyConditionAct1JalmerayLead, objective);
            }

            var quest = _blueprints.Get<BlueprintQuest>(ModBlueprintIds.Quests.BillyCondition);
            objective.Title = CreateText(LocalizationIds.Mod.BillyConditionAct1JalmerayLeadTitle);
            objective.Description = CreateText(LocalizationIds.Mod.BillyConditionAct1JalmerayLeadDescription);
            objective.Locations = objective.Locations ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintGlobalMapPoint.Reference>();
            objective.MultiEntranceEntries = objective.MultiEntranceEntries ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintMultiEntranceEntry.Reference>();
            objective.AutoFailDays = 0;
            objective.IsFakeFail = false;
            objective.StartOnKingdomTime = false;
            SetField(objective, "m_Addendums", new List<BlueprintQuestObjectiveReference>());
            SetField(objective, "m_Areas", new List<BlueprintAreaReference>());
            SetField(objective, "m_FinishParent", false);
            SetField(objective, "m_Hidden", false);
            SetField(
                objective,
                "m_NextObjectives",
                new List<BlueprintQuestObjectiveReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(
                        EnsureBillyConditionTransferRecordObjective())
                });
            SetField(
                objective,
                "m_Quest",
                quest == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintQuestReference>(quest));
            SetField(objective, "m_Type", BlueprintQuestObjective.Type.Objective);
            QuestRewardInstaller.SetExperienceReward(
                _blueprints,
                objective,
                "WotrMod_BillyConditionAct1JalmerayLeadReward",
                Act1TransferRecordReward);

            return objective;
        }

        private BlueprintQuestObjective EnsureBillyConditionTransferRecordObjective()
        {
            var objective = _blueprints.Get<BlueprintQuestObjective>(ModBlueprintIds.QuestObjectives.BillyConditionTransferRecord);
            if (objective == null)
            {
                objective = new BlueprintQuestObjective
                {
                    name = "WotrMod_BillyConditionTransferRecordObjective",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.QuestObjectives.BillyConditionTransferRecord)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.QuestObjectives.BillyConditionTransferRecord, objective);
            }

            var quest = _blueprints.Get<BlueprintQuest>(ModBlueprintIds.Quests.BillyCondition);
            objective.Title = CreateText(LocalizationIds.Mod.BillyConditionTransferRecordTitle);
            objective.Description = CreateText(LocalizationIds.Mod.BillyConditionTransferRecordDescription);
            objective.Locations = objective.Locations ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintGlobalMapPoint.Reference>();
            objective.MultiEntranceEntries = objective.MultiEntranceEntries ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintMultiEntranceEntry.Reference>();
            objective.AutoFailDays = 0;
            objective.IsFakeFail = false;
            objective.StartOnKingdomTime = false;
            SetField(objective, "m_Addendums", new List<BlueprintQuestObjectiveReference>());
            SetField(objective, "m_Areas", new List<BlueprintAreaReference>());
            SetField(objective, "m_FinishParent", false);
            SetField(objective, "m_Hidden", false);
            SetField(
                objective,
                "m_NextObjectives",
                new List<BlueprintQuestObjectiveReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(
                        EnsureBillyConditionAct1TrailColdObjective())
                });
            SetField(
                objective,
                "m_Quest",
                quest == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintQuestReference>(quest));
            SetField(objective, "m_Type", BlueprintQuestObjective.Type.Objective);
            QuestRewardInstaller.SetExperienceReward(
                _blueprints,
                objective,
                "WotrMod_BillyConditionTransferRecordReward",
                Act1TrailColdReward);

            return objective;
        }

        private BlueprintQuestObjective EnsureBillyConditionAct1TrailColdObjective()
        {
            var objective = _blueprints.Get<BlueprintQuestObjective>(ModBlueprintIds.QuestObjectives.BillyConditionAct1TrailCold);
            if (objective == null)
            {
                objective = new BlueprintQuestObjective
                {
                    name = "WotrMod_BillyConditionAct1TrailColdObjective",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.QuestObjectives.BillyConditionAct1TrailCold)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.QuestObjectives.BillyConditionAct1TrailCold, objective);
            }

            var quest = _blueprints.Get<BlueprintQuest>(ModBlueprintIds.Quests.BillyCondition);
            objective.Title = CreateText(LocalizationIds.Mod.BillyConditionAct1TrailColdTitle);
            objective.Description = CreateText(LocalizationIds.Mod.BillyConditionAct1TrailColdDescription);
            objective.Locations = objective.Locations ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintGlobalMapPoint.Reference>();
            objective.MultiEntranceEntries = objective.MultiEntranceEntries ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintMultiEntranceEntry.Reference>();
            objective.AutoFailDays = 0;
            objective.IsFakeFail = false;
            objective.StartOnKingdomTime = false;
            SetField(objective, "m_Addendums", new List<BlueprintQuestObjectiveReference>());
            SetField(objective, "m_Areas", new List<BlueprintAreaReference>());
            SetField(objective, "m_FinishParent", false);
            SetField(objective, "m_Hidden", false);
            SetField(
                objective,
                "m_NextObjectives",
                new List<BlueprintQuestObjectiveReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(
                        EnsureBillyConditionAct2LostChapelObjective())
                });
            SetField(
                objective,
                "m_Quest",
                quest == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintQuestReference>(quest));
            SetField(objective, "m_Type", BlueprintQuestObjective.Type.Objective);
            QuestRewardInstaller.SetExperienceReward(
                _blueprints,
                objective,
                "WotrMod_BillyConditionAct1TrailColdReward",
                Act2LeperSmileReward);

            return objective;
        }

        private BlueprintQuestObjective EnsureBillyConditionAct2LostChapelObjective()
        {
            return EnsureBillyConditionObjective(
                ModBlueprintIds.QuestObjectives.BillyConditionAct2LostChapel,
                "WotrMod_BillyConditionAct2LostChapelObjective",
                LocalizationIds.Mod.BillyConditionAct2LostChapelTitle,
                LocalizationIds.Mod.BillyConditionAct2LostChapelDescription,
                EnsureBillyConditionAct3IvorySanctumObjective,
                "WotrMod_BillyConditionAct2LostChapelReward",
                Act2LostChapelReward);
        }

        private BlueprintQuestObjective EnsureBillyConditionAct3IvorySanctumObjective()
        {
            return EnsureBillyConditionObjective(
                ModBlueprintIds.QuestObjectives.BillyConditionAct3IvorySanctum,
                "WotrMod_BillyConditionAct3IvorySanctumObjective",
                LocalizationIds.Mod.BillyConditionAct3IvorySanctumTitle,
                LocalizationIds.Mod.BillyConditionAct3IvorySanctumDescription,
                EnsureBillyConditionAct3MidnightFaneObjective,
                "WotrMod_BillyConditionAct3IvorySanctumReward",
                Act3IvorySanctumReward);
        }

        private BlueprintQuestObjective EnsureBillyConditionAct3MidnightFaneObjective()
        {
            return EnsureBillyConditionObjective(
                ModBlueprintIds.QuestObjectives.BillyConditionAct3MidnightFane,
                "WotrMod_BillyConditionAct3MidnightFaneObjective",
                LocalizationIds.Mod.BillyConditionAct3MidnightFaneTitle,
                LocalizationIds.Mod.BillyConditionAct3MidnightFaneDescription,
                EnsureBillyConditionAct4AbyssObjective,
                "WotrMod_BillyConditionAct3MidnightFaneReward",
                Act3MidnightFaneReward);
        }

        private BlueprintQuestObjective EnsureBillyConditionAct4AbyssObjective()
        {
            return EnsureBillyConditionObjective(
                ModBlueprintIds.QuestObjectives.BillyConditionAct4Abyss,
                "WotrMod_BillyConditionAct4AbyssObjective",
                LocalizationIds.Mod.BillyConditionAct4AbyssTitle,
                LocalizationIds.Mod.BillyConditionAct4AbyssDescription,
                nextObjectiveFactory: null,
                rewardName: "WotrMod_BillyConditionAct4AbyssReward",
                reward: null);
        }

        private BlueprintQuestObjective EnsureBillyConditionObjective(
            string objectiveGuid,
            string internalName,
            string titleKey,
            string descriptionKey,
            Func<BlueprintQuestObjective> nextObjectiveFactory,
            string rewardName,
            QuestExperienceReward reward)
        {
            var objective = _blueprints.Get<BlueprintQuestObjective>(objectiveGuid);
            if (objective == null)
            {
                objective = new BlueprintQuestObjective
                {
                    name = internalName,
                    AssetGuid = BlueprintGuid.Parse(objectiveGuid)
                };
                _blueprints.AddCachedBlueprint(objectiveGuid, objective);
            }

            var quest = _blueprints.Get<BlueprintQuest>(ModBlueprintIds.Quests.BillyCondition);
            objective.Title = CreateText(titleKey);
            objective.Description = CreateText(descriptionKey);
            objective.Locations = objective.Locations
                                  ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintGlobalMapPoint.Reference>();
            objective.MultiEntranceEntries = objective.MultiEntranceEntries
                                             ?? new List<Kingmaker.Globalmap.Blueprints.BlueprintMultiEntranceEntry.Reference>();
            objective.AutoFailDays = 0;
            objective.IsFakeFail = false;
            objective.StartOnKingdomTime = false;
            SetField(objective, "m_Addendums", new List<BlueprintQuestObjectiveReference>());
            SetField(objective, "m_Areas", new List<BlueprintAreaReference>());
            SetField(objective, "m_FinishParent", false);
            SetField(objective, "m_Hidden", false);
            var nextObjectives = new List<BlueprintQuestObjectiveReference>();
            if (nextObjectiveFactory != null)
            {
                nextObjectives.Add(
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(
                        nextObjectiveFactory()));
            }

            SetField(objective, "m_NextObjectives", nextObjectives);
            SetField(
                objective,
                "m_Quest",
                quest == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintQuestReference>(quest));
            SetField(objective, "m_Type", BlueprintQuestObjective.Type.Objective);
            QuestRewardInstaller.SetExperienceReward(
                _blueprints,
                objective,
                rewardName,
                reward);

            return objective;
        }

        private static void StartQuestObjective(Player player, BlueprintQuestObjective objective)
        {
            if (player?.QuestBook == null || objective == null)
            {
                return;
            }

            if (player.QuestBook.GetObjectiveState(objective) == QuestObjectiveState.None)
            {
                player.QuestBook.GiveObjective(objective);
            }
        }

        private static LocalizedString CreateText(string key)
        {
            return new LocalizedString { Key = key };
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target?.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static bool PlayerHasBow()
        {
            var player = Game.Instance?.Player;
            var inventoryHasBow = player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == BowGuid) == true;
            if (inventoryHasBow)
            {
                return true;
            }

            return player?.AllCharacters
                ?.Any(unit => unit?.Body?.CurrentEquipmentSlots
                    ?.Any(slot => slot?.MaybeItem?.Blueprint?.AssetGuid == BowGuid) == true) == true;
        }

        private static bool PlayerHasPilgrimageRecord()
        {
            return Game.Instance?.Player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == PilgrimageRecordGuid) == true;
        }

        private static bool PlayerHasArchersTunic()
        {
            var player = Game.Instance?.Player;
            var inventoryHasTunic = player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == ArchersTunicGuid) == true;
            if (inventoryHasTunic)
            {
                return true;
            }

            return player?.AllCharacters
                ?.Any(unit => unit?.Body?.CurrentEquipmentSlots
                    ?.Any(slot => slot?.MaybeItem?.Blueprint?.AssetGuid == ArchersTunicGuid) == true) == true;
        }

        private static bool PlayerHasItem(BlueprintGuid itemGuid)
        {
            var player = Game.Instance?.Player;
            var inventoryHasItem = player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == itemGuid) == true;
            if (inventoryHasItem)
            {
                return true;
            }

            return player?.AllCharacters
                ?.Any(unit => unit?.Body?.CurrentEquipmentSlots
                    ?.Any(slot => slot?.MaybeItem?.Blueprint?.AssetGuid == itemGuid) == true) == true;
        }

        private static UnitEntityData FindBilly()
        {
            var player = Game.Instance?.Player;
            return player?.Party.FirstOrDefault(IsBilly)
                   ?? player?.ActiveCompanions.FirstOrDefault(IsBilly)
                   ?? player?.AllCharacters.FirstOrDefault(IsBilly)
                   ?? Game.Instance?.State?.LoadedAreaState?.AllEntityData
                       ?.OfType<UnitEntityData>()
                       ?.FirstOrDefault(IsBilly);
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint != null
                   && (unit.Descriptor.Blueprint.AssetGuid == BillyGuid
                       || unit.Descriptor.Blueprint.AssetGuid == BillyStandInGuid);
        }
    }
}
