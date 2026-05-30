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
   - Evoker-owned spell clones, including Evoker-cloned spawned area effects, clear `ContextRankConfig` maximums for damaging Evocation spells; keep this rule isolated to the Evoker spell list and preserve rank-driven projectile delivery.
   - Reusable `PerDieBonusDamage` covers Evoker class-spellbook evocation per-die bonuses, explicit ability allowlists for granted rays, and force-damage matching for Arcwright Force Ray.
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
- Evoker elemental/Umbral rays and Necromancer Withering Ray use class-level `OnePlusDivStep(start 1, step 2)` for d6 dice count with zero flat bonus, avoiding `Div2` floor scaling at level 3.
- Magic Missile-style custom missiles use `OnePlusDiv2`; non-Evokers retain a maximum of 5 missiles, while Evoker clones remove the rank-based cap but preserve rank-driven projectile delivery.
- When swapping projectile visuals, preserve or restore the donor projectile slot count for all spells, not only Magic Missile; rank-driven deliveries such as Magic Missile, Scorching Ray, and Hellfire Ray require repeated projectile refs up to their max rank, and Evoker uncapped clones must expand slots after clearing the rank maximum.
- Elemental ray legacy placeholders have been removed after save migration/respec; necromancer legacy placeholders must remain until tested.
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
- Base-game lightning/electricity FX candidates include `LightningBolt00` (`c7734162c01abdc478418bfb286ed7a5`; Lightning Bolt, Chain Lightning, blue dragon breath), `ElectroCommonProjectile00` (`1af8385214ca8774b98922b56caa0e92`; Jolt, Air domain, simple electricity rays), `LightningBolt00_Miss` (`23cffcf4535a9654895fc7815aa0442d`; Air bloodline electric Scorching Ray/ray), and `ArcanistDancingElectricity00_Projectile` (`71af6bc04a9a8794c9b6f8439649bb6c`; present under FX but not directly referenced by ability blueprints in the checked search).
- Persistent electric/lightning AOEs cloned from Sirocco should not keep `SiroccoArea` Fx (`AssetId 9f9ebe136ce5a9345b5b016f011c5aa6`). Use `CloudThunderstormBlastArea` (`BlueprintAbilityAreaEffect` `3659ce23ae102ca47a7bf3a30dd98609`) as the shape-matched donor for persistent cylinder storm area Fx (`AssetId ea0829270d8996146be0b8e39c6ec472`). `CallLightning`, `CallLightningStorm`, and `Stormbolts` are one-shot strike/cast FX references, not persistent area-effect Fx donors.
- Custom lightning spells should retain stable mod GUIDs while cloning or tinting shape-matched electric visuals: `WotrMod_EmperorsWrath` and `WotrMod_ElectricHellfireRay` clone from `LightningBolt00_Miss` (`23cffcf4535a9654895fc7815aa0442d`), while area storm spells (`Thunderhead`, `CataclysmicStorm`, `HeavensWrath`) use electric ability/area tinting plus the persistent thunderstorm area Fx above.
- Base-game cold FX candidates include `RayOfFrost00` (`d6c9daec1256561408a7a72a6979359e`; Ray of Frost and base-game elemental water `ScorchingRayCold`), `PolarRay00` (`68ce28c9ac213e7458670a72da007dd8`; Polar Ray and elemental water ray), `SnowBall00` (`81a8bff536bae184bacb3a58f0bc381a`; simple cold projectile), and `ColdCone50Feet00` (`79a66a3766ae87146beb6000a73e8213`; Cone of Cold and silver/white dragon breath).
- Custom cold spells should retain stable mod GUIDs while cloning shape-matched cold visuals: `WotrMod_FrostBlast` clones from `RayOfFrost00`, `WotrMod_ColdHellfireRay` clones from `PolarRay00`, `WotrMod_FrozenLance` clones from `SnowBall00`, and cone cold spells cloned from `ConeOfCold` can keep `ColdCone50Feet00`.
- Persistent cold AOEs cloned from Sirocco should not keep `SiroccoArea` Fx (`AssetId 9f9ebe136ce5a9345b5b016f011c5aa6`). `WotrMod_GlacialPrison` uses `IceStormArea` as the shape-matched persistent 40-foot cold cylinder Fx (`AssetId 4a5d52b2e20e1e449a7a79bf3882dc06`).
- Base-game acid/earth FX candidates include `AcidCommonProjectile00` (`d8abd128c02331a45a4f250a62722e8b`; Acid Splash), `AcidArrow00` (`89cd363b66b1df440b5281f7d3ef188d`; Acid Arrow and base-game elemental earth `ScorchingRayAcid`), `AcidLine00` (`33af0c7694f8d734397bd03e6d4b72f1`; Acidic Spray and earth elemental ray), `AcidCone50Feet00` (`214036a0c1b35464780ad140324c249c`; acid cone trap/cutscene spells), and `AlchemistAcidBomb00` (`b33865d0fbc186946a485fbd549f74ec`; acid impact projectile).
- Custom acid/earth spells should retain stable mod GUIDs while cloning shape-matched acid visuals: `WotrMod_CausticBeam` clones from `AcidArrow00`, `WotrMod_CorrosiveCascade`, `WotrMod_CausticOblivion`, and `WotrMod_AcidHellfireRay` clone from `AcidLine00`, and `WotrMod_DissolutionWave` clones from `AcidCone50Feet00`.
- Elemental Magic Missile variants (`WotrMod_AcidMissile`, `WotrMod_ElectricMissile`, `WotrMod_IceMissile`) should clone the source `Magic Missile` projectiles and apply tints through `SpellEffectTintRegistry` / `RegisterProjectileTint`. Do not use native acid/electric/cold donor projectiles for these spells; single-projectile donor FX can collapse the separate missiles or render oversized.
- No verified spell-level acid fireball projectile was found in the checked base set; fireball-style acid spells (`WotrMod_VitriolicBurst`, `WotrMod_VitriolicSphere`, `WotrMod_VitriolicApocalypse`) use `AlchemistAcidBomb00` as the closest base-game acid impact projectile donor.

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
