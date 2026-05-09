using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using wotr_mod.Classes.Necromancer.Archetypes;

namespace wotr_mod.Classes.Necromancer
{
    internal sealed partial class NecromancerInstaller
    {
        private BlueprintArchetype[] EnsureArchetypes(
            BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook,
            BlueprintSpellList spellList)
        {
            return new[]
            {
                new SepulchritInstaller(_blueprints, _localization)
                    .Ensure(characterClass, spellbook, spellList),
                new GravebladeInstaller(_blueprints, _localization, _logger, _icons)
                    .Ensure(characterClass, spellbook, spellList)
            };
        }
    }
}
