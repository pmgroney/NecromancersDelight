using System.Collections.Generic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes
{
    internal static class EvokerSpellRegistry
    {
        private static readonly IReadOnlyList<ClassSpellDefinition> Spells = new[]
        {
            Spell("AcidSplash", "0c852a2405dd9f14a8bbcfaf245ff823", 0),
            Spell("Ignition", "564c2ac83c7844beb1921e69ab159ac6", 0),
            Spell("Flare", "f0f8e5b9808f44e4eadd22b138131d52", 0),
            Spell("MageLight", "95f206566c5261c42aa5b3e7e0d1e36c", 0),
            Spell("RayOfFrost", "9af2ab69df6538f4793b2f9c3cc85603", 0),

            Spell("CorrosiveTouchCast", "95810d2829895724f950c8c4086056e7", 1),
            Spell("Grease", "95851f6e85fe87d4190675db0419d112", 1),
            Spell("MageArmor", "9e1ad5d6f87d19e4d8883d63a6e35568", 1),
            Spell("Snowball", "9f10909f0be1f5141bf1c102041f93d9", 1),
            Spell("SummonMonsterISingle", "8fd74eddd9b6c224693d9ab241f25e84", 1),
            Spell("BurningHands", "4783c3709a74a794dbe7c8e7e0b1b038", 1),
            Spell("DivineFavor", "9d5d2d3ffdd73c648af3eb3e585b1113", 1),
            Spell("EarPiercingScream", "8e7cfa5f213a90549aadd18f8f6f4664", 1),
            Spell("FaerieFire", "4d9bf81b7939b304185d58a09960f589", 1),
            Spell("FlareBurst", "39a602aa80cc96f4597778b6d4d49c0a", 1),
            Spell("MagicMissile", "4ac47ddb9fa1eaf43a1b6809980cfbd2", 1),
            Spell("ShockingGraspCast", "ab395d2335d3f384e99dddee8562978f", 1),
            Spell("AcidMissile", ModBlueprintIds.Spells.AcidMissile, 1),
            Spell("ElectricMissile", ModBlueprintIds.Spells.ElectricMissile, 1),
            Spell("FireMissile", ModBlueprintIds.Spells.FireMissile, 1),
            Spell("IceMissile", ModBlueprintIds.Spells.IceMissile, 1),

            Spell("AcidArrow", "9a46dfd390f943647ab4395fc997936d", 2),
            Spell("CreatePit", "29ccc62632178d344ad0be0865fd3113", 2),
            Spell("Glitterdust", "ce7dad2b25acf85429b6c9550787b2d9", 2),
            Spell("StoneCall", "5181c2ed0190fc34b8a1162783af5bf4", 2),
            Spell("SummonElementalSmallBase", "970c6db48ff0c6f43afc9dbb48780d03", 2),
            Spell("SummonMonsterIIBase", "1724061e89c667045a6891179ee2e8e7", 2),
            Spell("Web", "134cb6d492269aa4f8662700ef57449f", 2),
            Spell("ArrowOfLaw", "dd2a5a6e76611c04e9eac6254fcf8c6b", 2),
            Spell("BurningArc", "eaac3d36e0336cb479209a6f65e25e7c", 2),
            Spell("FrigidTouchCast", "b6010dda6333bcf4093ce20f0063cd41", 2),
            Spell("MoltenOrb", "42a65895ba0cb3a42b6019039dd2bff1", 2),
            Spell("ScorchingRay", "cdb106d53c65bbc4086183d54c3b97c7", 2),
            Spell("SoundBurst", "c3893092a333b93499fd0a21845aa265", 2),
            Spell("CausticBeam", ModBlueprintIds.Spells.CausticBeam, 2),
            Spell("EmperorsWrath", ModBlueprintIds.Spells.EmperorsWrath, 2),
            Spell("ForceRay", ModBlueprintIds.Spells.ForceRay, 2),
            Spell("FrostBlast", ModBlueprintIds.Spells.FrostBlast, 2),

            Spell("SilverDarts", "b0ffc8eaff404f2e9e0a3ee9f4c35486", 3),
            Spell("SpikedPit", "46097f610219ac445b4d6403fc596b9f", 3),
            Spell("StinkingCloud", "68a9e6d7256f1354289a39003a46d826", 3),
            Spell("SummonMonsterIIIBase", "5d61dde0020bbf54ba1521f7ca0229dc", 3),
            Spell("ArchonsAura", "e67efd8c84f69d24ab472c9f546fff7e", 3),
            Spell("ArchonsTrumpet", "c6c516e59ca34f34aefb82a850287b97", 3),
            Spell("BatteringBlast", "0a2f7c6aa81bc6548ac7780d8b70bcbc", 3),
            Spell("BurningEntangle", "8a76293f5ab8485da95ef6293a11358c", 3),
            Spell("CallLightning", "2a9ef0e0b5822a24d88b16673a267456", 3),
            Spell("Fireball", "2d81362af43aeac4387a3d4fced489c3", 3),
            Spell("ForcePunchCast", "fc58ddcff6ab1394eb6c18e9126bb990", 3),
            Spell("HolyWhisper", "5f1ca17be3ba44949be427f18e696d9b", 3),
            Spell("LightningBolt", "d2cff9243a7ee804cb6d5be47af30c73", 3),
            Spell("SearingLight", "bf0accce250381a44b857d4af6c8e10d", 3),
            Spell("ThunderingDrums", "c26eeeeabf732914ba723f2b67fe9b9d", 3),
            Spell("VengefulComets", "0e1272506f9f4480b7c3e7e1e53b6439", 3),
            Spell("VitriolicBlast", ModBlueprintIds.Spells.VitriolicBlast, 3),

            Spell("AcidPit", "1407fb5054d087d47a4c40134c809f12", 4),
            Spell("DimensionDoorBase", "4a648b57935a59547b7a2ee86fb4f26a", 4),
            Spell("SummonElementalMediumBase", "e42b1dbff4262c6469a9ff0a6ce730e3", 4),
            Spell("SummonMonsterIVBase", "7ed74a3ec8c458d4fb50b192fd7be6ef", 4),
            Spell("TouchOfSlimeCast", "84ccca10da2a4484c89a837fbea2a829", 4),
            Spell("ChaosHammer", "c42ac3feb96d1e54e9bc77c84082f05f", 4),
            Spell("ControlledFireball", "f72f8f03bf0136c4180cd1d70eb773a5", 4),
            Spell("DivinePower", "ef16771cb05d1344989519e87f25b3c5", 4),
            Spell("DragonsBreath", "5e826bcdfde7f82468776b55315b2403", 4),
            Spell("FlameStrike", "f9910c76efc34af41b6e43d5d8752f0f", 4),
            Spell("HolySmite", "ad5ed5ea4ec52334a94e975a64dad336", 4),
            Spell("HolySword", "bea9deffd3ab6734c9534153ddc70bde", 4),
            Spell("IceStorm", "fcb028205a71ee64d98175ff39a0abf9", 4),
            Spell("OrdersWrath", "1ec8f035d8329134d96cdc7b90fdc2e1", 4),
            Spell("ResoundingBlow", "9047cb1797639924487ec0ad566a3fea", 4),
            Spell("SacredNimbus", "bf74b3b54c21a9344afe9947546e036f", 4),
            Spell("ShieldOfDawn", "62888999171921e4dafb46de83f4d67d", 4),
            Spell("Shout", "f09453607e683784c8fca646eec49162", 4),
            Spell("UnholyBlight", "a02cf51787df937489ef5d4cf5970335", 4),
            Spell("VolcanicStorm", "16ce660837fb2544e96c3b7eaad73c63", 4),
            Spell("CorrosiveCascade", ModBlueprintIds.Spells.CorrosiveCascade, 4),
            Spell("FrozenLance", ModBlueprintIds.Spells.FrozenLance, 4),
            Spell("Thunderhead", ModBlueprintIds.Spells.Thunderhead, 4),

            Spell("AcidicSpray", "c543eef6d725b184ea8669dd09b3894c", 5),
            Spell("Cloudkill", "548d339ba87ee56459c98e80167bdf10", 5),
            Spell("HungryPit", "f63f4d1806b78604a952b3958892ce1c", 5),
            Spell("SummonElementalLargeBase", "89404dd71edc1aa42962824b44156fe5", 5),
            Spell("SummonMonsterVBase", "630c8b85d9f07a64f917d79cb5905741", 5),
            Spell("CallLightningStorm", "d5a36a7ee8177be4f848b953d1c53c84", 5),
            Spell("Cleanse", "be2062d6d85f4634ea4f26e9e858c3b8", 5),
            Spell("ConeOfCold", "e7c530f8137630f4d9d7ee1aa7b1edc0", 5),
            Spell("FireSnake", "ebade19998e1f8542a1b55bd4da766b3", 5),
            Spell("IcyPrison", "65e8d23aef5e7784dbeb27b1fca40931", 5),
            Spell("KiShout", "5c8cde7f0dcec4e49bfa2632dfe2ecc0", 5),
            Spell("ProfaneNimbus", "b56521d58f996cd4299dab3f38d5fe31", 5),

            Spell("AcidFog", "dbf99b00cd35d0a4491c6cc9e771b487", 6),
            Spell("ChainsOfLight", "f8cea58227f59c64399044a82c9735c4", 6),
            Spell("SummonElementalHugeBase", "766ec978fa993034f86a372c8eb1fc10", 6),
            Spell("SummonMonsterVIBase", "e740afbab0147944dab35d83faa0ae1c", 6),
            Spell("Arbitrament", "0f5bd128c76dd374b8cb9111e3b5186b", 6),
            Spell("BladeBarrier", "36c8971e91f1745418cc3ffdfac17b74", 6),
            Spell("Blasphemy", "bd10c534a09f44f4ea632c8b8ae97145", 6),
            Spell("BrilliantInspiration", "a5c56f0f699daec44b7aedd8b273b08a", 6),
            Spell("ChainLightning", "645558d63604747428d55f0dd3a4cb58", 6),
            Spell("ColdIceStrike", "5ef85d426783a5347b420546f91a677b", 6),
            Spell("Dictum", "302ab5e241931a94881d323a7844ae8f", 6),
            Spell("ElementalAssessor", "6303b404df12b0f4793fa0763b21dd2c", 6),
            Spell("HellfireRay", "700cfcbd0cb2975419bcab7dbb8c6210", 6),
            Spell("HolyWord", "4737294a66c91b844842caee8cf505c8", 6),
            Spell("PoisonBreath", "b5be90707c17a9643b90d90b7c4096e2", 6),
            Spell("ShoutGreater", "fd0d3840c48cafb44bb29e8eb74df204", 6),
            Spell("Sirocco", "093ed1d67a539ad4c939d9d05cfe192c", 6),
            Spell("WordOfChaos", "69f2e7aff2d1cd148b8075ee476515b1", 6),

            Spell("JoyfulRapture", "15a04c40f84545949abeedef7279751a", 7),
            Spell("SummonElementalGreaterBase", "8eb769e3b583f594faabe1cfdb0bb696", 7),
            Spell("SummonMonsterVIIBase", "ab167fd8203c1314bac6568932f1752f", 7),
            Spell("WalkThroughSpace", "368d7cf2fb69d8a46be5a650f5a5a173", 7),
            Spell("CausticEruption", "8c29e953190cc67429dc9c701b16b7c2", 7),
            Spell("FireStorm", "e3d0dfe1c8527934294f241e0ae96a8d", 7),
            Spell("JoltingPortent", "0dd638688edf68a4da865752d7b9ee82", 7),
            Spell("PrismaticSpray", "b22fd434bdb60fb4ba1068206402c4cf", 7),
            Spell("Sunbeam", "1fca0ba2fdfe2994a8c8bc1f0f2fc5b1", 7),

            Spell("RiftOfRuin", "dd3dacafcf40a0145a5824c838e2698d", 8),
            Spell("Seamantle", "7ef49f184922063499b8f1346fb7f521", 8),
            Spell("SummonElementalElderBase", "8a7f8c1223bda1541b42fd0320cdbe2b", 8),
            Spell("SummonMonsterVIIIBase", "d3ac756a229830243a72e84f3ab050d0", 8),
            Spell("PolarRay", "17696c144a0194c478cbe402b496cb23", 8),
            Spell("Stormbolts", "7cfbefe0931257344b2cb7ddc4cdff6f", 8),
            Spell("Sunburst", "e96424f70ff884947b06f41a765b7658", 8),

            Spell("ClashingRocks", "01300baad090d634cb1a1b2defe068d6", 9),
            Spell("SummonMonsterIXBase", "52b5df2a97df18242aec67610616ded0", 9),
            Spell("Tsunami", "d8144161e352ca846a73cf90e85bf9ac", 9),
            Spell("IcyPrisonMass", "1852a9393a23d5741b650a1ea7078abc", 9),
            Spell("Implosion", "78abd9a61abf4c80a8a8cf05ff55f033", 9),
            Spell("MeteorSwarm", "5e36df08c71748f7936bce310181fb71", 9),
            Spell("WindsOfVengeance", "5d8f1da2fdc0b9242af9f326f9e507be", 9)
        };

        public static IReadOnlyList<ClassSpellDefinition> GetAll()
        {
            return Spells;
        }

        private static ClassSpellDefinition Spell(string displayName, string spellGuid, int spellLevel)
        {
            return new ClassSpellDefinition(displayName, spellGuid, spellLevel);
        }
    }
}
