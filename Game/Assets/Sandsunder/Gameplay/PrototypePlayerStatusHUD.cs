using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Compatibility marker retained for older scenes. Status presentation now belongs to the
    /// single <see cref="SandboxModernHUD"/> Canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerStatusHUD : MonoBehaviour
    {
        public static PrototypePlayerStatusHUD Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
