using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace wotr_mod.Infrastructure
{
    internal static class SpellEffectRuntimeTintRegistry
    {
        private static readonly Dictionary<UnitEntityData, List<Entry>> ActiveThemes =
            new Dictionary<UnitEntityData, List<Entry>>();

        public static void Register(UnitEntityData caster, object source, SpellEffectTheme theme)
        {
            if (caster == null || source == null)
            {
                return;
            }

            if (!ActiveThemes.TryGetValue(caster, out var entries))
            {
                entries = new List<Entry>();
                ActiveThemes[caster] = entries;
            }

            Remove(entries, source);
            entries.Add(new Entry(source, theme));
        }

        public static void Unregister(UnitEntityData caster, object source)
        {
            if (caster == null || source == null)
            {
                return;
            }

            if (!ActiveThemes.TryGetValue(caster, out var entries))
            {
                return;
            }

            Remove(entries, source);
            if (entries.Count == 0)
            {
                ActiveThemes.Remove(caster);
            }
        }

        public static bool TryGetActiveTheme(UnitEntityData caster, out SpellEffectTheme theme)
        {
            theme = default(SpellEffectTheme);
            if (caster == null ||
                !ActiveThemes.TryGetValue(caster, out var themes) ||
                themes.Count == 0)
            {
                return false;
            }

            theme = themes[themes.Count - 1].Theme;
            return true;
        }

        private static void Remove(List<Entry> entries, object source)
        {
            for (var index = entries.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(entries[index].Source, source))
                {
                    entries.RemoveAt(index);
                }
            }
        }

        private sealed class Entry
        {
            public Entry(object source, SpellEffectTheme theme)
            {
                Source = source;
                Theme = theme;
            }

            public object Source { get; }
            public SpellEffectTheme Theme { get; }
        }
    }
}
