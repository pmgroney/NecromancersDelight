using System;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Achievements;
using Kingmaker.Achievements.Blueprints;
using Kingmaker.Settings;

namespace wotr_mod.Patches
{
    [HarmonyPatch(typeof(AchievementEntity), "get_IsDisabled")]
    internal static class AchievementReenablePatch
    {
        private static void Postfix(AchievementEntity __instance, ref bool __result)
        {
            if (!__result || Main.Settings == null || !Main.Settings.EnableAchievementsWhileModded)
            {
                return;
            }

            try
            {
                __result = IsDisabledForNonModReason(__instance.Data);
            }
            catch (Exception ex)
            {
                Main.Warning("Failed to evaluate non-mod achievement restrictions: " + ex);
            }
        }

        private static bool IsDisabledForNonModReason(AchievementData data)
        {
            var player = Game.Instance?.Player;
            if (player == null || data == null)
            {
                return true;
            }

            CoreDifficultyOverridePatch.Apply(player.MinDifficultyController);

            if (data.ExcludedFromCurrentPlatform)
            {
                return true;
            }

            if (data.OnlyMainCampaign && !player.Campaign.IsMainGameContent)
            {
                return true;
            }

            var specificCampaign = data.SpecificCampaign?.Get();
            if (!data.OnlyMainCampaign && specificCampaign != null && player.Campaign != specificCampaign)
            {
                return true;
            }

            if (data.MinDifficulty != null &&
                player.MinDifficultyController.MinDifficulty.CompareTo(data.MinDifficulty.Preset) < 0)
            {
                return true;
            }

            if (data.MinCrusadeDifficulty > (KingdomDifficulty)SettingsRoot.Difficulty.KingdomDifficulty)
            {
                return true;
            }

            if (data.IronMan && !SettingsRoot.Difficulty.OnlyOneSave)
            {
                return true;
            }

            return false;
        }
    }
}
