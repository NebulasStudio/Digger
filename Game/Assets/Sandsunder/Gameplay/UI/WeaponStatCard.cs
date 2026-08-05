using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>
    /// Dynamic weapon stat card (Sandsunder Modern UI, right side of the TAB inventory).
    /// Shows three comparative bars (damage, range, fire-rate) against the currently equipped weapon,
    /// fed by versioned weapon data (Design/balance/weapons.csv -> WeaponCatalog).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponStatCard : MonoBehaviour
    {
        [System.Serializable]
        public struct WeaponStats
        {
            public float Damage;
            public float Range;
            public float FireRate;
        }

        [SerializeField] private Slider damageBar;
        [SerializeField] private Slider rangeBar;
        [SerializeField] private Slider fireRateBar;
        [SerializeField] private Text weaponName;

        /// <summary>Runtime wiring used by the auto-built modern HUD (SandboxModernHUD).</summary>
        public void Setup(Text nameText, Slider damage, Slider range, Slider fireRate)
        {
            weaponName = nameText;
            damageBar = damage;
            rangeBar = range;
            fireRateBar = fireRate;
        }

        public void Show(string itemId, WeaponStats stats, WeaponStats equipped)
        {
            if (weaponName != null && !string.IsNullOrEmpty(itemId))
            {
                weaponName.text = itemId.Replace(".", " ").ToUpperInvariant();
            }

            // Normalize each stat against the max of (self, equipped) so the comparison is readable.
            if (damageBar != null) damageBar.value = Stat01(stats.Damage, equipped.Damage);
            if (rangeBar != null) rangeBar.value = Stat01(stats.Range, equipped.Range);
            if (fireRateBar != null) fireRateBar.value = Stat01(stats.FireRate, equipped.FireRate);
        }

        private static float Stat01(float value, float reference)
        {
            float max = Mathf.Max(value, reference, 0.01f);
            return Mathf.Clamp01(value / max);
        }
    }
}