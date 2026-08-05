using UnityEngine;
using Sandsunder.Simulation;

namespace Sandsunder.Gameplay
{

[CreateAssetMenu(
    fileName = "TopDownMovementProfile",
    menuName = "Sandsunder/Gameplay/Top-down Movement Profile")]
public sealed class TopDownMovementProfile : ScriptableObject
{
    public const int CurrentSchemaVersion = 1;

    [SerializeField, HideInInspector]
    private int schemaVersion = CurrentSchemaVersion;

    [SerializeField, Min(0f)]
    private float mouseActivityThreshold = 0.01f;

    [SerializeField, Min(0f)]
    private float cameraFollowSharpness = 14f;

    public int SchemaVersion => schemaVersion;

    public float BaseMoveSpeed => PlayerKinematicsRules.MilestoneOne.SpeedMillimetresPerSecond / 1000f;

    public float CollisionRadius => PlayerKinematicsRules.MilestoneOne.CollisionRadiusMillimetres / 1000f;

    public float GamepadAimDeadZone => PlayerKinematicsRules.MilestoneOne.AimDeadZoneUnits
        / (float)PlayerKinematicsRules.AxisUnits;

    public float MouseActivityThreshold => mouseActivityThreshold;

    public float CameraFollowSharpness => cameraFollowSharpness;

    private void OnValidate()
    {
        schemaVersion = CurrentSchemaVersion;
        mouseActivityThreshold = Mathf.Max(0f, mouseActivityThreshold);
        cameraFollowSharpness = Mathf.Max(0f, cameraFollowSharpness);
    }
}
}
