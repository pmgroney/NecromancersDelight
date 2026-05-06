using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using UnityModManagerNet;
using wotr_mod.Infrastructure;

namespace wotr_mod.Content
{
    internal sealed class BillyQuestStarter : IContentModule, IAreaLoadModule, IItemsCollectionHandler
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);
        private static readonly BlueprintGuid BowGuid = BlueprintGuid.Parse(ModBlueprintIds.Items.NeophytesLongbowOfDiscipline);

        private readonly BlueprintTool _blueprints;
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        private IDisposable _subscription;

        public BillyQuestStarter(BlueprintTool blueprints, UnityModManager.ModEntry.ModLogger logger)
        {
            _blueprints = blueprints;
            _logger = logger;
        }

        public string Name => "Billy Quest Starter";

        public void RegisterLocalization()
        {
        }

        public void Install()
        {
            EnsureStartedFlag();
            if (_subscription == null)
            {
                _subscription = EventBus.Subscribe(this);
            }
        }

        public void OnAreaLoaded()
        {
            TryStartBowQuest();
        }

        public void HandleItemsAdded(ItemsCollection collection, ItemEntity item, int count)
        {
            if (collection != Game.Instance?.Player?.Inventory || item?.Blueprint?.AssetGuid != BowGuid)
            {
                return;
            }

            TryStartBowQuest();
        }

        public void HandleItemsRemoved(ItemsCollection collection, ItemEntity item, int count)
        {
        }

        private BlueprintUnlockableFlag EnsureStartedFlag()
        {
            var flag = _blueprints.Get<BlueprintUnlockableFlag>(ModBlueprintIds.Flags.BillyBowQuestStarted);
            if (flag != null)
            {
                return flag;
            }

            flag = new BlueprintUnlockableFlag
            {
                name = "WotrMod_BillyBowQuestStarted",
                AssetGuid = BlueprintGuid.Parse(ModBlueprintIds.Flags.BillyBowQuestStarted)
            };
            _blueprints.AddCachedBlueprint(ModBlueprintIds.Flags.BillyBowQuestStarted, flag);
            return flag;
        }

        private void TryStartBowQuest()
        {
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                if (player?.Inventory == null || !PlayerHasBow() || game.IsModeActive(GameModeType.Dialog))
                {
                    return;
                }

                var flag = EnsureStartedFlag();
                if (player.UnlockableFlags.IsUnlocked(flag))
                {
                    return;
                }

                var billy = FindBilly();
                var initiator = player.MainCharacter.Value ?? player.GetMainPartyUnit();
                var dialog = _blueprints.Get<BlueprintDialog>(ModBlueprintIds.Dialogs.BillyBowQuestDialog);
                if (billy == null || initiator == null || dialog == null)
                {
                    return;
                }

                player.UnlockableFlags.Unlock(flag);
                game.DialogController.StartDialogWithUnit(dialog, billy, initiator);
                _logger.Log("Started Billy bow quest dialog.");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Billy bow quest dialog failed to start: {ex}");
            }
        }

        private static bool PlayerHasBow()
        {
            return Game.Instance?.Player?.Inventory?.Items
                ?.Any(item => item?.Blueprint?.AssetGuid == BowGuid) == true;
        }

        private static UnitEntityData FindBilly()
        {
            var player = Game.Instance?.Player;
            return player?.Party.FirstOrDefault(IsBilly)
                   ?? player?.ActiveCompanions.FirstOrDefault(IsBilly)
                   ?? player?.AllCharacters.FirstOrDefault(IsBilly)
                   ?? Game.Instance?.State?.LoadedAreaState?.AllEntityData
                       ?.OfType<UnitEntityData>()
                       ?.FirstOrDefault(IsBilly);
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint != null && unit.Descriptor.Blueprint.AssetGuid == BillyGuid;
        }
    }
}
