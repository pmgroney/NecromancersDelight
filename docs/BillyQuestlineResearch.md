# Billy Companion Questline Research

## Verified IDs

- Prologue Labyrinth / Shield Maze area: `944a6947fe8ffa8458c278aa1c0c4226`
- Defender's Heart area: `089e897983fef564d9e15b46ff679d7e`
- Kenabres Burning / Market Square area: `92180b58582ec5f43a756071cd339f52`
- Market Square SpecialThieflingStash loot: `4b7d47f312f186646a05beff7f501c3b`
- CultistsLair area: `a3bfeef875b14484e9c32335b3d53f2c`
- CultistsLair Luxery_caseket static loot: `de82c5ff1d3879e468c02fc86709be5d`
- ZachariusNecromancy note item donor: `de12840a4662481f937ff9542a6beb6b`
- Hosilla unit: `64dcc27d70edc1148b31257fcc2241ce`
- From the Deep quest: `59ca88537b2839545aa5eba63e05fc79`
- Slay Hosilla objective: `1992ddc7d1b615748a0652a964846d4c`
- PlayerIsLich etude: `11fc5662e0ce8074ea145a022282b879`
- PlayerIsLich MythicInfo: `2c5990ad4a017cd4f9dbc99c09d4af47`
- LichProgression: `ccec4e01b85bf5d46a3c3717471ba639`
- Lich mythic class referenced by LichProgression: `5d501618a28bdc24c80007a5c937dcb7`

Sources inspected:
- `WOTR_Blueprints\Units\NPC\Unique\Act_0_Prologue\Prologue_Labyrinth\Hosilla.jbp`
- `WOTR_Blueprints\World\Areas\Act_1_WorldwoundIncursion\DefendersHeart\DefendersHeart.jbp`
- `WOTR_Blueprints\World\Areas\Act_1_WorldwoundIncursion\KenabresBurning\KenabresBurning.jbp`
- `WOTR_Blueprints\Loot\Quest\KenabresBurning\SpecialThieflingStash.jbp`
- `WOTR_Blueprints\World\Areas\Act_1_WorldwoundIncursion\CultistsLair\CultistsLair.jbp`
- `WOTR_Blueprints\Loot\Cooking\CultistsLair\Luxery_caseket.jbp`
- `WOTR_Blueprints\Items\Books\UniqueBooks\ZachariusNecromancy.jbp`
- `WOTR_Blueprints\World\Quests\c0\FromTheDeep\Obj4_SlayHosilla.jbp`
- `WOTR_Blueprints\World\Etudes\Common\WrathOfTheRighteous\MythicLich\PlayerIsLich.jbp`
- `WOTR_Blueprints\Root\Dialog\MythicInfo\PlayerIsLich.jbp`
- `WOTR_Blueprints\Mythic\Lich\LichProgression.jbp`

## Existing Mod Patterns

- `CustomItemInstaller` already supports custom item placement.
- Chest placement patches a static `BlueprintLoot` during install.
- Unit and map-object placement run during `OnAreaLoaded`.
- Unit placement finds a loaded `UnitEntityData` by unit blueprint GUID and adds the item to `unit.Inventory`.
- `ItemPlacementDefinition.OnUnit` requires a `unitLootGuid`, but the current installer does not use that value.
- Hosilla has empty `m_StartingInventory` and no static `AddLoot` component in her unit blueprint.
- The existing Shield Maze custom item uses map-object loot for the weapon rack, not unit loot.

## Quest And Item Trigger Patterns

- Quest shape is `BlueprintQuest` plus one or more `BlueprintQuestObjective` blueprints.
- Companion quests use `m_Group = CompanionQuests`.
- Objectives point back to their parent quest through `m_Quest`.
- Objective flow uses `m_NextObjectives`, `m_FinishParent`, hidden objectives, and action components such as `ObjectiveStatusTrigger`.
- Existing companion quests often call `UnlockCompanionStory` when an objective completes.
- WotR item-trigger examples use:
  - `PartyInventoryTrigger` with `OnAddActions`
  - `AddItemShowInfoCallback`
  - `GiveObjective`
  - `SetObjectiveStatus`
- `HilorLetterChapter4.jbp` is a useful item-trigger example because it starts or advances objectives when the item enters inventory or is shown.
- `BlueprintItemNote` examples, such as `ZachariusNecromancy.jbp`, are useful if the starter should behave like a readable note.

## Recommended First Implementation Shape

