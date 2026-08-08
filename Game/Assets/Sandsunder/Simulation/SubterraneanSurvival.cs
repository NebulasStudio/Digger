using System;

namespace Sandsunder.Simulation
{
    public readonly struct SubterraneanOxygenRules
    {
        public const int UnitsPerPercent = 1000;
        public const int OxygenFlaskRestorePercent = 35;
        public const int CurrentSchemaVersion = 1;
        public const string MilestoneOneVersion = "subterranean-oxygen-1";

        public static readonly SubterraneanOxygenRules MilestoneOne = new SubterraneanOxygenRules(
            CurrentSchemaVersion,
            MilestoneOneVersion,
            ticksPerSecond: 60,
            maximumOxygenUnits: 100 * UnitsPerPercent,
            depletionUnitsPerSecond: UnitsPerPercent,
            refillUnitsPerSecond: 5 * UnitsPerPercent,
            suffocationDamagePerSecond: 5);

        public SubterraneanOxygenRules(
            int schemaVersion,
            string version,
            int ticksPerSecond,
            int maximumOxygenUnits,
            int depletionUnitsPerSecond,
            int refillUnitsPerSecond,
            int suffocationDamagePerSecond)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Rules version is required.", nameof(version));
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            if (maximumOxygenUnits <= 0) throw new ArgumentOutOfRangeException(nameof(maximumOxygenUnits));
            if (depletionUnitsPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(depletionUnitsPerSecond));
            if (refillUnitsPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(refillUnitsPerSecond));
            if (suffocationDamagePerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(suffocationDamagePerSecond));

            SchemaVersion = schemaVersion;
            Version = version;
            TicksPerSecond = ticksPerSecond;
            MaximumOxygenUnits = maximumOxygenUnits;
            DepletionUnitsPerSecond = depletionUnitsPerSecond;
            RefillUnitsPerSecond = refillUnitsPerSecond;
            SuffocationDamagePerSecond = suffocationDamagePerSecond;
        }

        public int SchemaVersion { get; }
        public string Version { get; }
        public int TicksPerSecond { get; }
        public int MaximumOxygenUnits { get; }
        public int DepletionUnitsPerSecond { get; }
        public int RefillUnitsPerSecond { get; }
        public int SuffocationDamagePerSecond { get; }
    }

    /// <summary>Authoritative-ready, tick-driven oxygen and suffocation state.</summary>
    public sealed class SubterraneanOxygenState
    {
        private readonly SubterraneanOxygenRules rules;
        private int oxygenUnits;
        private int depletionRemainder;
        private int refillRemainder;
        private int suffocationRemainder;
        private long tick;

        public SubterraneanOxygenState(SubterraneanOxygenRules rules)
        {
            this.rules = rules;
            oxygenUnits = rules.MaximumOxygenUnits;
        }

        public long Tick => tick;
        public int OxygenUnits => oxygenUnits;
        public int MaximumOxygenUnits => rules.MaximumOxygenUnits;
        public double OxygenPercent => oxygenUnits / (double)SubterraneanOxygenRules.UnitsPerPercent;
        public bool IsDepleted => oxygenUnits == 0;

        /// <summary>
        /// Applies a server-authorized oxygen grant and returns whether the state changed. The
        /// caller owns inventory consumption; the simulation only clamps the deterministic value.
        /// </summary>
        public bool RestorePercent(int percent)
        {
            if (percent <= 0) throw new ArgumentOutOfRangeException(nameof(percent));
            int previous = oxygenUnits;
            oxygenUnits = Math.Min(
                rules.MaximumOxygenUnits,
                oxygenUnits + (percent * SubterraneanOxygenRules.UnitsPerPercent));
            return oxygenUnits != previous;
        }

        /// <summary>Advances exactly one authoritative tick and returns suffocation damage due.</summary>
        public int Step(bool isSubterranean)
        {
            tick++;
            if (isSubterranean)
            {
                refillRemainder = 0;
                depletionRemainder += rules.DepletionUnitsPerSecond;
                int depleted = depletionRemainder / rules.TicksPerSecond;
                depletionRemainder %= rules.TicksPerSecond;
                oxygenUnits = Math.Max(0, oxygenUnits - depleted);
            }
            else
            {
                depletionRemainder = 0;
                refillRemainder += rules.RefillUnitsPerSecond;
                int refilled = refillRemainder / rules.TicksPerSecond;
                refillRemainder %= rules.TicksPerSecond;
                oxygenUnits = Math.Min(rules.MaximumOxygenUnits, oxygenUnits + refilled);
            }

            if (!isSubterranean || oxygenUnits > 0)
            {
                suffocationRemainder = 0;
                return 0;
            }

            suffocationRemainder += rules.SuffocationDamagePerSecond;
            int damage = suffocationRemainder / rules.TicksPerSecond;
            suffocationRemainder %= rules.TicksPerSecond;
            return damage;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StableHash.Offset;
            StableHash.Add(ref hash, unchecked((ulong)rules.SchemaVersion));
            StableHash.Add(ref hash, unchecked((ulong)tick));
            StableHash.Add(ref hash, unchecked((ulong)oxygenUnits));
            StableHash.Add(ref hash, unchecked((ulong)depletionRemainder));
            StableHash.Add(ref hash, unchecked((ulong)refillRemainder));
            StableHash.Add(ref hash, unchecked((ulong)suffocationRemainder));
            return hash;
        }
    }

    /// <summary>Single simulation owner for the player's authoritative excavation depth.</summary>
    public sealed class PlayerDepthState
    {
        public const int SubterraneanThresholdDepth = 2;
        public const int MaximumDepth = 2;

        public int CurrentDepth { get; private set; }
        public bool IsSubterranean => CurrentDepth >= SubterraneanThresholdDepth;

        public bool SetAuthoritativeDepth(int depth)
        {
            if (depth < 0 || depth > MaximumDepth)
                throw new ArgumentOutOfRangeException(nameof(depth));
            if (CurrentDepth == depth)
                return false;

            CurrentDepth = depth;
            return true;
        }

        public bool ApplyAuthoritativeCellDepth(int cellDepth)
        {
            if (cellDepth < 0 || cellDepth > MaximumDepth)
                throw new ArgumentOutOfRangeException(nameof(cellDepth));
            return SetAuthoritativeDepth(Math.Max(CurrentDepth, cellDepth));
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StableHash.Offset;
            StableHash.Add(ref hash, unchecked((ulong)CurrentDepth));
            return hash;
        }
    }
}
