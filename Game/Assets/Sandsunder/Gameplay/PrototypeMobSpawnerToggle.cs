using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMobSpawnerToggle : MonoBehaviour
    {
        private Canvas canvas;
        private Toggle toggle;
        private bool mobsActive = true;

        public static PrototypeMobSpawnerToggle Instance { get; private set; }

        public bool MobsActive => mobsActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindFirstObjectByType<PrototypeMobSpawnerToggle>() == null)
            {
                GameObject autoToggle = new("PrototypeMobSpawnerToggle_Auto");
                autoToggle.AddComponent<PrototypeMobSpawnerToggle>();
            }
        }

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        public void TriggerRespawnAll()
        {
            var spitters = FindObjectsByType<PrototypeDuneSpitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var spitter in spitters)
            {
                spitter.gameObject.SetActive(true);
                var hp = spitter.GetComponent<PrototypeHealth>();
                if (hp != null)
                {
                    hp.RespawnNow();
                }
            }
            SandboxVisualEffects.SpawnDust(Vector3.zero, 25, new Color(0.94f, 0.36f, 0.25f));
            Debug.Log("[MobSpawner] MOB RESPAWNATI TRAMITE TASTO M!");
        }

        private void BuildUI()
        {
            GameObject canvasObj = new("MobToggle_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panelObj = new("MobTogglePanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24, -20);
            panelRect.sizeDelta = new Vector2(220, 30);

            Image bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);

            GameObject textObj = new("Text");
            textObj.transform.SetParent(panelObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition = new Vector2(10, 0);
            textRect.sizeDelta = new Vector2(200, 30);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.text = "Nemici Attivi [M]";
            text.alignment = TextAnchor.MiddleLeft;

            toggle = panelObj.AddComponent<Toggle>();
            toggle.isOn = mobsActive;
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                mobsActive = !mobsActive;
                if (toggle != null) toggle.isOn = mobsActive;
                ApplyState();
            }
        }

        private void OnToggleChanged(bool value)
        {
            mobsActive = value;
            ApplyState();
        }

        private void ApplyState()
        {
            var spitters = FindObjectsByType<PrototypeDuneSpitter>(FindObjectsSortMode.None);
            foreach (var spitter in spitters)
            {
                spitter.gameObject.SetActive(mobsActive);
            }
        }
    }
}
