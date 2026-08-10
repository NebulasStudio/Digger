using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>Context-action adapter for either side of the surface/dungeon threshold.</summary>
    [DisallowMultipleComponent]
    public sealed class SandboxDungeonEntrance : MonoBehaviour, ISandboxInteractable
    {
        [SerializeField] private SandboxDungeonController controller;
        [SerializeField] private bool exitsDungeon;

        public bool ExitsDungeon => exitsDungeon;
        public MonoBehaviour InteractionComponent => this;
        public Transform InteractionTransform => transform;

        public void Configure(SandboxDungeonController configuredController, bool configuredAsExit)
        {
            controller = configuredController;
            exitsDungeon = configuredAsExit;
        }

        public bool IsInteractionAvailable(PrototypePlayerCombat player)
        {
            SandboxDungeonController dungeon = ResolveController();
            return player != null
                && dungeon != null
                && dungeon.Runtime != null
                && dungeon.IsInsideDungeon == exitsDungeon;
        }

        public string GetInteractionPrompt(PrototypePlayerCombat player)
        {
            return exitsDungeon
                ? "E / GAMEPAD WEST: TORNA IN SUPERFICIE"
                : "E / GAMEPAD WEST: ENTRA NEL DUNGEON";
        }

        public bool TryInteract(PrototypePlayerCombat player)
        {
            SandboxDungeonController dungeon = ResolveController();
            if (dungeon == null || !IsInteractionAvailable(player)) return false;
            return exitsDungeon ? dungeon.ExitDungeon(player) : dungeon.EnterDungeon(player);
        }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger == null)
            {
                CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = 0.55f;
                trigger = circle;
            }

            trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            SandboxInteractionController.Register(this);
        }

        private void OnDisable()
        {
            SandboxInteractionController.Unregister(this);
        }

        private SandboxDungeonController ResolveController()
        {
            controller = controller != null ? controller : SandboxDungeonController.Instance;
            return controller;
        }
    }
}
