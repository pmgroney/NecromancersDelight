# Debugging Notes

## Necromancer Class Visibility

- Culprit found 2026-05-01: the Necromancer class was hidden because class content installation threw before `AddCharacterClassToRoot` ran.
- The specific failure was `NecromancerInstaller.EnsureGravebladeArchetype` hard-requiring unfinished custom Necromancer feature blueprints, starting with `Withering Ray` (`c3d4e5f6a7b84960c1d2e3f4a5b6c7d8`), while building Graveblade archetype remove-feature entries.
- Do not use `BlueprintTool.Require` for optional, unfinished, or best-effort mod-owned features during class/archetype setup. Use nullable lookups, log the missing feature, and filter nulls from `LevelEntry` and UI groups.
- Class content installer failures must be logged but must not prevent `ConfigureClassPresentation`, `AddCharacterClassToRoot`, or registration diagnostics from running. A partially configured class is easier to inspect than a class that never appears.
- Local WotR class visibility fields are `HideInUI` and `HideIfRestricted`. Older guessed fields such as `m_HiddenInCharacterCreation` and `m_Hidden` are not the culprit in this install.
- Recovery action 2026-05-01: `Classes\NecromancerInstaller.cs` was removed from the project compile list, and `CharacterClassInstaller` now calls its complete in-place Necromancer install path again. Do not re-enable the extracted installer until it creates every Necromancer feature and archetype that the old path creates.
- Billy placement must tolerate a missing Billy companion unit during `Apply`; companion installation may log-and-continue, so unrelated downstream patches like Fetchling and custom heritages still need to run.
- Billy area stand-ins: `CreateUnitVacuum` plus manual `LoadedAreaState.AddEntityData` can yield viewless units and adding roster Billy can trigger "Entity Data <id> Unique ID was duplicated"; use `Game.Instance.AddUnitToPersistentState(BlueprintUnit)`, then position/orient/create view, and make existing-stand-in guards ignore recruited roster instances.
- Handle `UIGroup.m_Features` as a `BlueprintFeatureBaseReference` enumerable, not `BlueprintFeatureReference[]`; otherwise progression UI chains can keep stale references when level-entry features are replaced.
- Cloned custom bloodlines may reference mod-owned replacement features; replace both the original donor feature and the mod-owned replacement.
