# Mod Design Notes

This WotR mod should be built as one cohesive mod, not as a collection of separately branded ports.

## Core Direction

- Follow SOLID design principles and C# best practices.
- Keep systems modular so new classes, archetypes, feats, spells, bloodlines, companions, and fixes can be added without rewriting existing installers.
- Favor reusable infrastructure for recurring blueprint tasks, localization, spell lists, class setup, selections, and patches.
- Keep content organized by domain and responsibility, not by the Kingmaker mod it originally came from.

## Porting Guidance

- The Kingmaker Evocation Plus project is a reference for content ideas and behavior.
- Do not preserve `EvocationPlus` naming in WotR source files, type names, localization keys, or logs unless the user explicitly requests it.
- Adapt mechanics to WotR intentionally. If a Kingmaker implementation depends on APIs, blueprints, or assumptions that differ in WotR, pause and ask before inventing a substitute.

## Open Questions Policy

When unsure, ask before proceeding. This is especially important for:

- Blueprint GUID choices.
- Player-facing names and descriptions.
- Class, archetype, feat, spell, or progression design.
- Mechanical changes that differ from tabletop, Kingmaker, or existing WotR behavior.
