using Sandsunder.Simulation;
using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Final dungeon chest. Its only reward is idempotent, match-local gold.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxDungeonChest : MonoBehaviour, ISandboxInteractable
    {
        [SerializeField] private SandboxDungeonController controller;
        [SerializeField] private SpriteRenderer chestRenderer;

        public bool IsOpened => controller != null
            && controller.Runtime != null
            && controller.Runtime.IsFinalChestClaimed;
        public MonoBehaviour InteractionComponent => this;
        public Transform InteractionTransform => transform;

        public void Configure(SandboxDungeonController configuredController, Sprite configuredSprite = null)
        {
            controller = configuredController;
            chestRenderer = chestRenderer != null ? chestRenderer : GetComponent<SpriteRenderer>();
            if (chestRenderer != null && configuredSprite != null) chestRenderer.sprite = configuredSprite;
            ApplyVisualState();
        }

        public bool IsInteractionAvailable(PrototypePlayerCombat player)
        {
            return player != null
                && controller != null
                && controller.IsInsideDungeon
                && controller.Runtime != null
                && !controller.Runtime.IsFinalChestClaimed;
        }

        public string GetInteractionPrompt(PrototypePlayerCombat player)
        {
            if (IsOpened) return "SCRIGNO ESPLORATO";
            int reward = controller?.Runtime?.FinalChestGold ?? SandboxDungeonRuntime.DefaultFinalChestGold;
            return $"E / GAMEPAD WEST: APRI SCRIGNO (+{reward} ORO MATCH)";
        }

        public bool TryInteract(PrototypePlayerCombat player)
        {
            if (!IsInteractionAvailable(player) || !controller.TryClaimFinalChest(out _)) return false;
            ApplyVisualState();
            return true;
        }

        private void Awake()
        {
            chestRenderer = GetComponent<SpriteRenderer>();
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.9f, 0.7f);
                trigger = box;
            }

            trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            SandboxInteractionController.Register(this);
            ApplyVisualState();
        }

        private void OnDisable()
        {
            SandboxInteractionController.Unregister(this);
        }

        private void ApplyVisualState()
        {
            if (chestRenderer == null) return;
            chestRenderer.color = IsOpened
                ? new Color(0.42f, 0.46f, 0.50f, 0.72f)
                : Color.white;
        }
    }
}
