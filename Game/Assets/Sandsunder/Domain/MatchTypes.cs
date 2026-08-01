using System;

namespace Sandsunder.Domain
{
    public enum MatchPhase
    {
        Preparation = 0,
        CenterOpen = 1,
        SuddenDeath = 2,
        Completed = 3
    }

    public enum WinCondition
    {
        None = 0,
        RitualRace = 1,
        RelicExtraction = 2,
        LastSurvivor = 3,
        ObjectiveTimeout = 4
    }

    public readonly struct PlayerId : IEquatable<PlayerId>, IComparable<PlayerId>
    {
        public PlayerId(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Player id must be non-negative.");
            Value = value;
        }

        public int Value { get; }
        public int CompareTo(PlayerId other) => Value.CompareTo(other.Value);
        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"player:{Value}";
        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);
        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }

    public readonly struct GridCell : IEquatable<GridCell>, IComparable<GridCell>
    {
        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public int CompareTo(GridCell other)
        {
            var xComparison = X.CompareTo(other.X);
            return xComparison != 0 ? xComparison : Y.CompareTo(other.Y);
        }
        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        public override string ToString() => $"({X},{Y})";
    }

    public sealed class MatchIdentity
    {
        public MatchIdentity(string matchId, string buildId, string rulesetVersion)
        {
            MatchId = Required(matchId, nameof(matchId));
            BuildId = Required(buildId, nameof(buildId));
            RulesetVersion = Required(rulesetVersion, nameof(rulesetVersion));
        }

        public string MatchId { get; }
        public string BuildId { get; }
        public string RulesetVersion { get; }

        private static string Required(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            return value;
        }
    }

    /// <summary>Server-only identity projection. The map seed never enters client-facing contracts.</summary>
    internal readonly struct AuthoritativeMatchIdentity
    {
        internal AuthoritativeMatchIdentity(MatchIdentity publicIdentity, ulong mapSeed)
        {
            PublicIdentity = publicIdentity ?? throw new ArgumentNullException(nameof(publicIdentity));
            MapSeed = mapSeed;
        }

        internal MatchIdentity PublicIdentity { get; }
        internal ulong MapSeed { get; }
    }

    public readonly struct MatchOutcome
    {
        public MatchOutcome(PlayerId winner, WinCondition condition, long completedTick)
        {
            Winner = winner;
            Condition = condition;
            CompletedTick = completedTick;
        }

        public PlayerId Winner { get; }
        public WinCondition Condition { get; }
        public long CompletedTick { get; }
    }
}
