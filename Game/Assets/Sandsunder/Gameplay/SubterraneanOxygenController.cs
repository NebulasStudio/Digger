using System;
using Sandsunder.Gameplay.UI;
using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Unity projection of deterministic oxygen state. Depth and elapsed authoritative ticks are its
    /// only inputs; the HUD is a read-only subscriber through <see cref="IPlayerOxygenProvider"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrototypeHealth))]
    public sealed class SubterraneanOxygenController : MonoBehaviour, IPlayerOxygenProvider
    {
        private const long MicrounitsPerSecond = 1_000_000L;

        private readonly SubterraneanOxygenState state = new(SubterraneanOxygenRules.MilestoneOne);
        [SerializeField]
        private DigDepthSystem depthSource;
        private PrototypeHealth health;
        private long tickAccumulatorMicrounits;

        public float CurrentOxygen => (float)state.OxygenPercent;
        public float MaximumOxygen => SubterraneanOxygenRules.MilestoneOne.MaximumOxygenUnits
            / (float)SubterraneanOxygenRules.UnitsPerPercent;
        public bool IsSubterranean => (depthSource != null ? depthSource : DigDepthSystem.Instance)?.IsSubterranean == true;
        public ulong StateHash => state.ComputeStateHash();

        public event Action<float, float> OxygenChanged;

        /// <summary>Consumes no inventory itself; only applies an already-authorized match grant.</summary>
        public bool TryRestoreFromFlask()
        {
            if (!IsSubterranean || !state.RestorePercent(SubterraneanOxygenRules.OxygenFlaskRestorePercent))
            {
                return false;
            }

            OxygenChanged?.Invoke(CurrentOxygen, MaximumOxygen);
            return true;
        }

        public void ConfigureDepthSource(DigDepthSystem source)
        {
            depthSource = source;
        }

        private void Awake()
        {
            health = GetComponent<PrototypeHealth>();
        }

        private void FixedUpdate()
        {
            AdvanceSimulation(Time.fixedDeltaTime);
        }

        public void AdvanceSimulation(double elapsedSeconds)
        {
            if (elapsedSeconds <= 0d)
            {
                return;
            }

            int previousOxygenUnits = state.OxygenUnits;
            long elapsedMicrounits = (long)Math.Round(
                elapsedSeconds * MicrounitsPerSecond,
                MidpointRounding.AwayFromZero);
            tickAccumulatorMicrounits +=
                elapsedMicrounits * SubterraneanOxygenRules.MilestoneOne.TicksPerSecond;

            while (tickAccumulatorMicrounits >= MicrounitsPerSecond)
            {
                tickAccumulatorMicrounits -= MicrounitsPerSecond;
                int suffocationDamage = state.Step(IsSubterranean);
                if (suffocationDamage > 0)
                {
                    health ??= GetComponent<PrototypeHealth>();
                    health?.ApplyEnvironmentalDamage(suffocationDamage);
                }
            }

            if (previousOxygenUnits != state.OxygenUnits)
            {
                OxygenChanged?.Invoke(CurrentOxygen, MaximumOxygen);
            }
        }
    }
}
