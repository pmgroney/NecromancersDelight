using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Recommendations;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using UnityModManagerNet;
using UnityEngine;

namespace wotr_mod.Infrastructure
{
    internal sealed class BlueprintTool
    {
        public static BlueprintTool Instance { get; private set; }
        private readonly UnityModManager.ModEntry.ModLogger _logger;
        private readonly string _fallbackLogPath;
        private readonly BlueprintCloner _cloner;
        private readonly BlueprintClassRegistration _classRegistration;

        public BlueprintTool(UnityModManager.ModEntry.ModLogger logger)
        {
            Instance = this;
            _logger = logger;
            _cloner = new BlueprintCloner(Log, Error);
            _classRegistration = new BlueprintClassRegistration(this);
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var modPath = System.IO.Path.GetDirectoryName(assembly.Location);
                _fallbackLogPath = System.IO.Path.Combine(modPath, "install.log");
            }
            catch
            {
                // Ignore
            }
        }

        public void Log(string message)
        {
            _logger.Log(message);
        }

        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
            if (!string.IsNullOrEmpty(_fallbackLogPath))
            {
                try
                {
                    System.IO.File.AppendAllText(_fallbackLogPath, $"[ERROR] {message}{Environment.NewLine}");
                }
                catch
                {
                    // Ignore
                }
            }
        }

        public void ReportError(string message)
        {
            Error(message);
        }

        public T Get<T>(string guid) where T : BlueprintScriptableObject
        {
            return ResourcesLibrary.TryGetBlueprint<T>(NormalizeGuid(guid));
        }

        public T Require<T>(string guid, string name) where T : BlueprintScriptableObject
        {
            var blueprint = Get<T>(guid);
            if (blueprint == null)
            {
                throw new InvalidOperationException($"{name} ({guid}) was not available.");
            }

            return blueprint;
        }

        public void AddFeatureToLevel(BlueprintProgression progression, int level, BlueprintFeatureBase feature)
        {
            var entry = progression.LevelEntries.FirstOrDefault(e => e.Level == level);
            if (entry == null)
            {
                throw new InvalidOperationException($"{progression.name} has no level {level} entry.");
            }

            if (entry.Features.Any(f => f == feature))
            {
                return;
            }

            entry.SetFeatures(entry.Features.Concat(new[] { feature }));
        }

        public void AddScalingClass(BlueprintProgression progression, BlueprintCharacterClass characterClass)
        {
            var classes = (BlueprintProgression.ClassWithLevel[])BlueprintFields.ProgressionClasses.GetValue(progression);
            if (classes.Any(c => c.m_Class.Get() == characterClass))
            {
                return;
            }

            var classWithLevel = new BlueprintProgression.ClassWithLevel
            {
                m_Class = BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass),
                AdditionalLevel = 0
            };

            BlueprintFields.ProgressionClasses.SetValue(progression, classes.Concat(new[] { classWithLevel }).ToArray());
        }

        public void AddFeatureToSelection(BlueprintFeatureSelection selection, BlueprintFeature feature)
        {
            var reference = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature);

            AddFeatureReferenceToSelectionField(selection, BlueprintFields.FeatureSelectionFeatures, feature, reference);
            AddFeatureReferenceToSelectionField(selection, BlueprintFields.FeatureSelectionAllFeatures, feature, reference);
        }

        public void SetFeatureSelectionAllFeatures(BlueprintFeatureSelection selection, IEnumerable<BlueprintFeature> features)
        {
            var references = features
                .Where(feature => feature != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>)
                .ToArray();

            BlueprintFields.FeatureSelectionAllFeatures.SetValue(selection, references);
        }

        public void SetFeatureSelectionFeatures(BlueprintFeatureSelection selection, IEnumerable<BlueprintFeature> features)
        {
            var references = features
                .Where(feature => feature != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>)
                .ToArray();

            BlueprintFields.FeatureSelectionFeatures.SetValue(selection, references);
        }

        public BlueprintFeature[] GetFeatureSelectionAllFeatures(BlueprintFeatureSelection selection)
        {
            var references = (BlueprintFeatureReference[])BlueprintFields.FeatureSelectionAllFeatures.GetValue(selection);
            return (references ?? Array.Empty<BlueprintFeatureReference>())
                .Select(reference => reference.Get())
                .Where(feature => feature != null)
                .ToArray();
        }

        public void AddFeatureToRace(BlueprintRace race, BlueprintFeatureBase feature)
        {
            var features = (BlueprintFeatureBaseReference[])BlueprintFields.RaceFeatures.GetValue(race)
                           ?? Array.Empty<BlueprintFeatureBaseReference>();
            if (features.Any(featureReference => featureReference.Get() == feature))
            {
                return;
            }

            var newReference = BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>(feature);
            BlueprintFields.RaceFeatures.SetValue(race, features.Concat(new[] { newReference }).ToArray());
        }

        public void AddFactToUnitBlueprint(BlueprintUnit unit, BlueprintUnitFact fact)
        {
            var addFacts = (BlueprintUnitFactReference[])BlueprintFields.UnitAddFacts.GetValue(unit);
            if (addFacts.Any(r => r.Get() == fact))
            {
                return;
            }

            var reference = BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(fact);
            BlueprintFields.UnitAddFacts.SetValue(unit, addFacts.Concat(new[] { reference }).ToArray());
        }

        public T[] GetComponents<T>(BlueprintScriptableObject blueprint) where T : BlueprintComponent
        {
            var components = (BlueprintComponent[])BlueprintFields.BlueprintComponents.GetValue(blueprint);
            return components.OfType<T>().ToArray();
        }

        public void SetComponents(BlueprintScriptableObject blueprint, params BlueprintComponent[] components)
        {
            foreach (var component in components)
            {
                component.OwnerBlueprint = blueprint;
            }

            BlueprintFields.BlueprintComponents.SetValue(blueprint, components);
        }

        public void RemoveComponents<T>(BlueprintScriptableObject blueprint) where T : BlueprintComponent
        {
            var components = (BlueprintComponent[])BlueprintFields.BlueprintComponents.GetValue(blueprint) ??
                             Array.Empty<BlueprintComponent>();
            var filtered = components
                .Where(component => !(component is T))
                .ToArray();

            if (filtered.Length != components.Length)
            {
                BlueprintFields.BlueprintComponents.SetValue(blueprint, filtered);
            }
        }

        public void SetProgressionUiDeterminators(BlueprintProgression progression, IEnumerable<BlueprintFeatureBase> features)
        {
            if (BlueprintFields.ProgressionUIDeterminatorsGroup == null)
            {
                Error($"m_UIDeterminatorsGroup field not found, cannot set UI determinators for {progression.name}.");
                return;
            }

            var references = (features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .Where(feature => feature != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>)
                .ToArray();

            BlueprintFields.ProgressionUIDeterminatorsGroup.SetValue(progression, references);
        }

        public void SetProgressionUiGroups(BlueprintProgression progression, params IEnumerable<BlueprintFeatureBase>[] featureGroups)
        {
            if (BlueprintFields.UIGroupFeatures == null)
            {
                Error($"UIGroup.m_Features field not found, cannot set UI groups for {progression.name}.");
                return;
            }

            if (BlueprintFields.ProgressionUIGroups == null)
            {
                Error($"BlueprintProgression.UIGroups field not found, cannot set UI groups for {progression.name}.");
                return;
            }

            var groups = (featureGroups ?? Array.Empty<IEnumerable<BlueprintFeatureBase>>())
                .Select(CreateUiGroup)
                .Where(group => group != null)
                .ToArray();

            BlueprintFields.ProgressionUIGroups.SetValue(progression, groups);
        }

        public void AddProgressionUiGroup(BlueprintProgression progression, params BlueprintFeatureBase[] features)
        {
            if (BlueprintFields.UIGroupFeatures == null)
            {
                Error($"UIGroup.m_Features field not found, cannot add UI group for {progression.name}.");
                return;
            }

            if (BlueprintFields.ProgressionUIGroups == null)
            {
                Error($"BlueprintProgression.UIGroups field not found, cannot add UI group for {progression.name}.");
                return;
            }

            var groupFeatures = (features ?? Array.Empty<BlueprintFeatureBase>())
                .Where(feature => feature != null)
                .ToArray();
            if (groupFeatures.Length == 0)
            {
                return;
            }

            var groups = ((UIGroup[])BlueprintFields.ProgressionUIGroups.GetValue(progression) ?? Array.Empty<UIGroup>())
                .ToList();
            if (groups.Any(group => UiGroupContains(group, groupFeatures[0])))
            {
                return;
            }

            var newGroup = CreateUiGroup(groupFeatures);
            if (newGroup != null)
            {
                groups.Add(newGroup);
                BlueprintFields.ProgressionUIGroups.SetValue(progression, groups.ToArray());
            }
        }

        public void SetUnitFactDisplay(BlueprintUnitFact fact, LocalizedString name, LocalizedString description)
        {
            BlueprintFields.UnitFactDisplayName.SetValue(fact, name);
            BlueprintFields.UnitFactDescription.SetValue(fact, description);
        }

        public void CopyUnitFactDisplay(BlueprintUnitFact target, BlueprintUnitFact source)
        {
            if (target == null || source == null)
            {
                return;
            }

            SetUnitFactDisplay(
                target,
                (LocalizedString)BlueprintFields.UnitFactDisplayName.GetValue(source),
                (LocalizedString)BlueprintFields.UnitFactDescription.GetValue(source));

            if (source.Icon != null)
            {
                SetUnitFactIcon(target, source.Icon);
            }
        }

        public void SetUnitFactShortDescription(BlueprintUnitFact fact, LocalizedString description)
        {
            if (BlueprintFields.UnitFactDescriptionShort != null)
            {
                BlueprintFields.UnitFactDescriptionShort.SetValue(fact, description);
            }
        }

        public void ClearUnitFactDisplay(BlueprintUnitFact fact)
        {
            SetUnitFactDisplay(fact, new LocalizedString(), new LocalizedString());
        }

        public void SetAbilityDisplay(BlueprintAbility ability, LocalizedString name, LocalizedString description)
        {
            BlueprintFields.AbilityDisplayName.SetValue(ability, name);
            BlueprintFields.AbilityDescription.SetValue(ability, description);
        }

        public void SetItemDisplay(BlueprintItem item, LocalizedString name, LocalizedString description)
        {
            BlueprintFields.ItemDisplayName.SetValue(item, name);
            BlueprintFields.ItemDescription.SetValue(item, description);
        }

        public void AddWeaponEnchantment(BlueprintItemWeapon weapon, BlueprintWeaponEnchantment enchantment)
        {
            if (weapon == null || enchantment == null || BlueprintFields.ItemWeaponEnchantments == null)
            {
                return;
            }

            var enchantments = (BlueprintWeaponEnchantmentReference[])BlueprintFields.ItemWeaponEnchantments.GetValue(weapon)
                               ?? Array.Empty<BlueprintWeaponEnchantmentReference>();
            if (enchantments.Any(reference => reference?.Get()?.AssetGuid == enchantment.AssetGuid))
            {
                return;
            }

            BlueprintFields.ItemWeaponEnchantments.SetValue(
                weapon,
                enchantments
                    .Concat(new[] { BlueprintReferenceBase.CreateTyped<BlueprintWeaponEnchantmentReference>(enchantment) })
                    .ToArray());
        }

        public void SetArmorEnchantments(BlueprintItemArmor armor, params BlueprintArmorEnchantment[] enchantments)
        {
            if (armor == null || BlueprintFields.ItemArmorEnchantments == null)
            {
                return;
            }

            var references = (enchantments ?? Array.Empty<BlueprintArmorEnchantment>())
                .Where(enchantment => enchantment != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintEquipmentEnchantmentReference>)
                .ToArray();

            BlueprintFields.ItemArmorEnchantments.SetValue(armor, references);
        }

        public void SetAddUnitFeatureEquipmentFeature(AddUnitFeatureEquipment component, BlueprintFeature feature)
        {
            if (component == null || BlueprintFields.AddUnitFeatureEquipmentFeature == null)
            {
                return;
            }

            BlueprintFields.AddUnitFeatureEquipmentFeature.SetValue(
                component,
                feature == null ? null : BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));
        }

        public void SetUnitFactIcon(BlueprintUnitFact fact, Sprite icon)
        {
            if (BlueprintFields.UnitFactIcon != null)
            {
                BlueprintFields.UnitFactIcon.SetValue(fact, icon);
            }
        }

        public void AddComponent(BlueprintScriptableObject blueprint, BlueprintComponent component)
        {
            var components = (BlueprintComponent[])BlueprintFields.BlueprintComponents.GetValue(blueprint) ??
                             Array.Empty<BlueprintComponent>();
            component.OwnerBlueprint = blueprint;
            BlueprintFields.BlueprintComponents.SetValue(blueprint, components.Concat(new[] { component }).ToArray());
        }

        public void ReplaceComponent(BlueprintScriptableObject blueprint, BlueprintComponent oldComponent, BlueprintComponent newComponent)
        {
            var components = (BlueprintComponent[])BlueprintFields.BlueprintComponents.GetValue(blueprint) ?? Array.Empty<BlueprintComponent>();
            for (var i = 0; i < components.Length; i++)
            {
                if (!ReferenceEquals(components[i], oldComponent))
                {
                    continue;
                }

                newComponent.OwnerBlueprint = blueprint;
                components[i] = newComponent;
                BlueprintFields.BlueprintComponents.SetValue(blueprint, components);
                return;
            }
        }

        public T CloneComponent<T>(T source) where T : BlueprintComponent
        {
            return _cloner.CloneComponent(source);
        }

        public T EnsureComponent<T>(BlueprintScriptableObject blueprint, Func<T> factory) where T : BlueprintComponent
        {
            var existing = GetComponents<T>(blueprint).FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var component = factory();
            AddComponent(blueprint, component);
            return component;
        }

        public void SetCharacterClassDisplay(BlueprintCharacterClass characterClass, LocalizedString name, LocalizedString description)
        {
            BlueprintFields.CharacterClassLocalizedName.SetValue(characterClass, name);
            BlueprintFields.CharacterClassLocalizedDescription.SetValue(characterClass, description);
            BlueprintFields.CharacterClassLocalizedDescriptionShort.SetValue(characterClass, description);
        }

        public void SetCharacterClassProgression(BlueprintCharacterClass characterClass, BlueprintProgression progression)
        {
            BlueprintFields.CharacterClassProgression.SetValue(
                characterClass,
                BlueprintReferenceBase.CreateTyped<BlueprintProgressionReference>(progression));
        }

        public void SetCharacterClassSpellbook(BlueprintCharacterClass characterClass, BlueprintSpellbook spellbook)
        {
            BlueprintFields.CharacterClassSpellbook.SetValue(
                characterClass,
                BlueprintReferenceBase.CreateTyped<BlueprintSpellbookReference>(spellbook));
        }

        public void SetCharacterClassHitDie(BlueprintCharacterClass characterClass, Kingmaker.RuleSystem.DiceType hitDie)
        {
            BlueprintFields.CharacterClassHitDie.SetValue(characterClass, hitDie);
        }

        public void SetCharacterClassSkillPoints(BlueprintCharacterClass characterClass, int skillPoints)
        {
            characterClass.SkillPoints = skillPoints;
        }

        public void SetCharacterClassBaseAttackBonus(BlueprintCharacterClass characterClass, BlueprintStatProgression progression)
        {
            BlueprintFields.CharacterClassBaseAttackBonus.SetValue(
                characterClass,
                progression == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintStatProgressionReference>(progression));
        }

        public void SetCharacterClassArchetypes(BlueprintCharacterClass characterClass, params BlueprintArchetype[] archetypes)
        {
            foreach (var archetype in (archetypes ?? Array.Empty<BlueprintArchetype>()).Where(archetype => archetype != null))
            {
                SetArchetypeParentClass(archetype, characterClass);
            }

            var references = (archetypes ?? Enumerable.Empty<BlueprintArchetype>())
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintArchetypeReference>)
                .ToArray();
            BlueprintFields.CharacterClassArchetypes.SetValue(characterClass, references);

            if (string.Equals(characterClass?.AssetGuid.ToString(), ModBlueprintIds.Classes.Necromancer, StringComparison.OrdinalIgnoreCase))
            {
                Log("Necromancer archetypes assigned: " + DescribeArchetypes(archetypes));
            }
        }

        public void SetCharacterClassAppearanceFromClass(BlueprintCharacterClass target, BlueprintCharacterClass source)
        {
            CopyClassAppearanceField(BlueprintFields.CharacterClassPrimaryColor, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassSecondaryColor, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassEquipmentEntities, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassMaleEquipmentEntities, target, source);
            CopyClassAppearanceField(BlueprintFields.CharacterClassFemaleEquipmentEntities, target, source);
        }

        private static void CopyClassAppearanceField(FieldInfo field, BlueprintCharacterClass target, BlueprintCharacterClass source)
        {
            var value = field?.GetValue(source);
            if (value is Array array)
            {
                value = array.Clone();
            }

            field?.SetValue(target, value);
        }

        private static string DescribeArchetypes(IEnumerable<BlueprintArchetype> archetypes)
        {
            var values = (archetypes ?? Enumerable.Empty<BlueprintArchetype>())
                .Select(archetype => archetype == null
                    ? "<null>"
                    : $"{archetype.name}({archetype.AssetGuid})")
                .ToArray();
            return values.Length == 0 ? "<none>" : string.Join(", ", values);
        }

        private static string DescribeLevelEntries(IEnumerable<LevelEntry> entries)
        {
            var values = (entries ?? Enumerable.Empty<LevelEntry>())
                .Select(entry => entry == null
                    ? "<null>"
                    : $"L{entry.Level}=[{DescribeFeatures(entry.Features)}]")
                .ToArray();
            return values.Length == 0 ? "<none>" : string.Join("; ", values);
        }

        private static string DescribeFeatures(IEnumerable<BlueprintFeatureBase> features)
        {
            var values = (features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .Select(feature => feature == null
                    ? "<null>"
                    : $"{feature.name}({feature.AssetGuid})")
                .ToArray();
            return values.Length == 0 ? "<none>" : string.Join(", ", values);
        }

        public void SetArchetypeDisplay(BlueprintArchetype archetype, LocalizedString name, LocalizedString description)
        {
            BlueprintFields.ArchetypeLocalizedName.SetValue(archetype, name);
            BlueprintFields.ArchetypeLocalizedDescription.SetValue(archetype, description);
            BlueprintFields.ArchetypeLocalizedDescriptionShort.SetValue(archetype, description);
        }

        public void SetArchetypeReplaceSpellbook(BlueprintArchetype archetype, BlueprintSpellbook spellbook)
        {
            BlueprintFields.ArchetypeReplaceSpellbook.SetValue(
                archetype,
                spellbook == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintSpellbookReference>(spellbook));
        }

        public void SetArchetypeFeatureChanges(BlueprintArchetype archetype, IEnumerable<LevelEntry> addFeatures, IEnumerable<LevelEntry> removeFeatures)
        {
            var addEntries = (addFeatures ?? Enumerable.Empty<LevelEntry>()).ToArray();
            var removeEntries = (removeFeatures ?? Enumerable.Empty<LevelEntry>()).ToArray();
            BlueprintFields.ArchetypeAddFeatures.SetValue(archetype, addEntries);
            BlueprintFields.ArchetypeRemoveFeatures.SetValue(archetype, removeEntries);

            if (string.Equals(archetype?.AssetGuid.ToString(), ModBlueprintIds.Archetypes.Graveblade, StringComparison.OrdinalIgnoreCase))
            {
                Log("Graveblade AddFeatures: " + DescribeLevelEntries(addEntries));
                Log("Graveblade RemoveFeatures: " + DescribeLevelEntries(removeEntries));
            }
        }

        public void SetArchetypeBuildChanging(BlueprintArchetype archetype, bool buildChanging)
        {
            BlueprintFields.ArchetypeBuildChanging?.SetValue(archetype, buildChanging);
        }

        public void SetArchetypeParentClass(BlueprintArchetype archetype, BlueprintCharacterClass characterClass)
        {
            BlueprintFields.ArchetypeParentClass?.SetValue(archetype, characterClass);
        }

        public void SetArchetypeBaseAttackBonus(BlueprintArchetype archetype, BlueprintStatProgression progression)
        {
            BlueprintFields.ArchetypeBaseAttackBonus.SetValue(
                archetype,
                progression == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintStatProgressionReference>(progression));
        }

        public void SetArchetypeStartingEquipment(
            BlueprintArchetype archetype,
            bool replaceStartingEquipment,
            int startingGold,
            params BlueprintItem[] items)
        {
            BlueprintFields.ArchetypeReplaceStartingEquipment?.SetValue(archetype, replaceStartingEquipment);
            BlueprintFields.ArchetypeStartingGold?.SetValue(archetype, startingGold);
            BlueprintFields.ArchetypeStartingItems?.SetValue(
                archetype,
                (items ?? Array.Empty<BlueprintItem>())
                    .Where(item => item != null)
                    .Select(BlueprintReferenceBase.CreateTyped<BlueprintItemReference>)
                    .ToArray());
        }

        public int GetCharacterClassStartingGold(BlueprintCharacterClass characterClass)
        {
            return BlueprintFields.CharacterClassStartingGold?.GetValue(characterClass) is int gold
                ? gold
                : 0;
        }

        public void SetArchetypeStartingEquipmentFromClass(
            BlueprintArchetype archetype,
            BlueprintCharacterClass characterClass,
            params BlueprintItem[] additionalItems)
        {
            var startingGold = BlueprintFields.CharacterClassStartingGold?.GetValue(characterClass) is int gold
                ? gold
                : 0;
            var classItems =
                (BlueprintItemReference[])BlueprintFields.CharacterClassStartingItems?.GetValue(characterClass) ??
                Array.Empty<BlueprintItemReference>();
            var addedItems = (additionalItems ?? Array.Empty<BlueprintItem>())
                .Where(item => item != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintItemReference>);

            var startingItems = classItems
                .Concat(addedItems)
                .Where(reference => reference != null)
                .ToArray();

            BlueprintFields.ArchetypeReplaceStartingEquipment?.SetValue(archetype, true);
            BlueprintFields.ArchetypeStartingGold?.SetValue(archetype, startingGold);
            BlueprintFields.ArchetypeStartingItems?.SetValue(archetype, startingItems);
        }

        public void SetArchetypeAttributeRecommendations(
            BlueprintArchetype archetype,
            IEnumerable<StatType> recommendedAttributes,
            IEnumerable<StatType> notRecommendedAttributes)
        {
            BlueprintFields.ArchetypeOverrideAttributeRecommendations?.SetValue(archetype, true);
            BlueprintFields.ArchetypeRecommendedAttributes?.SetValue(
                archetype,
                (recommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
            BlueprintFields.ArchetypeNotRecommendedAttributes?.SetValue(
                archetype,
                (notRecommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
        }

        public void SetCharacterClassSignatureAbilities(BlueprintCharacterClass characterClass, params BlueprintFeature[] features)
        {
            var references = (features ?? Enumerable.Empty<BlueprintFeature>())
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>)
                .ToArray();
            BlueprintFields.CharacterClassSignatureAbilities.SetValue(characterClass, references);
        }

        public void SetArchetypeSignatureAbilities(BlueprintArchetype archetype, params BlueprintFeature[] features)
        {
            var references = (features ?? Enumerable.Empty<BlueprintFeature>())
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>)
                .ToArray();
            BlueprintFields.ArchetypeSignatureAbilities.SetValue(archetype, references);
        }

        public void SetCharacterClassDifficulty(BlueprintCharacterClass characterClass, int difficulty)
        {
            BlueprintFields.CharacterClassDifficulty?.SetValue(characterClass, difficulty);
        }

        public void SetCharacterClassAttributeRecommendations(
            BlueprintCharacterClass characterClass,
            IEnumerable<StatType> recommendedAttributes,
            IEnumerable<StatType> notRecommendedAttributes)
        {
            BlueprintFields.CharacterClassRecommendedAttributes?.SetValue(
                characterClass,
                (recommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
            BlueprintFields.CharacterClassNotRecommendedAttributes?.SetValue(
                characterClass,
                (notRecommendedAttributes ?? Enumerable.Empty<StatType>()).ToArray());
        }

        public void SetCharacterClassDefaultBuild(BlueprintCharacterClass characterClass, BlueprintFeature defaultBuild)
        {
            BlueprintFields.CharacterClassDefaultBuild?.SetValue(
                characterClass,
                defaultBuild == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(defaultBuild));
        }

        public void SetSpellbookSpellList(BlueprintSpellbook spellbook, BlueprintSpellList spellList)
        {
            BlueprintFields.SpellbookSpellList.SetValue(
                spellbook,
                BlueprintReferenceBase.CreateTyped<BlueprintSpellListReference>(spellList));
        }

        public void SetSpellbookCharacterClass(BlueprintSpellbook spellbook, BlueprintCharacterClass characterClass)
        {
            BlueprintFields.SpellbookCharacterClass.SetValue(
                spellbook,
                BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
        }

        public void CopySpellbookProgression(BlueprintSpellbook target, BlueprintSpellbook source)
        {
            BlueprintFields.SpellbookSpellsPerDay.SetValue(target, BlueprintFields.SpellbookSpellsPerDay.GetValue(source));
            BlueprintFields.SpellbookSpellsKnown.SetValue(target, BlueprintFields.SpellbookSpellsKnown.GetValue(source));
            BlueprintFields.SpellbookSpellSlots.SetValue(target, BlueprintFields.SpellbookSpellSlots.GetValue(source));

            CopySpellbookField(target, source, "Spontaneous");
            CopySpellbookField(target, source, "SpellsPerLevel");
            CopySpellbookField(target, source, "AllSpellsKnown");
            CopySpellbookField(target, source, "CasterLevelModifier");
        }

        public void ConfigureClassLevelsForPrerequisites(
            BlueprintFeature feature,
            BlueprintCharacterClass fakeClass,
            BlueprintCharacterClass actualClass,
            BlueprintFeatureSelection selection,
            double modifier,
            int summand)
        {
            foreach (var component in GetComponents<BlueprintComponent>(feature)
                         .Where(component => component.GetType().Name == "ClassLevelsForPrerequisites"))
            {
                SetComponentField(component, "m_FakeClass", BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(fakeClass));
                SetComponentField(component, "m_ActualClass", BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(actualClass));
                SetComponentField(component, "m_ForSelection", BlueprintReferenceBase.CreateTyped<BlueprintFeatureSelectionReference>(selection));
                SetComponentField(component, "Modifier", modifier);
                SetComponentField(component, "Summand", summand);
            }
        }

        public void SetProgressionClasses(BlueprintFeatureBase feature, params BlueprintCharacterClass[] classes)
        {
            SetProgressionClassesInternal(feature, classes, new HashSet<BlueprintGuid>());
        }

        private void SetProgressionClassesInternal(BlueprintFeatureBase feature, BlueprintCharacterClass[] classes, HashSet<BlueprintGuid> visited)
        {
            if (feature == null || visited.Contains(feature.AssetGuid)) return;
            visited.Add(feature.AssetGuid);
            
            Log($"Setting progression classes for {feature.name} ({feature.AssetGuid})");
            
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
                // WotR sometimes uses m_CharacterClass (singular) for some features
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
                        for (int i = 0; i < classes.Length; i++)
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
                
                // Deep register for progression features
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
                // Deep register for selection features
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

        public T CloneBlueprint<T>(T source, string guid, string name) where T : BlueprintScriptableObject
        {
            return _cloner.CloneBlueprint(source, guid, name);
        }

        public bool AddSpellToList(BlueprintSpellList spellList, BlueprintAbility spell, int level)
        {
            if (spellList?.SpellsByLevel == null || spell == null)
            {
                return false;
            }

            if (level < 0 || level >= spellList.SpellsByLevel.Length)
            {
                throw new InvalidOperationException($"{spellList.name} has no spell level {level}.");
            }

            var levelList = spellList.SpellsByLevel[level];
            if (levelList.Spells.Any(existing => existing != null && existing.AssetGuid == spell.AssetGuid))
            {
                return false;
            }

            var spells = (List<BlueprintAbilityReference>)BlueprintFields.SpellLevelListSpells.GetValue(levelList);
            spells.Add(BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(spell));
            return true;
        }

        public bool RemoveSpellFromList(BlueprintSpellList spellList, BlueprintAbility spell)
        {
            if (spellList?.SpellsByLevel == null || spell == null)
            {
                return false;
            }

            var removed = false;
            foreach (var levelList in spellList.SpellsByLevel)
            {
                var spells = (List<BlueprintAbilityReference>)BlueprintFields.SpellLevelListSpells.GetValue(levelList);
                removed |= spells.RemoveAll(reference => reference?.Get()?.AssetGuid == spell.AssetGuid) > 0;
            }

            return removed;
        }

        public void SetSpellListSpells(BlueprintSpellList spellList, IEnumerable<KeyValuePair<BlueprintAbility, int>> spellsByLevel)
        {
            if (spellList == null)
            {
                return;
            }

            var maxLevel = Math.Max(9, (spellList.SpellsByLevel?.Length ?? 0) - 1);
            var levels = Enumerable.Range(0, maxLevel + 1)
                .Select(level => new SpellLevelList(level))
                .ToArray();

            foreach (var pair in spellsByLevel ?? Enumerable.Empty<KeyValuePair<BlueprintAbility, int>>())
            {
                var spell = pair.Key;
                var level = pair.Value;
                if (spell == null || level < 0 || level >= levels.Length)
                {
                    continue;
                }

                var spells = (List<BlueprintAbilityReference>)BlueprintFields.SpellLevelListSpells.GetValue(levels[level]);
                if (spells.Any(existing => existing?.Get()?.AssetGuid == spell.AssetGuid))
                {
                    continue;
                }

                spells.Add(BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(spell));
            }

            spellList.SpellsByLevel = levels;
        }

        public void CopySpellList(
            BlueprintSpellList source,
            BlueprintSpellList target,
            Func<BlueprintAbility, bool> include)
        {
            if (source?.SpellsByLevel == null || target == null)
            {
                return;
            }

            target.SpellsByLevel = source.SpellsByLevel
                .Select(level =>
                {
                    var copy = new SpellLevelList(level.SpellLevel);
                    var spells = level.Spells
                        .Where(spell => spell != null && (include == null || include(spell)))
                        .Select(BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>)
                        .ToList();

                    BlueprintFields.SpellLevelListSpells.SetValue(copy, spells);
                    return copy;
                })
                .ToArray();
        }

        public IEnumerable<T> GetLoadedBlueprints<T>() where T : SimpleBlueprint
        {
            var result = new List<T>();
            ResourcesLibrary.BlueprintsCache.ForEachLoaded((guid, blueprint) =>
            {
                var typed = blueprint as T;
                if (typed != null)
                {
                    result.Add(typed);
                }
            });

            return result;
        }

        public void SetAddFeatureOnApplyFeature(AddFeatureOnApply component, BlueprintFeature feature)
        {
            BlueprintFields.AddFeatureOnApplyFeature.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));
        }

        public void SetRemoveFeatureOnApplyFeature(RemoveFeatureOnApply component, BlueprintFeature feature)
        {
            BlueprintFields.RemoveFeatureOnApplyFeature.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(feature));
        }

        public void SetAddFacts(AddFacts component, params BlueprintUnitFact[] facts)
        {
            var references = (facts ?? Array.Empty<BlueprintUnitFact>())
                .Where(fact => fact != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>)
                .ToArray();
            BlueprintFields.AddFactsFacts.SetValue(component, references);
        }

        public void SetPrerequisiteNoFeatureFeature(PrerequisiteNoFeature component, BlueprintFeature feature)
        {
            BlueprintFields.PrerequisiteNoFeatureFeature.SetValue(
                component,
                BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));
        }

        public void SetAddStartingEquipment(
            AddStartingEquipment component,
            IEnumerable<BlueprintItem> basicItems,
            params BlueprintCharacterClass[] restrictedByClass)
        {
            BlueprintFields.AddStartingEquipmentBasicItems?.SetValue(
                component,
                (basicItems ?? Enumerable.Empty<BlueprintItem>())
                    .Where(item => item != null)
                    .Select(BlueprintReferenceBase.CreateTyped<BlueprintItemReference>)
                    .ToArray());
            BlueprintFields.AddStartingEquipmentRestrictedByClass?.SetValue(
                component,
                (restrictedByClass ?? Array.Empty<BlueprintCharacterClass>())
                    .Where(characterClass => characterClass != null)
                    .Select(BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>)
                    .ToArray());
            component.CategoryItems = Array.Empty<WeaponCategory>();
            component.ParametrizedCategory = false;
        }

        public bool AddItemToLoot(BlueprintLoot loot, BlueprintItem item, int count, bool identify)
        {
            if (loot == null || item == null)
            {
                return false;
            }

            var entries = loot.Items ?? Array.Empty<LootEntry>();
            if (entries.Any(entry => LootEntryMatches(entry, item)))
            {
                return false;
            }

            var newEntry = new LootEntry
            {
                Count = count,
                Identify = identify
            };
            BlueprintFields.LootEntryItem.SetValue(
                newEntry,
                BlueprintReferenceBase.CreateTyped<BlueprintItemReference>(item));

            loot.Items = entries.Concat(new[] { newEntry }).ToArray();
            return true;
        }

        public LootItemsPackFixed CreateFixedLootItem(
            BlueprintItem item,
            int count,
            bool identify,
            string nameSuffix)
        {
            var lootItem = new LootItem();
            BlueprintFields.LootItemType.SetValue(lootItem, LootItemType.Item);
            BlueprintFields.LootItemItem.SetValue(
                lootItem,
                item == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintItemReference>(item));
            BlueprintFields.LootItemLoot.SetValue(lootItem, null);

            var pack = new LootItemsPackFixed
            {
                name = "$LootItemsPackFixed$" + nameSuffix
            };
            BlueprintFields.LootItemsPackFixedItem.SetValue(pack, lootItem);
            BlueprintFields.LootItemsPackFixedCount.SetValue(pack, count);
            return pack;
        }

        public bool AddLootToUnit(BlueprintUnit unit, BlueprintUnitLoot loot, string componentName)
        {
            if (unit == null || loot == null)
            {
                return false;
            }

            if (GetComponents<AddLoot>(unit).Any(component => AddLootMatches(component, loot)))
            {
                return false;
            }

            var newComponent = new AddLoot { name = componentName };
            BlueprintFields.AddLootLoot.SetValue(
                newComponent,
                BlueprintReferenceBase.CreateTyped<BlueprintUnitLootReference>(loot));
            AddComponent(unit, newComponent);
            return true;
        }

        private static bool LootEntryMatches(LootEntry entry, BlueprintItem item)
        {
            var reference = BlueprintFields.LootEntryItem.GetValue(entry) as BlueprintItemReference;
            return reference?.Get()?.AssetGuid == item.AssetGuid;
        }

        private static bool AddLootMatches(AddLoot component, BlueprintUnitLoot loot)
        {
            var reference = BlueprintFields.AddLootLoot.GetValue(component) as BlueprintUnitLootReference;
            return reference?.Get()?.AssetGuid == loot.AssetGuid;
        }

        public void AddCheckedFact(AddStatBonusIfHasFact component, BlueprintUnitFact fact)
        {
            AddCheckedFactReference(BlueprintFields.AddStatBonusIfHasFactCheckedFacts, component, fact);
        }

        public void AddCheckedFact(RecalculateOnFactsChange component, BlueprintUnitFact fact)
        {
            AddCheckedFactReference(BlueprintFields.RecalculateOnFactsChangeCheckedFacts, component, fact);
        }

        private static void AddCheckedFactReference(FieldInfo checkedFactsField, object component, BlueprintUnitFact fact)
        {
            if (checkedFactsField == null || component == null || fact == null)
            {
                return;
            }

            var references = (BlueprintUnitFactReference[])checkedFactsField.GetValue(component) ??
                             Array.Empty<BlueprintUnitFactReference>();
            if (references.Any(reference => reference != null && reference.Get() == fact))
            {
                return;
            }

            var newReference = BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(fact);
            checkedFactsField.SetValue(component, references.Concat(new[] { newReference }).ToArray());
        }

        public void SetAddKnownSpell(
            AddKnownSpell component,
            BlueprintCharacterClass characterClass,
            BlueprintAbility spell,
            int spellLevel)
        {
            BlueprintFields.AddKnownSpellCharacterClass.SetValue(
                component,
                characterClass == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
            BlueprintFields.AddKnownSpellSpell.SetValue(
                component,
                spell == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(spell));
            if (BlueprintFields.AddKnownSpellArchetype != null)
            {
                BlueprintFields.AddKnownSpellArchetype.SetValue(component, null);
            }

            component.SpellLevel = spellLevel;
        }

        public void AddSelectionRecommendation(
            BlueprintScriptableObject blueprint,
            SelectionRecommendation recommendation,
            string componentName)
        {
            if (blueprint == null)
            {
                return;
            }

            var priority = ToRecommendationPriority(recommendation);
            if (GetComponents<PureRecommendation>(blueprint).Any(component =>
                component.Priority == priority &&
                component.name == componentName))
            {
                return;
            }

            AddComponent(
                blueprint,
                new PureRecommendation
                {
                    name = componentName,
                    Priority = priority
                });
        }

        public void AddSelectionRecommendationIfMissingFeature(
            BlueprintScriptableObject blueprint,
            BlueprintUnitFact feature,
            SelectionRecommendation recommendationWhenMissingFeature,
            string componentName)
        {
            if (blueprint == null || feature == null)
            {
                return;
            }

            var goodIfNoFeature = recommendationWhenMissingFeature == SelectionRecommendation.Recommended;
            if (GetComponents<RecommendationNoFeatFromGroup>(blueprint).Any(component =>
                component.GoodIfNoFeature == goodIfNoFeature &&
                GetRecommendationFeatures(component).Any(reference => reference?.Get()?.AssetGuid == feature.AssetGuid)))
            {
                return;
            }

            var recommendation = new RecommendationNoFeatFromGroup
            {
                name = componentName,
                GoodIfNoFeature = goodIfNoFeature
            };

            BlueprintFields.RecommendationNoFeatFromGroupFeatures.SetValue(
                recommendation,
                new[] { BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(feature) });
            BlueprintFields.RecommendationNoFeatFromGroupExcludedFeatures.SetValue(
                recommendation,
                Array.Empty<BlueprintUnitFactReference>());

            AddComponent(blueprint, recommendation);
        }

        private static BlueprintUnitFactReference[] GetRecommendationFeatures(RecommendationNoFeatFromGroup component)
        {
            return (BlueprintUnitFactReference[])BlueprintFields.RecommendationNoFeatFromGroupFeatures.GetValue(component) ??
                   Array.Empty<BlueprintUnitFactReference>();
        }

        private static RecommendationPriority ToRecommendationPriority(SelectionRecommendation recommendation)
        {
            return recommendation == SelectionRecommendation.Recommended
                ? RecommendationPriority.Good
                : RecommendationPriority.Bad;
        }

        public void SetAddAbilityResourcesResource(AddAbilityResources component, BlueprintAbilityResource resource)
        {
            BlueprintFields.AddAbilityResourcesResource.SetValue(
                component,
                resource == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintAbilityResourceReference>(resource));
        }

        public void ConfigureAbilityResourceMaxAmount(
            BlueprintAbilityResource resource,
            int baseValue,
            StatType bonusStat,
            BlueprintCharacterClass characterClass = null,
            int levelIncrease = 0)
        {
            var amountType = typeof(BlueprintAbilityResource).GetNestedType(
                "Amount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var amount = Activator.CreateInstance(amountType);
            amountType.GetField("BaseValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(amount, baseValue);
            amountType.GetField("IncreasedByStat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(amount, true);
            amountType.GetField("ResourceBonusStat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(amount, bonusStat);
            if (characterClass != null && levelIncrease > 0)
            {
                amountType.GetField("IncreasedByLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(amount, true);
                amountType.GetField("LevelIncrease", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(amount, levelIncrease);
                amountType.GetField("m_Class", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(
                        amount,
                        new[] { BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass) });
            }

            BlueprintFields.AbilityResourceMaxAmount.SetValue(resource, amount);
        }

        public void SetAbilityResourceLogicResource(AbilityResourceLogic component, BlueprintAbilityResource resource)
        {
            BlueprintFields.AbilityResourceLogicRequiredResource.SetValue(
                component,
                resource == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintAbilityResourceReference>(resource));
        }

        public void SetAbilityResourceLogicSpendResource(AbilityResourceLogic component, bool spendResource)
        {
            BlueprintFields.AbilityResourceLogicIsSpendResource.SetValue(component, spendResource);
        }

        public void SetAbilityDeliverProjectiles(
            AbilityDeliverProjectile component,
            params BlueprintProjectile[] projectiles)
        {
            var references = (projectiles ?? Array.Empty<BlueprintProjectile>())
                .Where(projectile => projectile != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintProjectileReference>)
                .ToArray();

            BlueprintFields.AbilityDeliverProjectileProjectiles.SetValue(component, references);
        }

        public void SetAbilityDeliverProjectileLength(
            AbilityDeliverProjectile component,
            Feet length)
        {
            BlueprintFields.AbilityDeliverProjectileLength.SetValue(component, length);
        }

        public void SetApplyBuffActionBuff(ContextActionApplyBuff action, BlueprintBuff buff)
        {
            BlueprintFields.ContextActionApplyBuffBuff.SetValue(
                action,
                buff == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(buff));
        }

        public void SetRemoveBuffActionBuff(ContextActionRemoveBuff action, BlueprintBuff buff)
        {
            BlueprintFields.ContextActionRemoveBuffBuff.SetValue(
                action,
                buff == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(buff));
        }

        public void SetAddAreaEffect(AddAreaEffect component, BlueprintAbilityAreaEffect areaEffect)
        {
            BlueprintFields.AddAreaEffectArea.SetValue(
                component,
                areaEffect == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(areaEffect));
        }

        public void SetBuffOnArmorBuff(BuffOnArmor component, BlueprintBuff buff)
        {
            BlueprintFields.BuffOnArmorBuff.SetValue(
                component,
                buff == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>(buff));
        }

        public void SetAbilityCasterHasNoFacts(
            AbilityCasterHasNoFacts component,
            params BlueprintUnitFact[] facts)
        {
            var references = (facts ?? Array.Empty<BlueprintUnitFact>())
                .Where(fact => fact != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>)
                .ToArray();
            BlueprintFields.AbilityCasterHasNoFactsFacts.SetValue(component, references);
        }

        public void BindAbilityComponentsToClass(BlueprintFeature feature, BlueprintCharacterClass characterClass)
        {
            foreach (var component in GetComponents<BindAbilitiesToClass>(feature))
            {
                BlueprintFields.BindAbilitiesToClassCharacterClass.SetValue(
                    component,
                    characterClass == null
                        ? null
                        : BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
                BlueprintFields.BindAbilitiesToClassAdditionalClasses.SetValue(component, Array.Empty<BlueprintCharacterClassReference>());
                BlueprintFields.BindAbilitiesToClassArchetypes.SetValue(component, Array.Empty<BlueprintArchetypeReference>());
            }

            foreach (var component in GetComponents<ReplaceCasterLevelOfAbility>(feature))
            {
                BlueprintFields.ReplaceCasterLevelOfAbilityClass.SetValue(
                    component,
                    characterClass == null
                        ? null
                        : BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(characterClass));
                BlueprintFields.ReplaceCasterLevelOfAbilityAdditionalClasses.SetValue(component, Array.Empty<BlueprintCharacterClassReference>());
                BlueprintFields.ReplaceCasterLevelOfAbilityArchetypes.SetValue(component, Array.Empty<BlueprintArchetypeReference>());
            }
        }

        public void AddCachedBlueprint(string guid, SimpleBlueprint blueprint)
        {
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(BlueprintGuid.Parse(NormalizeGuid(guid)), blueprint);
        }

        public void SetSpawnAreaEffect(ContextActionSpawnAreaEffect action, BlueprintAbilityAreaEffect areaEffect)
        {
            BlueprintFields.SpawnAreaEffectArea.SetValue(
                action,
                BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(areaEffect));
        }

        public void ConfigureContextRankConfig(
            ContextRankConfig config,
            AbilityRankType type = AbilityRankType.Default,
            ContextRankBaseValueType baseValueType = ContextRankBaseValueType.CasterLevel,
            ContextRankProgression progression = ContextRankProgression.AsIs,
            int startLevel = 0,
            int stepLevel = 0,
            BlueprintCharacterClass characterClass = null,
            BlueprintCharacterClass[] additionalClasses = null)
        {
            BlueprintFields.ContextRankConfigType.SetValue(config, type);
            BlueprintFields.ContextRankConfigBaseValueType.SetValue(config, baseValueType);
            BlueprintFields.ContextRankConfigProgression.SetValue(config, progression);
            BlueprintFields.ContextRankConfigStartLevel.SetValue(config, startLevel);
            BlueprintFields.ContextRankConfigStepLevel.SetValue(config, stepLevel);

            var classReferences = new[] { characterClass }
                .Concat(additionalClasses ?? Array.Empty<BlueprintCharacterClass>())
                .Where(c => c != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>)
                .ToArray();

            if (classReferences.Length > 0)
            {
                if (BlueprintFields.ContextRankConfigClass.FieldType.IsArray)
                {
                    BlueprintFields.ContextRankConfigClass.SetValue(config, classReferences);
                }
                else
                {
                    BlueprintFields.ContextRankConfigClass.SetValue(config, classReferences[0]);
                }
            }

            if (BlueprintFields.ContextRankConfigArchetype != null)
            {
                BlueprintFields.ContextRankConfigArchetype.SetValue(config, null);
            }

            if (BlueprintFields.ContextRankConfigAdditionalArchetypes != null)
            {
                BlueprintFields.ContextRankConfigAdditionalArchetypes.SetValue(config, Array.Empty<BlueprintArchetypeReference>());
            }
        }

        public void SetContextRankMinimum(ContextRankConfig config, int minimum)
        {
            if (BlueprintFields.ContextRankConfigUseMin != null)
            {
                BlueprintFields.ContextRankConfigUseMin.SetValue(config, true);
            }

            if (BlueprintFields.ContextRankConfigMin != null)
            {
                BlueprintFields.ContextRankConfigMin.SetValue(config, minimum);
            }
        }

        public void SetContextRankMaximum(ContextRankConfig config, int maximum)
        {
            if (BlueprintFields.ContextRankConfigUseMax != null)
            {
                BlueprintFields.ContextRankConfigUseMax.SetValue(config, true);
            }

            if (BlueprintFields.ContextRankConfigMax != null)
            {
                BlueprintFields.ContextRankConfigMax.SetValue(config, maximum);
            }
        }

        public void ConfigureFeatureRankCustomProgression(
            ContextRankConfig config,
            BlueprintFeature feature,
            params int[] progressionValues)
        {
            BlueprintFields.ContextRankConfigType.SetValue(config, AbilityRankType.Default);
            BlueprintFields.ContextRankConfigBaseValueType.SetValue(config, ContextRankBaseValueType.FeatureRank);
            BlueprintFields.ContextRankConfigProgression.SetValue(config, ContextRankProgression.Custom);
            BlueprintFields.ContextRankConfigFeature.SetValue(
                config,
                feature == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));

            var itemType = typeof(ContextRankConfig).GetNestedType("CustomProgressionItem", BindingFlags.Public | BindingFlags.NonPublic);
            var items = Array.CreateInstance(itemType, progressionValues.Length);
            var baseValueField = itemType.GetField("BaseValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var progressionValueField = itemType.GetField("ProgressionValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (var i = 0; i < progressionValues.Length; i++)
            {
                var item = Activator.CreateInstance(itemType);
                baseValueField.SetValue(item, i + 1);
                progressionValueField.SetValue(item, progressionValues[i]);
                items.SetValue(item, i);
            }

            BlueprintFields.ContextRankConfigCustomProgression.SetValue(config, items);
        }

        public void ConfigureFeatureRankAsIs(ContextRankConfig config, BlueprintFeature feature)
        {
            BlueprintFields.ContextRankConfigType.SetValue(config, AbilityRankType.Default);
            BlueprintFields.ContextRankConfigBaseValueType.SetValue(config, ContextRankBaseValueType.FeatureRank);
            BlueprintFields.ContextRankConfigProgression.SetValue(config, ContextRankProgression.AsIs);
            BlueprintFields.ContextRankConfigFeature.SetValue(
                config,
                feature == null
                    ? null
                    : BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature));
        }

        public void SetCharacterClassHidden(BlueprintCharacterClass characterClass, bool hidden)
        {
            if (characterClass == null) return;
            if (BlueprintFields.CharacterClassHiddenFields.Length == 0)
            {
                Error("No BlueprintCharacterClass hidden fields found, cannot set visibility.");
                return;
            }

            foreach (var field in BlueprintFields.CharacterClassHiddenFields)
            {
                field.SetValue(characterClass, hidden);
            }
        }

        public void AddCharacterClassToRoot(BlueprintCharacterClass characterClass)
        {
            _classRegistration.AddCharacterClassToRoot(characterClass);
        }

        public void AddRaceToRoot(BlueprintRace race, int insertAt = -1)
        {
            var root = Require<Kingmaker.Blueprints.Root.BlueprintRoot>(
                GameBlueprintIds.Root.BlueprintRoot, "BlueprintRoot");

            var rootType = root.GetType();
            var progressionProp = rootType.GetProperty(
                "Progression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var progressionRoot = progressionProp != null
                ? progressionProp.GetValue(root, null)
                : rootType.GetField("Progression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(root);

            if (progressionRoot == null)
            {
                Error("BlueprintRoot.Progression was not available.");
                return;
            }

            var racesField = progressionRoot.GetType()
                .GetField("m_CharacterRaces", BindingFlags.Instance | BindingFlags.NonPublic);
            if (racesField == null)
            {
                Error("ProgressionRoot.m_CharacterRaces field not found.");
                return;
            }

            var reference = BlueprintReferenceBase.CreateTyped<BlueprintRaceReference>(race);
            var races = ((BlueprintRaceReference[])racesField.GetValue(progressionRoot)
                        ?? Array.Empty<BlueprintRaceReference>()).ToList();
            if (races.Any(r => r?.Get()?.AssetGuid == race.AssetGuid))
                return;

            if (insertAt >= 0 && insertAt <= races.Count)
                races.Insert(insertAt, reference);
            else
                races.Add(reference);

            racesField.SetValue(progressionRoot, races.ToArray());
        }

        public void ReportCharacterClassRegistrationErrors(BlueprintCharacterClass characterClass, string contextName)
        {
            _classRegistration.ReportCharacterClassRegistrationErrors(characterClass, contextName);
        }

        public static string NormalizeGuid(string guid)
        {
            return string.IsNullOrWhiteSpace(guid)
                ? guid
                : guid.Trim().Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void AddFeatureReferenceToSelectionField(
            BlueprintFeatureSelection selection,
            System.Reflection.FieldInfo field,
            BlueprintFeature feature,
            BlueprintFeatureReference reference)
        {
            var features = (BlueprintFeatureReference[])field.GetValue(selection)
                           ?? Array.Empty<BlueprintFeatureReference>();
            if (features.Any(r => r.Get() == feature))
            {
                return;
            }

            field.SetValue(selection, features.Concat(new[] { reference }).ToArray());
        }

        private static UIGroup CreateUiGroup(IEnumerable<BlueprintFeatureBase> features)
        {
            var references = (features ?? Enumerable.Empty<BlueprintFeatureBase>())
                .Where(feature => feature != null)
                .Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureBaseReference>)
                .ToList();

            if (references.Count == 0)
            {
                return null;
            }

            var group = new UIGroup();
            BlueprintFields.UIGroupFeatures.SetValue(group, references);
            return group;
        }

        private static bool UiGroupContains(UIGroup group, BlueprintFeatureBase feature)
        {
            var references = BlueprintFields.UIGroupFeatures?.GetValue(group) as IEnumerable<BlueprintFeatureBaseReference>;
            return references != null && references.Any(reference => reference?.Get() == feature);
        }

        private static void CopySpellbookField(BlueprintSpellbook target, BlueprintSpellbook source, string fieldName)
        {
            var field = typeof(BlueprintSpellbook).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, field.GetValue(source));
            }
        }

        private static void SetComponentField(BlueprintComponent component, string fieldName, object value)
        {
            var field = component.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            if (value != null && value.GetType() != field.FieldType && value is IConvertible)
            {
                value = Convert.ChangeType(value, field.FieldType);
            }

            field.SetValue(component, value);
        }

    }
}
