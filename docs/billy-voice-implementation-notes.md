# Billy Voice Implementation Notes

These notes track the Billy voice pipeline used by the mod.

## Current Billy Dialogue Surface

- Main dialogue is built in `wotr_mod/Content/CompanionInstaller.cs`.
- Billy's conversation clones Ciar zombie `BlueprintDialog`, `BlueprintCue`, `BlueprintAnswer`, and `BlueprintAnswersList` assets, then replaces localized text and speaker wiring.
- Billy's reactivity bark scaffolding remains, but the old line set was intentionally cleared:
  - `wotr_mod/Content/BillyReactivityLines.cs` keeps the category/action surface with no current text entries.
  - `docs/billy_barks.json` keeps an empty `billyReactivityLines` object for the next story-accurate line pass.
  - `wotr_mod/Patches/BillyReactivityBarkAction.cs` calls `BarkManager.ShowBark(..., new VoiceOverStatus())`.
- Billy's normal unit barks use `UnitAsksComponent` in `CompanionInstaller.EnsureBillyBarks`.
  - `UnitAsksComponent.SoundBanks` is currently empty.
  - Each `BarkEntry.AkEvent` is currently `string.Empty`.

## Cleanest Likely Path

Start with Billy's `UnitAsksComponent` barks before full dialogue VO. Bark entries already expose `AkEvent`, which is the most direct local hook found so far. The least invasive path is:

1. Add a small Billy voice manifest that maps each bark line key or logical line id to a Wwise event name.
2. Populate `UnitAsksComponent.SoundBanks` with Billy's sound bank names.
3. Set `BarkEntry.AkEvent` from the manifest when building Billy barks.
4. Keep subtitle text unchanged so missing audio falls back to silent subtitles.

For full dialogue cues, do not assume `BlueprintCue` has a direct VO field. The extracted cue JSON inspected so far only showed text, speaker, actions, answers, and continue fields. Before coding, inspect Owlcat dialogue/VO examples and runtime types to find the real voice hook. If there is no stable cue VO field, use a cue `OnShow` action or a small component/action to trigger Billy voice playback by cue id.

## Confirmed Voice Asset Type

For Billy's unit barks, the runtime asset should be a generated Wwise sound bank: a `.bnk` file, not a loose `.wav`, `.ogg`, or Unity `AudioClip`.

Local examples:

- `WOTR_Blueprints/Sound/Barks/Companions/ZombieCiar/CMP_ZombieCiar_Barks.jbp`
  - `SoundBanks = ["CMP_ZombieCiar_GVR_ENG"]`
  - `AkEvent = "ZombieCiar_CombatStart_01"` and similar event names per bark entry.
- Installed runtime file:
  - `Wrath_Data/StreamingAssets/Audio/GeneratedSoundBanks/Windows/CMP_ZombieCiar_GVR_ENG.bnk`
- `SoundbanksInfo.xml` maps that bank to Wwise events and generated `.wem` media paths. The source paths shown there are `.wav`, but those are source assets that Wwise converts into `.wem` media and packs into generated sound output.

Use this as the Billy bark target:

- Source recording/editing format: `.wav`
- Wwise-generated media: `.wem`
- Runtime bank to ship/load: `Billy_GVR_ENG.bnk` or `CMP_Billy_GVR_ENG.bnk`
- Runtime blueprint wiring: `UnitAsksComponent.SoundBanks = ["CMP_Billy_GVR_ENG"]`, with each `BarkEntry.AkEvent` set to the matching Wwise event name.

## Regenerating Billy Audio

The TTS helper scripts live outside the mod repo:

```powershell
cd C:\Users\Paul\RiderProjects\Utility
```

Dialogue WAVs are generated from `docs\billy_dialogue.json`:

```powershell
node --env-file=.env tts-billy-dialogue.js
```

Bark WAVs are generated from `docs\billy_barks.json`:

```powershell
node --env-file=.env tts-billy-barks.js
```

Both scripts skip existing WAV files. To regenerate one line, move the existing WAV aside first:

```powershell
Move-Item .\output\<file>.wav .\output\<file>.backup.wav
```

Examples used during verification:

```powershell
Move-Item .\output\billy_dialog_greeting.wav .\output\billy_dialog_greeting.backup.wav
Move-Item .\output\billy_combat_start_04.wav .\output\billy_combat_start_04.backup.wav
```

`docs\billy_dialogue.json` and `docs\billy_barks.json` are TTS generation text. They may use phonetic spelling or punctuation to steer pronunciation and intonation, such as `Kenahbrays` for spoken `Kenabres` or `Oh! You almost scared me to death!` for a surprised read. Keep `wotr_mod/Content/Localization/ModText.cs` as the in-game display text unless the visible game text should also change.

After the WAVs are generated, open the BillyVoice Wwise project in Wwise 2019.2.15 and enable:

```text
Project -> User Preferences -> Enable Wwise Authoring API
```

Then import/update the Wwise project from the Utility folder:

```powershell
node --env-file=.env wwise-build-billy.js
```

When the script prompts for manual bank generation:

```text
Layouts -> SoundBank -> select CMP_Billy_GVR_ENG -> Shift+F7
```

The generated bank should be copied to:

```text
wotr_mod\Audio\CMP_Billy_GVR_ENG.bnk
```

Verify the Wwise-generated bank and mod bank have matching size and timestamp:

```powershell
Get-ChildItem -LiteralPath C:\Users\Paul\Documents\WwiseProjects\BillyVoice\GeneratedSoundBanks\Windows\CMP_Billy_GVR_ENG.bnk | Select-Object FullName,Length,LastWriteTime
Get-ChildItem -LiteralPath C:\Users\Paul\RiderProjects\wotr_mod\wotr_mod\Audio\CMP_Billy_GVR_ENG.bnk | Select-Object FullName,Length,LastWriteTime
```

