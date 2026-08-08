using System.Collections.Generic;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Controller-first, horizontal in-match sandbox shop. It projects the deterministic match-local
    /// ledger and never invokes account, backend, platform-commerce, or payment APIs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxShopPanel : MonoBehaviour
    {
        public const string KeyboardToggleBinding = "<Keyboard>/b";
        public const string GamepadToggleBinding = "<Gamepad>/select";
        public const string KeyboardCloseBinding = "<Keyboard>/escape";
        public const string GamepadCloseBinding = "<Gamepad>/buttonEast";

        private static readonly Color Gold = new(.84f, .70f, .21f, 1f);
        private static readonly Color Cyan = new(0f, .94f, .90f, 1f);
        private static readonly Color Ink = new(.055f, .06f, .075f, .98f);
        private static readonly Color Card = new(.12f, .13f, .16f, 1f);

        private readonly List<Button> itemButtons = new();
        private readonly List<Text> itemLabels = new();
        private SandboxShopSession session;
        private InputAction toggleAction;
        private InputAction closeAction;
        private Text balanceLabel;
        private Text feedbackLabel;

        public static SandboxShopPanel Instance { get; private set; }
        public GameObject ShopRoot { get; private set; }
        public RectTransform ItemRow { get; private set; }
        public bool IsOpen { get; private set; }
        public int ItemButtonCount => itemButtons.Count;
        public SandboxShopSession Session => session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null || FindFirstObjectByType<SandboxShopPanel>() != null) return;
            new GameObject("SandboxShop_Auto").AddComponent<SandboxShopPanel>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInputActions();
            toggleAction.performed += OnTogglePerformed;
            closeAction.performed += OnClosePerformed;
            toggleAction.Enable();
            closeAction.Enable();
        }

        private void OnDisable()
        {
            if (toggleAction != null)
            {
                toggleAction.performed -= OnTogglePerformed;
                toggleAction.Disable();
            }

            if (closeAction != null)
            {
                closeAction.performed -= OnClosePerformed;
                closeAction.Disable();
            }
        }

        private void OnDestroy()
        {
            if (session != null) session.PurchaseProcessed -= OnPurchaseProcessed;
            toggleAction?.Dispose();
            closeAction?.Dispose();
            if (Instance == this) Instance = null;
        }

        public void EnsureInitialized()
        {
            if (ShopRoot != null) return;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            session = GetComponent<SandboxShopSession>() ?? gameObject.AddComponent<SandboxShopSession>();
            session.EnsureInitialized();
            session.PurchaseProcessed += OnPurchaseProcessed;

            SandboxModernHUD hud = SandboxModernHUD.Instance ?? FindFirstObjectByType<SandboxModernHUD>();
            if (hud == null) hud = new GameObject("SandboxModernHUD_ShopHost").AddComponent<SandboxModernHUD>();
            hud.EnsureInitialized();
            BuildShop(hud.HudCanvas.transform);
            EnsureInputActions();
            SetOpen(false);
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (ShopRoot != null) ShopRoot.SetActive(open);

            if (open)
            {
                SandboxModernHUD.Instance?.InventoryController?.SetOpen(false);
                RefreshProjection();
                EventSystem eventSystem = EventSystem.current ?? FindFirstObjectByType<EventSystem>();
                if (eventSystem != null && itemButtons.Count > 0)
                    eventSystem.SetSelectedGameObject(itemButtons[0].gameObject);
            }
        }

        public bool HasToggleBinding(string path) => HasBinding(toggleAction, path);
        public bool HasCloseBinding(string path) => HasBinding(closeAction, path);

        private void BuildShop(Transform canvas)
        {
            ShopRoot = new GameObject("SandboxShopRoot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            ShopRoot.transform.SetParent(canvas, false);
            RectTransform blocker = ShopRoot.GetComponent<RectTransform>();
            Stretch(blocker, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ShopRoot.GetComponent<Image>().color = new Color(.01f, .015f, .025f, .62f);

            RectTransform panel = CreateImage(ShopRoot.transform, "SandboxShopPanel", Ink);
            panel.anchorMin = new Vector2(.5f, .5f);
            panel.anchorMax = new Vector2(.5f, .5f);
            panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(840f, 292f);

            CreateLabel(panel, "Title", "SANDBOX MATCH SHOP", 22, TextAnchor.MiddleLeft, Gold,
                new Vector2(20f, -12f), new Vector2(-40f, 34f), Vector2.up, Vector2.one, Vector2.up);
            balanceLabel = CreateLabel(panel, "MatchCredits", string.Empty, 16, TextAnchor.MiddleRight, Cyan,
                new Vector2(20f, -12f), new Vector2(-40f, 34f), Vector2.up, Vector2.one, Vector2.one);

            ItemRow = new GameObject("HorizontalItemRow", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            ItemRow.SetParent(panel, false);
            ItemRow.anchorMin = new Vector2(0f, 0f);
            ItemRow.anchorMax = new Vector2(1f, 1f);
            ItemRow.offsetMin = new Vector2(20f, 54f);
            ItemRow.offsetMax = new Vector2(-20f, -58f);
            HorizontalLayoutGroup layout = ItemRow.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            IReadOnlyList<SandboxShopItemDefinition> items = SandboxShopCatalog.Current.Items;
            for (int index = 0; index < items.Count; index++)
            {
                SandboxShopItemDefinition item = items[index];
                Button button = CreateItemButton(ItemRow, item);
                itemButtons.Add(button);
            }
            ConfigureHorizontalNavigation();

            feedbackLabel = CreateLabel(panel, "Feedback", "B / VIEW: OPEN   A / CLICK: BUY   ESC / B: CLOSE", 12,
                TextAnchor.MiddleCenter, new Color(.75f, .80f, .83f), new Vector2(16f, 9f), new Vector2(-32f, 34f),
                Vector2.zero, Vector2.right, new Vector2(.5f, 0f));
            RefreshProjection();
        }

        private Button CreateItemButton(Transform parent, SandboxShopItemDefinition item)
        {
            GameObject buttonObject = new($"Buy_{item.Id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = Card;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Card;
            colors.highlightedColor = new Color(.18f, .48f, .50f, 1f);
            colors.selectedColor = new Color(.16f, .52f, .54f, 1f);
            colors.pressedColor = Gold;
            button.colors = colors;
            button.onClick.AddListener(() => session.PurchaseFromUi(item.Id));

            Text label = CreateLabel(buttonObject.transform, "Label", string.Empty, 13, TextAnchor.MiddleCenter, Color.white,
                new Vector2(8f, 8f), new Vector2(-16f, -16f), Vector2.zero, Vector2.one, new Vector2(.5f, .5f));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            itemLabels.Add(label);
            return button;
        }

        private void ConfigureHorizontalNavigation()
        {
            for (int index = 0; index < itemButtons.Count; index++)
            {
                Navigation navigation = itemButtons[index].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = itemButtons[(index + itemButtons.Count - 1) % itemButtons.Count];
                navigation.selectOnRight = itemButtons[(index + 1) % itemButtons.Count];
                itemButtons[index].navigation = navigation;
            }
        }

        private void RefreshProjection()
        {
            if (session?.State == null) return;
            if (balanceLabel != null)
                balanceLabel.text = $"MATCH CREDITS  {session.State.MatchCredits}";

            IReadOnlyList<SandboxShopItemDefinition> items = SandboxShopCatalog.Current.Items;
            for (int index = 0; index < items.Count && index < itemLabels.Count; index++)
            {
                SandboxShopItemDefinition item = items[index];
                int owned = session.State.GetOwnedQuantity(item.Id);
                itemLabels[index].text = $"{item.DisplayName.ToUpperInvariant()}\n{KindLabel(item.Kind)}\n\n{item.MatchCreditPrice} MATCH CREDITS\nOWNED {owned}/{item.MaximumPerMatch}";
                itemButtons[index].interactable = owned < item.MaximumPerMatch;
            }
        }

        private void OnPurchaseProcessed(SandboxShopPurchaseResult result)
        {
            RefreshProjection();
            if (feedbackLabel == null) return;
            feedbackLabel.text = result.Status switch
            {
                SandboxShopPurchaseStatus.Purchased => "PURCHASED FOR THIS MATCH ONLY",
                SandboxShopPurchaseStatus.RejectedInsufficientMatchCredits => "NOT ENOUGH MATCH CREDITS",
                SandboxShopPurchaseStatus.RejectedLimitReached => "MATCH LIMIT REACHED",
                SandboxShopPurchaseStatus.RejectedRequestConflict => "DUPLICATE REQUEST CONFLICT REJECTED",
                _ => "PURCHASE REJECTED"
            };
        }

        private void EnsureInputActions()
        {
            if (toggleAction != null) return;
            toggleAction = new InputAction("Sandbox Shop", InputActionType.Button);
            toggleAction.AddBinding(KeyboardToggleBinding);
            toggleAction.AddBinding(GamepadToggleBinding);

            closeAction = new InputAction("Close Sandbox Shop", InputActionType.Button);
            closeAction.AddBinding(KeyboardCloseBinding);
            closeAction.AddBinding(GamepadCloseBinding);
        }

        private void OnTogglePerformed(InputAction.CallbackContext context) => Toggle();

        private void OnClosePerformed(InputAction.CallbackContext context)
        {
            if (IsOpen) SetOpen(false);
        }

        private static bool HasBinding(InputAction action, string path)
        {
            if (action == null || path == null) return false;
            foreach (InputBinding binding in action.bindings)
            {
                if (string.Equals(binding.path, path, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static string KindLabel(SandboxShopItemKind kind)
        {
            return kind switch
            {
                SandboxShopItemKind.LoadoutSidegrade => "SIDEGRADE",
                SandboxShopItemKind.Cosmetic => "COSMETIC",
                SandboxShopItemKind.Consumable => "CONSUMABLE",
                _ => "MATCH ITEM"
            };
        }

        private static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            imageObject.GetComponent<Image>().color = color;
            return imageObject.GetComponent<RectTransform>();
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

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
