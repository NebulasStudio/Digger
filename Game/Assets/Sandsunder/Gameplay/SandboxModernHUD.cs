using UnityEngine;
using UnityEngine.UI;
using Sandsunder.Gameplay.UI;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Feature 4 runtime orchestrator — Sandsunder Modern UI. Auto-builds the TAB inventory modal
    /// (glass panel, paper-doll area, HP/Stamina bars, weapon stat card, stealth indicator) and
    /// wires the TabInventoryController. Follows the same RuntimeInitializeOnLoadMethod pattern as
    /// PrototypeInventoryHUD / PrototypePlayerStatusHUD so no scene wiring is required.
    ///
    /// This is a pure presentation layer (UI only) — it never reads from or writes to the
    /// authoritative simulation, per AGENTS.md.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxModernHUD : MonoBehaviour
    {
        public static SandboxModernHUD Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // Only build the TAB inventory; the hotbar + status bars are owned by
            // PrototypeInventoryHUD / PrototypePlayerStatusHUD.
            if (Instance == null && FindFirstObjectByType<SandboxModernHUD>() == null)
            {
                GameObject hudObj = new("SandboxModernHUD_Auto");
                hudObj.AddComponent<SandboxModernHUD>();
            }
        }

        private void Awake()
        {
            Instance = this;
            BuildTabInventory();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildTabInventory()
        {
            // Modal canvas (its own canvas so it can toggle above the HUD).
            GameObject canvasObj = new("TabInventory_Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Root panel (hidden by default; toggled with Tab).
            GameObject root = new("InventoryRoot", typeof(RectTransform));
            root.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Glass panel (background + gold border).
            Image panelImg = root.AddComponent<Image>();
            panelImg.color = new Color(0.10f, 0.08f, 0.06f, 0.90f);
            GlassPanel glass = root.AddComponent<GlassPanel>();

            // Left column: HP + Stamina bars (glass-styled).
            BuildStatBar(root, "StaminaBar", new Vector2(80, 0), new Vector2(360, 22), new Color(0.84f, 0.70f, 0.21f));
            BuildStatBar(root, "HealthBar", new Vector2(80, 34), new Vector2(360, 22), new Color(0.00f, 1.00f, 0.48f));

            // Stealth indicator (top-right cyan dot).
            GameObject stealthObj = new("StealthIndicator", typeof(RectTransform), typeof(Image));
            stealthObj.transform.SetParent(root.transform, false);
            RectTransform stealthRect = stealthObj.GetComponent<RectTransform>();
            stealthRect.anchorMin = new Vector2(1f, 1f);
            stealthRect.anchorMax = new Vector2(1f, 1f);
            stealthRect.pivot = new Vector2(1f, 1f);
            stealthRect.anchoredPosition = new Vector2(-24, -24);
            stealthRect.sizeDelta = new Vector2(18, 18);
            stealthObj.AddComponent<StealthIndicator>();

            // Weapon stat card (right side).
            GameObject cardObj = new("WeaponStatCard", typeof(RectTransform));
            cardObj.transform.SetParent(root.transform, false);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(1f, 0.5f);
            cardRect.anchorMax = new Vector2(1f, 0.5f);
            cardRect.pivot = new Vector2(1f, 0.5f);
            cardRect.anchoredPosition = new Vector2(-120, 0);
            cardRect.sizeDelta = new Vector2(300, 240);
            WeaponStatCard statCard = BuildWeaponStatCard(cardObj);

            // TAB controller wires the whole modal.
            TabInventoryController tabController = gameObject.AddComponent<TabInventoryController>();
            tabController.Setup(root, statCard);

            root.SetActive(false);
        }

        private static void BuildStatBar(Transform parent, string name, Vector2 pos, Vector2 size, Color fill)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            StatBarWidget widget = go.AddComponent<StatBarWidget>();
            widget.SetValue(100f, 100f);
            widget.SetColor(fill);
        }

        private static WeaponStatCard BuildWeaponStatCard(GameObject card)
        {
            // Name label.
            GameObject nameObj = new("WeaponName", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(card.transform, false);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0, -8);
            nameRect.sizeDelta = new Vector2(0, 26);
            Text nameText = nameObj.GetComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = new Color(0.84f, 0.70f, 0.21f);

            string[] rows = { "Damage", "Range", "Fire Rate" };
            Slider[] sliders = new Slider[3];

            for (int i = 0; i < rows.Length; i++)
            {
                GameObject rowObj = new(rows[i], typeof(RectTransform), typeof(Slider));
                rowObj.transform.SetParent(card.transform, false);
                RectTransform rowRect = rowObj.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0, -40 - (i * 34));
                rowRect.sizeDelta = new Vector2(-40, 22);

                Slider slider = rowObj.GetComponent<Slider>();
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 0.5f;
                slider.interactable = false;

                // Fill (foreground) so the bar is visible.
                GameObject fillObj = new("Fill", typeof(RectTransform), typeof(Image));
                fillObj.transform.SetParent(rowObj.transform, false);
                RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                Image fillImg = fillObj.GetComponent<Image>();
                fillImg.color = new Color(0.00f, 0.94f, 0.90f, 0.9f);
                slider.fillRect = fillRect;

                sliders[i] = slider;
            }

            // Reuse the component but wire sliders via a small bridge: the card reads the assigned
            // serialized fields, so we assign them reflection-free by adding a typed accessor.
            WeaponStatCard cardComponent = card.AddComponent<WeaponStatCard>();
            cardComponent.Setup(nameText, sliders[0], sliders[1], sliders[2]);
            return cardComponent;
        }
    }
}