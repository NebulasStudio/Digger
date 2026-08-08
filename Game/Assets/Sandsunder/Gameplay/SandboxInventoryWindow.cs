using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Legacy scene bridge. It delegates to the modern modal and never creates a Canvas.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxInventoryWindow : MonoBehaviour
    {
        public static SandboxInventoryWindow Instance { get; private set; }

        public bool IsOpen => SandboxModernHUD.Instance != null
            && SandboxModernHUD.Instance.InventoryController != null
            && SandboxModernHUD.Instance.InventoryController.IsOpen;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ToggleWindow()
        {
            SandboxModernHUD.Instance?.InventoryController?.Toggle();
        }
    }
}
