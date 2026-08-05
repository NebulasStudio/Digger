using System;
using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed partial class PrototypeDigGridAuthority : MonoBehaviour
    {
        public static PrototypeDigGridAuthority Instance { get; private set; }

        public const string CounterLootId = "prototype_counter";
        public const string HealLootId = "prototype_heal";
        public const string KeyLootId = "key.subterranean";
        public const string ShovelLootId = "shovel.default";

        [SerializeField]
        private int mapSeed = 1337;

        private DigGrid grid;
        private static int nextPickupId = 300000;

        public CombatDigNodeState CreateNodeState(int x, int y)
        {
            EnsureGrid();
            int safeX = Mathf.Clamp(x + 32, 0, 63);
            int safeY = Mathf.Clamp(y + 32, 0, 63);
            return new CombatDigNodeState(
                grid,
                safeX,
                safeY,
                CombatRules.PrototypeOne.DigStrikesRequired);
        }

        public DigResult TryDigAtWorldPosition(Vector2 worldPos)
        {
            EnsureGrid();

            // Check if attempting to dig hard stone structures/ruins
            Collider2D stoneHit = Physics2D.OverlapCircle(worldPos, 0.38f);
            if (stoneHit != null && !stoneHit.isTrigger && (stoneHit.name.Contains("Wall") || stoneHit.name.Contains("Ruin") || stoneHit.GetComponent<PrototypeDesertRuinDoor>() != null))
            {
                SandboxVisualEffects.SpawnDust(worldPos, 10, new Color(0.95f, 0.85f, 0.40f));
                Debug.Log("[DigGrid] PIETRA DURA - IMPOSSIBILE SCAVARE SULLE ROVINE IN PIETRA!");
                return default;
            }

            int gridX = Mathf.Clamp(Mathf.FloorToInt(worldPos.x) + 32, 0, 63);
            int gridY = Mathf.Clamp(Mathf.FloorToInt(worldPos.y) + 32, 0, 63);

            DigResult result = grid.Dig(new Domain.GridCell(gridX, gridY));
            if (result.Changed)
            {
                SandboxVisualEffects.SpawnDust(worldPos, 6, new Color(0.72f, 0.52f, 0.28f));
                Vector2 cellCenter = new Vector2(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f);
                SandboxPitDecal.SpawnAt(cellCenter, result.NewDepth);

                // Feature 1 — Dynamic Sand Excavation & Crepe Cracks:
                // per-cell 3-stage overlay (Intact -> Cracked -> Opened/Pit) + live starburst cracks.
                DigTerrainView.Instance?.SetCellDepth(cellCenter, result.NewDepth);
                SandCrepeCracksFX.Instance?.SpawnStarburst(cellCenter, result.NewDepth);

                if (!string.IsNullOrEmpty(result.RevealedLootId))
                {
                    PrototypePickup.Spawn(worldPos, nextPickupId++, result.RevealedLootId);
                }
            }
            return result;
        }

        private void Awake()
        {
            Instance = this;
            EnsureGrid();
        }

        private void EnsureGrid()
        {
            if (grid == null)
            {
                grid = new DigGrid(
                    width: 64,
                    height: 64,
                    unchecked((ulong)mapSeed),
                    new[] { CounterLootId, HealLootId, KeyLootId, ShovelLootId },
                    emptyWeight: 1);
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed partial class PrototypeDigNode
    {
        private static int nextPickupId = 200000;

        [SerializeField]
        private PrototypeDigGridAuthority authority;

        [SerializeField]
        private int cellX;

        [SerializeField]
        private int cellY;

        private CombatDigNodeState state;

        public event Action<PrototypeDigNode, CombatDigStrikeResult> Struck;
        public event Action<PrototypeDigNode, CombatDigStrikeResult> Revealed;

        public int StrikesRemaining => State.StrikesRemaining;
        public bool IsRevealed => State.IsRevealed;

        public void Configure(PrototypeDigGridAuthority configuredAuthority, int x, int y)
        {
            authority = configuredAuthority;
            cellX = x;
            cellY = y;
            state = null;
        }

        public CombatDigStrikeResult Strike()
        {
            CombatDigStrikeResult result = State.Strike();
            if (result.Changed)
            {
                GetComponent<SandboxDigVisual>()?.PlayStrike(result.StrikesRemaining);
                Struck?.Invoke(this, result);
            }

            if (!result.RevealedNow)
            {
                return result;
            }

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.color = new Color(0.48f, 0.31f, 0.18f, 0.55f);
            GetComponent<SandboxDigVisual>()?.PlayReveal();
            Revealed?.Invoke(this, result);
            if (!string.IsNullOrEmpty(result.RevealedLootId))
            {
                PrototypePickup.Spawn(transform.position, nextPickupId++, result.RevealedLootId);
            }

            return result;
        }

        private CombatDigNodeState State
        {
            get
            {
                if (state == null)
                {
                    if (authority == null)
                    {
                        authority = FindFirstObjectByType<PrototypeDigGridAuthority>();
                    }

                    state = authority.CreateNodeState(cellX, cellY);
                }

                return state;
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed partial class PrototypePickup
    {
        private CombatPickupState state;

        public string LootId => state?.LootId;
        public bool IsCollected => state != null && state.IsCollected;

        public static PrototypePickup Spawn(Vector2 position, int pickupId, string lootId)
        {
            GameObject pickupObject = new($"Pickup {lootId}");
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = Vector3.one * 0.48f;
            SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 15;
            Sprite pickupSprite = null;
#if UNITY_EDITOR
            pickupSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Runtime/Environment/env_relic_chest_32.png");
#endif
            Color color = lootId == PrototypeDigGridAuthority.HealLootId
                ? new Color(0.47f, 0.78f, 0.43f)
                : new Color(0.39f, 0.96f, 0.9f);

            if (pickupSprite != null)
            {
                renderer.sprite = pickupSprite;
                renderer.color = Color.white;
            }
            else
            {
                PrototypePixelArt art = pickupObject.AddComponent<PrototypePixelArt>();
                art.Configure(PrototypePixelKind.Pickup, color);
            }
            CircleCollider2D collider = pickupObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.65f;
            PrototypePickup pickup = pickupObject.AddComponent<PrototypePickup>();
            pickup.Configure(pickupId, lootId);
            SandboxPickupVisual pickupVisual = pickupObject.AddComponent<SandboxPickupVisual>();
            pickupVisual.Configure(renderer, color);
            return pickup;
        }

        public void Configure(int pickupId, string lootId)
        {
            state = new CombatPickupState(pickupId, lootId);
        }

        public bool TryCollect(PrototypePlayerCombat player)
        {
            if (player == null || state == null)
            {
                return false;
            }

            // Feature 2 — Interaction rule: surface chests/objects cannot be collected while the
            // Nomad is in the subterranean layer (-1). Return to Level 0 first.
            if (DigDepthSystem.Instance != null && DigDepthSystem.Instance.IsSubterranean)
            {
                return false;
            }

            CombatPickupResult result = state.TryCollect(player.GetComponent<PrototypeHealth>().EntityId);
            if (!result.Changed)
            {
                return false;
            }

            player.AcceptPickup(result.LootId);
            GetComponent<SandboxPickupVisual>()?.PlayCollect();
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PrototypePlayerCombat player = other.GetComponentInParent<PrototypePlayerCombat>();
            if (TryCollect(player))
            {
                Destroy(gameObject);
            }
        }
    }
}
