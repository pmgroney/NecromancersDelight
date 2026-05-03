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
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using wotr_mod.Infrastructure;

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

            answers.Conditions = new ConditionsChecker();
            answers.Answers = new List<BlueprintAnswerBaseReference>
            {
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(joinAnswer),
                BlueprintReferenceBase.CreateTyped<BlueprintAnswerBaseReference>(leaveAnswer)
            };

            joinAnswer.Text = _localization.Text(LocalizationIds.Mod.BillyJoinAnswer);
            joinAnswer.ShowConditions = new ConditionsChecker();
            joinAnswer.SelectConditions = new ConditionsChecker();
            joinAnswer.OnSelect = CreateRecruitActions(speaker);
            joinAnswer.NextCue = CreateEmptyCueSelection();
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
            unit.Strength = 10;
            unit.Dexterity = 18;
            unit.Constitution = 10;
            unit.Intelligence = 10;
            unit.Wisdom = 19;
            unit.Charisma = 16;
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

            SetUnitFacts(unit, featureList, undeadType, longbowProficiency, shortbowProficiency, positiveEnergyImmunity);

            SetUnitPortrait(unit, EnsureBillyPortrait());
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
            featureList.HideInUI = true;
            featureList.HideInCharacterSheetAndLevelUp = true;
            return featureList;
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
                StatType.SkillLoreNature,
                StatType.SkillLoreReligion,
                StatType.SkillPerception
            };
            SetField(addClassLevels, "m_SelectSpells", Array.Empty<BlueprintAbilityReference>());
            SetField(addClassLevels, "m_MemorizeSpells", Array.Empty<BlueprintAbilityReference>());
            addClassLevels.Selections = new[]
            {
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.Deity,
                    GameBlueprintIds.Features.Pharasma,
                    "Deity selection",
                    "Pharasma"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.ChannelEnergy,
                    GameBlueprintIds.Features.ChannelNegative,
                    "Channel Energy selection",
                    "Channel Negative Energy"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.Domain,
                    GameBlueprintIds.Features.HealingDomainProgression,
                    "Domain selection",
                    "Healing domain"),
                CreateSelectionEntry(
                    GameBlueprintIds.Selections.SecondaryDomain,
                    GameBlueprintIds.Features.DeathDomainProgressionSecondary,
                    "Secondary domain selection",
                    "Death domain"),
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
            var fullPath = Path.Combine(_modPath ?? string.Empty, "Icons", "billy.png");
            var headshotPath = Path.Combine(_modPath ?? string.Empty, "Icons", "billy_headshot.png");
            var storage = global::CustomPortraitsManager.Instance.Storage;

            var data = new PortraitData("wotr_mod_billy")
            {
                PortraitCategory = PortraitCategory.KingmakerNPC,
                IsDefault = false,
                InitiativePortrait = true
            };

            SetPropertyBackingField(
                data,
                "SmallPortraitHandle",
                new CustomPortraitHandle(headshotPath, PortraitType.SmallPortrait, storage));
            SetPropertyBackingField(
                data,
                "HalfPortraitHandle",
                new CustomPortraitHandle(headshotPath, PortraitType.HalfLengthPortrait, storage));
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
            var field = FindField(typeof(DialogSpeaker), "m_Blueprint");
            field?.SetValue(speaker, BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(unit));
        }

        private static void SetUnitPortrait(BlueprintUnit unit, BlueprintPortrait portrait)
        {
            var field = FindField(typeof(BlueprintUnit), "m_Portrait");
            field?.SetValue(unit, BlueprintReferenceBase.CreateTyped<BlueprintPortraitReference>(portrait));
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
