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

        public int ApplyEnvironmentalDamage(int amount)
        {
            bool wasAlive = !State.IsDead;
            int applied = State.ApplyEnvironmentalDamage(amount);
            if (applied > 0)
            {
                ApplyDamagePresentation(CombatDamageResult.Applied);
            }
            if (wasAlive && State.IsDead)
            {
                BeginDeath();
            }

            return applied;
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
        private const float RifleReloadSeconds = 1.2f;
        private const float DigChannelSeconds = 3f;

        private PrototypeHealth health;
        private TopDownPlayerController movement;
        private InputActionMap inputMap;
        private InputAction fireAction;
        private InputAction shovelAction;
        private InputAction rollAction;
        private int pickupCount;
        private readonly SandboxRifleMagazine rifleMagazine = new();

        public int PickupCount => pickupCount;
        public int CurrentHealth => health != null ? health.CurrentHealth : CombatRules.PrototypeOne.PlayerMaximumHealth;
        public bool IsRolling => health != null && health.State.IsRolling;
        public bool IsInvulnerable => health != null && health.State.IsInvulnerable;
        public float PistolCooldownRemainingSeconds => CooldownSeconds(health?.State.PistolCooldownRemainingTicks ?? 0);
        public float ShovelCooldownRemainingSeconds => CooldownSeconds(health?.State.ShovelCooldownRemainingTicks ?? 0);
        public float ScimitarCooldownRemainingSeconds => CooldownSeconds(health?.State.ScimitarCooldownRemainingTicks ?? 0);
        public float RollCooldownRemainingSeconds => CooldownSeconds(health?.State.RollCooldownRemainingTicks ?? 0);
        public int CurrentRifleAmmo => rifleMagazine.Ammunition;
        public int MaximumRifleAmmo => rifleMagazine.Capacity;
        public bool IsReloading => rifleMagazine.IsReloading;
        public bool IsDiggingChanneling => isDiggingChannel;
        public float DiggingProgressRatio => isDiggingChannel
            ? Mathf.Clamp01(digChannelTimer / DigChannelSeconds)
            : 0f;

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
            string itemId = GetEquippedItemId();
            if (itemId == "sword.scimitar" || itemId == "shovel.default")
            {
                return TryMeleeForTesting(itemId);
            }

            if (itemId != "rifle.brass" || !rifleMagazine.CanFire)
            {
                return false;
            }

            if (!health.State.TryUse(CombatAction.Pistol))
            {
                return false;
            }

            SandboxGameplayCatalog catalog = SandboxGameplayCatalog.MilestoneOne;
            Vector2 aim = movement.AimDirection.sqrMagnitude > 0.0001f
                ? movement.AimDirection.normalized
                : Vector2.right;

            // Rifle Brass (Firearm)
            SandboxWeaponDefinition rifle = catalog.Rifle;
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
                rifle.Damage,
                rifle.ProjectileSpeedMillimetresPerSecond / 1000f,
                rifle.ProjectileRangeMillimetres / 1000f,
                telegraphSeconds: 0f,
                new Color(0.95f, 0.85f, 0.40f));
            rifleMagazine.TryConsumeShot();
            if (rifleMagazine.IsReloading) SandboxReloadBar.Instance?.StartReload(RifleReloadSeconds);
            return true;
        }

        public bool TryShovelForTesting()
        {
            return TryMeleeForTesting("shovel.default");
        }

        public bool TryMeleeForTesting(string weaponId)
        {
            EnsureReferences();
            SandboxWeaponDefinition weapon = SandboxGameplayCatalog.MilestoneOne.GetWeapon(weaponId);
            if (weapon.AttackKind != SandboxWeaponAttackKind.Melee)
            {
                return false;
            }

            CombatAction action = weaponId == "sword.scimitar"
                ? CombatAction.Scimitar
                : CombatAction.Shovel;
            if (!health.State.TryUse(action))
            {
                return false;
            }

            Vector2 facing = movement.AimDirection.sqrMagnitude > 0.0001f
                ? movement.AimDirection.normalized
                : Vector2.right;
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                weapon.ReachMillimetres / 1000f);
            HashSet<int> processedHealth = new();
            HashSet<int> processedVases = new();

            foreach (Collider2D hit in hits)
            {
                PrototypeDestructibleVase vase = hit.GetComponentInParent<PrototypeDestructibleVase>();
                if (vase != null
                    && processedVases.Add(vase.GetInstanceID())
                    && IsInsideWeaponArc(vase.transform.position, facing, weapon))
                {
                    vase.TakeDamage(weapon.Damage);
                }

                PrototypeHealth targetHealth = hit.GetComponentInParent<PrototypeHealth>();
                if (targetHealth != null
                    && targetHealth != health
                    && processedHealth.Add(targetHealth.GetInstanceID())
                    && IsInsideWeaponArc(targetHealth.transform.position, facing, weapon))
                {
                    targetHealth.TryDamage(
                        new CombatDamageRequest(health.EntityId, health.Team, weapon.Damage));
                }
            }

            PrototypeArcFlash.Spawn(transform.position, facing, weapon.ReachMillimetres / 1000f);
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

            if (IsGameplayModalOpen())
            {
                CancelDigChannel();
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                BeginReload();
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

                if (digChannelTimer >= DigChannelSeconds)
                {
                    CompleteDigChanneling(facing);
                    CancelDigChannel();
                }
            }
            else if (isDiggingChannel)
            {
                CancelDigChannel();
            }
        }

        public bool TryUseSelectedConsumable()
        {
            EnsureReferences();
            if (PrototypeInventoryHUD.Instance == null || health == null) return false;
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
                        PrototypeInventoryHUD.Instance.TryRemoveAt(selectedIndex, itemId);
                        SandboxVisualEffects.SpawnDust(transform.position, 12, new Color(0.20f, 0.95f, 0.40f));
                        Debug.Log("[Combat] CONSUMABILE UTILIZZATO! VITA RIPRISTINATA +35 HP!");
                        return true;
                    }
                }
                else if (itemId == "consumable.oxygen-flask")
                {
                    SubterraneanOxygenController oxygen = GetComponent<SubterraneanOxygenController>();
                    if (oxygen != null && oxygen.TryRestoreFromFlask())
                    {
                        PrototypeInventoryHUD.Instance.TryRemoveAt(selectedIndex, itemId);
                        SandboxVisualEffects.SpawnDust(transform.position, 12, new Color(0.10f, 0.95f, 0.92f));
                        return true;
                    }
                }
            }

            return false;
        }

        private void CancelDigChannel()
        {
            isDiggingChannel = false;
            digChannelTimer = 0f;
            if (movement != null) movement.IsDiggingChanneling = false;
            SandDustEmitter.Instance?.SetChanneling(false, Vector2.zero);
        }

        private static bool IsGameplayModalOpen()
        {
            bool inventoryOpen = SandboxModernHUD.Instance != null
                && SandboxModernHUD.Instance.InventoryController != null
                && SandboxModernHUD.Instance.InventoryController.IsOpen;
            bool shopOpen = SandboxShopPanel.Instance != null && SandboxShopPanel.Instance.IsOpen;
            return inventoryOpen || shopOpen;
        }

        private void CompleteDigChanneling(Vector2 facing)
        {
            Vector2 targetDigPos = (Vector2)transform.position + (facing * 0.75f);
            if (PrototypeDigGridAuthority.Instance != null)
            {
                DigResult result = PrototypeDigGridAuthority.Instance.TryDigAtWorldPosition(targetDigPos);
                DigDepthSystem.Instance?.ApplyAuthoritativeDigResult(result);
            }

            // Feature 2 — Subterranean Depth: reaching depth >= 2 drops the Nomad to Level -1.
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
            if (IsGameplayModalOpen()) return;
            string itemId = GetEquippedItemId();
            if (itemId == "rifle.brass")
            {
                TryFireForTesting();
            }
            else if (itemId == "sword.scimitar" || itemId == "shovel.default")
            {
                TryMeleeForTesting(itemId);
            }
        }

        private void OnShovelPerformed(InputAction.CallbackContext context)
        {
            // Digging is now channeled via holding Mouse Right (shovelAction.IsPressed()) in Update
        }

        private void OnRollPerformed(InputAction.CallbackContext context)
        {
            if (IsGameplayModalOpen()) return;
            TryRollForTesting();
        }

        public void AdvanceReloadForTesting(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f) return;
            rifleMagazine.AdvanceTicks(Mathf.RoundToInt(
                elapsedSeconds * SandboxGameplayCatalog.MilestoneOne.TicksPerSecond));
        }

        private void BeginReload()
        {
            if (GetEquippedItemId() != "rifle.brass" || !rifleMagazine.TryBeginReload()) return;
            SandboxVisualEffects.SpawnDust(transform.position, 6, new Color(0.95f, 0.70f, 0.30f));
            SandboxReloadBar.Instance?.StartReload(RifleReloadSeconds);
        }

        public bool TryShovelMeleeAttack()
        {
            if (isDiggingChannel) return false;
            string weaponId = GetEquippedItemId();
            if (weaponId != "sword.scimitar" && weaponId != "shovel.default")
            {
                weaponId = "shovel.default";
            }
            return TryMeleeForTesting(weaponId);
        }

        private bool IsInsideWeaponArc(
            Vector3 targetPosition,
            Vector2 facing,
            SandboxWeaponDefinition weapon)
        {
            Vector2 origin = transform.position;
            return CombatMath.IsInsideArc(
                Mathf.RoundToInt(origin.x * 1000f),
                Mathf.RoundToInt(origin.y * 1000f),
                Mathf.RoundToInt(facing.x * 1000f),
                Mathf.RoundToInt(facing.y * 1000f),
                Mathf.RoundToInt(targetPosition.x * 1000f),
                Mathf.RoundToInt(targetPosition.y * 1000f),
                weapon.ReachMillimetres,
                weapon.ArcCosinePermille);
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

            // Passive / Neutral AI: Dune Spitters do not aggro or attack unless damaged or player approaches within 2.5m.
            bool isDamaged = health.CurrentHealth < health.MaximumHealth;
            bool isThreatened = distance <= 2.5f;
            if (!isDamaged && !isThreatened)
            {
                return;
            }
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
        private float sweepAngle = 90f;
        private Vector2 origin;
        private float radius;

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
            behavior.startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45f;
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            float progress = 1f - (remaining / duration);
            
            // The visual sweep matches the authoritative 90-degree melee arc.
            float currentAngle = startAngle + (progress * sweepAngle);
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
            
            // Arc moves outward slightly during swing
            Vector2 radial = Quaternion.Euler(0f, 0f, currentAngle) * Vector2.right;
            transform.position = origin + (radial * (radius * 0.4f * progress));

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
