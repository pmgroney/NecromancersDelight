using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Localization;
using wotr_mod.Patches;

namespace wotr_mod.Content
{
    internal sealed partial class CompanionInstaller
    {
        private static readonly string[] BillyIdleBanterLines =
        {
            "Breathing is optional. Still deciding if I miss it.",
            "Step. Aim. Release. Repeat. Eternity's great for practice.",
            "Irori teaches perfection. Unfortunately, he never covered spontaneous undeath.",
            "You ever notice how the living waste a lot of motion?",
            "No heartbeat. Fewer distractions.",
            "Still me. Just... crunchier.",
            "I don't get tired anymore. Honestly, huge upgrade.",
            "I used to meditate to ignore discomfort. Now my nerves just gave up entirely.",
            "Focus is easier when your knees stopped filing complaints.",
            "I should probably be more alarmed by all this.",
            "Good posture is important. Even when half your spine is decorative."
        };

        private static readonly string[] BillyMovementLines =
        {
            "Careful. I don't creak, but I still sneak.",
            "Every step is intentional.",
            "Stay sharp. I already dulled once.",
            "Quiet. Let them embarrass themselves first.",
            "Positioning wins fights.",
            "Discipline over panic.",
            "I don't rush. I disappoint enemies at a reasonable pace."
        };

        private static readonly string[] BillyCombatStartLines =
        {
            "Ah. Violence with structure.",
            "Let's see who disappoints Irori first.",
            "Targets acquired.",
            "Focus.",
            "Try not to die. Paperwork gets confusing.",
            "Excellent. A teaching opportunity.",
            "Form over fury. Fury pulls muscles."
        };

        private static readonly string[] BillyRangedAttackLines =
        {
            "Breathe--oh. Right.",
            "Release.",
            "Stillness. Then strike.",
            "Predictable.",
            "You moved. Brave choice.",
            "Center mass. Usually reliable.",
            "Efficiency.",
            "Missed. Disgusting."
        };

        private static readonly string[] BillyOnHitLines =
        {
            "There it is.",
            "Correct.",
            "Better.",
            "That felt professional.",
            "Progress.",
            "Precision matters. Unlike enthusiasm."
        };

        private static readonly string[] BillyOnKillLines =
        {
            "Rest. Properly this time.",
            "Cycle corrected.",
            "You can stop screaming now.",
            "That's one less problem.",
            "Remarkably fragile.",
            "Still improving."
        };

        private static readonly string[] BillyTakingDamageLines =
        {
            "Rude.",
            "Structural integrity compromised.",
            "I felt that. Which seems unnecessary.",
            "Adjustment required.",
            "Pain is... weird now."
        };

        private static readonly string[] BillyLowHealthLines =
        {
            "Pieces are becoming negotiable.",
            "I should really tighten a few bolts.",
            "This is getting medically irresponsible.",
            "Losing cohesion."
        };

        private static readonly string[] BillyBuffingLines =
        {
            "Enhancement accepted.",
            "Clarity.",
            "Excellent. Magical cheating.",
            "Focus restored.",
            "Irori approves. Probably."
        };

        private static readonly string[] BillyPartyBanterLines =
        {
            "If I fall apart, at least throw the useful pieces.",
            "I don't sleep, so I'll take watch. Apparently forever.",
            "Don't worry--I'm very stable. Emotionally less so.",
            "I've stopped worrying about dying. Very freeing.",
            "If you hear rattling, that's either me or a tactical warning."
        };

        private static readonly string[] BillyIroriFlavorLines =
        {
            "Perfection takes time. I apparently have plenty.",
            "Discipline surviving death feels excessive.",
            "Mastery doesn't require a pulse. Good to know.",
            "The body failed. The will filed an appeal.",
            "Irori probably did not intend this lesson."
        };

        private static readonly string[] BillyCheckFailLines =
        {
            "Missed. Disgusting.",
            "Adjustment required.",
            "That could have gone better."
        };

        private static readonly string[] BillyStealthLines =
        {
            "Careful. I don't creak, but I still sneak.",
            "Quiet. Let them embarrass themselves first."
        };

        private static readonly Dictionary<string, string> BillyBarkLocalizationKeys = BuildBillyBarkLocalizationKeys();
        private static readonly Dictionary<string, string> BillyBarkAkEvents = BuildBillyBarkAkEvents();
        private static readonly HashSet<string> TextOnlyBillyBanterLineIds = new HashSet<string>
        {
            "BILLY_ARU_001_A",
            "BILLY_ARU_003_A",
            "BILLY_ARU_004_A",
            "BILLY_CAM_002_A",
            "BILLY_GRY_003_A",
            "BILLY_LAN_002_A",
            "BILLY_LAN_002_B",
            "BILLY_LAN_003_A",
            "BILLY_NEN_004_A",
            "BILLY_NEN_004_B",
            "BILLY_NEN_004_C",
            "BILLY_SEE_004_A",
            "BILLY_SEE_004_B",
            "BILLY_SOS_001_A",
            "BILLY_ULB_001_A",
            "BILLY_ULB_001_B",
            "BILLY_ULB_002_A",
            "BILLY_ULB_003_A",
            "BILLY_ULB_003_B",
            "BILLY_ULB_004_A",
            "BILLY_ULB_004_B"
        };

