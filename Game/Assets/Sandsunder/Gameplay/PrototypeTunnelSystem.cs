using UnityEngine;

namespace Sandsunder.Gameplay
{
    public enum MatrixLayerDepth
    {
        Surface_L0 = 0,
        Subterranean_L1 = -1,
        RuneVault_L2 = -2
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeTunnelSystem : MonoBehaviour
    {
        public static PrototypeTunnelSystem Instance { get; private set; }

        public MatrixLayerDepth CurrentLayer { get; private set; } = MatrixLayerDepth.Surface_L0;

        private Camera mainCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindFirstObjectByType<PrototypeTunnelSystem>() == null)
            {
                GameObject tunnelObj = new("PrototypeTunnelSystem_Auto");
                tunnelObj.AddComponent<PrototypeTunnelSystem>();
            }
        }

        private void Awake()
        {
            Instance = this;
            mainCamera = Camera.main;
        }

        public void TransitionToLayer(MatrixLayerDepth targetLayer)
        {
            CurrentLayer = targetLayer;
            Debug.Log($"[TunnelSystem] Transizione al layer matriciale: {targetLayer}");

            if (mainCamera == null) mainCamera = Camera.main;

            switch (targetLayer)
            {
                case MatrixLayerDepth.Surface_L0:
                    if (mainCamera != null) mainCamera.backgroundColor = new Color(0.86f, 0.70f, 0.43f);
                    RenderSettings.ambientLight = new Color(0.95f, 0.85f, 0.70f);
                    break;
                case MatrixLayerDepth.Subterranean_L1:
                    if (mainCamera != null) mainCamera.backgroundColor = new Color(0.18f, 0.14f, 0.10f);
                    RenderSettings.ambientLight = new Color(0.40f, 0.32f, 0.22f);
                    break;
                case MatrixLayerDepth.RuneVault_L2:
                    if (mainCamera != null) mainCamera.backgroundColor = new Color(0.05f, 0.12f, 0.18f);
                    RenderSettings.ambientLight = new Color(0.15f, 0.45f, 0.55f);
                    break;
            }

            SandboxVisualEffects.SpawnDust(Vector3.zero, 30, new Color(0.20f, 0.90f, 0.85f, 0.8f));
        }

        public void ToggleNextLayer()
        {
            MatrixLayerDepth next = CurrentLayer switch
            {
                MatrixLayerDepth.Surface_L0 => MatrixLayerDepth.Subterranean_L1,
                MatrixLayerDepth.Subterranean_L1 => MatrixLayerDepth.RuneVault_L2,
                _ => MatrixLayerDepth.Surface_L0
            };
            TransitionToLayer(next);
        }
    }
}
