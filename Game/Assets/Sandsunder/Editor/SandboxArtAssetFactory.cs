using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sandsunder.Editor
{
    internal sealed class SandboxArtSet
    {
        public Sprite SandTile { get; set; }
        public Sprite RuinTile { get; set; }
        public Sprite Nomad { get; set; }
        public Sprite Spitter { get; set; }
        public Sprite Shadow { get; set; }
        public Sprite Pistol { get; set; }
        public Sprite Shovel { get; set; }
        public Sprite Scimitar { get; set; }
        public Sprite Shotgun { get; set; }
        public Sprite Blaster { get; set; }
        public Sprite DigIntact { get; set; }
        public Sprite DigCracked { get; set; }
        public Sprite DigOpened { get; set; }
        public Sprite SandTuft { get; set; }
        public Sprite Bone { get; set; }
        public Sprite CyanRune { get; set; }
        public RuntimeAnimatorController PlayerAnimator { get; set; }
    }

    internal static class SandboxArtAssetFactory
    {
        private const string RuntimeRoot = "Assets/Sandsunder/Art/Runtime";
        private const string GeneratedRoot = "Assets/Sandsunder/Art/Generated";

        public static SandboxArtSet LoadOrCreate()
        {
            EnsureAssetFolder(GeneratedRoot);

            Sprite sand = ImportTile($"{RuntimeRoot}/Processed/sand_basecolor.png", pixelsPerUnit: 256f);
            Sprite ruin = ImportTile($"{RuntimeRoot}/Processed/ruin_basecolor.png", pixelsPerUnit: 256f);
            Sprite nomad = ImportSprite(
                $"{RuntimeRoot}/Processed/nomad_32.png",
                pixelsPerUnit: 32f,
                pivot: new Vector2(0.5f, 0.08f));
            Sprite spitter = ImportSprite(
                $"{RuntimeRoot}/Processed/spitter_32.png",
                pixelsPerUnit: 32f,
                pivot: new Vector2(0.5f, 0.08f));

            RuntimeAnimatorController animator = LoadOrCreatePlayerAnimator();

            return new SandboxArtSet
            {
                SandTile = sand,
                RuinTile = ruin,
                Nomad = nomad,
                Spitter = spitter,
                PlayerAnimator = animator,
                Shadow = CreateProceduralSprite("BlobShadow", 32, 16, 32f, DrawShadow),
                Pistol = CreateProceduralSprite("BrassPistol", 20, 10, 32f, DrawPistol),
                Shovel = CreateProceduralSprite("StarterShovel", 24, 12, 32f, DrawShovel),
                Scimitar = CreateProceduralSprite("DesertScimitar", 28, 14, 32f, DrawScimitar),
                Shotgun = CreateProceduralSprite("HeavyShotgun", 30, 12, 32f, DrawShotgun),
                Blaster = CreateProceduralSprite("RuneBlaster", 26, 12, 32f, DrawBlaster),
                DigIntact = CreateProceduralSprite("DigIntact", 32, 24, 32f,
                    (pixels, width, height) => DrawDigNode(pixels, width, height, 0)),
                DigCracked = CreateProceduralSprite("DigCracked", 32, 24, 32f,
                    (pixels, width, height) => DrawDigNode(pixels, width, height, 1)),
                DigOpened = CreateProceduralSprite("DigOpened", 32, 24, 32f,
                    (pixels, width, height) => DrawDigNode(pixels, width, height, 2)),
                SandTuft = CreateProceduralSprite("SandTuft", 20, 20, 32f, DrawSandTuft),
                Bone = CreateProceduralSprite("DesertBone", 24, 16, 32f, DrawBone),
                CyanRune = CreateProceduralSprite("CyanRune", 32, 32, 32f, DrawCyanRune),
            };
        }

        private static Sprite ImportTile(string assetPath, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing sandbox art source: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            TextureImporterSettings importerSettings = new();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(importerSettings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Unable to import tile sprite at {assetPath}.");
            }

            return sprite;
        }

        private static Sprite ImportSprite(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing sandbox runtime sprite: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            TextureImporterSettings importerSettings = new();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importerSettings.spritePivot = pivot;
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(importerSettings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Unable to import runtime sprite at {assetPath}.");
            }

            return sprite;
        }

        private static Sprite CreateKeyedSprite(
            string sourcePath,
            string assetName,
            int targetHeightPixels,
            float pixelsPerUnit)
        {
            string texturePath = $"{GeneratedRoot}/{assetName}KeyedTexture.asset";
            string spritePath = $"{GeneratedRoot}/{assetName}KeyedSprite.asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null)
            {
                return existing;
            }

            TextureImporter importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing keyed sprite source: {sourcePath}");
            }

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            Color32[] sourcePixels = source.GetPixels32();
            int minimumX = source.width;
            int minimumY = source.height;
            int maximumX = -1;
            int maximumY = -1;

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    int index = (y * source.width) + x;
                    Color32 pixel = sourcePixels[index];
                    if (IsKeyMagenta(pixel))
                    {
                        pixel.a = 0;
                        sourcePixels[index] = pixel;
                        continue;
                    }

                    pixel.a = 255;
                    sourcePixels[index] = pixel;
                    minimumX = Math.Min(minimumX, x);
                    minimumY = Math.Min(minimumY, y);
                    maximumX = Math.Max(maximumX, x);
                    maximumY = Math.Max(maximumY, y);
                }
            }

            if (maximumX < minimumX || maximumY < minimumY)
            {
                throw new InvalidOperationException($"No opaque subject remained after keying {sourcePath}.");
            }

            const int sourcePadding = 8;
            minimumX = Math.Max(0, minimumX - sourcePadding);
            minimumY = Math.Max(0, minimumY - sourcePadding);
            maximumX = Math.Min(source.width - 1, maximumX + sourcePadding);
            maximumY = Math.Min(source.height - 1, maximumY + sourcePadding);
            int cropWidth = maximumX - minimumX + 1;
            int cropHeight = maximumY - minimumY + 1;
            float scale = Math.Min(1f, targetHeightPixels / (float)cropHeight);
            int targetWidth = Math.Max(1, Mathf.RoundToInt(cropWidth * scale));
            int targetHeight = Math.Max(1, Mathf.RoundToInt(cropHeight * scale));
            Color32[] targetPixels = new Color32[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                int sourceY = minimumY + Math.Min(cropHeight - 1, Mathf.FloorToInt(y / scale));
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = minimumX + Math.Min(cropWidth - 1, Mathf.FloorToInt(x / scale));
                    targetPixels[(y * targetWidth) + x] = sourcePixels[(sourceY * source.width) + sourceX];
                }
            }

            Texture2D texture = new(targetWidth, targetHeight, TextureFormat.RGBA32, false)
            {
                name = $"{assetName} Keyed Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(targetPixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(texture, texturePath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, targetWidth, targetHeight),
                new Vector2(0.5f, 0.08f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{assetName} Keyed Sprite";
            AssetDatabase.CreateAsset(sprite, spritePath);
            return sprite;
        }

        private static bool IsKeyMagenta(Color32 pixel)
        {
            return pixel.r >= 145
                && pixel.b >= 145
                && pixel.g <= 145
                && pixel.r > pixel.g + 35
                && pixel.b > pixel.g + 35;
        }

        private static Sprite CreateProceduralSprite(
            string assetName,
            int width,
            int height,
            float pixelsPerUnit,
            Action<Color32[], int, int> draw)
        {
            string texturePath = $"{GeneratedRoot}/{assetName}Texture.asset";
            string spritePath = $"{GeneratedRoot}/{assetName}Sprite.asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null)
            {
                return existing;
            }

            Color32[] pixels = new Color32[width * height];
            draw(pixels, width, height);
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{assetName} Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(texture, texturePath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.12f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{assetName} Sprite";
            AssetDatabase.CreateAsset(sprite, spritePath);
            return sprite;
        }

        private static void DrawShadow(Color32[] pixels, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - ((width - 1) * 0.5f)) / (width * 0.48f);
                    float dy = (y - ((height - 1) * 0.5f)) / (height * 0.42f);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        pixels[(y * width) + x] = new Color32(43, 33, 30, 120);
                    }
                }
            }
        }

        private static void DrawPistol(Color32[] pixels, int width, int height)
        {
            Fill(pixels, width, 2, 4, 15, 7, new Color32(43, 33, 30, 255));
            Fill(pixels, width, 3, 5, 17, 6, new Color32(205, 139, 51, 255));
            Fill(pixels, width, 5, 2, 8, 5, new Color32(90, 54, 35, 255));
            Fill(pixels, width, 4, 7, 9, 8, new Color32(243, 228, 194, 255));
        }

        private static void DrawShovel(Color32[] pixels, int width, int height)
        {
            Color32 umber = new(43, 33, 30, 255);
            Color32 wood = new(135, 82, 45, 255);
            Color32 steelLight = new(220, 225, 235, 255);
            Color32 steelDark = new(100, 110, 125, 255);

            // T-Handle Top
            Fill(pixels, width, 2, 3, 4, 8, umber);
            Fill(pixels, width, 3, 4, 3, 7, wood);

            // Wooden Shaft
            for (int x = 4; x <= 14; x++)
            {
                Set(pixels, width, x, 5, umber);
                Set(pixels, width, x, 6, wood);
                Set(pixels, width, x, 7, umber);
            }

            // Spade Scoop Blade
            Fill(pixels, width, 14, 3, 21, 9, umber);
            Fill(pixels, width, 15, 4, 20, 8, steelDark);
            Fill(pixels, width, 16, 5, 22, 7, steelLight);
            Set(pixels, width, 23, 6, steelLight);
        }

        private static void DrawScimitar(Color32[] pixels, int width, int height)
        {
            Color32 steel = new(210, 215, 222, 255);
            Color32 gold = new(240, 192, 60, 255);
            Color32 wood = new(92, 57, 39, 255);

            Fill(pixels, width, 2, 4, 6, 8, wood);
            Fill(pixels, width, 6, 3, 8, 9, gold);
            for (int x = 8; x < width - 2; x++)
            {
                int y = 5 + (x > 18 ? (x - 18) / 2 : 0);
                Set(pixels, width, x, y, steel);
                Set(pixels, width, x, y + 1, steel);
            }
        }

        private static void DrawShotgun(Color32[] pixels, int width, int height)
        {
            Color32 steel = new(80, 85, 92, 255);
            Color32 wood = new(135, 82, 45, 255);

            Fill(pixels, width, 2, 3, 9, 7, wood);
            Fill(pixels, width, 9, 6, width - 2, 8, steel);
            Fill(pixels, width, 9, 3, width - 2, 5, steel);
        }

        private static void DrawBlaster(Color32[] pixels, int width, int height)
        {
            Color32 steel = new(60, 65, 75, 255);
            Color32 cyanEnergy = new(50, 240, 230, 255);

            Fill(pixels, width, 3, 3, 18, 8, steel);
            Fill(pixels, width, 8, 5, 14, 7, cyanEnergy);
            Fill(pixels, width, 18, 4, width - 2, 7, cyanEnergy);
        }

        private static void DrawDigNode(Color32[] pixels, int width, int height, int state)
        {
            Color32 umberOutline = new(43, 33, 30, 255);
            Color32 woodDark = new(92, 57, 39, 255);
            Color32 woodLight = new(154, 98, 54, 255);
            Color32 ironBand = new(72, 78, 85, 255);
            Color32 goldLatch = new(240, 192, 60, 255);

            if (state == 2)
            {
                // Opened chest / excavated loot pit
                Fill(pixels, width, 4, 3, 27, 20, umberOutline);
                Fill(pixels, width, 6, 5, 25, 18, woodDark);
                Fill(pixels, width, 8, 4, 23, 11, new Color32(20, 15, 12, 255));
                Fill(pixels, width, 10, 5, 21, 9, new Color32(220, 180, 50, 255));
            }
            else
            {
                // Intact or Cracked wooden chest
                Fill(pixels, width, 4, 3, 27, 20, umberOutline);
                Fill(pixels, width, 6, 5, 25, 18, woodLight);
                // Wood grain planks
                DrawLine(pixels, width, 6, 11, 25, 11, umberOutline);
                // Iron bands
                Fill(pixels, width, 9, 5, 11, 18, ironBand);
                Fill(pixels, width, 20, 5, 22, 18, ironBand);
                // Gold latch
                Fill(pixels, width, 14, 9, 17, 13, goldLatch);

                if (state == 1)
                {
                    // Crack lines
                    DrawLine(pixels, width, 12, 16, 15, 8, umberOutline);
                    DrawLine(pixels, width, 15, 8, 18, 14, umberOutline);
                }
            }
        }

        private static void DrawSandTuft(Color32[] pixels, int width, int height)
        {
            Color32 dark = new(111, 76, 41, 255);
            Color32 light = new(211, 149, 55, 255);
            DrawLine(pixels, width, 9, 2, 6, 16, dark);
            DrawLine(pixels, width, 10, 2, 10, 18, light);
            DrawLine(pixels, width, 11, 2, 15, 15, dark);
        }

        private static void DrawBone(Color32[] pixels, int width, int height)
        {
            Color32 outline = new(62, 44, 36, 255);
            Color32 bone = new(243, 228, 194, 255);
            Fill(pixels, width, 4, 6, 19, 9, outline);
            Fill(pixels, width, 5, 7, 18, 8, bone);
            Fill(pixels, width, 2, 4, 6, 11, outline);
            Fill(pixels, width, 17, 4, 21, 11, outline);
            Fill(pixels, width, 3, 5, 5, 10, bone);
            Fill(pixels, width, 18, 5, 20, 10, bone);
        }

        private static void DrawCyanRune(Color32[] pixels, int width, int height)
        {
            Color32 dark = new(43, 33, 30, 255);
            Color32 cyan = new(100, 244, 229, 230);
            for (int index = 5; index <= 26; index++)
            {
                Set(pixels, width, index, 5, dark);
                Set(pixels, width, index, 26, dark);
                Set(pixels, width, 5, index, dark);
                Set(pixels, width, 26, index, dark);
            }
            for (int index = 8; index <= 23; index++)
            {
                Set(pixels, width, index, 8, cyan);
                Set(pixels, width, index, 23, cyan);
                Set(pixels, width, 8, index, cyan);
                Set(pixels, width, 23, index, cyan);
            }
        }

        private static void DrawLine(Color32[] pixels, int width, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                Set(pixels, width, x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int doubled = 2 * error;
                if (doubled >= dy) { error += dy; x0 += sx; }
                if (doubled <= dx) { error += dx; y0 += sy; }
            }
        }

        private static void Fill(Color32[] pixels, int width, int minimumX, int minimumY, int maximumX, int maximumY, Color32 color)
        {
            for (int y = minimumY; y <= maximumY; y++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                {
                    Set(pixels, width, x, y, color);
                }
            }
        }

        private static void Set(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= width || y >= pixels.Length / width) return;
            pixels[(y * width) + x] = color;
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string[] segments = assetPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }

        public static RuntimeAnimatorController LoadOrCreatePlayerAnimator()
        {
            string controllerPath = $"{GeneratedRoot}/NomadAnimatorController.controller";
            var existingController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (existingController != null) return existingController;

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRolling", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDigging", AnimatorControllerParameterType.Bool);

            AnimationClip idleClip = CreateClip($"{GeneratedRoot}/Player_Idle.anim", "Idle");
            AnimationClip runClip = CreateClip($"{GeneratedRoot}/Player_Run.anim", "Run");
            AnimationClip rollClip = CreateClip($"{GeneratedRoot}/Player_Roll.anim", "Roll");
            AnimationClip digClip = CreateClip($"{GeneratedRoot}/Player_Dig.anim", "Dig");

            var rootStateMach = controller.layers[0].stateMachine;
            var idleState = rootStateMach.AddState("Idle");
            idleState.motion = idleClip;

            var runState = rootStateMach.AddState("Run");
            runState.motion = runClip;

            var rollState = rootStateMach.AddState("Roll");
            rollState.motion = rollClip;

            var digState = rootStateMach.AddState("Dig");
            digState.motion = digClip;

            rootStateMach.defaultState = idleState;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip CreateClip(string path, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;

            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
