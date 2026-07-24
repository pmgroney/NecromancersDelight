using System.Collections.Generic;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using wotr_mod.Infrastructure;
using wotr_mod.Spells.Modifiers;

namespace wotr_mod.Spells
{
    internal static class SpellRegistry
    {
        public static IReadOnlyList<SpellDefinition> GetAll()
        {
            return new[]
            {
                Spell(GameBlueprintIds.Spells.ScorchingRay, ModBlueprintIds.Spells.EmperorsWrath, "WotrMod_EmperorsWrath", 2, SpellSchool.Evocation, "emperors_wrath", "Icons\\emperors_wrath.png", Ray(DamageEnergyType.Electricity, SpellDescriptor.Electricity)),
                Spell(GameBlueprintIds.Spells.ScorchingRay, ModBlueprintIds.Spells.FrostBlast, "WotrMod_FrostBlast", 2, SpellSchool.Evocation, "frost_blast", null, Ray(DamageEnergyType.Cold, SpellDescriptor.Cold)),
                Spell(GameBlueprintIds.Spells.ScorchingRay, ModBlueprintIds.Spells.CausticBeam, "WotrMod_CausticBeam", 2, SpellSchool.Evocation, "caustic_beam", null, Ray(DamageEnergyType.Acid, SpellDescriptor.Acid)),
                Spell(GameBlueprintIds.Spells.ScorchingRay, ModBlueprintIds.Spells.ForceRay, "WotrMod_ForceRay", 2, SpellSchool.Evocation, "force_ray", "Icons\\force_ray_arcane.png", ForceRay()),
                Spell(GameBlueprintIds.Spells.MagicMissile, ModBlueprintIds.Spells.BoneSpike, "WotrMod_BoneSpike", 1, SpellSchool.Necromancy, "bone_spike", "Icons\\bone_spike.png", Missile(DamageEnergyType.Unholy, SpellDescriptor.Death, SpellSchool.Necromancy)),
                Spell(GameBlueprintIds.Spells.MagicMissile, ModBlueprintIds.Spells.AcidMissile, "WotrMod_AcidMissile", 1, SpellSchool.Evocation, "acid_missile", null, Missile(DamageEnergyType.Acid, SpellDescriptor.Acid, SpellSchool.Evocation)),
                Spell(GameBlueprintIds.Spells.MagicMissile, ModBlueprintIds.Spells.FireMissile, "WotrMod_FireMissile", 1, SpellSchool.Evocation, "fire_missile", null, Missile(DamageEnergyType.Fire, SpellDescriptor.Fire, SpellSchool.Evocation)),
                Spell(GameBlueprintIds.Spells.MagicMissile, ModBlueprintIds.Spells.ElectricMissile, "WotrMod_ElectricMissile", 1, SpellSchool.Evocation, "electric_missile", null, Missile(DamageEnergyType.Electricity, SpellDescriptor.Electricity, SpellSchool.Evocation)),
                Spell(GameBlueprintIds.Spells.MagicMissile, ModBlueprintIds.Spells.IceMissile, "WotrMod_IceMissile", 1, SpellSchool.Evocation, "ice_missile", null, Missile(DamageEnergyType.Cold, SpellDescriptor.Cold, SpellSchool.Evocation)),
                Spell(GameBlueprintIds.Spells.Fireball, ModBlueprintIds.Spells.VitriolicBlast, "WotrMod_VitriolicBurst", 3, SpellSchool.Evocation, "vitriolic_blast", "Icons\\vitriolic_blast.png", Fireball(DamageEnergyType.Acid, SpellDescriptor.Acid, SpellSchool.Evocation)),
                Spell(GameBlueprintIds.Spells.AcidicSpray, ModBlueprintIds.Spells.CorrosiveCascade, "WotrMod_CorrosiveCascade", 4, SpellSchool.Evocation, "corrosive_cascade", "Icons\\corrosive_cascade.png", new CorrosiveCascadeModifier()),
                Spell(GameBlueprintIds.Spells.Snowball, ModBlueprintIds.Spells.FrozenLance, "WotrMod_FrozenLance", 4, SpellSchool.Evocation, "frozen_lance", "Icons\\frozen_lance.png", new FrozenLanceModifier()),
                Spell(GameBlueprintIds.Spells.Sirocco, ModBlueprintIds.Spells.Thunderhead, "WotrMod_Thunderhead", 4, SpellSchool.Evocation, "thunderhead", "Icons\\thunderhead_256.png", new ThunderheadModifier()),
                Spell(GameBlueprintIds.Spells.Fireball, ModBlueprintIds.Spells.VitriolicSphere, "WotrMod_VitriolicSphere", 5, SpellSchool.Evocation, "vitriolic_sphere", "Icons\\vitriolic_sphere.png", new VitriolicSphereModifier()),
                Spell(GameBlueprintIds.Spells.ConeOfCold, ModBlueprintIds.Spells.AbsoluteZero, "WotrMod_AbsoluteZero", 6, SpellSchool.Evocation, "absolute_zero", "Icons\\absolute_zero.png", new AbsoluteZeroModifier()),
                Spell(GameBlueprintIds.Spells.HellfireRay, ModBlueprintIds.Spells.AcidHellfireRay, "WotrMod_AcidHellfireRay", 6, SpellSchool.Evocation, "acid_hellfire_ray", null, HellfireRayVariant(DamageEnergyType.Acid, SpellDescriptor.Acid)),
                Spell(GameBlueprintIds.Spells.HellfireRay, ModBlueprintIds.Spells.ColdHellfireRay, "WotrMod_ColdHellfireRay", 6, SpellSchool.Evocation, "cold_hellfire_ray", null, HellfireRayVariant(DamageEnergyType.Cold, SpellDescriptor.Cold)),
                Spell(GameBlueprintIds.Spells.HellfireRay, ModBlueprintIds.Spells.ElectricHellfireRay, "WotrMod_ElectricHellfireRay", 6, SpellSchool.Evocation, "electric_hellfire_ray", null, HellfireRayVariant(DamageEnergyType.Electricity, SpellDescriptor.Electricity)),
                Spell(GameBlueprintIds.Spells.HellfireRay, ModBlueprintIds.Spells.FireHellfireRay, "WotrMod_FireHellfireRay", 6, SpellSchool.Evocation, "fire_hellfire_ray", null, new ConfigureSpellModifier(SpellSchool.Evocation)),
                Spell(GameBlueprintIds.Spells.ConeOfCold, ModBlueprintIds.Spells.DissolutionWave, "WotrMod_DissolutionWave", 7, SpellSchool.Evocation, "dissolution_wave", "Icons\\dissolution_wave.png", new DissolutionWaveModifier()),
                Spell(GameBlueprintIds.Spells.Sirocco, ModBlueprintIds.Spells.GlacialPrison, "WotrMod_GlacialPrison", 7, SpellSchool.Evocation, "glacial_prison", "Icons\\glacial_prison.png", new GlacialPrisonModifier()),
                Spell(GameBlueprintIds.Spells.Sirocco, ModBlueprintIds.Spells.CataclysmicStorm, "WotrMod_CataclysmicStorm", 8, SpellSchool.Evocation, "cataclysmic_storm", "Icons\\cataclysmic_storm.png", new CataclysmicStormModifier()),
                Spell(GameBlueprintIds.Spells.PolarRay, ModBlueprintIds.Spells.CausticOblivion, "WotrMod_CausticOblivion", 8, SpellSchool.Evocation, "caustic_oblivion", "Icons\\caustic_oblivion.png", new CausticOblivionModifier()),
                Spell(GameBlueprintIds.Spells.Sirocco, ModBlueprintIds.Spells.PolarCatastrophe, "WotrMod_PolarCatastrophe", 8, SpellSchool.Evocation, "polar_catastrophe", "Icons\\polar_catastrophe.png", new PolarCatastropheModifier()),
                Spell(GameBlueprintIds.Spells.Sirocco, ModBlueprintIds.Spells.HeavensWrath, "WotrMod_HeavensWrath", 9, SpellSchool.Evocation, "heavens_wrath", "Icons\\heavens_wrath.png", new HeavensWrathModifier()),
                Spell(GameBlueprintIds.Spells.Fireball, ModBlueprintIds.Spells.VitriolicApocalypse, "WotrMod_VitriolicApocalypse", 9, SpellSchool.Evocation, "vitriolic_apocalypse", "Icons\\vitriolic_apocalypse.png", new VitriolicApocalypseModifier()),
                Spell(GameBlueprintIds.Spells.Fireball, ModBlueprintIds.Spells.CorpseExplosion, "WotrMod_CorpseExplosion", 2, SpellSchool.Necromancy, "corpse_explosion", "Icons\\corpse_explosion.png", new CorpseExplosionModifier()),
                Spell(GameBlueprintIds.Spells.Entangle, ModBlueprintIds.Spells.EldritchHorror, "WotrMod_EldritchHorror", 3, SpellSchool.Necromancy, "eldritch_horror", "Icons\\eldritch_horror.png", EldritchHorror()),
                Spell(GameBlueprintIds.Spells.MagicMissile, ModBlueprintIds.Spells.GraveboltCascade, "WotrMod_GraveboltCascade", 4, SpellSchool.Necromancy, "gravebolt_cascade", "Icons\\gravebolt_cascade.png", new GraveboltCascadeModifier()),
                Spell(GameBlueprintIds.Spells.Entangle, ModBlueprintIds.Spells.HellOnEarth, "WotrMod_HellOnEarth", 9, SpellSchool.Necromancy, "hell_on_earth", "Icons\\hell_on_earth.png", HellOnEarth()),
                Spell(GameBlueprintIds.Spells.ScorchingRay, ModBlueprintIds.Spells.DeathRay, "WotrMod_DeathRay", 2, SpellSchool.Necromancy, "death_ray", "Icons\\death_ray.png", DeathRay()),
                Spell(GameBlueprintIds.Spells.HellfireRay, ModBlueprintIds.Spells.ShadowHellfireRay, "WotrMod_ShadowHellfireRay", 6, SpellSchool.Necromancy, "shadow_hellfire_ray", "Icons\\shadowblight_stream.png", HellfireRayVariant(DamageEnergyType.NegativeEnergy, SpellDescriptor.Death, SpellSchool.Necromancy)),
                Spell(GameBlueprintIds.Spells.AuraOfGreaterCourage, ModBlueprintIds.Spells.DespairOfTheSepulchre, "WotrMod_DespairOfTheSepulchre", 5, SpellSchool.Necromancy, "despair_of_sepulchre", "Icons\\despair_of_sepulchre.png", new DespairOfTheSepulchreModifier()),
                Spell(GameBlueprintIds.Spells.FingerOfDeath, ModBlueprintIds.Spells.HarvestSoul, "WotrMod_HarvestSoul", 7, SpellSchool.Necromancy, "harvest_soul", "Icons\\harvest_soul.png", new HarvestSoulModifier()),
                Spell(GameBlueprintIds.Spells.FalseLife, ModBlueprintIds.Spells.HarvestTheFallen, "WotrMod_HarvestTheFallen", 5, SpellSchool.Necromancy, "harvest_the_fallen", "Icons\\harvest_the_fallen.png", new HarvestTheFallenModifier())
            };
        }

