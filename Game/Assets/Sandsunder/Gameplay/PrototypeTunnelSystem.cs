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

        private Color targetBgColor = new Color(0.86f, 0.70f, 0.43f);
        private Color targetAmbient = new Color(0.95f, 0.85f, 0.70f);
        private float transitionSpeed = 3.5f;

        private void Awake()
        {
            Instance = this;
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = targetBgColor;
            }
            RenderSettings.ambientLight = targetAmbient;
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, targetBgColor, Time.deltaTime * transitionSpeed);
            }
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, Time.deltaTime * transitionSpeed);
        }

        public void TransitionToLayer(MatrixLayerDepth targetLayer)
        {
            CurrentLayer = targetLayer;
            Debug.Log($"[TunnelSystem] Transizione al layer matriciale: {targetLayer}");

            switch (targetLayer)
            {
                case MatrixLayerDepth.Surface_L0:
                    targetBgColor = new Color(0.86f, 0.70f, 0.43f);
                    targetAmbient = new Color(0.95f, 0.85f, 0.70f);
                    break;
                case MatrixLayerDepth.Subterranean_L1:
                    targetBgColor = new Color(0.18f, 0.14f, 0.10f);
                    targetAmbient = new Color(0.40f, 0.32f, 0.22f);
                    break;
                case MatrixLayerDepth.RuneVault_L2:
                    targetBgColor = new Color(0.05f, 0.12f, 0.18f);
                    targetAmbient = new Color(0.15f, 0.45f, 0.55f);
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
