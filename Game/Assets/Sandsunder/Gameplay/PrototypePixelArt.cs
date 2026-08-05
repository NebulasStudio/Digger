using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    public enum PrototypePixelKind
    {
        Player = 0,
        Spitter = 1,
        DigNode = 2,
        Pickup = 3,
        Projectile = 4
    }

    /// <summary>Runtime-only readable pixel proxy. It is not a production asset.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypePixelArt : MonoBehaviour
    {
        private const int CanvasSize = 8;

        private static readonly Dictionary<PixelCacheKey, Sprite> SpriteCache = new();

        [SerializeField]
        private PrototypePixelKind kind;

        [SerializeField]
        private Color primary = Color.white;

        [SerializeField]
        private Sprite authoredSprite;

        internal static int CachedSpriteCount => SpriteCache.Count;

        public void Configure(PrototypePixelKind pixelKind, Color primaryColor)
        {
            Configure(pixelKind, primaryColor, null);
        }

        /// <summary>
        /// Supplies an authored/imported sprite while retaining the generated proxy as a fallback.
        /// This is the seam used by the sandbox builder for reviewed Higgsfield derivatives.
        /// </summary>
        public void Configure(PrototypePixelKind pixelKind, Color primaryColor, Sprite replacementSprite)
        {
            kind = pixelKind;
            primary = primaryColor;
            authoredSprite = replacementSprite;
            if (isActiveAndEnabled)
            {
                ApplySprite();
            }
        }

        private void Awake()
        {
            ApplySprite();
        }

        public static Sprite GetCachedSprite(PrototypePixelKind pixelKind, Color color)
        {
            PixelCacheKey key = new(pixelKind, (Color32)color);
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = BuildSprite(pixelKind, color);
            SpriteCache[key] = sprite;
            return sprite;
        }

        private void ApplySprite()
        {
            GetComponent<SpriteRenderer>().sprite = authoredSprite != null
                ? authoredSprite
                : GetCachedSprite(kind, primary);
        }

        private static Sprite BuildSprite(PrototypePixelKind pixelKind, Color color)
        {
            Texture2D texture = new(CanvasSize, CanvasSize, TextureFormat.RGBA32, false)
            {
                name = $"Prototype {pixelKind} Cached Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color clear = new(0f, 0f, 0f, 0f);
            Color outline = new(0.17f, 0.13f, 0.12f, 1f);
            Color highlight = Color.Lerp(color, Color.white, 0.35f);
            Color shadow = Color.Lerp(color, outline, 0.35f);
            Color[] pixels = new Color[CanvasSize * CanvasSize];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = clear;
            }

            switch (pixelKind)
            {
                case PrototypePixelKind.Player:
                    Fill(pixels, 2, 1, 5, 6, outline);
                    Fill(pixels, 3, 2, 4, 6, color);
                    Fill(pixels, 3, 5, 4, 6, highlight);
                    Set(pixels, 5, 3, highlight);
                    break;
                case PrototypePixelKind.Spitter:
                    Fill(pixels, 1, 2, 6, 5, outline);
                    Fill(pixels, 2, 3, 5, 4, color);
                    Fill(pixels, 5, 4, 6, 4, shadow);
                    Set(pixels, 5, 5, highlight);
                    break;
                case PrototypePixelKind.DigNode:
                    Fill(pixels, 1, 1, 6, 6, outline);
                    Fill(pixels, 2, 2, 5, 5, color);
                    Set(pixels, 3, 3, shadow);
                    Set(pixels, 4, 4, shadow);
                    Set(pixels, 5, 2, highlight);
                    break;
                case PrototypePixelKind.Pickup:
                    Fill(pixels, 3, 0, 4, 7, outline);
                    Fill(pixels, 0, 3, 7, 4, outline);
                    Fill(pixels, 3, 1, 4, 6, color);
                    Fill(pixels, 1, 3, 6, 4, color);
                    Set(pixels, 3, 5, highlight);
                    break;
                case PrototypePixelKind.Projectile:
                    Fill(pixels, 2, 3, 5, 4, outline);
                    Fill(pixels, 3, 3, 5, 4, color);
                    Set(pixels, 5, 4, highlight);
                    break;
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, CanvasSize, CanvasSize),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: CanvasSize,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
            sprite.name = $"Prototype {pixelKind} Cached Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static void Fill(Color[] pixels, int minimumX, int minimumY, int maximumX, int maximumY, Color color)
        {
            for (int y = minimumY; y <= maximumY; y++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                {
                    Set(pixels, x, y, color);
                }
            }
        }

        private static void Set(Color[] pixels, int x, int y, Color color)
        {
            pixels[(y * CanvasSize) + x] = color;
        }

        private readonly struct PixelCacheKey : IEquatable<PixelCacheKey>
        {
            private readonly PrototypePixelKind kind;
            private readonly Color32 color;

            public PixelCacheKey(PrototypePixelKind kind, Color32 color)
            {
                this.kind = kind;
                this.color = color;
            }

            public bool Equals(PixelCacheKey other)
            {
                return kind == other.kind
                    && color.r == other.color.r
                    && color.g == other.color.g
                    && color.b == other.color.b
                    && color.a == other.color.a;
            }

            public override bool Equals(object obj)
            {
                return obj is PixelCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)kind;
                    hash = (hash * 397) ^ color.r;
                    hash = (hash * 397) ^ color.g;
                    hash = (hash * 397) ^ color.b;
                    hash = (hash * 397) ^ color.a;
                    return hash;
                }
            }
        }
    }
}