- Create a Billy companion quest as a `BlueprintQuest` in `CompanionQuests`.
- Create a starter objective that begins when the quest item is picked up or viewed.
- Make the starter item a simple note or quest item rather than a mechanically active item.
- Put the item on Hosilla by reusing the existing `OnAreaLoaded` unit-inventory placement pattern.
- Add a backfill/debug path from the start, because Hosilla is a one-time early trigger.

The Hosilla placement should use:
- Area gate: Prologue Labyrinth `944a6947fe8ffa8458c278aa1c0c4226`
- Unit target: Hosilla `64dcc27d70edc1148b31257fcc2241ce`

## Current Implementation

- `BillyQuestStarter` creates a runtime `BlueprintQuest` in `CompanionQuests` with an initial investigation objective and an Act 1 Jalmeray lead objective.
- The bow trigger (`HandleItemsAdded` / `OnAreaLoaded`) starts the objective through `Player.QuestBook.GiveObjective` when the bow is in party inventory and Billy is available.
- The `BillyBowQuestStarted` unlockable flag still prevents the starter dialogue from repeating; if the flag is already set, the objective-start safety still runs for older saves.
- Act 1 Defender's Heart stage advances when the player selects Billy's Jalmeray-origin dialogue answer, not on area load; the new Jalmeray dialogue and audio keys are implemented.
- The Jalmeray response uses a dedicated Continue answer to exit cleanly after the quest update.
- Billy quest-stage dialogue cues, including Jalmeray, Scorched Pilgrimage Record, and Irori Neophyte's Armor pickup/dialogue phases, exit through the reusable Continue answer rather than returning to normal hub options.
- Shield Maze Billy is represented by a fresh pre-recruit runtime stand-in.
- Defender's Heart uses the recruited roster Billy itself, as local-map party portrait pins require an in-game roster or party unit; a runtime stand-in can appear in-world but will not generate the portrait pin.
- Defender's Heart Billy placement uses fixed hub coordinates instead of party-relative placement, ensuring a stable room and map position across all entry points.
- Final static coordinate for Billy in the Defender's Heart center room is `(-82, 40, -7)` with orientation `0`.
- Prior coordinates `(-84, 40, 4)` and `(-101, 40, -11)` were rejected for placing the map pin too far south near/below the center-room lower edge and inside the top-right bedroom, respectively; `Y=40` remained valid throughout.
- Billy Defender's Heart placement checks `Player.AllCharacters` as well as active/remote/party collections so roster-only recruitment still satisfies the stand-in placement gate.
- `Player.AllCharacters` can include runtime Billy stand-ins; treat a Billy unit in that list as recruited roster only when `UnitPartCompanion.State` is `Remote`, `InParty`, `InPartyDetached`, or `ExCompanion`.
- `AddUnitToPersistentState` stores runtime stand-ins cross-scene, so Shield Maze duplicate prevention must check cross-scene non-roster Billy; Defender's Heart duplicate prevention checks only loaded-area non-roster Billy so stale Shield Maze stand-ins do not block hub placement.
- After recruitment, Billy's click dialogue uses a hub greeting and hides the recruitment answer; his Defender's Heart hub placement is static rather than party-relative.
- Billy copies Ciar's companion shell, which has `ClassLevelLimit = 10`; Hilor's respec action only lists units where `CharacterLevel > ClassLevelLimit`, so Billy must override this limit to `1` like early companions or he will be hidden from the respec picker.
- Billy uses the Priest of Balance archetype, which grants both positive and negative channeling; do not add custom channel feature/ability clones or select the base Channel Energy feature.
- Act 1 Stage 2 adds `Scorched Pilgrimage Record`, cloned from `ZachariusNecromancy` with donor callbacks/components cleared so it behaves as a plain readable note.
- The record is added to static loot `SpecialThieflingStash` in Kenabres Burning / Market Square, avoiding runtime map-object reseeding.
- When the record enters party inventory through loot, `BillyQuestStarter` completes the Jalmeray lead objective, starts `Trace the second transfer`, and opens the matching Billy dialogue; area-load/backfill checks may advance objectives but must not auto-open dialogue.
- Act 1 reward stage places Irori Neophyte's Armor, a +1 breastplate with the retained bow attack/damage bonus, in CultistsLair static loot `Luxery_caseket` as the cult cache reward.
- Picking up or equipping Irori Neophyte's Armor through loot completes `Trace the second transfer`, starts `Wait for another lead`, and opens the matching Billy dialogue; future Billy quest item pickups should follow this immediate-dialogue pattern.
- Billy Act 1 quest rewards use `QuestRewardInstaller` / `QuestExperienceReward`, attaching `Experience` components by stable reward ID so repeated runtime blueprint setup does not stack duplicate rewards.
- Assigned Billy Act 1 rewards: Investigate completion grants `ChallengeMinor` CR 3, Jalmeray lead completion grants `ChallengeMinor` CR 4, and Transfer Record completion grants `QuestNormal` CR 4; Trail Cold remains unrewarded because it stays open.
- Billy's Condition uses `m_LastChapter = 5`, leaving the unresolved mystery open for later acts.

