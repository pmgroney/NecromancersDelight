using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content
{
    internal sealed class MythicSpellbookMergeInstaller : IContentModule
    {
        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public MythicSpellbookMergeInstaller(
            BlueprintTool blueprints,
            UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Mythic Spellbook Merge Eligibility";

        public void RegisterLocalization()
        {
        }

        public void Install()
        {
            var lichMergeFeature = _blueprints.Require<BlueprintFeatureSelectMythicSpellbook>(
                GameBlueprintIds.Features.LichIncorporateSpellbookFeature,
                "Lich incorporate spellbook feature");
            var evokerSpellbook = _blueprints.Require<BlueprintSpellbook>(
                ModBlueprintIds.Spellbooks.Evoker,
                "Evoker spellbook");
            var necromancerSpellbook = _blueprints.Require<BlueprintSpellbook>(
                ModBlueprintIds.Spellbooks.Necromancer,
                "Necromancer spellbook");
            var sepulchritSpellbook = _blueprints.Require<BlueprintSpellbook>(
                ModBlueprintIds.Spellbooks.Sepulchrit,
                "Sepulchrit spellbook");
            var gravebladeSpellbook = _blueprints.Require<BlueprintSpellbook>(
                ModBlueprintIds.Spellbooks.Graveblade,
                "Graveblade spellbook");
            var deathstalkerSpellbook = _blueprints.Require<BlueprintSpellbook>(
                ModBlueprintIds.Spellbooks.Deathstalker,
                "Deathstalker spellbook");

            _blueprints.AddAllowedMythicSpellbooks(
                lichMergeFeature,
                new[]
                {
                    evokerSpellbook,
                    necromancerSpellbook,
                    sepulchritSpellbook,
                    gravebladeSpellbook,
                    deathstalkerSpellbook
                });
            _logger.Log("Lich merged spellbook eligibility includes Evoker, Necromancer, Sepulchrit, Graveblade, and Deathstalker.");
        }
    }
}
