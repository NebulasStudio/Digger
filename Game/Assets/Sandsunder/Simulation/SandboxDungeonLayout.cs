using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    /// <summary>An ordered, orthogonal connection between two local dungeon cells.</summary>
    public readonly struct SandboxDungeonSegment : IEquatable<SandboxDungeonSegment>
    {
        public SandboxDungeonSegment(GridCell fromCell, GridCell toCell)
        {
            int manhattanDistance = ManhattanDistance(fromCell, toCell);
            if (manhattanDistance != 1)
                throw new ArgumentException("Dungeon segments must connect orthogonally adjacent cells.");

            FromCell = fromCell;
            ToCell = toCell;
        }

        public GridCell FromCell { get; }
        public GridCell ToCell { get; }

        public bool Equals(SandboxDungeonSegment other)
        {
            return FromCell.Equals(other.FromCell) && ToCell.Equals(other.ToCell);
        }

        public override bool Equals(object obj)
        {
            return obj is SandboxDungeonSegment other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((FromCell.GetHashCode() * 397) ^ ToCell.GetHashCode());
        }

        internal static int ManhattanDistance(GridCell first, GridCell second)
        {
            return Math.Abs((long)first.X - second.X) + Math.Abs((long)first.Y - second.Y) > int.MaxValue
                ? int.MaxValue
                : (int)(Math.Abs((long)first.X - second.X) + Math.Abs((long)first.Y - second.Y));
        }
    }

    /// <summary>
    /// Immutable authoritative-ready dungeon topology. A map seed and the surface dig cell choose
    /// one of a small set of reviewed paths; clients can render the resulting local cells without
    /// owning or inventing dungeon structure.
    /// </summary>
    public sealed class SandboxDungeonLayout
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentVersion = "sandbox-dungeon-layout-1";
        public const int TemplateCount = 4;
        public const int MaximumLocalCoordinateMagnitude = 6;

        private static readonly GridCell[][] Templates =
        {
            new[]
            {
                new GridCell(0, 0),
                new GridCell(0, 1),
                new GridCell(0, 2),
                new GridCell(1, 2),
                new GridCell(2, 2),
                new GridCell(2, 3),
                new GridCell(2, 4),
            },
            new[]
            {
                new GridCell(0, 0),
                new GridCell(1, 0),
                new GridCell(2, 0),
                new GridCell(2, 1),
                new GridCell(2, 2),
                new GridCell(1, 2),
                new GridCell(1, 3),
                new GridCell(1, 4),
                new GridCell(0, 4),
            },
            new[]
            {
                new GridCell(0, 0),
                new GridCell(0, -1),
                new GridCell(0, -2),
                new GridCell(1, -2),
                new GridCell(2, -2),
                new GridCell(3, -2),
                new GridCell(3, -1),
                new GridCell(3, 0),
                new GridCell(4, 0),
            },
            new[]
            {
                new GridCell(0, 0),
                new GridCell(-1, 0),
                new GridCell(-2, 0),
                new GridCell(-2, 1),
                new GridCell(-2, 2),
                new GridCell(-1, 2),
                new GridCell(0, 2),
                new GridCell(0, 3),
                new GridCell(1, 3),
                new GridCell(2, 3),
            },
        };

        private readonly ReadOnlyCollection<GridCell> orderedPathCells;
        private readonly ReadOnlyCollection<SandboxDungeonSegment> orderedSegments;
        private readonly HashSet<GridCell> walkableCells;

        private SandboxDungeonLayout(int templateIndex, GridCell surfaceEntranceCell, GridCell[] pathCells)
        {
            TemplateIndex = templateIndex;
            SurfaceEntranceCell = surfaceEntranceCell;
            SchemaVersion = CurrentSchemaVersion;
            Version = CurrentVersion;

            GridCell[] ownedCells = (GridCell[])pathCells.Clone();
            orderedPathCells = Array.AsReadOnly(ownedCells);
            walkableCells = new HashSet<GridCell>(ownedCells);

            var segments = new SandboxDungeonSegment[Math.Max(0, ownedCells.Length - 1)];
            for (int index = 0; index < segments.Length; index++)
                segments[index] = new SandboxDungeonSegment(ownedCells[index], ownedCells[index + 1]);
            orderedSegments = Array.AsReadOnly(segments);

            EntranceCell = ownedCells[0];
            ChestEndpointCell = ownedCells[ownedCells.Length - 1];
            ReturnCell = EntranceCell;

            if (!TryValidate(out string validationFailure))
                throw new InvalidOperationException($"Invalid dungeon template {templateIndex}: {validationFailure}");
        }

        public int SchemaVersion { get; }
        public string Version { get; }
        public int TemplateIndex { get; }
        public GridCell SurfaceEntranceCell { get; }
        public GridCell EntranceCell { get; }
        public GridCell ChestEndpointCell { get; }
        public GridCell ReturnCell { get; }
        public GridCell ExitCell => ReturnCell;
        public IReadOnlyList<GridCell> OrderedPathCells => orderedPathCells;
        public IReadOnlyList<SandboxDungeonSegment> OrderedSegments => orderedSegments;

        public static SandboxDungeonLayout Create(ulong mapSeed, GridCell surfaceEntranceCell)
        {
            int templateIndex = SelectTemplateIndex(mapSeed, surfaceEntranceCell);
            return new SandboxDungeonLayout(templateIndex, surfaceEntranceCell, Templates[templateIndex]);
        }

        public static int SelectTemplateIndex(ulong mapSeed, GridCell surfaceEntranceCell)
        {
            ulong selector = StableHash.Offset;
            StableHash.Add(ref selector, unchecked((ulong)CurrentSchemaVersion));
            StableHash.Add(ref selector, mapSeed);
            StableHash.Add(ref selector, unchecked((ulong)(long)surfaceEntranceCell.X));
            StableHash.Add(ref selector, unchecked((ulong)(long)surfaceEntranceCell.Y));
            return (int)(selector % TemplateCount);
        }

        public bool ContainsWalkableCell(GridCell localCell)
        {
            return walkableCells.Contains(localCell);
        }

        public bool TryValidate(out string failure)
        {
            if (SchemaVersion <= 0 || string.IsNullOrWhiteSpace(Version))
            {
                failure = "Layout version is missing.";
                return false;
            }

            if (TemplateIndex < 0 || TemplateIndex >= TemplateCount)
            {
                failure = "Template index is outside the catalog.";
                return false;
            }

            if (orderedPathCells == null || orderedPathCells.Count < 2)
            {
                failure = "A dungeon path requires an entrance and a chest endpoint.";
                return false;
            }

            if (!EntranceCell.Equals(orderedPathCells[0])
                || !ReturnCell.Equals(EntranceCell)
                || !ChestEndpointCell.Equals(orderedPathCells[orderedPathCells.Count - 1]))
            {
                failure = "Entrance, return, or chest endpoint does not match the ordered path.";
                return false;
            }

            if (walkableCells.Count != orderedPathCells.Count)
            {
                failure = "The ordered path contains a repeated cell.";
                return false;
            }

            if (orderedSegments.Count != orderedPathCells.Count - 1)
            {
                failure = "The segment count does not match the path.";
                return false;
            }

            for (int index = 0; index < orderedPathCells.Count; index++)
            {
                GridCell cell = orderedPathCells[index];
                if (Math.Abs((long)cell.X) > MaximumLocalCoordinateMagnitude
                    || Math.Abs((long)cell.Y) > MaximumLocalCoordinateMagnitude)
                {
                    failure = $"Cell {cell} escapes the local dungeon bounds.";
                    return false;
                }

                if (index == 0)
                    continue;

                GridCell previous = orderedPathCells[index - 1];
                if (SandboxDungeonSegment.ManhattanDistance(previous, cell) != 1)
                {
                    failure = $"Cells {previous} and {cell} are not orthogonally contiguous.";
                    return false;
                }

                SandboxDungeonSegment segment = orderedSegments[index - 1];
                if (!segment.FromCell.Equals(previous) || !segment.ToCell.Equals(cell))
                {
                    failure = "Segment order does not match the ordered path.";
                    return false;
                }
            }

            int chestConnections = 0;
            for (int index = 0; index < orderedPathCells.Count; index++)
            {
                if (SandboxDungeonSegment.ManhattanDistance(ChestEndpointCell, orderedPathCells[index]) == 1)
                    chestConnections++;
            }

            if (chestConnections != 1)
            {
                failure = "The chest must terminate a single non-looping tunnel.";
                return false;
            }

            failure = null;
            return true;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StableHash.Offset;
            StableHash.Add(ref hash, unchecked((ulong)SchemaVersion));
            StableHash.Add(ref hash, Version);
            StableHash.Add(ref hash, unchecked((ulong)TemplateIndex));
            AddCell(ref hash, SurfaceEntranceCell);
            AddCell(ref hash, EntranceCell);
            AddCell(ref hash, ChestEndpointCell);
            AddCell(ref hash, ReturnCell);
            StableHash.Add(ref hash, unchecked((ulong)orderedPathCells.Count));
            for (int index = 0; index < orderedPathCells.Count; index++)
                AddCell(ref hash, orderedPathCells[index]);
            return hash;
        }

        private static void AddCell(ref ulong hash, GridCell cell)
        {
            StableHash.Add(ref hash, unchecked((ulong)(long)cell.X));
            StableHash.Add(ref hash, unchecked((ulong)(long)cell.Y));
        }
    }
}
