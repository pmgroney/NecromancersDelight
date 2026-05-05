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
                Spell(GameBlueprintIds.Spells.Fireball, ModBlueprintIds.Spells.CorpseExplosion, "WotrMod_CorpseExplosion", 2, SpellSchool.Necromancy, "corpse_explosion", "Icons\\corpse_explosion.png", new CorpseExplosionModifier()),
                Spell(GameBlueprintIds.Spells.Entangle, ModBlueprintIds.Spells.EldritchHorror, "WotrMod_EldritchHorror", 3, SpellSchool.Necromancy, "eldritch_horror", "Icons\\eldritch_horror.png", EldritchHorror()),
                Spell(GameBlueprintIds.Spells.Entangle, ModBlueprintIds.Spells.HellOnEarth, "WotrMod_HellOnEarth", 9, SpellSchool.Necromancy, "hell_on_earth", "Icons\\hell_on_earth.png", HellOnEarth()),
                Spell(GameBlueprintIds.Spells.ScorchingRay, ModBlueprintIds.Spells.DeathRay, "WotrMod_DeathRay", 2, SpellSchool.Necromancy, "death_ray", "Icons\\death_ray.png", DeathRay())
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
                        Progression = Kingmaker.UnitLogic.Mechanics.Components.ContextRankProgression.OnePlusDivStep,
                        StartLevel = 1,
                        StepLevel = 2
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
                    7,
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
