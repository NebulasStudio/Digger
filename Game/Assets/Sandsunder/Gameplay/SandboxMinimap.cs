using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Sandsunder.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SandboxMinimap : MonoBehaviour
    {
        public static SandboxMinimap Instance { get; private set; }

        private Canvas minimapCanvas;
        private RectTransform minimapFrame;
        private RawImage mapImage;
        private Texture2D minimapTexture;
        private Color32[] texturePixels;
        private Transform playerTransform;

        private const int MapResolution = 96;
        private const float ArenaWidth = 48f;
        private const float ArenaHeight = 32f;

        private void Awake()
        {
            Instance = this;
            BuildMinimapUI();
        }

        private void BuildMinimapUI()
        {
            GameObject canvasObj = new("SandboxMinimap_Canvas");
            minimapCanvas = canvasObj.AddComponent<Canvas>();
            minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            minimapCanvas.sortingOrder = 95;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Minimap Frame in Top-Right Corner
            GameObject frameObj = new("MinimapFrame");
            frameObj.transform.SetParent(canvasObj.transform, false);
            minimapFrame = frameObj.AddComponent<RectTransform>();
            minimapFrame.anchorMin = new Vector2(1f, 1f);
            minimapFrame.anchorMax = new Vector2(1f, 1f);
            minimapFrame.pivot = new Vector2(1f, 1f);
            minimapFrame.anchoredPosition = new Vector2(-16, -16);
            minimapFrame.sizeDelta = new Vector2(140, 100);

            Image border = frameObj.AddComponent<Image>();
#if UNITY_EDITOR
            Sprite glassSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sandsunder/Art/Runtime/UI/ui_glass_panel.png");
            if (glassSprite != null)
            {
                border.sprite = glassSprite;
                border.type = Image.Type.Sliced;
                border.color = Color.white;
            }
            else
            {
                border.color = new Color(0.12f, 0.10f, 0.08f, 0.85f);
            }
#else
            border.color = new Color(0.12f, 0.10f, 0.08f, 0.85f);
#endif

            // Inner Map Texture Display
            GameObject mapObj = new("MapDisplay");
            mapObj.transform.SetParent(frameObj.transform, false);
            RectTransform mapRect = mapObj.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = new Vector2(4, 4);
            mapRect.offsetMax = new Vector2(-4, -4);

            mapImage = mapObj.AddComponent<RawImage>();
            minimapTexture = new Texture2D(MapResolution, MapResolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            texturePixels = new Color32[MapResolution * MapResolution];
            mapImage.texture = minimapTexture;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            RenderMinimapFrame();
        }

        private void RenderMinimapFrame()
        {
            Color32 sandBg = new(215, 175, 115, 255);
            Color32 wallColor = new(135, 95, 60, 255);
            Color32 sanctuaryColor = new(160, 110, 70, 255);
            Color32 playerCyan = new(50, 240, 230, 255);
            Color32 enemyRed = new(240, 80, 60, 255);
            Color32 doorGold = new(245, 200, 50, 255);

            for (int i = 0; i < texturePixels.Length; i++)
            {
                texturePixels[i] = sandBg;
            }

            // Draw Central Sanctuary (Ruin Sanctuary 16x12m)
            int minX = Mathf.RoundToInt(((-8f + (ArenaWidth * 0.5f)) / ArenaWidth) * MapResolution);
            int maxX = Mathf.RoundToInt(((8f + (ArenaWidth * 0.5f)) / ArenaWidth) * MapResolution);
            int minY = Mathf.RoundToInt(((-6f + (ArenaHeight * 0.5f)) / ArenaHeight) * MapResolution);
            int maxY = Mathf.RoundToInt(((6f + (ArenaHeight * 0.5f)) / ArenaHeight) * MapResolution);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (x >= 0 && x < MapResolution && y >= 0 && y < MapResolution)
                    {
                        texturePixels[(y * MapResolution) + x] = sanctuaryColor;
                    }
                }
            }

            // Draw Outer Walls
            for (int x = 0; x < MapResolution; x++)
            {
                texturePixels[x] = wallColor;
                texturePixels[((MapResolution - 1) * MapResolution) + x] = wallColor;
            }
            for (int y = 0; y < MapResolution; y++)
            {
                texturePixels[y * MapResolution] = wallColor;
                texturePixels[(y * MapResolution) + (MapResolution - 1)] = wallColor;
            }

            // Draw Ruin Doors / Key Objects
            var doors = FindObjectsByType<PrototypeDesertRuinDoor>(FindObjectsSortMode.None);
            foreach (var door in doors)
            {
                Vector2 pos = door.transform.position;
                DrawBlip(pos, doorGold, 2);
            }

            // Draw Enemies (Dune Spitters)
            var spitters = FindObjectsByType<PrototypeDuneSpitter>(FindObjectsSortMode.None);
            foreach (var spitter in spitters)
            {
                var hp = spitter.GetComponent<PrototypeHealth>();
                if (hp != null && !hp.IsDead)
                {
                    DrawBlip(spitter.transform.position, enemyRed, 2);
                }
            }

            // Draw Player Position
            if (playerTransform != null)
            {
                DrawBlip(playerTransform.position, playerCyan, 3);
            }

            minimapTexture.SetPixels32(texturePixels);
            minimapTexture.Apply(false);
        }

        private void DrawBlip(Vector2 worldPos, Color32 color, int radius)
        {
            int cx = Mathf.Clamp(Mathf.RoundToInt(((worldPos.x + (ArenaWidth * 0.5f)) / ArenaWidth) * MapResolution), 0, MapResolution - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt(((worldPos.y + (ArenaHeight * 0.5f)) / ArenaHeight) * MapResolution), 0, MapResolution - 1);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < MapResolution && py >= 0 && py < MapResolution)
                    {
                        texturePixels[(py * MapResolution) + px] = color;
                    }
                }
            }
        }
    }
}
