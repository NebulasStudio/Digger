using UnityEngine;

namespace Sandsunder.Gameplay
{

public enum AimInputDevice
{
    None = 0,
    Mouse = 1,
    Gamepad = 2,
}

public sealed class AimInputArbiter
{
    private const float MinimumDirectionMagnitudeSquared = 0.000001f;

    private double lastActivityTime = double.NegativeInfinity;

    public AimInputArbiter(Vector2 initialAim)
    {
        LastValidAim = NormalizeOrRight(initialAim);
    }

    public AimInputDevice Owner { get; private set; }

    public Vector2 LastValidAim { get; private set; }

    public bool SubmitMouseWorldDirection(Vector2 worldDirection, double activityTime)
    {
        return TryTakeOwnership(AimInputDevice.Mouse, worldDirection, activityTime);
    }

    public bool SubmitGamepadStick(Vector2 stick, float deadZone, double activityTime)
    {
        float clampedDeadZone = Mathf.Clamp(deadZone, 0f, 0.95f);
        if (stick.sqrMagnitude <= clampedDeadZone * clampedDeadZone)
        {
            return false;
        }

        return TryTakeOwnership(AimInputDevice.Gamepad, stick, activityTime);
    }

    private bool TryTakeOwnership(AimInputDevice device, Vector2 direction, double activityTime)
    {
        if (direction.sqrMagnitude < MinimumDirectionMagnitudeSquared || activityTime <= lastActivityTime)
        {
            return false;
        }

        Owner = device;
        LastValidAim = direction.normalized;
        lastActivityTime = activityTime;
        return true;
    }

    private static Vector2 NormalizeOrRight(Vector2 direction)
    {
        return direction.sqrMagnitude < MinimumDirectionMagnitudeSquared
            ? Vector2.right
            : direction.normalized;
    }
}
}
