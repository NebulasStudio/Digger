using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    public sealed class RelicExtractionState
    {
        private PlayerId? _holder;

        public bool GuardianDefeated { get; private set; }
        public PlayerId? Holder => _holder;

        public void DefeatGuardian() => GuardianDefeated = true;

        public bool TryClaim(PlayerId player)
        {
            if (!GuardianDefeated || _holder.HasValue)
                return false;
            _holder = player;
            return true;
        }

        public bool Drop(PlayerId player)
        {
            if (!_holder.HasValue || _holder.Value != player)
                return false;
            _holder = null;
            return true;
        }

        public bool CanExtract(PlayerId player, int exitId, MatchPhase phase, MatchRules rules)
        {
            if (!_holder.HasValue || _holder.Value != player)
                return false;
            if (exitId < 0 || exitId >= rules.ExtractionExitCount)
                return false;
            return phase != MatchPhase.SuddenDeath || exitId == rules.SuddenDeathExitId;
        }

        internal ulong ComputeFingerprint()
        {
            var hash = StableHash.Offset;
            StableHash.Add(ref hash, GuardianDefeated ? 1UL : 0UL);
            StableHash.Add(ref hash, _holder.HasValue ? unchecked((ulong)_holder.Value.Value) : ulong.MaxValue);
            return hash;
        }
    }
}
