using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace wotr_mod.Infrastructure
{
    internal static class BlueprintReferencePatcher
    {
        public static void ReplaceBuffReferences(
            BlueprintScriptableObject blueprint,
            string oldBuffGuid,
            BlueprintBuff newBuff)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldBuffGuid));
            foreach (var component in blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                ReplaceBuffReferencesInObject(component, oldGuid, newBuff);
            }

            ReplaceBuffReferencesInObject(blueprint, oldGuid, newBuff);
        }

        public static void ConfigureAddFeatureOnClassLevel(
            BlueprintComponent component,
            BlueprintCharacterClass characterClass,
            BlueprintFeature level1Feature,
            BlueprintFeature level2Feature)
        {
            var featureField = FindField(component.GetType(), "m_Feature");
            var beforeThisLevelField = FindField(component.GetType(), "BeforeThisLevel");
            var feature = beforeThisLevelField?.GetValue(component) is bool beforeThisLevel && beforeThisLevel
                ? level1Feature
                : level2Feature;

            featureField?.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));

            FindField(component.GetType(), "m_Class")?.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
            FindField(component.GetType(), "m_AdditionalClasses")?.SetValue(
                component,
                Array.Empty<BlueprintCharacterClassReference>());
            FindField(component.GetType(), "m_Archetypes")?.SetValue(
                component,
                Array.Empty<BlueprintArchetypeReference>());
        }

        public static void ReplaceProgressionFeature(
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

        public static void ReplaceProgressionFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase oldFeature,
            BlueprintFeatureBase newFeature)
        {
            if (oldFeature == null || newFeature == null)
            {
                return;
            }

            var oldGuid = oldFeature.AssetGuid;
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                entry.SetFeatures((entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature => feature != null && feature.AssetGuid == oldGuid ? newFeature : feature));
            }
        }

        public static void ReplaceProgressionUiFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase oldFeature,
            BlueprintFeatureBase newFeature)
        {
            if (oldFeature == null || newFeature == null)
            {
                return;
            }

            var field = FindField(typeof(UIGroup), "m_Features");
            if (field == null)
            {
                return;
            }

            var oldGuid = oldFeature.AssetGuid;
            foreach (var group in progression.UIGroups ?? Array.Empty<UIGroup>())
            {
                var references = field.GetValue(group) as IEnumerable<BlueprintFeatureBaseReference>;
                if (references == null || !references.Any(reference => reference?.Get()?.AssetGuid == oldGuid))
                {
                    continue;
                }

                field.SetValue(
                    group,
                    references
                        .Select(reference => reference?.Get()?.AssetGuid == oldGuid
                            ? BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>(newFeature)
                            : reference)
                        .ToList());
            }
        }

        public static void BindAbilityRankConfigsToClass(
            BlueprintTool blueprints,
            BlueprintAbility ability,
            BlueprintCharacterClass characterClass)
        {
            foreach (var oldConfig in blueprints.GetComponents<ContextRankConfig>(ability))
            {
                var newConfig = blueprints.CloneComponent(oldConfig);
                var reference = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass);
                if (BlueprintFields.ContextRankConfigClass.FieldType.IsArray)
                {
                    BlueprintFields.ContextRankConfigClass.SetValue(newConfig, new[] { reference });
                }
                else
                {
                    BlueprintFields.ContextRankConfigClass.SetValue(newConfig, reference);
                }

                BlueprintFields.ContextRankConfigBaseValueType?.SetValue(
                    newConfig,
                    ContextRankBaseValueType.ClassLevel);
                BlueprintFields.ContextRankConfigArchetype?.SetValue(newConfig, null);
                BlueprintFields.ContextRankConfigAdditionalArchetypes?.SetValue(
                    newConfig,
                    Array.Empty<BlueprintArchetypeReference>());
                blueprints.ReplaceComponent(ability, oldConfig, newConfig);
            }
        }

        public static void ReplaceFeatureReferences(
            BlueprintScriptableObject blueprint,
            BlueprintGuid oldFeatureGuid,
            BlueprintFeature newFeature)
        {
            foreach (var component in blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                foreach (var field in GetInstanceFields(component.GetType()))
                {
                    if (field.FieldType == typeof(BlueprintFeatureReference))
                    {
                        var reference = (BlueprintFeatureReference)field.GetValue(component);
                        if (reference != null && reference.Get()?.AssetGuid == oldFeatureGuid)
                        {
                            field.SetValue(
                                component,
                                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(newFeature));
                        }
                    }
                    else if (field.FieldType == typeof(BlueprintUnitFactReference))
                    {
                        var reference = (BlueprintUnitFactReference)field.GetValue(component);
                        if (reference != null && reference.Get()?.AssetGuid == oldFeatureGuid)
                        {
                            field.SetValue(
                                component,
                                BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(newFeature));
                        }
                    }
                }
            }
        }

        public static void ReplaceAbilityReferences(
            BlueprintScriptableObject blueprint,
            string oldAbilityGuid,
            BlueprintAbility newAbility)
        {
            var oldGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(oldAbilityGuid));
            foreach (var component in blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                foreach (var field in GetInstanceFields(component.GetType()))
                {
                    if (field.FieldType == typeof(BlueprintAbilityReference))
                    {
                        var reference = (BlueprintAbilityReference)field.GetValue(component);
                        if (ReferencesAbility(reference, oldGuid))
                        {
                            field.SetValue(
                                component,
                                BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(newAbility));
                        }
                    }
                    else if (field.FieldType == typeof(BlueprintAbilityReference[]))
                    {
                        var references = (BlueprintAbilityReference[])field.GetValue(component);
                        if (references == null || !references.Any(reference => ReferencesAbility(reference, oldGuid)))
                        {
                            continue;
                        }

                        field.SetValue(
                            component,
                            references
                                .Select(reference => ReferencesAbility(reference, oldGuid)
                                    ? BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(newAbility)
                                    : reference)
                                .ToArray());
                    }
                }
            }
        }

        public static string DeterministicGuid(string seed)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes("wotr_mod:" + seed));
                return new Guid(bytes).ToString("N");
            }
        }

        public static IEnumerable<FieldInfo> GetInstanceFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                foreach (var field in current.GetFields(flags))
                {
                    yield return field;
                }
            }
        }

        public static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static void ReplaceBuffReferencesInObject(object instance, BlueprintGuid oldGuid, BlueprintBuff newBuff)
        {
            foreach (var field in GetInstanceFields(instance.GetType()))
            {
                if (field.FieldType == typeof(BlueprintBuffReference))
                {
                    var reference = (BlueprintBuffReference)field.GetValue(instance);
                    if (reference != null && reference.Get()?.AssetGuid == oldGuid)
                    {
                        field.SetValue(
                            instance,
                            BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(newBuff));
                    }
                }
                else if (field.FieldType == typeof(BlueprintBuffReference[]))
                {
                    var references = (BlueprintBuffReference[])field.GetValue(instance);
                    if (references == null || !references.Any(reference => reference != null && reference.Get()?.AssetGuid == oldGuid))
                    {
                        continue;
                    }

                    field.SetValue(
                        instance,
                        references
                            .Select(reference => reference != null && reference.Get()?.AssetGuid == oldGuid
                                ? BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(newBuff)
                                : reference)
                            .ToArray());
                }
            }
        }

        private static bool ReferencesAbility(BlueprintAbilityReference reference, BlueprintGuid guid)
        {
            return reference != null && reference.Get()?.AssetGuid == guid;
        }
    }
}
