using System;
using System.Collections.Generic;
using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Local composition root for one disposable sandbox-match shop state.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxShopSession : MonoBehaviour
    {
        public const int DefaultStartingMatchCredits = 100;
        public const string LocalSandboxMatchId = "sandbox.local-match";

        [SerializeField, Min(0)] private int startingMatchCredits = DefaultStartingMatchCredits;
        private int nextUiRequestSequence;
        private readonly HashSet<string> appliedGrantRequests = new(StringComparer.Ordinal);

        public SandboxShopState State { get; private set; }
        public bool HasDuneScarfCosmetic { get; private set; }
        public event Action<SandboxShopPurchaseResult> PurchaseProcessed;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            State ??= new SandboxShopState(
                LocalSandboxMatchId,
                startingMatchCredits,
                SandboxShopCatalog.Current);
        }

        public SandboxShopPurchaseResult PurchaseFromUi(string itemId)
        {
            string requestId = $"sandbox-ui-{++nextUiRequestSequence:000000}";
            return ProcessPurchase(new SandboxShopPurchaseCommand(requestId, itemId));
        }

        public SandboxShopPurchaseResult ProcessPurchase(SandboxShopPurchaseCommand command)
        {
            EnsureInitialized();
            SandboxShopPurchaseResult result = State.ProcessPurchase(command);
            ApplyMatchGrant(command, result);
            PurchaseProcessed?.Invoke(result);
            return result;
        }

        private void ApplyMatchGrant(SandboxShopPurchaseCommand command, SandboxShopPurchaseResult result)
        {
            if (!result.WasPurchased || !appliedGrantRequests.Add(command.RequestId)) return;

            SandboxShopItemDefinition definition = SandboxShopCatalog.Current.GetItem(result.ItemId);
            if (definition.Kind == SandboxShopItemKind.Cosmetic)
            {
                HasDuneScarfCosmetic = true;
                return;
            }

            PrototypeInventoryHUD inventory = PrototypeInventoryHUD.Instance
                ?? FindFirstObjectByType<PrototypeInventoryHUD>();
            if (inventory == null)
            {
                inventory = new GameObject("PrototypeInventoryModel_Shop").AddComponent<PrototypeInventoryHUD>();
            }

            string inventoryItemId = result.ItemId switch
            {
                "sidegrade.scimitar" => "sword.scimitar",
                "consumable.oxygen-flask" => "consumable.oxygen-flask",
                _ => definition.GrantId
            };
            bool allowDuplicate = definition.Kind == SandboxShopItemKind.Consumable;
            for (int index = 0; index < result.GrantedQuantity; index++)
            {
                inventory.TryAddItem(inventoryItemId, allowDuplicate);
            }
        }
    }
}
