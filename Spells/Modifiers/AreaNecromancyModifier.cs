using System;
using System.Linq;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

namespace wotr_mod.Spells.Modifiers
{
    internal sealed class AreaNecromancyModifier : ISpellModifier
    {
        private readonly string _areaGuid;
        private readonly int _radiusFeet;
        private readonly DiceType _diceType;
        private readonly ContextValue _diceCount;
        private readonly ContextDurationValue _duration;
        private readonly Action<BlueprintAbilityAreaEffect, SpellModifierContext> _configureArea;

        public AreaNecromancyModifier(
            string areaGuid,
            int radiusFeet,
            DiceType diceType,
            ContextValue diceCount,
            ContextDurationValue duration,
            Action<BlueprintAbilityAreaEffect, SpellModifierContext> configureArea = null)
        {
            _areaGuid = areaGuid;
            _radiusFeet = radiusFeet;
            _diceType = diceType;
            _diceCount = diceCount;
            _duration = duration;
            _configureArea = configureArea;
        }

        public void Apply(SpellModifierContext context)
        {
            var spell = context.Ability;
            SpellModifierUtility.SetSchool(spell, SpellSchool.Necromancy, context.Blueprints);
            spell.Range = AbilityRange.Long;

            var spawn = FindSpawnAreaEffect(spell);
            if (spawn?.AreaEffect == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: no spawn area effect found.");
                return;
            }

            var area = context.Blueprints.Get<BlueprintAbilityAreaEffect>(_areaGuid);
            if (area == null)
            {
                area = context.Blueprints.CloneBlueprint(spawn.AreaEffect, _areaGuid, context.Definition.InternalName + "_AreaEffect");
                context.Blueprints.AddCachedBlueprint(_areaGuid, area);
            }

            context.Blueprints.SetSpawnAreaEffect(spawn, area);
            spawn.OnUnit = false;
            spawn.DurationValue = _duration;

            area.Size = _radiusFeet.Feet();
            _configureArea?.Invoke(area, context);

            // Add ContextRankConfig to the area effect for damage scaling
            var rank = context.Blueprints.EnsureComponent(area, () => new ContextRankConfig());
            if (_diceCount.ValueType == ContextValueType.Rank)
            {
                context.Blueprints.ConfigureContextRankConfig(
                    rank,
                    type: _diceCount.ValueRank,
                    baseValueType: ContextRankBaseValueType.CasterLevel);
            }

            var aoeRadius = context.Blueprints.GetComponents<AbilityAoERadius>(spell).FirstOrDefault();
            if (aoeRadius != null)
            {
                SpellModifierUtility.SetPrivateField(aoeRadius, "m_Radius", _radiusFeet.Feet());
            }

            EnsureAreaDamage(area, context);
        }

        private void EnsureAreaDamage(BlueprintAbilityAreaEffect area, SpellModifierContext context)
        {
            var areaRun = context.Blueprints.GetComponents<AbilityAreaEffectRunAction>(area).FirstOrDefault();
            if (areaRun == null)
            {
                context.Logger.Warning($"{context.Definition.InternalName}: area effect has no run action.");
                return;
            }

            areaRun.Round = EnsureDamageAction(areaRun.Round);
        }

        private ActionList EnsureDamageAction(ActionList list)
        {
            if (list.Actions != null &&
                list.Actions.OfType<ContextActionDealDamage>().Any(action => action.name == "$NecromancersDelight_AreaDamage"))
            {
                return list;
            }

            var damage = new ContextActionDealDamage
            {
                name = "$NecromancersDelight_AreaDamage",
                DamageType = new DamageTypeDescription
                {
                    Type = DamageType.Energy,
                    Energy = DamageEnergyType.Unholy
                },
                Value = new ContextDiceValue
                {
                    DiceType = _diceType,
                    DiceCountValue = _diceCount,
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0
                    }
                },
                IsAoE = true,
                Half = false,
                HalfIfSaved = false
            };

            var actions = list.Actions ?? Array.Empty<GameAction>();
            list.Actions = actions.Concat(new GameAction[] { damage }).ToArray();
            return list;
        }

        private static ContextActionSpawnAreaEffect FindSpawnAreaEffect(BlueprintAbility spell)
        {
            ContextActionSpawnAreaEffect result = null;
            SpellModifierUtility.PatchRunActions(spell, action =>
            {
                if (result == null)
                {
                    result = action as ContextActionSpawnAreaEffect;
                }

                return 0;
            });

            return result;
        }

        public static ContextDurationValue Rounds(int rounds)
        {
            return new ContextDurationValue
            {
                Rate = DurationRate.Rounds,
                DiceType = DiceType.Zero,
                DiceCountValue = new ContextValue
                {
                    ValueType = ContextValueType.Simple,
                    Value = 0
                },
                BonusValue = new ContextValue
                {
                    ValueType = ContextValueType.Simple,
                    Value = rounds
                }
            };
        }

        public static void ConfigureCasterLevelRank(BlueprintAbilityAreaEffect area, SpellModifierContext context, ContextRankProgression progression, int startLevel, int stepLevel)
        {
            var rank = context.Blueprints.EnsureComponent(area, () => new ContextRankConfig());
            context.Blueprints.ConfigureContextRankConfig(
                rank,
                progression: progression,
                startLevel: startLevel,
                stepLevel: stepLevel);
        }
    }
}
