using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandsunder.Gameplay
{
    public interface ISandboxInteractable
    {
        MonoBehaviour InteractionComponent { get; }
        Transform InteractionTransform { get; }
        bool IsInteractionAvailable(PrototypePlayerCombat player);
        string GetInteractionPrompt(PrototypePlayerCombat player);
        bool TryInteract(PrototypePlayerCombat player);
    }

    /// <summary>Owns the sandbox's single contextual action.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxInteractionController : MonoBehaviour
    {
        public const string KeyboardInteractionBinding = "<Keyboard>/e";
        public const string GamepadInteractionBinding = "<Gamepad>/buttonWest";

        private static readonly HashSet<ISandboxInteractable> Interactables = new();
        private static readonly List<ISandboxInteractable> StaleInteractables = new();

        [SerializeField] private PrototypePlayerCombat player;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.8f;

        private InputAction interactAction;

        public ISandboxInteractable CurrentTarget { get; private set; }
        public string CurrentPrompt { get; private set; } = string.Empty;
        public bool HasWorldTarget => CurrentTarget != null;
        public bool IsInputBlockedByModal =>
            (SandboxModernHUD.Instance?.InventoryController?.IsOpen ?? false)
            || (SandboxShopPanel.Instance?.IsOpen ?? false);

        public event Action InteractionStateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            Interactables.Clear();
            StaleInteractables.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            PrototypePlayerCombat localPlayer = FindFirstObjectByType<PrototypePlayerCombat>();
            if (localPlayer != null && localPlayer.GetComponent<SandboxInteractionController>() == null)
            {
                localPlayer.gameObject.AddComponent<SandboxInteractionController>();
            }

            SandboxDungeonController.EnsureInstance();
        }

        public static void Register(ISandboxInteractable interactable)
        {
            if (interactable != null) Interactables.Add(interactable);
        }

        public static void Unregister(ISandboxInteractable interactable)
        {
            if (interactable != null) Interactables.Remove(interactable);
        }

        public void Configure(PrototypePlayerCombat configuredPlayer, float configuredRadius = 1.8f)
        {
            player = configuredPlayer;
            interactionRadius = Mathf.Max(0.1f, configuredRadius);
            RefreshTarget();
        }

        public bool HasInteractionBinding(string effectivePath)
        {
            EnsureInputAction();
            foreach (InputBinding binding in interactAction.bindings)
            {
                if (binding.effectivePath == effectivePath) return true;
            }

            return false;
        }

        public void RefreshTarget()
        {
            EnsurePlayer();
            if (player == null || IsInputBlockedByModal)
            {
                SetCurrentTarget(null, string.Empty);
                return;
            }

            ISandboxInteractable nearest = null;
            float nearestDistanceSquared = interactionRadius * interactionRadius;
            int nearestInstanceId = int.MaxValue;
            StaleInteractables.Clear();

            foreach (ISandboxInteractable candidate in Interactables)
            {
                MonoBehaviour component = candidate?.InteractionComponent;
                if (component == null)
                {
                    StaleInteractables.Add(candidate);
                    continue;
                }

                if (!component.isActiveAndEnabled || !candidate.IsInteractionAvailable(player)) continue;

                Vector2 delta = candidate.InteractionTransform.position - player.transform.position;
                float distanceSquared = delta.sqrMagnitude;
                int instanceId = component.GetInstanceID();
                if (distanceSquared > nearestDistanceSquared
                    || (Mathf.Approximately(distanceSquared, nearestDistanceSquared)
                        && instanceId >= nearestInstanceId))
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSquared = distanceSquared;
                nearestInstanceId = instanceId;
            }

            foreach (ISandboxInteractable stale in StaleInteractables) Interactables.Remove(stale);
            SetCurrentTarget(nearest, nearest?.GetInteractionPrompt(player) ?? string.Empty);
        }

        public bool TryInteractNearest()
        {
            if (IsInputBlockedByModal) return false;

            RefreshTarget();
            if (CurrentTarget != null)
            {
                bool changed = CurrentTarget.TryInteract(player);
                RefreshTarget();
                return changed;
            }

            return player != null && player.TryUseSelectedConsumable();
        }

        private void Awake()
        {
            EnsurePlayer();
            EnsureInputAction();
        }

        private void OnEnable()
        {
            EnsureInputAction();
            interactAction.performed += OnInteractPerformed;
            interactAction.Enable();
        }

        private void OnDisable()
        {
            if (interactAction == null) return;
            interactAction.performed -= OnInteractPerformed;
            interactAction.Disable();
        }

        private void OnDestroy()
        {
            interactAction?.Dispose();
        }

        private void Update()
        {
            RefreshTarget();
        }

        private void EnsurePlayer()
        {
            player = player != null ? player : GetComponent<PrototypePlayerCombat>();
        }

        private void EnsureInputAction()
        {
            if (interactAction != null) return;
            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding(KeyboardInteractionBinding);
            interactAction.AddBinding(GamepadInteractionBinding);
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteractNearest();
        }

        private void SetCurrentTarget(ISandboxInteractable target, string prompt)
        {
            prompt ??= string.Empty;
            if (ReferenceEquals(CurrentTarget, target) && CurrentPrompt == prompt) return;
            CurrentTarget = target;
            CurrentPrompt = prompt;
            InteractionStateChanged?.Invoke();
        }
    }
}
