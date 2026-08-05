using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>
    /// Reactive status bar (Sandsunder Modern UI). HP uses neon green (#00FF7A), Stamina uses sand
    /// yellow (#D6B336). The fill lerps smoothly and a label shows "current/max".
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class StatBarWidget : MonoBehaviour
    {
        [SerializeField] private Color lowColor = new(0.93f, 0.20f, 0.16f);
        [SerializeField] private float smoothTime = 6f;

        private Image fill;
        private Text label;
        private float targetAmount = 1f;

        private void Awake()
        {
            fill = GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            BuildLabel();
        }

        private void BuildLabel()
        {
            if (transform.Find("Value") != null)
            {
                label = transform.Find("Value").GetComponent<Text>();
                return;
            }
            GameObject go = new("Value", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public void SetColor(Color color)
        {
            if (fill != null) fill.color = color;
        }

        public void SetValue(float current, float max)
        {
            if (max <= 0f) return;
            targetAmount = Mathf.Clamp01(current / max);
        }

        private void Update()
        {
            if (fill == null) return;
            fill.fillAmount = Mathf.Lerp(fill.fillAmount, targetAmount, Time.deltaTime * smoothTime);
            if (Mathf.Abs(fill.fillAmount - targetAmount) < 0.001f) fill.fillAmount = targetAmount;

            if (label != null)
            {
                float shown = Mathf.RoundToInt(targetAmount * 100f);
                label.text = $"{shown}/100";
            }
        }
    }
}