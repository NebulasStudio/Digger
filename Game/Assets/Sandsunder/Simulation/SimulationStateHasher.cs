using System;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Stable non-cryptographic replay checksum. It detects divergent authoritative state;
    /// it is not a signature and must never be used as an anti-tamper primitive.
    /// </summary>
    public static class SimulationStateHasher
    {
        public static ulong Compute(MatchSimulation simulation, DigGrid digGrid)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            if (digGrid == null) throw new ArgumentNullException(nameof(digGrid));
            var hash = StableHash.Offset;
            Add(ref hash, unchecked((ulong)simulation.Tick));
            Add(ref hash, (ulong)simulation.Phase);
            Add(ref hash, simulation.AuthoritativeMapSeed);

            foreach (var player in simulation.Players)
            {
                Add(ref hash, unchecked((ulong)player.Id.Value));
                Add(ref hash, unchecked((ulong)player.SeatIndex));
                Add(ref hash, player.IsAlive ? 1UL : 0UL);
                Add(ref hash, player.AwaitingRespawn ? 1UL : 0UL);
                Add(ref hash, unchecked((ulong)player.RespawnAtTick));
                Add(ref hash, player.IsPermanentlyEliminated ? 1UL : 0UL);
                Add(ref hash, unchecked((ulong)player.RespawnsRemaining));
                Add(ref hash, unchecked((ulong)player.ObjectiveMilestones));
                Add(ref hash, unchecked((ulong)player.LastMilestoneTick));
            }

            if (simulation.Outcome.HasValue)
            {
                var outcome = simulation.Outcome.Value;
                Add(ref hash, unchecked((ulong)outcome.Winner.Value));
                Add(ref hash, (ulong)outcome.Condition);
                Add(ref hash, unchecked((ulong)outcome.CompletedTick));
            }

            Add(ref hash, simulation.ComputeObjectiveFingerprint());
            Add(ref hash, digGrid.ComputeFingerprint());

            return hash;
        }

        private static void Add(ref ulong hash, ulong value)
        {
            StableHash.Add(ref hash, value);
        }
    }
}
