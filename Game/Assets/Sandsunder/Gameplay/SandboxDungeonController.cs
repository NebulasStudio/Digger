using System;
using System.Collections.Generic;
using Sandsunder.Domain;
using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Applies complete dungeon depth/layer snapshots through DigDepthSystem.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxDungeonController : MonoBehaviour
    {
        public const int SurfaceDepth = 0;
        public const int DungeonDepth = 2;

        private const float CellSpacing = 1.25f;
        private const float WalkableHalfExtent = 0.50f;
        private const float WallThickness = 0.12f;

        [SerializeField] private DigDepthSystem depthSource;
        [SerializeField] private PrototypeTunnelSystem layerSource;
        [Header("Optional reviewed art; deterministic runtime fallbacks are used when empty.")]
        [SerializeField] private Sprite floorSprite;
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite entranceSprite;
        [SerializeField] private Sprite chestSprite;

        private Transform dungeonRoot;
        private GameObject surfaceEntranceObject;
        private Vector2 surfaceWorldEntry;
        private readonly List<SpriteRenderer> dungeonRenderers = new();
        private PrototypePlayerCombat trackedPlayer;

        private const float NearbyRevealRadius = 3.10f;

        private static Sprite fallbackFloorSprite;
        private static Sprite fallbackWallSprite;
        private static Sprite fallbackEntranceSprite;
        private static Sprite fallbackChestSprite;
        private static Sprite fallbackGuideSprite;

        public static SandboxDungeonController Instance { get; private set; }
        public SandboxDungeonRuntime Runtime { get; private set; }
        public SandboxDungeonLayout Layout => Runtime?.Layout;
        public Transform DungeonRoot => dungeonRoot;
        public Vector2 SurfaceWorldEntry => surfaceWorldEntry;
        public int MatchGold => Runtime?.MatchGold ?? 0;
        public int CurrentDepth => ResolveDepthSource()?.CurrentDepth ?? SurfaceDepth;
        public bool IsInsideDungeon => ResolveDepthSource()?.IsSubterranean == true;
        public MatrixLayerDepth CurrentLayer => ResolveLayerSource()?.CurrentLayer
            ?? (IsInsideDungeon ? MatrixLayerDepth.Subterranean_L1 : MatrixLayerDepth.Surface_L0);

        public event Action<bool> DungeonStateChanged;
        public event Action<int> MatchGoldChanged;

        public static SandboxDungeonController EnsureInstance()
        {
            if (Instance != null) return Instance;
            SandboxDungeonController existing = FindFirstObjectByType<SandboxDungeonController>();
            if (existing != null) return existing;
            return new GameObject(nameof(SandboxDungeonController)).AddComponent<SandboxDungeonController>();
        }

        public void Configure(DigDepthSystem configuredDepthSource, PrototypeTunnelSystem configuredLayerSource)
        {
            if (Instance == null) Instance = this;
            depthSource = configuredDepthSource;
            layerSource = configuredLayerSource;
        }

        public void ConfigureArt(
            Sprite configuredFloorSprite,
            Sprite configuredWallSprite,
            Sprite configuredEntranceSprite,
            Sprite configuredChestSprite)
        {
            floorSprite = configuredFloorSprite != null ? configuredFloorSprite : floorSprite;
            wallSprite = configuredWallSprite != null ? configuredWallSprite : wallSprite;
            entranceSprite = configuredEntranceSprite != null ? configuredEntranceSprite : entranceSprite;
            chestSprite = configuredChestSprite != null ? configuredChestSprite : chestSprite;
        }

        public void BuildDungeon(ulong mapSeed, GridCell surfaceEntranceCell, Vector2 worldEntry)
        {
            ConfigureDungeon(mapSeed, surfaceEntranceCell, worldEntry);
        }

        public void ConfigureDungeon(ulong mapSeed, GridCell surfaceEntranceCell, Vector2 worldEntry)
        {
            ConfigureDungeon(SandboxDungeonLayout.Create(mapSeed, surfaceEntranceCell), worldEntry);
        }

        /// <summary>
        /// Projects an already-authorized layout. Downstream presentation never receives the map seed.
        /// </summary>
        public void ConfigureDungeon(SandboxDungeonLayout layout, Vector2 worldEntry)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (Instance == null) Instance = this;
            surfaceWorldEntry = worldEntry;
            Runtime = new SandboxDungeonRuntime(layout);

            ClearGeneratedDungeon();
            BuildSurfaceEntrance();
            BuildDungeonProjection();
            ApplyRootVisibility(false);
        }

        public void ConfigureDungeon(
            ulong mapSeed,
            GridCell surfaceEntranceCell,
            Vector2 worldEntry,
            Sprite configuredFloorSprite,
            Sprite configuredWallSprite,
            Sprite configuredEntranceSprite,
            Sprite configuredChestSprite)
        {
            ConfigureArt(configuredFloorSprite, configuredWallSprite, configuredEntranceSprite, configuredChestSprite);
            ConfigureDungeon(SandboxDungeonLayout.Create(mapSeed, surfaceEntranceCell), worldEntry);
        }

        public bool EnterDungeon()
        {
            return EnterDungeon(null);
        }

        public bool EnterDungeon(PrototypePlayerCombat player)
        {
            if (Runtime == null || !Runtime.Enter()) return false;

            if (!ApplyDungeonState(DungeonDepth, MatrixLayerDepth.Subterranean_L1))
            {
                Runtime?.Exit();
                return false;
            }

            ApplyRootVisibility(true);
            MovePlayer(player, WorldPositionForCell(Layout.EntranceCell));
            if (player != null) ApplyProximityVisibilityForTesting(player.transform.position);
            return true;
        }

        public bool ExitDungeon()
        {
            return ExitDungeon(null);
        }

        public bool ExitDungeon(PrototypePlayerCombat player)
        {
            if (Runtime == null || !Runtime.Exit()) return false;

            if (!ApplyDungeonState(SurfaceDepth, MatrixLayerDepth.Surface_L0))
            {
                Runtime?.Enter();
                return false;
            }

            ApplyRootVisibility(false);
            MovePlayer(player, surfaceWorldEntry);
            return true;
        }

        public bool TryClaimFinalChest(out int awardedGold)
        {
            awardedGold = 0;
            if (Runtime == null || !Runtime.TryClaimFinalChest(out awardedGold)) return false;
            MatchGoldChanged?.Invoke(Runtime.MatchGold);
            return true;
        }

        public Vector2 WorldPositionForCell(GridCell localCell)
        {
            return surfaceWorldEntry + new Vector2(localCell.X * CellSpacing, localCell.Y * CellSpacing);
        }

        public bool IsWorldPositionInsideWalkableBounds(Vector2 worldPosition)
        {
            if (Layout == null) return false;
            for (int index = 0; index < Layout.OrderedPathCells.Count; index++)
            {
                Vector2 offset = worldPosition - WorldPositionForCell(Layout.OrderedPathCells[index]);
                if (Mathf.Abs(offset.x) <= WalkableHalfExtent
                    && Mathf.Abs(offset.y) <= WalkableHalfExtent)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Projects a world point into the union of reviewed corridor cell bounds.</summary>
        public Vector2 ConfineWorldPosition(Vector2 worldPosition)
        {
            if (Layout == null) return worldPosition;

            Vector2 confined = WorldPositionForCell(Layout.OrderedPathCells[0]);
            float nearestDistanceSquared = float.PositiveInfinity;
            for (int index = 0; index < Layout.OrderedPathCells.Count; index++)
            {
                Vector2 center = WorldPositionForCell(Layout.OrderedPathCells[index]);
                Vector2 candidate = new(
                    Mathf.Clamp(worldPosition.x, center.x - WalkableHalfExtent, center.x + WalkableHalfExtent),
                    Mathf.Clamp(worldPosition.y, center.y - WalkableHalfExtent, center.y + WalkableHalfExtent));
                float distanceSquared = (worldPosition - candidate).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared) continue;
                confined = candidate;
                nearestDistanceSquared = distanceSquared;
            }

            return confined;
        }

        /// <summary>Points from the nearest path node toward the next node/chest.</summary>
        public Vector2 GetGuidanceDirection(Vector2 worldPosition)
        {
            if (Layout == null || Layout.OrderedPathCells.Count < 2) return Vector2.zero;

            int nearestIndex = 0;
            float nearestDistanceSquared = float.PositiveInfinity;
            for (int index = 0; index < Layout.OrderedPathCells.Count; index++)
            {
                float distanceSquared = (worldPosition - WorldPositionForCell(Layout.OrderedPathCells[index])).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared) continue;
                nearestDistanceSquared = distanceSquared;
                nearestIndex = index;
            }

            if (nearestIndex >= Layout.OrderedPathCells.Count - 1) return Vector2.zero;
            Vector2 next = WorldPositionForCell(Layout.OrderedPathCells[nearestIndex + 1]);
            Vector2 direction = next - worldPosition;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        }

        /// <summary>Shows only the corridor, guide, exit, and chest near the current explorer.</summary>
        public int ApplyProximityVisibilityForTesting(Vector2 viewerWorldPosition)
        {
            if (dungeonRoot == null) return 0;

            float radiusSquared = NearbyRevealRadius * NearbyRevealRadius;
            int visibleCount = 0;
            for (int index = 0; index < dungeonRenderers.Count; index++)
            {
                SpriteRenderer renderer = dungeonRenderers[index];
                if (renderer == null) continue;
                bool visible = ((Vector2)renderer.transform.position - viewerWorldPosition).sqrMagnitude <= radiusSquared;
                renderer.enabled = visible;
                if (visible) visibleCount++;
            }

            return visibleCount;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            ResolveDepthSource();
            ResolveLayerSource();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            if (!IsInsideDungeon || dungeonRoot == null || !dungeonRoot.gameObject.activeInHierarchy) return;
            trackedPlayer = trackedPlayer != null ? trackedPlayer : FindFirstObjectByType<PrototypePlayerCombat>();
            if (trackedPlayer != null) ApplyProximityVisibilityForTesting(trackedPlayer.transform.position);
        }

        private void BuildSurfaceEntrance()
        {
            surfaceEntranceObject = CreateSpriteObject(
                "Dungeon Surface Entrance",
                transform,
                surfaceWorldEntry,
                entranceSprite != null ? entranceSprite : GetFallbackEntranceSprite(),
                Vector2.one,
                18,
                new Color(0.90f, 0.76f, 0.43f));
            SandboxDungeonEntrance entrance = surfaceEntranceObject.AddComponent<SandboxDungeonEntrance>();
            entrance.Configure(this, configuredAsExit: false);
            SandboxInteractionController.Register(entrance);
        }

        private void BuildDungeonProjection()
        {
            GameObject rootObject = new("SandboxDungeon_RuntimeRoot");
            rootObject.transform.SetParent(transform, false);
            dungeonRoot = rootObject.transform;

            foreach (GridCell cell in Layout.OrderedPathCells)
            {
                BuildFloorCell(cell);
                BuildWalls(cell);
            }

            for (int index = 0; index < Layout.OrderedPathCells.Count - 1; index++)
            {
                GridCell current = Layout.OrderedPathCells[index];
                GridCell next = Layout.OrderedPathCells[index + 1];
                Vector2 from = WorldPositionForCell(current);
                Vector2 to = WorldPositionForCell(next);
                GameObject guide = CreateSpriteObject(
                    $"Guide_{index:00}",
                    dungeonRoot,
                    Vector2.Lerp(from, to, 0.38f),
                    GetFallbackGuideSprite(),
                    new Vector2(0.28f, 0.28f),
                    7,
                    new Color(0.20f, 0.90f, 0.95f, 0.62f));
                guide.transform.up = (to - from).normalized;
            }

            GameObject exitObject = CreateSpriteObject(
                "Dungeon Return Threshold",
                dungeonRoot,
                WorldPositionForCell(Layout.ExitCell),
                entranceSprite != null ? entranceSprite : GetFallbackEntranceSprite(),
                new Vector2(0.82f, 0.82f),
                16,
                new Color(0.20f, 0.90f, 0.95f, 0.85f));
            SandboxDungeonEntrance exit = exitObject.AddComponent<SandboxDungeonEntrance>();
            exit.Configure(this, configuredAsExit: true);
            SandboxInteractionController.Register(exit);

            GameObject chestObject = CreateSpriteObject(
                "Dungeon Final Match Chest",
                dungeonRoot,
                WorldPositionForCell(Layout.ChestEndpointCell),
                chestSprite != null ? chestSprite : GetFallbackChestSprite(),
                new Vector2(0.92f, 0.92f),
                20,
                Color.white);
            SandboxDungeonChest chest = chestObject.AddComponent<SandboxDungeonChest>();
            chest.Configure(this, chestSprite);
            SandboxInteractionController.Register(chest);

            dungeonRenderers.Clear();
            dungeonRenderers.AddRange(dungeonRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true));
        }

        private void BuildFloorCell(GridCell cell)
        {
            CreateSpriteObject(
                $"Floor_{cell.X}_{cell.Y}",
                dungeonRoot,
                WorldPositionForCell(cell),
                floorSprite != null ? floorSprite : GetFallbackFloorSprite(),
                new Vector2(CellSpacing, CellSpacing),
                2,
                new Color(0.34f, 0.26f, 0.18f));
        }

        private void BuildWalls(GridCell cell)
        {
            BuildWallIfExposed(cell, new GridCell(0, 1));
            BuildWallIfExposed(cell, new GridCell(1, 0));
            BuildWallIfExposed(cell, new GridCell(0, -1));
            BuildWallIfExposed(cell, new GridCell(-1, 0));
        }

        private void BuildWallIfExposed(GridCell cell, GridCell direction)
        {
            GridCell neighbour = new(cell.X + direction.X, cell.Y + direction.Y);
            if (Layout.ContainsWalkableCell(neighbour)) return;

            bool horizontal = direction.Y != 0;
            Vector2 center = WorldPositionForCell(cell)
                + new Vector2(direction.X, direction.Y) * (CellSpacing * 0.5f);
            Vector2 size = horizontal
                ? new Vector2(CellSpacing + WallThickness, WallThickness)
                : new Vector2(WallThickness, CellSpacing + WallThickness);
            GameObject wall = CreateSpriteObject(
                $"Wall_{cell.X}_{cell.Y}_{direction.X}_{direction.Y}",
                dungeonRoot,
                center,
                wallSprite != null ? wallSprite : GetFallbackWallSprite(),
                size,
                5,
                new Color(0.66f, 0.51f, 0.34f));
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private void ApplyRootVisibility(bool inside)
        {
            if (dungeonRoot != null) dungeonRoot.gameObject.SetActive(inside);
            if (surfaceEntranceObject != null) surfaceEntranceObject.SetActive(!inside);
        }

        private void ClearGeneratedDungeon()
        {
            dungeonRenderers.Clear();
            DestroyGenerated(surfaceEntranceObject);
            surfaceEntranceObject = null;
            if (dungeonRoot != null) DestroyGenerated(dungeonRoot.gameObject);
            dungeonRoot = null;
        }

        private static void MovePlayer(PrototypePlayerCombat player, Vector2 worldPosition)
        {
            if (player == null) return;
            TopDownPlayerController movement = player.GetComponent<TopDownPlayerController>();
            if (movement != null)
            {
                movement.SetAuthoritativeWorldPosition(worldPosition);
                return;
            }

            player.transform.position = new Vector3(worldPosition.x, worldPosition.y, player.transform.position.z);
        }

        private static void DestroyGenerated(GameObject target)
        {
            if (target == null) return;
            target.SetActive(false);
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private static GameObject CreateSpriteObject(
            string objectName,
            Transform parent,
            Vector2 position,
            Sprite sprite,
            Vector2 worldSize,
            int sortingOrder,
            Color color)
        {
            GameObject created = new(objectName);
            created.transform.SetParent(parent, worldPositionStays: true);
            created.transform.position = new Vector3(position.x, position.y, 0f);
            SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            Vector2 spriteSize = sprite != null ? sprite.bounds.size : Vector2.one;
            created.transform.localScale = new Vector3(
                spriteSize.x > 0f ? worldSize.x / spriteSize.x : worldSize.x,
                spriteSize.y > 0f ? worldSize.y / spriteSize.y : worldSize.y,
                1f);
            return created;
        }

        private static Sprite GetFallbackFloorSprite()
        {
            return fallbackFloorSprite != null
                ? fallbackFloorSprite
                : fallbackFloorSprite = CreateFallbackSprite("dungeon_floor_fallback", new Color32(77, 58, 43, 255), false);
        }

        private static Sprite GetFallbackWallSprite()
        {
            return fallbackWallSprite != null
                ? fallbackWallSprite
                : fallbackWallSprite = CreateFallbackSprite("dungeon_wall_fallback", new Color32(151, 112, 71, 255), false);
        }

        private static Sprite GetFallbackEntranceSprite()
        {
            return fallbackEntranceSprite != null
                ? fallbackEntranceSprite
                : fallbackEntranceSprite = CreateFallbackSprite("dungeon_entrance_fallback", new Color32(184, 143, 88, 255), true);
        }

        private static Sprite GetFallbackChestSprite()
        {
            return fallbackChestSprite != null
                ? fallbackChestSprite
                : fallbackChestSprite = CreateFallbackSprite("dungeon_chest_fallback", new Color32(211, 160, 54, 255), true);
        }

        private static Sprite GetFallbackGuideSprite()
        {
            if (fallbackGuideSprite != null) return fallbackGuideSprite;
            const int size = 8;
            Texture2D texture = NewRuntimeTexture("dungeon_guide_fallback", size);
            Color32 clear = new(0, 0, 0, 0);
            Color32 cyan = new(51, 230, 242, 255);
            for (int y = 0; y < size; y++)
            {
                int halfWidth = Mathf.Max(0, 3 - y / 2);
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, Mathf.Abs(x - 3) <= halfWidth && y >= 2 ? cyan : clear);
            }

            texture.Apply();
            fallbackGuideSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            fallbackGuideSprite.name = "dungeon_guide_fallback";
            return fallbackGuideSprite;
        }

        private static Sprite CreateFallbackSprite(string spriteName, Color32 fill, bool framed)
        {
            const int size = 16;
            Texture2D texture = NewRuntimeTexture(spriteName, size);
            Color32 dark = new(45, 37, 34, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                    texture.SetPixel(x, y, framed && edge ? dark : fill);
                }
            }

            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            return sprite;
        }

        private static Texture2D NewRuntimeTexture(string textureName, int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private bool ApplyDungeonState(int depth, MatrixLayerDepth layer)
        {
            DigDepthSystem depthSystem = ResolveDepthSource();
            if (depthSystem == null) return false;

            bool wasInside = depthSystem.IsSubterranean;
            depthSystem.SetAuthoritativeDepth(depth);

            PrototypeTunnelSystem tunnel = ResolveLayerSource();
            if (tunnel != null && tunnel.CurrentLayer != layer) tunnel.TransitionToLayer(layer);

            bool isInside = depthSystem.IsSubterranean;
            if (wasInside != isInside) DungeonStateChanged?.Invoke(isInside);
            return depthSystem.CurrentDepth == depth && (tunnel == null || tunnel.CurrentLayer == layer);
        }

        private DigDepthSystem ResolveDepthSource()
        {
            depthSource = depthSource != null ? depthSource : DigDepthSystem.Instance;
            depthSource = depthSource != null ? depthSource : FindFirstObjectByType<DigDepthSystem>();
            return depthSource;
        }

        private PrototypeTunnelSystem ResolveLayerSource()
        {
            layerSource = layerSource != null ? layerSource : PrototypeTunnelSystem.Instance;
            layerSource = layerSource != null ? layerSource : FindFirstObjectByType<PrototypeTunnelSystem>();
            return layerSource;
        }
    }
}