Known issue: `wwise-build-billy.js` can detect an already-existing bank before Wwise finishes writing the fresh bank, leaving the mod copy stale. If the generated bank is newer or has a different size than the mod bank, copy it manually:

```powershell
Copy-Item -LiteralPath C:\Users\Paul\Documents\WwiseProjects\BillyVoice\GeneratedSoundBanks\Windows\CMP_Billy_GVR_ENG.bnk -Destination C:\Users\Paul\RiderProjects\wotr_mod\wotr_mod\Audio\CMP_Billy_GVR_ENG.bnk -Force
```

## Full Dialogue VO Findings

Base-game full dialogue VO uses Wwise file packages: large `.pck` files under `Wrath_Data/StreamingAssets/Audio/GeneratedSoundBanks/Windows/Packages`.

Local examples:

- `Wrath_Main_VO_Dialogues_Prologue.pck`
- `Wrath_Main_VO_Dialogues_CH1.pck` through `Wrath_Main_VO_Dialogues_CH6.pck`
- `Wrath_Main_VO_Dialogues_Other.pck`
- `Wrath_Main_VO_Dialogues_PartyBanter.pck`
- `Wrath_DLC1_VO_Dialogues.pck` through `Wrath_DLC6_VO_Dialogues.pck`

`SoundbanksInfo.xml` lists dialogue source paths like `Voices\English\Dialogs\...wav` and generated streamed media paths like `SFX\Voices\English\Dialogs\...wem`. It does not expose simple package filename matches for the dialogue `.pck` files, and its `<DialogueEvents/>` node is empty in the installed build inspected.

Runtime reflection also points to an engine-managed VO path:

- `BlueprintCue` has `PlayVoiceOver()`, but extracted cue JSON does not expose direct `AkEvent`, sound bank, or package fields.
- `AudioFilePackagesSettings` owns package/bank mappings and has `LoadPackagesChunk`, `UnloadPackagesChunk`, `LoadBanksChunk`, and `UnloadBanksChunk`.
- `BlueprintCampaign` has an `AudioChunk` field such as `MainGame`, `DLC1`, etc., which lines up with chunk-based package loading.

Conclusion: full dialogue VO's native asset path is source `.wav` -> Wwise `.wem` -> Wwise `.pck` package, plus whatever Wwise event/bank/voice lookup `BlueprintCue.PlayVoiceOver()` uses. The `.pck` package is the streamed media container, not a value directly assigned on cue JSON.

Do not assume normal Billy dialogue can be handled by only adding `.bnk` bark banks. For full dialogue VO, prototype package loading and cue voice lookup before committing to a production pipeline.

## Asset Strategy

Prefer Wwise bank/event integration if the game will resolve mod-provided banks reliably. It matches the existing `UnitAsksComponent` shape and avoids custom audio playback systems.

Use a deterministic naming convention:

- Bank: `Billy_VO`
- Dialogue events: `Play_Billy_Dialog_<CueName>`
- Bark events: `Play_Billy_Bark_<Category>_<Index>`
- Source files: `Audio/VO/Billy/en/<EventName>.wav`

For full dialogue VO, keep `.pck` as the expected native packaging target, but do not hardcode package filenames into cue data. The base game appears to resolve packages through audio chunks and Wwise metadata rather than per-cue package paths.

Keep a manifest as the source of truth, for example `Data/Audio/BillyVoiceMap.json`:

```json
{
  "barks": {
    "wotr_mod.companion.billy.bark.00": "Play_Billy_Bark_GeneralDemonEncounter_00"
  },
  "cues": {
    "BillyGreetingCue": "Play_Billy_Dialog_Greeting"
  }
}
```

The manifest should use stable localization keys or logical cue names, not raw English text, so future text edits do not break audio mapping.

## Exploratory Checks Before Coding

1. Inspect base-game companion `UnitAsksComponent` blueprints with non-empty `SoundBanks` and `AkEvent` values.
2. Inspect base-game voiced dialogue cue implementation, not just plain `BlueprintCue` JSON.
3. Verify whether mod-packaged Wwise banks can be loaded from the mod output directory.
4. Confirm whether `VoiceOverStatus` can carry an event/status for `BarkManager.ShowBark`, or whether reactivity barks need a separate audio trigger.
5. Confirm fallback behavior when a bank or event name is missing.

## Risk Notes

- Direct Unity `AudioClip` playback would be faster to prototype but would create a parallel audio path, likely ignoring game VO volume, ducking, subtitles, and localization behavior.
- Wwise banks are cleaner if loadable, but packaging and event naming need validation in-game.
- Full dialogue VO may require more work than barks because the currently cloned `BlueprintCue` shape does not expose an obvious voice field.
- Do not tie audio to mutable English line text.

## Troubleshooting

- If dialogue localization triggers an event such as `Play_CMP_Billy_Dialog_Greeting` but Wwise logs `Event ID not found`, verify that the deployed mod bank is the freshly generated bank. On 2026-05-09, the stale deployed bank was 1.79 MB and lacked dialogue events; the generated 2.72 MB bank and `SoundbanksInfo.xml` contained them.

## Recommended Implementation Order

1. Prototype one normal Billy unit bark with a non-empty `AkEvent` and Billy sound bank.
2. Add the manifest and a tiny resolver helper only after the first event plays in-game.
3. Expand to all `UnitAsksComponent` barks.
4. Add support for `BillyReactivityBarkAction`.
5. Add full dialogue cue VO once the correct cue hook is verified.
