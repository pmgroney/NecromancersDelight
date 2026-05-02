using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

namespace wotr_mod.Infrastructure
{
    internal sealed class BlueprintClassRegistration
    {
        private readonly BlueprintTool _blueprints;

        public BlueprintClassRegistration(BlueprintTool blueprints)
        {
            _blueprints = blueprints;
        }

        public void AddCharacterClassToRoot(BlueprintCharacterClass characterClass)
        {
            var root = _blueprints.Require<Kingmaker.Blueprints.Root.BlueprintRoot>(
                GameBlueprintIds.Root.BlueprintRoot,
                "BlueprintRoot");
            var progressionRoot = GetFieldOrPropertyValue(root, "Progression");
            if (progressionRoot == null)
            {
                throw new InvalidOperationException("BlueprintRoot.Progression was not available.");
            }

            var field = progressionRoot.GetType().GetField("m_CharacterClasses", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Progression root character class field was not available.");
            }

            var reference = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass);
            AddCharacterClassReference(field, progressionRoot, characterClass, reference);

            AddCharacterClassToOptionalField(progressionRoot, "m_ClassProgression", characterClass, reference, reportMissing: false);
            AddCharacterClassToOptionalField(progressionRoot, "m_CharGenClasses", characterClass, reference, reportMissing: true);
            AddCharacterClassToOptionalField(progressionRoot, "m_CollectableExpansionClasses", characterClass, reference, reportMissing: true);
            AddCharacterClassToOptionalField(progressionRoot, "m_VisibleClasses", characterClass, reference, reportMissing: false);
            AddCharacterClassToOptionalField(root, "m_CharacterClasses", characterClass, reference, reportMissing: false);

            _blueprints.SetCharacterClassHidden(characterClass, false);
        }

        public void ReportCharacterClassRegistrationErrors(BlueprintCharacterClass characterClass, string contextName)
        {
            try
            {
                var label = string.IsNullOrEmpty(contextName) ? "character class" : contextName;
                if (characterClass == null)
                {
                    _blueprints.Error($"{label}: class blueprint is null after install.");
                    return;
                }

                if (characterClass.Spellbook == null)
                {
                    _blueprints.Error($"{label}: class {characterClass.name} ({characterClass.AssetGuid}) has no spellbook reference.");
                }

                if (characterClass.Progression == null)
                {
                    _blueprints.Error($"{label}: class {characterClass.name} ({characterClass.AssetGuid}) has no progression reference.");
                }

                ReportHiddenClassState(characterClass, label);
                ReportRootRegistrationErrors(characterClass, label);
            }
            catch (Exception ex)
            {
                _blueprints.Error($"{contextName}: class registration validation failed: {ex}");
            }
        }

        private void AddCharacterClassToOptionalField(
            object owner,
            string fieldName,
            BlueprintCharacterClass characterClass,
            BlueprintCharacterClassReference reference,
            bool reportMissing)
        {
            var field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                AddCharacterClassReference(field, owner, characterClass, reference);
                return;
            }

            if (reportMissing)
            {
                _blueprints.Error($"{fieldName} field not found in {owner.GetType().Name}");
            }
        }

        private static object GetFieldOrPropertyValue(object instance, string name)
        {
            var type = instance.GetType();
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(instance, null);
            }

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(instance);
        }

        private static void AddCharacterClassReference(
            FieldInfo field,
            object owner,
            BlueprintCharacterClass characterClass,
            BlueprintCharacterClassReference reference)
        {
            var classes = (BlueprintCharacterClassReference[])field.GetValue(owner) ??
                          Array.Empty<BlueprintCharacterClassReference>();
            if (classes.Any(existingReference => existingReference?.Get() == characterClass))
            {
                return;
            }

            field.SetValue(owner, classes.Concat(new[] { reference }).ToArray());
        }

        private void ReportHiddenClassState(BlueprintCharacterClass characterClass, string label)
        {
            if (BlueprintFields.CharacterClassHiddenFields.Length == 0)
            {
                _blueprints.Error($"{label}: cannot verify class visibility because no hidden fields were found on BlueprintCharacterClass.");
                return;
            }

            foreach (var field in BlueprintFields.CharacterClassHiddenFields)
            {
                var value = field.GetValue(characterClass);
                if (value is bool hidden && hidden)
                {
                    _blueprints.Error($"{label}: class {characterClass.name} ({characterClass.AssetGuid}) still has {field.Name}=true.");
                }
            }
        }

        private void ReportRootRegistrationErrors(BlueprintCharacterClass characterClass, string label)
        {
            var root = _blueprints.Get<Kingmaker.Blueprints.Root.BlueprintRoot>(GameBlueprintIds.Root.BlueprintRoot);
            if (root == null)
            {
                _blueprints.Error($"{label}: BlueprintRoot was not available while validating class registration.");
                return;
            }

            var progressionRoot = GetFieldOrPropertyValue(root, "Progression");
            if (progressionRoot == null)
            {
                _blueprints.Error($"{label}: BlueprintRoot.Progression was not available while validating class registration.");
                return;
            }

            var matchedFields = new List<string>();
            ReportCharacterClassReferenceField(
                progressionRoot,
                "ProgressionRoot.m_CharacterClasses",
                "m_CharacterClasses",
                characterClass,
                label,
                matchedFields);
            ReportCharacterClassReferenceField(
                progressionRoot,
                "ProgressionRoot.m_ClassProgression",
                "m_ClassProgression",
                characterClass,
                label,
                matchedFields);
            ReportCharacterClassReferenceField(
                progressionRoot,
                "ProgressionRoot.m_VisibleClasses",
                "m_VisibleClasses",
                characterClass,
                label,
                matchedFields);
            ReportCharacterClassReferenceField(
                root,
                "BlueprintRoot.m_CharacterClasses",
                "m_CharacterClasses",
                characterClass,
                label,
                matchedFields);

            if (matchedFields.Count == 0)
            {
                var availableFields = progressionRoot.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => field.FieldType == typeof(BlueprintCharacterClassReference[]))
                    .Select(field => field.Name)
                    .ToArray();

                _blueprints.Error(
                    $"{label}: class {characterClass.name} ({characterClass.AssetGuid}) was not found in any checked root class arrays. " +
                    $"Available ProgressionRoot class-array fields: {string.Join(", ", availableFields)}");
            }
        }

        private void ReportCharacterClassReferenceField(
            object owner,
            string displayName,
            string fieldName,
            BlueprintCharacterClass characterClass,
            string label,
            ICollection<string> matchedFields)
        {
            var field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            var references = field.GetValue(owner) as BlueprintCharacterClassReference[];
            if (references == null)
            {
                _blueprints.Error($"{label}: {displayName} was present but was not a BlueprintCharacterClassReference array.");
                return;
            }

            if (references.Any(reference => reference?.Get() == characterClass))
            {
                matchedFields.Add(displayName);
                return;
            }

            _blueprints.Error($"{label}: class {characterClass.name} ({characterClass.AssetGuid}) is missing from {displayName}.");
        }
    }
}
