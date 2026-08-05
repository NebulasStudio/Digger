using System;
using System.Collections.Generic;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed partial class PrototypeHealth
    {
        [SerializeField]
        private int entityId;

        [SerializeField]
        private int team;

        [SerializeField]
        private int maximumHealth = 100;

        [SerializeField]
        private bool autoRespawn = true;

        private CombatantState state;
        private Vector3 spawnPosition;
        private int deadTicks;
        private long simulationTickAccumulatorMicrounits;
        private Collider2D[] colliders;
        private SpriteRenderer[] renderers;

        public event Action<PrototypeHealth> Died;
        public event Action<PrototypeHealth> Respawned;

        public CombatantState State
        {
            get
            {
                EnsureState();
                return state;
            }
        }

        public int EntityId => State.EntityId;
        public int Team => State.Team;
        public int CurrentHealth => State.Health;
        public int MaximumHealth => State.MaximumHealth;
        public bool IsDead => State.IsDead;
        public bool IsInvulnerable => State.IsInvulnerable;

        public void Configure(int configuredEntityId, int configuredTeam, int configuredMaximumHealth, bool shouldRespawn)
        {
            entityId = configuredEntityId;
            team = configuredTeam;
            maximumHealth = configuredMaximumHealth;
            autoRespawn = shouldRespawn;
            state = null;
            if (Application.isPlaying)
            {
                EnsureState();
            }
        }

        public CombatDamageResult TryDamage(CombatDamageRequest request)
        {
            bool wasAlive = !State.IsDead;
            CombatDamageResult result = State.TryApplyDamage(request);
            ApplyDamagePresentation(result);
            if (wasAlive && State.IsDead)
            {
                BeginDeath();
            }

            return result;
        }

        public int Heal(int amount)
        {
            return State.Heal(amount);
        }

        public CombatDamageResult ResolveProjectile(CombatProjectileState projectile)
        {
            if (projectile == null) throw new ArgumentNullException(nameof(projectile));
            bool wasAlive = !State.IsDead;
            CombatDamageResult result = projectile.TryHit(State);
            ApplyDamagePresentation(result);
            if (wasAlive && State.IsDead)
            {
                BeginDeath();
            }

            return result;
        }

        public void RespawnNow()
        {
            State.Reset();
            deadTicks = 0;
            transform.position = spawnPosition;
            if (TryGetComponent(out Rigidbody2D respawnBody))
            {
                respawnBody.position = spawnPosition;
            }
            SetPresentationEnabled(true);
            Respawned?.Invoke(this);
        }

        private void Awake()
        {
            spawnPosition = transform.position;
            colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
            renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            EnsureState();
        }

        private void FixedUpdate()
        {
            const long microunitsPerSecond = 1_000_000L;
            long fixedDeltaMicrounits = (long)Math.Round(
                Time.fixedDeltaTime * microunitsPerSecond,
                MidpointRounding.AwayFromZero);
            simulationTickAccumulatorMicrounits +=
                fixedDeltaMicrounits * CombatRules.PrototypeOne.TicksPerSecond;

            while (simulationTickAccumulatorMicrounits >= microunitsPerSecond)
            {
                simulationTickAccumulatorMicrounits -= microunitsPerSecond;
                State.AdvanceOneTick();
                if (!State.IsDead || !autoRespawn)
                {
                    continue;
                }

                deadTicks++;
                if (deadTicks >= CombatRules.PrototypeOne.RespawnDelayTicks)
                {
                    RespawnNow();
                }
            }
        }

        private void EnsureState()
        {
            if (state == null)
            {
                state = new CombatantState(entityId, team, maximumHealth, CombatRules.PrototypeOne);
            }
        }

        private void BeginDeath()
        {
            deadTicks = 0;
            SetPresentationEnabled(false);
            Died?.Invoke(this);
        }

        private void ApplyDamagePresentation(CombatDamageResult result)
        {
            if (result != CombatDamageResult.Applied)
            {
                return;
            }

            GetComponent<SandboxActorVisual>()?.PlayHit();
            SandboxVisualEffects.SpawnImpact(transform.position, team == 0
                ? new Color(0.40f, 0.92f, 0.91f)
                : new Color(0.94f, 0.38f, 0.24f));
            if (team == 0 && Camera.main != null)
            {
                Camera.main.GetComponent<OrthographicCameraFollow>()?.Shake(0.10f, 0.11f);
            }
        }

        private void SetPresentationEnabled(bool enabled)
        {
            if (colliders != null)
            {
                foreach (Collider2D targetCollider in colliders)
                {
                    if (targetCollider != null)
                    {
                        targetCollider.enabled = enabled;
                    }
                }
            }

            if (renderers != null)
            {
                foreach (SpriteRenderer targetRenderer in renderers)
                {
                    if (targetRenderer != null)
                    {
                        targetRenderer.enabled = enabled;
                    }
                }
            }

            GetComponent<SandboxActorVisual>()?.SetVisible(enabled);
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrototypeHealth), typeof(TopDownPlayerController))]
    public sealed partial class PrototypePlayerCombat
    {
        private static int nextProjectileId = 1000;

        private PrototypeHealth health;
        private TopDownPlayerController movement;
        private InputActionMap inputMap;
        private InputAction fireAction;
        private InputAction shovelAction;
        private InputAction rollAction;
        private int pickupCount;

        public int PickupCount => pickupCount;
        public int CurrentHealth => health != null ? health.CurrentHealth : CombatRules.PrototypeOne.PlayerMaximumHealth;
        public bool IsRolling => health != null && health.State.IsRolling;
        public bool IsInvulnerable => health != null && health.State.IsInvulnerable;
        public float PistolCooldownRemainingSeconds => CooldownSeconds(health?.State.PistolCooldownRemainingTicks ?? 0);
        public float ShovelCooldownRemainingSeconds => CooldownSeconds(health?.State.ShovelCooldownRemainingTicks ?? 0);
        public float RollCooldownRemainingSeconds => CooldownSeconds(health?.State.RollCooldownRemainingTicks ?? 0);

        public void Configure(int entityId)
        {
            health = GetComponent<PrototypeHealth>();
            health.Configure(
                entityId,
                configuredTeam: 0,
                configuredMaximumHealth: CombatRules.PrototypeOne.PlayerMaximumHealth,
                shouldRespawn: false);
        }

        public bool TryFireForTesting()
        {
            EnsureReferences();
            if (!health.State.TryUse(CombatAction.Pistol))
            {
                return false;
            }

            CombatRules rules = CombatRules.PrototypeOne;
            Vector2 aim = movement.AimDirection.sqrMagnitude > 0.0001f
                ? movement.AimDirection.normalized
                : Vector2.right;

            string itemId = GetEquippedItemId();

            if (itemId == "shotgun.heavy")
            {
                float[] angles = { -15f, -7.5f, 0f, 7.5f, 15f };
                foreach (float angle in angles)
                {
                    Vector2 spreadAim = Quaternion.Euler(0, 0, angle) * aim;
                    PrototypeProjectile.Spawn(
                        (Vector2)transform.position + (spreadAim * 0.42f),
                        spreadAim, nextProjectileId++, health.EntityId, health.Team,
                        22, 14.0f, 10.0f, 0f, new Color(0.98f, 0.75f, 0.30f));
                }
                GetComponent<SandboxActorVisual>()?.PlayFire(aim);
                SandboxVisualEffects.SpawnMuzzle(transform.position, aim, new Color(0.98f, 0.75f, 0.30f));
                SandboxVisualEffects.SpawnShellCasing(transform.position, aim, new Color(0.35f, 0.30f, 0.25f));
                SandboxVisualEffects.SpawnDust(transform.position, 4, new Color(0.60f, 0.60f, 0.65f));
                SandboxReloadBar.Instance?.StartReload(1.8f);
                return true;
            }
            else if (itemId == "blaster.rune")
            {
                PrototypeProjectile.Spawn(
                    (Vector2)transform.position + (aim * 0.42f),
                    aim, nextProjectileId++, health.EntityId, health.Team,
                    38, 22.0f, 16.0f, 0f, new Color(0.20f, 0.95f, 0.90f));
                GetComponent<SandboxActorVisual>()?.PlayFire(aim);
                SandboxVisualEffects.SpawnMuzzle(transform.position, aim, new Color(0.20f, 0.95f, 0.90f));
                SandboxVisualEffects.SpawnShellCasing(transform.position, aim, new Color(0.20f, 0.95f, 0.90f));
                SandboxVisualEffects.SpawnDust(transform.position, 2, new Color(0.20f, 0.95f, 0.90f));
                SandboxReloadBar.Instance?.StartReload(0.9f);
                return true;
            }
            else if (itemId == "sword.scimitar" || itemId == "shovel.default")
            {
                return TryShovelForTesting();
            }

            // Rifle Brass (Firearm)
            GetComponent<SandboxActorVisual>()?.PlayFire(aim);
            SandboxVisualEffects.SpawnMuzzle(transform.position, aim, new Color(0.95f, 0.85f, 0.40f));
            SandboxVisualEffects.SpawnShellCasing(transform.position, aim, new Color(0.95f, 0.75f, 0.30f));
            SandboxVisualEffects.SpawnDust(transform.position, 2, new Color(0.85f, 0.65f, 0.25f));
            PrototypeProjectile.Spawn(
                (Vector2)transform.position + (aim * 0.42f),
                aim,
                nextProjectileId++,
                health.EntityId,
                health.Team,
                rules.PistolDamage,
                rules.PistolProjectileSpeedMillimetresPerSecond / 1000f,
                rules.PistolRangeMillimetres / 1000f,
                telegraphSeconds: 0f,
                new Color(0.95f, 0.85f, 0.40f));
            SandboxReloadBar.Instance?.StartReload(1.2f);
            return true;
        }

        public bool TryShovelForTesting()
        {
            EnsureReferences();
            if (!health.State.TryUse(CombatAction.Shovel))
            {
                return false;
            }

            CombatRules rules = CombatRules.PrototypeOne;
            Vector2 facing = movement.AimDirection.sqrMagnitude > 0.0001f
                ? movement.AimDirection.normalized
                : Vector2.right;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rules.ShovelReachMillimetres / 1000f);
            HashSet<int> processedHealth = new();
            HashSet<int> processedNodes = new();

            foreach (Collider2D hit in hits)
            {
                PrototypeHealth targetHealth = hit.GetComponentInParent<PrototypeHealth>();
                if (targetHealth != null
                    && targetHealth != health
                    && processedHealth.Add(targetHealth.GetInstanceID())
                    && IsInsideShovelArc(targetHealth.transform.position, facing, rules))
                {
                    targetHealth.TryDamage(new CombatDamageRequest(health.EntityId, health.Team, rules.ShovelDamage));
                }

                PrototypeDigNode node = hit.GetComponentInParent<PrototypeDigNode>();
                if (node != null
                    && processedNodes.Add(node.GetInstanceID())
                    && IsInsideShovelArc(node.transform.position, facing, rules))
                {
                    node.Strike();
                }
            }

            if (processedNodes.Count == 0 && PrototypeDigGridAuthority.Instance != null)
            {
                Vector2 targetDigPos = (Vector2)transform.position + (facing * 0.75f);
                PrototypeDigGridAuthority.Instance.TryDigAtWorldPosition(targetDigPos);
            }

            PrototypeArcFlash.Spawn(transform.position, facing, rules.ShovelReachMillimetres / 1000f);
            GetComponent<SandboxActorVisual>()?.PlayMelee(facing);
            SandboxVisualEffects.SpawnDust(
                (Vector2)transform.position + (facing * 0.62f),
                3,
                new Color(0.86f, 0.70f, 0.43f));
            return true;
        }

        public bool TryRollForTesting()
        {
            EnsureReferences();
            Vector2 direction = movement.CurrentMoveInput.sqrMagnitude > 0.01f
                ? movement.CurrentMoveInput.normalized
                : movement.AimDirection.normalized;
            if (direction.sqrMagnitude <= 0.01f || !movement.TryConsumeStamina(25f) || !health.State.TryUse(CombatAction.Roll))
            {
                return false;
            }

            bool began = movement.BeginPrototypeRoll(direction);
            if (began)
            {
                GetComponent<SandboxActorVisual>()?.PlayRoll(direction);
            }

            return began;
        }

        public bool AcceptPickup(string lootId)
        {
            if (string.IsNullOrWhiteSpace(lootId))
            {
                return false;
            }

            EnsureReferences();
            if (lootId == PrototypeDigGridAuthority.HealLootId && health.CurrentHealth < health.MaximumHealth)
            {
                health.Heal(CombatRules.PrototypeOne.HealingPickupAmount);
            }
            else
            {
                pickupCount++;
            }

            PrototypeInventoryHUD.Instance?.AddItem(lootId);
            return true;
        }

        private void Awake()
        {
            EnsureReferences();
            CreateInputActions();
        }

        private void OnEnable()
        {
            inputMap?.Enable();
        }

        private void OnDisable()
        {
            inputMap?.Disable();
        }

        private void OnDestroy()
        {
            if (inputMap == null)
            {
                return;
            }

            fireAction.performed -= OnFirePerformed;
            shovelAction.performed -= OnShovelPerformed;
            rollAction.performed -= OnRollPerformed;
            inputMap.Dispose();
        }

        private float digChannelTimer = 0f;
        private bool isDiggingChannel = false;
        private float reloadTimer = 0f;
        private bool isReloading = false;

        public string GetEquippedItemId()
        {
            string itemId = "rifle.brass";
            if (PrototypeInventoryHUD.Instance != null)
            {
                int sel = PrototypeInventoryHUD.Instance.SelectedIndex;
                var items = PrototypeInventoryHUD.Instance.InventoryItems;
                if (sel >= 0 && sel < items.Count) itemId = items[sel];
            }
            return itemId;
        }

        private void Update()
        {
            EnsureReferences();
            if (movement == null) return;

            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                string itemId = GetEquippedItemId();
                if (itemId == "rifle.brass" || itemId == "shotgun.heavy" || itemId == "blaster.rune")
                {
                    isReloading = true;
                    reloadTimer = 1.2f;
                    Debug.Log("[Combat] RICARICA ARMA IN CORSO...");
                    SandboxVisualEffects.SpawnDust(transform.position, 6, new Color(0.95f, 0.70f, 0.30f));
                    SandboxReloadBar.Instance?.StartReload(1.2f);
                }
            }

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0f)
                {
                    isReloading = false;
                    Debug.Log("[Combat] RICARICA COMPLETATA!");
                }
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryUseConsumable();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                PrototypeMobSpawnerToggle.Instance?.TriggerRespawnAll();
            }

            if (shovelAction != null && shovelAction.IsPressed() && movement.CurrentStamina > 0.1f && GetEquippedItemId() == "shovel.default")
            {
                if (!isDiggingChannel)
                {
                    isDiggingChannel = true;
                    digChannelTimer = 0f;
                    movement.IsDiggingChanneling = true;
                }

                float previousTimer = digChannelTimer;
                digChannelTimer += Time.deltaTime;

                Vector2 facing = movement.AimDirection.sqrMagnitude > 0.0001f
                    ? movement.AimDirection.normalized
                    : Vector2.right;

                Vector2 digCenter = (Vector2)transform.position + (facing * 0.75f);
                // Continuous excavation dust while channeling (Feature 1).
                SandDustEmitter.Instance?.SetChanneling(true, digCenter);
                // Dynamically spawn spiral sand waves converging to the center!
                SandboxVisualEffects.SpawnSandSpiral(digCenter);

                if (Mathf.FloorToInt(previousTimer / 0.40f) != Mathf.FloorToInt(digChannelTimer / 0.40f))
                {
                    GetComponent<SandboxActorVisual>()?.PlayMelee(facing);
                    SandboxVisualEffects.SpawnDust(
                        digCenter,
                        4,
                        new Color(0.86f, 0.70f, 0.43f));
                }

                if (digChannelTimer >= 3.0f)
                {
                    CompleteDigChanneling(facing);
                    isDiggingChannel = false;
                    digChannelTimer = 0f;
                    movement.IsDiggingChanneling = false;
                    SandDustEmitter.Instance?.SetChanneling(false, Vector2.zero);
                }
            }
            else if (isDiggingChannel)
            {
                isDiggingChannel = false;
                digChannelTimer = 0f;
                movement.IsDiggingChanneling = false;
                SandDustEmitter.Instance?.SetChanneling(false, Vector2.zero);
            }
        }

        private void TryUseConsumable()
        {
            if (PrototypeInventoryHUD.Instance == null || health == null) return;
            int selectedIndex = PrototypeInventoryHUD.Instance.SelectedIndex;
            var items = PrototypeInventoryHUD.Instance.InventoryItems;
            if (selectedIndex >= 0 && selectedIndex < items.Count)
            {
                string itemId = items[selectedIndex];
                if (itemId == PrototypeDigGridAuthority.HealLootId || itemId.Contains("heal"))
                {
                    if (health.CurrentHealth < health.MaximumHealth)
                    {
                        health.Heal(35);
                        SandboxVisualEffects.SpawnDust(transform.position, 12, new Color(0.20f, 0.95f, 0.40f));
                        Debug.Log("[Combat] CONSUMABILE UTILIZZATO! VITA RIPRISTINATA +35 HP!");
                    }
                }
            }
        }

        private void CompleteDigChanneling(Vector2 facing)
        {
            Vector2 targetDigPos = (Vector2)transform.position + (facing * 0.75f);
            if (PrototypeDigGridAuthority.Instance != null)
            {
                PrototypeDigGridAuthority.Instance.TryDigAtWorldPosition(targetDigPos);
            }

            PrototypeTunnelSystem.Instance?.TransitionToLayer(DigDepthSystem.Instance?.IsSubterranean == true
                ? MatrixLayerDepth.Subterranean_L1
                : MatrixLayerDepth.Surface_L0);

            // Feature 2 — Subterranean Depth: reaching depth >= 2 drops the Nomad to Level -1.
            DigDepthSystem.Instance?.RaiseDepth(2);
            SandboxVisualEffects.SpawnDust(targetDigPos, 12, new Color(0.60f, 0.42f, 0.22f));
        }

        private void OnGUI()
        {
            // Floating overhead HUD and Inventory HUD handle status cleanly
        }

        private void CreateInputActions()
        {
            inputMap = new InputActionMap("Prototype Combat");

            fireAction = inputMap.AddAction("Fire", InputActionType.Button);
            fireAction.AddBinding("<Mouse>/leftButton");
            fireAction.AddBinding("<Gamepad>/rightTrigger");

            shovelAction = inputMap.AddAction("Shovel", InputActionType.Button);
            shovelAction.AddBinding("<Mouse>/rightButton");
            shovelAction.AddBinding("<Keyboard>/f");
            shovelAction.AddBinding("<Gamepad>/buttonSouth");

            rollAction = inputMap.AddAction("Roll", InputActionType.Button);
            rollAction.AddBinding("<Keyboard>/space");
            rollAction.AddBinding("<Gamepad>/buttonEast");

            fireAction.performed += OnFirePerformed;
            shovelAction.performed += OnShovelPerformed;
            rollAction.performed += OnRollPerformed;
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            string itemId = GetEquippedItemId();
            if (itemId == "rifle.brass" || itemId == "shotgun.heavy" || itemId == "blaster.rune")
            {
                TryFireForTesting();
            }
            else if (itemId == "sword.scimitar" || itemId == "shovel.default")
            {
                TryShovelForTesting();
            }
        }

        private void OnShovelPerformed(InputAction.CallbackContext context)
        {
            // Digging is now channeled via holding Mouse Right (shovelAction.IsPressed()) in Update
        }

        private void OnRollPerformed(InputAction.CallbackContext context)
        {
            TryRollForTesting();
        }

        public bool TryShovelMeleeAttack()
        {
            EnsureReferences();
            if (isDiggingChannel) return false;
            if (!health.State.TryUse(CombatAction.Shovel))
            {
                return false;
            }

            CombatRules rules = CombatRules.PrototypeOne;
            Vector2 facing = movement.AimDirection.sqrMagnitude > 0.0001f
                ? movement.AimDirection.normalized
                : Vector2.right;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rules.ShovelReachMillimetres / 1000f);
            HashSet<int> processedHealth = new();

            foreach (Collider2D hit in hits)
            {
                PrototypeDestructibleVase vase = hit.GetComponentInParent<PrototypeDestructibleVase>();
                if (vase != null)
                {
                    vase.TakeDamage(35);
                }

                PrototypeHealth targetHealth = hit.GetComponentInParent<PrototypeHealth>();
                if (targetHealth != null
                    && targetHealth != health
                    && processedHealth.Add(targetHealth.GetInstanceID())
                    && IsInsideShovelArc(targetHealth.transform.position, facing, rules))
                {
                    targetHealth.TryDamage(new CombatDamageRequest(health.EntityId, health.Team, rules.ShovelDamage));
                }
            }

            PrototypeArcFlash.Spawn(transform.position, facing, rules.ShovelReachMillimetres / 1000f);
            GetComponent<SandboxActorVisual>()?.PlayMelee(facing);
            return true;
        }

        private bool IsInsideShovelArc(Vector3 targetPosition, Vector2 facing, CombatRules rules)
        {
            Vector2 origin = transform.position;
            return CombatMath.IsInsideArc(
                Mathf.RoundToInt(origin.x * 1000f),
                Mathf.RoundToInt(origin.y * 1000f),
                Mathf.RoundToInt(facing.x * 1000f),
                Mathf.RoundToInt(facing.y * 1000f),
                Mathf.RoundToInt(targetPosition.x * 1000f),
                Mathf.RoundToInt(targetPosition.y * 1000f),
                rules.ShovelReachMillimetres,
                rules.ShovelArcCosinePermille);
        }

        private void EnsureReferences()
        {
            health = health != null ? health : GetComponent<PrototypeHealth>();
            movement = movement != null ? movement : GetComponent<TopDownPlayerController>();
        }

        private static float CooldownSeconds(long ticks)
        {
            return ticks / (float)CombatRules.PrototypeOne.TicksPerSecond;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed partial class PrototypeProjectile
    {
        private CombatProjectileState state;
        private Vector2 direction;
        private float speed;
        private float range;
        private float travelled;
        private float telegraphRemaining;
        private Rigidbody2D body;

        public CombatProjectileState State => state;
        public int OwnerEntityId => state?.OwnerEntityId ?? -1;

        public static PrototypeProjectile Spawn(
            Vector2 position,
            Vector2 direction,
            int projectileId,
            int ownerEntityId,
            int ownerTeam,
            int damage,
            float speed,
            float range,
            float telegraphSeconds,
            Color color)
        {
            GameObject projectileObject = new($"Projectile {projectileId}");
            projectileObject.transform.position = position;
            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 20;
            PrototypePixelArt pixelArt = projectileObject.AddComponent<PrototypePixelArt>();
            pixelArt.Configure(PrototypePixelKind.Projectile, color);
            SandboxProjectileVisual projectileVisual = projectileObject.AddComponent<SandboxProjectileVisual>();
            projectileVisual.Configure(null, color, direction, telegraphSeconds, ownerTeam != 0);

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.10f;
            Rigidbody2D projectileBody = projectileObject.AddComponent<Rigidbody2D>();
            projectileBody.bodyType = RigidbodyType2D.Kinematic;
            projectileBody.gravityScale = 0f;
            projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            PrototypeProjectile projectile = projectileObject.AddComponent<PrototypeProjectile>();
            projectile.Configure(
                projectileId,
                ownerEntityId,
                ownerTeam,
                damage,
                speed,
                range,
                direction,
                telegraphSeconds);
            return projectile;
        }

        public void Configure(
            int projectileId,
            int ownerEntityId,
            int ownerTeam,
            int damage,
            float configuredSpeed,
            float configuredRange,
            Vector2 configuredDirection,
            float telegraphSeconds)
        {
            direction = configuredDirection.sqrMagnitude > 0.0001f
                ? configuredDirection.normalized
                : Vector2.right;
            speed = configuredSpeed;
            range = configuredRange;
            telegraphRemaining = Mathf.Max(0f, telegraphSeconds);
            state = new CombatProjectileState(
                projectileId,
                ownerEntityId,
                ownerTeam,
                damage,
                Mathf.RoundToInt(speed * 1000f),
                Mathf.RoundToInt(range * 1000f));
            transform.right = direction;
        }

        public CombatDamageResult ResolveHit(PrototypeHealth target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.ResolveProjectile(state);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (state == null)
            {
                return;
            }

            if (telegraphRemaining > 0f)
            {
                telegraphRemaining -= Time.fixedDeltaTime;
                return;
            }

            float distance = speed * Time.fixedDeltaTime;
            body.MovePosition(body.position + (direction * distance));
            travelled += distance;
            if (travelled >= range)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (state == null || telegraphRemaining > 0f)
            {
                return;
            }

            PrototypeDestructibleVase vase = other.GetComponentInParent<PrototypeDestructibleVase>();
            if (vase != null)
            {
                vase.TakeDamage(state.Damage);
                GetComponent<SandboxProjectileVisual>()?.PlayImpact();
                Destroy(gameObject);
                return;
            }

            PrototypeHealth target = other.GetComponentInParent<PrototypeHealth>();
            if (target != null)
            {
                if (target.EntityId == state.OwnerEntityId || target.Team == state.OwnerTeam)
                {
                    return;
                }

                if (target.GetComponent<SubterraneanStealth>()?.IsStealthed == true)
                {
                    return; // Subterranean stealth: surface projectiles overfly underground players!
                }

                CombatDamageResult result = target.ResolveProjectile(state);
                if (result == CombatDamageResult.Applied || result == CombatDamageResult.RejectedInvulnerable)
                {
                    GetComponent<SandboxProjectileVisual>()?.PlayImpact();
                    Destroy(gameObject);
                }

                return;
            }

            GetComponent<SandboxProjectileVisual>()?.PlayImpact();
            Destroy(gameObject);
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrototypeHealth), typeof(Rigidbody2D))]
    public sealed partial class PrototypeDuneSpitter
    {
        private static int nextProjectileId = 100000;

        [SerializeField]
        private PrototypePlayerCombat target;

        private PrototypeHealth health;
        private Rigidbody2D body;
        private float spitterFootstepTimer;

        public void Configure(PrototypePlayerCombat configuredTarget, int entityId)
        {
            target = configuredTarget;
            health = GetComponent<PrototypeHealth>();
            health.Configure(
                entityId,
                configuredTeam: 1,
                configuredMaximumHealth: CombatRules.PrototypeOne.SpitterMaximumHealth,
                shouldRespawn: true);
        }

        private void Awake()
        {
            health = GetComponent<PrototypeHealth>();
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }

        private void FixedUpdate()
        {
            if (health.IsDead)
            {
                return;
            }

            if (target == null)
            {
                target = FindFirstObjectByType<PrototypePlayerCombat>();
            }

            if (target == null)
            {
                return;
            }

            if (target.GetComponent<SubterraneanStealth>()?.IsStealthed == true)
            {
                // Subterranean stealth: spitter cannot see or attack the underground player!
                return;
            }

            CombatRules rules = CombatRules.PrototypeOne;
            Vector2 offset = target.transform.position - transform.position;
            float distance = offset.magnitude;
            if (distance > 0.001f)
            {
                Vector2 toward = offset / distance;
                GetComponent<SandboxActorVisual>()?.SetAimDirection(toward);
                Vector2 moveDirection = distance > rules.SpitterPreferredRangeMillimetres / 1000f
                    ? toward
                    : new Vector2(-toward.y, toward.x) * ((health.EntityId & 1) == 0 ? 1f : -1f);
                Vector2 targetPos = body.position + (moveDirection * (rules.SpitterMoveSpeedMillimetresPerSecond / 1000f) * Time.fixedDeltaTime);
                Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.42f);
                if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform))
                {
                    body.MovePosition(targetPos);
                }

                spitterFootstepTimer += Time.fixedDeltaTime;
                if (spitterFootstepTimer >= 0.55f)
                {
                    spitterFootstepTimer = 0f;
                    SandboxFootprint.SpawnAt(transform.position, moveDirection);
                }
            }

            if (distance <= rules.SpitterAttackRangeMillimetres / 1000f
                && health.State.TryUse(CombatAction.SpitterShot))
            {
                Vector2 shotDirection = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.right;
                GetComponent<SandboxActorVisual>()?.PlayFire(shotDirection);
                SandboxVisualEffects.SpawnMuzzle(
                    (Vector2)transform.position,
                    shotDirection,
                    new Color(0.94f, 0.36f, 0.25f));
                PrototypeProjectile.Spawn(
                    (Vector2)transform.position + (shotDirection * 0.42f),
                    offset,
                    nextProjectileId++,
                    health.EntityId,
                    health.Team,
                    rules.SpitterDamage,
                    rules.SpitterProjectileSpeedMillimetresPerSecond / 1000f,
                    rules.SpitterAttackRangeMillimetres / 1000f,
                    rules.SpitterTelegraphTicks / (float)rules.TicksPerSecond,
                    new Color(0.94f, 0.36f, 0.25f));
            }
        }
    }

    internal sealed class PrototypeArcFlash : MonoBehaviour
    {
        private float duration = 0.12f;
        private float remaining = 0.12f;
        private float startAngle;
        private float sweepAngle = 180f;
        private Vector2 origin;
        private float radius;
        private Vector2 baseDir;

        internal static void Spawn(Vector2 origin, Vector2 direction, float reach)
        {
            GameObject flash = new("Melee Cyan Arc Flash");
            flash.transform.position = origin;
            flash.transform.right = direction;
            
            // Scaled arc representation
            flash.transform.localScale = new Vector3(reach * 1.2f, reach * 0.6f, 1f);
            
            SpriteRenderer renderer = flash.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 25;
            
            PrototypePixelArt art = flash.AddComponent<PrototypePixelArt>();
            // Color is now native cyan arc flash!
            art.Configure(PrototypePixelKind.Projectile, new Color(0.20f, 0.90f, 0.95f, 0.90f));
            
            PrototypeArcFlash behavior = flash.AddComponent<PrototypeArcFlash>();
            behavior.origin = origin;
            behavior.radius = reach;
            behavior.baseDir = direction;
            behavior.startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            float progress = 1f - (remaining / duration);
            
            // Sweep rotation over 180 degrees!
            float currentAngle = startAngle + (progress * sweepAngle);
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
            
            // Arc moves outward slightly during swing
            transform.position = origin + (Vector2)(Quaternion.Euler(0f, 0f, currentAngle - 90f) * baseDir * (radius * 0.4f * progress));

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
