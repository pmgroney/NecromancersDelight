using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic;

namespace wotr_mod.Features
{
    public sealed class NecromancerProficiencyCleanup : UnitFactComponentDelegate
    {
        public BlueprintFeature MartialWeaponProficiency;
        public string GravebladeProficienciesGuid;
        public string DeathstalkerProficienciesGuid;

        protected override void OnTurnOn()
        {
            if (Owner == null ||
                MartialWeaponProficiency == null ||
                HasFact(GravebladeProficienciesGuid) ||
                HasFact(DeathstalkerProficienciesGuid))
            {
                return;
            }

            var martialFact = (Owner.Facts?.List ?? Enumerable.Empty<EntityFact>())
                .OfType<Feature>()
                .FirstOrDefault(IsSourceLessMartialProficiency);
            if (martialFact == null)
            {
                return;
            }

            Owner.Progression.Features.RemoveFact(MartialWeaponProficiency);
            Main.Log("[NecromancerProficiencyCleanup] Removed orphaned Martial Weapon Proficiency from base Necromancer.");
        }

        private bool IsSourceLessMartialProficiency(Feature feature)
        {
            return feature?.Blueprint == MartialWeaponProficiency &&
                   feature.SourceClass == null &&
                   feature.SourceProgression == null &&
                   feature.SourceRace == null &&
                   feature.MythicSource == null &&
                   feature.SourceFact == null &&
                   feature.SourceItem == null &&
                   feature.SourceAbility == null &&
                   feature.SourceLevel == 0;
        }

        private bool HasFact(string featureGuid)
        {
            return !string.IsNullOrWhiteSpace(featureGuid) &&
                   (Owner.Facts?.List ?? Enumerable.Empty<EntityFact>())
                   .Any(fact => fact?.Blueprint?.AssetGuid.ToString() == featureGuid);
        }
    }
}
