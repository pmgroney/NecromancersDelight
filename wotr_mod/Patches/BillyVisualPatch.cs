using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Kingmaker.Visual.CharacterSystem;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal static class BillyVisualPatch
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid BillyStandInGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.BillyShieldMazeStandIn);
        private static readonly HashSet<int> ApplyingViews = new HashSet<int>();
        private static readonly HashSet<int> LoggedViews = new HashSet<int>();
        private static readonly MethodInfo AddEquipmentEntityMethod = AccessTools.Method(
            typeof(Character),
            "AddEquipmentEntity",
            new[] { typeof(EquipmentEntity), typeof(bool), typeof(int), typeof(int) });
        private static readonly MethodInfo RemoveEquipmentEntityMethod = AccessTools.Method(
            typeof(Character),
            "RemoveEquipmentEntity",
            new[] { typeof(EquipmentEntity), typeof(bool) });
        private const BodyPartType BillyZombieOverlayParts =
            BodyPartType.Head
            | BodyPartType.HeadTop
            | BodyPartType.HeadBottom
            | BodyPartType.Brows
            | BodyPartType.Eyes
            | BodyPartType.Lashes
            | BodyPartType.Ears
            | BodyPartType.Nose
            | BodyPartType.Teeth
            | BodyPartType.Hair
            | BodyPartType.Neck
            | BodyPartType.NeckTorso
            | BodyPartType.Torso
            | BodyPartType.UpperArms
            | BodyPartType.Hands
            | BodyPartType.UpperLegs
            | BodyPartType.LowerLegs;
        private const BodyPartType BillyMythicLichLimbParts =
            BodyPartType.Feet;
        private const BodyPartType BillyGhoulForearmParts =
            BodyPartType.Forearms
            | BodyPartType.DownArms;
        private const BodyPartType BillySkeletonHeadParts =
            BodyPartType.Head
            | BodyPartType.HeadTop
            | BodyPartType.HeadBottom
            | BodyPartType.Brows
            | BodyPartType.Eyes
            | BodyPartType.Lashes
            | BodyPartType.Ears
            | BodyPartType.Nose
            | BodyPartType.Teeth
            | BodyPartType.Hair
            | BodyPartType.Helmet
            | BodyPartType.Mask
            | BodyPartType.MaskBottom
            | BodyPartType.MaskGoggles;
        private const BodyPartType BillyHumanHeadPartsToHide =
            BodyPartType.Head
            | BodyPartType.HeadTop
            | BodyPartType.HeadBottom
            | BodyPartType.Brows
            | BodyPartType.Eyes
            | BodyPartType.Lashes
            | BodyPartType.Ears
            | BodyPartType.Nose
            | BodyPartType.Teeth
            | BodyPartType.Hair;

        private static EquipmentEntity[] BillyOverlayEntities;
        private static string OverlayEntitySummary;

        [HarmonyPatch(typeof(UnitEntityView), "OnDidAttachToData")]
        private static class OnDidAttachToDataPatch
        {
            [HarmonyPostfix]
            private static void Postfix(UnitEntityView __instance)
            {
                ApplyBillyOverlay(__instance, "attach");
            }
        }

        [HarmonyPatch(typeof(UnitEntityView), "UpdateBodyEquipmentModel")]
        private static class UpdateBodyEquipmentModelPatch
        {
            [HarmonyPostfix]
            private static void Postfix(UnitEntityView __instance)
            {
                ApplyBillyOverlay(__instance, "equipment");
            }
        }

        private static void ApplyBillyOverlay(UnitEntityView view, string source)
        {
            if (view == null || !IsBilly(view.EntityData))
            {
                return;
            }

            var viewId = view.GetInstanceID();
            if (ApplyingViews.Contains(viewId))
            {
                return;
            }

            try
            {
                ApplyingViews.Add(viewId);
                var character = view.CharacterAvatar;
                if (character == null)
                {
                    LogOnce(viewId, $"Billy visual patch skipped from {source}: CharacterAvatar is null.");
                    return;
                }

                var removedHairEntities = RemoveExistingHairEntities(character);
                var overlayEntities = GetBillyOverlayEntities();
                if (overlayEntities.Length == 0)
                {
                    LogOnce(viewId, $"Billy visual patch skipped from {source}: no overlay body entities loaded.");
                    return;
                }

                var existing = character.EquipmentEntities ?? new List<EquipmentEntity>();
                var added = 0;
                foreach (var entity in overlayEntities)
                {
                    if (entity == null || existing.Contains(entity))
                    {
                        continue;
                    }

                    AddEquipmentEntityMethod?.Invoke(character, new object[] { entity, false, 0, 0 });
                    added++;
                }

                if (added > 0)
                {
                    character.ForceDoUpdate();
                }

                LogOnce(
                    viewId,
                    $"Billy visual patch ran from {source}: removedHair={removedHairEntities}, overlayEntities={overlayEntities.Length}, added={added}, totalEquipment={character.EquipmentEntityCount}. {OverlayEntitySummary}");
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy visual patch failed from {source}: {ex}");
            }
            finally
            {
                ApplyingViews.Remove(viewId);
            }
        }

        private static EquipmentEntity[] GetBillyOverlayEntities()
        {
            if (BillyOverlayEntities != null)
            {
                return BillyOverlayEntities;
            }

            var overlays = new List<EquipmentEntity>();
            var mythicLichHead = ResourcesLibrary.TryGetBlueprint<KingmakerEquipmentEntity>(
                BlueprintGuid.Parse(GameBlueprintIds.EquipmentEntities.MythicLichHead));
            if (mythicLichHead != null)
            {
                overlays.AddRange(
                    mythicLichHead.Load(Gender.Male, Race.Human)
                        .Where(entity => entity != null)
                        .Select(source => CloneFilteredOverlay(
                            source,
                            BillySkeletonHeadParts,
                            BillyHumanHeadPartsToHide,
                            "MythicLichHead")));
            }

            var mythicLichBody = ResourcesLibrary.TryGetBlueprint<KingmakerEquipmentEntity>(
                BlueprintGuid.Parse(GameBlueprintIds.EquipmentEntities.MythicLichBody));
            if (mythicLichBody != null)
            {
                overlays.AddRange(
                    mythicLichBody.Load(Gender.Male, Race.Human)
                        .Where(entity => entity != null)
                        .Select(source => CloneFilteredOverlay(
                            source,
                            BillyMythicLichLimbParts,
                            null,
                            "MythicLichLimbs")));
            }

            var ghoulBody = ResourcesLibrary.TryGetBlueprint<KingmakerEquipmentEntity>(
                BlueprintGuid.Parse(GameBlueprintIds.EquipmentEntities.GhoulBody));
            if (ghoulBody != null)
            {
                overlays.AddRange(
                    ghoulBody.Load(Gender.Male, Race.Human)
                        .Where(entity => entity != null)
                        .Select(source => CloneFilteredOverlay(
                            source,
                            BillyGhoulForearmParts,
                            null,
                            "GhoulForearms")));
            }

            var zombieBody = ResourcesLibrary.TryGetBlueprint<KingmakerEquipmentEntity>(
                BlueprintGuid.Parse(GameBlueprintIds.EquipmentEntities.ZombieBody));
            if (zombieBody != null)
            {
                overlays.AddRange(
                    zombieBody.Load(Gender.Male, Race.Human)
                        .Where(entity => entity != null)
                        .Select(source => CloneFilteredOverlay(
                            source,
                            BillyZombieOverlayParts,
                            null,
                            "Zombie")));
            }

            BillyOverlayEntities = overlays
                .Where(entity => entity.BodyParts != null && entity.BodyParts.Count > 0)
                .ToArray();
            return BillyOverlayEntities;
        }

        private static EquipmentEntity CloneFilteredOverlay(
            EquipmentEntity source,
            BodyPartType bodyPartsToKeep,
            BodyPartType? humanPartsToHide,
            string donorName)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            var bodyPartsBefore = clone.BodyParts?.Count ?? 0;
            var outfitPartsBefore = clone.OutfitParts?.Count ?? 0;
            clone.name = source.name + "_Billy" + donorName;
            clone.OutfitParts?.Clear();
            if (clone.BodyParts != null)
            {
                clone.BodyParts.RemoveAll(part => part == null || (part.Type & bodyPartsToKeep) == 0);
            }

            clone.HideBodyParts = humanPartsToHide ?? GetBodyPartTypes(clone.BodyParts);
            var bodyPartsAfter = clone.BodyParts?.Count ?? 0;
            var keptTypes = SummarizeBodyPartTypes(clone.BodyParts);
            var hiddenTypes = clone.HideBodyParts == 0 ? "none" : clone.HideBodyParts.ToString();
            OverlayEntitySummary = string.IsNullOrEmpty(OverlayEntitySummary)
                ? $"Overlay filter: donor={donorName}, source={source.name}, bodyParts={bodyPartsBefore}->{bodyPartsAfter}, outfitParts={outfitPartsBefore}->0, hide=[{hiddenTypes}], kept=[{keptTypes}]"
                : OverlayEntitySummary + $"; donor={donorName}, source={source.name}, bodyParts={bodyPartsBefore}->{bodyPartsAfter}, outfitParts={outfitPartsBefore}->0, hide=[{hiddenTypes}], kept=[{keptTypes}]";
            return clone;
        }

        private static int RemoveExistingHairEntities(Character character)
        {
            var entities = character.EquipmentEntities?.ToArray() ?? Array.Empty<EquipmentEntity>();
            var removed = 0;
            foreach (var entity in entities)
            {
                if (entity == null || entity.name?.Contains("_Billy") == true)
                {
                    continue;
                }

                if (!HasBodyPart(entity, BodyPartType.Hair))
                {
                    continue;
                }

                RemoveEquipmentEntityMethod?.Invoke(character, new object[] { entity, false });
                removed++;
            }

            return removed;
        }

        private static bool HasBodyPart(EquipmentEntity entity, BodyPartType bodyPartType)
        {
            return entity.BodyParts != null
                && entity.BodyParts.Any(part => part != null && (part.Type & bodyPartType) != 0);
        }

        private static BodyPartType GetBodyPartTypes(IEnumerable<BodyPart> bodyParts)
        {
            if (bodyParts == null)
            {
                return 0;
            }

            var result = (BodyPartType)0;
            foreach (var bodyPart in bodyParts)
            {
                if (bodyPart != null)
                {
                    result |= bodyPart.Type;
                }
            }

            return result;
        }

        private static string SummarizeBodyPartTypes(IEnumerable<BodyPart> bodyParts)
        {
            if (bodyParts == null)
            {
                return "none";
            }

            var types = bodyParts
                .Where(part => part != null)
                .Select(part => part.Type.ToString())
                .Distinct()
                .ToArray();
            return types.Length == 0 ? "none" : string.Join("|", types);
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint != null
                   && (unit.Descriptor.Blueprint.AssetGuid == BillyGuid
                       || unit.Descriptor.Blueprint.AssetGuid == BillyStandInGuid);
        }

        private static void LogOnce(int viewId, string message)
        {
            if (LoggedViews.Add(viewId))
            {
                Main.Log(message);
            }
        }
    }
}
