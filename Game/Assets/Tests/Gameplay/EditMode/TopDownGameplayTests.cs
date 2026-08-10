using NUnit.Framework;
using Sandsunder.Gameplay;
using Sandsunder.Simulation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sandsunder.Tests.Gameplay
{

public sealed class TopDownGameplayTests
{
    [Test]
    public void MovementProfile_HasVersionedMilestoneOneDefaults()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();

        Assert.That(profile.SchemaVersion, Is.EqualTo(1));
        Assert.That(profile.BaseMoveSpeed, Is.EqualTo(5.2f).Within(0.0001f));
        Assert.That(profile.CollisionRadius, Is.EqualTo(0.38f).Within(0.0001f));

        Object.DestroyImmediate(profile);
    }

    [Test]
    public void MovementMath_ClampsDiagonalInputBeforeApplyingSpeed()
    {
        Vector2 velocity = TopDownMovementMath.Velocity(new Vector2(1f, 1f), 5.2f);

        Assert.That(velocity.magnitude, Is.EqualTo(5.2f).Within(0.0001f));
        Assert.That(velocity.x, Is.EqualTo(velocity.y).Within(0.0001f));
    }

    [Test]
    public void AimArbiter_MostRecentActiveDeviceOwnsAim()
    {
        AimInputArbiter arbiter = new(Vector2.right);

        Assert.That(arbiter.SubmitMouseWorldDirection(Vector2.up, 10d), Is.True);
        Assert.That(arbiter.SubmitGamepadStick(Vector2.left, 0.2f, 11d), Is.True);

        Assert.That(arbiter.Owner, Is.EqualTo(AimInputDevice.Gamepad));
        Assert.That(arbiter.LastValidAim, Is.EqualTo(Vector2.left));
    }

    [Test]
    public void AimArbiter_DeadZoneAndStaleActivityPreserveLastValidAim()
    {
        AimInputArbiter arbiter = new(Vector2.right);
        arbiter.SubmitMouseWorldDirection(Vector2.up, 10d);

        Assert.That(arbiter.SubmitGamepadStick(new Vector2(0.1f, 0f), 0.2f, 11d), Is.False);
        Assert.That(arbiter.SubmitGamepadStick(Vector2.left, 0.2f, 9d), Is.False);
        Assert.That(arbiter.LastValidAim, Is.EqualTo(Vector2.up));
        Assert.That(arbiter.Owner, Is.EqualTo(AimInputDevice.Mouse));
    }

    [Test]
    public void AimArbiter_EqualTimestampKeepsCurrentOwner()
    {
        AimInputArbiter arbiter = new(Vector2.right);
        arbiter.SubmitMouseWorldDirection(Vector2.up, 10d);

        Assert.That(arbiter.SubmitGamepadStick(Vector2.left, 0.2f, 10d), Is.False);
        Assert.That(arbiter.Owner, Is.EqualTo(AimInputDevice.Mouse));
        Assert.That(arbiter.LastValidAim, Is.EqualTo(Vector2.up));
    }

    [Test]
    public void Kinematics_IdenticalInputsProduceIdenticalStateAndHash()
    {
        PlayerKinematics first = new(PlayerKinematicsRules.MilestoneOne);
        PlayerKinematics second = new(PlayerKinematicsRules.MilestoneOne);
        PlayerKinematicsInput[] inputs =
        {
            PlayerKinematicsInput.Create(1000, 1000, 0, 1000),
            PlayerKinematicsInput.Create(-750, 250, -1000, 0),
            PlayerKinematicsInput.Create(0, 0, 100, 0),
            PlayerKinematicsInput.Create(1000, 0, 1000, 0, hasFocus: false),
        };

        foreach (PlayerKinematicsInput input in inputs)
        {
            first.Step(input);
            second.Step(input);
        }

        Assert.That(first.State.PositionXMillimetres, Is.EqualTo(second.State.PositionXMillimetres));
        Assert.That(first.State.PositionYMillimetres, Is.EqualTo(second.State.PositionYMillimetres));
        Assert.That(first.State.FacingX, Is.EqualTo(second.State.FacingX));
        Assert.That(first.State.FacingY, Is.EqualTo(second.State.FacingY));
        Assert.That(first.ComputeStateHash(), Is.EqualTo(second.ComputeStateHash()));
    }

    [Test]
    public void Kinematics_DiagonalInputIsClampedToBaseSpeed()
    {
        PlayerKinematics kinematics = new(PlayerKinematicsRules.MilestoneOne);
        PlayerKinematicsInput input = PlayerKinematicsInput.Create(1000, 1000, 1000, 0);

        for (int tick = 0; tick < PlayerKinematicsRules.MilestoneOne.TicksPerSecond; tick++)
        {
            kinematics.Step(input);
        }

        PlayerKinematicsState state = kinematics.State;
        double distance = System.Math.Sqrt(
            state.PositionXMillimetres * (double)state.PositionXMillimetres
            + state.PositionYMillimetres * (double)state.PositionYMillimetres);
        Assert.That(distance, Is.LessThanOrEqualTo(5200d));
        Assert.That(distance, Is.GreaterThan(5190d));
    }

    [Test]
    public void Kinematics_OppositeDigitalDirectionsCancel()
    {
        PlayerKinematics kinematics = new(PlayerKinematicsRules.MilestoneOne);
        PlayerKinematicsInput input = PlayerKinematicsInput.FromDigitalMovement(
            up: true,
            down: true,
            left: true,
            right: true,
            aimX: 1000,
            aimY: 0);

        kinematics.Step(input);

        Assert.That(kinematics.State.PositionXMillimetres, Is.Zero);
        Assert.That(kinematics.State.PositionYMillimetres, Is.Zero);
    }

    [Test]
    public void Kinematics_AimDeadZoneAndFocusLossPreserveFacingAndStopMovement()
    {
        PlayerKinematics kinematics = new(PlayerKinematicsRules.MilestoneOne);
        kinematics.Step(PlayerKinematicsInput.Create(0, 0, 0, 1000));
        kinematics.Step(PlayerKinematicsInput.Create(1000, 0, 200, 0));
        long positionBeforeFocusLoss = kinematics.State.PositionXMillimetres;

        kinematics.Step(PlayerKinematicsInput.Create(1000, 0, -1000, 0, hasFocus: false));

        Assert.That(kinematics.State.PositionXMillimetres, Is.EqualTo(positionBeforeFocusLoss));
        Assert.That(kinematics.State.FacingX, Is.Zero);
        Assert.That(kinematics.State.FacingY, Is.EqualTo(1000));
    }

    [Test]
    public void Kinematics_RectangularArenaBoundsIncludeCollisionRadius()
    {
        PlayerKinematics kinematics = new(PlayerKinematicsRules.MilestoneOne);
        PlayerKinematicsInput input = PlayerKinematicsInput.Create(1000, 1000, 1000, 0);

        for (int tick = 0; tick < 1000; tick++)
        {
            kinematics.Step(input);
        }

        Assert.That(kinematics.State.PositionXMillimetres, Is.EqualTo(23620));
        Assert.That(kinematics.State.PositionYMillimetres, Is.EqualTo(15620));
    }

    [Test]
    public void Kinematics_AuthoritativePositionSnapshotResetsRemainderAndClampsArena()
    {
        PlayerKinematics first = new(PlayerKinematicsRules.MilestoneOne);
        PlayerKinematics second = new(PlayerKinematicsRules.MilestoneOne);
        first.Step(PlayerKinematicsInput.Create(333, 777, 1000, 0));
        second.Step(PlayerKinematicsInput.Create(-999, 111, 1000, 0));

        Assert.That(first.SetAuthoritativePositionMillimetres(5000, -4000), Is.True);
        Assert.That(second.SetAuthoritativePositionMillimetres(5000, -4000), Is.True);
        first.Step(PlayerKinematicsInput.Create(0, 0, 1000, 0));
        second.Step(PlayerKinematicsInput.Create(0, 0, 1000, 0));

        Assert.That(first.State.PositionXMillimetres, Is.EqualTo(5000));
        Assert.That(first.State.PositionYMillimetres, Is.EqualTo(-4000));
        Assert.That(second.State.PositionXMillimetres, Is.EqualTo(first.State.PositionXMillimetres));
        Assert.That(second.State.PositionYMillimetres, Is.EqualTo(first.State.PositionYMillimetres));

        first.SetAuthoritativePositionMillimetres(long.MaxValue, long.MinValue);
        Assert.That(first.State.PositionXMillimetres, Is.EqualTo(23620));
        Assert.That(first.State.PositionYMillimetres, Is.EqualTo(-15620));
    }

    [Test]
    public void GameplayLab_UsesExpectedWorldDimensions()
    {
        const string scenePath = "Assets/Scenes/GameplayLab.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            GameObject surfaceWorld = System.Array.Find(scene.GetRootGameObjects(), root => root.name == "SurfaceWorldRoot");
            GameObject arena = surfaceWorld?.transform.Find("Arena")?.gameObject;
            GameObject player = System.Array.Find(scene.GetRootGameObjects(), root => root.name == "Player");
            GameObject dungeonController = System.Array.Find(
                scene.GetRootGameObjects(),
                root => root.name == nameof(SandboxDungeonController));
            Assert.That(surfaceWorld, Is.Not.Null);
            Assert.That(arena, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(dungeonController, Is.Not.Null);

            Transform surfaceGrid = arena.transform.Find("SandCellGrid");
            Transform subterraneanGrid = arena.transform.Find("SubterraneanCellGrid");
            Assert.That(surfaceGrid, Is.Not.Null);
            Assert.That(subterraneanGrid, Is.Null, "The dungeon must not be a second full-map floor.");
            Assert.That(surfaceGrid.childCount, Is.EqualTo(48 * 32));

            SandboxActorVisual playerVisual = player.GetComponent<SandboxActorVisual>();
            Assert.That(playerVisual, Is.Not.Null);
            Assert.That(playerVisual.BodyRenderer, Is.Not.Null);
            Assert.That(playerVisual.BodyRenderer.sprite, Is.Not.Null);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
}
