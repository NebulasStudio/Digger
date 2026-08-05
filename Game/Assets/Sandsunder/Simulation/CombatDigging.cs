using System;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    public readonly struct CombatDigStrikeResult
    {
        public CombatDigStrikeResult(bool changed, bool revealedNow, int strikesRemaining, string revealedLootId)
        {
            Changed = changed;
            RevealedNow = revealedNow;
            StrikesRemaining = strikesRemaining;
            RevealedLootId = revealedLootId;
        }

        public bool Changed { get; }
        public bool RevealedNow { get; }
        public int StrikesRemaining { get; }
        public string RevealedLootId { get; }
    }

    /// <summary>
    /// Authoritative per-node effort. DigGrid remains the only owner of hidden outcome;
    /// this state asks it to reveal exactly once after the required strike count.
    /// </summary>
    public sealed class CombatDigNodeState
    {
        private readonly DigGrid digGrid;
        private readonly GridCell cell;
        private int acceptedStrikes;
        private bool resolved;

        public CombatDigNodeState(DigGrid digGrid, GridCell cell, int requiredStrikes)
        {
            this.digGrid = digGrid ?? throw new ArgumentNullException(nameof(digGrid));
            if (requiredStrikes <= 0) throw new ArgumentOutOfRangeException(nameof(requiredStrikes));

            this.cell = cell;
            RequiredStrikes = requiredStrikes;
            PublicDigCell publicCell = digGrid.GetPublicCell(cell);
            resolved = publicCell.IsDug;
            acceptedStrikes = resolved ? requiredStrikes : 0;
        }

        public CombatDigNodeState(DigGrid digGrid, int cellX, int cellY, int requiredStrikes)
            : this(digGrid, new GridCell(cellX, cellY), requiredStrikes)
        {
        }

        public int RequiredStrikes { get; }
        public int StrikesRemaining => Math.Max(0, RequiredStrikes - acceptedStrikes);
        public bool IsRevealed => resolved;

        public CombatDigStrikeResult Strike()
        {
            if (resolved)
            {
                return new CombatDigStrikeResult(false, false, 0, null);
            }

            acceptedStrikes++;
            if (acceptedStrikes < RequiredStrikes)
            {
                return new CombatDigStrikeResult(true, false, StrikesRemaining, null);
            }

            DigResult digResult = digGrid.Dig(cell);
            resolved = true;
            return new CombatDigStrikeResult(
                changed: digResult.Changed,
                revealedNow: digResult.Changed,
                strikesRemaining: 0,
                revealedLootId: digResult.Changed ? digResult.RevealedLootId : null);
        }
    }

    public readonly struct CombatPickupResult
    {
        public CombatPickupResult(bool changed, int collectorEntityId, string lootId)
        {
            Changed = changed;
            CollectorEntityId = collectorEntityId;
            LootId = lootId;
        }

        public bool Changed { get; }
        public int CollectorEntityId { get; }
        public string LootId { get; }
    }

    public sealed class CombatPickupState
    {
        public CombatPickupState(int pickupId, string lootId)
        {
            if (pickupId < 0) throw new ArgumentOutOfRangeException(nameof(pickupId));
            if (string.IsNullOrWhiteSpace(lootId)) throw new ArgumentException("Loot id is required.", nameof(lootId));

            PickupId = pickupId;
            LootId = lootId;
        }

        public int PickupId { get; }
        public string LootId { get; }
        public bool IsCollected { get; private set; }

        public CombatPickupResult TryCollect(int collectorEntityId)
        {
            if (collectorEntityId < 0) throw new ArgumentOutOfRangeException(nameof(collectorEntityId));
            if (IsCollected)
            {
                return new CombatPickupResult(false, collectorEntityId, null);
            }

            IsCollected = true;
            return new CombatPickupResult(true, collectorEntityId, LootId);
        }
    }
}