## Act 1 Narrative Plan: The Last Shot Before Silence

- Billy is from a temple in the Jalmeray area, far from Kenabres and the Shield Maze.
- The bow looted from Hosilla is the prologue starter. Billy recognizes it as his, but its presence under Kenabres should not be possible from what he remembers.
- The Act 1 phase should reveal clues, not solve the full mystery of how Billy became undead in a labyrinth on the other side of the continent.
- Early Act 1 trigger: a refugee, crusader scholar, or Iroran traveler recognizes one of Billy's training phrases, such as "three breaths, one shot," as a Jalmeray-area monastic archery drill.
- Investigation clue: a Kenabres lead shows Billy's traveling group vanished before reaching official crusader channels. The record should imply capture or transfer below the city, not ordinary battlefield death.
- Reward stage: a cult cache contains Irori Neophyte's Armor as confiscated property or experimental material. The cache should include a partial note implying Billy was intentionally preserved or tested to see whether discipline, memory, and obedience survived undeath.
- Act 1 ending beat: Billy recovers part of himself and gains proof that someone brought him to Kenabres and kept notes, but the responsible party and full method remain unknown.

## Voice And Text Follow-Up

- New Act 1 quest lines will need generated voice before release.
- Existing Billy starter lines may need continuity edits to support the Jalmeray origin and wider undead-origin mystery.
- Any edited existing lines will need regenerated voice to match the revised text.
- First implementation pass should stub the Act 1 information flow; after that, run a separate text and voice audit before generating new audio.

## Lich Branch Strategy

- For dialog-only branch visibility, WotR already uses `MythicRequirement = PlayerIsLich`.
- For scripted branching, use `EtudeStatus` against PlayerIsLich etude `11fc5662e0ce8074ea145a022282b879`.
- If a progression/fact check is needed later, `LichProgression` is `ccec4e01b85bf5d46a3c3717471ba639`.
- Do not build the early quest around Lich state, because the branch is several stages later and the player will not be Lich during the Shield Maze.

## Save Compatibility And Testing

Adding later quest stages should usually work in existing saves if:
- Blueprint GUIDs remain stable.
- Existing objectives are not renamed or replaced.
- New objectives are added as new GUIDs.
- Progression to the new content is driven by a dialog/action/backfill check that can still run in that save.

The important limit: the Hosilla starter trigger is not automatically save-compatible after the trigger point.

Likely cases:
- Save before entering Shield Maze: normal Hosilla item placement should work.
- Save in Shield Maze before Hosilla is loaded/dead: likely works if `OnAreaLoaded` runs before the fight or after a reload.
- Save after Hosilla is dead but before loot is opened: uncertain; depends on whether the runtime corpse/loot inventory still accepts the injected item.
- Save after Hosilla loot is generated or collected: the Hosilla-only starter will not reliably appear.
- Save after leaving Shield Maze: the Hosilla-only starter will not appear unless a backfill path grants it.

Testing recommendation:
- Add a temporary debug/backfill route before relying on the questline in a real save.
- Good options are: grant the starter item if Billy is recruited and the item/objective is missing, add a temporary Billy dialog answer that starts the objective, or add a temporary local debug action.
- This lets new stages be tested without restarting before Billy's zone.

## Open Questions And Risks

- Confirm whether adding an item to a loaded boss inventory before death reliably appears in corpse loot.
- Decide whether the starter should trigger on item pickup, item view, talking to Billy after pickup, or some combination.
- Decide whether the quest should be visible immediately on pickup or hidden until Billy comments on the item.
- Decide whether Billy must be recruited before the item can start the quest.
- Decide whether the first quest entry should be a full companion quest or an errand that promotes into a companion quest later.
- The current `unitLootGuid` field is unused; either remove it later or implement true unit loot blueprint support if needed.
