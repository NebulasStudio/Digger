using System;
using System.Collections.Generic;

namespace Sandsunder.Simulation
{
    public enum SandboxShopPurchaseStatus
    {
        Purchased = 0,
        RejectedInvalidQuantity = 1,
        RejectedUnknownItem = 2,
        RejectedInsufficientMatchCredits = 3,
        RejectedLimitReached = 4,
        RejectedRequestConflict = 5
    }

    public readonly struct SandboxShopPurchaseCommand : IEquatable<SandboxShopPurchaseCommand>
    {
        public SandboxShopPurchaseCommand(string requestId, string itemId, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Purchase request id is required.", nameof(requestId));
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Purchase item id is required.", nameof(itemId));

            RequestId = requestId;
            ItemId = itemId;
            Quantity = quantity;
        }

        public string RequestId { get; }
        public string ItemId { get; }
        public int Quantity { get; }

        public bool Equals(SandboxShopPurchaseCommand other)
        {
            return string.Equals(RequestId, other.RequestId, StringComparison.Ordinal)
                && string.Equals(ItemId, other.ItemId, StringComparison.Ordinal)
                && Quantity == other.Quantity;
        }

        public override bool Equals(object obj) => obj is SandboxShopPurchaseCommand other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RequestId, ItemId, Quantity);
    }

    public readonly struct SandboxShopPurchaseResult : IEquatable<SandboxShopPurchaseResult>
    {
        public SandboxShopPurchaseResult(
            string requestId,
            string itemId,
            SandboxShopPurchaseStatus status,
            int grantedQuantity,
            int ownedQuantityAfter,
            int matchCreditsAfter)
        {
            RequestId = requestId;
            ItemId = itemId;
            Status = status;
            GrantedQuantity = grantedQuantity;
            OwnedQuantityAfter = ownedQuantityAfter;
            MatchCreditsAfter = matchCreditsAfter;
        }

        public string RequestId { get; }
        public string ItemId { get; }
        public SandboxShopPurchaseStatus Status { get; }
        public int GrantedQuantity { get; }
        public int OwnedQuantityAfter { get; }
        public int MatchCreditsAfter { get; }
        public bool WasPurchased => Status == SandboxShopPurchaseStatus.Purchased;

        public bool Equals(SandboxShopPurchaseResult other)
        {
            return string.Equals(RequestId, other.RequestId, StringComparison.Ordinal)
                && string.Equals(ItemId, other.ItemId, StringComparison.Ordinal)
                && Status == other.Status
                && GrantedQuantity == other.GrantedQuantity
                && OwnedQuantityAfter == other.OwnedQuantityAfter
                && MatchCreditsAfter == other.MatchCreditsAfter;
        }

        public override bool Equals(object obj) => obj is SandboxShopPurchaseResult other && Equals(other);
        public override int GetHashCode()
        {
            return HashCode.Combine(RequestId, ItemId, Status, GrantedQuantity, OwnedQuantityAfter, MatchCreditsAfter);
        }
    }

    /// <summary>
    /// Authoritative-ready, deterministic ledger for a single sandbox match. It intentionally has
    /// no account identifier, real-money price, backend call, Unity type, or persistence adapter.
    /// </summary>
    public sealed class SandboxShopState
    {
        private readonly struct ProcessedPurchase
        {
            public ProcessedPurchase(SandboxShopPurchaseCommand command, SandboxShopPurchaseResult result)
            {
                Command = command;
                Result = result;
            }

            public SandboxShopPurchaseCommand Command { get; }
            public SandboxShopPurchaseResult Result { get; }
        }

        private readonly SandboxShopCatalog catalog;
        private readonly Dictionary<string, int> ownedByItem = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ProcessedPurchase> processedByRequest = new(StringComparer.Ordinal);

        public SandboxShopState(string matchId, int startingMatchCredits, SandboxShopCatalog catalog)
        {
            if (string.IsNullOrWhiteSpace(matchId)) throw new ArgumentException("Match id is required.", nameof(matchId));
            if (startingMatchCredits < 0) throw new ArgumentOutOfRangeException(nameof(startingMatchCredits));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            MatchId = matchId;
            MatchCredits = startingMatchCredits;
        }

        public string MatchId { get; }
        public int MatchCredits { get; private set; }
        public int ProcessedPurchaseCount => processedByRequest.Count;
        public string CatalogVersion => catalog.CatalogVersion;

        public int GetOwnedQuantity(string itemId)
        {
            if (itemId == null) throw new ArgumentNullException(nameof(itemId));
            return ownedByItem.TryGetValue(itemId, out int quantity) ? quantity : 0;
        }

        public SandboxShopPurchaseResult ProcessPurchase(SandboxShopPurchaseCommand command)
        {
            if (processedByRequest.TryGetValue(command.RequestId, out ProcessedPurchase processed))
            {
                if (processed.Command.Equals(command)) return processed.Result;
                return new SandboxShopPurchaseResult(
                    command.RequestId,
                    command.ItemId,
                    SandboxShopPurchaseStatus.RejectedRequestConflict,
                    grantedQuantity: 0,
                    ownedQuantityAfter: GetOwnedQuantity(command.ItemId),
                    matchCreditsAfter: processed.Result.MatchCreditsAfter);
            }

            SandboxShopPurchaseResult result = Evaluate(command);
            processedByRequest.Add(command.RequestId, new ProcessedPurchase(command, result));
            return result;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StableHash.Offset;
            StableHash.Add(ref hash, MatchId);
            StableHash.Add(ref hash, catalog.CatalogVersion);
            StableHash.Add(ref hash, unchecked((ulong)catalog.SchemaVersion));
            StableHash.Add(ref hash, unchecked((ulong)MatchCredits));

            for (int index = 0; index < catalog.Items.Count; index++)
            {
                SandboxShopItemDefinition item = catalog.Items[index];
                StableHash.Add(ref hash, item.Id);
                StableHash.Add(ref hash, unchecked((ulong)GetOwnedQuantity(item.Id)));
            }

            List<string> requestIds = new(processedByRequest.Keys);
            requestIds.Sort(StringComparer.Ordinal);
            for (int index = 0; index < requestIds.Count; index++)
            {
                ProcessedPurchase processed = processedByRequest[requestIds[index]];
                StableHash.Add(ref hash, processed.Command.RequestId);
                StableHash.Add(ref hash, processed.Command.ItemId);
                StableHash.Add(ref hash, unchecked((ulong)processed.Command.Quantity));
                StableHash.Add(ref hash, unchecked((ulong)processed.Result.Status));
                StableHash.Add(ref hash, unchecked((ulong)processed.Result.GrantedQuantity));
                StableHash.Add(ref hash, unchecked((ulong)processed.Result.OwnedQuantityAfter));
                StableHash.Add(ref hash, unchecked((ulong)processed.Result.MatchCreditsAfter));
            }

            return hash;
        }

        private SandboxShopPurchaseResult Evaluate(SandboxShopPurchaseCommand command)
        {
            if (command.Quantity <= 0)
                return Reject(command, SandboxShopPurchaseStatus.RejectedInvalidQuantity);

            if (!catalog.TryGetItem(command.ItemId, out SandboxShopItemDefinition item))
                return Reject(command, SandboxShopPurchaseStatus.RejectedUnknownItem);

            int owned = GetOwnedQuantity(command.ItemId);
            if (command.Quantity > item.MaximumPerMatch - owned)
                return Reject(command, SandboxShopPurchaseStatus.RejectedLimitReached);

            long totalPrice = (long)item.MatchCreditPrice * command.Quantity;
            if (totalPrice > MatchCredits)
                return Reject(command, SandboxShopPurchaseStatus.RejectedInsufficientMatchCredits);

            MatchCredits -= (int)totalPrice;
            int newOwned = owned + command.Quantity;
            ownedByItem[command.ItemId] = newOwned;
            return new SandboxShopPurchaseResult(
                command.RequestId,
                command.ItemId,
                SandboxShopPurchaseStatus.Purchased,
                command.Quantity,
                newOwned,
                MatchCredits);
        }

        private SandboxShopPurchaseResult Reject(
            SandboxShopPurchaseCommand command,
            SandboxShopPurchaseStatus status)
        {
            return new SandboxShopPurchaseResult(
                command.RequestId,
                command.ItemId,
                status,
                grantedQuantity: 0,
                ownedQuantityAfter: GetOwnedQuantity(command.ItemId),
                matchCreditsAfter: MatchCredits);
        }
    }
}
