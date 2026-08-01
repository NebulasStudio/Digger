using System;
using System.Collections.Generic;
using System.Linq;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    public sealed class RitualRaceState
    {
        private sealed class Progress
        {
            public readonly HashSet<string> Seals = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<int> Stations = new HashSet<int>();
            public int ChannelTicks;
            public long LastAdvanceTick = -1;
            public long LastInterruptTick = -1;
        }

        private readonly Dictionary<PlayerId, Progress> _progress = new Dictionary<PlayerId, Progress>();
        private readonly MatchRules _rules;

        public RitualRaceState(MatchRules rules) => _rules = rules ?? throw new ArgumentNullException(nameof(rules));

        public bool AwardSeal(PlayerId player, string sealId)
        {
            if (string.IsNullOrWhiteSpace(sealId)) throw new ArgumentException("Seal id is required.", nameof(sealId));
            return For(player).Seals.Add(sealId);
        }

        public bool ActivateStation(PlayerId player, int stationId)
        {
            if (stationId < 0) throw new ArgumentOutOfRangeException(nameof(stationId));
            return For(player).Stations.Add(stationId);
        }

        public bool AdvanceChannel(PlayerId player, MatchPhase phase, long authoritativeTick, bool interrupted)
        {
            var state = For(player);
            if (interrupted)
            {
                if (state.LastInterruptTick == authoritativeTick)
                    return false;
                state.LastInterruptTick = authoritativeTick;
                state.ChannelTicks = 0;
                return false;
            }

            // An interrupt wins regardless of command order within the authoritative tick.
            if (state.LastInterruptTick == authoritativeTick || state.LastAdvanceTick == authoritativeTick)
                return false;

            if (state.Seals.Count < _rules.RequiredSeals || state.Stations.Count < _rules.RequiredStations)
                return false;

            if (state.LastAdvanceTick != authoritativeTick - 1)
                state.ChannelTicks = 0;
            state.LastAdvanceTick = authoritativeTick;
            state.ChannelTicks++;
            var required = phase == MatchPhase.SuddenDeath
                ? _rules.SuddenDeathRitualChannelTicks
                : _rules.RitualChannelTicks;
            return state.ChannelTicks >= required;
        }

        private Progress For(PlayerId player)
        {
            if (!_progress.TryGetValue(player, out var state))
            {
                state = new Progress();
                _progress.Add(player, state);
            }
            return state;
        }

        internal ulong ComputeFingerprint()
        {
            var hash = StableHash.Offset;
            foreach (var pair in _progress.OrderBy(pair => pair.Key))
            {
                StableHash.Add(ref hash, unchecked((ulong)pair.Key.Value));
                foreach (var seal in pair.Value.Seals.OrderBy(value => value, StringComparer.Ordinal))
                    StableHash.Add(ref hash, seal);
                foreach (var station in pair.Value.Stations.OrderBy(value => value))
                    StableHash.Add(ref hash, unchecked((ulong)station));
                StableHash.Add(ref hash, unchecked((ulong)pair.Value.ChannelTicks));
                StableHash.Add(ref hash, unchecked((ulong)pair.Value.LastAdvanceTick));
                StableHash.Add(ref hash, unchecked((ulong)pair.Value.LastInterruptTick));
            }
            return hash;
        }
    }
}