        private static SpellDefinition Spell(
            string baseGuid,
            string newGuid,
            string internalName,
            int level,
            SpellSchool school,
            string key,
            string iconPath,
            ISpellModifier modifier)
        {
            return new SpellDefinition(
                baseGuid,
                newGuid,
                internalName,
                level,
                school,
                $"wotr_mod.spell.{key}.name",
                $"wotr_mod.spell.{key}.description",
                iconPath,
                modifier);
        }

        private static ISpellModifier Ray(DamageEnergyType energy, SpellDescriptor descriptor)
        {
            return new CompositeSpellModifier(
                new ConfigureSpellModifier(SpellSchool.Evocation),
                new DamageTypeSpellModifier(
                    SpellDescriptor.Fire,
                    descriptor,
                    DamageEnergyType.Fire,
                    energy,
                    scaling: new DamageTypeSpellModifier.ScalingConfig
                    {
                        RankType = Kingmaker.Enums.AbilityRankType.Default,
                        Progression = Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.StartPlusDivStep,
                        StartLevel = 1,
                        StepLevel = 4
                    }));
        }

        private static ISpellModifier ForceRay()
        {
            return new CompositeSpellModifier(
                new ConfigureSpellModifier(SpellSchool.Evocation),
                new DamageTypeSpellModifier(
                    SpellDescriptor.Fire,
                    SpellDescriptor.Force,
                    DamageEnergyType.Fire,
                    null,
                    toForce: true,
                    diceType: DiceType.D4,
                    scaling: new DamageTypeSpellModifier.ScalingConfig
                    {
                        RankType = Kingmaker.Enums.AbilityRankType.Default,
                        Progression = Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.StartPlusDivStep,
                        StartLevel = 1,
                        StepLevel = 4
                    }));
        }

