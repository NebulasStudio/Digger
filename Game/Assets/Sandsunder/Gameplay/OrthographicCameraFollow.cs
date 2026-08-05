using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Presentation-only camera rig for the top-down sandbox. Simulation never reads this transform.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class OrthographicCameraFollow : MonoBehaviour
    {
        private const float DefaultOrthographicSize = 5.4f;
        private const int DefaultPixelsPerUnit = 16;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private TopDownPlayerController aimSource;

        [SerializeField, Min(0f)]
        private float followSharpness = 14f;

        [SerializeField, Min(0f)]
        private float aimLookAhead = 1.2f;

        [SerializeField]
        private Vector2 minimumBounds = new(-24f, -16f);

        [SerializeField]
        private Vector2 maximumBounds = new(24f, 16f);

        [SerializeField, Min(1)]
        private int pixelsPerUnit = DefaultPixelsPerUnit;

        private Camera controlledCamera;
        private float shakeAmplitude;
        private float shakeDuration;
        private float shakeRemaining;
        private uint shakeSequence;

        public Vector2 CurrentLookAhead => aimSource != null
            ? aimSource.AimDirection.normalized * aimLookAhead
            : Vector2.zero;

        public void Configure(Transform followTarget, float sharpness)
        {
            target = followTarget;
            followSharpness = Mathf.Max(0f, sharpness);
        }

        public void Configure(
            Transform followTarget,
            TopDownPlayerController controller,
            float sharpness,
            Vector2 minBounds,
            Vector2 maxBounds,
            float configuredAimLookAhead)
        {
            target = followTarget;
            aimSource = controller;
            followSharpness = Mathf.Max(0f, sharpness);
            minimumBounds = Vector2.Min(minBounds, maxBounds);
            maximumBounds = Vector2.Max(minBounds, maxBounds);
            aimLookAhead = Mathf.Max(0f, configuredAimLookAhead);
            EnsureCamera();
            controlledCamera.orthographicSize = DefaultOrthographicSize;
        }

        public void SetPixelDensity(int configuredPixelsPerUnit)
        {
            pixelsPerUnit = Mathf.Max(1, configuredPixelsPerUnit);
        }

        /// <summary>Adds a cosmetic, unscaled-time camera impulse. Safe to call repeatedly.</summary>
        public void Shake(float amplitude = 0.12f, float duration = 0.12f)
        {
            if (amplitude <= 0f || duration <= 0f)
            {
                return;
            }

            shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
            shakeSequence++;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            EnsureCamera();
            Vector2 desired = CalculateDesiredPosition(includeShake: false);
            transform.position = new Vector3(desired.x, desired.y, transform.position.z);
        }

        internal Vector2 CalculateDesiredPosition(bool includeShake)
        {
            if (target == null)
            {
                return transform.position;
            }

            EnsureCamera();
            Vector2 desired = (Vector2)target.position + CurrentLookAhead;
            float verticalExtent = controlledCamera.orthographicSize;
            float horizontalExtent = verticalExtent * Mathf.Max(0.01f, controlledCamera.aspect);

            float minX = minimumBounds.x + horizontalExtent;
            float maxX = maximumBounds.x - horizontalExtent;
            float minY = minimumBounds.y + verticalExtent;
            float maxY = maximumBounds.y - verticalExtent;
            desired.x = ClampWithCollapsedRange(desired.x, minX, maxX);
            desired.y = ClampWithCollapsedRange(desired.y, minY, maxY);

            if (includeShake && shakeRemaining > 0f)
            {
                desired += CalculateShakeOffset();
            }

            float snapStep = 1f / Mathf.Max(1, pixelsPerUnit);
            desired.x = Mathf.Round(desired.x / snapStep) * snapStep;
            desired.y = Mathf.Round(desired.y / snapStep) * snapStep;
            return desired;
        }

        private void Awake()
        {
            EnsureCamera();
            controlledCamera.orthographic = true;
            controlledCamera.orthographicSize = DefaultOrthographicSize;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null)
                {
                    var controller = FindFirstObjectByType<TopDownPlayerController>();
                    if (controller != null) playerObj = controller.gameObject;
                }

                if (playerObj != null)
                {
                    target = playerObj.transform;
                }
                else
                {
                    return;
                }
            }

            if (shakeRemaining > 0f)
            {
                shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
            }

            Vector2 desired = CalculateDesiredPosition(includeShake: true);
            Vector2 current = transform.position;
            Vector2 smoothed;
            if (followSharpness <= 0f)
            {
                smoothed = desired;
            }
            else
            {
                float blend = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                smoothed = Vector2.Lerp(current, desired, blend);
            }

            float snapStep = 1f / Mathf.Max(1, pixelsPerUnit);
            smoothed.x = Mathf.Round(smoothed.x / snapStep) * snapStep;
            smoothed.y = Mathf.Round(smoothed.y / snapStep) * snapStep;
            transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);
        }

        private Vector2 CalculateShakeOffset()
        {
            float normalized = shakeDuration > 0f ? shakeRemaining / shakeDuration : 0f;
            float envelope = normalized * normalized;
            float phase = (Time.unscaledTime * 73f) + (shakeSequence * 1.618f);
            return new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.37f))
                * shakeAmplitude
                * envelope;
        }

        private void EnsureCamera()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }
        }

        private static float ClampWithCollapsedRange(float value, float minimum, float maximum)
        {
            return minimum <= maximum
                ? Mathf.Clamp(value, minimum, maximum)
                : (minimum + maximum) * 0.5f;
        }
    }
}
