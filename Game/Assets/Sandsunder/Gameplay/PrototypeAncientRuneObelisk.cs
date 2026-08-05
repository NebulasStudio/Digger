using UnityEngine;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypeAncientRuneObelisk : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float pulseTimer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateObeliskSprite();
                spriteRenderer.sortingOrder = 6;
            }

            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);
        }

        private void Update()
        {
            pulseTimer += Time.deltaTime * 2.5f;
            float glow = 0.7f + (Mathf.Sin(pulseTimer) * 0.3f);
            spriteRenderer.color = new Color(glow, 1.0f, glow, 1.0f);

            if (Random.value < 0.04f)
            {
                Vector3 particlePos = transform.position + new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-0.5f, 0.8f),
                    0f);
                SandboxVisualEffects.SpawnDust(particlePos, 2, new Color(0.20f, 0.95f, 0.90f, 0.8f));
            }
        }

        private static Sprite obeliskSpriteCache;

        private static Sprite CreateObeliskSprite()
        {
            if (obeliskSpriteCache != null) return obeliskSpriteCache;

            int width = 24;
            int height = 48;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color stoneDark = new(0.28f, 0.24f, 0.22f, 1f);
            Color stoneLight = new(0.55f, 0.48f, 0.42f, 1f);
            Color cyanRune = new(0.20f, 0.95f, 0.90f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            for (int y = 2; y <= 45; y++)
            {
                int inset = y > 36 ? (y - 36) / 2 : 2;
                for (int x = inset + 2; x <= width - 3 - inset; x++)
                {
                    if (x == inset + 2 || x == width - 3 - inset || y == 2 || y == 45)
                    {
                        tex.SetPixel(x, y, stoneDark);
                    }
                    else if (x == 11 || x == 12)
                    {
                        // Vertical cyan energy rune core
                        tex.SetPixel(x, y, cyanRune);
                    }
                    else if (x > 12)
                    {
                        tex.SetPixel(x, y, stoneLight);
                    }
                    else
                    {
                        tex.SetPixel(x, y, stoneDark);
                    }
                }
            }

            tex.Apply();
            obeliskSpriteCache = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.15f), 24);
            return obeliskSpriteCache;
        }
    }
}
