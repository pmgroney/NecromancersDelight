using Kingmaker.UnitLogic.Abilities.Blueprints;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class SpellModifierContext
    {
        public SpellModifierContext(
            BlueprintAbility ability,
            SpellDefinition definition,
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            Ability = ability;
            Definition = definition;
            Blueprints = blueprints;
            Logger = logger;
        }

        public BlueprintAbility Ability { get; }
        public SpellDefinition Definition { get; }
        public BlueprintTool Blueprints { get; }
        public UnityModManager.ModEntry.ModLogger Logger { get; }
    }
}
