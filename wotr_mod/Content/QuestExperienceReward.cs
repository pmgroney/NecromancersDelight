using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.Blueprints.Quests;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content
{
    internal sealed class QuestExperienceReward
    {
        public QuestExperienceReward(EncounterType encounter, int cr, float modifier = 1f)
        {
            Encounter = encounter;
            CR = cr;
            Modifier = modifier;
        }

        public EncounterType Encounter { get; }

        public int CR { get; }

        public float Modifier { get; }
    }

    internal static class QuestRewardInstaller
    {
        public static void SetExperienceReward(
            BlueprintTool blueprints,
            BlueprintQuestObjective objective,
            string rewardId,
            QuestExperienceReward reward)
        {
            if (blueprints == null || objective == null || string.IsNullOrEmpty(rewardId))
            {
                return;
            }

            var componentName = "$Experience$" + rewardId;
            var components = blueprints.GetComponents<BlueprintComponent>(objective)
                .Where(component => !IsRewardComponent(component, componentName))
                .ToList();

            if (reward != null)
            {
                components.Add(CreateExperience(componentName, reward));
            }

            blueprints.SetComponents(objective, components.ToArray());
        }

        private static bool IsRewardComponent(BlueprintComponent component, string componentName)
        {
            return component is Experience experience
                   && string.Equals(experience.name, componentName, StringComparison.Ordinal);
        }

        private static Experience CreateExperience(string componentName, QuestExperienceReward reward)
        {
            return new Experience
            {
                name = componentName,
                Encounter = reward.Encounter,
                CR = reward.CR,
                Modifier = reward.Modifier,
                Count = null,
                PlayerGainsNoExp = false,
                Dummy = false
            };
        }
    }
}
