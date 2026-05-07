using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal static class CharacterClassRegistry
    {
        private static readonly IReadOnlyList<CharacterClassDefinition> AllDefinitions =
            Array.AsReadOnly(new[]
            {
                CreateEvoker(),
                CreateNecromancer()
            });

        public static IReadOnlyList<CharacterClassDefinition> GetAll()
        {
            return AllDefinitions;
        }

        public static IReadOnlyList<CharacterClassDefinition> GetActive()
        {
            return AllDefinitions;
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
                chassis: new ClassChassisDefinition(
                    DiceType.D8),
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
                    difficulty: 4,
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
