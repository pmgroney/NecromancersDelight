using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace wotr_mod.Spells
{
    internal sealed class SpellIconLoader
    {
        private readonly string _modPath;
        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public SpellIconLoader(string modPath)
        {
            _modPath = modPath;
        }

        public Sprite Load(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(_modPath))
            {
                return null;
            }

            var fullPath = Path.Combine(_modPath, relativePath);
            if (_cache.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            if (!File.Exists(fullPath))
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            _cache[fullPath] = sprite;
            return sprite;
        }
    }
}
