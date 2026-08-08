using System;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Applies complete dungeon depth/layer snapshots through DigDepthSystem.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxDungeonController : MonoBehaviour
    {
        public const int SurfaceDepth = 0;
        public const int DungeonDepth = 2;

        [SerializeField] private DigDepthSystem depthSource;
        [SerializeField] private PrototypeTunnelSystem layerSource;

        public static SandboxDungeonController Instance { get; private set; }
        public int CurrentDepth => ResolveDepthSource()?.CurrentDepth ?? SurfaceDepth;
        public bool IsInsideDungeon => ResolveDepthSource()?.IsSubterranean == true;
        public MatrixLayerDepth CurrentLayer => ResolveLayerSource()?.CurrentLayer
            ?? (IsInsideDungeon ? MatrixLayerDepth.Subterranean_L1 : MatrixLayerDepth.Surface_L0);

        public event Action<bool> DungeonStateChanged;

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

        public bool EnterDungeon()
        {
            return ApplyDungeonState(DungeonDepth, MatrixLayerDepth.Subterranean_L1);
        }

        public bool ExitDungeon()
        {
            return ApplyDungeonState(SurfaceDepth, MatrixLayerDepth.Surface_L0);
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
