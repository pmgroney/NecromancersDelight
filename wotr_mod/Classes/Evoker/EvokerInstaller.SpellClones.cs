using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using wotr_mod.Infrastructure;

namespace wotr_mod.Classes.Evoker
{
    internal sealed partial class EvokerInstaller
    {
        private const string EvokerSpellClonePrefix = "WotrMod_EvokerSpell_";
        private const string EvokerDamageCapDescriptionNote =
            "Evoker: when cast from the Evoker spellbook, this spell's damage scaling ignores its normal maximum cap.";

        private BlueprintAbility EnsureEvokerSpellClone(ClassSpellDefinition definition)
        {
            var cloneGuid = EvokerSpellCloneGuid(definition);
            var clone = _blueprints.Get<BlueprintAbility>(cloneGuid);
            var source = _blueprints.Require<BlueprintAbility>(
                definition.SpellGuid,
                definition.DisplayName + " donor spell");

            if (clone == null)
            {
                clone = _blueprints.CloneBlueprint(
                    source,
                    cloneGuid,
                    EvokerSpellClonePrefix + definition.DisplayName);
                _blueprints.AddCachedBlueprint(cloneGuid, clone);
            }

            ConfigureEvokerSpellClone(clone, source, definition);
            return clone;
        }

        private void ConfigureEvokerSpellClone(
            BlueprintAbility clone,
            BlueprintAbility source,
            ClassSpellDefinition definition)
        {
            // Keep Evoker-only spell behavior and description changes isolated to cloned abilities.
            if (!clone.IsSpell || clone.School != SpellSchool.Evocation)
            {
                return;
            }

            var areaEffects = EnsureEvokerAreaEffectClones(clone, definition);
            if (!HasDamageAction(clone) && !areaEffects.Any(HasDamageAction))
            {
                return;
            }

            ClearDamageScalingCaps(clone);
            RestoreRankDrivenProjectileDelivery(clone, source);
            foreach (var areaEffect in areaEffects)
            {
                ClearDamageScalingCaps(areaEffect);
            }

            ApplyEvokerDamageCapDescription(clone, source, definition);
        }

        private IReadOnlyList<BlueprintAbilityAreaEffect> EnsureEvokerAreaEffectClones(
            BlueprintAbility clone,
            ClassSpellDefinition definition)
        {
            var areaEffects = new List<BlueprintAbilityAreaEffect>();
            var replacements = new Dictionary<string, BlueprintAbilityAreaEffect>();

            foreach (var spawn in GetActions(clone).SelectMany(FindSpawnAreaEffects))
            {
                var sourceArea = spawn.AreaEffect;
                if (sourceArea == null)
                {
                    continue;
                }

                if (IsEvokerAreaEffectClone(sourceArea, definition))
                {
                    if (!areaEffects.Contains(sourceArea))
                    {
                        areaEffects.Add(sourceArea);
                    }

                    continue;
                }

                var sourceGuid = sourceArea.AssetGuid.ToString();
                BlueprintAbilityAreaEffect evokerArea;
                if (!replacements.TryGetValue(sourceGuid, out evokerArea))
                {
                    var areaGuid = EvokerAreaEffectCloneGuid(definition, sourceArea);
                    evokerArea = _blueprints.Get<BlueprintAbilityAreaEffect>(areaGuid);
                    if (evokerArea == null)
                    {
                        evokerArea = _blueprints.CloneBlueprint(
                            sourceArea,
                            areaGuid,
                            EvokerSpellClonePrefix + definition.DisplayName + "_Area_" + sourceArea.name);
                        _blueprints.AddCachedBlueprint(areaGuid, evokerArea);
                    }

                    replacements[sourceGuid] = evokerArea;
                    areaEffects.Add(evokerArea);
                }

                _blueprints.SetSpawnAreaEffect(spawn, evokerArea);
            }

            return areaEffects;
        }

        private static bool IsEvokerAreaEffectClone(
            BlueprintAbilityAreaEffect areaEffect,
            ClassSpellDefinition definition)
        {
            return areaEffect.name != null &&
                   areaEffect.name.StartsWith(
                       EvokerSpellClonePrefix + definition.DisplayName + "_Area_",
                       StringComparison.Ordinal);
        }

        internal void ClearDamageScalingCaps(BlueprintScriptableObject blueprint)
        {
            foreach (var rank in _blueprints.GetComponents<ContextRankConfig>(blueprint))
            {
                _blueprints.ClearContextRankMaximum(rank);
            }
        }

        private void RestoreRankDrivenProjectileDelivery(BlueprintAbility clone, BlueprintAbility source)
        {
            var sourceDeliveries = _blueprints.GetComponents<AbilityDeliverProjectile>(source).ToArray();
            var cloneDeliveries = _blueprints.GetComponents<AbilityDeliverProjectile>(clone).ToArray();
            var count = Math.Min(sourceDeliveries.Length, cloneDeliveries.Length);

            for (var i = 0; i < count; i++)
            {
                if (!sourceDeliveries[i].UseMaxProjectilesCount)
                {
                    continue;
                }

                cloneDeliveries[i].UseMaxProjectilesCount = true;
                cloneDeliveries[i].MaxProjectilesCountRank = sourceDeliveries[i].MaxProjectilesCountRank;
            }
        }