        private static ISpellModifier DeathRay()
        {
            return new CompositeSpellModifier(
                new ConfigureSpellModifier(SpellSchool.Necromancy),
                new DamageTypeSpellModifier(
                    SpellDescriptor.Fire,
                    SpellDescriptor.Death,
                    DamageEnergyType.Fire,
                    DamageEnergyType.NegativeEnergy,
                    scaling: new DamageTypeSpellModifier.ScalingConfig
                    {
                        RankType = Kingmaker.Enums.AbilityRankType.Default,
                        Progression = Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.StartPlusDivStep,
                        StartLevel = 1,
                        StepLevel = 4
                    }));
        }

        private static ISpellModifier HellfireRayVariant(
            DamageEnergyType energy,
            SpellDescriptor descriptor,
            SpellSchool school = SpellSchool.Evocation)
        {
            return new CompositeSpellModifier(
                new ConfigureSpellModifier(school),
                new DamageTypeSpellModifier(
                    SpellDescriptor.Fire,
                    descriptor,
                    DamageEnergyType.Fire,
                    energy));
        }

        private static ISpellModifier Missile(DamageEnergyType energy, SpellDescriptor descriptor, SpellSchool school)
        {
            return new CompositeSpellModifier(
                new ConfigureSpellModifier(school),
                new DamageTypeSpellModifier(
                    SpellDescriptor.Force,
                    descriptor,
                    null,
                    energy,
                    fromForce: true,
                    diceType: DiceType.D6,
                    scaling: new DamageTypeSpellModifier.ScalingConfig
                    {
                        RankType = Kingmaker.Enums.AbilityRankType.Default,
                        Progression = Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.OnePlusDiv2
                    }));
        }

