using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;

namespace wotr_mod.Classes
{
    internal interface IClassContentInstaller
    {
        bool CanInstall(CharacterClassDefinition definition);
        void Install(CharacterClassDefinition definition, BlueprintCharacterClass characterClass, BlueprintSpellbook spellbook, BlueprintSpellList spellList);
        void RegisterLocalization();
    }
}
