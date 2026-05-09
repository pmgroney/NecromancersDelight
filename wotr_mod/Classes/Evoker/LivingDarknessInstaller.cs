using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using UnityModManagerNet;
using wotr_mod.Classes;
using wotr_mod.Features;
using wotr_mod.Infrastructure;
using wotr_mod.Spells;

namespace wotr_mod.Classes.Evoker
{
    internal sealed class LivingDarknessInstaller
    {
        private readonly BlueprintTool _blueprints;
        private readonly LocalizationTool _localization;
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly SpellIconLoader _icons;

        public LivingDarknessInstaller(
            BlueprintTool blueprints,
            LocalizationTool localization,
            UnityModManager.ModEntry.ModLogger logger,
            SpellIconLoader icons)
        {
            _blueprints = blueprints;
            _localization = localization;
            _logger = logger;
            _icons = icons;
        }

        public void Install(BlueprintProgression shadowbornBloodline, BlueprintCharacterClass characterClass)
        {
            var resource = EnsureLivingDarknessResource(characterClass);
            var feature1 = EnsureLivingDarknessFeature(1, characterClass, resource);
            var feature2 = EnsureLivingDarknessFeature(2, characterClass, resource);
            var feature3 = EnsureLivingDarknessFeature(3, characterClass, resource);
            var feature4 = EnsureLivingDarknessFeature(4, characterClass, resource);

            ReplaceProgressionFeature(shadowbornBloodline, GameBlueprintIds.Features.BloodlineElementalSpellLevel4, feature1);
            ReplaceProgressionFeature(shadowbornBloodline, GameBlueprintIds.Features.BloodlineElementalSpellLevel5, feature2);
            ReplaceProgressionFeature(shadowbornBloodline, GameBlueprintIds.Features.BloodlineElementalSpellLevel6, feature3);
            ReplaceProgressionFeature(shadowbornBloodline, GameBlueprintIds.Features.BloodlineElementalSpellLevel7, feature4);
            _blueprints.AddProgressionUiGroup(shadowbornBloodline, feature1, feature2, feature3, feature4);
            if (characterClass?.Progression != null)
            {
                _blueprints.AddProgressionUiGroup(characterClass.Progression, feature1, feature2, feature3, feature4);
            }
        }

        private BlueprintFeature EnsureLivingDarknessFeature(
            int tier,
            BlueprintCharacterClass characterClass,
            BlueprintAbilityResource resource)
        {
            var featureGuid = TierFeatureGuid(tier);
            var feature = _blueprints.Get<BlueprintFeature>(featureGuid);
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    name = $"WotrMod_LivingDarknessFeature{tier}",
                    AssetGuid = BlueprintGuid.Parse(featureGuid),
                    Ranks = 1,
                    IsClassFeature = true
                };
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            feature.Ranks = 1;
            feature.IsClassFeature = true;
            _blueprints.SetUnitFactDisplay(
                feature,
                _localization.Text(TierNameKey(tier)),
                _localization.Text(TierDescriptionKey(tier)));

            var ability = EnsureLivingDarknessAbility(tier, characterClass, resource);
            var addFacts = new AddFacts { name = $"$AddFacts$LivingDarkness{tier}" };
            _blueprints.SetAddFacts(addFacts, ability);
            var addResources = new AddAbilityResources
            {
                name = $"$AddAbilityResources$LivingDarkness{tier}",
                RestoreAmount = true
            };
            _blueprints.SetAddAbilityResourcesResource(addResources, resource);
            if (tier > 1)
            {
                var previousFeature = _blueprints.Get<BlueprintFeature>(TierFeatureGuid(tier - 1));
                var removePrevious = new RemoveFeatureOnApply { name = $"$RemoveFeatureOnApply$LivingDarkness{tier}" };
                _blueprints.SetRemoveFeatureOnApplyFeature(removePrevious, previousFeature);
                _blueprints.SetComponents(feature, addFacts, addResources, removePrevious);
            }
            else
            {
                _blueprints.SetComponents(feature, addFacts, addResources);
            }
            SetIcon(feature, "Icons\\living_darkness.png");

            return feature;
        }

        private BlueprintAbilityResource EnsureLivingDarknessResource(BlueprintCharacterClass characterClass)
        {
            var resource = _blueprints.Get<BlueprintAbilityResource>(ModBlueprintIds.AbilityResources.LivingDarkness);
            if (resource == null)
            {
                resource = new BlueprintAbilityResource
                {
                    name = "WotrMod_LivingDarknessResource",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.AbilityResources.LivingDarkness)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.AbilityResources.LivingDarkness, resource);
            }

