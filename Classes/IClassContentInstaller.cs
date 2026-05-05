using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;

namespace wotr_mod.Classes
{
    internal interface IClassContentInstaller
    {
        bool CanInstall(CharacterClassDefinition definition);

        /// <summary>
        /// Populates the class spell list. Called before Install so the spell list is
        /// ready when archetype spellbooks and feature installers need it.
        /// </summary>
        void ConfigureSpellList(CharacterClassDefinition definition, BlueprintSpellList spellList);

        void Install(CharacterClassDefinition definition, BlueprintCharacterClass characterClass,
            BlueprintSpellbook spellbook, BlueprintSpellList spellList);

        void RegisterLocalization();
    }
}
