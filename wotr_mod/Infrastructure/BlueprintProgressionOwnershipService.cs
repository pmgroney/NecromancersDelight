using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace wotr_mod.Infrastructure
{
    internal sealed class BlueprintProgressionOwnershipService
    {
        private readonly BlueprintTool _blueprints;

        public BlueprintProgressionOwnershipService(BlueprintTool blueprints)
        {
            _blueprints = blueprints;
        }

        public void SetProgressionClasses(BlueprintFeatureBase feature, params BlueprintCharacterClass[] classes)
        {
            SetProgressionClassesInternal(feature, classes, new HashSet<BlueprintGuid>());
        }

        public void EnsureCustomClassOwnsProgressionFeatures(
            BlueprintProgression progression,
            string ownershipSeed,
            BlueprintCharacterClass characterClass)
        {
            if (progression == null || characterClass == null)
            {
                return;
            }

            var visiting = new HashSet<BlueprintGuid>();
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                var features = (entry.Features ?? Enumerable.Empty<BlueprintFeatureBase>())
                    .Select(feature =>
                    {
                        var owned = EnsureCustomClassOwnedFeature(feature, ownershipSeed, characterClass, visiting);
                        if (owned != feature)
                        {
                            ReplaceProgressionUiFeature(progression, feature, owned);
                        }

                        return owned;
                    })
                    .Where(feature => feature != null)
                    .ToArray();
                entry.SetFeatures(features);
            }

            SetProgressionClassesShallow(progression, characterClass);
        }

        public void SetProgressionClassesShallow(
            BlueprintFeatureBase feature,
            params BlueprintCharacterClass[] classes)
        {
            if (feature == null)
            {
                return;
            }

            if (feature is BlueprintProgression progression)
            {
                var levelEntries = progression.LevelEntries;
                try
                {
                    progression.LevelEntries = Array.Empty<LevelEntry>();
                    SetProgressionClasses(progression, classes);
                }
                finally
                {
                    progression.LevelEntries = levelEntries;
                }

                return;
            }

            if (feature is BlueprintFeatureSelection selection)
            {
                var features = BlueprintFields.FeatureSelectionFeatures?.GetValue(selection);
                var allFeatures = BlueprintFields.FeatureSelectionAllFeatures?.GetValue(selection);
                try
                {
                    _blueprints.SetFeatureSelectionFeatures(selection, Array.Empty<BlueprintFeature>());
                    _blueprints.SetFeatureSelectionAllFeatures(selection, Array.Empty<BlueprintFeature>());
                    SetProgressionClasses(selection, classes);
                }
                finally
                {
                    BlueprintFields.FeatureSelectionFeatures?.SetValue(selection, features);
                    BlueprintFields.FeatureSelectionAllFeatures?.SetValue(selection, allFeatures);
                }

                return;
            }

            SetProgressionClasses(feature, classes);
        }

        private BlueprintFeatureBase EnsureCustomClassOwnedFeature(
            BlueprintFeatureBase source,
            string ownershipSeed,
            BlueprintCharacterClass characterClass,
            HashSet<BlueprintGuid> visiting)
        {
            if (source == null || IsModOwned(source))
            {
                return source;
            }

            var featureGuid = DeterministicGuid(ownershipSeed + ".OwnedFeature." + source.AssetGuid);
            var feature = _blueprints.Get<BlueprintFeatureBase>(featureGuid);
            if (feature == null)
            {
                feature = CloneFeatureBase(source, featureGuid, ownershipSeed + "_" + source.name);
                _blueprints.AddCachedBlueprint(featureGuid, feature);
            }

            if (!visiting.Add(source.AssetGuid))
            {
                return feature;
            }

            try
            {
                ConfigureCustomClassOwnedFeature(feature, ownershipSeed, characterClass, visiting);
            }
            finally
            {
                visiting.Remove(source.AssetGuid);
            }

            SetProgressionClassesShallow(feature, characterClass);
            return feature;
        }

        private BlueprintFeatureBase CloneFeatureBase(
            BlueprintFeatureBase source,
            string featureGuid,
            string internalName)
        {
            if (source is BlueprintFeatureSelection selection)
            {
                return _blueprints.CloneBlueprint(selection, featureGuid, internalName);
            }

            if (source is BlueprintProgression progression)
            {
                return _blueprints.CloneBlueprint(progression, featureGuid, internalName);
            }

            if (source is BlueprintFeature feature)
            {
                return _blueprints.CloneBlueprint(feature, featureGuid, internalName);
            }

            throw new InvalidOperationException(source.name + " is not a cloneable custom class donor feature.");
        }

        private void ConfigureCustomClassOwnedFeature(
            BlueprintFeatureBase feature,
            string ownershipSeed,
            BlueprintCharacterClass characterClass,
            HashSet<BlueprintGuid> visiting)
        {
            if (feature is BlueprintFeatureSelection selection)
            {
                SetProgressionClassesShallow(selection, characterClass);
                return;
            }

            var components = _blueprints.GetComponents<BlueprintComponent>(feature).ToList();
            var knownSpell = components.OfType<AddKnownSpell>().FirstOrDefault();
            if (knownSpell != null)
            {
                var spell = GetKnownSpell(knownSpell);
                var addKnownSpell = new AddKnownSpell { name = "$AddKnownSpell$" + feature.name };
                _blueprints.SetAddKnownSpell(addKnownSpell, characterClass, spell, knownSpell.SpellLevel);
                _blueprints.SetComponents(feature, addKnownSpell);
                return;
            }

            var filtered = components
                .Where(component => component.GetType().Name != "PrerequisiteNoArchetype")
                .ToArray();
            foreach (var component in filtered)
            {
                BindComponentToCustomClass(component, ownershipSeed, characterClass, visiting);
            }

            _blueprints.SetComponents(feature, filtered);
        }

        private void BindComponentToCustomClass(
            BlueprintComponent component,
            string ownershipSeed,
            BlueprintCharacterClass characterClass,
            HashSet<BlueprintGuid> visiting)
        {
            var classReference = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass);
            SetReferenceField(component, "m_Class", classReference);
            SetReferenceField(component, "m_CharacterClass", classReference);
            SetField(component, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>());
            SetField(component, "m_Classes", Array.Empty<BlueprintCharacterClassReference>());
            SetField(component, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            SetField(component, "m_AdditionalArchetypes", Array.Empty<BlueprintArchetypeReference>());
            SetField(component, "m_Archetype", null);
            SetField(component, "m_ExcludeArchetype", null);
            RetargetFeatureReferenceFields(component, ownershipSeed, characterClass, visiting);
        }

        private void RetargetFeatureReferenceFields(
            BlueprintComponent component,
            string ownershipSeed,
            BlueprintCharacterClass characterClass,
            HashSet<BlueprintGuid> visiting)
        {
            foreach (var field in GetInstanceFields(component.GetType()))
            {
                if (field.FieldType == typeof(BlueprintFeatureReference))
                {
                    var reference = field.GetValue(component) as BlueprintFeatureReference;
                    var owned = EnsureCustomClassOwnedFeature(reference?.Get(), ownershipSeed, characterClass, visiting)
                        as BlueprintFeature;
                    if (owned != null && owned.AssetGuid != reference?.Get()?.AssetGuid)
                    {
                        field.SetValue(component, BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(owned));
                    }
                }
                else if (field.FieldType == typeof(BlueprintUnitFactReference))
                {
                    var reference = field.GetValue(component) as BlueprintUnitFactReference;
                    var owned = EnsureCustomClassOwnedFeature(reference?.Get() as BlueprintFeatureBase, ownershipSeed, characterClass, visiting)
                        as BlueprintUnitFact;
                    if (owned != null && owned.AssetGuid != reference?.Get()?.AssetGuid)
                    {
                        field.SetValue(component, BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(owned));
                    }
                }
                else if (field.FieldType == typeof(BlueprintFeatureBaseReference))
                {
                    var reference = field.GetValue(component) as BlueprintFeatureBaseReference;
                    var owned = EnsureCustomClassOwnedFeature(reference?.Get(), ownershipSeed, characterClass, visiting);
                    if (owned != null && owned.AssetGuid != reference?.Get()?.AssetGuid)
                    {
                        field.SetValue(component, BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>(owned));
                    }
                }
            }
        }

        private static BlueprintAbility GetKnownSpell(AddKnownSpell component)
        {
            var reference = BlueprintFields.AddKnownSpellSpell?.GetValue(component) as BlueprintAbilityReference;
            return reference?.Get();
        }

        private static bool IsModOwned(BlueprintFeatureBase feature)
        {
            return feature?.name != null && feature.name.StartsWith("WotrMod_", StringComparison.Ordinal);
        }

        private static void SetReferenceField<TReference>(
            object instance,
            string fieldName,
            TReference reference)
            where TReference : BlueprintReferenceBase
        {
            var field = FindField(instance.GetType(), fieldName);
            if (field == null || field.FieldType != typeof(TReference))
            {
                return;
            }

            field.SetValue(instance, reference);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = FindField(instance.GetType(), fieldName);
            if (field == null)
            {
                return;
            }

            if (value != null && !field.FieldType.IsInstanceOfType(value))
            {
                return;
            }

            field.SetValue(instance, value);
        }

        private static void ReplaceProgressionUiFeature(
            BlueprintProgression progression,
            BlueprintFeatureBase source,
            BlueprintFeatureBase replacement)
        {
            if (progression == null || source == null || replacement == null)
            {
                return;
            }

            var uiGroups = (progression.UIGroups ?? Array.Empty<UIGroup>()).ToList();
            for (var i = 0; i < uiGroups.Count; i++)
            {
                var group = uiGroups[i];
                var references = BlueprintFields.UIGroupFeatures?.GetValue(group) as IEnumerable<BlueprintFeatureBaseReference>;
                if (references == null || references.All(reference => reference?.Get()?.AssetGuid != source.AssetGuid))
                {
                    continue;
                }

                BlueprintFields.UIGroupFeatures.SetValue(
                    group,
                    references
                        .Select(reference => reference?.Get()?.AssetGuid == source.AssetGuid
                            ? BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>(replacement)
                            : reference)
                        .Where(reference => reference != null)
                        .ToList());
            }
        }

        private void SetProgressionClassesInternal(BlueprintFeatureBase feature, BlueprintCharacterClass[] classes, HashSet<BlueprintGuid> visited)
        {
            if (feature == null || visited.Contains(feature.AssetGuid)) return;
            visited.Add(feature.AssetGuid);

            _blueprints.Log($"Setting progression classes for {feature.name} ({feature.AssetGuid})");

            FieldInfo field = null;
            for (var type = feature.GetType(); type != null; type = type.BaseType)
            {
                field = type.GetField("m_Classes", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) break;
            }

            if (field != null && !(feature is BlueprintProgression))
            {
                var references = classes
                    .Where(c => c != null)
                    .Select(BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>)
                    .ToArray();
                field.SetValue(feature, references);
            }
            else if (field == null)
            {
                for (var type = feature.GetType(); type != null; type = type.BaseType)
                {
                    field = type.GetField("m_CharacterClass", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null) break;
                }

                if (field != null)
                {
                    var reference = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(classes.FirstOrDefault());
                    field.SetValue(feature, reference);
                }
            }

            if (feature is BlueprintProgression progression)
            {
                var classWithLevelType = typeof(BlueprintProgression).GetNestedType("ClassWithLevel", BindingFlags.Public | BindingFlags.NonPublic);
                if (classWithLevelType != null)
                {
                    var mClassField = classWithLevelType.GetField("m_Class", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var additionalLevelField = classWithLevelType.GetField("AdditionalLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (mClassField != null)
                    {
                        var values = Array.CreateInstance(classWithLevelType, classes.Length);
                        for (var i = 0; i < classes.Length; i++)
                        {
                            var characterClass = classes[i];
                            if (characterClass == null) continue;

                            var classWithLevel = Activator.CreateInstance(classWithLevelType);
                            mClassField.SetValue(classWithLevel, BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
                            additionalLevelField?.SetValue(classWithLevel, 0);
                            values.SetValue(classWithLevel, i);
                        }

                        var progClassesField = BlueprintFields.ProgressionClasses ?? typeof(BlueprintProgression).GetField("m_Classes", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (progClassesField != null)
                        {
                            progClassesField.SetValue(progression, values);
                        }
                    }
                }

                if (progression.LevelEntries != null)
                {
                    foreach (var entry in progression.LevelEntries)
                    {
                        if (entry?.Features == null) continue;
                        foreach (var f in entry.Features)
                        {
                            if (f != null)
                            {
                                SetProgressionClassesInternal(f, classes, visited);
                            }
                        }
                    }
                }
            }

            if (feature is BlueprintFeatureSelection selection)
            {
                var features = (BlueprintFeatureReference[])BlueprintFields.FeatureSelectionFeatures?.GetValue(selection);
                if (features != null)
                {
                    foreach (var fRef in features)
                    {
                        var f = fRef?.Get();
                        if (f != null)
                        {
                            SetProgressionClassesInternal(f, classes, visited);
                        }
                    }
                }

                var allFeatures = (BlueprintFeatureReference[])BlueprintFields.FeatureSelectionAllFeatures?.GetValue(selection);
                if (allFeatures != null)
                {
                    foreach (var fRef in allFeatures)
                    {
                        var f = fRef?.Get();
                        if (f != null)
                        {
                            SetProgressionClassesInternal(f, classes, visited);
                        }
                    }
                }
            }
        }

        private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
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

        private static FieldInfo FindField(Type type, string fieldName)
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

        private static string DeterministicGuid(string seed)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("wotr_mod:" + seed));
                return new Guid(bytes).ToString("N");
            }
        }
    }
}
