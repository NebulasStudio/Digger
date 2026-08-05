using UnityEngine;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDesertRuinDoor : MonoBehaviour
    {
        [SerializeField]
        private bool isLocked = true;

        private Collider2D doorCollider;
        private SpriteRenderer doorRenderer;

        public bool IsLocked => isLocked;

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

            if (doorRenderer == null)
            {
                doorRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            doorRenderer.sprite = CreateDoorSprite(false);
            doorRenderer.color = Color.white;
        }

        private static Sprite CreateDoorSprite(bool isOpen)
        {
            int size = 32;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color stone = new(170 / 255f, 130 / 255f, 90 / 255f, 1f);
            Color wood = new(95 / 255f, 58 / 255f, 38 / 255f, 1f);
            Color iron = new(60 / 255f, 65 / 255f, 75 / 255f, 1f);
            Color cyanRune = new(50 / 255f, 240 / 255f, 230 / 255f, 0.8f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 4 || x > 27 || y > 26)
                    {
                        tex.SetPixel(x, y, stone);
                    }
                    else if (!isOpen)
                    {
                        if (x == 15 || x == 16) tex.SetPixel(x, y, iron);
                        else if (y == 8 || y == 20) tex.SetPixel(x, y, iron);
                        else tex.SetPixel(x, y, wood);
                    }
                    else
                    {
                        tex.SetPixel(x, y, cyanRune);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        private bool showPrompt = false;

        private void Update()
        {
            showPrompt = false;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                var controller = FindFirstObjectByType<TopDownPlayerController>();
                if (controller != null) playerObj = controller.gameObject;
            }

            if (playerObj != null)
            {
                float dist = Vector2.Distance(transform.position, playerObj.transform.position);

                if (dist < 1.8f)
                {
                    showPrompt = true;

                    if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                    {
                        var playerController = playerObj.GetComponent<TopDownPlayerController>();
                        if (playerController != null)
                        {
                            playerController.SetDepth(2);
                        }

                        UnlockDoor("INGRESSO TUNNEL [E]/[SHIFT]");
                        PrototypeTunnelSystem.Instance?.TransitionToLayer(MatrixLayerDepth.Subterranean_L1);
                        return;
                    }
                }

                // Auto unlock if Has Key
                if (isLocked && dist < 1.5f && PrototypeInventoryHUD.Instance != null && PrototypeInventoryHUD.Instance.HasItem("key.subterranean"))
                {
                    UnlockDoor("CHIAVE SOTTERRANEA");
                    return;
                }
            }
        }

        private void OnGUI()
        {
            if (!showPrompt) return;

            Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.0f) : Vector3.zero;
            if (screenPos.z < 0) return;

            GUIStyle style = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = new Color(0.20f, 0.95f, 0.90f, 1.0f);

            float width = 340f;
            float height = 30f;
            Rect rect = new(screenPos.x - (width * 0.5f), Screen.height - screenPos.y - (height * 0.5f), width, height);

            GUI.Box(rect, GUIContent.none, GUI.skin.box);
            GUI.Label(rect, "PREMI [E] O [SHIFT] PER ENTRARE NEL TUNNEL", style);
        }

        public void UnlockDoor(string reason)
        {
            isLocked = false;

            if (doorCollider != null) doorCollider.enabled = false;
            if (doorRenderer != null)
            {
                doorRenderer.sprite = CreateDoorSprite(true);
                doorRenderer.color = Color.white;
            }

            SandboxVisualEffects.SpawnDust(transform.position, 15, new Color(0.20f, 0.95f, 0.90f));
            Debug.Log($"[DesertRuinDoor] Varco sotterraneo aperto tramite: {reason}");
        }
    }
}
