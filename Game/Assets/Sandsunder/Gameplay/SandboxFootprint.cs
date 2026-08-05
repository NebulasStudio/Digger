using System.Collections.Generic;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    public sealed class SandboxFootprint : MonoBehaviour
    {
        private static Sprite footprintSprite;
        private SpriteRenderer spriteRenderer;
        private float alpha = 0.55f;

        public static void SpawnAt(Vector2 worldPos, Vector2 facingDirection)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Shift Stealth: No footprints left on sand!
                return;
            }

            GameObject printObj = new("SandFootprint");
            printObj.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
            printObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            SpriteRenderer sr = printObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1; // On ground level
            sr.sprite = GetOrCreateSprite();
            sr.color = new Color(0.38f, 0.28f, 0.16f, 0.55f);

            printObj.AddComponent<SandboxFootprint>();
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            alpha -= Time.deltaTime * 0.18f;
            if (alpha <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }
        }

        private static Sprite GetOrCreateSprite()
        {
            if (footprintSprite != null) return footprintSprite;

            int size = 16;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color darkSand = new(0.36f, 0.26f, 0.14f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            // Left print
            tex.SetPixel(5, 6, darkSand);
            tex.SetPixel(5, 7, darkSand);
            tex.SetPixel(5, 8, darkSand);
            tex.SetPixel(6, 6, darkSand);
            tex.SetPixel(6, 7, darkSand);

            // Right print
            tex.SetPixel(10, 3, darkSand);
            tex.SetPixel(10, 4, darkSand);
            tex.SetPixel(10, 5, darkSand);
            tex.SetPixel(11, 3, darkSand);
            tex.SetPixel(11, 4, darkSand);

            tex.Apply();
            footprintSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16);
            return footprintSprite;
        }
    }
}
