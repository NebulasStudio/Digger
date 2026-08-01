using System;
using System.Collections.Generic;
using System.Linq;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Provider-independent authoritative match foundation. All mutations happen at an explicit tick.
    /// </summary>
    public sealed class MatchSimulation
    {
        private readonly SortedDictionary<PlayerId, PlayerState> _players = new SortedDictionary<PlayerId, PlayerState>();
        private readonly MatchRules _rules;
        private readonly RitualRaceState _ritual;
        private readonly RelicExtractionState _relic = new RelicExtractionState();
        private readonly LastSurvivorState _lastSurvivor = new LastSurvivorState();
        private readonly Dictionary<PlayerId, HashSet<string>> _milestones = new Dictionary<PlayerId, HashSet<string>>();
        private readonly HashSet<string> _processedEliminations = new HashSet<string>(StringComparer.Ordinal);
        private readonly SortedDictionary<PlayerId, HashSet<WinCondition>> _victoryCandidates = new SortedDictionary<PlayerId, HashSet<WinCondition>>();
        private readonly ulong _mapSeed;

        internal MatchSimulation(AuthoritativeMatchIdentity identity, MatchRules rules)
        {
            Identity = identity.PublicIdentity;
            _mapSeed = identity.MapSeed;
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _ritual = new RitualRaceState(rules);
            Phase = MatchPhase.Preparation;
        }

        public MatchIdentity Identity { get; }
        public long Tick { get; private set; }
        public MatchPhase Phase { get; private set; }
        public MatchOutcome? Outcome { get; private set; }
        public IReadOnlyCollection<PlayerState> Players => _players.Values;

        public PlayerState AddPlayer(PlayerId player)
        {
            EnsureActive();
            if (Tick != 0) throw new InvalidOperationException("New players cannot join after the simulation starts.");
            if (_players.ContainsKey(player)) throw new InvalidOperationException($"{player} already joined.");
            if (_players.Count >= _rules.MaxPlayers) throw new InvalidOperationException("Match is full.");
            var state = new PlayerState(player, _players.Count);
            _players.Add(player, state);
            _milestones.Add(player, new HashSet<string>(StringComparer.Ordinal));
            return state;
        }

        public void AdvanceToTick(long targetTick)
        {
            EnsureActive();
            if (targetTick < Tick) throw new ArgumentOutOfRangeException(nameof(targetTick), "Simulation cannot move backwards.");
            if (targetTick > Tick && _victoryCandidates.Count > 0)
            {
                FinalizeCurrentTick();
                if (Outcome.HasValue)
                    return;
            }
            Tick = Math.Min(targetTick, _rules.MatchEndTick);
            ActivateDueRespawns();

            if (Tick == _rules.MatchEndTick)
            {
                CompleteByTimeout();
                return;
            }

            Phase = Tick >= _rules.SuddenDeathTick
                ? MatchPhase.SuddenDeath
                : Tick >= _rules.CenterOpenTick
                    ? MatchPhase.CenterOpen
                    : MatchPhase.Preparation;
        }

        public EliminationResult Eliminate(PlayerId player, string eliminationId)
        {
            EnsureActive();
            if (string.IsNullOrWhiteSpace(eliminationId)) throw new ArgumentException("Elimination id is required.", nameof(eliminationId));
            var state = GetPlayer(player);
            if (!_processedEliminations.Add(eliminationId))
                return EliminationResult.Ignored;
            if (!state.IsAlive || state.AwaitingRespawn || state.IsPermanentlyEliminated)
                return EliminationResult.Ignored;

            if (Tick < _rules.CenterOpenTick && state.RespawnsRemaining > 0)
            {
                state.RespawnsRemaining--;
                state.IsAlive = false;
                state.AwaitingRespawn = true;
                state.RespawnAtTick = Tick + _rules.RespawnDelayTicks;
                return EliminationResult.Respawned;
            }

            state.IsAlive = false;
            state.AwaitingRespawn = false;
            state.RespawnAtTick = -1;
            state.IsPermanentlyEliminated = true;
            _relic.Drop(player);
            TryCompleteLastSurvivor();
            return EliminationResult.PermanentlyEliminated;
        }

        public bool AwardRitualSeal(PlayerId player, string sealId)
        {
            EnsureEligible(player);
            var changed = _ritual.AwardSeal(player, sealId);
            if (changed) RecordMilestone(player, $"ritual:seal:{sealId}");
            return changed;
        }

        public bool ActivateRitualStation(PlayerId player, int stationId)
        {
            EnsureEligible(player);
            var changed = _ritual.ActivateStation(player, stationId);
            if (changed) RecordMilestone(player, $"ritual:station:{stationId}");
            return changed;
        }

        public bool AdvanceRitualChannel(PlayerId player, bool interrupted = false)
        {
            EnsureEligible(player);
            EnsureCenterOpen();
            var completed = _ritual.AdvanceChannel(player, Phase, Tick, interrupted);
            if (interrupted)
                RemoveVictoryCandidate(player, WinCondition.RitualRace);
            if (completed) RegisterVictory(player, WinCondition.RitualRace);
            return completed;
        }

        public void DefeatGuardian(PlayerId creditedPlayer)
        {
            EnsureEligible(creditedPlayer);
            EnsureCenterOpen();
            if (!_relic.GuardianDefeated)
            {
                _relic.DefeatGuardian();
                RecordMilestone(creditedPlayer, "relic:guardian");
            }
        }

        public bool ClaimRelic(PlayerId player)
        {
            EnsureEligible(player);
            EnsureCenterOpen();
            var claimed = _relic.TryClaim(player);
            if (claimed) RecordMilestone(player, "relic:claimed");
            return claimed;
        }

        public bool ExtractRelic(PlayerId player, int exitId)
        {
            EnsureEligible(player);
            EnsureCenterOpen();
            if (!_relic.CanExtract(player, exitId, Phase, _rules)) return false;
            RegisterVictory(player, WinCondition.RelicExtraction);
            return true;
        }

        public bool RecordMilestone(PlayerId player, string milestoneId)
        {
            EnsureEligible(player);
            if (string.IsNullOrWhiteSpace(milestoneId)) throw new ArgumentException("Milestone id is required.", nameof(milestoneId));
            if (!_milestones[player].Add(milestoneId))
                return false;
            var state = GetPlayer(player);
            state.ObjectiveMilestones++;
            state.LastMilestoneTick = Tick;
            return true;
        }

        /// <summary>Resolve all win candidates emitted during this tick independently of command arrival order.</summary>
        public bool FinalizeCurrentTick()
        {
            EnsureActive();
            if (_victoryCandidates.Count == 0)
                return false;

            var selected = _victoryCandidates
                .SelectMany(pair => pair.Value.Select(condition => new { Player = GetPlayer(pair.Key), Condition = condition }))
                .OrderBy(candidate => ComputeSeatPriority(candidate.Player.SeatIndex))
                .ThenBy(candidate => candidate.Player.SeatIndex)
                .ThenBy(candidate => candidate.Condition)
                .First();
            _victoryCandidates.Clear();
            Complete(selected.Player.Id, selected.Condition);
            return true;
        }

        private void TryCompleteLastSurvivor()
        {
            var survivor = _lastSurvivor.Evaluate(_players.Values);
            if (survivor.HasValue)
                RegisterVictory(survivor.Value, WinCondition.LastSurvivor);
        }

        private void CompleteByTimeout()
        {
            if (_players.Count == 0)
            {
                Phase = MatchPhase.Completed;
                return;
            }

            // Every participant remains timeout-eligible. Objective progress wins first;
            // exact ties use the same seeded seat priority as same-tick victory arbitration.
            var winner = _players.Values
                .OrderByDescending(player => player.ObjectiveMilestones)
                .ThenBy(player => player.LastMilestoneTick)
                .ThenBy(player => ComputeSeatPriority(player.SeatIndex))
                .ThenBy(player => player.SeatIndex)
                .First();

            RegisterVictory(winner.Id, WinCondition.ObjectiveTimeout);
            FinalizeCurrentTick();
        }

        private void RegisterVictory(PlayerId winner, WinCondition condition)
        {
            if (!_victoryCandidates.TryGetValue(winner, out var conditions))
            {
                conditions = new HashSet<WinCondition>();
                _victoryCandidates.Add(winner, conditions);
            }
            conditions.Add(condition);
        }

        private void RemoveVictoryCandidate(PlayerId player, WinCondition condition)
        {
            if (!_victoryCandidates.TryGetValue(player, out var conditions))
                return;
            conditions.Remove(condition);
            if (conditions.Count == 0)
                _victoryCandidates.Remove(player);
        }

        private ulong ComputeSeatPriority(int seatIndex)
        {
            var value = _mapSeed ^ unchecked((ulong)Tick * 0x9E3779B97F4A7C15UL) ^ unchecked((ulong)(seatIndex + 1));
            value ^= value >> 30;
            value = unchecked(value * 0xBF58476D1CE4E5B9UL);
            value ^= value >> 27;
            value = unchecked(value * 0x94D049BB133111EBUL);
            return value ^ (value >> 31);
        }

        private void Complete(PlayerId winner, WinCondition condition)
        {
            if (Outcome.HasValue) return;
            Outcome = new MatchOutcome(winner, condition, Tick);
            Phase = MatchPhase.Completed;
        }

        private void ActivateDueRespawns()
        {
            foreach (var player in _players.Values)
            {
                if (!player.AwaitingRespawn || player.RespawnAtTick > Tick)
                    continue;
                player.AwaitingRespawn = false;
                player.RespawnAtTick = -1;
                player.IsAlive = true;
            }
        }

        private PlayerState GetPlayer(PlayerId player)
        {
            if (!_players.TryGetValue(player, out var state))
                throw new KeyNotFoundException($"{player} is not registered.");
            return state;
        }

        private void EnsureEligible(PlayerId player)
        {
            EnsureActive();
            var state = GetPlayer(player);
            if (!state.IsAlive || state.IsPermanentlyEliminated)
                throw new InvalidOperationException($"{player} cannot act while eliminated.");
        }

        private void EnsureActive()
        {
            if (Outcome.HasValue || Phase == MatchPhase.Completed)
                throw new InvalidOperationException("Match is already complete.");
        }

        private void EnsureCenterOpen()
        {
            if (Phase == MatchPhase.Preparation)
                throw new InvalidOperationException("The center objective is not open yet.");
        }

        internal ulong ComputeObjectiveFingerprint()
        {
            var hash = StableHash.Offset;
            StableHash.Add(ref hash, _ritual.ComputeFingerprint());
            StableHash.Add(ref hash, _relic.ComputeFingerprint());
            foreach (var pair in _milestones.OrderBy(pair => pair.Key))
            foreach (var milestone in pair.Value.OrderBy(value => value, StringComparer.Ordinal))
            {
                StableHash.Add(ref hash, unchecked((ulong)pair.Key.Value));
                StableHash.Add(ref hash, milestone);
            }
            foreach (var elimination in _processedEliminations.OrderBy(value => value, StringComparer.Ordinal))
                StableHash.Add(ref hash, elimination);
            foreach (var candidate in _victoryCandidates)
            {
                StableHash.Add(ref hash, unchecked((ulong)candidate.Key.Value));
                foreach (var condition in candidate.Value.OrderBy(value => value))
                    StableHash.Add(ref hash, (ulong)condition);
            }
            return hash;
        }

        internal ulong AuthoritativeMapSeed => _mapSeed;
    }
}
