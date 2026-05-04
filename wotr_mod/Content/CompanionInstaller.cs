using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.ResourceManagement;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using Kingmaker.Visual.Sound;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Patches;

namespace wotr_mod.Content
{
    internal sealed class CompanionInstaller : IContentModule
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly string _modPath;

        public CompanionInstaller(BlueprintTool blueprints, LocalizationTool localization, string modPath)
        {
            _blueprints = blueprints;
            _localization = localization;
            _modPath = modPath;
        }

        public string Name => "Companions";

        private static readonly string[] BillyIdleBanterLines =
        {
            "Breathing is optional. Still deciding if I miss it.",
            "Step. Aim. Release. Repeat. Eternity's great for practice.",
            "Irori teaches perfection. I just have... more time than most.",
            "You ever notice how the living waste a lot of motion?",
            "No heartbeat. No distractions.",
            "Still me. Just... quieter.",
            "I don't get tired. Turns out that's a competitive advantage.",
            "I used to need rest. Now I just need direction.",
            "Focus is easier when your body stops complaining.",
            "I should probably be more concerned about this situation.",
            "Good posture is important. Even post-mortem."
        };

        private static readonly string[] BillyMovementLines =
        {
            "Careful. I don't creak, but I still sneak.",
            "Every step is intentional.",
            "Stay sharp. I already dulled once.",
            "Quiet. Let them make the mistakes.",
            "Positioning wins fights.",
            "Discipline over speed.",
            "I don't rush. I arrive."
        };

        private static readonly string[] BillyCombatStartLines =
        {
            "Ah. Practical application.",
            "Let's improve.",
            "Targets acquired.",
            "Focus.",
            "Try not to die. It's inconvenient.",
            "I'll demonstrate.",
            "Form over fury."
        };

        private static readonly string[] BillyRangedAttackLines =
        {
            "Breathe--oh. Right.",
            "Release.",
            "Stillness. Then strike.",
            "Predictable.",
            "You moved. That helped.",
            "Center mass.",
            "Efficiency.",
            "Missed. Noted."
        };

        private static readonly string[] BillyOnHitLines =
        {
            "There it is.",
            "Correct.",
            "Better.",
            "That felt right.",
            "Refinement.",
            "Precision matters."
        };

        private static readonly string[] BillyOnKillLines =
        {
            "Rest. Properly this time.",
            "Cycle corrected.",
            "You can stop now.",
            "That's one less problem.",
            "Final form achieved.",
            "Still improving."
        };

        private static readonly string[] BillyTakingDamageLines =
        {
            "Not ideal.",
            "Structural integrity compromised.",
            "I felt that. Oddly.",
            "Adjustment required.",
            "Pain is... inconsistent."
        };

        private static readonly string[] BillyLowHealthLines =
        {
            "Pieces are becoming optional.",
            "I should address this.",
            "This is inefficient.",
            "Losing cohesion."
        };

        private static readonly string[] BillyBuffingLines =
        {
            "Enhancement accepted.",
            "Clarity.",
            "Alignment maintained.",
            "Focus restored.",
            "Irori guides."
        };

        private static readonly string[] BillyPartyBanterLines =
        {
            "If I fall apart, just point me toward the enemy first.",
            "I don't sleep, so I'll take watch. Forever, apparently.",
            "Don't worry--I'm very stable. Structurally.",
            "I've stopped worrying about dying. Big time saver.",
            "If you hear rattling, that's normal."
        };

        private static readonly string[] BillyIroriFlavorLines =
        {
            "Perfection isn't a destination. Good thing.",
            "Discipline survives death. That's reassuring.",
            "Mastery doesn't require a pulse.",
            "The body failed. The will didn't.",
            "Irori didn't promise this... but I'll make use of it."
        };

        private static readonly Dictionary<string, string> BillyBarkLocalizationKeys = BuildBillyBarkLocalizationKeys();

        public void RegisterLocalization()
        {
        }

        public void Install()
        {
            EnsureUndeadCiarCompanion();
        }

