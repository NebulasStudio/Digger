using System;
using System.Collections.Generic;

namespace Sandsunder.Simulation
{
    public enum SandboxShopItemKind
    {
        LoadoutSidegrade = 0,
        Cosmetic = 1,
        Consumable = 2
    }

    public enum SandboxShopPersistence
    {
        MatchOnly = 0
    }

    /// <summary>
    /// Versioned shop data for the local gameplay sandbox. Shop definitions cannot describe
    /// account persistence or a permanent competitive stat modifier.
    /// </summary>
    public readonly struct SandboxShopItemDefinition
    {
        public SandboxShopItemDefinition(
            string id,
            string displayName,
            string description,
            SandboxShopItemKind kind,
            int matchCreditPrice,
            int maximumPerMatch,
            string grantId,
            SandboxShopPersistence persistence = SandboxShopPersistence.MatchOnly)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Item id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));
            if (matchCreditPrice <= 0) throw new ArgumentOutOfRangeException(nameof(matchCreditPrice));
            if (maximumPerMatch <= 0) throw new ArgumentOutOfRangeException(nameof(maximumPerMatch));
            if (string.IsNullOrWhiteSpace(grantId)) throw new ArgumentException("Grant id is required.", nameof(grantId));
            if (persistence != SandboxShopPersistence.MatchOnly)
                throw new ArgumentOutOfRangeException(nameof(persistence), "Sandbox shop grants must be match-local.");

            Id = id;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
            MatchCreditPrice = matchCreditPrice;
            MaximumPerMatch = maximumPerMatch;
            GrantId = grantId;
            Persistence = persistence;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public SandboxShopItemKind Kind { get; }
        public int MatchCreditPrice { get; }
        public int MaximumPerMatch { get; }
        public string GrantId { get; }
        public SandboxShopPersistence Persistence { get; }
        public bool GrantsPermanentCompetitivePower => false;
    }

    /// <summary>Immutable, versioned catalog for the small in-match sandbox shop.</summary>
    public sealed class SandboxShopCatalog
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentCatalogVersion = "sandbox-shop-1";

        public static readonly SandboxShopCatalog Current = new SandboxShopCatalog(
            CurrentSchemaVersion,
            CurrentCatalogVersion,
            new[]
            {
                new SandboxShopItemDefinition(
                    "sidegrade.scimitar",
                    "Brass Scimitar",
                    "Match-only close-range sidegrade; it trades rifle reach for melee handling.",
                    SandboxShopItemKind.LoadoutSidegrade,
                    matchCreditPrice: 45,
                    maximumPerMatch: 1,
                    grantId: "loadout.scimitar"),
                new SandboxShopItemDefinition(
                    "cosmetic.dune-scarf",
                    "Dune Scarf",
                    "Cosmetic sandbox tint with no combat effect or account unlock.",
                    SandboxShopItemKind.Cosmetic,
                    matchCreditPrice: 25,
                    maximumPerMatch: 1,
                    grantId: "cosmetic.dune-scarf"),
                new SandboxShopItemDefinition(
                    "consumable.oxygen-flask",
                    "Oxygen Flask",
                    "Single-match consumable charge; discarded when the sandbox match ends.",
                    SandboxShopItemKind.Consumable,
                    matchCreditPrice: 20,
                    maximumPerMatch: 3,
                    grantId: "consumable.oxygen-flask")
            });

        private readonly Dictionary<string, SandboxShopItemDefinition> byId;
        private readonly SandboxShopItemDefinition[] items;

        public SandboxShopCatalog(
            int schemaVersion,
            string catalogVersion,
            IReadOnlyList<SandboxShopItemDefinition> definitions)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (string.IsNullOrWhiteSpace(catalogVersion))
                throw new ArgumentException("Catalog version is required.", nameof(catalogVersion));
            if (definitions == null || definitions.Count == 0)
                throw new ArgumentException("At least one shop item is required.", nameof(definitions));

            SchemaVersion = schemaVersion;
            CatalogVersion = catalogVersion;
            items = new SandboxShopItemDefinition[definitions.Count];
            byId = new Dictionary<string, SandboxShopItemDefinition>(definitions.Count, StringComparer.Ordinal);

            for (int index = 0; index < definitions.Count; index++)
            {
                SandboxShopItemDefinition definition = definitions[index];
                if (!byId.TryAdd(definition.Id, definition))
                    throw new ArgumentException($"Duplicate shop item id '{definition.Id}'.", nameof(definitions));
                items[index] = definition;
            }
        }

        public int SchemaVersion { get; }
        public string CatalogVersion { get; }
        public IReadOnlyList<SandboxShopItemDefinition> Items => items;

        public bool TryGetItem(string itemId, out SandboxShopItemDefinition definition)
        {
            if (itemId != null) return byId.TryGetValue(itemId, out definition);
            definition = default;
            return false;
        }

        public SandboxShopItemDefinition GetItem(string itemId)
        {
            if (itemId == null) throw new ArgumentNullException(nameof(itemId));
            if (!byId.TryGetValue(itemId, out SandboxShopItemDefinition definition))
                throw new KeyNotFoundException($"Shop item '{itemId}' is not present in catalog '{CatalogVersion}'.");
            return definition;
        }
    }
}
