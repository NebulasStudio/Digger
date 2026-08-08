using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sandsunder.Gameplay;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public IEnumerator InventoryModal_BlocksGameplayMovementAndRestoresItOnClose()
    {
        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out TopDownPlayerController controller);
        controller.enabled = false;
        controller.SetMoveInputForTesting(Vector2.right);

        SandboxModernHUD hud = SandboxModernHUD.Instance
            ?? Object.FindFirstObjectByType<SandboxModernHUD>()
            ?? new GameObject("Modal Gate HUD Test").AddComponent<SandboxModernHUD>();
        hud.EnsureInitialized();
        Assert.That(hud.InventoryController, Is.Not.Null);
        hud.InventoryController.SetOpen(true);
        controller.AdvanceSimulation(1d / 60d);
        Assert.That(controller.KinematicState.PositionXMillimetres, Is.Zero);

        hud.InventoryController.SetOpen(false);
        controller.AdvanceSimulation(1d / 60d);
        Assert.That(controller.KinematicState.PositionXMillimetres, Is.GreaterThan(0));

        hud.InventoryController.SetOpen(false);
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

    [UnityTest]
    public IEnumerator Depth_HasNoSpaceOrShiftUpdateBypass_AndDigUsesRmbPlusControllerSouth()
    {
        Assert.That(typeof(DigDepthSystem).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);

        GameObject depthObject = new("Depth Authority Test");
        DigDepthSystem depth = depthObject.AddComponent<DigDepthSystem>();
        Assert.That(depth.ApplyAuthoritativeCellDepth(1), Is.True);
        Assert.That(depth.IsSubterranean, Is.False);

        TopDownMovementProfile profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        GameObject player = CreatePlayer(profile, out _);
        player.SetActive(false);
        PrototypeHealth health = player.AddComponent<PrototypeHealth>();
        PrototypePlayerCombat combat = player.AddComponent<PrototypePlayerCombat>();
        health.Configure(1, configuredTeam: 0, configuredMaximumHealth: 100, shouldRespawn: false);
        combat.Configure(1);
        player.SetActive(true);

        FieldInfo shovelField = typeof(PrototypePlayerCombat).GetField(
            "shovelAction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        InputAction shovelAction = (InputAction)shovelField?.GetValue(combat);
        Assert.That(shovelAction, Is.Not.Null);
        string[] paths = shovelAction.bindings.Select(binding => binding.path).ToArray();
        Assert.That(paths, Does.Contain("<Mouse>/rightButton"));
        Assert.That(paths, Does.Contain("<Gamepad>/buttonSouth"));
        Assert.That(paths.Any(path => path.Contains("Keyboard")), Is.False);

        Object.Destroy(player);
        Object.Destroy(profile);
        Object.Destroy(depthObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator OxygenController_DealsFiveDamagePerSecondAfterHundredSecondTank_ThenRefills()
    {
        GameObject depthObject = new("Oxygen Depth Authority Test");
        DigDepthSystem depth = depthObject.AddComponent<DigDepthSystem>();

        GameObject player = new("Oxygen Player Test");
        player.SetActive(false);
        player.AddComponent<CircleCollider2D>();
        PrototypeHealth health = player.AddComponent<PrototypeHealth>();
        health.Configure(1, configuredTeam: 0, configuredMaximumHealth: 100, shouldRespawn: false);
        SubterraneanOxygenController oxygen = player.AddComponent<SubterraneanOxygenController>();
        oxygen.ConfigureDepthSource(depth);
        player.SetActive(true);

        depth.SetAuthoritativeDepth(2);
        oxygen.AdvanceSimulation(100d);
        Assert.That(oxygen.CurrentOxygen, Is.Zero);
        Assert.That(health.CurrentHealth, Is.EqualTo(100));

        oxygen.AdvanceSimulation(1d);
        Assert.That(health.CurrentHealth, Is.EqualTo(95));

        depth.SetAuthoritativeDepth(0);
        oxygen.AdvanceSimulation(20d);
        Assert.That(oxygen.CurrentOxygen, Is.EqualTo(100f).Within(0.0001f));

        Object.Destroy(player);
        Object.Destroy(depthObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DigTerrainView_ReusesExactlyOneBuildSafeOverlayPerCell()
    {
        GameObject viewObject = new("Dig Terrain View Test");
        DigTerrainView view = viewObject.AddComponent<DigTerrainView>();

        view.SetCellDepth(new Vector2(2.5f, -1.5f), 1);
        view.SetCellDepth(new Vector2(2.5f, -1.5f), 2);
        view.SetCellDepth(new Vector2(2.75f, -1.25f), 2);

        Assert.That(view.ActiveCellCount, Is.EqualTo(1));
        Assert.That(viewObject.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1));

        view.SetCellDepth(new Vector2(2.5f, -1.5f), 0);
        Assert.That(view.ActiveCellCount, Is.Zero);

        Object.Destroy(viewObject);
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
