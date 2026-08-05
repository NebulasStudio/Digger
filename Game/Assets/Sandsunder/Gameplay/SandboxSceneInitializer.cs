using System.Collections.Generic;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Runtime auto-initializer that expands the arena into a vast 52x36m desert ruin region,
    /// adding destructible clay vases, ancient cyan obelisks, ruin sanctuaries, and tiered chests.
    /// </summary>
    public sealed class SandboxSceneInitializer : MonoBehaviour
    {
        private static bool hasInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRun()
        {
            if (hasInitialized) return;
            hasInitialized = true;

            GameObject initObj = new("SandboxSceneInitializer_Auto");
            initObj.AddComponent<SandboxSceneInitializer>();
        }

        private void Awake()
        {
            EnsureRuntimeSystems();
            ApplyVisualUpgrades();
            SpawnExpandedElements();
        }

        /// <summary>
        /// Instantiates the singletons for the Feature 1/2 runtime systems (dig terrain overlay,
        /// crepe-crack FX, continuous sand dust, and the excavation depth owner). These are pure
        /// presentation/relay systems: they never mutate the authoritative simulation.
        /// </summary>
        private void EnsureRuntimeSystems()
        {
            if (DigDepthSystem.Instance == null && FindFirstObjectByType<DigDepthSystem>() == null)
            {
                new GameObject("DigDepthSystem").AddComponent<DigDepthSystem>();
            }
            if (DigTerrainView.Instance == null && FindFirstObjectByType<DigTerrainView>() == null)
            {
                new GameObject("DigTerrainView").AddComponent<DigTerrainView>();
            }
            if (SandCrepeCracksFX.Instance == null && FindFirstObjectByType<SandCrepeCracksFX>() == null)
            {
                new GameObject("SandCrepeCracksFX").AddComponent<SandCrepeCracksFX>();
            }
            if (SandDustEmitter.Instance == null && FindFirstObjectByType<SandDustEmitter>() == null)
            {
                new GameObject("SandDustEmitter").AddComponent<SandDustEmitter>();
            }
        }

        private void ApplyVisualUpgrades()
        {
            var digNodes = FindObjectsByType<PrototypeDigNode>(FindObjectsSortMode.None);
            Sprite chestSprite = CreateChestSprite();

            foreach (var node in digNodes)
            {
                var sr = node.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                    sr.sprite = chestSprite;
                    sr.sortingOrder = 5;
                }

                var visual = node.GetComponent<SandboxDigVisual>();
                if (visual != null)
                {
                    visual.Configure(chestSprite, chestSprite, CreateOpenedChestSprite());
                }
            }

            var walls = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (var wall in walls)
            {
                if (wall.name.Contains("Rampart") || wall.name.Contains("Wall") || wall.name.Contains("Pillar"))
                {
                    var sr = wall.GetComponent<SpriteRenderer>();
                    if (sr != null && (sr.color.a < 0.99f || sr.color.r > 0.8f))
                    {
                        sr.color = new Color(0.78f, 0.62f, 0.44f, 1.0f);
                    }
                }
            }
        }

        private void SpawnExpandedElements()
        {
            // 1. Ancient Rune Obelisks in Sanctuary
            if (FindFirstObjectByType<PrototypeAncientRuneObelisk>() == null)
            {
                GameObject obelisk1 = new("RuneObelisk_West");
                obelisk1.transform.position = new Vector3(-8.5f, 4.2f, 0f);
                obelisk1.AddComponent<PrototypeAncientRuneObelisk>();

                GameObject obelisk2 = new("RuneObelisk_East");
                obelisk2.transform.position = new Vector3(8.5f, 4.2f, 0f);
                obelisk2.AddComponent<PrototypeAncientRuneObelisk>();
            }

            // 2. Destructible Clay Vases in Ruin Courtyard
            if (FindFirstObjectByType<PrototypeDestructibleVase>() == null)
            {
                Vector2[] vasePositions =
                {
                    new(-4.2f, 2.8f),
                    new(-3.6f, 2.8f),
                    new(4.2f, -2.8f),
                    new(4.8f, -2.8f),
                    new(0f, 5.5f)
                };

                for (int i = 0; i < vasePositions.Length; i++)
                {
                    GameObject vaseObj = new($"DestructibleVase_{i + 1}");
                    vaseObj.transform.position = vasePositions[i];
                    // PrototypeDestructibleVase requires a Collider2D + SpriteRenderer.
                    vaseObj.AddComponent<SpriteRenderer>();
                    vaseObj.AddComponent<BoxCollider2D>();
                    vaseObj.AddComponent<PrototypeDestructibleVase>();
                }
            }

            // 3. Desert Ruin Locked Door
            if (FindFirstObjectByType<PrototypeDesertRuinDoor>() == null)
            {
                GameObject doorObj = new("DesertRuin_LockedDoor");
                doorObj.transform.position = new Vector3(0f, 2.5f, 0f);
                doorObj.AddComponent<PrototypeDesertRuinDoor>();
            }

            // 4. Sandstorm Golem Boss (Feature 3) — spawned north of the arena, away from the
            // starting zone. PrototypeHealth + Rigidbody2D are auto-added by RequireComponent.
            if (FindFirstObjectByType<SandstormGolemAI>() == null)
            {
                GameObject golemObj = new("SandstormGolem_Boss");
                golemObj.transform.position = new Vector3(0f, 9f, 0f);
                golemObj.AddComponent<PrototypeHealth>();
                golemObj.AddComponent<Rigidbody2D>();
                golemObj.AddComponent<SandstormGolemAI>();
            }
        }

        private static Sprite chestSpriteCache;
        private static Sprite openChestSpriteCache;

        private static Sprite CreateChestSprite()
        {
            if (chestSpriteCache != null) return chestSpriteCache;

            int size = 32;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color umberOutline = new(43 / 255f, 33 / 255f, 30 / 255f, 1f);
            Color woodLight = new(168 / 255f, 108 / 255f, 54 / 255f, 1f);
            Color ironBand = new(75 / 255f, 82 / 255f, 90 / 255f, 1f);
            Color goldLatch = new(245 / 255f, 195 / 255f, 55 / 255f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            for (int y = 6; y <= 24; y++)
            {
                for (int x = 4; x <= 27; x++)
                {
                    if (x == 4 || x == 27 || y == 6 || y == 24)
                    {
                        tex.SetPixel(x, y, umberOutline);
                    }
                    else if (x == 9 || x == 10 || x == 21 || x == 22)
                    {
                        tex.SetPixel(x, y, ironBand);
                    }
                    else
                    {
                        tex.SetPixel(x, y, woodLight);
                    }
                }
            }

            for (int y = 13; y <= 17; y++)
            {
                for (int x = 14; x <= 17; x++)
                {
                    tex.SetPixel(x, y, goldLatch);
                }
            }

            tex.Apply();
            chestSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            return chestSpriteCache;
        }

        private static Sprite CreateOpenedChestSprite()
        {
            if (openChestSpriteCache != null) return openChestSpriteCache;

            int size = 32;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color umberOutline = new(43 / 255f, 33 / 255f, 30 / 255f, 1f);
            Color woodDark = new(95 / 255f, 58 / 255f, 38 / 255f, 1f);
            Color goldGlow = new(255 / 255f, 215 / 255f, 60 / 255f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            for (int y = 6; y <= 24; y++)
            {
                for (int x = 4; x <= 27; x++)
                {
                    if (x == 4 || x == 27 || y == 6 || y == 24)
                    {
                        tex.SetPixel(x, y, umberOutline);
                    }
                    else if (y >= 10 && y <= 18 && x >= 8 && x <= 23)
                    {
                        tex.SetPixel(x, y, goldGlow);
                    }
                    else
                    {
                        tex.SetPixel(x, y, woodDark);
                    }
                }
            }

            tex.Apply();
            openChestSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            return openChestSpriteCache;
        }
    }
}
