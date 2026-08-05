using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Sandsunder.Gameplay;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sandsunder.Tests.Gameplay
{

public sealed class TopDownPlayerPlayModeTests
{
    [UnityTest]
    public IEnumerator Controller_ConfiguresPhysicsAndMovesAtClampedSpeed()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out TopDownPlayerController controller);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        CircleCollider2D circle = player.GetComponent<CircleCollider2D>();

        Assert.That(circle.radius, Is.EqualTo(0.38f).Within(0.0001f));
        Assert.That(body.gravityScale, Is.Zero);
        Assert.That(body.freezeRotation, Is.True);

        PlayerKinematicsState start = controller.KinematicState;
        controller.SetMoveInputForTesting(new Vector2(1f, 1f));
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        PlayerKinematicsState end = controller.KinematicState;
        float distance = Vector2.Distance(
            new Vector2(start.PositionXMillimetres, start.PositionYMillimetres),
            new Vector2(end.PositionXMillimetres, end.PositionYMillimetres)) / 1000f;
        Assert.That(distance, Is.GreaterThan(0f));
        Assert.That(distance, Is.LessThanOrEqualTo(5.2f * Time.fixedDeltaTime * 2.1f));

        Object.Destroy(player);
        Object.Destroy(profile);
    }

    [UnityTest]
    public IEnumerator FocusLoss_ClearsHeldMovementImmediately()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out TopDownPlayerController controller);

        controller.SetMoveInputForTesting(Vector2.right);
        controller.HandleFocusChanged(false);
        Vector2 position = player.transform.position;

        Assert.That(controller.CurrentMoveInput, Is.EqualTo(Vector2.zero));
        yield return new WaitForFixedUpdate();
        Assert.That((Vector2)player.transform.position, Is.EqualTo(position));

        Object.Destroy(player);
        Object.Destroy(profile);
    }

    [UnityTest]
    public IEnumerator Controller_AccumulatesSixtySimulationTicksPerRealSecondAtFiftyHertz()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out TopDownPlayerController controller);
        controller.enabled = false;
        controller.SetMoveInputForTesting(Vector2.right);

        for (int fixedUpdate = 0; fixedUpdate < 50; fixedUpdate++)
        {
            controller.AdvanceSimulation(0.02d);
        }

        Assert.That(controller.KinematicState.Tick, Is.EqualTo(60));
        Assert.That(controller.KinematicState.PositionXMillimetres, Is.EqualTo(5200));
        Assert.That(controller.KinematicStateHash, Is.Not.Zero);

        Object.Destroy(player);
        Object.Destroy(profile);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Controller_FocusLossStopsMovementButContinuesSimulationTicks()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out TopDownPlayerController controller);
        controller.enabled = false;
        controller.SetMoveInputForTesting(Vector2.right);
        controller.HandleFocusChanged(false);

        controller.AdvanceSimulation(1d / 60d);

        Assert.That(controller.KinematicState.Tick, Is.EqualTo(1));
        Assert.That(controller.KinematicState.PositionXMillimetres, Is.Zero);

        Object.Destroy(player);
        Object.Destroy(profile);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Controller_RehydratesRuntimeOnlyStateAfterDomainReloadLikeFieldLoss()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out TopDownPlayerController controller);
        controller.enabled = false;

        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (string fieldName in new[] { "body", "circleCollider", "aimArbiter", "kinematics", "rollMotion" })
        {
            typeof(TopDownPlayerController).GetField(fieldName, PrivateInstance)?.SetValue(controller, null);
        }

        Assert.DoesNotThrow(() => controller.AdvanceSimulation(1d / 60d));
        Assert.That(controller.KinematicState.Tick, Is.EqualTo(1));
        Assert.That(player.GetComponent<Rigidbody2D>().gravityScale, Is.Zero);

        Object.Destroy(player);
        Object.Destroy(profile);
        yield return null;
    }

    private static GameObject CreatePlayer(
        TopDownMovementProfile profile,
        out TopDownPlayerController controller)
    {
        GameObject player = new("Test Player");
        player.SetActive(false);
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CircleCollider2D>();
        controller = player.AddComponent<TopDownPlayerController>();
        controller.Configure(profile, null);
        player.SetActive(true);
        return player;
    }
}
}
