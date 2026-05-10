using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
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
        private static readonly BlueprintGuid BowGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.NeophytesLongbowOfDiscipline);

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
        }

        public void HandleItemsAdded(ItemsCollection collection, ItemEntity item, int count)
        {
            if (collection != Game.Instance?.Player?.Inventory || item?.Blueprint?.AssetGuid != BowGuid)
            {
                return;
            }

            TryStartBowQuest();
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
            quest.Title = CreateText(LocalizationIds.Mod.BillyConditionQuestTitle);
            quest.Description = CreateText(LocalizationIds.Mod.BillyConditionQuestDescription);
            quest.CompletionText = CreateText(LocalizationIds.Mod.BillyConditionQuestCompletion);
            SetField(quest, "m_Group", QuestGroupId.CompanionQuests);
            SetField(quest, "m_DescriptionPriority", 0);
            SetField(quest, "m_Type", QuestType.Normal);
            SetField(quest, "m_LastChapter", 1);
            SetField(
                quest,
                "m_Objectives",
                new List<BlueprintQuestObjectiveReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(objective)
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
            SetField(objective, "m_NextObjectives", new List<BlueprintQuestObjectiveReference>());
            SetField(
                objective,
                "m_Quest",
                quest == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintQuestReference>(quest));
            SetField(objective, "m_Type", BlueprintQuestObjective.Type.Objective);

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
            return Game.Instance?.Player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == BowGuid) == true;
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
            return unit?.Descriptor?.Blueprint != null && unit.Descriptor.Blueprint.AssetGuid == BillyGuid;
        }
    }
}
