using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Small, presentation-only feedback helpers backed by cached proxy sprites.</summary>
    internal static class SandboxVisualEffects
    {
        private static Material trailMaterial;

        internal static Material SharedTrailMaterial
        {
            get
            {
                if (trailMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader == null)
                    {
                        shader = Shader.Find("Sprites/Default");
                    }

                    if (shader != null)
                    {
                        trailMaterial = new Material(shader)
                        {
                            name = "Sandbox Shared Trail Material",
                            hideFlags = HideFlags.HideAndDontSave,
                        };
                    }
                }

                return trailMaterial;
            }
        }

        internal static void SpawnMuzzle(Vector2 origin, Vector2 direction, Color color)
        {
            Vector2 facing = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            SpawnTransient(
                "Muzzle Flash",
                origin + (facing * 0.48f),
                PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, Color.Lerp(color, Color.white, 0.55f)),
                color,
                new Vector3(0.34f, 0.16f, 1f),
                0.09f,
                facing * 0.4f,
                SortingFor(origin, 4),
                facing);
        }

        internal static void SpawnImpact(Vector2 position, Color color)
        {
            for (int index = 0; index < 4; index++)
            {
                float angle = (index * 90f) + 22.5f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                SpawnTransient(
                    "Impact Spark",
                    position,
                    PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, color),
                    Color.Lerp(color, Color.white, 0.3f),
                    new Vector3(0.16f, 0.08f, 1f),
                    0.16f,
                    direction * 1.6f,
                    SortingFor(position, 3),
                    direction);
            }
        }

        internal static void SpawnDust(Vector2 position, int count, Color color)
        {
            int particleCount = Mathf.Clamp(count, 1, 10);
            for (int index = 0; index < particleCount; index++)
            {
                float angle = ((index + 0.5f) / particleCount) * 180f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                SpawnTransient(
                    "Sand Dust",
                    position + new Vector2(0f, -0.12f),
                    PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, color),
                    new Color(color.r, color.g, color.b, 0.72f),
                    Vector3.one * Mathf.Lerp(0.10f, 0.18f, index / (float)particleCount),
                    0.28f,
                    new Vector2(direction.x * 0.65f, Mathf.Abs(direction.y) * 0.8f + 0.25f),
                    SortingFor(position, -1),
                    direction);
            }
        }

        internal static void SpawnSandSpiral(Vector2 center)
        {
            // Gentle converging dust burst while digging — smaller, faster, subtler than the
            // original fast-rotating spiral so it reads as excavation dust, not a swirl.
            Color sandColor = new Color(0.85f, 0.72f, 0.45f, 0.80f);
            int count = 5;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / count)) * Mathf.Deg2Rad;
                float radius = 0.55f;
                Vector2 spawnOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Vector2 spawnPos = center + spawnOffset;

                Vector2 radialDir = -spawnOffset.normalized;
                Vector2 velocity = radialDir * 0.9f;

                SpawnTransient(
                    "Sand Dust",
                    spawnPos,
                    PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, sandColor),
                    sandColor,
                    Vector3.one * 0.08f,
                    0.35f,
                    velocity,
                    SortingFor(center, 2),
                    radialDir);
            }
        }

        internal static void SpawnShellCasing(Vector2 position, Vector2 direction, Color color)
        {
            // Shell casings are ejected outwards/upwards from the weapon
            Vector2 ejectDir = new Vector2(-direction.y, direction.x) * Random.Range(1.3f, 1.9f) + (-direction * Random.Range(0.2f, 0.5f));
            SpawnTransient(
                "Shell Casing",
                position,
                PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, color),
                color,
                new Vector3(0.08f, 0.04f, 1f),
                1.5f, // casing stays on ground for 1.5 seconds
                ejectDir,
                SortingFor(position, -1),
                ejectDir);
        }

        private static void SpawnTransient(
            string name,
            Vector2 position,
            Sprite sprite,
            Color color,
            Vector3 scale,
            float lifetime,
            Vector2 velocity,
            int sortingOrder,
            Vector2 facing)
        {
            GameObject effectObject = new(name);
            effectObject.transform.position = position;
            effectObject.transform.localScale = scale;
            effectObject.transform.right = facing;
            SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            SandboxTransientSpriteFx effect = effectObject.AddComponent<SandboxTransientSpriteFx>();
            effect.Configure(renderer, lifetime, velocity);
        }

        internal static int SortingFor(Vector2 worldPosition, int offset = 0)
        {
            return 500 - Mathf.RoundToInt(worldPosition.y * 20f) + offset;
        }
    }

    internal sealed class SandboxTransientSpriteFx : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime;
        private float remaining;
        private Vector2 velocity;
        private Vector3 initialScale;
        private Color initialColor;

        internal void Configure(SpriteRenderer renderer, float duration, Vector2 configuredVelocity)
        {
            spriteRenderer = renderer;
            lifetime = Mathf.Max(0.01f, duration);
            remaining = lifetime;
            velocity = configuredVelocity;
            initialScale = transform.localScale;
            initialColor = renderer != null ? renderer.color : Color.white;
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= Mathf.Pow(0.05f, Time.deltaTime);
            float normalized = Mathf.Clamp01(remaining / lifetime);
            transform.localScale = initialScale * Mathf.Lerp(1.35f, 0.45f, 1f - normalized);
            if (spriteRenderer != null)
            {
                Color color = initialColor;
                color.a = initialColor.a * normalized;
                spriteRenderer.color = color;
            }

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class SandboxProjectileVisual : MonoBehaviour
    {
        private SpriteRenderer coreRenderer;
        private SpriteRenderer glowRenderer;
        private TrailRenderer trail;
        private Transform visualRoot;
        private float telegraphRemaining;
        private float pulsePhase;
        private Color projectileColor;
        private readonly Vector3 baseScale = new(0.55f, 0.32f, 1f);

        public SpriteRenderer CoreRenderer => coreRenderer;
        public TrailRenderer Trail => trail;

        public void Configure(Sprite projectileSprite, Color color, Vector2 direction, float telegraphSeconds, bool hostile)
        {
            EnsureHierarchy();
            projectileColor = color;
            telegraphRemaining = Mathf.Max(0f, telegraphSeconds);
            Sprite sprite = projectileSprite;
#if UNITY_EDITOR
            if (sprite == null)
            {
                // Use the generated cyan rune projectile when available (editor-only load, no
                // runtime dependency on the Editor assembly).
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Runtime/Processed/proj_sentinel_cyan_rune_32.png");
            }
#endif
            sprite = sprite != null
                ? sprite
                : PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, color);
            coreRenderer.sprite = sprite;
            coreRenderer.color = Color.white;
            glowRenderer.sprite = sprite;
            glowRenderer.color = new Color(color.r, color.g, color.b, 0.32f);
            glowRenderer.transform.localScale = new Vector3(1.8f, 2.2f, 1f);
            transform.right = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            trail.time = hostile ? 0.14f : 0.10f;
            trail.startWidth = hostile ? 0.16f : 0.12f;
            trail.endWidth = 0f;
            trail.startColor = new Color(color.r, color.g, color.b, 0.68f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            ApplySorting();
            if (SandboxVisualEffects.SharedTrailMaterial != null)
            {
                trail.sharedMaterial = SandboxVisualEffects.SharedTrailMaterial;
            }
        }

        public void PlayImpact()
        {
            SandboxVisualEffects.SpawnImpact(transform.position, projectileColor);
        }

        private void Awake()
        {
            EnsureHierarchy();
        }

        private void Update()
        {
            ApplySorting();
            pulsePhase += Time.deltaTime * 22f;
            if (telegraphRemaining > 0f)
            {
                telegraphRemaining = Mathf.Max(0f, telegraphRemaining - Time.deltaTime);
                float pulse = 0.58f + (Mathf.Sin(pulsePhase) * 0.18f);
                coreRenderer.color = new Color(projectileColor.r, projectileColor.g, projectileColor.b, pulse);
                trail.emitting = false;
                float scale = 0.55f + pulse * 0.25f;
                visualRoot.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
            }
            else
            {
                coreRenderer.color = Color.white;
                trail.emitting = true;
                float pulse = 1f + (Mathf.Sin(pulsePhase) * 0.08f);
                visualRoot.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
            }
        }

        private void EnsureHierarchy()
        {
            if (coreRenderer != null && glowRenderer != null && visualRoot != null && trail != null)
            {
                return;
            }

            SpriteRenderer physicalRenderer = GetComponent<SpriteRenderer>();
            visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                GameObject visualObject = new("VisualRoot");
                visualObject.transform.SetParent(transform, false);
                visualRoot = visualObject.transform;
            }

            coreRenderer = visualRoot.GetComponent<SpriteRenderer>();
            if (coreRenderer == null)
            {
                coreRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            }

            if (physicalRenderer != null)
            {
                if (coreRenderer.sprite == null)
                {
                    coreRenderer.sprite = physicalRenderer.sprite;
                    coreRenderer.color = physicalRenderer.color;
                }

                physicalRenderer.enabled = false;
            }

            Transform glow = visualRoot.Find("Glow");
            if (glow == null)
            {
                GameObject glowObject = new("Glow");
                glowObject.transform.SetParent(visualRoot, false);
                glow = glowObject.transform;
            }

            glowRenderer = glow.GetComponent<SpriteRenderer>();
            if (glowRenderer == null)
            {
                glowRenderer = glow.gameObject.AddComponent<SpriteRenderer>();
            }

            trail = trail != null ? trail : GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            trail.minVertexDistance = 0.05f;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            visualRoot.localScale = baseScale;
        }

        private void ApplySorting()
        {
            int baseOrder = SandboxVisualEffects.SortingFor(transform.position);
            trail.sortingOrder = baseOrder;
            glowRenderer.sortingOrder = baseOrder + 1;
            coreRenderer.sortingOrder = baseOrder + 2;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SandboxDigVisual : MonoBehaviour
    {
        [SerializeField]
        private Sprite intactSprite;

        [SerializeField]
        private Sprite crackedSprite;

        [SerializeField]
        private Sprite openedSprite;

        private SpriteRenderer spriteRenderer;
        private Transform visualRoot;
        private Vector3 restingScale;
        private float strikeRemaining;
        private bool revealed;

        public void Configure(Sprite intact, Sprite cracked, Sprite opened)
        {
            intactSprite = intact;
            crackedSprite = cracked;
            openedSprite = opened;
            EnsureRenderer();
            if (intactSprite != null)
            {
                spriteRenderer.sprite = intactSprite;
                PrototypePixelArt proxy = GetComponent<PrototypePixelArt>();
                if (proxy != null)
                {
                    proxy.enabled = false;
                }
            }

            restingScale = visualRoot.localScale;
        }

        public void PlayStrike(int strikesRemaining)
        {
            EnsureRenderer();
            strikeRemaining = 0.16f;
            if (!revealed && crackedSprite != null && strikesRemaining > 0)
            {
                spriteRenderer.sprite = crackedSprite;
            }

            SandboxVisualEffects.SpawnDust(transform.position, 4, new Color(0.83f, 0.62f, 0.31f));
        }

        public void PlayReveal()
        {
            EnsureRenderer();
            revealed = true;
            if (openedSprite != null)
            {
                spriteRenderer.sprite = openedSprite;
                spriteRenderer.color = Color.white;
            }

            SandboxVisualEffects.SpawnDust(transform.position, 8, new Color(0.95f, 0.76f, 0.36f));
        }

        private void Awake()
        {
            EnsureRenderer();
            restingScale = visualRoot.localScale;
        }

        private void Update()
        {
            if (strikeRemaining <= 0f)
            {
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, restingScale, 1f - Mathf.Exp(-18f * Time.deltaTime));
                return;
            }

            strikeRemaining = Mathf.Max(0f, strikeRemaining - Time.deltaTime);
            float impact = Mathf.Sin((strikeRemaining / 0.16f) * Mathf.PI);
            visualRoot.localScale = new Vector3(
                restingScale.x * (1f + impact * 0.13f),
                restingScale.y * (1f - impact * 0.10f),
                restingScale.z);
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer != null && visualRoot != null)
            {
                return;
            }

            SpriteRenderer physicalRenderer = GetComponent<SpriteRenderer>();
            visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                GameObject visualObject = new("VisualRoot");
                visualObject.transform.SetParent(transform, false);
                visualRoot = visualObject.transform;
            }

            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            }

            if (physicalRenderer != null)
            {
                if (spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = physicalRenderer.sprite;
                    spriteRenderer.color = physicalRenderer.color;
                    spriteRenderer.sortingOrder = physicalRenderer.sortingOrder;
                }

                physicalRenderer.enabled = false;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class SandboxPickupVisual : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color accent = Color.white;
        private Vector3 restingPosition;
        private float phase;

        public void Configure(SpriteRenderer renderer, Color configuredAccent)
        {
            spriteRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            accent = configuredAccent;
            restingPosition = transform.localPosition;
        }

        public void PlayCollect()
        {
            SandboxVisualEffects.SpawnImpact(transform.position, accent);
        }

        private void Awake()
        {
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            restingPosition = transform.localPosition;
            phase = Mathf.Abs(transform.position.x * 1.77f + transform.position.y * 2.31f);
        }

        private void Update()
        {
            phase += Time.deltaTime * 3.5f;
            transform.localPosition = restingPosition + new Vector3(0f, Mathf.Sin(phase) * 0.09f, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.62f) * 7f);
            if (spriteRenderer != null)
            {
                float pulse = 0.82f + Mathf.Sin(phase * 1.8f) * 0.12f;
                spriteRenderer.color = new Color(
                    Mathf.Lerp(accent.r, 1f, 0.22f),
                    Mathf.Lerp(accent.g, 1f, 0.22f),
                    Mathf.Lerp(accent.b, 1f, 0.22f),
                    pulse);
                spriteRenderer.sortingOrder = 760 - Mathf.RoundToInt(transform.position.y * 20f);
            }
        }
    }
}
