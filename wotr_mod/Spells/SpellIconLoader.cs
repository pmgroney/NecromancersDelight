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

        public Sprite Tint(Sprite source, string cacheKey, Color tint, float strength)
        {
            if (source == null)
            {
                return null;
            }

            var key = "tint:" + cacheKey + ":" + source.GetInstanceID();
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                var sourceTexture = source.texture;
                var rect = source.textureRect;
                var width = Mathf.RoundToInt(rect.width);
                var height = Mathf.RoundToInt(rect.height);
                var pixels = ReadPixels(sourceTexture, rect, width, height);

                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    var alpha = pixel.a;
                    var luminance = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
                    var tinted = new Color(
                        luminance * tint.r,
                        luminance * tint.g,
                        luminance * tint.b,
                        alpha);
                    pixels[i] = Color.Lerp(pixel, tinted, strength);
                    pixels[i].a = alpha;
                }

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = sourceTexture.filterMode
                };
                texture.SetPixels(pixels);
                texture.Apply();

                var pivot = new Vector2(source.pivot.x / rect.width, source.pivot.y / rect.height);
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, width, height),
                    pivot,
                    source.pixelsPerUnit);
                _cache[key] = sprite;
                return sprite;
            }
            catch
            {
                return source;
            }
        }

        private static Color[] ReadPixels(Texture2D sourceTexture, Rect rect, int width, int height)
        {
            try
            {
                return sourceTexture.GetPixels(
                    Mathf.RoundToInt(rect.x),
                    Mathf.RoundToInt(rect.y),
                    width,
                    height);
            }
            catch
            {
                var previous = RenderTexture.active;
                var renderTexture = RenderTexture.GetTemporary(
                    sourceTexture.width,
                    sourceTexture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);
                try
                {
                    Graphics.Blit(sourceTexture, renderTexture);
                    RenderTexture.active = renderTexture;

                    var readable = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                    readable.ReadPixels(rect, 0, 0);
                    readable.Apply();
                    return readable.GetPixels();
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
        }
    }
}