        private static ISpellModifier Fireball(DamageEnergyType energy, SpellDescriptor descriptor, SpellSchool school)
        {
            return new CompositeSpellModifier(
                new ConfigureSpellModifier(school),
                new DamageTypeSpellModifier(
                    SpellDescriptor.Fire,
                    descriptor,
                    DamageEnergyType.Fire,
                    energy,
                    scaling: new DamageTypeSpellModifier.ScalingConfig
                    {
                        RankType = Kingmaker.Enums.AbilityRankType.Default,
                        Progression = Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.AsIs
                    }));
        }

        private static ISpellModifier EldritchHorror()
        {
            return new AreaNecromancyModifier(
                ModBlueprintIds.AreaEffects.EldritchHorror,
                20,
                DiceType.D4,
                new Kingmaker.UnitLogic.Mechanics.ContextValue
                {
                    ValueType = Kingmaker.UnitLogic.Mechanics.ContextValueType.Rank,
                    ValueRank = Kingmaker.Enums.AbilityRankType.Default
                },
                AreaNecromancyModifier.Rounds(10),
                (area, context) => AreaNecromancyModifier.ConfigureCasterLevelRank(
                    area,
                    context,
                    Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.DelayedStartPlusDivStep,
                    5,
                    2));
        }

        private static ISpellModifier HellOnEarth()
        {
            return new AreaNecromancyModifier(
                ModBlueprintIds.AreaEffects.HellOnEarth,
                20,
                DiceType.D12,
                new Kingmaker.UnitLogic.Mechanics.ContextValue
                {
                    ValueType = Kingmaker.UnitLogic.Mechanics.ContextValueType.Rank,
                    ValueRank = Kingmaker.Enums.AbilityRankType.Default
                },
                AreaNecromancyModifier.Rounds(10),
                (area, context) => AreaNecromancyModifier.ConfigureCasterLevelRank(
                    area,
                    context,
                    Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.AsIs,
                    0,
                    0));
        }
    }
}
