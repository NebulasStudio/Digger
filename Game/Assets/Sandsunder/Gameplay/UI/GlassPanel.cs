using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>
    /// Dark-glassmorphism panel (Sandsunder Modern UI): sandstone-dark fill at ~90% opacity, thin
    /// gold border and a subtle diagonal reflection. Works as a runtime-styled Image so it degrades
    /// gracefully when the generated glass frame sprite is not present yet.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class GlassPanel : MonoBehaviour
    {
        [SerializeField] private Color glassColor = new(0.10f, 0.08f, 0.06f, 0.90f); // #1A1410 @ 90%
        [SerializeField] private Color goldBorder = new(0.84f, 0.70f, 0.21f, 1f);    // #D6B336
        [SerializeField] private float borderWidth = 2f;

        private Image image;
        private RectTransform rect;

        private void Awake()
        {
            image = GetComponent<Image>();
            rect = GetComponent<RectTransform>();
            Apply();
        }

        public void Apply()
        {
            if (image == null) return;

#if UNITY_EDITOR
            Sprite glassSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Runtime/Processed/ui_glass_panel.png");
            if (glassSprite != null)
            {
                image.sprite = glassSprite;
            }
#endif
            image.color = new Color(0.06f, 0.05f, 0.04f, 0.88f);
            image.raycastTarget = false;
        }
    }
}