        private void ApplyEvokerDamageCapDescription(
            BlueprintAbility clone,
            BlueprintAbility source,
            ClassSpellDefinition definition)
        {
            var sourceDescription = RemoveDamageCapText(source.Description ?? string.Empty);
            var description = string.IsNullOrEmpty(sourceDescription)
                ? EvokerDamageCapDescriptionNote
                : sourceDescription + "\n\n" + EvokerDamageCapDescriptionNote;
            var descriptionKey = "wotr_mod.evoker_spell." + EvokerSpellCloneGuid(definition) + ".description";
            var name = BlueprintFields.AbilityDisplayName.GetValue(source) as LocalizedString
                       ?? BlueprintFields.AbilityDisplayName.GetValue(clone) as LocalizedString
                       ?? new LocalizedString();

            _localization.Put(descriptionKey, description);
            _blueprints.SetAbilityDisplay(clone, name, _localization.Text(descriptionKey));
        }

        private static string RemoveDamageCapText(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return description;
            }

            var text = Regex.Replace(
                description,
                @",?\s*to a maximum of [^.;]+(?=[.;])",
                string.Empty,
                RegexOptions.IgnoreCase);
            text = Regex.Replace(
                text,
                @",?\s*maximum \d+d\d+",
                string.Empty,
                RegexOptions.IgnoreCase);
            text = Regex.Replace(
                text,
                @"per caster level,\s+to ",
                "per caster level to ",
                RegexOptions.IgnoreCase);
            return Regex.Replace(text, @"\s+([.,;])", "$1").Trim();
        }

        private bool HasDamageAction(BlueprintAbility ability)
        {
            return GetActions(ability).Any(ContainsDamageAction);
        }

        private bool HasDamageAction(BlueprintAbilityAreaEffect areaEffect)
        {
            return _blueprints.GetComponents<BlueprintComponent>(areaEffect)
                .SelectMany(FindActionLists)
                .SelectMany(actionList => actionList?.Actions ?? Array.Empty<GameAction>())
                .Any(ContainsDamageAction);
        }

        private static bool ContainsDamageAction(GameAction action)
        {
            return ContainsDamageAction(action, new HashSet<GameAction>());
        }

        private static bool ContainsDamageAction(GameAction action, ISet<GameAction> visited)
        {
            if (action == null)
            {
                return false;
            }

            if (!visited.Add(action))
            {
                return false;
            }

            if (action is ContextActionDealDamage)
            {
                return true;
            }

            foreach (var field in BlueprintReferencePatcher.GetInstanceFields(action.GetType()))
            {
                var value = field.GetValue(action);
                var actionList = value as ActionList;
                if (actionList?.Actions?.Any(nested => ContainsDamageAction(nested, visited)) == true)
                {
                    return true;
                }

                var nestedAction = value as GameAction;
                if (ContainsDamageAction(nestedAction, visited))
                {
                    return true;
                }

                var actions = value as GameAction[];
                if (actions?.Any(nested => ContainsDamageAction(nested, visited)) == true)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<ContextActionSpawnAreaEffect> FindSpawnAreaEffects(GameAction action)
        {
            return FindSpawnAreaEffects(action, new HashSet<GameAction>());
        }

        private static IEnumerable<ContextActionSpawnAreaEffect> FindSpawnAreaEffects(
            GameAction action,
            ISet<GameAction> visited)
        {
            if (action == null || !visited.Add(action))
            {
                yield break;
            }

            var spawn = action as ContextActionSpawnAreaEffect;
            if (spawn != null)
            {
                yield return spawn;
            }

            foreach (var field in BlueprintReferencePatcher.GetInstanceFields(action.GetType()))
            {
                var value = field.GetValue(action);
                var actionList = value as ActionList;
                if (actionList?.Actions != null)
                {
                    foreach (var nested in actionList.Actions.SelectMany(nested => FindSpawnAreaEffects(nested, visited)))
                    {
                        yield return nested;
                    }
                }

                var nestedAction = value as GameAction;
                if (nestedAction != null)
                {
                    foreach (var nested in FindSpawnAreaEffects(nestedAction, visited))
                    {
                        yield return nested;
                    }
                }

                var actions = value as GameAction[];
                if (actions != null)
                {
                    foreach (var nested in actions.SelectMany(nested => FindSpawnAreaEffects(nested, visited)))
                    {
                        yield return nested;
                    }
                }
            }
        }

        private static IEnumerable<ActionList> FindActionLists(BlueprintComponent component)
        {
            foreach (var field in BlueprintReferencePatcher.GetInstanceFields(component.GetType()))
            {
                var actionList = field.GetValue(component) as ActionList;
                if (actionList != null)
                {
                    yield return actionList;
                }
            }
        }

        private static string EvokerSpellCloneGuid(ClassSpellDefinition definition)
        {
            return BlueprintReferencePatcher.DeterministicGuid(
                "Evoker.SpellClone." +
                definition.SpellGuid + "." +
                definition.SpellLevel + "." +
                definition.DisplayName);
        }

        private static string EvokerAreaEffectCloneGuid(
            ClassSpellDefinition definition,
            BlueprintAbilityAreaEffect sourceArea)
        {
            return BlueprintReferencePatcher.DeterministicGuid(
                "Evoker.SpellClone.AreaEffect." +
                definition.SpellGuid + "." +
                definition.SpellLevel + "." +
                definition.DisplayName + "." +
                sourceArea.AssetGuid);
        }
    }
}
