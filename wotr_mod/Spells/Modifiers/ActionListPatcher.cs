using System;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace wotr_mod.Spells.Modifiers
{
    internal static class ActionListPatcher
    {
        public static int Patch(ActionList list, Func<GameAction, int> patch)
        {
            if (list?.Actions == null || patch == null)
            {
                return 0;
            }

            var changed = 0;
            foreach (var action in list.Actions)
            {
                changed += PatchRecursive(action, patch);
            }

            return changed;
        }

        private static int PatchRecursive(GameAction action, Func<GameAction, int> patch)
        {
            if (action == null)
            {
                return 0;
            }

            var changed = patch(action);

            var saved = action as ContextActionConditionalSaved;
            if (saved != null)
            {
                changed += Patch(saved.Succeed, patch);
                changed += Patch(saved.Failed, patch);
            }

            return changed;
        }
    }
}
