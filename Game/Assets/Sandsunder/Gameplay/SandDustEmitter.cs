using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Continuous sand-dust particle emission while the player holds the Right Mouse Button digging
    /// with the shovel. Decoupled from Unity's optional ParticleSystem module.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class SandDustEmitter : MonoBehaviour
    {
        public static SandDustEmitter Instance { get; private set; }

        [SerializeField] private Color sandTint = new(0.86f, 0.70f, 0.43f);
        [SerializeField] private float emissionPerSecond = 16f;

        private bool active;
        private Vector2 targetPosition;
        private float emitTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!active) return;

            emitTimer += Time.deltaTime * emissionPerSecond;
            while (emitTimer >= 1f)
            {
                emitTimer -= 1f;
                SandboxVisualEffects.SpawnDust(targetPosition + (Random.insideUnitCircle * 0.18f), 1, sandTint);
            }
        }

        /// <summary>Enable/disable continuous dust at a dig center while channeling.</summary>
        public void SetChanneling(bool isChanneling, Vector2 worldCenter)
        {
            active = isChanneling;
            targetPosition = worldCenter;
            if (!active)
            {
                emitTimer = 0f;
            }
        }
    }
}