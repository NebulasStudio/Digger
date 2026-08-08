using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sandsunder.Gameplay;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class SandboxShopTests
    {
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            DestroyAll<SandboxShopPanel>();
            DestroyAll<SandboxShopSession>();
            DestroyAll<SandboxModernHUD>();
            DestroyAll<PrototypeInventoryHUD>();
            DestroyAll<EventSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            DestroyAll<SandboxShopPanel>();
            DestroyAll<SandboxShopSession>();
            DestroyAll<SandboxModernHUD>();
            DestroyAll<PrototypeInventoryHUD>();
            DestroyAll<EventSystem>();
        }

        [Test]
        public void Catalog_IsVersionedMatchOnlyAndHorizontal()
        {
            SandboxShopCatalog catalog = SandboxShopCatalog.Current;
            HashSet<SandboxShopItemKind> kinds = new();

            Assert.That(catalog.SchemaVersion, Is.GreaterThan(0));
            Assert.That(catalog.CatalogVersion, Is.Not.Empty);
            foreach (SandboxShopItemDefinition item in catalog.Items)
            {
                kinds.Add(item.Kind);
                Assert.That(item.Persistence, Is.EqualTo(SandboxShopPersistence.MatchOnly));
                Assert.That(item.GrantsPermanentCompetitivePower, Is.False);
                Assert.That(item.MatchCreditPrice, Is.GreaterThan(0));
            }

            Assert.That(kinds, Does.Contain(SandboxShopItemKind.LoadoutSidegrade));
            Assert.That(kinds, Does.Contain(SandboxShopItemKind.Cosmetic));
            Assert.That(kinds, Does.Contain(SandboxShopItemKind.Consumable));
        }

        [Test]
        public void DuplicatePurchase_IsIdempotentAndChargesOnce()
        {
            SandboxShopState state = NewState();
            SandboxShopPurchaseCommand command = new("purchase-1", "sidegrade.scimitar");

            SandboxShopPurchaseResult first = state.ProcessPurchase(command);
            SandboxShopPurchaseResult retry = state.ProcessPurchase(command);

            Assert.That(first.Status, Is.EqualTo(SandboxShopPurchaseStatus.Purchased));
            Assert.That(retry, Is.EqualTo(first));
            Assert.That(state.MatchCredits, Is.EqualTo(55));
            Assert.That(state.GetOwnedQuantity("sidegrade.scimitar"), Is.EqualTo(1));
            Assert.That(state.ProcessedPurchaseCount, Is.EqualTo(1));
        }

        [Test]
        public void ReusedRequestIdWithDifferentPayload_IsRejectedWithoutMutation()
        {
            SandboxShopState state = NewState();
            state.ProcessPurchase(new SandboxShopPurchaseCommand("purchase-1", "cosmetic.dune-scarf"));
            ulong before = state.ComputeStateHash();

            SandboxShopPurchaseResult conflict = state.ProcessPurchase(
                new SandboxShopPurchaseCommand("purchase-1", "consumable.oxygen-flask"));

            Assert.That(conflict.Status, Is.EqualTo(SandboxShopPurchaseStatus.RejectedRequestConflict));
            Assert.That(state.MatchCredits, Is.EqualTo(75));
            Assert.That(state.GetOwnedQuantity("consumable.oxygen-flask"), Is.Zero);
            Assert.That(state.ProcessedPurchaseCount, Is.EqualTo(1));
            Assert.That(state.ComputeStateHash(), Is.EqualTo(before));
        }

        [Test]
        public void IdenticalCommandStreams_ProduceIdenticalStateHashes()
        {
            SandboxShopState first = NewState();
            SandboxShopState second = NewState();
            SandboxShopPurchaseCommand[] commands =
            {
                new("purchase-c", "consumable.oxygen-flask", 2),
                new("purchase-a", "cosmetic.dune-scarf"),
                new("purchase-b", "sidegrade.scimitar"),
                new("purchase-c", "consumable.oxygen-flask", 2)
            };

            foreach (SandboxShopPurchaseCommand command in commands)
            {
                Assert.That(second.ProcessPurchase(command), Is.EqualTo(first.ProcessPurchase(command)));
            }

            Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));
        }

        [Test]
        public void Session_AppliesMatchOnlyInventoryGrantsExactlyOnce()
        {
            PrototypeInventoryHUD inventory = new GameObject("Inventory Test").AddComponent<PrototypeInventoryHUD>();
            SandboxShopSession session = new GameObject("Shop Session Test").AddComponent<SandboxShopSession>();

            SandboxShopPurchaseCommand scimitar = new("grant-scimitar", "sidegrade.scimitar");
            SandboxShopPurchaseCommand oxygen = new("grant-oxygen", "consumable.oxygen-flask");
            SandboxShopPurchaseCommand cosmetic = new("grant-scarf", "cosmetic.dune-scarf");
            session.ProcessPurchase(scimitar);
            session.ProcessPurchase(scimitar);
            session.ProcessPurchase(oxygen);
            session.ProcessPurchase(cosmetic);

            Assert.That(inventory.InventoryItems.Count(item => item == "sword.scimitar"), Is.EqualTo(1));
            Assert.That(inventory.InventoryItems.Count(item => item == "consumable.oxygen-flask"), Is.EqualTo(1));
            Assert.That(session.HasDuneScarfCosmetic, Is.True);
        }

        [Test]
        public void Panel_IsHorizontalControllerFirstMouseClickableAndDoesNotPause()
        {
            SandboxModernHUD hud = new GameObject("HUD Test").AddComponent<SandboxModernHUD>();
            hud.EnsureInitialized();
            SandboxShopPanel panel = new GameObject("Shop Test").AddComponent<SandboxShopPanel>();
            panel.EnsureInitialized();
            Time.timeScale = .65f;

            panel.SetOpen(true);

            Assert.That(panel.ShopRoot.activeSelf, Is.True);
            Assert.That(panel.ItemRow.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(panel.ItemButtonCount, Is.EqualTo(SandboxShopCatalog.Current.Items.Count));
            Assert.That(panel.ItemRow.GetComponentsInChildren<Button>(), Has.Length.EqualTo(panel.ItemButtonCount));
            Assert.That(panel.HasToggleBinding(SandboxShopPanel.KeyboardToggleBinding), Is.True);
            Assert.That(panel.HasToggleBinding(SandboxShopPanel.GamepadToggleBinding), Is.True);
            Assert.That(panel.HasCloseBinding(SandboxShopPanel.GamepadCloseBinding), Is.True);
            EventSystem eventSystem = EventSystem.current ?? UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.currentSelectedGameObject, Is.Not.Null);
            Assert.That(Time.timeScale, Is.EqualTo(.65f));
        }

        private static SandboxShopState NewState()
        {
            return new SandboxShopState("match-test", 100, SandboxShopCatalog.Current);
        }

        private static void DestroyAll<T>() where T : UnityEngine.Object
        {
            foreach (T item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item is Component component) UnityEngine.Object.DestroyImmediate(component.gameObject);
                else UnityEngine.Object.DestroyImmediate(item);
            }
        }
    }
}
