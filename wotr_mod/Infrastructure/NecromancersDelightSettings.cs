using UnityModManagerNet;

namespace wotr_mod.Infrastructure
{
    public sealed class NecromancersDelightSettings : UnityModManager.ModSettings
    {
        public bool EnableAchievementsWhileModded = true;
        public int TooltipIconMagnificationMode = 1;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
