using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Continuous sand-dust particle emission while the player holds the Right Mouse Button digging
    /// with the shovel. Wraps a dedicated ParticleSystem so the burst of excavation dust is
    /// decoupled from the one-shot SandboxVisualEffects.SpawnDust.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class SandDustEmitter : MonoBehaviour
    {
        public static SandDustEmitter Instance { get; private set; }

        [SerializeField] private Color sandTint = new(0.86f, 0.70f, 0.43f);
        [SerializeField] private float emissionPerSecond = 32f;

        private ParticleSystem dust;
        private ParticleSystem.EmissionModule emission;
        private bool active;

        private void Awake()
        {
            Instance = this;
            ParticleSystem existing = GetComponent<ParticleSystem>();
            if (existing != null)
            {
                dust = existing;
            }
            else
            {
                dust = CreateParticleSystem();
            }

            emission = dust.emission;
            emission.rateOverTime = 0f;
            var main = dust.main;
            main.startColor = new ParticleSystem.MinMaxGradient(sandTint);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private ParticleSystem CreateParticleSystem()
        {
            GameObject go = new("SandDustEmitter");
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 0.45f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.gravityModifier = -0.15f; // gentle upward drift, soft fall
            main.maxParticles = 200;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(sandTint, 0f), new GradientColorKey(sandTint, 0.6f), new GradientColorKey(sandTint, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 12;

            return ps;
        }

        /// <summary>Enable/disable continuous dust at a dig center while channeling.</summary>
        public void SetChanneling(bool isChanneling, Vector2 worldCenter)
        {
            if (active == isChanneling) return;
            active = isChanneling;

            if (active)
            {
                dust.transform.position = worldCenter;
                emission.rateOverTime = emissionPerSecond;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }
    }
}