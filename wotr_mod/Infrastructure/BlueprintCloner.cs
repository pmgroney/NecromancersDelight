using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using UnityEngine;

namespace wotr_mod.Infrastructure
{
    internal sealed class BlueprintCloner
    {
        private readonly Action<string> _log;
        private readonly Action<string> _error;

        public BlueprintCloner(Action<string> log, Action<string> error)
        {
            _log = log;
            _error = error;
        }

        public T CloneBlueprint<T>(T source, string guid, string name) where T : BlueprintScriptableObject
        {
            try
            {
                _log?.Invoke($"Cloning blueprint: {source.name} (Type: {typeof(T).Name}) -> {name} ({guid})");
                var clone = CloneManaged(source);
                clone.name = name;
                clone.AssetGuid = BlueprintGuid.Parse(BlueprintTool.NormalizeGuid(guid));
                CloneBlueprintMutableState(source, clone);
                CloneComponents(source, clone);
                return clone;
            }
            catch (Exception ex)
            {
                _error?.Invoke($"Failed to clone blueprint {source.name} to {name} ({guid}): {ex}");
                throw;
            }
        }

        public T CloneComponent<T>(T source) where T : BlueprintComponent
        {
            return (T)CloneComponent(source, source?.OwnerBlueprint);
        }

        private void CloneBlueprintMutableState(BlueprintScriptableObject source, BlueprintScriptableObject clone)
        {
            foreach (var field in GetInstanceFields(source.GetType()))
            {
                if (field.IsInitOnly || field.IsLiteral || IsComponentsField(field))
                {
                    continue;
                }

                try
                {
                    var value = field.GetValue(source);
                    var clonedValue = CloneBlueprintMutableValue(value);
                    if (!ReferenceEquals(value, clonedValue))
                    {
                        field.SetValue(clone, clonedValue);
                    }
                }
                catch (Exception ex)
                {
                    _error?.Invoke($"Failed to clone mutable blueprint field {field.Name} on {source.name}: {ex}");
                }
            }
        }

        private object CloneBlueprintMutableValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is LevelEntry levelEntry)
            {
                return CloneLevelEntry(levelEntry);
            }

            if (value is SpellLevelList spellLevelList)
            {
                return CloneSpellLevelList(spellLevelList);
            }

            if (value is UIGroup uiGroup)
            {
                return CloneUiGroup(uiGroup);
            }

            var type = value.GetType();
            if (type.IsArray)
            {
                return CloneBlueprintMutableArray((Array)value);
            }

            if (value is IList list && HasDefaultConstructor(type))
            {
                var clone = (IList)Activator.CreateInstance(type);
                foreach (var item in list)
                {
                    clone.Add(CloneBlueprintMutableValue(item) ?? item);
                }

                return clone;
            }

