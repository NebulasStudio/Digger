using System;
using System.Collections.Generic;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    public readonly struct PublicDigCell
    {
        public PublicDigCell(GridCell cell, bool isDug, string revealedLootId)
            : this(cell, isDug, revealedLootId, isDug ? 1 : 0, false)
        {
        }

        public PublicDigCell(GridCell cell, bool isDug, string revealedLootId, int depth, bool hasSpecialTell)
        {
            Cell = cell;
            IsDug = isDug;
            RevealedLootId = revealedLootId;
            Depth = depth;
            HasSpecialTell = hasSpecialTell;
        }

        public GridCell Cell { get; }
        public bool IsDug { get; }
        public string RevealedLootId { get; }
        public int Depth { get; }
        public bool HasSpecialTell { get; }
    }

    public readonly struct DigResult
    {
        public DigResult(bool changed, string revealedLootId)
            : this(changed, revealedLootId, changed ? 1 : 0, false)
        {
        }

        public DigResult(bool changed, string revealedLootId, int newDepth, bool isTunnel)
        {
            Changed = changed;
            RevealedLootId = revealedLootId;
            NewDepth = newDepth;
            IsTunnel = isTunnel;
        }

        public bool Changed { get; }
        public string RevealedLootId { get; }
        public int NewDepth { get; }
        public bool IsTunnel { get; }
    }

    /// <summary>
    /// Server-side dig state. Hidden loot and deep tunnel structure are never exposed before reveal.
    /// Supports 2 layers of depth: -1 (subsurface loot/items) and -2 (tunnels/special secrets).
    /// </summary>
    public sealed class DigGrid
    {
        private sealed class CellState
        {
            public string HiddenLootIdLevel1;
            public string HiddenLootIdLevel2;
            public int Depth;
            public bool HasSpecialTell;

            public bool IsDug => Depth > 0;
        }

        private readonly int _width;
        private readonly int _height;
        private readonly CellState[] _cells;

        public DigGrid(int width, int height, ulong mapSeed, IReadOnlyList<string> lootTable, int emptyWeight = 2)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (lootTable == null || lootTable.Count == 0) throw new ArgumentException("At least one loot id is required.", nameof(lootTable));
            if (emptyWeight < 0) throw new ArgumentOutOfRangeException(nameof(emptyWeight));

            _width = width;
            _height = height;
            _cells = new CellState[width * height];
            var rng = new DeterministicRng(mapSeed, 0xD16UL);
            var outcomes = lootTable.Count + emptyWeight;

            for (var index = 0; index < _cells.Length; index++)
            {
                var roll1 = rng.NextInt(outcomes);
                var roll2 = rng.NextInt(outcomes);
                var specialTellRoll = rng.NextInt(100);

                // Roughly 30% of cells have a special surface texture/tell allowing digging down to Level 2 (tunnels)
                bool hasTell = specialTellRoll < 30;

                _cells[index] = new CellState
                {
                    HiddenLootIdLevel1 = roll1 < lootTable.Count ? lootTable[roll1] : null,
                    HiddenLootIdLevel2 = hasTell ? (roll2 < lootTable.Count ? lootTable[roll2] : "tunnel.level2") : null,
                    Depth = 0,
                    HasSpecialTell = hasTell
                };
            }
        }

        public PublicDigCell GetPublicCell(GridCell cell)
        {
            var state = GetCell(cell);
            string loot = null;
            if (state.Depth == 1) loot = state.HiddenLootIdLevel1;
            else if (state.Depth == 2) loot = state.HiddenLootIdLevel2;

            return new PublicDigCell(cell, state.IsDug, loot, state.Depth, state.HasSpecialTell);
        }

        public IReadOnlyList<PublicDigCell> CreatePublicSnapshot()
        {
            var result = new PublicDigCell[_cells.Length];
            for (var y = 0; y < _height; y++)
            for (var x = 0; x < _width; x++)
                result[(y * _width) + x] = GetPublicCell(new GridCell(x, y));
            return result;
        }

        public DigResult Dig(GridCell cell)
        {
            var state = GetCell(cell);
            if (state.Depth == 0)
            {
                state.Depth = 1;
                return new DigResult(true, state.HiddenLootIdLevel1, 1, false);
            }

            if (state.Depth == 1)
            {
                if (!state.HasSpecialTell)
                {
                    // Cannot dig to Level 2 unless cell has special tell
                    return new DigResult(false, null, 1, false);
                }

                state.Depth = 2;
                return new DigResult(true, state.HiddenLootIdLevel2, 2, true);
            }

            // Max depth 2 reached
            return new DigResult(false, null, 2, true);
        }

        internal ulong ComputeFingerprint()
        {
            var hash = StableHash.Offset;
            for (var index = 0; index < _cells.Length; index++)
            {
                StableHash.Add(ref hash, (ulong)_cells[index].Depth);
                StableHash.Add(ref hash, _cells[index].HasSpecialTell ? 1UL : 0UL);
                StableHash.Add(ref hash, _cells[index].HiddenLootIdLevel1);
                StableHash.Add(ref hash, _cells[index].HiddenLootIdLevel2);
            }
            return hash;
        }

        private CellState GetCell(GridCell cell)
        {
            if (cell.X < 0 || cell.X >= _width || cell.Y < 0 || cell.Y >= _height)
                throw new ArgumentOutOfRangeException(nameof(cell), $"Cell {cell} is outside the grid.");
            return _cells[(cell.Y * _width) + cell.X];
        }
    }
}