        private BlueprintUnit EnsureUndeadCiarCompanion()
        {
            var existing = _blueprints.Get<BlueprintUnit>(ModBlueprintIds.Units.UndeadCiarCompanion);
            var undeadCiar = _blueprints.Require<BlueprintUnit>(
                GameBlueprintIds.Units.CiarUndead,
                "Undead Ciar unit");
            var companionCiar = _blueprints.Require<BlueprintUnit>(
                GameBlueprintIds.Units.CiarCompanion,
                "Ciar companion unit");

            var unit = existing ?? _blueprints.CloneBlueprint(
                undeadCiar,
                ModBlueprintIds.Units.UndeadCiarCompanion,
                "WotrMod_BillyCompanion");

            var dialog = EnsureBillyDialog(unit);
            CopyCompanionShell(unit, companionCiar, undeadCiar, dialog);
            SetUnitName(unit, LocalizationIds.Mod.BillyName);
            ConfigureBillyUnit(unit);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Units.UndeadCiarCompanion, unit);
            }

            return unit;
        }

        private BlueprintDialog EnsureBillyDialog(BlueprintUnit speaker)
        {
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

            dialog.FirstCue = CreateCueSelection(greeting);
            dialog.Conditions = new ConditionsChecker();
            dialog.StartActions = new ActionList();
            dialog.FinishActions = new ActionList();
            dialog.ReplaceActions = new ActionList();

            greeting.Text = _localization.Text(LocalizationIds.Mod.BillyGreeting);
            greeting.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(greeting.Speaker, speaker);
            greeting.OnShow = new ActionList();
            greeting.OnStop = new ActionList();
            greeting.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(answers)
            };
            greeting.Continue = CreateEmptyCueSelection();

            joinCue.Text = _localization.Text(LocalizationIds.Mod.BillyJoinCue);
            joinCue.Speaker = new DialogSpeaker();
            SetSpeakerBlueprint(joinCue.Speaker, speaker);
            joinCue.OnShow = new ActionList();
            joinCue.OnStop = CreateRecruitActions(speaker);
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
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(joinAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(leaveAnswer)
            };

            ConfigureBillyInfoCue(whatAreYouCue, LocalizationIds.Mod.BillyWhatAreYouCue, speaker, answers);
            ConfigureBillyInfoCue(whyHereCue, LocalizationIds.Mod.BillyWhyHereCue, speaker, answers);
            ConfigureBillyInfoCue(dangerousCue, LocalizationIds.Mod.BillyDangerousCue, speaker, answers);
            ConfigureBillyInfoCue(planCue, LocalizationIds.Mod.BillyPlanCue, speaker, answers);
            ConfigureBillyQuestionAnswer(
                whatAreYouAnswer,
                LocalizationIds.Mod.BillyWhatAreYouAnswer,
                whatAreYouCue);
            ConfigureBillyQuestionAnswer(whyHereAnswer, LocalizationIds.Mod.BillyWhyHereAnswer, whyHereCue);
            ConfigureBillyQuestionAnswer(dangerousAnswer, LocalizationIds.Mod.BillyDangerousAnswer, dangerousCue);
            ConfigureBillyQuestionAnswer(planAnswer, LocalizationIds.Mod.BillyPlanAnswer, planCue);

            joinAnswer.Text = _localization.Text(LocalizationIds.Mod.BillyJoinAnswer);
            joinAnswer.ShowConditions = new ConditionsChecker();
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

        private static ActionList CreateRecruitActions(BlueprintUnit companion)
        {
            var recruitData = new Recruit.RecruitData
            {
                NPCUnit = new DialogCurrentSpeaker(),
                MustBeInParty = false
            };
            SetField(
                recruitData,
                "m_CompanionBlueprint",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(companion));

            return new ActionList
            {
                Actions = new GameAction[]
                {
                    new Recruit
                    {
                        name = "WotrMod_RecruitBilly",
                        Recruited = new[]
                        {
                            recruitData
                        },
                        AddToParty = true,
                        MatchPlayerXpExactly = true,
                        OnRecruit = new ActionList(),
                        OnRecruitImmediate = new ActionList()
                    },
                    new BillyRecruitmentFallbackAction
                    {
                        name = "WotrMod_BillyRecruitFallback"
                    }
                }
            };
        }

        private TBlueprint GetOrClone<TBlueprint>(
            string sourceGuid,
            string cloneGuid,
            string cloneName,
            string sourceName)
            where TBlueprint : BlueprintScriptableObject
        {
            var existing = _blueprints.Get<TBlueprint>(cloneGuid);
            if (existing != null)
            {
                return existing;
            }

            var source = _blueprints.Require<TBlueprint>(sourceGuid, sourceName);
            var clone = _blueprints.CloneBlueprint(source, cloneGuid, cloneName);
            _blueprints.AddCachedBlueprint(cloneGuid, clone);
            return clone;
        }

        private void CopyCompanionShell(
            BlueprintUnit target,
            BlueprintUnit companionSource,
            BlueprintUnit conversationSource,
            BlueprintDialog dialog)
        {
            var companionComponents = companionSource.ComponentsArray ?? Array.Empty<BlueprintComponent>();
            var conversationComponents = (conversationSource.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(component => component.GetType().Name == "DialogOnClick");
            var components = companionComponents
                .Concat(conversationComponents)
                .Select(component => _blueprints.CloneComponent(component))
                .ToArray();

            foreach (var component in components.Where(component => component.GetType().Name == "DialogOnClick"))
            {
                SetDialogOnClickDialog(component, dialog);
            }

            _blueprints.SetComponents(target, components);

            CopyField(target, companionSource, "m_Faction");
            CopyField(target, companionSource, "m_Brain");
            CopyField(target, companionSource, "m_AddFacts");
            CopyField(target, companionSource, "m_AllowNonContextActions");

            target.Alignment = companionSource.Alignment;
            target.IsCheater = companionSource.IsCheater;
            target.IsFake = companionSource.IsFake;
        }

        private void ConfigureBillyUnit(BlueprintUnit unit)
        {
            unit.Strength = 16;
            unit.Dexterity = 18;
            unit.Constitution = 10;
            unit.Intelligence = 12;
            unit.Wisdom = 17;
            unit.Charisma = 14;
            unit.Alignment = Alignment.LawfulNeutral;

            var longbowProficiency = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.LongbowProficiency,
                "Longbow Proficiency");
            var shortbowProficiency = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.ShortbowProficiency,
                "Shortbow Proficiency");
            var undeadType = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.UndeadType,
                "Undead Type");
            var featureList = EnsureBillyFeatureList();
            var positiveEnergyImmunity = EnsureBillyPositiveEnergyImmunity();
            var scalingClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Cleric,
                "Cleric class");
            var scalingArchetype = _blueprints.Require<BlueprintArchetype>(
                GameBlueprintIds.Archetypes.Ecclesitheurge,
                "Ecclesitheurge archetype");
            var monkAcBonus = EnsureBillyMonkAcBonus(scalingClass, scalingArchetype);
            var wayOfTheBow = EnsureBillyWayOfTheBow(scalingClass);
            var visualSource = _blueprints.Require<BlueprintUnit>(
                GameBlueprintIds.Units.MythicLichSkeletonArcher,
                "Mythic lich skeleton archer unit");
            var startingBow = _blueprints.Require<BlueprintItemWeapon>(
                GameBlueprintIds.Items.CompositeLongbow,
                "Composite Longbow");

            SetUnitFacts(
                unit,
                featureList,
                undeadType,
                longbowProficiency,
                shortbowProficiency,
                positiveEnergyImmunity,
                monkAcBonus,
                wayOfTheBow);

            SetUnitPortrait(unit, EnsureBillyPortrait());
            CopyVisualModel(unit, visualSource);
            SetStartingEquipment(unit, startingBow);
            SetUnitBarks(unit, EnsureBillyBarks());
        }

        private BlueprintFeature EnsureBillyMonkAcBonus(
            BlueprintCharacterClass scalingClass,
            BlueprintArchetype scalingArchetype)
        {
            var unarmoredBuff = GetOrClone<BlueprintBuff>(
                GameBlueprintIds.Buffs.MonkAcBonusBuffUnarmored,
                ModBlueprintIds.Buffs.BillyMonkAcBonusBuffUnarmored,
                "WotrMod_BillyMonkACBonusBuffUnarmored",
                "Monk AC bonus unarmored buff");
            ConfigureContextRankClass(unarmoredBuff, scalingClass, scalingArchetype);

            var feature = GetOrClone<BlueprintFeature>(
                GameBlueprintIds.Features.MonkAcBonus,
                ModBlueprintIds.Features.BillyMonkAcBonus,
                "WotrMod_BillyMonkACBonus",
                "Monk AC bonus");
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.BillyIroriDisciplineName),
                _localization.Text(LocalizationIds.Mod.BillyIroriDisciplineDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.BillyIroriDisciplineDescription));
            var wisdomBuff = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Buffs.MonkAcBonusBuff,
                "Monk AC wisdom buff");
            SetAddFacts(feature, wisdomBuff, unarmoredBuff);
            return feature;
        }

        private BlueprintFeature EnsureBillyWayOfTheBow(BlueprintCharacterClass scalingClass)
        {
            var feature = GetOrClone<BlueprintFeature>(
                GameBlueprintIds.Features.ZenArcherWayOfTheBowLongbowFocus,
                ModBlueprintIds.Features.BillyWayOfTheBowLongbow,
                "WotrMod_BillyWayOfTheBowLongbow",
                "Zen Archer Way of the Bow - Longbow");
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(LocalizationIds.Mod.BillyWayOfTheBowName),
                _localization.Text(LocalizationIds.Mod.BillyWayOfTheBowDescription));
            _blueprints.SetUnitFactShortDescription(
                feature,
                _localization.Text(LocalizationIds.Mod.BillyWayOfTheBowDescription));
            ConfigureAddFeatureOnClassLevel(feature, scalingClass);
            return feature;
        }

        private static void ConfigureContextRankClass(
            BlueprintBuff buff,
            BlueprintCharacterClass scalingClass,
            BlueprintArchetype scalingArchetype)
        {
            foreach (var component in buff.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component.GetType().Name != "ContextRankConfig")
                {
                    continue;
                }

                var classReference = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(scalingClass);
                var classField = FindField(component.GetType(), "m_Class");
                if (classField?.FieldType.IsArray == true)
                {
                    classField.SetValue(component, new[] { classReference });
                }
                else
                {
                    classField?.SetValue(component, classReference);
                }

                SetField(
                    component,
                    "Archetype",
                    BlueprintReferenceBase.CreateTyped<BlueprintArchetypeReference>(scalingArchetype));
                SetField(component, "m_AdditionalArchetypes", Array.Empty<BlueprintArchetypeReference>());
            }
        }

        private static void ConfigureAddFeatureOnClassLevel(BlueprintFeature feature, BlueprintCharacterClass scalingClass)
        {
            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component.GetType().Name != "AddFeatureOnClassLevel")
                {
                    continue;
                }

                SetField(
                    component,
                    "m_Class",
                    BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(scalingClass));
                SetField(component, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>());
                SetField(component, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            }
        }

        private static void SetAddFacts(BlueprintFeature feature, params BlueprintUnitFact[] facts)
        {
            var references = facts
                .Select(fact => BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(fact))
                .ToArray();

            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component.GetType().Name == "AddFacts")
                {
                    SetField(component, "m_Facts", references);
                }
            }
        }

        private static void CopyVisualModel(BlueprintUnit target, BlueprintUnit source)
        {
            target.Gender = source.Gender;
            target.Size = source.Size;
            target.Color = source.Color;
            target.Prefab = source.Prefab;
            CopyField(target, source, "m_Race");
            CopyField(target, source, "m_CustomizationPreset");
        }

        private static void SetStartingEquipment(BlueprintUnit unit, BlueprintItemWeapon startingBow)
        {
            SetField(unit, "m_StartingInventory", Array.Empty<BlueprintItemReference>());

            unit.Body.DisableHands = false;
            unit.Body.ActiveHandSet = 0;
            SetField(
                unit.Body,
                "m_PrimaryHand",
                BlueprintReferenceBase.CreateTyped<BlueprintItemEquipmentHandReference>(startingBow));
            SetField(unit.Body, "m_SecondaryHand", null);
            SetField(unit.Body, "m_PrimaryHandAlternative1", null);
            SetField(unit.Body, "m_SecondaryHandAlternative1", null);
            SetField(unit.Body, "m_PrimaryHandAlternative2", null);
            SetField(unit.Body, "m_SecondaryHandAlternative2", null);
            SetField(unit.Body, "m_PrimaryHandAlternative3", null);
            SetField(unit.Body, "m_SecondaryHandAlternative3", null);
            SetField(unit.Body, "m_AdditionalLimbs", Array.Empty<BlueprintItemWeaponReference>());
            SetField(unit.Body, "m_AdditionalSecondaryLimbs", Array.Empty<BlueprintItemWeaponReference>());
            SetField(unit.Body, "m_Armor", null);
            SetField(unit.Body, "m_Shirt", null);
            SetField(unit.Body, "m_Belt", null);
            SetField(unit.Body, "m_Head", null);
            SetField(unit.Body, "m_Glasses", null);
            SetField(unit.Body, "m_Feet", null);
            SetField(unit.Body, "m_Gloves", null);
            SetField(unit.Body, "m_Neck", null);
            SetField(unit.Body, "m_Ring1", null);
            SetField(unit.Body, "m_Ring2", null);
            SetField(unit.Body, "m_Wrist", null);
            SetField(unit.Body, "m_Shoulders", null);
            SetField(unit.Body, "m_QuickSlots", new BlueprintItemEquipmentUsableReference[5]);
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
                SoundBanks = Array.Empty<string>(),
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
                cooldown: 30f,
                chance: 0.25f);
            component.RefuseEquip = CreateBark(component, BillyPartyBanterLines, cooldown: 0f, interruptOthers: true);
            component.RefuseCast = CreateBark(component, BillyBuffingLines, cooldown: 0f, interruptOthers: true);
            component.CheckSuccess = CreateBark(component, BillyOnHitLines, cooldown: 0f);
            component.CheckFail = CreateBark(component, new[] { "Missed. Noted.", "Adjustment required.", "This is inefficient." }, cooldown: 0f);
            component.RefuseUnequip = CreateEmptyBark(component);
            component.Discovery = CreateBark(component, BillyMovementLines.Concat(BillyIroriFlavorLines), cooldown: 0f);
            component.Stealth = CreateBark(
                component,
                new[] { "Careful. I don't creak, but I still sneak.", "Quiet. Let them make the mistakes." },
                cooldown: 0f);
            component.StormRain = CreateEmptyBark(component);
            component.StormSnow = CreateEmptyBark(component);
            component.AnimationBarks = Array.Empty<UnitAsksComponent.AnimationBark>();

            _blueprints.SetComponents(barks, component);
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
            float chance = 1f)
        {
            return new UnitAsksComponent.Bark
            {
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
                AkEvent = string.Empty,
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
                .Concat(BillyIroriFlavorLines);
        }

        private BlueprintFeature EnsureBillyPositiveEnergyImmunity()
        {
            var feature = _blueprints.Get<BlueprintFeature>(ModBlueprintIds.Features.BillyPositiveEnergyImmunity);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = "WotrMod_BillyPositiveEnergyImmunity",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Features.BillyPositiveEnergyImmunity),
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true,
                    Ranks = 1,
                    IsClassFeature = true
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Features.BillyPositiveEnergyImmunity, feature);
            }

            feature.HideInUI = true;
            feature.HideInCharacterSheetAndLevelUp = true;
            feature.Ranks = 1;
            feature.IsClassFeature = true;
            _blueprints.SetComponents(
                feature,
                new AddEnergyDamageImmunity
                {
                    name = "$AddEnergyDamageImmunity$BillyPositiveEnergy",
                    EnergyType = DamageEnergyType.PositiveEnergy,
                    HealOnDamage = false
                });
            return feature;
        }

        private BlueprintFeature EnsureBillyFeatureList()
        {
            var featureList = GetOrClone<BlueprintFeature>(
                GameBlueprintIds.Units.CiarFeatureList,
                ModBlueprintIds.Features.BillyFeatureList,
                "WotrMod_BillyFeatureList",
                "Ciar feature list");

            var addClassLevels = featureList.ComponentsArray.OfType<AddClassLevels>().FirstOrDefault();
            if (addClassLevels == null)
            {
                throw new InvalidOperationException("Billy feature list does not have an AddClassLevels component.");
            }

            ConfigureBillyClassLevels(addClassLevels);
            ConfigureBillyClassSkills(featureList);
            featureList.HideInUI = true;
            featureList.HideInCharacterSheetAndLevelUp = true;
            return featureList;
        }

        private void ConfigureBillyClassSkills(BlueprintFeature featureList)
        {
            var components = (featureList.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(component => !(component is AddClassSkill) && !(component is SkillPointsPerCharacterLevel))
                .Concat(new BlueprintComponent[]
                {
                    CreateClassSkill(StatType.SkillPerception, "$AddClassSkill$BillyPerception"),
                    CreateClassSkill(StatType.SkillThievery, "$AddClassSkill$BillyThievery"),
                    CreateClassSkill(StatType.SkillLoreNature, "$AddClassSkill$BillyLoreNature"),
                    CreateBillyEcclesitheurgeSkillPointCorrection()
                })
                .ToArray();

            _blueprints.SetComponents(featureList, components);
        }

        private static SkillPointsPerCharacterLevel CreateBillyEcclesitheurgeSkillPointCorrection()
        {
            return new SkillPointsPerCharacterLevel
            {
                name = "$SkillPointsPerCharacterLevel$BillyEcclesitheurgeCorrection",
                SkillPointsPerLevel = -1
            };
        }

        private static AddClassSkill CreateClassSkill(StatType skill, string name)
        {
            return new AddClassSkill
            {
                name = name,
                Skill = skill
            };
        }

        private void ConfigureBillyClassLevels(AddClassLevels addClassLevels)
        {
            var cleric = _blueprints.Require<BlueprintCharacterClass>(GameBlueprintIds.Classes.Cleric, "Cleric class");
            var ecclesitheurge = _blueprints.Require<BlueprintArchetype>(
                GameBlueprintIds.Archetypes.Ecclesitheurge,
                "Ecclesitheurge archetype");

            SetField(
                addClassLevels,
                "m_CharacterClass",
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(cleric));
            SetField(
                addClassLevels,
                "m_Archetypes",
                new[]
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintArchetypeReference>(ecclesitheurge)
                });
            addClassLevels.Levels = 1;
            addClassLevels.RaceStat = StatType.Wisdom;
            addClassLevels.LevelsStat = StatType.Wisdom;
            addClassLevels.Skills = new[]
            {
                StatType.SkillLoreReligion,
                StatType.SkillPerception,
                StatType.SkillThievery,
                StatType.SkillLoreNature
            };
            SetField(addClassLevels, "m_SelectSpells", Array.Empty<BlueprintAbilityReference>());
            SetField(addClassLevels, "m_MemorizeSpells", Array.Empty<BlueprintAbilityReference>());
            addClassLevels.Selections = new[]
            {
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.Deity,
                    GameBlueprintIds.Features.Irori,
                    "Deity selection",
                    "Irori"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.ChannelEnergy,
                    new[]
                    {
                        GameBlueprintIds.Features.ChannelPositive,
                        GameBlueprintIds.Features.ChannelNegative
                    },
                    "Channel Energy selection",
                    "Channel Positive and Negative Energy"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.Domain,
                    GameBlueprintIds.Features.HealingDomainProgression,
                    "Domain selection",
                    "Healing domain"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.SecondaryDomain,
                    GameBlueprintIds.Features.LawDomainProgressionSecondary,
                    "Secondary domain selection",
                    "Law domain"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.BasicFeat,
                    new[]
                    {
                        GameBlueprintIds.Features.PointBlankShot,
                        GameBlueprintIds.Features.PreciseShot
                    },
                    "Basic feat selection",
                    "starting feats")
            };
            addClassLevels.DoNotApplyAutomatically = false;
        }

        private SelectionEntry CreateSelectionEntry(
            string selectionGuid,
            string featureGuid,
            string selectionName,
            string featureName)
        {
            return CreateSelectionEntry(selectionGuid, new[] { featureGuid }, selectionName, featureName);
        }

        private SelectionEntry CreateSelectionEntry(
            string selectionGuid,
            IEnumerable<string> featureGuids,
            string selectionName,
            string featureName)
        {
            var selection = _blueprints.Require<BlueprintFeatureSelection>(selectionGuid, selectionName);
            var features = featureGuids
                .Select(guid => _blueprints.Require<BlueprintFeature>(guid, featureName))
                .ToArray();

            var entry = new SelectionEntry
            {
                IsParametrizedFeature = false,
                IsFeatureSelectMythicSpellbook = false,
                ParamSpellSchool = SpellSchool.None,
                ParamWeaponCategory = WeaponCategory.UnarmedStrike,
                Stat = StatType.Unknown
            };
            SetField(entry, "m_Selection", BlueprintReferenceBase.CreateTyped<BlueprintFeatureSelectionReference>(selection));
            SetField(entry, "m_Features", features.Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>).ToArray());
            SetField(entry, "m_ParametrizedFeature", null);
            SetField(entry, "m_ParamObject", null);
            SetField(entry, "m_FeatureSelectMythicSpellbook", null);
            SetField(entry, "m_Spellbook", null);
            return entry;
        }

        private BlueprintPortrait EnsureBillyPortrait()
        {
            var portrait = GetOrClone<BlueprintPortrait>(
                GameBlueprintIds.Portraits.Ciar,
                ModBlueprintIds.Portraits.Billy,
                "WotrMod_BillyPortrait",
                "Ciar portrait");

            portrait.Data = CreateBillyPortraitData();
            return portrait;
        }

        private PortraitData CreateBillyPortraitData()
        {
            var halfPath = Path.Combine(_modPath ?? string.Empty, "Icons", "billy.png");
            var fullPath = Path.Combine(_modPath ?? string.Empty, "Icons", "billy_full.png");
            var headshotPath = Path.Combine(_modPath ?? string.Empty, "Icons", "billy_headshot.png");
            var storage = global::CustomPortraitsManager.Instance.Storage;

            var data = new PortraitData("wotr_mod_billy")
            {
                PortraitCategory = PortraitCategory.KingmakerNPC,
                IsDefault = false,
                InitiativePortrait = false
            };

            SetPropertyBackingField(
                data,
                "SmallPortraitHandle",
                new CustomPortraitHandle(headshotPath, PortraitType.SmallPortrait, storage));
            SetPropertyBackingField(
                data,
                "HalfPortraitHandle",
                new CustomPortraitHandle(halfPath, PortraitType.HalfLengthPortrait, storage));
            SetPropertyBackingField(
                data,
                "FullPortraitHandle",
                new CustomPortraitHandle(fullPath, PortraitType.FullLengthPortrait, storage));

            return data;
        }

        private void SetUnitName(BlueprintUnit unit, string localizationKey)
        {
            var sharedName = ScriptableObject.CreateInstance<SharedStringAsset>();
            sharedName.String = _localization.Text(localizationKey);
            unit.LocalizedName = sharedName;
        }

        private static CueSelection CreateCueSelection(BlueprintCueBase cue)
        {
            return new CueSelection
            {
                Cues = new List<BlueprintCueBaseReference>
                {
                    BlueprintReferenceBase.CreateTyped<BlueprintCueBaseReference>(cue)
                },
                Strategy = Strategy.First
            };
        }

        private static CueSelection CreateEmptyCueSelection()
        {
            return new CueSelection
            {
                Cues = new List<BlueprintCueBaseReference>(),
                Strategy = Strategy.First
            };
        }

        private static void SetDialogOnClickDialog(BlueprintComponent component, BlueprintDialog dialog)
        {
            var field = FindField(component.GetType(), "m_Dialog");
            field?.SetValue(component, BlueprintReferenceBase.CreateTyped<BlueprintDialogReference>(dialog));
        }

        private static void SetSpeakerBlueprint(DialogSpeaker speaker, BlueprintUnit unit)
        {
            var reference = BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(unit);
            var blueprintField = FindField(typeof(DialogSpeaker), "m_Blueprint");
            var speakerPortraitField = FindField(typeof(DialogSpeaker), "m_SpeakerPortrait");
            blueprintField?.SetValue(speaker, reference);
            speakerPortraitField?.SetValue(speaker, reference);
        }

        private static void SetUnitPortrait(BlueprintUnit unit, BlueprintPortrait portrait)
        {
            var field = FindField(typeof(BlueprintUnit), "m_Portrait");
            field?.SetValue(unit, BlueprintReferenceBase.CreateTyped<BlueprintPortraitReference>(portrait));
        }

        private static void SetUnitBarks(BlueprintUnit unit, BlueprintUnitAsksList barks)
        {
            SetField(
                unit.Visual,
                "m_Barks",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitAsksListReference>(barks));
        }

        private static void SetUnitFacts(BlueprintUnit unit, params BlueprintUnitFact[] facts)
        {
            var references = (facts ?? Array.Empty<BlueprintUnitFact>())
                .Where(fact => fact != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>)
                .ToArray();
            var field = FindField(typeof(BlueprintUnit), "m_AddFacts");
            field?.SetValue(unit, references);
        }

        private static void SetPropertyBackingField(object target, string propertyName, object value)
        {
            var field = FindField(target.GetType(), $"<{propertyName}>k__BackingField");
            field?.SetValue(target, value);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            field?.SetValue(target, value);
        }

        private static void CopyField(object target, object source, string fieldName)
        {
            var field = FindField(target.GetType(), fieldName);
            field?.SetValue(target, field.GetValue(source));
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }
    }
}
