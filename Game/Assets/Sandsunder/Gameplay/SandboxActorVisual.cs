using System.Linq;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Presentation rig kept below the authoritative/physical actor root. The root collider and
    /// Rigidbody never bob, squash, flip, or recoil with the visuals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxActorVisual : MonoBehaviour
    {
        private const float WalkBobAmplitude = 0.055f;
        private const float WalkBobFrequency = 10.5f;
        private const float RollVisualDuration = 0.30f;

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private SpriteRenderer shadowRenderer;

        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private SpriteRenderer weaponRenderer;

        [SerializeField]
        private WeaponAnimator weaponAnimator;

        [SerializeField]
        private NomadAnimator nomadAnimator;

        [SerializeField]
        private TopDownPlayerController controller;

        [SerializeField]
        private PrototypePlayerCombat combat;

        [SerializeField]
        private bool hostile;

        private Sprite bodySprite;
        private Transform bodyRoot;
        private Transform weaponRoot;
        private Animator animator;
        private RuntimeAnimatorController runtimeAnimatorController;
        private Vector3 previousWorldPosition;
        private Vector2 explicitAim = Vector2.right;
        private float walkPhase;
        private float rollRemaining;
        private float recoilRemaining;
        private float meleeRemaining;
        private float hitRemaining;
        private float afterimageCountdown;
        private float dustSpawnTimer;
        private bool initialized;
        private Sprite stealthSprite;

        public Transform VisualRoot => visualRoot;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public SpriteRenderer WeaponRenderer => weaponRenderer;
        public bool IsHostile => hostile;

        public static SandboxActorVisual Ensure(
            GameObject actor,
            PrototypePixelKind fallbackKind,
            Color fallbackColor,
            TopDownPlayerController movement = null,
            PrototypePlayerCombat playerCombat = null,
            bool isHostile = false)
        {
            SandboxActorVisual visual = actor.GetComponent<SandboxActorVisual>();
            if (visual == null)
            {
                visual = actor.AddComponent<SandboxActorVisual>();
            }

            Sprite body = PrototypePixelArt.GetCachedSprite(fallbackKind, fallbackColor);
            visual.Configure(body, null, null, movement, playerCombat, isHostile);
            return visual;
        }

        /// <summary>
        /// Configures imported or generated sprites. Null sprites keep readable cached fallbacks.
        /// This method is safe both in the editor builder and during play mode.
        /// </summary>
        public void Configure(
            Sprite body,
            Sprite shadow,
            Sprite weapon,
            TopDownPlayerController configuredController,
            PrototypePlayerCombat configuredCombat,
            bool isHostile,
            RuntimeAnimatorController animatorController = null)
        {
            controller = configuredController;
            combat = configuredCombat;
            hostile = isHostile;
            runtimeAnimatorController = animatorController;

            // Preserve Nomad's official 32x32 sprite (nomad_32.png) without letting Animator clip overrides swap it to yellow wanderer
            if (animator != null)
            {
                animator.enabled = false;
            }

            bodySprite = body;
            EnsureHierarchy();

            if (body == null && !hostile)
            {
#if UNITY_EDITOR
                body = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Runtime/Characters/nomad_32.png");
                if (body == null)
                {
                    var subSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sandsunder/Art/Runtime/Characters/nomad_32.png").OfType<Sprite>().ToArray();
                    if (subSprites.Length > 0) body = subSprites[0];
                }
#endif
            }

            bodyRenderer.sprite = body != null
                ? body
                : PrototypePixelArt.GetCachedSprite(
                    hostile ? PrototypePixelKind.Spitter : PrototypePixelKind.Player,
                    hostile ? new Color(0.82f, 0.30f, 0.20f) : new Color(0.25f, 0.72f, 0.78f));
            shadowRenderer.sprite = shadow != null
                ? shadow
                : PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, new Color(0.08f, 0.06f, 0.05f, 0.48f));
            weaponRenderer.sprite = weapon != null
                ? weapon
                : hostile
                    ? null
                    : PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, new Color(0.94f, 0.76f, 0.35f));

            shadowRenderer.color = shadow != null ? Color.white : new Color(0.08f, 0.06f, 0.05f, 0.48f);
            bodyRenderer.color = Color.white;
            weaponRenderer.color = Color.white;
            weaponRenderer.enabled = weaponRenderer.sprite != null;
            bodyRenderer.transform.localScale = Vector3.one;
            shadowRenderer.transform.localScale = new Vector3(0.95f, 0.42f, 1f);
            weaponRenderer.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            bodyRoot.localPosition = new Vector3(0f, 0.16f, 0f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -0.27f, 0f);
            shadowRenderer.transform.localScale = new Vector3(0.95f, 0.42f, 1f);
            weaponRoot.localPosition = new Vector3(0.06f, 0.02f, 0f);
            previousWorldPosition = transform.position;
            initialized = true;
            ApplySorting();
        }

        public void SetAimDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                explicitAim = direction.normalized;
            }
        }

        public void PlayFire(Vector2 direction)
        {
            SetAimDirection(direction);
            recoilRemaining = 0.10f;
            if (weaponAnimator != null) weaponAnimator.PlayFire();
        }

        public void PlayMelee(Vector2 direction)
        {
            SetAimDirection(direction);
            meleeRemaining = 0.18f;
            if (weaponAnimator != null) weaponAnimator.PlaySwing();
        }

        public void PlayRoll(Vector2 direction)
        {
            SetAimDirection(direction);
            rollRemaining = RollVisualDuration;
            afterimageCountdown = 0f;
        }

        public void PlayHit()
        {
            hitRemaining = 0.13f;
        }

        public void SetVisible(bool visible)
        {
            EnsureHierarchy();
            shadowRenderer.enabled = visible;
            bodyRenderer.enabled = visible;
            weaponRenderer.enabled = visible && weaponRenderer.sprite != null;
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }
        }

        private void Awake()
        {
            EnsureHierarchy();
            previousWorldPosition = transform.position;
        }

        private void LateUpdate()
        {
            EnsureHierarchy();
            float delta = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector2 velocity = ((Vector2)transform.position - (Vector2)previousWorldPosition) / delta;
            previousWorldPosition = transform.position;
            bool walking = velocity.sqrMagnitude > 0.08f;
            if (controller != null)
            {
                SetAimDirection(controller.AimDirection);
                walking = controller.CurrentMoveInput.sqrMagnitude > 0.02f;
            }

            if (walking)
            {
                walkPhase += Time.deltaTime * WalkBobFrequency;
                footstepTimer += Time.deltaTime;
                if (footstepTimer >= 0.38f)
                {
                    footstepTimer = 0f;
                    SandboxFootprint.SpawnAt(transform.position, explicitAim);
                }
            }
            else
            {
                footstepTimer = 0.30f;
            }

            rollRemaining = Mathf.Max(0f, rollRemaining - Time.deltaTime);
            recoilRemaining = Mathf.Max(0f, recoilRemaining - Time.deltaTime);
            meleeRemaining = Mathf.Max(0f, meleeRemaining - Time.deltaTime);
            hitRemaining = Mathf.Max(0f, hitRemaining - Time.deltaTime);

            bool stealthed = DigDepthSystem.Instance != null && DigDepthSystem.Instance.IsSubterranean;
            if (nomadAnimator != null)
            {
                nomadAnimator.SetMoving(walking && !hostile ? 1f : 0f);
                nomadAnimator.SetRolling(rollRemaining > 0f);
                nomadAnimator.SetStealthed(stealthed && !hostile);
            }

            if (bodyRenderer != null && bodySprite != null && !hostile)
            {
                if (stealthed)
                {
                    if (stealthSprite == null)
                    {
#if UNITY_EDITOR
                        stealthSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Runtime/Animations/nomad_stealth_crouch.png");
#endif
                    }
                    bodyRenderer.sprite = stealthSprite != null ? stealthSprite : bodySprite;
                    bodyRenderer.color = new Color(0.2f, 0.9f, 1.0f, 0.75f);
                }
                else
                {
                    bodyRenderer.sprite = bodySprite;
                    bodyRenderer.color = Color.white;
                }
            }

            UpdateHeldItemSprite();
            ApplyFacing();
            ApplyActionAnimation();
            ApplySorting();
        }

        private float footstepTimer = 0f;

        private void UpdateHeldItemSprite()
        {
            if (weaponRenderer == null || hostile) return;
            if (PrototypeInventoryHUD.Instance == null)
            {
                weaponRenderer.enabled = false;
                return;
            }

            int selectedIndex = PrototypeInventoryHUD.Instance.SelectedIndex;
            var items = PrototypeInventoryHUD.Instance.InventoryItems;
            if (selectedIndex >= 0 && selectedIndex < items.Count)
            {
                string itemId = items[selectedIndex];
                Sprite icon = PrototypeInventoryHUD.Instance.GetItemSprite(itemId);
                if (icon != null)
                {
                    weaponRenderer.sprite = icon;
                    weaponRenderer.enabled = true;
                    return;
                }
            }

            weaponRenderer.enabled = false;
        }

        private void ApplyFacing()
        {
            bool faceLeft = explicitAim.x < -0.02f;
            bodyRenderer.flipX = faceLeft;

            // Anchor weapon to hand pivot position based on facing direction
            float handX = faceLeft ? -0.25f : 0.25f;
            weaponRoot.localPosition = new Vector3(handX, 0.08f, 0f);

            float angle = Mathf.Atan2(explicitAim.y, explicitAim.x) * Mathf.Rad2Deg;
            weaponRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            weaponRenderer.flipY = faceLeft;
        }

        private void ApplyActionAnimation()
        {
            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool isSubterranean = isShiftHeld || (controller != null && controller.CurrentDepth > 0);
            bool isMoving = controller != null && controller.CurrentMoveInput.sqrMagnitude > 0.01f;
            bool isDigging = controller != null && controller.IsDiggingChanneling;

            // Set Animator parameters dynamically
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsMoving", isMoving);
                animator.SetBool("IsRolling", rollRemaining > 0f);
                animator.SetBool("IsDigging", isDigging);
                animator.SetFloat("Speed", controller != null ? controller.CurrentMoveInput.magnitude : 0f);
            }

            if (rollRemaining > 0f)
            {
                float progress = 1f - (rollRemaining / RollVisualDuration);
                float rollAngle = progress * 360f * (explicitAim.x >= 0f ? -1f : 1f);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, rollAngle);

                afterimageCountdown -= Time.deltaTime;
                if (afterimageCountdown <= 0f)
                {
                    SandboxAfterimage.Spawn(bodyRenderer, bodyRenderer.transform.position, bodyRenderer.sortingOrder - 1);
                    afterimageCountdown = 0.05f; // Emit exactly every 0.05s!
                }
            }
            else if (isSubterranean)
            {
                // Stealth / Tunnel Crouch Stance (100% full scale, natural crouch tilt)
                float tiltAngle = explicitAim.x >= 0f ? 8f : -8f;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
            }
            else if (isMoving)
            {
                // Run Lean Tilt vs Walk Oscillating Stride
                float moveSpeed = controller != null ? controller.CurrentMoveInput.magnitude : 0f;
                if (moveSpeed > 0.75f)
                {
                    // Running
                    float lean = explicitAim.x >= 0f ? 12f : -12f;
                    visualRoot.localRotation = Quaternion.Euler(0f, 0f, lean);

                    // Emetti polvere sollevata dai piedi per la corsa
                    dustSpawnTimer += Time.deltaTime;
                    if (dustSpawnTimer >= 0.10f)
                    {
                        dustSpawnTimer = 0f;
                        SandboxVisualEffects.SpawnDust(transform.position, 1, new Color(0.85f, 0.75f, 0.55f));
                    }
                }
                else
                {
                    // Walking
                    float stride = Mathf.Sin(walkPhase) * 5f;
                    visualRoot.localRotation = Quaternion.Euler(0f, 0f, stride);
                }
            }
            else
            {
                visualRoot.localRotation = Quaternion.identity;
            }

            visualRoot.localScale = Vector3.one; // Always 100% full scale! NO shrinking!
            float recoil = recoilRemaining > 0f ? recoilRemaining / 0.10f : 0f;
            float melee = meleeRemaining > 0f ? Mathf.Sin((1f - meleeRemaining / 0.18f) * Mathf.PI) : 0f;

            if (hostile)
            {
                // Dune Spitter jump-hop animation on spit attack and movement
                float spitterJumpY = recoil > 0f
                    ? Mathf.Sin(recoil * Mathf.PI) * 0.32f
                    : (isMoving ? Mathf.Abs(Mathf.Sin(walkPhase * 1.5f)) * 0.14f : 0f);
                bodyRoot.localPosition = new Vector3(0f, 0.16f + spitterJumpY, 0f);
            }

            weaponRoot.localPosition = new Vector3(0.22f - (recoil * 0.10f), 0.03f, 0f);
            weaponRoot.localRotation *= Quaternion.Euler(0f, 0f, melee * (explicitAim.x >= 0f ? -42f : 42f));

            Color bodyColor = Color.white;
            if (hitRemaining > 0f)
            {
                bodyColor = Color.red;
            }
            else if (isSubterranean)
            {
                bodyColor = new Color(0.20f, 0.90f, 0.95f, 0.85f); // Subterranean Cyan Silhouette Overlay
            }

            bodyRenderer.color = bodyColor;
        }

        private void ApplySorting()
        {
            int baseOrder = 500 - Mathf.RoundToInt(transform.position.y * 20f);
            shadowRenderer.sortingOrder = baseOrder - 2;
            bodyRenderer.sortingOrder = baseOrder;
            weaponRenderer.sortingOrder = baseOrder + 1;
        }

        private void EnsureHierarchy()
        {
            // Transform shortcuts are intentionally not serialized. Rehydrate them after a
            // domain reload or scene deserialization before taking the fast path.
            if (visualRoot != null)
            {
                bodyRoot = bodyRoot != null
                    ? bodyRoot
                    : bodyRenderer != null
                        ? bodyRenderer.transform
                        : visualRoot.Find("Body");
                weaponRoot = weaponRoot != null
                    ? weaponRoot
                    : weaponRenderer != null
                        ? weaponRenderer.transform
                        : visualRoot.Find("Weapon");
            }

            visualRoot = GetOrCreateChild(transform, "VisualRoot");
            Transform shadowRoot = GetOrCreateChild(visualRoot, "Shadow");
            bodyRoot = GetOrCreateChild(visualRoot, "Body");
            weaponRoot = GetOrCreateChild(visualRoot, "Weapon");
            shadowRenderer = GetOrAddRenderer(shadowRoot);
            bodyRenderer = GetOrAddRenderer(bodyRoot);
            weaponRenderer = GetOrAddRenderer(weaponRoot);

            // Frame-player for weapon action animations (idle/fire/reload/swing). It is driven by
            // PlayFire/PlayMelee/PlayRoll and is a no-op until animation frames are assigned.
            if (weaponAnimator == null)
            {
                weaponAnimator = weaponRoot.GetComponent<WeaponAnimator>();
                if (weaponAnimator == null)
                {
                    weaponAnimator = weaponRoot.gameObject.AddComponent<WeaponAnimator>();
                }
            }

            if (animator == null)
            {
                animator = bodyRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = bodyRoot.gameObject.AddComponent<Animator>();
                }
            }

            // NomadAnimator drives the AnimatorController (Idle/Walk/Run/Roll/Dig/StealthCrouch)
            // from combat/movement state. Attached next to the Animator on the body root.
            if (nomadAnimator == null)
            {
                nomadAnimator = bodyRoot.GetComponent<NomadAnimator>();
                if (nomadAnimator == null)
                {
                    nomadAnimator = bodyRoot.gameObject.AddComponent<NomadAnimator>();
                }
            }

            if (animator.runtimeAnimatorController == null && runtimeAnimatorController != null)
            {
                animator.runtimeAnimatorController = runtimeAnimatorController;
            }

            initialized = true;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static SpriteRenderer GetOrAddRenderer(Transform target)
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer : target.gameObject.AddComponent<SpriteRenderer>();
        }

        private static void NormalizeSpriteHeight(SpriteRenderer renderer, float targetHeight)
        {
            if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0.0001f)
            {
                renderer.transform.localScale = Vector3.one;
                return;
            }

            float scale = targetHeight / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    internal sealed class SandboxAfterimage : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float remaining = 0.18f;

        internal static void Spawn(SpriteRenderer source, Vector3 worldPosition, int sortingOrder)
        {
            if (source == null || source.sprite == null)
            {
                return;
            }

            GameObject afterimage = new("Roll Afterimage");
            afterimage.transform.position = worldPosition;
            afterimage.transform.localScale = source.transform.lossyScale;
            SpriteRenderer renderer = afterimage.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.flipX = source.flipX;
            renderer.sortingOrder = sortingOrder;
            renderer.color = new Color(0.34f, 0.86f, 0.88f, 0.34f);
            SandboxAfterimage effect = afterimage.AddComponent<SandboxAfterimage>();
            effect.spriteRenderer = renderer;
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            float alpha = Mathf.Clamp01(remaining / 0.18f) * 0.34f;
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
            transform.localScale *= 1f + (Time.deltaTime * 1.7f);
            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
