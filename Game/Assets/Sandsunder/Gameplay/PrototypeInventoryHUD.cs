using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryHUD : MonoBehaviour
    {
        private static PrototypeInventoryHUD instance;
        public static PrototypeInventoryHUD Instance => instance;

        private Canvas canvas;
        private RectTransform panel;
        private readonly List<InventorySlotUI> slots = new();
        private readonly List<string> inventoryItems = new()
        {
            "shovel.default",
            "rifle.brass",
            "sword.scimitar",
            "shotgun.heavy",
            "blaster.rune",
            "key.subterranean",
            "prototype_heal"
        };
        public IReadOnlyList<string> InventoryItems => inventoryItems;
        private readonly Dictionary<string, Sprite> spriteCache = new();

        public Sprite GetItemSprite(string itemId) => GetOrCreateSprite(itemId);

        private sealed class InventorySlotUI
        {
            public GameObject Root;
            public Image Background;
            public Image Icon;
            public Text Label;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null && FindFirstObjectByType<PrototypeInventoryHUD>() == null)
            {
                GameObject autoHUD = new("PrototypeInventoryHUD_Auto");
                autoHUD.AddComponent<PrototypeInventoryHUD>();
            }
        }

        private void Awake()
        {
            instance = this;
            BuildUI();
        }

        public void AddItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (!inventoryItems.Contains(itemId))
            {
                inventoryItems.Add(itemId);
                RefreshUI();
            }
        }

        public bool HasItem(string itemId)
        {
            return inventoryItems.Contains(itemId);
        }

        private void BuildUI()
        {
            GameObject canvasObj = new("InventoryHUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panelObj = new("InventoryPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            panel = panelObj.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0, 16);
            panel.sizeDelta = new Vector2(250, 48);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.07f, 0.06f, 0.85f);

            // Create 5 compact slots (36x36px)
            for (int i = 0; i < 5; i++)
            {
                GameObject slotObj = new($"Slot_{i}");
                slotObj.transform.SetParent(panel.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0.5f);
                slotRect.anchorMax = new Vector2(0f, 0.5f);
                slotRect.pivot = new Vector2(0f, 0.5f);
                slotRect.anchoredPosition = new Vector2(8 + (i * 48), 0);
                slotRect.sizeDelta = new Vector2(36, 36);

                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.18f, 0.15f, 0.13f, 0.95f);

                GameObject iconObj = new("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-4, -4);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.preserveAspect = true;

                GameObject labelObj = new("Label");
                labelObj.transform.SetParent(slotObj.transform, false);
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                Text labelText = labelObj.AddComponent<Text>();
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.alignment = TextAnchor.LowerRight;
                labelText.fontSize = 9;
                labelText.color = Color.yellow;

                slots.Add(new InventorySlotUI
                {
                    Root = slotObj,
                    Background = slotBg,
                    Icon = iconImg,
                    Label = labelText
                });
            }

            RefreshUI();
        }

        private int selectedIndex = 0;
        public int SelectedIndex => selectedIndex;

        private void Update()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.05f)
            {
                selectedIndex = (selectedIndex - 1 + slots.Count) % slots.Count;
                RefreshUI();
            }
            else if (scroll < -0.05f)
            {
                selectedIndex = (selectedIndex + 1) % slots.Count;
                RefreshUI();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SetSelectedSlot(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SetSelectedSlot(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SetSelectedSlot(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SetSelectedSlot(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SetSelectedSlot(4);
        }

        public void SetSelectedSlot(int index)
        {
            if (index >= 0 && index < slots.Count && selectedIndex != index)
            {
                selectedIndex = index;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                bool isSelected = (i == selectedIndex);

                if (i < inventoryItems.Count)
                {
                    string item = inventoryItems[i];
                    slot.Root.SetActive(true);
                    slot.Label.text = i == 0 ? "SX/DX" : $"{i + 1}";

                    if (isSelected)
                    {
                        slot.Background.color = new Color(0.20f, 0.85f, 0.85f, 0.95f);
                    }
                    else
                    {
                        slot.Background.color = i == 0 ? new Color(0.20f, 0.40f, 0.30f, 0.95f) : new Color(0.30f, 0.24f, 0.18f, 0.95f);
                    }

                    slot.Icon.sprite = GetOrCreateSprite(item);
                    slot.Icon.color = Color.white;
                }
                else
                {
                    slot.Label.text = $"{i + 1}";
                    slot.Background.color = isSelected ? new Color(0.35f, 0.65f, 0.65f, 0.70f) : new Color(0.12f, 0.10f, 0.08f, 0.4f);
                    slot.Icon.sprite = null;
                    slot.Icon.color = Color.clear;
                }
            }
        }

        private Sprite GetOrCreateSprite(string itemId)
        {
            if (spriteCache.TryGetValue(itemId, out var existing))
            {
                return existing;
            }

#if UNITY_EDITOR
            var art = Sandsunder.Editor.SandboxArtAssetFactory.LoadOrCreate();
            Sprite hfSprite = itemId switch
            {
                "shovel.default" => art.Shovel,
                "rifle.brass" => art.Pistol,
                "sword.scimitar" => art.Scimitar,
                "shotgun.heavy" => art.Shotgun,
                "blaster.rune" => art.Blaster,
                "key.subterranean" => art.Relic,
                _ => null
            };
            if (hfSprite != null)
            {
                spriteCache[itemId] = hfSprite;
                return hfSprite;
            }
#endif

            Sprite sprite = CreateItemPixelSprite(itemId);
            spriteCache[itemId] = sprite;
            return sprite;
        }

        private static Sprite CreateItemPixelSprite(string itemId)
        {
            Texture2D tex = new(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color transparent = Color.clear;
            Color steel = new(0.80f, 0.82f, 0.85f);
            Color wood = new(0.60f, 0.40f, 0.22f);
            Color gold = new(0.98f, 0.78f, 0.20f);
            Color cyan = new(0.28f, 0.88f, 0.92f);
            Color red = new(0.88f, 0.25f, 0.20f);

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            if (itemId == "shovel.default")
            {
                // Shovel T-Handle Top
                for (int x = 5; x <= 10; x++) tex.SetPixel(x, 13, wood);
                // Shaft
                for (int y = 5; y <= 12; y++)
                {
                    tex.SetPixel(7, y, wood);
                    tex.SetPixel(8, y, wood);
                }
                // Spade Blade Scoop
                for (int y = 1; y <= 5; y++)
                {
                    int width = y >= 4 ? 3 : (y >= 2 ? 2 : 1);
                    for (int x = 7 - width; x <= 8 + width; x++)
                    {
                        tex.SetPixel(x, y, steel);
                    }
                }
            }
            else if (itemId == "sword.scimitar")
            {
                // Gold Hilt Guard & Pommel
                for (int x = 4; x <= 9; x++) tex.SetPixel(x, 4, gold);
                tex.SetPixel(6, 2, gold);
                tex.SetPixel(7, 2, gold);
                tex.SetPixel(6, 3, wood);
                tex.SetPixel(7, 3, wood);

                // Curved Steel Scimitar Blade
                for (int y = 5; y <= 14; y++)
                {
                    int curveX = 6 + (y > 9 ? (y - 9) / 2 : 0);
                    tex.SetPixel(curveX, y, steel);
                    tex.SetPixel(curveX + 1, y, steel);
                }
            }
            else if (itemId == "shotgun.heavy")
            {
                // Heavy Stock & Barrels
                for (int x = 2; x <= 6; x++)
                {
                    tex.SetPixel(x, 6, wood);
                    tex.SetPixel(x, 7, wood);
                }
                for (int x = 6; x <= 14; x++)
                {
                    tex.SetPixel(x, 7, steel);
                    tex.SetPixel(x, 9, steel);
                    if (x < 10) tex.SetPixel(x, 6, wood);
                }
            }
            else if (itemId == "blaster.rune")
            {
                // Metallic Gun Body
                for (int x = 3; x <= 13; x++)
                {
                    for (int y = 6; y <= 9; y++)
                    {
                        tex.SetPixel(x, y, steel);
                    }
                }
                // Glowing Cyan Rune Core
                for (int x = 7; x <= 10; x++)
                {
                    tex.SetPixel(x, 7, cyan);
                    tex.SetPixel(x, 8, Color.white);
                }
            }
            else if (itemId == "rifle.brass")
            {
                // Long Brass Rifle Barrel & Stock
                for (int x = 2; x <= 6; x++) tex.SetPixel(x, 6, wood);
                for (int x = 5; x <= 14; x++)
                {
                    tex.SetPixel(x, 8, gold);
                    if (x < 9) tex.SetPixel(x, 7, wood);
                }
            }
            else if (itemId.Contains("key"))
            {
                // Gold Key Ring
                for (int x = 3; x <= 7; x++)
                    for (int y = 9; y <= 13; y++)
                        tex.SetPixel(x, y, gold);
                // Shaft & Teeth
                for (int x = 7; x <= 13; x++) tex.SetPixel(x, 10, gold);
                tex.SetPixel(11, 8, gold);
                tex.SetPixel(13, 8, gold);
            }
            else if (itemId == PrototypeDigGridAuthority.HealLootId)
            {
                // Medkit Bag & Red Cross
                for (int x = 3; x <= 12; x++)
                    for (int y = 3; y <= 12; y++)
                        tex.SetPixel(x, y, Color.white);
                for (int i = 5; i <= 10; i++)
                {
                    tex.SetPixel(7, i, red);
                    tex.SetPixel(8, i, red);
                    tex.SetPixel(i, 7, red);
                    tex.SetPixel(i, 8, red);
                }
            }
            else
            {
                // Gem / Weapon Item
                for (int x = 4; x <= 11; x++)
                    for (int y = 4; y <= 11; y++)
                        tex.SetPixel(x, y, cyan);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }
    }
}
