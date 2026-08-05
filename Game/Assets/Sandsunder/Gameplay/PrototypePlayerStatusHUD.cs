using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypePlayerStatusHUD : MonoBehaviour
    {
        public static PrototypePlayerStatusHUD Instance { get; private set; }

        private Canvas canvas;
        private Image healthFill;
        private Text healthText;
        private Image staminaFill;
        private Text staminaText;

        private PrototypeHealth playerHealth;
        private TopDownPlayerController playerMovement;
        private RectTransform frameRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindFirstObjectByType<PrototypePlayerStatusHUD>() == null)
            {
                GameObject hudObj = new("PrototypePlayerStatusHUD_Auto");
                hudObj.AddComponent<PrototypePlayerStatusHUD>();
            }
        }

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            GameObject canvasObj = new("StatusHUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 105;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Floating Over-head status frame
            GameObject frameObj = new("StatusFrame_Overhead");
            frameObj.transform.SetParent(canvasObj.transform, false);
            frameRect = frameObj.AddComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0f);
            frameRect.sizeDelta = new Vector2(140, 24);

            Image frameBg = frameObj.AddComponent<Image>();
            frameBg.color = new Color(0.10f, 0.08f, 0.06f, 0.85f);

            // --- HEALTH BAR ---
            GameObject hpBgObj = new("HealthBar_BG");
            hpBgObj.transform.SetParent(frameObj.transform, false);
            RectTransform hpBgRect = hpBgObj.AddComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 1f);
            hpBgRect.anchorMax = new Vector2(1f, 1f);
            hpBgRect.pivot = new Vector2(0f, 1f);
            hpBgRect.anchoredPosition = new Vector2(3, -3);
            hpBgRect.sizeDelta = new Vector2(-6, 9);
            Image hpBgImg = hpBgObj.AddComponent<Image>();
            hpBgImg.color = new Color(0.35f, 0.12f, 0.10f, 0.95f);

            GameObject hpFillObj = new("HealthBar_Fill");
            hpFillObj.transform.SetParent(hpBgObj.transform, false);
            RectTransform hpFillRect = hpFillObj.AddComponent<RectTransform>();
            hpFillRect.anchorMin = new Vector2(0f, 0f);
            hpFillRect.anchorMax = new Vector2(1f, 1f);
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillRect.sizeDelta = Vector2.zero;
            healthFill = hpFillObj.AddComponent<Image>();
            healthFill.color = new Color(0.20f, 0.82f, 0.40f, 1.0f);

            GameObject hpTextObj = new("HealthText");
            hpTextObj.transform.SetParent(hpBgObj.transform, false);
            RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.sizeDelta = Vector2.zero;
            healthText = hpTextObj.AddComponent<Text>();
            healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthText.alignment = TextAnchor.MiddleCenter;
            healthText.fontSize = 8;
            healthText.color = Color.white;

            // --- STAMINA BAR ---
            GameObject stamBgObj = new("StaminaBar_BG");
            stamBgObj.transform.SetParent(frameObj.transform, false);
            RectTransform stamBgRect = stamBgObj.AddComponent<RectTransform>();
            stamBgRect.anchorMin = new Vector2(0f, 1f);
            stamBgRect.anchorMax = new Vector2(1f, 1f);
            stamBgRect.pivot = new Vector2(0f, 1f);
            stamBgRect.anchoredPosition = new Vector2(3, -14);
            stamBgRect.sizeDelta = new Vector2(-6, 7);
            Image stamBgImg = stamBgObj.AddComponent<Image>();
            stamBgImg.color = new Color(0.28f, 0.22f, 0.10f, 0.95f);

            GameObject stamFillObj = new("StaminaBar_Fill");
            stamFillObj.transform.SetParent(stamBgObj.transform, false);
            RectTransform stamFillRect = stamFillObj.AddComponent<RectTransform>();
            stamFillRect.anchorMin = new Vector2(0f, 0f);
            stamFillRect.anchorMax = new Vector2(1f, 1f);
            stamFillRect.pivot = new Vector2(0f, 0.5f);
            stamFillRect.sizeDelta = Vector2.zero;
            staminaFill = stamFillObj.AddComponent<Image>();
            staminaFill.color = new Color(0.92f, 0.78f, 0.18f, 1.0f);

            GameObject stamTextObj = new("StaminaText");
            stamTextObj.transform.SetParent(stamBgObj.transform, false);
            RectTransform stamTextRect = stamTextObj.AddComponent<RectTransform>();
            stamTextRect.anchorMin = Vector2.zero;
            stamTextRect.anchorMax = Vector2.one;
            stamTextRect.sizeDelta = Vector2.zero;
            staminaText = stamTextObj.AddComponent<Text>();
            staminaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            staminaText.alignment = TextAnchor.MiddleCenter;
            staminaText.fontSize = 7;
            staminaText.color = Color.white;
        }

        private void LateUpdate()
        {
            if (playerHealth == null || playerMovement == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj == null)
                {
                    var controller = FindFirstObjectByType<TopDownPlayerController>();
                    if (controller != null) playerObj = controller.gameObject;
                }

                if (playerObj != null)
                {
                    playerHealth = playerObj.GetComponent<PrototypeHealth>();
                    playerMovement = playerObj.GetComponent<TopDownPlayerController>();
                }
            }

            if (playerMovement != null && frameRect != null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 worldPos = playerMovement.transform.position + new Vector3(0f, 1.35f, 0f);
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                    RectTransform UtilityCanvasRect = canvas.GetComponent<RectTransform>();
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        UtilityCanvasRect, screenPos, null, out Vector2 localPos))
                    {
                        frameRect.anchoredPosition = localPos;
                    }
                }
            }

            if (playerHealth != null)
            {
                float hpRatio = Mathf.Clamp01((float)playerHealth.CurrentHealth / Mathf.Max(1, playerHealth.MaximumHealth));
                healthFill.rectTransform.anchorMax = new Vector2(hpRatio, 1f);
                healthText.text = $"HP  {playerHealth.CurrentHealth} / {playerHealth.MaximumHealth}";
            }

            if (playerMovement != null)
            {
                float stamRatio = Mathf.Clamp01(playerMovement.CurrentStamina / playerMovement.MaxStamina);
                staminaFill.rectTransform.anchorMax = new Vector2(stamRatio, 1f);
                staminaText.text = $"STAMINA  {playerMovement.CurrentStamina:F0}";
            }
        }
    }
}
