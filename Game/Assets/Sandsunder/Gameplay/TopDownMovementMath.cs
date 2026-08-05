using UnityEngine;

namespace Sandsunder.Gameplay
{

public static class TopDownMovementMath
{
    public static Vector2 ClampInput(Vector2 input)
    {
        return Vector2.ClampMagnitude(input, 1f);
    }

    public static Vector2 Velocity(Vector2 input, float speed)
    {
        return ClampInput(input) * Mathf.Max(0f, speed);
    }
}
}