            resource.LocalizedName = _localization.Text(LocalizationIds.Mod.LivingDarknessName1);
            resource.LocalizedDescription = _localization.Text(LocalizationIds.Mod.LivingDarknessDescription1);
            _blueprints.ConfigureAbilityResourceMaxAmount(resource, 0, StatType.Charisma, characterClass, 1);
            return resource;
        }

        private BlueprintAbility EnsureLivingDarknessAbility(
            int tier,
            BlueprintCharacterClass characterClass,
            BlueprintAbilityResource resource)
        {
            var abilityGuid = TierAbilityGuid(tier);
            var ability = _blueprints.Get<BlueprintAbility>(abilityGuid);
            if (ability == null)
            {
                ability = new BlueprintAbility
                {
                    name = $"WotrMod_LivingDarkness{tier}",
                    AssetGuid = BlueprintGuid.Parse(abilityGuid)
                };
                _blueprints.AddCachedBlueprint(abilityGuid, ability);
            }

            ability.Type = AbilityType.Supernatural;
            ability.Range = AbilityRange.Personal;
            ability.ActionType = UnitCommand.CommandType.Standard;
            ability.CanTargetSelf = true;
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = false;
            ability.CanTargetPoint = false;
            ability.SpellResistance = false;
            ability.NotOffensive = true;

            _blueprints.SetAbilityDisplay(
                ability,
                _localization.Text(TierNameKey(tier)),
                _localization.Text(TierDescriptionKey(tier)));

            var auraBuff = EnsureLivingDarknessAuraBuff(tier, characterClass);
            var applyAura = new ContextActionApplyBuff
            {
                name = $"$ContextActionApplyBuff$LivingDarkness{tier}Aura",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = CasterLevelMinutes(),
                IsFromSpell = false,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = false,
                SameDuration = false
            };
            _blueprints.SetApplyBuffActionBuff(applyAura, auraBuff);

            var rank = new ContextRankConfig { name = $"$ContextRankConfig$LivingDarkness{tier}" };
            _blueprints.ConfigureContextRankConfig(rank);

            var resourceLogic = new AbilityResourceLogic { name = $"$AbilityResourceLogic$LivingDarkness{tier}", Amount = 1 };
            _blueprints.SetAbilityResourceLogicResource(resourceLogic, resource);
            _blueprints.SetAbilityResourceLogicSpendResource(resourceLogic, true);

            _blueprints.SetComponents(
                ability,
                resourceLogic,
                rank,
                new AbilityEffectRunAction
                {
                    name = $"$AbilityEffectRunAction$LivingDarkness{tier}",
                    SavingThrowType = SavingThrowType.Unknown,
                    Actions = new ActionList { Actions = new GameAction[] { applyAura } }
                });

            SetIcon(ability, "Icons\\living_darkness.png");
            return ability;
        }

        private BlueprintBuff EnsureLivingDarknessAuraBuff(int tier, BlueprintCharacterClass characterClass)
        {
            var buffGuid = TierAuraBuffGuid(tier);
            var buff = _blueprints.Get<BlueprintBuff>(buffGuid);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = $"WotrMod_LivingDarkness{tier}AuraBuff",
                    AssetGuid = BlueprintGuid.Parse(buffGuid)
                };
                _blueprints.AddCachedBlueprint(buffGuid, buff);
            }

            buff.Stacking = StackingType.Replace;
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(TierNameKey(tier)),
                _localization.Text(TierDescriptionKey(tier)));

            var area = EnsureLivingDarknessArea(tier, characterClass);
            var addArea = new AddAreaEffect { name = $"$AddAreaEffect$LivingDarkness{tier}" };
            _blueprints.SetAddAreaEffect(addArea, area);

            var concealment = new AddConcealment
            {
                name = $"$AddConcealment$LivingDarkness{tier}",
                Concealment = tier == 1 ? Concealment.Partial : Concealment.Total,
                Descriptor = tier == 1 ? ConcealmentDescriptor.Blur : ConcealmentDescriptor.Displacement,
                CheckDistance = false,
                DistanceGreater =  0.Feet(),
                OnlyForAttacks = false
            };

            if (tier >= 2)
            {
                var penetration = new LivingDarknessResistancePenetration
                {
                    name = $"$LivingDarknessResistancePenetration$LivingDarkness{tier}",
                    Penetration = tier == 2 ? 5 : 10
                };
                _blueprints.SetComponents(buff, addArea, concealment, penetration);
            }
            else
            {
                _blueprints.SetComponents(buff, addArea, concealment);
            }

            SetIcon(buff, "Icons\\living_darkness.png");
            return buff;
        }

        private BlueprintAbilityAreaEffect EnsureLivingDarknessArea(int tier, BlueprintCharacterClass characterClass)
        {
            var areaGuid = TierAreaGuid(tier);
            var area = _blueprints.Get<BlueprintAbilityAreaEffect>(areaGuid);
            if (area == null)
            {
                area = new BlueprintAbilityAreaEffect
                {
                    name = $"WotrMod_LivingDarkness{tier}Area",
                    AssetGuid = BlueprintGuid.Parse(areaGuid)
                };
                _blueprints.AddCachedBlueprint(areaGuid, area);
            }

            area.Shape = AreaEffectShape.Cylinder;
            area.Size = TierAreaSize(tier);
            area.SpellResistance = false;
            area.AffectEnemies = true;
            area.AggroEnemies = true;
            area.AffectDead = false;
            area.IgnoreSleepingUnits = false;

            var debuff = EnsureLivingDarknessDebuff(tier);
            var shakenDebuff = tier >= 3 ? EnsureLivingDarknessShakenDebuff() : null;

            var applyDebuff = new ContextActionApplyBuff
            {
                name = $"$ContextActionApplyBuff$LivingDarkness{tier}Debuff",
                Permanent = false,
                UseDurationSeconds = false,
                DurationValue = AreaLinkedDuration(),
                IsFromSpell = false,
                IsNotDispelable = false,
                ToCaster = false,
                AsChild = true,
                SameDuration = true,
                NotLinkToAreaEffect = false,
                IgnoreParentContext = false
            };
            _blueprints.SetApplyBuffActionBuff(applyDebuff, debuff);

            var removeDebuff = new ContextActionRemoveBuff
            {
                name = $"$ContextActionRemoveBuff$LivingDarkness{tier}Debuff",
                RemoveRank = false,
                ToCaster = false,
                OnlyFromCaster = true
            };
            _blueprints.SetRemoveBuffActionBuff(removeDebuff, debuff);

            var enterActions = new List<GameAction> { applyDebuff };
            var exitActions = new List<GameAction> { removeDebuff };
            var roundComponents = new List<BlueprintComponent>();

            if (tier >= 3)
            {
                var applyShakenDebuff = new ContextActionApplyBuff
                {
                    name = $"$ContextActionApplyBuff$LivingDarkness{tier}Shaken",
                    Permanent = false,
                    UseDurationSeconds = false,
                    DurationValue = AreaLinkedDuration(),
                    IsFromSpell = false,
                    IsNotDispelable = false,
                    ToCaster = false,
                    AsChild = true,
                    SameDuration = true,
                    NotLinkToAreaEffect = false,
                    IgnoreParentContext = false
                };
                _blueprints.SetApplyBuffActionBuff(applyShakenDebuff, shakenDebuff);

                var removeShakenDebuff = new ContextActionRemoveBuff
                {
                    name = $"$ContextActionRemoveBuff$LivingDarkness{tier}Shaken",
                    RemoveRank = false,
                    ToCaster = false,
                    OnlyFromCaster = true
                };
                _blueprints.SetRemoveBuffActionBuff(removeShakenDebuff, shakenDebuff);

                var shakenSave = new ContextActionSavingThrow
                {
                    name = $"$ContextActionSavingThrow$LivingDarkness{tier}",
                    Type = SavingThrowType.Will,
                    Actions = new ActionList
                    {
                        Actions = new GameAction[]
                        {
                            new ContextActionConditionalSaved
                            {
                                name = $"$ContextActionConditionalSaved$LivingDarkness{tier}",
                                Succeed = new ActionList { Actions = Array.Empty<GameAction>() },
                                Failed = new ActionList { Actions = new GameAction[] { applyShakenDebuff } }
                            }
                        }
                    }
                };

                enterActions.Add(shakenSave);
                exitActions.Add(removeShakenDebuff);
            }

            if (tier == 4)
            {
                var rankConfig = new ContextRankConfig { name = $"$ContextRankConfig$LivingDarkness{tier}Round" };
                _blueprints.ConfigureContextRankConfig(rankConfig);
                roundComponents.Add(rankConfig);

                var dealDamage = new ContextActionDealDamage
                {
                    name = $"$ContextActionDealDamage$LivingDarkness{tier}",
                    DamageType = new DamageTypeDescription
                    {
                        Type = DamageType.Energy,
                        Energy = DamageEnergyType.NegativeEnergy
                    },
                    Value = new ContextDiceValue
                    {
                        DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                        DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                        BonusValue = new ContextValue
                        {
                            ValueType = ContextValueType.Rank,
                            ValueRank = AbilityRankType.Default
                        }
                    }
                };

                var enemyDamage = new Conditional
                {
                    name = $"$Conditional$LivingDarkness{tier}RoundEnemy",
                    ConditionsChecker = new ConditionsChecker
                    {
                        Operation = Operation.And,
                        Conditions = new Condition[]
                        {
                            new ContextConditionIsEnemy
                            {
                                name = $"$ContextConditionIsEnemy$LivingDarkness{tier}Round"
                            }
                        }
                    },
                    IfTrue = new ActionList { Actions = new GameAction[] { dealDamage } },
                    IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
                };

                var roundAction = new AbilityAreaEffectRunAction
                {
                    name = $"$AbilityAreaEffectRunAction$LivingDarkness{tier}",
                    UnitEnter = new ActionList { Actions = new GameAction[] { EnemyConditional(tier, enterActions) } },
                    UnitExit = new ActionList { Actions = exitActions.ToArray() },
                    UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                    Round = new ActionList { Actions = new GameAction[] { enemyDamage } }
                };

                var components = roundComponents.Concat(new BlueprintComponent[] { roundAction }).ToArray();
                _blueprints.SetComponents(area, components);
            }
            else
            {
                _blueprints.SetComponents(
                    area,
                    new AbilityAreaEffectRunAction
                    {
                        name = $"$AbilityAreaEffectRunAction$LivingDarkness{tier}",
                        UnitEnter = new ActionList { Actions = new GameAction[] { EnemyConditional(tier, enterActions) } },
                        UnitExit = new ActionList { Actions = exitActions.ToArray() },
                        UnitMove = new ActionList { Actions = Array.Empty<GameAction>() },
                        Round = new ActionList { Actions = Array.Empty<GameAction>() }
                    });
            }

            return area;
        }

        private static Conditional EnemyConditional(int tier, List<GameAction> ifTrueActions)
        {
            return new Conditional
            {
                name = $"$Conditional$LivingDarkness{tier}Enemy",
                ConditionsChecker = new ConditionsChecker
                {
                    Operation = Operation.And,
                    Conditions = new Condition[]
                    {
                        new ContextConditionIsEnemy
                        {
                            name = $"$ContextConditionIsEnemy$LivingDarkness{tier}"
                        }
                    }
                },
                IfTrue = new ActionList { Actions = ifTrueActions.ToArray() },
                IfFalse = new ActionList { Actions = Array.Empty<GameAction>() }
            };
        }

        private BlueprintBuff EnsureLivingDarknessDebuff(int tier)
        {
            var buffGuid = TierDebuffGuid(tier);
            var buff = _blueprints.Get<BlueprintBuff>(buffGuid);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = $"WotrMod_LivingDarkness{tier}Debuff",
                    AssetGuid = BlueprintGuid.Parse(buffGuid)
                };
                _blueprints.AddCachedBlueprint(buffGuid, buff);
            }

            buff.Stacking = StackingType.Replace;
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(TierNameKey(tier)),
                _localization.Text(TierDescriptionKey(tier)));

            var savePenalty = new LivingDarknessNegativeEnergySavePenalty
            {
                name = $"$LivingDarknessNegativeEnergySavePenalty$LivingDarkness{tier}",
                Penalty = -tier
            };

            if (tier == 2)
            {
                _blueprints.SetComponents(
                    buff,
                    savePenalty,
                    new AddCondition
                    {
                        name = $"$AddCondition$LivingDarkness{tier}Fatigued",
                        Condition = UnitCondition.Fatigued
                    });
            }
            else
            {
                _blueprints.SetComponents(buff, savePenalty);
            }

            return buff;
        }

        private BlueprintBuff EnsureLivingDarknessShakenDebuff()
        {
            var buff = _blueprints.Get<BlueprintBuff>(ModBlueprintIds.Buffs.LivingDarknessShakenDebuff);
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    name = "WotrMod_LivingDarknessShakenDebuff",
                    AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Buffs.LivingDarknessShakenDebuff)
                };
                _blueprints.AddCachedBlueprint(ModBlueprintIds.Buffs.LivingDarknessShakenDebuff, buff);
            }

            buff.Stacking = StackingType.Replace;
            _blueprints.SetUnitFactDisplay(
                buff,
                _localization.Text(LocalizationIds.Mod.LivingDarknessShakenName),
                _localization.Text(LocalizationIds.Mod.LivingDarknessShakenDescription));
            _blueprints.SetComponents(
                buff,
                new AddCondition
                {
                    name = "$AddCondition$LivingDarknessShakenDebuff",
                    Condition = UnitCondition.Shaken
                });

            return buff;
        }

        private void SetIcon(BlueprintUnitFact fact, string iconPath)
        {
            var icon = _icons.Load(iconPath);
            if (icon != null)
            {
                _blueprints.SetUnitFactIcon(fact, icon);
            }
        }

        private static void ReplaceProgressionFeature(
            BlueprintProgression progression,
            string oldFeatureGuid,
            BlueprintFeatureBase newFeature)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldFeatureGuid));
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature => feature != null && feature.AssetGuid == oldGuid ? newFeature : feature));
            }
        }

        private static string TierFeatureGuid(int tier)
        {
            switch (tier)
            {
                case 1: return ModBlueprintIds.Features.LivingDarknessFeature1;
                case 2: return ModBlueprintIds.Features.LivingDarknessFeature2;
                case 3: return ModBlueprintIds.Features.LivingDarknessFeature3;
                case 4: return ModBlueprintIds.Features.LivingDarknessFeature4;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static string TierAbilityGuid(int tier)
        {
            switch (tier)
            {
                case 1: return ModBlueprintIds.Abilities.LivingDarkness1;
                case 2: return ModBlueprintIds.Abilities.LivingDarkness2;
                case 3: return ModBlueprintIds.Abilities.LivingDarkness3;
                case 4: return ModBlueprintIds.Abilities.LivingDarkness4;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static string TierAuraBuffGuid(int tier)
        {
            switch (tier)
            {
                case 1: return ModBlueprintIds.Buffs.LivingDarkness1AuraBuff;
                case 2: return ModBlueprintIds.Buffs.LivingDarkness2AuraBuff;
                case 3: return ModBlueprintIds.Buffs.LivingDarkness3AuraBuff;
                case 4: return ModBlueprintIds.Buffs.LivingDarkness4AuraBuff;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static string TierDebuffGuid(int tier)
        {
            switch (tier)
            {
                case 1: return ModBlueprintIds.Buffs.LivingDarkness1Debuff;
                case 2: return ModBlueprintIds.Buffs.LivingDarkness2Debuff;
                case 3: return ModBlueprintIds.Buffs.LivingDarkness3Debuff;
                case 4: return ModBlueprintIds.Buffs.LivingDarkness4Debuff;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static string TierAreaGuid(int tier)
        {
            switch (tier)
            {
                case 1: return ModBlueprintIds.AreaEffects.LivingDarkness1Area;
                case 2: return ModBlueprintIds.AreaEffects.LivingDarkness2Area;
                case 3: return ModBlueprintIds.AreaEffects.LivingDarkness3Area;
                case 4: return ModBlueprintIds.AreaEffects.LivingDarkness4Area;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static string TierNameKey(int tier)
        {
            switch (tier)
            {
                case 1: return LocalizationIds.Mod.LivingDarknessName1;
                case 2: return LocalizationIds.Mod.LivingDarknessName2;
                case 3: return LocalizationIds.Mod.LivingDarknessName3;
                case 4: return LocalizationIds.Mod.LivingDarknessName4;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static string TierDescriptionKey(int tier)
        {
            switch (tier)
            {
                case 1: return LocalizationIds.Mod.LivingDarknessDescription1;
                case 2: return LocalizationIds.Mod.LivingDarknessDescription2;
                case 3: return LocalizationIds.Mod.LivingDarknessDescription3;
                case 4: return LocalizationIds.Mod.LivingDarknessDescription4;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static Feet TierAreaSize(int tier)
        {
            switch (tier)
            {
                case 1: return 15.Feet();
                case 2: return 20.Feet();
                case 3: return 25.Feet();
                case 4: return 30.Feet();
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static ContextDurationValue CasterLevelMinutes()
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Minutes,
                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue
                {
                    ValueType = ContextValueType.Rank,
                    ValueRank = AbilityRankType.Default
                }
            };
        }

        private static ContextDurationValue AreaLinkedDuration()
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                DiceCountValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 0 },
                BonusValue = new ContextValue { ValueType = ContextValueType.Simple, Value = 1 }
            };
        }
    }
}
