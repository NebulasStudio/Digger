using UnityEngine;
using UnityEngine.InputSystem;
using Sandsunder.Simulation;

namespace Sandsunder.Gameplay
{

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class TopDownPlayerController : MonoBehaviour
{
    [SerializeField]
    private TopDownMovementProfile profile;

    [SerializeField]
    private Camera worldCamera;

    private Rigidbody2D body;
    private CircleCollider2D circleCollider;
    private InputActionMap inputMap;
    private InputAction moveAction;
    private InputAction mouseDeltaAction;
    private InputAction rightStickAction;
    private AimInputArbiter aimArbiter;
    private PlayerKinematics kinematics;
    private CombatRollMotion rollMotion;
    private Vector2 moveInput;
    private Vector2 committedRollOffset;
    private bool hasFocus = true;
    private double simulationAccumulator;

    public Vector2 AimDirection => aimArbiter?.LastValidAim ?? Vector2.right;

    public AimInputDevice ActiveAimDevice => aimArbiter?.Owner ?? AimInputDevice.None;

    public PlayerKinematicsState KinematicState
    {
        get
        {
            EnsureRuntimeState();
            return kinematics.State;
        }
    }

    public ulong KinematicStateHash
    {
        get
        {
            EnsureRuntimeState();
            return kinematics.ComputeStateHash();
        }
    }

    public Vector2 AuthoritativeWorldPosition
    {
        get
        {
            EnsureRuntimeState();
            if (rollMotion != null && rollMotion.IsActive)
            {
                return new Vector2(
                    rollMotion.PositionXMillimetres / 1000f,
                    rollMotion.PositionYMillimetres / 1000f);
            }

            PlayerKinematicsState state = kinematics.State;
            return new Vector2(
                state.PositionXMillimetres / 1000f,
                state.PositionYMillimetres / 1000f) + committedRollOffset;
        }
    }

    public int CurrentDepth => DigDepthSystem.Instance?.CurrentDepth ?? 0;

    /// <summary>Compatibility entrypoint for authoritative scene interactions such as tunnel exits.</summary>
    public void SetDepth(int depth)
    {
        DigDepthSystem.Instance?.SetAuthoritativeDepth(depth);
    }

    internal Vector2 CurrentMoveInput => moveInput;

    public void Configure(TopDownMovementProfile movementProfile, Camera aimCamera)
    {
        profile = movementProfile;
        worldCamera = aimCamera;

        if (body != null)
        {
            ApplyPhysicsConfiguration();
        }
    }

    public bool BeginPrototypeRoll(Vector2 direction)
    {
        EnsureRuntimeState();
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        PlayerKinematicsState state = kinematics.State;
        Vector2 current = new Vector2(
            state.PositionXMillimetres / 1000f,
            state.PositionYMillimetres / 1000f) + committedRollOffset;
        return rollMotion.Begin(
            Mathf.RoundToInt(current.x * 1000f),
            Mathf.RoundToInt(current.y * 1000f),
            Mathf.RoundToInt(direction.x * PlayerKinematicsRules.AxisUnits),
            Mathf.RoundToInt(direction.y * PlayerKinematicsRules.AxisUnits));
    }

    private void Awake()
    {
        EnsureRuntimeState();
    }

    private void OnEnable()
    {
        EnsureRuntimeState();
        if (hasFocus)
        {
            inputMap?.Enable();
        }
    }

    private void OnDisable()
    {
        inputMap?.Disable();
        ClearHeldInput();
    }

    private void OnDestroy()
    {
        if (inputMap == null)
        {
            return;
        }

        if (moveAction != null)
        {
            moveAction.performed -= OnMoveChanged;
            moveAction.canceled -= OnMoveChanged;
        }

        if (mouseDeltaAction != null)
        {
            mouseDeltaAction.performed -= OnMouseMoved;
        }

        if (rightStickAction != null)
        {
            rightStickAction.performed -= OnRightStickChanged;
        }

        inputMap.Dispose();
    }

    private void FixedUpdate()
    {
        AdvanceSimulation(Time.fixedDeltaTime);
    }

    public bool IsDiggingChanneling { get; set; }
    public float CurrentStamina { get; private set; } = 100f;
    public float MaxStamina { get; } = 100f;

    public bool TryConsumeStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            return true;
        }
        return false;
    }

    internal void AdvanceSimulation(double elapsedSeconds)
    {
        EnsureRuntimeState();
        if (elapsedSeconds <= 0d)
        {
            return;
        }

        if (IsDiggingChanneling)
        {
            moveInput = Vector2.zero;
            CurrentStamina = Mathf.Max(0f, CurrentStamina - (float)elapsedSeconds * 15f);
        }
        else if (!rollMotion.IsActive)
        {
            CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + (float)elapsedSeconds * 22f);
        }

        simulationAccumulator += elapsedSeconds;
        double tickDuration = 1d / PlayerKinematicsRules.MilestoneOne.TicksPerSecond;
        bool stepped = false;

        while (simulationAccumulator + 0.000000000001d >= tickDuration)
        {
            Vector2 aim = aimArbiter.LastValidAim;
            bool rollingThisTick = rollMotion.IsActive;
            bool modalBlocked = IsGameplayModalOpen();
            Vector2 effectiveMove = (IsDiggingChanneling || rollingThisTick || modalBlocked)
                ? Vector2.zero
                : moveInput;
            PlayerKinematicsInput input = PlayerKinematicsInput.Create(
                Mathf.RoundToInt(effectiveMove.x * PlayerKinematicsRules.AxisUnits),
                Mathf.RoundToInt(effectiveMove.y * PlayerKinematicsRules.AxisUnits),
                Mathf.RoundToInt(aim.x * PlayerKinematicsRules.AxisUnits),
                Mathf.RoundToInt(aim.y * PlayerKinematicsRules.AxisUnits),
                hasFocus);

            kinematics.Step(input);
            if (rollingThisTick)
            {
                rollMotion.Step();
                if (!rollMotion.IsActive)
                {
                    PlayerKinematicsState completedState = kinematics.State;
                    committedRollOffset = new Vector2(
                        (rollMotion.PositionXMillimetres - completedState.PositionXMillimetres) / 1000f,
                        (rollMotion.PositionYMillimetres - completedState.PositionYMillimetres) / 1000f);
                }
            }

            simulationAccumulator -= tickDuration;
            stepped = true;
        }

        if (!stepped)
        {
            return;
        }

        // PlayerKinematics/CombatRollMotion are the sole movement authority. Rigidbody2D only
        // projects their position so presentation and physics never integrate a second velocity.
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.MovePosition(AuthoritativeWorldPosition);
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        HandleFocusChanged(focused);
    }

    internal void HandleFocusChanged(bool focused)
    {
        hasFocus = focused;
        ClearHeldInput();

        if (focused && isActiveAndEnabled)
        {
            inputMap?.Enable();
        }
        else
        {
            inputMap?.Disable();
        }
    }

    internal void SetMoveInputForTesting(Vector2 input)
    {
        moveInput = TopDownMovementMath.ClampInput(input);
    }

    private void CreateInputActions()
    {
        inputMap = new InputActionMap("Top-down Gameplay");

        moveAction = inputMap.AddAction("Move", InputActionType.Value);
        moveAction.expectedControlType = "Vector2";
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        mouseDeltaAction = inputMap.AddAction("Mouse Aim Activity", InputActionType.PassThrough);
        mouseDeltaAction.expectedControlType = "Vector2";
        mouseDeltaAction.AddBinding("<Mouse>/delta");

        rightStickAction = inputMap.AddAction("Gamepad Aim", InputActionType.PassThrough);
        rightStickAction.expectedControlType = "Vector2";
        rightStickAction.AddBinding("<Gamepad>/rightStick");

        moveAction.performed += OnMoveChanged;
        moveAction.canceled += OnMoveChanged;
        mouseDeltaAction.performed += OnMouseMoved;
        rightStickAction.performed += OnRightStickChanged;
    }

    private void OnMoveChanged(InputAction.CallbackContext context)
    {
        moveInput = TopDownMovementMath.ClampInput(context.ReadValue<Vector2>());
    }

    private void OnMouseMoved(InputAction.CallbackContext context)
    {
        EnsureRuntimeState();
        if (profile == null || Mouse.current == null)
        {
            return;
        }

        Vector2 delta = context.ReadValue<Vector2>();
        float threshold = profile.MouseActivityThreshold;
        if (delta.sqrMagnitude <= threshold * threshold)
        {
            return;
        }

        Camera cameraForAim = worldCamera != null ? worldCamera : Camera.main;
        if (cameraForAim == null)
        {
            return;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = cameraForAim.ScreenToWorldPoint(pointerPosition);
        PlayerKinematicsState state = kinematics.State;
        Vector2 authoritativePosition = new(
            state.PositionXMillimetres / 1000f,
            state.PositionYMillimetres / 1000f);
        Vector2 worldDirection = (Vector2)worldPosition - authoritativePosition;
        aimArbiter.SubmitMouseWorldDirection(worldDirection, context.time);
    }

    private void OnRightStickChanged(InputAction.CallbackContext context)
    {
        EnsureRuntimeState();
        if (profile == null)
        {
            return;
        }

        aimArbiter.SubmitGamepadStick(
            context.ReadValue<Vector2>(),
            profile.GamepadAimDeadZone,
            context.time);
    }

    private void ClearHeldInput()
    {
        moveInput = Vector2.zero;
    }

    private static bool IsGameplayModalOpen()
    {
        bool inventoryOpen = SandboxModernHUD.Instance != null
            && SandboxModernHUD.Instance.InventoryController != null
            && SandboxModernHUD.Instance.InventoryController.IsOpen;
        bool shopOpen = SandboxShopPanel.Instance != null && SandboxShopPanel.Instance.IsOpen;
        return inventoryOpen || shopOpen;
    }

    private void EnsureRuntimeState()
    {
        body = body != null ? body : GetComponent<Rigidbody2D>();
        circleCollider = circleCollider != null ? circleCollider : GetComponent<CircleCollider2D>();
        aimArbiter = aimArbiter ?? new AimInputArbiter(transform.right);

        if (kinematics == null)
        {
            kinematics = new PlayerKinematics(
                PlayerKinematicsRules.MilestoneOne,
                Mathf.RoundToInt(transform.position.x * 1000f),
                Mathf.RoundToInt(transform.position.y * 1000f));
            committedRollOffset = Vector2.zero;
            simulationAccumulator = 0d;
        }

        rollMotion = rollMotion ?? new CombatRollMotion(
            CombatRules.PrototypeOne,
            PlayerKinematicsRules.MilestoneOne.ArenaHalfWidthMillimetres,
            PlayerKinematicsRules.MilestoneOne.ArenaHalfHeightMillimetres,
            PlayerKinematicsRules.MilestoneOne.CollisionRadiusMillimetres);
        worldCamera = worldCamera != null ? worldCamera : Camera.main;

        if (inputMap == null)
        {
            CreateInputActions();
            if (hasFocus && isActiveAndEnabled)
            {
                inputMap.Enable();
            }
        }

        ApplyPhysicsConfiguration();
    }

    private void ApplyPhysicsConfiguration()
    {
        if (body == null || circleCollider == null || profile == null)
        {
            return;
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        circleCollider.radius = profile.CollisionRadius;
        circleCollider.isTrigger = false;
    }
}
}
