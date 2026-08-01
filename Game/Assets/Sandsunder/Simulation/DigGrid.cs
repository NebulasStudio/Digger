using System;
using System.Collections.Generic;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    public readonly struct PublicDigCell
    {
        public PublicDigCell(GridCell cell, bool isDug, string revealedLootId)
        {
            Cell = cell;
            IsDug = isDug;
            RevealedLootId = revealedLootId;
        }

        public GridCell Cell { get; }
        public bool IsDug { get; }
        public string RevealedLootId { get; }
    }

    public readonly struct DigResult
    {
        public DigResult(bool changed, string revealedLootId)
        {
            Changed = changed;
            RevealedLootId = revealedLootId;
        }

        public bool Changed { get; }
        public string RevealedLootId { get; }
    }

    /// <summary>
    /// Server-side dig state. Hidden loot is never exposed by the public snapshot API.
    /// </summary>
    public sealed class DigGrid
    {
        private sealed class CellState
        {
            public string HiddenLootId;
            public bool IsDug;
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
                var roll = rng.NextInt(outcomes);
                _cells[index] = new CellState
                {
                    HiddenLootId = roll < lootTable.Count ? lootTable[roll] : null
                };
            }
        }

        public PublicDigCell GetPublicCell(GridCell cell)
        {
            var state = GetCell(cell);
            return new PublicDigCell(cell, state.IsDug, state.IsDug ? state.HiddenLootId : null);
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
            if (state.IsDug)
                return new DigResult(false, null);

            state.IsDug = true;
            return new DigResult(true, state.HiddenLootId);
        }

        internal ulong ComputeFingerprint()
        {
            var hash = StableHash.Offset;
            for (var index = 0; index < _cells.Length; index++)
            {
                StableHash.Add(ref hash, _cells[index].IsDug ? 1UL : 0UL);
                StableHash.Add(ref hash, _cells[index].HiddenLootId);
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
