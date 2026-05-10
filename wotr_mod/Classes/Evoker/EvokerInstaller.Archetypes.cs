using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using wotr_mod.Classes;
using wotr_mod.Classes.Evoker.Archetypes;

namespace wotr_mod.Classes.Evoker
{
    internal sealed partial class EvokerInstaller
    {
        private BlueprintArchetype[] EnsureArchetypes(
            CharacterClassDefinition definition,
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            return new[]
            {
                new ArcanistEvokerInstaller(_blueprints, _localization, this).Ensure(characterClass),
                new ShadowbornInstaller(_blueprints, _localization, this).Ensure(characterClass),
                new DraconicEvokerInstaller(_blueprints, _localization, _logger, this).Ensure(characterClass)
            };
        }
    }
}
