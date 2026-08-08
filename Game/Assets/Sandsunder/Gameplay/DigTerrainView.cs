using System.Collections.Generic;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Renders the 3-stage dynamic sand deformation (Intact -> Cracked -> Opened/Pit) as a
    /// persistent per-cell overlay, tightly integrated with the sand base color.
    ///
    /// This is a PURELY PRESENTATIONAL layer: it never reads from or writes to the authoritative
    /// simulation. It only consumes the per-cell depth reported by the (server-owned) DigGrid via
    /// PrototypeDigGridAuthority -- the same channel that already drives SandboxPitDecal.
    ///
    /// Overlay sprites are pooled and reused; sortingOrder sits between the ground and entities so
    /// the excavation reads as carved into the sand.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class DigTerrainView : MonoBehaviour
    {
        private const int OverlaySortingOrder = 10;

        public static DigTerrainView Instance { get; private set; }

        [SerializeField] private int poolPrealloc = 256;
        [SerializeField] private Sprite intactSprite;
        [SerializeField] private Sprite crackedSprite;
        [SerializeField] private Sprite openedSprite;

        private readonly Dictionary<Vector2Int, SpriteRenderer> _overlays = new();
        private readonly Stack<SpriteRenderer> _pool = new();
        private readonly List<Object> _runtimeAssets = new();

        // Cached stage sprites (fall back to procedural tiles from the art factory).
        private Sprite _intact;
        private Sprite _cracked;
        private Sprite _opened;

        public int ActiveCellCount => _overlays.Count;

        private void Awake()
        {
            Instance = this;
            LoadStageSprites();
            for (int i = 0; i < poolPrealloc; i++)
            {
                _pool.Push(CreatePooledRenderer());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            for (int index = 0; index < _runtimeAssets.Count; index++)
            {
                if (_runtimeAssets[index] != null)
                {
                    Destroy(_runtimeAssets[index]);
                }
            }
        }

        /// <summary>Apply a cell's excavation stage (0 = intact, 1 = cracked, 2 = opened/pit).</summary>
        public void SetCellDepth(Vector2 cellCenter, int depth)
        {
            Vector2Int key = new(Mathf.FloorToInt(cellCenter.x), Mathf.FloorToInt(cellCenter.y));

            // Depth 0 -> restore the sand: release the overlay sprite back to the pool.
            if (depth <= 0)
            {
                if (_overlays.TryGetValue(key, out SpriteRenderer existing))
                {
                    Release(existing);
                    _overlays.Remove(key);
                }
                return;
            }

            Sprite stage = StageSprite(depth);
            if (stage == null) return;

            if (_overlays.TryGetValue(key, out SpriteRenderer renderer))
            {
                renderer.sprite = stage;
                return;
            }

            renderer = _pool.Count > 0 ? _pool.Pop() : CreatePooledRenderer();
            renderer.sprite = stage;
            renderer.transform.position = new Vector3(cellCenter.x, cellCenter.y, 0f);
            renderer.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            renderer.gameObject.SetActive(true);
            _overlays[key] = renderer;
        }

        private SpriteRenderer CreatePooledRenderer()
        {
            GameObject go = new("DigOverlay");
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = OverlaySortingOrder;
            go.SetActive(false);
            return renderer;
        }

        private void Release(SpriteRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            _pool.Push(renderer);
        }

        private Sprite StageSprite(int depth)
        {
            return depth >= 2
                ? _opened ?? _cracked ?? _intact
                : _cracked ?? _intact;
        }

        private void LoadStageSprites()
        {
            _intact = intactSprite;
            _cracked = crackedSprite;
            _opened = openedSprite;

            // Build-safe fallback for dynamically created scene roots. No UnityEditor API or
            // generated asset path is required in a player build.
            _intact ??= CreateRuntimeStageSprite(0);
            _cracked ??= CreateRuntimeStageSprite(1);
            _opened ??= CreateRuntimeStageSprite(2);
        }

        /// <summary>Optional runtime wiring when the editor factory is unavailable.</summary>
        public void SetStageSprites(Sprite intact, Sprite cracked, Sprite opened)
        {
            intactSprite = intact;
            crackedSprite = cracked;
            openedSprite = opened;
            _intact = intact;
            _cracked = cracked;
            _opened = opened;
        }

        private Sprite CreateRuntimeStageSprite(int stage)
        {
            const int Size = 32;
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = $"DigStage{stage}RuntimeTexture"
            };

            Color clear = Color.clear;
            Color crack = new(0.20f, 0.11f, 0.05f, 0.92f);
            Color pit = new(0.30f, 0.19f, 0.10f, 0.90f);
            Vector2 center = new(15.5f, 15.5f);
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool fissure = (x == 15 || y == 15 || x == y || x + y == 31) && distance <= 13f;
                Color pixel = stage == 0
                    ? clear
                    : stage == 1
                        ? (fissure ? crack : clear)
                        : (distance <= 10f ? pit : (fissure ? crack : clear));
                texture.SetPixel(x, y, pixel);
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, Size, Size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 32f);
            sprite.name = $"DigStage{stage}RuntimeSprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _runtimeAssets.Add(sprite);
            _runtimeAssets.Add(texture);
            return sprite;
        }
    }
}
