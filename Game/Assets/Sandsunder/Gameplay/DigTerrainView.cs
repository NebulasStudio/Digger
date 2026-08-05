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
        private const int OverlaySortingOrder = 5;

        public static DigTerrainView Instance { get; private set; }

        [SerializeField] private int poolPrealloc = 256;

        private readonly Dictionary<Vector2Int, SpriteRenderer> _overlays = new();
        private readonly Stack<SpriteRenderer> _pool = new();

        // Cached stage sprites (fall back to procedural tiles from the art factory).
        private Sprite _intact;
        private Sprite _cracked;
        private Sprite _opened;

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
        }

        /// <summary>Apply a cell's excavation stage (0 = intact, 1 = cracked, 2 = opened/pit).</summary>
        public void SetCellDepth(Vector2 cellCenter, int depth)
        {
            Vector2Int key = new(Mathf.RoundToInt(cellCenter.x * 2f), Mathf.RoundToInt(cellCenter.y * 2f));

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
            renderer.gameObject.SetActive(true);
            _overlays[key] = renderer;
        }

        private SpriteRenderer CreatePooledRenderer()
        {
            GameObject go = new("DigOverlay");
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = OverlaySortingOrder;
            renderer.filterMode = FilterMode.Point;
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
            switch (depth)
            {
                case 1: return _cracked ?? _intact;
                case 2: return _opened ?? _cracked ?? _intact;
                default: return _intact;
            }
        }

        private void LoadStageSprites()
        {
#if UNITY_EDITOR
            // The stage sprites are generated as assets by the editor art factory; load them
            // directly (no dependency from this runtime assembly onto the Editor assembly).
            _intact = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Generated/DigIntactSprite.asset");
            _cracked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Generated/DigCrackedSprite.asset");
            _opened = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Generated/DigOpenedSprite.asset");
#else
            // Runtime fallback: no editor-only assets; caller wires sprites via SetStageSprites().
#endif
        }

        /// <summary>Optional runtime wiring when the editor factory is unavailable.</summary>
        public void SetStageSprites(Sprite intact, Sprite cracked, Sprite opened)
        {
            _intact = intact;
            _cracked = cracked;
            _opened = opened;
        }
    }
}