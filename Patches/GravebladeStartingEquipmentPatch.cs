using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class GravebladeStartingEquipmentPatch : StartingEquipmentPatch
    {
        public GravebladeStartingEquipmentPatch(BlueprintTool blueprints, UnityModManager.ModEntry.ModLogger logger)
            : base(
                blueprints,
                logger,
                new StartingEquipmentRule(
                    "Graveblade",
                    ModBlueprintIds.Archetypes.Graveblade,
                    new[]
                    {
                        new StartingEquipmentItem(
                            GameBlueprintIds.Items.MasterworkScythe,
                            "Masterwork scythe",
                            equip: true)
                    },
                    new[]
                    {
                        ModBlueprintIds.Features.GravebladeProficiencies,
                        ModBlueprintIds.Features.GravebladeReapingEdge
                    },
                    new[]
                    {
                        GameBlueprintIds.Items.PlayersStartingBracers
                    }))
        {
        }

        public override string Name => "Graveblade Starting Equipment";
    }
}