            return value;
        }

        private Array CloneBlueprintMutableArray(Array source)
        {
            var elementType = source.GetType().GetElementType();
            var clone = Array.CreateInstance(elementType, source.Length);

            for (var i = 0; i < source.Length; i++)
            {
                var item = source.GetValue(i);
                clone.SetValue(CloneBlueprintMutableValue(item) ?? item, i);
            }

            return clone;
        }

        private static LevelEntry CloneLevelEntry(LevelEntry source)
        {
            var clone = new LevelEntry { Level = source.Level };
            clone.SetFeatures(source.Features ?? Enumerable.Empty<BlueprintFeatureBase>());
            return clone;
        }

        private static SpellLevelList CloneSpellLevelList(SpellLevelList source)
        {
            var clone = new SpellLevelList(source.SpellLevel);
            var spells = source.Spells?
                .Where(spell => spell != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>)
                .ToList() ?? new List<BlueprintAbilityReference>();
            BlueprintFields.SpellLevelListSpells.SetValue(clone, spells);
            return clone;
        }

        private static UIGroup CloneUiGroup(UIGroup source)
        {
            var clone = new UIGroup();
            if (BlueprintFields.UIGroupFeatures == null)
            {
                return clone;
            }

            var features = (IEnumerable<BlueprintFeatureBaseReference>)BlueprintFields.UIGroupFeatures.GetValue(source);
            BlueprintFields.UIGroupFeatures.SetValue(clone, features?.ToList() ?? new List<BlueprintFeatureBaseReference>());
            return clone;
        }

        private static bool IsComponentsField(FieldInfo field)
        {
            return BlueprintFields.BlueprintComponents != null &&
                   field.DeclaringType == BlueprintFields.BlueprintComponents.DeclaringType &&
                   field.Name == BlueprintFields.BlueprintComponents.Name;
        }

        private static T CloneManaged<T>(T source) where T : class
        {
            if (source == null)
            {
                return null;
            }

            return (T)MemberwiseClone(source);
        }

        private static object MemberwiseClone(object source)
        {
            var type = source.GetType();
            var method = type.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, "MemberwiseClone");
            }

            return method.Invoke(source, null);
        }

        private void CloneComponents(BlueprintScriptableObject source, BlueprintScriptableObject clone)
        {
            var components = (BlueprintComponent[])BlueprintFields.BlueprintComponents.GetValue(source);
            if (components == null)
            {
                BlueprintFields.BlueprintComponents.SetValue(clone, null);
                return;
            }

            var clonedComponents = components
                .Select(component => CloneComponent(component, clone))
                .ToArray();

            BlueprintFields.BlueprintComponents.SetValue(clone, clonedComponents);
        }

        private BlueprintComponent CloneComponent(BlueprintComponent component, BlueprintScriptableObject owner)
        {
            if (component == null)
            {
                return null;
            }

            try
            {
                _log?.Invoke($"  Cloning component: {component.GetType().Name}");
                var clone = (BlueprintComponent)MemberwiseClone(component);
                DeepCloneFieldsInPlace(clone, new Dictionary<object, object>(ReferenceEqualityComparer.Instance));
                clone.OwnerBlueprint = owner;
                return clone;
            }
            catch (Exception ex)
            {
                _error?.Invoke($"    Failed to clone component {component.GetType().Name}: {ex}");
                throw;
            }
        }

        private void DeepCloneFieldsInPlace(object instance, Dictionary<object, object> visited)
        {
            if (instance == null || visited.ContainsKey(instance))
            {
                return;
            }

            visited[instance] = instance;

            var type = instance.GetType();
            foreach (var field in GetInstanceFields(type))
            {
                if (field.IsNotSerialized)
                {
                    continue;
                }

                try
                {
                    var value = field.GetValue(instance);
                    if (value == null)
                    {
                        continue;
                    }

                    field.SetValue(instance, CloneFieldValue(value, field.FieldType, visited));
                }
                catch (Exception ex)
                {
                    _error?.Invoke($"      Failed to clone field {field.Name} of type {field.FieldType.Name} in {type.Name}: {ex}");
                }
            }
        }

        private object CloneFieldValue(object value, Type fieldType, Dictionary<object, object> visited)
        {
            if (value == null || fieldType.IsValueType || fieldType == typeof(string))
            {
                return value;
            }

            if (visited.TryGetValue(value, out var existing))
            {
                return existing;
            }

            if (value is SimpleBlueprint || value is BlueprintReferenceBase || value is UnityEngine.Object || value is LocalizedString)
            {
                return value;
            }

            if (fieldType.FullName != null &&
                (fieldType.FullName.StartsWith("UnityEngine.") || fieldType.FullName.StartsWith("TMPro.")))
            {
                return value;
            }

            if (value is ActionList actionList)
            {
                return CloneActionList(actionList, visited);
            }

            if (value is GameAction || value is Condition)
            {
                return CloneManagedNode(value, visited);
            }

            if (fieldType.IsArray)
            {
                return CloneArray((Array)value, visited);
            }

            if (value is IList list && HasDefaultConstructor(fieldType))
            {
                var clone = (IList)Activator.CreateInstance(fieldType);
                foreach (var item in list)
                {
                    clone.Add(CloneUnknownValue(item, visited));
                }

                return clone;
            }

            if (IsSafeManagedConfig(fieldType))
            {
                return CloneManagedNode(value, visited);
            }

            return value;
        }

        private object CloneUnknownValue(object value, Dictionary<object, object> visited)
        {
            if (value == null)
            {
                return null;
            }

            return CloneFieldValue(value, value.GetType(), visited);
        }

        private Array CloneArray(Array source, Dictionary<object, object> visited)
        {
            var elementType = source.GetType().GetElementType();
            var clone = Array.CreateInstance(elementType, source.Length);

            for (var i = 0; i < source.Length; i++)
            {
                clone.SetValue(CloneUnknownValue(source.GetValue(i), visited), i);
            }

            return clone;
        }

        private ActionList CloneActionList(ActionList source, Dictionary<object, object> visited)
        {
            var clone = new ActionList();
            visited[source] = clone;
            if (source.Actions != null)
            {
                clone.Actions = source.Actions
                    .Select(action => (GameAction)CloneManagedNode(action, visited))
                    .ToArray();
            }

            return clone;
        }

        private object CloneManagedNode(object source, Dictionary<object, object> visited)
        {
            if (source == null)
            {
                return null;
            }

            var clone = MemberwiseClone(source);
            visited[source] = clone;
            DeepCloneFieldsInPlace(clone, visited);
            return clone;
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

        private static bool HasDefaultConstructor(Type type)
        {
            return !type.IsInterface && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null;
        }

        private static bool IsSafeManagedConfig(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return false;
            }

            return type.Namespace != null &&
                   (type.Namespace.StartsWith("Kingmaker.UnitLogic.Mechanics", StringComparison.Ordinal) ||
                    type.Namespace.StartsWith("Kingmaker.ElementsSystem", StringComparison.Ordinal) ||
                    type.Namespace.StartsWith("Kingmaker.RuleSystem", StringComparison.Ordinal) ||
                    type.Namespace.StartsWith("Kingmaker.Designers.Mechanics", StringComparison.Ordinal));
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