        private static readonly HashSet<string> TextOnlyBillySceneInterjectionIds = new HashSet<string>
        {
            "SCN_HOUNDHEART_LANN_RING",
            "SCN_HOUNDHEART_ELAN_ATTACKS_CURL",
            "SCN_HOUNDHEART_REDEMPTION",
            "SCN_HURLUN_JUDGE_JURY",
            "SCN_HURLUN_BLASPHEMY",
            "SCN_REGILL_RECRUITMENT_DISCIPLINE",
            "SCN_REGILL_CRUELTY_PURPOSE",
            "SCN_RADIANCE_LEGACY",
            "SCN_STAUNTON_DEATH_DIDNT_CHANGE",
            "SCN_STAUNTON_NO_PEACE",
            "SCN_COMMANDER_BECOMES_LICH",
            "SCN_ZACHARIUS_DAERAN_SCANDAL",
            "SCN_ZACHARIUS_SEELAH_UNDEAD_EVIL",
            "SCN_ZACHARIUS_NENIO_VOLUNTEERS",
            "SCN_ZACHARIUS_ULBRIG_UNDEAD_FOLLY",
            "SCN_ZACHARIUS_OFFER_WARNING"
        };

        private static readonly BillyBanterReplacement[] CanonicalBillyBanterReplacements =
        {
            new BillyBanterReplacement("BILLY_SEE_001", "Seelah", "4061abcd06662f347ad3aefb525eae08", "54be53f0b35bf3c4592a97ae335fe765", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SEE_001_A", "Good. I was worried the rigor mortis was making me seem aloof.") }),
            new BillyBanterReplacement("BILLY_SEE_002", "Seelah", "a21e1d8d1e7dfec498efb6a91d026e74", "54be53f0b35bf3c4592a97ae335fe765", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SEE_002_A", "Been there. Mixed results.") }),
            new BillyBanterReplacement("BILLY_SEE_003", "Seelah", "7cb70f2e08a04f74691441ac52f62c06", "54be53f0b35bf3c4592a97ae335fe765", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SEE_003_A", "You have a remarkably violent approach to emotional support. I like it.") }),
            new BillyBanterReplacement("BILLY_SEE_004", "Seelah", "38a4ba127aa4bbb44a0518fe21e06134", "54be53f0b35bf3c4592a97ae335fe765", new[] { BillyLine("BILLY_SEE_004_A", "Have you ever wondered why the gods don't simply solve all of this themselves?"), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_SEE_004_B", "Fair point. Let us remain grateful for divine neglect.") }),
            new BillyBanterReplacement("BILLY_CAM_001", "Camellia", "9cd57f7eb455e094997f8b12974cd645", "397b090721c41044ea3220445300e1b8", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_CAM_001_A", "I usually buy them dinner first.") }),
            new BillyBanterReplacement("BILLY_CAM_002", "Camellia", "d664ebef4df2dc34e86684c4fb994269", "397b090721c41044ea3220445300e1b8", new[] { VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_CAM_002_A", "They're a little late.") }),
            new BillyBanterReplacement("BILLY_CAM_003", "Camellia", "fc837e9d30d9efa40936524e50eb9ece", "397b090721c41044ea3220445300e1b8", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_CAM_003_A", "Most people call that a nightmare. With you, I'm beginning to suspect it's foreplay.") }),
            new BillyBanterReplacement("BILLY_CAM_004", "Camellia", "f7f17bbfc8415274981c9fc6da967dae", "397b090721c41044ea3220445300e1b8", new[] { VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_CAM_004_A", "I did once. Wouldn't recommend it.") }),
            new BillyBanterReplacement("BILLY_LAN_001", "Lann", "bb6c454e7f460dc4590ba6b4e490a89c", "cb29621d99b902e4da6f5d232352fbda", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_LAN_001_A", "You'd be amazed how quickly immortality becomes mostly waiting for everyone else to finish eating.") }),
            new BillyBanterReplacement("BILLY_LAN_002", "Lann", "c59c1553e823d1c41a379a20d84a9bba", "cb29621d99b902e4da6f5d232352fbda", new[] { BillyLine("BILLY_LAN_002_A", "Have you ever considered making a list of things you want to do before you die?"), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_LAN_002_B", "Thirty? I only managed one. It didn't take.") }),
            new BillyBanterReplacement("BILLY_LAN_003", "Lann", "b8c630dfdd7de7d49abffa0c1912b63d", "cb29621d99b902e4da6f5d232352fbda", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_LAN_003_A", "Show-off.") }),
            new BillyBanterReplacement("BILLY_LAN_004", "Lann", "c4592758c5cc69548b7a69f8e4775841", "cb29621d99b902e4da6f5d232352fbda", new[] { BillyLine("BILLY_LAN_004_A", "If you die before me, may I have your body?"), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_LAN_004_B", "I meant for burial rites, you sick bastard.") }),
            new BillyBanterReplacement("BILLY_WEN_001", "Wenduag", "6e57435ac3323ae43ba66b239b33e55c", "ae766624c03058440a036de90a7f2009", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_WEN_001_A", "No. It's judgment.") }),
            new BillyBanterReplacement("BILLY_WEN_002", "Wenduag", "69c5922b760e32b409a8bf07df78803c", "ae766624c03058440a036de90a7f2009", new[] { BillyLine("BILLY_WEN_002_A", "Do you ever look around and simply appreciate something beautiful?"), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_WEN_002_B", "Efficient. Disturbing, but efficient.") }),
            new BillyBanterReplacement("BILLY_WEN_003", "Wenduag", "70643b831c3af9e4980b8a36fcd29944", "ae766624c03058440a036de90a7f2009", new[] { BillyLine("BILLY_WEN_003_A", "You don't seem particularly concerned about what happens after death."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_WEN_003_B", "Would you like a moment to reconsider that sentence?") }),
            new BillyBanterReplacement("BILLY_WEN_004", "Wenduag", "2bc823ca0a4335344b9078dc2eddd98f", "ae766624c03058440a036de90a7f2009", new[] { BillyLine("BILLY_WEN_004_A", "You speak of strength as though you were born with it."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_WEN_004_B", "That I understand. Irori would too. He'd probably object to the fucking-and-eating portion of your philosophy, but the foundation is sound.") }),
            new BillyBanterReplacement("BILLY_WOL_001", "Woljif", "2990e03bdfde72842b8a51ba79f245ec", "766435873b1361c4287c351de194e5f9", new[] { BillyLine("BILLY_WOL_001_A", "Irori teaches that silence clears the mind of distraction, allowing one to contemplate the perfection of the self."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_WOL_001_B", "And yet it worked.") }),
            new BillyBanterReplacement("BILLY_WOL_002", "Woljif", "232bd336bd9bb90409d1ab6d3a364a74", "766435873b1361c4287c351de194e5f9", new[] { BillyLine("BILLY_WOL_002_A", "You know, Woljif, I've always thought there was a certain animal magnetism about you."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_WOL_002_B", "You have vastly overestimated how badly I want to see your cock.") }),
            new BillyBanterReplacement("BILLY_WOL_003", "Woljif", "5a59dc95c91885a459a834e464c99a1f", "766435873b1361c4287c351de194e5f9", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_WOL_003_A", "Excellent. I was afraid this relationship was becoming emotionally demanding.") }),
            new BillyBanterReplacement("BILLY_WOL_004", "Woljif", "c65144cc11fcb054ea4a771af8017b4f", "766435873b1361c4287c351de194e5f9", new[] { BillyLine("BILLY_WOL_004_A", "Why do you call everyone 'uncle'?"), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_WOL_004_B", "Billy will do. 'Uncle Billy' makes the murder sound incestuous.") }),
            new BillyBanterReplacement("BILLY_EMB_001", "Ember", "5f92344dc9660cd4c825e757fca2fc9c", "2779754eecffd044fbd4842dba55312c", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_EMB_001_A", "Remind me never to play cards with you.") }),
            new BillyBanterReplacement("BILLY_EMB_002", "Ember", "716e575e463998c4faec2da38445cea3", "2779754eecffd044fbd4842dba55312c", new[] { BillyLine("BILLY_EMB_002_A", "You know, someday I'll die too. Properly, I mean."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_EMB_002_B", "...Thank you, Ember.") }),
            new BillyBanterReplacement("BILLY_EMB_003", "Ember", "427c780c455537447976bc47c94b10fe", "2779754eecffd044fbd4842dba55312c", new[] { VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_EMB_003_A", "I asked the same question once. I strongly recommend not investigating it personally.") }),
            new BillyBanterReplacement("BILLY_EMB_004", "Ember", "99b8916614b5b5344b3ce82fb1ce5f62", "2779754eecffd044fbd4842dba55312c", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_EMB_004_A", "Don't say it aloud. The universe can hear you.") }),
            new BillyBanterReplacement("BILLY_NEN_001", "Nenio", "bc81817cfdbb4c84fa2ab230f235c99c", "1b893f7cf2b150e4f8bc2b3c389ba71d", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_NEN_001_A", "Nenio. Stop looking at me while you explain it.") }),
            new BillyBanterReplacement("BILLY_NEN_002", "Nenio", "c1ec0c002bcd7904daae15d0d39cf56a", "1b893f7cf2b150e4f8bc2b3c389ba71d", new[] { BillyLine("BILLY_NEN_002_A", "You're remarkably unconcerned by your own mortality."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_NEN_002_B", "Your humility will also be difficult to replace.") }),
            new BillyBanterReplacement("BILLY_NEN_003", "Nenio", "6361993ecf2842ed9172b7fce255d5c0", "1b893f7cf2b150e4f8bc2b3c389ba71d", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_NEN_003_A", "Fascinating. I've been pinching myself for years and the evidence remains inconclusive.") }),
            new BillyBanterReplacement("BILLY_NEN_004", "Nenio", "a1e5fd6ebac9d334ba1b5d0ecf90a29d", "1b893f7cf2b150e4f8bc2b3c389ba71d", new[] { BillyLine("BILLY_NEN_004_A", "You know, forgetting everything after a passionate evening does have certain advantages."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_NEN_004_B", "Please don't write my name next to that."), BillyLine("BILLY_NEN_004_C", "You're writing my name next to that.") }),
            new BillyBanterReplacement("BILLY_DAE_001", "Daeran", "67d9d4f962a8798469c96d5435c8e0db", "096fc4a96d675bb45a0396bcaa7aa993", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_DAE_001_A", "You mean me, don't you?"), BillyLine("BILLY_DAE_001_B", "I fucking knew it.") }),
            new BillyBanterReplacement("BILLY_DAE_002", "Daeran", "d6fa8e614250cca49aa5523460f38b8f", "096fc4a96d675bb45a0396bcaa7aa993", new[] { BillyLine("BILLY_DAE_002_A", "For a man blessed with divine magic, you show remarkably little gratitude."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_DAE_002_B", "Sensible. The interest alone is murder.") }),
            new BillyBanterReplacement("BILLY_DAE_003", "Daeran", "658fc8c27ae74b4980a8d2f5545e2c65", "096fc4a96d675bb45a0396bcaa7aa993", new[] { VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_DAE_003_A", "I don't have a functioning liver."), BillyLine("BILLY_DAE_003_B", "I didn't say no.") }),
            new BillyBanterReplacement("BILLY_SOS_001", "Sosiel", "346b9015a15b47c4592a5a68d1701d16", "1cbbbb892f93c3d439f8417ad7cbb6aa", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SOS_001_A", "Wonderful. I can start escalating.") }),
            new BillyBanterReplacement("BILLY_SOS_002", "Sosiel", "3e724576853181d429d2cf0330c67af3", "1cbbbb892f93c3d439f8417ad7cbb6aa", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SOS_002_A", "How much time do you have?") }),
            new BillyBanterReplacement("BILLY_SOS_003", "Sosiel", "eff4ea2c007479f41a601ddb2fa1aa8d", "1cbbbb892f93c3d439f8417ad7cbb6aa", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SOS_003_A", "Mostly they're haunted by the living. The dead are surprisingly quiet.") }),
            new BillyBanterReplacement("BILLY_SOS_004", "Sosiel", "0b3c48a62a9e8824c9a71f7844aaaa2d", "1cbbbb892f93c3d439f8417ad7cbb6aa", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_SOS_004_A", "I admire your optimism.") }),
            new BillyBanterReplacement("BILLY_REG_001", "Regill", "48581979d08e32b4fb709baa93d3061b", "0d37024170b172346b3769df92a971f5", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_REG_001_A", "I believe that particular ship has sailed.") }),
            new BillyBanterReplacement("BILLY_REG_002", "Regill", "7c21aabd31584ed4fad09c4e9c245048", "0d37024170b172346b3769df92a971f5", new[] { BillyLine("BILLY_REG_002_A", "Surely death entitles a man to some time off."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_REG_002_B", "I walked directly into that one.") }),
            new BillyBanterReplacement("BILLY_REG_003", "Regill", "d6d964c6ea067674086079b26ad90a62", "0d37024170b172346b3769df92a971f5", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_REG_003_A", "I agree."), BillyLine("BILLY_REG_003_B", "Don't look so pleased. I hate it too.") }),
            new BillyBanterReplacement("BILLY_REG_004", "Regill", "50cc2948a02671942b869334ee05198a", "0d37024170b172346b3769df92a971f5", new[] { BillyLine("BILLY_REG_004_A", "Interesting fact: the average adult human has two hundred and six bones."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_REG_004_B", "I suddenly understand why Nenio keeps trying to poison you.") }),
            new BillyBanterReplacement("BILLY_GRY_001", "Greybor", "d00a11ea86ffce64c93f18bb6ab0c6e3", "f72bb7c48bb3e45458f866045448fb58", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_GRY_001_A", "There is a fairly significant flaw in the premise of that compliment.") }),
            new BillyBanterReplacement("BILLY_GRY_002", "Greybor", "7fc86f27cca043743b139a473c034d88", "f72bb7c48bb3e45458f866045448fb58", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_GRY_002_A", "I'm a cleric of Irori. If this were a sermon, you'd be doing push-ups.") }),
            new BillyBanterReplacement("BILLY_GRY_003", "Greybor", "27299db961995ed45b7ede09f0f59cbd", "f72bb7c48bb3e45458f866045448fb58", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_GRY_003_A", "Magic. I've never understood the insistence on touching everything you kill.") }),
            new BillyBanterReplacement("BILLY_GRY_004", "Greybor", "9cb4ad5d3faf8404d977aeb2f75c05ab", "f72bb7c48bb3e45458f866045448fb58", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_GRY_004_A", "Death is surprisingly bad at enforcing that sort of thing.") }),
            new BillyBanterReplacement("BILLY_ARU_001", "Arueshalae", "51963fc5f5611a449a35f483414a2dee", "a352873d37ec6c54c9fa8f6da3a6b3e1", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_ARU_001_A", "Not anymore.") }),
            new BillyBanterReplacement("BILLY_ARU_002", "Arueshalae", "147ccda9b96c95e49aae7152feaa5f76", "a352873d37ec6c54c9fa8f6da3a6b3e1", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_ARU_002_A", "Sometimes leaving isn't a choice.") }),
            new BillyBanterReplacement("BILLY_ARU_003", "Arueshalae", "5f0001705f616eb41953921ac1c63229", "a352873d37ec6c54c9fa8f6da3a6b3e1", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_ARU_003_A", "Because mortality encourages terrible long-term planning.") }),
            new BillyBanterReplacement("BILLY_ARU_004", "Arueshalae", "a71cd333a7dec7c4684ad9416de19b00", "a352873d37ec6c54c9fa8f6da3a6b3e1", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_ARU_004_A", "I died in it and still came back. I suppose that counts as a favorable review.") }),
            new BillyBanterReplacement("BILLY_ULB_001", "Ulbrig", "18c41757f41042d59aca2d772e9780fa", "42f0d5ec3dc844feb44b04507a7c1bfc", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_ULB_001_A", "I'm undead, Ulbrig. Not a spirit."), BillyLine("BILLY_ULB_001_B", "Though I appreciate you finding an entirely new way to get it wrong.") }),
            new BillyBanterReplacement("BILLY_ULB_002", "Ulbrig", "9fac68cb19d04a0a8ac37300445ad7ad", "42f0d5ec3dc844feb44b04507a7c1bfc", new[] { VanillaLine(BanterSourceRole.FirstPhrase), BillyLine("BILLY_ULB_002_A", "I'm going to need you to choose a different verb.") }),
            new BillyBanterReplacement("BILLY_ULB_003", "Ulbrig", "9c46d14146414d2b813aa6169e5991f3", "42f0d5ec3dc844feb44b04507a7c1bfc", new[] { BillyLine("BILLY_ULB_003_A", "A hundred years of uninterrupted sleep sounds rather pleasant."), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_ULB_003_B", "Finally. A Sarkorian custom I can support.") }),
            new BillyBanterReplacement("BILLY_ULB_004", "Ulbrig", "77b6755ce52d49e09696a7b6e35e5874", "42f0d5ec3dc844feb44b04507a7c1bfc", new[] { BillyLine("BILLY_ULB_004_A", "So Sarkoris really had no proper civilization? No forks, no manners, nothing?"), VanillaLine(BanterSourceRole.Response), BillyLine("BILLY_ULB_004_B", "I withdraw the question before you demonstrate.") })
        };
        private static readonly BillySceneInterjection[] BillySceneInterjections =
        {
            new BillySceneInterjection("SCN_HOUNDHEART_LANN_RING", "e777df0d978fd0b4da332937e281d63e", new[]
                {
                    new BillySceneInterjectionLine("SCN_HOUNDHEART_LANN_RING_A", "194340b0e9dc53dbae4ff08d748be858", "194340b0-e9dc-53db-ae4f-f08d748be858", "And people say romance is dead. Present company excluded.")
                }),
            new BillySceneInterjection("SCN_HOUNDHEART_ELAN_ATTACKS_CURL", "cba5a385ca4c13848bc3b0b182dd5a43", new[]
                {
                    new BillySceneInterjectionLine("SCN_HOUNDHEART_ELAN_ATTACKS_CURL_A", "fa59597bca6c5cabaa758ce2f7756881", "fa59597b-ca6c-5cab-aa75-8ce2f7756881", "Certainty is a poor substitute for self-control.")
                }),
            new BillySceneInterjection("SCN_HOUNDHEART_REDEMPTION", "97a5486259aa78d48be5cf366168e9d9", new[]
                {
                    new BillySceneInterjectionLine("SCN_HOUNDHEART_REDEMPTION_A", "2e5b446f1c965b8491495f9bb81de722", "2e5b446f-1c96-5b84-9149-5f9bb81de722", "If being an idiot were grounds for execution, this crusade would be considerably smaller.")
                }),
            new BillySceneInterjection("SCN_HURLUN_JUDGE_JURY", "60aa49983cb5dac45b645a115bfc2667", new[]
                {
                    new BillySceneInterjectionLine("SCN_HURLUN_JUDGE_JURY_A", "4894984318f3563f8ebefe2c4586258e", "48949843-18f3-563f-8ebe-fe2c4586258e", "Efficient. He's eliminated the troublesome middleman known as justice.")
                }),
            new BillySceneInterjection("SCN_HURLUN_BLASPHEMY", "3815a1e17cb32cf4d8fe65d06b780fd8", new[]
                {
                    new BillySceneInterjectionLine("SCN_HURLUN_BLASPHEMY_A", "01cc7f8c023159aba49a33d736e3f976", "01cc7f8c-0231-59ab-a49a-33d736e3f976", "Technically, blasphemy requires him to be wrong about the god.")
                }),
            new BillySceneInterjection("SCN_REGILL_RECRUITMENT_DISCIPLINE", "f434b3dfef8361d41939be7b9f52ac0e", new[]
                {
                    new BillySceneInterjectionLine("SCN_REGILL_RECRUITMENT_DISCIPLINE_A", "0d78da0c7b0a53899bae8540f2e5216f", "0d78da0c-7b0a-5389-9bae-8540f2e5216f", "Discipline is admirable. What one chooses to do with it remains rather important.")
                }),
            new BillySceneInterjection("SCN_REGILL_CRUELTY_PURPOSE", "2c9b602f446ef2d4b84db18b9389741a", new[]
                {
                    new BillySceneInterjectionLine("SCN_REGILL_CRUELTY_PURPOSE_A", "801a1174948b589197b38f74f28ee96d", "801a1174-948b-5891-97b3-8f74f28ee96d", "Discipline without purpose is merely suffering with better posture.")
                }),
            new BillySceneInterjection("SCN_NENIO_REVEAL_LANN", "916fdd8d928795a42a9abcdf1cdba523", new[]
                {
                    new BillySceneInterjectionLine("SCN_NENIO_REVEAL_LANN_A", "026c20cb48f0501dac7d60a61651ffbf", "026c20cb-48f0-501d-ac7d-60a61651ffbf", "I've been doing that for years. People assume wisdom.")
                }),
            new BillySceneInterjection("SCN_NENIO_REVEAL_DANGER", "6b65edfd32448cd44a381b961125537f", new[]
                {
                    new BillySceneInterjectionLine("SCN_NENIO_REVEAL_DANGER_A", "f1b543f5ba755061988dc53ea16bcb36", "f1b543f5-ba75-5061-988d-c53ea16bcb36", "I tested that hypothesis once. The results were inconclusive.")
                },
                new[] { "4563f5cb2208c1b43b9032beae24ae0a" },
                "2d4c91b69ee64b0f9098b1edcdd3611b"),
            new BillySceneInterjection("SCN_NENIO_REVEAL_CAMELLIA_PRIVACY", "1a1ed64070db3824e8604e6d6648dfff", new[]
                {
                    new BillySceneInterjectionLine("SCN_NENIO_REVEAL_CAMELLIA_PRIVACY_A", "c8cd593d3ad459d5a2e2d15afac3fd1b", "c8cd593d-3ad4-59d5-a2e2-d15afac3fd1b", "Yes. Imagine the horror.")
                }),
            new BillySceneInterjection("SCN_DAERAN_FIRST_MEET_LONELY", "7c6ba5d07d21bd548aa310d0b32fa67b", new[]
                {
                    new BillySceneInterjectionLine("SCN_DAERAN_FIRST_MEET_LONELY_A", "faa33f9af0085a66824f51f51f80c238", "faa33f9a-f008-5a66-824f-51f51f80c238", "Those aren't friends, Ember. They're furniture that drinks.")
                }),
            new BillySceneInterjection("BILLY_SCENE_002", "42f4cc0a1948a8647bcad0cd405efe23", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_002", "32c152e9d4d4590796e713667ceda2ab", "32c152e9-d4d4-5907-96e7-13667ceda2ab", "I've met enough clergy to assure you the two are not mutually exclusive.")
                }),
            new BillySceneInterjection("BILLY_SCENE_003", "49b15396177edab4ebb46fa5b3f959d4", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_003", "ae7efe8c5dba556a87d95498ed66c7ce", "ae7efe8c-5dba-556a-87d9-5498ed66c7ce", "Look on the bright side. If we fail, he won't have anyone left to invoice.")
                }),
            new BillySceneInterjection("BILLY_SCENE_004", "80f0af9f5e5fc7e43b9975b279a41de7", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_004", "fb6252569ebc54e98bea7cf119a16cb8", "fb625256-9ebc-54e9-8bea-7cf119a16cb8", "And here I thought I was supposed to be the unsettling one.")
                }),
            new BillySceneInterjection("BILLY_SCENE_005", "e4e711820cfcc7f41a9a0a355ac08fb1", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_005", "061a4636b1395b30aa5fca6e972d6391", "061a4636-b139-5b30-aa5f-ca6e972d6391", "Regret is useful only while you're still willing to learn from it.")
                }),
            new BillySceneInterjection("BILLY_SCENE_006", "798cfed412194074eb0f53759d53c120", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_006", "54817db5acbd5987abfd2e970da212f4", "54817db5-acbd-5987-abfd-2e970da212f4", "On that, paladin, we agree.")
                }),
            new BillySceneInterjection("BILLY_SCENE_007", "4883404530f5a89468d05573b7f8051c", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_007", "67f2dd1597fa5254b4c40964cb2a0360", "67f2dd15-97fa-5254-b4c4-0964cb2a0360", "Nenio, perhaps wait until after we've determined whether she's going to kill us.")
                }),
            new BillySceneInterjection("BILLY_SCENE_008", "0e6996174d355a946aa8581d61e7395a", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_008", "adf0e71e04d85754816c6d7da309c9b4", "adf0e71e-04d8-5754-816c-6d7da309c9b4", "I'd also like an answer to the 'someone who died is saved' portion.")
                }),
            new BillySceneInterjection("BILLY_SCENE_009", "fe1b35b58c4b71c49849e7a2df6c2db8", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_009", "d16f157ef0a45783ade89789c8aaa4c1", "d16f157e-f0a4-5783-ade8-9789c8aaa4c1", "Your confidence has reassured me completely.")
                }),
            new BillySceneInterjection("BILLY_SCENE_010", "6be2e11b82435384984904d2eca129e8", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_010", "bc7e31ef51935951abc61b62561fc56d", "bc7e31ef-5193-5951-abc6-1b62561fc56d", "A comprehensive plan. I particularly admire the consistency.")
                }),
            new BillySceneInterjection("BILLY_SCENE_011", "4f8ecb2c52b7ec848930924e23152d90", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_011", "0cdaa86329515d84af81e76c7ce27a8f", "0cdaa863-2951-5d84-af81-e76c7ce27a8f", "Tempting, but she knows where I sleep.")
                }),
            new BillySceneInterjection("BILLY_SCENE_012", "af36fec407b21b9439562233be4f2b56", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_012", "9d1a7ffb83d650eb81ce979c99b4c0a1", "9d1a7ffb-83d6-50eb-81ce-979c99b4c0a1", "Remarkably, the church has the same problem.")
                }),
            new BillySceneInterjection("BILLY_SCENE_013", "478cb2d2e05ac374881de0ef61969675", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_013", "e3910e29de0d580c904598be732f7030", "e3910e29-de0d-580c-9045-98be732f7030", "I didn't know she could do that.")
                }),
            new BillySceneInterjection("BILLY_SCENE_014", "e4403a7c1af9f4c4ca9e7b1492825dc1", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_014", "6274368b82115818b713a78ea9b1a9b8", "6274368b-8211-5818-b713-a78ea9b1a9b8", "If it helps, age doesn't improve the experience.")
                }),
            new BillySceneInterjection("BILLY_SCENE_015", "1fa3c030f3dcfa24eb1c5787e60c0230", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_015", "f6c7c013bb98599588124591c9653a7f", "f6c7c013-bb98-5995-8812-4591c9653a7f", "Nenio, for once in your life, please stop doing science.")
                }),
            new BillySceneInterjection("BILLY_SCENE_016", "b289c5a0480ec1847b3dfb25b7c722c9", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_016", "df047ddb942250a6ba227e6b953adde7", "df047ddb-9422-50a6-ba22-7e6b953adde7", "Faith that demands certainty is just a transaction.")
                }),
            new BillySceneInterjection("BILLY_SCENE_017", "d9f391f62b3307742a968f662ca4c940", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_017", "cac6e6370237573c9091f5436b707c9b", "cac6e637-0237-573c-9091-f5436b707c9b", "And this is why no god has invited you anywhere in person.")
                }),
            new BillySceneInterjection("BILLY_SCENE_018", "008b44bd33e335a44b3ad629da80b2cf", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_018", "1b1ba5347e25513e8d2bbffc4711d4bb", "1b1ba534-7e25-513e-8d2b-bffc4711d4bb", "You don't have to understand it. Just don't mistake what we are for who we are.")
                }),
            new BillySceneInterjection("BILLY_SCENE_019", "3965490c7cdded5499f7eed9dc339f68", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_019", "d2dceef7ec3657bd996eb8261835c9a9", "d2dceef7-ec36-57bd-996e-b8261835c9a9", "That may be the most honest thing you've ever said.")
                }),
            new BillySceneInterjection("BILLY_SCENE_020", "55f091a91ce31884392e3dfe6a31f916", new[]
                {
                    new BillySceneInterjectionLine("BILLY_SCENE_020", "8659f2ff86e951c38339e6ce601213a3", "8659f2ff-86e9-51c3-8339-e6ce601213a3", "Kill him first. We'll worry about interior decorating afterward.")
                }),
            new BillySceneInterjection("SCN_RADIANCE_LEGACY", "f75bb9463b053424c8dd819f4cd13bb9", new[]
                {
                    new BillySceneInterjectionLine("SCN_RADIANCE_LEGACY_A", "b98f997075975af6b83880701565f81a", "b98f9970-7597-5af6-b838-80701565f81a", "Legacy is what remains after perfection proves unattainable.")
                },
                new[] { "77d27b1abad91364691bcdbbd708dd43" },
                "aeace238aa70401b8d5730afb5ac38c5"),
            new BillySceneInterjection("SCN_STAUNTON_DEATH_DIDNT_CHANGE", "b77ea2cd37dd52947b0d06e36d52cff9", new[]
                {
                    new BillySceneInterjectionLine("SCN_STAUNTON_DEATH_DIDNT_CHANGE_A", "9a7527deefda57a1b601d793276b72c0", "9a7527de-efda-57a1-b601-d793276b72c0", "Why would it? Dying doesn't improve a man. He has to manage that himself.")
                }),
            new BillySceneInterjection("SCN_STAUNTON_NO_PEACE", "b5d6283ffb24c8a40a2772d9178499cb", new[]
                {
                    new BillySceneInterjectionLine("SCN_STAUNTON_NO_PEACE_A", "d6de48397876575d9267818517701208", "d6de4839-7876-575d-9267-818517701208", "Peace isn't something death grants. Trust me.")
                }),
            new BillySceneInterjection("SCN_COMMANDER_BECOMES_LICH", "da926d9e1e5615247a7b78bd779ecaa1", new[]
                {
                    new BillySceneInterjectionLine("SCN_COMMANDER_BECOMES_LICH_A", "9e267f45177758b6adddcab77c83943f", "9e267f45-1777-58b6-addd-cab77c83943f", "You think dying is the difficult part."),
                    new BillySceneInterjectionLine("SCN_COMMANDER_BECOMES_LICH_B", "6c8052a5e4e75c10b90759460264961c", "6c8052a5-e4e7-5c10-b907-59460264961c", "It isn't."),
                    new BillySceneInterjectionLine("SCN_COMMANDER_BECOMES_LICH_C", "996ac5f2ad6555038f30519dad96b1e5", "996ac5f2-ad65-5503-8f30-519dad96b1e5", "What comes next is deciding whether the person you were still matters.")
                }),
            new BillySceneInterjection("SCN_ZACHARIUS_DAERAN_SCANDAL", "ab580ebc40d45064ba0f39a60b0a0d78", new[]
                {
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_DAERAN_SCANDAL_A", "f1497d09c4c252b7951e011e3aeb65f3", "f1497d09-c4c2-52b7-951e-011e3aeb65f3", "Please include me in the correspondence. I haven't enjoyed religious scandal this much in years.")
                }),
            new BillySceneInterjection("SCN_ZACHARIUS_SEELAH_UNDEAD_EVIL", "9d4c099f7494576428b81bc936a26f56", new[]
                {
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_SEELAH_UNDEAD_EVIL_A", "824208f52ec457cab0d3e7ee063c22b2", "824208f5-2ec4-57ca-b0d3-e7ee063c22b2", "Well. This has become awkward.")
                }),
            new BillySceneInterjection("SCN_ZACHARIUS_NENIO_VOLUNTEERS", "343749cf99e09df4181719b6b726e098", new[]
                {
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_NENIO_VOLUNTEERS_A", "6a81d822229851c4afadbc7e761dac27", "6a81d822-2298-51c4-afad-bc7e761dac27", "No."),
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_NENIO_VOLUNTEERS_B", "f7f3bcaeb3a0597b8c9279a9392a1105", "f7f3bcae-b3a0-597b-8c92-79a9392a1105", "Absolutely fucking not.")
                }),
            new BillySceneInterjection("SCN_ZACHARIUS_ULBRIG_UNDEAD_FOLLY", "119ffc00295542f19ff06ee72f0f5e98", new[]
                {
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_ULBRIG_UNDEAD_FOLLY_A", "2a3ce3d81b725e9a84342e8d9fc335cb", "2a3ce3d8-1b72-5e9a-8434-2e8d9fc335cb", "Ulbrig."),
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_ULBRIG_UNDEAD_FOLLY_B", "7388c6a9bd35559997ba87d0f0c29025", "7388c6a9-bd35-5599-97ba-87d0f0c29025", "I'm standing right here.")
                }),
            new BillySceneInterjection("SCN_ZACHARIUS_OFFER_WARNING", "bce20069fa618c841bbf47475a1b3298", new[]
                {
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_OFFER_WARNING_A", "9ec53b719b345c7dbace5e955b1d248f", "9ec53b71-9b34-5c7d-bace-5e955b1d248f", "Undeath isn't power without a price."),
                    new BillySceneInterjectionLine("SCN_ZACHARIUS_OFFER_WARNING_B", "0ff3ea7dd6ad53a88cac5bff90f0d3bd", "0ff3ea7d-d6ad-53a8-8cac-5bff90f0d3bd", "The disturbing part is how long you have to regret paying it.")
                })
        };

        private sealed class BillyBanterReplacement
        {
            public BillyBanterReplacement(
                string lineId,
                string companionName,
                string sourceGuid,
                string companionGuid,
                BillyBanterSequenceLine[] sequence)
            {
                LineId = lineId;
                CompanionName = companionName;
                SourceGuid = sourceGuid;
                CompanionGuid = companionGuid;
                Sequence = sequence ?? Array.Empty<BillyBanterSequenceLine>();
            }

            public string LineId { get; }
            public string CompanionName { get; }
            public string SourceGuid { get; }
            public string CompanionGuid { get; }
            public BillyBanterSequenceLine[] Sequence { get; }
            public IEnumerable<BillyBanterSequenceLine> BillyLines =>
                Sequence.Where(line => line.Kind == BillyBanterLineKind.Billy);

            public string SourceName => $"Banter {LineId}";
        }

        private static BillyBanterSequenceLine BillyLine(string lineId, string text)
        {
            return new BillyBanterSequenceLine(BillyBanterLineKind.Billy, BanterSourceRole.None, lineId, text);
        }

        private static BillyBanterSequenceLine VanillaLine(BanterSourceRole sourceRole)
        {
            return new BillyBanterSequenceLine(BillyBanterLineKind.Vanilla, sourceRole, null, null);
        }

        private enum BillyBanterLineKind
        {
            Vanilla,
            Billy
        }

        private enum BanterSourceRole
        {
            None,
            FirstPhrase,
            Response
        }

        private sealed class BillyBanterSequenceLine
        {
            public BillyBanterSequenceLine(
                BillyBanterLineKind kind,
                BanterSourceRole sourceRole,
                string lineId,
                string text)
            {
                Kind = kind;
                SourceRole = sourceRole;
                LineId = lineId;
                Text = text;
            }

            public BillyBanterLineKind Kind { get; }
            public BanterSourceRole SourceRole { get; }
            public string LineId { get; }
            public string Text { get; }
            public string EventSuffix => BuildEventSuffix(LineId);
            public string LocalizationKey => $"wotr_mod.companion.billy.banter.{LineId.ToLowerInvariant()}";
            public string AkEvent => $"Play_CMP_Billy_Dialog_{EventSuffix}";

            public BillyBanterRuntimePatch.SequenceLine ToRuntimeLine(LocalizedString text)
            {
                return new BillyBanterRuntimePatch.SequenceLine(Kind == BillyBanterLineKind.Billy, SourceRole.ToString(), text);
            }

            private static string BuildEventSuffix(string lineId)
            {
                if (string.IsNullOrEmpty(lineId))
                {
                    return string.Empty;
                }

                var parts = lineId.Split('_');
                if (parts.Length < 3)
                {
                    return lineId;
                }

                var suffix = parts.Length > 3 ? "_" + string.Join("_", parts.Skip(3)) : string.Empty;
                return $"Banter{ToTitleCase(parts[1])}_{parts[2]}{suffix}";
            }

            private static string ToTitleCase(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return value;
                }

                return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
            }
        }

        private sealed class BillySceneInterjection
        {
            public BillySceneInterjection(
                string sceneId,
                string anchorCueGuid,
                BillySceneInterjectionLine[] lines,
                string[] predecessorAnswerGuids = null,
                string anchorCloneGuid = null)
            {
                SceneId = sceneId;
                AnchorCueGuid = anchorCueGuid;
                Lines = lines ?? Array.Empty<BillySceneInterjectionLine>();
                PredecessorAnswerGuids = predecessorAnswerGuids ?? Array.Empty<string>();
                AnchorCloneGuid = anchorCloneGuid;
            }

            public string SceneId { get; }
            public string AnchorCueGuid { get; }
            public BillySceneInterjectionLine[] Lines { get; }
            public string[] PredecessorAnswerGuids { get; }
            public string AnchorCloneGuid { get; }
        }

        private sealed class BillySceneInterjectionLine
        {
            public BillySceneInterjectionLine(string lineId, string cueGuid, string localizationKey, string text)
            {
                LineId = lineId;
                CueGuid = cueGuid;
                LocalizationKey = localizationKey;
                Text = text;
            }

            public string LineId { get; }
            public string CueGuid { get; }
            public string LocalizationKey { get; }
            public string Text { get; }
            public string AkEvent => "Play_CMP_Billy_Dialog_" + LineId;
        }

    }
}
