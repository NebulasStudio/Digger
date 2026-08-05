using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SandboxReloadBar : MonoBehaviour
    {
        public static SandboxReloadBar Instance { get; private set; }

        private GameObject reloadBarObj;
        private Image fillImage;
        private Text ammoText;
        private Transform playerTransform;

        private float reloadDuration = 0f;
        private float reloadTimer = 0f;
        private bool isReloading = false;

        private void Awake()
        {
            Instance = this;
            BuildReloadUI();
        }

        public void StartReload(float duration)
        {
            reloadDuration = Mathf.Max(0.1f, duration);
            reloadTimer = reloadDuration;
            isReloading = true;
            if (reloadBarObj != null) reloadBarObj.SetActive(true);
        }

        private void BuildReloadUI()
        {
            GameObject canvasObj = new("SandboxReload_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            reloadBarObj = new GameObject("ReloadBarFrame");
            reloadBarObj.transform.SetParent(canvasObj.transform, false);

            RectTransform frameRect = reloadBarObj.AddComponent<RectTransform>();
            frameRect.sizeDelta = new Vector2(72, 10);

            Image bg = reloadBarObj.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.08f, 0.06f, 0.90f);

            // Gold border frame (premium look).
            GameObject borderObj = new("Border");
            borderObj.transform.SetParent(reloadBarObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = -Vector2.one * 2f;
            borderRect.offsetMax = Vector2.one * 2f;
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0.84f, 0.70f, 0.21f, 0.9f); // gold
            borderObj.transform.SetAsFirstSibling();

            // "RELOADING" label above the bar.
            GameObject labelObj = new("Label");
            labelObj.transform.SetParent(reloadBarObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.sizeDelta = new Vector2(0, 14);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.text = "RELOADING";
            labelText.fontSize = 9;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.84f, 0.70f, 0.21f);

            GameObject fillObj = new("Fill");
            fillObj.transform.SetParent(reloadBarObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);

            fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.20f, 0.95f, 0.90f, 1.0f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            reloadBarObj.SetActive(false);
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(reloadTimer / reloadDuration);
                fillImage.fillAmount = progress;

                if (playerTransform != null && Camera.main != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(playerTransform.position + Vector3.up * 1.1f);
                    reloadBarObj.transform.position = screenPos;
                }

                if (reloadTimer <= 0f)
                {
                    isReloading = false;
                    if (reloadBarObj != null) reloadBarObj.SetActive(false);
                    SandboxVisualEffects.SpawnDust(playerTransform != null ? playerTransform.position : Vector3.zero, 12, new Color(0.20f, 0.95f, 0.90f));
                }
            }
        }
    }
}
