using System.Collections.Generic;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal static class CharacterClassRegistry
    {
        public static IReadOnlyList<CharacterClassDefinition> GetAll()
        {
            return new[]
            {
                CreateEvoker(),
                CreateNecromancer()
            };
        }

        public static IReadOnlyList<CharacterClassDefinition> GetActive()
        {
            return new[]
            {
                CreateEvoker(),
                CreateNecromancer()
            };
        }

        private static CharacterClassDefinition CreateEvoker()
        {
            return new CharacterClassDefinition(
                "WotrMod_EvokerClass",
                ModBlueprintIds.Classes.Evoker,
                ModBlueprintIds.Progressions.Evoker,
                ModBlueprintIds.Spellbooks.Evoker,
                ModBlueprintIds.SpellLists.Evoker,
                LocalizationIds.Mod.EvokerName,
                LocalizationIds.Mod.EvokerDescription,
                StatType.Charisma,
                useEvokerBloodlines: true,
                removeSorcererBloodline: true,
                presentation: new CharacterClassPresentationDefinition(
                    difficulty: 3,
                    recommendedAttributes: new[] { StatType.Charisma, StatType.Dexterity, StatType.Constitution },
                    notRecommendedAttributes: new[] { StatType.Strength, StatType.Intelligence }));
        }

        private static CharacterClassDefinition CreateNecromancer()
        {
            return new CharacterClassDefinition(
                "WotrMod_NecromancerClass",
                ModBlueprintIds.Classes.Necromancer,
                ModBlueprintIds.Progressions.Necromancer,
                ModBlueprintIds.Spellbooks.Necromancer,
                ModBlueprintIds.SpellLists.Necromancer,
                LocalizationIds.Mod.NecromancerName,
                LocalizationIds.Mod.NecromancerDescription,
                StatType.Charisma,
                useEvokerBloodlines: false,
                removeSorcererBloodline: true,
                useUndeadBloodline: false,
                useNecromancerBloodline: true,
                chassis: new ClassChassisDefinition(
                    DiceType.D8,
                    GameBlueprintIds.StatProgressions.BaseAttackBonusMedium),
                presentation: new CharacterClassPresentationDefinition(
                    difficulty: 3,
                    recommendedAttributes: new[] { StatType.Charisma, StatType.Dexterity, StatType.Constitution },
                    notRecommendedAttributes: new[] { StatType.Strength, StatType.Intelligence },
                    signatureAbilityGuids: new[]
                    {
                        ModBlueprintIds.Features.NecromancerBloodlineArcana,
                        ModBlueprintIds.Features.NecromancerBoneArmor
                    }));
        }
    }
}
