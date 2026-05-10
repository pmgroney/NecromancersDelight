# Spell Implementation Notes

These notes are mutable. They document current working assumptions from the codebase and should be updated when a spell implementation proves one of them wrong.

## Add A Spell

Use the existing spell pipeline unless a specific mechanic forces a different path:

1. Add stable GUID constants in `wotr_mod/Infrastructure/ModBlueprintIds.cs`.
   - Add spell GUIDs under `Spells`.
   - Add projectile GUIDs under `Projectiles` when the spell clones or retints projectile visuals.
   - Add buff GUIDs under `Buffs` for failed-save riders, damage-over-time effects, area debuffs, or conditions.
   - Preserve existing GUIDs once added.
2. Add the spell definition in `wotr_mod/Spells/SpellRegistry.cs`.
   - Pick the simplest source blueprint that already matches the delivery shape.
   - Use `Spell(...)` with the source GUID, new GUID, internal name, level, school, localization key stem, icon path, and modifier.
   - Reuse small modifier helpers for simple conversions. Use a spell-specific `ISpellModifier` when the action list, area, range, or rider logic is custom.
3. Add localization in `wotr_mod/Data/Localization/Spells.resx`.
   - Registry key stem `vitriolic_sphere` maps to `wotr_mod.spell.vitriolic_sphere.name` and `.description`.
4. Add class-list entries where needed.
   - `SpellInstaller.Install()` adds most spells to the wizard list.
   - Necromancy spells are also added to Cleric/Oracle by `IsDivineListSpell`.
   - Evoker availability requires an entry in `wotr_mod/Classes/Evoker/EvokerSpellRegistry.cs`.
   - Evoker spell-list entries resolve through Evoker-owned ability clones with deterministic GUIDs; update the clone path for Evoker-specific behavior or description changes rather than altering global source spell blueprints.
   - Evoker-owned spell clones, including Evoker-cloned spawned area effects, clear `ContextRankConfig` maximums and projectile max-count caps for damaging Evocation spells; keep this rule isolated to the Evoker spell list.
   - Necromancer availability requires an entry in `wotr_mod/Classes/Necromancer/NecromancerSpellRegistry.cs`.
5. Add new modifier files to `wotr_mod/wotr_mod.csproj`.
6. Add icon content to the project only if the icon is new. Existing icons may already be included.

## Installer Shape

`SpellInstaller.EnsureSpell` is the core path:

- Look up an existing mod blueprint by GUID.
- If missing, clone the source `BlueprintAbility`.
- Apply display metadata and icon.
- Apply the `ISpellModifier`.
- Call `OnEnable()`.
- Add the clone to the blueprint cache.
- Configure visuals after the modifier.

Modifiers are also applied to existing cached spell blueprints, so modifier code must be idempotent. Prefer checking by action/component names before appending new actions.

## Modifier Patterns

Use `ISpellModifier.Apply(SpellModifierContext context)` for spell behavior.

Common patterns:

- `ConfigureSpellModifier`: set school and optionally range for simple spells.
- `DamageTypeSpellModifier`: convert existing damage type, descriptor, dice type, and scaling when the donor spell already has the right shape.
- `SpellModifierUtility.PatchRunActions`: patch actions inside `AbilityEffectRunAction`.
- `ContextRankConfig`: control caster-level scaling and caps. Use `ConfigureContextRankConfig`, then `SetContextRankMaximum` when a cap is required.
- `ContextActionConditionalSaved`: add failed-save riders when the donor spell already has a saving throw context.
- `ContextActionApplyBuff`: apply custom condition/debuff buffs. Use `SetApplyBuffActionBuff` instead of assigning private refs directly.
- `AddCondition`: implement condition buffs such as `Dazed`, `Dazzled`, `Nauseated`, `CantMove`.
- `AddFactContextActions`: implement round-by-round buff behavior such as corrosion damage.
- When copying modifier patterns, also copy/import every namespace for the Kingmaker types used. Example: `ContextRankConfig` needs `using Kingmaker.UnitLogic.Mechanics.Components;`.

For area-effect spells, clone or create a `BlueprintAbilityAreaEffect`, configure it separately, then connect it with `SetSpawnAreaEffect`.

## Range, Radius, And Delivery

Match the donor blueprint to the desired delivery:

- Fireball-style burst: use Fireball as the donor and patch `AbilityTargetsAround`.
- Line/cone/projectile delivery: use a donor that already has the appropriate delivery component.
- Persistent area: use a donor with `ContextActionSpawnAreaEffect`.

Range is usually public on `BlueprintAbility`:

- `spell.Range = AbilityRange.Custom`
- `spell.CustomRange = 90.Feet()`
- `spell.Range = AbilityRange.Long` when normal long range is enough.

Radius on `AbilityTargetsAround` currently uses private `m_Radius`; use the existing `SpellModifierUtility.SetPrivateField(targetAround, "m_Radius", feet.Feet())` pattern until a helper exists.

## Visuals

Visual setup lives in `SpellInstaller.ConfigureSpellVisuals`.

- Retint ability spawn FX through `SpellEffectTintRegistry.RegisterAbilitySpawnFxTint`.
- Retint projectile clones through `RegisterProjectileTint`.
- Use `GetMissileVisuals` for most projectile clone/tint mappings.
- Use a special visual branch only when a spell needs extra behavior, such as Vitriolic Burst retinting caster-appear projectile FX or Thunderhead retinting an area effect.

Projectile GUIDs should be in `ModBlueprintIds.Projectiles`; do not reuse donor projectile GUIDs for custom clones.

## Resistance Notes

Do not confuse these:

- `BlueprintAbility.SpellResistance` controls spell resistance checks.
- Energy resistance and damage reduction are handled during damage calculation.

For partial energy-resistance bypass, current experimental pattern is a custom `ContextActionDealDamage` subclass that temporarily calls `UnitPartDamageReduction.AddPenaltyEntry(10, null)` around `base.RunAction()`, then removes the penalty in `finally`. This is targeted but should be treated as risky until verified in-game, because it relies on how WotR applies energy resistance through `UnitPartDamageReduction`.

If this pattern fails, update this note and replace it with the verified approach.

## Verification

Before calling a spell done:

- Check `git diff --check`.
- Confirm the GUID constants are present and stable.
- Confirm localization keys match the registry key stem.
- Confirm new `.cs` files are included in `wotr_mod/wotr_mod.csproj`.
- Confirm the spell appears in the intended class spell lists.
- In game, verify damage dice, cap, radius, range, save behavior, spell resistance behavior, energy resistance behavior, and visuals.

Per repo instructions, do not run a verification build by default. Ask the user to run:

```powershell
dotnet build .\wotr_mod.sln
```
