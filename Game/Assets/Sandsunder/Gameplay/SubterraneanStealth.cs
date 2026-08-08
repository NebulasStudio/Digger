using UnityEngine;
using Sandsunder.Simulation;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Subterranean stealth (Tunnel Level -1):
    ///  - The Nomad is 100% invisible and untargetable by surface projectiles and enemies (Dune Spitter).
    ///  - Surface projectiles overfly the underground player (no damage, no hit).
    ///  - Surface chests/objects cannot be interacted with while underground.
    ///  - Rendering: translucent cyan silhouette (#00F0E6), opacity 65%, sortingOrder -10, visible
    ///    through the excavated sand to convey floating/subterranean sliding.
    ///
    /// This component reads the depth from DigDepthSystem and applies presentation + rule effects.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public sealed class SubterraneanStealth : MonoBehaviour
    {
        [SerializeField] private float silhouetteOpacity = 0.65f;
        [SerializeField] private Color silhouetteColor = new(0f, 0.94f, 0.90f); // #00F0E6
        [SerializeField] private int silhouetteSortingOrder = -10;

        private SpriteRenderer[] cachedRenderers;
        private int[] baseSortingOrders;
        private Color[] baseColors;
        private bool isStealthed;

        /// <summary>True while the Nomad is underground and immune to surface attacks.</summary>
        public bool IsStealthed => isStealthed;

        private void OnEnable()
        {
            if (DigDepthSystem.Instance != null)
            {
                ApplyDepth(DigDepthSystem.Instance.CurrentDepth);
                DigDepthSystem.Instance.DepthChanged += OnDepthChanged;
            }
        }

        private void OnDisable()
        {
            if (DigDepthSystem.Instance != null)
            {
                DigDepthSystem.Instance.DepthChanged -= OnDepthChanged;
            }
        }

        private void Start()
        {
            CacheRenderers();
            ApplyDepth(DigDepthSystem.Instance?.CurrentDepth ?? 0, force: true);
        }

        private void OnDepthChanged(int depth)
        {
            ApplyDepth(depth);
        }

        private void ApplyDepth(int depth, bool force = false)
        {
            bool stealth = depth >= PlayerDepthState.SubterraneanThresholdDepth;
            if (!force && stealth == isStealthed) return;
            isStealthed = stealth;

            CacheRenderers();
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer renderer = cachedRenderers[i];
                if (renderer == null) continue;

                if (isStealthed)
                {
                    baseSortingOrders[i] = renderer.sortingOrder;
                    renderer.color = new Color(silhouetteColor.r, silhouetteColor.g, silhouetteColor.b, silhouetteOpacity);
                    renderer.sortingOrder = silhouetteSortingOrder;
                }
                else
                {
                    renderer.color = baseColors[i];
                    renderer.sortingOrder = baseSortingOrders[i];
                }
            }
        }

        private void CacheRenderers()
        {
            if (cachedRenderers != null && cachedRenderers.Length > 0) return;
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            baseSortingOrders = new int[cachedRenderers.Length];
            baseColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                baseSortingOrders[i] = cachedRenderers[i].sortingOrder;
                baseColors[i] = cachedRenderers[i].color;
            }
        }
    }
}
