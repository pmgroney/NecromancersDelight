using System.Collections.Generic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal static class NecromancerSpellRegistry
    {
        private static readonly IReadOnlyList<ClassSpellDefinition> Spells = new[]
        {
            Spell("DisruptUndead", "652739779aa05504a9ad5db1db6d02ae", 0),
            Spell("TouchOfFatigueCast", "5bf3315ce1ed4d94e8805706820ef64d", 0),

            Spell("CauseFear", "bd81a3931aa285a4f9844585b5d97e51", 1),
            Spell("Bane", "8bc64d869456b004b9db255cdd1ea734", 1),
            Spell("BoneSpike", ModBlueprintIds.Spells.BoneSpike, 1, SelectionRecommendation.NotRecommended),
            Spell("Doom", "fbdd8c455ac4cde4a9a3e18c84af9485", 1),
            Spell("InflictLightWoundsCast", "e5af3674bb241f14b9a9f6b0c7dc3d27", 1),
            Spell("RayOfEnfeeblement", "450af0402422b0b4980d9c2175869612", 1),
            Spell("RayOfSickening", "fa3078b9976a5b24caf92e20ee9c0f54", 1),

            Spell("AnimateDeadLesser", "57fcf8016cf04da4a8b33d2add14de7e", 2),
            Spell("Blindness", "46fd02ad56c35224c9c91c88cd457791", 2),
            Spell("BoneFists", "0da2046b4517427bb9b2e304ea6342bf", 2),
            Spell("Boneshaker", "b7731c2b4fa1c9844a092329177be4c3", 2),
            Spell("CommandUndead", "0b101dd5618591e478f825f0eef155b4", 2),
            Spell("FalseLife", "7a5b5bf845779a941a67251539545762", 2),
            Spell("Fester", "2dbe271c979d9104c8e2e6b42e208e32", 2),
            Spell("GhoulTouchCast", "a2b05555c704458aaadc34be52a63633", 2),
            Spell("InflictModerateWoundsCast", "65f0b63c45ea82a4f8b8325768a3832d", 2),
            Spell("OracleBurden", "039b8b8b6d5747e3a37c447a46a58761", 2),
            Spell("PerniciousPoison", "dee3074b2fbfb064b80b973f9b56319e", 2),
            Spell("PoxPustules", "bc153808ef4884a4594bc9bec2299b69", 2),
            Spell("Scare", "08cb5f4c3b2695e44971bf5c45205df0", 2),
            Spell("CorpseExplosion", ModBlueprintIds.Spells.CorpseExplosion, 2),
            Spell("DeathRay", ModBlueprintIds.Spells.DeathRay, 2),

            Spell("AnimateDead", "4b76d32feb089ad4499c3a1ce8e1ac27", 3),
            Spell("BestowCurse", "989ab5c44240907489aba0a8568d0603", 3),
            Spell("Contagion", "48e2744846ed04b4580be1a3343a5d3d", 3),
            Spell("Fear", "d2aeac47450c76347aebbc02e4f463e0", 3),
            Spell("FungalInfestationAbility", "73f8182ddd684a57a7f9678e516209a3", 3),
            Spell("InflictSeriousWoundsCast", "bd5da98859cf2b3418f6d68ea66cabbe", 3),
            Spell("LifeBlast", "a8666d26bbbd9b640958284e0eee3602", 3),
            Spell("PoisonCast", "2a6eda8ef30379142a4b75448fb214a3", 3),
            Spell("RayOfExhaustion", "8eead52509987034ea9025d60cc05985", 3),
            Spell("VampiricTouchCast", "8a28a811ca5d20d49a863e832c31cce1", 3),
            Spell("EldritchHorror", ModBlueprintIds.Spells.EldritchHorror, 3),

            Spell("Boneshatter", "f2f1efac32ea2884e84ecaf14657298b", 4),
            Spell("DeathWardCast", "e9cc9378fd6841f48ad59384e79e9953", 4),
            Spell("Enervation", "f34fb78eaaec141469079af124bcfa0f", 4),
            Spell("ExplosionOfRot", "98544596a01d4f7bbf7cc3ff98a1fb69", 4),
            Spell("FalseLifeGreater", "dc6af3b4fd149f841912d8a3ce0983de", 4),
            Spell("InflictCriticalWoundsCast", "651110ed4f117a948b41c05c5c7624c0", 4),
            Spell("Poison", "d797007a142a6c0409a74b064065a15e", 4),

            Spell("InflictLightWoundsMass", "9da37873d79ef0a468f969e4e5116ad2", 5),
            Spell("Cloudkill", "548d339ba87ee56459c98e80167bdf10", 5),
            Spell("SlayLivingCast", "4fbd47525382517419c66fb548fe9a67", 5),
            Spell("VampiricShadowShield", "a34921035f2a6714e9be5ca76c5e34b5", 5),
            Spell("WavesOfFatigue", "8878d0c46dfbd564e9d5756349d5e439", 5),
            Spell("WrackingRay", "1cde0691195feae45bab5b83ea3f221e", 5),

            Spell("BansheeBlast", "d42c6d3f29e07b6409d670792d72bc82", 6),
            Spell("CircleOfDeath", "a89dcbbab8f40e44e920cc60636097cf", 6),
            Spell("CreateUndeadBase", "76a11b460be25e44ca85904d6806e5a3", 6),
            Spell("Eyebite", "3167d30dd3c622c46b0c0cb242061642", 6),
            Spell("FesterMass", "52b8b14360a87104482b2735c7fc8606", 6),
            Spell("HarmCast", "cc09224ecc9af79449816c45bc5be218", 6),
            Spell("InflictModerateWoundsMass", "03944622fbe04824684ec29ff2cec6a7", 6),
            Spell("PlagueStorm", "82a5b848c05e3f342b893dedb1f9b446", 6),
            Spell("UmbralStrike", "474ed0aa656cc38499cc9a073d113716", 6),
            Spell("UndeathToDeath", "a9a52760290591844a96d0109e30e04d", 6),

            Spell("BestowCurseGreater", "6101d0f0720927e4ca413de7b3c4b7e5", 7),
            Spell("Destruction", "3b646e1db3403b940bf620e01d2ce0c7", 7),
            Spell("FingerOfDeath", "6f1dcf6cfa92d1948a740195707c0dbe", 7),
            Spell("FingerOfDeathSithhud", "e03024c8a03f454db5b78660f524757d", 7),
            Spell("Harm", "137af566f68fd9b428e2e12da43c1482", 7),
            Spell("InflictSeriousWoundsMass", "820170444d4d2a14abc480fcbdb49535", 7),
            Spell("SymbolOfWeakness", "8b02310b46e54de1ae9ba7161831938d", 7),
            Spell("WavesOfExhaustion", "3e4d3b9a5bd03734d9b053b9067c2f38", 7),

            Spell("CreateUndeadGreaterBase", "8ba9b6e4df4c46a597154e2b8e7e6e4a", 8),
            Spell("DeathClutch", "c3d2294a6740bc147870fff652f3ced5", 8),
            Spell("HorridWilting", "08323922485f7e246acb3d2276515526", 8),
            Spell("InflictCriticalWoundsMass", "5ee395a2423808c4baf342a4f8395b19", 8),
            Spell("Soulreaver", "b4afacd337dac4a40a769a567c038ab7", 8),

            Spell("EnergyDrain", "37302f72b06ced1408bf5bb965766d46", 9),
            Spell("Weird", "870af83be6572594d84d276d7fc583e0", 9),
            Spell("WailOfBanshee", "b24583190f36a8442b212e45226c54fc", 9),
            Spell("HellOnEarth", ModBlueprintIds.Spells.HellOnEarth, 9)
        };

        public static IReadOnlyList<ClassSpellDefinition> GetAll()
        {
            return Spells;
        }

        private static ClassSpellDefinition Spell(
            string displayName,
            string spellGuid,
            int spellLevel,
            SelectionRecommendation? recommendation = null)
        {
            return new ClassSpellDefinition(displayName, spellGuid, spellLevel, recommendation);
        }
    }
}
