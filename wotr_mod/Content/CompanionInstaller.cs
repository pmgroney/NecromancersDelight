using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.ResourceManagement;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;
using Kingmaker.Visual.Sound;
using wotr_mod.Infrastructure;
using wotr_mod.Patches;

namespace wotr_mod.Content
{
    internal sealed partial class CompanionInstaller : IContentModule
    {
        private const string BillyStoryBundlePath = "Assets\\billystory";
        private const string BillyStoryAssetId = "wotr_mod:Assets/billystory";

        private static readonly Dictionary<string, Sprite> StorySpriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, AssetBundle> StoryBundleCache =
            new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<UnityEngine.Object> StoryImageRoots =
            new List<UnityEngine.Object>();

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
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyGreeting,              "Play_CMP_Billy_Dialog_Greeting");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyJoinCue,               "Play_CMP_Billy_Dialog_JoinCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyWhatAreYouCue,         "Play_CMP_Billy_Dialog_WhatAreYouCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyWhyHereCue,            "Play_CMP_Billy_Dialog_WhyHereCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyDangerousCue,          "Play_CMP_Billy_Dialog_DangerousCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyPlanCue,               "Play_CMP_Billy_Dialog_PlanCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyBowQuestStartCue,      "Play_CMP_Billy_Dialog_BowQuestStartCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyBowQuestTempleCue,     "Play_CMP_Billy_Dialog_BowQuestTempleCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyBowQuestHosillaCue,    "Play_CMP_Billy_Dialog_BowQuestHosillaCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyBowQuestDisciplineCue, "Play_CMP_Billy_Dialog_BowQuestDisciplineCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyBowQuestEndCue,        "Play_CMP_Billy_Dialog_BowQuestEndCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct1JalmerayCue,       "Play_CMP_Billy_Dialog_Act1JalmerayCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct1RecordCue,         "Play_CMP_Billy_Dialog_Act1RecordCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct1TunicCue,          "Play_CMP_Billy_Dialog_Act1TunicCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct2BowCue,            "Play_CMP_Billy_Dialog_Act2BowCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct2ArmorCue,          "Play_CMP_Billy_Dialog_Act2ArmorCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct3ArmorCue,          "Play_CMP_Billy_Dialog_Act3ArmorCue");
            _localization.PutSoundEvent(LocalizationIds.Mod.BillyAct3BowCue,            "Play_CMP_Billy_Dialog_Act3BowCue");
            RegisterBillyBanterLocalization();
            RegisterBillySceneInterjectionLocalization();
        }

        public void Install()
        {
            ConvertWoljifToBaseRogue();
            ReplaceSeelahShieldFocus();
            LoadBillyVoiceBank();
            var billy = EnsureUndeadCiarCompanion();
            EnsureBillyBanterReplacements(billy);
            EnsureBillySceneInterjections(billy);
            EnsureBillyShieldMazeStandIn(billy);
        }

        private void ConvertWoljifToBaseRogue()
        {
            if (Main.Settings == null || !Main.Settings.MakeWoljifBaseRogue)
            {
                return;
            }

            var featureList = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Units.WoljifFeatureList,
                "Woljif starting feature list");
            var rogue = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Rogue,
                "Rogue class");

            if (!_blueprints.ConvertClassLevelsToBaseClass(featureList, rogue))
            {
                throw new InvalidOperationException(
                    "Woljif starting feature list does not have Rogue class levels.");
            }

            Main.Log("Converted Woljif's starting build from Eldritch Scoundrel to base Rogue.");
        }

        private void ReplaceSeelahShieldFocus()
        {
            var featureList = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Units.SeelahFeatureList,
                "Seelah starting feature list");
            var shieldFocus = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.ShieldFocus,
                "Shield Focus feat");
            var powerAttack = _blueprints.Require<BlueprintFeature>(
                GameBlueprintIds.Features.PowerAttack,
                "Power Attack feat");

            if (!_blueprints.ReplaceClassLevelSelectionFeature(featureList, shieldFocus, powerAttack))
            {
                throw new InvalidOperationException(
                    "Seelah starting feature list does not select Shield Focus.");
            }

            Main.Log("Replaced Seelah's starting Shield Focus feat with Power Attack.");
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
            EnsureBillyBowQuestDialog(unit);
            CopyCompanionShell(unit, companionCiar, undeadCiar, dialog);
            SetUnitName(unit, LocalizationIds.Mod.BillyName);
            ConfigureBillyUnit(unit);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Units.UndeadCiarCompanion, unit);
            }

            return unit;
        }

        private BlueprintUnit EnsureBillyShieldMazeStandIn(BlueprintUnit billyCompanion)
        {
            var existing = _blueprints.Get<BlueprintUnit>(ModBlueprintIds.Units.BillyShieldMazeStandIn);
            var undeadCiar = _blueprints.Require<BlueprintUnit>(
                GameBlueprintIds.Units.CiarUndead,
                "Undead Ciar unit");

            var unit = existing ?? _blueprints.CloneBlueprint(
                undeadCiar,
                ModBlueprintIds.Units.BillyShieldMazeStandIn,
                "WotrMod_BillyShieldMazeStandIn");

            var dialog = _blueprints.Require<BlueprintDialog>(
                ModBlueprintIds.Dialogs.BillyDialog,
                "Billy dialog");
            ConfigureBillyStandInUnit(unit, billyCompanion, undeadCiar, dialog);

            if (existing == null)
            {
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Units.BillyShieldMazeStandIn, unit);
            }

            return unit;
        }

        private BlueprintCompanionStory EnsureBillyStory(BlueprintUnit companion)
        {
            var story = _blueprints.Get<BlueprintCompanionStory>(ModBlueprintIds.CompanionStories.Billy);
            if (story == null)
            {
                story = new BlueprintCompanionStory
                {
                    name = "WotrMod_BillyStory",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.CompanionStories.Billy)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.CompanionStories.Billy, story);
            }

            story.Title = _localization.Text(LocalizationIds.Mod.BillyStoryTitle);
            story.Description = _localization.Text(LocalizationIds.Mod.BillyStoryDescription);
            story.Gender = Gender.Male;
            SetField(
                story,
                "m_Companion",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(companion));
            var imageLink = CreateStoryImageLink(BillyStoryBundlePath);
            Main.Log(
                "Billy story image link " +
                (imageLink == null ? "was not created." : $"created with AssetId '{imageLink.AssetId}'."));
            SetField(
                story,
                "m_ImageLink",
                imageLink);

            return story;
        }

        private SpriteLink CreateStoryImageLink(string relativeBundlePath)
        {
            try
            {
                var sprite = LoadStoryBundleSprite(relativeBundlePath);
                if (sprite == null)
                {
                    return null;
                }

                if (!RegisterStorySpriteResource(BillyStoryAssetId, sprite))
                {
                    return null;
                }

                Main.Log($"Billy story image creating SpriteLink with AssetId '{BillyStoryAssetId}'.");
                var link = new SpriteLink
                {
                    AssetId = BillyStoryAssetId
                };
                var handle = CreateHeldStoryImageHandle(BillyStoryAssetId, sprite);
                SetProperty(link, "m_Handle", handle);
                Main.Log(
                    $"Billy story image seeded held handle for '{BillyStoryAssetId}': " +
                    (handle == null ? "<null>" : $"held={handle.IsHeld}, assetId='{handle.AssetId}', loaded={DescribeSprite(link.Load(false, false))}"));
                return link;
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy story image could not be loaded from {relativeBundlePath}: {ex.Message}");
                return null;
            }
        }

        private Sprite LoadStoryBundleSprite(string relativeBundlePath)
        {
            if (string.IsNullOrWhiteSpace(relativeBundlePath) || string.IsNullOrWhiteSpace(_modPath))
            {
                var loggedBundlePath = string.IsNullOrWhiteSpace(relativeBundlePath) ? "<null>" : relativeBundlePath;
                var loggedModPath = string.IsNullOrWhiteSpace(_modPath) ? "<null>" : _modPath;
                Main.Warning(
                    $"Billy story image skipped: bundle path or mod path is blank. " +
                    $"relative='{loggedBundlePath}', modPath='{loggedModPath}'.");
                return null;
            }

            var fullPath = Path.Combine(_modPath, relativeBundlePath);
            if (StorySpriteCache.TryGetValue(fullPath, out var cached))
            {
                var cachedName = cached == null ? "<null>" : cached.name;
                Main.Log($"Billy story image using cached sprite '{cachedName}' from '{fullPath}'.");
                return cached;
            }

            if (!File.Exists(fullPath))
            {
                Main.Warning($"Billy story image bundle file was not found at '{fullPath}'.");
                return null;
            }

            var fileInfo = new FileInfo(fullPath);
            Main.Log($"Billy story image loading bundle '{fullPath}' ({fileInfo.Length} bytes).");

            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles().ToArray();
            Main.Log(
                "Billy story image loaded bundles: " +
                (loadedBundles.Length == 0
                    ? "<none>"
                    : string.Join(
                        ", ",
                        loadedBundles
                            .Select(loaded => string.IsNullOrWhiteSpace(loaded.name) ? "<unnamed>" : loaded.name)
                            .ToArray())));

            var bundle = StoryBundleCache.TryGetValue(fullPath, out var cachedBundle) ? cachedBundle : null;
            if (bundle != null)
            {
                Main.Log($"Billy story image reusing cached bundle '{bundle.name}'.");
            }
            else
            {
                bundle = loadedBundles
                    .FirstOrDefault(loaded => string.Equals(
                        loaded.name,
                        Path.GetFileName(relativeBundlePath),
                        StringComparison.OrdinalIgnoreCase));
                Main.Log(
                    bundle == null
                        ? $"Billy story image did not find a loaded bundle named '{Path.GetFileName(relativeBundlePath)}'; loading from file."
                        : $"Billy story image reusing loaded bundle '{bundle.name}'.");
                bundle = bundle ?? AssetBundle.LoadFromFile(fullPath);
            }

            if (bundle == null)
            {
                Main.Warning($"Billy story image AssetBundle.LoadFromFile returned null for '{fullPath}'.");
                return null;
            }

            try
            {
                StoryBundleCache[fullPath] = bundle;
                Main.Log($"Billy story image bundle loaded as '{bundle.name}'.");
                var assetNames = bundle.GetAllAssetNames();
                Main.Log(
                    "Billy story image bundle assets: " +
                    (assetNames.Length == 0 ? "<none>" : string.Join(", ", assetNames)));

                var sprites = bundle.LoadAllAssets<Sprite>();
                Main.Log(
                    "Billy story image sprites found: " +
                    (sprites.Length == 0
                        ? "<none>"
                        : string.Join(", ", sprites.Select(DescribeSprite).ToArray())));

                var sprite = sprites.FirstOrDefault();
                if (sprite != null)
                {
                    StorySpriteCache[fullPath] = sprite;
                    Main.Log($"Billy story image selected sprite {DescribeSprite(sprite)}.");
                }
                else
                {
                    Main.Warning($"Billy story image bundle '{fullPath}' did not contain any Sprite assets.");
                }

                return sprite;
            }
            catch
            {
                if (!loadedBundles.Contains(bundle))
                {
                    StoryBundleCache.Remove(fullPath);
                    bundle.Unload(true);
                }

                throw;
            }
        }

        private static string DescribeSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return "<null>";
            }

            var texture = sprite.texture;
            var textureSize = texture == null ? "no texture" : $"{texture.width}x{texture.height}";
            return $"'{sprite.name}' rect={sprite.rect.width}x{sprite.rect.height} texture={textureSize}";
        }

        private static bool RegisterStorySpriteResource(string assetId, Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(assetId) || sprite == null)
            {
                return false;
            }

            RootStoryImage(sprite);

            var loadedResourcesField = typeof(ResourcesLibrary).GetField(
                "s_LoadedResources",
                BindingFlags.Static | BindingFlags.NonPublic);
            var loadedResources = loadedResourcesField?.GetValue(null) as IDictionary;
            var loadedResourceType = typeof(ResourcesLibrary).GetNestedType(
                "LoadedResource",
                BindingFlags.Public | BindingFlags.NonPublic);
            if (loadedResources == null || loadedResourceType == null)
            {
                Main.Warning($"Billy story image could not access ResourcesLibrary loaded resource cache for '{assetId}'.");
                return false;
            }

            var loadedResource = Activator.CreateInstance(loadedResourceType, sprite);
            SetField(loadedResource, "AssetId", assetId);
            loadedResources[assetId] = loadedResource;
            ResourcesLibrary.HoldResource(assetId);
            Main.Log($"Billy story image registered ResourcesLibrary asset '{assetId}' from sprite {DescribeSprite(sprite)}.");
            return true;
        }

        private static BundledResourceHandle<Sprite> CreateHeldStoryImageHandle(string assetId, Sprite sprite)
        {
            var handle = (BundledResourceHandle<Sprite>)Activator.CreateInstance(
                typeof(BundledResourceHandle<Sprite>),
                true);
            SetField(handle, "m_AssetId", assetId);
            SetField(handle, "m_Held", true);
            SetField(handle, "m_Object", new WeakReference<Sprite>(sprite));
            return handle;
        }

        private static void RootStoryImage(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (!StoryImageRoots.Contains(sprite))
            {
                StoryImageRoots.Add(sprite);
            }

            var texture = sprite.texture;
            if (texture != null && !StoryImageRoots.Contains(texture))
            {
                StoryImageRoots.Add(texture);
            }
        }

        private static ActionList CreateRecruitActions(
            BlueprintUnit companion,
            BlueprintCompanionStory story,
            BlueprintUnlockableFlag recruitedFlag)
        {
            var recruitData = new Recruit.RecruitData
            {
                NPCUnit = new DialogCurrentSpeaker(),
                MustBeInParty = true
            };
            SetField(
                recruitData,
                "m_CompanionBlueprint",
                BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(companion));

            return new ActionList
            {
                Actions = new GameAction[]
                {
                    new BillyRecruitmentCleanupAction
                    {
                        name = "WotrMod_BillyRecruitCleanup",
                        Stage = "before Recruit"
                    },
                    new BillyRecruitmentLogAction
                    {
                        name = "WotrMod_BillyRecruitLogBefore",
                        Stage = "before Recruit"
                    },
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
                    new BillyRecruitmentLogAction
                    {
                        name = "WotrMod_BillyRecruitLogAfterRecruit",
                        Stage = "after Recruit"
                    },
                    new BillyRecruitmentFallbackAction
                    {
                        name = "WotrMod_BillyRecruitFallback"
                    },
                    new BillyRecruitmentLogAction
                    {
                        name = "WotrMod_BillyRecruitLogAfterFallback",
                        Stage = "after fallback"
                    },
                    CreateUnlockFlagAction(recruitedFlag),
                    new UnlockCompanionStory
                    {
                        name = "WotrMod_UnlockBillyStory",
                        Story = story
                    }
                }
            };
        }

        private static UnlockFlag CreateUnlockFlagAction(BlueprintUnlockableFlag flag)
        {
            return CreateUnlockFlagAction(flag, "WotrMod_UnlockBillyRecruitedFlag");
        }

        private static UnlockFlag CreateUnlockFlagAction(BlueprintUnlockableFlag flag, string name)
        {
            var action = new UnlockFlag
            {
                name = name,
                flagValue = 1,
                flag = flag
            };
            return action;
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
            ConfigureBillyRespecLevelLimit(target);

            CopyField(target, companionSource, "m_Faction");
            CopyField(target, companionSource, "m_Brain");
            CopyField(target, companionSource, "m_AddFacts");
            CopyField(target, companionSource, "m_AllowNonContextActions");

            target.Alignment = companionSource.Alignment;
            target.IsCheater = companionSource.IsCheater;
            target.IsFake = companionSource.IsFake;
        }

        private void ConfigureBillyStandInUnit(
            BlueprintUnit target,
            BlueprintUnit visualSource,
            BlueprintUnit conversationSource,
            BlueprintDialog dialog)
        {
            var dialogComponents = (conversationSource.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(component => component.GetType().Name == "DialogOnClick")
                .Select(component => _blueprints.CloneComponent(component))
                .ToArray();

            foreach (var component in dialogComponents)
            {
                SetDialogOnClickDialog(component, dialog);
            }

            _blueprints.SetComponents(target, dialogComponents);
            SetUnitName(target, LocalizationIds.Mod.BillyName);
            CopyField(target, conversationSource, "m_Faction");
            CopyField(target, conversationSource, "m_Brain");
            CopyField(target, conversationSource, "m_AllowNonContextActions");
            target.Alignment = visualSource.Alignment;
            target.IsCheater = visualSource.IsCheater;
            target.IsFake = visualSource.IsFake;
            target.Strength = visualSource.Strength;
            target.Dexterity = visualSource.Dexterity;
            target.Constitution = visualSource.Constitution;
            target.Intelligence = visualSource.Intelligence;
            target.Wisdom = visualSource.Wisdom;
            target.Charisma = visualSource.Charisma;
            CopyVisualModel(target, visualSource);
            SetStartingEquipment(target, _blueprints.Require<BlueprintItemWeapon>(
                GameBlueprintIds.Items.CompositeLongbow,
                "Composite Longbow"));
            SetUnitBarks(target, EnsureBillyBarks());
        }

        private void ConfigureBillyRespecLevelLimit(BlueprintUnit unit)
        {
            var classLevelLimit = unit.ComponentsArray.OfType<ClassLevelLimit>().FirstOrDefault();
            if (classLevelLimit == null)
            {
                classLevelLimit = new ClassLevelLimit
                {
                    name = "$ClassLevelLimit$Billy"
                };
                _blueprints.AddComponent(unit, classLevelLimit);
            }

            classLevelLimit.LevelLimit = 1;
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
            var dodge = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.Dodge,
                "Dodge");
            var undeadType = _blueprints.Require<BlueprintUnitFact>(
                GameBlueprintIds.Features.UndeadType,
                "Undead Type");
            var featureList = EnsureBillyFeatureList();
            var positiveEnergyImmunity = EnsureBillyPositiveEnergyImmunity();
            var scalingClass = _blueprints.Require<BlueprintCharacterClass>(
                GameBlueprintIds.Classes.Cleric,
                "Cleric class");
            var scalingArchetype = _blueprints.Require<BlueprintArchetype>(
                GameBlueprintIds.Archetypes.PriestOfBalance,
                "Priest of Balance archetype");
            var monkAcBonus = EnsureBillyMonkAcBonus(scalingClass, scalingArchetype);
            var wayOfTheBow = EnsureBillyWayOfTheBow(scalingClass);
            var visualSource = _blueprints.Require<BlueprintUnit>(
                GameBlueprintIds.Units.Dlc5StartPregenFighter,
                "DLC5 start pregen fighter visual unit");
            var visualRace = _blueprints.Require<BlueprintRace>(
                GameBlueprintIds.Races.Human,
                "Human race");
            var startingBow = _blueprints.Require<BlueprintItemWeapon>(
                GameBlueprintIds.Items.CompositeLongbow,
                "Composite Longbow");

            SetUnitFacts(
                unit,
                featureList,
                undeadType,
                longbowProficiency,
                shortbowProficiency,
                dodge,
                positiveEnergyImmunity,
                monkAcBonus,
                wayOfTheBow);

            SetUnitPortrait(unit, EnsureBillyPortrait());
            CopyVisualModel(unit, visualSource);
            SetField(unit, "m_Race", BlueprintReferenceBase.CreateTyped<BlueprintRaceReference>(visualRace));
            SetField(unit, "m_CustomizationPreset", null);
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

        private static CueSelection CreateCueSelection(params BlueprintCueBase[] cues)
        {
            return new CueSelection
            {
                Cues = cues
                    .Where(cue => cue != null)
                    .Select(cue => BlueprintReferenceBase.CreateTyped<BlueprintCueBaseReference>(cue))
                    .ToList(),
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
            var blueprintField = FindField(typeof(DialogSpeaker), "m_Blueprint");
            var speakerPortraitField = FindField(typeof(DialogSpeaker), "m_SpeakerPortrait");
            blueprintField?.SetValue(speaker, null);
            speakerPortraitField?.SetValue(speaker, BlueprintReferenceBase.CreateTyped<BlueprintUnitReference>(unit));
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

        private static void SetProperty(object target, string propertyName, object value)
        {
            var property = FindProperty(target.GetType(), propertyName);
            property?.SetValue(target, value, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            field?.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            var field = FindField(target.GetType(), fieldName);
            return field?.GetValue(target);
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

        private static PropertyInfo FindProperty(Type type, string propertyName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                var property = current.GetProperty(propertyName, flags);
                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }
    }
}
