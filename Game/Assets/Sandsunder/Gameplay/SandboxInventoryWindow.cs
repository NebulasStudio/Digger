using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SandboxInventoryWindow : MonoBehaviour
    {
        public static SandboxInventoryWindow Instance { get; private set; }

        private GameObject windowRoot;
        private bool isOpen = false;

        private Text titleText;
        private Text statsText;
        private Text descriptionText;

        public bool IsOpen => isOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindFirstObjectByType<SandboxInventoryWindow>() == null)
            {
                GameObject obj = new("SandboxInventoryWindow_Auto");
                obj.AddComponent<SandboxInventoryWindow>();
            }
        }

        private void Awake()
        {
            Instance = this;
            BuildInventoryWindowUI();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleWindow();
            }

            if (isOpen)
            {
                UpdateSelectedWeaponDetails();
            }
        }

        public void ToggleWindow()
        {
            isOpen = !isOpen;
            if (windowRoot != null) windowRoot.SetActive(isOpen);
            SandboxVisualEffects.SpawnDust(Vector3.zero, 10, new Color(0.20f, 0.90f, 0.85f));
        }

        private void BuildInventoryWindowUI()
        {
            GameObject canvasObj = new("SandboxInventory_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            windowRoot = new GameObject("InventoryWindowRoot");
            windowRoot.transform.SetParent(canvasObj.transform, false);

            RectTransform windowRect = windowRoot.AddComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(580, 380);

            // Dark Pixel Art Background
            Image bg = windowRoot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.07f, 0.06f, 0.94f);

            // Header Title
            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(windowRoot.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -12);
            titleRect.sizeDelta = new Vector2(0, 36);

            titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.20f, 0.95f, 0.90f, 1.0f);
            titleText.text = "INVENTARIO ARSENALE & SCHEDA STATISTICHE [TAB]";
            titleText.alignment = TextAnchor.MiddleCenter;

            // Left Side: Weapon Stats Card Panel
            GameObject statsPanel = new("StatsCardPanel");
            statsPanel.transform.SetParent(windowRoot.transform, false);
            RectTransform statsRect = statsPanel.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(0.5f, 1f);
            statsRect.offsetMin = new Vector2(16, 16);
            statsRect.offsetMax = new Vector2(-8, -50);

            Image statsBg = statsPanel.AddComponent<Image>();
            statsBg.color = new Color(0.14f, 0.12f, 0.10f, 0.90f);

            GameObject statsTextObj = new("StatsText");
            statsTextObj.transform.SetParent(statsPanel.transform, false);
            RectTransform statsTextRect = statsTextObj.AddComponent<RectTransform>();
            statsTextRect.anchorMin = Vector2.zero;
            statsTextRect.anchorMax = Vector2.one;
            statsTextRect.offsetMin = new Vector2(12, 12);
            statsTextRect.offsetMax = new Vector2(-12, -12);

            statsText = statsTextObj.AddComponent<Text>();
            statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsText.fontSize = 13;
            statsText.color = Color.white;
            statsText.text = "Seleziona un'arma dall'HUD per visualizzare le statistiche...";

            // Right Side: Description Card Panel
            GameObject descPanel = new("DescCardPanel");
            descPanel.transform.SetParent(windowRoot.transform, false);
            RectTransform descRect = descPanel.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.5f, 0f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.offsetMin = new Vector2(8, 16);
            descRect.offsetMax = new Vector2(-16, -50);

            Image descBg = descPanel.AddComponent<Image>();
            descBg.color = new Color(0.14f, 0.12f, 0.10f, 0.90f);

            GameObject descTextObj = new("DescText");
            descTextObj.transform.SetParent(descPanel.transform, false);
            RectTransform descTextRect = descTextObj.AddComponent<RectTransform>();
            descTextRect.anchorMin = Vector2.zero;
            descTextRect.anchorMax = Vector2.one;
            descTextRect.offsetMin = new Vector2(12, 12);
            descTextRect.offsetMax = new Vector2(-12, -12);

            descriptionText = descTextObj.AddComponent<Text>();
            descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descriptionText.fontSize = 12;
            descriptionText.color = new Color(0.85f, 0.82f, 0.75f);
            descriptionText.text = "Premi i tasti [1-6] per cambiare arma equipaggiata.";

            windowRoot.SetActive(false);
        }

        private void UpdateSelectedWeaponDetails()
        {
            if (PrototypeInventoryHUD.Instance == null) return;

            int sel = PrototypeInventoryHUD.Instance.SelectedIndex;
            var items = PrototypeInventoryHUD.Instance.InventoryItems;
            if (sel < 0 || sel >= items.Count) return;

            string itemId = items[sel];

            switch (itemId)
            {
                case "shovel.default":
                    statsText.text = "<b>PALA DA SCAVO IN ACCIAIO</b>\n\n" +
                        "• Danno Melee: 15 HP\n" +
                        "• Portata: 1.25 Metri\n" +
                        "• Azione Secondaria: Scavo Sabbia / Tunnel\n" +
                        "• Tempo Scavo: Canalizzato (0.8s)\n" +
                        "• Tipo: Attrezzo Terreno & Melee";
                    descriptionText.text = "Spada da scavo forgiata in acciaio inossidabile con manico in legno rinforzato. Permette di scavare trincee, rivelare reliquie sepolte ed aprire tunnel sotterranei.";
                    break;

                case "rifle.brass":
                    statsText.text = "<b>FUCILE DI PRECISIONE IN OTTONE</b>\n\n" +
                        "• Danno: 40 HP per colpo\n" +
                        "• Velocità Proiettile: 24.0 m/s\n" +
                        "• Gittata: 18.0 Metri\n" +
                        "• Caricatore: 10 Colpi\n" +
                        "• Tempo Ricarica: 1.2 Secondi\n" +
                        "• Effetti: Reiezione Bossolo Dorato";
                    descriptionText.text = "Arma da fuoco a lungo raggio forgiata in ottone con ottica di puntamento. Spara proiettili dorati ad alta penetrazione capaci di abbattere i mob a distanza.";
                    break;

                case "sword.scimitar":
                    statsText.text = "<b>SCIMITARRA DEL DESERTO</b>\n\n" +
                        "• Danno Melee: 35 HP ad fendente\n" +
                        "• Arco di Fendente: 180°\n" +
                        "• Portata: 1.6 Metri\n" +
                        "• Cadenza Fendente: 0.45s\n" +
                        "• Effetti: Fendente ad Arco Ciano & Impulso Knockback";
                    descriptionText.text = "Lama curva del deserto forgiata con elsa in oro decorato. Ideale per sminuzzare nemici e distruggere vasi, scrigni e porte nelle rovine.";
                    break;

                case "shotgun.heavy":
                    statsText.text = "<b>SHOTGUN PESANTE A DOPPIA CANNA</b>\n\n" +
                        "• Danno: 22 HP x 5 Pallini (110 Totale)\n" +
                        "• Rosata: 5 Proiettili a Cono (30°)\n" +
                        "• Gittata: 10.0 Metri\n" +
                        "• Caricatore: 6 Cartucce\n" +
                        "• Tempo Ricarica: 1.8 Secondi\n" +
                        "• Effetti: Vampata Pesante & Fumo";
                    descriptionText.text = "Fucile a canne mozze pesante che spara una devastante rosata di 5 pallini d'acciaio a corto raggio. Devastante nei combattimenti ravvicinati.";
                    break;

                case "blaster.rune":
                    statsText.text = "<b>BLASTER RUNICO AL PLASMA</b>\n\n" +
                        "• Danno: 38 HP per dardo\n" +
                        "• Velocità Proiettile: 22.0 m/s\n" +
                        "• Cadenza di Fuoco: Ultra Rapida (0.2s)\n" +
                        "• Caricatore: 15 Cariche d'Energia\n" +
                        "• Tempo Ricarica: 0.9 Secondi\n" +
                        "• Effetti: Dardo d'Energia Ciano Concentrato";
                    descriptionText.text = "Antica pistole runica che canalizza l'energia sotterranea sputando dardi al plasma ciano ad elevatissima frequenza.";
                    break;

                default:
                    statsText.text = $"<b>OGGETTO: {itemId.ToUpper()}</b>\n\n• Categoria: Risorsa / Chiave\n• Utilizzo: Automatico";
                    descriptionText.text = "Oggetto speciale utilizzato per sbloccare vasi, porte o ripristinare la salute.";
                    break;
            }
        }
    }
}
