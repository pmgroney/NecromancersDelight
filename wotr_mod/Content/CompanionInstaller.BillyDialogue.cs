using System;
using System.Collections.Generic;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using wotr_mod.Infrastructure;
using wotr_mod.Patches;

namespace wotr_mod.Content
{
    internal sealed partial class CompanionInstaller
    {
        private BlueprintDialog EnsureBillyDialog(BlueprintUnit speaker)
        {
            var story = EnsureBillyStory(speaker);
            var recruitedFlag = EnsureBillyRecruitedFlag();
            var act1RecordDialogSeenFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct1RecordDialogSeen,
                "WotrMod_BillyAct1RecordDialogSeen");
            var act1RecordDialogPendingFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct1RecordDialogPending,
                "WotrMod_BillyAct1RecordDialogPending");
            var act1TunicDialogSeenFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct1TunicDialogSeen,
                "WotrMod_BillyAct1TunicDialogSeen");
            var act1TunicDialogPendingFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct1TunicDialogPending,
                "WotrMod_BillyAct1TunicDialogPending");
            var act2BowDialogSeenFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct2BowDialogSeen,
                "WotrMod_BillyAct2BowDialogSeen");
            var act2BowDialogPendingFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct2BowDialogPending,
                "WotrMod_BillyAct2BowDialogPending");
            var act2ArmorDialogSeenFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct2ArmorDialogSeen,
                "WotrMod_BillyAct2ArmorDialogSeen");
            var act2ArmorDialogPendingFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct2ArmorDialogPending,
                "WotrMod_BillyAct2ArmorDialogPending");
            var act3ArmorDialogSeenFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct3ArmorDialogSeen,
                "WotrMod_BillyAct3ArmorDialogSeen");
            var act3ArmorDialogPendingFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct3ArmorDialogPending,
                "WotrMod_BillyAct3ArmorDialogPending");
            var act3BowDialogSeenFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct3BowDialogSeen,
                "WotrMod_BillyAct3BowDialogSeen");
            var act3BowDialogPendingFlag = EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyAct3BowDialogPending,
                "WotrMod_BillyAct3BowDialogPending");
            var dialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyDialog,
                "WotrMod_BillyDialog",
                "Ciar zombie dialog");
            var greeting = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyGreetingCue,
                "WotrMod_BillyGreetingCue",
                "Ciar zombie greeting cue");
            var hubGreeting = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyHubGreetingCue,
                "WotrMod_BillyHubGreetingCue",
                "Ciar zombie greeting cue");
            var joinCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyJoinCue,
                "WotrMod_BillyJoinCue",
                "Ciar zombie greeting cue");
            var answers = GetOrClone<BlueprintAnswersList>(
                GameBlueprintIds.Dialogs.CiarZombieAnswers,
                ModBlueprintIds.Dialogs.BillyAnswers,
                "WotrMod_BillyAnswers",
                "Ciar zombie answers");
            var act1RecordDialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyAct1RecordDialog,
                "WotrMod_BillyAct1RecordDialog",
                "Ciar zombie dialog");
            var act1TunicDialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyAct1TunicDialog,
                "WotrMod_BillyAct1TunicDialog",
                "Ciar zombie dialog");
            var act2BowDialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyAct2BowDialog,
                "WotrMod_BillyAct2BowDialog",
                "Ciar zombie dialog");
            var act2ArmorDialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyAct2ArmorDialog,
                "WotrMod_BillyAct2ArmorDialog",
                "Ciar zombie dialog");
            var act3ArmorDialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyAct3ArmorDialog,
                "WotrMod_BillyAct3ArmorDialog",
                "Ciar zombie dialog");
            var act3BowDialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyAct3BowDialog,
                "WotrMod_BillyAct3BowDialog",
                "Ciar zombie dialog");
            var leaveAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyLeaveAnswer,
                "WotrMod_BillyLeaveAnswer",
                "Ciar zombie leave answer");
            var joinAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyJoinAnswer,
                "WotrMod_BillyJoinAnswer",
                "Ciar zombie leave answer");
            var whatAreYouCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyWhatAreYouCue,
                "WotrMod_BillyWhatAreYouCue",
                "Ciar zombie greeting cue");
            var whyHereCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyWhyHereCue,
                "WotrMod_BillyWhyHereCue",
                "Ciar zombie greeting cue");
            var dangerousCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyDangerousCue,
                "WotrMod_BillyDangerousCue",
                "Ciar zombie greeting cue");
            var planCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyPlanCue,
                "WotrMod_BillyPlanCue",
                "Ciar zombie greeting cue");
            var whatAreYouAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyWhatAreYouAnswer,
                "WotrMod_BillyWhatAreYouAnswer",
                "Ciar zombie leave answer");
            var whyHereAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyWhyHereAnswer,
                "WotrMod_BillyWhyHereAnswer",
                "Ciar zombie leave answer");
            var dangerousAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyDangerousAnswer,
                "WotrMod_BillyDangerousAnswer",
                "Ciar zombie leave answer");
            var planAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyPlanAnswer,
                "WotrMod_BillyPlanAnswer",
                "Ciar zombie leave answer");
            var act1RecordCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct1RecordCue,
                "WotrMod_BillyAct1RecordCue",
                "Ciar zombie greeting cue");
            var act1JalmerayCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct1JalmerayCue,
                "WotrMod_BillyAct1JalmerayCue",
                "Ciar zombie greeting cue");
            var act1TunicCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct1TunicCue,
                "WotrMod_BillyAct1TunicCue",
                "Ciar zombie greeting cue");
            var act2BowCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct2BowCue,
                "WotrMod_BillyAct2BowCue",
                "Ciar zombie greeting cue");
            var act2BowUpgradeCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct2BowUpgradeCue,
                "WotrMod_BillyAct2BowUpgradeCue",
                "Ciar zombie greeting cue");
            var act2ArmorCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct2ArmorCue,
                "WotrMod_BillyAct2ArmorCue",
                "Ciar zombie greeting cue");
            var act2ArmorUpgradeCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct2ArmorUpgradeCue,
                "WotrMod_BillyAct2ArmorUpgradeCue",
                "Ciar zombie greeting cue");
            var act3ArmorCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct3ArmorCue,
                "WotrMod_BillyAct3ArmorCue",
                "Ciar zombie greeting cue");
            var act3ArmorUpgradeCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct3ArmorUpgradeCue,
                "WotrMod_BillyAct3ArmorUpgradeCue",
                "Ciar zombie greeting cue");
            var act3BowCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct3BowCue,
                "WotrMod_BillyAct3BowCue",
                "Ciar zombie greeting cue");
            var act3BowUpgradeCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyAct3BowUpgradeCue,
                "WotrMod_BillyAct3BowUpgradeCue",
                "Ciar zombie greeting cue");
            var act1JalmerayAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct1JalmerayAnswer,
                "WotrMod_BillyAct1JalmerayAnswer",
                "Ciar zombie leave answer");
            var act1JalmerayContinueAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct1JalmerayContinueAnswer,
                "WotrMod_BillyAct1JalmerayContinueAnswer",
                "Ciar zombie leave answer");
            var act1RecordAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct1RecordAnswer,
                "WotrMod_BillyAct1RecordAnswer",
                "Ciar zombie leave answer");
            var act1TunicAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct1TunicAnswer,
                "WotrMod_BillyAct1TunicAnswer",
                "Ciar zombie leave answer");
            var act2BowAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct2BowAnswer,
                "WotrMod_BillyAct2BowAnswer",
                "Ciar zombie leave answer");
            var act2ArmorAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct2ArmorAnswer,
                "WotrMod_BillyAct2ArmorAnswer",
                "Ciar zombie leave answer");
            var act3ArmorAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct3ArmorAnswer,
                "WotrMod_BillyAct3ArmorAnswer",
                "Ciar zombie leave answer");
            var act3BowAnswer = GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                ModBlueprintIds.Dialogs.BillyAct3BowAnswer,
                "WotrMod_BillyAct3BowAnswer",
                "Ciar zombie leave answer");

            dialog.FirstCue = CreateCueSelection(greeting, hubGreeting);
            dialog.Conditions = new ConditionsChecker();
            dialog.StartActions = new ActionList();
            dialog.FinishActions = new ActionList();
            dialog.ReplaceActions = new ActionList();

            greeting.Text = _localization.Text(LocalizationIds.Mod.BillyGreeting);
            greeting.Conditions = CreateBillyRecruitedConditions(speaker, not: true);
            greeting.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(greeting.Speaker, speaker);
            greeting.OnShow = new ActionList();
            greeting.OnStop = new ActionList();
            greeting.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(answers)
            };
            greeting.Continue = CreateEmptyCueSelection();

            hubGreeting.Text = _localization.Text(LocalizationIds.Mod.BillyHubGreeting);
            hubGreeting.Conditions = CreateBillyRecruitedConditions(speaker, not: false);
            hubGreeting.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(hubGreeting.Speaker, speaker);
            hubGreeting.OnShow = new ActionList();
            hubGreeting.OnStop = new ActionList();
            hubGreeting.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(answers)
            };
            hubGreeting.Continue = CreateEmptyCueSelection();
            hubGreeting.ShowOnce = false;
            hubGreeting.ShowOnceCurrentDialog = false;

            joinCue.Text = _localization.Text(LocalizationIds.Mod.BillyJoinCue);
            joinCue.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(joinCue.Speaker, speaker);
            joinCue.OnShow = new ActionList();
            joinCue.OnStop = CreateRecruitActions(speaker, story, recruitedFlag);
            joinCue.Answers = new List<BlueprintAnswerBaseReference>();
            joinCue.Continue = CreateEmptyCueSelection();
            joinCue.ShowOnce = false;
            joinCue.ShowOnceCurrentDialog = false;

            answers.Conditions = new ConditionsChecker();
            answers.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(whatAreYouAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(whyHereAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(dangerousAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(planAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act1JalmerayAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act1RecordAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act1TunicAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act2BowAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act2ArmorAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act3ArmorAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(act3BowAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(joinAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(leaveAnswer)
            };

            ConfigureBillyInfoCue(whatAreYouCue, LocalizationIds.Mod.BillyWhatAreYouCue, speaker, answers);
            ConfigureBillyInfoCue(whyHereCue, LocalizationIds.Mod.BillyWhyHereCue, speaker, answers);
            ConfigureBillyInfoCue(dangerousCue, LocalizationIds.Mod.BillyDangerousCue, speaker, answers);
            ConfigureBillyInfoCue(planCue, LocalizationIds.Mod.BillyPlanCue, speaker, answers);
            ConfigureBillyInfoCue(act1JalmerayCue, LocalizationIds.Mod.BillyAct1JalmerayCue, speaker, answers);
            ConfigureBillyInfoCue(act1RecordCue, LocalizationIds.Mod.BillyAct1RecordCue, speaker, answers);
            ConfigureBillyInfoCue(act1TunicCue, LocalizationIds.Mod.BillyAct1TunicCue, speaker, answers);
            ConfigureBillyInfoCue(act2BowCue, LocalizationIds.Mod.BillyAct2BowCue, speaker, answers);
            ConfigureBillyNarrationCue(act2BowUpgradeCue, LocalizationIds.Mod.BillyAct2BowUpgradeCue);
            ConfigureBillyInfoCue(act2ArmorCue, LocalizationIds.Mod.BillyAct2ArmorCue, speaker, answers);
            ConfigureBillyNarrationCue(act2ArmorUpgradeCue, LocalizationIds.Mod.BillyAct2ArmorUpgradeCue);
            ConfigureBillyInfoCue(act3ArmorCue, LocalizationIds.Mod.BillyAct3ArmorCue, speaker, answers);
            ConfigureBillyNarrationCue(act3ArmorUpgradeCue, LocalizationIds.Mod.BillyAct3ArmorUpgradeCue);
            ConfigureBillyInfoCue(act3BowCue, LocalizationIds.Mod.BillyAct3BowCue, speaker, answers);
            ConfigureBillyNarrationCue(act3BowUpgradeCue, LocalizationIds.Mod.BillyAct3BowUpgradeCue);
            ConfigureBillyQuestionAnswer(
                whatAreYouAnswer,
                LocalizationIds.Mod.BillyWhatAreYouAnswer,
                whatAreYouCue);
            ConfigureBillyQuestionAnswer(whyHereAnswer, LocalizationIds.Mod.BillyWhyHereAnswer, whyHereCue);
            ConfigureBillyQuestionAnswer(dangerousAnswer, LocalizationIds.Mod.BillyDangerousAnswer, dangerousCue);
            ConfigureBillyQuestionAnswer(planAnswer, LocalizationIds.Mod.BillyPlanAnswer, planCue);
            ConfigureBillyAct1JalmerayAnswer(act1JalmerayAnswer, act1JalmerayCue, act1JalmerayContinueAnswer);
            ConfigureBillyQuestPendingAnswer(
                act1RecordAnswer,
                LocalizationIds.Mod.BillyAct1RecordAnswer,
                act1RecordCue,
                act1RecordDialogPendingFlag,
                act1RecordDialogSeenFlag);
            ConfigureBillyQuestCueExit(act1RecordCue, act1JalmerayContinueAnswer);
            ConfigureBillyQuestPendingAnswer(
                act1TunicAnswer,
                LocalizationIds.Mod.BillyAct1TunicAnswer,
                act1TunicCue,
                act1TunicDialogPendingFlag,
                act1TunicDialogSeenFlag);
            ConfigureBillyQuestCueExit(act1TunicCue, act1JalmerayContinueAnswer);
            ConfigureBillySingleCueDialog(act1RecordDialog, act1RecordCue);
            ConfigureBillySingleCueDialog(act1TunicDialog, act1TunicCue);
            ConfigureBillyQuestPendingAnswer(
                act2BowAnswer,
                LocalizationIds.Mod.BillyAct2BowAnswer,
                act2BowCue,
                act2BowDialogPendingFlag,
                act2BowDialogSeenFlag);
            ConfigureBillyQuestCueContinue(act2BowCue, act2BowUpgradeCue);
            ConfigureBillyQuestCueExit(act2BowUpgradeCue, act1JalmerayContinueAnswer);
            ConfigureBillyQuestPendingAnswer(
                act2ArmorAnswer,
                LocalizationIds.Mod.BillyAct2ArmorAnswer,
                act2ArmorCue,
                act2ArmorDialogPendingFlag,
                act2ArmorDialogSeenFlag);
            ConfigureBillyQuestCueContinue(act2ArmorCue, act2ArmorUpgradeCue);
            ConfigureBillyQuestCueExit(act2ArmorUpgradeCue, act1JalmerayContinueAnswer);
            ConfigureBillyQuestPendingAnswer(
                act3ArmorAnswer,
                LocalizationIds.Mod.BillyAct3ArmorAnswer,
                act3ArmorCue,
                act3ArmorDialogPendingFlag,
                act3ArmorDialogSeenFlag);
            ConfigureBillyQuestCueContinue(act3ArmorCue, act3ArmorUpgradeCue);
            ConfigureBillyQuestCueExit(act3ArmorUpgradeCue, act1JalmerayContinueAnswer);
            ConfigureBillyQuestPendingAnswer(
                act3BowAnswer,
                LocalizationIds.Mod.BillyAct3BowAnswer,
                act3BowCue,
                act3BowDialogPendingFlag,
                act3BowDialogSeenFlag);
            ConfigureBillyQuestCueContinue(act3BowCue, act3BowUpgradeCue);
            ConfigureBillyQuestCueExit(act3BowUpgradeCue, act1JalmerayContinueAnswer);
            ConfigureBillySingleCueDialog(act2BowDialog, act2BowCue);
            ConfigureBillySingleCueDialog(act2ArmorDialog, act2ArmorCue);
            ConfigureBillySingleCueDialog(act3ArmorDialog, act3ArmorCue);
            ConfigureBillySingleCueDialog(act3BowDialog, act3BowCue);

            joinAnswer.Text = _localization.Text(LocalizationIds.Mod.BillyJoinAnswer);
            joinAnswer.ShowConditions = CreateBillyRecruitedConditions(speaker, not: true);
            joinAnswer.SelectConditions = new ConditionsChecker();
            joinAnswer.OnSelect = new ActionList();
            joinAnswer.NextCue = CreateCueSelection(joinCue);
            joinAnswer.ShowOnce = false;
            joinAnswer.ShowOnceCurrentDialog = false;
            joinAnswer.RequireValidCue = false;
            joinAnswer.AddToHistory = true;

            leaveAnswer.Text = _localization.Text(LocalizationIds.Mod.BillyLeaveAnswer);
            leaveAnswer.ShowConditions = new ConditionsChecker();
            leaveAnswer.SelectConditions = new ConditionsChecker();
            leaveAnswer.OnSelect = new ActionList();
            leaveAnswer.NextCue = CreateEmptyCueSelection();
            leaveAnswer.ShowOnce = false;
            leaveAnswer.ShowOnceCurrentDialog = false;
            leaveAnswer.RequireValidCue = false;
            leaveAnswer.AddToHistory = true;

            return dialog;
        }

        private BlueprintUnlockableFlag EnsureBillyRecruitedFlag()
        {
            return EnsureBillyQuestDialogSeenFlag(
                ModBlueprintIds.Flags.BillyRecruited,
                "WotrMod_BillyRecruited");
        }

        private BlueprintUnlockableFlag EnsureBillyQuestDialogSeenFlag(string guid, string name)
        {
            var flag = _blueprints.Get<BlueprintUnlockableFlag>(guid);
            if (flag != null)
            {
                return flag;
            }

            flag = new BlueprintUnlockableFlag
            {
                name = name,
                AssetGuid = BlueprintGuid.Parse(guid)
            };
            _blueprints.AddCachedBlueprint(guid, flag);
            return flag;
        }

        private BlueprintDialog EnsureBillyBowQuestDialog(BlueprintUnit speaker)
        {
            var dialog = GetOrClone<BlueprintDialog>(
                GameBlueprintIds.Dialogs.CiarZombieDialog,
                ModBlueprintIds.Dialogs.BillyBowQuestDialog,
                "WotrMod_BillyBowQuestDialog",
                "Ciar zombie dialog");
            var startCue = GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                ModBlueprintIds.Dialogs.BillyBowQuestStartCue,
                "WotrMod_BillyBowQuestStartCue",
                "Ciar zombie greeting cue");
            var answers = GetOrClone<BlueprintAnswersList>(
                GameBlueprintIds.Dialogs.CiarZombieAnswers,
                ModBlueprintIds.Dialogs.BillyBowQuestAnswers,
                "WotrMod_BillyBowQuestAnswers",
                "Ciar zombie answers");
            var templeAnswer = CreateBillyBowQuestAnswer(
                ModBlueprintIds.Dialogs.BillyBowQuestTempleAnswer,
                "WotrMod_BillyBowQuestTempleAnswer");
            var templeCue = CreateBillyBowQuestCue(
                ModBlueprintIds.Dialogs.BillyBowQuestTempleCue,
                "WotrMod_BillyBowQuestTempleCue");
            var hosillaAnswer = CreateBillyBowQuestAnswer(
                ModBlueprintIds.Dialogs.BillyBowQuestHosillaAnswer,
                "WotrMod_BillyBowQuestHosillaAnswer");
            var hosillaCue = CreateBillyBowQuestCue(
                ModBlueprintIds.Dialogs.BillyBowQuestHosillaCue,
                "WotrMod_BillyBowQuestHosillaCue");
            var disciplineAnswer = CreateBillyBowQuestAnswer(
                ModBlueprintIds.Dialogs.BillyBowQuestDisciplineAnswer,
                "WotrMod_BillyBowQuestDisciplineAnswer");
            var disciplineCue = CreateBillyBowQuestCue(
                ModBlueprintIds.Dialogs.BillyBowQuestDisciplineCue,
                "WotrMod_BillyBowQuestDisciplineCue");
            var endAnswer = CreateBillyBowQuestAnswer(
                ModBlueprintIds.Dialogs.BillyBowQuestEndAnswer,
                "WotrMod_BillyBowQuestEndAnswer");
            var endCue = CreateBillyBowQuestCue(
                ModBlueprintIds.Dialogs.BillyBowQuestEndCue,
                "WotrMod_BillyBowQuestEndCue");

            dialog.FirstCue = CreateCueSelection(startCue);
            dialog.Conditions = new ConditionsChecker();
            dialog.StartActions = new ActionList();
            dialog.FinishActions = new ActionList();
            dialog.ReplaceActions = new ActionList();

            startCue.Text = _localization.Text(LocalizationIds.Mod.BillyBowQuestStartCue);
            ConfigureBillyBowQuestCue(startCue, speaker, answers);

            answers.Conditions = new ConditionsChecker();
            answers.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(templeAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(hosillaAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(disciplineAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(endAnswer)
            };

            ConfigureBillyBowQuestAnswer(templeAnswer, LocalizationIds.Mod.BillyBowQuestTempleAnswer, templeCue);
            ConfigureBillyBowQuestResponse(templeCue, LocalizationIds.Mod.BillyBowQuestTempleCue, speaker, answers);
            ConfigureBillyBowQuestAnswer(hosillaAnswer, LocalizationIds.Mod.BillyBowQuestHosillaAnswer, hosillaCue);
            ConfigureBillyBowQuestResponse(hosillaCue, LocalizationIds.Mod.BillyBowQuestHosillaCue, speaker, answers);
            ConfigureBillyBowQuestAnswer(disciplineAnswer, LocalizationIds.Mod.BillyBowQuestDisciplineAnswer, disciplineCue);
            ConfigureBillyBowQuestResponse(disciplineCue, LocalizationIds.Mod.BillyBowQuestDisciplineCue, speaker, answers);
            ConfigureBillyBowQuestAnswer(endAnswer, LocalizationIds.Mod.BillyBowQuestEndAnswer, endCue);
            ConfigureBillyBowQuestResponse(endCue, LocalizationIds.Mod.BillyBowQuestEndCue, speaker, null);

            return dialog;
        }

        private BlueprintAnswer CreateBillyBowQuestAnswer(string guid, string name)
        {
            return GetOrClone<BlueprintAnswer>(
                GameBlueprintIds.Dialogs.CiarZombieLeaveAnswer,
                guid,
                name,
                "Ciar zombie leave answer");
        }

        private BlueprintCue CreateBillyBowQuestCue(string guid, string name)
        {
            return GetOrClone<BlueprintCue>(
                GameBlueprintIds.Dialogs.CiarZombieGreetingCue,
                guid,
                name,
                "Ciar zombie greeting cue");
        }

        private void ConfigureBillyBowQuestCue(
            BlueprintCue cue,
            BlueprintUnit speaker,
            BlueprintAnswersList answers)
        {
            cue.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(cue.Speaker, speaker);
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(answers)
            };
            cue.Continue = CreateEmptyCueSelection();
            cue.ShowOnce = false;
            cue.ShowOnceCurrentDialog = false;
        }

        private void ConfigureBillyBowQuestResponse(
            BlueprintCue cue,
            string localizationKey,
            BlueprintUnit speaker,
            BlueprintAnswersList answers)
        {
            cue.Text = _localization.Text(localizationKey);
            cue.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(cue.Speaker, speaker);
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Answers = answers == null
                ? new List<BlueprintAnswerBaseReference>()
                : new List<BlueprintAnswerBaseReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(answers)
                };
            cue.Continue = CreateEmptyCueSelection();
            cue.ShowOnce = false;
            cue.ShowOnceCurrentDialog = false;
        }

        private void ConfigureBillyBowQuestAnswer(BlueprintAnswer answer, string localizationKey, BlueprintCue cue)
        {
            answer.Text = _localization.Text(localizationKey);
            answer.ShowConditions = new ConditionsChecker();
            answer.SelectConditions = new ConditionsChecker();
            answer.OnSelect = new ActionList();
            answer.NextCue = CreateCueSelection(cue);
            answer.ShowOnce = false;
            answer.ShowOnceCurrentDialog = false;
            answer.RequireValidCue = false;
            answer.AddToHistory = true;
        }

        private void ConfigureBillyInfoCue(
            BlueprintCue cue,
            string localizationKey,
            BlueprintUnit speaker,
            BlueprintAnswersList answers)
        {
            cue.Text = _localization.Text(localizationKey);
            cue.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(cue.Speaker, speaker);
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(answers)
            };
            cue.Continue = CreateEmptyCueSelection();
            cue.ShowOnce = false;
            cue.ShowOnceCurrentDialog = false;
        }

        private void ConfigureBillyQuestionAnswer(BlueprintAnswer answer, string localizationKey, BlueprintCue cue)
        {
            answer.Text = _localization.Text(localizationKey);
            answer.ShowConditions = new ConditionsChecker();
            answer.SelectConditions = new ConditionsChecker();
            answer.OnSelect = new ActionList();
            answer.NextCue = CreateCueSelection(cue);
            answer.ShowOnce = false;
            answer.ShowOnceCurrentDialog = false;
            answer.RequireValidCue = false;
            answer.AddToHistory = true;
        }

        private void ConfigureBillyAct1JalmerayAnswer(
            BlueprintAnswer answer,
            BlueprintCue cue,
            BlueprintAnswer continueAnswer)
        {
            ConfigureBillyQuestionAnswer(answer, LocalizationIds.Mod.BillyAct1JalmerayAnswer, cue);
            answer.ShowConditions = new ConditionsChecker
            {
                Operation = Operation.And,
                Conditions = new Condition[]
                {
                    CreateObjectiveStatusCondition(
                        ModBlueprintIds.QuestObjectives.BillyConditionInvestigate,
                        "WotrMod_BillyAct1JalmerayInvestigateStarted",
                        QuestObjectiveState.Started),
                    CreateCurrentAreaCondition(
                        GameBlueprintIds.Areas.DefendersHeart,
                        "WotrMod_BillyAct1JalmerayInDefendersHeart")
                }
            };
            cue.OnStop = CreateAct1JalmerayAdvanceActions();
            ConfigureBillyExitAnswer(continueAnswer, LocalizationIds.Mod.BillyAct1JalmerayContinueAnswer);
            ConfigureBillyQuestCueExit(cue, continueAnswer);
        }

        private void ConfigureBillyExitAnswer(BlueprintAnswer answer, string localizationKey)
        {
            answer.Text = _localization.Text(localizationKey);
            answer.ShowConditions = new ConditionsChecker();
            answer.SelectConditions = new ConditionsChecker();
            answer.OnSelect = new ActionList();
            answer.NextCue = CreateEmptyCueSelection();
            answer.ShowOnce = false;
            answer.ShowOnceCurrentDialog = false;
            answer.RequireValidCue = false;
            answer.AddToHistory = true;
        }

        private void ConfigureBillyQuestPendingAnswer(
            BlueprintAnswer answer,
            string localizationKey,
            BlueprintCue cue,
            BlueprintUnlockableFlag pendingFlag,
            BlueprintUnlockableFlag seenFlag)
        {
            ConfigureBillyQuestionAnswer(answer, localizationKey, cue);
            answer.ShowConditions = new ConditionsChecker
            {
                Operation = Operation.And,
                Conditions = new Condition[]
                {
                    CreateFlagUnlockedCondition(
                        pendingFlag,
                        "WotrMod_BillyQuestDialoguePending_" + cue.name,
                        not: false),
                    CreateFlagUnlockedCondition(
                        seenFlag,
                        "WotrMod_BillyQuestDialogueSeen_" + cue.name,
                        not: true)
                }
            };
            cue.OnStop = new ActionList
            {
                Actions = new GameAction[]
                {
                    CreateUnlockFlagAction(seenFlag, "WotrMod_MarkBillyQuestDialogueSeen_" + cue.name)
                }
            };
        }

        private static void ConfigureBillyQuestCueExit(BlueprintCue cue, BlueprintAnswer continueAnswer)
        {
            cue.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(continueAnswer)
            };
            cue.Continue = CreateEmptyCueSelection();
        }

        private void ConfigureBillyNarrationCue(BlueprintCue cue, string localizationKey)
        {
            cue.Text = _localization.Text(localizationKey);
            cue.Speaker = new DialogSpeaker
            {
                MoveCamera = true,
                NoSpeaker = true
            };
            cue.OnShow = new ActionList();
            cue.OnStop = new ActionList();
            cue.Answers = new List<BlueprintAnswerBaseReference>();
            cue.Continue = CreateEmptyCueSelection();
            cue.ShowOnce = false;
            cue.ShowOnceCurrentDialog = false;
        }

        private static void ConfigureBillyQuestCueContinue(BlueprintCue cue, BlueprintCue nextCue)
        {
            cue.Answers = new List<BlueprintAnswerBaseReference>();
            cue.Continue = CreateCueSelection(nextCue);
        }

        private static void ConfigureBillySingleCueDialog(BlueprintDialog dialog, BlueprintCue cue)
        {
            dialog.FirstCue = CreateCueSelection(cue);
            dialog.Conditions = new ConditionsChecker();
            dialog.StartActions = new ActionList();
            dialog.FinishActions = new ActionList();
            dialog.ReplaceActions = new ActionList();
        }

        private Condition CreateObjectiveStatusCondition(
            string objectiveGuid,
            string conditionName,
            QuestObjectiveState state)
        {
            var objectiveStatus = new ObjectiveStatus
            {
                name = "$ObjectiveStatus$" + conditionName,
                Not = false,
                State = state
            };
            SetField(
                objectiveStatus,
                "m_QuestObjective",
                BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(
                    _blueprints.Require<BlueprintQuestObjective>(objectiveGuid, conditionName + " objective")));

            return objectiveStatus;
        }

        private Condition CreateFlagUnlockedCondition(
            BlueprintUnlockableFlag flag,
            string conditionName,
            bool not)
        {
            var flagUnlocked = new FlagUnlocked
            {
                name = "$FlagUnlocked$" + conditionName,
                Not = not
            };
            SetField(
                flagUnlocked,
                "m_ConditionFlag",
                BlueprintReferenceBase.CreateTyped<BlueprintUnlockableFlagReference>(flag));

            return flagUnlocked;
        }

        private Condition CreateCurrentAreaCondition(string areaGuid, string conditionName)
        {
            var areaCondition = new CurrentAreaIs
            {
                name = "$CurrentAreaIs$" + conditionName,
                Not = false
            };
            SetField(
                areaCondition,
                "m_Area",
                BlueprintReferenceBase.CreateTyped<BlueprintAreaReference>(
                    _blueprints.Require<BlueprintArea>(areaGuid, conditionName + " area")));

            return areaCondition;
        }

        private ActionList CreateAct1JalmerayAdvanceActions()
        {
            var investigateObjective = _blueprints.Require<BlueprintQuestObjective>(
                ModBlueprintIds.QuestObjectives.BillyConditionInvestigate,
                "Billy investigate objective");
            var jalmerayObjective = _blueprints.Require<BlueprintQuestObjective>(
                ModBlueprintIds.QuestObjectives.BillyConditionAct1JalmerayLead,
                "Billy Act 1 Jalmeray objective");
            var completeInvestigate = new SetObjectiveStatus
            {
                name = "$SetObjectiveStatus$WotrMod_BillyAct1CompleteInvestigate",
                Status = Kingmaker.Designers.Quests.Common.SummonPoolCountTrigger.ObjectiveStatus.Complete,
                StartObjectiveIfNone = false
            };
            SetField(
                completeInvestigate,
                "m_Objective",
                BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(investigateObjective));

            var giveJalmerayObjective = new GiveObjective
            {
                name = "$GiveObjective$WotrMod_BillyAct1JalmerayLead"
            };
            SetField(
                giveJalmerayObjective,
                "m_Objective",
                BlueprintReferenceBase.CreateTyped<BlueprintQuestObjectiveReference>(jalmerayObjective));

            return new ActionList
            {
                Actions = new GameAction[]
                {
                    completeInvestigate,
                    giveJalmerayObjective
                }
            };
        }

        private static ConditionsChecker CreateBillyRecruitedConditions(BlueprintUnit companion, bool not)
        {
            var condition = new BillyRecruitedCondition
            {
                name = "$BillyRecruitedCondition$WotrMod_Billy",
                Not = not
            };

            return new ConditionsChecker
            {
                Operation = Operation.And,
                Conditions = new Condition[]
                {
                    condition
                }
            };
        }

    }
}
