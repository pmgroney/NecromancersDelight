using System.Reflection;
using HarmonyLib;
using Kingmaker.Settings;
using Kingmaker.Settings.Difficulty;

namespace wotr_mod.Patches
{
    [HarmonyPatch(typeof(MinDifficultyController), nameof(MinDifficultyController.UpdateMinDifficulty))]
    internal static class CoreDifficultyOverridePatch
    {
        private static readonly MethodInfo MinDifficultySetter =
            AccessTools.PropertySetter(typeof(MinDifficultyController), nameof(MinDifficultyController.MinDifficulty));

        private static readonly FieldInfo DifficultyPresetsField =
            AccessTools.Field(typeof(DifficultyPresetsController), "m_Presets");

        private static void Postfix(MinDifficultyController __instance)
        {
            Apply(__instance);
        }

        internal static void Apply(MinDifficultyController controller)
        {
            if (controller == null || Main.Settings == null || !Main.Settings.EnableAchievementsWhileModded)
            {
                return;
            }

            var current = SettingsController.DifficultySettingsController?.ExtractFromSettings();
            var core = GetCorePreset();
            if (current == null || core == null)
            {
                return;
            }

            if (current.CompareTo(core) < 0)
            {
                return;
            }

            if (controller.MinDifficulty != null && controller.MinDifficulty.CompareTo(core) >= 0)
            {
                return;
            }

            if (MinDifficultySetter == null)
            {
                return;
            }

            MinDifficultySetter.Invoke(controller, new object[] { core.Copy() });
        }

        private static DifficultyPreset GetCorePreset()
        {
            var controller = SettingsController.DifficultyPresetsController;
            if (controller == null || DifficultyPresetsField == null)
            {
                return null;
            }

            var presets = DifficultyPresetsField.GetValue(controller)
                as DifficultyPreset[];
            if (presets == null)
            {
                return null;
            }

            foreach (var preset in presets)
            {
                if (preset != null && preset.GameDifficulty == GameDifficultyOption.Core)
                {
                    return preset;
                }
            }

            return null;
        }
    }
}
