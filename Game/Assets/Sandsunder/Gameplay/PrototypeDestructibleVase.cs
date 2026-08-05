using UnityEngine;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class PrototypeDestructibleVase : MonoBehaviour
    {
        [SerializeField]
        private int health = 30;

        [SerializeField]
        private string lootDropId = "prototype_heal";

        private Collider2D objectCollider;
        private SpriteRenderer objectRenderer;
        private bool isBroken = false;

        private void Awake()
        {
            objectCollider = GetComponent<Collider2D>();
            objectRenderer = GetComponent<SpriteRenderer>();

            if (objectCollider != null) objectCollider.isTrigger = false;

            objectRenderer.sprite = CreateVaseSprite(false);
            objectRenderer.color = Color.white;
            objectRenderer.sortingOrder = 10;
        }

        public void TakeDamage(int amount)
        {
            if (isBroken) return;

            health -= amount;
            SandboxVisualEffects.SpawnDust(transform.position, 6, new Color(0.82f, 0.58f, 0.35f));

            if (health <= 0)
            {
                BreakObject();
            }
        }

        private void BreakObject()
        {
            isBroken = true;
            if (objectCollider != null) objectCollider.enabled = false;
            objectRenderer.sprite = CreateVaseSprite(true);

            // Explosive Debris & Loot Drop
            SandboxVisualEffects.SpawnDust(transform.position, 20, new Color(0.85f, 0.60f, 0.35f));
            SandboxVisualEffects.SpawnDust(transform.position + Vector3.up * 0.2f, 12, new Color(0.95f, 0.80f, 0.40f));

            // Spawn Loot Drop
            PrototypePickup.Spawn(transform.position, Random.Range(400000, 500000), lootDropId);
            Debug.Log("[DestructibleVase] VASE DISTRUTTO! LOOT RILASCIATO!");
        }

        private static Sprite CreateVaseSprite(bool broken)
        {
            int size = 24;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color clay = new(0.82f, 0.52f, 0.30f, 1f);
            Color darkClay = new(0.55f, 0.32f, 0.18f, 1f);
            Color goldBand = new(0.95f, 0.78f, 0.25f, 1f);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            if (!broken)
            {
                // Intact Clay Urn Vase
                for (int x = 6; x <= 17; x++)
                {
                    for (int y = 4; y <= 19; y++)
                    {
                        float dx = (x - 11.5f) / 6f;
                        float dy = (y - 11.5f) / 7.5f;
                        if ((dx * dx) + (dy * dy) <= 1f)
                        {
                            tex.SetPixel(x, y, y == 11 || y == 12 ? goldBand : clay);
                        }
                    }
                }
                for (int x = 8; x <= 15; x++) tex.SetPixel(x, 20, darkClay);
            }
            else
            {
                // Broken Clay Shards Debris
                for (int x = 3; x <= 8; x++) for (int y = 2; y <= 6; y++) tex.SetPixel(x, y, clay);
                for (int x = 14; x <= 20; x++) for (int y = 3; y <= 7; y++) tex.SetPixel(x, y, darkClay);
                for (int x = 9; x <= 13; x++) for (int y = 1; y <= 4; y++) tex.SetPixel(x, y, goldBand);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), 32);
        }
    }
}
