using NUnit.Framework;
using Sandsunder.Domain;
using Sandsunder.Gameplay;
using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class SandboxDungeonRuntimeTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyAll<SandboxDungeonController>();
            DestroyAll<PrototypeTunnelSystem>();
            DestroyAll<DigDepthSystem>();
        }

        [Test]
        public void IdenticalSeedAndInputs_ProduceIdenticalRuntimeStateHash()
        {
            var first = new SandboxDungeonRuntime(0x5A17UL, new GridCell(8, -3));
            var second = new SandboxDungeonRuntime(0x5A17UL, new GridCell(8, -3));

            Assert.That(first.ComputeStateHash(), Is.EqualTo(second.ComputeStateHash()));
            Assert.That(first.Enter(), Is.True);
            Assert.That(second.Enter(), Is.True);
            Assert.That(first.TryClaimFinalChest(out int firstAward), Is.True);
            Assert.That(second.TryClaimFinalChest(out int secondAward), Is.True);

            Assert.That(secondAward, Is.EqualTo(firstAward));
            Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));
        }

        [Test]
        public void FinalChest_AwardsMatchGoldExactlyOnce()
        {
            var runtime = new SandboxDungeonRuntime(71UL, new GridCell(2, 9));

            Assert.That(runtime.TryClaimFinalChest(out int outsideAward), Is.False);
            Assert.That(outsideAward, Is.Zero);
            Assert.That(runtime.Enter(), Is.True);
            Assert.That(runtime.TryClaimFinalChest(out int firstAward), Is.True);
            Assert.That(firstAward, Is.EqualTo(SandboxDungeonRuntime.DefaultFinalChestGold));
            Assert.That(runtime.MatchGold, Is.EqualTo(firstAward));

            Assert.That(runtime.TryClaimFinalChest(out int duplicateAward), Is.False);
            Assert.That(duplicateAward, Is.Zero);
            Assert.That(runtime.MatchGold, Is.EqualTo(firstAward));
        }

        [Test]
        public void ControllerEntryAndExit_ToggleDepthLayerAndDungeonProjection()
        {
            DigDepthSystem depth = new GameObject("Dungeon Runtime Depth").AddComponent<DigDepthSystem>();
            PrototypeTunnelSystem tunnel = new GameObject("Dungeon Runtime Layer").AddComponent<PrototypeTunnelSystem>();
            SandboxDungeonController controller = new GameObject("Dungeon Runtime Controller").AddComponent<SandboxDungeonController>();
            controller.Configure(depth, tunnel);
            controller.BuildDungeon(99UL, new GridCell(4, 6), new Vector2(3f, -2f));

            Assert.That(controller.DungeonRoot.gameObject.activeSelf, Is.False);
            Assert.That(controller.EnterDungeon(), Is.True);
            Assert.That(controller.CurrentDepth, Is.EqualTo(SandboxDungeonController.DungeonDepth));
            Assert.That(controller.CurrentLayer, Is.EqualTo(MatrixLayerDepth.Subterranean_L1));
            Assert.That(controller.DungeonRoot.gameObject.activeSelf, Is.True);

            Assert.That(controller.ExitDungeon(), Is.True);
            Assert.That(controller.CurrentDepth, Is.EqualTo(SandboxDungeonController.SurfaceDepth));
            Assert.That(controller.CurrentLayer, Is.EqualTo(MatrixLayerDepth.Surface_L0));
            Assert.That(controller.DungeonRoot.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void PathCellsAndWorldConfinement_RemainInsideReviewedCorridor()
        {
            SandboxDungeonController controller = new GameObject("Dungeon Bounds Controller").AddComponent<SandboxDungeonController>();
            controller.BuildDungeon(1234UL, new GridCell(-7, 11), new Vector2(6f, 5f));

            foreach (GridCell pathCell in controller.Layout.OrderedPathCells)
            {
                Assert.That(controller.Runtime.IsWalkableCell(pathCell), Is.True);
                Assert.That(controller.IsWorldPositionInsideWalkableBounds(controller.WorldPositionForCell(pathCell)), Is.True);
            }

            GridCell confinedCell = controller.Runtime.ConfineToPath(new GridCell(500, -500));
            Assert.That(controller.Runtime.IsWalkableCell(confinedCell), Is.True);

            Vector2 confinedWorld = controller.ConfineWorldPosition(new Vector2(1000f, -1000f));
            Assert.That(controller.IsWorldPositionInsideWalkableBounds(confinedWorld), Is.True);

            GridCell first = controller.Layout.OrderedPathCells[0];
            GridCell second = controller.Layout.OrderedPathCells[1];
            Vector2 expectedDirection = (controller.WorldPositionForCell(second) - controller.WorldPositionForCell(first)).normalized;
            Assert.That(controller.GetGuidanceDirection(controller.WorldPositionForCell(first)), Is.EqualTo(expectedDirection));
        }

        [Test]
        public void NearbyReveal_DoesNotExposeTheWholeDungeon()
        {
            SandboxDungeonController controller = new GameObject("Dungeon Reveal Controller").AddComponent<SandboxDungeonController>();
            controller.BuildDungeon(1234UL, new GridCell(2, 3), Vector2.zero);

            int total = controller.DungeonRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true).Length;
            int visible = controller.ApplyProximityVisibilityForTesting(controller.WorldPositionForCell(controller.Layout.EntranceCell));

            Assert.That(visible, Is.GreaterThan(0));
            Assert.That(visible, Is.LessThan(total));
        }

        private static void DestroyAll<T>() where T : Object
        {
            foreach (T item in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item is Component component) Object.DestroyImmediate(component.gameObject);
                else Object.DestroyImmediate(item);
            }
        }
    }
}
