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
        private const int BaseDelayDays = 3;
        private const int PreviousExtraDelayDays = 3;
        private const int MaximumExtraDelayDays = 6;
        private const int PreviousModdedDelayDays = BaseDelayDays + PreviousExtraDelayDays;
        private const int MaximumModdedDelayDays = BaseDelayDays + MaximumExtraDelayDays;

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
            EnsureRuntimeAdjustmentFlag(
                ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjusted,
                "WotrMod_DefendersHeartAssaultTimerAdjusted");
            EnsureRuntimeAdjustmentFlag(
                ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjustedSixExtra,
                "WotrMod_DefendersHeartAssaultTimerAdjustedSixExtra");
            ApplyCurrentSetting("install");
        }

        public void OnAreaLoaded()
        {
            ApplyCurrentSetting("area load");
        }

        public void ApplyCurrentSetting(string source)
        {
            var extraDelayDays = GetConfiguredExtraDelayDays();
            ApplyBlueprintDelay(BaseDelayDays + extraDelayDays);
            if (extraDelayDays > 0)
            {
                RepairSavedTimerIfNeeded(source, extraDelayDays);
                return;
            }

            RemoveSavedTimerDelayIfNeeded(source);
        }

        private void ApplyBlueprintDelay(int targetDelayDays)
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
            if (currentDelayDays != BaseDelayDays
                && currentDelayDays != PreviousModdedDelayDays
                && currentDelayDays != MaximumModdedDelayDays
                && currentDelayDays != targetDelayDays)
            {
                _logger.Warning(
                    $"Defender's Heart assault timer had unexpected delay {currentDelayDays} days; overriding to {targetDelayDays}.");
            }

            if (currentDelayDays == targetDelayDays)
            {
                return;
            }

            BlueprintFields.EtudeInvokeActionsDelayedDays.SetValue(delayedActions, targetDelayDays);
            _logger.Log($"Defender's Heart assault timer set to {targetDelayDays} days.");
        }

        private BlueprintUnlockableFlag EnsureRuntimeAdjustmentFlag(string flagGuid, string flagName)
        {
            var flag = _blueprints.Get<BlueprintUnlockableFlag>(flagGuid);
            if (flag != null)
            {
                return flag;
            }

            flag = new BlueprintUnlockableFlag
            {
                name = flagName,
                AssetGuid = BlueprintGuid.Parse(flagGuid)
            };
            _blueprints.AddCachedBlueprint(flagGuid, flag);
            return flag;
        }

        private static int GetConfiguredExtraDelayDays()
        {
            if (Main.Settings == null)
            {
                return PreviousExtraDelayDays;
            }

            switch (Main.Settings.DefendersHeartAssaultDelayMode)
            {
                case 0:
                    return 0;
                case 2:
                    return MaximumExtraDelayDays;
                default:
                    return PreviousExtraDelayDays;
            }
        }

        private void RepairSavedTimerIfNeeded(string source, int targetExtraDelayDays)
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            var player = Game.Instance.Player;
            var previousAdjustmentFlag = EnsureRuntimeAdjustmentFlag(
                ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjusted,
                "WotrMod_DefendersHeartAssaultTimerAdjusted");
            var adjustmentFlag = EnsureRuntimeAdjustmentFlag(
                ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjustedSixExtra,
                "WotrMod_DefendersHeartAssaultTimerAdjustedSixExtra");
            var alreadyAppliedExtraDelayDays = player.UnlockableFlags.IsUnlocked(adjustmentFlag)
                ? MaximumExtraDelayDays
                : player.UnlockableFlags.IsUnlocked(previousAdjustmentFlag)
                    ? PreviousExtraDelayDays
                    : 0;
            if (alreadyAppliedExtraDelayDays == targetExtraDelayDays)
            {
                return;
            }

            Action<int> setAdjustmentFlags = extraDelayDays =>
            {
                if (extraDelayDays >= PreviousExtraDelayDays)
                {
                    player.UnlockableFlags.Unlock(previousAdjustmentFlag);
                }
                else
                {
                    player.UnlockableFlags.Lock(previousAdjustmentFlag);
                }

                if (extraDelayDays >= MaximumExtraDelayDays)
                {
                    player.UnlockableFlags.Unlock(adjustmentFlag);
                }
                else
                {
                    player.UnlockableFlags.Lock(adjustmentFlag);
                }
            };
            var delayDeltaDays = targetExtraDelayDays - alreadyAppliedExtraDelayDays;
            var maximumAlreadyAdjustedDays = BaseDelayDays + alreadyAppliedExtraDelayDays;

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
                setAdjustmentFlags(targetExtraDelayDays);
                _logger.Log($"Defender's Heart assault timer repair skipped from {source}; assault is already active or resolved.");
                return;
            }

            if (etudes.EtudeIsStarted(timer) && !etudes.EtudeIsCompleted(timer))
            {
                if (delayDeltaDays < 0 && TryRemoveSavedTimerDelay(
                        etudes,
                        timer,
                        TimeSpan.FromDays(-delayDeltaDays),
                        out var reducedBefore,
                        out var reducedAfter))
                {
                    setAdjustmentFlags(targetExtraDelayDays);
                    _logger.Log(
                        $"Defender's Heart saved assault timer reduced from {reducedBefore.TotalDays:0.##} to {reducedAfter.TotalDays:0.##} days remaining from {source}.");
                    return;
                }

                if (delayDeltaDays > 0 && TryAddSavedTimerDelay(
                        etudes,
                        timer,
                        TimeSpan.FromDays(delayDeltaDays),
                        TimeSpan.FromDays(maximumAlreadyAdjustedDays),
                        out var before,
                        out var after))
                {
                    setAdjustmentFlags(targetExtraDelayDays);
                    _logger.Log(
                        $"Defender's Heart saved assault timer extended from {before.TotalDays:0.##} to {after.TotalDays:0.##} days remaining from {source}.");
                    return;
                }
            }

            if (etudes.EtudeIsCompleted(timer) && etudes.EtudeIsStarted(warning) && !etudes.EtudeIsCompleted(warning))
            {
                if (delayDeltaDays <= 0)
                {
                    setAdjustmentFlags(targetExtraDelayDays);
                    _logger.Log($"Defender's Heart assault timer reduction skipped from {source}; warning state is already active.");
                    return;
                }

                etudes.UnstartEtude(warning, false);
                etudes.UnstartEtude(timer, false);
                etudes.StartEtude(timer, false, true);
                SetSavedTimerRemaining(etudes, timer, TimeSpan.FromDays(delayDeltaDays));
                setAdjustmentFlags(targetExtraDelayDays);
                _logger.Log($"Defender's Heart warning state rolled back to a {delayDeltaDays}-day remaining timer from {source}.");
                return;
            }

            setAdjustmentFlags(targetExtraDelayDays);
            _logger.Log($"Defender's Heart assault timer repair marked complete from {source}; blueprint delay will apply.");
        }

        private void RemoveSavedTimerDelayIfNeeded(string source)
        {
            if (!Game.HasInstance || Game.Instance.Player == null)
            {
                return;
            }

            var player = Game.Instance.Player;
            var previousAdjustmentFlag = EnsureRuntimeAdjustmentFlag(
                ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjusted,
                "WotrMod_DefendersHeartAssaultTimerAdjusted");
            var adjustmentFlag = EnsureRuntimeAdjustmentFlag(
                ModBlueprintIds.Flags.DefendersHeartAssaultTimerAdjustedSixExtra,
                "WotrMod_DefendersHeartAssaultTimerAdjustedSixExtra");
            var appliedExtraDelayDays = player.UnlockableFlags.IsUnlocked(adjustmentFlag)
                ? MaximumExtraDelayDays
                : player.UnlockableFlags.IsUnlocked(previousAdjustmentFlag)
                    ? PreviousExtraDelayDays
                    : 0;
            if (appliedExtraDelayDays <= 0)
            {
                return;
            }

            var etudes = player.EtudesSystem;
            var timer = _blueprints.Get<BlueprintEtude>(GameBlueprintIds.Etudes.DefendersHeartAssaultTimer);
            if (etudes != null
                && timer != null
                && etudes.EtudeIsStarted(timer)
                && !etudes.EtudeIsCompleted(timer))
            {
                var timerData = FindTimerData(etudes, timer);
                if (timerData != null)
                {
                    var before = timerData.TimeRemaining;
                    var after = before - TimeSpan.FromDays(appliedExtraDelayDays);
                    timerData.TimeRemaining = after > TimeSpan.Zero ? after : TimeSpan.Zero;
                    _logger.Log(
                        $"Defender's Heart saved assault timer reduced from {before.TotalDays:0.##} to {timerData.TimeRemaining.TotalDays:0.##} days remaining from {source}.");
                }
            }

            player.UnlockableFlags.Lock(adjustmentFlag);
            player.UnlockableFlags.Lock(previousAdjustmentFlag);
        }

        private static bool TryAddSavedTimerDelay(
            EtudesSystem etudes,
            BlueprintEtude timer,
            TimeSpan extraDelay,
            TimeSpan maximumAlreadyAdjustedDelay,
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
            if (before > maximumAlreadyAdjustedDelay)
            {
                after = before;
                return false;
            }

            timerData.TimeRemaining += extraDelay;
            after = timerData.TimeRemaining;
            return true;
        }

        private static bool TryRemoveSavedTimerDelay(
            EtudesSystem etudes,
            BlueprintEtude timer,
            TimeSpan delayToRemove,
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
            after = before - delayToRemove;
            timerData.TimeRemaining = after > TimeSpan.Zero ? after : TimeSpan.Zero;
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
