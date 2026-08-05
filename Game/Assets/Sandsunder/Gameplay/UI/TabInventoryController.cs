using UnityEngine;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>
    /// TAB inventory modal (Sandsunder Modern UI). Opens/closes the inventory root on Tab, pauses
    /// gameplay input while open, and refreshes the paper-doll + weapon stat card. Layout is built
    /// at 1280x720 reference resolution so it stays legible on supported resolutions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TabInventoryController : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryRoot;
        [SerializeField] private WeaponStatCard statCard;

        public bool IsOpen { get; private set; }

        /// <summary>Runtime wiring used by the auto-built modern HUD (SandboxModernHUD).</summary>
        public void Setup(GameObject root, WeaponStatCard card)
        {
            inventoryRoot = root;
            statCard = card;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Toggle();
            }

            if (IsOpen)
            {
                // Esc closes the inventory too, instead of pausing the game.
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetOpen(false);
                }
            }
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (inventoryRoot != null)
            {
                inventoryRoot.SetActive(open);
            }

            // Pause/unpause gameplay simulation while the inventory is open.
            Time.timeScale = open ? 0f : 1f;

            if (open)
            {
                RefreshCard();
            }
        }

        private void RefreshCard()
        {
            if (statCard == null) return;
            // Placeholder: real stats come from the WeaponCatalog (Design/balance/weapons.csv).
            var equipped = new WeaponStatCard.WeaponStats { Damage = 60f, Range = 0.7f, FireRate = 0.5f };
            var current = new WeaponStatCard.WeaponStats { Damage = 45f, Range = 0.55f, FireRate = 0.4f };
            statCard.Show(PrototypeInventoryHUD.Instance?.InventoryItems?[0] ?? "shovel.default", current, equipped);
        }
    }
}