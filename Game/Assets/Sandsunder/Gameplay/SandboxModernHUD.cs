using System;
using System.Collections.Generic;
using Sandsunder.Gameplay.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Single runtime HUD composition root. All elements are presentation-only projections of
    /// gameplay state, and no scene/prefab wiring or UnityEditor API is required in a player build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxModernHUD : MonoBehaviour
    {
        private static readonly Color Gold = new(0.84f, 0.70f, 0.21f, 1f);
        private static readonly Color Red = new(0.78f, 0.12f, 0.10f, 1f);
        private static readonly Color Cyan = new(0f, 0.94f, 0.90f, 1f);
        private static readonly Color Ink = new(0.055f, 0.06f, 0.075f, 0.96f);
        private static readonly Color SlotIdle = new(0.12f, 0.13f, 0.16f, 0.98f);
        private static readonly Color SlotSelected = new(0.56f, 0.42f, 0.12f, 1f);

        private readonly List<SlotView> hotbarSlots = new();
        private readonly List<SlotView> inventorySlots = new();
        private PrototypeInventoryHUD inventory;
        private PrototypeHealth playerHealth;
        private IPlayerOxygenProvider oxygenProvider;
        private Image healthFill;
        private Text healthLabel;
        private Image oxygenFill;
        private Text oxygenLabel;
        private RectTransform oxygenFrame;
        private Text depthLabel;
        private RectTransform digProgressRoot;
        private Image digProgressFill;
        private Text actionHintLabel;
        private PrototypePlayerCombat playerCombat;
        private SandboxInteractionController interactionController;
        private Text itemName;
        private Text itemCategory;
        private Text itemStats;
        private Text itemDescription;
        private Text rulesetLabel;
        private Image paperDoll;
        private GridLayoutGroup inventoryGrid;
        private int inspectedIndex;
        private float nextProviderSearchTime;

        private sealed class SlotView
        {
            public int Index;
            public Button Button;
            public Image Background;
            public Image Icon;
            public Text Shortcut;
        }

        public static SandboxModernHUD Instance { get; private set; }
        public Canvas HudCanvas { get; private set; }
        public GameObject InventoryRoot { get; private set; }
        public TabInventoryController InventoryController { get; private set; }
        public int HotbarSlotCount => hotbarSlots.Count;
        public int InventorySlotCount => inventorySlots.Count;
        public float DisplayedOxygenRatio => oxygenFill != null ? oxygenFill.rectTransform.anchorMax.x : 0f;
        public bool IsOxygenVisible => oxygenFrame != null && oxygenFrame.gameObject.activeSelf;
        public bool IsDigProgressVisible => digProgressRoot != null && digProgressRoot.gameObject.activeSelf;
        public string DepthLabelText => depthLabel != null ? depthLabel.text : string.Empty;
        public string ActionHintText => actionHintLabel != null ? actionHintLabel.text : string.Empty;
        public string InspectedItemName => itemName != null ? itemName.text : string.Empty;
        public Sprite PaperDollSprite => paperDoll != null ? paperDoll.sprite : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null || FindFirstObjectByType<SandboxModernHUD>() != null) return;
            new GameObject("SandboxModernHUD_Auto").AddComponent<SandboxModernHUD>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Idempotent composition entry point used by runtime Awake and deterministic EditMode tests.
        /// </summary>
        public void EnsureInitialized()
        {
            if (HudCanvas != null) return;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            inventory = PrototypeInventoryHUD.Instance ?? FindFirstObjectByType<PrototypeInventoryHUD>();
            if (inventory == null)
            {
                GameObject model = new("PrototypeInventoryModel");
                model.transform.SetParent(transform, false);
                inventory = model.AddComponent<PrototypeInventoryHUD>();
            }

            BuildEventSystem();
            BuildHud();
            SubscribeInventory();
            RefreshAll();
            RefreshContextState();
        }

        private void OnDestroy()
        {
            UnsubscribeInventory();
            BindOxygenProvider(null);
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            RefreshPlayerStatus();
            RefreshContextState();
            ResizeInventoryCells();

            if (oxygenProvider == null && Time.unscaledTime >= nextProviderSearchTime)
            {
                nextProviderSearchTime = Time.unscaledTime + 1f;
                TryDiscoverOxygenProvider();
            }
        }

        public void BindOxygenProvider(IPlayerOxygenProvider provider)
        {
            if (ReferenceEquals(oxygenProvider, provider)) return;
            if (oxygenProvider != null) oxygenProvider.OxygenChanged -= OnOxygenChanged;
            oxygenProvider = provider;
            if (oxygenProvider != null)
            {
                oxygenProvider.OxygenChanged += OnOxygenChanged;
                OnOxygenChanged(oxygenProvider.CurrentOxygen, oxygenProvider.MaximumOxygen);
                if (oxygenFrame != null) oxygenFrame.gameObject.SetActive(oxygenProvider.IsSubterranean);
            }
            else
            {
                SetBar(oxygenFill, oxygenLabel, 0f, 0f, "O2  --");
                if (oxygenFrame != null) oxygenFrame.gameObject.SetActive(false);
            }
        }

        public void RefreshAll()
        {
            RefreshHotbar();
            RefreshInventory();
            ShowItem(inspectedIndex);
            RefreshPlayerStatus();
        }

        private void BuildHud()
        {
            GameObject canvasObject = new("SandsunderHUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            HudCanvas = canvasObject.GetComponent<Canvas>();
            HudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            HudCanvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            scaler.referencePixelsPerUnit = 32f;

            BuildStatusPanel(canvasObject.transform);
            BuildHotbar(canvasObject.transform);
            BuildContextPanel(canvasObject.transform);
            BuildInventoryModal(canvasObject.transform);
        }

        private void BuildStatusPanel(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "PlayerStatus", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -20f), new Vector2(330f, 112f), new Vector2(0f, 1f));
            CreateLabel(panel, "NomadLabel", "NOMAD", 15, TextAnchor.MiddleLeft, Gold,
                new Vector2(12, -8), new Vector2(-24, 22), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));
            depthLabel = CreateLabel(panel, "DepthLabel", "SURFACE  /  L0", 12, TextAnchor.MiddleRight, Cyan,
                new Vector2(12, -8), new Vector2(-24, 22), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1));
            CreateStatusBar(panel, "Health", "HP", Red, new Vector2(12, -38), out healthFill, out healthLabel);
            CreateStatusBar(panel, "Oxygen", "O2", Cyan, new Vector2(12, -70), out oxygenFill, out oxygenLabel);
            oxygenFrame = oxygenFill != null ? oxygenFill.transform.parent.parent as RectTransform : null;
            if (oxygenFrame != null) oxygenFrame.gameObject.SetActive(false);
        }

        private void BuildHotbar(Transform parent)
        {
            RectTransform bar = CreatePanel(parent, "Hotbar", new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 18f), new Vector2(354f, 74f), new Vector2(.5f, 0f));

            for (int index = 0; index < PrototypeInventoryHUD.HotbarCapacity; index++)
            {
                int captured = index;
                SlotView slot = CreateSlot(bar, index, new Vector2(12 + index * 68, 8), new Vector2(58, 58), (index + 1).ToString());
                slot.Button.onClick.AddListener(() =>
                {
                    inventory.SetSelectedSlot(captured);
                    inspectedIndex = captured;
                    ShowItem(captured);
                });
                hotbarSlots.Add(slot);
            }
        }

        private void BuildContextPanel(Transform parent)
        {
            RectTransform context = CreatePanel(parent, "ContextAction", new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 102f), new Vector2(430f, 54f), new Vector2(.5f, 0f));
            actionHintLabel = CreateLabel(context, "ActionHint", "HOLD RMB / A  DIG   |   E  USE   |   TAB  INVENTORY",
                12, TextAnchor.MiddleCenter, new Color(.88f, .84f, .70f),
                new Vector2(10f, -7f), new Vector2(-20f, 20f), Vector2.up, Vector2.one, new Vector2(.5f, 1f));

            digProgressRoot = CreateStatusBar(context, "DigProgress", "EXCAVATING", Gold,
                new Vector2(12f, -29f), out digProgressFill, out _);
            digProgressRoot.sizeDelta = new Vector2(-24f, 16f);
            digProgressRoot.gameObject.SetActive(false);
        }

        private void BuildInventoryModal(Transform parent)
        {
            InventoryRoot = new GameObject("InventoryModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            InventoryRoot.transform.SetParent(parent, false);
            RectTransform blocker = InventoryRoot.GetComponent<RectTransform>();
            Stretch(blocker, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image blockerImage = InventoryRoot.GetComponent<Image>();
            blockerImage.color = new Color(0.01f, 0.015f, 0.025f, .72f);

            RectTransform panel = CreatePanel(blocker, "InventoryGlassPanel", new Vector2(.06f, .07f), new Vector2(.94f, .93f),
                Vector2.zero, Vector2.zero, new Vector2(.5f, .5f), stretch: true);
            Sprite brochurePanel = SandboxHudSpriteLibrary.GetBrochurePanelSprite();
            if (brochurePanel != null)
            {
                Image brochureBackground = panel.GetComponent<Image>();
                brochureBackground.sprite = brochurePanel;
                brochureBackground.type = Image.Type.Simple;
                brochureBackground.color = new Color(1f, 1f, 1f, .94f);
            }
            CreateLabel(panel, "Title", "INVENTORY", 24, TextAnchor.MiddleLeft, Gold,
                new Vector2(18, -10), new Vector2(-36, 38), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));
            rulesetLabel = CreateLabel(panel, "Ruleset", $"UI v{SandboxHudCatalog.SchemaVersion}  |  {SandboxHudCatalog.RulesetVersion}", 11,
                TextAnchor.MiddleRight, new Color(.7f, .76f, .8f), new Vector2(18, -10), new Vector2(-36, 38),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1));

            RectTransform paperPanel = CreateInsetPanel(panel, "NomadPaperDoll");
            RectTransform inventoryPanel = CreateInsetPanel(panel, "Backpack");
            RectTransform statPanel = CreateInsetPanel(panel, "ItemStatCard");
            panel.gameObject.AddComponent<ResponsiveInventoryLayout>().Configure(paperPanel, inventoryPanel, statPanel);

            BuildPaperDoll(paperPanel);
            Button firstInventoryButton = BuildInventoryGrid(inventoryPanel);
            BuildStatCard(statPanel);

            InventoryController = gameObject.AddComponent<TabInventoryController>();
            InventoryController.Setup(InventoryRoot, firstInventoryButton, RefreshAll);
            InventoryRoot.SetActive(false);
        }

        private void BuildPaperDoll(RectTransform panel)
        {
            CreateLabel(panel, "CharacterTitle", "NOMAD", 16, TextAnchor.MiddleCenter, Gold,
                new Vector2(8, -8), new Vector2(-16, 28), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1));

            GameObject imageObject = new("Nomad_32_PaperDoll", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(panel, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(.2f, .2f);
            imageRect.anchorMax = new Vector2(.8f, .82f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            paperDoll = imageObject.GetComponent<Image>();
            paperDoll.sprite = SandboxHudSpriteLibrary.GetNomadSprite();
            paperDoll.preserveAspect = true;
            paperDoll.raycastTarget = false;

            CreateLabel(panel, "CharacterNote", "2D PAPER-DOLL\nCOSMETIC LOADOUT", 11, TextAnchor.MiddleCenter,
                new Color(.68f, .78f, .82f), new Vector2(8, 6), new Vector2(-16, 42),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0));
        }

        private Button BuildInventoryGrid(RectTransform panel)
        {
            CreateLabel(panel, "BackpackTitle", $"BACKPACK  {PrototypeInventoryHUD.BackpackCapacity} SLOTS", 15,
                TextAnchor.MiddleLeft, Gold, new Vector2(10, -8), new Vector2(-20, 26),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));

            GameObject gridObject = new("InventoryGrid_15", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(panel, false);
            RectTransform gridRect = gridObject.GetComponent<RectTransform>();
            Stretch(gridRect, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -42));
            inventoryGrid = gridObject.GetComponent<GridLayoutGroup>();
            inventoryGrid.cellSize = new Vector2(58, 58);
            inventoryGrid.spacing = new Vector2(8, 8);
            inventoryGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            inventoryGrid.constraintCount = 5;
            inventoryGrid.childAlignment = TextAnchor.UpperCenter;

            Button first = null;
            for (int visible = 0; visible < PrototypeInventoryHUD.BackpackCapacity; visible++)
            {
                int itemIndex = PrototypeInventoryHUD.HotbarCapacity + visible;
                SlotView slot = CreateSlot(gridRect, itemIndex, Vector2.zero, new Vector2(58, 58), string.Empty, layoutDriven: true);
                slot.Button.onClick.AddListener(() =>
                {
                    inspectedIndex = itemIndex;
                    ShowItem(itemIndex);
                });
                inventorySlots.Add(slot);
                first ??= slot.Button;
            }

            return first;
        }

        private void BuildStatCard(RectTransform panel)
        {
            itemName = CreateLabel(panel, "ItemName", "EMPTY SLOT", 18, TextAnchor.UpperLeft, Gold,
                new Vector2(12, -12), new Vector2(-24, 46), Vector2.up, Vector2.one, Vector2.up);
            itemName.fontStyle = FontStyle.Bold;
            itemCategory = CreateLabel(panel, "Category", string.Empty, 12, TextAnchor.UpperLeft, Cyan,
                new Vector2(12, -58), new Vector2(-24, 25), Vector2.up, Vector2.one, Vector2.up);
            itemStats = CreateLabel(panel, "Stats", string.Empty, 13, TextAnchor.UpperLeft, Color.white,
                new Vector2(12, -92), new Vector2(-24, 100), Vector2.up, Vector2.one, Vector2.up);
            itemDescription = CreateLabel(panel, "Description", string.Empty, 12, TextAnchor.UpperLeft, new Color(.76f, .8f, .82f),
                new Vector2(12, 12), new Vector2(-24, -205), Vector2.zero, Vector2.one, Vector2.zero);
            itemDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemDescription.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void SubscribeInventory()
        {
            if (inventory == null) return;
            inventory.InventoryChanged += RefreshAll;
            inventory.SelectionChanged += OnSelectionChanged;
        }

        private void UnsubscribeInventory()
        {
            if (inventory == null) return;
            inventory.InventoryChanged -= RefreshAll;
            inventory.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(int index)
        {
            inspectedIndex = index;
            RefreshHotbar();
            ShowItem(index);
        }

        private void RefreshHotbar()
        {
            if (inventory == null) return;
            foreach (SlotView slot in hotbarSlots)
            {
                string itemId = inventory.GetItemAt(slot.Index);
                slot.Icon.sprite = SandboxHudSpriteLibrary.GetItemSprite(itemId);
                slot.Icon.enabled = !string.IsNullOrEmpty(itemId);
                slot.Background.color = slot.Index == inventory.SelectedIndex ? SlotSelected : SlotIdle;
            }
        }

        private void RefreshInventory()
        {
            if (inventory == null) return;
            foreach (SlotView slot in inventorySlots)
            {
                string itemId = inventory.GetItemAt(slot.Index);
                slot.Icon.sprite = SandboxHudSpriteLibrary.GetItemSprite(itemId);
                slot.Icon.enabled = !string.IsNullOrEmpty(itemId);
                slot.Background.color = slot.Index == inspectedIndex ? SlotSelected : SlotIdle;
            }
        }

        private void ShowItem(int index)
        {
            inspectedIndex = Mathf.Clamp(index, 0, PrototypeInventoryHUD.TotalCapacity - 1);
            string itemId = inventory != null ? inventory.GetItemAt(inspectedIndex) : string.Empty;
            SandboxHudItemDefinition definition = SandboxHudCatalog.Get(itemId);
            if (itemName != null) itemName.text = definition.DisplayName.ToUpperInvariant();
            if (itemCategory != null) itemCategory.text = definition.Category.ToUpperInvariant();
            if (itemStats != null)
            {
                itemStats.text = string.IsNullOrEmpty(itemId)
                    ? string.Empty
                    : $"DAMAGE   {definition.Damage:0.#}\nRANGE    {definition.Range:0.#} m\nCADENCE  {definition.Cadence:0.#}/s";
            }
            if (itemDescription != null) itemDescription.text = definition.Description;
            RefreshInventory();
        }

        private void RefreshPlayerStatus()
        {
            if (playerHealth == null)
            {
                TopDownPlayerController player = FindFirstObjectByType<TopDownPlayerController>();
                if (player != null)
                {
                    playerHealth = player.GetComponent<PrototypeHealth>();
                    playerCombat = player.GetComponent<PrototypePlayerCombat>();
                    interactionController = player.GetComponent<SandboxInteractionController>();
                }
            }

            if (playerHealth != null)
            {
                SetBar(healthFill, healthLabel, playerHealth.CurrentHealth, playerHealth.MaximumHealth,
                    $"HP  {playerHealth.CurrentHealth} / {playerHealth.MaximumHealth}");
            }
            else
            {
                SetBar(healthFill, healthLabel, 0f, 0f, "HP  --");
            }

            if (oxygenProvider != null)
            {
                OnOxygenChanged(oxygenProvider.CurrentOxygen, oxygenProvider.MaximumOxygen);
                if (oxygenFrame != null) oxygenFrame.gameObject.SetActive(oxygenProvider.IsSubterranean);
                if (depthLabel != null)
                {
                    depthLabel.text = oxygenProvider.IsSubterranean ? "DUNGEON  /  L-1" : "SURFACE  /  L0";
                    depthLabel.color = oxygenProvider.IsSubterranean ? Cyan : Gold;
                }
            }
        }

        private void RefreshContextState()
        {
            if (playerCombat == null)
            {
                TopDownPlayerController player = FindFirstObjectByType<TopDownPlayerController>();
                if (player != null)
                {
                    playerCombat = player.GetComponent<PrototypePlayerCombat>();
                    interactionController = player.GetComponent<SandboxInteractionController>();
                }
            }
            if (interactionController == null && playerCombat != null)
            {
                interactionController = playerCombat.GetComponent<SandboxInteractionController>();
            }

            bool digging = playerCombat != null && playerCombat.IsDiggingChanneling;
            if (digProgressRoot != null) digProgressRoot.gameObject.SetActive(digging);
            if (digProgressFill != null)
            {
                digProgressFill.rectTransform.anchorMax = new Vector2(
                    digging ? playerCombat.DiggingProgressRatio : 0f,
                    1f);
            }

            if (actionHintLabel == null) return;
            if (digging)
            {
                actionHintLabel.text = $"EXCAVATING  {Mathf.CeilToInt(playerCombat.DiggingProgressRatio * 100f)}%";
                actionHintLabel.color = Gold;
                return;
            }

            if (interactionController != null && !string.IsNullOrWhiteSpace(interactionController.CurrentPrompt))
            {
                actionHintLabel.text = interactionController.CurrentPrompt;
                actionHintLabel.color = Cyan;
                return;
            }

            bool underground = oxygenProvider != null && oxygenProvider.IsSubterranean;
            actionHintLabel.text = underground
                ? "DUNGEON L-1   |   E  INTERACT / USE   |   TAB  INVENTORY"
                : "HOLD RMB / A  DIG   |   E  INTERACT / USE   |   TAB  INVENTORY";
            actionHintLabel.color = underground ? Cyan : new Color(.88f, .84f, .70f);
        }

        private void TryDiscoverOxygenProvider()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IPlayerOxygenProvider provider)
                {
                    BindOxygenProvider(provider);
                    return;
                }
            }
        }

        private void OnOxygenChanged(float current, float maximum)
        {
            string label = maximum > 0f ? $"O2  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}" : "O2  --";
            SetBar(oxygenFill, oxygenLabel, current, maximum, label);
            if (oxygenFill != null)
            {
                float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
                oxygenFill.color = ratio <= .10f
                    ? Red
                    : ratio <= .25f
                        ? new Color(1f, .50f, .08f, 1f)
                        : Cyan;
            }
        }

        private void ResizeInventoryCells()
        {
            if (inventoryGrid == null) return;
            float available = inventoryGrid.GetComponent<RectTransform>().rect.width;
            float size = Mathf.Clamp((available - inventoryGrid.spacing.x * 4f) / 5f, 34f, 64f);
            inventoryGrid.cellSize = new Vector2(size, size);
        }

        private void BuildEventSystem()
        {
            EventSystem eventSystem = EventSystem.current ?? FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new("SandsunderUI_EventSystem", typeof(EventSystem));
                eventObject.transform.SetParent(transform, false);
                eventSystem = eventObject.GetComponent<EventSystem>();
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                BaseInputModule[] existingModules = eventSystem.GetComponents<BaseInputModule>();
                foreach (BaseInputModule module in existingModules) module.enabled = false;
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            inputModule.AssignDefaultActions();
            eventSystem.sendNavigationEvents = true;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot,
            bool stretch = false)
        {
            GameObject panelObject = new(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            if (stretch)
            {
                rect.offsetMin = position;
                rect.offsetMax = size;
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            Image image = panelObject.GetComponent<Image>();
            image.sprite = SandboxHudSpriteLibrary.GetGlassPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            return rect;
        }

        private static RectTransform CreateInsetPanel(Transform parent, string name)
        {
            GameObject panelObject = new(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.GetComponent<Image>();
            image.color = Ink;
            return panelObject.GetComponent<RectTransform>();
        }

        private static Text CreateLabel(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            GameObject labelObject = new(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreateStatusBar(
            Transform parent,
            string name,
            string prefix,
            Color fillColor,
            Vector2 position,
            out Image fill,
            out Text label)
        {
            GameObject frameObject = new($"{name}Frame", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(parent, false);
            RectTransform frame = frameObject.GetComponent<RectTransform>();
            frame.anchorMin = new Vector2(0, 1);
            frame.anchorMax = new Vector2(1, 1);
            frame.pivot = new Vector2(0, 1);
            frame.anchoredPosition = position;
            frame.sizeDelta = new Vector2(-24, 24);
            frameObject.GetComponent<Image>().color = Gold;

            GameObject trackObject = new("Track", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(frame, false);
            RectTransform track = trackObject.GetComponent<RectTransform>();
            Stretch(track, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            trackObject.GetComponent<Image>().color = new Color(.06f, .07f, .09f, 1f);

            GameObject fillObject = new("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            Stretch(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fillRect.pivot = new Vector2(0, .5f);
            fill = fillObject.GetComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;

            label = CreateLabel(frame, "Value", prefix, 12, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(.5f, .5f));
            return frame;
        }

        private static SlotView CreateSlot(
            Transform parent,
            int index,
            Vector2 position,
            Vector2 size,
            string shortcut,
            bool layoutDriven = false)
        {
            GameObject slotObject = new($"Slot_{index:00}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObject.transform.SetParent(parent, false);
            RectTransform rect = slotObject.GetComponent<RectTransform>();
            if (!layoutDriven)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            Image background = slotObject.GetComponent<Image>();
            background.color = SlotIdle;
            Button button = slotObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(.18f, .56f, .58f, 1f);
            colors.selectedColor = new Color(.16f, .50f, .54f, 1f);
            colors.pressedColor = Gold;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(rect, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            Stretch(iconRect, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text key = CreateLabel(rect, "Shortcut", shortcut, 10, TextAnchor.LowerRight, Gold,
                new Vector2(-3, 2), new Vector2(-6, -4), Vector2.zero, Vector2.one, new Vector2(1, 0));

            return new SlotView { Index = index, Button = button, Background = background, Icon = icon, Shortcut = key };
        }

        private static void SetBar(Image fill, Text label, float current, float maximum, string value)
        {
            if (fill != null)
            {
                float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
                fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            }
            if (label != null) label.text = value;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
