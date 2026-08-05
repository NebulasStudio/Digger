using System;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Runtime owner of the player's excavation depth. It is ALWAYS a presentation-layer projection
    /// of the server-owned dig state (see ADR-0001): the server decides depth; this system relays it
    /// to the visuals and the stealth rules.
    ///
    /// Reaching depth >= 2 takes the Nomad down to the subterranean level (-1). Depth 0 is the
    /// surface. The only consumer that decides the tunel layer is this system.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class DigDepthSystem : MonoBehaviour
    {
        private const int SubterraneanThresholdDepth = 2;

        public static DigDepthSystem Instance { get; private set; }

        public int CurrentDepth { get; private set; } = 0;

        /// <summary>True when the Nomad is subsurface (depth >= 1), i.e. fully underground.</summary>
        public bool IsSubterranean => CurrentDepth >= 1;

        public event Action<int> DepthChanged;
        public event Action<bool> SubterraneanChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Space))
            {
                if (IsSubterranean)
                {
                    ReturnToSurface();
                }
                else
                {
                    RaiseDepth(2);
                }
            }
        }

        /// <summary>Called by the dig channel completion (server-authorized depth gain).</summary>
        public void RaiseDepth(int by = 2)
        {
            SetDepth(CurrentDepth + by);
        }

        public void SetDepth(int depth)
        {
            depth = Mathf.Max(0, depth);
            if (depth == CurrentDepth) return;

            bool wasSub = IsSubterranean;
            CurrentDepth = depth;
            DepthChanged?.Invoke(depth);

            if (wasSub != IsSubterranean)
            {
                NotifyLayerTransition();
                SubterraneanChanged?.Invoke(IsSubterranean);
            }
        }

        /// <summary>Depth required to leave the tunnels back to surface level 0.</summary>
        public void ReturnToSurface()
        {
            SetDepth(0);
        }

        private void NotifyLayerTransition()
        {
            if (PrototypeTunnelSystem.Instance != null)
            {
                MatrixLayerDepth layer = IsSubterranean
                    ? MatrixLayerDepth.Subterranean_L1
                    : MatrixLayerDepth.Surface_L0;
                PrototypeTunnelSystem.Instance.TransitionToLayer(layer);
            }
        }
    }
}