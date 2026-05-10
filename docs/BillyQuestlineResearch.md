# Billy Companion Questline Research

## Verified IDs

- Prologue Labyrinth / Shield Maze area: `944a6947fe8ffa8458c278aa1c0c4226`
- Hosilla unit: `64dcc27d70edc1148b31257fcc2241ce`
- From the Deep quest: `59ca88537b2839545aa5eba63e05fc79`
- Slay Hosilla objective: `1992ddc7d1b615748a0652a964846d4c`
- PlayerIsLich etude: `11fc5662e0ce8074ea145a022282b879`
- PlayerIsLich MythicInfo: `2c5990ad4a017cd4f9dbc99c09d4af47`
- LichProgression: `ccec4e01b85bf5d46a3c3717471ba639`
- Lich mythic class referenced by LichProgression: `5d501618a28bdc24c80007a5c937dcb7`

Sources inspected:
- `WOTR_Blueprints\Units\NPC\Unique\Act_0_Prologue\Prologue_Labyrinth\Hosilla.jbp`
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

- `BillyQuestStarter` creates a runtime `BlueprintQuest` in `CompanionQuests` and a single investigation `BlueprintQuestObjective`.
- The bow trigger (`HandleItemsAdded` / `OnAreaLoaded`) starts the objective through `Player.QuestBook.GiveObjective` when the bow is in party inventory and Billy is available.
- The `BillyBowQuestStarted` unlockable flag still prevents the starter dialogue from repeating; if the flag is already set, the objective-start safety still runs for older saves.

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
