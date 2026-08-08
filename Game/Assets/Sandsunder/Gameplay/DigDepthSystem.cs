using System;
using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Unity adapter for the single, deterministic <see cref="PlayerDepthState"/>. The server-owned
    /// cell depth is the only input; keyboard shortcuts and presentation code cannot raise depth.
    ///
    /// Reaching depth >= 2 takes the Nomad down to the subterranean level (-1). Depth 0 is the
    /// surface. The only consumer that decides the tunel layer is this system.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class DigDepthSystem : MonoBehaviour
    {
        public static DigDepthSystem Instance { get; private set; }

        private readonly PlayerDepthState state = new();

        public int CurrentDepth => state.CurrentDepth;

        public bool IsSubterranean => state.IsSubterranean;
        public ulong StateHash => state.ComputeStateHash();

        public event Action<int> DepthChanged;
        public event Action<bool> SubterraneanChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Consumes a server-authorized cell result without allowing local depth inflation.</summary>
        public bool ApplyAuthoritativeDigResult(DigResult result)
        {
            return result.Changed && ApplyAuthoritativeCellDepth(result.NewDepth);
        }

        public bool ApplyAuthoritativeCellDepth(int depth)
        {
            bool wasSubterranean = IsSubterranean;
            if (!state.ApplyAuthoritativeCellDepth(depth))
            {
                return false;
            }

            PublishState(wasSubterranean);
            return true;
        }

        /// <summary>Authoritative snapshot/reconnect entrypoint. Local input must never call this.</summary>
        public bool SetAuthoritativeDepth(int depth)
        {
            bool wasSubterranean = IsSubterranean;
            if (!state.SetAuthoritativeDepth(depth))
            {
                return false;
            }

            PublishState(wasSubterranean);
            return true;
        }

        private void PublishState(bool wasSubterranean)
        {
            DepthChanged?.Invoke(CurrentDepth);

            if (wasSubterranean != IsSubterranean)
            {
                NotifyLayerTransition();
                SubterraneanChanged?.Invoke(IsSubterranean);
            }
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
