using System;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Match-local dungeon state. This type deliberately owns no account progression and has no
    /// dependency on scene objects; Unity components only project its deterministic state.
    /// </summary>
    public sealed class SandboxDungeonRuntime
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentRulesetVersion = "sandbox-dungeon-runtime-1";
        public const int DefaultFinalChestGold = 75;

        private const ulong HashOffset = 14695981039346656037UL;
        private const ulong HashPrime = 1099511628211UL;

        public SandboxDungeonRuntime(
            ulong mapSeed,
            GridCell surfaceEntranceCell,
            int finalChestGold = DefaultFinalChestGold)
            : this(SandboxDungeonLayout.Create(mapSeed, surfaceEntranceCell), finalChestGold)
        {
        }

        public SandboxDungeonRuntime(
            SandboxDungeonLayout layout,
            int finalChestGold = DefaultFinalChestGold)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (finalChestGold < 0)
                throw new ArgumentOutOfRangeException(nameof(finalChestGold));

            FinalChestGold = finalChestGold;
        }

        public SandboxDungeonLayout Layout { get; }
        public int SchemaVersion => CurrentSchemaVersion;
        public string RulesetVersion => CurrentRulesetVersion;
        public int FinalChestGold { get; }
        public int MatchGold { get; private set; }
        public bool IsInsideDungeon { get; private set; }
        public bool IsFinalChestClaimed { get; private set; }

        public bool Enter()
        {
            if (IsInsideDungeon) return false;
            IsInsideDungeon = true;
            return true;
        }

        public bool Exit()
        {
            if (!IsInsideDungeon) return false;
            IsInsideDungeon = false;
            return true;
        }

        /// <summary>Credits match-only gold once. No inventory or account state is touched.</summary>
        public bool TryClaimFinalChest(out int awardedGold)
        {
            awardedGold = 0;
            if (!IsInsideDungeon || IsFinalChestClaimed) return false;

            IsFinalChestClaimed = true;
            awardedGold = FinalChestGold;
            MatchGold = checked(MatchGold + awardedGold);
            return true;
        }

        public bool IsWalkableCell(GridCell localCell)
        {
            return Layout.ContainsWalkableCell(localCell);
        }

        public bool TryGetNextPathCell(GridCell localCell, out GridCell nextCell)
        {
            for (int index = 0; index < Layout.OrderedPathCells.Count - 1; index++)
            {
                if (!Layout.OrderedPathCells[index].Equals(localCell)) continue;
                nextCell = Layout.OrderedPathCells[index + 1];
                return true;
            }

            nextCell = default;
            return false;
        }

        /// <summary>Returns the nearest reviewed path cell, resolving equal distances by path order.</summary>
        public GridCell ConfineToPath(GridCell candidate)
        {
            GridCell nearest = Layout.OrderedPathCells[0];
            long nearestDistance = ManhattanDistance(candidate, nearest);
            for (int index = 1; index < Layout.OrderedPathCells.Count; index++)
            {
                GridCell pathCell = Layout.OrderedPathCells[index];
                long distance = ManhattanDistance(candidate, pathCell);
                if (distance >= nearestDistance) continue;
                nearest = pathCell;
                nearestDistance = distance;
            }

            return nearest;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = HashOffset;
            Add(ref hash, unchecked((ulong)CurrentSchemaVersion));
            Add(ref hash, Layout.ComputeStateHash());
            Add(ref hash, IsInsideDungeon ? 1UL : 0UL);
            Add(ref hash, IsFinalChestClaimed ? 1UL : 0UL);
            Add(ref hash, unchecked((ulong)MatchGold));
            Add(ref hash, unchecked((ulong)FinalChestGold));
            return hash;
        }

        private static long ManhattanDistance(GridCell first, GridCell second)
        {
            return Math.Abs((long)first.X - second.X) + Math.Abs((long)first.Y - second.Y);
        }

        private static void Add(ref ulong hash, ulong value)
        {
            for (int shift = 0; shift < 64; shift += 8)
            {
                hash ^= (byte)(value >> shift);
                hash *= HashPrime;
            }
        }
    }
}
