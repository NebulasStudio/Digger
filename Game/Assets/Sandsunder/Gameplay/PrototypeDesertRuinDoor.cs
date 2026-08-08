using UnityEngine;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDesertRuinDoor : MonoBehaviour, ISandboxInteractable
    {
        public const string RequiredKeyLootId = "key.subterranean";

        [SerializeField] private bool isLocked = true;

        private Collider2D doorCollider;
        private SpriteRenderer doorRenderer;

        public bool IsLocked => isLocked;
        public MonoBehaviour InteractionComponent => this;
        public Transform InteractionTransform => transform;

        public void ConfigureLocked(bool locked)
        {
            isLocked = locked;
            ApplyDoorState();
        }

        public bool IsInteractionAvailable(PrototypePlayerCombat player)
        {
            return player != null;
        }

        public string GetInteractionPrompt(PrototypePlayerCombat player)
        {
            SandboxDungeonController dungeon = SandboxDungeonController.Instance;
            if (dungeon != null && dungeon.IsInsideDungeon)
            {
                return "E / GAMEPAD WEST: ESCI DAL DUNGEON";
            }

            if (isLocked && !HasRequiredKey())
            {
                return "PORTA BLOCCATA: SERVE LA CHIAVE SOTTERRANEA";
            }

            return isLocked
                ? "E / GAMEPAD WEST: SBLOCCA ED ENTRA"
                : "E / GAMEPAD WEST: ENTRA NEL DUNGEON";
        }

        public bool TryInteract(PrototypePlayerCombat player)
        {
            SandboxDungeonController dungeon = SandboxDungeonController.EnsureInstance();
            if (dungeon.IsInsideDungeon)
            {
                return dungeon.ExitDungeon();
            }

            if (isLocked)
            {
                if (!HasRequiredKey()) return false;
                UnlockDoor("CHIAVE SOTTERRANEA");
            }

            return dungeon.EnterDungeon();
        }

        public void UnlockDoor(string reason)
        {
            isLocked = false;
            ApplyDoorState();

            if (Application.isPlaying)
            {
                SandboxVisualEffects.SpawnDust(transform.position, 15, new Color(0.20f, 0.95f, 0.90f));
                Debug.Log($"[DesertRuinDoor] Varco sotterraneo aperto tramite: {reason}");
            }
        }

        private void Awake()
        {
            doorCollider = GetComponent<Collider2D>();
            doorRenderer = GetComponent<SpriteRenderer>();

            if (doorCollider == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.2f, 1.2f);
                doorCollider = box;
            }

            if (doorRenderer == null) doorRenderer = gameObject.AddComponent<SpriteRenderer>();
            ApplyDoorState();
        }

        private void OnEnable()
        {
            SandboxInteractionController.Register(this);
        }

        private void OnDisable()
        {
            SandboxInteractionController.Unregister(this);
        }

        private bool HasRequiredKey()
        {
            return PrototypeInventoryHUD.Instance?.HasItem(RequiredKeyLootId) == true;
        }

        private void ApplyDoorState()
        {
            if (doorCollider != null) doorCollider.enabled = isLocked;
            if (doorRenderer == null) return;
            doorRenderer.sprite = CreateDoorSprite(!isLocked);
            doorRenderer.color = Color.white;
        }

        private static Sprite CreateDoorSprite(bool isOpen)
        {
            const int Size = 32;
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color stone = new(170 / 255f, 130 / 255f, 90 / 255f, 1f);
            Color wood = new(95 / 255f, 58 / 255f, 38 / 255f, 1f);
            Color iron = new(60 / 255f, 65 / 255f, 75 / 255f, 1f);
            Color cyanRune = new(50 / 255f, 240 / 255f, 230 / 255f, 0.8f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (x < 4 || x > 27 || y > 26)
                    {
                        texture.SetPixel(x, y, stone);
                    }
                    else if (!isOpen)
                    {
                        texture.SetPixel(x, y, x == 15 || x == 16 || y == 8 || y == 20 ? iron : wood);
                    }
                    else
                    {
                        texture.SetPixel(x, y, cyanRune);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 32);
        }
    }
}
