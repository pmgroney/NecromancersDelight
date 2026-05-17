using System;
using System.Linq;
using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content
{
    internal sealed class DefendersHeartAssaultTimerPatch : IContentModule, IAreaLoadModule
    {
        private const int OriginalDelayDays = 3;
        private const int ModdedDelayDays = 6;

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public DefendersHeartAssaultTimerPatch(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Defender's Heart Assault Timer";

        public void RegisterLocalization()
        {
        }

        public void Install()
        {
            EnsureRuntimeAdjustmentFlag();
            ApplyBlueprintDelay();
            RepairSavedTimerIfNeeded("install");
        }

        public void OnAreaLoaded()
        {
            RepairSavedTimerIfNeeded("area load");
        }

        private void ApplyBlueprintDelay()
        {
            var timer = _blueprints.Require<BlueprintEtude>(
                GameBlueprintIds.Etudes.DefendersHeartAssaultTimer,
                "Defender's Heart assault timer etude");
            var delayedActions = _blueprints.GetComponents<EtudeInvokeActionsDelayed>(timer).FirstOrDefault();
            if (delayedActions == null)
            {
                throw new InvalidOperationException("Defender's Heart assault timer etude has no delayed action component.");
            }

            if (BlueprintFields.EtudeInvokeActionsDelayedDays == null)
            {
                throw new InvalidOperationException("EtudeInvokeActionsDelayed.m_Days field was not found.");
            }

            var currentDelayDays = (int)BlueprintFields.EtudeInvokeActionsDelayedDays.GetValue(delayedActions);
            if (currentDelayDays != OriginalDelayDays && currentDelayDays != ModdedDelayDays)
            {
                _logger.Warning(
                    $"Defender's Heart assault timer had unexpected delay {currentDelayDays} days; overriding to {ModdedDelayDays}.");
            }

            BlueprintFields.EtudeInvokeActionsDelayedDays.SetValue(delayedActions, ModdedDelayDays);
            _logger.Log($"Defender's Heart assault timer set to {ModdedDelayDays} days.");
        }

        private BlueprintUnlockableFlag EnsureRuntimeAdjustmentFlag()
        {
            var flag = _blueprints.Get<BlueprintUnlockableFlag>(ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjusted);
            if (flag != null)
            {
                return flag;
            }

            flag = new BlueprintUnlockableFlag
            {
                name = "WotrMod_DefendersHeartAssaultTimerAdjusted",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjusted)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjusted, flag);
            return flag;
        }

        private void RepairSavedTimerIfNeeded(string source)
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            var player = Game.Instance.Player;
            var adjustmentFlag = EnsureRuntimeAdjustmentFlag();
            if (player.UnlockableFlags.IsUnlocked(adjustmentFlag))
            {
                return;
            }

            var etudes = player.EtudesSystem;
            if (etudes == null)
            {
                return;
            }

            var timer = _blueprints.Get<BlueprintEtude>(GameBlueprintIds.Etudes.DefendersHeartAssaultTimer);
            var warning = _blueprints.Get<BlueprintEtude>(GameBlueprintIds.Etudes.DefendersHeartWarning);
            var readyForAttack = _blueprints.Get<BlueprintEtude>(GameBlueprintIds.Etudes.DefendersHeartReadyForAttack);
            if (timer == null || warning == null || readyForAttack == null)
            {
                return;
            }

            if (etudes.EtudeIsStarted(readyForAttack) || etudes.EtudeIsCompleted(readyForAttack))
            {
                player.UnlockableFlags.Unlock(adjustmentFlag);
                _logger.Log($"Defender's Heart assault timer repair skipped from {source}; assault is already active or resolved.");
                return;
            }

            if (etudes.EtudeIsStarted(timer) && !etudes.EtudeIsCompleted(timer))
            {
                if (TryAddSavedTimerDelay(etudes, timer, TimeSpan.FromDays(OriginalDelayDays), out var before, out var after))
                {
                    player.UnlockableFlags.Unlock(adjustmentFlag);
                    _logger.Log(
                        $"Defender's Heart saved assault timer extended from {before.TotalDays:0.##} to {after.TotalDays:0.##} days remaining from {source}.");
                    return;
                }
            }

            if (etudes.EtudeIsCompleted(timer) && etudes.EtudeIsStarted(warning) && !etudes.EtudeIsCompleted(warning))
            {
                etudes.UnstartEtude(warning, false);
                etudes.UnstartEtude(timer, false);
                etudes.StartEtude(timer, false, true);
                SetSavedTimerRemaining(etudes, timer, TimeSpan.FromDays(OriginalDelayDays));
                player.UnlockableFlags.Unlock(adjustmentFlag);
                _logger.Log($"Defender's Heart warning state rolled back to a {OriginalDelayDays}-day remaining timer from {source}.");
                return;
            }

            player.UnlockableFlags.Unlock(adjustmentFlag);
            _logger.Log($"Defender's Heart assault timer repair marked complete from {source}; blueprint delay will apply.");
        }

        private static bool TryAddSavedTimerDelay(
            EtudesSystem etudes,
            BlueprintEtude timer,
            TimeSpan extraDelay,
            out TimeSpan before,
            out TimeSpan after)
        {
            var timerData = FindTimerData(etudes, timer);
            if (timerData == null)
            {
                before = TimeSpan.Zero;
                after = TimeSpan.Zero;
                return false;
            }

            before = timerData.TimeRemaining;
            if (before > extraDelay)
            {
                after = before;
                return false;
            }

            timerData.TimeRemaining += extraDelay;
            after = timerData.TimeRemaining;
            return true;
        }

        private static void SetSavedTimerRemaining(EtudesSystem etudes, BlueprintEtude timer, TimeSpan remaining)
        {
            var timerData = FindTimerData(etudes, timer);
            if (timerData != null)
            {
                timerData.TimeRemaining = remaining;
                timerData.Executed = false;
            }
        }

        private static EtudeInvokeActionsDelayed.EtudeInvokeActionDelayedData FindTimerData(
            EtudesSystem etudes,
            BlueprintEtude timer)
        {
            var fact = etudes.Facts.Get(timer);
            return fact?.Components
                .OfType<EtudeInvokeActionsDelayed.EtudeInvokeActionDelayedData>()
                .FirstOrDefault();
        }
    }
}
