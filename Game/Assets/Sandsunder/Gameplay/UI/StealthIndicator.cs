using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>
    /// Stealth status indicator (Sandsunder Modern UI): a cyan (#00F0E6) dot/glyph that lights up
    /// while the Nomad is subterranean, fed by DigDepthSystem.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class StealthIndicator : MonoBehaviour
    {
        [SerializeField] private Color activeColor = new(0f, 0.94f, 0.90f, 1f);
        [SerializeField] private Color idleColor = new(0.25f, 0.22f, 0.18f, 0.6f);

        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            Refresh(DigDepthSystem.Instance != null && DigDepthSystem.Instance.IsSubterranean);
            if (DigDepthSystem.Instance != null)
            {
                DigDepthSystem.Instance.SubterraneanChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (DigDepthSystem.Instance != null)
            {
                DigDepthSystem.Instance.SubterraneanChanged -= Refresh;
            }
        }

        private void Refresh(bool subterranean)
        {
            if (image != null) image.color = subterranean ? activeColor : idleColor;
        }
    }
}