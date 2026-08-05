using System.Collections.Generic;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    public sealed class SandboxPitDecal : MonoBehaviour
    {
        private static readonly Dictionary<int, Sprite> pitSpriteCache = new();

        public static void SpawnAt(Vector2 worldCellCenter, int depth)
        {
            GameObject pitObj = new($"ExcavatedPit_Depth_{depth}");
            pitObj.transform.position = new Vector3(worldCellCenter.x, worldCellCenter.y, 0f);

            SpriteRenderer sr = pitObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            sr.sprite = GetOrCreatePitSprite(depth);
            sr.color = Color.white;

            pitObj.AddComponent<SandboxPitDecal>();

            // Dynamic Sand Wave Displacements
            SandboxVisualEffects.SpawnDust(worldCellCenter, 18, new Color(0.88f, 0.72f, 0.45f));
            SandboxVisualEffects.SpawnDust(worldCellCenter + Vector2.up * 0.2f, 12, new Color(0.55f, 0.40f, 0.25f));
            SandboxVisualEffects.SpawnDust(worldCellCenter - Vector2.up * 0.2f, 12, new Color(0.95f, 0.85f, 0.55f));
        }

        private static Sprite GetOrCreatePitSprite(int depth)
        {
            if (pitSpriteCache.TryGetValue(depth, out var cached))
            {
                return cached;
            }

            int size = 32;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color sandBase = new(0.93f, 0.82f, 0.67f, 0.0f);
            Color sandRim = new(0.78f, 0.62f, 0.40f, 0.70f);
            Color trenchDepth = depth >= 2 ? new(0.35f, 0.24f, 0.14f, 0.85f) : new(0.55f, 0.42f, 0.26f, 0.80f);
            Color pebbleColor = new(0.40f, 0.30f, 0.20f, 0.90f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    if (distFromCenter > 15f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (distFromCenter > 9f)
                    {
                        float t = (distFromCenter - 9f) / 6f;
                        Color blend = Color.Lerp(trenchDepth, sandRim, t);
                        blend.a = Mathf.Lerp(0.85f, 0.0f, t);
                        tex.SetPixel(x, y, blend);
                    }
                    else
                    {
                        float t = distFromCenter / 9f;
                        Color inner = Color.Lerp(new Color(0.25f, 0.16f, 0.08f, 0.90f), trenchDepth, t);
                        
                        // Progressive Sand Cracks & Crepe Overlay (Safe Cracking Aesthetic)
                        bool isCrackPixel = (x == y && distFromCenter < 12f) ||
                                            (x + y == 31 && distFromCenter < 12f) ||
                                            (x == 15 && y > 4 && y < 27) ||
                                            (y == 15 && x > 4 && x < 27);
                        if (isCrackPixel)
                        {
                            inner = new Color(0.15f, 0.08f, 0.04f, 0.95f);
                        }
                        
                        tex.SetPixel(x, y, inner);
                    }
                }
            }

            // Scatter desert pebbles around trench rim
            tex.SetPixel(8, 24, pebbleColor);
            tex.SetPixel(9, 24, pebbleColor);
            tex.SetPixel(23, 10, pebbleColor);
            tex.SetPixel(7, 9, pebbleColor);

            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            pitSpriteCache[depth] = sprite;
            return sprite;
        }
    }
}
