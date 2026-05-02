using Kingmaker.EntitySystem.Entities;

namespace wotr_mod.Infrastructure
{
    internal interface IGamePatch
    {
        string Name { get; }
        void RegisterLocalization();
        void Apply();
    }

    internal interface IUnitLoadHandler
    {
        void OnUnitLoaded(UnitEntityData unit);
    }
}
