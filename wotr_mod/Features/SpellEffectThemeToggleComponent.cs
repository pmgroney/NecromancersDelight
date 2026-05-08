using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using wotr_mod.Infrastructure;

namespace wotr_mod.Features
{
    public sealed class SpellEffectThemeToggleComponent : UnitFactComponentDelegate
    {
        public SpellEffectTheme Theme;

        private UnitEntityData _registeredOwner;
        private object _registeredSource;

        protected override void OnActivate()
        {
            RegisterTheme();
        }

        protected override void OnDeactivate()
        {
            UnregisterTheme();
        }

        protected override void OnTurnOn()
        {
            RegisterTheme();
        }

        protected override void OnTurnOff()
        {
            UnregisterTheme();
        }

        private void RegisterTheme()
        {
            var owner = Owner;
            var source = SourceKey;
            if (owner == null || source == null)
            {
                return;
            }

            if (_registeredOwner != null &&
                (!ReferenceEquals(_registeredOwner, owner) || !ReferenceEquals(_registeredSource, source)))
            {
                SpellEffectRuntimeTintRegistry.Unregister(_registeredOwner, _registeredSource);
            }

            SpellEffectRuntimeTintRegistry.Register(owner, source, Theme);
            _registeredOwner = owner;
            _registeredSource = source;
        }

        private void UnregisterTheme()
        {
            SpellEffectRuntimeTintRegistry.Unregister(
                _registeredOwner ?? Owner,
                _registeredSource ?? SourceKey);
            _registeredOwner = null;
            _registeredSource = null;
        }

        private object SourceKey => Fact ?? (object)this;
    }
}
