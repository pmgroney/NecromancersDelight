using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using UnityEngine;
using wotr_mod.Content;
using wotr_mod.Infrastructure;

namespace wotr_mod.Patches
{
    internal sealed class BillyReactivityBarkAction : GameAction
    {
        private static readonly BlueprintGuid BillyGuid = BlueprintGuid.Parse(ModBlueprintIds.Units.UndeadCiarCompanion);

        public BillyReactivityCategory Category = BillyReactivityCategory.GeneralDemonEncounter;
        public float Duration = 4f;

        public override void RunAction()
        {
            var billy = FindBilly();
            if (billy == null)
            {
                return;
            }

            var lines = BillyReactivityLines.Get(Category);
            if (lines.Count == 0)
            {
                return;
            }

            var line = lines[UnityEngine.Random.Range(0, lines.Count)];
            try
            {
                Game.Instance.UI?.BarkManager?.ShowBark(billy, line, Duration, new VoiceOverStatus());
            }
            catch (Exception ex)
            {
                Main.Warning($"Billy reactivity bark failed: {ex}");
            }
        }

        public override string GetCaption()
        {
            return $"Billy reactivity bark: {Category}";
        }

        private static UnitEntityData FindBilly()
        {
            var player = Game.Instance?.Player;
            if (player == null)
            {
                return null;
            }

            return player.Party.FirstOrDefault(IsBilly)
                   ?? player.ActiveCompanions.FirstOrDefault(IsBilly)
                   ?? player.AllCharacters.FirstOrDefault(IsBilly);
        }

        private static bool IsBilly(UnitEntityData unit)
        {
            return unit?.Descriptor?.Blueprint?.AssetGuid == BillyGuid;
        }
    }
}
