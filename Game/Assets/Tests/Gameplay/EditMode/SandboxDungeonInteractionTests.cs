using NUnit.Framework;
using Sandsunder.Gameplay;
using UnityEngine;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class SandboxDungeonInteractionTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyTestObjects();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyTestObjects();
        }

        [Test]
        public void LockedDoor_RejectsEntryWithoutKey_AndHasNoShiftBinding()
        {
            TestRig rig = CreateRig();
            PrototypeDesertRuinDoor door = new GameObject("Locked Door").AddComponent<PrototypeDesertRuinDoor>();
            door.ConfigureLocked(true);

            Assert.That(door.TryInteract(rig.Player), Is.False);
            Assert.That(door.IsLocked, Is.True);
            Assert.That(rig.Depth.CurrentDepth, Is.Zero);
            Assert.That(rig.Interaction.HasInteractionBinding(SandboxInteractionController.KeyboardInteractionBinding), Is.True);
            Assert.That(rig.Interaction.HasInteractionBinding(SandboxInteractionController.GamepadInteractionBinding), Is.True);
            Assert.That(rig.Interaction.HasInteractionBinding("<Keyboard>/leftShift"), Is.False);
        }

        [Test]
        public void SingleAction_CollectsOnlyNearestAvailableWorldTarget()
        {
            TestRig rig = CreateRig();
            PrototypePickup near = PrototypePickup.Spawn(new Vector2(0.5f, 0f), 101, "loot.near");
            PrototypePickup far = PrototypePickup.Spawn(new Vector2(1.2f, 0f), 102, "loot.far");

            rig.Interaction.RefreshTarget();
            Assert.That(rig.Interaction.IsInputBlockedByModal, Is.False);
            Assert.That(rig.Interaction.HasWorldTarget, Is.True);
            Assert.That(rig.Interaction.TryInteractNearest(), Is.True);

            Assert.That(near.IsCollected, Is.True);
            Assert.That(far.IsCollected, Is.False);
            Assert.That(rig.Player.PickupCount, Is.EqualTo(1));
        }

        [Test]
        public void SceneStylePickup_InitializesFromSavedObjectName_AndCanBeCollectedWithContextAction()
        {
            TestRig rig = CreateRig();
            GameObject pickupObject = new("Pickup weapon.rifle");
            pickupObject.transform.position = new Vector2(0.5f, 0f);
            pickupObject.AddComponent<CircleCollider2D>().isTrigger = true;
            PrototypePickup pickup = pickupObject.AddComponent<PrototypePickup>();
            // EditMode AddComponent does not guarantee OnEnable; scene load in Player does.
            SandboxInteractionController.Register(pickup);

            rig.Interaction.RefreshTarget();

            Assert.That(pickup.LootId, Is.EqualTo("weapon.rifle"));
            Assert.That(pickup.IsInteractionAvailable(rig.Player), Is.True);
            Assert.That(rig.Interaction.TryInteractNearest(), Is.True);
            Assert.That(pickup.IsCollected, Is.True);
            Assert.That(rig.Player.PickupCount, Is.EqualTo(1));
        }

        [Test]
        public void EnterAndExit_RestoreAuthoritativeDepthAndMatrixLayer()
        {
            TestRig rig = CreateRig();

            Assert.That(rig.Dungeon.EnterDungeon(), Is.True);
            Assert.That(rig.Depth.CurrentDepth, Is.EqualTo(SandboxDungeonController.DungeonDepth));
            Assert.That(rig.Tunnel.CurrentLayer, Is.EqualTo(MatrixLayerDepth.Subterranean_L1));

            Assert.That(rig.Dungeon.ExitDungeon(), Is.True);
            Assert.That(rig.Depth.CurrentDepth, Is.EqualTo(SandboxDungeonController.SurfaceDepth));
            Assert.That(rig.Tunnel.CurrentLayer, Is.EqualTo(MatrixLayerDepth.Surface_L0));
        }

        [Test]
        public void LootSpawnedAtDepthTwo_RemainsReachableOnSubterraneanLayerOnly()
        {
            TestRig rig = CreateRig();
            PrototypePickup pickup = PrototypePickup.Spawn(
                new Vector2(0.4f, 0f),
                201,
                "loot.depth-two",
                SandboxDungeonController.DungeonDepth);

            Assert.That(pickup.RequiredDepth, Is.EqualTo(SandboxDungeonController.DungeonDepth));
            Assert.That(pickup.LootLayer, Is.EqualTo(MatrixLayerDepth.Subterranean_L1));
            Assert.That(pickup.IsAvailableAtDepth(SandboxDungeonController.SurfaceDepth), Is.False);
            Assert.That(pickup.IsAvailableAtDepth(SandboxDungeonController.DungeonDepth), Is.True);
            Assert.That(rig.Interaction.TryInteractNearest(), Is.False);
            Assert.That(rig.Dungeon.EnterDungeon(), Is.True);
            Assert.That(rig.Interaction.IsInputBlockedByModal, Is.False);
            Assert.That(
                pickup.IsInteractionAvailable(rig.Player),
                Is.True,
                $"playerNull={rig.Player == null}; collected={pickup.IsCollected}; "
                + $"dungeonInstanceNull={SandboxDungeonController.Instance == null}; "
                + $"instanceDepth={SandboxDungeonController.Instance?.CurrentDepth}; rigDepth={rig.Depth.CurrentDepth}");
            rig.Interaction.RefreshTarget();
            Assert.That(rig.Interaction.HasWorldTarget, Is.True);
            Assert.That(rig.Interaction.TryInteractNearest(), Is.True);
            Assert.That(pickup.IsCollected, Is.True);
        }

        private static TestRig CreateRig()
        {
            DigDepthSystem depth = new GameObject("Depth Test").AddComponent<DigDepthSystem>();
            PrototypeTunnelSystem tunnel = new GameObject("Tunnel Test").AddComponent<PrototypeTunnelSystem>();
            SandboxDungeonController dungeon = new GameObject("Dungeon Test").AddComponent<SandboxDungeonController>();
            dungeon.Configure(depth, tunnel);
            new GameObject("Inventory Test").AddComponent<PrototypeInventoryHUD>();

            GameObject playerObject = new("Player Test");
            PrototypePlayerCombat player = playerObject.AddComponent<PrototypePlayerCombat>();
            player.Configure(1);
            SandboxInteractionController interaction = playerObject.AddComponent<SandboxInteractionController>();
            interaction.Configure(player, 1.8f);
            return new TestRig(depth, tunnel, dungeon, player, interaction);
        }

        private static void DestroyTestObjects()
        {
            DestroyAll<SandboxInteractionController>();
            DestroyAll<PrototypeDesertRuinDoor>();
            DestroyAll<PrototypePickup>();
            DestroyAll<SandboxDungeonController>();
            DestroyAll<PrototypeTunnelSystem>();
            DestroyAll<DigDepthSystem>();
            DestroyAll<PrototypeInventoryHUD>();
            DestroyAll<PrototypePlayerCombat>();
        }

        private static void DestroyAll<T>() where T : Object
        {
            foreach (T item in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item is Component component) Object.DestroyImmediate(component.gameObject);
                else Object.DestroyImmediate(item);
            }
        }

        private readonly struct TestRig
        {
            public TestRig(
                DigDepthSystem depth,
                PrototypeTunnelSystem tunnel,
                SandboxDungeonController dungeon,
                PrototypePlayerCombat player,
                SandboxInteractionController interaction)
            {
                Depth = depth;
                Tunnel = tunnel;
                Dungeon = dungeon;
                Player = player;
                Interaction = interaction;
            }

            public DigDepthSystem Depth { get; }
            public PrototypeTunnelSystem Tunnel { get; }
            public SandboxDungeonController Dungeon { get; }
            public PrototypePlayerCombat Player { get; }
            public SandboxInteractionController Interaction { get; }
        }
    }
}
